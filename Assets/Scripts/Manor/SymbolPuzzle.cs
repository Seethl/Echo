using System;
using Echo.Audio;
using Echo.Interactions;
using Echo.Radio;
using UnityEngine;

namespace Echo.Manor
{
    public sealed class SymbolPuzzle:MonoBehaviour
    {
        public string[] solution={"tree","moon","bird"};
        public bool useRunOrder;
        public string[] ActiveSolution=>useRunOrder?RunSecrets.Order(ManorProgress.Active.Get(RunSecrets.LibraryKey)):solution;
        public string[] symbolIds={"moon","tree","bird"};
        public string[] symbolNames={"Місяць","Дерево","Птах"};
        public string solvedFlag="library.cache";
        public TextMesh inputDisplay;
        public float feedbackSeconds=3;
        public int EnteredCount=>ManorProgress.Active.Get("library.sequence");
        public bool Solved=>ManorProgress.Active.Has(solvedFlag);
        public string Feedback {get;private set;}="Почуй послідовність. Повтори її символами.";
        private float busyUntil,feedbackUntil;
        private SymbolButton[] buttons;
        private AudioClip transmission;
        private string transmissionKey;
        private void Awake()=>buttons=FindObjectsByType<SymbolButton>();
        private SymbolButton Panel(string id)=>Array.Find(buttons,b=>b.puzzle==this&&b.demonstration&&b.symbolId==id);
        public string TransmissionText
        {
            get
            {
                var order=ActiveSolution;var sounds=new string[order.Length];
                for(int i=0;i<order.Length;i++){var panel=Panel(order[i]);sounds[i]=panel!=null?panel.rhythm:"невідомий звук";}
                return string.Join(" → ",sounds);
            }
        }
        public AudioClip TransmissionClip
        {
            get
            {
                string key=string.Join(".",ActiveSolution);
                if(transmission!=null&&transmissionKey==key)return transmission;
                var clips=new AudioClip[ActiveSolution.Length];
                for(int i=0;i<clips.Length;i++){var panel=Panel(ActiveSolution[i]);clips[i]=panel!=null?panel.SoundClip:PrototypeAudio.Clip(PrototypeAudio.Cue.Click);}
                transmissionKey=key;transmission=PrototypeAudio.Sequence("library."+key,clips,.45f);return transmission;
            }
        }

        public string Slots
        {
            get
            {
                var slots=new string[solution.Length];
                for(int i=0;i<slots.Length;i++)
                {
                    int value=ManorProgress.Active.Get("library.input."+i)-1;
                    slots[i]=i<EnteredCount&&value>=0&&value<symbolNames.Length?"["+symbolNames[value]+"]":"[ — ]";
                }
                return string.Join("  ",slots);
            }
        }
        private void Update()
        {
            if(inputDisplay==null||ManorProgress.Active==null)return;
            inputDisplay.text=Solved?"ВІДЧИНЕНО\nЗаберіть руків’я зі схованки":Slots+"\n"+(Time.time<feedbackUntil?Feedback:"Введіть усі символи. «Скинути» — очистити спробу.");
            inputDisplay.color=Solved?new Color(.55f,1,.72f):Color.white;
        }
        public bool TryDemonstrate(SymbolButton button,PlayerInteractor player)
        {
            if(Time.time<busyUntil){player.ui.ShowToast("Дослухайте звук, потім натисніть іншу панель.");return false;}
            var clip=button.SoundClip;
            busyUntil=Time.time+clip.length+.15f;
            QuietRadio(clip.length+.2f);
            PrototypeAudio.Play(button.gameObject,PrototypeAudio.Cue.Click,.8f,clip);
            NoiseSystem.Emit(button.transform.position,10,button.gameObject,NoiseSystem.Kind.Mechanism,1,player.gameObject);
            bool first=!ManorProgress.Active.Has("library.panels.learned");
            ManorProgress.Active.Set("library.panels.learned");
            player.ui.ShowToast(button.symbolName+": "+button.rhythm+(first?". Зістав ці звуки з радіозаписом.":"."),4);
            return true;
        }
        public void Submit(string symbol,PlayerInteractor player)
        {
            if(Solved)return;
            if(Time.time<busyUntil){player.ui.ShowToast("Дочекайтеся кінця звуку.");return;}
            int value=Array.IndexOf(symbolIds,symbol);
            if(value<0)return;
            QuietRadio(.3f);
            var progress=ManorProgress.Active;
            int count=Mathf.Clamp(EnteredCount,0,solution.Length-1);
            progress.Set("library.input."+count,value+1);
            progress.Set("library.sequence",count+1);
            Feedback="Символ додано: "+(count+1)+" / "+solution.Length;feedbackUntil=Time.time+feedbackSeconds;
            if(count+1<solution.Length)
            {
                PrototypeAudio.Play(gameObject,PrototypeAudio.Cue.Click,.5f);
                player.ui.ShowToast(Feedback);
            }
            else
            {
                bool correct=true;
                var expected=ActiveSolution;
                for(int i=0;i<expected.Length;i++)
                {int index=progress.Get("library.input."+i)-1;if(index<0||index>=symbolIds.Length||symbolIds[index]!=expected[i])correct=false;}
                if(correct)
                {
                    progress.Set(solvedFlag);Feedback="Схованка відчинилася. Заберіть руків’я.";
                    PrototypeAudio.Play(gameObject,PrototypeAudio.Cue.DoubleClick,.8f);
                }
                else
                {
                    ClearInput();Feedback="Порядок не збігся. Спробу очищено; зіставте всі три звуки.";
                    PrototypeAudio.Play(gameObject,PrototypeAudio.Cue.Thud,.45f);
                }
                feedbackUntil=Time.time+feedbackSeconds;player.ui.ShowToast(Feedback,4);
            }
            NoiseSystem.Emit(player.transform.position,4,player.gameObject,NoiseSystem.Kind.Interaction);
        }
        private void ClearInput()
        {
            var progress=ManorProgress.Active;progress.Set("library.sequence",0);
            for(int i=0;i<solution.Length;i++)progress.Set("library.input."+i,0);
        }
        public void ResetInput(PlayerInteractor player)
        {
            if(Solved)return;
            ClearInput();Feedback="Введення скинуто. Можна повторити передачу 1060.";
            feedbackUntil=Time.time+feedbackSeconds;player.ui.ShowToast(Feedback);
        }
        private void QuietRadio(float seconds)
        {
            foreach(var radio in FindObjectsByType<PortableRadio>())
                if(Vector3.Distance(radio.transform.position,transform.position)<12)radio.SuspendForCue(seconds);
        }
    }
}
