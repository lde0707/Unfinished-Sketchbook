const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 try{
  const page=await browser.newPage({viewport:{width:1280,height:1000}}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));
  await page.goto('http://127.0.0.1:8080/enemy-motion-preview.html');
  await page.waitForFunction(()=>ready,{},{timeout:180000});await page.waitForTimeout(6000);
  assert.equal(await page.locator('nav button').count(),13);
  for(const id of ['COMMON01','A02','B03']){
   await page.locator(`[data-id="${id}"]`).click();
   await page.waitForTimeout(250);await page.screenshot({path:`Book/Validation/enemy-motion-${id}-start.png`});
   await page.waitForTimeout(5000);await page.screenshot({path:`Book/Validation/enemy-motion-${id}-strike.png`});
   await page.waitForTimeout(4000);await page.screenshot({path:`Book/Validation/enemy-motion-${id}-end.png`});
  }
  assert.deepEqual(errors,[]);
  fs.writeFileSync('Book/Validation/enemy-motion-web.json',JSON.stringify({passed:true,errors,checks:'Unity selector loads, 13 targets, rolling/hopping/dropping captures; numerical collision checks in EnemyAnimationValidation'},null,2));
  console.log('PASS: Unity motion selector, roll / hop / drop screenshots.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1)});
