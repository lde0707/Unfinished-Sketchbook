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
        public int columns=4,rows=3;
        public EnemyClipSource[] clips;
    }
    [Serializable] public class EnemyPlacement { public int sourceIndex; public string id; }
    [Serializable] public class EnemyAnimationManifest { public EnemyArtSource[] enemies; public EnemyPlacement[] placements; }

    // Animation prototype: authored patrol area, readable attacks, hit/death transitions.
    // Projectiles are timed to the authored attack; HP damage and progression remain separate.
    public sealed class PaperEnemyAnimation : MonoBehaviour
    {
        static EnemyAnimationManifest manifest;
        static readonly Dictionary<string,Sprite[]> sheets=new Dictionary<string,Sprite[]>();
        public string SpeciesId {get;private set;}
        public string StateName => source.clips[state].name;
        public int Health {get;private set;}=3;
        public int FrameIndex {get;private set;}
        EnemyArtSource source;
        SpriteRenderer visual;
        PaperAttackTarget receiver;
        WorldPrototype world;
        Sprite[] frames;
        Vector3 origin;
        float clock,cooldown=1,deadTime,stepDistance;bool attackSound;
        int state,direction=1,resetSerial;
        float width;
        Vector3 attackOrigin,patrolOrigin;
        Vector2 attackAim;
        Vector2 attackTarget;
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
        bool deathParticles;
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
            var component=marker.gameObject.AddComponent<PaperEnemyAnimation>();component.Initialize(data);return true;
        }
        public void Initialize(EnemyArtSource data)
        {
            source=data;SpeciesId=data.id;visual=GetComponent<SpriteRenderer>();
            world=FindAnyObjectByType<WorldPrototype>();resetSerial=world!=null?world.ResetSerial:0;
            if(!sheets.TryGetValue(data.id,out frames))
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
            origin=patrolOrigin=transform.position;visual.sprite=frames[0];visual.color=Color.white;
            width=Mathf.Max(.25f,visual.bounds.size.x*.33f);
            receiver=GetComponent<PaperAttackTarget>();if(receiver==null)receiver=gameObject.AddComponent<PaperAttackTarget>();
            receiver.onHit.AddListener(OnHit);
        }
        void OnDestroy(){if(receiver!=null)receiver.onHit.RemoveListener(OnHit);}
        void OnHit()
        {
            if(state==2)return;
            Health--; // Nonlethal hits keep the current animation and its time intact.
            if(Health<=0){GameAudio.Emit(SoundCue.Death,transform.position);SetState(2);receiver.enabled=false;deadTime=0;deathParticles=false;}
        }
        void SetState(int value)
        {
            state=value;clock=0;attackSound=false;
            if(value==1){
                attackOrigin=transform.position;attackMotion=EnemyAttackMotion.For(SpeciesId);
                Vector2 target=world!=null?(Vector2)world.player.position:(Vector2)attackOrigin+Vector2.right*direction*attackMotion.distance;
                attackTarget=target;
                attackAim=EnemyAttackMotion.Aim(attackMotion,target-(Vector2)attackOrigin);
            }
            Render();
        }
        public void ResetEnemy()
        {
            Health=3;clock=deadTime=0;deathParticles=false;state=0;cooldown=1;transform.position=patrolOrigin=origin;
            visual.enabled=true;visual.color=Color.white;receiver.enabled=true;Render();
        }
        void Update(){Tick(Time.deltaTime);}
        public void Tick(float dt)
        {
            if(source==null)return;
            if(world!=null&&world.ResetSerial!=resetSerial){resetSerial=world.ResetSerial;ResetEnemy();}
            if(world!=null&&world.MapOpen)return;
            // Keep off-screen actors still rather than attacking the player from outside view.
            if(world!=null&&Vector2.Distance(world.player.position,transform.position)>16&&state<2)return;
            dt=Mathf.Min(dt,.05f);clock+=dt;cooldown-=dt;
            float duration=0;foreach(float d in source.clips[state].durations)duration+=d;
            if(state==2){
                deadTime+=dt;
                float fadeStart=duration*.7f;
                if(deadTime>=fadeStart&&!deathParticles){deathParticles=true;PaperVfx.Get()?.Death(BodyBounds);}
                float fade=Mathf.SmoothStep(0,1,Mathf.InverseLerp(fadeStart,duration+.55f,deadTime));
                visual.color=new Color(1,1,1,1-fade);
                if(fade>=1)visual.enabled=false;
                Render();return;
            }
            if(state==1)
            {
                var timings=source.clips[1].motionDurations??source.clips[1].durations;
                if(!attackSound&&clock>=timings[0]+timings[1]){
                    attackSound=true;GameAudio.Emit(SoundCue.EnemyAttack,transform.position,.65f);
                    if(EnemyProjectileSystem.IsShooter(SpeciesId)&&world!=null&&(!Application.isPlaying||OnScreen()))
                    {
                        var system=EnemyProjectileSystem.Get(world);
                        if(system!=null&&system.Fire(this,SpeciesId,ProjectileMuzzle,attackTarget,direction))ProjectileShots++;
                    }
                }
                MoveAttack();
                if(clock>=duration){patrolOrigin=transform.position;SetState(0);cooldown=.8f;}
            }
            if(state==0)
            {
                if(world!=null)
                {
                    Vector2 delta=world.player.position-transform.position;
                    bool shooter=EnemyProjectileSystem.IsShooter(SpeciesId);
                    float reach=shooter?7.5f:SpeciesId=="D01"?1.65f:3.2f;
                    float verticalReach=shooter?4:(SpeciesId=="B03"||SpeciesId=="C01"||SpeciesId=="COMMON02")?5:2;
                    Vector2 sight=shooter?Muzzle(delta.x>=0?1:-1):(Vector2)transform.position+Vector2.up*.15f;
                    if(cooldown<=0&&(!shooter||OnScreen())&&Mathf.Abs(delta.x)<reach&&Mathf.Abs(delta.y)<verticalReach&&PaperSpritePlayer.AttackPathClear(world.Motor,sight,world.player.position))
                    {direction=delta.x>=0?1:-1;SetState(1);}
                }
                if(state==0&&!source.stationary){var prior=transform.position;Patrol(dt);stepDistance+=Vector2.Distance(prior,transform.position);if(stepDistance>.65f){stepDistance=0;GameAudio.Emit(SoundCue.EnemyMove,transform.position,source.flying?.5f:1);}}
            }
            visual.flipX=direction<0;Render();
        }
        void Patrol(float dt)
        {
            if(SpeciesId=="D01"&&world!=null)
            {
                Vector2 delta=world.player.position-transform.position;
                if(Mathf.Abs(delta.x)<6&&Mathf.Abs(delta.y)<2)
                {
                    direction=delta.x>=0?1:-1;
                    MoveSafely((Vector2)transform.position+Vector2.right*direction*1.1f*dt,true,transform.position.y);
                    patrolOrigin=transform.position;return;
                }
            }
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
            MoveSafely(new Vector2(next,patrolOrigin.y),!source.flying,patrolOrigin.y);
        }
        void MoveAttack()
        {
            var clip=source.clips[1];
            var timings=clip.motionDurations??clip.durations;
            Vector2 offset=source.flying?EnemyAttackMotion.AimedOffset(attackAim,clock,timings,source.stationary):EnemyAttackMotion.Offset(attackMotion,clock,timings,direction);
            Vector2 desired=(Vector2)attackOrigin+offset;
            MoveSafely(desired,!source.flying,attackOrigin.y);
        }
        void MoveSafely(Vector2 desired,bool guardEdge,float groundY)
        {
            // Flying sprite pivots are centered; collision sweep uses a bottom anchor.
            float pivotOffset=source.flying?source.bodyHeight*.5f:0;
            Vector2 from=(Vector2)transform.position-Vector2.up*pivotOffset;
            Vector2 result=EnemyAttackMotion.Sweep(from,desired-Vector2.up*pivotOffset,Mathf.Min(width,.55f),source.bodyHeight*.85f,world!=null?world.Motor.Get:null,guardEdge,groundY);
            transform.position=new Vector3(result.x,result.y+pivotOffset,transform.position.z);
        }
        void Render()
        {
            float t=clock;var durations=source.clips[state].durations;
            if(state==0){float sum=0;foreach(float d in durations)sum+=d;t=Mathf.Repeat(t,sum);}
            int f=0;while(f<durations.Length-1&&t>=durations[f]){t-=durations[f];f++;}
            int start=0;for(int i=0;i<state;i++)start+=source.clips[i].durations.Length;
            FrameIndex=start+f;visual.sprite=frames[FrameIndex];
        }
    }
}
