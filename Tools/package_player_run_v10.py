"""Thirteen complete drawings; whole-sprite registration retains v9 movement."""
from pathlib import Path
from PIL import Image,ImageDraw
import json,hashlib,shutil
R=Path(__file__).resolve().parents[1];O=R/'Art/animations/player-c-v10';B=R/'Art/animations/player-c-v9'
# source key, cell or existing v9 index, reference index, duration, new
PLAN=[(f'phase-{i//4}',i%4,i,d,i in (4,11)) for i,d in enumerate([100,100,100,30,40,30,100,50,50,100,30,40,30])]
def bounds(im):return im.getchannel('A').point(lambda v:255 if v>=128 else 0).getbbox()
def main():
 (O/'frames').mkdir(exist_ok=True);base=json.loads((O/'sources/pose-registration.json').read_text());frames=[];rows=[]
 for i,(key,cell,bi,duration,new) in enumerate(PLAN):
  if i==8:key,cell='transitions',2
  if i==9:key,cell='color-fix',1
  prior=base['frames'][bi];dx,dy=prior['renderOffset'];support=prior['supportLeg']
  if new:dx=4;dy=0
  dst=O/'frames'/f'{i:02}.png'
  if key=='old':
   shutil.copy2(B/'frames'/f'{cell:02}.png',dst);im=Image.open(dst).convert('RGBA');head=prior['headCenter']
  else:
   source=Image.open(O/'sources'/f'{key}.png').convert('RGBA');w,h=source.width//2,source.height//2
   im=source.crop((cell%2*w,cell//2*h,(cell%2+1)*w,(cell//2+1)*h));b=bounds(im);a=im.getchannel('A')
   crown=a.crop((0,b[1],w,b[1]+20)).point(lambda v:255 if v>=128 else 0).getbbox();anchor=round((crown[0]+crown[2])/2);spans=[]
   for y in range(b[1]+int(w*.065),b[1]+int(w*.18)):
    if a.getpixel((anchor,y))<128:continue
    l=r=anchor
    while l>0 and a.getpixel((l-1,y))>=128:l-=1
    while r<w-1 and a.getpixel((r+1,y))>=128:r+=1
    spans.append((r-l+1,(l+r+1)/2))
   hw,hx=max(spans);scale=160/hw;hy=b[1]+hw/2;sprite=im.resize((round(w*scale),round(h*scale)),Image.Resampling.LANCZOS);sb=bounds(sprite)
   tx=round(350-hx*scale);ty=round(prior['headCenter'][1]-hy*scale)
   if support:ty=448-sb[3]
   else:ty=min(ty,446-dy-sb[3])
   assert 0<sb[0]+tx<sb[2]+tx<512 and 0<sb[1]+ty<sb[3]+ty<512
   im=Image.new('RGBA',(512,512));im.alpha_composite(sprite,(tx,ty));im.save(dst);head=[350,round(hy*scale+ty)]
  b=bounds(im);gap=448-b[3]-dy
  assert gap>=0 and (not support or gap==0)
  render=Image.new('RGBA',(544,512));render.alpha_composite(im,(16+dx,dy));frames.append(render)
  sole=None
  if support:
   a=im.getchannel('A');xs=[x for x in range(512) if a.getpixel((x,443))>=128];sole=round((min(xs)+max(xs))/2+dx,1)
  rows.append(dict(index=i,number=i+1,label=str(i+1),source=key,sourceCell=cell,previousIndex=prior['previousIndex'],previousLabel=prior['previousLabel'],new=new,durationMs=duration,renderOffset=[dx,dy],headCenter=head,renderedHeadCenter=[head[0]+dx,head[1]+dy],footGap=gap,supportLeg=support,soleCenterX=sole,bounds=b,forwardHandColorIntent='dark' if i in (0,1,9,10,11,12) else 'bright',armTransition=i in (1,2,7,8,9)))
 atlas=Image.new('RGBA',(2048,2048));sheet=Image.new('RGB',(1360,1400),'#242a30');d=ImageDraw.Draw(sheet);gif=[]
 for i,f in enumerate(frames):
  atlas.alpha_composite(Image.open(O/'frames'/f'{i:02}.png'),((i%4)*512,(i//4)*512))
  x=i%4*340;y=i//4*350;t=f.resize((340,320),Image.Resampling.LANCZOS);sheet.paste(t,(x,y),t);d.line((x+12,y+280,x+328,y+280),fill='#667683');d.text((x+14,y+322),str(i+1)+('  NEW' if rows[i]['new'] else ''),fill='#eadfce')
  bg=Image.new('RGBA',f.size,'#242a30');bg.alpha_composite(f);gif.append(bg.convert('RGB'))
 atlas.save(O/'run-atlas.png');sheet.save(O/'run-contact-sheet.jpg',quality=95)
 pal=gif[0].quantize(colors=240);g=[im.quantize(palette=pal,dither=Image.Dither.NONE) for im in gif];g[0].save(O/'run-preview.gif',save_all=True,append_images=g[1:],duration=[m['durationMs'] for m in rows],loop=0,disposal=2,optimize=False)
 meta=dict(frameCount=13,insertedThisRevision=2,loopMs=sum(m['durationMs'] for m in rows),cellSize=512,renderCanvas=[544,512],renderPaddingX=16,groundY=448,columns=4,rows=4,frames=rows)
 assert meta['loopMs']==800
 (O/'registration.json').write_text(json.dumps(meta,indent=2),encoding='utf-8')
 report=dict(frameCount=13,labels=[r['label'] for r in rows],newNumbers=[5,12],loopMs=800,footGaps=[r['footGap'] for r in rows],headY=[r['renderedHeadCenter'][1] for r in rows],soleCenters={r['label']:r['soleCenterX'] for r in rows if r['supportLeg']},uniqueFrames=len(set(hashlib.sha256(im.tobytes()).hexdigest() for im in frames)),note='File/contact checks only; animation continuity needs visual review.')
 (O/'validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print(json.dumps(report))
if __name__=='__main__':main()
