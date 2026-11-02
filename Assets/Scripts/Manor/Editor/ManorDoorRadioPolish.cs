using System.Linq;
using Echo.Interactions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Echo.Manor.Editor
{
    public static class ManorDoorRadioPolish
    {
        public const string Folder="Assets/Props/ResidentDoorMotion/";
        public const string Marker="Resident passage anchors";
        [MenuItem("Echo/Радіо, ромашки та двері мешканця")]
        public static void ApplyAndSave(){Apply();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();}
        public static void Apply()
        {
            if(EditorSceneManager.GetActiveScene().path!=ManorPrototypeBuilder.ScenePath)throw new System.InvalidOperationException("Open ManorPrototype first");
            ConfigureMotion();
            var root=GameObject.Find(Marker)??new GameObject(Marker);
            foreach(var name in new[]{"Hall Code Door","Kitchen Shortcut Door","Jammed Garden Door","Study Door","Under-stair door"})
            {
                var go=GameObject.Find(name);if(go==null)continue;var door=go.GetComponent<HingedDoor>();if(door==null)continue;
                var panel=door.GetComponentsInChildren<BoxCollider>().FirstOrDefault(c=>c.name=="Door Panel"||c.name=="Door leaf collider"||c.name=="Cubby door collider");
                if(panel==null)panel=door.GetComponentsInChildren<BoxCollider>().First(c=>!c.isTrigger);
                var local=door.hinge.InverseTransformPoint(panel.bounds.center);float width=Mathf.Abs(local.x)*2;float height=panel.bounds.size.y;
                if(width<.5f)width=panel.bounds.size.x;var anchor=root.transform.Find(name+" portal");
                if(anchor==null){anchor=new GameObject(name+" portal").transform;anchor.SetParent(root.transform,false);}
                anchor.SetPositionAndRotation(new Vector3(panel.bounds.center.x,door.hinge.position.y,panel.bounds.center.z),door.hinge.rotation);
                var portal=anchor.GetComponent<ResidentDoorway>();if(portal==null)portal=anchor.gameObject.AddComponent<ResidentDoorway>();portal.door=door;portal.width=width;portal.height=height;portal.hingePosition=door.hinge.position;portal.closedRotation=door.hinge.rotation;
                door.residentPassage=true;EditorUtility.SetDirty(door);EditorUtility.SetDirty(portal);
            }
            foreach(var enemy in Object.FindObjectsByType<EnemyInvestigator>(FindObjectsInactive.Include))
            {var traversal=enemy.GetComponent<EnemyDoorTraversal>();if(traversal==null)traversal=enemy.gameObject.AddComponent<EnemyDoorTraversal>();traversal.approachDistance=1.5f;traversal.animationSeconds=2;EditorUtility.SetDirty(traversal);}
            foreach(var t in Object.FindObjectsByType<Transform>().Where(t=>(t.name.StartsWith("Mixed flower ")&&t.name.EndsWith(" Daisy"))||t.name.StartsWith("Gatherable daisy ")).ToArray())
            {
                var plant=t.GetComponent<PlantGathering>();if(plant==null)plant=t.gameObject.AddComponent<PlantGathering>();plant.speciesId=plant.requiredSpecies="daisy";plant.itemId="remedy.flower";plant.pickedFlag="garden.flower."+t.name.Replace(" ","-");plant.displayName="ромашка";plant.pickupPrompt="E — зірвати ромашку";plant.description="Білі пелюстки й жовта серединка.";
                var bounds=t.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});var collider=t.GetComponent<BoxCollider>();if(collider==null)collider=t.gameObject.AddComponent<BoxCollider>();collider.center=t.InverseTransformPoint(bounds.center);collider.size=new Vector3(.34f/t.lossyScale.x,Mathf.Max(.5f,bounds.size.y)/t.lossyScale.y,.34f/t.lossyScale.z);
                EditorUtility.SetDirty(plant);EditorUtility.SetDirty(collider);
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
        static void ConfigureMotion()
        {
            string path=Folder+"SM_ResidentDoorMotion.fbx";var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.importAnimation=true;importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CopyFromOther;importer.sourceAvatar=AssetDatabase.LoadAllAssetsAtPath(ManorResidentModel.Folder+"SM_CaretakerResident.fbx").OfType<Avatar>().First();
            var specs=importer.defaultClipAnimations;foreach(var c in specs){c.name=c.name.Contains("Close")?"ResidentDoorClose":"ResidentDoorOpen";c.loopTime=false;c.loopPose=false;c.events=System.Array.Empty<AnimationEvent>();}importer.clipAnimations=specs;importer.SaveAndReimport();
            var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();if(clips.Length!=2)throw new System.Exception("Expected two door motions");
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(ManorResidentModel.Folder+"Resident.controller");var machine=controller.layers[0].stateMachine;var idle=machine.states.Single(s=>s.state.name=="Idle").state;
            foreach(var c in clips)
            {
                string name=c.name.Contains("Close")?"DoorClose":"DoorOpen";var state=machine.states.FirstOrDefault(s=>s.state.name==name).state??machine.AddState(name);state.motion=c;state.speed=c.length/2f;
                if(state.transitions.Length==0){var exit=state.AddTransition(idle);exit.hasExitTime=true;exit.exitTime=1;exit.duration=.12f;}
            }
            EditorUtility.SetDirty(controller);
        }
    }
}
