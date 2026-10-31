using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
public static class ManorDecorSetup
{
 const string Folder="Assets/Props/ManorDecor";
 [Serializable] class MaterialSpec {public string name;public float[] color;public float metallic;public float smoothness;public bool textured;}
 [Serializable] class Item {public string name;public int triangles;public float[] size;}
 [Serializable] class Manifest {public MaterialSpec[] materials;public Item[] items;}
 [MenuItem("Tools/Echo/Build Manor Decor")]
 public static void Build()
 {
  AssetDatabase.Refresh();var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Folder+"/DecorManifest.json"));
  string tp=Folder+"/ManorDecor_Wood.png";var ti=(TextureImporter)AssetImporter.GetAtPath(tp);
  ti.sRGBTexture=true;ti.mipmapEnabled=true;ti.maxTextureSize=2048;ti.SaveAndReimport();
  var shader=Shader.Find("Universal Render Pipeline/Lit");if(shader==null)throw new Exception("URP Lit missing");
  var materials=new Material[manifest.materials.Length];
  for(int i=0;i<materials.Length;i++)
  {
   var spec=manifest.materials[i];string mp=Folder+"/"+spec.name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(mp);
   if(mat==null){mat=new Material(shader);AssetDatabase.CreateAsset(mat,mp);}
   mat.shader=shader;mat.SetTexture("_BaseMap",spec.textured?AssetDatabase.LoadAssetAtPath<Texture2D>(tp):null);
   mat.SetColor("_BaseColor",spec.textured?Color.white:new Color(spec.color[0],spec.color[1],spec.color[2]));
   mat.SetFloat("_Metallic",spec.metallic);mat.SetFloat("_Smoothness",spec.smoothness);mat.enableInstancing=true;EditorUtility.SetDirty(mat);materials[i]=mat;
  }
  foreach(var item in manifest.items)
  {
   string fp=Folder+"/SM_"+item.name+".fbx";var mi=(ModelImporter)AssetImporter.GetAtPath(fp);
   mi.importAnimation=false;mi.importCameras=false;mi.importLights=false;mi.globalScale=1;mi.generateSecondaryUV=true;mi.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
   foreach(var mat in materials)mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),mat.name),mat);
   mi.SaveAndReimport();var model=AssetDatabase.LoadAssetAtPath<GameObject>(fp);var root=new GameObject("PF_"+item.name);
   try
   {
    var child=(GameObject)PrefabUtility.InstantiatePrefab(model);child.transform.SetParent(root.transform,false);
    var renderer=root.GetComponentInChildren<Renderer>();if(renderer==null)throw new Exception("Missing renderer "+item.name);
    var b=renderer.bounds;var expected=new Vector3(item.size[0],item.size[2],item.size[1]);
    if(Vector3.Distance(b.size,expected)>.002f||Mathf.Abs(b.min.y)>.001f)throw new Exception("Incorrect imported bounds "+item.name+": "+b);
    foreach(var mat in renderer.sharedMaterials)if(mat==null||mat.shader!=shader)throw new Exception("Unassigned material "+item.name);
    var collider=root.AddComponent<BoxCollider>();collider.center=root.transform.InverseTransformPoint(b.center);collider.size=new Vector3(Mathf.Max(b.size.x,.002f),Mathf.Max(b.size.y,.002f),Mathf.Max(b.size.z,.002f));
    PrefabUtility.SaveAsPrefabAsset(root,Folder+"/PF_"+item.name+".prefab");
   }
   finally{Object.DestroyImmediate(root);}
  }
  AssetDatabase.SaveAssets();File.WriteAllText(Folder+"/Validation.txt","PASS: "+manifest.items.Length+" prefab models imported.\nPASS: dimensions, floor origins, URP material assignments.\nEach prefab has a simple BoxCollider. Static decor; no Rigidbody or interaction.\n");Debug.Log("MANOR_DECOR_UNITY_OK");
 }
}
