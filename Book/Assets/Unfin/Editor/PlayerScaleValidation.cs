using System;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace Unfin.Editor
{
    public static class PlayerScaleValidation
    {
        static void Check(bool ok,string message){if(!ok)throw new Exception("Player size: "+message);}
        public static void Run(WorldPrototype world)
        {
            var data=new WorldData{width=20,height=20};var cells=new byte[400];var m=new GridMotor(data,cells);
            foreach(byte type in new byte[]{1,4,6,15,16}){
                Array.Clear(cells,0,cells.Length);for(int x=0;x<20;x++)cells[10*20+x]=type;
                m.x=80;m.y=110;m.vx=0;m.vy=140;m.dropTime=0;
                for(int i=0;i<120;i++){m.vy+=8;m.Move(1f/120);}
                Check(m.grounded&&Mathf.Abs(m.Bottom-160)<.01f,"feet stay on floor type "+type);
                Check(Mathf.Abs(m.WorldBounds.size.y-21*.7f/16)<.001f,"smaller damage body");
                if(GridMotor.Platform(type)){m.dropTime=.22f;m.y+=2;m.vy=80;m.Move(.02f);Check(!m.grounded&&m.Bottom>160,"platform drop");}
            }
            Array.Clear(cells,0,cells.Length);for(int y=0;y<20;y++)cells[y*20+10]=1;
            foreach(int side in new[]{1,-1}){
                m.x=side>0?140:185;m.y=80;m.vy=0;m.vx=side*80;
                for(int i=0;i<80;i++)m.Move(1f/120);
                Check(Mathf.Abs((side>0?m.Right:m.Left)-(side>0?160:176))<.01f,"wall contact "+side);
                Check(m.TouchingWall()==side,"scaled wall sensor "+side);
                float before=m.x;m.SnapToWall(side);Check(Mathf.Abs(before-m.x)<.01f,"wall snap stable "+side);
            }
            Array.Clear(cells,0,cells.Length);for(int x=0;x<20;x++)cells[5*20+x]=1;
            m.x=80;m.y=110;m.vx=0;m.vy=-140;
            for(int i=0;i<60;i++)m.Move(1f/120);
            Check(Mathf.Abs(m.Top-96)<.01f,"head contact uses scaled top");
            var p=world.player.GetComponentInChildren<PaperSpritePlayer>();Check(p!=null,"sprite player present");
            var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
            var animation=typeof(PaperSpritePlayer).GetProperty("CurrentAnimation",flags);
            var clock=typeof(PaperSpritePlayer).GetProperty("AnimationTime",flags);
            int clips=0;
            foreach(var clip in p.library.clips){animation.SetValue(p,clip.name);clock.SetValue(p,.01f);p.Render(1);Check(Mathf.Abs(p.transform.localScale.x-.7f)<.001f&&Mathf.Abs(p.transform.localScale.y-.7f)<.001f,"scale "+clip.name);p.Render(-1);Check(Mathf.Abs(p.transform.localScale.x+.7f)<.001f,"left facing "+clip.name);clips++;}
            world.Motor.grounded=true;
            var attack=typeof(PaperSpritePlayer).GetMethod("StartAttack",flags);var cancel=typeof(PaperSpritePlayer).GetMethod("CancelAttack",flags);
            for(int i=0;i<3;i++){attack.Invoke(p,new object[]{i});foreach(int face in new[]{1,-1}){p.Render(face);Check(Mathf.Abs(p.transform.localScale.x-face*.7f)<.001f,"attack does not reset scale");}cancel.Invoke(p,new object[]{true});}
            animation.SetValue(p,"Idle");clock.SetValue(p,0f);p.Render(1);
            Directory.CreateDirectory("Validation");File.WriteAllText("Validation/player-scale70.json","{\"passed\":true,\"scale\":0.7,\"bodyWidth\":7,\"bodyHeight\":14.7,\"floorAndWallContact\":true,\"ceiling\":true,\"platformDrop\":true,\"clips\":"+clips+",\"attackScalePreserved\":true}");
        }
    }
}
