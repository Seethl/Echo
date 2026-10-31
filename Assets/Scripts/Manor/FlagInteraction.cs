using Echo.Interactions;
using UnityEngine;

namespace Echo.Manor
{
    public sealed class FlagInteraction:Interactable
    {
        public string prompt="E — оглянути";
        public string setFlag,requiredFlag,missingMessage,doneMessage;
        public bool hideWhenDone;
        public string requiredKeyId;
        public GameObject completedVisual;
        public float noiseRadius;
        public Vector3 allowedSide;
        public override string Prompt=>prompt;
        public override bool CanInteract(PlayerInteractor p)=>base.CanInteract(p)&&!ManorProgress.Active.Has(setFlag)&&
            (allowedSide==Vector3.zero||Vector3.Dot(p.transform.position-transform.position,allowedSide)>0);
        public override void Interact(PlayerInteractor p)
        {
            var progress=ManorProgress.Active;
            if(!string.IsNullOrEmpty(requiredKeyId)&&!p.HasKey(requiredKeyId)){p.ui.ShowToast(missingMessage);return;}
            if(!string.IsNullOrEmpty(requiredFlag)&&!progress.Has(requiredFlag)){p.ui.ShowToast(missingMessage);return;}
            progress.Set(setFlag);p.ui.ShowToast(doneMessage);
            if(completedVisual!=null)completedVisual.SetActive(true);
            if(noiseRadius>0){NoiseSystem.Emit(transform.position,noiseRadius,gameObject,NoiseSystem.Kind.Interaction,1,p.gameObject);Echo.Audio.PrototypeAudio.Play(gameObject,Echo.Audio.PrototypeAudio.Cue.Click,.5f);}
            if(hideWhenDone)gameObject.SetActive(false);
        }
        private void Start(){bool done=ManorProgress.Active.Has(setFlag);if(completedVisual!=null)completedVisual.SetActive(done);if(hideWhenDone&&done)gameObject.SetActive(false);}
    }
}
