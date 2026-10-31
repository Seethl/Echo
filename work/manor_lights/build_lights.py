import bpy, math, json
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parent
ASSETS=ROOT.parents[1]/'Assets'/'Props'/'ManorLights'
ASSETS.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
mats={}
specs=[]
for name,color,metal,rough in [('Iron',(.045,.052,.048),.65,.7),('Brass',(.33,.21,.075),.8,.48),('Wax',(.64,.53,.32),0,.85),('Flame',(1,.40,.065),0,.5)]:
    mat=bpy.data.materials.new('M_ManorLight_'+name);mat.use_nodes=True;mat.diffuse_color=(*color,1)
    node=mat.node_tree.nodes['Principled BSDF'];node.inputs['Base Color'].default_value=(*color,1);node.inputs['Metallic'].default_value=metal;node.inputs['Roughness'].default_value=rough
    if name=='Flame':node.inputs['Emission Color'].default_value=(1,.28,.035,1);node.inputs['Emission Strength'].default_value=2
    mats[name]=mat;specs.append({'name':mat.name,'color':list(color),'metallic':metal,'smoothness':1-rough,'emission':name=='Flame'})
parts=[];items=[]
def box(name,p,s,mat):
    bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.name=name;o.dimensions=s;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    mod=o.modifiers.new('Worn bevel','BEVEL');mod.width=min(.004,min(s)*.15);mod.segments=1;bpy.ops.object.modifier_apply(modifier=mod.name)
    o.data.materials.append(mats[mat]);parts.append(o);return o
def rod(name,a,b,r,mat='Brass',r2=None,n=8):
    a,b=Vector(a),Vector(b);d=b-a
    bpy.ops.mesh.primitive_cone_add(vertices=n,radius1=r,radius2=r if r2 is None else r2,depth=d.length,location=(a+b)/2)
    o=bpy.context.object;o.name=name;o.rotation_euler=d.to_track_quat('Z','Y').to_euler();o.data.materials.append(mats[mat]);parts.append(o);return o
def path(points,r,mat):
    for a,b in zip(points,points[1:]):rod('Forged curve',a,b,r,mat)
def disk(z,r,h,mat='Brass',x=0,y=0):rod('Turned metal',(x,y,z),(x,y,z+h),r,mat,n=12)
def candle(x,y,z,h=.12):
    disk(z,.035,.012,'Brass',x,y);rod('Wax candle',(x,y,z+.012),(x,y,z+h),.018,'Wax',n=9)
    for angle in (0,2,4):
        xx=x+math.cos(angle)*.017;yy=y+math.sin(angle)*.017
        rod('Wax drip',(xx,yy,z+h-.034),(xx,yy,z+h-.006),.004,'Wax',n=5)
    rod('Wick',(x,y,z+h),(x,y,z+h+.009),.002,'Iron',n=5)
    rod('Flame',(x,y,z+h+.006),(x+.005,y,z+h+.041),.012,'Flame',r2=0,n=6)
def save(name,light,range_,intensity):
    bpy.ops.object.select_all(action='DESELECT')
    for o in parts:o.select_set(True)
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=bpy.context.object;o.name='SM_'+name
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True);bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    o.modifiers.new('Triangulate','TRIANGULATE')
    bpy.ops.export_scene.fbx(filepath=str(ASSETS/(o.name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_space_transform=True,apply_unit_scale=True,use_mesh_modifiers=True,bake_anim=False,add_leaf_bones=False)
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/(name+'.blend')))
    items.append({'name':name,'light':light,'range':range_,'intensity':intensity,'triangles':sum(len(p.vertices)-2 for p in o.data.polygons)})
    bpy.data.objects.remove(o,do_unlink=True);parts.clear()

# Hexagonal portable lantern, open cage, candle and polygonal carrying handle.
disk(0,.108,.025,'Iron');disk(.025,.092,.018)
for angle in [i*math.pi/2+math.pi/4 for i in range(4)]:
    x,y=math.cos(angle)*.087,math.sin(angle)*.087
    rod('Cage upright',(x,y,.035),(x,y,.285),.007,'Iron')
disk(.277,.108,.018,'Iron');rod('Pitched roof',(0,0,.295),(0,0,.365),.115,'Iron',r2=.024,n=8)
disk(.365,.028,.02,'Brass');candle(0,0,.043,.14)
for x in (-.079,.079):rod('Handle bracket',(x,0,.315),(x,0,.395),.006,'Iron')
path([(math.cos(a)*.079,0,.395+math.sin(a)*.079) for a in [i*math.pi/8 for i in range(9)]],.006,'Iron')
save('ManorLantern',[0,.22,0],3.5,1.5)

# Three-candle table candelabrum.
disk(0,.085,.02);rod('Base flare',(0,0,.02),(0,0,.07),.075,r2=.027)
rod('Central stem',(0,0,.07),(0,0,.34),.016)
for side in (-1,1):
    path([(0,0,.16),(side*.065,0,.13),(side*.15,0,.18),(side*.17,0,.28)],.012,'Brass')
    candle(side*.17,0,.28,.12)
disk(.13,.028,.022);disk(.30,.025,.022);candle(0,0,.34,.14)
save('ManorCandelabrum',[0,.46,0],4.2,1.9)

# Wall sconce. Back plate sits at local Z=0 in Unity, light faces +Z.
box('Wall back plate',(0,.015,.21),(.10,.035,.27),'Iron')
for z in (.065,.355):
    rod('Plate finial',(0,.015,z-.02),(0,.015,z+.02),.032,'Brass',r2=0,n=4)
for z in (.13,.29):rod('Mounting stud',(0,-.008,z),(0,-.017,z),.009,'Brass',n=6)
path([(0,-.01,.13),(0,-.08,.09),(0,-.16,.15),(0,-.16,.22)],.012,'Brass')
disk(.21,.048,.015,'Brass',0,-.16);candle(0,-.16,.225,.14)
save('ManorWallSconce',[0,.41,.16],3.2,1.35)
(ASSETS/'LightsManifest.json').write_text(json.dumps({'materials':specs,'items':items},indent=2))
print('MANOR_LIGHT_MODELS_OK')
