using UnityEngine;
using UnityEngine.InputSystem;

namespace Unfin
{
    public sealed partial class PaperSpritePlayer
    {
        public SpriteRenderer attackEffect;
        public const float ThrustEffectScale = 3.2f;
        public const float ComboPause = .08f;
        public const float AirAttackInterval = .36f;
        PaperAttackArc outerArc;
        public bool SlashEffectVisible => outerArc != null && outerArc.Visible;
        public string AttackAnimation {get;private set;}
        public int AttackFrame {get;private set;}
        readonly FiniteStateMachine<PlayerCombatState> combat=new FiniteStateMachine<PlayerCombatState>(PlayerCombatState.Ready);
        public PlayerCombatState CombatState=>combat.State;
        public float AttackTime {get=>combat.Time;private set=>combat.Time=value;}
        public int AttackFacing {get;private set;}=1;
        public int AttackCount {get;private set;}
        public bool Attacking => combat.State!=PlayerCombatState.Ready;
        public string DisplayedAnimation => AttackAnimation??ReactionAnimation??CurrentAnimation;
        float combatClock,queuedUntil=-1,comboUntil=-1,recoverUntil=-1;
        float airRepeatReady=-1;
        bool queuedAirAttack;
        int lastStage=-1,stage;
        bool airAttack=>combat.State==PlayerCombatState.AirAttack;
        bool attackVfxEmitted;
        void ReadAttackInput()
        {
            var k=Keyboard.current;var mouse=Mouse.current;
            if((k!=null&&k.jKey.wasPressedThisFrame)||(mouse!=null&&mouse.leftButton.wasPressedThisFrame&&(world==null||world.Progression==null||!world.Progression.PointerOverHud)))RequestAttack();
        }
        void ResetCombat(){combatClock=0;AttackCount=0;recoverUntil=airRepeatReady=-1;fallBlend=0;hitGrace=0;hitFeedback?.Clear();CancelAttack();}
        void CancelAttack(bool clearBuffer=true){if(clearBuffer)queuedAirAttack=false;AttackAnimation=null;combat.Change(PlayerCombatState.Ready,true);queuedUntil=comboUntil=-1;lastStage=-1;if(attackEffect!=null)attackEffect.enabled=false;if(outerArc!=null)outerArc.Hide();}
        bool MovingAttack => Mathf.Abs(world.Motor.vx)>8||world.MoveInput!=0;
        // Combo position is independent of the preserved authored clip names.
        // New order: original thrust (3), slash (1), upward slash (2).
        int GroundArtStage => (stage+2)%3;
        bool ThrustAttack => !airAttack&&GroundArtStage==2;
        string GroundAttackName(int n)=>(MovingAttack?"MoveAttack":"Attack")+((n+2)%3+1);
        void StartAttack(int n)
        {
            reaction=null;hitTargets.Clear();attackVfxEmitted=false;queuedAirAttack=false;
            stage=n;combat.Change(world.Motor.grounded?PlayerCombatState.GroundAttack:PlayerCombatState.AirAttack,true);AttackFacing=world.Facing;
            // Preserve this deadline across landing and dash cancellation, so
            // switching locomotion states cannot bypass the airborne attack cadence.
            if(airAttack){airRepeatReady=combatClock+AirAttackInterval;recoverUntil=Mathf.Max(recoverUntil,airRepeatReady);}
            queuedUntil=-1;AttackCount++;
            AttackAnimation=airAttack?(world.Motor.vy<0?"AirRiseAttack":"AirFallAttack"):GroundAttackName(n);
            // The existing impact drawing is the input response. Skip preparation
            // frames without changing the authored swing, follow-through or pose.
            var clip=library.Find(AttackAnimation);
            int impact=airAttack?1:2;
            for(int i=0;i<impact;i++)AttackTime+=clip.frames[i].duration;
            AttackFrame=clip.FrameAt(AttackTime);
        }
        public bool RequestAttack()
        {
            if(world==null||world.Motor==null||world.HazardInputBlocked||world.ActionLocked||world.Progression.Parrying||world.flying||world.MapOpen||world.IsDashing||library==null)return false;
            if(Attacking){if(airAttack){queuedAirAttack=true;return true;}if(stage==2)return false;queuedUntil=combatClock+.15f+ComboPause;return true;}
            if(combatClock<recoverUntil){if(combatClock<airRepeatReady){queuedAirAttack=true;return true;}return false;}
            StartAttack(combatClock<=comboUntil&&lastStage>=0&&lastStage<2?lastStage+1:0);
            Render(world.Facing);ResolveAttackHits();return true;
        }
        void TickCombat(float dt)
        {
            combatClock+=dt;
            // A strike keeps its drawing and playback time for its entire duration. Steering,
            // jumping and landing must not restart or cancel its wind-up/impact.
            if(world.HazardInputBlocked||world.flying||world.MapOpen||world.IsDashing){CancelAttack();return;}
            if(!Attacking)
            {
                if(queuedAirAttack&&combatClock+0.00001f>=recoverUntil)StartAttack(0);
                return;
            }
            combat.Advance(dt);
            var clip=library.Find(AttackAnimation);
            // Hold the recovery drawing briefly between combo hits. The next
            // strike still starts on its impact frame, so neutral attacks stay immediate.
            float duration=clip.Duration+(!airAttack&&stage<2?ComboPause:0);
            if(AttackTime+0.00001f>=duration)
            {
                float ended=combatClock-Mathf.Max(0,AttackTime-duration);
                if(airAttack){CancelAttack(false);return;}
                lastStage=stage;comboUntil=ended+.25f;
                if(stage<2&&queuedUntil>=ended){float remainder=Mathf.Max(0,AttackTime-duration);StartAttack(stage+1);AttackTime+=remainder;}
                else{AttackAnimation=null;combat.Change(PlayerCombatState.Ready);queuedUntil=-1;if(stage==2){recoverUntil=ended+.12f;comboUntil=-1;}return;}
            }
            AttackFrame=library.Find(AttackAnimation).FrameAt(AttackTime);
        }
        void RenderCombat(PaperSpriteClip locomotion,int facing)
        {
            if(attackEffect!=null)attackEffect.enabled=false;
            if(outerArc!=null)outerArc.Hide();
            if(!Attacking)return;
            AttackFacing=facing<0?-1:1;
            var clip=library.Find(AttackAnimation);AttackFrame=clip.FrameAt(AttackTime);
            visual.sprite=clip.frames[AttackFrame].sprite;transform.localScale=new Vector3(AttackFacing,1,1)*GridMotor.BodyScale;
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
            if(attackEffect==null||airAttack||!ThrustAttack||(AttackFrame!=2&&AttackFrame!=3))return;
            bool move=AttackAnimation.StartsWith("Move");float width=AttackFrame==2?140:200;
            Vector2 anchor=move?(AttackFrame==2?new Vector2(486,420):new Vector2(530,418)):(AttackFrame==2?new Vector2(528,420):new Vector2(562,408));
            attackEffect.enabled=true;attackEffect.sprite=library.thrustEffect;
            attackEffect.transform.localPosition=new Vector3((anchor.x-384)/library.pixelsPerUnit,(560-anchor.y)/library.pixelsPerUnit,0);
            attackEffect.transform.localScale=new Vector3(ThrustEffectScale,1.6f,1)*(width/1000);
        }
        void RenderOuterArc()
        {
            bool active=airAttack?AttackFrame==1:!ThrustAttack&&(AttackFrame==2||AttackFrame==3);
            if(!active)return;
            if(outerArc==null)
            {
                var go=new GameObject("Larger paper slash");go.transform.SetParent(visual.transform,false);
                outerArc=go.AddComponent<PaperAttackArc>();outerArc.Initialize(visual);
            }
            bool move=AttackAnimation.StartsWith("Move");
            Vector2 center,radius;float start,end;
            if(airAttack){center=new Vector2(424,282);radius=new Vector2(420,106);start=-88;end=88;}
            else if(GroundArtStage==0)
            {
                center=move?new Vector2(409,470):new Vector2(447,456);
                radius=new Vector2(420,103);start=-88;end=88;
            }
            else
            {
                center=move?new Vector2(426,330):new Vector2(450,327);
                radius=new Vector2(440,156);start=-88;end=88;
            }
            outerArc.Show(center,radius,start,end,AttackFrame==2?.7f:.95f,library.pixelsPerUnit);
        }
    }
}
