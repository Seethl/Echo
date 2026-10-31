using UnityEngine;

namespace Echo.Interactions
{
    public sealed class NoteInteractable : Interactable
    {
        [TextArea(3, 8)] public string text = "Запасний ключ від службового проходу — у шафці біля дверей";
        public override string Prompt => "E — прочитати";
        public override void Interact(PlayerInteractor player) => player.ui.ShowNote(text);
    }
}
