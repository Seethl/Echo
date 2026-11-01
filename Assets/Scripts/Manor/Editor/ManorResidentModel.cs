using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Echo.Manor.Editor
{
    public static class ManorResidentModel
    {
        public const string Folder="Assets/Props/CaretakerResident/";
        [Serializable] class Spec{public string name;public float[] color;public float roughness;}
        [Serializable] class Specs{public Spec[] items;}
        [MenuItem("Echo/Модель та анімації мешканця")]
        public static void ApplyAndSave()
        {
            if(EditorSceneManager.GetActiveScene().path!=ManorPrototypeBuilder.ScenePath)throw new InvalidOperationException("Open ManorPrototype first.");
            Apply();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
        public static void Apply()
        {
            foreach(var spec in JsonUtility.FromJson<Specs>(System.IO.File.ReadAllText(Folder+"materials.json")).items)
            {
                var path=Folder+spec.name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
                material.SetColor("_BaseColor",new Color(spec.color[0],spec.color[1],spec.color[2]).gamma);material.SetFloat("_Metallic",spec.name.Contains("buttons")?.35f:0);material.SetFloat("_Smoothness",1-spec.roughness);EditorUtility.SetDirty(material);
            }
            string modelPath=Folder+"SM_CaretakerResident.fbx";var importer=(ModelImporter)AssetImporter.GetAtPath(modelPath);
            importer.isReadable=true;importer.importAnimation=true;importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
            foreach(var material in AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Material>())importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),material.name),AssetDatabase.LoadAssetAtPath<Material>(Folder+material.name+".mat"));
            var clips=importer.defaultClipAnimations;
            foreach(var clip in clips)
            {
                string name=new[]{"ResidentIdle","ResidentWalk","ResidentDrink","ResidentAttack"}.Single(n=>clip.name.Contains(n));clip.name=name;
                clip.loopTime=name=="ResidentIdle"||name=="ResidentWalk";clip.loopPose=clip.loopTime;
                clip.events=name=="ResidentWalk"?new[]{new AnimationEvent{functionName="Footstep",time=.12f},new AnimationEvent{functionName="Footstep",time=.82f}}:Array.Empty<AnimationEvent>();
            }
            if(clips.Length!=4)throw new InvalidOperationException("Expected four resident clips: "+string.Join(",",clips.Select(c=>c.name)));
            importer.clipAnimations=clips;importer.SaveAndReimport();
            var controller=Controller();
            var temp=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath));temp.name="Caretaker resident visual";
            var animator=temp.GetComponent<Animator>();if(animator==null)animator=temp.AddComponent<Animator>();
            animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            if(animator.avatar==null||!animator.avatar.isValid)throw new InvalidOperationException("Invalid resident generic avatar");
            temp.AddComponent<EnemyFootstepRelay>();
            AddCup(temp);
            foreach(var renderer in temp.GetComponentsInChildren<SkinnedMeshRenderer>()){renderer.updateWhenOffscreen=true;renderer.quality=SkinQuality.Bone4;}
            string prefabPath=Folder+"PF_CaretakerResident.prefab";PrefabUtility.SaveAsPrefabAsset(temp,prefabPath);Object.DestroyImmediate(temp);
            var enemy=Object.FindAnyObjectByType<EnemyInvestigator>(FindObjectsInactive.Include);
            var instances=enemy.transform.Cast<Transform>().Where(t=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject)==prefabPath).ToArray();
            var current=enemy.transform.Find("Caretaker resident visual")??instances.FirstOrDefault();
            foreach(var duplicate in instances)if(duplicate!=current)Object.DestroyImmediate(duplicate.gameObject);
            if(current!=null)current.name="Caretaker resident visual";
            foreach(var renderer in enemy.GetComponentsInChildren<Renderer>(true))if(current==null||!renderer.transform.IsChildOf(current))renderer.enabled=false;
            if(current==null){var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath),enemy.transform);visual.name="Caretaker resident visual";current=visual.transform;current.localPosition=Vector3.zero;current.localRotation=Quaternion.identity;current.localScale=Vector3.one;}
            var presentation=enemy.GetComponent<EnemyPresentation>()??enemy.gameObject.AddComponent<EnemyPresentation>();presentation.drinkingProp=AddCup(current.gameObject);presentation.animator=current.GetComponent<Animator>();presentation.disableRootMotion=true;presentation.useAnimationFootsteps=true;
            current.GetComponent<EnemyFootstepRelay>().enemy=enemy;enemy.GetComponent<EnemyCatchPlayer>().attackWindupSeconds=.65f;
            var delivery=Object.FindAnyObjectByType<RemedyDelivery>();delivery.drinkingSeconds=5f;
            EditorUtility.SetDirty(delivery);EditorUtility.SetDirty(presentation);EditorUtility.SetDirty(enemy.GetComponent<EnemyCatchPlayer>());
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
        static GameObject AddCup(GameObject visual)
        {
            var existing=visual.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Resident drinking mug");if(existing!=null)return existing.gameObject;
            var clips=AssetDatabase.LoadAllAssetsAtPath(Folder+"SM_CaretakerResident.fbx").OfType<AnimationClip>().ToArray();
            var drink=clips.Single(c=>c.name=="ResidentDrink");var idle=clips.Single(c=>c.name=="ResidentIdle");drink.SampleAnimation(visual,drink.length*.48f);
            var head=visual.GetComponentsInChildren<Transform>().Single(t=>t.name=="head");
            var hand=visual.GetComponentsInChildren<Transform>().Where(t=>t.name=="hand.L"||t.name=="hand.R").OrderBy(t=>Vector3.Distance(t.position,head.position)).First();
            var mug=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Props/ManorDecor/PF_DecorMug.prefab"));mug.name="Resident drinking mug";
            foreach(var collider in mug.GetComponentsInChildren<Collider>())Object.DestroyImmediate(collider);
            var bounds=mug.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
            mug.transform.localScale=Vector3.Scale(mug.transform.localScale,new Vector3(.115f/bounds.size.x,.13f/bounds.size.y,.10f/bounds.size.z));
            bounds=mug.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});mug.transform.position+=hand.position+Vector3.up*.035f-bounds.center;
            mug.transform.SetParent(hand,true);idle.SampleAnimation(visual,0);mug.SetActive(false);return mug;
        }
        static AnimatorController Controller()
        {
            string path=Folder+"Resident.controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if(controller!=null)
            {
                var available=AssetDatabase.LoadAllAssetsAtPath(Folder+"SM_CaretakerResident.fbx").OfType<AnimationClip>().ToArray();
                foreach(var child in controller.layers[0].stateMachine.states)child.state.motion=available.Single(c=>c.name=="Resident"+child.state.name);
                EditorUtility.SetDirty(controller);return controller;
            }
            controller=AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Speed",AnimatorControllerParameterType.Float);controller.AddParameter("Suspicion",AnimatorControllerParameterType.Float);controller.AddParameter("State",AnimatorControllerParameterType.Int);
            foreach(string name in new[]{"IsChasing","IsSearching","IsAttacking","IsDrinking"})controller.AddParameter(name,AnimatorControllerParameterType.Bool);
            var clips=AssetDatabase.LoadAllAssetsAtPath(Folder+"SM_CaretakerResident.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
            var machine=controller.layers[0].stateMachine;
            AnimatorState State(string name,string clip){var state=machine.AddState(name);state.motion=clips.Single(c=>c.name==clip);return state;}
            var idle=State("Idle","ResidentIdle");var walk=State("Walk","ResidentWalk");var attack=State("Attack","ResidentAttack");var drink=State("Drink","ResidentDrink");machine.defaultState=idle;
            walk.speed=2.4f;walk.speedParameterActive=true;walk.speedParameter="Speed";attack.speed=1.4f;drink.speed=1.8f;
            var move=idle.AddTransition(walk);move.hasExitTime=false;move.duration=.15f;move.AddCondition(AnimatorConditionMode.Greater,.04f,"Speed");move.AddCondition(AnimatorConditionMode.IfNot,0,"IsDrinking");move.AddCondition(AnimatorConditionMode.IfNot,0,"IsAttacking");
            var stop=walk.AddTransition(idle);stop.hasExitTime=false;stop.duration=.15f;stop.AddCondition(AnimatorConditionMode.Less,.025f,"Speed");
            foreach(var pair in new[]{(state:attack,parameter:"IsAttacking"),(state:drink,parameter:"IsDrinking")})
            {
                var enter=machine.AddAnyStateTransition(pair.state);enter.hasExitTime=false;enter.duration=.12f;enter.canTransitionToSelf=false;enter.AddCondition(AnimatorConditionMode.If,0,pair.parameter);
                var exit=pair.state.AddTransition(idle);exit.hasExitTime=false;exit.duration=.12f;exit.AddCondition(AnimatorConditionMode.IfNot,0,pair.parameter);
            }
            EditorUtility.SetDirty(controller);return controller;
        }
    }
}
