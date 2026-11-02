from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import random
ROOT=Path(__file__).resolve().parent;PROJECT=ROOT.parents[1] if ROOT.parent.name=='ArtSource' else ROOT/'stage';assets=PROJECT/'Assets/Props/CubbyMusiciansNote';assets.mkdir(parents=True,exist_ok=True)
fontpath=PROJECT/'Assets/Resources/EchoHandwriting.ttf'
if not fontpath.exists():fontpath=Path('C:/Projects/Echo/Assets/Resources/EchoHandwriting.ttf')
rng=random.Random(6187);im=Image.new('RGB',(512,640));pix=im.load()
for y in range(640):
 for x in range(512):
  grain=rng.randrange(-4,5);edge=min(x,511-x,y,639-y);dark=max(0,16-edge)*.7;pix[x,y]=(int(220+grain-dark),int(207+grain-dark),int(170+grain-dark))
d=ImageDraw.Draw(im);font=ImageFont.truetype(str(fontpath),49);ink=(60,43,29)
for line,y in [('музиканти здоров,',190),('ви попали',264),('в гру!!!',338)]:d.text((43,y),line,font=font,fill=ink)
d.line([(50,433),(277,433)],fill=(110,88,58),width=2)
im.save(assets/'MusiciansNote.png');print('MUSICIANS_NOTE_TEXTURE_READY')
