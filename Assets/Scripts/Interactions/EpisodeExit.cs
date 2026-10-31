using UnityEngine;

namespace Echo.Interactions
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class EpisodeExit : MonoBehaviour
    {
        private bool completed;
        private void OnTriggerEnter(Collider other)
        {
            if (completed) return;
            var player = other.GetComponentInParent<PlayerInteractor>();
            if (player == null || player.ui == null) return;
            if (player.ui.ShowCompletion()) completed = true;
        }
    }
}
