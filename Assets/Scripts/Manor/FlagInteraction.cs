using Echo.Interactions;
using UnityEngine;

namespace Echo.Manor
{
    public sealed class FlagInteraction:Interactable
    {
        public string prompt="E — оглянути";
        public string setFlag,requiredFlag,missingMessage,doneMessage;
        public bool hideWhenDone;
        public Vector3 allowedSide;
        public override string Prompt=>prompt;
        public override bool CanInteract(PlayerInteractor p)=>base.CanInteract(p)&&!ManorProgress.Active.Has(setFlag)&&
            (allowedSide==Vector3.zero||Vector3.Dot(p.transform.position-transform.position,allowedSide)>0);
        public override void Interact(PlayerInteractor p)
        {
            var progress=ManorProgress.Active;
            if(!string.IsNullOrEmpty(requiredFlag)&&!progress.Has(requiredFlag)){p.ui.ShowToast(missingMessage);return;}
            progress.Set(setFlag);p.ui.ShowToast(doneMessage);
            if(hideWhenDone)gameObject.SetActive(false);
        }
        private void Start(){if(hideWhenDone&&ManorProgress.Active.Has(setFlag))gameObject.SetActive(false);}
    }
}
