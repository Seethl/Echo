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
        [Tooltip("Room passage handled by the resident; cupboards and tutorial doors stay excluded.")]
        public bool residentPassage;
        public bool ResidentCanTraverse=>residentPassage&&!IsLocked;
        public bool PassageFullyOpen=>IsOpen&&!IsMoving;
        private Echo.Manor.ResidentDoorway residentDoorway;
        public bool initiallyLocked;
        public UnityEngine.Events.UnityEvent onOpened = new UnityEngine.Events.UnityEvent();
        public UnityEngine.AI.NavMeshObstacle navigationBlocker;
        public string CheckpointId => checkpointId;
        public bool IsOpen { get; private set; }
        public bool IsLocked { get; private set; }
        public bool IsMoving => !Mathf.Approximately(angle, IsOpen ? openAngle : 0f);
        public bool HasRestoredState {get;private set;}
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
            if(!IsLocked&&IsOpen&&residentPassage){if(residentDoorway==null)residentDoorway=Echo.Manor.ResidentDoorway.For(this);if(residentDoorway!=null&&residentDoorway.IsOccupied()){player.ui.ShowToast("У проході хтось стоїть");return;}}
            IsLocked = false;
            IsOpen = stayOpen || !IsOpen;
            NoiseSystem.Emit(transform.position+Vector3.up,4,gameObject,NoiseSystem.Kind.Interaction,1,player.gameObject);
            if(GetComponent<Echo.Audio.HingedDoorAudio>()?.HasMovementAudio!=true)
                Echo.Audio.PrototypeAudio.Play(gameObject,Echo.Audio.PrototypeAudio.Cue.Creak,.5f);
            if(IsOpen)onOpened.Invoke();
        }

        public void UnlockAndOpen() { IsLocked=false; IsOpen=true; }
        public bool OpenForResident()
        {
            if(!isActiveAndEnabled||!ResidentCanTraverse)return false;
            if(!IsOpen){IsOpen=true;onOpened.Invoke();}return true;
        }
        public bool CloseForResident()
        {
            if(!isActiveAndEnabled||!ResidentCanTraverse)return false;
            if(residentDoorway==null)residentDoorway=Echo.Manor.ResidentDoorway.For(this);
            if(residentDoorway!=null&&residentDoorway.IsOccupied())return false;
            IsOpen=false;return true;
        }
        [System.Serializable] private struct Saved { public bool open,locked; }
        public string CaptureState() => JsonUtility.ToJson(new Saved {open=IsOpen,locked=IsLocked});
        public void RestoreState(string state)
        {
            HasRestoredState=true;
            var saved=JsonUtility.FromJson<Saved>(state);IsOpen=saved.open;IsLocked=saved.locked;
            angle=IsOpen?openAngle:0;hinge.localRotation=closedRotation*Quaternion.Euler(0,angle,0);
            if(navigationBlocker!=null)navigationBlocker.enabled=!ResidentCanTraverse&&!IsOpen;
        }

        private void Update()
        {
            angle = Mathf.MoveTowards(angle, IsOpen ? openAngle : 0f, degreesPerSecond * Time.deltaTime);
            hinge.localRotation = closedRotation * Quaternion.Euler(0f, angle, 0f);
            if(navigationBlocker!=null)navigationBlocker.enabled=!ResidentCanTraverse&&Mathf.Abs(angle)<75f;
        }
    }
}
