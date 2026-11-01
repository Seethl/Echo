using UnityEngine;
using UnityEngine.AI;

public class EnemyCatchPlayer : MonoBehaviour
{
    public PlayerCaught target;
    [Min(0f)] public float catchDistance = 1.1f;
    public float eyeHeight = 0.9f;
    [Min(0)] public float attackWindupSeconds;
    private float attackStarted=-1;
    private void CancelAttack(){attackStarted=-1;var presentation=GetComponent<EnemyPresentation>();if(presentation!=null)presentation.SetAttacking(false);}
    private void OnDisable()=>CancelAttack();
    public LayerMask obstructionLayers = Physics.DefaultRaycastLayers;

    void Start()
    {
        if (target == null) target = FindAnyObjectByType<PlayerCaught>();
    }

    void Update()
    {
        if (Time.timeScale <= 0f) return;
        if(Echo.Manor.ManorProgress.Active!=null&&target!=null&&Echo.Manor.ManorProgress.Active.IsTutorialPosition(target.transform.position)){CancelAttack();return;}
        if (target == null || target.IsCaught || !target.isActiveAndEnabled){CancelAttack();return;}
        var controller = target.GetComponent<CharacterController>();
        Vector3 targetPosition = controller != null ? controller.bounds.center : target.transform.position;
        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        if ((targetPosition - origin).sqrMagnitude > catchDistance * catchDistance){CancelAttack();return;}

        // A nearby player behind a wall must not be caught through it.
        if (Physics.Linecast(origin, targetPosition, out RaycastHit hit, obstructionLayers, QueryTriggerInteraction.Ignore)
            && hit.collider.GetComponentInParent<PlayerCaught>() != target){CancelAttack();return;}

        if(attackStarted<0){attackStarted=Time.time;var presentation=GetComponent<EnemyPresentation>();if(presentation!=null)presentation.SetAttacking(true);}
        if(Time.time-attackStarted<attackWindupSeconds)return;
        target.Catch();
        var investigator = GetComponent<EnemyInvestigator>();
        if (investigator != null) investigator.enabled = false;
        var agent = GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled && agent.isOnNavMesh) agent.ResetPath();
    }
}
