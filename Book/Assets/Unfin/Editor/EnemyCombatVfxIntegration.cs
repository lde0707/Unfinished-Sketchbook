using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Unfin.Editor
{
    public static class EnemyCombatVfxIntegration
    {
        static void Check(bool v,string text){if(!v)throw new Exception("Enemy combat effects: "+text);}
        static void Private(object o,string name,params object[] args)=>o.GetType().GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,args);
        static void Position(WorldPrototype w,int x,int floor){w.Respawn(x,floor);w.Progression.Health=10000;w.Motor.vy=20;w.Motor.Move(.1f);Private(w,"SyncView");w.player.GetComponentInChildren<PaperSpritePlayer>().Tick(.001f);}
        static void Capture(WorldPrototype w,string name){var camera=w.worldCamera;var rt=RenderTexture.GetTemporary(1280,720,24);camera.targetTexture=rt;camera.aspect=16f/9;camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes("Validation/enemy-combat-effects/"+name+".png",image.EncodeToPNG());RenderTexture.active=old;camera.targetTexture=null;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);}
        public static void Validate(){
            Directory.CreateDirectory("Validation/enemy-combat-effects");EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);var w=UnityEngine.Object.FindFirstObjectByType<WorldPrototype>();Private(w,"Awake");
            var pool=new GameObject("Enemy effects validation pool").AddComponent<EnemyCombatEffects>();pool.Initialize(w);
            var markers=UnityEngine.Object.FindObjectsByType<MapMarker>(FindObjectsSortMode.None);foreach(var m in markers)Private(m,"Start");
            var all=markers.Where(m=>m.enemy).ToArray();Check(all.All(m=>m.GetComponent<EnemyCombatVfx>()!=null),"every existing enemy has effect observer");
            foreach(var m in all)m.GetComponent<EnemyCombatVfx>().Tick(0);
            int normals=0;foreach(var normal in UnityEngine.Object.FindObjectsByType<PaperEnemyAnimation>(FindObjectsSortMode.None).GroupBy(e=>e.SpeciesId).Select(g=>g.First())){
                var marker=normal.GetComponent<MapMarker>();var data=w.Data.enemies[marker.sourceIndex];Position(w,Mathf.Max(1,(int)normal.transform.position.x-4),data.floor);pool.Tick(0);normal.Tick(.001f);var fx=normal.GetComponent<EnemyCombatVfx>();fx.Tick(0);int before=fx.AttackBursts;Private(normal,"SetState",1);fx.Tick(0);
                var source=Array.Find(PaperEnemyAnimation.Manifest.enemies,e=>e.id==normal.SpeciesId);float duration=source.clips[1].durations.Sum();bool captured=false;
                for(float t=0;t<duration+.1f;t+=.01f){normal.Tick(.01f);float clock=normal.EffectClock,health=w.Progression.Health;fx.Tick(.01f);pool.Tick(.01f);Check(normal.EffectClock==clock&&w.Progression.Health==health,"decorative only "+normal.SpeciesId);if(!captured&&fx.AttackBursts>before){captured=true;if(new[]{"COMMON01","R102","R203","G01","H03"}.Contains(normal.SpeciesId))Capture(w,"normal-"+normal.SpeciesId);}}
                Check(fx.AttackBursts>before,"strike effect "+normal.SpeciesId);normals++;
            }
            var boss=all.Single(m=>m.sourceIndex==47).GetComponent<PaperBossRegion1>();var bossfx=boss.GetComponent<EnemyCombatVfx>();
            for(int p=0;p<6;p++){Position(w,255,34);pool.Tick(0);boss.Tick(.001f);bossfx.Tick(0);int before=bossfx.AttackBursts;boss.BeginPattern(p);bossfx.Tick(0);float captureAt=PaperBossRegion1.Art.patterns[p].activeStart+(p==3?.41f:p==5?.35f:.025f);bool captured=false;for(float t=0;t<1.65f;t+=.005f){boss.Tick(.005f);bossfx.Tick(.005f);pool.Tick(.005f);if(!captured&&t>=captureAt){Capture(w,"boss1-"+PaperBossRegion1.Names[p]);captured=true;}}Check(bossfx.AttackBursts>before,"boss pattern effect "+p);}
            foreach(int index in new[]{69,91}){var boss2=all.Single(m=>m.sourceIndex==index).GetComponent<PaperRegion2Encounter>();var fx=boss2.GetComponent<EnemyCombatVfx>();for(int p=0;p<(index==69?2:4);p++){Position(w,index==69?379:518,index==69?94:50);pool.Tick(0);boss2.Tick(.001f);fx.Tick(0);int before=fx.AttackBursts;boss2.BeginPattern(p);fx.Tick(0);float cap=index==69?(p==0?.74f:.89f):p==0?1.28f:p==1?.69f:p==2?.70f:.89f;bool captured=false;for(float t=0;t<1.7f;t+=.005f){boss2.Tick(.005f);fx.Tick(.005f);pool.Tick(.005f);if(!captured&&t>=cap){Capture(w,(index==69?"turtle-":"knight-")+p);captured=true;}}Check(fx.AttackBursts>before,"second-region pattern effect "+index+" "+p);}}
            foreach(int index in new[]{23,44}){var actor=all.Single(m=>m.sourceIndex==index).GetComponent<PaperRegion1Elite>();var fx=actor.GetComponent<EnemyCombatVfx>();Position(w,index==23?13:191,index==23?120:40);pool.Tick(0);actor.Tick(.001f);fx.Tick(0);int before=fx.AttackBursts;actor.BeginAttack();fx.Tick(0);for(float t=0;t<.6f;t+=.01f){actor.Tick(.01f);fx.Tick(.01f);pool.Tick(.01f);}Check(fx.AttackBursts>before,"elite attack effect "+index);Capture(w,"elite-"+index);}
            var sample=all.First(m=>m.GetComponent<PaperEnemyAnimation>()!=null).GetComponent<PaperEnemyAnimation>();Position(w,(int)sample.transform.position.x-3,Mathf.RoundToInt(-sample.transform.position.y));pool.Tick(0);sample.Tick(.001f);var deathfx=sample.GetComponent<EnemyCombatVfx>();deathfx.Tick(0);int deaths=pool.Deaths;int healthBefore=sample.Health;for(int i=0;i<healthBefore;i++){if(sample.StateName=="Death")break;sample.GetComponent<PaperAttackTarget>().ReceiveHit(1);}deathfx.Tick(.001f);Check(pool.Deaths>deaths,"real enemy death emits breakup");
            sample.GetComponent<SpriteHitFeedback>().Clear();sample.Tick(.2f);pool.Tick(.06f);Capture(w,"normal-death");
            Position(w,255,34);pool.Tick(0);boss.Tick(.001f);bossfx.Tick(0);int bossHealth=boss.Health;for(int i=0;i<bossHealth;i++)boss.GetComponent<PaperAttackTarget>().ReceiveHit(1);bossfx.Tick(.001f);boss.GetComponent<SpriteHitFeedback>().Clear();boss.Tick(.28f);pool.Tick(.08f);Capture(w,"boss1-death");
            Check(normals==33,"33 normal species");Check(pool.Trails>0&&pool.Landings>0,"rush traces and landing plumes");Check(pool.GetComponentsInChildren<Collider2D>().Length==0&&pool.GetComponentsInChildren<PaperAttackTarget>().Length==0,"no decorative collision or hit targets");
            for(int i=0;i<16;i++)pool.Death(new Bounds(w.player.position,Vector3.one*2),Color.gray,true);Check(pool.ActiveCount<=EnemyCombatEffects.Capacity,"bounded particle count");Private(w,"SyncView");typeof(WorldPrototype).GetField("showMap",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(w,true);int active=pool.ActiveCount;pool.Tick(2);Check(pool.ActiveCount==active,"map pauses decoration");typeof(WorldPrototype).GetField("showMap",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(w,false);pool.Tick(1.5f);Check(pool.ActiveCount==0,"effects expire");pool.Death(new Bounds(w.player.position,Vector3.one),Color.gray,false);w.Respawn(78,123);pool.Tick(0);Check(pool.ActiveCount==0,"respawn clears decoration");
            File.WriteAllText("Validation/enemy-combat-effects.json","{\"passed\":true,\"placements\":"+all.Length+",\"normalSpecies\":"+normals+",\"bossPatterns\":12,\"elitePatterns\":2,\"attackBursts\":"+pool.Strikes+",\"trailSamples\":"+pool.Trails+",\"landingBursts\":"+pool.Landings+",\"capacity\":"+EnemyCombatEffects.Capacity+",\"peakActive\":"+pool.PeakActive+",\"combatClocksUnchanged\":true,\"decorativeDamage\":false,\"mapPause\":true,\"respawnClear\":true}");Debug.Log("ENEMY_COMBAT_EFFECTS_OK 33 species / 12 boss patterns / 2 elites");EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
        }
        public static void RunAndBuild(){Validate();WebGLBuilder.Build();}
    }
}
