using UnityEngine;
namespace Unfin
{
    public sealed partial class WorldProgression
    {
        static bool ManualPickup(string type)=>type=="wj"||type=="cut"||type=="dash"||type=="parry"||type=="climb"||type=="heart"||type=="healup"||type=="shard"||type=="diary"||type.StartsWith("item_")||type.StartsWith("sw_");
        public WorldMarker NearbyInteraction()
        {
            if(world.MapOpen||world.flying||world.HazardInputBlocked||world.IsDashing)return null;
            var player=(Vector2)world.Motor.WorldBounds.center;WorldMarker nearest=null;float best=float.MaxValue;
            foreach(var p in world.Data.pickups){
                if(p.type=="save"&&!CheckpointPlan.IsShop(p))continue;
                if(p.type!="save"&&p.type!="station"&&Collected(p))continue;
                if(p.type!="save"&&p.type!="station"&&p.type!="sketch"&&p.type!="npc"&&p.type!="lever"&&!ManualPickup(p.type))continue;
                var at=SocialPosition(p);float distance=Vector2.Distance(player,at);
                if(distance>(p.type=="save"?2.2f:1.8f)||distance>=best||!InteractionProximity.Visible(world,player,at))continue;
                nearest=p;best=distance;
            }
            return nearest;
        }
        // One key-down selects one nearby object; held movement keys never activate it.
        public bool TryInteract()
        {
            if(world.ActionLocked||PortalTravelling||SocialTarget!=null)return false;
            var p=NearbyInteraction();if(p==null)return false;
            if(!AbilityPickupUnlocked(p.type)){Collect(p);return true;}
            if((p.type=="sketch"||p.type=="npc")&&!SocialEligible(p)){Say(p.optionsJson.Contains("sun")?"벽타기 능력이 필요합니다":"연필 깎기 능력이 필요합니다",2);return true;}
            if(NeedsInteractionChoice(p.type)){QueueInteraction(p);return true;}
            return ExecuteInteraction(p);
        }
        bool ExecuteInteraction(WorldMarker p)
        {
            if(p.type=="save")return CheckpointPlan.IsShop(p)&&OpenShop();
            if(p.type=="sketch"||p.type=="npc"){
                if(!SocialEligible(p)){Say(p.optionsJson.Contains("sun")?"벽타기 능력이 필요합니다":"연필 깎기 능력이 필요합니다",2);return true;}
                return BeginSocial(p);
            }
            if(p.type=="station"){
                if(stationCooldown>0)return true;
                string sid=JsonUtility.FromJson<Station>(p.optionsJson).sid;
                visitedStations.Add(sid);Travel(sid);stationCooldown=1;return true;
            }
            if(p.type=="lever"){
                for(int y=0;y<world.Data.height;y++)for(int x=0;x<world.Data.regionX;x++)if(world.Motor.Get(x,y)==7)world.ClearGateAt(x,y);
                collected.Add(Key(p));GameAudio.Emit(SoundCue.Gate,SocialPosition(p),.45f);Say("뿌리 지름길의 잠금이 풀렸습니다");return true;
            }
            Collect(p);return true;
        }
        void DrawInteractionPrompt()
        {
            var p=NearbyInteraction();if(p==null||p.type=="save"||p.type=="npc"||p.type=="sketch")return;
            var screen=HudScreenPoint(SocialPosition(p)+Vector2.up*.75f);
            float x=Mathf.Clamp(screen.x,120,UiWidth-170),y=Mathf.Clamp(screen.y,240,UiHeight-170);
            string action=p.type=="station"?"이동":p.type=="lever"?"작동":p.type=="diary"?"읽기":"획득";
            if(!AbilityPickupUnlocked(p.type))action="봉인";
            HudPanel(new Rect(x-60,y-34,160,48),.9f);HudKey(x-43,y-25,"E",31);HudText(new Rect(x,y-22,95,28),action,hudInk,17);
        }
    }
}
