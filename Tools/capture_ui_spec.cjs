const fs=require('fs'),path=require('path');
const {chromium}=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright'));
(async()=>{const out='Docs/UI-Spec/build/screens';fs.mkdirSync(out,{recursive:true});const browser=await chromium.launch({executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe',headless:true,args:['--enable-webgl','--use-angle=swiftshader','--enable-unsafe-swiftshader']});try{
const page=await browser.newPage({viewport:{width:1600,height:900}});
page.on('pageerror',e=>console.log('PAGE_ERROR',e.message));
await page.route(u=>u.pathname==='/',async r=>{const response=await r.fetch();await r.fulfill({response,body:(await response.text()).replace('function resizeGameBuffer(){','function resizeGameBuffer(){canvas.width=1600;canvas.height=900;return;')});});
const wait=ms=>page.waitForTimeout(ms);
async function shot(name){await page.mouse.move(1595,895);await wait(600);await page.locator('canvas').screenshot({path:out+'/'+name+'.png'});console.log('CAPTURED',name);}
async function click(x,y){await page.mouse.click(x/1920*1600,y/1080*900);await wait(400);}
async function key(k){await page.keyboard.press(k);await wait(500);}
async function send(m,v){await page.evaluate(({m,v})=>v===undefined?unityInstance.SendMessage('World Prototype',m):unityInstance.SendMessage('World Prototype',m,v),{m,v});}
await page.goto('http://127.0.0.1:8080/?v=ui-spec-20261031');await page.waitForFunction(()=>!!window.unityInstance,{},{timeout:300000});await wait(8000);await page.locator('canvas').focus();
await shot('title');await click(470,744);await shot('graphics');await click(1340,380);await shot('resolution-preview');await click(920,155);await shot('sound');await click(1310,155);await shot('controls');await key('Escape');await click(470,630);await shot('new-empty');await click(1300,900);await wait(6500);await shot('hud');await key('Escape');await shot('pause');await click(960,710);await wait(6500);await shot('title-saved');await click(470,520);await shot('continue');await key('Escape');await click(470,630);await shot('new-overwrite');await key('Escape');await click(470,520);await click(1300,900);await wait(6500);
await send('PreviewPosition','236,131');await wait(1800);await shot('shop-world');await key('KeyE');await shot('shop-entry');await key('Enter');await shot('shop');await key('Escape');await key('Escape');await send('PreviewPosition','10,138');await wait(1800);await key('KeyE');await shot('portal');await key('Escape');await send('PreviewPosition','190,132');await wait(1800);await key('KeyE');await shot('toast');
console.log('UI_SPEC_CAPTURE_OK');
}finally{await browser.close();}})().catch(e=>{console.error(e);process.exit(1)});
