import bpy, math, json, shutil, random
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parent;PROJECT=ROOT.parents[1];ASSET=PROJECT/'Assets'/'Props'/'ManorDecor';ASSET.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
shutil.copy2(PROJECT/'Assets'/'Props'/'ManorTables'/'ManorTables_BaseColor.png',ASSET/'ManorDecor_Wood.png')
img=bpy.data.images.load(str(ASSET/'ManorDecor_Wood.png'))
specs=[];mats={}
for name,color,metal,rough in [('Wood',(.15,.09,.04),0,.78),('Burgundy',(.20,.035,.05),0,.9),('Olive',(.13,.15,.07),0,.9),('Blue',(.045,.07,.09),0,.9),('Paper',(.47,.39,.25),0,.95),('Brass',(.36,.24,.08),.8,.5),('Iron',(.055,.06,.065),.7,.65),('Ceramic',(.49,.44,.33),0,.45),('Cardboard',(.26,.17,.09),0,.95),('Bottle',(.07,.105,.035),0,.35),('Jar',(.14,.07,.035),0,.5),('Ink',(.045,.034,.025),0,.95)]:
 m=bpy.data.materials.new('M_ManorDecor_'+name);m.use_nodes=True;m.diffuse_color=(*color,1);b=m.node_tree.nodes.get('Principled BSDF');b.inputs['Base Color'].default_value=(*color,1);b.inputs['Metallic'].default_value=metal;b.inputs['Roughness'].default_value=rough
 mats[name]=m;specs.append({'name':m.name,'color':list(color),'metallic':metal,'smoothness':1-rough,'textured':name=='Wood'})
t=mats['Wood'].node_tree.nodes.new('ShaderNodeTexImage');t.image=img;mats['Wood'].node_tree.links.new(t.outputs['Color'],mats['Wood'].node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
parts=[];models=[];items=[];rng=random.Random(41)
def mesh(name,v,f,mat):
 me=bpy.data.meshes.new(name);me.from_pydata(v,[],f);me.update();o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);me.materials.append(mats[mat]);parts.append(o)
 uv=me.uv_layers.new(name='UVMap')
 for p in me.polygons:
  for li in p.loop_indices:
   co=me.vertices[me.loops[li].vertex_index].co;uv.data[li].uv=(.08+co.x*.08,.15+co.z*.08)
 return o
def box(name,pos,size,mat='Wood',bevel=.002):
 bpy.ops.mesh.primitive_cube_add(size=1,location=pos);o=bpy.context.object;o.name=name;o.dimensions=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(mats[mat])
 if bevel:
  mod=o.modifiers.new('Worn edges','BEVEL');mod.width=min(bevel,min(size)*.2);mod.segments=1;bpy.ops.object.modifier_apply(modifier=mod.name)
 uv=o.data.uv_layers.active
 for p in o.data.polygons:
  for li in p.loop_indices:
   co=o.data.vertices[o.data.loops[li].vertex_index].co
   a=co.x/size[0]+.5 if abs(p.normal.y)>.5 or abs(p.normal.z)>.5 else co.y/size[1]+.5
   b=co.y/size[1]+.5 if abs(p.normal.z)>.5 else co.z/size[2]+.5;uv.data[li].uv=(.047+.155*a,.095+.185*b)
 parts.append(o);return o
def lathe(name,profile,mat,n=10,pos=(0,0,0)):
 v=[];rings=[];f=[]
 for r,z in profile:
  ring=[]
  for i in range(n if r else 1):
   angle=2*math.pi*i/n;ring.append(len(v));v.append((pos[0]+r*math.cos(angle),pos[1]+r*math.sin(angle),pos[2]+z))
  rings.append(ring)
 for a,b in zip(rings,rings[1:]):
  for i in range(n):
   face=[a[i%len(a)],a[(i+1)%len(a)],b[(i+1)%len(b)],b[i%len(b)]]
   face=list(dict.fromkeys(face))
   if len(face)>2:f.append(tuple(face))
 return mesh(name,v,f,mat)
def rod(name,a,b,r,mat='Iron',r2=None,n=6):
 a=Vector(a);b=Vector(b);direction=b-a
 bpy.ops.mesh.primitive_cone_add(vertices=n,radius1=r,radius2=r if r2 is None else r2,depth=direction.length,location=(a+b)/2)
 o=bpy.context.object;o.name=name;o.rotation_euler=direction.to_track_quat('Z','Y').to_euler();o.data.materials.append(mats[mat])
 if mat=='Wood':
  for uv in o.data.uv_layers.active.data:uv.uv=(.047+uv.uv.x*.155,.095+uv.uv.y*.185)
 parts.append(o);return o
def path(name,points,r,mat='Iron'):
 for a,b in zip(points,points[1:]):rod(name,a,b,r,mat)
def finish(name):
 bpy.ops.object.select_all(action='DESELECT')
 for o in parts:o.select_set(True)
 bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=bpy.context.object;o.name='SM_'+name
 bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
 bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
 v=o.data.vertices;low=Vector(tuple(min(p.co[i] for p in v) for i in range(3)));high=Vector(tuple(max(p.co[i] for p in v) for i in range(3)));shift=Vector(((low.x+high.x)/2,(low.y+high.y)/2,low.z))
 for p in v:p.co-=shift
 o.data.update();o.modifiers.new('Stable triangulation','TRIANGULATE')
 bpy.ops.export_scene.fbx(filepath=str(ASSET/(o.name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_space_transform=True,apply_unit_scale=True,use_mesh_modifiers=True,bake_anim=False,path_mode='RELATIVE',add_leaf_bones=False)
 items.append({'name':name,'triangles':sum(len(p.vertices)-2 for p in o.data.polygons),'size':[round(float(x),5) for x in high-low]})
 bpy.context.collection.objects.unlink(o);models.append(o);parts.clear()
def book(pos=(0,0,0),color='Burgundy',angle=0):
 start=len(parts);x,y,z=pos
 box('Page block',(x,y,z+.019),(.142,.20,.026),'Paper',.001)
 for zz in (z+.003,z+.036):box('Book cover',(x,y,zz),(.16,.22,.006),color,.001)
 box('Book spine',(x-.077,y,z+.019),(.009,.22,.038),color,.001)
 for yy in (y-.083,y+.083):box('Spine band',(x-.082,yy,z+.019),(.003,.009,.036),'Brass',0)
 for o in parts[start:]:
  p=Vector((x,y,z));o.location=p+Vector((math.cos(angle)*(o.location.x-x)-math.sin(angle)*(o.location.y-y),math.sin(angle)*(o.location.x-x)+math.cos(angle)*(o.location.y-y),o.location.z-z));o.rotation_euler.z=angle
book();finish('DecorBook')
for i,c in enumerate(('Blue','Burgundy','Olive')):book((0,0,i*.04),c,(-.06+i*.05))
finish('DecorBookStack')
for i,c in enumerate(('Burgundy','Olive','Blue','Burgundy','Olive')):
 box('Upright pages',((i-2)*.045,0,.108),(.032,.14,.20),'Paper',0)
 box('Upright spine',((i-2)*.045,-.077,.108),(.041,.016,.216),c,.001)
 for xx in ((i-2)*.045-.019,(i-2)*.045+.019):box('Upright cover',(xx,0,.108),(.004,.16,.216),c,0)
finish('DecorBookRow')
# Wooden crate with raised diagonal braces on all four sides.
box('Crate core',(0,0,.23),(.44,.44,.44))
for s in (-1,1):
 for x in (-.20,.20):box('Crate edge',(x,s*.232,.23),(.055,.025,.46))
 for z in (.025,.435):box('Crate rail',(0,s*.235,z),(.46,.027,.05))
 for sign in (-1,1):
  o=box('Diagonal brace',(0,s*.25,.23),(.50,.025,.038));o.rotation_euler.y=sign*math.radians(43)
 for y in (-.20,.20):box('Crate edge',(s*.232,y,.23),(.025,.055,.46))
 for z in (.025,.435):box('Crate rail',(s*.235,0,z),(.027,.46,.05))
for y in (-.20,.20):box('Top rim',(0,y,.468),(.46,.05,.025))
for x in (-.20,.20):box('Top rim',(x,0,.468),(.05,.46,.025))
finish('DecorCrate')
box('Cardboard parcel',(0,0,.20),(.43,.38,.40),'Cardboard',.006)
box('Top tape',(0,0,.404),(.065,.38,.004),'Paper',0);box('Front tape',(0,-.193,.20),(.065,.004,.40),'Paper',0)
finish('DecorParcel')
box('Leather suitcase',(0,0,.22),(.58,.23,.39),'Jar',.025)
for x in (-.19,.19):
 box('Suitcase strap',(x,-.122,.22),(.032,.012,.39),'Cardboard');box('Strap top',(x,0,.418),(.032,.24,.012),'Cardboard')
for x in (-.265,.265):
 for z in (.05,.39):box('Brass corner',(x,-.125,z),(.05,.012,.055),'Brass',.008)
path('Suitcase handle',[(-.08,0,.415),(-.08,0,.475),(.08,0,.475),(.08,0,.415)],.012,'Wood')
for x in (-.12,.12):box('Latch',(x,-.126,.34),(.025,.012,.045),'Brass')
finish('DecorSuitcase')
lathe('Bottle',[(0,0),(.046,0),(.052,.015),(.052,.22),(.023,.27),(.018,.34),(.023,.342),(.023,.36),(0,.36)],'Bottle',8)
lathe('Bottle label',[(.0525,.10),(.0525,.20)],'Paper',8);finish('DecorBottle')
lathe('Jar',[(0,0),(.058,0),(.068,.025),(.068,.16),(.055,.19),(.055,.205),(0,.205)],'Jar',8)
lathe('Cloth lid',[(0,.211),(.055,.211),(.072,.19),(.064,.175)],'Paper',8)
lathe('Lid tie',[(.064,.183),(.064,.191)],'Cardboard',8);finish('DecorJar')
lathe('Mug',[(0,0),(.038,0),(.049,.012),(.052,.12),(.044,.12),(.041,.02),(0,.02)],'Ceramic',8)
path('Mug handle',[(.048,0,.105),(.085,0,.102),(.09,0,.048),(.047,0,.035)],.008,'Ceramic');finish('DecorMug')
lathe('Plate',[(0,0),(.065,0),(.13,.012),(.145,.032),(.14,.04),(.12,.026),(.07,.012),(0,.012)],'Ceramic',12);finish('DecorPlate')
lathe('Kettle',[(0,0),(.09,0),(.11,.04),(.115,.13),(.085,.19),(.07,.20),(0,.20)],'Iron',10)
lathe('Kettle lid',[(0,.205),(.085,.205),(.067,.223),(0,.223)],'Iron',10)
lathe('Lid knob',[(0,.223),(.019,.223),(.02,.243),(0,.25)],'Wood',8)
path('Kettle handle',[(-.09,0,.17),(-.13,0,.27),(-.08,0,.33),(.07,0,.33),(.12,0,.27),(.085,0,.17)],.011,'Iron')
rod('Kettle spout',(-.09,-.02,.09),(-.185,-.02,.19),.027,'Iron',.016,8)
lathe('Spout hole',[(0,0),(.014,0)],'Ink',8,pos=(-.185,-.02,.192));finish('DecorKettle')
lathe('Vase',[(0,0),(.055,0),(.085,.065),(.09,.14),(.047,.23),(.035,.28),(.055,.33),(.044,.33),(.027,.28),(.038,.23),(.077,.14),(.072,.07),(.045,.015),(0,.015)],'Ceramic',8);finish('DecorVase')
lathe('Candle holder',[(0,0),(.07,0),(.07,.015),(.025,.03),(.016,.12),(.033,.15),(.049,.155),(.049,.17),(.018,.17),(0,.17)],'Brass',10)
lathe('Wax candle',[(0,.17),(.021,.17),(.021,.34),(.017,.35),(0,.35)],'Paper',8)
rod('Wick',(0,0,.345),(0,0,.37),.0015,'Ink',n=4);finish('DecorCandle')
mesh('Folded note',[(-.085,-.12,0),(.085,-.12,.002),(.085,.12,.012),(-.085,.12,.006),(-.085,0,.004),(.085,0,.007)],[(0,1,5,4),(4,5,2,3)],'Paper')
for i in range(5):box('Faded handwriting',(-.01,-.065+i*.022,.006+i*.001),(.10-rng.random()*.035,.0014,.0007),'Ink',0)
finish('DecorNote')
box('Picture back',(0,0,.26),(.34,.025,.49),'Ink')
for x in (-.18,.18):box('Picture upright',(x,-.018,.26),(.035,.04,.55))
for z in (.002,.518):box('Picture rail',(0,-.018,z),(.39,.04,.035))
mesh('Portrait shoulders',[(-.11,-.016,.08),(.11,-.016,.08),(.10,-.016,.18),(.035,-.016,.225),(-.035,-.016,.225),(-.10,-.016,.18)],[(0,1,2,3,4,5)],'Cardboard')
lathe('Portrait head',[(0,0),(.043,.02),(.047,.10),(.025,.13),(0,.135)],'Paper',7,pos=(0,-.02,.23))
# Compress the head's depth so the artwork remains almost flat.
for v in parts[-1].data.vertices:v.co.y=-.017+(v.co.y+.02)*.1
finish('DecorPortrait')
rod('Rolled carpet',(-.40,0,.145),(.40,0,.145),.14,'Burgundy',n=12)
for x in (-.30,.30):rod('Carpet binding',(x-.013,0,.145),(x+.013,0,.145),.142,'Brass',n=12)
pts=[]
for i in range(56):
 a=i/55*math.pi*5;r=.132*(1-i/65);pts.append((.405,math.sin(a)*r,.145+math.cos(a)*r))
path('Rolled end spiral',pts,.004,'Cardboard');finish('DecorRolledRug')
v=[];f=[];n=12
for j,(rx,ry,z) in enumerate([(.39,.31,0),(.32,.27,.10),(.27,.22,.49),(.22,.18,.57)]):
 for i in range(n):
  a=2*math.pi*i/n;corner=max(abs(math.cos(a)),abs(math.sin(a))) if j>0 else 1
  fold=1+(.04 if i%2 else -.04) if j in (1,2) else 1
  v.append((rx*math.cos(a)/corner*fold,ry*math.sin(a)/corner*fold,z+(rng.uniform(0,.018) if j==0 else rng.uniform(-.017,.017))))
for j in range(3):
 for i in range(n):
  a=j*n+i;b=j*n+(i+1)%n;c=(j+1)*n+(i+1)%n;d=(j+1)*n+i;f.extend([(a,b,c),(a,c,d)])
f.append(tuple(range(3*n,4*n)));mesh('Covered furniture',v,f,'Olive');finish('DecorCoveredObject')
lathe('Bucket',[(0,0),(.085,0),(.12,.29),(.11,.30),(.078,.02),(0,.02)],'Iron',10)
lathe('Bucket rim',[(.121,.275),(.124,.29),(.119,.305),(.11,.305)],'Iron',10)
path('Bucket wire',[(-.113,0,.25),(-.14,0,.40),(0,0,.46),(.14,0,.40),(.113,0,.25)],.004,'Iron')
rod('Wooden grip',(-.055,0,.436),(.055,0,.436),.012,'Wood');finish('DecorBucket')
# The library scene is a tidy 6x3 layout; exported model origins stay at floor.
for i,o in enumerate(models):bpy.context.collection.objects.link(o);o.location=((i%6)*.88,(i//6)*1.02,0)
img.pack();bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ManorDecor_Library.blend'))
def aim(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(6,-7,7));cam=bpy.context.object;aim(cam,(2.1,1.0,.1));cam.data.type='ORTHO';cam.data.ortho_scale=5.8
scene=bpy.context.scene;scene.camera=cam
for loc,power,size in [((0,-3,5),800,5),((5,3,5),650,4)]:
 bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.data.energy=power;o.data.size=size;aim(o,(2,1,0))
scene.world=bpy.data.worlds.new('Studio');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.10,.12,.14,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.45
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.render.resolution_x=1900;scene.render.resolution_y=1300;scene.render.resolution_percentage=100;scene.render.filepath=str(ROOT/'ManorDecor_Preview.png');scene.render.image_settings.file_format='PNG';bpy.ops.render.render(write_still=True)
manifest={'materials':specs,'items':items};(ASSET/'DecorManifest.json').write_text(json.dumps(manifest,indent=2));(ROOT/'geometry.json').write_text(json.dumps(items,indent=2));print('MANOR_DECOR_BLENDER_OK',len(items),sum(i['triangles'] for i in items))
