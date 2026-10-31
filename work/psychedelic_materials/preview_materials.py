import bpy, math, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parent
PROJECT=ROOT.parents[1]
ASSET=PROJECT/'Assets'/'Props'/'PsychedelicMaterials'
bpy.ops.wm.read_factory_settings(use_empty=True)
def load(source,name,texture):
 with bpy.data.libraries.load(str(source)) as (a,b):b.objects=[name]
 ob=b.objects[0];bpy.context.collection.objects.link(ob)
 image=bpy.data.images.load(str(ASSET/texture));image.pack()
 for slot in ob.material_slots:
  mat=slot.material.copy();slot.material=mat;mat.name=mat.name.split('.')[0]+'_Psy'
  for node in mat.node_tree.nodes:
   if node.type=='TEX_IMAGE':node.image=image
 return ob
wall=load(PROJECT/'work'/'manor_wall'/'ManorWall_2x3.blend','SM_ManorWall_2x3','Psy_Wall_BaseColor.png')
wall.location=(0,3,0)
for x in (2,4):
 ob=wall.copy();ob.data=wall.data;bpy.context.collection.objects.link(ob);ob.location.x=x
for i,(name,tex) in enumerate([('ManorFloor','Wood'),('ManorStoneFloor','Stone'),('ManorParquetFloor','Parquet')]):
 ob=load(PROJECT/'work'/'manor_surfaces'/(name+'_2x2.blend'),'SM_'+name+'_2x2','Psy_'+tex+'_BaseColor.png')
 ob.location=(i*2,1,0)
ceiling=load(PROJECT/'work'/'manor_surfaces'/'ManorCeiling_2x2.blend','SM_ManorCeiling_2x2','Psy_Ceiling_BaseColor.png')
ceiling.location=(7,3,.10);ceiling.rotation_euler.x=math.pi
def aim(ob,p):ob.rotation_euler=(Vector(p)-ob.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(10,-12,9));cam=bpy.context.object;aim(cam,(4,2,1.1));cam.data.type='ORTHO';cam.data.ortho_scale=10.3
scene=bpy.context.scene;scene.camera=cam
for pos,power,size in [((1,-3,7),1250,7),((8,0,6),800,5)]:
 bpy.ops.object.light_add(type='AREA',location=pos);ob=bpy.context.object;ob.data.energy=power;ob.data.size=size;aim(ob,(4,2,1))
scene.world=bpy.data.worlds.new('Neutral preview');scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.12,.14,.16,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value=.5
scene.render.engine='CYCLES';scene.cycles.samples=32
scene.render.resolution_x=1600;scene.render.resolution_y=1050;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.filepath=str(ROOT/'PsychedelicMaterials_Preview.png')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'PsychedelicMaterials_Preview.blend'))
bpy.ops.render.render(write_still=True)
print('PSY_PREVIEW_OK')
