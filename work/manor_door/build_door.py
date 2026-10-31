import bpy, math, json, shutil
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parent
ASSET=ROOT.parents[1]/'Assets'/'Props'/'ManorDoor'
ASSET.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
texpath=ASSET/'ManorDoor_BaseColor.png'
shutil.copy2(ROOT.parent/'textures'/'manor_wall'/'manor_wall_basecolor.png',texpath)
img=bpy.data.images.load(str(texpath))
def mat(name,color,metal=0,rough=.8,textured=False):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
 bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*color,1);bs.inputs['Metallic'].default_value=metal;bs.inputs['Roughness'].default_value=rough
 if textured:
  node=m.node_tree.nodes.new('ShaderNodeTexImage');node.image=img
  m.node_tree.links.new(node.outputs['Color'],bs.inputs['Base Color'])
 return m
wood=mat('M_ManorDoor_Wood',(.12,.07,.04),textured=True)
brass=mat('M_ManorDoor_Brass',(.34,.235,.085),.82,.48)
iron=mat('M_ManorDoor_Iron',(.055,.045,.033),.65,.72)
wallmat=mat('M_ManorDoor_Wall',(.3,.3,.3),textured=True)
parts=[]
def box(name,loc,size,material,bevel=0):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
 ob=bpy.context.object;ob.name=name;ob.dimensions=size
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 ob.data.materials.append(material)
 if bevel:
  mod=ob.modifiers.new('Worn bevel','BEVEL');mod.width=bevel;mod.segments=1
  bpy.context.view_layer.objects.active=ob;bpy.ops.object.modifier_apply(modifier=mod.name)
 # Sample only the wood field inside the first painted panel of our atlas.
 uv=ob.data.uv_layers.active
 for p in ob.data.polygons:
  normal=p.normal
  for li in p.loop_indices:
   v=ob.data.vertices[ob.data.loops[li].vertex_index].co
   a=(v.x/size[0]+.5) if abs(normal.y)>.5 or abs(normal.z)>.5 else (v.y/size[1]+.5)
   b=(v.z/size[2]+.5) if abs(normal.z)<.5 else (v.y/size[1]+.5)
   uv.data[li].uv=(.045+a*.165,.09+b*.195)
 parts.append(ob);return ob
def join(obs,name):
 bpy.ops.object.select_all(action='DESELECT')
 for ob in obs:ob.select_set(True)
 bpy.context.view_layer.objects.active=obs[0];bpy.ops.object.join()
 ob=bpy.context.object;ob.name=name
 bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
 return ob
frame=[]
for x in (.375,1.625):
 frame.append(box('Jamb',(x,.08,1.125),(.17,.30,2.25),wood,.009))
 frame.append(box('Foot',(x,.075,.13),(.205,.335,.26),wood,.009))
 frame.append(box('Capital',(x,.065,2.21),(.205,.335,.13),wood,.008))
frame.append(box('Lintel',(1,.08,2.335),(1.42,.30,.17),wood,.01))
frame.append(box('Crown',(1,.055,2.455),(1.55,.355,.09),wood,.01))
frame=join(frame,'DoorFrame')
leaf=[]
leaf.append(box('DoorSlab',(1,.09,1.13),(1.04,.07,2.21),wood,.008))
# Two recessed fields framed with raised mitred-looking low-poly rails.
for z,h in ((.59,.78),(1.64,.91)):
 for side in (-1,1):
  y=.09+side*.044
  leaf.append(box('InsetField',(1,y,z),(.73,.014,h),wood,.008))
  for x in (.605,1.395):leaf.append(box('PanelStile',(x,y+side*.012,z),(.055,.035,h+.11),wood,.008))
  for zz in (z-h/2-.0275,z+h/2+.0275):leaf.append(box('PanelRail',(1,y+side*.012,zz),(.845,.035,.055),wood,.008))
# Aged brass escutcheons, lever handles and black keyholes on both faces.
for side in (-1,1):
 y=.09+side*.075
 plate=box('BrassEscutcheon',(1.36,y,1.12),(.09,.022,.23),brass,.018);leaf.append(plate)
 leaf.append(box('HandleStem',(1.36,y+side*.032,1.17),(.035,.065,.035),brass,.008))
 leaf.append(box('HandleLever',(1.285,y+side*.065,1.17),(.18,.035,.035),brass,.008))
 leaf.append(box('Keyhole',(1.36,y+side*.013,1.065),(.018,.006,.037),iron,.005))
 for z in (1.205,1.035):leaf.append(box('PlateScrew',(1.36,y+side*.013,z),(.011,.006,.011),iron,.003))
for z in (.37,1.13,1.94):
 bpy.ops.mesh.primitive_cylinder_add(vertices=8,radius=.021,depth=.14,location=(.50,.045,z))
 ob=bpy.context.object;ob.name='HingeBarrel';ob.data.materials.append(iron);leaf.append(ob)
 leaf.append(box('HingeStrap',(.56,.042,z),(.12,.015,.045),iron,.004))
leaf=join(leaf,'DoorLeaf')
bpy.ops.object.empty_add(type='PLAIN_AXES',location=(.50,.09,0))
hinge=bpy.context.object;hinge.name='DoorHinge'
leaf.parent=hinge;leaf.matrix_parent_inverse=hinge.matrix_world.inverted()
# Wall-with-opening module, matching the previously built 2x3m wall.
profile=[(-.055,0),(-.055,.08),(-.035,.10),(-.018,.12),(-.018,1.09),(-.045,1.10),(-.045,1.14),(-.06,1.15),(-.06,1.19),(-.03,1.21),(0,1.22),(0,2.84),(-.02,2.85),(-.02,2.9),(-.05,2.93),(-.07,2.97),(-.07,3),(.2,3),(.2,0)]
def clip(poly,zmin):
 out=[]
 for a,b in zip(poly,poly[1:]+poly[:1]):
  ai=a[1]>=zmin;bi=b[1]>=zmin
  if ai:out.append(a)
  if ai!=bi:
   t=(zmin-a[1])/(b[1]-a[1]);out.append((a[0]+(b[0]-a[0])*t,zmin))
 return out
wv=[];wf=[]
for left,right,poly in [(0,.46,profile),(1.54,2,profile),(.46,1,clip(profile,2.25)),(1,1.54,clip(profile,2.25))]:
 n=len(poly);base=len(wv)
 wv.extend((x,y,z) for x in (left,right) for y,z in poly)
 for j in range(n):wf.append(tuple(base+i for i in (j,n+j,n+(j+1)%n,(j+1)%n)))
 wf.extend([tuple(base+j for j in reversed(range(n))),tuple(base+n+j for j in range(n))])
mesh=bpy.data.meshes.new('DoorWallMesh');mesh.from_pydata(wv,[],wf);mesh.update()
wall=bpy.data.objects.new('DoorWall',mesh);bpy.context.collection.objects.link(wall);mesh.materials.append(wallmat)
uv=mesh.uv_layers.new(name='UVMap')
for p in mesh.polygons:
 is_trim=min(mesh.vertices[i].co.z for i in p.vertices)>=2.8399
 for li in p.loop_indices:
  x,y,z=mesh.vertices[mesh.loops[li].vertex_index].co
  uv.data[li].uv=(.5*(1-abs(x-1)),.035+(z-2.84)*.08 if is_trim else z/3)
bpy.ops.object.select_all(action='DESELECT');wall.select_set(True);bpy.context.view_layer.objects.active=wall
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT')
bpy.ops.object.empty_add(type='PLAIN_AXES');root=bpy.context.object;root.name='ManorDoorAssembly'
for ob in (wall,frame,hinge):ob.parent=root
for ob in (wall,frame,leaf):ob.modifiers.new('Stable Triangles','TRIANGULATE')
for f,angle in [(1,0),(36,100),(72,100),(108,0)]:
 hinge.rotation_euler.z=math.radians(angle);hinge.keyframe_insert(data_path='rotation_euler',frame=f)
if hinge.animation_data and hinge.animation_data.action:hinge.animation_data.action.name='Door_Open_Hold_Close'
scene=bpy.context.scene;scene.frame_start=1;scene.frame_end=108;scene.render.fps=24;scene.frame_set(1)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=str(ASSET/'SM_ManorDoor.fbx'),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',bake_space_transform=True,apply_unit_scale=True,use_mesh_modifiers=True,path_mode='RELATIVE',bake_anim=False,add_leaf_bones=False)
img.pack()
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ManorDoor_Animated.blend'))
# Preview the assembled module next to another, with its leaf opened.
scene.frame_set(1)
for original in (frame,wall):
 ob=original.copy();ob.data=original.data;bpy.context.collection.objects.link(ob);ob.parent=None;ob.location.x+=2.4
hinge2=hinge.copy();hinge2.animation_data_clear();bpy.context.collection.objects.link(hinge2);hinge2.parent=None;hinge2.location.x+=2.4
leaf2=leaf.copy();leaf2.data=leaf.data;bpy.context.collection.objects.link(leaf2);leaf2.parent=hinge2;leaf2.matrix_parent_inverse=leaf.matrix_parent_inverse.copy();hinge2.rotation_euler.z=math.radians(-72)
def aim(ob,p):ob.rotation_euler=(Vector(p)-ob.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(6.8,-9,4.8));cam=bpy.context.object;aim(cam,(2.2,0,1.45));cam.data.type='ORTHO';cam.data.ortho_scale=5.9;scene.camera=cam
for loc,power,size in [((1,-4,6),800,5),((5,-2,4),350,3)]:
 bpy.ops.object.light_add(type='AREA',location=loc);ob=bpy.context.object;ob.data.energy=power;ob.data.size=size;aim(ob,(2,0,1.4))
scene.world=bpy.data.worlds.new('Studio');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.10,.12,.14,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.45
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.render.resolution_x=1400;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.filepath=str(ROOT/'ManorDoor_Preview.png')
bpy.ops.render.render(write_still=True)
stats={ob.name:sum(len(p.vertices)-2 for p in ob.data.polygons) for ob in (wall,frame,leaf)}
(ROOT/'geometry.json').write_text(json.dumps(stats,indent=2));print('DOOR_BLENDER_OK',stats)
