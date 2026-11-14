using System;
using System.Collections.Generic;
using UnityEngine;
namespace Unfin
{
    [Serializable] public sealed class StilldrawnSave
    {
        public int schema=1,width,height;
        // One-based zone ID keeps older saves (zero) compatible with checkpoint fallback.
        public int lastZonePlusOne;
        public int proximityStoryGoneMask;
        public bool inPrologue;
        public string mapHash,savedAt,discovered;
        public float checkpointX,checkpointY,playSeconds;
        public string[] abilities,collected,stations,frontiers;
        public int[] defeated,cellIndices,cellValues;
        public int hearts,shards,healBonus,openedGates;
        public int inkDust,bindingLevel,bottleLevel,gatherLevel;
        public bool healing,shield;
        public MapNote[] notes;
    }
    public static class WorldSession
    {
        public const string SaveKey="stilldrawn.save.v1";
        public static float PlaySeconds;
        public static StilldrawnSave Read(WorldPrototype world)
        {
            try{
                if(!PlayerPrefs.HasKey(SaveKey))return null;
                var s=JsonUtility.FromJson<StilldrawnSave>(PlayerPrefs.GetString(SaveKey));
                if(s==null||s.schema!=1)return null;
                var savedMap=s.inPrologue?JsonUtility.FromJson<WorldData>(Resources.Load<TextAsset>("Prologue/map").text):world.Data;
                if(s.mapHash!=savedMap.gridSha256||s.width!=savedMap.width||s.height!=savedMap.height)return null;
                if(!float.IsFinite(s.checkpointX)||!float.IsFinite(s.checkpointY)||s.checkpointX<0||s.checkpointX>=s.width*16||s.checkpointY<0||s.checkpointY>=s.height*16)return null;
                if(s.abilities==null||s.collected==null||s.stations==null||s.frontiers==null||s.defeated==null||s.cellIndices==null||s.cellValues==null||s.cellIndices.Length!=s.cellValues.Length)return null;
                for(int i=0;i<s.cellIndices.Length;i++)if(s.cellIndices[i]<0||s.cellIndices[i]>=s.width*s.height||s.cellValues[i]<0||s.cellValues[i]>18)return null;
                if(Convert.FromBase64String(s.discovered).Length!=s.width*s.height)return null;
                return s;
            }catch{return null;}
        }
        public static void Save(WorldPrototype world)
        {
            if(world==null||world.Motor==null)return;
            var s=world.CaptureSession();s.playSeconds=PlaySeconds;s.savedAt=DateTime.Now.ToString("yyyy.MM.dd  HH:mm");
            PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(s));PlayerPrefs.Save();
        }
        public static string TimeLabel(float seconds)=>((int)seconds/3600).ToString("D2")+":"+((int)seconds/60%60).ToString("D2");
    }
    public sealed partial class WorldProgression
    {
        public void CaptureSession(StilldrawnSave s)
        {
            CaptureEconomy(s);s.notes=notes.ToArray();s.abilities=new List<string>(abilities).ToArray();s.collected=new List<string>(collected).ToArray();s.stations=new List<string>(visitedStations).ToArray();
            s.frontiers=new List<string>(SeenFrontiers).ToArray();s.defeated=new List<int>(DefeatedEncounters).ToArray();
            var map=new byte[Discovered.Length];for(int i=0;i<map.Length;i++)map[i]=Discovered[i]?(byte)1:(byte)0;s.discovered=Convert.ToBase64String(map);
            s.hearts=HeartFragments;s.shards=Shards;s.healBonus=HealBonus;s.openedGates=OpenedGates;s.healing=HealingUnlocked;s.shield=ShieldUnlocked;
        }
        public void RestoreSession(StilldrawnSave s)
        {
            void Set<T>(HashSet<T> set,T[] values){set.Clear();foreach(var value in values)set.Add(value);}
            Set(abilities,s.abilities);Set(collected,s.collected);Set(visitedStations,s.stations);Set(SeenFrontiers,s.frontiers);Set(DefeatedEncounters,s.defeated);
            // A save can be written during the death drawing, before the earned ability is presented.
            if(DefeatedEncounters.Contains(23))abilities.Add("cut");
            if(DefeatedEncounters.Contains(47))abilities.Add("dash");
            if(DefeatedEncounters.Contains(69))abilities.Add("parry");
            if(DefeatedEncounters.Contains(91))abilities.Add("climb");
            var map=Convert.FromBase64String(s.discovered);for(int i=0;i<Discovered.Length;i++)Discovered[i]=map[i]!=0;revealedAreas.Clear();CurrentMapArea=-1;mapDirty=true;
            RefreshMapConnections();
            RestoreEconomy(s);LoadNotes(s.notes);HeartFragments=s.hearts;Shards=s.shards;HealBonus=s.healBonus;OpenedGates=s.openedGates;HealingUnlocked=s.healing;ShieldUnlocked=s.shield;Restore();
        }
    }
    public sealed partial class WorldPrototype
    {
        public StilldrawnSave CaptureSession()
        {
            var s=new StilldrawnSave{width=Data.width,height=Data.height,mapHash=Data.gridSha256,checkpointX=safe.x,checkpointY=safe.y,
                lastZonePlusOne=RegionalTilePalette.ZoneAt(Data,Mathf.FloorToInt((Motor.x+6)/16),Mathf.FloorToInt((Motor.y+11)/16))+1};
            s.inPrologue=Data.isPrologue;
            if(s.inPrologue){s.checkpointX=Motor.x;s.checkpointY=Motor.y;}
            s.proximityStoryGoneMask=GetComponent<ProximityStory>()?.GoneMask??0;
            Progression.CaptureSession(s);var original=Data.Decode();var indices=new List<int>();var values=new List<int>();
            var transient=new HashSet<int>();foreach(var boss in FindObjectsByType<PaperRegion2Encounter>(FindObjectsSortMode.None))foreach(int index in boss.TemporaryPlatformCells)transient.Add(index);
            for(int i=0;i<original.Length;i++){
                int value=transient.Contains(i)?original[i]:Motor.cells[i];if(changes.TryGetValue(i,out var temporary))value=temporary.original;if(arenaHotTiles.ContainsKey(i))value=4;
                if(value==original[i])continue;indices.Add(i);values.Add(value);
            }
            s.cellIndices=indices.ToArray();s.cellValues=values.ToArray();return s;
        }
        public void RestoreSession(StilldrawnSave s)
        {
            Progression.RestoreSession(s);
            GetComponent<ProximityStory>()?.Restore(s.proximityStoryGoneMask);
            for(int i=0;i<s.cellIndices.Length;i++){
                int index=s.cellIndices[i],x=index%Data.width,y=index/Data.width,old=Motor.cells[index],value=s.cellValues[i];
                Map(x,old).SetTile(WorldData.Cell(x,y),null);Motor.cells[index]=(byte)value;
                if(value!=0){PrototypeTile art=null;foreach(var t in terrainTiles)if(t.region==(x>=Data.regionX?2:1)&&t.htmlType==value){art=t;break;}Map(x,value).SetTile(WorldData.Cell(x,y),art);}
                RefreshTerrain(x,y);
            }
            Respawn(Mathf.RoundToInt((s.checkpointX-2)/16),Mathf.RoundToInt((s.checkpointY+GridMotor.Height)/16));
            safe=new Vector2(s.checkpointX,s.checkpointY);Motor.x=safe.x;Motor.y=safe.y;SyncView();Progression.RefreshMapConnections();WorldSession.PlaySeconds=s.playSeconds;
        }
    }
}
