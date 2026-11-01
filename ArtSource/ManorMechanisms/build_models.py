import bpy, math, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parent
OUT=ROOT.parents[1]/'Assets/Props/ManorMechanisms'
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
mats={}
spec=[('Iron',(.16,.19,.20),.7,.67),('Edge',(.38,.41,.40),.8,.48),('Rust',(.24,.10,.045),.3,.85),('Paint',(.24,.035,.028),.2,.78),('Rope',(.31,.24,.14),0,.95),('Brass',(.43,.30,.12),.7,.5)]
for name,c,m,r in spec:
 mat=bpy.data.materials.new('Mechanism_'+name);mat.diffuse_color=(*c,1);mat.use_nodes=True
 bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*c,1);bs.inputs['Metallic'].default_value=m;bs.inputs['Roughness'].default_value=r;mats[name]=mat

def empty(name,p=(0,0,0),parent=None):
 o=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(o);o.location=p;o.parent=parent;return o
def finish(o,name,mat,parent):
 o.name=name;o.data.materials.append(mats[mat]);o.parent=parent
 for poly in o.data.polygons:poly.use_smooth=False
 return o
def box(name,p,size,mat,parent):
 bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.dimensions=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 bevel=o.modifiers.new('Small worn edges','BEVEL');bevel.width=min(.006,min(size)*.15);bevel.segments=1;bpy.ops.object.modifier_apply(modifier=bevel.name)
 return finish(o,name,mat,parent)
def rod(name,a,b,r,mat,parent,n=8):
 a,b=Vector(a),Vector(b);bpy.ops.mesh.primitive_cylinder_add(vertices=n,radius=r,depth=(b-a).length,location=(a+b)/2);o=bpy.context.object;o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();return finish(o,name,mat,parent)
def sweep(name,points,r,mat,parent):
 # Continuous octagonal metal shaft with distinct bends, without overlapping cylinder caps.
 verts=[];faces=[]
 for i,p in enumerate(points):
  tangent=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(i-1,0)])
  u=Vector((0,0,1));v=tangent.normalized().cross(u).normalized()
  for j in range(8):verts.append(Vector(p)+r*(math.cos(j*math.tau/8)*u+math.sin(j*math.tau/8)*v))
 for i in range(len(points)-1):
  for j in range(8):faces.append((i*8+j,i*8+(j+1)%8,(i+1)*8+(j+1)%8,(i+1)*8+j))
 faces.extend([tuple(reversed(range(8))),tuple((len(points)-1)*8+j for j in range(8))])
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);return finish(o,name,mat,parent)
def export(root,name):
 bpy.ops.object.select_all(action='DESELECT')
 for ob in [root]+list(root.children_recursive):ob.select_set(True)
 bpy.context.view_layer.objects.active=root
 bpy.ops.export_scene.fbx(filepath=str(OUT/('SM_'+name+'.fbx')),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',bake_space_transform=True,apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)

crow=empty('Crowbar_72cm')
sweep('Bent octagonal steel',[(-.31,0,.022),(-.28,0,.022),(.23,0,.022),(.29,.015,.022),(.33,.06,.022),(.33,.11,.022),(.30,.14,.022)],.018,'Iron',crow)
for y in (.128,.151):box('Split nail claw',(.273,y,.022),(.075,.016,.013),'Edge',crow)
box('Flattened pry blade',(-.34,0,.022),(.075,.044,.013),'Edge',crow)
for x in (-.18,-.06,.06,.15):rod('Worn red paint',(x-.045,0,.022),(x+.045,0,.022),.0188,'Paint',crow)
export(crow,'Crowbar')
crow.hide_set(True)

winch=empty('ManualWinch_125cm')
box('Foot plate',(0,0,.04),(.68,.57,.08),'Iron',winch)
for x in (-.26,.26):
 for y in (-.19,.19):rod('Anchor bolt',(x,y,.065),(x,y,.09),.026,'Edge',winch,6)
for x in (-.25,.25):
 box('A frame upright',(x,0,.58),(.09,.15,1.08),'Iron',winch)
 rod('Diagonal brace',(x,-.23,.09),(x,0,.69),.035,'Iron',winch)
rod('Drum axle',(-.32,0,.94),(.32,0,.94),.04,'Edge',winch)
rod('Cable drum',(-.19,0,.94),(.19,0,.94),.145,'Iron',winch,16)
for x in (-.21,.21):rod('Drum flange',(x-.015,0,.94),(x+.015,0,.94),.205,'Edge',winch,16)
for i in range(13):
 x=-.18+i*.029
 bpy.ops.mesh.primitive_torus_add(major_segments=20,minor_segments=4,location=(x,0,.94),major_radius=.151,minor_radius=.012,rotation=(0,math.pi/2,0));finish(bpy.context.object,'Wound cable','Rope',winch)
# Front ratchet wheel and visible locking pawl: the source of the timed clicks.
rod('Ratchet wheel',(0,-.11,.94),(0,-.18,.94),.215,'Iron',winch,16)
for i in range(12):
 a=i*math.tau/12;o=box('Ratchet tooth',(.216*math.cos(a),-.15,.94+.216*math.sin(a)),(.055,.065,.055),'Rust',winch);o.rotation_euler.y=-a
box('Locking pawl',(.20,-.20,1.16),(.19,.06,.04),'Brass',winch)
rod('Empty handle socket',(0,-.18,.94),(0,-.25,.94),.055,'Brass',winch,8)
crank=empty('CrankPivot',(0,-.26,.94),winch)
# Child locations are local to the handle pivot.
rod('Removable crank arm',(0,0,0),(.28,0,0),.025,'Edge',crank)
rod('Hand grip',(.28,0,0),(.28,-.14,0),.038,'Paint',crank,10)
export(winch,'ManualWinch')
crow.hide_set(False)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ManorMechanisms.blend'))
(OUT/'materials.json').write_text(json.dumps([{'name':'Mechanism_'+n,'color':list(c),'metallic':m,'smoothness':1-r} for n,c,m,r in spec],indent=2))
print('BLENDER_MODELS_EXPORTED')
