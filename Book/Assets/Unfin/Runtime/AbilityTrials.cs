using UnityEngine;
namespace Unfin
{
    // Stable authored enemy indices are also used by the existing save file.
    public static class AbilityTrials
    {
        public const int PaperGuardian=1;
        public static int Encounter(string pickup)
        {
            switch(pickup){case "wj":return PaperGuardian;case "cut":return 23;case "dash":return 47;case "parry":return 69;case "climb":return 91;default:return -1;}
        }
    }
    public sealed partial class WorldProgression
    {
        public bool PaperGuardianCleared=>DefeatedEncounters.Contains(AbilityTrials.PaperGuardian)||HasAcquired("walljump");
        public bool AbilityPickupUnlocked(string kind)
        {
            int encounter=AbilityTrials.Encounter(kind);
            return encounter<0||(kind=="wj"?PaperGuardianCleared:DefeatedEncounters.Contains(encounter));
        }
    }
    public sealed partial class WorldPrototype
    {
        [System.Serializable] class AbilityTrialSnapshot {public bool unlocked,acquired,collected;public int health=-1;public string state;public float guardianX;}
        public void ReportAbilityTrial()
        {
            var hand=System.Array.Find(Data.pickups,p=>p.type=="wj");
            var status=new AbilityTrialSnapshot{unlocked=Progression.AbilityPickupUnlocked("wj"),acquired=Progression.HasAcquired("walljump"),collected=Progression.Collected(hand)};
            foreach(var actor in FindObjectsByType<PaperRegion1Elite>(FindObjectsSortMode.None))if(actor.IsPaperGuardian){status.health=actor.Health;status.state=actor.StateName;status.guardianX=actor.transform.position.x;break;}
            Debug.Log("ABILITY_TRIAL "+JsonUtility.ToJson(status));
        }
    }
}
