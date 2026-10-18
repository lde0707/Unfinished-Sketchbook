"""Slice and register whole-body attack drawings; no limb deformation or interpolation."""
from pathlib import Path
from PIL import Image, ImageDraw
import hashlib, json

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'Art/animations'
OUT = ART / 'player-c-attack-v1'
SIZE = (768, 640)
GROUND = 560
# Approximate head seeds in the 512px source cells; measure the contiguous head silhouette.
SEEDS = [
    [(337,185),(320,185),(343,200),(394,170),(392,180),(365,180)],
    [(350,190),(356,230),(330,195),(310,160),(345,150),(350,165)],
    [(328,231),(312,230),(284,230),(341,163),(319,158),(296,163)],
]
# Preserve the chamber timing; one brief extension, sharp contact, fast retract.
# Attack2 uses one swing drawing, followed by a140ms settled pose before chaining.
DURATIONS = [[40,40,25,70,75,110],[40,40,0,70,0,140],[70,80,12,48,60,190]]
OFFSETS = [[0,-8,10,20,14,0],[0,-6,6,14,8,0],[0,-8,6,24,18,0]]
PHASES = ['ready','windup','smear','impact','follow-through','recovery']
# Visually measured head width/center where the arm or raised cloak joins its alpha silhouette.
HEAD_OVERRIDES = {(1,4):(184,345),(1,5):(184,349),(2,0):(196,327)}

def head_measure(cell, seed):
    a = cell.getchannel('A'); sx,sy = seed; rows=[]
    for y in range(sy-12,sy+13):
        if a.getpixel((sx,y))<128: continue
        l=r=sx
        while l>0 and a.getpixel((l-1,y))>=128: l-=1
        while r<cell.width-1 and a.getpixel((r+1,y))>=128: r+=1
        rows.append((r-l+1,(l+r)/2,y))
    return max(rows)

def main():
    selection=json.loads((ART/'player-animation-selection.json').read_text(encoding='utf-8'))
    checked=0
    for state in ['idle','run','jump']:
        for name,digest in selection[state]['files'].items():
            assert hashlib.sha256((ART/name).read_bytes()).hexdigest()==digest,name
            checked+=1
    clips=[]; all_frames=[]
    for s in range(3):
        source_name='attack3-thrust-keys.png' if s==2 else f'attack{s+1}-keys.png'
        source=Image.open(OUT/'sources'/source_name).convert('RGBA')
        assert source.size==(1536,1024), source.size
        assert source.getchannel('A').getextrema()[0]==0, 'Source must contain generated transparency'
        target=OUT/f'attack{s+1}';(target/'frames').mkdir(parents=True,exist_ok=True)
        records=[];frames=[];elapsed=0
        for i in range(6):
            box=(i%3*512,i//3*512,(i%3+1)*512,(i//3+1)*512)
            # The drawn swing extends past the nominal grid line into empty gutter.
            # Use that actual gutter boundary so its arc is kept with the correct pose.
            crop_shift=0
            if s==0 and i==3: box=(0,512,568,1024)
            if s==0 and i==4: box=(568,512,1024,1024);crop_shift=56
            if s==2 and i==3: box=(0,512,540,1024)
            if s==2 and i==4: box=(540,512,1024,1024);crop_shift=28
            cell=source.crop(box); alpha=cell.getchannel('A')
            bb=alpha.point(lambda v:255 if v>=128 else 0).getbbox()
            seed=SEEDS[s][i]
            width,cx,_=head_measure(cell,(seed[0]-crop_shift,seed[1]))
            if (s,i) in HEAD_OVERRIDES:
                width,cx=HEAD_OVERRIDES[(s,i)];cx-=crop_shift
            assert 145<width<230,(s,i,width)
            scale=164/width
            tx=round(384+OFFSETS[s][i]-cx*scale);ty=round(GROUND-bb[3]*scale)
            sprite=cell.resize((round(cell.width*scale),round(cell.height*scale)),Image.Resampling.LANCZOS)
            im=Image.new('RGBA',SIZE);im.alpha_composite(sprite,(tx,ty))
            # Share the exact final upward-swing drawing at the combo boundary.
            if s==2 and i==0: im=all_frames[11].copy()
            bounds=im.getchannel('A').point(lambda v:255 if v>=128 else 0).getbbox()
            assert bounds[0]>4 and bounds[1]>4 and bounds[2]<SIZE[0]-4 and bounds[3]<=GROUND+1,(s,i,bounds)
            im.save(target/'frames'/f'{i:02}.png');frames.append(im);all_frames.append(im)
            phase=PHASES[i] if s<2 else ['swing-end','chamber','thrust','thrust-impact','retract','recovery'][i]
            records.append(dict(frame=i+1,globalFrame=s*6+i+1,phase=phase,durationMs=DURATIONS[s][i],startMs=elapsed,sourceRect=box,scale=scale,translation=[tx,ty],sourceHeadWidth=width,headX=384+OFFSETS[s][i],bounds=bounds,sharedFrom='Attack2/6' if s==2 and i==0 else None))
            if s==2 and i==0:
                for key in ['sourceRect','scale','translation','sourceHeadWidth','headX']: records[-1][key]=clips[1]['frames'][5][key]
            elapsed+=DURATIONS[s][i]
        atlas=Image.new('RGBA',(SIZE[0]*6,SIZE[1]))
        for i,im in enumerate(frames): atlas.alpha_composite(im,(SIZE[0]*i,0))
        atlas.save(target/'atlas.png')
        clips.append(dict(name=f'Attack{s+1}',durationMs=elapsed,frames=records,entryFrame=0,chainMs=elapsed,activeMs=[sum(DURATIONS[s][:2]),sum(DURATIONS[s][:4])]))
    for s,clip in enumerate(clips):
        clip['playbackFrames']=[i for i,d in enumerate(DURATIONS[s]) if d>0]
        if s==1: clip.update(swingFrames=[3],postSwingHoldMs=140)
    reg=dict(status='preview_pending_user_review',cellSize=list(SIZE),groundY=GROUND,pivot=[.5,.125],pixelsPerUnit=256,frameCount=18,playbackFramesPerClip=[6,4,6],clips=clips,inputBufferMs=150,comboWindowMs=250,finisherRecoveryMs=500,unityApplied=False)
    (OUT/'registration.json').write_text(json.dumps(reg,indent=2)+'\n',encoding='utf-8')
    contact=Image.new('RGB',(1536,750),'#202629');draw=ImageDraw.Draw(contact)
    for n,im in enumerate(all_frames):
        thumb=im.resize((256,213),Image.Resampling.LANCZOS);x=n%6*256;y=n//6*250
        contact.paste(thumb,(x,y),thumb)
        draw.text((x+10,y+220),f"{n+1:02} / A{n//6+1}  {clips[n//6]['frames'][n%6]['phase']}",fill='#e6d8bf')
    contact.save(OUT/'attack-contact-sheet.jpg',quality=94)
    report=dict(passed=True,approvedHashesChecked=checked,fullBodyDrawings=18,generatedAlphaPreserved=True,registrationOnly=True,clipsMs=[c['durationMs'] for c in clips],unityApplied=False)
    (OUT/'validation.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps(report))

if __name__=='__main__':main()
