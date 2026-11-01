from pathlib import Path
import json,subprocess,wave,hashlib
import numpy as np
from scipy import signal
import imageio_ffmpeg

ROOT=Path(__file__).resolve().parent
if (ROOT/'PhoneOriginals').exists() and '--replace-human-voice' not in __import__('sys').argv:
 raise SystemExit('Human voice is installed. Use process_phone.py; synthetic regeneration requires --replace-human-voice.')
OUT=ROOT.parents[1]/'Assets/Audio/ManorRadioVoice'
OUT.mkdir(parents=True,exist_ok=True)
(ROOT/'raw').mkdir(exist_ok=True)
texts={
 'Service820':'Запасний ключ від службового проходу — у шафці біля дверей.',
 'GalleryPrefix':'Службовий замок на поточну зміну.',
 'GallerySuffix':'Повторіть ці цифри на механічному замку.',
 'GalleryInterruptedStart':'Службовий замок на поточну зміну...',
 'GalleryInterruptedEnd':'Зачиніть усі віконниці й повторіть запис.',
 'LibraryPrefix':'Слухай порядок.',
 'LibrarySuffix':'Повтори його на вінілових платівках. Запис можна повторити кнопкою на радіо.'
}
texts.update({'Digit'+str(i):w for i,w in enumerate(['Нуль.','Один.','Два.','Три.','Чотири.','П’ять.','Шість.','Сім.','Вісім.','Дев’ять.'])})
lines=[json.dumps({'text':text,'output_file':'raw/'+name+'.wav','speaker_id':1},ensure_ascii=False) for name,text in texts.items()]
result=subprocess.run([str(ROOT/'tools/piper/piper.exe'),'-m','uk_UA-ukrainian_tts-medium.onnx','--json-input','--speaker','1','--length_scale','1.03','--sentence_silence','0.12'],input=('\n'.join(lines)+'\n').encode('utf-8'),cwd=ROOT,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
if result.returncode:raise RuntimeError(result.stderr.decode('utf-8',errors='replace'))
rate=22050
def load(p):
 with wave.open(str(p)) as w:
  assert w.getframerate()==rate and w.getnchannels()==1 and w.getsampwidth()==2
  return np.frombuffer(w.readframes(w.getnframes()),dtype='<i2').astype(float)/32768
def write(name,x):
 x=np.nan_to_num(x);peak=np.max(np.abs(x));assert peak>0
 x=x*min(2,.79/peak)
 with wave.open(str(OUT/(name+'.wav')),'wb') as w:
  w.setnchannels(1);w.setsampwidth(2);w.setframerate(rate);w.writeframes((np.clip(x,-.96,.96)*32767).astype('<i2').tobytes())
 return {'name':name,'seconds':round(len(x)/rate,3),'peak':round(float(np.max(abs(x))),3)}
report=[]
def radio_filter(x):
 # Mild radio bandwidth, retaining consonants and intelligibility.
 y=signal.sosfilt(signal.butter(2,[150,4800],btype='bandpass',fs=rate,output='sos'),x)
 active=np.flatnonzero(np.abs(y)>.006)
 if len(active):y=y[max(0,active[0]-int(.05*rate)):min(len(y),active[-1]+int(.16*rate))]
 fade=min(220,len(y)//4);y[:fade]*=np.linspace(0,1,fade);y[-fade:]*=np.linspace(1,0,fade)
 return y
voices={name:radio_filter(load(ROOT/'raw'/(name+'.wav'))) for name in texts}
for name,x in voices.items():
 if not name.startswith('GalleryInterrupted'):report.append(write(name,x))
rng=np.random.default_rng(940)
burst=signal.sosfilt(signal.butter(2,[150,6000],btype='bandpass',fs=rate,output='sos'),rng.normal(0,.12,int(rate*.8)))
report.append(write('GalleryInterrupted',np.concatenate([voices['GalleryInterruptedStart'],burst,np.zeros(int(rate*.15)),voices['GalleryInterruptedEnd']])))
# Downloaded real recording: mono and a seamless equal-power overlap loop, not random noise.
raw=subprocess.check_output([imageio_ffmpeg.get_ffmpeg_exe(),'-v','error','-i',str(ROOT/'window-wind-original.mp3'),'-ac','1','-ar',str(rate),'-f','f32le','-'])
x=np.frombuffer(raw,dtype='<f4').astype(float);x=signal.sosfilt(signal.butter(2,70,btype='highpass',fs=rate,output='sos'),x)
overlap=int(rate*1.2);a=np.linspace(0,np.pi/2,overlap)
cross=x[-overlap:]*np.cos(a)+x[:overlap]*np.sin(a)
loop=np.concatenate([cross,x[overlap:-overlap]])
loop*=.16/max(.001,np.sqrt(np.mean(loop**2)))
report.append(write('WindowWindLoop',loop))
(ROOT/'voice-texts.json').write_text(json.dumps(texts,ensure_ascii=False,indent=2),encoding='utf-8')
(ROOT/'audio-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
for r in report:print(r)
