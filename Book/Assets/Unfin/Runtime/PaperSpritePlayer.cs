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
        public string CurrentAnimation {get;private set;}="Idle";
        public int CurrentFrame {get;private set;}
        public float AnimationTime {get;private set;}
        int jumpSerial=-1,resetSerial=-1;
        bool wasGrounded=true,jumpActive,movingJump;
        Vector2 dashOffset;
        Vector2 fallOffset;
        float fallBlend;
        float projectileContactFlash;
        public void ShowProjectileContact(Vector2 point)
        {
            projectileContactFlash=.18f;PaperVfx.Get()?.Hit(point,world.Facing);GameAudio.Emit(SoundCue.Hit,point,.65f);
        }
        void Update(){Tick(Mathf.Min(Time.deltaTime,.1f));ReadAttackInput();}
        void Play(string name){if(CurrentAnimation==name)return;CurrentAnimation=name;AnimationTime=0;}
        public void Tick(float dt)
        {
            if(world==null||world.Motor==null||library==null||visual==null)return;
            // Sample short impact drawings even when a rendered frame spans the whole impact.
            if(dt>1f/120f){while(dt>1f/120f){Tick(1f/120f);dt-=1f/120f;}if(dt>0)Tick(dt);return;}
            var m=world.Motor;
            projectileContactFlash=Mathf.Max(0,projectileContactFlash-dt);
            if(resetSerial!=world.ResetSerial){resetSerial=world.ResetSerial;jumpSerial=world.JumpSerial;wasGrounded=true;jumpActive=false;CurrentAnimation="Idle";AnimationTime=0;ResetCombat();}
            AnimationTime+=dt*(CurrentAnimation=="Run"?PlayerMotion.RunPlaybackRate:1);fallBlend=Mathf.Max(0,fallBlend-dt);
            bool moving=Mathf.Abs(m.vx)>12;
            bool newJump=jumpSerial!=world.JumpSerial;
            if(newJump){jumpSerial=world.JumpSerial;jumpActive=true;movingJump=world.JumpMoving;CurrentAnimation=movingJump?"MovingJump":"Jump";AnimationTime=0;}
            if(world.IsDashing)
            {
                if(CurrentAnimation!="Dash")
                {
                    var prior=library.Find(CurrentAnimation);var frame=prior.frames[prior.FrameAt(AnimationTime)];
                    dashOffset=world.DashAirborne?new Vector2(prior.anchorX-364+412-frame.head.x,308-frame.head.y)/library.pixelsPerUnit:Vector2.zero;
                    // X moves the sprite opposite the anchor change; Y preserves the airborne head height.
                    dashOffset.x=-dashOffset.x;if(CurrentAnimation=="Fall")dashOffset+=fallOffset*(fallBlend/.08f);Play("Dash");
                }
                AnimationTime=world.DashElapsed;
            }
            else if(world.flying){jumpActive=false;Play("Idle");}
            else if(world.OnWall){jumpActive=false;Play(world.WallMoving?"WallClimb":"WallHold");}
            else if(!m.grounded)
            {
                if(world.ForceFall)jumpActive=false;
                if(jumpActive)
                {
                    Play(movingJump?"MovingJump":"Jump");
                    float progress=Mathf.Clamp01(1+m.vy/Mathf.Max(1,world.JumpLaunchSpeed));
                    if(m.vy<0)AnimationTime=Mathf.Lerp(movingJump?0:.035f,movingJump?.23f:.265f,progress);
                    else AnimationTime=Mathf.Lerp(movingJump?.23f:.265f,movingJump?.459f:.494f,Mathf.Clamp01(m.vy/Mathf.Max(1,world.JumpLaunchSpeed)));
                    if(!movingJump&&world.JumpElapsed<.035f)AnimationTime=world.JumpElapsed;
                }
                else
                {
                    bool fromDash=CurrentAnimation=="Dash";
                    bool fromJump=CurrentAnimation=="Jump"||CurrentAnimation=="MovingJump";
                    if(fromJump)
                    {
                        var prior=library.Find(CurrentAnimation);var h=prior.frames[prior.FrameAt(AnimationTime)].head;
                        var next=library.Find("Fall");var n=next.frames[1].head;
                        fallOffset=new Vector2(h.x-prior.anchorX-n.x+next.anchorX,prior.groundY-h.y-next.groundY+n.y)/library.pixelsPerUnit;
                        fallBlend=.08f;
                    }
                    Play("Fall");if(fromDash||fromJump)AnimationTime=.065f;
                }
            }
            else
            {
                if(!wasGrounded&&!newJump){Play(jumpActive?(movingJump?"MovingLand":"JumpLand"):"FallLand");jumpActive=false;}
                else if(CurrentAnimation.EndsWith("Land")&&AnimationTime<library.Find(CurrentAnimation).Duration){}
                else if(moving)
                {
                    if(CurrentAnimation=="Idle"||CurrentAnimation=="RunStop")Play("RunStart");
                    else if(CurrentAnimation!="RunStart"||AnimationTime>=library.Find("RunStart").Duration)Play("Run");
                }
                else
                {
                    if(CurrentAnimation=="Run"||CurrentAnimation=="RunStart"||CurrentAnimation=="Dash")Play("RunStop");
                    else if(CurrentAnimation!="RunStop"||AnimationTime>=library.Find("RunStop").Duration)Play("Idle");
                }
            }
            wasGrounded=m.grounded;
            TickCombat(dt);
            Render(world.Facing);
            ResolveAttackHits();
        }
        public void Render(int facing)
        {
            var clip=library.Find(CurrentAnimation);
            // Intro once, then alternate the two falling drawings until actual contact.
            CurrentFrame=CurrentAnimation=="Fall"&&AnimationTime>=.155f?2-(Mathf.FloorToInt((AnimationTime-.155f)/.14f)%2):clip.FrameAt(AnimationTime);
            visual.sprite=clip.frames[CurrentFrame].sprite;
            visual.color=Color.Lerp(Color.white,new Color(.55f,.53f,.51f,1),projectileContactFlash/.18f);
            transform.localScale=new Vector3(facing<0?-1:1,1,1);
            visual.transform.localPosition=CurrentAnimation=="Dash"?(Vector3)dashOffset:Vector3.zero;
            if(CurrentAnimation=="Fall")visual.transform.localPosition=(Vector3)(fallOffset*(fallBlend/.08f));
            RenderCombat(clip,facing);
            if(trails==null)return;
            for(int i=0;i<trails.Length;i++)
            {
                trails[i].enabled=CurrentAnimation=="Dash";
                trails[i].sprite=visual.sprite;
                trails[i].transform.localPosition=visual.transform.localPosition+Vector3.left*((i+1)*24/library.pixelsPerUnit);
            }
        }
    }
}
