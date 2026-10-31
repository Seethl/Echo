using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NoiseListener), typeof(NavMeshAgent))]
public class EnemyInvestigator : MonoBehaviour
{
    public enum State { Idle, Investigating, Searching }
    [Min(0f)] public float searchDuration = 3f;
    [Min(0.1f)] public float destinationSampleRadius = 2f;
    [Min(1f)] public float investigationTimeout = 20f;
    public State CurrentState { get; private set; }

    private NoiseListener ears;
    private NavMeshAgent agent;
    private float deadline;

    void Awake()
    {
        ears = GetComponent<NoiseListener>();
        agent = GetComponent<NavMeshAgent>();
    }

    void OnEnable() { ears.onNoiseHeard.AddListener(Investigate); }
    void OnDisable()
    {
        ears.onNoiseHeard.RemoveListener(Investigate);
        Stop();
    }

    public void Investigate(Vector3 position)
    {
        if (!agent.enabled || !agent.isOnNavMesh) return;
        if (!NavMesh.SamplePosition(position, out NavMeshHit hit, destinationSampleRadius, agent.areaMask)) return;

        // Do not walk to an unreachable partial destination or follow the player silently.
        var path = new NavMeshPath();
        if (!agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete) return;
        agent.isStopped = false;
        if (!agent.SetPath(path)) return;
        CurrentState = State.Investigating;
        deadline = Time.time + investigationTimeout;
    }

    void Update()
    {
        if (CurrentState == State.Idle) return;
        if (!agent.enabled || !agent.isOnNavMesh) { CurrentState = State.Idle; return; }
        if (CurrentState == State.Searching)
        {
            if (Time.time >= deadline) Stop();
            return;
        }
        if (Time.time >= deadline || (!agent.pathPending && agent.pathStatus != NavMeshPathStatus.PathComplete))
        {
            Stop();
            return;
        }
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            agent.ResetPath();
            CurrentState = State.Searching;
            deadline = Time.time + searchDuration;
        }
    }

    private void Stop()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh) agent.ResetPath();
        CurrentState = State.Idle;
    }
}
