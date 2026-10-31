using System;
using System.IO;
using System.Linq;
using Echo.Interactions;
using Echo.Manor;
using Echo.Radio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class MergedManorIntegration
{
 const string ScenePath="Assets/Scenes/ManorPrototype/ManorPrototype.unity";
 [MenuItem("Tools/Echo/Integrate Physical Radio And Lock In Manor")]
 public static void ApplyAndSave()
 {
  if(!Application.isBatchMode&&(EditorSceneManager.GetActiveScene().path!=ScenePath||EditorSceneManager.GetActiveScene().isDirty))
   throw new Exception("Open saved ManorPrototype before integration.");
  // Reload after compilation so fields added by the location branch deserialize
  // from disk instead of being saved with defaults from a stale open scene.
  EditorSceneManager.OpenScene(ScenePath);
  var radios=Object.FindObjectsByType<PortableRadio>();
  var locks=Object.FindObjectsByType<KeypadLock>();
  if(radios.Length!=1)throw new Exception("Expected exactly one existing radio, found "+radios.Length);
  var radio=radios[0];int frequency=radio.frequency;bool placement=radio.allowPlacement;
  var channels=radio.channels.ToArray();
  if(radio.physicalView==null)
  {
   var oldCollider=radio.GetComponent<Collider>();float bottom=oldCollider!=null?oldCollider.bounds.min.y:radio.transform.position.y-.11f;
   HideOldVisuals(radio.transform);radio.transform.localScale=Vector3.one;
   var p=radio.transform.position;p.y=bottom+.16f;radio.transform.position=p;
   var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(InteractivePropsSetup.Folder+"/PF_ManorRadio.prefab"));
   visual.name="Physical Radio Visual";visual.transform.SetParent(radio.transform,false);
   var redundant=visual.GetComponent<PortableRadio>();var view=visual.GetComponent<PhysicalRadioView>();
   Object.DestroyImmediate(redundant);view.radio=radio;radio.physicalView=view;
   radio.heldLocalEulerAngles=new Vector3(0,180,0);radio.placementHalfExtents=new Vector3(.25f,.16f,.10f);
   EditorUtility.SetDirty(radio);
  }
  foreach(var keypad in locks)
  {
   if(keypad.physicalView!=null)continue;
   HideOldVisuals(keypad.transform);keypad.transform.localScale=Vector3.one;
   // Gallery approaches from the south; expose the controls to that room.
   if(keypad.unlockFlag=="hall.open")keypad.transform.rotation=Quaternion.Euler(0,180,0);
   var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(InteractivePropsSetup.Folder+"/PF_ManorCombinationLock.prefab"));
   visual.name="Physical Lock Visual";visual.transform.SetParent(keypad.transform,false);
   var redundant=visual.GetComponent<KeypadLock>();var view=visual.GetComponent<MechanicalLockView>();
   Object.DestroyImmediate(redundant);view.keypad=keypad;keypad.physicalView=view;EditorUtility.SetDirty(keypad);
  }
  if(radio.frequency!=frequency||radio.allowPlacement!=placement||!radio.channels.SequenceEqual(channels))throw new Exception("Scenario radio settings changed");
  if(Object.FindObjectsByType<PortableRadio>().Length!=1||Object.FindObjectsByType<KeypadLock>().Length!=locks.Length)throw new Exception("Duplicate gameplay components created");
  int missing=0;foreach(var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))missing+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
  if(missing!=0)throw new Exception("Missing scene scripts: "+missing);
  EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
  Directory.CreateDirectory("work/interactive_props");File.WriteAllText("work/interactive_props/merged-manor-integration.txt",
   "PASS: new manor opened, no missing scripts.\nPASS: one radio, "+locks.Length+" lock(s), no duplicates.\nPASS: original radio channels, frequency, allowPlacement preserved.\nPASS: physical views attached to original gameplay roots; old visual colliders disabled.\n");
  Debug.Log("MERGED_MANOR_PHYSICAL_PROPS_OK");
 }
 static void HideOldVisuals(Transform root)
 {
  foreach(var label in root.GetComponentsInChildren<WorldLabelVisibility>(true))label.enabled=false;
  foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
  foreach(var collider in root.GetComponentsInChildren<Collider>(true))collider.enabled=false;
 }
}
