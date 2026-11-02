using Echo.Interactions;
using UnityEngine;

namespace Echo.Manor
{
    public sealed class PlantGathering : Interactable
    {
        public string speciesId="moonflower",requiredSpecies="moonflower",itemId="remedy.flower";
        [TextArea]public string description="П’ять блідих пелюсток і супротивні листки.";
        public string pickedFlag;
        private PlayerInteractor owner;
        public string displayName="садова квітка",pickupPrompt="E — зірвати квітку";
        public override string Prompt=>speciesId==requiredSpecies?(owner!=null&&owner.HasKey(itemId)?"Ромашка вже є в інвентарі":pickupPrompt):"E — оглянути рослину";
        public override bool CanInteract(PlayerInteractor p)=>base.CanInteract(p);
        private void Start(){
            // Picking volumes must not become invisible barriers to walking.
            foreach(var c in GetComponentsInChildren<Collider>())
            {foreach(var actor in FindObjectsByType<FPSController>())foreach(var other in actor.GetComponentsInChildren<Collider>())Physics.IgnoreCollision(c,other);
             foreach(var actor in FindObjectsByType<EnemyInvestigator>())foreach(var other in actor.GetComponentsInChildren<Collider>())Physics.IgnoreCollision(c,other);}
            owner=FindAnyObjectByType<PlayerInteractor>();if(!string.IsNullOrEmpty(pickedFlag)&&ManorProgress.Active!=null&&ManorProgress.Active.Has(pickedFlag))gameObject.SetActive(false);}
        public override void Interact(PlayerInteractor player)
        {
            if(speciesId!=requiredSpecies){player.ui.ShowToast(description+" Це не квітка з рецепта. J — записи.",5);return;}
            if(!CanInteract(player))return;
            if(player.HasKey(itemId)){player.ui.ShowToast("Для ліків достатньо однієї ромашки. Вона вже є в інвентарі.");return;}
            if(!string.IsNullOrEmpty(pickedFlag))ManorProgress.Active?.Set(pickedFlag);
            player.AddKey(itemId);player.ui.ShowToast("Отримано: "+displayName+". "+description,5);gameObject.SetActive(false);
        }
    }
}
