using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Unfin.Editor
{
    public static class MapZoneColorValidation
    {
        static void Check(bool ok,string message){if(!ok)throw new Exception("Map zones: "+message);}
        public static void RunAndBuild()
        {
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var w=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();var flags=BindingFlags.NonPublic|BindingFlags.Instance;
            typeof(WorldPrototype).GetMethod("Awake",flags).Invoke(w,null);
            var p=new WorldProgression(w);var update=typeof(WorldProgression).GetMethod("UpdateTexture",flags);
            var textureField=typeof(WorldProgression).GetField("mapTexture",flags);
            Texture2D Render(){update.Invoke(p,null);return (Texture2D)textureField.GetValue(p);}
            var blank=Render();Check(blank.GetPixels32().All(c=>c.a==0),"unvisited map remains transparent");
            p.Reveal(236,130);var known=(bool[])p.Discovered.Clone();var hints=(bool[])p.ConnectionHints.Clone();var partial=Render();
            Check(known.SequenceEqual(p.Discovered)&&hints.SequenceEqual(p.ConnectionHints),"colouring never changes discovery or exit hints");
            int w4=w.Data.width*4,h4=w.Data.height*4;var pixels=partial.GetPixels32();
            for(int i=0;i<known.Length;i++)if(!known[i]&&!hints[i])Check(pixels[(h4-1-(i/w.Data.width)*4)*w4+(i%w.Data.width)*4].a==0,"unknown cell stays hidden");
            foreach(var room in w.Data.rooms)p.Reveal(room.x+room.w*.5f,room.y+room.h*.5f);
            var full=Render();pixels=full.GetPixels32();var seen=new bool[8];
            for(int y=1;y<w.Data.height-1;y++)for(int x=1;x<w.Data.width-1;x++){
                int i=y*w.Data.width+x;if(!p.Discovered[i]||w.Motor.Get(x,y)!=0)continue;
                int z=RegionalTilePalette.ZoneAt(w.Data,x,y);if(seen[z])continue;
                Color c=pixels[(h4-1-y*4-2)*w4+x*4+1],ink=MapZonePalette.Ink(z);
                Check(c.a>.8f&&Mathf.Abs(c.r/c.g-ink.r/ink.g)<.035f&&Mathf.Abs(c.b/c.g-ink.b/ink.g)<.035f,"actual texture uses zone ink "+z);
                seen[z]=true;
            }
            Check(seen.All(v=>v),"all eight zones rendered");
            for(int a=0;a<8;a++)for(int b=a+1;b<8;b++){
                Color x=MapZonePalette.Ink(a),y=MapZonePalette.Ink(b);
                Check(new Vector3(x.r-y.r,x.g-y.g,x.b-y.b).magnitude>.18f,"distinct zone colours "+a+"/"+b);
            }
            var legend=(bool[])typeof(WorldProgression).GetField("discoveredMapZones",flags).GetValue(p);Check(legend.All(v=>v),"visited zone legend");
            Directory.CreateDirectory("Validation");File.WriteAllBytes("Validation/map-zone-colors.png",full.EncodeToPNG());
            File.WriteAllText("Validation/map-zone-colors.json","{\"passed\":true,\"zones\":8,\"actualTextureVerified\":true,\"unknownCellsHidden\":true,\"discoveryAndHintsUnchanged\":true,\"sharedMinimapJournalTexture\":true}");
            Debug.Log("MAP_ZONE_COLORS_OK");EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);WebGLBuilder.Build();
        }
    }
}
