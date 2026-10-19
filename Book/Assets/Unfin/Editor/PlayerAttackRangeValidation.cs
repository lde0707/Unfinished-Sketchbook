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
            Reset();var near=Target("inside extended sweep",new Vector2(1.7f,.406f));
            var far=Target("outside sweep",new Vector2(2.2f,.406f));
            var behind=Target("behind player",new Vector2(-.7f,.406f));
            var high=Target("above sweep",new Vector2(1.7f,1.6f));
            p.RequestAttack();Check(near.HitCount==1,"input resolves damage immediately without another tick");
            Step(50);Check(near.HitCount==1,"extended range hits at1.7tiles");Check(far.HitCount==0&&behind.HitCount==0&&high.HitCount==0,"outside and behind miss");
            Step(235);Check(near.HitCount==1&&p.ActiveHitPolygon==null,"one hit per attack and recovery inactive");
            Reset();m.cells[39*w.Data.width+101]=1;p.RequestAttack();Step(180);Check(near.HitCount==1,"solid wall blocks hit");m.cells[39*w.Data.width+101]=0;
            Reset(-1);near.transform.position=p.transform.position+new Vector3(-1.7f,.406f,0);p.RequestAttack();Step(125);
            Check(near.HitCount==2&&near.LastHitDirection.x==-1,"mirrored hit and facing");
            Reset();near.transform.position=p.transform.position+new Vector3(1.7f,.406f,0);p.RequestAttack();p.Tick(.25f);
            Check(near.HitCount==3,"low fps crossing entire swing still hits");
            Step(110);p.RequestAttack();Check(p.AttackAnimation=="Attack2","second attack");
            near.transform.position=p.transform.position+new Vector3(1.8f,.91f,0);p.Tick(0);Check(near.HitCount==4,"second independent hit set");
            Step(295);p.RequestAttack();Step(15);near.transform.position=p.transform.position+new Vector3(2.3f,.594f,0);p.Tick(0);
            Check(near.HitCount==5,"thrust tip reaches2.3tiles");
            foreach(bool falling in new[]{false,true})foreach(int facing in new[]{-1,1})
            {
                Reset(facing);m.y-=50;m.vy=falling?100:-100;m.grounded=false;Step(5);p.RequestAttack();Step(60);
                var polygon=p.ActiveHitPolygon;Check(polygon!=null,"air active outline");
                // Use the authored center-to-tip sweep after airborne head anchoring and facing.
                near.transform.position=Vector2.Lerp(polygon[0],polygon[21],.85f);int count=near.HitCount;p.Tick(0);
                Check(near.HitCount==count+1,"air hit follows rendered transform "+falling+" "+facing);
            }
            Reset();Step(180,1);p.RequestAttack();Step(40,1);
            var moving=p.ActiveHitPolygon;Check(moving!=null,"moving outline active");near.transform.position=Vector2.Lerp(moving[0],moving[21],.85f);
            int before=near.HitCount;p.Tick(0);Check(near.HitCount==before+1,"moving attack follows player");
            w.RequestDash(1);p.Tick(0);Check(p.ActiveHitPolygon==null,"cancel removes hit volume");
            File.WriteAllText("Validation/attack-range.json","{\"passed\":true,\"checks\":[\"1.7tile sweep hit\",\"outside/behind/above miss\",\"immediate hit/recovery inactive\",\"one hit per swing\",\"wall blocking\",\"left facing\",\"low fps crossing\",\"second swing reset\",\"2.3tile thrust hit\",\"rise/fall both directions\",\"moving hit\",\"dash cancellation\"]}");
            Debug.Log("UNFIN_ATTACK_RANGE_OK");
        }
    }
}
