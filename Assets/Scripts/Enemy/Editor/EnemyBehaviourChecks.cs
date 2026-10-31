using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class EnemyBehaviourChecks
{
    private const string Pending="Echo.EnemyBehaviourChecks.Pending";
    static EnemyBehaviourChecks(){EditorApplication.update+=PollBatchRun;}
    private static void PollBatchRun()
    {
        if(!Application.isBatchMode||!SessionState.GetBool(Pending,false)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(!EditorApplication.isPlaying)
        {
            if(!EditorApplication.isPlayingOrWillChangePlaymode)EditorApplication.EnterPlaymode();
            return;
        }
        SessionState.SetBool(Pending,false);
        try{Run();EditorApplication.Exit(0);}
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
    [MenuItem("Tools/Echo/Checks/Enemy Behaviour")]
    public static void Run()
    {
        if(!Application.isPlaying)
        {
            if(!Application.isBatchMode)throw new Exception("Run this check in Play Mode in an empty test scene.");
            SessionState.SetBool(Pending,true);
            UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=null;
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
            EditorApplication.EnterPlaymode();return;
        }
        NavMeshData data=null;NavMeshDataInstance nav=default;
        GameObject root=null;
        string report="";
        try
        {
            var settings=NavMesh.GetSettingsByID(0);
            var sources=new System.Collections.Generic.List<NavMeshBuildSource>{new NavMeshBuildSource{
                shape=NavMeshBuildSourceShape.Box,size=new Vector3(20,.2f,20),
                transform=Matrix4x4.TRS(new Vector3(0,-.1f,0),Quaternion.identity,Vector3.one),area=0}};
            data=NavMeshBuilder.BuildNavMeshData(settings,sources,new Bounds(Vector3.zero,new Vector3(24,8,24)),Vector3.zero,Quaternion.identity);
            Assert(data!=null,"Test NavMesh generated");nav=NavMesh.AddNavMeshData(data);
            root=new GameObject("Enemy isolated check");root.SetActive(false);
            var brain=root.AddComponent<EnemyInvestigator>();brain.visionEnabled=false;
            var agent=root.GetComponent<NavMeshAgent>();agent.enabled=false;
            Call(brain,"Awake");root.SetActive(true);agent.enabled=true;
            Assert(agent.Warp(Vector3.zero)&&agent.isOnNavMesh,"Enemy on test NavMesh");
            brain.heardReactionSeconds=1;
            int changes=0,reactions=0;brain.StateChanged+=_=>changes++;brain.NoiseReacted+=_=>reactions++;
            brain.Investigate(new Vector3(3,0,0));
            Assert(brain.CurrentState==EnemyInvestigator.State.Investigating,"Noise starts investigation");
            Assert(changes==1&&reactions==1,"One state event and one hearing reaction");
            Call(brain,"Update");Assert(agent.isStopped,"Enemy pauses to react before walking");
            brain.Investigate(new Vector3(3,0,0));
            Assert(changes==1&&reactions==1,"Repeated pulse does not restart the reaction");
            Vector3 remembered=brain.LastSoundGoal;
            Change(brain,EnemyInvestigator.State.Chasing);
            Assert(Mathf.Approximately(agent.speed,brain.chaseSpeed),"Chase speed applied");
            brain.Investigate(new Vector3(-3,0,0));
            Assert(brain.CurrentState==EnemyInvestigator.State.Chasing&&brain.LastSoundGoal==remembered,"Radio cannot redirect a chase");
            Change(brain,EnemyInvestigator.State.Investigating);
            Assert(Mathf.Approximately(agent.speed,brain.patrolSpeed*1.2f),"Investigation restores its own speed after chase");
            Assert(brain.Suspicion01==0,"Investigation is not reported as visual suspicion");
            Change(brain,EnemyInvestigator.State.Chasing);Assert(brain.Suspicion01==1,"Chase reports full awareness");
            var adapter=root.GetComponent<EnemyPresentation>();Assert(adapter!=null,"Presentation adapter auto-attached");
            adapter.RefreshAnimator();Assert(!adapter.TryPlayFootstep(),"Empty recorded step list retains old fallback");
            var clip=AudioClip.Create("Transient step check",64,1,22050,false);
            try
            {
                adapter.footstepVariations=new[]{clip,null};
                Assert(adapter.TryPlayFootstep()&&adapter.TryPlayFootstep(),"Single valid step among null entries can repeat");
            }
            finally{Object.DestroyImmediate(clip);}
            report="PASS: investigation reaction, pulse suppression, chase priority, speed transitions, awareness values, optional presentation and audio fallback.\n";
            Debug.Log("ENEMY_BEHAVIOUR_CHECKS_OK");
        }
        finally
        {
            if(root!=null)Object.DestroyImmediate(root);
            if(nav.valid)nav.Remove();if(data!=null)Object.DestroyImmediate(data);
            string dir="work/enemy_pass";Directory.CreateDirectory(dir);
            File.WriteAllText(dir+"/checks.txt",string.IsNullOrEmpty(report)?"FAILED: see Unity log":report);
        }
    }
    private static void Call(EnemyInvestigator brain,string method)=>typeof(EnemyInvestigator).GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(brain,null);
    private static void Change(EnemyInvestigator brain,EnemyInvestigator.State state)=>typeof(EnemyInvestigator).GetMethod("Change",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(brain,new object[]{state});
    private static void Assert(bool condition,string message){if(!condition)throw new Exception("Enemy check failed: "+message);}
}
