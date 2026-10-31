using Echo.Interactions;
using UnityEngine;

namespace Echo.Manor
{
    public sealed class ClueNote:Interactable
    {
        public string clueId,title="СЛУЖБОВА КАРТКА";
        [TextArea(3,8)] public string text;
        public string fullTextFlag;
        [TextArea(3,8)] public string fullText;
        public int frequency;
        public Echo.Radio.RadioChannel radioChannel;
        public override string Prompt=>"E — прочитати";
        public override void Interact(PlayerInteractor player)
        {
            var progress=ManorProgress.Active;
            string content=!string.IsNullOrEmpty(fullTextFlag)&&progress.Has(fullTextFlag)?fullText:text;
            int tunedFrequency=radioChannel!=null?radioChannel.frequency:frequency;
            content=content.Replace("{frequency}",tunedFrequency.ToString());
            progress.AddClue(clueId,content);
            if(tunedFrequency>0)progress.Set("frequency."+tunedFrequency);
            player.ui.OpenPanel(this,title,()=>content,new[]{new InteractionUI.Choice("Закрити",()=>player.ui.ClosePanel())});
        }
    }
}
