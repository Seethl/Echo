using UnityEngine;

namespace Echo.Interactions
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class EpisodeExit : MonoBehaviour
    {
        private bool completed;
        public string completionMessage="Шлях відкрито";
        private void OnTriggerEnter(Collider other)
        {
            if (completed) return;
            var player = other.GetComponentInParent<PlayerInteractor>();
            if (player == null || player.ui == null) return;
            var life = player.GetComponent<PlayerCaught>();
            if (life != null && life.IsCaught) return;
            if (player.ui.ShowCompletion(completionMessage)) completed = true;
        }
    }
}
