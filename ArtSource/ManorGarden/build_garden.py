import bpy,math,json,random
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parent
PROJECT=ROOT/'stage'
if ROOT.parent.name=='ArtSource':PROJECT=ROOT.parents[1]
OUT=PROJECT/'Assets/Props/ManorGarden';SRC=PROJECT/'ArtSource/ManorGarden';OUT.mkdir(parents=True,exist_ok=True);SRC.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
palette={'Bark':(.12,.065,.035),'Leaf':(.11,.22,.075),'LeafLight':(.23,.34,.11),'Sage':(.22,.3,.22),'Ivory':(.84,.80,.67),'Gold':(.78,.43,.035),'Purple':(.28,.10,.45),'Red':(.57,.065,.04),'Pink':(.62,.24,.32),'Clay':(.3,.13,.065),'Wicker':(.28,.17,.075),'BookGreen':(.065,.14,.105),'BookRed':(.22,.045,.035)}
for name,c in palette.items():
 m=bpy.data.materials.new('Garden_'+name);m.diffuse_color=(*c,1);m.use_nodes=True;bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*c,1);bs.inputs['Roughness'].default_value=.88
def mat(o,m):o.data.materials.append(bpy.data.materials['Garden_'+m]);return o
def box(n,p,s,m):
 bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.name=n;o.dimensions=s;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return mat(o,m)
def ball(n,p,s,m,sub=1):
 bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=p);o=bpy.context.object;o.name=n;o.scale=s;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);return mat(o,m)
def rod(n,a,b,r,m,r2=None):
 a,b=Vector(a),Vector(b);bpy.ops.mesh.primitive_cone_add(vertices=8,radius1=r,radius2=r if r2 is None else r2,depth=(b-a).length,location=(a+b)/2);o=bpy.context.object;o.name=n;o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();return mat(o,m)
def leaf(p,length=.12,yaw=0):
 o=ball('Leaf',p,(length,.032,.012),'LeafLight');o.rotation_euler[2]=yaw
def stem(h):
 rod('Living stem',(0,0,0),(0,0,h),.009,'Leaf',.005)
 for i in range(3):leaf(((-1)**i*.07,0,.12+i*.09),.095,i*.9)
def daisy():
 stem(.43)
 for i in range(13):
  a=i*math.tau/13;o=ball('White ray petal',(math.cos(a)*.064,math.sin(a)*.064,.44),(.055,.016,.012),'Ivory');o.rotation_euler[2]=a
 ball('Yellow daisy centre',(0,0,.454),(.033,.033,.018),'Gold',2)
def poppy():
 stem(.52)
 for i in range(5):
  a=i*math.tau/5;o=ball('Broad red petal',(.037*math.cos(a),.037*math.sin(a),.525),(.066,.045,.018),'Red');o.rotation_euler[2]=a
 ball('Dark seed head',(0,0,.546),(.022,.022,.012),'Bark')
def lupin():
 stem(.70)
 for j in range(7):
  for i in range(4):
   a=i*math.tau/4+j*.6;r=.034*(1-j*.08);ball('Purple bell',(math.cos(a)*r,math.sin(a)*r,.34+j*.05),(.025,.023,.028),'Purple')
def marigold():
 stem(.31)
 for i in range(9):
  a=i*math.tau/9;ball('Gold layered petal',(.038*math.cos(a),.038*math.sin(a),.32),(.031,.031,.02),'Gold')
 ball('Marigold heart',(0,0,.34),(.035,.035,.02),'Gold')
def shrub():
 for i in range(7):
  a=i*2.4;p=(math.cos(a)*.35,math.sin(a)*.28,.28+(i%3)*.12);rod('Shrub branch',(0,0,0),p,.025,'Bark',.008);ball('Shrub crown',p,(.32,.28,.29),'Sage' if i%3==0 else 'Leaf',2)
def tree():
 rod('Old trunk',(0,0,0),(.12,0,2.6),.17,'Bark',.06)
 for i in range(7):
  a=i*2.4;h=1.6+i*.19;p=(math.cos(a)*(.8 if i<5 else .5),math.sin(a)*.7,h+.6)
  rod('Branch',(.05,0,h-.2),p,.07,'Bark',.012);ball('Faceted tree crown',p,(.85,.68,.7),'LeafLight' if i%3==0 else 'Leaf',2)
def jug():
 # Open ceramic vessel, modelled as a low-poly surface of revolution.
 profile=[(.0,.07),(.025,.10),(.10,.135),(.20,.12),(.29,.065),(.34,.065),(.34,.052),(.29,.052),(.20,.10),(.10,.11),(.025,.075)]
 verts=[];faces=[]
 for z,r in profile:
  for i in range(12):a=i*math.tau/12;verts.append((r*math.cos(a),r*math.sin(a),z))
 for j in range(len(profile)-1):
  for i in range(12):faces.append((j*12+i,j*12+(i+1)%12,(j+1)*12+(i+1)%12,(j+1)*12+i))
 mesh=bpy.data.meshes.new('Jug hollow mesh');mesh.from_pydata(verts,[],faces);o=bpy.data.objects.new('Ceramic jug',mesh);bpy.context.collection.objects.link(o);mat(o,'Clay')
 for i in range(7):
  a1=math.pi*(-.65+i*.22);a2=math.pi*(-.65+(i+1)*.22)
  rod('Jug handle',(.13+.075*math.cos(a1),0,.18+.105*math.sin(a1)),(.13+.075*math.cos(a2),0,.18+.105*math.sin(a2)),.015,'Clay')
def basket():
 box('Basket base',(0,0,.025),(.48,.34,.05),'Wicker')
 for j in range(5):
  z=.06+j*.04
  for y in [-.17,.17]:box('Woven horizontal',(0,y,z),(.5,.025,.025),'Wicker')
  for x in [-.24,.24]:box('Woven end',(x,0,z),(.025,.34,.025),'Wicker')
 for i in range(9):
  for y in [-.175,.175]:box('Basket upright',(-.23+i*.0575,y,.13),(.016,.028,.24),'Wicker')
 for i in range(10):
  a1=i*math.pi/10;a2=(i+1)*math.pi/10
  rod('Basket carry handle',(.25*math.cos(a1),0,.24+.18*math.sin(a1)),(.25*math.cos(a2),0,.24+.18*math.sin(a2)),.018,'Wicker')
def books():
 for i in range(3):
  z=.01+i*.065;box('Book pages',(0,0,z+.025),(.25,.18,.045),'Ivory')
  for zz in [z,z+.05]:o=box('Book cover',(0,0,zz),(.27,.20,.012),'BookRed' if i%2 else 'BookGreen');o.rotation_euler[2]=i*.07
for name,fn in [('Daisy',daisy),('Poppy',poppy),('Lupin',lupin),('Marigold',marigold),('Shrub',shrub),('Tree',tree),('Jug',jug),('Basket',basket),('BookStack',books)]:
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);fn();bpy.ops.object.select_all(action='SELECT')
 meshes=[o for o in bpy.context.selected_objects if o.type=='MESH'];bpy.context.view_layer.objects.active=meshes[0];bpy.ops.object.join();bpy.context.object.name=name+' visual';bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 root=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(root)
 for o in list(bpy.context.selected_objects):o.parent=root
 root.select_set(True);bpy.context.view_layer.objects.active=root
 bpy.ops.export_scene.fbx(filepath=str(OUT/('SM_'+name+'.fbx')),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',bake_space_transform=True,apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
 bpy.ops.wm.save_as_mainfile(filepath=str(SRC/(name+'.blend')))
(OUT/'palette.json').write_text(json.dumps({'materials':[{'name':'Garden_'+n,'color':list(c)} for n,c in palette.items()]},indent=2))
print('GARDEN_MODELS_READY')
