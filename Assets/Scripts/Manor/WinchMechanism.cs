using Echo.Interactions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Echo.Manor
{
    public sealed class WinchMechanism:Interactable
    {
        public string handleId="tool.winch";
        public int cyclesRequired=3;
        public float clickLeadTime=2.4f;
        [Range(1,3)] public float releaseWindow=1.5f;
        public Transform grate;
        public UnityEngine.AI.NavMeshObstacle navigationBlocker;
        public float raisedHeight=3.3f;
        private Vector3 closedPosition;
        private PlayerInteractor user;
        private float started=-1;
        private bool heldLast,failedUntilRelease;
        private bool doublePlayed;
        private float nextClick;
        public AudioClip singleClickClip,doubleClickClip,failureClip;
        public int Steps=>ManorProgress.Active.Get("winch.steps");
        public bool InReleaseWindow=>started>=0&&Time.time-started>=clickLeadTime&&Time.time-started<=clickLeadTime+releaseWindow;
        public override string Prompt=>"E — "+(ManorProgress.Active.Has("winch.installed")?"працювати з лебідкою":"встановити руків’я");
        private void Awake(){closedPosition=grate.position;}
        public override void Interact(PlayerInteractor player)
        {
            user=player;var progress=ManorProgress.Active;
            if(!progress.Has("winch.installed"))
            {if(!player.HasKey(handleId)){player.ui.ShowToast("Порожнє гніздо. Потрібне руків’я зі схованки в бібліотеці.",4);return;}progress.Set("winch.installed");}
            player.ui.OpenPanel(this,"РУЧНА ЛЕБІДКА",Body,new[]{new InteractionUI.Choice("Відійти",()=>player.ui.ClosePanel())},false,()=>{started=-1;heldLast=false;failedUntilRelease=false;});
        }
        private string Body()=>"Утримуй Space. Після подвійного клацання ВІДПУСТИ Space.\n"+
            "Зафіксовано ступенів: "+Steps+" / "+cyclesRequired+"\n"+(InReleaseWindow?"ПОДВІЙНЕ КЛАЦАННЯ — відпусти зараз":failedUntilRelease?"Відпусти Space, щоб почати нову спробу":started>=0?"Одиночні клацання…":"Крути до подвійного клацання; дай стопору сісти в паз.");
        private void Update()
        {
            int steps=Steps;float raise=steps>=cyclesRequired?raisedHeight:Mathf.Min(.8f,steps*.4f);
            grate.position=Vector3.MoveTowards(grate.position,closedPosition+Vector3.up*raise,Time.deltaTime*2f);
            if(navigationBlocker!=null)navigationBlocker.enabled=steps<cyclesRequired||grate.position.y-closedPosition.y<2.8f;
            if(user==null||!user.ui.IsCustomOpen(this)||Time.timeScale<=0)return;
            bool held=Keyboard.current!=null&&Keyboard.current.spaceKey.isPressed;
            if(held&&!heldLast&&!failedUntilRelease&&steps<cyclesRequired){started=Time.time;nextClick=Time.time+.65f;doublePlayed=false;}
            if(started>=0&&held&&!doublePlayed&&Time.time-started>=clickLeadTime)
            {doublePlayed=true;Echo.Audio.PrototypeAudio.Play(gameObject,Echo.Audio.PrototypeAudio.Cue.DoubleClick,1,doubleClickClip);NoiseSystem.Emit(transform.position,8,gameObject,NoiseSystem.Kind.Mechanism,1,user.gameObject);}
            else if(started>=0&&held&&!doublePlayed&&Time.time>=nextClick)
            {nextClick=Time.time+.65f;Echo.Audio.PrototypeAudio.Play(gameObject,Echo.Audio.PrototypeAudio.Cue.Click,.5f,singleClickClip);NoiseSystem.Emit(transform.position,6,gameObject,NoiseSystem.Kind.Mechanism,1,user.gameObject);}
            if(started>=0&&held&&Time.time-started>clickLeadTime+releaseWindow)Fail();
            if(!held&&heldLast)
            {
                if(InReleaseWindow&&!failedUntilRelease)
                {ManorProgress.Active.Set("winch.steps",steps+1);Echo.Audio.PrototypeAudio.Play(gameObject,Echo.Audio.PrototypeAudio.Cue.Click,.8f);user.ui.ShowToast("Стопор зафіксовано: "+(steps+1)+" / "+cyclesRequired);if(steps+1>=cyclesRequired){ManorProgress.Active.Set("grate.open");user.ui.ShowToast("Решітку піднято. Закрийте панель і пройдіть у зимовий сад.",4);}started=-1;}
                else if(started>=0&&!failedUntilRelease)Fail();
                failedUntilRelease=false;
            }
            heldLast=held;
        }
        private void Fail(){started=-1;failedUntilRelease=true;NoiseSystem.Emit(transform.position,19,gameObject,NoiseSystem.Kind.Mechanism,1,user.gameObject);Echo.Audio.PrototypeAudio.Play(gameObject,Echo.Audio.PrototypeAudio.Cue.Thud,1,failureClip);user.ui.ShowToast("Стопор не зафіксувався. Відпустіть Space і спробуйте знову; завершені ступені збережено.",4);}
    }
}
