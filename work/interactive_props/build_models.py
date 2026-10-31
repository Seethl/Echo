import bpy, math, shutil, json, random
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parent;PROJECT=ROOT.parents[1];ASSET=PROJECT/'Assets'/'Props'/'ManorInteractive';ASSET.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
shutil.copy2(PROJECT/'Assets'/'Props'/'ManorTables'/'ManorTables_BaseColor.png',ASSET/'ManorInteractive_Wood.png')
image=bpy.data.images.load(str(ASSET/'ManorInteractive_Wood.png'))
mats={};spec=[]
for name,c,metal,rough in [('Wood',(.12,.07,.04),0,.8),('Brass',(.34,.23,.08),.8,.45),('Iron',(.035,.04,.035),.65,.65),('Glass',(.065,.12,.16),.1,.25),('Linen',(.44,.40,.29),0,.95),('Burgundy',(.22,.035,.055),0,.95),('Radio',(.12,.13,.08),0,.8),('Dial',(.70,.43,.14),0,.8),('Red',(.45,.025,.015),0,.7)]:
 m=bpy.data.materials.new('M_Interactive_'+name);m.use_nodes=True;m.diffuse_color=(*c,1);b=m.node_tree.nodes.get('Principled BSDF');b.inputs['Base Color'].default_value=(*c,1);b.inputs['Metallic'].default_value=metal;b.inputs['Roughness'].default_value=rough;mats[name]=m;spec.append({'name':m.name,'color':list(c),'metallic':metal,'smoothness':1-rough,'textured':name=='Wood'})
t=mats['Wood'].node_tree.nodes.new('ShaderNodeTexImage');t.image=image;mats['Wood'].node_tree.links.new(t.outputs['Color'],mats['Wood'].node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
parts=[];roots=[]
def box(name,p,s,mat='Wood',bevel=.003):
 bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.name=name;o.dimensions=s;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(mats[mat])
 if bevel:
  mod=o.modifiers.new('Bevel','BEVEL');mod.width=min(bevel,min(s)*.2);mod.segments=1;bpy.ops.object.modifier_apply(modifier=mod.name)
 for uv in o.data.uv_layers.active.data:uv.uv=(.075+.11*uv.uv.x,.13+.12*uv.uv.y)
 parts.append(o);return o
def cylinder(name,p,r,depth,mat,axis='Z',n=10):
 bpy.ops.mesh.primitive_cylinder_add(vertices=n,radius=r,depth=depth,location=p);o=bpy.context.object;o.name=name;o.data.materials.append(mats[mat])
 if axis=='X':o.rotation_euler.y=math.pi/2
 if axis=='Y':o.rotation_euler.x=math.pi/2
 bpy.ops.object.transform_apply(location=False,rotation=True,scale=True);parts.append(o);return o
def join(name,origin=(0,0,0)):
 bpy.ops.object.select_all(action='DESELECT')
 for o in parts:o.select_set(True)
 bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=bpy.context.object;o.name=name;bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
 bpy.context.scene.cursor.location=origin;bpy.ops.object.origin_set(type='ORIGIN_CURSOR');o.modifiers.new('Triangles','TRIANGULATE');parts.clear();return o
def parent(o,r):o.parent=r;o.matrix_parent_inverse=r.matrix_world.inverted()
def empty(name):
 bpy.ops.object.empty_add();o=bpy.context.object;o.name=name;return o
def save(root,name):
 bpy.ops.object.select_all(action='DESELECT')
 for o in [root]+list(root.children_recursive):o.select_set(True)
 bpy.ops.export_scene.fbx(filepath=str(ASSET/('SM_'+name+'.fbx')),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',bake_space_transform=True,apply_unit_scale=True,use_mesh_modifiers=True,bake_anim=False,path_mode='RELATIVE',add_leaf_bones=False)
 image.pack();bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/(name+'.blend')))
 for o in [root]+list(root.children_recursive):bpy.context.collection.objects.unlink(o)
 roots.append(root)
# Four-pane window, separate opaque glass surfaces.
r=empty('ManorWindow')
for x in (-.58,.58):box('Frame',(x,0,.76),(.10,.14,1.52))
for z in (.045,1.475):box('Frame',(0,0,z),(1.25,.14,.09))
box('Sill',(0,-.03,.02),(1.36,.24,.06));box('Vertical mullion',(0,-.025,.76),(.045,.10,1.36));box('Crossbar',(0,-.025,.76),(1.10,.10,.045))
parent(join('WindowFrame'),r)
for x in (-.285,.285):
 for z in (.397,1.12):box('Glass pane',(x,.015,z),(.51,.018,.655),'Glass',.001)
parent(join('WindowGlass'),r);save(r,'ManorWindow')
# Single bed with high timber headboard and layered bedding.
r=empty('ManorBed')
for x in (-.53,.53):
 for y in (-1,1):box('Bed post',(x,y,.45 if y>0 else .30),(.085,.085,.90 if y>0 else .60))
for x in (-.53,.53):box('Side rail',(x,0,.28),(.07,1.96,.20))
box('Foot board',(0,-1,.33),(1.07,.07,.29));box('Head board',(0,1,.68),(1.07,.07,.47));box('Head cap',(0,1,.94),(1.20,.11,.07))
for x in (-.38,.38):box('Head panel trim',(x,.955,.68),(.025,.025,.35))
box('Mattress',(0,0,.43),(1.02,1.94,.20),'Linen',.055)
for x in (-.25,.25):
 o=box('Pillow',(x,.64,.57),(.44,.48,.12),'Linen',.05);o.rotation_euler.z=x*.15
box('Burgundy blanket',(0,-.32,.55),(1.04,1.20,.065),'Burgundy',.012)
box('Hanging blanket',(0,-.95,.39),(1.04,.035,.32),'Burgundy',.009)
parent(join('BedMesh'),r);save(r,'ManorBed')
# Radio with distinct dial, needle, control knobs and grille.
r=empty('ManorRadio')
box('Cabinet',(0,0,0),(.48,.16,.28),'Radio',.015)
for x in (-.225,.225):box('Side trim',(x,-.092,0),(.025,.035,.265),'Wood')
box('Speaker inset',(-.115,-.087,0),(.205,.025,.21),'Iron')
for z in [i*.019-.095 for i in range(11)]:box('Grille slat',(-.115,-.108,z),(.205,.012,.007),'Wood',.001)
box('Dial rim',(.108,-.09,.055),(.205,.026,.105),'Brass');box('Dial face',(.108,-.107,.055),(.18,.008,.08),'Dial',.001)
box('Base feet',(0,0,-.147),(.40,.12,.025),'Iron')
for x in (-.14,.14):box('Handle leg',(x,0,.19),(.022,.025,.11),'Wood')
box('Carry handle',(0,0,.244),(.30,.028,.025),'Wood')
cylinder('Antenna',(.19,.035,.31),.004,.35,'Brass',n=6)
parent(join('RadioBody'),r)
for name,x in [('TuneKnob',.055),('VolumeKnob',.165)]:
 parent(cylinder(name,(x,-.125,-.065),.027,.025,'Wood','Y',8),r);parts.clear()
# Give knobs origins at their own centres. Cylinder primitive origins already do so.
box('Power button',(.195,-.115,.112),(.035,.02,.018),'Brass');parent(join('PowerButton',(.195,-.115,.112)),r)
box('Replay button',(.10,-.115,.112),(.04,.02,.018),'Brass');parent(join('ReplayButton',(.10,-.115,.112)),r)
box('Needle',(.108,-.116,.055),(.003,.003,.069),'Red',0);parent(join('DialNeedle',(.108,-.116,.055)),r)
save(r,'ManorRadio')
# Three exposed ten-sided rotors and a pressable submit latch.
r=empty('ManorCombinationLock')
box('Lock body',(0,0,.065),(.19,.055,.115),'Brass',.012)
parent(join('LockBody'),r)
for x in (-.052,.052):cylinder('Shackle upright',(x,0,.153),.009,.10,'Iron',n=8)
cylinder('Shackle arch',(0,0,.203),.009,.104,'Iron','X',8)
parent(join('Shackle'),r)
for i,x in enumerate((-.052,0,.052)):
 cylinder('Rotor',(x,-.046,.065),.029,.040,'Iron','X',10);parent(join('Drum'+str(i),(x,-.046,.065)),r)
box('Submit latch',(0,-.039,.018),(.055,.025,.019),'Wood');parent(join('SubmitButton',(0,-.039,.018)),r)
save(r,'ManorCombinationLock')
# Display models at comfortable preview scales; exports retain metre scale.
for r,loc,scale in zip(roots,[(-1.8,1.4,0),(0,1,0),(1.8,0,.3),(2.6,1.2,0)],[1,1,2.5,4]):
 for o in [r]+list(r.children_recursive):bpy.context.collection.objects.link(o)
 r.location=loc;r.scale=(scale,)*3
def aim(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(6,-9,5));cam=bpy.context.object;aim(cam,(.3,.7,.65));cam.data.type='ORTHO';cam.data.ortho_scale=7
scene=bpy.context.scene;scene.camera=cam
for loc,power,size in [((-2,-4,6),1000,5),((5,2,5),700,4)]:
 bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.data.energy=power;o.data.size=size;aim(o,(0,0,.8))
scene.world=bpy.data.worlds.new('Studio');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.10,.12,.14,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.45
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.render.resolution_x=1700;scene.render.resolution_y=1100;scene.render.resolution_percentage=100;scene.render.filepath=str(ROOT/'InteractiveModels_Preview.png');bpy.ops.render.render(write_still=True)
(ASSET/'MaterialManifest.json').write_text(json.dumps({'materials':spec},indent=2));print('INTERACTIVE_MODELS_OK')
