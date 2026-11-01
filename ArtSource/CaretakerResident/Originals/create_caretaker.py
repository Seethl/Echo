import bpy, math, random, os, json
from mathutils import Vector
from math import sin, cos, pi

OUT = os.path.dirname(os.path.abspath(__file__))
random.seed(31)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for d in list(bpy.data.materials): bpy.data.materials.remove(d)

def material(name, color, rough=.85):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=(*color,1); bs.inputs['Roughness'].default_value=rough
    return m
coat=material('01 | weathered umber wool',(.105,.081,.043))
coat_l=material('02 | lit wool facets',(.14,.11,.062))
coat_d=material('03 | worn wool shadows',(.075,.055,.027))
lining=material('04 | dark coat lining',(.075,.048,.032))
shirt=material('05 | oxblood shirt',(.115,.036,.028))
shirt_l=material('06 | faded oxblood',(.15,.052,.038))
pants=material('07 | brown trousers',(.105,.048,.033))
boot=material('08 | distressed leather',(.052,.033,.025))
sole=material('09 | rubber soles',(.025,.021,.018))
skin=material('10 | waxen skin',(.52,.365,.235))
skin_l=material('11 | bone highlights',(.63,.465,.32))
skin_d=material('12 | gaunt shadows',(.35,.226,.145))
socket=material('13 | recessed sockets',(.033,.022,.018))
mouth=material('14 | mouth crease',(.105,.052,.035))
button=material('15 | tarnished buttons',(.095,.071,.042),.5)
parts=[]; binds={}

def mesh(name, vs, fs, mats, bone=None, variation=False):
    me=bpy.data.meshes.new(name); me.from_pydata(vs,[],fs); me.update()
    ob=bpy.data.objects.new(name,me); bpy.context.collection.objects.link(ob)
    for mat in mats: me.materials.append(mat)
    if variation:
        for p in me.polygons:
            r=random.random(); p.material_index=0 if r<.76 else min(1 if r<.91 else 2,len(mats)-1)
    parts.append(ob)
    if bone: binds[ob.name]=bone
    return ob

def ellipsoid(name, loc, scale, mat, bone=None, seg=10, rings=5):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg,ring_count=rings,location=loc)
    ob=bpy.context.object; ob.name=name; ob.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    ob.data.materials.append(mat); parts.append(ob)
    if bone: binds[ob.name]=bone
    return ob

def tube(name, centers, radii, mats, bone=None, n=8):
    vs=[]
    for j,c in enumerate(centers):
        c=Vector(c)
        axis=Vector(centers[min(j+1,len(centers)-1)])-Vector(centers[max(j-1,0)])
        axis.normalize(); u=axis.cross(Vector((0,1,0))).normalized(); v=axis.cross(u).normalized()
        rx,ry = radii[j] if isinstance(radii[j],tuple) else (radii[j],radii[j])
        for i in range(n):
            a=2*pi*i/n+pi/8; vs.append(tuple(c+u*cos(a)*rx+v*sin(a)*ry))
    fs=[tuple(reversed(range(n)))]
    for j in range(len(centers)-1):
        for i in range(n): fs.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
    fs.append(tuple((len(centers)-1)*n+i for i in range(n)))
    return mesh(name,vs,fs,mats,bone,True)

def panel(name, pts, mat, bone='chest', thick=.018):
    ob=mesh(name,pts,[tuple(range(len(pts)))],[mat],bone)
    mod=ob.modifiers.new('Tailored thickness','SOLIDIFY'); mod.thickness=thick
    return ob

# Long narrow legs, sculpted boot silhouettes.
for s,label in [(-1,'R'),(1,'L')]:
    x=s*.205
    tube('Trouser thigh.'+label,[(x,.035,1.86),(x*1.05,.025,1.48),(x*1.14,-.035,1.03)],[(.145,.145),(.127,.126),(.10,.108)],[pants,shirt_l,shirt], 'thigh.'+label)
    tube('Trouser shin.'+label,[(x*1.14,-.035,1.06),(x*1.2,.025,.64),(x*1.26,.015,.29)],[(.105,.106),(.088,.085),(.073,.075)],[pants,shirt,shirt_l], 'shin.'+label)
    bx=x*1.26
    # profiles run from toe to heel, front is negative Y
    vs=[(bx-.11,-.34,.055),(bx+.11,-.34,.055),(bx+.105,.135,.055),(bx-.105,.135,.055),
        (bx-.095,-.32,.15),(bx+.095,-.32,.15),(bx+.085,-.045,.26),(bx+.075,.115,.27),(bx-.075,.115,.27),(bx-.085,-.045,.26)]
    fs=[(0,1,5,4),(1,2,7,6,5),(2,3,8,7),(3,0,4,9,8),(4,5,6,9),(9,6,7,8),(0,3,2,1)]
    mesh('Angular leather boot.'+label,vs,fs,[boot,coat_d], 'foot.'+label,True)
    tube('Boot ankle.'+label,[(bx,.035,.14),(bx,.025,.34)],[(.088,.09),(.078,.077)],[boot], 'foot.'+label)
    mesh('Sole.'+label,[(a,b,c-.025) for a,b,c in vs[:4]]+vs[:4],[(0,1,2,3),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)],[sole],'foot.'+label)

tube('Shirt torso',[(0,.04,1.72),(0,.045,2.07),(0,.04,2.46),(0,.07,2.78)],[(.255,.16),(.24,.16),(.33,.205),(.365,.18)],[shirt,shirt_l,shirt], 'chest')
panel('Shirt center placket',[(-.018,-.17,1.79),(.018,-.17,1.79),(.018,-.177,2.65),(-.018,-.177,2.65)],shirt_l)
for z in [2.48,2.27,2.07,1.88]: ellipsoid('Small shirt button',(0,-.19,z),(.017,.013,.017),button,'chest',8,4)

# Open long coat. Staggered hem vertices create fabric tears rather than a smooth skirt.
N=18; vs=[]
levels=[(.83,.57,.32,.085),(1.22,.49,.29,.065),(1.88,.315,.23,.04),(2.28,.36,.245,.055),(2.71,.47,.245,.08),(2.88,.34,.20,.11)]
for k,(z,w,d,cy) in enumerate(levels):
    for i in range(N+1):
        a=.48+(2*pi-.96)*i/N
        zz=z
        if k==0: zz += [.13,-.04,.07,-.12,.035,-.03,.12,-.07,.05,-.035,.10,-.1,.025,-.04,.11,-.09,.03,-.04,.11][i]
        if k==1: zz+=.025*sin(i*3)
        vs.append((sin(a)*w,cy-cos(a)*d,zz))
fs=[]
for k in range(len(levels)-1):
    for i in range(N):
        a=k*(N+1)+i; b=a+1; c=b+N+1; d=a+N+1
        if (k+i)%3==0: fs.extend([(a,b,d),(b,c,d)])
        else: fs.append((a,b,c,d))
ob=mesh('COAT | open ragged shell',vs,fs,[coat,coat_l,coat_d,lining],'pelvis',True)
solid=ob.modifiers.new('Lined wool thickness','SOLIDIFY'); solid.thickness=.022; solid.material_offset=3
# give coat vertices chest and pelvis weights later
for s,label in [(-1,'R'),(1,'L')]:
    panel('Wide notched lapel.'+label,[(s*.08,-.235,2.88),(s*.28,-.255,2.84),(s*.40,-.234,2.62),(s*.29,-.281,2.65),(s*.325,-.292,2.53),(s*.145,-.255,2.13),(s*.19,-.253,2.56)],coat_l)
    panel('Raised coat collar.'+label,[(s*.075,-.17,2.90),(s*.13,.07,3.015),(s*.29,.09,2.90),(s*.30,-.13,2.79),(s*.20,-.23,2.74)],coat)
    panel('Slanted pocket welt.'+label,[(s*.24,-.117,1.91),(s*.31,-.054,1.98),(s*.31,-.054,2.01),(s*.24,-.117,1.94)],coat_d,'pelvis',.008)

# Neck leans forward out of the hunched shoulders.
tube('Long sinewy neck',[(0,.055,2.77),(0,-.12,2.98),(0,-.24,3.12)],[(.11,.105),(.103,.095),(.105,.095)],[skin,skin_l,skin_d],'neck')
tube('Oxblood high collar',[(0,.03,2.76),(0,-.11,2.965)],[(.145,.13),(.13,.12)],[shirt,shirt_l],'neck')
for s,label in [(-1,'R'),(1,'L')]:
    panel('Shirt pointed collar.'+label,[(s*.015,-.242,2.89),(s*.12,-.22,2.94),(s*.15,-.23,2.75),(s*.07,-.23,2.80)],shirt_l,'neck')

# Long hanging sleeves and oversized articulated hands.
for s,label in [(-1,'R'),(1,'L')]:
    shoulder=(s*.43,.07,2.74); elbow=(s*.57,-.015,2.18); wrist=(s*.65,-.16,1.61)
    ellipsoid('Tailored shoulder cap.'+label,(s*.42,.065,2.74),(.205,.19,.17),coat,'upper_arm.'+label,10,5)
    tube('Coat upper sleeve.'+label,[shoulder,(s*.51,.07,2.5),elbow],[(.185,.175),(.15,.145),(.115,.115)],[coat,coat_l,coat_d],'upper_arm.'+label)
    tube('Coat forearm sleeve.'+label,[elbow,(s*.615,-.07,1.93),wrist],[(.12,.12),(.125,.12),(.13,.115)],[coat,coat_l,coat_d],'forearm.'+label)
    tube('Turned cuff.'+label,[(s*.642,-.145,1.67),(s*.655,-.163,1.59)],[(.135,.12),(.137,.123)],[coat_d,coat],'forearm.'+label)
    tube('Bony wrist.'+label,[(s*.65,-.16,1.65),(s*.66,-.18,1.49)],[(.058,.055),(.065,.048)],[skin,skin_l,skin_d],'hand.'+label)
    tube('Angular palm.'+label,[(s*.66,-.18,1.53),(s*.685,-.20,1.39),(s*.69,-.205,1.32)],[(.064,.047),(.098,.048),(.089,.042)],[skin,skin_l,skin_d],'hand.'+label,n=8)
    for f in range(4):
        fx=s*(.615+f*.049); length=[.20,.25,.265,.215][f]; zz=1.345-abs(f-1.5)*.013
        points=[(fx,-.215,zz),(fx+s*.016,-.25,zz-length*.47),(fx+s*.012,-.285,zz-length*.83),(fx-s*.008,-.32,zz-length)]
        tube('Long finger %d.%s'%(f+1,label),points,[.025,.023,.019,.012],[skin,skin_l,skin_d],'hand.'+label,n=5)
    tube('Hooked thumb.'+label,[(s*.60,-.195,1.47),(s*.535,-.23,1.38),(s*.52,-.28,1.30),(s*.55,-.32,1.26)],[.037,.031,.025,.016],[skin,skin_l,skin_d],'hand.'+label,n=6)

# Bald elongated cranium, built as sparse angular rings.
vs=[]
headlevels=[(3.075,.105,.105,-.25),(3.19,.16,.135,-.255),(3.35,.19,.15,-.245),(3.51,.192,.153,-.225),(3.62,.155,.135,-.21),(3.66,.08,.085,-.20)]
HN=12
for z,w,d,cy in headlevels:
    for i in range(HN):
        a=2*pi*i/HN; vs.append((sin(a)*w,cy-cos(a)*d,z))
fs=[tuple(reversed(range(HN)))]
for j in range(len(headlevels)-1):
    for i in range(HN):
        a=j*HN+i;b=j*HN+(i+1)%HN;c=b+HN;d=a+HN
        fs.append((a,b,c,d))
fs.append(tuple((len(headlevels)-1)*HN+i for i in range(HN)))
mesh('HEAD | elongated faceted cranium',vs,fs,[skin,skin_l,skin_d],'head',True)
# Face sits in front of the cranium. Low eyebrows hood deep dark socket wells.
for s,label in [(-1,'R'),(1,'L')]:
    def mir(pts): return [(s*x,y,z) for x,y,z in pts]
    panel('Recessed eye socket.'+label,mir([(.025,-.405,3.405),(.092,-.423,3.424),(.161,-.389,3.391),(.13,-.406,3.324),(.065,-.425,3.327)]),socket,'head',.008)
    panel('Heavy angular brow.'+label,mir([(.014,-.398,3.444),(.105,-.421,3.465),(.178,-.372,3.423),(.157,-.415,3.392),(.088,-.45,3.407),(.027,-.434,3.387)]),skin_l,'head',.012)
    panel('Socket lower rim.'+label,mir([(.058,-.421,3.329),(.128,-.408,3.325),(.158,-.391,3.37),(.17,-.375,3.286),(.10,-.419,3.287)]),skin_d,'head')
    panel('High hollow cheek.'+label,mir([(.17,-.382,3.34),(.185,-.335,3.30),(.125,-.38,3.20),(.075,-.433,3.245),(.10,-.419,3.287)]),skin_l,'head')
    panel('Sunken cheek plane.'+label,mir([(.185,-.335,3.30),(.15,-.322,3.16),(.098,-.365,3.083),(.08,-.42,3.19),(.125,-.38,3.20)]),skin,'head')
    ellipsoid('Ear.'+label,(s*.19,-.19,3.36),(.042,.035,.077),skin_d,'head',8,4)
    ellipsoid('Ear rim.'+label,(s*.209,-.202,3.365),(.023,.027,.052),skin,'head',8,4)
mesh('Long blade nose',[(0,-.426,3.423),(-.038,-.426,3.275),(-.029,-.486,3.24),(0,-.515,3.25),(.029,-.486,3.24),(.038,-.426,3.275),(0,-.455,3.20)],[(0,1,2,3),(0,3,4,5),(1,6,2),(2,6,4,3),(4,6,5)],[skin_l,skin,skin_d],'head',True)
panel('Muzzle plane',[(-.076,-.421,3.24),(0,-.45,3.21),(.076,-.421,3.24),(.068,-.425,3.16),(-.068,-.425,3.16)],skin,'head')
panel('Thin downturned mouth',[(-.067,-.435,3.174),(-.038,-.446,3.185),(0,-.45,3.181),(.038,-.446,3.185),(.067,-.435,3.174),(.035,-.444,3.175),(-.034,-.444,3.175)],mouth,'head',.002)
mesh('Tapered chin',[(-.07,-.428,3.16),(.07,-.428,3.16),(.087,-.373,3.087),(.048,-.39,3.043),(-.048,-.39,3.043),(-.087,-.373,3.087),(0,-.444,3.09)],[(0,1,6),(1,2,3,6),(3,4,6),(4,5,0,6)],[skin,skin_l],'head',True)

# A simple usable deform rig. Separate meshes remain editable.
arm=bpy.data.armatures.new('Caretaker skeleton'); rig=bpy.data.objects.new('RIG | Caretaker',arm); bpy.context.collection.objects.link(rig)
bpy.context.view_layer.objects.active=rig; rig.select_set(True); bpy.ops.object.mode_set(mode='EDIT')
def bone(name,head,tail,parent=None):
    b=arm.edit_bones.new(name); b.head=head; b.tail=tail
    if parent: b.parent=arm.edit_bones[parent]
bone('root',(0,0,.05),(0,0,.4))
bone('pelvis',(0,.04,1.72),(0,.04,2.04),'root')
bone('chest',(0,.04,2.04),(0,.07,2.78),'pelvis')
bone('neck',(0,.07,2.78),(0,-.24,3.12),'chest')
bone('head',(0,-.24,3.12),(0,-.22,3.65),'neck')
for s,label in [(-1,'R'),(1,'L')]:
    bone('thigh.'+label,(s*.205,.035,1.86),(s*.234,-.035,1.03),'pelvis')
    bone('shin.'+label,(s*.234,-.035,1.03),(s*.258,.015,.29),'thigh.'+label)
    bone('foot.'+label,(s*.258,.015,.29),(s*.258,-.28,.12),'shin.'+label)
    bone('upper_arm.'+label,(s*.43,.07,2.74),(s*.57,-.015,2.18),'chest')
    bone('forearm.'+label,(s*.57,-.015,2.18),(s*.65,-.16,1.61),'upper_arm.'+label)
    bone('hand.'+label,(s*.65,-.16,1.61),(s*.69,-.205,1.32),'forearm.'+label)
bpy.ops.object.mode_set(mode='OBJECT'); rig.show_in_front=True
for ob in parts:
    bn=binds.get(ob.name)
    if not bn: continue
    ob.parent=rig
    if ob.name.startswith('COAT'):
        gp=ob.vertex_groups.new(name='pelvis'); gc=ob.vertex_groups.new(name='chest')
        for v in ob.data.vertices:
            t=max(0,min(1,(v.co.z-1.95)/.65)); gp.add([v.index],1-t,'REPLACE');gc.add([v.index],t,'REPLACE')
    else:
        g=ob.vertex_groups.new(name=bn);g.add(list(range(len(ob.data.vertices))),1,'REPLACE')
    mod=ob.modifiers.new('Caretaker deform','ARMATURE');mod.object=rig

# Subtle breathing / listening idle. This is a basic rig, not a full game animation set.
scene=bpy.context.scene; scene.frame_start=1;scene.frame_end=96;scene.render.fps=24
for frame,amount in [(1,0),(25,1),(49,0),(73,-.65),(96,0)]:
    for name,rot in [('chest',(.105+.012*amount,0,.008*amount)),('neck',(.085+.015*amount,0,-.025*amount)),('head',(.06+.01*amount,.018*amount,.025*amount)),('upper_arm.L',(-.08+.012*amount,0,.006*amount)),('upper_arm.R',(-.08-.01*amount,0,-.007*amount))]:
        pb=rig.pose.bones[name];pb.rotation_mode='XYZ';pb.rotation_euler=rot;pb.keyframe_insert(data_path='rotation_euler',frame=frame,group=name)
if rig.animation_data and rig.animation_data.action: rig.animation_data.action.name='Idle | listening - 4 seconds'
scene.frame_set(1)

# Studio stage is kept in its own collection and omitted from the GLB export.
studio=bpy.data.collections.new('STUDIO | render only');scene.collection.children.link(studio)
def studio_obj(ob):
    for c in list(ob.users_collection): c.objects.unlink(ob)
    studio.objects.link(ob)
floor=material('Studio charcoal',(.025,.03,.033))
bpy.ops.mesh.primitive_plane_add(size=200); ground=bpy.context.object;ground.name='Shadow floor';ground.data.materials.append(floor);studio_obj(ground)
def aim(ob,pt): ob.rotation_euler=(Vector(pt)-ob.location).to_track_quat('-Z','Y').to_euler()
def area(name,loc,power,color,size):
    bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.name=name;o.data.energy=power;o.data.color=color;o.data.shape='DISK';o.data.size=size;aim(o,(0,0,1.9));studio_obj(o)
area('Key | large warm softbox',(-3,-4.5,6),480,(1,.83,.66),4)
area('Fill | cool softbox',(3,-2,3.5),180,(.62,.75,1),3)
area('Rim | overhead edge',(1.5,3,5),650,(1,.86,.65),3)
bpy.ops.object.camera_add(location=(5,-9,4.15));cam=bpy.context.object;cam.name='Camera | three quarter';aim(cam,(0,-.03,1.88));cam.data.type='ORTHO';cam.data.ortho_scale=4.35;studio_obj(cam);scene.camera=cam
scene.world.color=(.13,.13,.13)
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
scene.render.resolution_x=1000;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX'
scene.render.image_settings.file_format='PNG'
scene.render.filepath=os.path.join(OUT,'caretaker_preview.png')
scene['Reference']='Caretaker Creature supplied by the user. Procedural low-poly interpretation.'
rig['Rig notes']='Basic body deformation rig + subtle listening idle. Fingers are modeled, not individually rigged. Coat tails require refinement for walk/run.'
bpy.ops.object.select_all(action='DESELECT')
for ob in parts+[rig]: ob.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.gltf(filepath=os.path.join(OUT,'caretaker.glb'),use_selection=True,export_animations=True,export_apply=False)
# Set a useful starting viewport.
for screen in bpy.data.screens:
    for a in screen.areas:
        if a.type=='VIEW_3D':
            a.spaces.active.region_3d.view_distance=5.5
            a.spaces.active.region_3d.view_location=(0,0,1.9)
            a.spaces.active.region_3d.view_rotation=cam.rotation_euler.to_quaternion()
            a.spaces.active.shading.type='MATERIAL'
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'caretaker.blend'))
bpy.ops.render.render(write_still=True)
cam.location=(0,-10,2.7);aim(cam,(0,-.03,1.88));scene.render.filepath=os.path.join(OUT,'caretaker_front.png');bpy.ops.render.render(write_still=True)
stats={'mesh_objects':len(parts),'vertices':sum(len(o.data.vertices) for o in parts),'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in parts),'bones':len(arm.bones)}
with open(os.path.join(OUT,'model_stats.json'),'w') as f: json.dump(stats,f,indent=2)
print('CARETAKER_DONE',stats)
