using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

[RequireComponent(typeof(NoiseListener), typeof(NavMeshAgent))]
public class EnemyInvestigator : MonoBehaviour
{
    public enum State { Idle, Investigating, Searching, Returning }
    [Min(0f)] public float searchDuration = 3f;
    [Min(0.1f)] public float destinationSampleRadius = 2f;
    [Min(1f)] public float investigationTimeout = 20f;
    public bool returnToStart = true;
    [Range(0f, 150f)] public float searchTurnAngle = 70f;
    [Min(0.1f)] public float stuckTimeout = 3f;
    public UnityEvent<State> onStateChanged = new UnityEvent<State>();
    public State CurrentState { get; private set; }
    public Vector3 HomePosition { get; private set; }
    public Vector3 LastInvestigatedPosition { get; private set; }

    private NoiseListener ears;
    private NavMeshAgent agent;
    private float deadline;
    private float searchStarted;
    private Quaternion searchRotation;
    private NavMeshPath path;
    private Vector3 progressPosition;
    private float progressTime;

    void Awake()
    {
        ears = GetComponent<NoiseListener>();
        agent = GetComponent<NavMeshAgent>();
        path = new NavMeshPath();
        HomePosition = transform.position;
    }

    void OnEnable() { ears.onNoiseHeard.AddListener(Investigate); }
    void OnDisable()
    {
        ears.onNoiseHeard.RemoveListener(Investigate);
        Stop();
    }

    public void Investigate(Vector3 position)
    {
        var threat = GetComponent<EnemyThreat>();
        if (threat != null && !threat.CanHear) return;
        if (Echo.Menu.GameMenu.BlocksPlayerInput || Time.timeScale <= 0f) return;
        if (!TrySetDestination(position)) return;
        LastInvestigatedPosition = agent.destination;
        if (CurrentState != State.Investigating) ResetProgressWatch();
        SetState(State.Investigating);
        deadline = Time.time + investigationTimeout;
    }

    void Update()
    {
        var threat = GetComponent<EnemyThreat>();
        if (threat != null && !threat.CanHear) { Stop(); return; }
        if (CurrentState == State.Idle) return;
        if (!agent.enabled || !agent.isOnNavMesh) { SetState(State.Idle); return; }
        if (Echo.Menu.GameMenu.BlocksPlayerInput || Time.timeScale <= 0f) return;
        if (CurrentState == State.Searching)
        {
            float turn = Mathf.Sin((Time.time - searchStarted) * 2f) * searchTurnAngle;
            transform.rotation = searchRotation * Quaternion.Euler(0f, turn, 0f);
            if (Time.time >= deadline) BeginReturn();
            return;
        }
        if (Time.time >= deadline || (!agent.pathPending && agent.pathStatus != NavMeshPathStatus.PathComplete))
        {
            if (CurrentState == State.Returning) Stop(); else BeginReturn();
            return;
        }
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            agent.ResetPath();
            if (CurrentState == State.Returning) { Stop(); return; }
            searchRotation = transform.rotation;
            searchStarted = Time.time;
            SetState(State.Searching);
            deadline = Time.time + searchDuration;
            return;
        }
        if ((transform.position - progressPosition).sqrMagnitude >= 0.0225f) ResetProgressWatch();
        else if (Time.time - progressTime >= Mathf.Max(0.1f, stuckTimeout))
        {
            if (CurrentState == State.Returning) Stop(); else BeginReturn();
        }
    }

    // A future patrol controller can change the return anchor without knowing the player.
    public void SetHome(Vector3 position) { HomePosition = position; }

    private bool TrySetDestination(Vector3 position)
    {
        if (!agent.enabled || !agent.isOnNavMesh) return false;
        var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
        if (!NavMesh.SamplePosition(position, out NavMeshHit hit, destinationSampleRadius, filter)) return false;
        if (!agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete) return false;
        if (!agent.SetPath(path)) return false;
        agent.isStopped = false;
        return true;
    }

    private void BeginReturn()
    {
        if (!returnToStart || !TrySetDestination(HomePosition)) { Stop(); return; }
        ResetProgressWatch();
        SetState(State.Returning);
        deadline = Time.time + investigationTimeout;
    }

    private void ResetProgressWatch() { progressPosition = transform.position; progressTime = Time.time; }

    private void SetState(State next)
    {
        if (CurrentState == next) return;
        CurrentState = next;
        onStateChanged.Invoke(next);
    }

    private void Stop()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh) agent.ResetPath();
        SetState(State.Idle);
    }
}
