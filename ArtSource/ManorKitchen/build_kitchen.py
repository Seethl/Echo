import bpy, math, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parent
OUT=ROOT.parents[1]/'Assets/Props/ManorKitchen';OUT.mkdir(parents=True,exist_ok=True)
SRC=ROOT;SRC.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
palette={
 'Kitchen_Enamel':(.46,.48,.39,0,.82),'Kitchen_Rust':(.19,.075,.035,0,.94),
 'Kitchen_Iron':(.055,.065,.06,.35,.75),'Kitchen_Zinc':(.28,.32,.31,.45,.65),
 'Kitchen_Wood':(.14,.075,.035,0,.9),'Kitchen_Ceramic':(.63,.57,.43,0,.65),
 'Kitchen_Paper':(.53,.40,.22,0,.95),'Kitchen_Green':(.14,.23,.12,0,.9),
 'Kitchen_Red':(.32,.085,.045,0,.9),'Kitchen_Blue':(.12,.19,.22,0,.85)}
for name,(r,g,b,m,rough) in palette.items():
 mat=bpy.data.materials.new(name);mat.diffuse_color=(r,g,b,1);mat.use_nodes=True
 bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(r,g,b,1);bs.inputs['Metallic'].default_value=m;bs.inputs['Roughness'].default_value=rough

def box(name,p,size,mat,bevel=0):
 bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.name=name;o.dimensions=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(bpy.data.materials[mat])
 if bevel:
  mod=o.modifiers.new('Worn edges','BEVEL');mod.width=bevel;mod.segments=1;bpy.ops.object.modifier_apply(modifier=mod.name)
 return o
def rod(name,a,b,r,mat,n=10):
 a,b=Vector(a),Vector(b);bpy.ops.mesh.primitive_cylinder_add(vertices=n,radius=r,depth=(b-a).length,location=(a+b)/2);o=bpy.context.object;o.name=name;o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();o.data.materials.append(bpy.data.materials[mat]);return o
def export(name,fn):
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);fn()
 # Local origin is on the floor; front faces -Y, preserving metre dimensions.
 bpy.ops.object.select_all(action='SELECT')
 root=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(root)
 for o in list(bpy.context.selected_objects):o.parent=root
 root.select_set(True);bpy.context.view_layer.objects.active=root
 bpy.ops.export_scene.fbx(filepath=str(OUT/('SM_'+name+'.fbx')),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',bake_space_transform=True,apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
 bpy.ops.wm.save_as_mainfile(filepath=str(SRC/(name+'.blend')))

def fridge():
 box('Unpowered icebox body',(0,0,.91),(.82,.69,1.72),'Kitchen_Enamel',.025)
 box('Dark door gasket',(0,-.353,.96),(.76,.035,1.56),'Kitchen_Iron',.01)
 for z,h in [(1.5,.46),(.76,.93)]:box('Enamel door',(0,-.395,z),(.735,.07,h),'Kitchen_Enamel',.02)
 for z in [.65,1.51]:rod('Door handle',(.24,-.475,z-.10),(.24,-.475,z+.10),.018,'Kitchen_Iron')
 for x in [-.29,.29]:
  for y in [-.24,.24]:box('Fridge foot',(x,y,.045),(.09,.10,.09),'Kitchen_Iron')
 for i in range(7):box('Lower ventilation slot',(-.245+i*.08,-.405,.17),(.04,.008,.055),'Kitchen_Iron')
 for i in range(9):box('Rust fleck',(-.30+(i%4)*.17,-.435,.35+(i//4)*.53),(.032+i*.002,.003,.022),'Kitchen_Rust')

def sink():
 for x in [-1.4,1.4]:
  for y in [-.25,.25]:box('Counter foot',(x,y,.02),(.08,.08,.04),'Kitchen_Iron')
 box('Counter cabinet',(0,.04,.43),(3.2,.70,.80),'Kitchen_Wood',.01)
 # Open basin consists of side walls and a recessed bottom, never a solid oval.
 box('Left worktop',(-.99,0,.865),(1.22,.80,.07),'Kitchen_Zinc',.008)
 box('Right worktop',(.99,0,.865),(1.22,.80,.07),'Kitchen_Zinc',.008)
 for y in [-.355,.355]:box('Basin lip',(0,y,.865),(.76,.09,.07),'Kitchen_Zinc',.008)
 for x in [-.365,.365]:box('Basin side',(x,0,.77),(.035,.64,.20),'Kitchen_Zinc')
 for y in [-.315,.315]:box('Basin wall',(0,y,.77),(.72,.035,.20),'Kitchen_Zinc')
 box('Basin floor',(0,0,.675),(.72,.64,.035),'Kitchen_Zinc',.008)
 rod('Drain',(0,0,.693),(0,0,.697),.055,'Kitchen_Iron')
 for x in [-1.15,-.4,.4,1.15]:
  box('Cupboard front',(x,-.322,.43),(.72,.035,.67),'Kitchen_Wood',.008)
  box('Cupboard pull',(x,-.365,.62),(.14,.035,.023),'Kitchen_Iron')
 rod('Tap rise',(0,.285,.90),(0,.285,1.17),.024,'Kitchen_Zinc')
 rod('Tap neck',(0,.285,1.17),(0,.05,1.17),.024,'Kitchen_Zinc')
 rod('Tap spout',(0,.05,1.17),(0,.05,1.11),.024,'Kitchen_Zinc')
 for x in [-.18,.18]:
  rod('Tap valve',(x,.28,.90),(x,.28,.96),.02,'Kitchen_Zinc');rod('Cross handle',(x-.045,.28,.96),(x+.045,.28,.96),.012,'Kitchen_Iron')

def dresser():
 box('Dresser back',(0,.22,1.05),(1.28,.07,2.10),'Kitchen_Wood')
 for x in [-.62,.62]:box('Dresser upright',(x,0,1.05),(.09,.56,2.10),'Kitchen_Wood')
 for z in [.12,.79,1.30,1.83,2.11]:box('Dresser shelf',(0,0,z),(1.35,.61,.065),'Kitchen_Wood',.008)
 for x in [-.33,.33]:box('Lower door',(x,-.29,.45),(.57,.04,.55),'Kitchen_Wood',.008);box('Door knob',(x,-.325,.6),(.045,.045,.045),'Kitchen_Iron')
 for z in [.855,1.365]:
  for i in range(4):rod('Plate stack',(-.36,-.04,z+i*.018),(-.36,-.04,z+i*.018+.012),.18,'Kitchen_Ceramic',14)
  for x in [.12,.35]:
   rod('Cup',(x,-.02,z),(x,-.02,z+.13),.06,'Kitchen_Ceramic',10)
   box('Cup handle',(x+.07,-.02,z+.07),(.035,.05,.06),'Kitchen_Ceramic')
 for x in [-.4,0,.4]:
  rod('Preserve jar',(x,0,1.865),(x,0,2.01),.09,'Kitchen_Green');rod('Jar lid',(x,0,2.01),(x,0,2.035),.095,'Kitchen_Iron')

def packet():
 box('Flour carton',(0,0,.14),(.19,.12,.28),'Kitchen_Paper',.006)
 box('Faded label',(0,-.061,.15),(.14,.004,.16),'Kitchen_Red')
 for i in range(3):box('Printed stripe',(0,-.065,.12+i*.025),(.09,.002,.006),'Kitchen_Paper')
 box('Folded carton top',(0,0,.288),(.18,.115,.02),'Kitchen_Paper',.006)
def tea():
 box('Tea tin',(0,0,.09),(.17,.14,.18),'Kitchen_Green',.012);box('Tin lid',(0,0,.19),(.178,.148,.025),'Kitchen_Iron',.008)
 box('Tin label',(0,-.071,.10),(.12,.003,.09),'Kitchen_Paper')
def preserve():
 rod('Preserve tin',(0,0,.01),(0,0,.15),.065,'Kitchen_Red',12);rod('Tin top',(0,0,.15),(0,0,.158),.069,'Kitchen_Iron',12)
 box('Paper band',(0,-.064,.08),(.085,.003,.055),'Kitchen_Paper')

for name,fn in [('Icebox',fridge),('SinkCounter',sink),('DishDresser',dresser),('FlourCarton',packet),('TeaTin',tea),('PreserveTin',preserve)]:export(name,fn)
(OUT/'palette.json').write_text(json.dumps({'materials':[{'name':name,'color':list(v[:3]),'metallic':v[3],'roughness':v[4]} for name,v in palette.items()]},indent=2))
print('ECHO_KITCHEN_MODELS_EXPORTED')
