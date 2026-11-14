using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Unfin.Editor
{
    public static class RoomConnectionValidation
    {
        static void Check(bool ok,string message){if(!ok)throw new Exception("Room connections: "+message);}
        public static void RunAndBuild()
        {
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var w=UnityEngine.Object.FindFirstObjectByType<WorldPrototype>();
            typeof(WorldPrototype).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(w,null);
            RoomMapValidation.Run(w);
            var data=new WorldData{width=24,height=10,rooms=new[]{
                new WorldArea{x=1,y=1,w=4,h=4},new WorldArea{x=11,y=1,w=4,h=4},
                new WorldArea{x=19,y=1,w=4,h=4},new WorldArea{x=1,y=7,w=4,h=2}},
                passages=new[]{new WorldArea{x=5,y=2,w=6,h=2},new WorldArea{x=15,y=2,w=4,h=2}},shafts=Array.Empty<WorldArea>()};
            var cells=new byte[data.width*data.height];Array.Fill(cells,(byte)1);
            foreach(var a in data.rooms.Concat(data.passages))for(int y=a.y;y<a.y+a.h;y++)for(int x=a.x;x<a.x+a.w;x++)cells[y*data.width+x]=0;
            var layout=new RoomMapLayout(data,cells);var known=new bool[cells.Length];var hints=new bool[cells.Length];
            layout.Reveal(layout.At(2,2),known);var visited=(bool[])known.Clone();layout.RevealConnections(known,cells,hints);
            for(int x=5;x<=12;x++)Check(hints[2*data.width+x],"corridor and two-cell entrance "+x);
            Check(!hints[2*data.width+13]&&!hints[2*data.width+15]&&!hints[2*data.width+19],"no next room interior or recursive reveal");
            Check(!hints[7*data.width+2],"stone wall does not imply doorway");
            Check(known.SequenceEqual(visited),"hints do not become visits or reveal item markers");
            cells[2*data.width+8]=cells[3*data.width+8]=2;layout.RevealConnections(known,cells,hints);
            Check(hints[2*data.width+8]&&!hints[2*data.width+9],"closed gate shown, far side hidden");
            cells[2*data.width+8]=cells[3*data.width+8]=0;layout.RevealConnections(known,cells,hints);
            Check(hints[2*data.width+12],"opening gate restores connecting route");
            layout.Reveal(layout.At(12,2),known);layout.RevealConnections(known,cells,hints);
            Check(known[2*data.width+13]&&hints[2*data.width+19]&&!hints[2*data.width+21],"entering next room expands to its exits only");
            var p=new WorldProgression(w);p.Reveal(236,130);
            int count=p.ConnectionHints.Count(v=>v);Check(count>10&&count<500,"actual starting room has bounded exit hints");
            var s=new StilldrawnSave();p.CaptureSession(s);var fresh=new WorldProgression(w);fresh.RestoreSession(s);
            Check(p.Discovered.SequenceEqual(fresh.Discovered)&&p.ConnectionHints.SequenceEqual(fresh.ConnectionHints),"existing save derives identical routes without schema migration");
            Directory.CreateDirectory("Validation");File.WriteAllText("Validation/room-connections.json","{\"passed\":true,\"startRoomHintCells\":"+count+",\"checks\":[\"whole connecting corridor\",\"two-cell next room entrance\",\"no recursive room reveal\",\"wall and locked gate boundaries\",\"gate opening refresh\",\"hints separate from visits\",\"save restore\"]}");
            Debug.Log("ROOM_CONNECTIONS_OK cells="+count);
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);WebGLBuilder.Build();
        }
    }
}
