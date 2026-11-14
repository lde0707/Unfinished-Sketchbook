using System;
using System.Collections.Generic;
using UnityEngine;
namespace Unfin
{
    [Serializable] public class EliteArtSource {public string id,name;public int sourceIndex,health;public float height,width,pixelsPerUnit,anchorX,groundY;public BossClip[] clips;}
    [Serializable] public class EliteArtManifest {public EliteArtSource[] actors;}
    // Whole drawings and physical motion, including the first wall-jump trial.
    public sealed class PaperRegion1Elite : MonoBehaviour
    {
        static EliteArtManifest manifest;
        static readonly Dictionary<string,Sprite[]> cache=new Dictionary<string,Sprite[]>();
        public static EliteArtManifest Manifest=>manifest??(manifest=JsonUtility.FromJson<EliteArtManifest>(Resources.Load<TextAsset>("Region1Elites/manifest").text));
        public static Sprite[] Frames(EliteArtSource a){
            if(cache.TryGetValue(a.id,out var ready)&&ready[0]!=null)return ready;
            var sprites=new Sprite[24];for(int i=0;i<24;i++){var t=Resources.Load<Texture2D>("Region1Elites/"+a.id+"/"+i.ToString("D2"));if(t==null)throw new Exception("Missing elite frame "+a.id+i);sprites[i]=Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(a.anchorX/t.width,1-a.groundY/t.height),a.pixelsPerUnit,0,SpriteMeshType.FullRect);}return cache[a.id]=sprites;
        }
        WorldPrototype world;EliteArtSource art;SpriteRenderer draw;PaperAttackTarget target;Sprite[] frames;
        BossDangerShape danger;Vector3 origin,launch;int serial,facing=1,state;float clock,cooldown=.8f;bool activeSound,rewardGiven,announced;
        public int Health {get;private set;}
        public bool IsPaperGuardian=>art.sourceIndex==AbilityTrials.PaperGuardian;
        public string DisplayName=>art.name;
        public float AttackWindup=>IsPaperGuardian?.48f:art.sourceIndex==44?.45f:.5f;
        public float AttackTravel=>IsPaperGuardian?.48f:art.sourceIndex==44?.32f:.75f;
        float Rest=>IsPaperGuardian?.35f:art.sourceIndex==44?.55f:.7f;
        public bool Dead=>Health<=0;
        public int FrameIndex {get;private set;}
        public float EffectClock=>clock;
        readonly BossDangerShape[] effectRanges=new BossDangerShape[1];
        public System.Collections.Generic.IReadOnlyList<BossDangerShape> EffectRanges=>effectRanges;
        public string StateName=>art.clips[state].name;
        public Bounds BodyBounds=>new Bounds(transform.position+Vector3.up*art.height*.5f,new Vector3(art.width,art.height,.1f));
        public static bool Attach(MapMarker marker){if(marker.sourceIndex!=AbilityTrials.PaperGuardian&&marker.sourceIndex!=23&&marker.sourceIndex!=44)return false;if(marker.GetComponent<PaperRegion1Elite>()!=null)return true;marker.gameObject.AddComponent<PaperRegion1Elite>().Initialize(marker);return true;}
        public void Initialize(MapMarker marker){
            world=FindFirstObjectByType<WorldPrototype>();art=Array.Find(Manifest.actors,a=>a.sourceIndex==(marker.sourceIndex==AbilityTrials.PaperGuardian?23:marker.sourceIndex));
            // Reuse the painted overseer drawings without mutating its shared stats or timing.
            if(marker.sourceIndex==AbilityTrials.PaperGuardian){art=JsonUtility.FromJson<EliteArtSource>(JsonUtility.ToJson(art));art.sourceIndex=AbilityTrials.PaperGuardian;art.name="종이 틈의 파수꾼";art.health=9;art.clips[1].durations=new[]{.12f,.12f,.12f,.12f,.24f,.24f,.21f,.21f};}
            frames=Frames(art);
            draw=GetComponent<SpriteRenderer>();draw.enabled=true;draw.sortingOrder=10;transform.localScale=Vector3.one;origin=new Vector3(world.Data.enemies[marker.sourceIndex].tx+.5f,-world.Data.enemies[marker.sourceIndex].floor,0);transform.position=origin;
            target=GetComponent<PaperAttackTarget>()??gameObject.AddComponent<PaperAttackTarget>();target.onHit.AddListener(Hit);
            danger=new BossDangerShape(transform,Region2Presentation.Fx(3));effectRanges[0]=danger;
            serial=world.ResetSerial;ResetActor();
        }
        void ResetActor(){clock=0;cooldown=.8f;state=0;Health=art.health;transform.position=origin;activeSound=rewardGiven=announced=false;bool gone=IsPaperGuardian?world.Progression.PaperGuardianCleared:world.Progression.DefeatedEncounters.Contains(art.sourceIndex);if(gone){Health=0;state=2;clock=art.clips[2].Duration+1;rewardGiven=true;GrantReward();}draw.enabled=!gone;target.enabled=!gone;target.ResetFeedback();danger.Hide();Render();}
        void Hit(){if(Dead)return;if(--Health<=0){EnemyRewardDrops.Emit(world,BodyBounds.center,IsPaperGuardian?10:art.sourceIndex==23?25:12);state=2;clock=0;world.Progression.DefeatedEncounters.Add(art.sourceIndex);target.enabled=false;danger.Hide();GameAudio.Emit(SoundCue.Death,transform.position,.6f);}/* Nonlethal hits keep the current whole drawing and attack clock. */}
        void Update(){Tick(Mathf.Min(Time.deltaTime,.05f));}
        public void BeginAttack(){if(Dead)return;if(IsPaperGuardian&&!announced){announced=true;world.Progression.Say("종이 틈의 파수꾼 · 돌진을 점프로 넘고 빈틈을 노리세요",3);}state=1;clock=0;launch=transform.position;facing=world.player.position.x<transform.position.x?-1:1;activeSound=false;}
        bool CanStand(Vector3 next){if(IsPaperGuardian&&(next.x<197.5f||next.x>211.5f))return false;var m=world.Motor;int y=Mathf.RoundToInt(-origin.y);for(int x=Mathf.FloorToInt(next.x-art.width*.35f);x<=Mathf.FloorToInt(next.x+art.width*.35f);x++){if(!GridMotor.Wall(m.Get(x,y))&&!GridMotor.Platform(m.Get(x,y)))return false;for(int h=1;h<=Mathf.CeilToInt(art.height);h++)if(GridMotor.Wall(m.Get(x,y-h)))return false;}return true;}
        public void Tick(float dt){
            if(art==null||world.MapOpen)return;if(serial!=world.ResetSerial){serial=world.ResetSerial;ResetActor();}
            if(!Dead&&Vector2.Distance(world.player.position,BodyBounds.center)>16)return;
            clock+=dt;cooldown-=dt;
            if(Dead){danger.Hide();float duration=art.clips[2].Duration;draw.color=new Color(1,1,1,1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(duration*.75f,duration+.3f,clock)));if(clock>=duration&&!rewardGiven){rewardGiven=true;PaperVfx.Get()?.Death(BodyBounds);GrantReward();}if(clock>=duration+.3f)draw.enabled=false;Render();return;}
            if(state==1){
                bool hunter=art.sourceIndex==44;float windup=AttackWindup,travel=AttackTravel;
                bool active=clock>=windup&&clock<windup+travel;
                if(active){if(!activeSound){activeSound=true;GameAudio.Emit(SoundCue.EnemyAttack,transform.position,.6f);PaperVfx.Get()?.Dust(transform.position,.7f,-facing,true);}
                    if(hunter){float t=Mathf.Clamp01((clock-windup)/travel);var next=launch+Vector3.right*(facing*3.1f*t);if(CanStand(new Vector3(next.x,origin.y,0)))transform.position=next+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.75f);}
                    else {var next=transform.position+Vector3.right*(facing*(IsPaperGuardian?6.7f:190f/16)*dt);if(CanStand(next))transform.position=next;else{clock=windup+travel;cooldown=Rest;}}
                }
                if(hunter&&clock>=windup+travel)transform.position=new Vector3(transform.position.x,origin.y,0);
                Vector2 radius=new Vector2(art.width*.55f,art.height*.44f),at=(Vector2)transform.position+new Vector2(facing*(hunter?.55f:.12f),art.height*.5f);
                if(clock>=windup-.22f&&clock<windup+travel){danger.Show(at,radius,0,active);if(active&&danger.Overlaps(PlayerBounds())&&PaperSpritePlayer.AttackPathClear(world.Motor,at,PlayerBounds().center))world.player.GetComponentInChildren<PaperSpritePlayer>()?.TryReceiveHit(PlayerBounds().ClosestPoint(at),facing);}
                else danger.Hide();
                if(clock>=art.clips[1].Duration){state=0;clock=0;cooldown=Rest;transform.position=new Vector3(transform.position.x,origin.y,0);}
            }else{
                danger.Hide();float distance=Mathf.Abs(world.player.position.x-transform.position.x);if(cooldown<=0&&(!Application.isPlaying||OnScreen())&&distance<(IsPaperGuardian?8:art.sourceIndex==23?8.75f:6)&&Mathf.Abs(world.player.position.y-origin.y)<3&&PaperSpritePlayer.AttackPathClear(world.Motor,BodyBounds.center,world.player.position)){BeginAttack();}
                else if(cooldown<=0){var next=transform.position+Vector3.right*(facing*(35f/16)*dt);if(Mathf.Abs(next.x-origin.x)>3||!CanStand(next))facing=-facing;else transform.position=next;}
            }
            Render();
        }
        bool OnScreen(){var p=world.worldCamera.WorldToViewportPoint(BodyBounds.center);return p.z>0&&p.x>.03f&&p.x<.97f&&p.y>.03f&&p.y<.97f;}
        Bounds PlayerBounds()=>world.Motor.WorldBounds;
        void Render(){FrameIndex=art.clips[state].Frame(state==0?Mathf.Repeat(clock,art.clips[0].Duration):clock);draw.sprite=frames[state*8+FrameIndex];draw.flipX=facing<0;if(!Dead)draw.color=IsPaperGuardian?new Color(.82f,.85f,.87f):Color.white;}
        void GrantReward(){if(IsPaperGuardian){if(!world.Progression.HasAcquired("walljump"))world.Progression.Say("파수꾼이 지키던 손의 봉인이 풀렸습니다 · 서쪽 끝에서 E로 획득",5);return;}if(art.sourceIndex==23)world.Progression.Grant("cut");else world.Progression.Collect(new WorldMarker{type="heart",tx=195,floor=40});}
        GUIStyle guardianLabel;
        void OnGUI(){
            if(art==null||!IsPaperGuardian||Dead||!announced||world.MapOpen||!OnScreen())return;
            if(guardianLabel==null)guardianLabel=new GUIStyle(GUI.skin.label){font=Resources.Load<Font>("UI/NotoSansCJKkr-Regular"),fontSize=19,alignment=TextAnchor.MiddleCenter};
            var old=GUI.matrix;var color=GUI.color;float scale=Mathf.Min(Screen.width/1920f,Screen.height/1080f);
            GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));float x=(Screen.width/scale-360)*.5f,y=Screen.height/scale-140;
            GUI.color=new Color(.76f,.73f,.65f);GUI.Label(new Rect(x,y-31,360,28),DisplayName,guardianLabel);
            GUI.color=new Color(.12f,.12f,.11f,.9f);GUI.DrawTexture(new Rect(x,y,360,7),Texture2D.whiteTexture);
            GUI.color=new Color(.57f,.63f,.63f);GUI.DrawTexture(new Rect(x,y,360*Health/art.health,7),Texture2D.whiteTexture);
            GUI.color=color;GUI.matrix=old;
        }
        void OnDestroy(){if(target!=null)target.onHit.RemoveListener(Hit);}
    }
}
