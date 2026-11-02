using System.Collections.Generic;
using Echo.Manor;
using UnityEngine;
using UnityEngine.AI;

// Navigation plans through unlocked door portals; the actual solid leaf must
// finish opening before locomotion is allowed to cross it.
[DisallowMultipleComponent,RequireComponent(typeof(EnemyInvestigator),typeof(NavMeshAgent))]
[DefaultExecutionOrder(-100)]
public sealed class EnemyDoorTraversal:MonoBehaviour
{
    [Min(.5f)]public float approachDistance=2.2f;
    [Min(.5f)]public float animationSeconds=1.5f;
    [Range(0,1)]public float contactFraction=.32f;
    [Min(.3f)]public float exitClearance=.95f;
    public ResidentDoorway ActiveDoorway{get;private set;}
    public bool IsAnimating=>phase==1||phase==3;
    private EnemyInvestigator brain;private EnemyPresentation presentation;private NavMeshAgent agent;
    private int phase;private float entered,entrySide,nextScan;
    private readonly List<ResidentDoorway> pending=new List<ResidentDoorway>();
    private void Awake(){brain=GetComponent<EnemyInvestigator>();presentation=GetComponent<EnemyPresentation>();agent=GetComponent<NavMeshAgent>();}
    private void OnDisable(){ActiveDoorway=null;pending.Clear();phase=0;}
    private void Update()
    {
        if(Time.timeScale<=0||!brain.isActiveAndEnabled||!agent.enabled||!agent.isOnNavMesh)return;
        if(ActiveDoorway!=null)
        {
            var p=ActiveDoorway;var door=p.door;
            if(door==null||!door.isActiveAndEnabled){Reset();return;}
            if(phase==1)
            {
                HoldAndFace(p);
                if(Time.time-entered>=animationSeconds*contactFraction)door.OpenForResident();
                if(Time.time-entered>=animationSeconds&&door.PassageFullyOpen)phase=2;
                return;
            }
            if(phase==2)
            {
                if(!door.IsOpen){Begin(p,1);return;}
                if(!door.PassageFullyOpen){brain.HoldForDoor(.15f);return;}
                float side=p.Side(transform.position);
                if(side*entrySide<-exitClearance){if(!pending.Contains(p))pending.Add(p);Reset();}
                else if(Time.time-entered>12||(!PathCrosses(p)&&Mathf.Abs(side)>exitClearance)){if(!pending.Contains(p))pending.Add(p);Reset();}
                return;
            }
            if(phase==3)
            {
                HoldAndFace(p);
                if(p.IsOccupied()){door.OpenForResident();Reset();return;}
                if(Time.time-entered>=animationSeconds*contactFraction)door.CloseForResident();
                if(Time.time-entered>=animationSeconds&&!door.IsMoving){pending.Remove(p);Reset();}
                return;
            }
        }
        if(Time.time<nextScan)return;nextScan=Time.time+.08f;
        // A player holding the doorway open delays closing; never sweep them.
        for(int i=pending.Count-1;i>=0;i--)
        {
            var p=pending[i];if(p==null||!p.door.IsOpen){pending.RemoveAt(i);continue;}
            if(!p.IsOccupied()){Begin(p,3);return;}
        }
        if(!agent.hasPath||agent.pathPending||agent.path.corners.Length<2)return;
        var corners=agent.path.corners;ResidentDoorway next=null;float best=approachDistance;
        foreach(var p in ResidentDoorway.All)
        {
            if(p==null||p.door==null||!p.door.isActiveAndEnabled||!p.door.ResidentCanTraverse)continue;
            if(Mathf.Abs(p.transform.position.y-transform.position.y)>.8f||Vector3.Distance(p.transform.position,transform.position)>approachDistance+p.width)continue;
            float travelled=0;var from=transform.position;
            for(int i=1;i<corners.Length&&travelled<approachDistance;i++)
            {
                if(p.Crosses(from,corners[i],out float d)&&travelled+d<best){best=travelled+d;next=p;}
                travelled+=Vector3.Distance(from,corners[i]);from=corners[i];
            }
        }
        if(next!=null){entrySide=Mathf.Sign(next.Side(transform.position));if(entrySide==0)entrySide=1;Begin(next,next.door.PassageFullyOpen?2:1);}
    }
    private bool PathCrosses(ResidentDoorway p)
    {if(!agent.hasPath)return false;var corners=agent.path.corners;var from=transform.position;for(int i=1;i<corners.Length;i++){if(p.Crosses(from,corners[i],out _))return true;from=corners[i];}return false;}
    private void Begin(ResidentDoorway p,int value)
    {ActiveDoorway=p;phase=value;entered=Time.time;if(value==1||value==3){presentation?.PlayDoorMotion(value==3,animationSeconds);HoldAndFace(p);}}
    private void HoldAndFace(ResidentDoorway p)
    {
        brain.HoldForDoor(.16f);agent.isStopped=true;
        var delta=p.transform.position-transform.position;delta.y=0;
        if(delta.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(delta),220*Time.deltaTime);
    }
    private void Reset(){ActiveDoorway=null;phase=0;}
}
