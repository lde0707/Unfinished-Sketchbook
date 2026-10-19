using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unfin.Editor
{
    // Test the actual collision motor, with the same full-jump launch/gravity/air steering
    // as WorldPrototype. No wall jump, dash, flight or opened gates are used.
    public static class MezzanineValidation
    {
        [Serializable] public class Route { public string room,label; public int fromX,fromEnd,fromY,toX,toEnd,toY; }
        [Serializable] public class Floor { public int r,n,x,y,w,h; }
        [Serializable] public class Layout { public string gridSha256; public Route[] routeChecks; public Floor[] solidFloors; }
        [Serializable] public class Result { public string label; public bool passed; public float startX,targetX,delay,landingX,seconds; }
        [Serializable] public class Report { public bool passed; public string gridSha256; public int floors; public Result[] routes; }
        const string Spec="Assets/Unfin/Data/room-mezzanines.json";

        public static void BuildAndValidate()
        {
            ValidateRoutes();
            WorldPrototypeBuilder.ReuseLegacyArt=true;
            try { WorldPrototypeBuilder.Build(); }
            finally { WorldPrototypeBuilder.ReuseLegacyArt=false; }
            var world=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            RegionalPlatformImporter.Validate(world);
            RenderRooms();
            WorldMapPresentation.Render();
            Debug.Log("UNFIN_MEZZANINE_BUILD_OK");
        }

        public static void ValidateRoutes()
        {
            var data=JsonUtility.FromJson<WorldData>(File.ReadAllText("Assets/Unfin/Data/world12.json"));
            var layout=JsonUtility.FromJson<Layout>(File.ReadAllText(Spec));
            if(data.gridSha256!=layout.gridSha256)throw new Exception("Mezzanine spec/grid mismatch");
            var cells=data.Decode();var results=new List<Result>();
            foreach(var f in layout.solidFloors)
                for(int y=f.y;y<f.y+f.h;y++)for(int x=f.x;x<f.x+f.w;x++)
                    if(cells[y*data.width+x]!=1)throw new Exception("Missing solid mezzanine cell");
            foreach(var route in layout.routeChecks)results.Add(FindJump(data,cells,route));
            var report=new Report{passed=results.All(r=>r.passed),gridSha256=data.gridSha256,floors=layout.solidFloors.Length,routes=results.ToArray()};
            Directory.CreateDirectory("Validation");File.WriteAllText("Validation/room-mezzanines-routes.json",JsonUtility.ToJson(report,true));
            if(!report.passed)throw new Exception("Unreachable mezzanine jumps: "+string.Join(", ",results.Where(r=>!r.passed).Select(r=>r.label)));
            Debug.Log($"UNFIN_MEZZANINE_ROUTES_OK {results.Count} jumps, {report.floors} solid floors");
        }

        static bool ClearBody(GridMotor m)
        {
            for(int y=Mathf.FloorToInt(m.y/16);y<=Mathf.FloorToInt((m.Bottom-.01f)/16);y++)
            for(int x=Mathf.FloorToInt(m.Left/16);x<=Mathf.FloorToInt((m.Right-.01f)/16);x++)
                if(GridMotor.Wall(m.Get(x,y))||m.Get(x,y)==9)return false;
            return true;
        }
        static Result FindJump(WorldData data,byte[] cells,Route r)
        {
            var result=new Result{label=r.label};
            const float dt=1f/120;
            foreach(float delay in new[]{0f,.05f,.1f,.15f})
            for(float sx=r.fromX+.5f;sx<r.fromEnd;sx+=.5f)
            for(float target=r.toX+.5f;target<r.toEnd;target+=.5f)
            {
                var m=new GridMotor(data,cells){x=sx*16-6,y=r.fromY*16-GridMotor.CollisionHeight};
                if(!ClearBody(m)||!(m.Row(r.fromY,false)||m.Row(r.fromY,true)))continue;
                int floor=m.Get(Mathf.FloorToInt(sx),r.fromY);if(floor==3||floor==9)continue;
                int dir=target>sx?1:-1;
                m.vx=delay==0?dir*PlayerMotion.JumpHorizontal:0;m.vy=-PlayerMotion.JumpSpeed*(floor==16?.84f:1);
                for(int tick=0;tick<180;tick++)
                {
                    if(tick>0){float dx=target*16-(m.x+6);int input=tick*dt<delay?0:Mathf.Abs(dx)<3?0:dx>0?1:-1;
                        m.vx=Mathf.Lerp(m.vx,input*PlayerMotion.AirSpeed,dt*10);m.vy=Mathf.Min(PlayerMotion.FallSpeed,m.vy+PlayerMotion.Gravity*dt);}
                    m.Move(dt);
                    if(!ClearBody(m))break;
                    if(m.grounded){float cx=(m.x+6)/16,fy=m.Bottom/16;
                        if(Mathf.Abs(fy-r.toY)<.01f&&cx>=r.toX+.375f&&cx<=r.toEnd-.375f){result.passed=true;result.startX=sx;result.targetX=target;result.delay=delay;result.landingX=cx;result.seconds=(tick+1)*dt;return result;}
                        break;
                    }
                    if(m.y>(Mathf.Max(r.fromY,r.toY)+6)*16)break;
                }
            }
            return result;
        }

        public static void RenderRooms()
        {
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var w=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            var data=JsonUtility.FromJson<WorldData>(w.mapJson.text);
            var layout=JsonUtility.FromJson<Layout>(File.ReadAllText(Spec));
            foreach(var group in layout.solidFloors.GroupBy(f=>$"{f.r}-{f.n}"))
            {
                var f=group.First();var room=data.rooms.First(r=>r.r==f.r&&r.n==f.n);
                w.worldCamera.aspect=1920f/1080;
                w.worldCamera.orthographicSize=Mathf.Max((room.h+4)*.5f,(room.w+4)*.5f/w.worldCamera.aspect);
                w.worldCamera.transform.position=new Vector3(room.x+room.w*.5f,-room.y-room.h*.5f,-10);
                PaperSpriteValidation.Capture(w.worldCamera,$"Validation/mezzanine-room-{group.Key}.png");
                w.worldCamera.orthographicSize=data.orthographicSize;
                w.worldCamera.transform.position=new Vector3(f.x+2,-f.y+2,-10);
                PaperSpriteValidation.Capture(w.worldCamera,$"Validation/mezzanine-play-{group.Key}.png");
            }
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
        }
    }
}
