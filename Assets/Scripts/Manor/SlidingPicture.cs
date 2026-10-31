using Echo.Interactions;
using Echo.Audio;
using UnityEngine;

namespace Echo.Manor
{
    // Move the visual AND its solid collider. The key behind it uses normal first-hit interaction.
    public sealed class SlidingPicture : Interactable
    {
        public Transform panel;
        public Vector3 closedPosition, openOffset = Vector3.right * 1.35f;
        public float travelSeconds = 1.1f;
        public string progressFlag = "study.picture";
        public override string Prompt => "E — відсунути картину";
        public override bool CanInteract(PlayerInteractor player) => base.CanInteract(player) && !ManorProgress.Active.Has(progressFlag);
        private void Start() { if (ManorProgress.Active.Has(progressFlag)) panel.localPosition = closedPosition + openOffset; }
        public override void Interact(PlayerInteractor player)
        {
            ManorProgress.Active.Set(progressFlag);
            PrototypeAudio.Play(gameObject, PrototypeAudio.Cue.Creak, .35f);
            player.ui.ShowToast("За картиною — невелика ніша.");
        }
        private void Update()
        {
            if (ManorProgress.Active == null || panel == null) return;
            Vector3 target = closedPosition + (ManorProgress.Active.Has(progressFlag) ? openOffset : Vector3.zero);
            panel.localPosition = Vector3.MoveTowards(panel.localPosition, target, openOffset.magnitude / Mathf.Max(.1f, travelSeconds) * Time.deltaTime);
        }
    }
}
