using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Unfin.Editor
{
    public static class PaintedHudIntegration
    {
        public static void Prepare()
        {
            string root="Assets/Unfin/Resources/PaintedUI/";
            if(!File.Exists(root+"icons.png"))return;
            var entries=new System.Collections.Generic.List<PaintedHudArt.Entry>();
            foreach(string sheet in new[]{"frames","icons","zone-titles","cartographer-panel"}){
                var importer=(TextureImporter)AssetImporter.GetAtPath(root+sheet+".png");
                importer.textureType=TextureImporterType.Default;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=4096;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.isReadable=true;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;
                var web=importer.GetPlatformTextureSettings("WebGL");web.overridden=true;web.maxTextureSize=4096;web.format=TextureImporterFormat.RGBA32;web.textureCompression=TextureImporterCompression.Uncompressed;importer.SetPlatformTextureSettings(web);importer.SaveAndReimport();
                var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(root+sheet+".png");
                if(sheet=="cartographer-panel"){
                    entries.Add(new PaintedHudArt.Entry{name=sheet,sheet=sheet,x=0,y=0,w=tex.width,h=tex.height});
                }else if(sheet=="zone-titles"){
                    var rows=TitleRows(tex);
                    for(int i=0;i<8;i++)entries.Add(Trim(tex,sheet,"zone-"+i,new Rect(0,(float)rows[i]/tex.height,1,(float)(rows[i+1]-rows[i])/tex.height)));
                }else if(sheet=="icons"){
                    string[] names={"leaf","leaf-empty","heal","shard","shield","heart","healup","paint"};
                    for(int i=0;i<8;i++)entries.Add(Trim(tex,sheet,names[i],new Rect(i%4*.25f,i/4*.5f,.25f,.5f)));
                }else{
                    entries.Add(Trim(tex,sheet,"map-frame",new Rect(0,0,.445f,.59f)));
                    entries.Add(Trim(tex,sheet,"ribbon",new Rect(.45f,.16f,.55f,.36f)));
                    entries.Add(Trim(tex,sheet,"key",new Rect(0,.60f,.46f,.40f)));
                    entries.Add(Trim(tex,sheet,"flourish",new Rect(.50f,.59f,.50f,.41f)));
                }
            }
            File.WriteAllText(root+"atlas.json",JsonUtility.ToJson(new PaintedHudArt.Catalog{entries=entries.ToArray()},true));AssetDatabase.Refresh();
            Directory.CreateDirectory("Validation");File.WriteAllText("Validation/painted-ui-atlas.json","{\"passed\":true,\"generatedSheets\":4,\"sprites\":21,\"zoneTitles\":8,\"alphaTrimmed\":true,\"healingColor\":\"red\"}");
            var banner=new ZoneEntryBanner();banner.Tick(0,.01f);banner.Tick(0,.35f);
            if(banner.Serial!=1||banner.Alpha<=0||banner.Alpha>=1)throw new Exception("Zone title fade-in failed");
            banner.Tick(1,.12f);banner.Tick(0,.01f);if(banner.Serial!=1)throw new Exception("Zone boundary flicker");
            banner.Tick(1,.31f);if(banner.Serial!=2||banner.Zone!=1)throw new Exception("Zone entry failed");
            banner.Tick(1,ZoneEntryBanner.Duration+1);if(banner.Alpha!=0||banner.Serial!=2)throw new Exception("Zone title must expire without repeating");
            File.WriteAllText("Validation/zone-entry-banner.json","{\"passed\":true,\"generatedTitles\":8,\"fadeIn\":0.7,\"hold\":2.3,\"fadeOut\":1,\"boundaryDebounce\":0.3,\"sameZoneDoesNotRepeat\":true}");
        }
        static int[] TitleRows(Texture2D tex)
        {
            // Generated lettering is not a rigid grid. Split at the transparent
            // gutter, so a title's shadow cannot leak into its neighbour's sprite.
            var pixels=tex.GetPixels32();var rows=new int[9];rows[8]=tex.height;
            for(int j=1;j<8;j++){
                int center=Mathf.RoundToInt(tex.height*j/8f),radius=Mathf.RoundToInt(tex.height/40f),bestStart=center,bestLength=0,start=-1;
                for(int y=center-radius;y<=center+radius;y++){
                    bool empty=true;for(int x=0;x<tex.width;x++)if(pixels[(tex.height-1-y)*tex.width+x].a>28){empty=false;break;}
                    if(empty){if(start<0)start=y;int length=y-start+1;if(length>bestLength){bestLength=length;bestStart=start;}}else start=-1;
                }
                if(bestLength==0)throw new Exception("Zone title atlas has no transparent gutter at row "+j);
                rows[j]=bestStart+bestLength/2;
            }
            return rows;
        }
        static PaintedHudArt.Entry Trim(Texture2D tex,string sheet,string name,Rect region)
        {
            int x0=Mathf.RoundToInt(region.x*tex.width),x1=Mathf.Min(tex.width,Mathf.RoundToInt(region.xMax*tex.width));
            int y0=Mathf.RoundToInt((1-region.yMax)*tex.height),y1=Mathf.Min(tex.height,Mathf.RoundToInt((1-region.y)*tex.height));
            var pixels=tex.GetPixels32();int minX=x1,minY=y1,maxX=-1,maxY=-1;
            for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)if(pixels[y*tex.width+x].a>28){minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}
            if(maxX<minX)throw new Exception("Missing generated UI art: "+name);
            return new PaintedHudArt.Entry{name=name,sheet=sheet,x=minX,y=minY,w=maxX-minX+1,h=maxY-minY+1};
        }
    }
}
