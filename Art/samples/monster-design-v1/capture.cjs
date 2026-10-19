const fs=require('node:fs'),path=require('node:path');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{
 const out=__dirname;fs.mkdirSync(path.join(out,'map-originals'),{recursive:true});
 const rooms=JSON.parse(fs.readFileSync('Art/room-backdrops-v1/room-layouts.json')).rooms;
 const plan={common:'1-05',r1:'1-08',a:'1-03',b:'1-09',c:'1-13',d:'1-23',r2:'2-03',e:'2-02',f:'2-07',g:'2-18',h:'2-21'};
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 try{
  const page=await browser.newPage({viewport:{width:1920,height:1250}});
  await page.goto('http://127.0.0.1:8080/?v=monster-concepts-20261018');
  await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:180000});await page.waitForTimeout(6500);
  const manifest=[];
  for(const [group,id] of Object.entries(plan)){
   const room=rooms.find(r=>r.id===id);
   await page.evaluate(spawn=>window.unityInstance.SendMessage('World Prototype','PreviewPosition',spawn),room.previewSpawn);
   await page.waitForTimeout(900);
   await page.locator('#unity-canvas').screenshot({path:path.join(out,'map-originals',group+'.png')});
   manifest.push({group,room:id,roomName:room.name,spawn:room.previewSpawn,source:'current local Unity WebGL build',image:'map-originals/'+group+'.png'});
  }
  fs.writeFileSync(path.join(out,'map-captures.json'),JSON.stringify(manifest,null,2));console.log('Captured 11 current Unity map views.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1)});
