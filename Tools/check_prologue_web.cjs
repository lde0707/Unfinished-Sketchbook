const fs=require('fs'),path=require('path'),assert=require('assert/strict');
const deps='C:/Users/doun0/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules';
const {chromium}=require(path.join(deps,'playwright')),sharp=require(path.join(deps,'sharp'));
(async()=>{const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});try{
 const page=await browser.newPage({viewport:{width:1280,height:720}}),errors=[],reports=[];
 const out='Book/Validation/prologue';fs.mkdirSync(out,{recursive:true});
 page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(/Exception|Shader.*error|out of memory|memory access out of bounds/i.test(m.text()))errors.push(m.text());});
 const send=(object,method,value='')=>page.evaluate(({object,method,value})=>unityInstance.SendMessage(object,method,value),{object,method,value});
 async function state(){const log=page.waitForEvent('console',{predicate:m=>m.text().startsWith('STILLDRAWN_OPENING '),timeout:60000});await send('World Prototype','ReportOpening');const text=(await log).text();const value=JSON.parse(text.slice(text.indexOf('{')).split('\n')[0]);reports.push(value);return value;}
 async function waitScene(prologue){for(let n=0;n<35;n++){await page.waitForTimeout(700);const s=await state();if(s.prologue===prologue)return s;}throw Error('scene transition timeout');}
 async function capture(name){const file=out+'/web-'+name+'.png';await page.locator('#unity-canvas').screenshot({path:file});assert((await sharp(file).stats()).entropy>1,'visible render '+name);}
 async function walk(until){await page.locator('#unity-canvas').focus();await page.keyboard.down('d');let s;try{for(let i=0;i<180;i++){await page.waitForTimeout(350);s=await state();if(until(s))return s;if(s.prologue&&s.x>16&&s.x<20.5)await page.keyboard.press('Space');}throw Error('walk route timed out: '+JSON.stringify(s));}finally{await page.keyboard.up('d');}}
 await page.goto('http://127.0.0.1:8080/?v=separate-prologue-20261104');await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:300000});await page.waitForTimeout(3000);
 await send('Stilldrawn Title','StartNewGame');let s=await waitScene(true);assert.equal(s.scene,'StilldrawnPrologue');await page.waitForTimeout(2500);await capture('opening');
 s=await walk(s=>s.prologue&&s.x>=28);await page.waitForTimeout(2700);s=await state();assert.equal(s.mask&3,3);const savedX=s.x;await capture('tree-erased');
 await send('Stilldrawn Title','ReturnToTitle');s=await waitScene(false);assert(s.savedPrologue,'title retains prologue bookmark');await send('Stilldrawn Title','ContinueGame');s=await waitScene(true);assert(Math.abs(s.x-savedX)<1,'resume position');assert.equal(s.mask&3,3,'resume disappearance flags');await capture('resumed');
 await walk(s=>!s.prologue);await page.waitForTimeout(1800);s=await state();assert.equal(s.scene,'World12Prototype');assert(!s.savedPrologue);assert(s.x>=243&&s.x<250,'original region 1 start');await capture('region1-arrival');
 await send('Stilldrawn Title','ReturnToTitle');await waitScene(false);await send('Stilldrawn Title','ContinueGame');await page.waitForTimeout(2000);s=await state();assert(!s.prologue&&!s.savedPrologue,'main-world continue skips completed opening');
 assert.deepEqual(errors,[]);fs.writeFileSync('Book/Validation/prologue-web.json',JSON.stringify({passed:true,newGame:true,realKeyboardRoute:true,prologueResume:true,region1Arrival:true,mainContinue:true,reports,errors},null,2));console.log('PROLOGUE_WEB_OK');
 }finally{await browser.close();}})().catch(e=>{console.error(e);process.exitCode=1});
