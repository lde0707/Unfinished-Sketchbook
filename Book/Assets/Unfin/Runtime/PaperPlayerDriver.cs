using UnityEngine;
using UnityEngine.InputSystem;

namespace Unfin
{
    [DefaultExecutionOrder(50)]
    public sealed class PaperPlayerDriver : MonoBehaviour
    {
        public WorldPrototype world;
        public Animator animator;
        public PaperPlayerRig rig;
        string state="";
        float lockTime,attackTime,lastSpeed;
        int attackIndex;
        bool grounded;
        int direction=1;
        public string CurrentAnimation => state;
        void Update()
        {
            var keyboard=Keyboard.current;
            Tick(Mathf.Min(Time.deltaTime,.1f),keyboard!=null && keyboard.jKey.wasPressedThisFrame);
        }
        public void Tick(float dt,bool attackRequested=false)
        {
            if(world==null || world.Motor==null || animator==null || rig==null) return;
            var m=world.Motor;
            lockTime-=dt; attackTime-=dt;
            rig.Facing=world.Facing;
            bool moving=Mathf.Abs(m.vx)>12;
            // Animation preview only: combat hitboxes/damage remain a separate system.
            if(attackRequested && m.grounded && !world.IsDashing && lockTime<=.12f) {
                attackIndex=attackTime>0 ? attackIndex%3+1 : 1;
                Play("Attack"+attackIndex,.50f); attackTime=1.1f;
            }
            if(world.IsDashing) Play("Dash");
            else if(!world.flying && !m.grounded) {
                if(grounded && m.vy<0) Play("Jump",.10f);
                else if(lockTime<=0 || state.StartsWith("Attack"))
                    Play(m.hitWall!=0 && m.vy>0 ? "WallSlide" : m.vy < -50 ? "AirRise" : m.vy > 50 ? "AirFall" : "AirApex");
            }
            else if(!grounded && m.grounded) Play("Land",.16f);
            else if(lockTime<=0) {
                if(world.Facing!=direction && moving) Play("Turn",.12f);
                else if(moving && lastSpeed<=12) Play("RunStart",.12f);
                else if(!moving && lastSpeed>12) Play("RunStop",.18f);
                else Play(moving ? (Mathf.Abs(m.vx)<50 ? "Walk" : "Run") : "Idle");
            }
            direction=world.Facing; grounded=m.grounded; lastSpeed=Mathf.Abs(m.vx);
            animator.speed=state=="Run" ? Mathf.Clamp(Mathf.Abs(m.vx)/125f,.6f,1.3f) : 1;
        }
        void Play(string next,float hold=0)
        {
            if(state==next) return;
            state=next; lockTime=hold;
            animator.CrossFadeInFixedTime(next,.075f,0,0);
        }
    }
}
