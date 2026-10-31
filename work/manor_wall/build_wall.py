import bpy, json, math, shutil
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parent
ASSET = ROOT.parents[1] / 'Assets' / 'Props' / 'ManorWall'
ASSET.mkdir(parents=True, exist_ok=True)
texture = ROOT.parent / 'textures' / 'manor_wall' / 'manor_wall_basecolor.png'
shutil.copy2(texture, ASSET / 'ManorWall_BaseColor.png')
bpy.ops.wm.read_factory_settings(use_empty=True)
image = bpy.data.images.load(str(ASSET / 'ManorWall_BaseColor.png'))
def material(name, roughness):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bs = mat.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Roughness'].default_value = roughness
    tex = mat.node_tree.nodes.new('ShaderNodeTexImage')
    tex.image = image
    tex.extension = 'REPEAT'
    mat.node_tree.links.new(tex.outputs['Color'], bs.inputs['Base Color'])
    return mat
wallmat = material('M_ManorWall', .88)
trimmat = material('M_ManorWall_Trim', .74)
# One closed extruded profile. Endpoints never protrude beyond 0..2 metres.
profile = [(-.055,0),(-.055,.08),(-.035,.10),(-.018,.12),
    (-.018,1.09),(-.045,1.10),(-.045,1.14),(-.060,1.15),
    (-.060,1.19),(-.030,1.21),(0,1.22),(0,2.84),
    (-.020,2.85),(-.020,2.90),(-.050,2.93),(-.070,2.97),
    (-.070,3),(.20,3),(.20,0)]
n=len(profile)
verts=[(x,y,z) for x in (0,1,2) for y,z in profile]
faces=[]; assignments=[]
for seg in range(2):
    for j in range(n):
        k=(j+1)%n
        faces.append((seg*n+j,(seg+1)*n+j,(seg+1)*n+k,seg*n+k))
        assignments.append(1 if 11<=j<=16 else 0)
faces.extend([tuple(reversed(range(n))), tuple(2*n+j for j in range(n))])
assignments.extend([0,0])
mesh=bpy.data.meshes.new('SM_ManorWall_2x3_Mesh')
mesh.from_pydata(verts,[],faces); mesh.update()
wall=bpy.data.objects.new('SM_ManorWall_2x3',mesh)
bpy.context.collection.objects.link(wall)
mesh.materials.append(wallmat); mesh.materials.append(trimmat)
uv=mesh.uv_layers.new(name='UVMap')
for poly,mi in zip(mesh.polygons,assignments):
    poly.material_index=mi
    for li in poly.loop_indices:
        x,y,z=mesh.vertices[mesh.loops[li].vertex_index].co
        # Matching UV at BOTH boundaries. Texture is mirrored about centre.
        u=.5*(1-abs(x-1))
        v=.035+(z-2.84)*.08 if mi==1 else z/3
        uv.data[li].uv=(u,v)
bpy.context.view_layer.objects.active=wall
wall.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.mesh.normals_make_consistent(inside=False)
bpy.ops.object.mode_set(mode='OBJECT')
wall['module_width_m']=2.0
wall['module_height_m']=3.0
wall['snap']='Local X: 2 metres. Origin: bottom left front structural plane.'
wall['texture_seam']='Matching U=0 on left/right boundaries; mirrored texture inside each module.'
wall['collision']='Use a BoxCollider, centre (1,1.5,-0.065), size (2,3,0.27) after FBX import into Unity.'
tri=wall.modifiers.new('Stable triangulation','TRIANGULATE')
# Validate manifold shell, dimensions and matching front boundary UVs.
edge_uses={}
for p in mesh.polygons:
    ids=list(p.vertices)
    for a,b in zip(ids,ids[1:]+ids[:1]):
        key=tuple(sorted((a,b))); edge_uses[key]=edge_uses.get(key,0)+1
assert all(count==2 for count in edge_uses.values()), 'Open/nonmanifold edge'
assert abs(wall.dimensions.x-2)<1e-5 and abs(wall.dimensions.z-3)<1e-5
boundary={0:[],2:[]}
for p in mesh.polygons[:2*n]:
    for li in p.loop_indices:
        co=mesh.vertices[mesh.loops[li].vertex_index].co
        for side in boundary:
            if abs(co.x-side)<1e-5:
                boundary[side].append(tuple(round(float(q),5) for q in (co.y,co.z,*uv.data[li].uv)))
assert sorted(boundary[0])==sorted(boundary[2]), 'UV/profile seam mismatch'
stats={'width_m':2,'height_m':3,'depth_m':.27,'vertices':len(mesh.vertices),'triangles':sum(len(p.vertices)-2 for p in mesh.polygons),'manifold':True,'matching_boundary_uv':True}
(ROOT/'validation.json').write_text(json.dumps(stats,indent=2))
bpy.ops.export_scene.fbx(filepath=str(ASSET/'SM_ManorWall_2x3.fbx'),use_selection=True,
    object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,
    bake_space_transform=True,mesh_smooth_type='FACE',use_mesh_modifiers=True,
    path_mode='RELATIVE',add_leaf_bones=False,bake_anim=False)
# Clean standalone Blender source, with texture packed for portability.
image.pack()
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ManorWall_2x3.blend'))
# A separate three-module demonstration scene, without changing source/export.
for i in (1,2):
    dup=wall.copy(); dup.data=wall.data
    bpy.context.collection.objects.link(dup); dup.location.x=i*2
def aim(ob,p): ob.rotation_euler=(Vector(p)-ob.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(7,-10,5.2))
cam=bpy.context.object; aim(cam,(3,0,1.45)); cam.data.type='ORTHO';cam.data.ortho_scale=7.4
scene=bpy.context.scene;scene.camera=cam
for pos,power,size in [((1,-4,6),1100,5),((6,-2,3),350,4)]:
    bpy.ops.object.light_add(type='AREA',location=pos)
    light=bpy.context.object;light.data.energy=power;light.data.shape='DISK';light.data.size=size;aim(light,(3,0,1.5))
scene.world=bpy.data.worlds.new('Neutral studio')
scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.12,.14,.16,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value=.5
scene.render.engine='CYCLES';scene.cycles.samples=24
scene.render.resolution_x=1500;scene.render.resolution_y=850;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.render.filepath=str(ROOT/'ManorWall_ThreeModules.png')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ManorWall_ThreeModules.blend'))
bpy.ops.render.render(write_still=True)
print('WALL_BUILD_OK',json.dumps(stats))
