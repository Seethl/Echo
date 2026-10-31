using System;
using System.IO;
using Echo.Environment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

public static class ManorLightsSetup
{
    const string Folder="Assets/Props/ManorLights";
    [Serializable] class Mat {public string name;public float[] color;public float metallic,smoothness;public bool emission;}
    [Serializable] class Item {public string name;public float[] light;public float range,intensity;public int triangles;}
    [Serializable] class Manifest {public Mat[] materials;public Item[] items;}
    [MenuItem("Tools/Echo/Build Manor Lights")]
    public static void Build()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
        AssetDatabase.Refresh();
        var data=JsonUtility.FromJson<Manifest>(File.ReadAllText(Folder+"/LightsManifest.json"));
        var shader=Shader.Find("Universal Render Pipeline/Lit");
        foreach(var spec in data.materials)
        {
            string path=Folder+"/"+spec.name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_BaseColor",new Color(spec.color[0],spec.color[1],spec.color[2]));
            material.SetFloat("_Metallic",spec.metallic);material.SetFloat("_Smoothness",spec.smoothness);
            if(spec.emission){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",new Color(2,.56f,.07f));material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;}
            EditorUtility.SetDirty(material);
        }
        foreach(var item in data.items)
        {
            string path=Folder+"/SM_"+item.name+".fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.globalScale=1;importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;importer.generateSecondaryUV=true;
            foreach(var spec in data.materials)importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),spec.name),AssetDatabase.LoadAssetAtPath<Material>(Folder+"/"+spec.name+".mat"));
            importer.SaveAndReimport();
            var root=new GameObject("PF_"+item.name);
            try
            {
                var mesh=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));mesh.transform.SetParent(root.transform,false);
                var renderer=root.GetComponentInChildren<Renderer>();var bounds=renderer.bounds;
                if(bounds.size.y<.3f||bounds.size.y>.7f)throw new Exception("Unexpected scale: "+item.name+" "+bounds);
                foreach(var material in renderer.sharedMaterials)if(material==null||material.shader!=shader)throw new Exception("Missing material: "+item.name);
                var collider=root.AddComponent<BoxCollider>();collider.center=bounds.center;collider.size=bounds.size;
                var lamp=new GameObject("Warm light").AddComponent<Light>();lamp.transform.SetParent(root.transform,false);
                lamp.transform.localPosition=new Vector3(item.light[0],item.light[1],item.light[2]);
                lamp.type=LightType.Point;lamp.color=new Color(1,.64f,.29f);lamp.intensity=item.intensity;lamp.range=item.range;
                lamp.shadows=LightShadows.None;lamp.lightmapBakeType=LightmapBakeType.Realtime;
                lamp.gameObject.AddComponent<WarmPropLight>();
                PrefabUtility.SaveAsPrefabAsset(root,Folder+"/PF_"+item.name+".prefab");
            }
            finally{Object.DestroyImmediate(root);}
        }
        AssetDatabase.SaveAssets();Debug.Log("MANOR_LIGHT_PREFABS_OK: 3 metre-scale models, URP materials, colliders and shadow-free warm lights");
    }
    public static void BuildAndPreview()
    {
        Build();
        if(!Application.isBatchMode)return;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var pipeline=GraphicsSettings.defaultRenderPipeline;var quality=QualitySettings.renderPipeline;
        try
        {
            GraphicsSettings.defaultRenderPipeline=null;QualitySettings.renderPipeline=null;
            var data=JsonUtility.FromJson<Manifest>(File.ReadAllText(Folder+"/LightsManifest.json"));
            for(int i=0;i<data.items.Length;i++)
            {
                var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/PF_"+data.items[i].name+".prefab"));
                model.transform.position=new Vector3((i-1)*.55f,0,0);
                foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>())
                {
                    var originals=renderer.sharedMaterials;var preview=new Material[originals.Length];
                    for(int n=0;n<preview.Length;n++)
                    {
                        var source=originals[n];var material=new Material(Shader.Find("Standard"));material.color=source.GetColor("_BaseColor");material.SetFloat("_Metallic",source.GetFloat("_Metallic"));material.SetFloat("_Glossiness",source.GetFloat("_Smoothness"));
                        if(source.IsKeywordEnabled("_EMISSION")){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",source.GetColor("_EmissionColor"));}
                        preview[n]=material;
                    }
                    renderer.sharedMaterials=preview;
                }
            }
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(0,-.035f,0);floor.transform.localScale=new Vector3(4,.06f,4);
            floor.GetComponent<Renderer>().sharedMaterial=new Material(Shader.Find("Standard")){color=new Color(.085f,.075f,.065f)};
            var light=new GameObject("Studio fill").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(35,150,0);
            RenderSettings.ambientLight=new Color(.28f,.29f,.32f);
            var camera=new GameObject("Preview camera").AddComponent<Camera>();camera.backgroundColor=new Color(.04f,.045f,.055f);camera.clearFlags=CameraClearFlags.SolidColor;
            camera.orthographic=true;camera.orthographicSize=.44f;camera.transform.position=new Vector3(.65f,.8f,1.9f);camera.transform.LookAt(new Vector3(0,.23f,0));
            var target=new RenderTexture(1600,900,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();
            Directory.CreateDirectory("work/manor_lights");File.WriteAllBytes("work/manor_lights/ManorLights_Preview.png",image.EncodeToPNG());
            RenderTexture.active=null;camera.targetTexture=null;Object.DestroyImmediate(image);Object.DestroyImmediate(target);
            Debug.Log("MANOR_LIGHT_PREVIEW_OK");
        }
        finally{GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=quality;}
    }
}
