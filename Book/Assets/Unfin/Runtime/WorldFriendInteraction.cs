using UnityEngine;
namespace Unfin
{
    public sealed partial class WorldProgression
    {
        public WorldMarker SocialTarget {get;private set;}
        public float SocialProgress {get;private set;}
        public bool BeginSocial(WorldMarker target){if(target==null||!SocialEligible(target)||NearbySocial()!=target)return false;SocialTarget=target;SocialProgress=0;return true;}
        public int FoundFriends {get {int count=0;foreach(var p in world.Data.pickups)if(p.type=="sketch"&&Collected(p))count++;return count;}}
        public bool SocialEligible(WorldMarker p)=>p.type=="npc"||p.type=="sketch"&&Has(p.optionsJson.Contains("sun")?"climb":"cut");
        public Vector2 SocialPosition(WorldMarker p){var e=WorldPropPlacement.Find(p.type,new Vector2(p.x,p.y));return e==null?world.Data.Center(p):(Vector2)e.position;}
        public WorldMarker NearbySocial(){
            if(world.HazardInputBlocked||world.IsDashing||world.flying||!world.Motor.grounded)return null;
            var player=new Vector2((world.Motor.x+6)/16,-(world.Motor.y+11)/16);
            foreach(var p in world.Data.pickups)if((p.type=="sketch"||p.type=="npc")&&!Collected(p)&&Vector2.Distance(SocialPosition(p),player)<1.8f){
                var target=SocialPosition(p);int steps=Mathf.CeilToInt(Vector2.Distance(player,target)*6);bool blocked=false;
                for(int i=1;i<steps;i++){var a=Vector2.Lerp(player,target,(float)i/steps);if(GridMotor.Wall(world.Motor.Get(Mathf.FloorToInt(a.x),Mathf.FloorToInt(-a.y)))){blocked=true;break;}}
                if(!blocked)return p;
            }
            return null;
        }
        public void TickSocial(float dt,int ix,int iy){
            var target=NearbySocial();var sprite=world.player.GetComponentInChildren<PaperSpritePlayer>();
            if(SocialTarget==null)return;
            if(target!=SocialTarget||!SocialEligible(target)||ix!=0||(sprite!=null&&sprite.Attacking)) {SocialTarget=null;SocialProgress=0;return;}
            SocialProgress=Mathf.Min(1,SocialProgress+dt/(target.type=="npc"?.65f:1.2f));
            if(SocialProgress>=1){
                if(target.type=="npc"&&target.tx>=world.Data.regionX)Region2Story.Talk(world);else collected.Add(Key(target));GameAudio.Emit(SoundCue.Restore,SocialPosition(target),.45f);
                SocialTarget=null;SocialProgress=0;
                if(target.type=="sketch")ShowPickupNotice(target);
            }
        }
        void DrawSocialPrompt(){
            var target=NearbySocial();if(target==null)return;
            var pos=SocialPosition(target);var e=WorldPropPlacement.Find(target.type,new Vector2(target.x,target.y));pos.y+=(e==null?1:e.height)*.6f;
            var screen=HudScreenPoint(pos);float x=Mathf.Clamp(screen.x,100,UiWidth-160),y=Mathf.Clamp(screen.y,250,UiHeight-160);
            if(!SocialEligible(target)){GUI.Label(new Rect(x-50,y-32,220,28),target.optionsJson.Contains("sun")?"벽타기 필요":"연필 깎기 필요",small);return;}
            HudPanel(new Rect(x-53,y-43,148,47),.9f);HudKey(x-41,y-33,"E",31);HudText(new Rect(x+1,y-30,90,28),target.type=="npc"?"대화":"기억하기",hudInk,17);
            if(SocialTarget==target&&SocialProgress>0){Fill(new Rect(x-30,y,60,4),new Color(.12f,.12f,.12f,.8f));Fill(new Rect(x-30,y,60*SocialProgress,4),new Color(.9f,.82f,.61f,.8f));}
        }
    }
}
