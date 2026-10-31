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
        public bool IsTurning=>pending[0]!=0||pending[1]!=0||pending[2]!=0;
        private void Initialize()
        {
            if(initialized)return;
            if(keypad==null)keypad=GetComponent<KeypadLock>();
            for(int i=0;i<3;i++)if(drums[i]!=null)basis[i]=drums[i].localRotation;
            if(submitButton!=null)buttonPosition=submitButton.localPosition;
            if(shackle!=null)closedShacklePosition=shackle.localPosition;
            initialized=true;
        }
        protected override void OnOpened()
        {
            Initialize();
            for(int i=0;i<3;i++){target[i]=from[i]=keypad.GetDigit(i)*36;pending[i]=0;SetRotation(i,target[i]);}
            Message("ЛКМ + · ПКМ − · ESC назад");
        }
        public bool RotateDrum(int i,int direction)
        {
            Initialize();
            if(i<0||i>=3||keypad==null||keypad.IsUnlocked||drums[i]==null||pending[i]!=0||direction==0)return false;
            pending[i]=direction>0?1:-1;from[i]=target[i];target[i]+=pending[i]*36;timer[i]=0;
            var clickSource=PrototypeAudio.Source(gameObject);clickSource.ignoreListenerPause=true;
            clickSource.PlayOneShot(PrototypeAudio.Clip(PrototypeAudio.Cue.Click),.28f);return true;
        }
        public override void ActivateControl(int id,int direction)
        {
            if(!IsOpen)return;
            if(id<3){RotateDrum(id,direction);return;}
            if(id!=3||IsTurning||keypad.IsUnlocked)return;
            buttonTime=.15f;
            if(keypad.Submit(player)){Message("ВІДКРИТО");Close();}
            else Message("Код не підходить · ESC назад");
        }
        public void AdvanceAnimation(float delta)
        {
            Initialize();
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
        protected override void OnClosed(){AdvanceAnimation(10);}
        private void SetRotation(int i,float angle){if(drums[i]!=null)drums[i].localRotation=basis[i]*Quaternion.AngleAxis(angle,Vector3.right);}
        private void Message(string text){if(statusText!=null)statusText.text=text;}
    }
}
