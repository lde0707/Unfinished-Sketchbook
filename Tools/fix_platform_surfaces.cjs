// Reconcile the five painted one-way cells accidentally cleared by the new routes.
const fs=require('node:fs'),vm=require('node:vm'),assert=require('node:assert/strict'),crypto=require('node:crypto'),cp=require('node:child_process');
const sourcePath='Prototype/world_map_data.js';let source=fs.readFileSync(sourcePath,'utf8');
const d=JSON.parse(vm.runInNewContext(source+'\nJSON.stringify({W,H,grid:Array.from(buildMap())})'));
const hash=grid=>crypto.createHash('sha256').update(Buffer.from(grid)).digest('hex');
const beforeHash=hash(d.grid),fixes=[{x:142,y:105},{x:143,y:105},{x:144,y:105},{x:253,y:113},{x:254,y:113}];
const previous=JSON.parse(fs.readFileSync('Book/Assets/Unfin/Data/world12.json'));
assert.equal(previous.gridSha256,beforeHash,'Canonical export must match before repair');
for(const p of fixes){const i=p.y*d.W+p.x;assert([0,4].includes(d.grid[i]),'Unexpected protected terrain');d.grid[i]=4;}
const afterHash=hash(d.grid);if(beforeHash===afterHash){console.log('Platform surfaces already repaired.');process.exit(0)}
const runs=[];let value=d.grid[0],count=0;
for(const t of d.grid){if(t===value)count++;else{runs.push(value.toString(36)+'.'+count.toString(36));value=t;count=1;}}runs.push(value.toString(36)+'.'+count.toString(36));
source=source.replace(/const MAPRLE='[^']*';/,"const MAPRLE='"+runs.join(',')+"';");fs.writeFileSync(sourcePath,source);
const htmlPath='Prototype/unfin_world.html',html=fs.readFileSync(htmlPath,'utf8');
fs.writeFileSync(htmlPath,html.replace(/const MAPRLE='[^']*';/,"const MAPRLE='"+runs.join(',')+"';"));
cp.execFileSync(process.execPath,['Tools/export_unity_world.cjs'],{stdio:'inherit'});
for(const file of ['Book/Assets/Unfin/Data/room-mezzanines.json','Book/Assets/Unfin/Art/Region1V8/manifest.json']){
 const data=JSON.parse(fs.readFileSync(file));assert.equal(data.gridSha256,beforeHash);data.gridSha256=afterHash;fs.writeFileSync(file,JSON.stringify(data,null,2)+'\n');
}
// 0 -> one-way does not alter the ordinary solid mask or its painted geometry.
const terrain='Book/Assets/Unfin/Art/Region1V8/Region1Terrain.asset';let asset=fs.readFileSync(terrain,'utf8');assert(asset.includes(beforeHash));fs.writeFileSync(terrain,asset.replace(beforeHash,afterHash));
const ledger='Book/Assets/Unfin/Resources/ExplorationLayout.json',layout=JSON.parse(fs.readFileSync(ledger));assert.equal(layout.gridSha256,beforeHash);
layout.gridSha256=afterHash;
layout.changes=layout.changes.filter(c=>!fixes.some(p=>p.x===c.x&&p.y===c.y));
layout.platformSurfaceFixes=fixes.map(p=>({...p,before:0,after:4}));fs.writeFileSync(ledger,JSON.stringify(layout,null,2)+'\n');
fs.writeFileSync('Book/Assets/Unfin/Data/platform-surface-fixes.json',JSON.stringify({date:'2026-10-23',beforeGridSha256:beforeHash,gridSha256:afterHash,cells:layout.platformSurfaceFixes,reason:'Restore collision beneath painted one-way platforms; preserve passage traversal from below.'},null,2)+'\n');
console.log(JSON.stringify({restored:fixes.length,gridSha256:afterHash}));
