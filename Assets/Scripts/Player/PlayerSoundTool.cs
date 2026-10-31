using Echo.Menu;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>One reusable clock. No scene, UI or inventory singleton is required.</summary>
public class PlayerSoundTool : MonoBehaviour
{
    public GameProgression progression;
    public AlarmDecoy alarmPrefab;
    public Camera viewCamera;
    public LayerMask placementLayers = Physics.DefaultRaycastLayers;
    [Min(0.1f)] public float placementDistance = 3f;
    [Min(0.1f)] public float shortDelay = 3f;
    [Min(0.1f)] public float longDelay = 8f;
    public bool keyboardControls = true;
    public bool UseLongDelay { get; private set; }
    public AlarmDecoy ActiveAlarm { get; private set; }
    public bool IsUnlocked => progression != null && progression.SoundToolUnlocked;
    public bool CanUse
    {
        get
        {
            var life = GetComponent<PlayerCaught>();
            return isActiveAndEnabled && IsUnlocked && !GameMenu.BlocksPlayerInput &&
                Time.timeScale > 0f && (life == null || !life.IsCaught);
        }
    }

    void Awake() { if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>(); }

    void Update()
    {
        if (!keyboardControls || Keyboard.current == null || !CanUse) return;
        if (Keyboard.current.tKey.wasPressedThisFrame) ToggleDelay();
        if (Keyboard.current.qKey.wasPressedThisFrame) TryPlace();
    }

    public void ToggleDelay() { if (CanUse) UseLongDelay = !UseLongDelay; }

    public bool TryPlace()
    {
        if (!CanUse || ActiveAlarm != null || alarmPrefab == null || viewCamera == null) return false;
        if (alarmPrefab.GetComponent<AudioSource>().clip == null) return false;
        var ray = new Ray(viewCamera.transform.position, viewCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, placementDistance, placementLayers, QueryTriggerInteraction.Ignore)
            || hit.normal.y < 0.7f || hit.collider.transform.IsChildOf(transform)) return false;
        Vector3 position = hit.point + Vector3.up * 0.18f;
        // Reject cramped positions instead of placing a clock inside a wall or cupboard.
        if (Physics.CheckSphere(position, 0.12f, placementLayers, QueryTriggerInteraction.Ignore)) return false;
        ActiveAlarm = Instantiate(alarmPrefab, position, Quaternion.identity);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(ActiveAlarm.gameObject, gameObject.scene);
        ActiveAlarm.Arm(this, UseLongDelay ? longDelay : shortDelay);
        return true;
    }

    public void Recover(AlarmDecoy alarm)
    {
        if (ActiveAlarm == alarm) ActiveAlarm = null;
    }

    void OnDestroy() { if (ActiveAlarm != null) Destroy(ActiveAlarm.gameObject); }
}
