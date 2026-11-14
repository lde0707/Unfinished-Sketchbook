using UnityEngine;

namespace Unfin
{
    // Whole drawings only: no bone deformation or cross-fading between silhouettes.
    [DefaultExecutionOrder(50)]
    public sealed partial class PaperSpritePlayer : MonoBehaviour
    {
        public WorldPrototype world;
        public PaperSpriteLibrary library;
        public SpriteRenderer visual;
        public SpriteRenderer[] trails;
        static readonly string[] LocomotionNames=System.Enum.GetNames(typeof(PlayerLocomotionState));
        readonly FiniteStateMachine<PlayerLocomotionState> locomotion=new FiniteStateMachine<PlayerLocomotionState>(PlayerLocomotionState.Idle);
        public PlayerLocomotionState LocomotionState=>locomotion.State;
        public string CurrentAnimation=>LocomotionNames[(int)locomotion.State];
        public int CurrentFrame {get;private set;}
        public float AnimationTime {get=>locomotion.Time;private set=>locomotion.Time=value;}
        int jumpSerial=-1,resetSerial=-1;
        bool wasGrounded=true,jumpActive,movingJump;
        Vector2 dashOffset;
        Vector2 fallOffset;
        float fallBlend;
        float hitGrace;
        SpriteHitFeedback hitFeedback;
        public bool HitReactionActive => hitFeedback!=null&&hitFeedback.Active;
        [Min(0)] public float thinPlatformLiftPixels=9;
        Vector3 poseAnchor;
        bool poseAnchorCached;
        public void ShowProjectileContact(Vector2 point)
        {
            TryReceiveHit(point,point.x<world.player.position.x?1:-1);
        }
        public bool TryReceiveHit(Vector2 point,int incomingDirection,bool unblockable=false)
        {
            if(world==null||hitGrace>0||world.IsDashing||world.flying||world.MapOpen||world.HazardInputBlocked)return false;
            if(world.Progression.Parrying&&!unblockable){world.Progression.ConfirmParry();PaperVfx.Get()?.Hit(point,-incomingDirection);GameAudio.Emit(SoundCue.Gate,point,.5f);return false;}
            hitGrace=.55f;hitFeedback=SpriteHitFeedback.Get(visual);hitFeedback.Trigger(true);
            bool shielded=world.Progression.ShieldReady;world.Progression.Damage();
            if(!shielded&&!world.HazardInputBlocked)BeginHurt();
            PaperVfx.Get()?.Hit(point,incomingDirection);GameAudio.Emit(SoundCue.Hit,point,.75f);return true;
        }
        void Update(){if(StilldrawnTitle.Blocking||(world!=null&&world.MapOpen))return;Tick(Mathf.Min(Time.deltaTime,.1f));ReadAttackInput();}
        void Play(PlayerLocomotionState state){locomotion.Change(state);}
        public void Tick(float dt)
        {
            if(world==null||world.Motor==null||library==null||visual==null)return;
            // Sample short impact drawings even when a rendered frame spans the whole impact.
            if(dt>1f/120f){while(dt>1f/120f){Tick(1f/120f);dt-=1f/120f;}if(dt>0)Tick(dt);return;}
            var m=world.Motor;
            if(!world.MapOpen)hitGrace=Mathf.Max(0,hitGrace-dt);
            if(resetSerial!=world.ResetSerial){resetSerial=world.ResetSerial;jumpSerial=world.JumpSerial;wasGrounded=true;jumpActive=false;locomotion.Change(PlayerLocomotionState.Idle,true);ResetCombat();ResetReactions();}
            if(world.IsDrowning||world.IsReviving){
                CancelAttack();hitFeedback?.Clear();jumpActive=false;
                Play(world.IsDrowning?PlayerLocomotionState.Death:PlayerLocomotionState.Revive);AnimationTime=world.LifeElapsed;Render(world.Facing);return;
            }
            TickReactions(dt);
            locomotion.Advance(dt*(LocomotionState==PlayerLocomotionState.Run?PlayerMotion.RunPlaybackRate:1));fallBlend=Mathf.Max(0,fallBlend-dt);
            bool moving=Mathf.Abs(m.vx)>12;
            bool newJump=jumpSerial!=world.JumpSerial;
            if(newJump){jumpSerial=world.JumpSerial;jumpActive=true;movingJump=world.JumpMoving;locomotion.Change(movingJump?PlayerLocomotionState.MovingJump:PlayerLocomotionState.Jump,true);}
            if(world.IsDashing)
            {
                if(LocomotionState!=PlayerLocomotionState.Dash)
                {
                    var prior=PlayerLifeSprites.Find(CurrentAnimation)??library.Find(CurrentAnimation);var frame=prior.frames[prior.FrameAt(AnimationTime)];
                    dashOffset=world.DashAirborne?new Vector2(prior.anchorX-364+412-frame.head.x,308-frame.head.y)/library.pixelsPerUnit:Vector2.zero;
                    // X moves the sprite opposite the anchor change; Y preserves the airborne head height.
                    dashOffset.x=-dashOffset.x;if(LocomotionState==PlayerLocomotionState.Fall)dashOffset+=fallOffset*(fallBlend/.08f);Play(PlayerLocomotionState.Dash);
                }
                AnimationTime=world.DashElapsed;
            }
            else if(world.flying){jumpActive=false;Play(PlayerLocomotionState.Idle);}
            else if(world.OnWall){jumpActive=false;Play(world.WallMoving?PlayerLocomotionState.WallClimb:PlayerLocomotionState.WallHold);}
            else if(!m.grounded)
            {
                if(world.ForceFall)jumpActive=false;
                if(jumpActive)
                {
                    Play(movingJump?PlayerLocomotionState.MovingJump:PlayerLocomotionState.Jump);
                    float progress=Mathf.Clamp01(1+m.vy/Mathf.Max(1,world.JumpLaunchSpeed));
                    if(m.vy<0)AnimationTime=Mathf.Lerp(movingJump?0:.035f,movingJump?.23f:.265f,progress);
                    else AnimationTime=Mathf.Lerp(movingJump?.23f:.265f,movingJump?.459f:.494f,Mathf.Clamp01(m.vy/Mathf.Max(1,world.JumpLaunchSpeed)));
                    if(!movingJump&&world.JumpElapsed<.035f)AnimationTime=world.JumpElapsed;
                }
                else
                {
                    bool fromDash=LocomotionState==PlayerLocomotionState.Dash;
                    bool fromJump=LocomotionState==PlayerLocomotionState.Jump||LocomotionState==PlayerLocomotionState.MovingJump;
                    if(fromJump)
                    {
                        var prior=library.Find(CurrentAnimation);var h=prior.frames[prior.FrameAt(AnimationTime)].head;
                        var next=library.Find("Fall");var n=next.frames[1].head;
                        fallOffset=new Vector2(h.x-prior.anchorX-n.x+next.anchorX,prior.groundY-h.y-next.groundY+n.y)/library.pixelsPerUnit;
                        fallBlend=.08f;
                    }
                    Play(PlayerLocomotionState.Fall);if(fromDash||fromJump)AnimationTime=.065f;
                }
            }
            else
            {
                if(!wasGrounded&&!newJump){Play(jumpActive?(movingJump?PlayerLocomotionState.MovingLand:PlayerLocomotionState.JumpLand):PlayerLocomotionState.FallLand);jumpActive=false;}
                else if((LocomotionState==PlayerLocomotionState.JumpLand||LocomotionState==PlayerLocomotionState.MovingLand||LocomotionState==PlayerLocomotionState.FallLand)&&AnimationTime<library.Find(CurrentAnimation).Duration){}
                else if(moving)
                {
                    if(LocomotionState==PlayerLocomotionState.Idle||LocomotionState==PlayerLocomotionState.RunStop)Play(PlayerLocomotionState.RunStart);
                    else if(LocomotionState!=PlayerLocomotionState.RunStart||AnimationTime>=library.Find("RunStart").Duration)Play(PlayerLocomotionState.Run);
                }
                else
                {
                    if(LocomotionState==PlayerLocomotionState.Run||LocomotionState==PlayerLocomotionState.RunStart||LocomotionState==PlayerLocomotionState.Dash)Play(PlayerLocomotionState.RunStop);
                    else if(LocomotionState!=PlayerLocomotionState.RunStop||AnimationTime>=library.Find("RunStop").Duration)Play(PlayerLocomotionState.Idle);
                }
            }
            wasGrounded=m.grounded;
            TickCombat(dt);
            Render(world.Facing);
            ResolveAttackHits();
        }
        public void Render(int facing)
        {
            PaperPlayerSurface.Apply(visual);
            if(!poseAnchorCached){poseAnchor=transform.localPosition;poseAnchorCached=true;}
            var motor=world!=null?world.Motor:null;
            int supportRow=motor!=null?Mathf.RoundToInt(motor.Bottom/16):0;
            bool thinPlatform=motor!=null&&world.Data!=null&&motor.grounded&&!world.flying&&!world.OnWall&&motor.Row(supportRow,true)&&!motor.Row(supportRow,false);
            // Move the entire drawing/effect group together; collision and camera
            // anchors retain their existing coordinates. Pixels use the 128px tile scale.
            transform.localPosition=poseAnchor+Vector3.up*(thinPlatform?thinPlatformLiftPixels/Mathf.Max(1,world.Data.tilePixels):0);
            var clip=PlayerLifeSprites.Find(CurrentAnimation)??library.Find(CurrentAnimation);
            // Intro once, then alternate the two falling drawings until actual contact.
            CurrentFrame=LocomotionState==PlayerLocomotionState.Fall&&AnimationTime>=.155f?2-(Mathf.FloorToInt((AnimationTime-.155f)/.14f)%2):clip.FrameAt(AnimationTime);
            visual.sprite=clip.frames[CurrentFrame].sprite;
            visual.color=Color.white;
            transform.localScale=new Vector3(facing<0?-1:1,1,1)*GridMotor.BodyScale;
            visual.transform.localPosition=LocomotionState==PlayerLocomotionState.Dash?(Vector3)dashOffset:Vector3.zero;
            if(LocomotionState==PlayerLocomotionState.Fall)visual.transform.localPosition=(Vector3)(fallOffset*(fallBlend/.08f));
            RenderCombat(clip,facing);
            RenderReaction();
            if(trails==null)return;
            for(int i=0;i<trails.Length;i++)
            {
                PaperPlayerSurface.Apply(trails[i]);
                trails[i].enabled=LocomotionState==PlayerLocomotionState.Dash;
                trails[i].sprite=visual.sprite;
                trails[i].transform.localPosition=visual.transform.localPosition+Vector3.left*((i+1)*24/library.pixelsPerUnit);
            }
        }
    }
}
