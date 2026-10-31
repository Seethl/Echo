using Echo.Interactions;
using UnityEngine;

namespace Echo.Manor
{
    public sealed class DoorProgressBinding:MonoBehaviour
    {
        public HingedDoor door;
        public string openFlag;
        private void Update(){if(ManorProgress.Active!=null&&ManorProgress.Active.Has(openFlag))door.UnlockAndOpen();}
    }
}
