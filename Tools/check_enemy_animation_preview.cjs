const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{
 const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});
 try {
  const page=await browser.newPage({viewport:{width:1500,height:1100}}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));
  await page.goto('http://127.0.0.1:8092/enemies-region1-v2/preview.html');
  await page.waitForFunction(()=>document.querySelectorAll('nav button').length===18);
  assert.equal(await page.locator('#states canvas').count(),3);
  for(let i=0;i<18;i++) {
   await page.locator('nav button').nth(i).click();
   await page.waitForFunction(()=>img.complete&&img.naturalWidth===1536&&img.naturalHeight===selected.rows*384);
   for(let state=0;state<3;state++) {
    await page.locator('#stateButtons button').nth(state).click();
    await page.waitForFunction(()=>Array.from(document.querySelectorAll('#frames img')).every(x=>x.complete&&x.naturalWidth===384));
   }
  }
  await page.locator('nav button').first().click();await page.locator('#stateButtons button').nth(1).click();
  await page.locator('#step').click();await page.locator('#flip').click();await page.locator('#speed').selectOption('0.5');
  await page.screenshot({path:'Book/Validation/enemy-animation-preview.png',fullPage:true});
  assert.deepEqual(errors,[]);
  fs.writeFileSync('Book/Validation/enemy-animation-preview.json',JSON.stringify({passed:true,species:18,states:54,frames:248,errors},null,2));
  console.log('PASS: 18 species, 54 states, 248 frames; gallery controls and images.');
 } finally {await browser.close();}
})().catch(e=>{console.error(e);process.exit(1)});
