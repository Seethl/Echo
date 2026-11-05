"""Run with blender --background --python make_windows.py."""
import math
from pathlib import Path

import bpy


SOURCE = Path(__file__).resolve().parent
OUTPUT = SOURCE.parents[1] / 'Assets/Props/ManorWindowDress'
OUTPUT.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)


def material(name, rgb, metallic=0.0):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*rgb, 1)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*rgb, 1)
    bsdf.inputs['Metallic'].default_value = metallic
    bsdf.inputs['Roughness'].default_value = 0.72
    return mat


stone = material('Weathered limestone', (0.26, 0.26, 0.24))
oak = material('Dark carved oak', (0.093, 0.060, 0.040))
glass = material('Blue night glass', (0.085, 0.17, 0.27))
velvet = material('Wine velvet', (0.27, 0.042, 0.060))
brass = material('Aged brass', (0.43, 0.25, 0.092), 0.42)


def mesh(name, points, faces, mat):
    data = bpy.data.meshes.new(name)
    data.from_pydata([(x, -depth, z) for x, z, depth in points], [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(mat)
    return obj


def slab(name, outline, back, front, mat):
    count = len(outline)
    points = [(x, z, d) for d in (back, front) for x, z in outline]
    faces = [tuple(reversed(range(count))), tuple(range(count, 2 * count))]
    faces += [(i, (i + 1) % count, (i + 1) % count + count, i + count) for i in range(count)]
    return mesh(name, points, faces, mat)


def box(name, x0, x1, z0, z1, back, front, mat):
    return slab(name, [(x0, z0), (x1, z0), (x1, z1), (x0, z1)], back, front, mat)


def arch_point(radius, angle):
    return radius * math.cos(angle), 0.71 + radius * math.sin(angle)


def radial_bar(angle):
    a = arch_point(0.22, angle)
    b = arch_point(0.98, angle)
    dx, dz = -math.sin(angle) * 0.026, math.cos(angle) * 0.026
    slab('Fanlight spoke', [(a[0]+dx,a[1]+dz), (a[0]-dx,a[1]-dz),
                           (b[0]-dx,b[1]-dz), (b[0]+dx,b[1]+dz)], 0.015, 0.088, oak)


def arch_ring(name, inner, outer, back, front, mat, segments=16):
    for i in range(segments):
        start, end = math.pi * i / segments, math.pi * (i + 1) / segments
        slab(name, [arch_point(inner,start),arch_point(outer,start),
                    arch_point(outer,end),arch_point(inner,end)], back, front, mat)


def arch_window():
    bottom, spring = -1.98, 0.71
    outline = [(-0.99, bottom), (0.99, bottom)]
    outline += [arch_point(0.99, i * math.pi / 16) for i in range(17)]
    slab('Recessed night glass', outline, -0.035, 0.006, glass)
    arch_ring('Stone voussoir', 1.05, 1.19, -0.045, 0.13, stone)
    arch_ring('Oak arch', 0.94, 1.065, -0.033, 0.111, oak)
    arch_ring('Fanlight hub', 0.19, 0.24, 0.012, 0.082, oak, 8)
    for side in (-1, 1):
        x0, x1 = sorted((side * 1.05, side * 1.19))
        box('Stone jamb', x0, x1, bottom, spring, -0.045, 0.13, stone)
        x0, x1 = sorted((side * 0.94, side * 1.065))
        box('Oak jamb', x0, x1, bottom, spring, -0.033, 0.111, oak)
        box('Carved impost', side*1.10-0.13, side*1.10+0.13, 0.64, 0.78, 0.02, 0.17, stone)
    box('Stepped sill', -1.27, 1.27, -2.05, -1.92, -0.043, 0.20, stone)
    box('Sill molding', -1.23, 1.23, -2.09, -2.04, -0.043, 0.15, stone)
    slab('Crown keystone', [(-0.10,1.75),(0.10,1.75),(0.15,1.95),(-0.15,1.95)], 0.02, 0.17, stone)
    for x in (-0.32, 0.32):
        box('Vertical glazing bar', x-0.026, x+0.026, bottom, spring, 0.012, 0.079, oak)
    for z in (-1.08, -0.18, spring):
        box('Horizontal glazing bar', -0.97, 0.97, z-0.028, z+0.028, 0.012, 0.082, oak)
    for angle in (math.pi/4, math.pi/2, 3*math.pi/4):
        radial_bar(angle)


def curtains():
    # Inner and outer edges pinch at the tie. The lower folds flare again.
    rows = [(1.98, .73, 1.56), (1.71, .81, 1.61), (1.34, .95, 1.62),
            (.89, 1.07, 1.62), (.45, 1.19, 1.61), (-.05, 1.28, 1.59),
            (-.54, 1.35, 1.58), (-.79, 1.38, 1.58), (-1.04, 1.33, 1.59),
            (-1.59, 1.23, 1.60), (-2.17, 1.16, 1.62), (-2.65, 1.09, 1.65)]
    count = 19
    for side in (-1, 1):
        points = []
        for i, (z, inner, outer) in enumerate(rows):
            for j in range(count):
                t = j / (count - 1)
                fold = math.sin(t * 8 * math.pi + .32 * math.sin(i*.5))
                depth = .205 + (.055 + .042 * (outer-inner)) * fold
                hem = .034 * math.cos(j * 1.4) if i == len(rows)-1 else 0
                points.append((side * (inner+(outer-inner)*t), z+hem, depth))
        faces = [(i*count+j, i*count+j+1, (i+1)*count+j+1, (i+1)*count+j)
                 for i in range(len(rows)-1) for j in range(count-1)]
        if side < 0:
            faces = [tuple(reversed(face)) for face in faces]
        obj = mesh('Pleated velvet panel', points, faces, velvet)
        thickness = obj.modifiers.new('Cloth edge thickness', 'SOLIDIFY')
        thickness.thickness = .017
        x0, x1 = sorted((side*1.35, side*1.615))
        slab('Brass tieback', [(x0,-.84),(x1,-.77),(x1,-.68),(x0,-.75)], .103, .302, brass)
        box('Rod bracket', side*1.57-.045, side*1.57+.045, 1.99, 2.095, -.03, .24, brass)
    bpy.ops.mesh.primitive_cylinder_add(vertices=10, radius=.032, depth=3.42,
                                       location=(0,-.215,2.047), rotation=(0, math.pi/2, 0))
    bpy.context.object.name = 'Curtain rod'
    bpy.context.object.data.materials.append(brass)
    for side in (-1, 1):
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=.075, location=(side*1.72,-.215,2.047))
        bpy.context.object.name = 'Rod finial'
        bpy.context.object.data.materials.append(brass)


def export(name, create):
    before = set(bpy.context.scene.objects)
    create()
    objects = sorted(set(bpy.context.scene.objects) - before, key=lambda obj: obj.name)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        for modifier in list(obj.modifiers):
            bpy.ops.object.modifier_apply(modifier=modifier.name)
        obj.select_set(False)
    # Export one mesh per material so each installation stays inexpensive.
    groups = {}
    for obj in objects:
        groups.setdefault(obj.data.materials[0].name, []).append(obj)
    for name_of_material, group in sorted(groups.items()):
        for obj in group:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = group[0]
        if len(group) > 1:
            bpy.ops.object.join()
        joined = bpy.context.object
        joined.name = name + ' - ' + name_of_material
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
        bpy.ops.object.mode_set(mode='EDIT')
        bpy.ops.mesh.select_all(action='SELECT')
        bpy.ops.mesh.normals_make_consistent(inside=False)
        bpy.ops.object.mode_set(mode='OBJECT')
        joined.select_set(False)
    for obj in set(bpy.context.scene.objects) - before:
        obj.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(OUTPUT / (name + '.fbx')), use_selection=True,
                             axis_forward='-Z', axis_up='Y', bake_anim=False)


export('Grand arched window', arch_window)
export('Tied velvet curtains', curtains)
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / 'Manor window dress.blend'))
