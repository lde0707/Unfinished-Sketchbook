const fs=require('fs'),path=require('path');const root=path.resolve(__dirname,'..'),dest=path.join(root,'Builds/WebGLSite/dist'),source=path.join(root,'Book/Assets/Unfin/Resources');
function cp(from,to){fs.mkdirSync(path.dirname(to),{recursive:true});fs.copyFileSync(from,to);}
for(const name of ['PlayerReactions','Region1Elites'])fs.cpSync(path.join(source,name),path.join(dest,'animation-completion',name),{recursive:true,filter:p=>!p.endsWith('.meta')});
const native=path.join(root,'Book/Validation/animation-completion');if(fs.existsSync(native))fs.cpSync(native,path.join(dest,'animation-completion/screens'),{recursive:true});
cp(path.join(root,'Art/samples/animation-completion-v1/preview.html'),path.join(dest,'animation-completion.html'));console.log('ANIMATION_COMPLETION_PREVIEW_INSTALLED');
