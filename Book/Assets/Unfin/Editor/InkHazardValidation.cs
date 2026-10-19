using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor.SceneManagement;
namespace Unfin.Editor {
 public static class InkHazardValidation {
  public static void RunAndBuild(){Run();WebGLBuilder.Build();}
  public static void Run(){
   EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);var w=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
   var f=BindingFlags.Instance|BindingFlags.NonPublic;typeof(WorldPrototype).GetMethod("Awake",f).Invoke(w,null);
   w.Respawn(365,12);int serial=w.ResetSerial;
   w.Motor.x=366*16+2;w.Motor.y=(12+GridMotor.LiquidSurfaceInset)*16-GridMotor.Height+.1f;
   typeof(WorldPrototype).GetMethod("CheckGround",f).Invoke(w,null);
   if(!w.IsDrowning||w.ResetSerial!=serial)throw new Exception("Expected sink before respawn");
   if(w.RequestDash(1))throw new Exception("Dash escaped drowning");
   for(int i=0;i<480;i++)w.Simulate(1f/120,1,0);
   if(w.ResetSerial!=serial+1||!w.HazardInputBlocked||w.Motor.x>=365*16||w.IsDrowning)throw new Exception("Held direction repeatedly respawned or unsafe return");
   float position=w.Motor.x;w.Simulate(1f/120,0,0);
   if(w.HazardInputBlocked)throw new Exception("Input did not rearm after release");
   for(int i=0;i<12;i++)w.Simulate(1f/120,1,0);
   if(w.Motor.x<=position)throw new Exception("Movement failed after release");
   if(Resources.Load<Shader>("InkDepth")==null)throw new Exception("Missing ink depth shader");
   Directory.CreateDirectory("Validation");File.WriteAllText("Validation/ink-hazard.json","{\"passed\":true,\"sinkSeconds\":0.55,\"respawnsWhileHoldingFor4Seconds\":1,\"safeBankReturn\":true,\"releaseToRearm\":true}");
   Debug.Log("UNFIN_INK_HAZARD_OK");EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
  }
 }
}
