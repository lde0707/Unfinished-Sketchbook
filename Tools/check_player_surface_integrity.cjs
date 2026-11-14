const fs=require('fs'),crypto=require('crypto'),assert=require('assert/strict');
const baseline=JSON.parse(fs.readFileSync('Builds/player-surface-backup/source-hashes.json','utf8')),changed=[];
for(const [file,hash] of Object.entries(baseline)){const actual=crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');if(actual!==hash)changed.push(file);}
assert.deepEqual(changed,[],'Original drawings, metadata, timing library, life registration and movement constants must stay byte-identical');
const native=JSON.parse(fs.readFileSync('Book/Validation/player-surface.json'));assert.equal(native.passed,true);assert.equal(native.drawings,102);assert.equal(native.alphaMismatchPixels,0);
fs.writeFileSync('Book/Validation/player-surface-integrity.json',JSON.stringify({passed:true,unchangedFiles:Object.keys(baseline).length,changedFiles:changed,scope:'Original player PNGs/metas/manifests, life atlases and registration, animation library and movement constants',renderedDrawings:native.drawings},null,2));console.log('PLAYER_SURFACE_INTEGRITY_OK',Object.keys(baseline).length,'unchanged source files');
