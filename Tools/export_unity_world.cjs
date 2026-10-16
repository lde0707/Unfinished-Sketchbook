// Run from any directory: node Tools/export_unity_world.cjs
// No third-party dependencies. HTML is authoritative; the standalone map must agree.
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const crypto = require('node:crypto');
const assert = require('node:assert/strict');
const root = path.resolve(__dirname, '..');
const html = fs.readFileSync(path.join(root, 'Prototype/unfin_world.html'), 'utf8');
const start = html.indexOf('const T=16,');
const end = html.indexOf('/* ---------- 플레이 맵: 게임 ---------- */', start);
assert(start >= 0 && end > start, 'Cannot locate HTML map constants');
const expression = '\nJSON.stringify({T,W,H,REGION_X,ZONES,ROOMS,PASS,SHAFTS,ARENA,ARENA2,SPAWNS,PICKS,START,RIBBONS,MAPRLE,grid:Array.from(buildMap())})';
const read = code => JSON.parse(vm.runInNewContext(code + expression, {}, {timeout: 5000}));
const source = html.slice(start, end);
const a = read(source);
const b = read(fs.readFileSync(path.join(root, 'Prototype/world_map_data.js'), 'utf8'));
assert.deepEqual(a, b, 'HTML and standalone world map differ; resolve the source discrepancy first');
assert.equal(a.grid.length, a.W * a.H);
assert.equal(a.MAPRLE.split(',').reduce((n, run) => n + parseInt(run.split('.')[1], 36), 0), a.grid.length);
const hash = data => crypto.createHash('sha256').update(data).digest('hex');
const sizes = {soldier:[12,14,1.35],thrower:[12,14,1.3],bat:[12,10,1.35],drip:[12,8,1.3],turtle:[22,18,1.1],knight:[22,36,1.1],walker:[12,10,1.35],range:[12,14,1.3],fly:[12,10,1.35],elite:[20,28,1.1],boss:[40,40,1.1],ball:[12,12,1.35],mole:[14,12,1.3],spider:[12,10,1.35],blob:[14,12,1.3]};
const solid = (x,y) => { const t = a.grid[y*a.W+x]; return t > 0 && t !== 9; };
const markers = (entries, enemy) => entries.map(([type,tx,floor,options = {}]) => {
  let w,h,x,y;
  if (enemy) {
    const s = sizes[type]; assert(s, `Unknown enemy ${type}`);
    w = Math.round(s[0]*s[2]); h = Math.round(s[1]*s[2]);
    x=tx*a.T+(a.T-w)/2; y=floor*a.T-h;
    if(type==='fly'||type==='spider') y=floor*a.T;
    if(type==='bat') { let u=floor; while(u>0&&!solid(tx,u-1))u--; y=u*a.T; }
  } else {
    w=12; h=type==='bar'?40:['save','lever','target','npc'].includes(type)?24:12;
    x=tx*a.T+2; y=floor*a.T-h;
    if(options.w) {w=options.w;h=options.h;x=tx*a.T+8-w/2;y=floor*a.T-h;}
  }
  return {type,tx,floor,x,y,w,h,optionsJson:JSON.stringify(options)};
});
const output = {
  schema:1,width:a.W,height:a.H,htmlTileSize:a.T,tilePixels:128,pixelsPerUnit:128,
  referenceWidth:1920,referenceHeight:1080,orthographicSize:1080/(128*2),regionX:a.REGION_X,
  source:'Prototype/unfin_world.html',sourceSha256:hash(source),gridSha256:hash(Buffer.from(a.grid)),
  startX:a.START.tx,startFloor:a.START.floor,mapRle:a.MAPRLE,
  zones:Object.entries(a.ZONES).map(([id,v])=>({id,...v})),rooms:a.ROOMS,passages:a.PASS,shafts:a.SHAFTS,
  enemies:markers(a.SPAWNS,true),pickups:markers(a.PICKS,false),
  originalMetadataJson:JSON.stringify({ARENA:a.ARENA,ARENA2:a.ARENA2,RIBBONS:a.RIBBONS,SPAWNS:a.SPAWNS,PICKS:a.PICKS})
};
const dest=path.join(root,'Book/Assets/Unfin/Data');
fs.mkdirSync(dest,{recursive:true});
fs.writeFileSync(path.join(dest,'world12.json'),JSON.stringify(output,null,2)+'\n');
console.log(JSON.stringify({dimensions:[a.W,a.H],cells:a.grid.length,tiles:a.grid.filter(Boolean).length,rooms:a.ROOMS.length,shafts:a.SHAFTS.length,enemies:a.SPAWNS.length,pickups:a.PICKS.length,gridSha256:output.gridSha256,htmlStandaloneEqual:true},null,2));
