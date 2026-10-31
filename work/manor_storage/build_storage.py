import bpy, math, json, shutil, random
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parent;PROJECT=ROOT.parents[1];ASSET=PROJECT/'Assets'/'Props'/'ManorStorage';ASSET.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
shutil.copy2(PROJECT/'Assets'/'Props'/'ManorTables'/'ManorTables_BaseColor.png',ASSET/'ManorStorage_Wood.png')
img=bpy.data.images.load(str(ASSET/'ManorStorage_Wood.png'))
def mat(name,color,metal=0,rough=.8):
 m=bpy.data.materials.new(name);m.use_nodes=True;m.diffuse_color=(*color,1)
 b=m.node_tree.nodes.get('Principled BSDF');b.inputs['Base Color'].default_value=(*color,1);b.inputs['Metallic'].default_value=metal;b.inputs['Roughness'].default_value=rough;return m
wood=mat('M_ManorStorage_Wood',(.12,.07,.04));t=wood.node_tree.nodes.new('ShaderNodeTexImage');t.image=img;wood.node_tree.links.new(t.outputs['Color'],wood.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
brass=mat('M_ManorStorage_Brass',(.34,.23,.08),.8,.5)
bookmats=[mat('M_ManorBooks_'+n,c) for n,c in [('Burgundy',(.17,.035,.045)),('Olive',(.095,.12,.065)),('Blue',(.045,.075,.10)),('Brown',(.19,.10,.045))]]
paper=mat('M_ManorBooks_Paper',(.40,.34,.23))
parts=[];models=[];stats={};rng=random.Random(19)
def box(name,pos,size,material=wood,bevel=.004):
 bpy.ops.mesh.primitive_cube_add(size=1,location=pos);o=bpy.context.object;o.name=name;o.dimensions=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(material)
 if bevel:
  mod=o.modifiers.new('Worn edges','BEVEL');mod.width=bevel;mod.segments=1;bpy.ops.object.modifier_apply(modifier=mod.name)
 uv=o.data.uv_layers.active
 for p in o.data.polygons:
  for li in p.loop_indices:
   v=o.data.vertices[o.data.loops[li].vertex_index].co
   a=v.x/size[0]+.5 if abs(p.normal.y)>.5 or abs(p.normal.z)>.5 else v.y/size[1]+.5
   b=v.y/size[1]+.5 if abs(p.normal.z)>.5 else v.z/size[2]+.5
   uv.data[li].uv=(.047+a*.155,.095+b*.185)
 parts.append(o);return o
def join(name):
 bpy.ops.object.select_all(action='DESELECT')
 for o in parts:o.select_set(True)
 bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
 bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');o.modifiers.new('Stable triangulation','TRIANGULATE');parts.clear();return o
def empty(name,pos=(0,0,0)):
 bpy.ops.object.empty_add(type='PLAIN_AXES',location=pos);o=bpy.context.object;o.name=name;return o
def parent(o,p):o.parent=p;o.matrix_parent_inverse=p.matrix_world.inverted()
def shell(height,depth,bookcase=False):
 for x in (-.49,.49):
  box('Side panel',(x,0,height/2),(.065,depth,height-.16))
  for z in (.035,height-.11):box('Side edge',(x,0,z),(.12,depth+.035,.07))
  # Outer side panel trim.
  for y in (-depth/2+.035,depth/2-.035):box('Side frame',(x+(.038 if x>0 else -.038),y,height*.50),(.025,.035,height-.34))
 box('Back',(0,depth/2-.015,height/2),(.95,.03,height-.18),bevel=.002)
 box('Bottom plinth',(0,0,.105),(1.10,depth+.065,.085),bevel=.007)
 box('Base shelf',(0,0,.175),(.96,depth-.025,.055))
 box('Crown lower',(0,0,height-.095),(1.09,depth+.05,.07),bevel=.006)
 box('Crown top',(0,0,height-.035),(1.15,depth+.095,.07),bevel=.007)
 for x in (-.475,.475):
  for y in (-depth/2+.06,depth/2-.06):box('Foot',(x,y,.035),(.10,.10,.07))
 levels=(.64,1.02,1.40) if bookcase else (.58,1.0,1.42)
 for z in levels:box('Shelf',(0,-.005,z),(.96,depth-.04,.045))
 return join('CabinetBody'),levels
def door(root,s,depth,z,h):
 x=s*.245;y=-depth/2-.016
 box('Door slab',(x,y,z),(.475,.037,h),bevel=.005)
 box('Inset field',(x,y-.024,z),(.35,.014,h-.17),bevel=.003)
 for xx in (x-.195,x+.195):box('Raised stile',(xx,y-.032,z),(.03,.025,h-.085))
 for zz in (z-h/2+.055,z+h/2-.055):box('Raised rail',(x,y-.032,zz),(.42,.025,.032))
 # Short stepped corner blocks echo the reference's panel silhouette.
 for xx in (x-.176,x+.176):
  for zz in (z-h/2+.078,z+h/2-.078):box('Panel corner',(xx,y-.033,zz),(.04,.026,.044),bevel=.003)
 hx=s*.055
 o=box('Diamond escutcheon',(hx,y-.045,z+.01),(.027,.012,.039),brass,.003);o.rotation_euler.y=math.radians(35)
 box('Brass pull',(hx,y-.061,z+.01),(.018,.025,.023),brass,.004)
 leaf=join('DoorLeaf_Left' if s<0 else 'DoorLeaf_Right')
 hinge=empty('DoorHinge_Left' if s<0 else 'DoorHinge_Right',(s*.49,-depth/2,0));parent(hinge,root);parent(leaf,hinge)
 return hinge
def save(root,name):
 bpy.ops.object.select_all(action='DESELECT')
 for o in [root]+list(root.children_recursive):o.select_set(True)
 bpy.context.view_layer.objects.active=root
 bpy.ops.export_scene.fbx(filepath=str(ASSET/('SM_'+name+'.fbx')),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',bake_space_transform=True,apply_unit_scale=True,use_mesh_modifiers=True,bake_anim=False,path_mode='RELATIVE',add_leaf_bones=False)
 stats[name]={'triangles':sum(len(p.vertices)-2 for o in root.children_recursive if o.type=='MESH' for p in o.data.polygons),'doors':2}
 img.pack();bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/(name+'.blend')))
 for o in [root]+list(root.children_recursive):bpy.context.collection.objects.unlink(o)
 models.append(root)
root=empty('ManorWardrobe');body,_=shell(1.92,.54);parent(body,root)
for s in (-1,1):door(root,s,.54,1.015,1.60)
save(root,'ManorWardrobe')
root=empty('ManorBookcase');body,levels=shell(1.82,.37,True);parent(body,root)
for s in (-1,1):door(root,s,.37,.397,.39)
# Books are closed bound volumes with visible page blocks and contrasting covers.
def book(pos,w,d,h,color,lean=0,flat=False):
 start=len(parts);x,y,z=pos
 box('Pages',(x,y+.006,z+h/2),(w-.009,d-.017,h-.016),paper,0)
 box('Spine',(x,y-d/2,z+h/2),(w,.018,h),color,0)
 for xx in (x-w/2+.003,x+w/2-.003):box('Cover',(xx,y,z+h/2),(.006,d,h),color,0)
 for zz in (z+h*.15,z+h*.85):box('Spine raised band',(x,y-d/2-.01,zz),(w+.002,.005,.007),color,0)
 if lean:
  pivot=Vector((x,y,z))
  from mathutils import Matrix
  rot=Matrix.Rotation(lean,4,'Y')
  for o in parts[start:]:o.location=pivot+rot.to_3x3()@(o.location-pivot);o.rotation_euler.y=lean
for row,base in enumerate(levels):
 x=-.405
 for i in range(8):
  w=rng.uniform(.055,.085);h=rng.uniform(.22,.29)
  if x+w>.44:break
  book((x+w/2,-.035,base+.024),w,.22,h,bookmats[(i+row)%4],math.radians(10) if i==5 else 0)
  x+=w+.027
books=join('Books');parent(books,root)
save(root,'ManorBookcase')
# Preview: open one wardrobe leaf to reveal the real shelves.
for r,x in zip(models,(-.75,.75)):
 for o in [r]+list(r.children_recursive):bpy.context.collection.objects.link(o)
 r.location.x=x
hinge=next(o for o in models[0].children if o.name.startswith('DoorHinge_Left'));hinge.rotation_euler.z=math.radians(-53)
def aim(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(3.1,-6,3));cam=bpy.context.object;aim(cam,(0,0,.94));cam.data.type='ORTHO';cam.data.ortho_scale=3.6
scene=bpy.context.scene;scene.camera=cam
for loc,power,size in [((-2,-3,5),650,4),((3,1,4),450,3)]:
 bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.data.energy=power;o.data.size=size;aim(o,(0,0,1))
scene.world=bpy.data.worlds.new('Studio');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.10,.12,.14,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.45
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.render.resolution_x=1450;scene.render.resolution_y=1200;scene.render.resolution_percentage=100;scene.render.filepath=str(ROOT/'ManorStorage_Preview.png');scene.render.image_settings.file_format='PNG';bpy.ops.render.render(write_still=True)
(ROOT/'geometry.json').write_text(json.dumps(stats,indent=2));print('MANOR_STORAGE_BLENDER_OK',json.dumps(stats))
