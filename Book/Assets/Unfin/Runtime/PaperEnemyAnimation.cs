using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unfin
{
    [Serializable] public class EnemyClipSource { public string name; public float[] durations,motionDurations; }
    [Serializable] public class EnemyArtSource
    {
        public string id,name,group;
        public float pixelsPerUnit,pivotY,bodyWidth,bodyHeight;
        public bool stationary,flying;
        public int health;
        public float meleeReach;
        public float[] attackWindows;
        public int columns=4,rows=3;
        public EnemyClipSource[] clips;
    }
    [Serializable] public class EnemyPlacement { public int sourceIndex; public string id; public float overrideX; }
    [Serializable] public class EnemyAnimationManifest { public EnemyArtSource[] enemies; public EnemyPlacement[] placements; }

    // Animation prototype: authored patrol area, readable attacks, hit/death transitions.
    // Projectiles are timed to the authored attack; HP damage and progression remain separate.
    public sealed partial class PaperEnemyAnimation : MonoBehaviour
    {
        static EnemyAnimationManifest manifest;
        static readonly Dictionary<string,Sprite[]> sheets=new Dictionary<string,Sprite[]>();
        public string SpeciesId {get;private set;}
        public string StateName => source.clips[state].name;
        public int Health {get;private set;}=3;
        public int FrameIndex {get;private set;}
        public float EffectClock=>clock;
        EnemyArtSource source;
        SpriteRenderer visual;
        PaperAttackTarget receiver;
        WorldPrototype world;
        MapMarker marker;
        Sprite[] frames;
        Vector3 origin;
        readonly FiniteStateMachine<EnemyAnimationState> animation=new FiniteStateMachine<EnemyAnimationState>(EnemyAnimationState.Move);
        float clock {get=>animation.Time;set=>animation.Time=value;}
        AnimationTimeline[] timelines;
        int[] frameStarts;
        const float AttackPlayback=1.12f,AttackCooldown=.6f,InitialCooldown=.75f;
        float cooldown=InitialCooldown,deadTime,stepDistance,stun;bool attackSound,secondShot;
        int parryMask;
        bool Region2=>SpeciesId!=null&&(SpeciesId.StartsWith("R2")||"EFGH".Contains(SpeciesId.Substring(0,1)));
        int state=>(int)animation.State;
        int direction=1,resetSerial;
        float width;
        Vector3 attackOrigin,patrolOrigin;
        Vector2 attackAim;
        Vector2 attackTarget,hopLanding;
        public int ProjectileShots {get;private set;}
        public Vector2 ProjectileMuzzle=>Muzzle(direction);
        Vector2 Muzzle(int side)
        {
            float x=SpeciesId=="R102"?.55f:SpeciesId=="COMMON03"?.27f:.45f;
            float y=SpeciesId=="R102"?.65f:SpeciesId=="COMMON03"?.8f:.55f;
            return (Vector2)transform.position+new Vector2(side*source.bodyWidth*x,source.bodyHeight*y);
        }
        bool OnScreen()
        {
            if(world==null||world.worldCamera==null)return false;
            var p=world.worldCamera.WorldToViewportPoint(BodyBounds.center);
            return p.z>0&&p.x>.03f&&p.x<.97f&&p.y>.03f&&p.y<.97f;
        }
        bool deathParticles,enraged;
        EnemyAttackMotion.Profile attackMotion;
        public Bounds BodyBounds => new Bounds(transform.position+Vector3.up*(source.flying?0:source.bodyHeight*.5f),new Vector3(source.bodyWidth,source.bodyHeight,.1f));

        public static EnemyAnimationManifest Manifest
        {
            get
            {
                if(manifest==null){var text=Resources.Load<TextAsset>("EnemyAnimations/manifest");if(text!=null)manifest=JsonUtility.FromJson<EnemyAnimationManifest>(text.text);}
                return manifest;
            }
        }
        public static bool Attach(MapMarker marker)
        {
            if(!marker.enemy||Manifest?.placements==null)return false;
            var placement=Array.Find(Manifest.placements,p=>p.sourceIndex==marker.sourceIndex);
            if(placement==null)return false;
            var data=Array.Find(Manifest.enemies,e=>e.id==placement.id);if(data==null)return false;
            var component=marker.gameObject.AddComponent<PaperEnemyAnimation>();component.Initialize(data);if(placement.overrideX>0)component.Relocate(placement.overrideX);return true;
        }
        public void Initialize(EnemyArtSource data)
        {
            SpeciesId=data.id;specialty=EnemySpecialties.For(data.id);source=EnemySpecialties.Apply(data,specialty);data=source;
            timelines=new AnimationTimeline[data.clips.Length];frameStarts=new int[data.clips.Length];
            int start=0;
            for(int i=0;i<data.clips.Length;i++){
                var durations=data.clips[i].durations;
                timelines[i]=new AnimationTimeline(durations.Length,j=>durations[j]);
                frameStarts[i]=start;start+=durations.Length;
            }visual=GetComponent<SpriteRenderer>();
            marker=GetComponent<MapMarker>();
            enraged=Region2&&marker!=null&&(marker.originalOptions??"").Contains("\"red\":true");Health=(data.health>0?data.health:3)+(enraged?2:0);
            world=FindAnyObjectByType<WorldPrototype>();resetSerial=world!=null?world.ResetSerial:0;
            if(!sheets.TryGetValue(data.id,out frames)||frames.Length==0||frames[0]==null)
            {
                var texture=Resources.Load<Texture2D>("EnemyAnimations/"+data.id);
                if(texture==null)throw new InvalidOperationException("Missing enemy atlas "+data.id);
                int count=0;foreach(var clip in data.clips)count+=clip.durations.Length;
                int columns=data.columns>0?data.columns:4,rows=data.rows>0?data.rows:3;
                frames=new Sprite[count];int cw=texture.width/columns,ch=texture.height/rows;
                for(int i=0;i<count;i++)frames[i]=Sprite.Create(texture,new Rect((i%columns)*cw,(rows-1-i/columns)*ch,cw,ch),new Vector2(.5f,data.pivotY),data.pixelsPerUnit,0,SpriteMeshType.Tight);
                sheets[data.id]=frames;
            }
            float floor=visual.bounds.min.y;
            transform.localScale=Vector3.one;
            if(!source.flying)transform.position=new Vector3(transform.position.x,floor,0);
            origin=patrolOrigin=transform.position;visual.sprite=frames[0];visual.color=enraged?new Color(1,.78f,.72f):Color.white;
            width=Mathf.Max(.25f,visual.bounds.size.x*.33f);var m=marker;if(m!=null&&(m.sourceIndex==74||m.sourceIndex==75))gameObject.AddComponent<Region2KeeperVisual>().Initialize(world,m.sourceIndex);
            receiver=GetComponent<PaperAttackTarget>();if(receiver==null)receiver=gameObject.AddComponent<PaperAttackTarget>();
            receiver.onHit.AddListener(OnHit);
        }
        public void Relocate(float x){transform.position=new Vector3(x,transform.position.y,0);origin=patrolOrigin=transform.position;}
        void OnDestroy(){if(receiver!=null)receiver.onHit.RemoveListener(OnHit);}
        void OnHit()
        {
            if(animation.State==EnemyAnimationState.Death)return;
            if(marker!=null&&world!=null){if(marker.sourceIndex==74&&world.Motor.Get(459,88)!=4)return;if(marker.sourceIndex==75&&TargetGateClosed())return;}
            Health--; // Nonlethal hits keep the current animation and its time intact.
            if(Health<=0){EnemyRewardDrops.Emit(world,BodyBounds.center,enraged?6:source.health>=5?4:2);GameAudio.Emit(SoundCue.Death,transform.position);SetState(EnemyAnimationState.Death);receiver.enabled=false;deadTime=0;deathParticles=false;}
        }
        void SetState(EnemyAnimationState value)
        {
            animation.Change(value,true);attackSound=secondShot=false;parryMask=0;
            if(value==EnemyAnimationState.Attack){
                attackOrigin=transform.position;attackMotion=EnemyAttackMotion.For(SpeciesId);
                AimAttack();
            }
            Render();
        }
        void AimAttack()
        {
            attackTarget=world!=null?(Vector2)world.player.position:(Vector2)attackOrigin+Vector2.right*direction*attackMotion.distance;
            Vector2 delta=attackTarget-(Vector2)attackOrigin;
            if(Mathf.Abs(delta.x)>.12f)direction=delta.x>=0?1:-1;
            attackAim=EnemyAttackMotion.Aim(attackMotion,delta);
            if(specialty.lane==EnemyLane.Horizontal)attackAim=new Vector2(direction*attackMotion.distance,0);
            if(specialty.lane==EnemyLane.Vertical)attackAim=new Vector2(0,-Mathf.Min(attackMotion.drop,Mathf.Max(.4f,-delta.y)));
            if(SpeciesId=="G01"&&world!=null)hopLanding=FindHopLanding();
        }
        float MeleeTriggerReach()
        {
            if(specialty.lane==EnemyLane.Vertical)return SpeciesId=="B03"?.85f:1.25f;
            if(source.flying||SpeciesId=="G01")return SpeciesId=="R203"?5.5f:specialty.lane==EnemyLane.Horizontal?5:3.8f;
            // Short lunges must close the gap instead of swinging from 3.2 tiles away.
            float travel=EnemyAttackMotion.For(SpeciesId).distance;
            return Mathf.Clamp(travel+source.bodyWidth*.5f+source.meleeReach+.2f,.85f,3.2f);
        }
        public void ResetEnemy()
        {
            Health=(source.health>0?source.health:3)+(enraged?2:0);clock=deadTime=stun=turnWait=guardFlash=0;deathParticles=false;animation.Change(EnemyAnimationState.Move,true);cooldown=InitialCooldown;transform.position=patrolOrigin=origin;
            visual.enabled=true;visual.color=enraged?new Color(1,.78f,.72f):Color.white;receiver.enabled=true;receiver.ResetFeedback();Render();
        }
        void Update(){Tick(Time.deltaTime);}
        public void Tick(float dt)
        {
            if(source==null)return;
            if(world!=null&&world.ResetSerial!=resetSerial){resetSerial=world.ResetSerial;ResetEnemy();}
            if(world!=null&&world.MapOpen)return;
            turnWait=Mathf.Max(0,turnWait-dt);guardFlash=Mathf.Max(0,guardFlash-dt);
            if(stun>0){stun=Mathf.Max(0,stun-Mathf.Min(dt,.05f));Render();return;}
            // Keep off-screen actors still rather than attacking the player from outside view.
            if(world!=null&&Vector2.Distance(world.player.position,transform.position)>16&&animation.State!=EnemyAnimationState.Death)return;
            dt=Mathf.Min(dt,.05f);if(enraged&&animation.State!=EnemyAnimationState.Death)dt*=1.5f;
            // One clock drives painted frames, movement, projectiles, hit windows and VFX.
            // Enraged actors already have their own faster playback.
            float actionDt=dt*(animation.State==EnemyAnimationState.Attack&&!enraged?AttackPlayback:1);
            animation.Advance(actionDt);cooldown-=dt;
            float duration=timelines[state].Duration;
            if(animation.State==EnemyAnimationState.Death){
                deadTime+=dt;
                float fadeStart=duration*.7f;
                if(deadTime>=fadeStart&&!deathParticles){deathParticles=true;PaperVfx.Get()?.Death(BodyBounds);}
                float fade=Mathf.SmoothStep(0,1,Mathf.InverseLerp(fadeStart,duration+.55f,deadTime));
                visual.color=new Color(1,1,1,1-fade);
                if(fade>=1)visual.enabled=false;
                Render();return;
            }
            if(animation.State==EnemyAnimationState.Attack)
            {
                var timings=source.clips[1].motionDurations??source.clips[1].durations;
                float strikeStart=timings[0]+timings[1],strikeEnd=strikeStart+timings[2];
                float lockAt=Mathf.Min(strikeStart*.65f,strikeStart-.22f*(enraged?1.5f:AttackPlayback));
                if(clock<Mathf.Max(0,lockAt))AimAttack();
                if(!attackSound&&clock>=timings[0]+timings[1]){
                    attackSound=true;GameAudio.Emit(SoundCue.EnemyAttack,transform.position,.65f);
                    if(Region2)if(GetComponent<EnemyCombatVfx>()==null)Region2Presentation.Burst(world,transform.position+Vector3.up*.5f,SpeciesId,direction);
                    if(EnemyProjectileSystem.IsShooter(SpeciesId)&&world!=null&&(!Application.isPlaying||OnScreen()))
                    {
                        var system=EnemyProjectileSystem.Get(world);
                        if(system!=null&&system.Fire(this,SpeciesId,ProjectileMuzzle,attackTarget,direction))ProjectileShots++;
                    }
                }
                if(SpeciesId=="R201"&&attackSound&&!secondShot&&clock>=source.attackWindows[2]){secondShot=true;GameAudio.Emit(SoundCue.EnemyAttack,transform.position,.65f);if(GetComponent<EnemyCombatVfx>()==null)Region2Presentation.Burst(world,transform.position+Vector3.up*.5f,SpeciesId,direction);}
                if(SpeciesId=="H03"&&attackSound&&!secondShot&&clock>=timings[0]+timings[1]+.28f){secondShot=true;EnemyProjectileSystem.Get(world)?.Fire(this,SpeciesId,ProjectileMuzzle,attackTarget,direction);}
                var strikeFrom=BodyBounds;
                MoveAttack();
                if(!EnemyProjectileSystem.IsShooter(SpeciesId)){
                    if(source.attackWindows!=null&&source.attackWindows.Length>0){for(int i=0;i<source.attackWindows.Length;i+=2)if(clock>=source.attackWindows[i]&&clock-actionDt<source.attackWindows[i+1])ContactPlayer(strikeFrom,i/2);}
                    else if(clock>=strikeStart&&clock-actionDt<strikeEnd)ContactPlayer(strikeFrom,0);
                }
                if(animation.State==EnemyAnimationState.Attack&&clock>=duration){patrolOrigin=transform.position;SetState(EnemyAnimationState.Move);cooldown=specialty.cooldown;}
            }
            if(animation.State==EnemyAnimationState.Move)
            {
                if(world!=null)
                {
                    Vector2 delta=world.player.position-transform.position;
                    bool shooter=EnemyProjectileSystem.IsShooter(SpeciesId);
                    float reach=shooter?(marker!=null&&marker.sourceIndex==74?18:specialty.shotRange):MeleeTriggerReach();
                    float verticalReach=shooter?4:SpeciesId=="R203"?6:SpeciesId=="G01"?4:source.flying?6:2;
                    Vector2 sight=shooter?Muzzle(delta.x>=0?1:-1):(Vector2)transform.position+Vector2.up*.15f;
                    if(cooldown<=0&&(!Application.isPlaying||OnScreen())&&Mathf.Abs(delta.x)<reach&&Mathf.Abs(delta.y)<verticalReach&&PaperSpritePlayer.AttackPathClear(world.Motor,sight,world.player.position))
                    {direction=delta.x>=0?1:-1;SetState(EnemyAnimationState.Attack);}
                }
                if(animation.State==EnemyAnimationState.Move&&!source.stationary){var prior=transform.position;Patrol(dt);stepDistance+=Vector2.Distance(prior,transform.position);if(stepDistance>.65f){stepDistance=0;GameAudio.Emit(SoundCue.EnemyMove,transform.position,source.flying?.5f:1);}}
            }
            visual.flipX=direction<0;Render();
        }
        void Patrol(float dt)
        {
            if(specialty.retreat&&world!=null){var d=world.player.position-transform.position;if(Mathf.Abs(d.x)<2.8f&&Mathf.Abs(d.y)<1.5f){Face(d.x);MoveSafely((Vector2)transform.position-Vector2.right*direction*specialty.speed*dt,true,transform.position.y);patrolOrigin=transform.position;return;}}
            if(!source.flying&&!EnemyProjectileSystem.IsShooter(SpeciesId)&&world!=null)
            {
                Vector2 delta=world.player.position-transform.position;
                if(Mathf.Abs(delta.x)<specialty.awareness&&Mathf.Abs(delta.y)<1.5f&&
                    PaperSpritePlayer.AttackPathClear(world.Motor,BodyBounds.center,world.player.position))
                {
                    Face(delta.x);
                    if(Mathf.Abs(delta.x)>MeleeTriggerReach()*.8f)
                        MoveSafely((Vector2)transform.position+Vector2.right*direction*specialty.speed*dt,true,transform.position.y);
                    patrolOrigin=transform.position;return;
                }
            }
            if(source.flying&&specialty.lane==EnemyLane.Vertical)return;
            float next=transform.position.x+direction*.45f*dt;
            bool blocked=Mathf.Abs(next-patrolOrigin.x)>1.15f;
            if(world!=null)
            {
                int tx=Mathf.FloorToInt(next+direction*width),ty=Mathf.FloorToInt(-transform.position.y);
                blocked|=GridMotor.Wall(world.Motor.Get(tx,ty-1));
                if(!source.flying)blocked|=world.Motor.Get(tx,ty)==0||world.Motor.Get(tx,ty)==9;
                else blocked|=GridMotor.Wall(world.Motor.Get(tx,ty));
            }
            if(blocked){direction=-direction;return;}
            float height=source.flying&&specialty.lane==EnemyLane.Aim?Mathf.MoveTowards(transform.position.y,origin.y,dt*1.2f):patrolOrigin.y;
            MoveSafely(new Vector2(next,height),!source.flying,patrolOrigin.y);
        }
        Vector2 FindHopLanding(){var best=(Vector2)attackOrigin;float score=float.PositiveInfinity;for(float distance=3;distance>=.5f;distance-=.25f){float x=attackOrigin.x+direction*distance;for(int row=Mathf.FloorToInt(-attackOrigin.y)-3;row<=Mathf.FloorToInt(-attackOrigin.y)+3;row++){int a=world.Motor.Get(Mathf.FloorToInt(x-.35f),row),b=world.Motor.Get(Mathf.FloorToInt(x+.35f),row);if((!GridMotor.Wall(a)&&!GridMotor.Platform(a))||(!GridMotor.Wall(b)&&!GridMotor.Platform(b))||GridMotor.Wall(world.Motor.Get(Mathf.FloorToInt(x),row-1))||GridMotor.Wall(world.Motor.Get(Mathf.FloorToInt(x),row-2)))continue;float s=Mathf.Abs(-row+source.bodyHeight*.5f-attackTarget.y)+(3-distance)*.4f;if(s<score){score=s;best=new Vector2(x,-row);}}}return best;}
        void MoveAttack()
        {
            var clip=source.clips[1];
            var timings=clip.motionDurations??clip.durations;
            Vector2 offset=source.flying?EnemyAttackMotion.AimedOffset(attackAim,clock,timings,source.stationary||specialty.lane==EnemyLane.Vertical):EnemyAttackMotion.Offset(attackMotion,clock,timings,direction);
            Vector2 desired=(Vector2)attackOrigin+offset;
            if(SpeciesId=="G01"){float t=Mathf.Clamp01((clock-timings[0]-timings[1])/timings[2]);desired=Vector2.Lerp(attackOrigin,hopLanding,t)+Vector2.up*(1.2f*4*t*(1-t));MoveSafely(desired,false,hopLanding.y);}
            else MoveSafely(desired,!source.flying,attackOrigin.y);
        }
        void MoveSafely(Vector2 desired,bool guardEdge,float groundY)
        {
            // Flying sprite pivots are centered; collision sweep uses a bottom anchor.
            float pivotOffset=source.flying?source.bodyHeight*.5f:0;
            Vector2 from=(Vector2)transform.position-Vector2.up*pivotOffset;
            Vector2 result=EnemyAttackMotion.Sweep(from,desired-Vector2.up*pivotOffset,Mathf.Min(width,.55f),source.bodyHeight*.85f,world!=null?world.Motor.Get:null,guardEdge,groundY);
            transform.position=new Vector3(result.x,result.y+pivotOffset,transform.position.z);
        }
        bool TargetGateClosed(){for(int y=116;y<=124;y++)for(int x=400;x<=408;x++)if(world.Motor.Get(x,y)==13)return true;return false;}
        void ContactPlayer(Bounds from,int window)
        {
            if(world==null||world.player==null)return;
            var player=world.player.GetComponentInChildren<PaperSpritePlayer>();if(player==null)return;
            var m=world.Motor;
            var body=m.WorldBounds;
            var to=SpecialtyStrike(BodyBounds);
            from=SpecialtyStrike(from);
            int steps=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(from.center,to.center)/.06f));
            for(int i=0;i<=steps;i++){
                var strike=new Bounds(Vector3.Lerp(from.center,to.center,(float)i/steps),to.size);
                if(!strike.Intersects(body))continue;
                if(Region2&&world.Progression.Parrying){if((parryMask&(1<<window))==0){parryMask|=1<<window;world.Progression.ConfirmParry();PaperVfx.Get()?.Hit(body.center,-direction);GameAudio.Emit(SoundCue.Gate,body.center,.6f);if(SpeciesId!="R201"||parryMask==3){SetState(EnemyAnimationState.Move);cooldown=stun=1.2f;}}return;}
                Vector2 origin=strike.center,contact=body.ClosestPoint(origin);
                if(PaperSpritePlayer.AttackPathClear(m,origin,body.center))player.TryReceiveHit(contact,direction);
                break;
            }
        }
        void Render()
        {
            FrameIndex=frameStarts[state]+timelines[state].FrameAt(clock,animation.State==EnemyAnimationState.Move);visual.sprite=frames[FrameIndex];
            RenderSpecialty();
        }
    }
}
