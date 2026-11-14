using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace Unfin
{
    public sealed partial class WorldPrototype : MonoBehaviour
    {
        public TextAsset mapJson;
        public Tilemap[] maps; // region * 3 + walls / one-way platforms / water
        public PrototypeTile[] terrainTiles; // 47 shapes x 3 patterns per region
        public PrototypeTile[] filletTiles;
        public Tilemap[] filletMaps; // Visual concave arcs only, no collision.
        public Region1TerrainCatalog region1Terrain;
        byte[] terrainPatterns;
        public Camera worldCamera;
        public Transform player;
        public Texture2D overview;
        public WorldData Data { get; private set; }
        public GridMotor Motor { get; private set; }
        public WorldProgression Progression {get;private set;}
        public bool flying;
        public bool MapOpen => PrologueFlow.IsTransitioning||showMap||StilldrawnTitle.Blocking||(Progression!=null&&Progression.ShopBlocksInput);
        public bool ExplorationMapOpen=>showMap;
        public void ToggleExplorationMap(){if(Data.isPrologue)return;if(StilldrawnTitle.Blocking||Progression.ShopBlocksInput)return;showMap=!showMap;Time.timeScale=showMap?0:1;if(!showMap)Progression.CloseNotes();GameAudio.Emit(showMap?SoundCue.MapOpen:SoundCue.MapClose,player.position);}
        readonly PlatformCameraFollow cameraFollow = new PlatformCameraFollow();
        int cameraResetSerial = -1;
        public int MoveInput {get;private set;}
        public int WallSide {get;private set;}
        public bool OnWall => WallSide!=0;
        public bool WallMoving => OnWall && Mathf.Abs(Motor.vy)>1;
        float jumpStartBottom;
        bool showMap;
        float jumpBuffer, coyote, wallLock, dashTime, dashCooldown, accumulator;
        float actionPause;
        public bool ActionLocked=>actionPause>0;
        public void PausePlayerOnHit(float seconds){actionPause=Mathf.Max(actionPause,seconds);MoveInput=0;}
        int facing = 1, dashDirection=1, wallJumpSide;
        float wallReturnTime;
        float wallJumpGrace;
        int recentWallSide;
        void ClearWallJumpGrace(){wallJumpGrace=0;recentWallSide=0;}
        bool airDashUsed, dashAirborne;
        float dashFallSpeed,waxSlow;
        public void ApplyWaxSlow(float seconds){waxSlow=Mathf.Max(waxSlow,seconds);}
        readonly Dictionary<int,TileBase> arenaHotTiles=new Dictionary<int,TileBase>();
        readonly List<Tile> arenaHotClones=new List<Tile>();
        public int HeatedPlatformCount=>arenaHotTiles.Count;
        public void SetArenaHeat(WorldArea area,bool enabled){
            if(Motor==null)return;
            if(!enabled){foreach(var entry in arenaHotTiles){Motor.cells[entry.Key]=4;changes.Remove(entry.Key);int x=entry.Key%Data.width,y=entry.Key/Data.width;maps[4].SetTile(WorldData.Cell(x,y),entry.Value);maps[4].SetColor(WorldData.Cell(x,y),Color.white);if(entry.Value is Tile original)maps[4].SetTransformMatrix(WorldData.Cell(x,y),original.transform);}arenaHotTiles.Clear();foreach(var tile in arenaHotClones){if(Application.isPlaying)Destroy(tile);else DestroyImmediate(tile);}arenaHotClones.Clear();return;}
            for(int y=area.y;y<area.y+area.h;y++)for(int x=area.x;x<area.x+area.w;x++){int index=y*Data.width+x;if(Motor.Get(x,y)!=4||arenaHotTiles.ContainsKey(index))continue;var original=maps[4].GetTile(WorldData.Cell(x,y)) as Tile;if(original==null)continue;arenaHotTiles[index]=original;var hot=ScriptableObject.CreateInstance<Tile>();hot.sprite=original.sprite;hot.transform=original.transform;hot.color=new Color(1,.78f,.58f);hot.colliderType=Tile.ColliderType.None;arenaHotClones.Add(hot);Motor.cells[index]=15;maps[4].SetTile(WorldData.Cell(x,y),hot);}
        }
        public int JumpSerial { get; private set; }
        public int ResetSerial { get; private set; }
        public bool JumpMoving { get; private set; }
        public float JumpLaunchSpeed { get; private set; }
        public float JumpElapsed { get; private set; }
        public bool ForceFall { get; private set; }
        public bool JumpInProgress { get; private set; }
        public bool DashAirborne => dashAirborne;
        public bool AirDashUsed => airDashUsed;
        // The approved dash drawings cover180ms; stretch their clock to the new duration.
        public float DashElapsed => (PlayerMotion.DashDuration-dashTime)*(.18f/PlayerMotion.DashDuration);
        public int Facing => facing;
        public bool IsDashing => dashTime > 0;
        Vector2 safe;
        float drowning,recovering;
        bool releaseAfterHazard;
        public bool HazardInputBlocked => drowning>0||recovering>0||releaseAfterHazard||(Progression!=null&&Progression.PortalTravelling);
        public bool IsDrowning => drowning>0;
        public bool IsReviving => recovering>0;
        public float LifeElapsed => IsDrowning?PlayerLifeSprites.DeathDuration-drowning:IsReviving?PlayerLifeSprites.ReviveDuration-recovering:0;
        public Vector2 CheckpointPosition => new Vector2((safe.x+6)/16,-(safe.y+11)/16);
        float dustClock,dashDustClock;
        Vector2 EffectFeet => new Vector2((Motor.x+GridMotor.Width*.5f)/16,-(Motor.y+GridMotor.Height)/16);
        readonly Dictionary<int, Change> changes = new Dictionary<int, Change>();
        readonly List<int> changeKeys = new List<int>();
        readonly Dictionary<int, PrototypeTile> terrainLookup = new Dictionary<int, PrototypeTile>();
        readonly Dictionary<int, PrototypeTile> filletLookup = new Dictionary<int, PrototypeTile>();
        sealed class Change { public int original; public float time; public bool gone,hot; public TileBase tile; }

        public static int Layer(int type) => type == 9 ? 2 : GridMotor.Platform(type) ? 1 : 0;
        public static bool Gate(int type) => type == 2 || type == 5 || type == 7 || type == 13 || type == 14 || type == 18;
        void Awake()
        {
            if(worldCamera!=null&&worldCamera.GetComponent<WorldCameraGrade>()==null)worldCamera.gameObject.AddComponent<WorldCameraGrade>();
            Data = JsonUtility.FromJson<WorldData>(mapJson.text);
            CheckpointPlan.Apply(Data);
            Motor = new GridMotor(Data, Data.Decode());
            Progression=new WorldProgression(this);
            terrainPatterns = TerrainPatterns.Create(Data.width, Data.height);
            foreach (var tile in terrainTiles)
                terrainLookup[TerrainPatterns.Key(tile.region - 1, tile.patternVariant, tile.exposedEdges)] = tile;
            foreach (var tile in filletTiles)
                filletLookup[TerrainPatterns.Key(tile.region - 1, tile.patternVariant, tile.exposedEdges)] = tile;
            worldCamera.orthographicSize = Data.orthographicSize;
            Respawn(Data.startX, Data.startFloor);
            if(Application.isPlaying){
                if(!Data.isPrologue){InkHazardVisual.Create(this);Region2Presentation.Create(this);Region2Story.Create(this);InteractionTileProximity.Create(this);}
                WorldAtmosphere.Create(this);WorldAmbientDust.Create(this);PlayerLifeVfx.Create(this);
                if(Data.isPrologue){ProximityStory.Create(this);PrologueFlow.Create(this);}
            }
        }
        void Update()
        {
            if(StilldrawnTitle.Blocking||PrologueFlow.IsTransitioning)return;
            if(Progression.HandleShopInput())return;
            var k = Keyboard.current;
            if (k == null) return;
            if (k.mKey.wasPressedThisFrame&&!Progression.NotesEditing)ToggleExplorationMap();
            if(showMap)return;
            if (k.fKey.wasPressedThisFrame) { flying = !flying; Motor.vx = Motor.vy = 0; dashTime=0; airDashUsed=false; ForceFall=true; }
            if (k.rKey.wasPressedThisFrame) RestoreWorld();
            if (k.f1Key.wasPressedThisFrame || k.digit1Key.wasPressedThisFrame) Respawn(Data.startX, Data.startFloor);
            if (k.f2Key.wasPressedThisFrame || k.digit2Key.wasPressedThisFrame)
                foreach (var p in Data.pickups) if (p.type == "save" && p.tx >= Data.regionX) { Respawn(p.tx, p.floor); break; }
            if (k.eKey.wasPressedThisFrame&&flying) OpenNearbyGate();
            if (k.kKey.wasPressedThisFrame) Progression.Parry();
            if (k.lKey.wasPressedThisFrame) Progression.Heal();
            // A single press completes the normal arc, including after key release.
            // Short taps must still reach the map's three-tile platform steps.
            if (k.spaceKey.wasPressedThisFrame) RequestJump();
            int ix = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0);
            int iy = (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0) - (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0);
            if (k.leftShiftKey.wasPressedThisFrame || k.rightShiftKey.wasPressedThisFrame) RequestDash(ix);
            accumulator += Mathf.Min(Time.deltaTime, .1f);
            const float dt = 1f / 120;
            while (accumulator >= dt) { Step(dt, ix, iy); accumulator -= dt; }
            SyncView();
        }
        public void RequestJump()
        {
            if(HazardInputBlocked||showMap)return;
            jumpBuffer=.12f;
            if(IsDashing&&!dashAirborne)dashTime=0;
        }
        public bool RequestDash(int inputDirection=0)
        {
            if(HazardInputBlocked||ActionLocked||showMap||flying||IsDashing||dashCooldown>0||(!Motor.grounded&&airDashUsed)||!Progression.Has("dash"))return false;
            ClearWallJumpGrace();WallSide=0;wallReturnTime=0;wallLock=.25f;
            dashAirborne=!Motor.grounded;
            dashFallSpeed=(!JumpInProgress||ForceFall)?Mathf.Max(0,Motor.vy):0;
            if(dashAirborne)airDashUsed=true;
            dashDirection=inputDirection!=0?inputDirection:facing;facing=dashDirection;
            GameAudio.Emit(SoundCue.Dash,EffectFeet);
            PaperVfx.Get()?.Dash(EffectFeet+Vector2.up*.6f,dashDirection);dashDustClock=.05f;
            dashTime=PlayerMotion.DashDuration;dashCooldown=PlayerMotion.DashCooldown;jumpBuffer=0;
            return true;
        }
        void Launch(float speed,int ix)
        {
            GameAudio.Emit(SoundCue.Jump,EffectFeet);
            PaperVfx.Get()?.Dust(EffectFeet,.7f,ix,true);
            ClearWallJumpGrace();WallSide=0;wallReturnTime=0;Motor.vy=-speed;Motor.grounded=false;jumpBuffer=0;coyote=0;ForceFall=false;
            JumpInProgress=true;JumpSerial++;JumpMoving=ix!=0;JumpLaunchSpeed=speed;JumpElapsed=0;
            jumpStartBottom=Motor.y+GridMotor.Height;
        }
        int EligibleWall()
        {
            if(!Progression.Has("walljump"))return 0;
            int wall=Motor.TouchingWall();
            if(wall!=0){
                int column=Mathf.FloorToInt((wall>0?Motor.Right+1:Motor.Left-1)/16);
                if(Motor.Get(column,Mathf.FloorToInt((Motor.y+10)/16))==8&&!Progression.Has("climb"))return 0;
            }
            return wall;
        }
        void HoldWall(int wall,int iy)
        {
            WallSide=wall;Motor.SnapToWall(wall);facing=wall;Motor.vx=0;
            Motor.vy=Progression.Has("climb")?iy*55:Mathf.Clamp(Motor.vy,20,60);
            recentWallSide=wall;wallJumpGrace=.12f;coyote=0;
            ForceFall=true;JumpInProgress=false;
        }
        void LaunchGroundJump(bool wax,int ix)
        {
            Launch(PlayerMotion.JumpSpeed*(wax?.84f:1),ix);
            // A directional launch carries forward momentum, then blends to slower air steering.
            if(ix!=0)Motor.vx=ix*PlayerMotion.JumpHorizontal;
        }
        // Shared fixed-step entry point for gameplay and deterministic motor checks.
        public void Simulate(float dt,int ix=0,int iy=0){Step(dt,ix,iy);SyncView();}

        void Step(float dt, int ix, int iy)
        {
            if(showMap){MoveInput=0;return;}
            waxSlow=Mathf.Max(0,waxSlow-dt);Progression.Tick(dt,ix,iy);
            if(Progression.PortalTravelling){MoveInput=0;Motor.vx=Motor.vy=0;return;}
            if(drowning>0){
                MoveInput=0;Motor.vx=Motor.vy=0;Motor.grounded=false;drowning=Mathf.Max(0,drowning-dt);
                if(drowning==0){Motor.x=safe.x;Motor.y=safe.y;Motor.vx=Motor.vy=0;Motor.grounded=true;recovering=PlayerLifeSprites.ReviveDuration;releaseAfterHazard=true;ResetSerial++;Progression.Restore();GameAudio.Emit(SoundCue.Respawn,EffectFeet);PaperVfx.Get()?.Dust(EffectFeet,.65f,0,true);}
                TickChanges(dt);return;
            }
            if(recovering>0||releaseAfterHazard){
                recovering=Mathf.Max(0,recovering-dt);MoveInput=0;Motor.vx=Motor.vy=0;
                var keys=Keyboard.current;bool actionHeld=keys!=null&&(keys.spaceKey.isPressed||keys.leftShiftKey.isPressed||keys.rightShiftKey.isPressed);
                if(recovering==0&&ix==0&&iy==0&&!actionHeld)releaseAfterHazard=false;
                TickChanges(dt);return;
            }
            if(actionPause>0){actionPause=Mathf.Max(0,actionPause-dt);MoveInput=0;TickChanges(dt);return;}
            MoveInput=ix;
            if(flying||showMap){WallSide=0;ClearWallJumpGrace();}
            if(dashTime>0&&dashTime<dt-0.000001f){float first=dashTime;Step(first,ix,iy);Step(dt-first,ix,iy);return;}
            dashCooldown -= dt; wallLock -= dt; jumpBuffer -= dt;wallReturnTime-=dt;wallJumpGrace-=dt;
            if(Motor.grounded)ClearWallJumpGrace();
            if (ix != 0 && !IsDashing) facing = ix;
            JumpElapsed+=dt;
            if (flying)
            {
                Motor.x = Mathf.Clamp(Motor.x + ix * 260 * dt, 0, Data.width * 16 - GridMotor.Width);
                Motor.y = Mathf.Clamp(Motor.y + iy * 260 * dt, 0, Data.height * 16 - GridMotor.Height);
                Motor.vx = Motor.vy = 0;
            }
            else
            {
                if (Motor.grounded) coyote = .09f; else coyote -= dt;
                bool dashStep=IsDashing;
                if (dashStep) { Motor.vx=dashDirection*PlayerMotion.DashSpeed;Motor.vy=0; }
                else
                {
                    int under = Mathf.FloorToInt((Motor.y + GridMotor.Height + 1) / 16);
                    bool wax = Motor.grounded && (waxSlow>0||Motor.Get(Mathf.FloorToInt((Motor.x + 6) / 16), under) == 16);
                    if (wallLock <= 0) Motor.vx = Mathf.Lerp(Motor.vx, ix * (Motor.grounded ? PlayerMotion.GroundSpeed : PlayerMotion.AirSpeed) * (wax ? .5f : 1), dt * (Motor.grounded ? 18 : wallReturnTime>0&&ix==wallJumpSide ? PlayerMotion.WallReturnAcceleration : 10));
                    Motor.vy = Mathf.Min(PlayerMotion.FallSpeed, Motor.vy + PlayerMotion.Gravity * dt);
                    int wall = EligibleWall();
                    // Remember an eligible held wall before away input releases it. The release
                    // regrab lock must not suppress a jump pressed just after changing direction.
                    if(OnWall&&wall==WallSide&&!Motor.grounded&&wallLock<=0){recentWallSide=wall;wallJumpGrace=.12f;}
                    int jumpWall=wall!=0&&wallLock<=0?wall:
                        wallJumpGrace>0&&Progression.Has("walljump")?recentWallSide:0;
                    bool attachedBeforeRelease=OnWall;
                    if(attachedBeforeRelease)coyote=0;
                    bool releasing=OnWall&&ix==-WallSide;
                    if(releasing){WallSide=0;wallLock=.18f;ForceFall=true;JumpInProgress=false;}
                    bool hold=wall!=0&&wallLock<=0&&!showMap&&
                        (ix==wall||!Motor.grounded&&WallSide==wall);
                    WallSide=hold?wall:0;
                    if(hold)HoldWall(wall,iy);

                    if (jumpBuffer > 0 && iy > 0 && Motor.grounded && Motor.Row(under, true) && !Motor.Row(under, false))
                    { Motor.dropTime = .22f; Motor.y += 2; coyote = 0; jumpBuffer = 0; }
                    if (jumpBuffer > 0 && coyote > 0)
                    { LaunchGroundJump(wax,ix); }
                    else if (jumpBuffer > 0 && (!Motor.grounded||OnWall||attachedBeforeRelease) && jumpWall != 0)
                    {
                        Launch(PlayerMotion.WallJumpSpeed,-jumpWall);
                        // Holding toward this wall selects a short hop for repeated same-wall jumps.
                        Motor.vx=-jumpWall*(ix==jumpWall?PlayerMotion.WallReturnPush:PlayerMotion.WallJumpHorizontal);
                        facing=-jumpWall;wallJumpSide=jumpWall;wallReturnTime=.35f;
                        wallLock=PlayerMotion.WallRegrabDelay;
                    }
                }
                bool wasAirborne=!Motor.grounded;float landingSpeed=Motor.vy;
                Motor.Move(dt);
                if(Motor.grounded)ClearWallJumpGrace();
                if(wasAirborne&&Motor.grounded&&landingSpeed>45){PaperVfx.Get()?.Dust(EffectFeet,Mathf.Clamp(landingSpeed/280,.4f,1.4f),0,true);GameAudio.Emit(SoundCue.Land,EffectFeet,Mathf.Clamp(landingSpeed/280,.4f,1.4f));}
                if(!showMap){
                    dustClock-=dt;dashDustClock-=dt;
                    if(IsDashing&&dashDustClock<=0){PaperVfx.Get()?.Dash(EffectFeet+Vector2.up*.55f,facing);dashDustClock=.065f;}
                    else if(!IsDashing&&dustClock<=0&&((Motor.grounded&&Mathf.Abs(Motor.vx)>40)||WallMoving)){
                        PaperVfx.Get()?.Dust(OnWall?EffectFeet+new Vector2(WallSide*.35f,.5f):EffectFeet,.28f,facing,true);dustClock=.13f;
                    }
                }
                // Collision can establish contact during Move. Attach immediately, including
                // walking into a wall from the floor, without requiring an initial jump.
                if(!dashStep&&wallLock<=0&&ix!=0){
                    int contact=EligibleWall();
                    if(contact==ix)HoldWall(contact,iy);
                }
                if(OnWall){
                    int sideColumn=Mathf.FloorToInt((WallSide>0?Motor.Right+1:Motor.Left-1)/16);
                    if((Motor.grounded&&ix!=WallSide)||!Motor.Column(sideColumn,Motor.Top+2,GridMotor.BodyHeight-4))WallSide=0;
                }
                // Crossing below the takeoff surface without contact means a continuing fall.
                if(JumpInProgress&&!Motor.grounded&&Motor.vy>0&&Motor.y+GridMotor.Height>jumpStartBottom+.5f)
                {JumpInProgress=false;ForceFall=true;}
                if(dashStep)
                {
                    if(!dashAirborne)
                    {
                        float bottom=Motor.Bottom;int row=Mathf.RoundToInt(bottom/16);
                        if(Mathf.Abs(bottom-row*16)<.02f&&(Motor.Row(row,false)||(Motor.dropTime<=0&&Motor.Row(row,true))))Motor.grounded=true;
                    }
                    if(!Motor.grounded&&!dashAirborne){dashAirborne=true;airDashUsed=true;}
                    dashTime=Mathf.Max(0,dashTime-dt);if(dashTime<.000001f)dashTime=0;
                    if(!IsDashing){Motor.vy=dashAirborne?dashFallSpeed:0;ForceFall=dashAirborne;if(dashAirborne)JumpInProgress=false;}
                }
                if(Motor.grounded)
                {
                    airDashUsed=false;ForceFall=false;JumpInProgress=false;
                    // Consume a valid buffer on contact, even during a landing drawing.
                    if(jumpBuffer>0&&iy<=0&&!IsDashing)
                    {
                        int floor=Mathf.FloorToInt((Motor.y+GridMotor.Height+1)/16);
                        bool waxLanding=Motor.Get(Mathf.FloorToInt((Motor.x+6)/16),floor)==16;
                        LaunchGroundJump(waxLanding,ix);
                    }
                }
                CheckGround();
            }
            TickChanges(dt);
        }
        void CheckGround()
        {
            int left = Mathf.FloorToInt(Motor.Left / 16), right = Mathf.FloorToInt((Motor.Right - .01f) / 16);
            int bottom = Mathf.FloorToInt((Motor.y + GridMotor.Height + 1) / 16);
            bool hazard = false, unstable = false;
            for (int x = left; x <= right; x++)
            {
                int t = Motor.Get(x, bottom);
                hazard |= Motor.InLiquid(x*16+8,Motor.y+GridMotor.Height-.01f);
                unstable |= t == 6 || t == 15;
                if (Motor.grounded && (t == 6 || t == 15)) Schedule(x, bottom, t);
                for (int y = Mathf.FloorToInt((Motor.Top + 2) / 16); y <= Mathf.FloorToInt((Motor.Bottom - 1) / 16); y++)
                    hazard |= Motor.InLiquid(x*16+8,Mathf.Min((y+1)*16-.01f,Motor.y+GridMotor.Height-2));
            }
            hazard|=Motor.Bottom>=(Data.height-1)*16;
            if (hazard) {GameAudio.Emit(SoundCue.Hazard,player.position);BeginDeath();}
            else if (Motor.grounded && !unstable) {
                foreach(var point in Data.pickups){
                    if(point.type!="save"||Mathf.Abs(Motor.x+6-(point.tx*16+8))>16||Mathf.Abs(Motor.Bottom-point.floor*16)>3)continue;
                    var anchor=SafeAnchor(point.tx*16+2,point.floor*16-GridMotor.Height);
                    if(Vector2.Distance(anchor,safe)>.1f){safe=anchor;Progression.Restore();GameAudio.Emit(SoundCue.Respawn,EffectFeet,.4f);}
                    break;
                }
            }
        }
        Vector2 SafeAnchor(float x,float y){
            if(SafeStanding(x,y))return new Vector2(x,y);
            for(int offset=1;offset<=8;offset++)foreach(int direction in new[]{-1,1})
                if(SafeStanding(x+offset*16*direction,y))return new Vector2(x+offset*16*direction,y);
            return new Vector2(x,y);
        }
        public void BeginDeath(){
            if(HazardInputBlocked)return;
            Progression.Health=0;GameAudio.Emit(SoundCue.Death,player.position,.6f);drowning=PlayerLifeSprites.DeathDuration;WallSide=0;Motor.vx=Motor.vy=0;Motor.grounded=false;
            ClearWallJumpGrace();dashTime=jumpBuffer=coyote=wallLock=wallReturnTime=0;airDashUsed=false;JumpInProgress=false;ForceFall=true;
        }
        public void ClearGateAt(int x,int y){
            int type=Motor.Get(x,y);if(!Gate(type))return;
            var broken=new List<Vector2Int>();var pending=new Queue<Vector2Int>();pending.Enqueue(new Vector2Int(x,y));
            while(pending.Count>0){var p=pending.Dequeue();if(p.x<0||p.y<0||p.x>=Data.width||p.y>=Data.height||Motor.Get(p.x,p.y)!=type)continue;
                broken.Add(p);Motor.cells[p.y*Data.width+p.x]=0;Map(p.x,type).SetTile(WorldData.Cell(p.x,p.y),null);RefreshTerrain(p.x,p.y);
                pending.Enqueue(p+Vector2Int.up);pending.Enqueue(p+Vector2Int.down);pending.Enqueue(p+Vector2Int.left);pending.Enqueue(p+Vector2Int.right);
            }SpecialBlockFeedback.Emit(this,broken,type);
        }
        public void TravelTo(int x,int floor){var checkpoint=safe;float health=Progression.Health;int charges=Progression.HealingCharges;bool shield=Progression.ShieldReady;Respawn(x,floor);safe=checkpoint;Progression.Health=health;Progression.HealingCharges=charges;Progression.ShieldReady=shield;}
        public void BuildParryBridge(){
            UnityEngine.Tilemaps.TileBase bridge=null;var map=maps[4];
            for(int x=408;x<458&&bridge==null;x++)for(int y=78;y<=88&&bridge==null;y++)if(Motor.Get(x,y)==4)bridge=map.GetTile(WorldData.Cell(x,y));
            if(bridge==null)foreach(var tile in terrainTiles)if(tile.region==2&&tile.htmlType==4){bridge=tile;break;}
            for(int x=459;x<=468;x++){Motor.cells[88*Data.width+x]=4;map.SetTile(WorldData.Cell(x,88),bridge);}
        }
        bool SafeStanding(float px,float py){
            int row=Mathf.RoundToInt((py+GridMotor.Height)/16);
            for(int x=Mathf.FloorToInt((px-16)/16);x<=Mathf.FloorToInt((px+GridMotor.Width+16)/16);x++){
                int t=Motor.Get(x,row);if(t!=1&&t!=4&&t!=16)return false;
                if(Motor.Column(x,py,GridMotor.CollisionHeight-.1f))return false;
            }return true;
        }
        Tilemap Map(int x, int type) => maps[(x >= Data.regionX ? 3 : 0) + Layer(type)];
        void Schedule(int x, int y, int type)
        {
            int index = y * Data.width + x;
            if (!changes.ContainsKey(index)){bool hot=type==15&&(arenaHotTiles.ContainsKey(index)||Progression.HeatsPlatform(x,y));changes.Add(index,new Change{original=type,time=type==15?(hot?.6f:1.5f):.45f,hot=hot,tile=Map(x,type).GetTile(WorldData.Cell(x,y))});}
        }
        void TickChanges(float dt)
        {
            changeKeys.Clear(); changeKeys.AddRange(changes.Keys);
            foreach (int index in changeKeys)
            {
                Change change = changes[index];if(change.original==15&&!change.gone){bool hot=arenaHotTiles.ContainsKey(index)||Progression.HeatsPlatform(index%Data.width,index/Data.width);if(hot!=change.hot){change.time*=hot?.4f:2.5f;change.hot=hot;}}change.time -= dt;
                if (change.time > 0) {
                    if(!change.gone){
                        int cx=index%Data.width,cy=index/Data.width;var surface=Map(cx,change.original);var cell=WorldData.Cell(cx,cy);
                        surface.SetTileFlags(cell,TileFlags.None);
                        float urgency=1-Mathf.Clamp01(change.time/(change.original==15?1.5f:.45f));
                        // Crumbling stone warns immediately with a fast, visible side-to-side
                        // shake. Only the drawing moves: grid contact height stays fixed.
                        float shake=change.original==6
                            ?Mathf.Sin((.45f-change.time)*Mathf.PI*2*18)*Mathf.Lerp(.035f,.06f,urgency)
                            :Mathf.Sin(change.time*70)*.012f*urgency;
                        surface.SetTransformMatrix(cell,Matrix4x4.Translate(Vector3.right*shake)*((UnityEngine.Tilemaps.Tile)change.tile).transform);
                        surface.SetColor(cell,new Color(1,1,1,1-.22f*urgency*(.5f+.5f*Mathf.Sin(change.time*32))));
                    }
                    continue;
                }
                int x = index % Data.width, y = index / Data.width;
                if (!change.gone)
                {
                    Motor.cells[index] = 0; Map(x, change.original).SetTile(WorldData.Cell(x, y), null);
                    RefreshTerrain(x, y);
                    SpecialBlockFeedback.Emit(this,new[]{new Vector2Int(x,y)},change.original);
                    change.gone = true; change.time = change.original == 15 ? 3 : 2.6f;
                }
                else if (!(Motor.x < (x + 1) * 16 && Motor.x + 12 > x * 16 && Motor.y < (y + 1) * 16 && Motor.y + 22 > y * 16))
                {
                    Motor.cells[index] = (byte)change.original; Map(x, change.original).SetTile(WorldData.Cell(x, y), change.tile); changes.Remove(index);GameAudio.Emit(SoundCue.Restore,new Vector2(x,-y),.6f);
                    Map(x,change.original).SetColor(WorldData.Cell(x,y),Color.white);Map(x,change.original).SetTransformMatrix(WorldData.Cell(x,y),((UnityEngine.Tilemaps.Tile)change.tile).transform);
                    RefreshTerrain(x, y);
                }
            }
        }
        // Explicit QA interaction: remove one connected gate; does not implement combat/progression.
        void OpenNearbyGate()
        {
            int cx = Mathf.FloorToInt((Motor.x + 6) / 16), cy = Mathf.FloorToInt((Motor.y + 11) / 16);
            for (int radius = 0; radius <= 3; radius++)
            for (int y = cy - radius; y <= cy + radius; y++)
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                int type = Motor.Get(x, y);
                if (!Gate(type) || x < 0 || y < 0 || x >= Data.width || y >= Data.height) continue;
                ClearGateAt(x,y);
                return;
            }
        }
        // Web prototype review shortcut, alongside the existing flight/region QA controls.
        // Accept only a standing position on real terrain with room for the player.
        public void PreviewPosition(string cell)
        {
            var parts=cell.Split(',');
            if(parts.Length!=2||!int.TryParse(parts[0],out int x)||!int.TryParse(parts[1],out int y)||
                Motor==null||x<0||x>=Data.width||y<2||y>=Data.height)return;
            int t=Motor.Get(x,y);
            if(t==0||t==3||t==9)return;
            if(GridMotor.Wall(Motor.Get(x,y-1))||GridMotor.Wall(Motor.Get(x,y-2)))return;
            flying=false;showMap=false;Respawn(x,y);
        }
        public void Respawn(int tx, int floor)
        {
            if(Progression!=null){Progression.ClosePickupNotice();Progression.CancelInteractionChoice();if(Progression.ShopOpen)Progression.CloseShop();}
            Motor.x = tx * 16 + 2; Motor.y = floor * 16 - 22; Motor.vx = Motor.vy = 0; Motor.grounded = false;
            ClearWallJumpGrace();WallSide=0;wallReturnTime=0; safe = new Vector2(Motor.x, Motor.y); dashTime = dashCooldown = jumpBuffer = coyote = wallLock = 0;
            drowning=recovering=actionPause=0;releaseAfterHazard=false;
            safe=SafeAnchor(Motor.x,Motor.y);
            airDashUsed=dashAirborne=false;ForceFall=JumpInProgress=false;accumulator=0;MoveInput=0;ResetSerial++;
            SyncView();if(ResetSerial>1)GameAudio.Emit(SoundCue.Respawn,player.position);
            Progression.Restore();
        }
        void RefreshTerrain(int cx, int cy)
        {
            for (int y = cy - 1; y <= cy + 1; y++)
            for (int x = cx - 1; x <= cx + 1; x++)
            {
                if (x < 0 || y < 0 || x >= Data.width || y >= Data.height) continue;
                int region=x>=Data.regionX?1:0, variant=terrainPatterns[y*Data.width+x];
                // Gates and temporary platforms never change ordinary terrain occupancy.
                // The v8 atlas already exposes ordinary terrain beside every special tile.
                if (region == 0 && region1Terrain != null)
                {
                    if (Motor.Get(x,y)==1) Map(x,1).SetTile(WorldData.Cell(x,y),region1Terrain.Terrain(x,y));
                    filletMaps[0].SetTile(WorldData.Cell(x,y),
                        TerrainTopology.FilletMask(Motor.Get,x,y)==0 ? null : region1Terrain.Fillet(x,y));
                    continue;
                }
                if (Motor.Get(x,y)==1)
                {
                    int mask = TerrainTopology.Mask(Motor.Get, x, y);
                    Map(x, 1).SetTile(WorldData.Cell(x, y), terrainLookup[TerrainPatterns.Key(region,variant,mask)]);
                }
                int fillet=TerrainTopology.FilletMask(Motor.Get,x,y);
                filletMaps[region].SetTile(WorldData.Cell(x,y),fillet==0?null:filletLookup[TerrainPatterns.Key(region,variant,fillet)]);
                RegionalTilePalette.ApplyTerrainCell(this,Data,x,y);
            }
        }
        void RestoreWorld() => UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        void SyncView()
        {
            player.position = new Vector3((Motor.x + 6) / 16, -(Motor.y + 11) / 16, 0);
            if (cameraResetSerial != ResetSerial)
            {
                cameraResetSerial = ResetSerial;
                cameraFollow.Reset(player.position, worldCamera.orthographicSize, worldCamera.aspect, Data.width, Data.height);
                worldCamera.transform.position = new Vector3(cameraFollow.Position.x, cameraFollow.Position.y, -10);
            }
        }
        void LateUpdate() => TickCamera(Time.deltaTime);
        public void TickCamera(float dt)
        {
            if (Motor == null || Data == null) return;
            var p = cameraFollow.Tick(player.position, new Vector2(Motor.vx, -Motor.vy) / 16,
                Motor.grounded, flying, dt, worldCamera.orthographicSize, worldCamera.aspect, Data.width, Data.height);
            worldCamera.transform.position = new Vector3(p.x, p.y, -10);
        }
        void OnGUI(){if(Data!=null&&!Data.isPrologue&&!StilldrawnTitle.Blocking)Progression.DrawGUI();}
    }
}
