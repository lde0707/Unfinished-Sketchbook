"""Preserve v8 pixels/timing exactly; add whole-sprite run registration offsets."""
from pathlib import Path
from PIL import Image,ImageDraw
import json,hashlib,shutil
ROOT=Path(__file__).resolve().parents[1];BASE=ROOT/'Art/animations/player-c-v8';OUT=ROOT/'Art/animations/player-c-v9'
# Grounded poses retain y. Flight is lower, horizontal advance leads the small bob.
OFFSETS=[(-4,0),(6,6),(10,2),(6,0),(1,0),(-4,0),(6,4),(9,8),(10,6),(6,0),(1,0)]
WIDTH=544;HEIGHT=512;PAD=16
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
 (OUT/'frames').mkdir(parents=True,exist_ok=True)
 old=json.loads((BASE/'registration.json').read_text());meta=json.loads(json.dumps(old));checks=[];rendered=[]
 for m,(dx,dy) in zip(meta['frames'],OFFSETS):
  src=BASE/'frames'/f"{m['index']:02}.png";dst=OUT/'frames'/src.name;shutil.copy2(src,dst)
  assert sha(src)==sha(dst)
  im=Image.open(dst).convert('RGBA');b=im.getchannel('A').getbbox()
  assert b[0]+PAD+dx>=0 and b[2]+PAD+dx<=WIDTH and b[1]+dy>=0 and b[3]+dy<=HEIGHT
  out=Image.new('RGBA',(WIDTH,HEIGHT));out.alpha_composite(im,(PAD+dx,dy));rendered.append(out)
  # Padding is only for exports/preview; offsets remain relative to the v8 pivot.
  m['renderOffset']=[dx,dy];m['renderedHeadCenter']=[m['headCenter'][0]+dx,m['headCenter'][1]+dy]
  m['renderedFootGap']=m['footGap']-dy
  assert m['renderedFootGap']>=0
  if m['supportLeg']:assert m['renderedFootGap']==0
  checks.append(dict(label=m['label'],sourceSha256=sha(src),outputSha256=sha(dst),pixelsIdentical=True,offset=[dx,dy],footGap=m['renderedFootGap']))
 meta.update(baseVersion='v8',poseOrigin='v6-based v8 eleven drawings',framePixels='Byte-identical v8 PNG files. Apply renderOffset to the whole sprite; atlas alone is unchanged.',renderCanvas=[WIDTH,HEIGHT],renderPaddingX=PAD,motion='Integer whole-sprite translation only; grounded y preserved; flight2/6 lower with forward advance. No rig, redraw, rescale, rotation or arm/leg timing edits.')
 shutil.copy2(BASE/'run-atlas.png',OUT/'run-atlas.png')
 (OUT/'registration.json').write_text(json.dumps(meta,indent=2),encoding='utf-8')
 sheet=Image.new('RGB',(1360,1050),'#242a30');d=ImageDraw.Draw(sheet);gif=[]
 for i,im in enumerate(rendered):
  thumb=im.resize((340,320),Image.Resampling.LANCZOS);x=i%4*340;y=i//4*350;sheet.paste(thumb,(x,y),thumb)
  d.line((x+12,y+280,x+328,y+280),fill='#667683');d.text((x+14,y+322),meta['frames'][i]['label']+'  OFFSET '+str(OFFSETS[i]),fill='#e9decb')
  bg=Image.new('RGBA',im.size,'#242a30');bg.alpha_composite(im);gif.append(bg.convert('RGB'))
 sheet.save(OUT/'run-contact-sheet.jpg',quality=95);pal=gif[0].quantize(colors=240);g=[im.quantize(palette=pal,dither=Image.Dither.NONE) for im in gif]
 g[0].save(OUT/'run-preview.gif',save_all=True,append_images=g[1:],duration=[m['durationMs'] for m in meta['frames']],loop=0,disposal=2,optimize=False)
 result=dict(passed=True,frameCount=11,allFrameFilesByteIdenticalToV8=True,atlasByteIdenticalToV8=sha(OUT/'run-atlas.png')==sha(BASE/'run-atlas.png'),timingUnchanged=True,loopMs=800,noNonzeroAlphaClipped=True,noGroundPenetration=True,headX=[m['renderedHeadCenter'][0] for m in meta['frames']],headY=[m['renderedHeadCenter'][1] for m in meta['frames']],frames=checks,note='Geometry and identity checks do not establish user approval of motion.')
 (OUT/'validation.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
 print(json.dumps({k:v for k,v in result.items() if k!='frames'}))
if __name__=='__main__':main()

