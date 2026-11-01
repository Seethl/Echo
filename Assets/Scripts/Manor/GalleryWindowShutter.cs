using System.Linq;
using Echo.Audio;
using Echo.Interactions;
using UnityEngine;

namespace Echo.Manor
{
    public sealed class GalleryWindowShutter : Interactable
    {
        public string closedFlag;
        public string[] allWindowFlags;
        public string quietFlag = "gallery.quiet";
        public Transform leftHinge, rightHinge;
        public float openAngle = 110f;
        [Min(.1f)] public float closingSeconds = .8f;
        private float openness = 1f;
        private bool closing;
        private PlayerInteractor actor;
        public bool Closed => ManorProgress.Active != null && ManorProgress.Active.Has(closedFlag);
        public bool IsMoving => closing;
        public float OpenAmount => openness;
        public override string Prompt => "E — закрити й зафіксувати віконниці";
        public override bool CanInteract(PlayerInteractor player) => base.CanInteract(player) && !Closed && !closing;
        private void Start() { openness = Closed ? 0 : 1; ApplyPose(); }
        public override void Interact(PlayerInteractor player)
        {
            if (!CanInteract(player)) return;
            actor = player; closing = true;
            PrototypeAudio.Play(gameObject, PrototypeAudio.Cue.Creak, .35f);
            NoiseSystem.Emit(transform.position, 4, gameObject, NoiseSystem.Kind.Interaction, .4f, player.gameObject);
        }
        private void Update()
        {
            if (!closing || Time.timeScale <= 0) return;
            openness = Mathf.MoveTowards(openness, 0, Time.deltaTime / closingSeconds); ApplyPose();
            if (openness > 0) return;
            closing = false;
            var progress = ManorProgress.Active; progress.Set(closedFlag);
            bool quiet = allWindowFlags != null && allWindowFlags.Length > 0 && allWindowFlags.All(progress.Has);
            if (quiet) progress.Set(quietFlag);
            PrototypeAudio.Play(gameObject, PrototypeAudio.Cue.Click, .4f);
            if (actor != null) actor.ui.ShowToast(quiet
                ? "Усі віконниці зафіксовані. Стало тихо — послухай передачу 940."
                : "Віконниці зафіксовані. Ще чути гуркіт інших вікон.");
        }
        private void ApplyPose()
        {
            if (leftHinge != null) leftHinge.localRotation = Quaternion.Euler(0, -openAngle * openness, 0);
            if (rightHinge != null) rightHinge.localRotation = Quaternion.Euler(0, openAngle * openness, 0);
        }
    }
}
