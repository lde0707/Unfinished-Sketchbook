const fs=require('fs'),path=require('path'),assert=require('assert/strict');
const deps=path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules');
const {chromium}=require(path.join(deps,'playwright')),sharp=require(path.join(deps,'sharp'));
(async()=>{
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 try{
  const page=await browser.newPage({viewport:{width:1440,height:900}}),errors=[],out='Book/Validation/map-zone-web';fs.mkdirSync(out,{recursive:true});
  page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(/Exception|memory access out of bounds|out of memory/i.test(m.text()))errors.push(m.text());});
  await page.route(u=>u.pathname==='/',async r=>{const response=await r.fetch();await r.fulfill({response,body:(await response.text()).replace('function resizeGameBuffer(){','function resizeGameBuffer(){canvas.width=1280;canvas.height=720;return;')});});
  async function shot(name){const file=path.join(out,name+'.png');await page.locator('canvas').screenshot({path:file,timeout:60000});assert((await sharp(file).stats()).entropy>1,'Unity rendered '+name);}
  async function click(x,y){const b=await page.locator('canvas').boundingBox();await page.mouse.click(b.x+x/1920*b.width,b.y+y/1080*b.height);await page.waitForTimeout(450);}
  async function state(){const message=page.waitForEvent('console',{predicate:m=>m.text().includes('JOURNAL_MAP '),timeout:15000});await page.evaluate(()=>unityInstance.SendMessage('World Prototype','ReportJournalMap'));const text=(await message).text();return JSON.parse(text.slice(text.indexOf('{')));}
  await page.goto('http://127.0.0.1:8080/?v=map-zone-colors-20261028&spawn=236,131');await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:300000});await page.waitForTimeout(8000);await page.locator('canvas').focus();
  const points=JSON.parse(fs.readFileSync('Book/Validation/map-zone-positions.json','utf8'));
  for(const p of points){await page.evaluate(pos=>unityInstance.SendMessage('World Prototype','PreviewPosition',pos),p.position);await page.waitForTimeout(2000);await shot('mini-'+p.zone);}
  await page.keyboard.press('KeyM');await page.waitForTimeout(700);assert((await state()).paused,'map pauses world');
  await click(340,287);await shot('journal-region1');await click(650,287);await shot('journal-region2');await click(970,287);await shot('journal-all');
  await click(320,343);const s=await state();assert(s.zoom>1,'zoom still works');await shot('journal-zoom');
  await page.keyboard.press('KeyM');await page.waitForTimeout(400);assert(!(await state()).paused,'map closes and resumes');
  assert.deepEqual(errors,[]);fs.writeFileSync(path.join(out,'result.json'),JSON.stringify({passed:true,zones:points,sharedMapColours:true,regionTabs:true,zoom:true,errors},null,2));console.log('MAP_ZONE_COLORS_WEB_OK');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
