using System;
using System.IO;
using Echo.Interactions;
using Echo.Manor;
using Echo.Radio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class InteractivePropsSetup
{
 public const string Folder="Assets/Props/ManorInteractive";
 public const string TestScene="Assets/Scenes/Testing/InteractiveProps/InteractivePropsTest.unity";
 [Serializable] class MatSpec{public string name;public float[] color;public float metallic,smoothness;public bool textured;}
 [Serializable] class Manifest{public MatSpec[] materials;}
 [MenuItem("Tools/Echo/Build Interactive Props")]
 public static void Build()
 {
  AssetDatabase.Refresh();var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Folder+"/MaterialManifest.json"));
  string texturePath=Folder+"/ManorInteractive_Wood.png";var ti=(TextureImporter)AssetImporter.GetAtPath(texturePath);ti.sRGBTexture=true;ti.mipmapEnabled=true;ti.maxTextureSize=2048;ti.SaveAndReimport();
  var shader=Shader.Find("Universal Render Pipeline/Lit");if(shader==null)throw new Exception("URP missing");
  var mats=new Material[manifest.materials.Length];
  for(int i=0;i<mats.Length;i++)
  {
   var s=manifest.materials[i];string mp=Folder+"/"+s.name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(mp);if(m==null){m=new Material(shader);AssetDatabase.CreateAsset(m,mp);}
   m.shader=shader;m.SetTexture("_BaseMap",s.textured?AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath):null);m.SetColor("_BaseColor",s.textured?Color.white:new Color(s.color[0],s.color[1],s.color[2]));m.SetFloat("_Metallic",s.metallic);m.SetFloat("_Smoothness",s.smoothness);EditorUtility.SetDirty(m);mats[i]=m;
  }
  foreach(string name in new[]{"ManorWindow","ManorBed","ManorRadio","ManorCombinationLock"})
  {
   string fp=Folder+"/SM_"+name+".fbx";var mi=(ModelImporter)AssetImporter.GetAtPath(fp);mi.globalScale=1;mi.importAnimation=false;mi.importCameras=false;mi.importLights=false;mi.generateSecondaryUV=true;mi.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
   foreach(var mat in mats)mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),mat.name),mat);mi.SaveAndReimport();
   var model=AssetDatabase.LoadAssetAtPath<GameObject>(fp);var root=new GameObject("PF_"+name);
   try
   {
    var child=(GameObject)PrefabUtility.InstantiatePrefab(model);child.transform.SetParent(root.transform,false);
    if(name=="ManorRadio")SetupRadio(root);
    else if(name=="ManorCombinationLock")SetupLock(root);
    else
    {
     if(name=="ManorBed")
     {
      Box(root,new Vector3(0,.29f,0),new Vector3(1.16f,.58f,2.1f));
      Box(root,new Vector3(0,.72f,-1),new Vector3(1.2f,.52f,.12f));
     }
     else Box(root,new Vector3(0,.76f,0),new Vector3(1.36f,1.52f,.24f));
    }
    PrefabUtility.SaveAsPrefabAsset(root,Folder+"/PF_"+name+".prefab");
    if(name=="ManorRadio"){root.GetComponent<PortableRadio>().allowPickup=false;root.name="PF_ManorRadio_Stationary";PrefabUtility.SaveAsPrefabAsset(root,Folder+"/PF_ManorRadio_Stationary.prefab");}
   }
   finally{Object.DestroyImmediate(root);}
  }
  AssetDatabase.SaveAssets();CreateTestScene();AssetDatabase.SaveAssets();Debug.Log("INTERACTIVE_PROPS_BUILD_OK");
 }
 static Transform Find(Transform root,string name)
 {foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name.StartsWith(name,StringComparison.Ordinal))return t;throw new Exception("Missing "+name);}
 static void Box(GameObject root,Vector3 center,Vector3 size)
 {var o=new GameObject("Collision");o.transform.SetParent(root.transform,false);var c=o.AddComponent<BoxCollider>();c.center=center;c.size=size;}
 static void Control(Transform t,PropCloseupView owner,int id,Vector3? size=null)
 {
  var c=t.gameObject.AddComponent<BoxCollider>();var bounds=t.GetComponent<MeshFilter>().sharedMesh.bounds;c.center=bounds.center;c.size=size??bounds.size;
  var hit=t.gameObject.AddComponent<PropPhysicalControl>();hit.owner=owner;hit.controlId=id;
 }
 static Transform Anchor(GameObject root,Vector3 position,Vector3 look)
 {var o=new GameObject("CloseupCameraAnchor");o.transform.SetParent(root.transform,false);o.transform.localPosition=position;o.transform.localRotation=Quaternion.LookRotation(look-position,Vector3.up);return o.transform;}
 static TextMesh Text(Transform parent,string name,string value,Vector3 pos,float height,Color color)
 {
  var o=new GameObject(name);o.transform.SetParent(parent,false);o.transform.localPosition=pos;o.transform.localRotation=Quaternion.Euler(0,180,0);
  var text=o.AddComponent<TextMesh>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
  string fontPath=Folder+"/M_Interactive_Font.mat";var fontMat=AssetDatabase.LoadAssetAtPath<Material>(fontPath);
  if(fontMat==null){var fontShader=Shader.Find("Echo/WorldPropText");if(fontShader==null)throw new Exception("Depth-tested font shader missing");fontMat=new Material(fontShader);AssetDatabase.CreateAsset(fontMat,fontPath);}
  text.GetComponent<Renderer>().sharedMaterial=fontMat;text.fontSize=64;text.characterSize=height/6;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=color;text.text=value;o.AddComponent<WorldPropTextBinding>().Refresh();return text;
 }
 static void SetupLock(GameObject root)
 {
  Box(root,new Vector3(0,.075f,-.003f),new Vector3(.19f,.13f,.055f));
  var keypad=root.AddComponent<KeypadLock>();keypad.code="417";keypad.useRunCode=false;
  var view=root.AddComponent<MechanicalLockView>();view.keypad=keypad;keypad.physicalView=view;
  view.shackle=Find(root.transform,"Shackle");
  view.cameraAnchor=Anchor(root,new Vector3(0,.11f,.37f),new Vector3(0,.085f,0));view.closeupFov=40;view.freezeWorld=true;
  for(int i=0;i<3;i++)
  {
   var rotor=Find(root.transform,"Drum"+i);view.drums[i]=rotor;Control(rotor,view,i);
   for(int digit=0;digit<10;digit++)
   {
    float a=digit*Mathf.PI/5;Vector3 normal=new Vector3(0,Mathf.Sin(a),Mathf.Cos(a));
    var label=Text(rotor,"Digit_"+digit,digit.ToString(),normal*.0295f,.015f,new Color(.82f,.77f,.60f));
    label.transform.localRotation=Quaternion.LookRotation(-normal,new Vector3(0,Mathf.Cos(a),-Mathf.Sin(a)));
   }
  }
  view.submitButton=Find(root.transform,"SubmitButton");Control(view.submitButton,view,3);
  Text(root.transform,"ConfirmLabel","OK",new Vector3(0,.018f,.055f),.009f,Color.white);
  view.statusText=Text(root.transform,"LockStatus","ЛКМ + · ПКМ − · ESC",new Vector3(0,-.018f,.04f),.009f,new Color(.8f,.75f,.58f));
  Text(root.transform,"SelectionLeft","—",new Vector3(-.102f,.065f,.048f),.012f,new Color(.8f,.75f,.58f));
  Text(root.transform,"SelectionRight","—",new Vector3(.102f,.065f,.048f),.012f,new Color(.8f,.75f,.58f));
 }
 static void SetupRadio(GameObject root)
 {
  Box(root,Vector3.zero,new Vector3(.48f,.30f,.18f));
  var radio=root.AddComponent<PortableRadio>();radio.heldLocalEulerAngles=new Vector3(0,180,0);radio.placementHalfExtents=new Vector3(.25f,.16f,.10f);
  var channels=new System.Collections.Generic.List<RadioChannel>();foreach(string guid in AssetDatabase.FindAssets("t:RadioChannel",new[]{"Assets/RadioData"}))channels.Add(AssetDatabase.LoadAssetAtPath<RadioChannel>(AssetDatabase.GUIDToAssetPath(guid)));radio.channels=channels.ToArray();
  var view=root.AddComponent<PhysicalRadioView>();view.radio=radio;radio.physicalView=view;view.freezeWorld=false;
  view.cameraAnchor=Anchor(root,new Vector3(0,.07f,.68f),Vector3.zero);view.closeupFov=40;
  view.tuningKnob=Find(root.transform,"TuneKnob");view.volumeKnob=Find(root.transform,"VolumeKnob");view.needle=Find(root.transform,"DialNeedle");
  Control(view.tuningKnob,view,0);Control(view.volumeKnob,view,1);Control(Find(root.transform,"PowerButton"),view,2);Control(Find(root.transform,"ReplayButton"),view,3);
  view.readout=Text(root.transform,"Readout","820 · OFF",new Vector3(-.108f,.055f,.124f),.017f,new Color(.15f,.06f,.015f));
  Text(root.transform,"TuneLabel","TUNE",new Vector3(-.055f,-.112f,.105f),.012f,Color.white);
  Text(root.transform,"VolumeLabel","VOL",new Vector3(-.165f,-.112f,.105f),.012f,Color.white);
  Text(root.transform,"PowerLabel","PWR",new Vector3(-.195f,.14f,.125f),.008f,Color.white);
  Text(root.transform,"ReplayLabel","RPT",new Vector3(-.10f,.14f,.125f),.008f,Color.white);
 }
 [MenuItem("Tools/Echo/Finish Radio Dial Markings")]
 public static void UpgradeRadioDials()
 {
  foreach(string name in new[]{"PF_ManorRadio","PF_ManorRadio_Stationary"})
  {
   string path=Folder+"/"+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
   try
   {
    var readout=root.GetComponent<PhysicalRadioView>().readout;
    readout.transform.localPosition=new Vector3(-.108f,.033f,.124f);readout.characterSize=.010f/6;
    var old=root.transform.Find("FrequencyScale");if(old!=null)Object.DestroyImmediate(old.gameObject);
    var scale=new GameObject("FrequencyScale");scale.transform.SetParent(root.transform,false);
    for(int i=0;i<=8;i++)
    {
     float x=Mathf.Lerp(-.033f,-.183f,i/8f);
     var mark=GameObject.CreatePrimitive(PrimitiveType.Cube);mark.name="Scale tick";Object.DestroyImmediate(mark.GetComponent<Collider>());
     mark.transform.SetParent(scale.transform,false);mark.transform.localPosition=new Vector3(x,.065f,.123f);mark.transform.localScale=new Vector3(.0009f,i%2==0?.009f:.005f,.0008f);
     mark.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/M_Interactive_Iron.mat");
     if(i%2==0)Text(scale.transform,"Frequency_"+i,(760+i*65).ToString(),new Vector3(x,.080f,.124f),.0055f,new Color(.15f,.06f,.015f));
    }
    PrefabUtility.SaveAsPrefabAsset(root,path);
   }
   finally{PrefabUtility.UnloadPrefabContents(root);}
  }
  AssetDatabase.SaveAssets();Debug.Log("RADIO_DIAL_MARKINGS_OK");
 }
 public static void CreateTestScene()
 {
  var old=SceneManager.GetActiveScene();
  bool replaceUntitled=Application.isBatchMode&&string.IsNullOrEmpty(old.path);
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,replaceUntitled?NewSceneMode.Single:NewSceneMode.Additive);
  if(replaceUntitled)old=default;
  SceneManager.SetActiveScene(scene);
  try
  {
   var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Test floor";floor.transform.localScale=new Vector3(10,.2f,10);floor.transform.position=new Vector3(0,-.1f,0);
   var actor=new GameObject("Player");actor.transform.position=new Vector3(0,.95f,-2);var controller=actor.AddComponent<CharacterController>();controller.height=1.8f;controller.radius=.3f;
   var camera=new GameObject("Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.SetParent(actor.transform,false);camera.transform.localPosition=Vector3.up*.65f;camera.gameObject.AddComponent<AudioListener>();
   var fps=actor.AddComponent<FPSController>();fps.playerCamera=camera.transform;
   var ui=new GameObject("Interaction UI").AddComponent<InteractionUI>();var player=actor.AddComponent<PlayerInteractor>();player.viewCamera=camera;player.ui=ui;
   var table=GameObject.CreatePrimitive(PrimitiveType.Cube);table.name="Test tabletop";table.transform.position=new Vector3(0,.8f,0);table.transform.localScale=new Vector3(2,.10f,.75f);
   Place("ManorCombinationLock",new Vector3(-.45f,.85f,-.1f),Quaternion.Euler(0,180,0));
   Place("ManorRadio",new Vector3(.4f,1.02f,0),Quaternion.Euler(0,180,0));
   Place("ManorBed",new Vector3(2,0,1),Quaternion.identity);Place("ManorWindow",new Vector3(-2,.6f,1.5f),Quaternion.identity);
   new GameObject("Light").AddComponent<Light>().type=LightType.Directional;RenderSettings.ambientLight=new Color(.4f,.4f,.4f);
   Directory.CreateDirectory(Path.GetDirectoryName(TestScene));EditorSceneManager.SaveScene(scene,TestScene);
  }
  finally{EditorSceneManager.CloseScene(scene,true);if(old.IsValid())SceneManager.SetActiveScene(old);}
 }
 static void Place(string name,Vector3 pos,Quaternion rot)
 {var o=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/PF_"+name+".prefab"));o.transform.SetPositionAndRotation(pos,rot);}
 public static void RenderPreviews()
 {
  if(!Application.isBatchMode)throw new Exception("Render previews in the isolated batch validation project.");
  UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=null;QualitySettings.renderPipeline=null;
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  Directory.CreateDirectory("work/interactive_props");
  var camera=new GameObject("Preview camera").AddComponent<Camera>();camera.backgroundColor=new Color(.08f,.09f,.10f);camera.clearFlags=CameraClearFlags.SolidColor;camera.orthographic=true;
  var light=new GameObject("Preview light").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(35,150,0);light.intensity=1.5f;RenderSettings.ambientLight=new Color(.45f,.45f,.45f);
  foreach(string name in new[]{"ManorCombinationLock","ManorRadio","ManorWindow_Shutters","ManorBed"})
  {
   var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/PF_"+name+".prefab"));
   // This isolated project has no URP renderer asset. Use equivalent Standard
   // preview materials in memory to verify geometry, text orientation and scale.
   foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
   {
    if(renderer.GetComponent<TextMesh>()!=null)continue;
    var source=renderer.sharedMaterials;var replacement=new Material[source.Length];
    for(int i=0;i<source.Length;i++)
    {var m=new Material(Shader.Find("Standard"));m.color=source[i].GetColor("_BaseColor");m.mainTexture=source[i].GetTexture("_BaseMap");m.SetFloat("_Metallic",source[i].GetFloat("_Metallic"));m.SetFloat("_Glossiness",source[i].GetFloat("_Smoothness"));replacement[i]=m;}
    renderer.sharedMaterials=replacement;
   }
   bool small=name=="ManorCombinationLock",window=name=="ManorWindow_Shutters",bed=name=="ManorBed";
   camera.orthographicSize=small?.145f:window?1.25f:bed?1.5f:.36f;
   foreach(var binding in root.GetComponentsInChildren<WorldPropTextBinding>())binding.Refresh();
   camera.transform.position=small?new Vector3(.025f,.12f,.5f):window?new Vector3(.7f,1.1f,3):bed?new Vector3(2.6f,2,3.3f):new Vector3(.05f,.17f,.9f);
   camera.transform.LookAt(small?new Vector3(0,.09f,0):window?new Vector3(0,.76f,0):bed?new Vector3(0,.45f,0):new Vector3(0,.15f,0));
   var rt=new RenderTexture(1100,1100,24);camera.targetTexture=rt;camera.Render();
   var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1100,1100,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1100,1100),0,0);image.Apply();
   File.WriteAllBytes("work/interactive_props/"+name+"_UnityPreview.png",image.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;Object.DestroyImmediate(image);Object.DestroyImmediate(rt);Object.DestroyImmediate(root);
  }
  Debug.Log("INTERACTIVE_VISUAL_PREVIEWS_OK");
 }
}
