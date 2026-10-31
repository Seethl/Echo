using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
public static class ManorStorageSetup
{
 const string Folder="Assets/Props/ManorStorage";
 [MenuItem("Tools/Echo/Build Manor Storage")]
 public static void Build()
 {
  AssetDatabase.Refresh();string tp=Folder+"/ManorStorage_Wood.png";
  var ti=(TextureImporter)AssetImporter.GetAtPath(tp);ti.sRGBTexture=true;ti.mipmapEnabled=true;ti.maxTextureSize=2048;ti.SaveAndReimport();
  var shader=Shader.Find("Universal Render Pipeline/Lit");if(shader==null)throw new Exception("URP missing");
  string[] names={"M_ManorStorage_Wood","M_ManorStorage_Brass","M_ManorBooks_Burgundy","M_ManorBooks_Olive","M_ManorBooks_Blue","M_ManorBooks_Brown","M_ManorBooks_Paper"};
  Color[] colors={Color.white,new Color(.34f,.23f,.08f),new Color(.17f,.035f,.045f),new Color(.095f,.12f,.065f),new Color(.045f,.075f,.10f),new Color(.19f,.10f,.045f),new Color(.40f,.34f,.23f)};
  Material[] mats=new Material[names.Length];
  for(int i=0;i<names.Length;i++)
  {
   string mp=Folder+"/"+names[i]+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(mp);if(m==null){m=new Material(shader);AssetDatabase.CreateAsset(m,mp);}
   m.shader=shader;m.SetTexture("_BaseMap",i==0?AssetDatabase.LoadAssetAtPath<Texture2D>(tp):null);m.SetColor("_BaseColor",colors[i]);m.SetFloat("_Metallic",i==1?.8f:0);m.SetFloat("_Smoothness",i==1?.5f:.2f);m.enableInstancing=true;EditorUtility.SetDirty(m);mats[i]=m;
  }
  Make("ManorWardrobe",1.92f,.54f,mats,names);Make("ManorBookcase",1.82f,.37f,mats,names);
  AssetDatabase.SaveAssets();File.WriteAllText(Folder+"/Validation.txt","PASS: two prefab models, materials, compound colliders.\nPASS: floor origins and heights.\nPASS: both leaf pivots rotate independently of the body, then restore closed.\nDoors are prepared for animation; no E interaction attached.\n");Debug.Log("MANOR_STORAGE_UNITY_OK");
 }
 static void Make(string name,float h,float d,Material[] mats,string[] names)
 {
  string fp=Folder+"/SM_"+name+".fbx";var mi=(ModelImporter)AssetImporter.GetAtPath(fp);mi.importAnimation=false;mi.importLights=false;mi.importCameras=false;mi.globalScale=1;mi.generateSecondaryUV=true;mi.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
  for(int i=0;i<names.Length;i++)mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),names[i]),mats[i]);mi.SaveAndReimport();
  var model=AssetDatabase.LoadAssetAtPath<GameObject>(fp);var root=new GameObject("PF_"+name);
  try
  {
   var visual=(GameObject)PrefabUtility.InstantiatePrefab(model);visual.transform.SetParent(root.transform,false);
   var body=Find(visual.transform,"CabinetBody");Bounds before=body.GetComponent<Renderer>().bounds;
   if(Mathf.Abs(before.min.y)>.001f||Mathf.Abs(before.max.y-h)>.001f)throw new Exception("Wrong cabinet height/origin");
   foreach(float x in new[]{-.49f,.49f})Box(root,"Side",new Vector3(x,h/2,0),new Vector3(.065f,h-.16f,d));
   Box(root,"Back",new Vector3(0,h/2,-d/2+.015f),new Vector3(.95f,h-.18f,.03f));
   Box(root,"Base",new Vector3(0,.10f,0),new Vector3(1.10f,.20f,d+.065f));
   Box(root,"Crown",new Vector3(0,h-.06f,0),new Vector3(1.15f,.12f,d+.095f));
   foreach(float y in name=="ManorWardrobe"?new[]{.58f,1f,1.42f}:new[]{.64f,1.02f,1.40f})Box(root,"Shelf",new Vector3(0,y,.005f),new Vector3(.96f,.045f,d-.04f));
   foreach(string side in new[]{"Left","Right"})
   {
    var hinge=Find(visual.transform,"DoorHinge_"+side);var leaf=Find(hinge,"DoorLeaf_"+side);
    var c=leaf.gameObject.AddComponent<BoxCollider>();var mesh=leaf.GetComponent<MeshFilter>().sharedMesh;c.center=mesh.bounds.center;c.size=mesh.bounds.size;
    Vector3 closedCentre=leaf.GetComponent<Renderer>().bounds.center;Quaternion closed=hinge.localRotation;
    hinge.localRotation=closed*Quaternion.Euler(0,side=="Left"?110:-110,0);
    if(Vector3.Distance(closedCentre,leaf.GetComponent<Renderer>().bounds.center)<.2f)throw new Exception("Door did not move around edge pivot");
    if(Mathf.Abs(closedCentre.y-leaf.GetComponent<Renderer>().bounds.center.y)>.001f)throw new Exception("Hinge axis is not vertical after FBX import");
    if(body.GetComponent<Renderer>().bounds!=before)throw new Exception("Body moved with leaf");
    hinge.localRotation=closed;
   }
   if(name=="ManorBookcase")
   {
    var books=Find(visual.transform,"Books");var c=books.gameObject.AddComponent<BoxCollider>();var b=books.GetComponent<MeshFilter>().sharedMesh.bounds;c.center=b.center;c.size=b.size;
   }
   PrefabUtility.SaveAsPrefabAsset(root,Folder+"/PF_"+name+".prefab");
  }
  finally{Object.DestroyImmediate(root);}
 }
 static Transform Find(Transform root,string prefix)
 {foreach(var t in root.GetComponentsInChildren<Transform>())if(t.name.StartsWith(prefix,StringComparison.Ordinal))return t;throw new Exception("Missing node "+prefix);}
 static void Box(GameObject root,string name,Vector3 center,Vector3 size)
 {var o=new GameObject("Collision_"+name);o.transform.SetParent(root.transform,false);var c=o.AddComponent<BoxCollider>();c.center=center;c.size=size;}
}
