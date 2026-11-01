from pathlib import Path
import json, re, shutil, subprocess, wave
import numpy as np
from scipy import signal
import requests, imageio_ffmpeg

ROOT=Path(__file__).resolve().parent
REPO=ROOT.parents[1]
VOICE=REPO/'Assets/Audio/ManorRadioVoice'
EFFECTS=REPO/'Assets/Audio/ManorDoors'
ORIGINALS=ROOT/'PhoneOriginals'
FOLEY=REPO/'ArtSource/ManorDoorAudio'
for p in (VOICE,EFFECTS,ORIGINALS,FOLEY):p.mkdir(parents=True,exist_ok=True)
ffmpeg=imageio_ffmpeg.get_ffmpeg_exe(); RATE=22050

def decode(path):
    return np.frombuffer(subprocess.check_output([ffmpeg,'-v','error','-i',str(path),'-ac','1','-ar',str(RATE),'-f','f32le','-']),dtype='<f4').astype(float)

def trim(x):
    # 20 ms RMS detection, with enough pre/post roll to retain quiet consonants.
    hop=RATE//50
    frames=np.array([np.sqrt(np.mean(x[i:i+hop]**2)) for i in range(0,len(x),hop)])
    threshold=max(.003, min(.025,np.percentile(frames,90)*.09))
    active=np.flatnonzero(frames>threshold)
    assert len(active),'Silent recording'
    start=max(0,active[0]*hop-int(.11*RATE));end=min(len(x),(active[-1]+1)*hop+int(.18*RATE))
    return x[start:end],start/RATE,end/RATE

def write(path,x):
    x=np.clip(np.nan_to_num(x),-.89,.89)
    fade=min(int(.008*RATE),len(x)//4);x[:fade]*=np.linspace(0,1,fade);x[-fade:]*=np.linspace(1,0,fade)
    with wave.open(str(path),'wb') as w:
        w.setnchannels(1);w.setsampwidth(2);w.setframerate(RATE);w.writeframes((x*32767).astype('<i2').tobytes())
    return {'seconds':round(len(x)/RATE,3),'peak':round(float(np.max(abs(x))),4),'rms':round(float(np.sqrt(np.mean(x*x))),4)}

report={};voices={}
for path in sorted(ORIGINALS.glob('*.m4a')):
    name=path.stem.replace('Gellery','Gallery')
    raw=decode(path)
    x=signal.sosfilt(signal.butter(2,[90,6800],fs=RATE,btype='bandpass',output='sos'),raw)
    x,start,end=trim(x)
    # A speech-oriented compressor raises quiet syllables without crushing the voice.
    env=np.sqrt(signal.convolve(x*x,np.ones(441)/441,mode='same'))
    gain=np.minimum(1, (.10/np.maximum(env,.0001))**.65)
    x*=gain
    rms=np.sqrt(np.mean(x*x));x*=.18/max(rms,.0001)
    x=.88*np.tanh(x/.88)
    voices[name]=x
    if not name.startswith('GalleryInterrupted'):
        report[name]=write(VOICE/(name+'.wav'),x)
        report[name].update(source=path.name,rawSeconds=round(len(raw)/RATE,3),trimStart=round(start,3),trimEnd=round(end,3))
assert len(voices)==17
rng=np.random.default_rng(940)
burst=signal.sosfilt(signal.butter(2,[150,6000],fs=RATE,btype='bandpass',output='sos'),rng.normal(0,.14,int(RATE*.8)))
report['GalleryInterrupted']=write(VOICE/'GalleryInterrupted.wav',np.concatenate([voices['GalleryInterruptedStart'],burst,np.zeros(int(.15*RATE)),voices['GalleryInterruptedEnd']]))


(ROOT/'phone-audio-report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
