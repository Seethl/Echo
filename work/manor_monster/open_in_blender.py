"""Blender 4.x/5.x: open in Scripting, then Run Script. Saves beside this script."""
import bpy
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parent
# Keep any existing scene intact; build the model in a separate scene.
scene = bpy.data.scenes.new('The Last Steward')
bpy.context.window.scene = scene
bpy.ops.wm.obj_import(filepath=str(ROOT / 'last_steward.obj'), forward_axis='NEGATIVE_Y', up_axis='Z')
parts = list(bpy.context.selected_objects)
for ob in parts:
    if ob.type == 'MESH':
        for poly in ob.data.polygons:
            poly.use_smooth = False
        for mat in ob.data.materials:
            if mat:
                mat.use_nodes = True
                bs = mat.node_tree.nodes.get('Principled BSDF')
                if bs:
                    bs.inputs['Base Color'].default_value = mat.diffuse_color
                    bs.inputs['Roughness'].default_value = 0.87
                    if mat.name.startswith('eye'):
                        bs.inputs['Emission Color'].default_value = (0.85, 0.12, 0.01, 1)
                        bs.inputs['Emission Strength'].default_value = 2.0
bpy.ops.object.empty_add(type='PLAIN_AXES')
root = bpy.context.object
root.name = 'LAST STEWARD | 2.76m | unrigged'
for ob in parts:
    ob.parent = root

from mathutils import Vector
def aim(ob, point):
    ob.rotation_euler = (Vector(point) - ob.location).to_track_quat('-Z', 'Y').to_euler()

bpy.ops.object.camera_add(location=(3.5, -6.5, 2.9))
camera = bpy.context.object
aim(camera, (0, 0, 1.4))
camera.data.type = 'ORTHO'
camera.data.ortho_scale = 3.45
scene.camera = camera
for name, pos, power, color, size in [
    ('Moon key', (1, -3, 5), 550, (.57, .73, 1), 3),
    ('Old candle', (-2, -1, 2.7), 160, (1, .42, .16), 2),
    ('Rim', (0, 2, 3.5), 650, (.35, .6, .53), 2),
]:
    bpy.ops.object.light_add(type='AREA', location=pos)
    lamp = bpy.context.object
    lamp.name = name
    lamp.data.energy = power
    lamp.data.color = color
    lamp.data.shape = 'DISK'
    lamp.data.size = size
    aim(lamp, (0, 0, 1.4))
scene.world = bpy.data.worlds.new('Manor darkness')
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs[0].default_value = (.018, .025, .022, 1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value = .3
scene.render.engine = 'CYCLES'
scene.cycles.samples = 48
scene.render.resolution_x = 900
scene.render.resolution_y = 1100
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.film_transparent = False
bpy.ops.object.select_all(action='DESELECT')
root.select_set(True)
bpy.context.view_layer.objects.active = root
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT / 'last_steward.blend'))
scene.render.filepath = str(ROOT / 'last_steward_preview.png')
bpy.ops.render.render(write_still=True)
