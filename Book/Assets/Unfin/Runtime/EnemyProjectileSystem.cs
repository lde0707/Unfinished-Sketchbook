using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unfin
{
    // Whole painted sprites; pooled projectiles use the same logical map as actors.
    [DefaultExecutionOrder(80)]
    public sealed class EnemyProjectileSystem : MonoBehaviour
    {
        public const int Capacity=48;
        public enum Kind { Graphite, Ink, Seed, Puddle }
        public struct Snapshot { public Kind kind;public Vector2 position,velocity;public float life;public int bounces; }
        sealed class Shot
        {
            public SpriteRenderer draw;public UnityEngine.Object owner;public Kind kind;
            public Vector2 position,velocity;public float gravity,life,duration,radius,puddleWidth;public int bounces;
        }
        static EnemyProjectileSystem instance;
        readonly Shot[] shots=new Shot[Capacity];
        readonly Sprite[] art=new Sprite[3];
        WorldPrototype world;PaperSpritePlayer player;
        int serial;bool initialized;float playerGrace;
        public int FiredCount {get;private set;}
        public int WallImpactCount {get;private set;}
        public int CutCount {get;private set;}
        public int BounceCount {get;private set;}
        public int PlayerContactCount {get;private set;}
        public int ActiveCount {get {int n=0;foreach(var s in shots)if(s!=null&&s.life>0)n++;return n;}}
        public static bool IsShooter(string id)=>id=="R102"||id=="COMMON03"||id=="D02";
        public static EnemyProjectileSystem Get(WorldPrototype w)
        {
            if(w==null)return null;
            if(instance!=null&&instance.world==w)return instance;
            if(!Application.isPlaying)return null;
            var system=new GameObject("Enemy painted projectiles").AddComponent<EnemyProjectileSystem>();system.Initialize(w);return system;
        }
        public void Initialize(WorldPrototype w)
        {
            if(initialized)return;initialized=true;instance=this;world=w;serial=w.ResetSerial;
            player=w.player.GetComponentInChildren<PaperSpritePlayer>();
            string[] names={"graphite","ink","seed"};
            for(int i=0;i<3;i++)art[i]=Resources.Load<Sprite>("EnemyProjectiles/"+names[i]);
            for(int i=0;i<Capacity;i++)
            {
                var go=new GameObject("Pooled enemy projectile");go.transform.SetParent(transform,false);
                var sr=go.AddComponent<SpriteRenderer>();sr.sortingOrder=24;sr.enabled=false;
                shots[i]=new Shot{draw=sr};
            }
        }
        public static Vector2 BallisticVelocity(Vector2 from,Vector2 target,float gravity,float seconds)
            =>(target-from)/seconds+Vector2.up*(gravity*seconds*.5f);
        public bool Fire(UnityEngine.Object owner,string id,Vector2 muzzle,Vector2 target,int facing)
        {
            if(!IsShooter(id))return false;
            // Especially the bouncing seed: one live shot per source, no accumulating barrage.
            foreach(var existing in shots)if(existing.life>0&&existing.owner==owner&&existing.kind!=Kind.Puddle)return false;
            Shot s=Array.Find(shots,p=>p.life<=0);if(s==null)return false;
            s.kind=id=="R102"?Kind.Graphite:id=="COMMON03"?Kind.Ink:Kind.Seed;
            s.owner=owner;s.position=muzzle;s.bounces=0;s.radius=s.kind==Kind.Graphite?.09f:.12f;
            s.gravity=s.kind==Kind.Ink?12:s.kind==Kind.Seed?16:0;
            s.velocity=s.kind==Kind.Graphite?Vector2.right*facing*6.25f:
                s.kind==Kind.Ink?BallisticVelocity(muzzle,target,12,.9f):new Vector2(facing*4.6f,3.2f);
            s.life=s.duration=3.2f;s.puddleWidth=0;s.draw.sprite=art[(int)s.kind];s.draw.enabled=true;
            s.draw.transform.localScale=Vector3.one;s.draw.color=Color.white;Render(s);FiredCount++;return true;
        }
        void Update(){Tick(Mathf.Min(Time.deltaTime,.1f));}
        public void Tick(float dt)
        {
            if(!initialized||world==null||world.Motor==null)return;
            if(serial!=world.ResetSerial){serial=world.ResetSerial;Clear();}
            if(world.MapOpen)return;
            while(dt>0){float step=Mathf.Min(dt,1f/240);Step(step);dt-=step;}
        }
        void Step(float dt)
        {
            playerGrace=Mathf.Max(0,playerGrace-dt);
            foreach(var s in shots)
            {
                if(s.life<=0)continue;s.life-=dt;if(s.life<=0){Remove(s,false);continue;}
                if(s.kind==Kind.Puddle){Contact(s);Render(s);continue;}
                Vector2 previous=s.position;
                s.position+=s.velocity*dt+Vector2.down*(s.gravity*dt*dt*.5f);s.velocity+=Vector2.down*(s.gravity*dt);
                if(TerrainContact(s,previous,out float floor,out bool landed))
                {
                    if(s.kind==Kind.Seed&&s.bounces==0&&landed)
                    {
                        s.bounces++;BounceCount++;s.position.y=floor+s.radius+.005f;
                        s.velocity=new Vector2(s.velocity.x*.8f,Mathf.Min(4,Mathf.Abs(s.velocity.y)*.58f));
                        PaperVfx.Get()?.Dust(s.position,.22f);
                    }
                    else if(s.kind==Kind.Ink&&landed)MakePuddle(s,floor);
                    else{WallImpactCount++;Remove(s,true);continue;}
                }
                if(s.kind!=Kind.Puddle&&player!=null&&TryCut(s,player.ActiveHitPolygon,world.player.position))continue;
                Contact(s);if(s.life>0)Render(s);
            }
        }
        bool TerrainContact(Shot s,Vector2 previous,out float floor,out bool landed)
        {
            floor=0;landed=false;var m=world.Motor;
            if(s.velocity.y<0)
            {
                int row=Mathf.FloorToInt(-(s.position.y-s.radius));
                for(int x=Mathf.FloorToInt(s.position.x-s.radius);x<=Mathf.FloorToInt(s.position.x+s.radius);x++)
                {
                    int tile=m.Get(x,row);float top=-row;
                    if((GridMotor.Wall(tile)||GridMotor.Platform(tile))&&previous.y-s.radius>=top-.04f&&s.position.y-s.radius<=top)
                    {floor=top;landed=true;return true;}
                }
            }
            for(int x=Mathf.FloorToInt(s.position.x-s.radius);x<=Mathf.FloorToInt(s.position.x+s.radius);x++)
            for(int y=Mathf.FloorToInt(-s.position.y-s.radius);y<=Mathf.FloorToInt(-s.position.y+s.radius);y++)
                if(GridMotor.Wall(m.Get(x,y)))return true;
            return false;
        }
        void MakePuddle(Shot s,float floor)
        {
            WallImpactCount++;s.kind=Kind.Puddle;s.velocity=Vector2.zero;s.gravity=0;s.position.y=floor+.035f;
            s.life=s.duration=1.5f;s.puddleWidth=.95f;
            // Keep the patch on supported ground rather than bridging an air gap.
            int row=Mathf.RoundToInt(-floor);
            while(s.puddleWidth>.2f)
            {
                int a=world.Motor.Get(Mathf.FloorToInt(s.position.x-s.puddleWidth*.5f),row);
                int b=world.Motor.Get(Mathf.FloorToInt(s.position.x+s.puddleWidth*.5f),row);
                if((GridMotor.Wall(a)||GridMotor.Platform(a))&&(GridMotor.Wall(b)||GridMotor.Platform(b)))break;
                s.puddleWidth-=.1f;
            }
            PaperVfx.Get()?.Dust(s.position,.25f);
        }
        Bounds ShotBounds(Shot s)=>new Bounds(s.position,s.kind==Kind.Puddle?new Vector3(s.puddleWidth,.12f,.1f):Vector3.one*s.radius*2);
        void Contact(Shot s)
        {
            if(world.flying||world.HazardInputBlocked||world.IsDashing||playerGrace>0)return;
            var m=world.Motor;
            var body=new Bounds(new Vector3((m.Left+m.Right)/32,-(m.y+m.Bottom)/32,0),new Vector3((m.Right-m.Left)/16,GridMotor.CollisionHeight/16,.1f));
            if(!body.Intersects(ShotBounds(s)))return;
            playerGrace=.55f;PlayerContactCount++;
            player?.ShowProjectileContact(s.position);
            if(s.kind!=Kind.Puddle)Remove(s,false);
        }
        bool TryCut(Shot s,Vector2[] polygon,Vector2 origin)
        {
            if(s.life<=0||s.kind==Kind.Puddle||polygon==null)return false;
            if(!PaperSpritePlayer.OverlapsAttack(polygon,ShotBounds(s))||!PaperSpritePlayer.AttackPathClear(world.Motor,origin,s.position))return false;
            CutCount++;Remove(s,true);return true;
        }
        public void Cut(Vector2[] polygon,Vector2 origin){if(!initialized)return;foreach(var s in shots)TryCut(s,polygon,origin);}
        void Render(Shot s)
        {
            s.draw.transform.position=s.position;
            if(s.kind==Kind.Puddle)
            {
                s.draw.transform.rotation=Quaternion.identity;s.draw.transform.localScale=new Vector3(s.puddleWidth/.45f,.2f,1);
                s.draw.color=new Color(.78f,.78f,.82f,.65f*Mathf.Clamp01(s.life/.35f));
            }
            else
            {
                float angle=s.kind==Kind.Seed?(s.duration-s.life)*200:Mathf.Atan2(s.velocity.y,s.velocity.x)*Mathf.Rad2Deg;
                s.draw.transform.rotation=Quaternion.Euler(0,0,angle);s.draw.color=new Color(1,1,1,Mathf.Clamp01(s.life/.15f));
            }
        }
        void Remove(Shot s,bool effect)
        {
            if(effect){PaperVfx.Get()?.Hit(s.position,s.velocity.x<0?-1:1);GameAudio.Emit(SoundCue.Hit,s.position,.35f);}
            s.life=0;s.draw.enabled=false;
        }
        public void Clear(){foreach(var s in shots)if(s!=null){s.life=0;s.draw.enabled=false;}playerGrace=0;}
        public Snapshot[] Capture()
        {
            var list=new List<Snapshot>();foreach(var s in shots)if(s!=null&&s.life>0)list.Add(new Snapshot{kind=s.kind,position=s.position,velocity=s.velocity,life=s.life,bounces=s.bounces});return list.ToArray();
        }
        void OnDestroy(){if(instance==this)instance=null;}
    }
}
