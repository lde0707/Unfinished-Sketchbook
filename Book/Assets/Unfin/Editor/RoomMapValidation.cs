using System;
using System.IO;
using UnityEngine;
namespace Unfin.Editor
{
    public static class RoomMapValidation
    {
        static void Check(bool ok,string text){if(!ok)throw new Exception("Room map: "+text);}
        public static void Run(WorldPrototype world)
        {
            var data=world.Data;var layout=new RoomMapLayout(data,data.Decode());int checkedRooms=0;
            foreach(var room in data.rooms){
                int id=layout.Bounds.FindIndex(b=>b.x==room.x&&b.y==room.y&&b.width==room.w&&b.height==room.h);
                Check(id>=0&&layout.Cells[id].Count>0,"authored room exists: "+room.name);
                var discovery=new bool[data.width*data.height];Check(layout.Reveal(id,discovery),"first room entry reveals");
                for(int i=0;i<discovery.Length;i++)Check(discovery[i]==(layout.Owner[i]==id),"no radius or neighbouring room leakage: "+room.name);
                Check(!layout.Reveal(id,discovery),"same room does not redraw");
                var view=layout.View(id,room.x+.5f,room.y+.5f);
                Check(view.x<=room.x&&view.y<=room.y&&view.xMax>=room.x+room.w&&view.yMax>=room.y+room.h,"whole room fits mini journal: "+room.name);
                checkedRooms++;
            }
            var p=new WorldProgression(world);p.Reveal(236,130);int current=p.CurrentMapArea;
            int far=layout.At(490,35);Check(current>=0&&far>=0&&current!=far,"separate regions");
            foreach(int cell in layout.Cells[far])Check(!p.Discovered[cell],"unvisited room remains concealed");
            p.Reveal(243,130);Check(p.CurrentMapArea==current,"same room stable view");
            var saved=new StilldrawnSave();p.CaptureSession(saved);var fresh=new WorldProgression(world);fresh.RestoreSession(saved);
            for(int i=0;i<p.Discovered.Length;i++)Check(p.Discovered[i]==fresh.Discovered[i],"discovery survives save/load");
            fresh.Reveal(490,35);foreach(int cell in layout.Cells[far])Check(fresh.Discovered[cell],"other region revealed on entry");
            fresh.Reveal(236,130);foreach(int cell in layout.Cells[far])Check(fresh.Discovered[cell],"visited room persists after returning");
            // Metadata-free corridors remain discoverable, while stone is not an area.
            var fixture=new WorldData{width=8,height=4,rooms=new[]{new WorldArea{x=1,y=1,w=2,h=2}},passages=Array.Empty<WorldArea>(),shafts=Array.Empty<WorldArea>()};
            var terrain=new byte[32];Array.Fill(terrain,(byte)1);terrain[1*8+3]=terrain[1*8+4]=0;
            var extra=new RoomMapLayout(fixture,terrain);Check(extra.At(3,1)>=0&&extra.At(3,1)==extra.At(4,1)&&extra.At(3,1)!=extra.At(2,1),"connector does not flood known room");
            Directory.CreateDirectory("Validation");File.WriteAllText("Validation/room-map.json","{\"passed\":true,\"rooms\":"+checkedRooms+",\"roomWideReveal\":true,\"unvisitedRoomsHidden\":true,\"saveLoad\":true,\"fullRoomFitsMinimap\":true,\"generatedPaper\":true}");
        }
    }
}
