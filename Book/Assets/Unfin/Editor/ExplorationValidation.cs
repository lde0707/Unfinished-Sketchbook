using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Unfin.Editor
{
    public static class ExplorationValidation
    {
        static void Check(bool ok,string label){if(!ok)throw new Exception("Exploration: "+label);}
        public static void Run()
        {
            EditorSceneManager.OpenScene(WorldPrototypeBuilder.ScenePath);var w=UnityEngine.Object.FindAnyObjectByType<WorldPrototype>();
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(WorldPrototype).GetMethod("Awake",flags).Invoke(w,null);var m=w.Motor;var p=w.player.GetComponentInChildren<PaperSpritePlayer>();var state=w.Progression;state.UseAbilityLocks=true;
            void Step(int n,int ix=0,int iy=0){for(int i=0;i<n;i++){w.Simulate(.005f,ix,iy);p.Tick(.005f);}}
            Check(!state.Has("walljump")&&!state.Has("dash")&&!state.Has("climb")&&!state.Has("parry"),"normal play starts without later abilities");
            Check(!w.RequestDash(1),"dash locked at start");
            w.Respawn(260,131);Step(100,1);Check(m.Right<=261*16+.01f&&m.Get(261,129)==2,"starting detour physically blocked");
            Check(state.SeenFrontiers.Contains("start-return"),"first-seen barrier remembered");
            var start=state.Layout.frontiers.First(f=>f.id=="start-return");Check(!state.FrontierCleared(start),"closed marker before upgrade");
            state.Grant("dash");Check(state.Has("dash")&&state.SeenFrontiers.Contains(start.id),"new upgrade preserves discovery");
            Check(w.RequestDash(1),"dash unlock works");Step(40,1);Check(m.Get(261,129)==0&&state.FrontierCleared(start),"dash opens actual gate and remembered map state");
            state.Collect(w.Data.pickups.First(a=>a.type=="wj"));Check(state.Has("walljump"),"authored early wall-jump pickup");
            w.Respawn(179,132);Step(20);p.RequestAttack();Check(m.Get(181,129)==5,"ordinary attack cannot cut without reward");
            state.Grant("cut");w.Respawn(179,132);Step(20);p.RequestAttack();Check(m.Get(181,129)==0,"earned cut clears vine geometry");
            w.Respawn(156,92);Step(20);state.Parry();Check(m.Get(153,89)==13,"parry seal cannot open before ability");
            state.Grant("parry");state.Parry();Check(m.Get(153,89)==0&&m.Get(404,120)==13,"local parry seal opens without clearing distant gates");
            state.Collect(w.Data.pickups.First(a=>a.type=="item_shield"));float hp=state.Health;state.Damage();Check(!state.ShieldReady&&state.Health==hp,"revisit shield is functional");
            state.Health=2;state.Collect(w.Data.pickups.First(a=>a.type=="item_heal"));state.Heal();Check(state.Health==4&&state.HealingCharges==2,"revisit healing item is functional");
            m.x=97*16-GridMotor.Width+GridMotor.SideInset;m.y=50*16;m.vx=m.vy=0;m.grounded=false;Step(2,1,-1);Check(!w.OnWall,"wax cannot be climbed before region-two reward");
            state.Grant("climb");m.x=97*16-GridMotor.Width+GridMotor.SideInset;m.y=50*16;m.vx=m.vy=0;m.grounded=false;Step(3,1,-1);Check(w.OnWall&&m.vy<0,"later climb works on old region-one wax wall");
            var wax=state.Layout.frontiers.First(f=>f.id=="wax-nest");Check(!state.FrontierCleared(wax),"climb return stays marked until reward is claimed");
            var fall=state.Layout.frontiers.First(f=>f.id=="waterfall");state.Discovered[109*w.Data.width+550]=true;Check(!state.FrontierCleared(fall),"old discovery cannot hide new waterfall return");
            state.Collect(w.Data.pickups.First(a=>a.type=="healup"&&a.tx==87));Check(state.MaxHealingCharges==4&&state.FrontierCleared(wax),"climb reward increases real charges and completes marker");
            var remembered=new HashSet<string>(state.SeenFrontiers);w.BeginDeath();Step(360);Check(remembered.All(id=>state.SeenFrontiers.Contains(id))&&state.Has("climb"),"death preserves exploration and unlocks");
            Check(!state.Has("swim")&&!state.Has("glide"),"unimplemented later regions do not grant future abilities");
            Check(w.Data.pickups.Count(a=>a.type=="save")==10,"checkpoint density unchanged");
            Check(Resources.Load<Font>("UI/NotoSansCJKkr-Regular")!=null,"Korean map font included");
            var routeMethod=typeof(MezzanineValidation).GetMethod("FindJump",BindingFlags.Static|BindingFlags.NonPublic);
            var results=new List<MezzanineValidation.Result>();
            var west=new[]{new[]{141,150,132},new[]{144,147,129},new[]{142,145,126},new[]{144,147,123},new[]{142,145,120},new[]{144,147,117},new[]{142,145,114},new[]{144,147,111},new[]{142,145,108}};
            var east=new[]{new[]{400,406,88},new[]{402,405,87},new[]{400,403,84},new[]{400,403,81},new[]{402,405,78},new[]{400,403,75},new[]{402,405,72},new[]{400,403,69}};
            // Shaft step alternation follows the authored three-cell cadence.
            for(int i=3;i<east.Length;i++){int row=east[i][2];int n=(row-69)/3;east[i][0]=n%2==0?400:402;east[i][1]=east[i][0]+3;}
            foreach(var chain in new[]{west,east})for(int i=1;i<chain.Length;i++){
                var a=chain[i-1];var b=chain[i];var route=new MezzanineValidation.Route{label="new shaft "+a[0]+","+a[2]+" → "+b[0]+","+b[2],fromX=a[0],fromEnd=a[1],fromY=a[2],toX=b[0],toEnd=b[1],toY=b[2]};
                var result=(MezzanineValidation.Result)routeMethod.Invoke(null,new object[]{w.Data,w.Data.Decode(),route});results.Add(result);Check(result.passed,route.label);
            }
            Directory.CreateDirectory("Validation");File.WriteAllText("Validation/exploration-progression.json","{\"passed\":true,\"loops\":3,\"frontiers\":13,\"newShaftJumps\":"+results.Count+",\"checks\":[\"locked starting kit\",\"barrier memory\",\"dash collision gate\",\"cut gate\",\"local parry gate\",\"working shield and healing\",\"region-two climb on region-one wall\",\"death preserves discoveries\",\"future abilities remain locked\",\"ten checkpoints retained\"]}");
            Debug.Log("UNFIN_EXPLORATION_PROGRESSION_OK");
        }
    }
}
