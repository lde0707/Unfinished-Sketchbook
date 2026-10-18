const fs=require('node:fs'),path=require('node:path');
const target='Art/samples/room-backdrops-v1';fs.mkdirSync(target+'/rooms',{recursive:true});
for(const f of fs.readdirSync('Book/Validation/room-backdrops'))if(f.endsWith('.png'))fs.copyFileSync('Book/Validation/room-backdrops/'+f,target+'/rooms/'+f);
fs.copyFileSync('Art/room-backdrops-v1/room-layouts.json',target+'/room-layouts.json');
console.log('Packaged room backdrop gallery at '+target);
