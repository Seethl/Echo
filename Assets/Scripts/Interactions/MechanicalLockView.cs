using Echo.Manor;
using Echo.Audio;
using UnityEngine;

namespace Echo.Interactions
{
    public sealed class MechanicalLockView : PropCloseupView
    {
        public KeypadLock keypad;
        public Transform[] drums=new Transform[3];
        public Transform submitButton;
        public Transform shackle;
        public TextMesh statusText;
        public float turnSeconds=.18f;
        private readonly Quaternion[] basis=new Quaternion[3];
        private readonly float[] from=new float[3],target=new float[3],timer=new float[3];
        private readonly int[] pending=new int[3];
        private bool initialized;
        private float buttonTime;
        private Vector3 buttonPosition;
        private Vector3 closedShacklePosition;
        private LineRenderer selectionFrame;
        private Material frameMaterial;
        private Transform visualBody;
        private Vector3 bodyPosition;
        private float failureLeft,successLeft;
        private static readonly Color ReadyColor=new Color(.85f,.68f,.35f);
        public bool ShowingSuccess=>IsOpen&&successLeft>0;
        public bool ShowingFailure=>failureLeft>0;
        public bool IsTurning=>pending[0]!=0||pending[1]!=0||pending[2]!=0;
        private void Initialize()
        {
            if(initialized)return;
            if(keypad==null)keypad=GetComponent<KeypadLock>();
            for(int i=0;i<3;i++)if(drums[i]!=null)basis[i]=drums[i].localRotation;
            if(submitButton!=null)buttonPosition=submitButton.localPosition;
            if(shackle!=null)closedShacklePosition=shackle.localPosition;
            if(drums[0]!=null&&drums[0].parent!=transform&&cameraAnchor!=null&&!cameraAnchor.IsChildOf(drums[0].parent))
            {visualBody=drums[0].parent;bodyPosition=visualBody.localPosition;}
            frameMaterial=PropFeedbackVisuals.Material(ReadyColor);
            selectionFrame=PropFeedbackVisuals.Line(transform,"Selected digit row",frameMaterial,.0012f,
                new Vector3(-.08f,.054f,.078f),new Vector3(.08f,.054f,.078f),new Vector3(.08f,.076f,.078f),new Vector3(-.08f,.076f,.078f));
            selectionFrame.loop=true;selectionFrame.enabled=false;
            initialized=true;
        }
        protected override void OnOpened()
        {
            Initialize();
            failureLeft=successLeft=0;selectionFrame.enabled=true;
            for(int i=0;i<3;i++){target[i]=from[i]=keypad.GetDigit(i)*36;pending[i]=0;SetRotation(i,target[i]);}
            Message("ЛКМ + · ПКМ − · колесо ± · OK");
        }
        public bool RotateDrum(int i,int direction)
        {
            Initialize();
            if(i<0||i>=3||keypad==null||keypad.IsUnlocked||drums[i]==null||pending[i]!=0||direction==0)return false;
            failureLeft=0;Message("ЛКМ + · ПКМ − · колесо ± · OK");
            pending[i]=direction>0?1:-1;from[i]=target[i];target[i]+=pending[i]*36;timer[i]=0;
            var clickSource=PrototypeAudio.Source(gameObject);clickSource.ignoreListenerPause=true;
            clickSource.PlayOneShot(PrototypeAudio.Clip(PrototypeAudio.Cue.Click),.28f);return true;
        }
        protected override bool CanHoldControl(int id)=>id>=0&&id<3;
        protected override float HoldInterval=>Mathf.Max(.02f,turnSeconds)+.02f;
        protected override string ControlHint(int id)
        {
            if(ShowingSuccess)return "Замок відкрито";
            if(id>=0&&id<3)return "Цифра "+(id+1)+"\nЛКМ + / ПКМ − · колесо миші";
            return id==3?(IsTurning?"Зачекай завершення оберту":"Підтвердити код · ЛКМ"):"";
        }
        public override void ActivateControl(int id,int direction)
        {
            if(!IsOpen)return;
            if(id<3){RotateDrum(id,direction);return;}
            if(id!=3||IsTurning||keypad.IsUnlocked)return;
            buttonTime=.15f;
            if(keypad.Submit(player)){failureLeft=0;successLeft=.65f;Message("ВІДКРИТО");}
            else {failureLeft=.6f;Message("Код не підходить · спробуй ще раз");}
        }
        public void AdvanceAnimation(float delta)
        {
            Initialize();
            failureLeft=Mathf.Max(0,failureLeft-delta);
            if(visualBody!=null)visualBody.localPosition=bodyPosition+Vector3.right*(failureLeft>0?Mathf.Sin(failureLeft*65)*.0025f*(failureLeft/.6f):0);
            if(frameMaterial!=null)PropFeedbackVisuals.SetColor(frameMaterial,successLeft>0?new Color(.4f,1,.6f):failureLeft>0?new Color(1,.22f,.12f):ReadyColor);
            if(statusText!=null)statusText.color=successLeft>0?new Color(.5f,1,.65f):failureLeft>0?new Color(1,.4f,.3f):ReadyColor;
            if(successLeft>0)
            {
                successLeft=Mathf.Max(0,successLeft-delta);
                if(successLeft<=0&&IsOpen){Close();return;}
            }
            for(int i=0;i<3;i++)
            {
                if(pending[i]==0)continue;
                timer[i]+=Mathf.Max(0,delta);float t=Mathf.Clamp01(timer[i]/Mathf.Max(.01f,turnSeconds));
                SetRotation(i,Mathf.Lerp(from[i],target[i],Mathf.SmoothStep(0,1,t)));
                if(t>=1){keypad.StepDigit(i,pending[i]);pending[i]=0;}
            }
            if(submitButton!=null)
            {
                buttonTime=Mathf.Max(0,buttonTime-delta);submitButton.localPosition=buttonPosition+Vector3.back*(buttonTime>0?.003f:0);
            }
        }
        protected override void TickVisuals()
        {
            AdvanceAnimation(Time.unscaledDeltaTime);
            if(shackle!=null&&keypad!=null)shackle.localPosition=Vector3.MoveTowards(shackle.localPosition,closedShacklePosition+(keypad.IsUnlocked?Vector3.up*.025f:Vector3.zero),Time.unscaledDeltaTime*.08f);
        }
        protected override void OnClosed()
        {
            AdvanceAnimation(10);failureLeft=successLeft=0;
            if(selectionFrame!=null)selectionFrame.enabled=false;
            if(visualBody!=null)visualBody.localPosition=bodyPosition;
        }
        private void SetRotation(int i,float angle){if(drums[i]!=null)drums[i].localRotation=basis[i]*Quaternion.AngleAxis(angle,Vector3.right);}
        private void Message(string text){if(statusText!=null)statusText.text=text;}
        protected override void OnDestroy(){base.OnDestroy();if(frameMaterial!=null)Destroy(frameMaterial);}
    }
}
