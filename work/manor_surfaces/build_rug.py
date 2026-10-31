import bpy, json, sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parent
ASSET=ROOT.parents[1]/'Assets'/'Props'/'ManorSurfaces'
bpy.ops.wm.read_factory_settings(use_empty=True)
image=bpy.data.images.load(str(ASSET/'ManorRug_BaseColor.png'))
mat=bpy.data.materials.new('M_ManorRug');mat.use_nodes=True
bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.98
node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=image
mat.node_tree.links.new(node.outputs['Color'],bs.inputs['Base Color'])
outline=[(.03,0),(1.97,0),(2,.03),(2,2.97),(1.97,3),(.03,3),(0,2.97),(0,.03)]
verts=[(x,y,z) for z in (.003,.009) for x,y in outline]
faces=[tuple(reversed(range(8))),tuple(range(8,16))]
faces.extend((i,(i+1)%8,(i+1)%8+8,i+8) for i in range(8))
mesh=bpy.data.meshes.new('ManorRugMesh');mesh.from_pydata(verts,[],faces);mesh.update()
ob=bpy.data.objects.new('SM_ManorRug_2x3',mesh);bpy.context.collection.objects.link(ob);mesh.materials.append(mat)
uv=mesh.uv_layers.new(name='UVMap')
for p in mesh.polygons:
 for li in p.loop_indices:
  x,y,z=mesh.vertices[mesh.loops[li].vertex_index].co
  uv.data[li].uv=(x/2,y/3)
ob.modifiers.new('Stable triangulation','TRIANGULATE')
ob['size']='2x3 metres; place origin at floor height. Underside clearance 3mm.'
ob['usage']='Standalone non-tileable rug. No collider needed; walk on floor collider beneath.'
bpy.context.view_layer.objects.active=ob;ob.select_set(True)
if '--texture-only' not in sys.argv:
 bpy.ops.export_scene.fbx(filepath=str(ASSET/'SM_ManorRug_2x3.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_space_transform=True,apply_unit_scale=True,use_mesh_modifiers=True,bake_anim=False,path_mode='RELATIVE',add_leaf_bones=False)
image.pack();bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ManorRug_2x3.blend'))
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.004))
ground=bpy.context.object;gm=bpy.data.materials.new('PreviewBackdrop');gm.diffuse_color=(.07,.065,.055,1);ground.data.materials.append(gm)
def aim(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(3.5,-3,5));cam=bpy.context.object;aim(cam,(1,1.5,0));cam.data.type='ORTHO';cam.data.ortho_scale=4.2
scene=bpy.context.scene;scene.camera=cam
bpy.ops.object.light_add(type='AREA',location=(0,-1,6));lamp=bpy.context.object;lamp.data.energy=550;lamp.data.size=5;aim(lamp,(1,1,0))
scene.world=bpy.data.worlds.new('Studio');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[1].default_value=.4
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.render.resolution_x=1100;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.render.filepath=str(ROOT/'ManorRug_Preview.png');scene.render.image_settings.file_format='PNG'
bpy.ops.render.render(write_still=True)
print('MANOR_RUG_BLENDER_OK: 28 triangles, 2x3m')
