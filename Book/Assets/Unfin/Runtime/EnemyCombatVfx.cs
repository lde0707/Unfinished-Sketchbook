using System.Collections.Generic;
using UnityEngine;
namespace Unfin
{
    // Reads the existing action clocks and attack shapes; never changes combat timing or geometry.
    [DefaultExecutionOrder(250)]
    public sealed class EnemyCombatVfx : MonoBehaviour
    {
        MapMarker marker;WorldPrototype world;SpriteRenderer draw;PaperAttackTarget receiver;
        PaperEnemyAnimation normal;PaperBossRegion1 boss1;PaperRegion2Encounter boss2;PaperRegion1Elite elite;
        EnemyArtSource source;EnemyCombatEffects effects;string previousState;int serial,previousHits,previousHealth;
        readonly bool[] active=new bool[5];Vector3 previousPosition;float trailCooldown,startingFloor;bool airborne,ready,emittedLaunch;
        public Color Palette {get;private set;}
        public int AttackBursts {get;private set;}
        public static void Attach(MapMarker m){if(!m.enemy||m.GetComponent<EnemyCombatVfx>()!=null)return;var v=m.gameObject.AddComponent<EnemyCombatVfx>();v.marker=m;}
        bool Configure(){
            world=FindFirstObjectByType<WorldPrototype>();draw=GetComponent<SpriteRenderer>();receiver=GetComponent<PaperAttackTarget>();normal=GetComponent<PaperEnemyAnimation>();boss1=GetComponent<PaperBossRegion1>();boss2=GetComponent<PaperRegion2Encounter>();elite=GetComponent<PaperRegion1Elite>();
            if(world==null||normal==null&&boss1==null&&boss2==null&&elite==null)return false;
            if(normal!=null)source=normal.Art;
            effects=EnemyCombatEffects.Get(world);if(effects==null)return false;
            string id=normal!=null?normal.SpeciesId:marker.sourceIndex==91?"H":marker.sourceIndex==69?"F":"A";
            Palette=id.StartsWith("R2")||id.StartsWith("E")?new Color(.75f,.45f,.29f):id.StartsWith("F")?new Color(.76f,.62f,.32f):id.StartsWith("G")?new Color(.46f,.5f,.78f):id.StartsWith("H")?new Color(.75f,.4f,.47f):new Color(.62f,.58f,.5f);
            serial=world.ResetSerial;ResetObservation();ready=true;return true;
        }
        string State=>normal!=null?normal.StateName:boss1!=null?boss1.StateName:boss2!=null?boss2.StateName:elite.StateName;
        float Clock=>normal!=null?normal.EffectClock:boss1!=null?boss1.EffectClock:boss2!=null?boss2.EffectClock:elite.EffectClock;
        Bounds Body=>normal!=null?normal.BodyBounds:boss1!=null?boss1.BodyBounds:boss2!=null?boss2.BodyBounds:elite.BodyBounds;
        int Health=>normal!=null?normal.Health:boss1!=null?boss1.Health:boss2!=null?boss2.Health:elite.Health;
        bool Boss=>normal==null;
        bool Attack=>normal!=null?State=="Attack":elite!=null?State=="Attack":boss1!=null?boss1.Pattern>=0:boss2.Pattern>=0;
        IReadOnlyList<BossDangerShape> Ranges=>boss1!=null?boss1.EffectRanges:boss2!=null?boss2.EffectRanges:elite!=null?elite.EffectRanges:null;
        void ResetObservation(){previousState=State;previousPosition=transform.position;previousHits=receiver==null?0:receiver.HitCount;previousHealth=Health;trailCooldown=0;startingFloor=Body.min.y;airborne=emittedLaunch=false;for(int i=0;i<active.Length;i++)active[i]=false;}
        void LateUpdate(){Tick(Mathf.Min(Time.deltaTime,.05f));}
        public void Tick(float dt){
            if(!ready&&!Configure())return;if(world.MapOpen)return;if(serial!=world.ResetSerial){serial=world.ResetSerial;ResetObservation();return;}
            var state=State;var body=Body;int facing=draw.flipX?-1:1;bool onScreen=true;
            if(Application.isPlaying){var p=world.worldCamera.WorldToViewportPoint(body.center);onScreen=p.z>0&&p.x>-.1f&&p.x<1.1f&&p.y>-.15f&&p.y<1.15f;}
            if(!onScreen){ResetObservation();return;}
            if(state!=previousState){
                for(int i=0;i<active.Length;i++)active[i]=false;emittedLaunch=false;airborne=false;startingFloor=body.min.y;
                if(state=="Death")effects.Death(body,Palette,Boss);
                else if(Attack)effects.Windup(body,Palette,facing,Boss);
                else if(state=="Stagger")effects.Hit(body,Palette,-facing,true,true);
                previousState=state;
            }
            if(receiver!=null&&receiver.HitCount!=previousHits){if(state!="Death")effects.Hit(body,Palette,(int)receiver.LastHitDirection.x,Boss,Health>=previousHealth);previousHits=receiver.HitCount;}
            previousHealth=Health;
            if(!Attack||state=="Death"){previousPosition=transform.position;return;}
            var ranges=Ranges;
            if(ranges!=null){for(int i=0;i<ranges.Count&&i<active.Length;i++){var r=ranges[i];bool hit=r!=null&&r.Visible&&r.Damaging;if(hit&&!active[i]){bool ground=boss1!=null&&(boss1.Pattern==1||boss1.Pattern==3)||boss2!=null&&state=="Slam";effects.Strike(r.Center,r.Radius,r.Angle,Palette,facing,true,ground);AttackBursts++;if(ground)effects.Landing(new Vector2(r.Center.x,body.min.y),r.Radius.x*1.65f,Palette,true);}active[i]=hit;}}
            else if(normal!=null){
                var times=source.clips[1].motionDurations??source.clips[1].durations;float impact=times[0]+times[1];
                if(!active[0]&&Clock>=impact){active[0]=true;if(EnemyProjectileSystem.IsShooter(source.id)){var muzzle=source.id=="E02"?new Vector2(body.center.x-facing*body.size.x*.5f,body.min.y+.06f):normal.ProjectileMuzzle;effects.Hit(new Bounds(muzzle,Vector3.one*.55f),Palette,-facing,false);effects.Projectile(muzzle,Vector2.right*facing,Palette,true);}else {var strike=normal.AttackBounds;effects.Strike(strike.center,(Vector2)strike.extents,0,Palette,facing,false,normal.Specialty.lowSweep);}AttackBursts++;}
                if(source.id=="R201"&&!active[1]&&Clock>=source.attackWindows[2]){active[1]=true;effects.Strike((Vector2)body.center+Vector2.right*facing*.5f,new Vector2(.7f,.4f),-35,Palette,facing,false);AttackBursts++;}
                if(source.id=="H03"&&!active[1]&&Clock>=impact+.28f){active[1]=true;effects.Hit(new Bounds(normal.ProjectileMuzzle,Vector3.one*.55f),Palette,-facing,false);AttackBursts++;}
            }
            bool launch=boss1!=null&&boss1.Pattern==4&&Clock>=PaperBossRegion1.Art.patterns[4].activeStart||boss2!=null&&(state=="Throw"&&Clock>=.65f||state=="Shards"&&Clock>=.85f);
            if(launch&&!emittedLaunch){emittedLaunch=true;effects.Hit(new Bounds((Vector2)body.center+Vector2.right*facing*body.size.x*.45f,Vector3.one*.75f),Palette,-facing,true);AttackBursts++;}
            var delta=transform.position-previousPosition;trailCooldown-=dt;
            if(delta.magnitude<5&&dt>0&&delta.magnitude/dt>2.5f&&trailCooldown<=0){trailCooldown=.065f;effects.Trail(draw,body,Palette,facing);}
            if(normal!=null&&!source.flying){if(body.min.y>startingFloor+.18f)airborne=true;if(airborne&&body.min.y<=startingFloor+.035f){airborne=false;effects.Landing(new Vector2(body.center.x,body.min.y),body.size.x,Palette,false);}}
            previousPosition=transform.position;
        }
    }
}
