import bpy,json
from pathlib import Path
from mathutils import Matrix,Vector
ROOT=Path(__file__).resolve().parent;SRC=ROOT/'Originals';OUT=ROOT.parents[1]/'Assets/Props/CaretakerResident'
# Read the prepared, in-place Mixamo motions in world space.
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'MotionReference.blend'))
reference=next(o for o in bpy.data.objects if o.type=='ARMATURE');motions={};rests={b.name:(reference.matrix_world@b.matrix_local).to_quaternion() for b in reference.data.bones}
for name in ['ResidentIdle','ResidentWalk','ResidentDrink','ResidentAttack']:
 reference.animation_data.action=bpy.data.actions[name];samples=[]
 for frame in range(1,round(reference.animation_data.action.frame_range[1])+1):
  bpy.context.scene.frame_set(frame);samples.append({p.name:((reference.matrix_world@p.matrix).to_quaternion().copy(),(reference.matrix_world@p.matrix).translation.copy()) for p in reference.pose.bones})
 motions[name]=samples
rests={name:pose[0] for name,pose in motions['ResidentIdle'][0].items()}
bpy.ops.wm.open_mainfile(filepath=str(SRC/'caretaker.blend'))
old=next(o for o in bpy.data.objects if o.type=='ARMATURE');scale=1.95/3.685
bone_specs=[(b.name,b.parent.name if b.parent else None,b.matrix_local.copy(),b.head_local.copy(),b.tail_local.copy()) for b in old.data.bones]
parts=[]
for ob in list(bpy.data.objects):
 if ob.type!='MESH' or ob.name=='Shadow floor':continue
 for mod in ob.modifiers:
  if mod.type=='ARMATURE':mod.show_viewport=False
bpy.context.view_layer.update();deps=bpy.context.evaluated_depsgraph_get()
for ob in list(bpy.data.objects):
 if ob.type!='MESH' or ob.name=='Shadow floor':continue
 ev=ob.evaluated_get(deps);m=ev.to_mesh()
 parts.append((ob.name,[tuple(ev.matrix_world@v.co) for v in m.vertices],[(list(p.vertices),p.material_index) for p in m.polygons],[x.name for x in m.materials],[[(ob.vertex_groups[g.group].name,g.weight) for g in v.groups] for v in m.vertices]));ev.to_mesh_clear()
palette={s['source']:s for s in json.loads((OUT/'materials.json').read_text())['items']}
for ob in list(bpy.data.objects):bpy.data.objects.remove(ob,do_unlink=True)
for action in list(bpy.data.actions):bpy.data.actions.remove(action)
mats={}
for name,s in palette.items():
 m=bpy.data.materials.get(name);m.name=s['name'];mats[name]=m
data=bpy.data.armatures.new('Authored caretaker skeleton');rig=bpy.data.objects.new('CaretakerRig',data);bpy.context.collection.objects.link(rig);bpy.context.view_layer.objects.active=rig;rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
for name,parent,mat,head,tail in bone_specs:
 b=data.edit_bones.new(name);b.matrix=mat;b.head=head*scale+Vector((0,0,.02));b.tail=tail*scale+Vector((0,0,.02))
 if parent:b.parent=data.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT')
meshes=[]
for name,verts,faces,materials,weights in parts:
 mesh=bpy.data.meshes.new(name);mesh.from_pydata([Vector(v)*scale+Vector((0,0,.02)) for v in verts],[],[f for f,_ in faces]);mesh.update()
 for material in materials:mesh.materials.append(mats[material])
 for polygon,(_,index) in zip(mesh.polygons,faces):polygon.material_index=index
 ob=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(ob);ob.parent=rig
 for i,entries in enumerate(weights):
  for bone,weight in entries:
   group=ob.vertex_groups.get(bone) or ob.vertex_groups.new(name=bone);group.add([i],weight,'REPLACE')
 modifier=ob.modifiers.new('Authored skin','ARMATURE');modifier.object=rig;meshes.append(ob)
mapping={'pelvis':'mixamorig:Hips','chest':'mixamorig:Spine2','neck':'mixamorig:Neck','head':'mixamorig:Head'}
for side,label in [('L','Left'),('R','Right')]:
 for target,source in [('thigh','UpLeg'),('shin','Leg'),('foot','Foot'),('upper_arm','Arm'),('forearm','ForeArm'),('hand','Hand')]:mapping[target+'.'+side]='mixamorig:'+label+source
rig.animation_data_create();actions={};scene=bpy.context.scene;scene.render.fps=30
for name,samples in motions.items():
 action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action;actions[name]=action;initial=samples[0]['mixamorig:Hips'][1].z
 for frame,sample in enumerate(samples,1):
  desired={}
  for p in rig.pose.bones:
   rest=p.bone.matrix_local
   matrix=desired[p.parent.name]@p.parent.bone.matrix_local.inverted()@rest if p.parent else rest.copy()
   rotation=rest.to_quaternion();driver=mapping.get(p.name)
   if driver:rotation=(sample[driver][0]@rests[driver].inverted())@rotation
   loc=matrix.translation
   if p.name=='pelvis':loc.z+=(sample['mixamorig:Hips'][1].z-initial)*.75
   matrix=Matrix.LocRotScale(loc,rotation,Vector((1,1,1)));desired[p.name]=matrix
   kwargs={'parent_matrix':desired[p.parent.name],'parent_matrix_local':p.parent.bone.matrix_local} if p.parent else {}
   p.rotation_mode='QUATERNION';p.matrix_basis=p.bone.convert_local_to_pose(matrix,rest,invert=True,**kwargs)
   p.keyframe_insert('location',frame=frame,group=p.name);p.keyframe_insert('rotation_quaternion',frame=frame,group=p.name);p.keyframe_insert('scale',frame=frame,group=p.name)
bpy.ops.object.select_all(action='DESELECT')
for ob in meshes:ob.select_set(True)
bpy.context.view_layer.objects.active=meshes[0];bpy.ops.object.join();skin=bpy.context.object;skin.name='Caretaker coloured skin'
root=bpy.data.objects.new('ResidentVisual',None);bpy.context.collection.objects.link(root);rig.parent=root;rig.animation_data.action=actions['ResidentIdle'];scene.frame_start=1;scene.frame_end=61;scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT')
for ob in (root,rig,skin):ob.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.fbx(filepath=str(OUT/'SM_CaretakerResident.fbx'),use_selection=True,object_types={'MESH','ARMATURE','EMPTY'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'CaretakerResident.blend'))
print('AUTHORED_RIG_RETARGETED bones=17 meshes=1 parts=68')
