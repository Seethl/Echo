"""Team recording -> seamless stereo loop. Python + numpy; no online service."""
from pathlib import Path
import hashlib,json,wave
import numpy as np

ROOT=Path(__file__).resolve().parent
source=ROOT/'igor ambient.wav'
destination=ROOT.parents[1]/'Assets/Audio/ManorAmbience/ManorAmbientLoop.wav'
with wave.open(str(source),'rb') as wav:
    rate=wav.getframerate();channels=wav.getnchannels()
    assert wav.getsampwidth()==2 and channels==2,'Expected supplied 16-bit stereo WAV'
    samples=np.frombuffer(wav.readframes(wav.getnframes()),dtype='<i2').reshape(-1,channels).astype(np.float64)/32768
# Overlap the last 1.5 s and the first 1.5 s. Start at 1.5 s so the
# final blended sample leads directly into the first sample on every repeat.
fade=round(1.5*rate);t=np.linspace(0,1,fade)[:,None]
seam=samples[-fade:]*(1-t)+samples[:fade]*t
loop=np.concatenate((samples[fade:-fade],seam))
rms=float(np.sqrt(np.mean(loop**2)));peak=float(np.max(np.abs(loop)))
gain=min(.13/max(rms,1e-8),.9/max(peak,1e-8))
loop*=gain
destination.parent.mkdir(parents=True,exist_ok=True)
with wave.open(str(destination),'wb') as wav:
    wav.setnchannels(channels);wav.setsampwidth(2);wav.setframerate(rate)
    wav.writeframes(np.round(np.clip(loop,-1,.999969)*32768).astype('<i2').tobytes())
report={'source':'igor ambient.wav','sourceSHA256':hashlib.sha256(source.read_bytes()).hexdigest(),
        'runtimeSHA256':hashlib.sha256(destination.read_bytes()).hexdigest(),'rate':rate,'channels':channels,
        'sourceSeconds':len(samples)/rate,'loopSeconds':len(loop)/rate,'crossfadeSeconds':1.5,
        'gain':gain,'rms':float(np.sqrt(np.mean(loop**2))),'peak':float(np.max(np.abs(loop))),
        'boundaryStep':float(np.max(np.abs(loop[0]-loop[-1])))}
(ROOT/'processing-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
