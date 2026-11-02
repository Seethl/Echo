using System.Collections.Generic;
using Echo.Interactions;
using UnityEngine;
using UnityEngine.AI;
namespace Echo.Manor
{
    // Fixed doorway anchor, independent of the moving leaf and its collider.
    [DisallowMultipleComponent]
    public sealed class ResidentDoorway:MonoBehaviour
    {
        public HingedDoor door;
        public float width=1.3f,height=2.65f;
        public Vector3 hingePosition;
        public Quaternion closedRotation=Quaternion.identity;
        private static readonly List<ResidentDoorway> all=new List<ResidentDoorway>();
        private readonly Collider[] overlaps=new Collider[96];
        public static IReadOnlyList<ResidentDoorway> All=>all;
        public static ResidentDoorway For(HingedDoor value){foreach(var p in all)if(p!=null&&p.door==value)return p;return null;}
        private void OnEnable(){if(!all.Contains(this))all.Add(this);}
        private void OnDisable(){all.Remove(this);}
        public float Side(Vector3 point)=>Vector3.Dot(point-transform.position,transform.forward);
        public bool Crosses(Vector3 from,Vector3 to,out float distance)
        {
            distance=float.PositiveInfinity;float a=Side(from),b=Side(to);
            if(a*b>0||Mathf.Abs(a-b)<.001f)return false;
            var p=Vector3.Lerp(from,to,a/(a-b));var local=transform.InverseTransformPoint(p);
            if(Mathf.Abs(local.x)>width*.5f+.2f||Mathf.Abs(local.y)>1)return false;
            distance=Vector3.Distance(from,p);return true;
        }
        public bool IsOccupied()
        {
            if(door==null)return false;
            // Test the entire leaf sweep, not just the narrow doorway plane.
            var agents=Object.FindObjectsByType<NavMeshAgent>();
            for(int i=0;i<=5;i++)
            {
                var rotation=closedRotation*Quaternion.Euler(0,door.openAngle*i/5f,0);
                var center=hingePosition+rotation*new Vector3(width*.5f,height*.5f,0);
                int count=Physics.OverlapBoxNonAlloc(center,new Vector3(width*.5f+.08f,height*.5f,.15f),overlaps,rotation,~0,QueryTriggerInteraction.Ignore);
                if(count==overlaps.Length)return true;
                foreach(var agent in agents)if(agent.enabled&&agent.gameObject.activeInHierarchy)
                {var local=Quaternion.Inverse(rotation)*(agent.transform.position+Vector3.up*agent.height*.5f-center);if(Mathf.Abs(local.x)<width*.5f+.08f+agent.radius&&Mathf.Abs(local.y)<height*.5f+agent.height*.5f&&Mathf.Abs(local.z)<.15f+agent.radius)return true;}
                for(int j=0;j<count;j++)
                    if(overlaps[j].GetComponentInParent<FPSController>()!=null||overlaps[j].GetComponentInParent<NavMeshAgent>()!=null)return true;
            }
            return false;
        }
    }
}
