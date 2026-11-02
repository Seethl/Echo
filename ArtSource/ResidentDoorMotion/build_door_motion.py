import bpy,json,math
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parent;INSTALLED=ROOT.parent.name=='ArtSource';SRC=ROOT.parents[1] if INSTALLED else Path('C:/Projects/Echo');OUT=(SRC/'Assets/Props/ResidentDoorMotion') if INSTALLED else ROOT/'stage/Assets/Props/ResidentDoorMotion';OUT.mkdir(parents=True,exist_ok=True)
ART=ROOT if INSTALLED else ROOT/'stage/ArtSource/ResidentDoorMotion';ART.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(SRC/'ArtSource/CaretakerResident/MotionReference.blend'))
reference=next(o for o in bpy.data.objects if o.type=='ARMATURE');reference.animation_data.action=bpy.data.actions['ResidentIdle'];bpy.context.scene.frame_set(1)
rests={p.name:(reference.matrix_world@p.matrix).to_quaternion().copy() for p in reference.pose.bones}
before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str((ROOT/'Opening Door Inwards.fbx') if INSTALLED else SRC/'папочка/caretaker/animation/Opening Door Inwards.fbx'))
source=next(o for o in set(bpy.data.objects)-before if o.type=='ARMATURE');action=source.animation_data.action;first,last=[round(x) for x in action.frame_range];samples=[]
for frame in range(first,last+1):
 bpy.context.scene.frame_set(frame);samples.append({p.name:(source.matrix_world@p.matrix).to_quaternion().copy() for p in source.pose.bones})
bpy.ops.wm.open_mainfile(filepath=str(SRC/'ArtSource/CaretakerResident/CaretakerResident.blend'))
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE');mapping={'pelvis':'mixamorig:Hips','chest':'mixamorig:Spine2','neck':'mixamorig:Neck','head':'mixamorig:Head'}
for side,label in [('L','Left'),('R','Right')]:
 for target,original in [('thigh','UpLeg'),('shin','Leg'),('foot','Foot'),('upper_arm','Arm'),('forearm','ForeArm'),('hand','Hand')]:mapping[target+'.'+side]='mixamorig:'+label+original
for a in list(bpy.data.actions):bpy.data.actions.remove(a)
scene=bpy.context.scene;scene.render.fps=30;rig.animation_data_create();made=[]
for name,frames in [('ResidentDoorOpen',samples),('ResidentDoorClose',list(reversed(samples)))]:
 a=bpy.data.actions.new(name);a.use_fake_user=True;rig.animation_data.action=a;made.append(a)
 for frame,sample in enumerate(frames,1):
  desired={}
  for p in rig.pose.bones:
   rest=p.bone.matrix_local;matrix=desired[p.parent.name]@p.parent.bone.matrix_local.inverted()@rest if p.parent else rest.copy();rotation=rest.to_quaternion();driver=mapping.get(p.name)
   if driver:rotation=(sample[driver]@rests[driver].inverted())@rotation
   matrix=Matrix.LocRotScale(matrix.translation,rotation,Vector((1,1,1)));desired[p.name]=matrix
   kwargs={'parent_matrix':desired[p.parent.name],'parent_matrix_local':p.parent.bone.matrix_local} if p.parent else {}
   p.rotation_mode='QUATERNION';p.matrix_basis=p.bone.convert_local_to_pose(matrix,rest,invert=True,**kwargs)
   p.keyframe_insert('location',frame=frame,group=p.name);p.keyframe_insert('rotation_quaternion',frame=frame,group=p.name);p.keyframe_insert('scale',frame=frame,group=p.name)
rig.animation_data.action=made[0];scene.frame_start=1;scene.frame_end=len(samples);scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);root=rig.parent
if root:root.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(OUT/'SM_ResidentDoorMotion.fbx'),use_selection=True,object_types={'ARMATURE','EMPTY'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0)
bpy.ops.wm.save_as_mainfile(filepath=str(ART/'ResidentDoorMotion.blend'))
(ART/'report.json').write_text(json.dumps({'source':'Opening Door Inwards.fbx','frames':len(samples),'fps':30,'closing':'reversed opening motion','rigBones':len(rig.data.bones),'rootMotion':False,'modelAndPalette':'unchanged original caretaker','handIK':False},indent=2))
print('DOOR_MOTION_READY',len(samples))
