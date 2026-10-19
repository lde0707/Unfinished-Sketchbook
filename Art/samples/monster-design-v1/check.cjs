const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true});try{
 const page=await browser.newPage({viewport:{width:1440,height:1000}}),errors=[];page.on('pageerror',e=>errors.push(e.message));
 await page.goto('http://127.0.0.1:8093/monster-design-v1/preview.html');
 assert.equal(await page.locator('article').count(),33);assert.equal(await page.locator('section').count(),11);
 for(const img of await page.locator('.sheet').all()){await img.scrollIntoViewIfNeeded();await img.evaluate(x=>x.decode());assert(await img.evaluate(x=>x.naturalWidth>1000));}
 for(const a of await page.locator('a[href^="map-originals/"]').all()){const r=await page.request.get('http://127.0.0.1:8093/monster-design-v1/'+await a.getAttribute('href'));assert.equal(r.status(),200);}
 await page.selectOption('#region','1');assert.equal(await page.locator('section:visible').count(),6);
 await page.selectOption('#scope','구역 전용');assert.equal(await page.locator('section:visible').count(),4);
 await page.selectOption('#region','2');assert.equal(await page.locator('section:visible').count(),4);
 await page.selectOption('#scope','지역 전용');assert.equal(await page.locator('section:visible').count(),1);
 await page.selectOption('#region','all');await page.selectOption('#scope','all');await page.evaluate(()=>window.scrollTo(0,0));
 await page.screenshot({path:path.join(__dirname,'catalog-check.png')});assert.deepEqual(errors,[]);
 const file=path.join(__dirname,'validation.json'),v=JSON.parse(fs.readFileSync(file));v.browser={cards:33,loadedImages:11,originalLinks:11,filtersPassed:true,errors};fs.writeFileSync(file,JSON.stringify(v,null,2));console.log('PASS: 33 cards, 11 map images, 11 original links, region/scope filters.');
}finally{await browser.close();}})().catch(e=>{console.error(e);process.exit(1)});
