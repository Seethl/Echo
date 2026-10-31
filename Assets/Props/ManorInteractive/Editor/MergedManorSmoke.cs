using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Echo.Interactions;
using Echo.Manor;
using Echo.Menu;
using Echo.Radio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class MergedManorSmoke
{
 const string Pending="Echo.MergedManor.Smoke";
 static double readyAt=-1;
 static bool startedLevel;
 static MergedManorSmoke(){EditorApplication.update+=Poll;}
 public static void Begin()
 {
  if(!Application.isBatchMode)throw new Exception("Run in an isolated batch test project.");
  startedLevel=false;readyAt=-1;
  MergedManorIntegration.ApplyAndSave();
  EditorSceneManager.playModeStartScene=null;EditorSceneManager.OpenScene(GameMenu.MainMenuScene);
  EditorApplication.LockReloadAssemblies();Application.runInBackground=true;
  SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
 }
 static void Poll()
 {
  if(!Application.isBatchMode||!SessionState.GetBool(Pending,false)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  if(!EditorApplication.isPlaying){if(!EditorApplication.isPlayingOrWillChangePlaymode)EditorApplication.EnterPlaymode();return;}
  if(!startedLevel)
  {
   if(Object.FindAnyObjectByType<GameMenu>()==null)return;
   startedLevel=true;GameMenu.StartNewGame();readyAt=EditorApplication.timeSinceStartup+2;return;
  }
  if(readyAt<0){readyAt=EditorApplication.timeSinceStartup+1.5;return;}
  if(EditorApplication.timeSinceStartup<readyAt)return;
  if((ManorProgress.Active==null||Object.FindAnyObjectByType<ManorNavigation>()?.Ready!=true)&&EditorApplication.timeSinceStartup<readyAt+90)return;
  SessionState.SetBool(Pending,false);
  try{Check();EditorApplication.Exit(0);}catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
 }
 static void Check()
 {
  var progress=ManorProgress.Active;Assert(progress!=null&&progress.treatmentRoute,"New treatment route active");
  var player=Object.FindAnyObjectByType<PlayerInteractor>();Assert(player!=null&&player.viewCamera!=null&&player.ui!=null,"Player prefab and UI intact");
  Assert(Object.FindObjectsByType<ManorZone>().Length>=9,"Upper-floor zones retained");
  Assert(Object.FindAnyObjectByType<TreatmentWorkbench>()!=null&&Object.FindAnyObjectByType<RemedyDelivery>()!=null&&Object.FindAnyObjectByType<SlidingPicture>()!=null,"Treatment and picture mechanics retained");
  var enemy=Object.FindAnyObjectByType<EnemyInvestigator>();Assert(enemy!=null&&enemy.isActiveAndEnabled&&enemy.GetComponent<NavMeshAgent>().isOnNavMesh,"Enemy active on rebuilt navigation");
  var radios=Object.FindObjectsByType<PortableRadio>();Assert(radios.Length==1,"Exactly one scenario radio");
  var radio=radios[0];Assert(!radio.allowPlacement&&radio.physicalView!=null&&radio.channels.Length>=5,"Scenario radio options preserved");
  Assert(radio.transform.lossyScale==Vector3.one,"Radio uses real model scale");
  Assert(!GameMenu.BlocksPlayerInput,"Level starts without blocked input");
  radio.Interact(player);Assert(radio.Held,"Scenario radio picks up");
  radio.OpenControls();Assert(radio.physicalView.IsOpen&&Time.timeScale>0,"Physical radio opens without stopping level: open="+radio.physicalView.IsOpen+", time="+Time.timeScale+", freezes="+radio.physicalView.freezeWorld+", anchor="+(radio.physicalView.cameraAnchor!=null)+", playerCamera="+(player.viewCamera!=null)+", blocked="+GameMenu.BlocksPlayerInput);
  Physics.SyncTransforms();
  foreach(var control in radio.GetComponentsInChildren<PropPhysicalControl>())
  {
   var collider=control.GetComponent<Collider>();var origin=radio.physicalView.cameraAnchor.position;
   Assert(Physics.Raycast(origin,(collider.bounds.center-origin).normalized,out var hit,3)&&hit.collider==collider,"Radio control is unobstructed: "+control.controlId);
  }
  int previous=radio.frequency;radio.physicalView.ActivateControl(0,1);Assert(radio.frequency==Mathf.Min(1280,previous+20),"Scenario tuning works");
  radio.physicalView.Close();UnblockResumeFrame();
  var keypad=Object.FindObjectsByType<KeypadLock>().First(k=>k.unlockFlag=="hall.open");
  Assert(keypad.useRunCode&&keypad.physicalView!=null,"Random run code retained on physical gallery lock");
  Assert(keypad.physicalView.Open(player),"Gallery lock starts closeup");
  var code=keypad.EffectiveCode;keypad.SetDigits((code[0]-'0'+1)%10,code[1]-'0',code[2]-'0');
  Assert(!keypad.Submit(player)&&!progress.Has("hall.open"),"Wrong code cannot open route");
  keypad.physicalView.Close();UnblockResumeFrame();keypad.SetDigits(0,0,0);
  Assert(keypad.physicalView.Open(player),"Gallery lock reopens for physical entry");
  Physics.SyncTransforms();
  for(int i=0;i<3;i++)
  {
   var rotor=keypad.physicalView.drums[i];var origin=keypad.physicalView.cameraAnchor.position;
   Assert(Physics.Raycast(origin,(rotor.GetComponent<Collider>().bounds.center-origin).normalized,out var hit,3)&&hit.collider.GetComponent<PropPhysicalControl>()?.controlId==i,"Gallery rotor is unobstructed: "+i);
   for(int digit=0;digit<code[i]-'0';digit++){keypad.physicalView.ActivateControl(i,1);keypad.physicalView.AdvanceAnimation(.3f);}
   Assert(keypad.GetDigit(i)==code[i]-'0',"Physical drum enters run digit "+i);
  }
  keypad.physicalView.ActivateControl(3,1);
  Assert(progress.Has("hall.open")&&!keypad.physicalView.IsOpen&&Mathf.Approximately(Time.timeScale,1),"Correct run code opens route and restores gameplay");
  Assert(radio.allowPlacement==false,"Radio placement restriction remains after interaction");
  Directory.CreateDirectory("work/interactive_props");File.WriteAllText("work/interactive_props/merged-manor-smoke.txt",
   "PASS: treatment route, upper zones, player/UI, picture and medicine components.\nPASS: live enemy on NavMesh.\nPASS: one physical radio, original channel list and placement restriction.\nPASS: physical radio pickup, inspection and tuning in the actual level.\nPASS: gallery random code, wrong-code rejection, correct-code flag and modal cleanup.\nThis is a smoke test, not a complete new-route playthrough.\n");Debug.Log("MERGED_MANOR_SMOKE_OK");
 }
 static void UnblockResumeFrame(){var menu=Object.FindAnyObjectByType<GameMenu>();typeof(GameMenu).GetField("resumeFrame",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(menu,-1);}
 static void Assert(bool condition,string label){if(!condition)throw new Exception("Merged manor check failed: "+label);}
}
