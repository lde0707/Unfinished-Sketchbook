using System;
using System.Collections.Generic;
using UnityEngine;
namespace Unfin
{
    [Serializable] public class BossClip
    {
        public string name;public float[] durations;
        [NonSerialized] AnimationTimeline timeline;
        [NonSerialized] float[] timingSource;
        AnimationTimeline Timeline=>AnimationTimeline.Get(ref timeline,ref timingSource,durations);
        public float Duration=>Timeline.Duration;
        public int Frame(float time)=>Timeline.FrameAt(time,false);
    }
    [Serializable] public class BossPattern {public string name,label,response;public float activeStart,activeEnd,secondStart,secondEnd,leapStart,reach,moveSpeed,pulseGap,pulseLife,projectileSpeed;}
    [Serializable] public class BossArt {public string name;public int sourceIndex,health,canvas,effectCount;public float anchorX,groundY,pixelsPerUnit,bodyWidth,bodyHeight;public BossClip[] clips;public BossPattern[] patterns;public BossClip Clip(string value)=>Array.Find(clips,c=>c.name==value);}
    public sealed class PaperBossRegion1 : MonoBehaviour
    {
        public const int SourceIndex=47;
        static BossArt art;static readonly Dictionary<string,Sprite[]> drawings=new Dictionary<string,Sprite[]>();
        public static BossArt Art=>art??(art=JsonUtility.FromJson<BossArt>(Resources.Load<TextAsset>("BossRegion1/manifest").text));
        public static Sprite[] Frames(string name){
            if(drawings.TryGetValue(name,out var cached)&&cached.Length>0&&cached[0]!=null)return cached;
            var a=new Sprite[Art.Clip(name).durations.Length];for(int i=0;i<a.Length;i++){var t=Resources.Load<Texture2D>("BossRegion1/"+name+"/"+i.ToString("D2"));if(t==null)throw new Exception("Missing boss frame "+name+i);a[i]=Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(Art.anchorX/t.width,1-Art.groundY/t.height),Art.pixelsPerUnit,0,SpriteMeshType.FullRect);}return drawings[name]=a;
        }
        static Sprite Fx(int i){var t=Resources.Load<Texture2D>("BossRegion1/Effects/"+i.ToString("D2"));return Sprite.Create(t,new Rect(0,0,t.width,t.height),Vector2.one*.5f,256);}
        public int Health {get;private set;}=30;
        public int Pattern {get;private set;}=-1;
        public int FrameIndex {get;private set;}
        public float EffectClock=>clock;
        public System.Collections.Generic.IReadOnlyList<BossDangerShape> EffectRanges=>zones;
        public string StateName=>dead?"Death":stagger>0?"Stagger":Pattern<0?(moving?"Move":"Idle"):Names[Pattern];
        public bool Staggered=>stagger>0;
        public PaperAttackTarget ExposedKnot=>knotReceiver;
        public bool Dead=>dead;
        public bool Engaged {get;private set;}
        public int CompletedPatterns {get;private set;}
        public int PatternMask {get;private set;}
        public int ActiveShapes {get{int n=0;foreach(var s in zones)if(s.Damaging)n++;return n;}}
        public int LiveProjectiles {get{int n=0;foreach(var s in shots)if(s.life>0)n++;return n;}}
        public Bounds BodyBounds=>new Bounds(transform.position+Vector3.up*Art.bodyHeight*.5f,new Vector3(Art.bodyWidth,Art.bodyHeight,.1f));
        public static readonly string[] Names={"Sweep","Slam","Rush","Roots","Fan","Cross"};
        WorldPrototype world;PaperSpritePlayer player;SpriteRenderer visual,aura;PaperAttackTarget receiver;
        BossDangerShape[] zones;readonly List<Shot> shots=new List<Shot>();Sprite[] fx;
        readonly int[] bag={0,1,2,3,4,5};int bagAt=6,last=-1,serial,facing=1,cycle;
        float clock,pause=.2f,floor,minX,maxX,aimX,stagger;Vector3 origin,attackOrigin;bool dead,rewarded,emitted,deathDust,moving;
        SpriteRenderer knotVisual;PaperAttackTarget knotReceiver;
        WorldArea room;Material material;MaterialPropertyBlock properties;
        class Shot{public BossDangerShape shape;public Vector2 position,velocity;public float life,gravity,fxCooldown;}
        public static bool Attach(MapMarker marker){
            if(!marker.enemy||marker.sourceIndex!=SourceIndex)return false;
            if(marker.GetComponent<PaperBossRegion1>()==null)marker.gameObject.AddComponent<PaperBossRegion1>().Initialize(FindAnyObjectByType<WorldPrototype>());
            return true;
        }
        public void Initialize(WorldPrototype w){
            world=w;player=w.player.GetComponentInChildren<PaperSpritePlayer>();serial=w.ResetSerial;
            visual=GetComponent<SpriteRenderer>();transform.localScale=Vector3.one;
            room=Array.Find(w.Data.rooms,r=>r.r==1&&r.n==22);minX=room.x+2;maxX=room.x+room.w-2;
            floor=-w.Data.enemies[SourceIndex].floor;transform.position=new Vector3(w.Data.enemies[SourceIndex].x/16+Art.bodyWidth*.5f,floor,0);origin=transform.position;
            visual.sortingOrder=22;visual.color=Color.white;
            material=new Material(Resources.Load<Shader>("BossRegion1/BossPaper"));visual.sharedMaterial=material;
            foreach(var clip in Art.clips)Frames(clip.name); // Upload once before combat, never stall at a new pattern.
            fx=new Sprite[Art.effectCount];for(int i=0;i<fx.Length;i++)fx[i]=Fx(i);
            zones=new BossDangerShape[5];for(int i=0;i<5;i++)zones[i]=new BossDangerShape(null,fx[i==0?0:i==1?1:3]);
            for(int i=0;i<20;i++)shots.Add(new Shot{shape=new BossDangerShape(null,fx[4],24)});
            var ag=new GameObject("Quiet graphite aura");ag.transform.SetParent(transform,false);aura=ag.AddComponent<SpriteRenderer>();aura.sprite=fx[6];aura.sortingOrder=21;aura.color=new Color(.8f,.76f,.68f,.2f);ag.transform.localPosition=Vector3.up*.8f;ag.transform.localScale=Vector3.one*1.8f;
            receiver=GetComponent<PaperAttackTarget>();if(receiver==null)receiver=gameObject.AddComponent<PaperAttackTarget>();receiver.onHit.AddListener(Hit);
            var kg=new GameObject("Cuttable forearm knot");kg.transform.SetParent(transform,false);kg.transform.localScale=Vector3.one*.38f;knotVisual=kg.AddComponent<SpriteRenderer>();knotVisual.sprite=fx[8];knotVisual.sortingOrder=25;knotReceiver=kg.AddComponent<PaperAttackTarget>();knotReceiver.onHit.AddListener(BreakKnot);HideKnot();
            if(w.Progression.DefeatedEncounters.Contains(SourceIndex)){dead=rewarded=true;Health=0;clock=Art.clips[7].Duration;receiver.enabled=false;}
            Render();
        }
        void Hit(){if(dead)return;if(--Health>0)return;Health=0;dead=true;EnemyRewardDrops.Emit(world,BodyBounds.center,60);world.Progression.DefeatedEncounters.Add(SourceIndex);clock=0;Pattern=-1;stagger=0;receiver.enabled=false;HideKnot();HideRanges();ClearShots();GameAudio.Emit(SoundCue.Death,transform.position);}
        void HideKnot(){if(knotVisual!=null)knotVisual.enabled=false;if(knotReceiver!=null)knotReceiver.enabled=false;}
        void BreakKnot(){if(dead||Pattern!=3||!world.Progression.Has("cut"))return;Pattern=-1;clock=0;stagger=Art.Clip("Stagger").Duration;moving=false;HideKnot();HideRanges();ClearShots();PaperVfx.Get()?.Hit(knotVisual.transform.position,facing);world.Progression.Say("흑연 매듭이 끊어졌습니다",1.4f);}
        void OnDestroy(){if(receiver!=null)receiver.onHit.RemoveListener(Hit);if(zones!=null)foreach(var z in zones)z.Dispose();foreach(var s in shots)s.shape.Dispose();if(material!=null){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}}
        void Update(){Tick(Mathf.Min(Time.deltaTime,.05f));}
        public void Tick(float dt){
            if(world==null||zones==null)return;
            if(serial!=world.ResetSerial){serial=world.ResetSerial;if(!world.Progression.DefeatedEncounters.Contains(SourceIndex))ResetBoss();}
            if(world.MapOpen)return;
            if(dt>1f/120){while(dt>1f/120){Tick(1f/120);dt-=1f/120;}if(dt>0)Tick(dt);return;}
            if(dead){clock+=dt;if(clock>.4f&&!deathDust){deathDust=true;PaperVfx.Get()?.Death(BodyBounds);}if(clock>=Art.clips[7].Duration&&!rewarded){rewarded=true;world.Progression.DefeatedEncounters.Add(SourceIndex);world.Progression.Grant("dash");}Render();return;}
            Vector2 pp=world.player.position;
            bool inside=pp.x>=room.x+1&&pp.x<=room.x+room.w-1&&pp.y<=-room.y&&pp.y>=-(room.y+room.h+1);
            if(!inside||world.HazardInputBlocked){Engaged=false;Pattern=-1;pause=.3f;clock=stagger=0;moving=false;transform.position=new Vector3(transform.position.x,floor,0);HideKnot();HideRanges();ClearShots();Render();return;}
            Engaged=true;
            TickShots(dt);
            // Later pressure comes from shorter decisions, not unreadably accelerated wind-ups.
            float tempo=1;
            if(stagger>0){clock+=dt;stagger=Mathf.Max(0,stagger-dt);if(stagger<=0){clock=0;pause=.14f;}Render();return;}
            if(Pattern<0){
                clock+=dt;pause-=dt;facing=pp.x>=transform.position.x?1:-1;
                moving=Mathf.Abs(pp.x-transform.position.x)>3.4f;if(moving)MoveHorizontal(facing*1.7f*dt);
                if(Mathf.Abs(pp.x-transform.position.x)>6.5f)pause=Mathf.Max(pause,.1f);else if(pause<=0)BeginPattern(NextPattern());
                Render();return;
            }
            float previous=clock;clock+=dt*tempo;
            var spec=Art.patterns[Pattern];
            HideRanges();bool strike=false;
            switch(Pattern){
                case 0:{
                    bool active=clock>=spec.activeStart&&clock<spec.activeEnd;
                    if(clock<spec.activeEnd)zones[0].Show((Vector2)transform.position+new Vector2(facing*1.6f,.65f),new Vector2(1.8f,.7f),0,active);
                    strike=active;break;}
                case 1:{
                    if(clock>=spec.leapStart&&clock<spec.activeStart){float t=(clock-spec.leapStart)/(spec.activeStart-spec.leapStart);float x=Mathf.Lerp(attackOrigin.x,aimX,t);transform.position=new Vector3(x,floor+Mathf.Sin(t*Mathf.PI)*1.8f,0);}
                    else if(clock>=spec.activeStart)transform.position=new Vector3(aimX,floor,0);
                    bool active=clock>=spec.activeStart&&clock<spec.activeEnd;
                    if(clock<spec.activeEnd)zones[1].Show(new Vector2(aimX,floor+.3f),new Vector2(spec.reach,.6f),0,active);
                    if(previous<spec.activeStart&&clock>=spec.activeStart)PaperVfx.Get()?.Dust(new Vector2(aimX,floor),2.2f);
                    strike=active;break;}
                case 2:{
                    bool active=clock>=spec.activeStart&&clock<spec.activeEnd;
                    if(active)MoveHorizontal(facing*spec.moveSpeed*dt);
                    if(clock<spec.activeEnd)zones[0].Show((Vector2)transform.position+new Vector2(facing*.55f,.95f),new Vector2(1.55f,1.02f),0,active);
                    strike=active;break;}
                case 3:{
                    knotVisual.enabled=true;knotReceiver.enabled=true;knotVisual.transform.localPosition=new Vector3(facing*1.65f,.65f,0);
                    for(int i=0;i<3;i++){float start=spec.activeStart+i*spec.pulseGap;bool active=clock>=start&&clock<start+spec.pulseLife;
                        if(clock<start+spec.pulseLife)zones[i+2].Show(new Vector2(Mathf.Clamp(aimX+(i-1)*2.2f,minX+.5f,maxX-.5f),floor+1.05f),new Vector2(.48f,1.05f),0,active);
                        strike|=active;}
                    break;}
                case 4:{
                    if(clock<spec.activeStart)zones[0].Show((Vector2)transform.position+new Vector2(facing*1.65f,1.25f),new Vector2(.65f,.38f),0,false);
                    if(!emitted&&clock>=spec.activeStart){emitted=true;var muzzle=(Vector2)transform.position+new Vector2(facing*1.65f,1.25f);SpawnFan(muzzle,new Vector2(aimX-muzzle.x,.65f-muzzle.y+floor).normalized,5,64,spec.projectileSpeed,0);GameAudio.Emit(SoundCue.EnemyAttack,muzzle,.7f);}
                    break;}
                case 5:{
                    bool first=clock>=spec.activeStart&&clock<spec.activeEnd,second=clock>=spec.secondStart&&clock<spec.secondEnd;
                    if(clock<spec.activeEnd)zones[0].Show((Vector2)transform.position+new Vector2(facing*1.6f,1.05f),new Vector2(1.8f,.5f),-25*facing,first);
                    else if(clock<spec.secondEnd)zones[0].Show((Vector2)transform.position+new Vector2(facing*1.6f,1.05f),new Vector2(1.8f,.5f),25*facing,second);
                    strike=first||second;break;}
            }
            if(strike&&!emitted&&Pattern!=4){emitted=true;GameAudio.Emit(SoundCue.EnemyAttack,transform.position,.7f);PaperVfx.Get()?.Swing(transform.position+Vector3.up*.6f,facing);}
            foreach(var zone in zones)if(zone.Overlaps(PlayerBounds()))DamagePlayer(zone.Center);
            if(clock>=Art.clips[Pattern+1].Duration){last=Pattern;CompletedPatterns++;Pattern=-1;clock=0;pause=Health<=15?.12f:.18f;transform.position=new Vector3(transform.position.x,floor,0);HideKnot();HideRanges();}
            Render();
        }
        int NextPattern(){
            if(bagAt>=6){for(int i=0;i<6;i++)bag[i]=i;for(int i=5;i>0;i--){int j=UnityEngine.Random.Range(0,i+1),swap=bag[i];bag[i]=bag[j];bag[j]=swap;}cycle++;bagAt=0;}
            int p=bag[bagAt++];if(p==last&&bagAt<6){p=bag[bagAt];bag[bagAt]=bag[bagAt-1];bag[bagAt-1]=p;}return p;
        }
        public void BeginPattern(int value){if(dead||value<0||value>5)return;HideKnot();stagger=0;moving=false;transform.position=new Vector3(transform.position.x,floor,0);Pattern=value;PatternMask|=1<<value;clock=0;emitted=false;attackOrigin=transform.position;facing=world.player.position.x>=transform.position.x?1:-1;aimX=Mathf.Clamp(world.player.position.x,Mathf.Max(minX,attackOrigin.x-6.5f),Mathf.Min(maxX,attackOrigin.x+6.5f));zones[0].SetArt(fx[value==2?2:value==4?4:value==5?5:0]);HideRanges();}
        void MoveHorizontal(float amount){
            float x=Mathf.Clamp(transform.position.x+amount,minX,maxX),edge=x+Mathf.Sign(amount)*Art.bodyWidth*.5f;
            if(!GridMotor.Wall(world.Motor.Get(Mathf.FloorToInt(edge),Mathf.FloorToInt(-floor-.5f))))transform.position=new Vector3(x,transform.position.y,0);
        }
        Bounds PlayerBounds()=>world.Motor.WorldBounds;
        void DamagePlayer(Vector2 point){if(player!=null&&!world.HazardInputBlocked&&PaperSpritePlayer.AttackPathClear(world.Motor,point,world.player.position))player.TryReceiveHit(point,point.x<world.player.position.x?1:-1);}
        void SpawnFan(Vector2 from,Vector2 forward,int count,float spread,float speed,float gravity){
            float baseAngle=Mathf.Atan2(forward.y,forward.x)*Mathf.Rad2Deg;
            for(int i=0;i<count;i++){var s=shots.Find(a=>a.life<=0);if(s==null)break;float a=(baseAngle-spread*.5f+spread*i/(count-1))*Mathf.Deg2Rad;s.position=from;s.velocity=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*speed;s.gravity=gravity;s.life=1.5f;}
        }
        void TickShots(float dt){foreach(var s in shots){if(s.life<=0)continue;s.life-=dt;s.velocity+=Vector2.down*s.gravity*dt;s.position+=s.velocity*dt;
            if(s.life<=0||GridMotor.Wall(world.Motor.Get(Mathf.FloorToInt(s.position.x),Mathf.FloorToInt(-s.position.y)))){s.life=0;s.shape.Hide();continue;}
            s.fxCooldown-=dt;if(s.fxCooldown<=0){s.fxCooldown=.055f;EnemyCombatEffects.Get(world)?.Projectile(s.position,s.velocity,new Color(.62f,.58f,.5f),false);}
            s.shape.Show(s.position,Vector2.one*.17f,Mathf.Atan2(s.velocity.y,s.velocity.x)*Mathf.Rad2Deg,true);
            if(s.shape.Overlaps(PlayerBounds())){DamagePlayer(s.position);s.life=0;s.shape.Hide();}
        }}
        void ClearShots(){foreach(var s in shots){s.life=0;s.shape.Hide();}}
        void HideRanges(){if(zones!=null)foreach(var s in zones)s.Hide();}
        void ResetBoss(){Health=Art.health;dead=rewarded=deathDust=moving=false;stagger=0;Engaged=false;Pattern=-1;clock=0;pause=.3f;bagAt=6;last=-1;CompletedPatterns=PatternMask=0;transform.position=origin;visual.enabled=true;visual.color=Color.white;receiver.enabled=true;receiver.ResetFeedback();HideKnot();HideRanges();ClearShots();}
        void Render(){
            var clip=Art.Clip(StateName);float t=Pattern<0&&!dead&&stagger<=0?Mathf.Repeat(clock,clip.Duration):clock;
            FrameIndex=clip.Frame(t);visual.sprite=Frames(clip.name)[FrameIndex];visual.flipX=facing<0;
            if(properties==null)properties=new MaterialPropertyBlock();visual.GetPropertyBlock(properties);properties.SetFloat("_AttackReadability",Pattern>=0?1:0);visual.SetPropertyBlock(properties);
            aura.enabled=!dead||clock<Art.clips[7].Duration;
            aura.sprite=fx[dead?7:6];aura.color=new Color(.8f,.76f,.68f,dead?.32f*Mathf.Clamp01(1-clock/Art.clips[7].Duration):Pattern<0?.18f:.07f);
        }
        void OnGUI(){
            if(!Engaged||dead)return;
            float scale=Mathf.Min(Screen.width/1920f,Screen.height/1080f);var old=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));float x=(Screen.width/scale-500)*.5f,y=Screen.height/scale-150;
            var style=new GUIStyle(GUI.skin.label){fontSize=20,alignment=TextAnchor.MiddleCenter};GUI.Label(new Rect(x,y-30,500,26),Art.name,style);GUI.color=new Color(.11f,.1f,.09f,.9f);GUI.DrawTexture(new Rect(x,y,500,10),Texture2D.whiteTexture);GUI.color=new Color(.68f,.57f,.43f);GUI.DrawTexture(new Rect(x,y,500*Health/Art.health,10),Texture2D.whiteTexture);GUI.color=Color.white;GUI.matrix=old;
        }
    }
}
