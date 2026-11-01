import bpy,json,math,re
import numpy as np
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parent
SRC=ROOT/'Originals'
OUT=ROOT.parents[1]/'Assets/Props/CaretakerResident';OUT.mkdir(parents=True,exist_ok=True)
norm=lambda name:name.replace(' ','_')
bpy.ops.wm.open_mainfile(filepath=str(SRC/'caretaker.blend'))
bpy.context.scene.frame_set(1);deps=bpy.context.evaluated_depsgraph_get()
original={};specs=[]
for m in bpy.data.materials:
    if m.name=='Studio charcoal':continue
    bs=m.node_tree.nodes.get('Principled BSDF') if m.use_nodes else None
    specs.append({'source':m.name,'name':'Resident_'+re.sub(r'[^A-Za-z0-9]+','_',m.name).strip('_'),'color':list(m.diffuse_color[:3]),'roughness':float(bs.inputs['Roughness'].default_value) if bs else .8})
for ob in bpy.data.objects:
    if ob.type!='MESH' or ob.name=='Shadow floor':continue
    evaluated=ob.evaluated_get(deps);mesh=evaluated.to_mesh()
    original[norm(ob.name)]={'verts':np.array([tuple(evaluated.matrix_world@v.co) for v in mesh.vertices]),'faces':[(list(p.vertices),p.material_index) for p in mesh.polygons],'materials':[m.name for m in mesh.materials],'bone':ob.vertex_groups[0].name if ob.vertex_groups else 'head','weights':[[(ob.vertex_groups[g.group].name,float(g.weight)) for g in v.groups] for v in mesh.vertices]}
    evaluated.to_mesh_clear()
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(SRC/'animation/Walking.fbx'))
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');rig.name='CaretakerRig'
meshes=[o for o in bpy.data.objects if o.type=='MESH'];mats={}
for spec in specs:
    m=bpy.data.materials.new(spec['name']);m.diffuse_color=(*spec['color'],1);m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*spec['color'],1);bs.inputs['Roughness'].default_value=spec['roughness'];mats[spec['source']]=m
deform_map={'root':'mixamorig:Hips','pelvis':'mixamorig:Hips','chest':'mixamorig:Spine2','neck':'mixamorig:Neck','head':'mixamorig:Head'}
for suffix,side in [('L','Left'),('R','Right')]:
    for old,new in [('upper_arm','Arm'),('forearm','ForeArm'),('hand','Hand'),('thigh','UpLeg'),('shin','Leg'),('foot','Foot')]:deform_map[old+'.'+suffix]='mixamorig:'+side+new
restored=0;unmapped=0;local_fits={}
for ob in meshes:
    source=original[ob.name];a=source['verts'];b=np.array([tuple(ob.matrix_world@v.co) for v in ob.data.vertices])
    assert len(a)==len(b),(ob.name,len(a),len(b))
    affine=np.linalg.lstsq(np.column_stack([a,np.ones(len(a))]),b,rcond=None)[0];local_fits[ob.name]=affine
    # Restore authored rigid-part weights: Mixamo auto skinning sheared the separate
    # low-poly sleeves, face plates and boots. Keep modest leg influence on coat hems.
    ob.vertex_groups.clear()
    for index,entries in enumerate(source['weights']):
        weights={deform_map[name]:weight for name,weight in entries if weight>.0001}
        z=float(source['verts'][index][2]);x=float(source['verts'][index][0])
        if ob.name=='COAT_|_open_ragged_shell':
            hem=max(0,min(1,(1.7-z)/.9))*.35
            weights={name:weight*(1-hem) for name,weight in weights.items()}
            if hem>0:weights['mixamorig:LeftUpLeg' if x>0 else 'mixamorig:RightUpLeg']=hem
        for name,weight in weights.items():
            group=ob.vertex_groups.get(name) or ob.vertex_groups.new(name=name);group.add([index],weight,'REPLACE')
    for name in source['materials']:ob.data.materials.append(mats[name])
    faces=[(set(indices),material) for indices,material in source['faces']]
    for poly in ob.data.polygons:
        vertices=set(poly.vertices);matches=[material for indices,material in faces if vertices<=indices]
        if matches:poly.material_index=matches[0];restored+=1
        else:poly.material_index=0;unmapped+=1
        poly.use_smooth=False
assert unmapped==0,('Material topology differs',unmapped)
# Mixamo's OBJ upload omitted small ears/buttons/shoulder caps. Restore these original pieces.
bone_map={'head':'mixamorig:Head','chest':'mixamorig:Spine2','upper_arm.L':'mixamorig:LeftArm','upper_arm.R':'mixamorig:RightArm'}
for name,source in original.items():
    if name in {o.name for o in meshes}:continue
    bone=bone_map.get(source['bone'],'mixamorig:Spine2')
    reference='HEAD_|_elongated_faceted_cranium' if bone.endswith('Head') else 'Coat_upper_sleeve.L' if bone.endswith('LeftArm') else 'Coat_upper_sleeve.R' if bone.endswith('RightArm') else 'Shirt_torso'
    points=np.column_stack([source['verts'],np.ones(len(source['verts']))])@local_fits[reference]
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(points.tolist(),[],[indices for indices,_ in source['faces']]);mesh.update()
    for mat in source['materials']:mesh.materials.append(mats[mat])
    for p,(_,index) in zip(mesh.polygons,source['faces']):p.material_index=index
    ob=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(ob);ob.parent=rig;ob.matrix_world=Matrix.Identity(4)
    group=ob.vertex_groups.new(name=bone);group.add(list(range(len(mesh.vertices))),1,'REPLACE');modifier=ob.modifiers.new('Resident skin','ARMATURE');modifier.object=rig;meshes.append(ob)
assert len(meshes)==68
scene=bpy.context.scene;scene.render.fps=30
walk_source=rig.animation_data.action
actions=[];motion_report={}
def bake(source_rig,source_action,name):
    source_rig.animation_data.action=source_action
    # Read the entire source before assigning the new target action (walking uses the same rig).
    first,last=[round(x) for x in source_action.frame_range];samples=[]
    for frame in range(first,last+1):
        scene.frame_set(frame);samples.append({p.name:p.matrix_basis.copy() for p in source_rig.pose.bones})
    action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action
    initial=None;removed=[]
    for frame,values in enumerate(samples,1):
        for name2,mat in values.items():
            p=rig.pose.bones[name2];p.matrix_basis=mat;p.rotation_mode='QUATERNION'
        bpy.context.view_layer.update()
        hips=rig.pose.bones['mixamorig:Hips'];world=rig.matrix_world@hips.matrix.translation
        if initial is None:initial=world.copy()
        travel=Vector((world.x-initial.x,world.y-initial.y,0));removed.append(travel.length)
        matrix=hips.matrix.copy();matrix.translation-=rig.matrix_world.inverted().to_3x3()@travel;hips.matrix=matrix
        for p in rig.pose.bones:
            p.keyframe_insert('location',frame=frame,group=p.name);p.keyframe_insert('rotation_quaternion',frame=frame,group=p.name);p.keyframe_insert('scale',frame=frame,group=p.name)
    actions.append(action);motion_report[name]={'frames':len(samples),'removedHorizontalTravelMetres':max(removed)}
bake(rig,walk_source,'ResidentWalk')
for filename,name in [('Drinking.fbx','ResidentDrink'),('Zombie Attack.fbx','ResidentAttack')]:
    before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(SRC/'animation'/filename))
    imported=set(bpy.data.objects)-before;other=next(o for o in imported if o.type=='ARMATURE')
    assert set(other.data.bones.keys())==set(rig.data.bones.keys())
    for b in rig.data.bones:
        assert max(abs(v) for row in (b.matrix_local-other.data.bones[b.name].matrix_local) for v in row)<.001,'Different bind pose'
    bake(other,other.animation_data.action,name)
    for ob in imported:bpy.data.objects.remove(ob,do_unlink=True)
idle=bpy.data.actions.new('ResidentIdle');idle.use_fake_user=True;actions.append(idle);rig.animation_data.action=idle
for frame,amount in [(1,0),(16,1),(31,0),(46,-1),(61,0)]:
    for p in rig.pose.bones:p.matrix_basis=Matrix.Identity(4);p.rotation_mode='XYZ'
    rig.pose.bones['mixamorig:Spine2'].rotation_euler.x=.008*amount
    rig.pose.bones['mixamorig:Head'].rotation_euler.y=.018*amount
    for p in rig.pose.bones:
        p.rotation_mode='QUATERNION';p.keyframe_insert('location',frame=frame,group=p.name);p.keyframe_insert('rotation_quaternion',frame=frame,group=p.name);p.keyframe_insert('scale',frame=frame,group=p.name)
scene.frame_set(1)
for action in list(bpy.data.actions):
    if action not in actions:bpy.data.actions.remove(action)
# Merge small skinned pieces to avoid 68 independent animated renderers.
bpy.ops.object.select_all(action='DESELECT')
for ob in meshes:ob.select_set(True)
bpy.context.view_layer.objects.active=meshes[0];bpy.ops.object.join();skin=bpy.context.object;skin.name='Caretaker coloured skin'
deps=bpy.context.evaluated_depsgraph_get();evaluated=skin.evaluated_get(deps)
points=[evaluated.matrix_world@Vector(c) for c in evaluated.bound_box]
minimum=min(p.z for p in points);maximum=max(p.z for p in points);scale=1.95/(maximum-minimum)
root=bpy.data.objects.new('ResidentVisual',None);bpy.context.collection.objects.link(root)
world=rig.matrix_world.copy();rig.parent=root;rig.matrix_world=world
root.scale=(scale,scale,scale);root.location.z=-minimum*scale+.012
scene.frame_start=1;scene.frame_end=61;rig.animation_data.action=idle;scene.frame_set(1)
# Bake to a metre-based identity-transform rig. FBX skinning must not inherit the
# Mixamo centimetre scale and a second scaled wrapper while the Animator is active.
source_rig=rig;source_skin=skin;source_actions=list(actions)
data=bpy.data.armatures.new('Caretaker metre skeleton');unit=bpy.data.objects.new('Unit caretaker rig',data);bpy.context.collection.objects.link(unit)
bpy.context.view_layer.objects.active=unit;unit.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
for old in source_rig.data.bones:
    b=data.edit_bones.new(old.name)
    world=source_rig.matrix_world@old.matrix_local;loc,rot,_=world.decompose();b.matrix=Matrix.LocRotScale(loc,rot,Vector((1,1,1)))
    b.length=(source_rig.matrix_world@old.tail_local-source_rig.matrix_world@old.head_local).length
    if old.parent:b.parent=data.edit_bones[old.parent.name]
bpy.ops.object.mode_set(mode='OBJECT')
copy=source_skin.copy();copy.data=source_skin.data.copy();bpy.context.collection.objects.link(copy)
copy.data.transform(source_skin.matrix_world);copy.parent=unit;copy.matrix_parent_inverse=Matrix.Identity(4);copy.matrix_world=Matrix.Identity(4)
for modifier in list(copy.modifiers):copy.modifiers.remove(modifier)
modifier=copy.modifiers.new('Resident deform','ARMATURE');modifier.object=unit
unit.animation_data_create();unit_actions=[]
for source_action in source_actions:
    source_rig.animation_data.action=source_action;first,last=[round(v) for v in source_action.frame_range];samples=[]
    for frame in range(first,last+1):
        scene.frame_set(frame);values={}
        for p in source_rig.pose.bones:
            loc,rot,_=(source_rig.matrix_world@p.matrix).decompose();values[p.name]=Matrix.LocRotScale(loc,rot,Vector((1,1,1)))
        samples.append(values)
    name=source_action.name;source_action.name='Original_'+name
    action=bpy.data.actions.new(name);action.use_fake_user=True;unit.animation_data.action=action;unit_actions.append(action)
    for frame,values in enumerate(samples,1):
        for p in unit.pose.bones:
            p.rotation_mode='QUATERNION'
            kwargs={'parent_matrix':values[p.parent.name],'parent_matrix_local':p.parent.bone.matrix_local} if p.parent else {}
            p.matrix_basis=p.bone.convert_local_to_pose(values[p.name],p.bone.matrix_local,invert=True,**kwargs)
            p.keyframe_insert('location',frame=frame,group=p.name);p.keyframe_insert('rotation_quaternion',frame=frame,group=p.name);p.keyframe_insert('scale',frame=frame,group=p.name)
for old in list(bpy.data.objects):
    if old not in (unit,copy):bpy.data.objects.remove(old,do_unlink=True)
for action in list(bpy.data.actions):
    if action not in unit_actions:bpy.data.actions.remove(action)
unit.name='CaretakerRig';copy.name='Caretaker coloured skin';root=bpy.data.objects.new('ResidentVisual',None);bpy.context.collection.objects.link(root);unit.parent=root
rig=unit;skin=copy;actions=unit_actions;idle=next(a for a in actions if a.name=='ResidentIdle');rig.animation_data.action=idle;scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT')
for ob in [root,rig,skin]:ob.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.fbx(filepath=str(OUT/'SM_CaretakerResident.fbx'),use_selection=True,object_types={'MESH','ARMATURE','EMPTY'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=False,add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'MotionReference.blend'))
(OUT/'materials.json').write_text(json.dumps({'items':specs},indent=2),encoding='utf-8')
report={'restoredPolygons':restored,'unmappedPolygons':unmapped,'restoredMeshPieces':68,'runtimeSkinnedMeshes':1,'bones':len(rig.data.bones),'height':1.95,'scale':scale,'actions':motion_report}
(ROOT/'build-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print('RESIDENT_BUILT',report)
