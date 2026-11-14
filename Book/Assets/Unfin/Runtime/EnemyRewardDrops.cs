using UnityEngine;
namespace Unfin
{
    // Bounded pool of painted fragments. Death callbacks emit once per enemy life;
    // reset collects outstanding rewards, so hazards never erase earned currency.
    public sealed class EnemyRewardDrops : MonoBehaviour
    {
        sealed class Drop { public SpriteRenderer draw; public Vector2 velocity; public float age; public int value; }
        static Material rewardMaterial;
        readonly Drop[] drops=new Drop[64];
        WorldPrototype world;int serial;float soundCooldown;
        public int SpawnEvents { get; private set; }
        public int PendingValue { get { int n=0;foreach(var d in drops)if(d!=null)n+=d.value;return n;} }
        public int ActiveCount { get { int n=0;foreach(var d in drops)if(d!=null&&d.value>0)n++;return n;} }
        public static EnemyRewardDrops Get(WorldPrototype w)
        {
            if(w==null)return null;var system=w.GetComponent<EnemyRewardDrops>();
            if(system==null){system=w.gameObject.AddComponent<EnemyRewardDrops>();system.Initialize(w);}return system;
        }
        public static int Pending(WorldPrototype w)=>w.GetComponent<EnemyRewardDrops>()?.PendingValue??0;
        public static void Discard(WorldPrototype w){var s=w.GetComponent<EnemyRewardDrops>();if(s!=null)foreach(var d in s.drops)if(d!=null){d.value=0;d.draw.enabled=false;}}
        public static void Emit(WorldPrototype w,Vector2 position,int amount){Get(w)?.Spawn(position,amount);}
        public void Initialize(WorldPrototype w)
        {
            world=w;serial=w.ResetSerial;if(rewardMaterial==null)rewardMaterial=new Material(Resources.Load<Shader>("PickupArt/PickupRim"));var sprite=PaintedHudArt.Pickup("shard")??PaperPickupVisual.Load("shard");
            for(int i=0;i<drops.Length;i++){
                var go=new GameObject("Pooled ink reward");go.transform.SetParent(transform,false);var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sharedMaterial=rewardMaterial;sr.sortingOrder=26;sr.enabled=false;
                sr.transform.localScale=Vector3.one*(.20f/sprite.bounds.size.y);drops[i]=new Drop{draw=sr};
            }
        }
        public void Spawn(Vector2 at,int amount)
        {
            if(amount<=0)return;SpawnEvents++;int count=Mathf.Min(amount,5),remaining=amount;
            for(int i=0;i<count;i++){
                Drop d=null;foreach(var candidate in drops)if(candidate.value==0){d=candidate;break;}
                if(d==null){d=drops[0];Collect(d);}
                int value=Mathf.CeilToInt((float)remaining/(count-i));remaining-=value;
                d.value=value;d.age=0;d.velocity=new Vector2((i-(count-1)*.5f)*.75f,2.2f+(i%2)*.35f);
                d.draw.enabled=true;d.draw.transform.position=at;d.draw.transform.rotation=Quaternion.Euler(0,0,i*37);
                d.draw.color=new Color(.94f,.85f,1,1);
            }
        }
        void Collect(Drop d)
        {
            world.Progression.AddInkDust(d.value);d.value=0;d.draw.enabled=false;
            if(soundCooldown<=0){soundCooldown=.1f;GameAudio.Emit(SoundCue.Restore,world.player.position,.20f);}
        }
        void Update(){Tick(Time.deltaTime);}
        public void Tick(float dt)
        {
            if(world==null)return;
            if(serial!=world.ResetSerial){serial=world.ResetSerial;foreach(var d in drops)if(d.value>0)Collect(d);}
            if(world.MapOpen)return;
            dt=Mathf.Clamp(dt,0,.05f);soundCooldown-=dt;Vector2 player=world.Motor.WorldBounds.center;
            foreach(var d in drops){
                if(d.value<=0)continue;d.age+=dt;Vector2 at=d.draw.transform.position;float distance=Vector2.Distance(at,player);
                if(d.age>.22f&&distance<.38f&&!world.HazardInputBlocked){Collect(d);continue;}
                bool attract=d.age>.28f&&distance<world.Progression.GatherRadius&&!world.HazardInputBlocked&&PaperSpritePlayer.AttackPathClear(world.Motor,at,player);
                Vector2 next;
                if(attract){d.velocity=Vector2.MoveTowards(d.velocity,(player-at).normalized*7,dt*30);next=Vector2.MoveTowards(at,player,dt*Mathf.Max(2,d.velocity.magnitude));}
                else{
                    d.velocity.y=Mathf.Max(-7,d.velocity.y-8*dt);next=at+d.velocity*dt;
                    int x=Mathf.FloorToInt(next.x),y=Mathf.FloorToInt(-next.y),tile=world.Motor.Get(x,y);
                    if(d.velocity.y<0&&(GridMotor.Wall(tile)||GridMotor.Platform(tile)||tile==3||tile==9)){next.y=-y+.07f;d.velocity.y=0;d.velocity.x*=.8f;}
                    if(GridMotor.Wall(world.Motor.Get(x,Mathf.FloorToInt(-at.y)))){next.x=at.x;d.velocity.x*=-.3f;}
                }
                // Out-of-map and very old rewards are recovered rather than lost forever.
                if(d.age>45||next.y<-world.Data.height-2){Collect(d);continue;}
                d.draw.transform.position=next;d.draw.transform.Rotate(0,0,dt*(attract?160:35));
                float glow=.86f+.14f*Mathf.Sin(d.age*4);d.draw.color=new Color(.94f*glow,.85f*glow,glow,1);
            }
        }
    }
}
