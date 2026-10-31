using Echo.Interactions;
using UnityEngine;

namespace Echo.Manor
{
    // The built-in font material does not reliably depth-test; keep world guidance local and occluded.
    [RequireComponent(typeof(TextMesh))]
    public sealed class WorldLabelVisibility : MonoBehaviour
    {
        public float distance=3.8f;
        private PlayerInteractor player;
        private Renderer display;
        private void Start(){player=FindAnyObjectByType<PlayerInteractor>();display=GetComponent<Renderer>();}
        private void LateUpdate()
        {
            if(player==null||display==null)return;
            Vector3 origin=player.viewCamera.transform.position;bool visible=(origin-transform.position).sqrMagnitude<distance*distance;
            if(visible&&Physics.Linecast(origin,transform.position,out var hit,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
            {
                var owner=GetComponentInParent<Interactable>();visible=owner!=null&&hit.collider.GetComponentInParent<Interactable>()==owner;
            }
            display.enabled=visible;
        }
    }
}
