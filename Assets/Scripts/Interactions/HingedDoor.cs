using UnityEngine;

namespace Echo.Interactions
{
    public sealed class HingedDoor : Interactable
    {
        [Tooltip("Pivot on the door's edge. Put the panel and its collider beneath it.")]
        public Transform hinge;
        public float openAngle = 100f;
        [Min(1f)] public float degreesPerSecond = 140f;
        [Tooltip("Empty for an unlocked cupboard; otherwise match KeyPickup.keyId exactly.")]
        public string requiredKeyId = "";
        public string missingKeyMessage = "Потрібен службовий ключ";
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
            IsLocked = !string.IsNullOrWhiteSpace(requiredKeyId);
        }

        public override void Interact(PlayerInteractor player)
        {
            if (IsLocked && !player.HasKey(requiredKeyId))
            {
                player.ui.ShowToast(missingKeyMessage);
                return;
            }
            IsLocked = false;
            IsOpen = !IsOpen;
        }

        private void Update()
        {
            angle = Mathf.MoveTowards(angle, IsOpen ? openAngle : 0f, degreesPerSecond * Time.deltaTime);
            hinge.localRotation = closedRotation * Quaternion.Euler(0f, angle, 0f);
        }
    }
}
