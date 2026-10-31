using Echo.Interactions;
using UnityEngine;

namespace Echo.Manor
{
    public sealed class KeypadLock:Interactable
    {
        public string code="417";
        public bool useRunCode;
        public string EffectiveCode=>useRunCode&&ManorProgress.Active!=null?ManorProgress.Active.GalleryCode:code;
        public string unlockFlag="hall.open";
        public MechanicalLockView physicalView;
        private bool unlocked;
        public bool IsUnlocked=>unlocked||(ManorProgress.Active!=null&&!string.IsNullOrEmpty(unlockFlag)&&ManorProgress.Active.Has(unlockFlag));
        public override bool CanInteract(PlayerInteractor player)=>base.CanInteract(player)&&(physicalView==null||!IsUnlocked);
        private readonly int[] digits=new int[3];
        private string feedback="Три механічні коліщатка. Код міститься в службовому записі.";
        public override string Prompt=>"E — ввести код";
        public override void Interact(PlayerInteractor player)
        {
            if(physicalView!=null){physicalView.Open(player);return;}
            player.ui.OpenPanel(this,"МЕХАНІЧНИЙ ЗАМОК",()=>string.Join(" — ",digits)+"\n"+feedback,new[]{
                new InteractionUI.Choice("Перша цифра +1",()=>digits[0]=(digits[0]+1)%10),
                new InteractionUI.Choice("Друга цифра +1",()=>digits[1]=(digits[1]+1)%10),
                new InteractionUI.Choice("Третя цифра +1",()=>digits[2]=(digits[2]+1)%10),
                new InteractionUI.Choice("Підтвердити",()=>Submit(player)),new InteractionUI.Choice("Назад",()=>player.ui.ClosePanel())});
        }
        public bool Submit(PlayerInteractor player)
        {
            if(string.Concat(digits)!=EffectiveCode){feedback="Код не підходить. Послухайте поточну службову передачу; записки містять її частоту.";return false;}
            unlocked=true;
            if(ManorProgress.Active!=null)ManorProgress.Active.Set(unlockFlag);
            if(player!=null&&player.ui!=null){player.ui.ClosePanel();player.ui.ShowToast("Замок відкрито.");}return true;
        }
        public int GetDigit(int index)=>digits[Mathf.Clamp(index,0,2)];
        public void StepDigit(int index,int direction){if(index>=0&&index<3)digits[index]=(digits[index]+(direction>0?1:9))%10;}
        public void SetDigits(int a,int b,int c){digits[0]=Mathf.Clamp(a,0,9);digits[1]=Mathf.Clamp(b,0,9);digits[2]=Mathf.Clamp(c,0,9);}
    }
}
