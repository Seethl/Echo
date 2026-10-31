using UnityEngine;

namespace Echo.Interactions
{
    // Add another component derived from this class for a radio, lever, etc.
    public abstract class Interactable : MonoBehaviour
    {
        public abstract string Prompt { get; }
        public virtual bool CanInteract(PlayerInteractor player) => isActiveAndEnabled;
        public abstract void Interact(PlayerInteractor player);
    }
}
