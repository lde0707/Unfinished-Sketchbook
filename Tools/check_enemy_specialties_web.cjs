const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
const data=JSON.parse(fs.readFileSync('Book/Assets/Unfin/Data/world12.json','utf8').replace(/^\uFEFF/,''));
const output='Book/Validation/enemy-specialties';fs.mkdirSync(output,{recursive:true});
(async()=>{
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 try{
 const page=await browser.newPage({viewport:{width:1280,height:720}}),errors=[],observations=[];
 page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(/Exception|memory access out of bounds|SendMessage.*no receiver/i.test(m.text()))errors.push(m.text());});
 await page.goto('http://127.0.0.1:8080/?v=enemy-specialties-20261026&spawn=169,108');await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:300000});await page.locator('canvas').click();await page.waitForTimeout(9000);
 function objectName(i){const e=data.enemies[i];return `${String(i).padStart(3,'0')} ${e.type} (${e.tx}, ${e.floor})`;}
 async function state(i){const done=page.waitForEvent('console',{predicate:m=>m.text().includes('ENEMY_SPECIALTY '),timeout:20000});await page.evaluate(n=>unityInstance.SendMessage(n,'ReportSpecialty'),objectName(i));const m=(await done).text();return JSON.parse(m.slice(m.indexOf('{')));}
 async function shot(name){const data=await page.evaluate(async()=>{const c=document.querySelector('canvas'),p=document.createElement('canvas');p.width=64;p.height=36;const ctx=p.getContext('2d');for(let i=0;i<120;i++){await new Promise(requestAnimationFrame);ctx.drawImage(c,0,0,64,36);if(ctx.getImageData(0,0,64,36).data.some((v,j)=>j%4!==3&&v>30))return c.toDataURL();}throw Error('blank');});fs.writeFileSync(path.join(output,name+'.png'),Buffer.from(data.split(',')[1],'base64'));}
 async function place(pos){await page.evaluate(pos=>unityInstance.SendMessage('World Prototype','PreviewPosition',pos),pos);await page.waitForTimeout(2000);}
 for(const [i,id,pos,label] of [[11,'R103','169,108','front-guard'],[12,'A03','179,108','page-shell'],[14,'B01','163,132','burrow'],[41,'D01','231,14','pursuit'],[48,'D03','235,70','ground-sweep']]){
  await place(pos);const a=await state(i);assert.equal(a.species,id);assert(a.strength&&a.counter);await page.waitForTimeout(1800);let b=await state(i);for(let n=0;n<28&&b.state!=='Attack';n++){await page.waitForTimeout(650);b=await state(i);}assert.equal(b.state,'Attack',id+' actually engages in the mapped room');assert(Number.isFinite(b.x)&&Number.isFinite(b.clock));await shot(label);observations.push({a,b});
 }
 // Pause the actual world and verify enemy action clock and position stay fixed.
 await place('169,108');await page.keyboard.press('KeyM',{delay:200});await page.waitForTimeout(1200);const before=await state(11);await page.waitForTimeout(1200);const after=await state(11);assert.equal(before.clock,after.clock);assert.equal(before.x,after.x);await page.keyboard.press('KeyM',{delay:200});
 assert.deepEqual(errors,[]);fs.writeFileSync('Book/Validation/enemy-specialties-web.json',JSON.stringify({passed:true,checks:['deployed species profiles','five species actually enter attack in mapped rooms','finite enemy positions and clocks','map freezes enemies'],observations,errors},null,2));console.log('ENEMY_SPECIALTIES_WEB_OK');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1)});
