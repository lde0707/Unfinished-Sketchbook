using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Unfin.Editor
{
    public static class EnemyAnimationValidation
    {
        [MenuItem("Unfin/Validate Region 1 Enemy Animations")]
        public static void Run()
        {
            ValidateMotion();
            AssetDatabase.Refresh();
            foreach(string path in Directory.GetFiles("Assets/Unfin/Resources/EnemyAnimations","*.png"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;
                importer.mipmapEnabled=false;importer.isReadable=true;importer.filterMode=FilterMode.Bilinear;
                importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();
            }
            var manifest=PaperEnemyAnimation.Manifest;
            Require(manifest.enemies.Length==18,"18 species");
            Require(manifest.placements.Select(p=>p.id).Distinct().Count()==18,"all species assigned");
            foreach(var spec in manifest.enemies)
            {
                Require(spec.clips.Length==3&&spec.clips.All(c=>c.name!="Hurt"),spec.id+" no hurt clip");
                var phases=spec.clips[1].motionDurations;
                if(phases!=null)Require(phases.Length==4&&Mathf.Abs(phases.Sum()-spec.clips[1].durations.Sum())<.0001f,spec.id+" attack movement timing preserved");
                var go=new GameObject("Animation validation "+spec.id);
                var sr=go.AddComponent<SpriteRenderer>();
                var actor=go.AddComponent<PaperEnemyAnimation>();actor.Initialize(spec);
                typeof(PaperEnemyAnimation).GetField("world",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(actor,null);
                var receiver=go.GetComponent<PaperAttackTarget>();
                typeof(PaperAttackTarget).GetMethod("OnEnable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(receiver,null);
                Require(sr.sprite!=null&&actor.StateName=="Move",spec.id+" movement");
                for(int i=0;i<5;i++)actor.Tick(.05f);
                Require(actor.FrameIndex>0&&actor.FrameIndex<spec.clips[0].durations.Length,spec.id+" movement advances");
                typeof(PaperEnemyAnimation).GetMethod("SetState",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(actor,new object[]{1});
                for(int i=0;i<200;i++)actor.Tick(.05f);
                Require(actor.StateName=="Move",spec.id+" attack returns");
                int priorFrame=actor.FrameIndex;
                receiver.ReceiveHit(1);Require(actor.StateName=="Move"&&actor.Health==2&&actor.FrameIndex==priorFrame,spec.id+" nonlethal hit preserves animation");
                receiver.ReceiveHit(-1);receiver.ReceiveHit(1);
                Require(actor.StateName=="Death"&&!receiver.enabled,spec.id+" death removes target");
                float fadeSample=spec.clips[2].durations.Sum()+.1f;
                for(float elapsed=0;elapsed<fadeSample;elapsed+=.01f)actor.Tick(.01f);
                Require(sr.enabled&&sr.color.a>0&&sr.color.a<1,spec.id+" death fades gradually before hiding");
                for(int i=0;i<50;i++)actor.Tick(.05f);
                Require(actor.FrameIndex==spec.clips.Sum(c=>c.durations.Length)-1&&!sr.enabled,spec.id+" death stays final then hides");
                actor.ResetEnemy();Require(actor.StateName=="Move"&&receiver.enabled&&sr.enabled&&actor.Health==3,spec.id+" reset");
                Require(sr.color.a==1,spec.id+" reset restores opacity");
                UnityEngine.Object.DestroyImmediate(go);
            }
            int totalFrames=manifest.enemies.Sum(e=>e.clips.Sum(c=>c.durations.Length));
            Directory.CreateDirectory("Validation");File.WriteAllText("Validation/enemy-animation.json","{\"passed\":true,\"species\":18,\"states\":54,\"frames\":"+totalFrames+",\"checks\":\"movement, attack recovery, nonlethal hit continuity, death, reset, placement coverage\"}");
            AssetDatabase.SaveAssets();Debug.Log("UNFIN_ENEMY_ANIMATIONS_OK 18 species / 54 states / "+totalFrames+" frames");
        }
        public static void RunAndBuild(){Run();WebGLBuilder.Build();}
        static void ValidateMotion()
        {
            float[] timings={.3f,.3f,.4f,.5f};int moving=0;
            foreach(string id in new[]{"COMMON02","B03","C01","C03"})
            foreach(var delta in new[]{new Vector2(2,-3),new Vector2(-2,-3),new Vector2(2,1),new Vector2(-2,1),new Vector2(0,-2)})
            {
                var aim=EnemyAttackMotion.Aim(EnemyAttackMotion.For(id),delta);
                Require(Vector2.Dot(aim.normalized,delta.normalized)>.999f,id+" aims at player in both axes");
                Require(aim.magnitude<=delta.magnitude+.001f,id+" no target overshoot");
                Require(EnemyAttackMotion.AimedOffset(aim,.59f,timings,false)==Vector2.zero,id+" aimed telegraph stationary");
                Require(Vector2.Distance(EnemyAttackMotion.AimedOffset(aim,1,timings,false),aim)<.001f,id+" aimed strike endpoint");
                Require(EnemyAttackMotion.AimedOffset(aim,1.5f,timings,true).magnitude<.001f,id+" suspended enemy returns home");
            }
            foreach(string id in new[]{"COMMON01","COMMON02","R101","R103","A01","A02","B01","B02","B03","C01","C02","C03","D01"})
            {
                var profile=EnemyAttackMotion.For(id);
                Require(EnemyAttackMotion.Offset(profile,.59f,timings,1)==Vector2.zero,id+" telegraph stays still");
                Vector2 midpoint=EnemyAttackMotion.Offset(profile,.8f,timings,1);
                Require(midpoint.magnitude>.05f,id+" strike moves");moving++;
                Vector2 end=EnemyAttackMotion.Offset(profile,1.5f,timings,1);
                Require(Mathf.Abs(end.y)<.001f,id+" recovers height");
                Require(Mathf.Abs(end.x-profile.distance)<.001f,id+" distance");
                Vector2 left=EnemyAttackMotion.Offset(profile,.8f,timings,-1);
                Require(Mathf.Abs(left.x+midpoint.x)<.001f&&Mathf.Abs(left.y-midpoint.y)<.001f,id+" facing symmetry");
            }
            Func<int,int,int> wall=(x,y)=>y>=0||x>=3?1:0;
            Vector2 stopped=EnemyAttackMotion.Sweep(new Vector2(1,0),new Vector2(6,0),.4f,1,wall,true,0);
            Require(stopped.x<2.65f&&stopped.x>2.4f,"fast strike cannot tunnel through wall");
            Func<int,int,int> ledge=(x,y)=>y>=0&&x<3?1:0;
            stopped=EnemyAttackMotion.Sweep(new Vector2(1,0),new Vector2(6,0),.4f,1,ledge,true,0);
            Require(stopped.x<2.75f&&stopped.x>2.4f,"ground strike stops at edge");
            Func<int,int,int> floor=(x,y)=>y>=0?1:0;
            stopped=EnemyAttackMotion.Sweep(new Vector2(1,3),new Vector2(1,-3),.4f,1,floor,false,0);
            Require(stopped.y>=-.02f&&stopped.y<.1f,"falling strike stops on terrain");
            Debug.Log("UNFIN_ENEMY_MOTION_OK "+moving+" moving attacks; telegraph, direction, recovery, wall, edge, floor");
        }
        static void Require(bool value,string message){if(!value)throw new Exception("Enemy animation validation: "+message);}
    }
}
