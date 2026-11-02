using Echo.Interactions;
using UnityEngine;

namespace Echo.Manor
{
    public sealed class DoorProgressBinding:MonoBehaviour
    {
        public HingedDoor door;
        public string openFlag;
        private bool applied;
        private void Start()
        {
            // A restored closed door is an intentional checkpoint state.
            applied=door!=null&&door.HasRestoredState&&ManorProgress.Active!=null&&ManorProgress.Active.Has(openFlag);
        }
        private void Update()
        {
            if(applied||door==null||ManorProgress.Active==null||!ManorProgress.Active.Has(openFlag))return;
            applied=true;door.UnlockAndOpen();
        }
    }
}
