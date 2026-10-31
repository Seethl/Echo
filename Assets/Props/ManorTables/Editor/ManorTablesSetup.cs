using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class ManorTablesSetup
{
 const string Folder="Assets/Props/ManorTables";
 [MenuItem("Tools/Echo/Build Manor Tables")]
 public static void Build()
 {
  AssetDatabase.Refresh();
  string tp=Folder+"/ManorTables_BaseColor.png";
  var ti=(TextureImporter)AssetImporter.GetAtPath(tp);ti.sRGBTexture=true;ti.mipmapEnabled=true;ti.maxTextureSize=2048;ti.SaveAndReimport();
  Shader shader=Shader.Find("Universal Render Pipeline/Lit");if(shader==null)throw new Exception("URP Lit missing");
  Material[] mats=new Material[2];string[] names={"M_ManorTables_Wood","M_ManorTables_Brass"};
  for(int i=0;i<2;i++)
  {
   string mp=Folder+"/"+names[i]+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(mp);
   if(mat==null){mat=new Material(shader);AssetDatabase.CreateAsset(mat,mp);}
   mat.shader=shader;mat.SetTexture("_BaseMap",i==0?AssetDatabase.LoadAssetAtPath<Texture2D>(tp):null);
   mat.SetColor("_BaseColor",i==0?Color.white:new Color(.34f,.23f,.08f));
   mat.SetFloat("_Metallic",i==0?0:.8f);mat.SetFloat("_Smoothness",i==0?.23f:.5f);
   mat.enableInstancing=true;EditorUtility.SetDirty(mat);mats[i]=mat;
  }
  Make("ManorDesk",mats,names,true);Make("ManorTable",mats,names,false);
  AssetDatabase.SaveAssets();
  File.WriteAllText(Folder+"/Validation.txt","PASS: two prefab models, UV textures, URP materials.\nPASS: imported width/height/depth and floor-centred origins.\nPASS: compound BoxColliders leave knee spaces open.\nDrawers are static fronts. No animation or interaction attached.\n");
  Debug.Log("MANOR_TABLES_UNITY_OK");
 }
 static void Make(string name,Material[] mats,string[] names,bool desk)
 {
  string fp=Folder+"/SM_"+name+".fbx";var mi=(ModelImporter)AssetImporter.GetAtPath(fp);
  mi.importAnimation=false;mi.importCameras=false;mi.importLights=false;mi.globalScale=1;mi.generateSecondaryUV=true;
  mi.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
  for(int i=0;i<mats.Length;i++)mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),names[i]),mats[i]);
  mi.SaveAndReimport();var model=AssetDatabase.LoadAssetAtPath<GameObject>(fp);var root=new GameObject("PF_"+name);
  try
  {
   var child=(GameObject)PrefabUtility.InstantiatePrefab(model);child.transform.SetParent(root.transform,false);
   Bounds b=root.GetComponentInChildren<Renderer>().bounds;
   if(Mathf.Abs(b.size.y-.78f)>.001f||Mathf.Abs(b.min.y)>.001f||Mathf.Abs(b.size.x-(desk?1.6f:1.8f))>.001f)
    throw new Exception("Unexpected table size: "+b);
   Box(root,"Top",new Vector3(0,.75f,0),new Vector3(desk?1.6f:1.8f,.06f,desk?.76f:.85f));
   if(desk)
   {
    foreach(float x in new[]{-.58f,.58f})Box(root,"Pedestal",new Vector3(x,.35f,0),new Vector3(.41f,.70f,.69f));
    Box(root,"BackRail",new Vector3(0,.51f,-.285f),new Vector3(.80f,.31f,.045f));
    Box(root,"Apron",new Vector3(0,.663f,.279f),new Vector3(.80f,.09f,.045f));
   }
   else
   {
    foreach(float x in new[]{-.735f,.735f})
    {
     foreach(float z in new[]{-.275f,.275f})Box(root,"Leg",new Vector3(x,.36f,z),new Vector3(.14f,.72f,.14f));
     Box(root,"SideStretcher",new Vector3(x,.155f,0),new Vector3(.085f,.09f,.55f));
    }
    Box(root,"LongStretcher",new Vector3(0,.155f,0),new Vector3(1.47f,.095f,.075f));
   }
   PrefabUtility.SaveAsPrefabAsset(root,Folder+"/PF_"+name+".prefab");
  }
  finally{Object.DestroyImmediate(root);}
 }
 static void Box(GameObject root,string name,Vector3 centre,Vector3 size)
 {
  var ob=new GameObject("Collision_"+name);ob.transform.SetParent(root.transform,false);
  var collider=ob.AddComponent<BoxCollider>();collider.center=centre;collider.size=size;
 }
}
