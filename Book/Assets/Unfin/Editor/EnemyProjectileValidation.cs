using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unfin.Editor
{
    public static class EnemyProjectileValidation
    {
        static void Check(bool value,string message){if(!value)throw new Exception("Enemy projectiles: "+message);}
        static void ImportArt()
        {
            foreach(var name in new[]{"graphite","ink","seed"})
            {
                string path="Assets/Unfin/Resources/EnemyProjectiles/"+name+".png";
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.isReadable=true;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;
                importer.wrapMode=TextureWrapMode.Clamp;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;
                importer.SaveAndReimport();var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var pixels=texture.GetPixels32();int x0=texture.width,y0=texture.height,x1=0,y1=0;
                for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>128)
                {x0=Mathf.Min(x0,x);y0=Mathf.Min(y0,y);x1=Mathf.Max(x1,x);y1=Mathf.Max(y1,y);}
                Check(x1>x0&&y1>y0&&pixels[0].a==0,name+" opaque sprite and transparent background");
                float width=name=="graphite"?.5f:name=="ink"?.45f:.36f;
                importer.spritePixelsPerUnit=(x1-x0+1)/width;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
                settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=new Vector2((x0+x1+1)*.5f/texture.width,(y0+y1+1)*.5f/texture.height);
                settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
                Check(AssetDatabase.LoadAssetAtPath<Sprite>(path)!=null,name+" imported sprite");
            }
        }
        public static void RunAndBuild(){ImportArt();Run();EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);WebGLBuilder.Build();}
        [MenuItem("Unfin/Apply Enemy Projectiles")]
        public static void ApplyToProject()
        {
            ImportArt();Run();EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            AssetDatabase.SaveAssets();Debug.Log("UNFIN_ENEMY_PROJECTILES_APPLIED_TO_PROJECT");
        }
        public static void Run()
        {
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var w=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;typeof(WorldPrototype).GetMethod("Awake",flags).Invoke(w,null);
            var m=w.Motor;Array.Clear(m.cells,0,m.cells.Length);w.flying=true;
            var go=new GameObject("Projectile validation");var system=go.AddComponent<EnemyProjectileSystem>();system.Initialize(w);
            void Step(float seconds){for(float t=0;t<seconds-.00001f;t+=.005f)system.Tick(.005f);}
            for(int facing=-1;facing<=1;facing+=2)
            {
                system.Clear();Check(system.Fire(go,"R102",new Vector2(100,-70),new Vector2(100+facing*6,-70),facing),"horizontal spawn");
                Step(.4f);var p=system.Capture()[0];Check(Mathf.Abs(p.position.x-(100+facing*2.5f))<.002f&&Mathf.Abs(p.position.y+70)<.001f,"horizontal trajectory both directions");
            }
            Vector2 from=new Vector2(100,-70),target=new Vector2(104,-71);
            var velocity=EnemyProjectileSystem.BallisticVelocity(from,target,12,.9f);
            Check(Vector2.Distance(from+velocity*.9f+Vector2.down*(6*.9f*.9f),target)<.001f,"ink arc reaches locked aim");
            for(int y=0;y<m.data.height;y++)m.cells[y*m.data.width+103]=1;
            system.Clear();int impacts=system.WallImpactCount;system.Fire(go,"R102",from,target,1);system.Tick(.8f);
            Check(system.ActiveCount==0&&system.WallImpactCount==impacts+1,"no wall tunneling with a long tick");
            Array.Clear(m.cells,0,m.cells.Length);for(int x=0;x<m.data.width;x++)m.cells[80*m.data.width+x]=1;
            system.Clear();int bounces=system.BounceCount;system.Fire(go,"D02",new Vector2(100,-79),target,1);
            Check(!system.Fire(go,"D02",new Vector2(100,-79),target,1),"one seed per source");
            Step(.7f);Check(system.BounceCount==bounces+1&&system.ActiveCount==1,"seed bounces once");
            Step(1);Check(system.ActiveCount==0&&system.BounceCount==bounces+1,"seed shatters on next landing");
            system.Clear();system.Fire(go,"COMMON03",new Vector2(100,-79),new Vector2(104,-79.3f),1);Step(1.2f);
            Check(system.ActiveCount==1&&system.Capture()[0].kind==EnemyProjectileSystem.Kind.Puddle,"ink lands as a puddle");
            Step(1.6f);Check(system.ActiveCount==0,"puddle expires");
            system.Clear();int cuts=system.CutCount;system.Fire(go,"R102",from,target,1);
            var polygon=new[]{from+new Vector2(-.5f,-.5f),from+new Vector2(.5f,-.5f),from+new Vector2(.5f,.5f),from+new Vector2(-.5f,.5f)};
            system.Cut(polygon,from-Vector2.right);Check(system.ActiveCount==0&&system.CutCount==cuts+1,"player slash cuts projectile");
            system.Fire(go,"R102",from,target,1);w.Respawn(100,80);system.Tick(.005f);Check(system.ActiveCount==0,"respawn clears projectiles");
            w.flying=false;int contacts=system.PlayerContactCount;
            system.Fire(go,"R102",w.player.position,w.player.position+Vector3.right,1);system.Tick(.005f);
            Check(system.PlayerContactCount==contacts+1&&system.ActiveCount==0,"player contact consumes shot and gives feedback");
            // Fire through the actual monster attack, not just the projectile factory.
            var markerSprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,2,2),Vector2.one*.5f,2);
            foreach(string id in new[]{"COMMON03","R102","D02"})
            {
                system.Clear();var spec=Array.Find(PaperEnemyAnimation.Manifest.enemies,e=>e.id==id);
                var actorObject=new GameObject("Projectile attack timing "+id);actorObject.transform.position=new Vector3(105,-78,0);
                actorObject.AddComponent<SpriteRenderer>().sprite=markerSprite;var actor=actorObject.AddComponent<PaperEnemyAnimation>();actor.Initialize(spec);
                typeof(PaperEnemyAnimation).GetMethod("SetState",flags).Invoke(actor,new object[]{1});
                float release=spec.clips[1].motionDurations[0]+spec.clips[1].motionDurations[1];
                for(float t=0;t<release-.02f;t+=.01f)actor.Tick(.01f);
                Check(actor.ProjectileShots==0,id+" no emission during telegraph");
                actor.Tick(.03f);Check(actor.ProjectileShots==1,id+" emits at release frame");
                for(int i=0;i<10;i++)actor.Tick(.01f);Check(actor.ProjectileShots==1,id+" only one emission per attack");
                UnityEngine.Object.DestroyImmediate(actorObject);
            }
            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(markerSprite);
            Directory.CreateDirectory("Validation");File.WriteAllText("Validation/enemy-projectiles.json","{\"passed\":true,\"species\":3,\"capacity\":48,\"checks\":[\"transparent painted sprites\",\"attack-frame emission\",\"horizontal both directions\",\"locked ballistic aim\",\"wall sweep\",\"single seed bounce\",\"ink puddle expiry\",\"slash destroys projectile\",\"contact feedback\",\"respawn clear\"]}");
            Debug.Log("UNFIN_ENEMY_PROJECTILES_OK");
        }
    }
}
