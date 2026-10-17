// Preserve full-quality Unity data while meeting static hosting's 25 MiB per-file limit.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../Builds/WebGLSite/dist'),index=path.join(root,'index.html');
let html=fs.readFileSync(index,'utf8');
if(!html.includes('rel="icon"')){html=html.replace('</head>','<link rel="icon" href="data:,">\n</head>');fs.writeFileSync(index,html);}
if(html.includes('async function startChunkedUnity')){console.log('Unity data already split.');process.exit(0);}
const match=html.match(/dataUrl:'(Build\/[^']+)'/);if(!match)throw Error('Unity data URL missing');
const dataFile=path.join(root,match[1]),bytes=fs.readFileSync(dataFile);
if(bytes.length<=25*1024*1024){console.log('Unity data fits without splitting.');process.exit(0);}
const priorPath=path.join(root,'unity-data-parts.json');
const prior=fs.existsSync(priorPath)?JSON.parse(fs.readFileSync(priorPath,'utf8')):null;
const parts=[];
for(let at=0;at<bytes.length;at+=16*1024*1024){
 const buffer=bytes.subarray(at,at+16*1024*1024),file=match[1]+'.part'+parts.length;
 fs.writeFileSync(path.join(root,file),buffer);parts.push({file,bytes:buffer.length,integrity:'sha256-'+crypto.createHash('sha256').update(buffer).digest('base64')});
}
const manifest={source:match[1],bytes:bytes.length,sha256:crypto.createHash('sha256').update(bytes).digest('hex'),parts};
fs.writeFileSync(path.join(root,'unity-data-parts.json'),JSON.stringify(manifest,null,2)+'\n');
const helper=`
  async function startChunkedUnity(canvas,config,onProgress){
    const parts=${JSON.stringify(parts)};let loaded=0;
    const buffers=await Promise.all(parts.map(async part=>{
      const response=await fetch(part.file,{integrity:part.integrity});
      if(!response.ok)throw Error('게임 데이터 다운로드 실패: '+response.status);
      const buffer=await response.arrayBuffer();loaded+=buffer.byteLength;onProgress(.4*loaded/${bytes.length});return buffer;
    }));
    const dataUrl=URL.createObjectURL(new Blob(buffers,{type:'application/octet-stream'}));
    try{return await createUnityInstance(canvas,{...config,dataUrl},p=>onProgress(.4+.6*p));}
    finally{URL.revokeObjectURL(dataUrl);}
  }
`;
if(!html.includes('loader.onload=()=>createUnityInstance('))throw Error('Unexpected Unity template');
html=html.replace('<script>','<script>'+helper).replace('loader.onload=()=>createUnityInstance(','loader.onload=()=>startChunkedUnity(');
fs.writeFileSync(index,html);fs.unlinkSync(dataFile);
for(const part of prior?.parts||[]){
 if(parts.some(p=>p.file===part.file))continue;
 if(!/^Build\/[a-f0-9]{32}\.data\.unityweb\.part\d+$/.test(part.file))throw Error('Unexpected previous data part path');
 const old=path.resolve(root,part.file);
 if(path.dirname(old)!==path.join(root,'Build'))throw Error('Data part escaped build folder');
 if(fs.existsSync(old))fs.unlinkSync(old);
}
console.log(`Split ${bytes.length} bytes into ${parts.length} lossless parts.`);
