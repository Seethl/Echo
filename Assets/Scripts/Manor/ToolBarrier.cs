using Echo.Interactions;
using Echo.Menu;
using UnityEngine;
using UnityEngine.InputSystem;

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
        [Min(0),Tooltip("Zero retains the original single-press action.")]
        public float holdSeconds;
        public string requiredFlag,preparationMessage="Спочатку підготуйте механізм.";
        private float nextAction;
        private PlayerInteractor worker;
        private float started=-1,nextWorkSound;
        private bool requireRelease;
        public bool Working=>started>=0;
        public float HoldProgress=>Working&&holdSeconds>0?Mathf.Clamp01((Time.time-started)/holdSeconds):0;
        public override string Prompt=>(holdSeconds>0?"Утримуй E — ":"E — ")+actionName+" ("+(ManorProgress.Active.Get(counterId)+1)+" / "+requiredActions+")"+(Working?" · "+Mathf.RoundToInt(HoldProgress*100)+"%":"");
        public override bool CanInteract(PlayerInteractor player)=>base.CanInteract(player)&&!ManorProgress.Active.Has(openFlag);
        private void Start(){if(ManorProgress.Active.Has(openFlag))gameObject.SetActive(false);}
        public override void Interact(PlayerInteractor player)
        {
            if(!player.HasKey(toolId)){player.ui.ShowToast(missingMessage);return;}
            if(!string.IsNullOrEmpty(requiredFlag)&&!ManorProgress.Active.Has(requiredFlag)){player.ui.ShowToast(preparationMessage,4);return;}
            if(Time.time<nextAction||requireRelease)return;
            if(holdSeconds>0)
            {
                worker=player;started=Time.time;nextWorkSound=Time.time;
                player.ui.ShowToast("Тримай E та дивись на ланцюг. Відпусти або відійди, щоб сховатися.",4);return;
            }
            Complete(player);
        }
        private void Update()
        {
            bool held=Keyboard.current!=null&&Keyboard.current.eKey.isPressed;
            if(!held)requireRelease=false;
            if(!Working)return;
            if(worker==null||!held||GameMenu.BlocksPlayerInput||Time.timeScale<=0||worker.Target!=this)
            {started=-1;worker=null;return;}
            if(Time.time>=nextWorkSound)
            {
                nextWorkSound=Time.time+1;
                Echo.Audio.PrototypeAudio.Play(gameObject,Echo.Audio.PrototypeAudio.Cue.Creak,.4f,actionClip);
                NoiseSystem.Emit(transform.position,9,gameObject,NoiseSystem.Kind.Mechanism,.65f,worker.gameObject);
            }
            if(HoldProgress>=1)
            {var player=worker;started=-1;worker=null;requireRelease=true;Complete(player);}
        }
        private void OnDisable(){started=-1;worker=null;}
        private void Complete(PlayerInteractor player)
        {
            nextAction=Time.time+actionCooldown;
            var progress=ManorProgress.Active;int hits=progress.Get(counterId)+1;progress.Set(counterId,hits);
            bool final=hits>=requiredActions;
            NoiseSystem.Emit(transform.position,noiseRadius*(final?1.3f:1),gameObject,NoiseSystem.Kind.Mechanism,1,player.gameObject);
            Echo.Audio.PrototypeAudio.Play(gameObject,Echo.Audio.PrototypeAudio.Cue.Thud,final?1:.6f,final?finalClip:actionClip);
            player.ui.ShowToast(actionName+": "+hits+" / "+requiredActions);
            if(hits>=requiredActions){progress.Set(openFlag);player.ui.ShowToast("Прохід відкрито.");gameObject.SetActive(false);}
        }
    }
}
