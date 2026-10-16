// Run after Region1BackgroundImporter.ApplyAndValidate and RenderTransitions.
const fs=require('node:fs'), path=require('node:path'), assert=require('node:assert/strict');
let sharp; try { sharp=require('sharp'); } catch {
  sharp=require(path.join(process.env.USERPROFILE,'.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp'));
}
const root=path.resolve(__dirname,'..'), validation=path.join(root,'Book/Validation');
async function run() {
  const manifest=JSON.parse(fs.readFileSync(path.join(validation,'region1-background-v3.json')));
  const map=JSON.parse(fs.readFileSync(path.join(root,'Book/Assets/Unfin/Data/world12.json')));
  assert.equal(manifest.gridSha256,map.gridSha256);
  for(const name of ['A-paper-wall','B-graphite-wall','C-wood-wall','D-root-wall']) {
    assert(fs.readFileSync(path.join(root,'Art/samples/region1-backgrounds-v3/tiles',name+'.png'))
      .equals(fs.readFileSync(path.join(root,'Book/Assets/Unfin/Art/Region1BackgroundV3',name+'.png'))), 'Source art changed');
  }
  const report={};
  for(const name of ['A-B','C-A','C-D','D-A']) {
    const buffers=[];
    for(const suffix of ['', '-moved']) {
      const file=path.join(validation,'background-v3-transition-'+name+suffix+'.png');
      const metadata=await sharp(file).metadata(); assert.equal(metadata.width,1920); assert.equal(metadata.height,1080);
      buffers.push(await sharp(file).removeAlpha().raw().toBuffer());
    }
    const [a,b]=buffers, horizontal=name==='C-D'; let sum=0,max=0,count=0;
    // Moving the camera by one unit must shift the same scene by exactly 128px.
    // Any screen-anchored region transition or material phase breaks this check.
    for(let y=0;y<1080-(horizontal?0:128);y++) for(let x=horizontal?128:0;x<1920;x++) for(let c=0;c<3;c++) {
      const ai=(y*1920+x)*3+c, bi=((y+(horizontal?0:128))*1920+x-(horizontal?128:0))*3+c;
      const difference=Math.abs(a[ai]-b[bi]); sum+=difference; max=Math.max(max,difference); count++;
    }
    report[name]={cameraMoveUnits:1,meanAlignedPixelDifference:sum/count,maxAlignedPixelDifference:max};
    assert(sum/count<0.05,'Background drift at '+name);
  }
  report.world2Unchanged=fs.readFileSync(path.join(validation,'background-v3-world2.png'))
    .equals(fs.readFileSync(path.join(validation,'background-v3-world2-baseline.png')));
  assert(report.world2Unchanged,'Region 1 backdrop altered World 2');
  fs.writeFileSync(path.join(validation,'background-v3-camera-validation.json'),JSON.stringify(report,null,2)+'\n');
  console.log(JSON.stringify(report,null,2));
}
run().catch(error=>{ console.error(error); process.exitCode=1; });
