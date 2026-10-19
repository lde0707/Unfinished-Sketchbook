const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {pathToFileURL}=require('node:url');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 try{
  const page=await browser.newPage({viewport:{width:1200,height:1000}}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(/Exception|NullReference/.test(m.text()))errors.push(m.text())});
  await page.goto(pathToFileURL(path.resolve('Art/samples/special-tiles-v1/preview.html')).href);
  await page.locator('img').evaluateAll(imgs=>Promise.all(imgs.map(i=>i.decode())));
  assert.equal(await page.locator('img').count(),12);await page.screenshot({path:'Art/samples/special-tiles-v1/contact-sheet.png',fullPage:true});
  if(process.argv.includes('--gallery-only'))return;
  await page.route(url=>url.pathname==='/',async route=>{const response=await route.fetch();await route.fulfill({response,body:(await response.text()).replace('width="1920" height="1080"','width="640" height="360"')});});
  await page.goto('http://127.0.0.1:8080/?v=special-tile-geometry-20261019');await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:240000});await page.locator('#unity-canvas').click();await new Promise(r=>setTimeout(r,6000));
  const d=JSON.parse(fs.readFileSync('Book/Assets/Unfin/Data/world12.json','utf8')),cells=d.mapRle.split(',').flatMap(s=>{const [t,n]=s.split('.').map(x=>parseInt(x,36));return Array(n).fill(t)}),get=(x,y)=>cells[y*d.width+x];
  const platform=t=>[4,6,15].includes(t),wall=t=>t>0&&t!==3&&t!==9&&!platform(t),targets=[];
  for(const type of [7,6,15,18]){
   const idx=cells.indexOf(type),tx=idx%d.width,ty=Math.floor(idx/d.width);let best=null;
   for(let y=Math.max(2,ty-8);y<Math.min(d.height,ty+9);y++)for(let x=Math.max(0,tx-10);x<Math.min(d.width,tx+11);x++){
    const t=get(x,y);if(!t||t===3||t===9||wall(get(x,y-1))||wall(get(x,y-2)))continue;
    const score=Math.abs(x-tx)+Math.abs(y-ty)*1.2+(platform(t)?5:0);if(!best||score<best.score)best={x,y,score};
   }
   assert(best,'valid nearby spawn for '+type);targets.push({type,...best});
   await page.evaluate(p=>unityInstance.SendMessage('World Prototype','PreviewPosition',p),`${best.x},${best.y}`);
   await new Promise(r=>setTimeout(r,1800));await page.locator('#unity-canvas').screenshot({path:`Book/Validation/special-tile-${type}-web.png`,timeout:60000});
  }
  assert.deepEqual(errors,[]);fs.writeFileSync('Book/Validation/special-tiles-web.json',JSON.stringify({passed:true,targets,errors},null,2));console.log('SPECIAL_TILES_WEB_OK',targets);
 }finally{await browser.close()}
})().catch(e=>{console.error(e);process.exit(1)});
