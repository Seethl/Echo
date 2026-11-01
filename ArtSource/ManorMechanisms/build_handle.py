import bpy, math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ManorMechanisms.blend'))
source=bpy.data.objects['CrankPivot']
root=bpy.data.objects.new('LooseWinchHandle_34cm',None);bpy.context.collection.objects.link(root)
for ob in source.children:
    copy=ob.copy();copy.data=ob.data.copy();bpy.context.collection.objects.link(copy);copy.parent=root;copy.matrix_basis=ob.matrix_basis.copy()
def rod(name,a,b,r,material,n=8):
    a,b=Vector(a),Vector(b);bpy.ops.mesh.primitive_cylinder_add(vertices=n,radius=r,depth=(b-a).length,location=(a+b)/2)
    ob=bpy.context.object;ob.name=name;ob.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();ob.parent=root;ob.data.materials.append(bpy.data.materials[material])
rod('Hexagonal mounting socket',(0,.025,0),(0,-.045,0),.047,'Mechanism_Brass',6)
rod('Steel axle collar',(0,.035,0),(0,.05,0),.028,'Mechanism_Edge',8)
rod('Grip end cap',(.28,-.14,0),(.28,-.155,0),.042,'Mechanism_Iron',10)
# Remove all other models from this independent source file, preserving only the loose crank.
keep={root,*root.children_recursive}
for ob in list(bpy.data.objects):
    if ob not in keep:bpy.data.objects.remove(ob,do_unlink=True)
bpy.ops.object.select_all(action='SELECT');bpy.context.view_layer.objects.active=root
out=ROOT.parents[1]/'Assets/Props/ManorMechanisms';out.mkdir(parents=True,exist_ok=True)
bpy.ops.export_scene.fbx(filepath=str(out/'SM_WinchHandle.fbx'),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',bake_space_transform=True,apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'WinchHandle.blend'))
print('LOOSE_WINCH_HANDLE_EXPORTED')
