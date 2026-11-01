using System;
using System.Linq;
using Echo.Interactions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Echo.Manor.Editor
{
    public static class ManorMechanismModels
    {
        public const string Folder="Assets/Props/ManorMechanisms";
        [Serializable] private class Spec{public string name;public float[] color;public float metallic,smoothness;}
        [Serializable] private class Specs{public Spec[] items;}
        [MenuItem("Echo/Оновити моделі лома та лебідки")]
        public static void ApplyAndSave()
        {
            if(EditorSceneManager.GetActiveScene().path!=ManorPrototypeBuilder.ScenePath)throw new InvalidOperationException("Open ManorPrototype first.");
            Apply();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
        public static void Apply()
        {
            var specs=JsonUtility.FromJson<Specs>("{\"items\":"+System.IO.File.ReadAllText(Folder+"/materials.json")+"}").items;
            foreach(var spec in specs)
            {
                string path=Folder+"/"+spec.name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
                mat.SetColor("_BaseColor",new Color(spec.color[0],spec.color[1],spec.color[2]));mat.SetFloat("_Metallic",spec.metallic);mat.SetFloat("_Smoothness",spec.smoothness);EditorUtility.SetDirty(mat);
            }
            foreach(string name in new[]{"Crowbar","ManualWinch"})
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath(Folder+"/SM_"+name+".fbx");
                importer.isReadable=true;importer.importAnimation=false;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
                foreach(var source in AssetDatabase.LoadAllAssetsAtPath(Folder+"/SM_"+name+".fbx").OfType<Material>())importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),source.name),AssetDatabase.LoadAssetAtPath<Material>(Folder+"/"+source.name+".mat"));
                importer.SaveAndReimport();
                string prefabPath=Folder+"/PF_"+name+".prefab";
                var temp=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/SM_"+name+".fbx"));
                PrefabUtility.SaveAsPrefabAsset(temp,prefabPath);Object.DestroyImmediate(temp);
            }
            var crow=GameObject.Find("Crowbar");HideOld(crow);var crowModel=Instance(crow,"Crowbar");
            var bounds=Bounds(crowModel);var bench=GameObject.Find("Garden Tool Bench").GetComponent<Collider>().bounds;
            crowModel.transform.position+=new Vector3(26-bounds.center.x,bench.max.y+.004f-bounds.min.y,16.5f-bounds.center.z);
            MatchCollider(crow,Bounds(crowModel),new Vector3(.015f,.035f,.015f));
            var winch=GameObject.Find("Manual Winch");HideOld(winch);
            foreach(var r in Object.FindObjectsByType<Renderer>().Where(r=>r.name=="Manual winch wheel"||r.name=="Winch spoke"||r.name=="Empty dark socket"||r.name=="Installed handle"))r.enabled=false;
            var model=Instance(winch,"ManualWinch");model.transform.rotation=Quaternion.Euler(0,180,0);bounds=Bounds(model);model.transform.position+=new Vector3(33-bounds.center.x,-bounds.min.y,12-bounds.center.z);
            MatchCollider(winch,Bounds(model),Vector3.zero);
            var cable=winch.transform.Find("Cable to overhead hoist");
            if(cable==null){var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name="Cable to overhead hoist";Object.DestroyImmediate(go.GetComponent<Collider>());go.transform.SetParent(winch.transform,true);cable=go.transform;}
            Vector3 a=new Vector3(33,1.10f,12.2f),b=new Vector3(33.6f,1.35f,13.68f);cable.SetParent(null,true);cable.position=(a+b)/2;cable.up=(b-a).normalized;cable.localScale=new Vector3(.025f,(b-a).magnitude/2,.025f);cable.SetParent(winch.transform,true);
            cable.GetComponent<Renderer>().enabled=true;cable.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Mechanism_Rope.mat");
            var presentation=winch.GetComponent<WinchPresentation>()??winch.AddComponent<WinchPresentation>();presentation.mechanism=winch.GetComponent<WinchMechanism>();
            presentation.crank=model.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="CrankPivot");
            foreach(var readout in Object.FindObjectsByType<MechanismReadout>().Where(r=>r.winch==presentation.mechanism))readout.handleVisual=presentation.crank;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
        static Bounds Bounds(GameObject go)=>go.GetComponentsInChildren<Renderer>(true).Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
        static void HideOld(GameObject go)
        {var model=go.transform.Find("Blender model");foreach(var r in go.GetComponentsInChildren<Renderer>(true))if(model==null||!r.transform.IsChildOf(model))r.enabled=false;}
        static GameObject Instance(GameObject owner,string name)
        {
            var old=owner.transform.Find("Blender model");var go=old!=null?old.gameObject:(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/PF_"+name+".prefab"));
            go.name="Blender model";go.transform.SetParent(null,true);go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);go.transform.localScale=Vector3.one;go.transform.SetParent(owner.transform,true);return go;
        }
        static void MatchCollider(GameObject owner,Bounds b,Vector3 padding)
        {
            var box=owner.GetComponent<BoxCollider>();box.center=owner.transform.InverseTransformPoint(b.center);var s=owner.transform.lossyScale;box.size=new Vector3((b.size.x+padding.x)/s.x,(b.size.y+padding.y)/s.y,(b.size.z+padding.z)/s.z);
        }
    }
}
