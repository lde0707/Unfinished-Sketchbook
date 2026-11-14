using System;
using System.IO;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Unfin.Editor {
 public static class PlayerWallValidation {
  static void Check(bool ok,string message){if(!ok)throw new Exception("Wall movement: "+message);}
  public static void RunAndBuild(){PaperSpriteImporter.Generate();Run();PlayerCombatValidation.Run();PlayerAttackRangeValidation.Run();EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);WebGLBuilder.Build();}
  public static void Run(){
   EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
   var w=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
   typeof(WorldPrototype).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(w,null);
   var p=w.player.GetComponentInChildren<PaperSpritePlayer>();var m=w.Motor;
   var original=(byte[])m.cells.Clone();Array.Clear(m.cells,0,m.cells.Length);
   for(int y=10;y<50;y++)m.cells[y*w.Data.width+100]=1;
   for(int x=80;x<120;x++)m.cells[50*w.Data.width+x]=1;
   void Step(int ms,int ix=0,int iy=0){for(int i=0;i<ms;i+=5){w.Simulate(.005f,ix,iy);p.Tick(.005f);}}
   void Attach(int side){w.Respawn(side==1?99:101,35);m.x=side==1?100*16-GridMotor.Width+GridMotor.SideInset-.5f:101*16-GridMotor.SideInset+.5f;m.y=30*16;m.grounded=false;m.vy=80;Step(20,side);Check(w.OnWall&&w.WallSide==side,"attach "+side);Check(p.CurrentAnimation=="WallHold","hold drawing");}
   foreach(int side in new[]{1,-1}){
    // Walk into the wall from a full tile away with no jump or climb input.
    w.Respawn(side==1?98:102,50);m.x=side==1?98*16:102*16;
    m.y=50*16-GridMotor.CollisionHeight;m.grounded=true;
    Step(700,side);Check(w.OnWall&&w.WallSide==side,"ground approach auto attach "+side);
    Check(p.CurrentAnimation=="WallHold","ground contact wall drawing");
    m.grounded=true;int groundSerial=w.JumpSerial;w.RequestJump();Step(5,-side);
    Check(w.JumpSerial==groundSerial+1&&m.vy<0&&Mathf.Sign(m.vx)==-side,"ground wall away jump");
    // Air contact also attaches in the same simulation step as the collision.
    w.Respawn(side==1?98:102,35);m.x=side==1?100*16-GridMotor.Width-2:101*16+2;
    m.y=30*16;m.grounded=false;m.vx=side*100;m.vy=30;
    for(int i=0;i<100&&!w.OnWall;i++){
     Step(5,side);if(m.hitWall==side)Check(w.OnWall,"attach on contact tick");
    }
    Check(w.OnWall,"air approach auto attach "+side);
   }
   foreach(int side in new[]{1,-1}){
    Attach(side);float y=m.y;Step(100);Check(Mathf.Abs(m.y-y)<.01f,"hold stable without input");
    Step(200,0,-1);Check(m.y<y-8&&p.CurrentAnimation=="WallClimb","climb up");
    y=m.y;Step(200,0,1);Check(m.y>y+8&&p.CurrentAnimation=="WallClimb","climb down");
    w.RequestJump();Step(5);Check(!w.OnWall&&m.vy<0&&Mathf.Sign(m.vx)==-side,"wall jump launches away");Check(p.CurrentAnimation=="MovingJump","jump drawing");
    Step(20,side);Check(!w.OnWall,"no instant regrab");
    Attach(side);Step(20,-side);Check(!w.OnWall&&m.vy>0&&p.CurrentAnimation=="Fall","release falls");
    Attach(side);int awaySerial=w.JumpSerial;
    w.RequestJump();Step(5,-side);
    Check(w.JumpSerial==awaySerial+1&&!w.OnWall&&m.vy<0&&Mathf.Sign(m.vx)==-side,"simultaneous away + jump "+side);
    Step(20,-side);Check(w.JumpSerial==awaySerial+1,"away jump consumed once");
    Attach(side);Step(80,-side);awaySerial=w.JumpSerial;
    w.RequestJump();Step(5,-side);
    Check(w.JumpSerial==awaySerial+1&&m.vy<0&&Mathf.Sign(m.vx)==-side,"away before jump grace "+side);
    Attach(side);Step(200,-side);awaySerial=w.JumpSerial;
    w.RequestJump();Step(5,-side);Check(w.JumpSerial==awaySerial,"expired wall grace cannot jump");
    Attach(side);w.Respawn(90,35);m.grounded=false;awaySerial=w.JumpSerial;
    w.RequestJump();Step(5);Check(w.JumpSerial==awaySerial,"respawn clears wall grace");
    Attach(side);Check(w.RequestDash(-side),"wall dash accepted");Step(20,-side);Check(!w.OnWall&&p.CurrentAnimation=="Dash","wall dash drawing");
   }
   foreach(int side in new[]{1,-1}){
    Attach(side);float startY=m.y;
    for(int cycle=0;cycle<6;cycle++){
     float priorY=m.y;int serial=w.JumpSerial;
     w.RequestJump();Step(5,side);Check(w.JumpSerial==serial+1&&!w.OnWall,"repeated hop launched");
     Step(25,side);Check(!w.OnWall,"hop visibly separates before regrab");
     // Simulate releasing Space early, as when tapping jump rapidly.
     if(m.vy<0)m.vy*=.45f;
     int elapsed=30;
     while(!w.OnWall&&elapsed<500){Step(5,side);elapsed+=5;}
     Check(w.OnWall&&w.WallSide==side,"same wall regrab without up input");
     Check(m.y<priorY-5,"each tap gains height");
     Check(elapsed<250,"quick same-wall return");
    }
    Check(startY-m.y>30,"six hops gain height");
    // A buffered press just before contact starts the next hop on contact.
    Attach(side);w.RequestJump();Step(50,side);int first=w.JumpSerial;
    w.RequestJump();Step(150,side);Check(w.JumpSerial>first,"pre-contact buffered wall jump");
   }
   Attach(1);m.y=10*16-18;Step(350,0,-1);Check(!w.OnWall,"wall top releases");
   Attach(1);m.y=50*16-GridMotor.Height-1;Step(100,0,1);Check(m.grounded&&!w.OnWall,"bottom lands");
   // Capture actual wall hold / climb with the existing map restored after physics checks.
   Attach(1);w.worldCamera.orthographicSize=2.5f;w.worldCamera.transform.position=w.player.position+new Vector3(0,.2f,-10);
   PaperSpriteValidation.Capture(w.worldCamera,"Validation/player-wall-hold.png");
   Step(200,0,-1);PaperSpriteValidation.Capture(w.worldCamera,"Validation/player-wall-climb.png");
   Array.Copy(original,m.cells,original.Length);
   Directory.CreateDirectory("Validation");File.WriteAllText("Validation/player-wall-runtime.json","{\"passed\":true,\"checks\":[\"ground approach auto attach both walls\",\"ground wall away jump\",\"air contact immediate attach\",\"left/right attach\",\"hold\",\"climb up/down\",\"wall jump\",\"regrab lock\",\"six same-wall tap jumps on both sides\",\"buffered regrab jump\",\"away + jump both walls\",\"80ms delayed away jump\",\"expired grace\",\"respawn clears grace\",\"release to fall\",\"dash\",\"top release\",\"bottom landing\"]}");
   Debug.Log("UNFIN_WALL_RUNTIME_OK");
  }
 }
}
