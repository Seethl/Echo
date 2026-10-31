import bpy, json, math
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parent
ASSET=ROOT.parents[1]/'Assets'/'Props'/'ManorSurfaces'
ASSET.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
objects=[];stats={}
def build(name,texture_name,thickness,ceiling=False):
    image=bpy.data.images.load(str(ASSET/texture_name))
    mat=bpy.data.materials.new('M_'+name.removeprefix('SM_').removesuffix('_2x2'))
    mat.use_nodes=True
    bs=mat.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Roughness'].default_value=.92 if ceiling else .78
    node=mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=image
    mat.node_tree.links.new(node.outputs['Color'],bs.inputs['Base Color'])
    # Blender Z up. Walking surface at Z=0, ceiling visible underside at Z=0.
    heights=(0,thickness) if ceiling else (-thickness,0)
    vertices=[(x,y,z) for z in heights for y in (0,1,2) for x in (0,1,2)]
    faces=[]
    for k in range(2):
        for y in range(2):
            for x in range(2):
                a=k*9+y*3+x
                f=(a,a+1,a+4,a+3)
                faces.append(tuple(reversed(f)) if k==0 else f)
    boundary=[0,1,2,5,8,7,6,3]
    for a,b in zip(boundary,boundary[1:]+boundary[:1]):faces.append((a,b,b+9,a+9))
    mesh=bpy.data.meshes.new(name+'_Mesh');mesh.from_pydata(vertices,[],faces);mesh.update()
    ob=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(ob);mesh.materials.append(mat)
    uv=mesh.uv_layers.new(name='UVMap')
    for p in mesh.polygons:
        for li in p.loop_indices:
            co=mesh.vertices[mesh.loops[li].vertex_index].co
            u=.5*(1-abs(co.x-1));v=.5*(1-abs(co.y-1))
            if abs(p.normal.z)<.5:
                u=.5*(1-abs((co.x if abs(p.normal.y)>.5 else co.y)-1))
                v=.08+(co.z-heights[0])*.1
            uv.data[li].uv=(u,v)
    # Exact correspondence on both pairs of edges, not merely approximate tiling.
    for axis in (0,1):
        edge=[[],[]]
        for p in mesh.polygons[:8]:
            for li in p.loop_indices:
                co=mesh.vertices[mesh.loops[li].vertex_index].co
                for side in (0,1):
                    if abs(co[axis]-2*side)<1e-6:
                        edge[side].append(tuple(round(float(v),6) for v in (co[1-axis],co.z,*uv.data[li].uv)))
        assert sorted(edge[0])==sorted(edge[1]),'UV seam mismatch'
    counts={}
    for p in mesh.polygons:
        ids=list(p.vertices)
        for a,b in zip(ids,ids[1:]+ids[:1]):
            edge=tuple(sorted((a,b)));counts[edge]=counts.get(edge,0)+1
    assert all(n==2 for n in counts.values())
    ob['module_size']='2x2 metres; grid snap 2m'
    ob['surface_origin']='Visible surface is local Z=0 in Blender, Y=0 in Unity.'
    ob['uv']='Mirrored in both axes for exact matching edges.'
    ob.modifiers.new('Stable triangulation','TRIANGULATE')
    bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);bpy.context.view_layer.objects.active=ob
    bpy.ops.export_scene.fbx(filepath=str(ASSET/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_space_transform=True,apply_unit_scale=True,use_mesh_modifiers=True,bake_anim=False,path_mode='RELATIVE',add_leaf_bones=False)
    image.pack()
    stats[name]={'triangles':32,'vertices':18,'size_metres':[2,2,thickness],'closed_manifold':True,'matching_uv_both_axes':True}
    objects.append(ob)
    return ob
floor=build('SM_ManorFloor_2x2','ManorFloor_BaseColor.png',.12)
ceiling=build('SM_ManorCeiling_2x2','ManorCeiling_BaseColor.png',.10,True)
stone=build('SM_ManorStoneFloor_2x2','ManorStoneFloor_BaseColor.png',.12)
parquet=build('SM_ManorParquetFloor_2x2','ManorParquetFloor_BaseColor.png',.12)
# Save each source separately, so opening the file never stacks two surfaces.
for ob,name in [(floor,'ManorFloor_2x2.blend'),(ceiling,'ManorCeiling_2x2.blend'),(stone,'ManorStoneFloor_2x2.blend'),(parquet,'ManorParquetFloor_2x2.blend')]:
    others=[item for item in objects if item!=ob]
    for other in others:bpy.context.collection.objects.unlink(other)
    bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);bpy.context.view_layer.objects.active=ob
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/name))
    for other in others:bpy.context.collection.objects.link(other)
# Three-by-three surface checks. Ceiling is shown upside down, in a separate
# display patch, purely so its normally downward-facing side can be inspected.
floor.location=(0,0,0)
ceiling.location=(7,2,.10);ceiling.rotation_euler.x=math.pi
stone.location=(0,7,0)
parquet.location=(7,7,0)
for template in (floor,ceiling,stone,parquet):
    for y in range(3):
        for x in range(3):
            if x==0 and y==0:continue
            ob=template.copy();ob.data=template.data;bpy.context.collection.objects.link(ob)
            ob.location=template.location+Vector((x*2,y*2,0))
def aim(ob,point):ob.rotation_euler=(Vector(point)-ob.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(17,-15,24));camera=bpy.context.object;aim(camera,(6.5,6.5,0));camera.data.type='ORTHO';camera.data.ortho_scale=19.5
scene=bpy.context.scene;scene.camera=camera
for loc,power,size in [((3,2,11),2200,9),((11,9,10),2000,9)]:
    bpy.ops.object.light_add(type='AREA',location=loc);ob=bpy.context.object;ob.data.energy=power;ob.data.size=size;aim(ob,(6,2,0))
scene.world=bpy.data.worlds.new('Studio');scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.13,.15,.17,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value=.5
scene.render.engine='CYCLES';scene.cycles.samples=32
scene.render.resolution_x=1600;scene.render.resolution_y=950;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.filepath=str(ROOT/'ManorSurfaces_Preview.png')
bpy.ops.render.render(write_still=True)
(ROOT/'validation.json').write_text(json.dumps(stats,indent=2))
print('MANOR_SURFACES_BLENDER_OK',json.dumps(stats))
