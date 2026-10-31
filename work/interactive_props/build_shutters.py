import bpy, math
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parent
ASSETS = ROOT.parents[1] / 'Assets' / 'Props' / 'ManorInteractive'
bpy.ops.wm.read_factory_settings(use_empty=True)
wood = bpy.data.materials.new('M_Interactive_Wood')
wood.use_nodes = True
image = bpy.data.images.load(str(ASSETS / 'ManorInteractive_Wood.png'))
image.pack()
tex = wood.node_tree.nodes.new('ShaderNodeTexImage')
tex.image = image
wood.node_tree.links.new(tex.outputs['Color'], wood.node_tree.nodes['Principled BSDF'].inputs['Base Color'])
wood.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value = .85
iron = bpy.data.materials.new('M_Interactive_Iron')
iron.diffuse_color = (.035, .04, .035, 1)
iron.use_nodes = True
bsdf = iron.node_tree.nodes['Principled BSDF']
bsdf.inputs['Base Color'].default_value = iron.diffuse_color
bsdf.inputs['Metallic'].default_value = .65
bsdf.inputs['Roughness'].default_value = .65

def empty(name, pos=(0, 0, 0)):
    o = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(o)
    o.location = pos
    return o

def box(name, pos, size, material, parent, tilt=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=pos)
    o = bpy.context.object
    o.name = name
    o.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bevel = o.modifiers.new('Worn edges', 'BEVEL')
    bevel.width = min(.004, min(size) * .18)
    bevel.segments = 1
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    o.data.materials.append(material)
    for uv in o.data.uv_layers.active.data:
        uv.uv = (.075 + .11 * uv.uv.x, .13 + .12 * uv.uv.y)
    o.rotation_euler.x = tilt
    world = o.matrix_world.copy()
    o.parent = parent
    o.matrix_world = world
    return o

root = empty('ManorShutters')
for side in (-1, 1):
    hinge_x = side * .635
    pivot = empty('LeftHinge' if side < 0 else 'RightHinge', (hinge_x, -.11, .76))
    pivot.parent = root
    center = side * .3375
    for x in (side * .075, side * .60):
        box('Shutter stile', (x, -.11, .76), (.055, .065, 1.38), wood, pivot)
    for z in (.10, .76, 1.42):
        box('Shutter crossrail', (center, -.11, z), (.58, .065, .07), wood, pivot)
    for base in (.15, .81):
        for i in range(7):
            box('Louver', (center, -.11, base + i * .082), (.48, .028, .087), wood, pivot, math.radians(-22))
    for z in (.28, 1.24):
        box('Hinge strap', (side * .55, -.153, z), (.18, .018, .045), iron, pivot)
        bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=.014, depth=.10, location=(hinge_x, -.13, z))
        pin = bpy.context.object
        pin.name = 'Hinge pin'
        pin.data.materials.append(iron)
        world = pin.matrix_world.copy()
        pin.parent = pivot
        pin.matrix_world = world
    box('Shutter latch', (side * .09, -.16, .72), (.027, .025, .09), iron, pivot)
    # Consolidate each leaf while preserving its hinge origin.
    bpy.ops.object.select_all(action='DESELECT')
    for o in list(pivot.children):
        o.select_set(True)
    bpy.context.view_layer.objects.active = pivot.children[0]
    bpy.ops.object.join()
    leaf = bpy.context.object
    leaf.name = 'LeftLeaf' if side < 0 else 'RightLeaf'
    bpy.context.scene.cursor.location = pivot.location
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    leaf.modifiers.new('Triangles', 'TRIANGULATE')
    # Direct mesh children avoid FBX's nested-empty unit conversion. The mesh
    # origin itself is the hinge, so Unity gets metre scale and a usable pivot.
    world = leaf.matrix_world.copy()
    leaf.parent = root
    leaf.matrix_world = world
    bpy.data.objects.remove(pivot, do_unlink=True)
    leaf.name = 'LeftHinge' if side < 0 else 'RightHinge'

bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=str(ASSETS / 'SM_ManorShutters.fbx'), use_selection=True,
    object_types={'MESH', 'EMPTY'}, axis_forward='-Z', axis_up='Y', bake_space_transform=True,
    apply_unit_scale=True, use_mesh_modifiers=True, bake_anim=False, add_leaf_bones=False)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT / 'ManorShutters.blend'))
print('MANOR_SHUTTERS_MODEL_OK')
