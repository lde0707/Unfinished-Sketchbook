"""Package complete imagegen drawings: v6-based eleven-frame run, no limb warp."""
from pathlib import Path
from PIL import Image, ImageDraw
import json,hashlib
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Art/animations/player-c-v8'
# label, sheet, cell, baseline index, duration, support, forward arm, smear
PLAN=[
 ('1','a',0,0,100,'far','near',False),
 ('2','c',0,1,100,None,'near',False),
 ('3','a',2,2,100,None,'far','backward'),
 ('4','a',3,3,50,'near','far',False),
 ('4-5','c',1,3,50,'near','far',False),
 ('5','b',0,4,100,'near','far',False),
 ('6','b',1,5,50,None,'near','forward'),
 ('6-7','c',2,5,50,None,'near',False),
 ('7','b',2,6,100,None,'near',False),
 ('8','b',3,7,50,'far','near',False),
 ('8-1','c',3,7,50,'far','near',False)]
def bounds(im,threshold=128):
 return im.getchannel('A').point(lambda v:255 if v>=threshold else 0).getbbox()
def main():
 (OUT/'frames').mkdir(exist_ok=True)
 base=json.loads((ROOT/'Art/animations/player-c-v6/registration.json').read_text())
 frames=[];metadata=[]
 for n,(label,key,cell,bi,duration,support,frontarm,smear) in enumerate(PLAN):
  source=Image.open(OUT/'sources'/f'keys-{key}.png').convert('RGBA')
  w,h=source.width//2,source.height//2
  rect=(cell%2*w,cell//2*h,(cell%2+1)*w,(cell//2+1)*h)
  im=source.crop(rect);b=bounds(im);a=im.getchannel('A')
  crown=a.crop((0,b[1],w,b[1]+25)).point(lambda v:255 if v>=128 else 0).getbbox()
  anchor=round((crown[0]+crown[2])/2);spans=[]
  for y in range(b[1]+50,b[1]+130):
   if a.getpixel((anchor,y))<128:continue
   l=r=anchor
   while l>0 and a.getpixel((l-1,y))>=128:l-=1
   while r<w-1 and a.getpixel((r+1,y))>=128:r+=1
   spans.append((r-l+1,(l+r+1)/2))
  hw,hx=max(spans);hy=b[1]+hw/2;scale=160/hw
  sprite=im.resize((round(w*scale),round(h*scale)),Image.Resampling.LANCZOS)
  sb=bounds(sprite);tx=round(350-hx*scale)
  target=base['frames'][bi]['headCenter'][1]
  if label=='6-7':target=205
  ty=round(target-hy*scale)
  if support:ty=448-sb[3]
  else:ty=min(ty,442-sb[3])
  out=Image.new('RGBA',(512,512));out.alpha_composite(sprite,(tx,ty))
  bb=bounds(out)
  assert 0<sb[0]+tx<sb[2]+tx<512 and 0<sb[1]+ty<sb[3]+ty<512,(label,sb,tx,ty)
  assert bb[3]<=448,(label,bb)
  out.save(OUT/'frames'/f'{n:02}.png');frames.append(out)
  metadata.append(dict(index=n,label=label,source=f'keys-{key}.png',sourceRect=rect,baselineFrame=bi+1,durationMs=duration,inserted='-' in label,headCenter=[350,round(hy*scale+ty)],headWidth=160,scale=scale,translation=[tx,ty],bounds=bb,footGap=448-bb[3],supportLeg=support,forwardArm=frontarm,armSmear=smear))
 atlas=Image.new('RGBA',(2048,1536));sheet=Image.new('RGB',(1280,1050),'#242a30');draw=ImageDraw.Draw(sheet);gif=[]
 for i,im in enumerate(frames):
  atlas.alpha_composite(im,((i%4)*512,(i//4)*512));thumb=im.resize((320,320),Image.Resampling.LANCZOS);x=i%4*320;y=i//4*350;sheet.paste(thumb,(x,y),thumb)
  draw.line((x+12,y+280,x+308,y+280),fill='#667683')
  draw.text((x+14,y+322),metadata[i]['label']+('  NEW' if metadata[i]['inserted'] else '')+('  ARM SMEAR' if metadata[i]['armSmear'] else ''),fill='#e9decb')
  bg=Image.new('RGBA',(512,512),'#242a30');bg.alpha_composite(im);gif.append(bg.convert('RGB'))
 atlas.save(OUT/'run-atlas.png');sheet.save(OUT/'run-contact-sheet.jpg',quality=95)
 pal=gif[0].quantize(colors=240);g=[f.quantize(palette=pal,dither=Image.Dither.NONE) for f in gif]
 g[0].save(OUT/'run-preview.gif',save_all=True,append_images=g[1:],duration=[r['durationMs'] for r in metadata],loop=0,disposal=2,optimize=False)
 data=dict(baseVersion='v6',frameCount=11,inbetweenCount=3,loopMs=sum(r['durationMs'] for r in metadata),timing='Original 800ms loop; intervals4,6,8 split into50ms+50ms',cellSize=512,columns=4,rows=3,groundY=448,pivot=base['pivot'],frames=metadata)
 assert data['loopMs']==800
 (OUT/'registration.json').write_text(json.dumps(data,indent=2),encoding='utf-8')
 result=dict(frameCount=11,uniqueFrames=len(set(hashlib.sha256(f.tobytes()).hexdigest() for f in frames)),labels=[r['label'] for r in metadata],headY=[r['headCenter'][1] for r in metadata],footGaps=[r['footGap'] for r in metadata],contactErrorPx=max(r['footGap'] for r in metadata if r['supportLeg']),clippedSilhouettes=0,loopMs=800,note='Geometry checks only; leg/arm continuity reviewed visually, not established by metadata.')
 assert result['uniqueFrames']==11 and result['contactErrorPx']==0
 (OUT/'validation.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
 print(json.dumps(result))
if __name__=='__main__':main()

