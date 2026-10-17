using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unfin.Editor
{
    public static class PaperSpriteValidation
    {
        public static void BuildWebAndValidate(){Run();WebGLBuilder.Build();}
        static void Check(bool pass,string message){if(!pass)throw new Exception("Sprite integration: "+message);}
        public static void Run()
        {
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var w=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            typeof(WorldPrototype).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(w,null);
            var p=w.player.GetComponentInChildren<PaperSpritePlayer>();var m=w.Motor;
            Check(p!=null,"new sprite player attached");
            var original=(byte[])m.cells.Clone();Array.Clear(m.cells,0,m.cells.Length);
            for(int x=0;x<w.Data.width;x++)m.cells[40*w.Data.width+x]=1;
            void Step(int count,int dir=0){for(int i=0;i<count;i++){w.Simulate(.005f,dir);p.Tick(.005f);}}
            void Reset(){w.Respawn(100,40);Step(20);}
            var states=new HashSet<string>();
            void Expect(string state){Check(p.CurrentAnimation==state,$"expected {state}, got {p.CurrentAnimation}");states.Add(state);}
            Reset();Expect("Idle");Step(4,1);Expect("RunStart");Step(80,1);Expect("Run");
            Step(35);Expect("RunStop");Step(50);Expect("Idle");
            for(int dir=-1;dir<=1;dir++)
            {
                Reset();w.RequestJump();Step(1,dir);Expect(dir==0?"Jump":"MovingJump");Check(m.vy<0,"instant launch");
                Step(25,dir);float y=m.y;Check(w.RequestDash(dir),"air dash accepted");p.Tick(0);Expect("Dash");
                Step(36,-dir);Check(!w.IsDashing,"180ms dash ends");Check(Mathf.Abs(m.y-y)<.02f,"dash height held");
                Check(w.ForceFall&&m.vy>=0,"jump dash cancels remaining ascent");Expect("Fall");
                Step(45);Check(!w.RequestDash(),"one air dash even after cooldown");
                float before=m.y;Step(1);Check(m.y>=before,"never rises after dash");
                int oldJump=w.JumpSerial;
                while(m.y+GridMotor.Height<40*16-12)Step(1);
                w.RequestJump();for(int i=0;i<50&&w.JumpSerial==oldJump;i++)Step(1);
                Check(w.JumpSerial==oldJump+1&&m.vy<0,"buffered jump on contact");Check(!w.AirDashUsed,"landing restores charge");
            }
            Reset();w.RequestJump();Step(1);Step(150);Expect("JumpLand");Step(50);Expect("Idle");
            Reset();w.RequestJump();Step(1,1);for(int i=0;i<300&&!m.grounded;i++)Step(1,1);Expect("MovingLand");Step(20,1);Expect("Run");
            Reset();m.y-=120;m.grounded=false;m.vy=150;Step(1);Expect("Fall");
            float fallingSpeed=m.vy;Check(w.RequestDash(1),"fall dash accepted");Step(36,1);Expect("Fall");Check(Mathf.Abs(m.vy-fallingSpeed)<.1f,"fall speed restored");
            Step(150);Expect("Idle");
            Reset();Check(w.RequestDash(1),"ground dash");p.Tick(0);float dashStartX=m.x;Step(36,-1);Check(Mathf.Abs(m.x-dashStartX-72)<.1f,"dash direction locked and 3.2x speed");Check(m.grounded,"ground dash retains floor contact");
            Reset();w.RequestDash(1);Step(10,1);w.RequestJump();Step(1,1);Check(!w.IsDashing&&m.vy<0,"ground dash jump cancel");
            Reset();for(int col=0;col<w.Data.width;col++)m.cells[40*w.Data.width+col]=16;
            m.y-=80;m.grounded=false;Step(50);w.RequestJump();int waxJump=w.JumpSerial;
            for(int i=0;i<40&&w.JumpSerial==waxJump;i++)Step(1);
            Check(w.JumpSerial==waxJump+1&&Mathf.Abs(w.JumpLaunchSpeed-357)<.01f,$"buffered wax jump retains reduced height: serial={waxJump}->{w.JumpSerial}, speed={w.JumpLaunchSpeed}, y={m.y}, vy={m.vy}");
            Array.Copy(original,m.cells,original.Length);w.Respawn(w.Data.startX,w.Data.startFloor);Step(20);
            Capture(w.worldCamera,"Validation/player-sprites-in-game.png");
            Directory.CreateDirectory("Validation");
            File.WriteAllText("Validation/player-sprites-runtime.json","{\"passed\":true,\"states\":\""+string.Join(",",states)+"\",\"checks\":[\"idle/run/start/stop\",\"standing and directional jumps\",\"ground/air dash\",\"dash direction lock\",\"air dash to fall without ascent\",\"fall dash velocity restored\",\"one air dash until landing\",\"buffered landing jump\",\"ground dash jump cancel\"]}");
            Debug.Log("UNFIN_SPRITE_RUNTIME_OK");
        }
        static void Capture(Camera camera,string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var rt=new RenderTexture(1920,1080,24);var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
            finally{camera.targetTexture=null;RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);}
        }
    }
}
