from pathlib import Path
import numpy as np, wave, json
out=Path('Book/Assets/Unfin/Resources/Audio');rate=44100;report=[]
for k,(name,length,grain,body) in enumerate([('BreakGraphite',.48,5,120),('BreakVine',.38,2,230),('BreakWood',.52,8,95),('BreakSeal',.62,12,370),('BreakWax',.46,15,150)]):
 for v in range(2):
  rng=np.random.default_rng(725+k*9+v);n=int(length*rate);t=np.arange(n)/rate
  noise=rng.uniform(-1,1,n);low=np.convolve(noise,np.ones(grain)/grain,mode='same');soft=np.convolve(noise,np.ones(90)/90,mode='same')
  a=low*np.exp(-t*(12 if k==1 else 9))*.55+soft*np.exp(-t*6)*.8
  a+=np.sin(2*np.pi*(body*t-30*t*t))*np.exp(-t*40)*.13
  for j in range(7 if k!=1 else 13):
   start=int((.015+j*.022+rng.uniform(0,.012))*rate);size=min(int(rate*.035),n-start)
   if size>0:a[start:start+size]+=low[:size]*np.exp(-np.arange(size)/rate*120)*(.25*(1-j/16))
  if k==3:a+=.035*np.sin(2*np.pi*720*t)*np.exp(-t*12)
  a*=np.minimum(t/.003,1)*np.minimum((length-t)/.035,1);a=a/(max(np.max(np.abs(a)),.001))*.55
  p=out/(name+'_'+str(v)+'.wav')
  with wave.open(str(p),'wb') as f:f.setnchannels(1);f.setsampwidth(2);f.setframerate(rate);f.writeframes((a*32767).astype('<i2').tobytes())
  report.append({'file':str(p),'seconds':length,'peak':float(np.max(np.abs(a))),'rms':float(np.sqrt(np.mean(a*a)))})
Path('Art/Audio/SpecialBlocksV1').mkdir(parents=True,exist_ok=True);Path('Art/Audio/SpecialBlocksV1/manifest.json').write_text(json.dumps({'origin':'Original deterministic synthesis, no third-party recordings','purpose':'Soft graphite, paper-fibre, wood, seal and wax break sounds','variants':report},indent=2),encoding='utf-8')
