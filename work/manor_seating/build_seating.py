import bpy, math, json, shutil, random
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parent;PROJECT=ROOT.parents[1]
ASSET=PROJECT/'Assets'/'Props'/'ManorSeating';ASSET.mkdir(parents=True,exist_ok=True)
shutil.copy2(PROJECT/'Assets'/'Props'/'ManorTables'/'ManorTables_BaseColor.png',ASSET/'ManorSeating_Wood.png')
bpy.ops.wm.read_factory_settings(use_empty=True)
img=bpy.data.images.load(str(ASSET/'ManorSeating_Wood.png'))
def material(name,color,rough=.88):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
 b=m.node_tree.nodes.get('Principled BSDF');b.inputs['Base Color'].default_value=(*color,1);b.inputs['Roughness'].default_value=rough;return m
wood=material('M_ManorSeating_Wood',(.15,.09,.04),.77)
t=wood.node_tree.nodes.new('ShaderNodeTexImage');t.image=img;wood.node_tree.links.new(t.outputs['Color'],wood.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
olive=material('M_ManorSeating_Olive',(.19,.175,.095))
red=material('M_ManorSeating_Burgundy',(.235,.043,.064),.95)
parts=[];stats={};models=[];rng=random.Random(2077)
def box(name,pos,size,mat=wood,bevel=.005):
 bpy.ops.mesh.primitive_cube_add(size=1,location=pos);o=bpy.context.object;o.name=name;o.dimensions=size
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(mat)
 if bevel:
  mod=o.modifiers.new('Soft edges','BEVEL');mod.width=bevel;mod.segments=1;bpy.ops.object.modifier_apply(modifier=mod.name)
 uv=o.data.uv_layers.active
 for p in o.data.polygons:
  for li in p.loop_indices:
   v=o.data.vertices[o.data.loops[li].vertex_index].co
   a=v.x/size[0]+.5 if abs(p.normal.y)>.5 or abs(p.normal.z)>.5 else v.y/size[1]+.5
   b=v.y/size[1]+.5 if abs(p.normal.z)>.5 else v.z/size[2]+.5
   uv.data[li].uv=(.047+a*.155,.095+b*.185)
 parts.append(o);return o
def cushion(name,pos,size,mat,rotation=(0,0,0),nx=4,ny=3):
 w,d,h=size;v=[];faces=[]
 for j in range(ny+1):
  for i in range(nx+1):
   u=i/nx;q=j/ny
   bulge=math.sin(math.pi*u)*math.sin(math.pi*q)
   v.append(((u-.5)*w,(q-.5)*d,h*.3+bulge*h*.42+(rng.uniform(-.008,.008) if bulge>.01 else 0)))
 for j in range(ny):
  for i in range(nx):
   a=j*(nx+1)+i;b=a+1;c=a+nx+2;e=a+nx+1
   faces.extend([(a,b,c),(a,c,e)] if (i+j)%2 else [(a,b,e),(b,c,e)])
 edge=list(range(nx+1))+[j*(nx+1)+nx for j in range(1,ny+1)]+[ny*(nx+1)+i for i in range(nx-1,-1,-1)]+[j*(nx+1) for j in range(ny-1,0,-1)]
 bottom=[]
 for index in edge:
  x,y,z=v[index];bottom.append(len(v));v.append((x*.96,y*.96,-h*.5))
 for k,a in enumerate(edge):faces.append((a,bottom[k],bottom[(k+1)%len(edge)],edge[(k+1)%len(edge)]))
 faces.append(tuple(reversed(bottom)))
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(v,[],faces);mesh.update()
 o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);o.location=pos;o.rotation_euler=rotation;mesh.materials.append(mat)
 uv=mesh.uv_layers.new(name='UVMap')
 for p in mesh.polygons:
  for li in p.loop_indices:
   co=mesh.vertices[mesh.loops[li].vertex_index].co;uv.data[li].uv=(co.x/w+.5,co.y/d+.5)
 parts.append(o);return o
def finish(name):
 bpy.ops.object.select_all(action='DESELECT')
 for o in parts:o.select_set(True)
 bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=bpy.context.object;o.name='SM_'+name
 bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');o.modifiers.new('Stable triangulation','TRIANGULATE')
 stats[name]={'triangles':sum(len(p.vertices)-2 for p in o.data.polygons),'dimensions_m':[round(x,3) for x in o.dimensions]}
 assert abs(min(v.co.z for v in o.data.vertices))<.001,'Origin/ground mismatch'
 bpy.ops.export_scene.fbx(filepath=str(ASSET/(o.name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_space_transform=True,apply_unit_scale=True,use_mesh_modifiers=True,bake_anim=False,path_mode='RELATIVE',add_leaf_bones=False)
 img.pack();bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/(name+'.blend')))
 bpy.context.collection.objects.unlink(o);models.append(o);parts.clear();return o
# Dining chair, a burgundy seat and tall open wooden back.
for x in (-.185,.185):
 for y in (-.185,.185):box('Chair leg',(x,y,.205),(.047,.047,.41),bevel=.003)
box('Seat frame',(0,0,.405),(.47,.48,.055))
cushion('Red seat',(0,-.005,.452),(.43,.44,.065),red)
for x in (-.185,.185):box('Tall back post',(x,.19,.705),(.045,.055,.63),bevel=.003)
for x in (-.10,0,.10):
 o=box('Back slat',(x,.20,.72),(.032,.035,.39),bevel=.002);o.rotation_euler.y=x*.65
box('Crest lower',(0,.20,.958),(.37,.055,.064))
o=box('Crest top',(0,.20,.99),(.25,.055,.035),bevel=.008)
for x in (-.185,.185):box('Side stretcher',(x,0,.16),(.03,.37,.035),bevel=.002)
box('Front stretcher',(0,-.185,.16),(.37,.03,.035),bevel=.002)
chair=finish('ManorChair')
def upholstered(name,width):
 hw=width/2;depth=.82
 for x in (-hw+.105,hw-.105):
  for y in (-.285,.285):
   box('Wooden foot',(x,y,.085),(.075,.08,.17),bevel=.004)
 box('Wooden base',(0,0,.207),(width,.79,.095),bevel=.007)
 box('Seat base',(0,.01,.31),(width-.13,.70,.13),olive,.018)
 seatwidth=width-.24
 cushion('Faceted seat',(0,-.045,.421),(seatwidth,.64,.15),olive,nx=6 if width>1 else 3)
 cushion('Faceted back',(0,.285,.727),(width-.16,.55,.15),olive,(math.radians(80),0,0),nx=6 if width>1 else 3)
 for s in (-1,1):
  x=s*(hw-.065)
  box('Wood side frame',(x,.015,.40),(.075,.71,.35),wood,.006)
  box('Upholstered side',(x,.005,.49),(.115,.70,.43),olive,.025)
  box('Armrest',(x,-.02,.67),(.14,.75,.105),olive,.022)
 # Loose cushions lean on the back, included in this static mesh.
 for s in ((1,) if width<1 else (-1,1)):
  cushion('Loose burgundy cushion',(s*(width*.30),.07,.69),(.30,.34,.085),red,(math.radians(69),math.radians(s*7),math.radians(-s*12)),nx=3,ny=3)
 if width>1:
  # A triangulated cloth strip over the right seat, with irregular hanging hem.
  path=[(.13,.57),(.04,.555),(-.12,.55),(-.30,.54),(-.38,.49),(-.43,.34),(-.44,.15),(-.46,.045)]
  vv=[]
  for j,(y,z) in enumerate(path):
   for i in range(6):
    x=.40+i*.075;fold=math.sin(i*math.pi*.9)*(.014 if j<4 else .023)
    vv.append((x,y+fold,z+(.008*math.cos(i*2) if j<4 else (.012*math.sin(i*2) if j==len(path)-1 else 0))))
  ff=[]
  for j in range(len(path)-1):
   for i in range(5):
    a=j*6+i;ff.extend([(a,a+1,a+7),(a,a+7,a+6)])
  mesh=bpy.data.meshes.new('Draped throw');mesh.from_pydata(vv,[],ff);mesh.update()
  o=bpy.data.objects.new('Burgundy draped throw',mesh);bpy.context.collection.objects.link(o);mesh.materials.append(red)
  bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
  mod=o.modifiers.new('Cloth thickness','SOLIDIFY');mod.thickness=.004;bpy.ops.object.modifier_apply(modifier=mod.name)
  parts.append(o)
 return finish(name)
armchair=upholstered('ManorArmchair',.85)
sofa=upholstered('ManorSofa',1.9)
for o,loc in zip(models,[(-1.85,0,0),(-.8,0,0),(.85,0,0)]):bpy.context.collection.objects.link(o);o.location=loc
def aim(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(3,-6,2.7));cam=bpy.context.object;aim(cam,(-.1,0,.5));cam.data.type='ORTHO';cam.data.ortho_scale=4.9
scene=bpy.context.scene;scene.camera=cam
for loc,power,size in [((-2,-3,4),500,4),((3,1,4),450,3)]:
 bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.data.energy=power;o.data.size=size;aim(o,(0,0,.5))
scene.world=bpy.data.worlds.new('Studio');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.10,.12,.14,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.45
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.render.resolution_x=1700;scene.render.resolution_y=950;scene.render.resolution_percentage=100;scene.render.filepath=str(ROOT/'ManorSeating_Preview.png');scene.render.image_settings.file_format='PNG'
bpy.ops.render.render(write_still=True)
(ROOT/'geometry.json').write_text(json.dumps(stats,indent=2));print('MANOR_SEATING_BLENDER_OK',json.dumps(stats))
