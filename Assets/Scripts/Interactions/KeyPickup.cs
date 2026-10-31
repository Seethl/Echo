using UnityEngine;

namespace Echo.Interactions
{
    public sealed class KeyPickup : Interactable
    {
        public string keyId = "manor.service-passage";
        public string displayName = "службовий ключ";
        public string pickupPrompt = "E — забрати ключ";
        public override string Prompt => pickupPrompt;
        private void Start()
        {
            var player = FindAnyObjectByType<PlayerInteractor>();
            if(player != null && player.HasKey(keyId)) gameObject.SetActive(false);
        }
        public override bool CanInteract(PlayerInteractor player) => base.CanInteract(player) &&
            !string.IsNullOrWhiteSpace(keyId);

        public override void Interact(PlayerInteractor player)
        {
            player.AddKey(keyId);
            gameObject.SetActive(false);
            player.ui.ShowToast("Отримано: " + displayName);
        }
    }
}
