"""Original sparse cave cues separated by silence; no musical samples.
Run with the bundled Python (numpy). Both stereo loops have silent seams.
"""
from pathlib import Path
import json
import wave
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Art/Audio/StilldrawnAmbienceV1'
RUNTIME = ROOT / 'Book/Assets/Unfin/Resources/Audio'
RATE = 32000

def write_wave(path, samples):
    with wave.open(str(path), 'wb') as f:
        f.setnchannels(2); f.setsampwidth(2); f.setframerate(RATE)
        f.writeframes((np.clip(samples, -1, 1) * 32767).astype('<i2').tobytes())

def noise(rng, count, low, high, tilt=.55):
    # A band of independent phases, not a bank of harmonic notes.
    freq = np.fft.rfftfreq(count, 1 / RATE)
    shape = (1 - np.exp(-(freq / low) ** 4)) * np.exp(-(freq / high) ** 2)
    shape *= (np.maximum(freq, low) / low) ** -tilt
    spectrum = (rng.standard_normal(len(freq)) + 1j * rng.standard_normal(len(freq))) * shape
    spectrum[0] = 0
    x = np.fft.irfft(spectrum, n=count).astype(np.float32)
    return x / max(float(np.std(x)), 1e-9)

def design(name, seconds, seed, warm):
    rng = np.random.default_rng(seed)
    n = seconds * RATE
    mix = np.zeros((n, 2), np.float32)
    events = []
    at = 14.0 if warm else 12.0
    index = 0
    while at < seconds - 7:
        # Most of the loop is exact silence. No constant air, noise floor or pad.
        duration = rng.uniform(3.0, 4.8)
        length = int(duration * RATE)
        t = np.arange(length, dtype=np.float32) / RATE
        phase = t / duration
        if index % 2 == 0:
            # A small, distant water drop: rounded transient and soft cavity echo.
            body = np.sin(2*np.pi*(540*t-115*t*t)) * np.exp(-t*13)
            body += noise(rng, length, 270, 1050, .7) * np.exp(-t*6) * .13
            envelope = (1-np.exp(-t*70)) * np.sin(np.pi*phase)**.35
            event = body * envelope * .11
            kind = 'distant-water-drop'
        else:
            # Brief low cavern resonance, with a slow onset and no continuous wind.
            body = noise(rng, length, 75 if warm else 55, 250, .9)
            event = body * np.sin(np.pi*phase)**2 * .008
            kind = 'distant-cavern-resonance'
        pan = rng.uniform(-.28, .28)
        pos = int(at * RATE)
        for delay, gain in [(0,1),(.31,.32),(.73,.17),(1.19,.075),(1.7,.025)]:
            start = pos + int(delay*RATE)
            end = min(start+length,n)
            for ch in range(2):
                weight = np.sqrt((1+pan*(1 if ch else -1))/2)
                mix[start:end,ch] += event[:end-start] * gain * weight
        events.append({'time':round(at,2),'duration':round(duration+1.7,2),'kind':kind})
        at += rng.uniform(23,30)
        index += 1
    # Do not normalize: normalizing sparse events makes every small sound loud.
    write_wave(RUNTIME/(name+'.wav'),mix)
    write_wave(OUT/(name+'-preview.wav'),mix[:45*RATE])
    second_rms=[float(np.sqrt(np.mean(mix[i*RATE:(i+1)*RATE]**2))) for i in range(seconds)]
    report={'file':name+'.wav','durationSeconds':seconds,'sampleRate':RATE,'channels':2,'seed':seed,
            'peak':float(np.max(np.abs(mix))),'rms':float(np.sqrt(np.mean(mix**2))),
            'silentFraction':sum(r<1e-5 for r in second_rms)/seconds,
            'seamJump':float(np.max(np.abs(mix[-1]-mix[0]))),'events':events}
    assert report['peak']<.12 and .0001<report['rms']<.01
    assert report['silentFraction']>.7 and report['seamJump']==0
    return report

if __name__=='__main__':
    OUT.mkdir(parents=True,exist_ok=True)
    reports=[design('AmbienceGraphite_0',96,620251,False),design('AmbienceCrayon_0',112,620252,True)]
    manifest={'type':'original procedural environmental soundscape','method':'NumPy spectral sound design, not an AI music service or stock music',
              'sources':'Original synthesized distant water drops and brief cavern resonance. No external recordings or music samples.',
              'creativeBrief':'Mostly silence, occasional very quiet cave or dungeon cues. No continuous wind, pad, music, loud impacts or horror stingers.',
              'runtime':'Sparse stereo event loops with silent seams, five-second region crossfades, reduced runtime gain; existing environment volume preference retained.',
              'tracks':reports}
    (OUT/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(reports,ensure_ascii=False))
