using Echo.Interactions;
using UnityEngine;

namespace Echo.Manor
{
    // One shared inventory token; only the bottle actually taken disappears from the room.
    public sealed class RemedyBottle : Interactable, ICheckpointObject
    {
        public string bottleId;
        public string itemId="remedy.bottle";
        public string CheckpointId=>bottleId;
        public override string Prompt=>"E — взяти порожню пляшечку";
        public override bool CanInteract(PlayerInteractor player)=>base.CanInteract(player)&&!player.HasKey(itemId);
        public override void Interact(PlayerInteractor player)
        {
            if(!CanInteract(player))return;
            gameObject.SetActive(false);player.AddKey(itemId);
            player.ui.ShowToast("Порожня пляшечка. У неї можна набрати готові ліки на кухні.");
            ManorProgress.Active?.RefreshCheckpoint();
        }
        public string CaptureState()=>gameObject.activeSelf?"present":"taken";
        public void RestoreState(string state)=>gameObject.SetActive(state!="taken");
    }
}
