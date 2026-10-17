"""Package four complete breathing drawings into a six-frame ping-pong idle."""
from pathlib import Path
from PIL import Image,ImageDraw
import json,hashlib
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Art/animations/player-c-idle-v1'
SEQUENCE=[2,0,3,1,3,0] # Low-to-high breath order after checking the generated keys.
def bounds(im):return im.getchannel('A').point(lambda v:255 if v>=128 else 0).getbbox()
def verify_run():
 selection=json.loads((ROOT/'Art/animations/player-animation-selection.json').read_text(encoding='utf-8'))
 for name,digest in selection['run']['files'].items():
  assert hashlib.sha256((ROOT/'Art/animations'/name).read_bytes()).hexdigest()==digest,name
 return True
def main():
 verify_run();(OUT/'keys').mkdir(exist_ok=True);(OUT/'frames').mkdir(exist_ok=True)
 sheet=Image.open(OUT/'sources/idle-keys.png').convert('RGBA');cw,ch=sheet.width//2,sheet.height//2;keys=[];keymeta=[]
 for j in range(4):
  rect=(j%2*cw,j//2*ch,(j%2+1)*cw,(j//2+1)*ch);cell=sheet.crop(rect);a=cell.getchannel('A').point(lambda v:255 if v>=128 else 0);b=a.getbbox();rows=[]
  # Head is topmost and does not overlap the hanging cloak at these rows.
  for y in range(b[1]+int(ch*.03),b[1]+int(ch*.18)):
   r=a.crop((0,y,cw,y+1)).getbbox()
   if r:rows.append((r[2]-r[0],(r[0]+r[2])/2))
  w,hx=max(rows);hy=b[1]+w/2;scale=160/w;tx=round(364-hx*scale);ty=round(448-b[3]*scale)
  sprite=cell.resize((round(cw*scale),round(ch*scale)),Image.Resampling.LANCZOS)
  out=Image.new('RGBA',(512,512));out.alpha_composite(sprite,(tx,ty));ty+=448-bounds(out)[3]
  out=Image.new('RGBA',(512,512));out.alpha_composite(sprite,(tx,ty))
  sole=out.getchannel('A').point(lambda v:255 if v>=128 else 0).crop((290,440,440,448)).getbbox()
  dx=round(358-(290+(sole[0]+sole[2])/2));tx+=dx
  out=Image.new('RGBA',(512,512));out.alpha_composite(sprite,(tx,ty));bb=bounds(out)
  assert bb[0]>0 and bb[1]>0 and bb[2]<512 and bb[3]==448
  out.save(OUT/'keys'/f'{j:02}.png');keys.append(out)
  sole=out.getchannel('A').point(lambda v:255 if v>=128 else 0).crop((290,440,440,448)).getbbox()
  keymeta.append(dict(key=j,sourceRect=rect,scale=scale,translation=[tx,ty],headCenter=[364+dx,round(hy*scale+ty)],bounds=bb,groundY=448,soleCenterX=290+(sole[0]+sole[2])/2))
 atlas=Image.new('RGBA',(1536,1024));contact=Image.new('RGB',(1280,350),'#242a30');d=ImageDraw.Draw(contact);frames=[];meta=[];gif=[]
 for j,f in enumerate(keys):
  thumb=f.resize((320,320),Image.Resampling.LANCZOS);contact.paste(thumb,(j*320,0),thumb);d.text((j*320+15,325),f'KEY {j+1}',fill='#eadfce')
 for i,j in enumerate(SEQUENCE):
  f=keys[j];f.save(OUT/'frames'/f'{i:02}.png');frames.append(f);atlas.alpha_composite(f,((i%3)*512,(i//3)*512))
  m=dict(keymeta[j]);m.update(frame=i+1,durationMs=300);meta.append(m)
  bg=Image.new('RGBA',f.size,'#242a30');bg.alpha_composite(f);gif.append(bg.convert('RGB'))
 atlas.save(OUT/'idle-atlas.png');contact.save(OUT/'idle-contact-sheet.jpg',quality=95)
 pal=gif[0].quantize(colors=240);g=[f.quantize(palette=pal,dither=Image.Dither.NONE) for f in gif]
 g[0].save(OUT/'idle-preview.gif',save_all=True,append_images=g[1:],duration=300,loop=0,disposal=2,optimize=False)
 data=dict(frameCount=6,uniqueDrawings=4,sequence=SEQUENCE,loopMs=1800,cellSize=512,columns=3,rows=2,groundY=448,pivot=[364/512,1-448/512],frames=meta)
 (OUT/'registration.json').write_text(json.dumps(data,indent=2),encoding='utf-8')
 checks=dict(frameCount=6,uniqueDrawings=len(set(hashlib.sha256(f.tobytes()).hexdigest() for f in keys)),loopMs=1800,headY=[m['headCenter'][1] for m in keymeta],headTravelY=max(m['headCenter'][1] for m in keymeta)-min(m['headCenter'][1] for m in keymeta),soleCenterX=[m['soleCenterX'] for m in keymeta],groundY=[m['bounds'][3] for m in keymeta],approvedRunUnchanged=verify_run(),note='Ground and sole-center registration stabilize feet; generated silhouettes are not pixel-identical.')
 (OUT/'validation.json').write_text(json.dumps(checks,indent=2),encoding='utf-8');print(json.dumps(checks))
if __name__=='__main__':main()
