using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unfin.Editor
{
    public static class PlayerTempoValidation
    {
        [Serializable] class JumpResult {public int direction;public float angle,heightTiles,distanceTiles,seconds;}
        [Serializable] class Report {public bool passed;public float groundSpeed,airSpeed,gravity,dashSpeed,dashSeconds,dashDistance,runPlaybackRate;public JumpResult[] jumps;}
        static void Check(bool ok,string label){if(!ok)throw new Exception("Player tempo: "+label);}
        public static void RunAndBuild()
        {
            Run();MezzanineValidation.ValidateRoutes();PaperSpriteValidation.Run();CameraFollowValidation.Run();
            PlayerCombatValidation.Run();PlayerAttackRangeValidation.Run();
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);WebGLBuilder.Build();
        }
        public static void Run()
        {
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var w=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            typeof(WorldPrototype).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(w,null);
            var p=w.player.GetComponentInChildren<PaperSpritePlayer>();var m=w.Motor;
            Array.Clear(m.cells,0,m.cells.Length);for(int x=0;x<w.Data.width;x++)m.cells[80*w.Data.width+x]=1;
            const float dt=1f/120;
            void Step(int n,int dir=0){for(int i=0;i<n;i++){w.Simulate(dt,dir);p.Tick(dt);}}
            void Reset(){w.Respawn(100,80);Step(60);}
            Reset();Step(240,1);Check(Mathf.Abs(m.vx-88)<.01f,"ground speed20% slower");
            float frameTime=p.AnimationTime;Step(60,1);Check(Mathf.Abs(p.AnimationTime-frameTime-(88f/125*1.3f)*.5f)<.001f,"run animation30percent faster without changing movement");
            var jumps=new List<JumpResult>();
            foreach(int dir in new[]{-1,0,1})
            {
                Reset();float startX=m.x,startY=m.y;w.RequestJump();Step(1,dir);
                float angle=Mathf.Atan2(-m.vy,Mathf.Abs(m.vx))*Mathf.Rad2Deg;
                Check(!m.grounded&&m.vy<0,"launch on first simulation tick");
                if(dir!=0)Check(Mathf.Abs((Mathf.Atan2(425,155)*Mathf.Rad2Deg-angle)-10)<.02f,"moving launch10degrees lower");
                else Check(Mathf.Abs(m.vx)<.001f,"stationary jump stays stationary");
                float minY=m.y;int steps=1;
                while(!m.grounded&&steps<240){Step(1,dir);minY=Mathf.Min(minY,m.y);steps++;}
                float height=(startY-minY)/16,seconds=steps*dt,distance=Mathf.Abs(m.x-startX)/16;
                Check(m.grounded&&height>3.55f&&height<3.75f,"jump height reduced15%");
                Check(seconds>.72f&&seconds<.82f,"slower jump arc without excessive hang");
                if(dir!=0)Check(distance>4.7f&&distance<6,"directional jump carries forward");
                jumps.Add(new JumpResult{direction=dir,angle=angle,heightTiles=height,distanceTiles=distance,seconds=seconds});
            }
            Reset();w.RequestDash(1);float dashStart=m.x;Step(12,1);
            Check(w.IsDashing&&Mathf.Abs(m.vx-PlayerMotion.DashSpeed)<.01f,"configured dash speed");Step(12,1);
            float dashDistance=m.x-dashStart;Check(!w.IsDashing&&Mathf.Abs(dashDistance-48.96f)<.05f,"short dash travels3.06tiles in200ms");
            Reset();w.RequestJump();Step(1,1);int serial=w.JumpSerial;
            for(int i=0;i<180&&!m.grounded;i++)
            {
                if(m.vy>0&&80*16-m.y-GridMotor.Height<15)w.RequestJump();
                Step(1,1);if(w.JumpSerial>serial)break;
            }
            Check(w.JumpSerial==serial+1&&m.vy<0,"landing buffer immediately launches next jump");
            Check(Mathf.Abs(Mathf.Atan2(-m.vy,Mathf.Abs(m.vx))*Mathf.Rad2Deg-PlayerMotion.JumpAngle)<.02f,"buffered jump keeps lower launch angle");
            var report=new Report{passed=true,groundSpeed=PlayerMotion.GroundSpeed,airSpeed=PlayerMotion.AirSpeed,gravity=PlayerMotion.Gravity,dashSpeed=PlayerMotion.DashSpeed,dashSeconds=.2f,dashDistance=dashDistance,runPlaybackRate=PlayerMotion.RunPlaybackRate,jumps=jumps.ToArray()};
            Directory.CreateDirectory("Validation");File.WriteAllText("Validation/player-tempo.json",JsonUtility.ToJson(report,true));
            Debug.Log("UNFIN_PLAYER_TEMPO_OK");
        }
    }
}
