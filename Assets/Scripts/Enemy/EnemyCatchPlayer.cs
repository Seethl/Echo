using UnityEngine;
using UnityEngine.AI;

public class EnemyCatchPlayer : MonoBehaviour
{
    public PlayerCaught target;
    [Min(0f)] public float catchDistance = 1.1f;
    public float eyeHeight = 0.9f;
    public LayerMask obstructionLayers = Physics.DefaultRaycastLayers;

    void Start()
    {
        if (target == null) target = FindAnyObjectByType<PlayerCaught>();
    }

    void Update()
    {
        if (Echo.Menu.GameMenu.BlocksPlayerInput || Time.timeScale <= 0f) return;
        var threat = GetComponent<EnemyThreat>();
        if (threat != null && !threat.CanCatch) return;
        if (target == null || target.IsCaught || !target.isActiveAndEnabled) return;
        var controller = target.GetComponent<CharacterController>();
        Vector3 targetPosition = controller != null ? controller.bounds.center : target.transform.position;
        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        if ((targetPosition - origin).sqrMagnitude > catchDistance * catchDistance) return;

        // A nearby player behind a wall must not be caught through it.
        if (Physics.Linecast(origin, targetPosition, out RaycastHit hit, obstructionLayers, QueryTriggerInteraction.Ignore)
            && hit.collider.GetComponentInParent<PlayerCaught>() != target) return;

        target.Catch();
        var investigator = GetComponent<EnemyInvestigator>();
        if (investigator != null) investigator.enabled = false;
        var agent = GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled && agent.isOnNavMesh) agent.ResetPath();
    }
}
