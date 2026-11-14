using UnityEngine;
namespace Unfin
{
    public sealed partial class PaperSpritePlayer
    {
        string reaction;
        float reactionTime;
        int parrySerial,parrySuccessSerial;
        SpriteRenderer guard;
        public string ReactionAnimation=>reaction;
        public float ReactionTime=>reactionTime;
        public void BeginHurt()
        {
            reaction="Hurt";reactionTime=0;CancelAttack();
            world.PausePlayerOnHit(.1f);
        }
        void ResetReactions(){reaction=null;reactionTime=0;parrySerial=world.Progression.ParrySerial;parrySuccessSerial=world.Progression.ParrySuccessSerial;if(guard!=null)guard.enabled=false;}
        void TickReactions(float dt)
        {
            if(world.MapOpen)return;
            if(world.HazardInputBlocked||world.IsDashing){ResetReactions();return;}
            reactionTime+=dt;
            if(parrySerial!=world.Progression.ParrySerial){parrySerial=world.Progression.ParrySerial;reaction="Parry";reactionTime=0;CancelAttack();}
            if(parrySuccessSerial!=world.Progression.ParrySuccessSerial){parrySuccessSerial=world.Progression.ParrySuccessSerial;reaction="ParrySuccess";reactionTime=0;CancelAttack();}
            if(reaction!=null&&reactionTime>=PlayerReactionSprites.Find(reaction).Duration)reaction=null;
        }
        void RenderReaction()
        {
            if(guard!=null)guard.enabled=false;
            if(reaction==null||world.HazardInputBlocked||world.IsDashing||Attacking)return;
            var clip=PlayerReactionSprites.Find(reaction);visual.sprite=clip.frames[clip.FrameAt(reactionTime)].sprite;
            // Whole drawings have their own pixel registration; preserve the motor/world anchor.
            visual.transform.localPosition=Vector3.zero;
            if(reaction!="Parry"&&reaction!="ParrySuccess")return;
            if(guard==null){var go=new GameObject("Paper deflection trace");go.transform.SetParent(visual.transform,false);guard=go.AddComponent<SpriteRenderer>();guard.sprite=Region2Presentation.Fx(5);guard.sortingOrder=visual.sortingOrder+1;}
            // A subtle ring marks the guard, separate from the authored character frames.
            guard.enabled=world.Progression.Parrying||reaction=="ParrySuccess"&&reactionTime<.11f;
            guard.transform.localPosition=new Vector3(.32f,.72f,0);
            guard.transform.localScale=Vector3.one*(.45f/Mathf.Max(.01f,guard.sprite.bounds.size.y));
            guard.color=new Color(.78f,.73f,.6f,reaction=="ParrySuccess"?.6f:.22f);
        }
    }
}
