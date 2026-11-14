using UnityEngine;
namespace Unfin
{
    public sealed partial class WorldProgression
    {
        Texture2D mapTexture;
        byte[] mapZones;
        readonly bool[] discoveredMapZones=new bool[8];
        bool mapDirty=true;
        int mapRegion=-1;
        GUIStyle label,small,title;
        void Styles(){if(label!=null)return;var font=Resources.Load<Font>("UI/NotoSansCJKkr-Regular");label=new GUIStyle(GUI.skin.label){font=font,fontSize=22};label.normal.textColor=new Color(.9f,.88f,.81f);small=new GUIStyle(label){fontSize=18};title=new GUIStyle(label){fontSize=28,fontStyle=FontStyle.Bold};}
        void Fill(Rect rect,Color color){var old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;}
        void UpdateTexture()
        {
            const int resolution=4;
            if(mapTexture==null){mapTexture=new Texture2D(world.Data.width*resolution,world.Data.height*resolution,TextureFormat.RGBA32,false);mapTexture.filterMode=FilterMode.Bilinear;mapTexture.wrapMode=TextureWrapMode.Clamp;}
            if(!mapDirty)return;mapDirty=false;int w=world.Data.width,h=world.Data.height;
            System.Array.Clear(discoveredMapZones,0,discoveredMapZones.Length);
            // Draw the real terrain on generated graphite paper, rather than
            // showing an invented map painted into the frame. The grain is
            // sampled from the same image as the journal so both feel printed.
            var paper=PaintedHudArt.MapPaper;var grain=paper.GetPixels32();int tw=paper.width,th=paper.height;
            var pixels=new Color32[w*h*resolution*resolution];
            bool KnownAir(int x,int y)=>x>=0&&y>=0&&x<w&&y<h&&(Discovered[y*w+x]||ConnectionHints[y*w+x])&&world.Motor.Get(x,y)!=1;
            for(int y=0;y<h;y++)for(int x=0;x<w;x++){
                if(!Discovered[y*w+x]&&!ConnectionHints[y*w+x])continue;int t=world.Motor.Get(x,y);bool rock=t==1;
                int zone=MapZoneAt(x,y);if(Discovered[y*w+x])discoveredMapZones[zone]=true;
                bool left=KnownAir(x-1,y),right=KnownAir(x+1,y),up=KnownAir(x,y-1),down=KnownAir(x,y+1);
                // Solid interiors are quiet charcoal; only the actual traversable
                // outline gets a pale graphite stroke. Unknown rooms stay blank.
                Color color=rock?MapZonePalette.Rock(zone):GridMotor.Liquid(t)?new Color(.40f,.32f,.48f,.9f):WorldPrototype.Gate(t)?new Color(.66f,.40f,.25f,.95f):MapZonePalette.Fill(zone);
                for(int sy=0;sy<resolution;sy++)for(int sx=0;sx<resolution;sx++){
                    bool edge=rock?((sx==0&&left)||(sx==3&&right)||(sy==0&&up)||(sy==3&&down)):((sx==0&&!left)||(sx==3&&!right)||(sy==0&&!up)||(sy==3&&!down));
                    Color c=edge?MapZonePalette.Outline(zone):color;
                    if(GridMotor.Platform(t)&&sy==1)c=new Color(.88f,.77f,.55f,.95f);
                    int gx=(int)(tw*.15f)+(x*resolution+sx)%(int)(tw*.68f),gy=(int)(th*.2f)+(y*resolution+sy)%(int)(th*.5f);
                    float texture=.89f+grain[gy*tw+gx].r/255f*.42f;c.r*=texture;c.g*=texture;c.b*=texture;
                    pixels[((h*resolution-1-y*resolution-sy)*w*resolution)+x*resolution+sx]=c;
                }
            }mapTexture.SetPixels32(pixels);mapTexture.Apply(false);
        }
        int MapZoneAt(int x,int y)
        {
            // Zone membership is static; cache lazily as rooms/exit hints are revealed.
            if(mapZones==null){mapZones=new byte[world.Data.width*world.Data.height];System.Array.Fill(mapZones,(byte)255);}
            int i=y*world.Data.width+x;
            if(mapZones[i]==255)mapZones[i]=(byte)RegionalTilePalette.ZoneAt(world.Data,x,y);
            return mapZones[i];
        }
        void DrawZoneLegend()
        {
            int slot=0;
            for(int z=0;z<8;z++){
                if(!discoveredMapZones[z]||(mapRegion==1&&z>=4)||(mapRegion==2&&z<4))continue;
                float x=240+slot%4*220,y=754+slot/4*29;
                Fill(new Rect(x,y+7,13,13),MapZonePalette.Ink(z));
                HudText(new Rect(x+22,y,198,28),ZoneEntryBanner.Names[z],hudInk,16);
                slot++;
            }
        }
        Vector2 MapPoint(Rect rect,Rect view,float x,float y)=>new Vector2(rect.x+(x-view.x)/view.width*rect.width,rect.y+(y-view.y)/view.height*rect.height);
        void Marker(Rect rect,Rect view,float x,float y,Color color,float size=6){if(!view.Contains(new Vector2(x,y)))return;var p=MapPoint(rect,view,x,y);HudDiamond(p.x,p.y,size+3,new Color(.10f,.085f,.07f));HudDiamond(p.x,p.y,size,color);}
        void DrawMap(Rect rect,Rect view,bool detailed)
        {
            UpdateTexture();GUI.DrawTextureWithTexCoords(rect,mapTexture,new Rect(view.x/world.Data.width,1-view.yMax/world.Data.height,view.width/world.Data.width,view.height/world.Data.height));
            foreach(var point in world.Data.pickups){int x=point.tx,y=point.floor-1;if(y<0||!Discovered[y*world.Data.width+x]||Collected(point))continue;
                if(point.type=="save"){
                    Marker(rect,view,x,y,new Color(.54f,.85f,.68f),detailed?8:5);
                    if(CheckpointPlan.IsShop(point)&&view.Contains(new Vector2(x,y))){var at=MapPoint(rect,view,x,y);float size=detailed?16:11;Fill(new Rect(at.x-size*.5f,at.y-size*.5f,size,size),new Color(.13f,.10f,.07f));Fill(new Rect(at.x-size*.5f+2,at.y-size*.5f+2,size-4,size-4),new Color(.93f,.78f,.43f));}
                }
                else if(point.type=="station")Marker(rect,view,x,y,new Color(.76f,.49f,.73f),detailed?7:4);
                else if(point.type=="heart"||point.type.StartsWith("item_")||point.type=="healup")Marker(rect,view,x,y,new Color(.86f,.7f,.39f),detailed?6:3);
            }
            foreach(var f in Layout.frontiers)if(SeenFrontiers.Contains(f.id)&&!FrontierCleared(f)){
                bool ready=Has(f.ability);var color=ready?new Color(.3f,.94f,.83f):new Color(.94f,.58f,.25f);
                Marker(rect,view,f.x+.5f,f.y+f.h*.5f,color,detailed?11:7);

            }
            Marker(rect,view,(world.Motor.x+6)/16,(world.Motor.y+11)/16,new Color(.85f,.97f,1),9);
            DrawNotes(rect,view,detailed);
        }
        const float UiWidth=1920,UiHeight=1080;
        public void DrawGUI()
        {
            var old=GUI.matrix;var color=GUI.color;int depth=GUI.depth;GUI.depth=-100;
            float scale=Mathf.Min(Screen.width/UiWidth,Screen.height/UiHeight);
            if(world.MapOpen)Fill(new Rect(0,0,Screen.width,Screen.height),new Color(.015f,.02f,.025f,.65f));
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-UiWidth*scale)*.5f,(Screen.height-UiHeight*scale)*.5f,0),Quaternion.identity,new Vector3(scale,scale,1));
            try{Styles();HudStyles();if(InteractionChoiceOpen)DrawInteractionChoice();else if(ShopOpen)DrawShop();else if(world.ExplorationMapOpen)DrawJournalMap();else DrawGameplayHud();}finally{GUI.matrix=old;GUI.color=color;GUI.depth=depth;}
        }
    }
}
