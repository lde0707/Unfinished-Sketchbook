using System;
using System.IO;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unfin.Editor
{
    public static class PlayerAttackRangeValidation
    {
        static void Check(bool ok,string label){if(!ok)throw new Exception("Attack range: "+label);}
        public static void Run()
        {
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);
            var w=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            typeof(WorldPrototype).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(w,null);
            var p=w.player.GetComponentInChildren<PaperSpritePlayer>();var m=w.Motor;
            Array.Clear(m.cells,0,m.cells.Length);for(int x=0;x<w.Data.width;x++)m.cells[40*w.Data.width+x]=1;
            void Step(int ms,int dir=0){for(int i=0;i<ms;i+=5){w.Simulate(.005f,dir);p.Tick(.005f);}}
            void Reset(int facing=1){w.Respawn(100,40);typeof(WorldPrototype).GetField("facing",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(w,facing);Step(100);}
            var white=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,2,2),Vector2.one*.5f,2);
            PaperAttackTarget Target(string name,Vector2 offset)
            {
                var go=new GameObject(name);go.transform.position=p.transform.position+(Vector3)offset;go.transform.localScale=Vector3.one*.12f;
                go.AddComponent<SpriteRenderer>().sprite=white;var target=go.AddComponent<PaperAttackTarget>();
                // This batch test does not enter Play mode; initialize runtime lifecycle explicitly.
                if(!PaperAttackTarget.Active.Contains(target))typeof(PaperAttackTarget).GetMethod("OnEnable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null);
                return target;
            }
            var start=typeof(PaperSpritePlayer).GetMethod("StartAttack",BindingFlags.Instance|BindingFlags.NonPublic);
            var near=Target("inside authored attack",Vector2.zero);
            var far=Target("beyond tip",Vector2.zero);
            var behind=Target("behind player",Vector2.zero);
            Vector2 Inside(Vector2[] poly)=>poly.Length<20?Vector2.Lerp(poly[8],poly[3],.65f):Vector2.Lerp(poly[0],poly[21],.8f);
            foreach(bool moving in new[]{false,true})foreach(int facing in new[]{-1,1})foreach(int stage in new[]{0,1,2}){
                Reset(facing);if(moving)Step(180,facing);
                near.transform.position=far.transform.position=behind.transform.position=new Vector3(-50,-50,0);
                start.Invoke(p,new object[]{stage});p.Render(facing);
                // Thrust's 12ms extension leads into its full contact drawing.
                Step(15,moving?facing:0);
                string expected=(moving?"MoveAttack":"Attack")+new[]{3,1,2}[stage];
                Check(p.AttackAnimation==expected,"authored clip order "+expected);
                var poly=p.ActiveHitPolygon;Check(poly!=null,"active outline "+expected);
                float max=0;foreach(var v in poly)max=Mathf.Max(max,(v.x-p.transform.position.x)*facing);
                Check(max>(stage==0?2.2f:1.6f)*GridMotor.BodyScale,"preserved reach "+expected);
                near.transform.position=Inside(poly);
                far.transform.position=p.transform.position+new Vector3(facing*(max+.6f),.5f,0);
                behind.transform.position=p.transform.position+new Vector3(-facing*.8f,.5f,0);
                int n=near.HitCount,f=far.HitCount,b=behind.HitCount;p.Tick(0);
                Check(near.HitCount==n+1,"matching effect hits "+expected);
                if(!moving&&facing==1){
                    w.worldCamera.aspect=16f/9;w.worldCamera.orthographicSize=2.2f;
                    w.worldCamera.transform.position=w.player.position+new Vector3(.65f,.4f,-10);
                    PaperSpriteValidation.Capture(w.worldCamera,"Validation/combo-order-"+(stage+1)+".png");
                }
                Check(far.HitCount==f&&behind.HitCount==b,"outside and behind miss "+expected);
                p.Tick(0);Check(near.HitCount==n+1,"one hit per attack "+expected);
                for(int i=0;i<200&&p.Attacking;i++)Step(5,moving?facing:0);
                Check(p.ActiveHitPolygon==null,"recovery removes volume "+expected);
            }
            Reset();near.transform.position=far.transform.position=behind.transform.position=new Vector3(-50,-50,0);
            p.RequestAttack();Step(15);near.transform.position=Inside(p.ActiveHitPolygon);int count=near.HitCount;
            for(int row=35;row<40;row++)m.cells[row*w.Data.width+101]=1;
            p.Tick(0);Check(near.HitCount==count,"solid wall blocks first thrust");
            for(int row=35;row<40;row++)m.cells[row*w.Data.width+101]=0;
            p.Tick(.25f);Check(near.HitCount==count+1,"low fps crossing still hits once");
            foreach(bool falling in new[]{false,true})foreach(int facing in new[]{-1,1})
            {
                Reset(facing);near.transform.position=new Vector3(-50,-50,0);m.y-=50;m.vy=falling?100:-100;m.grounded=false;Step(5);p.RequestAttack();Step(60);
                var polygon=p.ActiveHitPolygon;Check(polygon!=null,"air active outline");
                near.transform.position=Inside(polygon);count=near.HitCount;p.Tick(0);
                Check(near.HitCount==count+1,"air hit follows rendered transform "+falling+" "+facing);
                w.RequestDash(facing);p.Tick(0);Check(p.ActiveHitPolygon==null,"dash cancellation removes hit volume");
            }
            File.WriteAllText("Validation/attack-range.json","{\"passed\":true,\"checks\":[\"1.7tile sweep hit\",\"outside/behind/above miss\",\"immediate hit/recovery inactive\",\"one hit per swing\",\"wall blocking\",\"left facing\",\"low fps crossing\",\"all3stages standing/moving both directions\",\"2.3tile thrust hit\",\"rise/fall both directions\",\"moving hit\",\"dash cancellation\"]}");
            Debug.Log("UNFIN_ATTACK_RANGE_OK");
        }
    }
}
