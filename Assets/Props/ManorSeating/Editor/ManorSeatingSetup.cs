using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
public static class ManorSeatingSetup
{
 const string Folder="Assets/Props/ManorSeating";
 [MenuItem("Tools/Echo/Build Manor Seating")]
 public static void Build()
 {
  AssetDatabase.Refresh();string tp=Folder+"/ManorSeating_Wood.png";
  var ti=(TextureImporter)AssetImporter.GetAtPath(tp);ti.sRGBTexture=true;ti.mipmapEnabled=true;ti.maxTextureSize=2048;ti.SaveAndReimport();
  var shader=Shader.Find("Universal Render Pipeline/Lit");if(shader==null)throw new Exception("URP missing");
  string[] names={"M_ManorSeating_Wood","M_ManorSeating_Olive","M_ManorSeating_Burgundy"};
  Color[] colors={Color.white,new Color(.19f,.175f,.095f),new Color(.235f,.043f,.064f)};
  Material[] mats=new Material[3];
  for(int i=0;i<3;i++)
  {
   string mp=Folder+"/"+names[i]+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(mp);
   if(m==null){m=new Material(shader);AssetDatabase.CreateAsset(m,mp);}
   m.shader=shader;m.SetTexture("_BaseMap",i==0?AssetDatabase.LoadAssetAtPath<Texture2D>(tp):null);
   m.SetColor("_BaseColor",colors[i]);m.SetFloat("_Metallic",0);m.SetFloat("_Smoothness",i==0?.23f:.1f);m.enableInstancing=true;EditorUtility.SetDirty(m);mats[i]=m;
  }
  Make("ManorChair",mats,names,.47f);Make("ManorArmchair",mats,names,.86f);Make("ManorSofa",mats,names,1.91f);
  AssetDatabase.SaveAssets();File.WriteAllText(Folder+"/Validation.txt","PASS: three prefabs with URP materials and compound BoxColliders.\nPASS: metre scale, model widths and floor origin.\nStatic props: cushions and throw are part of each mesh, no sitting interaction.\n");Debug.Log("MANOR_SEATING_UNITY_OK");
 }
 static void Make(string name,Material[] mats,string[] names,float width)
 {
  string fp=Folder+"/SM_"+name+".fbx";var mi=(ModelImporter)AssetImporter.GetAtPath(fp);
  mi.globalScale=1;mi.importAnimation=false;mi.importLights=false;mi.importCameras=false;mi.generateSecondaryUV=true;
  mi.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
  for(int i=0;i<3;i++)mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),names[i]),mats[i]);
  mi.SaveAndReimport();var model=AssetDatabase.LoadAssetAtPath<GameObject>(fp);var root=new GameObject("PF_"+name);
  try
  {
   var child=(GameObject)PrefabUtility.InstantiatePrefab(model);child.transform.SetParent(root.transform,false);
   var b=root.GetComponentInChildren<Renderer>().bounds;
   if(Mathf.Abs(b.min.y)>.001f||Mathf.Abs(b.size.x-width)>.025f)throw new Exception("Wrong furniture dimensions "+name+": "+b);
   if(name=="ManorChair")
   {
    Box(root,"Seat",new Vector3(0,.435f,0),new Vector3(.47f,.12f,.48f));
    Box(root,"Back",new Vector3(0,.72f,-.20f),new Vector3(.415f,.60f,.06f));
    foreach(float x in new[]{-.185f,.185f})foreach(float z in new[]{-.185f,.185f})Box(root,"Leg",new Vector3(x,.20f,z),new Vector3(.047f,.4f,.047f));
   }
   else
   {
    float w=name=="ManorSofa"?1.9f:.85f;
    Box(root,"Seat",new Vector3(0,.33f,0),new Vector3(w,.36f,.79f));
    Box(root,"Back",new Vector3(0,.74f,-.285f),new Vector3(w-.16f,.58f,.19f));
    foreach(float x in new[]{-w/2+.065f,w/2-.065f})Box(root,"Arm",new Vector3(x,.57f,0),new Vector3(.14f,.31f,.75f));
    foreach(float x in new[]{-w/2+.105f,w/2-.105f})foreach(float z in new[]{-.285f,.285f})Box(root,"Foot",new Vector3(x,.085f,z),new Vector3(.075f,.17f,.08f));
   }
   PrefabUtility.SaveAsPrefabAsset(root,Folder+"/PF_"+name+".prefab");
  }
  finally{Object.DestroyImmediate(root);}
 }
 static void Box(GameObject root,string name,Vector3 center,Vector3 size)
 {var o=new GameObject("Collision_"+name);o.transform.SetParent(root.transform,false);var c=o.AddComponent<BoxCollider>();c.center=center;c.size=size;}
}
