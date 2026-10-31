using System;
using System.IO;
using System.Reflection;
using Echo.Interactions;
using Echo.Manor;
using Echo.Radio;
using Echo.Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class InteractivePropsChecks
{
 const string Pending="Echo.InteractiveProps.Checks";
 static InteractivePropsChecks(){EditorApplication.update+=Poll;}
 static void Poll()
 {
  if(!Application.isBatchMode||!SessionState.GetBool(Pending,false)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  if(!EditorApplication.isPlaying){if(!EditorApplication.isPlayingOrWillChangePlaymode)EditorApplication.EnterPlaymode();return;}
  SessionState.SetBool(Pending,false);try{Run();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
 }
 public static void BuildAndRun(){InteractivePropsSetup.Build();Run();}
 [MenuItem("Tools/Echo/Checks/Interactive Props")]
 public static void Run()
 {
  if(!Application.isPlaying)
  {
   if(!Application.isBatchMode)throw new Exception("Run in Play Mode in an empty scene.");
   EditorSceneManager.playModeStartScene=null;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();return;
  }
  GameObject actor=null,lockRoot=null,radioRoot=null;string report="";
  try
  {
   var menu=Object.FindAnyObjectByType<GameMenu>();if(menu==null)menu=new GameObject("Test menu").AddComponent<GameMenu>();
   Set(menu,"inGameplay",true);Set(menu,"inMainMenu",false);Set(menu,"resumeFrame",-10);Set(menu,"loading",false);
   actor=new GameObject("Test player");var camera=new GameObject("Camera").AddComponent<Camera>();camera.transform.SetParent(actor.transform,false);camera.transform.position=new Vector3(0,.1f,.8f);camera.transform.rotation=Quaternion.Euler(0,180,0);camera.fieldOfView=60;
   var player=actor.AddComponent<PlayerInteractor>();player.viewCamera=camera;
   var ui=new GameObject("Test interaction UI").AddComponent<InteractionUI>();ui.transform.SetParent(actor.transform);player.ui=ui;
   lockRoot=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(InteractivePropsSetup.Folder+"/PF_ManorCombinationLock.prefab"));
   var view=lockRoot.GetComponent<MechanicalLockView>();var keypad=lockRoot.GetComponent<KeypadLock>();
   Vector3 position=camera.transform.localPosition;Quaternion rotation=camera.transform.localRotation;
   Assert(view.Open(player),"Lock opens via shared modal manager");Assert(Time.timeScale==0&&GameMenu.BlocksPlayerInput,"Lock inspection freezes world and movement");
   typeof(PropCloseupView).GetField("elapsed",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(view,.2f);
   typeof(PropCloseupView).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(view,null);
   Assert(Vector3.Distance(camera.transform.localPosition,position)>.01f,"Camera actually moves during closeup transition");
   Physics.SyncTransforms();
   for(int i=0;i<3;i++)
   {
    var target=view.drums[i].GetComponent<Renderer>().bounds.center;var ray=new Ray(view.cameraAnchor.position,(target-view.cameraAnchor.position).normalized);
    Assert(Physics.Raycast(ray,out var hit,1)&&hit.collider.GetComponent<PropPhysicalControl>()?.controlId==i,"Direct ray reaches drum "+i);
   }
   Quaternion before=view.drums[0].localRotation;
   view.ActivateControl(0,1);view.AdvanceAnimation(.06f);
   Assert(Quaternion.Angle(before,view.drums[0].localRotation)>1&&keypad.GetDigit(0)==0,"Drum rotates before committing digit");
   Assert(!view.RotateDrum(0,1),"Busy drum rejects another click");view.AdvanceAnimation(.3f);
   Assert(keypad.GetDigit(0)==1&&!view.IsTurning,"Digit commits at end of turn");
   view.ActivateControl(0,-1);view.AdvanceAnimation(.3f);Assert(keypad.GetDigit(0)==0,"Reverse turn works");
   view.ActivateControl(0,-1);view.AdvanceAnimation(.3f);Assert(keypad.GetDigit(0)==9,"Zero wraps to nine");
   view.ActivateControl(3,1);Assert(!keypad.IsUnlocked&&view.IsOpen,"Wrong code remains inspectable");
   view.Close();Assert(Time.timeScale==1&&!view.IsOpen,"Close restores time");
   Set(menu,"resumeFrame",-10);keypad.SetDigits(4,1,7);Assert(view.Open(player),"Lock reopens");view.ActivateControl(3,1);
   Assert(keypad.IsUnlocked&&view.ShowingSuccess,"417 starts opening feedback");view.AdvanceAnimation(1);
   Assert(!view.IsOpen&&camera.fieldOfView==60,"Opening feedback restores camera");
   Assert(Vector3.Distance(camera.transform.localPosition,position)<.001f&&Quaternion.Angle(camera.transform.localRotation,rotation)<.01f,"Camera pose restored");
   Object.DestroyImmediate(lockRoot);lockRoot=null;
   Set(menu,"resumeFrame",-10);
   radioRoot=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(InteractivePropsSetup.Folder+"/PF_ManorRadio_Stationary.prefab"));
   var radio=radioRoot.GetComponent<PortableRadio>();var controls=radioRoot.GetComponent<PhysicalRadioView>();radio.Interact(player);
   Assert(controls.IsOpen&&!radio.Held&&Time.timeScale==1,"Stationary radio opens without pickup or freezing world");
   int frequency=radio.frequency;controls.ActivateControl(0,1);Assert(radio.frequency==frequency+20,"Physical tuning knob changes frequency");
   controls.ActivateControl(1,1);Assert(Mathf.Abs(radio.volume-.5f)<.001f,"Volume knob works");controls.ActivateControl(2,1);Assert(radio.powered,"Power button works");
   radio.Tune(9999);Assert(radio.frequency==1280,"Tuning is clamped");controls.Close();Object.DestroyImmediate(radioRoot);radioRoot=null;
   Set(menu,"resumeFrame",-10);radioRoot=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(InteractivePropsSetup.Folder+"/PF_ManorRadio.prefab"));
   radio=radioRoot.GetComponent<PortableRadio>();controls=radioRoot.GetComponent<PhysicalRadioView>();radio.Interact(player);
   Assert(radio.Held&&radio.transform.parent==camera.transform,"Portable radio picks up");
   Vector3 held=radio.transform.localPosition;Assert(controls.Open(player)&&radio.transform.parent==null,"Held radio detaches before camera zoom");
   controls.Close();Assert(radio.transform.parent==camera.transform&&Vector3.Distance(radio.transform.localPosition,held)<.001f,"Held radio restores attachment");
   report="PASS: physical drum ray targets, animated turns, reverse/wrap, busy input, wrong/correct code, modal/time/camera cleanup, tuning/volume/power, portable/stationary radio.\n";Debug.Log("INTERACTIVE_PROPS_CHECKS_OK");
  }
  finally
  {
   if(lockRoot!=null)Object.DestroyImmediate(lockRoot);if(radioRoot!=null)Object.DestroyImmediate(radioRoot);if(actor!=null)Object.DestroyImmediate(actor);Time.timeScale=1;
   Directory.CreateDirectory("work/interactive_props");File.WriteAllText("work/interactive_props/checks.txt",report.Length>0?report:"FAILED: see Unity log");
  }
 }
 static void Set(object target,string field,object value)=>typeof(GameMenu).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
 static void Assert(bool condition,string label){if(!condition)throw new Exception("Interactive check failed: "+label);}
}
