using System.Collections.Generic;
using Echo.Menu;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Echo.Interactions
{
    [DefaultExecutionOrder(-9000)]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        public Camera viewCamera;
        public InteractionUI ui;
        [Min(0.1f)] public float distance = 2.5f;
        public LayerMask interactionLayers = ~0;
        private readonly HashSet<string> keys = new HashSet<string>();
        public Interactable Target { get; private set; }
        public bool HasKey(string id) => !string.IsNullOrWhiteSpace(id) && keys.Contains(id);
        public bool AddKey(string id) => !string.IsNullOrWhiteSpace(id) && keys.Add(id);

        private void Awake()
        {
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
        }

        private void Update()
        {
            Target = null;
            if (ui == null || viewCamera == null) return;
            var life = GetComponent<PlayerCaught>();
            if (GameMenu.BlocksPlayerInput || Time.timeScale <= 0f || (life != null && life.IsCaught))
            {
                ui.SetPrompt(null);
                return;
            }

            // The FIRST solid hit wins. Never search past a wall or a closed door.
            if (Physics.Raycast(viewCamera.transform.position, viewCamera.transform.forward,
                out var hit, distance, interactionLayers, QueryTriggerInteraction.Ignore))
            {
                var candidate = hit.collider.GetComponentInParent<Interactable>();
                if (candidate != null && candidate.CanInteract(this)) Target = candidate;
            }
            ui.SetPrompt(Target != null ? Target.Prompt : null);
            if (Target != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                Target.Interact(this);
        }
    }
}
