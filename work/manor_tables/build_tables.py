import bpy, math, json, shutil
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parent
PROJECT=ROOT.parents[1]
ASSET=PROJECT/'Assets'/'Props'/'ManorTables'
ASSET.mkdir(parents=True,exist_ok=True)
shutil.copy2(PROJECT/'Assets'/'Props'/'ManorDoor'/'ManorDoor_BaseColor.png',ASSET/'ManorTables_BaseColor.png')
bpy.ops.wm.read_factory_settings(use_empty=True)
image=bpy.data.images.load(str(ASSET/'ManorTables_BaseColor.png'))
wood=bpy.data.materials.new('M_ManorTables_Wood');wood.use_nodes=True
bs=wood.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.77
tex=wood.node_tree.nodes.new('ShaderNodeTexImage');tex.image=image;wood.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
brass=bpy.data.materials.new('M_ManorTables_Brass');brass.use_nodes=True;brass.diffuse_color=(.34,.23,.08,1)
bs=brass.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(.34,.23,.08,1);bs.inputs['Metallic'].default_value=.8;bs.inputs['Roughness'].default_value=.5
parts=[]
def box(name,pos,size,bevel=.004,mat=None):
 bpy.ops.mesh.primitive_cube_add(size=1,location=pos);ob=bpy.context.object;ob.name=name;ob.dimensions=size
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 ob.data.materials.append(mat or wood)
 if bevel:
  mod=ob.modifiers.new('Worn corners','BEVEL');mod.width=bevel;mod.segments=1
  bpy.context.view_layer.objects.active=ob;bpy.ops.object.modifier_apply(modifier=mod.name)
 uv=ob.data.uv_layers.active
 for p in ob.data.polygons:
  for li in p.loop_indices:
   v=ob.data.vertices[ob.data.loops[li].vertex_index].co
   a=v.x/size[0]+.5 if abs(p.normal.y)>.5 or abs(p.normal.z)>.5 else v.y/size[1]+.5
   b=v.y/size[1]+.5 if abs(p.normal.z)>.5 else v.z/size[2]+.5
   # Reuse the unadorned wood field of our manor texture atlas.
   uv.data[li].uv=(.047+a*.155,.095+b*.185)
 parts.append(ob);return ob
def diamond(pos):
 ob=box('Diamond pull backplate',pos,(.024,.012,.024),.002,brass)
 ob.rotation_euler.y=math.pi/4
 box('Brass knob',(pos[0],pos[1]-.014,pos[2]),(.015,.022,.018),.004,brass)
def finish(name):
 bpy.ops.object.select_all(action='DESELECT')
 for ob in parts:ob.select_set(True)
 bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();ob=bpy.context.object;ob.name='SM_'+name
 bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
 ob.modifiers.new('Stable triangulation','TRIANGULATE')
 ob['style']='Low-poly old manor furniture; metre scale; origin centred at floor.'
 ob['drawers']='Static decorative fronts; no opening animation.' if name=='ManorDesk' else 'No drawers'
 bpy.ops.export_scene.fbx(filepath=str(ASSET/(ob.name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_space_transform=True,apply_unit_scale=True,use_mesh_modifiers=True,bake_anim=False,path_mode='RELATIVE',add_leaf_bones=False)
 stats[name]={'triangles':sum(len(p.vertices)-2 for p in ob.data.polygons),'width_m':round(ob.dimensions.x,3),'depth_m':round(ob.dimensions.y,3),'height_m':round(ob.dimensions.z,3)}
 assert abs(ob.dimensions.z-.78)<.001
 image.pack();bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/(name+'.blend')))
 parts.clear();return ob
stats={}
# Double-pedestal writing desk: 1.60 x .76 x .78 m.
box('Thick desktop',(0,0,.751),(1.60,.76,.058),.008)
box('Top underlip',(0,0,.713),(1.49,.68,.025),.004)
for s in (-1,1):
 x=s*.58
 box('Pedestal body',(x,.015,.378),(.36,.61,.616),.005)
 box('Foot plinth',(x,.015,.062),(.41,.66,.065),.006)
 for sx in (-1,1):
  for sy in (-1,1):box('Short block foot',(x+sx*.153,.015+sy*.255,.022),(.075,.095,.044),.003)
 for xx in (x-.152,x+.152):box('Front stile',(xx,-.309,.387),(.043,.042,.583),.003)
 box('Upper drawer front',(x,-.314,.627),(.263,.036,.122),.004)
 box('Lower door front',(x,-.315,.331),(.263,.038,.437),.004)
 box('Lower inset field',(x,-.337,.323),(.194,.012,.335),.003)
 for xx in (x-.112,x+.112):box('Raised panel edge',(xx,-.344,.325),(.021,.022,.382),.003)
 for zz in (.134,.516):box('Raised panel edge',(x,-.344,zz),(.245,.022,.022),.003)
 diamond((x,-.341,.635));diamond((x,-.351,.475))
 # Framed outer side panel.
 box('Side inset',(x+s*.183,.025,.37),(.014,.46,.43),.003)
 for yy in (-.222,.272):box('Side border',(x+s*.195,yy,.37),(.025,.027,.49),.003)
 for zz in (.125,.615):box('Side border',(x+s*.195,.025,zz),(.025,.52,.027),.003)
box('Knee apron',(0,-.279,.663),(.80,.045,.09),.004)
box('Back modesty rail',(0,.285,.51),(.80,.045,.31),.004)
desk=finish('ManorDesk')
bpy.context.collection.objects.unlink(desk)
# Simple trestle-like table: 1.80 x .85 x .78 m.
box('Thick tabletop',(0,0,.75),(1.8,.85,.06),.008)
for x in (-.735,.735):
 for y in (-.275,.275):
  box('Straight leg',(x,y,.37),(.105,.105,.70),.004)
  box('Block foot',(x,y,.045),(.14,.14,.09),.004)
 box('Side low stretcher',(x,0,.155),(.085,.55,.09),.003)
 box('Side upper apron',(x,0,.665),(.065,.63,.11),.003)
for y in (-.285,.285):box('Long apron',(0,y,.665),(1.47,.06,.11),.003)
box('Long low stretcher',(0,0,.155),(1.47,.075,.095),.004)
table=finish('ManorTable')
bpy.context.collection.objects.link(desk)
desk.location=(-1.04,0,0);table.location=(1.1,0,0)
def aim(ob,p):ob.rotation_euler=(Vector(p)-ob.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(3.2,-5,2.5));cam=bpy.context.object;aim(cam,(0,0,.39));cam.data.type='ORTHO';cam.data.ortho_scale=4.6
scene=bpy.context.scene;scene.camera=cam
for loc,power,size in [((-2,-3,4),500,4),((3,1,3),350,3)]:
 bpy.ops.object.light_add(type='AREA',location=loc);ob=bpy.context.object;ob.data.energy=power;ob.data.size=size;aim(ob,(0,0,.4))
scene.world=bpy.data.worlds.new('Studio');scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.10,.12,.14,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.45
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.render.resolution_x=1600;scene.render.resolution_y=900;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.filepath=str(ROOT/'ManorTables_Preview.png')
bpy.ops.render.render(write_still=True)
(ROOT/'geometry.json').write_text(json.dumps(stats,indent=2));print('MANOR_TABLES_BLENDER_OK',json.dumps(stats))
