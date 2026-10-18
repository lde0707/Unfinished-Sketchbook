using UnityEngine;
using UnityEngine.InputSystem;

namespace Unfin
{
    public sealed partial class PaperSpritePlayer
    {
        public SpriteRenderer attackEffect;
        public const float ThrustEffectScale = 1.6f;
        PaperAttackArc outerArc;
        public bool SlashEffectVisible => outerArc != null && outerArc.Visible;
        public string AttackAnimation {get;private set;}
        public int AttackFrame {get;private set;}
        public float AttackTime {get;private set;}
        public int AttackFacing {get;private set;}=1;
        public int AttackCount {get;private set;}
        public bool Attacking => AttackAnimation!=null;
        public string DisplayedAnimation => AttackAnimation??CurrentAnimation;
        float combatClock,queuedUntil=-1,comboUntil=-1,recoverUntil=-1;
        int lastStage=-1,stage,attackJumpSerial;
        bool airAttack;
        void ReadAttackInput()
        {
            var k=Keyboard.current;var mouse=Mouse.current;
            if((k!=null&&k.jKey.wasPressedThisFrame)||(mouse!=null&&mouse.leftButton.wasPressedThisFrame))RequestAttack();
        }
        void ResetCombat(){combatClock=0;AttackCount=0;recoverUntil=-1;fallBlend=0;CancelAttack();}
        void CancelAttack(){AttackAnimation=null;AttackTime=0;queuedUntil=comboUntil=-1;lastStage=-1;if(attackEffect!=null)attackEffect.enabled=false;if(outerArc!=null)outerArc.Hide();}
        bool MovingAttack => Mathf.Abs(world.Motor.vx)>8||world.MoveInput!=0;
        string GroundAttackName(int n)=>(MovingAttack?"MoveAttack":"Attack")+(n+1);
        void StartAttack(int n)
        {
            stage=n;airAttack=!world.Motor.grounded;AttackTime=0;AttackFacing=world.Facing;
            attackJumpSerial=world.JumpSerial;queuedUntil=-1;AttackCount++;
            AttackAnimation=airAttack?(world.Motor.vy<0?"AirRiseAttack":"AirFallAttack"):GroundAttackName(n);
            AttackFrame=0;
        }
        public bool RequestAttack()
        {
            if(world==null||world.Motor==null||world.flying||world.MapOpen||world.IsDashing||library==null)return false;
            if(Attacking){if(airAttack||stage==2)return false;queuedUntil=combatClock+.15f;return true;}
            if(world.Motor.grounded&&combatClock<recoverUntil)return false;
            StartAttack(combatClock<=comboUntil&&lastStage>=0&&lastStage<2?lastStage+1:0);
            Render(world.Facing);return true;
        }
        void TickCombat(float dt)
        {
            combatClock+=dt;if(!Attacking)return;
            var m=world.Motor;
            if(world.flying||world.MapOpen||world.IsDashing||attackJumpSerial!=world.JumpSerial||airAttack==m.grounded||(!airAttack&&world.Facing!=AttackFacing)){CancelAttack();return;}
            if(!airAttack)
            {
                string name=GroundAttackName(stage);
                if(name!=AttackAnimation){AttackTime=AttackTime/library.Find(AttackAnimation).Duration*library.Find(name).Duration;AttackAnimation=name;}
            }
            AttackTime+=dt;
            var clip=library.Find(AttackAnimation);
            if(AttackTime+0.00001f>=clip.Duration)
            {
                float ended=combatClock-Mathf.Max(0,AttackTime-clip.Duration);
                if(airAttack){CancelAttack();return;}
                lastStage=stage;comboUntil=ended+.25f;
                if(stage<2&&queuedUntil>=ended){float remainder=Mathf.Max(0,AttackTime-clip.Duration);StartAttack(stage+1);AttackTime=remainder;}
                else{AttackAnimation=null;queuedUntil=-1;if(stage==2){recoverUntil=ended+.5f;comboUntil=-1;}return;}
            }
            AttackFrame=library.Find(AttackAnimation).FrameAt(AttackTime);
        }
        void RenderCombat(PaperSpriteClip locomotion,int facing)
        {
            if(attackEffect!=null)attackEffect.enabled=false;
            if(outerArc!=null)outerArc.Hide();
            if(!Attacking)return;
            var clip=library.Find(AttackAnimation);AttackFrame=clip.FrameAt(AttackTime);
            visual.sprite=clip.frames[AttackFrame].sprite;transform.localScale=new Vector3(AttackFacing,1,1);
            if(airAttack)
            {
                var h=locomotion.frames[CurrentFrame].head;var a=clip.frames[AttackFrame].head;
                var priorOffset=visual.transform.localPosition;
                visual.transform.localPosition=new Vector3((facing*AttackFacing*(h.x-locomotion.anchorX)-(a.x-clip.anchorX))/library.pixelsPerUnit,
                    (locomotion.groundY-h.y-clip.groundY+a.y)/library.pixelsPerUnit,0);
                visual.transform.localPosition+=new Vector3(priorOffset.x*facing*AttackFacing,priorOffset.y,0);
            }
            else visual.transform.localPosition=Vector3.zero;
            RenderOuterArc();
            if(attackEffect==null||airAttack||stage!=2||(AttackFrame!=2&&AttackFrame!=3))return;
            bool move=AttackAnimation.StartsWith("Move");float width=AttackFrame==2?140:200;
            Vector2 anchor=move?(AttackFrame==2?new Vector2(486,420):new Vector2(530,418)):(AttackFrame==2?new Vector2(528,420):new Vector2(562,408));
            attackEffect.enabled=true;attackEffect.sprite=library.thrustEffect;
            attackEffect.transform.localPosition=new Vector3((anchor.x-384)/library.pixelsPerUnit,(560-anchor.y)/library.pixelsPerUnit,0);
            attackEffect.transform.localScale=Vector3.one*(width/1000)*ThrustEffectScale;
        }
        void RenderOuterArc()
        {
            bool active=airAttack?AttackFrame==1:stage<2&&(AttackFrame==2||AttackFrame==3);
            if(!active)return;
            if(outerArc==null)
            {
                var go=new GameObject("Larger paper slash");go.transform.SetParent(visual.transform,false);
                outerArc=go.AddComponent<PaperAttackArc>();outerArc.Initialize(visual);
            }
            bool move=AttackAnimation.StartsWith("Move");
            Vector2 center,radius;float start,end;
            if(airAttack){center=new Vector2(424,282);radius=new Vector2(148,106);start=-76;end=91;}
            else if(stage==0)
            {
                center=move?new Vector2(409,470):new Vector2(447,456);
                radius=new Vector2(142,103);start=-76;end=98;
            }
            else
            {
                center=move?new Vector2(426,330):new Vector2(450,327);
                radius=new Vector2(155,156);start=-82;end=76;
            }
            outerArc.Show(center,radius,start,end,AttackFrame==2?.7f:.95f,library.pixelsPerUnit);
        }
    }
}
