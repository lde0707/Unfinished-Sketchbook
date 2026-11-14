"""Render the approved title painting as a deterministic, seamless cinemagraph.

No new image generation: local page motion, illumination and dust are animated.
All menu/logo pixels stay fixed. Integer-frequency motion closes after 10 sec.
"""
from pathlib import Path
import json, math, subprocess, hashlib
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Art/title/stilldrawn-painted-v2/loop-v1'
SOURCE = OUT.parent / 'title-concept.png'
FFMPEG = Path('C:/Program Files/BlueStacks_nxt/ffmpeg.exe')
W, H, FPS, SECONDS = 1920, 1080, 60, 10
OUT.mkdir(parents=True, exist_ok=True)
original = Image.open(SOURCE).convert('RGB').resize((W,H), Image.Resampling.LANCZOS)
y, x = np.mgrid[0:H,0:W].astype(np.float32)
x /= W; y /= H
tau = math.tau

def region(x,y,cx,cy,rx,ry):
    q = ((x-cx)/rx)**2 + ((y-cy)/ry)**2
    t = np.clip((1-q)/.7,0,1)
    return t*t*(3-2*t)

# Protect all type, including the lower-right version number.
light = region(x,y,.748,.42,.26,.60) * np.clip((x-.50)/.07,0,1)
spine = region(x,y,.725,.46,.065,.32)
light = (light*.035 + spine*.022)[...,None]
rng = np.random.default_rng(23026)
particles = [(rng.uniform(.53,.96),rng.uniform(.02,.79),rng.uniform(.6,1.7),
              rng.uniform(0,tau),rng.uniform(0,tau),rng.uniform(.45,1)) for _ in range(48)]

# Mesh sampling uses smooth local fields and leaves the paper binding anchored.
def offset(px,py,a):
    xx,yy=px/W,py/H
    if xx < .52: return px,py
    loose=region(xx,yy,.876,.112,.10,.14)
    left=region(xx,yy,.605,.47,.135,.265)
    right=region(xx,yy,.844,.47,.105,.275)
    anchor=min(1,abs(xx-(.755-.045*yy))/.06)
    dx=loose*7*math.sin(a+.5)+anchor*(left*2.6*math.sin(a+yy*5)+right*3.4*math.sin(a+yy*4+.5))
    dy=loose*10*math.sin(a)+anchor*(left*4.2*math.sin(a+xx*8)+right*5*math.sin(a+xx*7))
    # Water is a secondary, extremely restrained surface movement.
    water=region(xx,yy,.413,.67,.047,.22)
    return px+dx+water*math.sin(3*a+yy*40),py+dy

def render(t):
    a=tau*(t%SECONDS)/SECONDS
    mesh=[]
    for top in range(0,H,48):
        bottom=min(H,top+48)
        for left in range(0,W,48):
            right=min(W,left+48)
            corners=[offset(left,top,a),offset(left,bottom,a),offset(right,bottom,a),offset(right,top,a)]
            mesh.append(((left,top,right,bottom),tuple(v for p in corners for v in p)))
    frame=original.transform((W,H),Image.Transform.MESH,mesh,Image.Resampling.BICUBIC)
    pixels=np.asarray(frame).astype(np.float32)
    illumination=math.sin(a-.8)*.65+math.sin(2*a+.7)*.35
    pixels*=1+light*illumination
    frame=Image.fromarray(np.uint8(np.clip(pixels,0,255)))
    dust=Image.new('RGBA',(W,H)); draw=ImageDraw.Draw(dust)
    for px,py,r,phase,phase2,strength in particles:
        # Closed elliptical paths with independent phases avoid wrap/pop seams.
        sx=W*px+math.sin(a+phase)*16+math.sin(2*a+phase2)*4
        sy=H*py+math.cos(a+phase2)*25
        alpha=int((.55+.45*math.sin(a+phase)) * 100*strength)
        draw.ellipse((sx-r*3,sy-r*3,sx+r*3,sy+r*3),fill=(215,193,145,alpha//6))
        draw.ellipse((sx-r,sy-r,sx+r,sy+r),fill=(238,216,173,alpha))
    frame=Image.alpha_composite(frame.convert('RGBA'),dust.filter(ImageFilter.GaussianBlur(.55))).convert('RGB')
    return frame

def main():
    samples=[render(t) for t in [0,2.5,5,7.5,10-1/FPS,10]]
    first=np.asarray(samples[0]).astype(np.int16)
    last=np.asarray(samples[-2]).astype(np.int16)
    seam=np.asarray(samples[-1]).astype(np.int16)
    assert np.array_equal(first,seam),'period must close exactly'
    assert all(np.array_equal(first[:,:950],np.asarray(s)[:,:950]) for s in samples),'UI must be stationary'
    samples[0].save(OUT/'poster.jpg',quality=95)
    contact=Image.new('RGB',(1280,720))
    for i,s in enumerate(samples[:4]): contact.paste(s.resize((640,360)),((i%2)*640,(i//2)*360))
    contact.save(OUT/'motion-contact-sheet.jpg',quality=92)
    video=OUT/'stilldrawn-title-loop.mp4'
    args=[str(FFMPEG),'-y','-hide_banner','-loglevel','warning','-f','rawvideo','-pixel_format','rgb24',
          '-video_size',f'{W}x{H}','-framerate',str(FPS),'-i','pipe:0','-an','-c:v','libopenh264',
          '-b:v','16000k','-pix_fmt','yuv420p','-movflags','+faststart',str(video)]
    with (OUT/'encode.log').open('w') as log:
        process=subprocess.Popen(args,stdin=subprocess.PIPE,stderr=log)
        for n in range(FPS*SECONDS):
            process.stdin.write(render(n/FPS).tobytes())
            if n%60==0: print(f'Frame {n}/{FPS*SECONDS}',flush=True)
        process.stdin.close()
        if process.wait()!=0: raise RuntimeError('See encode.log')
    report={'source':str(SOURCE.relative_to(ROOT)),'sourceSha256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
            'method':'deterministic localized mesh motion, periodic light and dust; not generative AI video',
            'durationSeconds':SECONDS,'fps':FPS,'size':[W,H],'frames':FPS*SECONDS,'silent':True,
            'loopClosureMaxPixelDifference':int(np.max(abs(first-seam))),
            'lastToFirstMeanPixelDifference':float(np.mean(abs(first-last))),
            'menuAndLogoFixedBeforeEncoding':True,'unityInstalled':False}
    (OUT/'render.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    (OUT/'preview.html').write_text('''<!doctype html><html lang="ko"><meta charset="utf-8"><title>Stilldrawn · Title loop</title>
<style>html,body{margin:0;height:100%;background:#080b0e;display:grid;place-items:center}video{width:100vw;max-height:100vh;object-fit:contain}</style>
<video autoplay muted loop controls playsinline poster="poster.jpg" src="stilldrawn-title-loop.mp4"></video></html>''',encoding='utf-8')
    print(json.dumps(report),flush=True)

if __name__=='__main__': main()
