using Echo.Interactions;
using UnityEngine;

namespace Echo.Manor
{
    public sealed class KeypadLock:Interactable
    {
        public string code="417";
        public bool useRunCode;
        public string EffectiveCode=>useRunCode?ManorProgress.Active.GalleryCode:code;
        public string unlockFlag="hall.open";
        private readonly int[] digits=new int[3];
        private string feedback="Три механічні коліщатка. Код міститься в службовому записі.";
        public override string Prompt=>"E — ввести код";
        public override void Interact(PlayerInteractor player)
        {
            player.ui.OpenPanel(this,"МЕХАНІЧНИЙ ЗАМОК",()=>string.Join(" — ",digits)+"\n"+feedback,new[]{
                new InteractionUI.Choice("Перша цифра +1",()=>digits[0]=(digits[0]+1)%10),
                new InteractionUI.Choice("Друга цифра +1",()=>digits[1]=(digits[1]+1)%10),
                new InteractionUI.Choice("Третя цифра +1",()=>digits[2]=(digits[2]+1)%10),
                new InteractionUI.Choice("Підтвердити",()=>Submit(player)),new InteractionUI.Choice("Назад",()=>player.ui.ClosePanel())});
        }
        public bool Submit(PlayerInteractor player)
        {
            if(string.Concat(digits)!=EffectiveCode){feedback="Код не підходить. Послухайте поточну службову передачу; записки містять її частоту.";return false;}
            ManorProgress.Active.Set(unlockFlag);player.ui.ClosePanel();player.ui.ShowToast("Замок відкрито. У холі хтось ходить. Рухайтеся тихо й тримайтеся укриттів.");return true;
        }
        public void SetDigits(int a,int b,int c){digits[0]=Mathf.Clamp(a,0,9);digits[1]=Mathf.Clamp(b,0,9);digits[2]=Mathf.Clamp(c,0,9);}
    }
}
