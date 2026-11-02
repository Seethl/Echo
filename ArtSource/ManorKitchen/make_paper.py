from pathlib import Path
from PIL import Image,ImageDraw,ImageFilter
import numpy as np
ROOT=Path(__file__).resolve().parent.parents[1]/'Assets/Resources'
ROOT.mkdir(parents=True,exist_ok=True)
rng=np.random.default_rng(814);w,h=768,1024
y,x=np.mgrid[:h,:w];edge=np.minimum.reduce([x,w-1-x,y,h-1-y]).astype(float)
grain=rng.normal(0,1.3,(h,w));fold=np.exp(-((x-w*.49)/2.5)**2)*2.5
shade=(1-np.clip(edge/45,0,1))*18
stain=np.exp(-((x-65)**2+(y-840)**2)/19000)*8+np.exp(-((x-695)**2+(y-170)**2)/23000)*6
base=np.stack([236+grain-shade-stain-fold,222+grain-shade-stain-fold,190+grain-shade*.9-stain-fold],axis=2)
im=Image.fromarray(np.uint8(np.clip(base,0,255)),'RGB');im.save(ROOT/'EchoPaper.png')
icons=ROOT/'EchoInventoryIcons';icons.mkdir(exist_ok=True)
for name in ['radio','key','crank','crowbar','spoon','flower','bottle','potion','base','cutters']:
 im=Image.new('RGBA',(96,96));d=ImageDraw.Draw(im);ink=(255,255,255,255)
 if name=='radio':
  d.rounded_rectangle((10,32,86,81),5,outline=ink,width=5);d.arc((28,9,70,51),180,360,fill=ink,width=5)
  for yy in range(43,74,9):d.line((20,yy,53,yy),fill=ink,width=3)
  d.ellipse((64,46,76,58),outline=ink,width=3);d.ellipse((64,65,76,77),outline=ink,width=3)
 elif name=='key':
  d.ellipse((13,12,49,48),outline=ink,width=6);d.line((42,42,78,78),fill=ink,width=7);d.line((62,63,74,53),fill=ink,width=6);d.line((72,72,84,62),fill=ink,width=6)
 elif name in ['bottle','potion','base']:
  d.polygon([(37,13),(59,13),(59,35),(72,49),(72,82),(24,82),(24,49),(37,35)],outline=ink,width=5);d.line((36,13,60,13),fill=ink,width=7)
  if name!='bottle':d.rectangle((31,59,65,76),fill=ink)
 elif name=='spoon':d.ellipse((33,8,64,44),outline=ink,width=5);d.line((48,44,48,86),fill=ink,width=7)
 elif name=='crowbar':d.line([(70,14),(53,13),(43,25),(24,81)],fill=ink,width=8)
 elif name=='crank':d.line([(17,29),(17,51),(67,51),(67,76)],fill=ink,width=7);d.rounded_rectangle((59,64,77,85),4,outline=ink,width=4)
 elif name=='flower':
  d.line((48,40,48,88),fill=ink,width=4);d.ellipse((20,59,47,72),outline=ink,width=4);d.ellipse((49,65,79,78),outline=ink,width=4)
  for a in [0,72,144,216,288]:
   import math
   cx=48+14*math.cos(math.radians(a));cy=32+14*math.sin(math.radians(a));d.ellipse((cx-10,cy-10,cx+10,cy+10),outline=ink,width=3)
 elif name=='cutters':
  d.line([(18,84),(45,42),(63,15)],fill=ink,width=6);d.line([(78,84),(51,42),(33,15)],fill=ink,width=6);d.ellipse((41,36,56,51),fill=ink)
 im.save(icons/(name+'.png'))
print('PAPER_AND_ICONS_CREATED')
