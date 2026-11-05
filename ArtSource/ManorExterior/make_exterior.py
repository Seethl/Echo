"""Build Echo's exterior envelope in Blender. Units and coordinates match ManorPrototype."""
from collections import defaultdict
from math import cos, pi, sin
from pathlib import Path

import bpy


source = Path(__file__).resolve().parent
output = source.parents[1] / 'Assets/Props/ManorExterior'
output.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)

palette = {
    'Old brick': (.32, .165, .121), 'Limestone': (.46, .42, .35),
    'Slate': (.115, .15, .172), 'Oak': (.122, .082, .057),
    'Plaster': (.36, .33, .26), 'Night glazing': (.09, .142, .18),
    'Verdigris': (.139, .221, .20), 'Clay': (.30, .14, .095),
}
materials = {}
for name, rgb in palette.items():
    material = bpy.data.materials.new(name)
    material.diffuse_color = (*rgb, 1)
    materials[name] = material

parts = defaultdict(lambda: ([], []))
wing = 'Hall'


def face(points, mat):
    vertices, polygons = parts[wing, mat]
    first = len(vertices)
    vertices.extend(points)
    polygons.append(tuple(range(first, first + len(points))))


def prism(outline, z0, z1, mat):
    a = [(x, y, z0) for x, y in outline]
    b = [(x, y, z1) for x, y in outline]
    face(a[::-1], mat)
    face(b, mat)
    for i in range(len(a)):
        j = (i + 1) % len(a)
        face([a[i], a[j], b[j], b[i]], mat)


def box(x0, x1, y0, y1, z0, z1, mat):
    prism([(x0,y0),(x1,y0),(x1,y1),(x0,y1)], z0, z1, mat)


def roof(x0, x1, z0, z1, eave, ridge, infill='Old brick'):
    mid = (x0 + x1) / 2
    prism([(x0,eave-.12),(x1,eave-.12),(mid,ridge)], z0, z1, infill)
    for edge in (x0-.36, x1+.36):
        slope = (ridge-eave) / ((x1-x0)*.5)
        low = eave - .36*slope
        outline = [(edge,low),(mid,ridge+.06),(mid,ridge+.21),(edge,low+.15)]
        prism(outline, z0-.36, z1+.36, 'Slate')
        # Fine raised slate courses give the slopes a readable texture in silhouette.
        for row in range(1, 13):
            t = row / 13
            x = edge+(mid-edge)*t
            y = low+(ridge+.06-low)*t+.15
            box(x-.035,x+.035,y-.02,y+.035,z0-.34,z1+.34,'Slate')
        box(edge-.065,edge+.065,low-.095,low+.14,z0-.39,z1+.39,'Oak')
    box(mid-.12,mid+.12,ridge+.16,ridge+.28,z0-.41,z1+.41,'Slate')


def roof_along_x(x0, x1, z0, z1, eave, ridge):
    """Rotate a complete roof onto the long horizontal wing."""
    start = {key: len(value[0]) for key, value in parts.items()}
    roof(z0,z1,x0,x1,eave,ridge)
    for key, (vertices, _) in parts.items():
        for i in range(start.get(key,0), len(vertices)):
            x,y,z = vertices[i]
            vertices[i] = (z,y,x)


def panel(u0, u1, y0, y1, wall, side, mat, depth=.18):
    """Place a facade detail outward from its wall plane."""
    if side == 'south': box(u0,u1,y0,y1,wall-depth,wall,mat)
    elif side == 'north': box(u0,u1,y0,y1,wall,wall+depth,mat)
    elif side == 'east': box(wall,wall+depth,y0,y1,u0,u1,mat)
    else: box(wall-depth,wall,y0,y1,u0,u1,mat)


def surround(u, y, width, height, wall, side, columns=2, rows=2):
    a,b = u-width/2,u+width/2
    panel(a-.14,b+.14,y-.13,y+height+.17,wall,side,'Limestone',.105)
    panel(a,b,y,y+height,wall,side,'Night glazing',.124)
    panel(a-.2,b+.2,y-.14,y-.035,wall,side,'Limestone',.24)
    for i in range(columns+1):
        x = a + i*width/columns
        panel(x-.045,x+.045,y,y+height,wall,side,'Oak',.167)
    for i in range(rows+1):
        h = y + i*height/rows
        panel(a,b,h-.043,h+.043,wall,side,'Oak',.167)


def arch(u, base, width, rect, wall, side, glazed=True):
    radius = width/2
    spring = base + rect
    def shape(outline, depth, mat):
        if side == 'south': prism(outline, wall-depth, wall, mat)
        else:
            a = [(wall,y,x) for x,y in outline]
            b = [(wall+depth,y,x) for x,y in outline]
            face(a[::-1],mat)
            face(b,mat)
            for i in range(len(a)):
                j=(i+1)%len(a)
                face([a[i],a[j],b[j],b[i]],mat)
    outline = [(u-radius,base),(u+radius,base)] + [
        (u+radius*cos(pi*i/16),spring+radius*sin(pi*i/16)) for i in range(17)]
    shape(outline,.145,'Night glazing' if glazed else 'Oak')
    for sign in (-1,1):
        lo,hi=sorted((u+sign*radius,u+sign*(radius+.22)))
        panel(lo,hi,base,spring,wall,side,'Limestone',.23)
    for i in range(16):
        a,b=pi*i/16,pi*(i+1)/16
        outline=[(u+r*cos(t),spring+r*sin(t)) for r,t in ((radius,a),(radius+.22,a),(radius+.22,b),(radius,b))]
        shape(outline,.23,'Limestone')
    if glazed:
        for shift in (-width/6,width/6):
            panel(u+shift-.035,u+shift+.035,base,spring,wall,side,'Oak',.194)
        for i in range(4):
            y=base+rect*i/3
            panel(u-radius,u+radius,y-.037,y+.037,wall,side,'Oak',.194)
        for angle in (pi/4,pi/2,3*pi/4):
            a=(u+.19*cos(angle),spring+.19*sin(angle))
            b=(u+radius*cos(angle),spring+radius*sin(angle))
            dx,dy=.03*sin(angle),.03*cos(angle)
            shape([(a[0]-dx,a[1]+dy),(b[0]-dx,b[1]+dy),(b[0]+dx,b[1]-dy),(a[0]+dx,a[1]-dy)],.19,'Oak')
        panel(u-radius-.23,u+radius+.23,base-.13,base,wall,side,'Limestone',.31)


def cornice(a,b,wall,side,height):
    panel(a,b,height-.24,height-.09,wall,side,'Limestone',.17)
    panel(a,b,height-.09,height+.04,wall,side,'Limestone',.27)


def quoins(u, wall, side, height):
    for i in range(int(height/.46)):
        width = .36 if i%2 else .62
        panel(u-width/2,u+width/2,i*.46+.035,i*.46+.425,wall,side,'Limestone',.085)


def chimney(x,z,start,top):
    box(x-.53,x+.53,start,top,z-.45,z+.45,'Old brick')
    for dy in (.12,.34):
        box(x-.62,x+.62,top-dy-.08,top-dy,z-.54,z+.54,'Limestone')
    for offset in (-.25,.25):
        for i in range(10):
            a,b=i*pi/5,(i+1)*pi/5
            face([(x+offset+.16*cos(t),top+h,z+.16*sin(t)) for t,h in ((a,0),(b,0),(b,.42),(a,.42))],'Clay')


# Principal volume. The facings sit just outside the actual level collision shell.
box(3.86,24.14,-.22,9.22,-12.145,-12.102,'Old brick')
# The east hall wall becomes a party wall once the kitchen begins at z=0.
box(24.105,24.145,-.22,9.22,-12.145,-.145,'Old brick')
box(24.105,24.145,4.22,9.22,-.145,8.1,'Old brick')
for z0,z1 in ((-4.1,4.18),(7.64,8.0)):
    box(3.854,3.894,-.22,9.22,z0,z1,'Old brick')
box(3.854,3.894,-.22,4.35,4.18,7.64,'Old brick')
box(3.854,3.894,8.18,9.22,4.18,7.64,'Old brick')
box(3.86,24.14,4.8,9.22,8.105,8.145,'Old brick')
roof(3.85,24.15,-12.15,8.15,9.21,15.35)
chimney(7.1,-5.4,10.3,14.9)
chimney(20.2,1.65,10.5,15.25)
for side,wall,lo,hi in [('south',-12.15,3.85,24.15),('east',24.15,-12.15,8.12),('west',3.85,-4,8)]:
    cornice(lo,hi,wall,side,9.2)
    panel(lo,-.145 if side == 'east' else hi,.12,.38,wall,side,'Limestone',.13)
    if side == 'west':
        panel(lo,4.18,5.57,5.71,wall,side,'Limestone',.12)
        panel(7.64,hi,5.57,5.71,wall,side,'Limestone',.12)
    else:
        panel(lo,hi,5.57,5.71,wall,side,'Limestone',.12)
    quoins(lo+.30,wall,side,9.1)
    if side != 'east':
        quoins(hi-.30,wall,side,9.1)

for x in (7.5,20.5):
    arch(x,1.0,2.32,2.65,-12.15,'south')
    surround(x,6.13,1.65,1.94,-12.15,'south',3,2)
for z in (-8.5,-2.5):
    if z == -8.5: arch(z,1.12,1.91,2.69,24.15,'east')
    else: surround(z,1.0,1.65,1.95,24.15,'east')
for z in (-8.5,-2.5,3.55):
    surround(z,6.06,1.55,2.0,24.15,'east',2,2)
arch(14,10.02,1.84,1.60,-12.16,'south')

# The east dormer breaks the long slate slope and sits above the interior ceiling.
box(19.44,22.75,10.20,12.05,-7.73,-4.37,'Old brick')
roof_along_x(19.32,22.78,-7.75,-4.35,12.04,13.25)
surround(-6.05,10.69,1.76,1.15,22.76,'east',3,2)
cornice(-7.76,-4.34,22.76,'east',12.01)

wing = 'Entry porch'
box(10.63,17.37,-.24,4.94,-14.44,-12.12,'Old brick')
arch(14,.37,2.62,2.55,-14.46,'south',False)
panel(13.95,14.05,.4,2.91,-14.46,'south','Limestone',.17)
prism([(12.64,1.18),(15.39,1.49),(15.38,1.72),(12.62,1.41)],-14.69,-14.60,'Oak')
prism([(12.59,2.1),(15.42,1.87),(15.44,2.10),(12.61,2.33)],-14.69,-14.60,'Oak')
for x in (11.55,16.45):
    surround(x,1.4,.65,1.64,-14.46,'south',1,3)
roof(10.62,17.38,-14.49,-12.52,4.94,7.37,'Plaster')
for x in (11.1,12.3,14,15.7,16.9):
    peak = 7.37 - abs(x-14)*.71
    panel(x-.045,x+.045,4.97,peak,-14.496,'south','Oak',.077)
for row in range(3):
    box(10.4-row*.19,17.6+row*.19,-.18,.35-row*.115,-14.77-row*.31,-14.28,'Limestone')

wing = 'Caretaker and gallery wing'
edge = -20.15
for x in (-6.8,-2.8,1.2):
    left,right=x-.91,x+.91
    box(edge,left,-.22,4.22,-12.15,-12.105,'Old brick')
    box(left,right,-.22,1.01,-12.15,-12.105,'Old brick')
    box(left,right,3.09,4.22,-12.15,-12.105,'Old brick')
    for a,b in ((left-.16,left),(right,right+.16)):
        panel(a,b,.91,3.24,-12.15,'south','Limestone',.22)
    panel(left-.19,right+.19,3.09,3.25,-12.15,'south','Limestone',.22)
    panel(left-.19,right+.19,.9,1.01,-12.15,'south','Limestone',.27)
    edge = right
box(edge,4.02,-.22,4.22,-12.15,-12.105,'Old brick')
box(-20.15,4.02,-.22,4.22,-3.89,-3.85,'Old brick')
box(-20.15,-20.105,-.22,4.22,-12.15,-3.85,'Old brick')
roof_along_x(-20.15,-10.53,-12.15,-3.85,4.22,6.95)
roof_along_x(1.52,3.49,-12.15,-3.85,4.22,6.95)
roof_along_x(-10.53,1.52,-12.15,-10.52,4.22,5.05)
for x in (-17.35,-13.2):
    surround(x,1.15,1.44,1.82,-12.16,'south',2,2)
surround(-8.2,1.23,1.65,1.95,-20.16,'west')
cornice(-20.14,4.02,-12.15,'south',4.19)
chimney(-16,-7.75,6.11,8.7)

wing = 'Upper study'
box(-10.15,1.15,4.17,8.07,-10.15,-10.10,'Old brick')
box(-10.15,-10.105,4.17,8.07,-10.15,4.55,'Old brick')
box(-10.15,1.15,-.22,4.17,-3.84,4.52,'Old brick')
# The rest of this face adjoins the enclosed upper corridor.
box(-10.15,-2.14,4.17,8.07,4.50,4.55,'Old brick')
box(1.10,1.15,4.17,8.07,-10.15,4.55,'Old brick')
roof(-10.15,1.15,-10.15,4.55,8.07,11.67,'Plaster')
for x in (-8.95,-7.15,-5.35,-3.55,-1.75,.05):
    peak=11.67-abs(x+4.5)*.636
    panel(x-.068,x+.068,8.07,peak,-10.16,'south','Oak',.1)
surround(-5.5,5.12,1.75,1.95,-10.16,'west',2,2)
surround(1.1,1.13,1.65,1.93,-10.16,'west',2,2)
surround(-4.5,6.48,1.85,1.3,-10.16,'south',3,2)
box(-2.14,4.04,7.94,8.2,4.29,7.52,'Slate')
box(-2.14,4.02,4.37,7.94,7.50,7.55,'Old brick')
chimney(-8.4,.65,8.6,11.65)

wing = 'Library wing'
box(3.86,3.90,-.22,4.82,8.05,22.15,'Old brick')
box(3.86,24.085,-.22,4.82,22.105,22.15,'Old brick')
box(24.1,24.145,4.71,4.82,14.05,22.15,'Old brick')
roof(3.86,24.15,8.48,22.15,4.82,9.12)
for x in (7,11.55,16.2,20.75):
    surround(x,.96,2.05,2.64,22.16,'north',3,3)
for z in (10.4,15,19.5):
    surround(z,1.0,1.95,2.55,3.85,'west',3,2)
cornice(3.84,24.18,22.16,'north',4.82)
chimney(9.2,14.2,6.8,10.4)

wing = 'Kitchen wing'
box(24.12,36.15,-.2,4.01,-.145,-.105,'Old brick')
box(36.105,36.15,-.2,4.01,-.145,14.05,'Old brick')
roof(24.49,36.15,-.145,14.05,4.01,7.03)
for z in (2.05,6.6,10.6):
    surround(z,1.05,1.48,1.72,36.16,'east')
for x in (27.2,33.35):
    surround(x,1.04,1.5,1.76,-.15,'south')
cornice(-.14,14.05,36.15,'east',4.0)
chimney(32.2,3.7,5.66,8.75)

wing = 'Conservatory'
# The existing glasshouse route opens north at x=30. No mesh crosses that threshold.
for side,wall,lo,hi in [('east',38.15,14,28.13),('west',23.85,22.1,28.13),('south',13.85,36.12,38.15)]:
    panel(lo,hi,-.19,1.02,wall,side,'Old brick',.115)
    panel(lo,hi,1.01,4.54,wall,side,'Night glazing',.095)
    for j in range(int((hi-lo)/1.27)+1):
        u = lo + (hi-lo)*j/max(1,int((hi-lo)/1.27))
        panel(u-.049,u+.049,.95,4.57,wall,side,'Verdigris',.154)
    for y in (1.0,2.36,3.6,4.53):
        panel(lo,hi,y-.042,y+.042,wall,side,'Verdigris',.162)
for lo,hi in ((24,28.65),(31.35,38.16)):
    panel(lo,hi,-.19,1.02,28.14,'north','Old brick',.105)
    panel(lo,hi,1.02,4.54,28.14,'north','Night glazing',.085)
    for i in range(5):
        u=lo+(hi-lo)*i/4
        panel(u-.055,u+.055,.98,4.56,28.14,'north','Verdigris',.16)
    for h in (1.02,2.40,3.57,4.54):
        panel(lo,hi,h-.05,h+.05,28.14,'north','Verdigris',.16)
panel(28.65,31.35,2.8,4.54,28.14,'north','Night glazing',.09)
panel(28.60,28.68,.02,4.57,28.14,'north','Verdigris',.17)
panel(31.32,31.40,.02,4.57,28.14,'north','Verdigris',.17)
panel(28.65,31.35,2.79,2.91,28.14,'north','Verdigris',.17)
for i in range(12):
    z0=14+i*1.18
    for x0,x1 in ((23.9,31),(31,38.15)):
        y0=5.89 if x0==31 else 4.52
        y1=5.89 if x1==31 else 4.52
        face([(x0,y0,z0),(x1,y1,z0),(x1,y1,z0+1.18),(x0,y0,z0+1.18)],'Night glazing')
        prism([(x0,y0),(x1,y1),(x1,y1+.065),(x0,y0+.065)],z0-.038,z0+.038,'Verdigris')
for x in (23.9,26.25,28.65,31,33.38,35.76,38.15):
    y=5.89-abs(x-31)*.193
    box(x-.048,x+.048,y-.04,y+.052,14,28.23,'Verdigris')
for z in (14,28.18):
    prism([(23.9,4.51),(38.15,4.51),(31,5.89)],z,z+.045,'Night glazing')
    for x in (25.25,27.15,29.05,31,32.95,34.85,36.75):
        box(x-.045,x+.045,4.48,5.89-abs(x-31)*.193,z-.055,z+.075,'Verdigris')


for (section, mat), (vertices, faces) in parts.items():
    mesh = bpy.data.meshes.new(f'{section} - {mat}')
    mesh.from_pydata([(x,-z,y) for x,y,z in vertices], [], faces)
    mesh.update()
    obj=bpy.data.objects.new(mesh.name,mesh)
    bpy.context.collection.objects.link(obj)
    mesh.materials.append(materials[mat])
    uv=mesh.uv_layers.new(name='World scale')
    for polygon in mesh.polygons:
        axis=max(range(3),key=lambda i:abs(polygon.normal[i]))
        axes=[i for i in range(3) if i!=axis]
        for li in polygon.loop_indices:
            vertex=mesh.vertices[mesh.loops[li].vertex_index].co
            uv.data[li].uv=(vertex[axes[0]]/2.4,vertex[axes[1]]/2.4)
    obj.select_set(True)
    bpy.context.view_layer.objects.active=obj
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.remove_doubles(threshold=.00001)
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.object.mode_set(mode='OBJECT')
    obj.select_set(False)

bpy.ops.object.select_all(action='SELECT')


def reflect_export_axis():
    # Unity's FBX import reflects X. Keep the editable .blend in the level's coordinates.
    for obj in bpy.context.scene.objects:
        for vertex in obj.data.vertices:
            vertex.co.x = -vertex.co.x
        obj.data.flip_normals()
        obj.data.update()


reflect_export_axis()
try:
    bpy.ops.export_scene.fbx(filepath=str(output/'Manor exterior.fbx'), axis_forward='-Z', axis_up='Y', bake_anim=False)
finally:
    reflect_export_axis()
bpy.ops.wm.save_as_mainfile(filepath=str(source/'Manor exterior.blend'))
