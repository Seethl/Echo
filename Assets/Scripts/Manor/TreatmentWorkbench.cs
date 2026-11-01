using Echo.Interactions;
using Echo.Audio;
using UnityEngine;

namespace Echo.Manor
{
    public sealed class TreatmentWorkbench : Interactable
    {
        public string[] itemIds = { "remedy.base", "remedy.flower", "remedy.spoon" };
        public string[] missingMessages = { "Потрібна основа мікстури з аптечної шафки.", "Потрібна садова квітка: дивись замальовку в рецепті.", "Потрібна срібна ложечка. Пошукай серед посуду в холі." };
        public string recipeFlag = "library.cache", stepFlag = "remedy.steps", potionId = "remedy.potion";
        public GameObject preparedVisual;
        public bool requiresBottle;
        public string bottleId="remedy.bottle",bottledFlag="remedy.bottled";
        private float nextAction;
        private int Step => ManorProgress.Active.Get(stepFlag);
        public override string Prompt => requiresBottle&&Step>=itemIds.Length?"E — набрати ліки в пляшечку":Step == 0 ? "E — налити основу" : Step == 1 ? "E — додати квітку" : "E — розмішати срібною ложечкою";
        public override bool CanInteract(PlayerInteractor player) => base.CanInteract(player) && (Step < itemIds.Length || (requiresBottle&&!player.HasKey(potionId)));
        public override void Interact(PlayerInteractor player)
        {
            if (Time.time < nextAction) return;
            if (!ManorProgress.Active.Has(recipeFlag)) { player.ui.ShowToast("Спершу прочитай рецепт біля картини в кімнаті нагорі."); return; }
            int step = Step;
            if(requiresBottle&&step>=itemIds.Length)
            {
                if(!player.HasKey(bottleId)){player.ui.ShowToast("Ліки готові. Візьми будь-яку порожню пляшечку й набери їх із миски.",5);return;}
                ManorProgress.Active.Set(bottledFlag);player.AddKey(potionId);
                PrototypeAudio.Play(gameObject,PrototypeAudio.Cue.Water,.3f);
                player.ui.ShowToast("Ліки в пляшечці. Перелий їх у чашку мешканця в холі.",5);return;
            }
            if (!player.HasKey(itemIds[step])) { player.ui.ShowToast(missingMessages[step]); return; }
            nextAction = Time.time + .65f;
            ManorProgress.Active.Set(stepFlag, step + 1);
            PrototypeAudio.Play(gameObject, PrototypeAudio.Cue.Click, .3f);
            NoiseSystem.Emit(transform.position, 3, gameObject, NoiseSystem.Kind.Interaction, .35f, player.gameObject);
            if (step + 1 >= itemIds.Length)
            {
                if(requiresBottle){player.ui.ShowToast("Ліки готові. Набери їх у порожню пляшечку: E біля миски.",5);return;}
                player.AddKey(potionId);
                player.ui.ShowToast("Мікстура готова. Залиши її в чашці мешканця у холі та відійди за укриття.", 6);
            }
            else player.ui.ShowToast(step == 0 ? "Основу налито. Тепер додай квітку." : "Квітку додано. Залишилося розмішати.");
        }
        private void Update() { if (preparedVisual != null && ManorProgress.Active != null) preparedVisual.SetActive(Step >= itemIds.Length && (!requiresBottle||!ManorProgress.Active.Has(bottledFlag))); }
    }
}
