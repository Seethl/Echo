using UnityEngine;

namespace Echo.Interactions
{
    public sealed class HingedDoor : Interactable, ICheckpointObject
    {
        [Tooltip("Pivot on the door's edge. Put the panel and its collider beneath it.")]
        public Transform hinge;
        public float openAngle = 100f;
        [Min(1f)] public float degreesPerSecond = 140f;
        [Tooltip("Empty for an unlocked cupboard; otherwise match KeyPickup.keyId exactly.")]
        public string requiredKeyId = "";
        public string missingKeyMessage = "Потрібен службовий ключ";
        public string checkpointId;
        public bool stayOpen;
        public bool initiallyLocked;
        public UnityEngine.Events.UnityEvent onOpened = new UnityEngine.Events.UnityEvent();
        public UnityEngine.AI.NavMeshObstacle navigationBlocker;
        public string CheckpointId => checkpointId;
        public bool IsOpen { get; private set; }
        public bool IsLocked { get; private set; }
        public bool IsMoving => !Mathf.Approximately(angle, IsOpen ? openAngle : 0f);
        private Quaternion closedRotation;
        private float angle;

        public override string Prompt => IsLocked ? "E — відімкнути" :
            IsOpen ? "E — зачинити" : "E — відчинити";

        private void Awake()
        {
            if (hinge == null) hinge = transform;
            closedRotation = hinge.localRotation;
            IsLocked = initiallyLocked || !string.IsNullOrWhiteSpace(requiredKeyId);
        }

        public override void Interact(PlayerInteractor player)
        {
            if (IsLocked && (string.IsNullOrEmpty(requiredKeyId) || !player.HasKey(requiredKeyId)))
            {
                Echo.Audio.PrototypeAudio.Play(gameObject,Echo.Audio.PrototypeAudio.Cue.Click,.3f);
                player.ui.ShowToast(missingKeyMessage);
                return;
            }
            IsLocked = false;
            IsOpen = stayOpen || !IsOpen;
            NoiseSystem.Emit(transform.position+Vector3.up,4,gameObject,NoiseSystem.Kind.Interaction,1,player.gameObject);
            if(GetComponent<Echo.Audio.HingedDoorAudio>()?.HasMovementAudio!=true)
                Echo.Audio.PrototypeAudio.Play(gameObject,Echo.Audio.PrototypeAudio.Cue.Creak,.5f);
            if(IsOpen)onOpened.Invoke();
        }

        public void UnlockAndOpen() { IsLocked=false; IsOpen=true; }
        [System.Serializable] private struct Saved { public bool open,locked; }
        public string CaptureState() => JsonUtility.ToJson(new Saved {open=IsOpen,locked=IsLocked});
        public void RestoreState(string state)
        {
            var saved=JsonUtility.FromJson<Saved>(state);IsOpen=saved.open;IsLocked=saved.locked;
            angle=IsOpen?openAngle:0;hinge.localRotation=closedRotation*Quaternion.Euler(0,angle,0);
            if(navigationBlocker!=null)navigationBlocker.enabled=!IsOpen;
        }

        private void Update()
        {
            angle = Mathf.MoveTowards(angle, IsOpen ? openAngle : 0f, degreesPerSecond * Time.deltaTime);
            hinge.localRotation = closedRotation * Quaternion.Euler(0f, angle, 0f);
            if(navigationBlocker!=null)navigationBlocker.enabled=Mathf.Abs(angle)<75f;
        }
    }
}
