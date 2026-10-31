using Echo.Audio;
using Echo.Manor;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NoiseListener),typeof(NavMeshAgent))]
public class EnemyInvestigator:MonoBehaviour
{
    // Keep the original first three enum values for the existing test scenes.
    public enum State { Idle,Investigating,Searching,Patrol,Suspicious,Chasing,Returning }
    [Min(0)]public float searchDuration=9;
    [Min(.1f)]public float destinationSampleRadius=2;
    [Min(1)]public float investigationTimeout=24;
    public Transform[] patrolPoints;
    public string[] patrolUnlockFlags;
    public FPSController target;
    public bool visionEnabled=true;
    public float sightRange=12,fieldOfView=100,suspicionSeconds=1.1f;
    public float patrolSpeed=1.6f,chaseSpeed=3.6f,patrolWait=1.4f;
    public AudioClip footstepsClip,suspicionClip,chaseClip;
    public State CurrentState {get;private set;}
    public bool SeesPlayer {get;private set;}
    public Vector3 LastKnownPosition {get;private set;}
    public Vector3 LastSoundGoal {get;private set;}
    private NoiseListener ears;private NavMeshAgent agent;
    private float deadline,nextSense,lastSeen,suspicion,nextRepath,lastNoiseTime=-100,currentNoiseStrength,nextStep,waitUntil,nextSearchMove;
    private int patrolIndex=-1,searchIndex;
    private Vector3 searchCenter;
    private static readonly Vector3[] searchOffsets={Vector3.right*2,Vector3.left*2,Vector3.forward*2,Vector3.back*1.5f};
    private void Awake(){ears=GetComponent<NoiseListener>();agent=GetComponent<NavMeshAgent>();PrototypeAudio.Source(gameObject);}
    private void OnEnable(){ears.onNoiseHeard.AddListener(Investigate);}
    private void OnDisable(){ears.onNoiseHeard.RemoveListener(Investigate);Stop();}
    private void Start()
    {
        if(target==null)target=FindAnyObjectByType<FPSController>();
        if(ManorProgress.Active!=null&&ManorProgress.Active.RestoredEnemyPosition.HasValue)transform.position=ManorProgress.Active.RestoredEnemyPosition.Value;
    }
    public bool CanSeeTarget()
    {
        if(!visionEnabled||target==null||!target.isActiveAndEnabled)return false;
        var progress=ManorProgress.Active;
        if(progress!=null&&(!progress.Has("hall.open")||progress.IsTutorialPosition(target.transform.position)))return false;
        Vector3 origin=transform.position+Vector3.up*1.5f;
        var controller=target.GetComponent<CharacterController>();Vector3 point=controller!=null?controller.bounds.center+Vector3.up*.3f:target.transform.position;
        Vector3 delta=point-origin;float range=sightRange*(target.IsCrouching?.68f:1);
        if(delta.magnitude>range)return false;
        if(delta.magnitude>2.2f&&Vector3.Angle(transform.forward,delta)>fieldOfView*.5f)return false;
        if(Physics.Linecast(origin,point,out var hit,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&hit.collider.GetComponentInParent<FPSController>()!=target)return false;
        return true;
    }
    public void Investigate(Vector3 position)
    {
        if(Time.timeScale<=0||!Usable())return;
        var progress=ManorProgress.Active;
        if(progress!=null&&(!progress.Has("hall.open")||progress.IsTutorialPosition(position)||progress.IsLibraryOrientationPosition(position)))return;
        // Never replace visual pursuit with a radio pulse or read a hidden player's transform.
        if(CurrentState==State.Chasing||CanSeeTarget())return;
        if((CurrentState==State.Investigating||CurrentState==State.Searching)&&Time.time-lastNoiseTime<2&&
            (Vector3.Distance(position,LastSoundGoal)<1.5f||ears.LastStrength<=currentNoiseStrength+.18f))return;
        if(!Go(position))return;
        LastSoundGoal=position;LastKnownPosition=position;lastNoiseTime=Time.time;currentNoiseStrength=ears.LastStrength;
        Change(State.Investigating);deadline=Time.time+investigationTimeout;agent.speed=patrolSpeed*1.2f;
    }
    private bool Usable()=>agent!=null&&agent.enabled&&agent.isOnNavMesh;
    private bool Go(Vector3 point)
    {
        if(!Usable())return false;
        // A radio can sit on a table with an unreachable top. Try reachable floor nearby.
        for(int i=0;i<=searchOffsets.Length;i++)
        {
            Vector3 candidate=i==0?point:new Vector3(point.x,transform.position.y,point.z)+searchOffsets[i-1];
            if(!NavMesh.SamplePosition(candidate,out var sample,destinationSampleRadius,agent.areaMask))continue;
            var progress=ManorProgress.Active;if(progress!=null&&progress.IsTutorialPosition(sample.position))continue;
            var path=new NavMeshPath();if(!agent.CalculatePath(sample.position,path)||path.status!=NavMeshPathStatus.PathComplete)continue;
            agent.isStopped=false;return agent.SetPath(path);
        }
        return false;
    }
    private bool Arrived()=>!agent.pathPending&&(!agent.hasPath||agent.remainingDistance<=agent.stoppingDistance+.15f);
    private void Update()
    {
        if(!Usable())return;
        if(Time.timeScale<=0){agent.isStopped=true;return;}
        agent.isStopped=false;
        if(Time.time>=nextSense)
        {
            nextSense=Time.time+.12f;SeesPlayer=CanSeeTarget();
            if(SeesPlayer)
            {
                LastKnownPosition=target.transform.position;lastSeen=Time.time;
                if(CurrentState!=State.Chasing)
                {
                    if(CurrentState!=State.Suspicious){Change(State.Suspicious);suspicion=0;agent.ResetPath();}
                    suspicion+=.12f;
                    if(suspicion>=suspicionSeconds)Change(State.Chasing);
                }
            }
            else if(CurrentState==State.Suspicious&&Time.time-lastSeen>.45f)
            {if(Go(LastKnownPosition)){Change(State.Investigating);deadline=Time.time+investigationTimeout;}else BeginSearch();}
        }
        if(CurrentState==State.Chasing)
        {
            agent.speed=chaseSpeed;
            if(SeesPlayer&&Time.time>=nextRepath){Go(LastKnownPosition);nextRepath=Time.time+.25f;}
            if(!SeesPlayer&&Time.time-lastSeen>.85f)
            {if(Go(LastKnownPosition)){Change(State.Investigating);deadline=Time.time+investigationTimeout;}else BeginSearch();}
        }
        else if(CurrentState==State.Idle)NextPatrol();
        else if(CurrentState==State.Patrol)
        {
            agent.speed=patrolSpeed;
            if(Arrived()){if(waitUntil==0)waitUntil=Time.time+patrolWait;if(Time.time>=waitUntil)NextPatrol();}
        }
        else if(CurrentState==State.Returning){agent.speed=patrolSpeed;if(Arrived())NextPatrol();}
        else if(CurrentState==State.Investigating)
        {if(Time.time>=deadline||(!agent.pathPending&&agent.pathStatus!=NavMeshPathStatus.PathComplete))ReturnToPatrol();else if(Arrived())BeginSearch();}
        else if(CurrentState==State.Searching)
        {
            if(Time.time>=deadline)ReturnToPatrol();
            else if(Time.time>=nextSearchMove){Go(searchCenter+searchOffsets[searchIndex++%searchOffsets.Length]);nextSearchMove=Time.time+2.2f;}
        }
        if(agent.velocity.sqrMagnitude>.05f&&Time.time>=nextStep)
        {nextStep=Time.time+(CurrentState==State.Chasing?.42f:.7f);PrototypeAudio.Play(gameObject,PrototypeAudio.Cue.Footstep,.8f,footstepsClip);}
    }
    private void Change(State state)
    {
        if(CurrentState==state)return;CurrentState=state;
        if(state==State.Suspicious)PrototypeAudio.Play(gameObject,PrototypeAudio.Cue.Click,.85f,suspicionClip);
        if(state==State.Chasing)PrototypeAudio.Play(gameObject,PrototypeAudio.Cue.Alert,1,chaseClip);
    }
    private void BeginSearch(){Change(State.Searching);searchCenter=LastKnownPosition;deadline=Time.time+searchDuration;nextSearchMove=Time.time+1;searchIndex=0;if(Usable())agent.ResetPath();}
    private bool PointAvailable(int i)
    {
        if(patrolPoints==null||patrolPoints[i]==null)return false;
        string flag=patrolUnlockFlags!=null&&i<patrolUnlockFlags.Length?patrolUnlockFlags[i]:null;
        return string.IsNullOrEmpty(flag)||ManorProgress.Active==null||ManorProgress.Active.Has(flag);
    }
    private void NextPatrol()
    {
        if(patrolPoints==null||patrolPoints.Length==0){Stop();return;}
        for(int j=0;j<patrolPoints.Length;j++)
        {patrolIndex=(patrolIndex+1)%patrolPoints.Length;if(PointAvailable(patrolIndex)&&Go(patrolPoints[patrolIndex].position)){waitUntil=0;Change(State.Patrol);return;}}
        Stop();
    }
    private void ReturnToPatrol()
    {
        if(patrolPoints!=null)
        {
            float best=float.PositiveInfinity;int index=-1;
            for(int i=0;i<patrolPoints.Length;i++)if(PointAvailable(i)&&Vector3.Distance(transform.position,patrolPoints[i].position)<best){best=Vector3.Distance(transform.position,patrolPoints[i].position);index=i;}
            if(index>=0&&Go(patrolPoints[index].position)){patrolIndex=index;Change(State.Returning);return;}
        }
        Stop();
    }
    private void Stop(){if(Usable())agent.ResetPath();CurrentState=State.Idle;SeesPlayer=false;}
}
