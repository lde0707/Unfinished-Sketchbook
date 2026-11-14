using System;
using System.IO;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unfin.Editor
{
    public static class HitFeedbackValidation
    {
        static void Check(bool ok,string message){if(!ok)throw new Exception("Hit feedback: "+message);}
        public static void RunAndBuild()
        {
            Run();EnemyProjectileValidation.Run();PaperVfxValidation.Run();
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);WebGLBuilder.Build();
        }
        public static void Run()
        {
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var w=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(WorldPrototype).GetMethod("Awake",flags).Invoke(w,null);
            var p=w.player.GetComponentInChildren<PaperSpritePlayer>();var m=w.Motor;
            Array.Clear(m.cells,0,m.cells.Length);for(int x=0;x<w.Data.width;x++)m.cells[80*w.Data.width+x]=1;
            void Reset(){w.Respawn(100,80);w.Simulate(.02f);p.Tick(.02f);}
            Reset();var point=(Vector2)w.player.position;
            var position=p.visual.transform.position;var sprite=p.visual.sprite;string animation=p.CurrentAnimation;
            Check(p.TryReceiveHit(point,1),"first contact accepted");
            Check(!p.TryReceiveHit(point,-1),"repeated contact cannot flicker continuously");
            var fx=p.visual.GetComponent<SpriteHitFeedback>();var block=new MaterialPropertyBlock();p.visual.GetPropertyBlock(block);
            Check(fx.Active&&block.GetFloat("_HitFlash")>.7f,"readable impact tint");
            fx.Tick(.08f);p.visual.GetPropertyBlock(block);
            Check(block.GetColor("_HitColor").r>block.GetColor("_HitColor").g*2,"player has red follow-through");
            Check(p.visual.sprite==sprite&&p.CurrentAnimation==animation&&p.visual.transform.position==position,"no pose or movement replacement");
            fx.Tick(.4f);p.visual.GetPropertyBlock(block);Check(!fx.Active&&block.GetFloat("_HitFlash")==0,"flash fully expires");
            Reset();Check(!p.HitReactionActive,"respawn clears tint");w.RequestDash(1);Check(!p.TryReceiveHit(point,1),"dash avoids contact feedback");
            Reset();
            var go=new GameObject("Melee feedback validation");go.transform.position=new Vector3((m.Left+m.Right)/32,-79.5f,0);
            var sr=go.AddComponent<SpriteRenderer>();var marker=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,2,2),Vector2.one*.5f,2);sr.sprite=marker;
            var actor=go.AddComponent<PaperEnemyAnimation>();var spec=Array.Find(PaperEnemyAnimation.Manifest.enemies,e=>e.id=="R101");actor.Initialize(spec);
            typeof(PaperEnemyAnimation).GetMethod("SetState",flags).Invoke(actor,new object[]{EnemyAnimationState.Attack});
            var receiver=go.GetComponent<PaperAttackTarget>();int frame=actor.FrameIndex;
            receiver.ReceiveHit(1,actor.BodyBounds.center);
            Check(actor.Health==2&&actor.StateName==spec.clips[1].name&&actor.FrameIndex==frame,"enemy hit preserves attack animation");
            Check(sr.GetComponent<SpriteHitFeedback>().Active,"enemy flash active");
            var timing=spec.clips[1].motionDurations??spec.clips[1].durations;float release=timing[0]+timing[1];
            actor.Tick(.01f);Check(!p.HitReactionActive,"telegraph does not hit");
            for(float t=.01f;t<release+.04f;t+=.01f)actor.Tick(.01f);
            Check(p.HitReactionActive,"melee strike triggers player feedback");
            actor.ResetEnemy();Check(!sr.GetComponent<SpriteHitFeedback>().Active&&sr.color.a==1,"enemy reset restores drawing");
            UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(marker);
            Directory.CreateDirectory("Validation");
            File.WriteAllText("Validation/hit-feedback.json","{\"passed\":true,\"checks\":[\"enemy silhouette flash\",\"player impact and red decay\",\"no animation or movement interruption\",\"melee active window\",\"550ms contact grace\",\"dash avoidance\",\"expiry and respawn clear\"]}");
            Debug.Log("UNFIN_HIT_FEEDBACK_OK");
        }
    }
}
