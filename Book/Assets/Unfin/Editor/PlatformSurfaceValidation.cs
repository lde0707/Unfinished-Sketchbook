using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor.SceneManagement;
namespace Unfin.Editor
{
    public static class PlatformSurfaceValidation
    {
        [Serializable] public class Cell { public int x,y,expected,visual; public string map; }
        [Serializable] public class MapInfo { public string name; public Vector3 position,scale; }
        [Serializable] public class Report { public bool passed; public int platformCells; public Cell[] mismatches,fallThrough; public MapInfo[] maps; }
        public static void RepairAndValidate()
        {
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var w=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            RegionalPlatformImporter.Apply(w);EditorSceneManager.SaveScene(w.gameObject.scene);
            Audit();
            var report=JsonUtility.FromJson<Report>(File.ReadAllText("Validation/platform-surface-audit.json"));
            if(!report.passed)throw new Exception("Platform drawing/collision mismatch remains");
            w=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            typeof(WorldPrototype).GetMethod("Awake",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(w,null);
            void Step(int ticks,int ix=0,int iy=0){for(int i=0;i<ticks;i++)w.Simulate(1f/120,ix,iy);}
            foreach(var cell in new[]{new Vector2Int(142,105),new Vector2Int(143,105),new Vector2Int(144,105),new Vector2Int(253,113),new Vector2Int(254,113)}){
                w.Respawn(cell.x,cell.y);w.Motor.dropTime=0;Step(120);
                if(!w.Motor.grounded||Mathf.Abs(w.Motor.Bottom-cell.y*16)>.02f)throw new Exception("Restored platform cannot hold player: "+cell);
            }
            w.Respawn(140,105);w.Motor.dropTime=0;Step(30);
            while(w.Motor.Right<145*16-4){Step(1,1);if(!w.Motor.grounded)throw new Exception("Fell through photographed ledge while walking right");}
            while(w.Motor.Left>140*16+6){Step(1,-1);if(!w.Motor.grounded)throw new Exception("Fell through photographed ledge while walking left");}
            w.Respawn(143,108);w.Motor.dropTime=0;Step(30);w.RequestJump();Step(1);
            for(int i=0;i<200&&!w.Motor.grounded;i++)Step(1);
            if(!w.Motor.grounded||Mathf.Abs(w.Motor.Bottom-105*16)>.02f)throw new Exception("Cannot jump through and land on restored ledge");
            w.RequestJump();Step(1,0,1);Step(12,0,1);
            if(w.Motor.grounded||w.Motor.Bottom<=105*16)throw new Exception("Intentional down+jump drop-through no longer works");
            Debug.Log("UNFIN_PLATFORM_CONTACT_OK 677 cells / photographed ledge walk both ways / jump from below / intentional drop");
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
        }
        public static void Audit()
        {
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var w=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            var d=JsonUtility.FromJson<WorldData>(w.mapJson.text);var cells=d.Decode();
            var mismatches=new List<Cell>();var failures=new List<Cell>();var maps=new List<MapInfo>();int count=0;
            for(int r=0;r<2;r++){
                var map=w.maps[r*3+1];maps.Add(new MapInfo{name=map.name,position=map.transform.position,scale=map.transform.lossyScale});
                for(int y=0;y<d.height;y++)for(int x=r==0?0:d.regionX;x<(r==0?d.regionX:d.width);x++){
                    int expected=cells[y*d.width+x];var tile=map.GetTile<PrototypeTile>(WorldData.Cell(x,y));int visual=tile==null?0:tile.htmlType;
                    if(GridMotor.Platform(expected)||GridMotor.Platform(visual)){
                        if(expected!=visual)mismatches.Add(new Cell{x=x,y=y,expected=expected,visual=visual,map=map.name});
                    }
                    if(!GridMotor.Platform(expected))continue;count++;
                    var motor=new GridMotor(d,cells){x=x*16+2,y=y*16-GridMotor.CollisionHeight-.25f,vy=1,dropTime=0};
                    for(int i=0;i<120;i++){motor.vy=Mathf.Min(PlayerMotion.FallSpeed,motor.vy+PlayerMotion.Gravity/120);motor.Move(1f/120);}
                    if(!motor.grounded||Mathf.Abs(motor.Bottom-y*16)>.02f)failures.Add(new Cell{x=x,y=y,expected=expected,visual=visual,map=map.name});
                }
            }
            Directory.CreateDirectory("Validation");
            File.WriteAllText("Validation/platform-surface-audit.json",JsonUtility.ToJson(new Report{passed=mismatches.Count==0&&failures.Count==0,platformCells=count,mismatches=mismatches.ToArray(),fallThrough=failures.ToArray(),maps=maps.ToArray()},true));
            Debug.Log($"UNFIN_PLATFORM_AUDIT {count} cells / {mismatches.Count} mismatches / {failures.Count} fall-through");
        }
    }
}
