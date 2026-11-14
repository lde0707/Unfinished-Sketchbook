using System;
using System.Collections.Generic;
using UnityEngine;
namespace Unfin
{
    // Each cell belongs to one room or connector. Touching a doorway cannot
    // recursively reveal the next room, even where authored rectangles overlap.
    public sealed class RoomMapLayout
    {
        public readonly List<RectInt> Bounds=new List<RectInt>();
        public readonly List<List<int>> Cells=new List<List<int>>();
        public readonly int[] Owner;
        readonly int roomCount;
        readonly WorldData data;
        public RoomMapLayout(WorldData data,byte[] terrain)
        {
            this.data=data;Owner=new int[data.width*data.height];Array.Fill(Owner,-1);
            AddAreas(data.rooms);roomCount=Cells.Count;AddAreas(data.passages);AddAreas(data.shafts);
            // Hand-carved additions outside the metadata are separate connectors.
            // Solid stone and sealed gates stop this fill; known rooms are never crossed.
            for(int i=0;i<Owner.Length;i++)if(Owner[i]<0&&!GridMotor.Wall(terrain[i])){
                int id=Cells.Count;var cells=new List<int>();Cells.Add(cells);
                var pending=new Queue<int>();pending.Enqueue(i);Owner[i]=id;
                int minX=data.width,minY=data.height,maxX=0,maxY=0;
                while(pending.Count>0){int p=pending.Dequeue(),x=p%data.width,y=p/data.width;cells.Add(p);
                    minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);
                    void Visit(int nx,int ny){if(nx<0||ny<0||nx>=data.width||ny>=data.height)return;int n=ny*data.width+nx;if(Owner[n]>=0||GridMotor.Wall(terrain[n]))return;Owner[n]=id;pending.Enqueue(n);}
                    Visit(x-1,y);Visit(x+1,y);Visit(x,y-1);Visit(x,y+1);
                }
                Bounds.Add(new RectInt(minX,minY,maxX-minX+1,maxY-minY+1));
            }
        }
        void AddAreas(WorldArea[] input)
        {
            if(input==null)return;var sorted=(WorldArea[])input.Clone();Array.Sort(sorted,(a,b)=>(a.w*a.h).CompareTo(b.w*b.h));
            foreach(var a in sorted){int id=Cells.Count;var cells=new List<int>();Cells.Add(cells);Bounds.Add(new RectInt(a.x,a.y,a.w,a.h));
                for(int y=Math.Max(0,a.y);y<Math.Min(data.height,a.y+a.h);y++)for(int x=Math.Max(0,a.x);x<Math.Min(data.width,a.x+a.w);x++){
                    int n=y*data.width+x;if(Owner[n]>=0)continue;Owner[n]=id;cells.Add(n);
                }
            }
        }
        public int At(float x,float y){int ix=Mathf.FloorToInt(x),iy=Mathf.FloorToInt(y);return ix<0||iy<0||ix>=data.width||iy>=data.height?-1:Owner[iy*data.width+ix];}
        public bool Reveal(int area,bool[] discovered)
        {
            if(area<0||area>=Cells.Count)return false;bool changed=false;
            foreach(int i in Cells[area])if(!discovered[i]){discovered[i]=true;changed=true;}return changed;
        }
        // Routes are visible hints, not visits: room contents and pickup markers
        // remain undiscovered until entry. Stop at the first unvisited room.
        public void RevealConnections(bool[] discovered,byte[] terrain,bool[] hints)
        {
            Array.Clear(hints,0,hints.Length);
            var pending=new Queue<(int cell,int depth)>();
            var roomDepth=new byte[hints.Length];
            bool Open(int i)=>!GridMotor.Wall(terrain[i])||WorldPrototype.Gate(terrain[i]);
            void Visit(int n,int depth){
                if(discovered[n]||!Open(n))return;
                int nextDepth=Owner[n]>=0&&Owner[n]<roomCount?depth+1:0;
                if(nextDepth>2||hints[n]&&nextDepth>=roomDepth[n])return;
                hints[n]=true;roomDepth[n]=(byte)nextDepth;
                // Show the sealed entrance, but do not disclose the passage beyond it.
                if(!WorldPrototype.Gate(terrain[n]))pending.Enqueue((n,nextDepth));
            }
            void Neighbours(int cell,Action<int> visit){
                int x=cell%data.width,y=cell/data.width;
                if(x>0)visit(cell-1);if(x+1<data.width)visit(cell+1);
                if(y>0)visit(cell-data.width);if(y+1<data.height)visit(cell+data.width);
            }
            for(int i=0;i<discovered.Length;i++)if(discovered[i]&&Open(i)&&!WorldPrototype.Gate(terrain[i]))
                Neighbours(i,n=>Visit(n,0));
            while(pending.Count>0){
                var p=pending.Dequeue();
                Neighbours(p.cell,n=>{
                    if(p.depth>0&&Owner[n]!=Owner[p.cell])return;
                    Visit(n,p.depth);
                });
            }
        }
        public Rect View(int area,float px,float py)
        {
            float w=52,h=30,cx=px,cy=py;
            if(area>=0){var b=Bounds[area];cx=b.center.x;cy=b.center.y;w=Mathf.Max(w,b.width+16);h=Mathf.Max(h,b.height+16);}
            w=Mathf.Max(w,h*314/165);h=w*165/314;
            return new Rect(Mathf.Clamp(cx-w/2,0,Mathf.Max(0,data.width-w)),Mathf.Clamp(cy-h/2,0,Mathf.Max(0,data.height-h)),w,h);
        }
    }
}
