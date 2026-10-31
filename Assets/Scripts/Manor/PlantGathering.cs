using Echo.Interactions;
using UnityEngine;

namespace Echo.Manor
{
    public sealed class PlantGathering : Interactable
    {
        public string speciesId="moonflower",requiredSpecies="moonflower",itemId="remedy.flower";
        [TextArea]public string description="П’ять блідих пелюсток і супротивні листки.";
        public override string Prompt=>"E — оглянути рослину";
        private void Start(){var p=FindAnyObjectByType<PlayerInteractor>();if(p!=null&&speciesId==requiredSpecies&&p.HasKey(itemId))gameObject.SetActive(false);}
        public override void Interact(PlayerInteractor player)
        {
            if(speciesId!=requiredSpecies){player.ui.ShowToast(description+" Це не квітка з рецепта. J — записи.",5);return;}
            player.AddKey(itemId);player.ui.ShowToast("Отримано: бліда садова квітка. "+description,5);gameObject.SetActive(false);
        }
    }
}
