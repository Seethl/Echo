using Echo.Interactions;
using UnityEngine;

namespace Echo.Manor
{
    public sealed class ToolBarrier:Interactable
    {
        public string toolId="tool.crowbar",counterId="yard.hits",openFlag="yard.open";
        public string missingMessage="Потрібен лом",actionName="відтиснути засув";
        public int requiredActions=3;
        [Min(.1f)] public float actionCooldown=.8f;
        public float noiseRadius=16f;
        public AudioClip actionClip,finalClip;
        private float nextAction;
        public override string Prompt=>"E — "+actionName+" ("+(ManorProgress.Active.Get(counterId)+1)+" / "+requiredActions+")";
        public override bool CanInteract(PlayerInteractor player)=>base.CanInteract(player)&&!ManorProgress.Active.Has(openFlag);
        private void Start(){if(ManorProgress.Active.Has(openFlag))gameObject.SetActive(false);}
        public override void Interact(PlayerInteractor player)
        {
            if(!player.HasKey(toolId)){player.ui.ShowToast(missingMessage);return;}
            if(Time.time<nextAction)return;nextAction=Time.time+actionCooldown;
            var progress=ManorProgress.Active;int hits=progress.Get(counterId)+1;progress.Set(counterId,hits);
            bool final=hits>=requiredActions;
            NoiseSystem.Emit(transform.position,noiseRadius*(final?1.3f:1),gameObject,NoiseSystem.Kind.Mechanism);
            Echo.Audio.PrototypeAudio.Play(gameObject,Echo.Audio.PrototypeAudio.Cue.Thud,final?1:.6f,final?finalClip:actionClip);
            player.ui.ShowToast(actionName+": "+hits+" / "+requiredActions);
            if(hits>=requiredActions){progress.Set(openFlag);player.ui.ShowToast("Прохід відкрито.");gameObject.SetActive(false);}
        }
    }
}
