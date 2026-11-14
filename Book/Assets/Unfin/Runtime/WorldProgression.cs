using System;
using System.Collections.Generic;
using UnityEngine;
namespace Unfin
{
    [Serializable] public class ExplorationFrontier {public string id,name,ability,reward,kind;public int x,y,w,h;}
    [Serializable] public class ExplorationLayout {public string gridSha256;public ExplorationFrontier[] frontiers;public ExplorationChange[] changes;}
    [Serializable] public class ExplorationChange {public int x,y,before,after;}
    public sealed partial class WorldProgression
    {
        readonly WorldPrototype world;
        readonly HashSet<string> abilities=new HashSet<string>(),collected=new HashSet<string>(),visitedStations=new HashSet<string>();
        public readonly HashSet<string> SeenFrontiers=new HashSet<string>();
        public readonly HashSet<int> DefeatedEncounters=new HashSet<int>();
        public readonly bool[] Discovered;
        public readonly bool[] ConnectionHints;
        public readonly RoomMapLayout MapLayout;
        public int CurrentMapArea {get;private set;}=-1;
        readonly HashSet<int> revealedAreas=new HashSet<int>();
        readonly byte[] mapTerrain;
        public readonly ExplorationLayout Layout;
        public readonly ZoneEntryBanner AreaBanner=new ZoneEntryBanner();
        // Existing editor motor tests exercise the full movement kit. Progression
        // tests explicitly enable locks; actual play always starts with locks on.
        public bool UseAbilityLocks=Application.isPlaying;
        public float Health=5;
        public int HeartFragments,Shards,HealBonus,HealingCharges;
        public bool HealingUnlocked,ShieldUnlocked,ShieldReady;
        public float MaxHealth=>5+HeartFragments*.5f+BindingLevel;
        public int MaxHealingCharges=>HealingUnlocked?3+HealBonus+BottleLevel:0;
        public string Toast {get;private set;}="초반에 본 막힌 길은 지도에 기록됩니다";
        float toastTime=6,parryTime,parryCooldown,stationCooldown,mapClock;
        int lastRegion=-1;
        readonly Dictionary<int,float> fireCooldowns=new Dictionary<int,float>();readonly List<int> fireKeys=new List<int>();
        int FireKey(WorldMarker p)=>p.floor*world.Data.width+p.tx;
        public bool BrazierLit(WorldMarker p)=>!fireCooldowns.TryGetValue(FireKey(p),out float seconds)||seconds<=0;
        public void ExtinguishBrazier(WorldMarker p){fireCooldowns[FireKey(p)]=5;GameAudio.Emit(SoundCue.Gate,world.Data.Center(p),.45f);Region2Presentation.Burst(world,world.Data.Center(p),"smoke");}
        public bool HeatsPlatform(int x,int y){foreach(var p in world.Data.pickups)if(p.type=="brazier"&&BrazierLit(p)&&Vector2.Distance(new Vector2(x+.5f,y),new Vector2(p.tx+.5f,p.floor))<=4)return true;return false;}
        void TickFires(float dt){fireKeys.Clear();fireKeys.AddRange(fireCooldowns.Keys);foreach(int key in fireKeys)fireCooldowns[key]=Mathf.Max(0,fireCooldowns[key]-dt);}
        WorldMarker pendingPortal;
        float portalDelay;
        public bool PortalTravelling=>pendingPortal!=null;
        public bool Parrying=>parryTime>0;
        public int ParrySerial {get;private set;}
        public int ParrySuccessSerial {get;private set;}
        bool parryConfirmed;
        public void ConfirmParry(){if(parryTime<=0)return;parryCooldown=Mathf.Min(parryCooldown,.08f);if(parryConfirmed)return;parryConfirmed=true;ParrySuccessSerial++;Region2Presentation.Burst(world,world.player.position+Vector3.right*world.Facing*.4f,"parry",world.Facing);}
        public int OpenedGates {get;private set;}
        public WorldProgression(WorldPrototype w)
        {
            world=w;Discovered=new bool[w.Data.width*w.Data.height];ConnectionHints=new bool[Discovered.Length];
            MapLayout=new RoomMapLayout(w.Data,w.Data.Decode());mapTerrain=(byte[])w.Motor.cells.Clone();
            Layout=JsonUtility.FromJson<ExplorationLayout>(Resources.Load<TextAsset>("ExplorationLayout").text);LoadNotes();
        }
        public bool Has(string ability)=>!UseAbilityLocks||abilities.Contains(ability);
        public bool HasAcquired(string ability)=>abilities.Contains(ability);
        public static string AbilityName(string ability){switch(ability){case "walljump":return "벽 점프";case "cut":return "연필 깎기";case "dash":return "지우개 대시";case "parry":return "패링";case "climb":return "크레파스 벽타기";case "swim":return "물감 헤엄 · 이후 지역";case "glide":return "활공 · 이후 지역";default:return ability;}}
        public void Grant(string ability,bool showNotice=true)
        {
            if(!abilities.Add(ability))return;
            if(showNotice)ShowAbilityNotice(ability);
            GameAudio.Emit(SoundCue.Restore,world.player.position);
        }
        public void Restore(){Health=MaxHealth;HealingCharges=MaxHealingCharges;ShieldReady=ShieldUnlocked;parryTime=0;}
        public void Damage()
        {
            if(ShieldReady){ShieldReady=false;Say("종이 방패가 충격을 막았습니다",2);return;}
            Health=Mathf.Max(0,Health-1);if(Health<=0)world.BeginDeath();
        }
        public void Heal()
        {
            if(!HealingUnlocked||HealingCharges<=0||Health>=MaxHealth||world.HazardInputBlocked)return;
            HealingCharges--;Health=Mathf.Min(MaxHealth,Health+2);PaperVfx.Get()?.Dust(world.player.position,.6f,0,true);Say("회복 물감 · 남은 횟수 "+HealingCharges,2);
        }
        public void Say(string text,float seconds=3){Toast=text;toastTime=seconds;}
        static string Key(WorldMarker marker)=>marker.type+":"+marker.tx+":"+marker.floor;
        public bool Collected(WorldMarker marker)=>collected.Contains(Key(marker));
        public bool CollectedAt(string kind,Vector2 html)
        {
            foreach(var p in world.Data.pickups)if(p.type==kind&&Mathf.Abs(p.x-html.x)<.1f&&Mathf.Abs(p.y-html.y)<.1f)return Collected(p);
            return false;
        }
        public void Collect(WorldMarker p)
        {
            if(Collected(p))return;
            if(!AbilityPickupUnlocked(p.type)){Say(p.type=="wj"?"종이 틈의 파수꾼을 쓰러뜨리면 손의 봉인이 풀립니다":"이 능력을 지키는 적을 먼저 쓰러뜨려야 합니다",3);return;}
            switch(p.type){
                case "wj":Grant("walljump",false);break;
                case "cut":case "dash":case "parry":case "climb":Grant(p.type,false);break;
                case "item_heal":HealingUnlocked=true;HealingCharges=MaxHealingCharges;Say("회복 물감 획득 · L로 체력 2 회복",4);break;
                case "item_shield":ShieldUnlocked=ShieldReady=true;Say("종이 방패 획득 · 저장 후 첫 피격을 흡수합니다",4);break;
                case "healup":HealBonus++;HealingCharges=MaxHealingCharges;Say("회복 물감 최대 사용 횟수 +1",4);break;
                case "heart":HeartFragments++;Health=Mathf.Min(MaxHealth,Health+.5f);Say("체력 조각 · 최대 체력 "+MaxHealth.ToString("0.#"),4);break;
                case "shard":Shards++;Say("흑연 조각 "+Shards,2);break;
                case "diary":Region2Story.ReadDiary(world);break;
                case "sw_r":case "sw_b":case "sw_y":Say("물감통 기록 · 세 가지 색을 모으면 물감 문을 열 수 있습니다",3);break;
                case "item_paint":Say("미완성 물감 획득 · 찢긴 페이지 탐험 완료",4);break;
                default:return;
            }
            collected.Add(Key(p));GameAudio.Emit(SoundCue.Restore,world.player.position,.6f);PaperVfx.Get()?.Dust(world.player.position,.55f,0,true);
            ShowPickupNotice(p);
        }
        public bool PigmentsReady {get {int n=0;foreach(var p in world.Data.pickups)if(p.type.StartsWith("sw_")&&Collected(p))n++;return n>=3;}}
        public bool CanOpen(int type)=>type==2?Has("dash"):type==5?Has("cut"):type==13?Has("parry"):type==18?PigmentsReady:false;
        public void Parry()
        {
            if(!Has("parry")||parryCooldown>0||world.HazardInputBlocked||world.ActionLocked||world.MapOpen)return;
            parryTime=.16f;parryCooldown=.5f;parryConfirmed=false;ParrySerial++;GameAudio.Emit(SoundCue.Restore,world.player.position,.4f);
            float px=(world.Motor.x+6)/16,py=(world.Motor.y+11)/16;
            // The same learned deflection can strike a nearby resonant seal.
            int cx=Mathf.FloorToInt((world.Motor.x+6)/16),cy=Mathf.FloorToInt((world.Motor.y+11)/16);
            for(int y=cy-2;y<=cy+2;y++)for(int x=cx-2;x<=cx+2;x++)if(world.Motor.Get(x,y)==13&&!(x>=404&&x<=407&&y>=118&&y<=123)){world.ClearGateAt(x,y);OpenedGates++;return;}
        }
        public void AttackGate(Vector2[] polygon)
        {
            if(!Has("cut"))return;
            var min=polygon[0];var max=min;foreach(var p in polygon){min=Vector2.Min(min,p);max=Vector2.Max(max,p);}
            for(int y=Mathf.FloorToInt(-max.y);y<=Mathf.FloorToInt(-min.y);y++)for(int x=Mathf.FloorToInt(min.x);x<=Mathf.FloorToInt(max.x);x++)
                if(world.Motor.Get(x,y)==5&&PaperSpritePlayer.OverlapsAttack(polygon,new Bounds(new Vector3(x+.5f,-y-.5f,0),Vector3.one))){world.ClearGateAt(x,y);OpenedGates++;}
        }
        public void Tick(float dt,int ix,int iy)
        {
            if(world.MapOpen||world.Data.isPrologue)return;
            if(pendingPortal!=null){
                portalDelay-=dt;
                if(portalDelay<=0){var destination=pendingPortal;pendingPortal=null;world.TravelTo(destination.tx,destination.floor);PaperPickupVisual.PortalPulse(world,destination);Say("리본을 따라 익숙한 곳으로 돌아왔습니다");}
                return;
            }
            TickPickupNotice(dt);TickFires(dt);toastTime=Mathf.Max(0,toastTime-dt);parryTime=Mathf.Max(0,parryTime-dt);parryCooldown-=dt;stationCooldown-=dt;
            if(world.HazardInputBlocked){SocialTarget=null;SocialProgress=0;return;}
            AreaBanner.Tick(RegionalTilePalette.ZoneAt(world.Data,Mathf.FloorToInt((world.Motor.x+6)/16),Mathf.FloorToInt((world.Motor.y+11)/16)),dt);
            TickSocial(dt,ix,iy);
            var m=world.Motor;float px=(m.x+6)/16,py=(m.y+11)/16;
            if(world.IsDashing){int cx=Mathf.FloorToInt(px),cy=Mathf.FloorToInt(py);for(int y=cy-1;y<=cy+1;y++)for(int x=cx-2;x<=cx+2;x++)if(m.Get(x,y)==2&&Has("dash")){world.ClearGateAt(x,y);OpenedGates++;}}
            if(PigmentsReady){int cx=(int)px,cy=(int)py;for(int y=cy-2;y<=cy+2;y++)for(int x=cx-3;x<=cx+3;x++)if(m.Get(x,y)==18)world.ClearGateAt(x,y);}
            if(Has("dash")){int row=Mathf.FloorToInt(m.Bottom/16);for(int x=Mathf.FloorToInt(m.Left/16);x<=Mathf.FloorToInt(m.Right/16);x++)if(m.Get(x,row)==14&&m.Bottom<=row*16+.1f)world.ClearGateAt(x,row);}
            mapClock-=dt;if(mapClock<=0){mapClock=.1f;Reveal(px,py);}
            foreach(var f in Layout.frontiers)if(!SeenFrontiers.Contains(f.id)&&Mathf.Abs(f.x-px)<10&&Mathf.Abs(f.y+f.h*.5f-py)<5&&Visible(px,py,f.x,f.y+f.h*.5f))SeenFrontiers.Add(f.id);
            WorldMarker station=null;
            foreach(var p in world.Data.pickups)if(p.type=="station"&&Vector2.Distance(new Vector2((p.x+p.w*.5f)/16,(p.y+p.h*.5f)/16),new Vector2(px,py))<1.4f){station=p;break;}
            if(station!=null){
                string sid=JsonUtility.FromJson<Station>(station.optionsJson).sid;visitedStations.Add(sid);
            }
        }
        [Serializable] class Station {public string sid;}
        static readonly Dictionary<string,string> Pairs=new Dictionary<string,string>{{"r1a","r1b"},{"r1b","r1a"},{"r1d","r2b"},{"r2b","r1d"},{"r2a","r2c"},{"r2c","r2a"},{"r2d","r1s"},{"r1s","r2d"}};
        void Travel(string sid)
        {
            if(!Pairs.TryGetValue(sid,out string to)){Say("이 매듭은 이후 지역으로 이어집니다");return;}
            bool secret=sid=="r2d"||sid=="r1s";
            if(!secret&&!visitedStations.Contains(to)){Say("반대쪽 리본 끝을 찾아야 연결됩니다");return;}
            foreach(var p in world.Data.pickups)if(p.type=="station"&&JsonUtility.FromJson<Station>(p.optionsJson).sid==to){
                pendingPortal=p;portalDelay=.22f;
                foreach(var source in world.Data.pickups)if(source.type=="station"&&JsonUtility.FromJson<Station>(source.optionsJson).sid==sid)PaperPickupVisual.PortalPulse(world,source);
                GameAudio.Emit(SoundCue.Restore,world.player.position,.45f);return;
            }
        }
        bool Visible(float x,float y,float tx,float ty)
        {
            var a=new Vector2(x,y);var b=new Vector2(tx,ty);int n=Mathf.CeilToInt(Vector2.Distance(a,b)*3);
            for(int i=1;i<n-1;i++){var p=Vector2.Lerp(a,b,(float)i/n);int t=world.Motor.Get(Mathf.FloorToInt(p.x),Mathf.FloorToInt(p.y));if(GridMotor.Wall(t))return WorldPrototype.Gate(t)&&Vector2.Distance(p,b)<5;}return true;
        }
        public void Reveal(float px,float py)
        {
            CurrentMapArea=MapLayout.At(px,py);
            bool changed=CurrentMapArea>=0&&revealedAreas.Add(CurrentMapArea)&&MapLayout.Reveal(CurrentMapArea,Discovered);
            // Opening a gate or collapsing a platform updates the drawing even
            // when the player stays in the same already-discovered room.
            for(int i=0;i<mapTerrain.Length;i++)if((Discovered[i]||ConnectionHints[i])&&mapTerrain[i]!=world.Motor.cells[i]){mapTerrain[i]=world.Motor.cells[i];changed=true;}
            if(changed)RefreshMapConnections();
        }
        public void RefreshMapConnections(){MapLayout.RevealConnections(Discovered,world.Motor.cells,ConnectionHints);mapDirty=true;}
        public bool FrontierCleared(ExplorationFrontier f)
        {
            if(f.kind=="bridge")return world.Motor.Get(459,88)==4;
            if(f.kind=="future")return false;
            if(f.kind=="climb"){
                int rewardX=f.id=="wax-nest"?87:f.id=="waterfall"?554:-1;
                foreach(var pickup in world.Data.pickups)if(pickup.type=="healup"&&pickup.tx==rewardX)return Collected(pickup);
                return false;
            }
            for(int y=f.y;y<f.y+f.h;y++)for(int x=f.x;x<f.x+f.w;x++)if(WorldPrototype.Gate(world.Motor.Get(x,y)))return false;return true;
        }
    }
}
