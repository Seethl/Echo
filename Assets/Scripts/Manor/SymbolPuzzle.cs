using Echo.Interactions;
using UnityEngine;

namespace Echo.Manor
{
    public sealed class SymbolPuzzle:MonoBehaviour
    {
        public string[] solution={"tree","moon","bird"};
        public string solvedFlag="library.cache";
        public void Submit(string symbol,PlayerInteractor player)
        {
            var progress=ManorProgress.Active;if(progress.Has(solvedFlag))return;
            int index=progress.Get("library.sequence");
            if(index>=solution.Length||solution[index]!=symbol){progress.Set("library.sequence",0);player.ui.ShowToast("Хибний порядок. Поточну комбінацію скинуто.");return;}
            index++;progress.Set("library.sequence",index);
            if(index==solution.Length){progress.Set(solvedFlag);player.ui.ShowToast("Механічна схованка відчинилася.");}
            else player.ui.ShowToast("Механізм прийняв символ: "+index+" / "+solution.Length);
        }
    }
}
