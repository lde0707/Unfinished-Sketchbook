// Export generated art into engine-ready sizes; this does not synthesize the artwork.
const fs = require('node:fs');
const path = require('node:path');
let sharp;
try { sharp = require('sharp'); } catch {
  sharp = require(process.env.UNFIN_SHARP_PATH || path.join(process.env.USERPROFILE, '.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp'));
}
const root = __dirname;
const spec = JSON.parse(fs.readFileSync(path.join(root, 'prompts.json'), 'utf8'));
async function main() {
  const checks = [], panels = [];
  fs.mkdirSync(path.join(root, 'layers'), {recursive:true});
  for (let r = 0; r < spec.regions.length; r++) {
    const region = spec.regions[r];
    for (const kind of ['floor','wall']) {
      const entry = region[kind], src = path.join(root, 'sources', entry.file);
      const meta = await sharp(src).metadata();
      let result;
      if (kind === 'floor') {
        const capHeight = Math.round(meta.height * 4 / 256);
        const cap = await sharp(src).extract({left:0,top:0,width:meta.width,height:capHeight}).resize(256,4).removeAlpha().png().toBuffer();
        const body = await sharp(src).extract({left:0,top:capHeight,width:meta.width,height:meta.height-capHeight}).resize(256,252).removeAlpha().png().toBuffer();
        await sharp(cap).toFile(path.join(root,'layers',region.id+'-surface-4px.png'));
        await sharp(body).toFile(path.join(root,'layers',region.id+'-front-252px.png'));
        result = await sharp({create:{width:256,height:256,channels:3,background:'#050509'}}).composite([{input:cap,left:0,top:0},{input:body,left:0,top:4}]).removeAlpha().png().toBuffer();
      } else result = await sharp(src).resize(256,256).removeAlpha().png().toBuffer();
      fs.writeFileSync(path.join(root,entry.file),result);
      const m = await sharp(result).metadata();
      if(m.width!==256||m.height!==256) throw new Error('Invalid master size');
      const raw = await sharp(result).removeAlpha().raw().toBuffer();
      let total=0,count=0;
      for(let y=0;y<256;y++)for(let x=0;x<256;x++)if(kind==='floor'?y>=160:x<96){const i=(y*256+x)*3;total+=raw[i]*.2126+raw[i+1]*.7152+raw[i+2]*.0722;count++;}
      if (total/count > 16) throw new Error(entry.file+': ground interior is too bright');
      checks.push({file:entry.file,width:256,height:256,capPixels:kind==='floor'?4:null,deepInteriorMeanLuma:+(total/count).toFixed(2)});
      panels.push({input:result,left:r*256,top:kind==='floor'?0:256});
    }
  }
  await sharp({create:{width:512,height:512,channels:3,background:'#151923'}}).composite(panels).png().toFile(path.join(root,'active-region-pairs.png'));
  fs.writeFileSync(path.join(root,'validation.json'),JSON.stringify(checks,null,2)+'\n');
  console.log(JSON.stringify(checks,null,2));
}
main().catch(error=>{console.error(error);process.exitCode=1;});
