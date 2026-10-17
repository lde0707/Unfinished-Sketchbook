"""Register eight complete drawings. No inbetweens, limb rig or morphing."""
from pathlib import Path
from PIL import Image,ImageDraw
import json,shutil,hashlib
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Art/animations/player-c-v6'
SIZE=512;GROUND=448;HEAD=160
SOURCE_HEAD_DELTA=[0,0,1,2,1,-2,-1,2]
ROLES=[('near','far'),('near',None),('near',None),('near','near'),('far','near'),('far',None),('far',None),('far','far')]
def bounds(im):return im.getchannel('A').point(lambda v:255 if v>=128 else 0).getbbox()
def main():
 (OUT/'frames').mkdir(exist_ok=True);(OUT/'reference').mkdir(exist_ok=True)
 frames=[];meta=[];base_head=None
 for i in range(8):
  name='keys-'+('a' if i<4 else 'b')+'.png';im=Image.open(OUT/'sources'/name).convert('RGBA');cw,ch=im.width//2,im.height//2;j=i%4
  rect=(j%2*cw,j//2*ch,(j%2+1)*cw,(j//2+1)*ch);cell=im.crop(rect);b=bounds(cell);a=cell.getchannel('A');rows=[]
  for y in range(b[1]+30,b[1]+150):
   row=a.crop((0,y,cw,y+1)).point(lambda v:255 if v>=128 else 0).getbbox()
   if row:rows.append((row[2]-row[0],(row[0]+row[2])/2))
  w,hx=max(rows);hy=b[1]+w/2;scale=HEAD/w;tx=round(350-hx*scale)
  if base_head is None:base_head=GROUND-(b[3]-hy)*scale
  target_head=base_head+SOURCE_HEAD_DELTA[i]*4
  # Keep the same head size. Stance frames register to the sole, flight frames
  # follow the reference's head-height phase. Never stretch isolated body parts.
  ty=round(GROUND-b[3]*scale) if ROLES[i][1] else round(target_head-hy*scale)
  sprite=cell.resize((round(cw*scale),round(ch*scale)),Image.Resampling.LANCZOS)
  out=Image.new('RGBA',(SIZE,SIZE));out.alpha_composite(sprite,(tx,ty));bb=bounds(out)
  if ROLES[i][1]:
   correction=GROUND-bb[3]
   if correction:
    ty+=correction;out=Image.new('RGBA',(SIZE,SIZE));out.alpha_composite(sprite,(tx,ty));bb=bounds(out)
  assert min(bb[:2])>0 and max(bb[2:])<SIZE,(i,bb)
  out.save(OUT/'frames'/f'{i:02}.png');frames.append(out)
  meta.append(dict(frame=i+1,source=name,sourceRect=rect,scale=scale,translation=[tx,ty],headCenter=[350,round(hy*scale+ty)],headWidth=HEAD,bounds=bb,footGap=GROUND-bb[3],forwardLeg=ROLES[i][0],supportLeg=ROLES[i][1],durationMs=100))
  raw=ROOT/'Art/animations/player-c-v5/reference'/f'{i:02}.png'
  if raw.exists():shutil.copy2(raw,OUT/'reference'/raw.name)
 atlas=Image.new('RGBA',(SIZE*4,SIZE*2));sheet=Image.new('RGB',(1280,700),'#242a30');d=ImageDraw.Draw(sheet);gif=[]
 for i,f in enumerate(frames):
  atlas.alpha_composite(f,((i%4)*SIZE,(i//4)*SIZE));thumb=f.resize((320,320),Image.Resampling.LANCZOS);x,y=(i%4)*320,(i//4)*350;sheet.paste(thumb,(x,y),thumb)
  d.line((x+15,y+280,x+305,y+280),fill='#677581');d.text((x+15,y+322),f'{i+1}  FRONT: {meta[i]["forwardLeg"].upper()}  SUPPORT: {str(meta[i]["supportLeg"]).upper()}',fill='#eadfce')
  bg=Image.new('RGBA',f.size,'#242a30');bg.alpha_composite(f);gif.append(bg.convert('RGB'))
 atlas.save(OUT/'run-atlas.png');sheet.save(OUT/'run-contact-sheet.jpg',quality=95)
 pal=gif[0].quantize(colors=240);g=[im.quantize(palette=pal,dither=Image.Dither.NONE) for im in gif];g[0].save(OUT/'run-preview.gif',save_all=True,append_images=g[1:],duration=100,loop=0,disposal=2,optimize=False)
 data=dict(frameCount=8,inbetweenCount=0,fps=10,loopMs=800,cellSize=SIZE,columns=4,rows=2,groundY=GROUND,pivot=[350/SIZE,1-GROUND/SIZE],sourceHeadDeltaPx=SOURCE_HEAD_DELTA,headRegistration='Reference height phase for flight frames; actual sole registration for stance. Drawing compression adds a larger drop at frames4/8. All heads use the same width.',frames=meta)
 (OUT/'registration.json').write_text(json.dumps(data,indent=2),encoding='utf-8')
 validation=dict(frameCount=8,inbetweenCount=0,uniqueFrames=len(set(hashlib.sha256(f.tobytes()).hexdigest() for f in frames)),headWidthPx=HEAD,headY=[r['headCenter'][1] for r in meta],footGaps=[r['footGap'] for r in meta],supportFrames=[1,4,5,8],contactErrorPx=max(abs(meta[i]['footGap']) for i in [0,3,4,7]),clippedSilhouettes=0,note='Drawing leg identity and overlap require visual review; file checks do not establish animation quality.')
 assert validation['contactErrorPx']==0
 (OUT/'validation.json').write_text(json.dumps(validation,indent=2),encoding='utf-8');print(json.dumps(validation))
if __name__=='__main__':main()
