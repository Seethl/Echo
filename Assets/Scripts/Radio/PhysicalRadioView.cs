using Echo.Interactions;
using UnityEngine;
namespace Echo.Radio
{
    public sealed class PhysicalRadioView : PropCloseupView
    {
        public PortableRadio radio;
        public Transform tuningKnob,volumeKnob,needle;
        public TextMesh readout;
        private Quaternion tuneBase,volumeBase;
        private Vector3 needleBase;
        private bool initialized,detached;
        private Transform savedParent;
        private Vector3 savedPosition;
        private Quaternion savedRotation;
        private Collider[] controls;
        private bool[] colliderStates;
        private void Initialize()
        {
            if(initialized)return;
            if(radio==null)radio=GetComponent<PortableRadio>();
            if(tuningKnob!=null)tuneBase=tuningKnob.localRotation;
            if(volumeKnob!=null)volumeBase=volumeKnob.localRotation;
            if(needle!=null)needleBase=needle.localPosition;
            initialized=true;
        }
        public override bool Open(PlayerInteractor user)
        {
            Initialize();
            if(IsOpen)return false;
            if(radio.Held)
            {
                savedParent=radio.transform.parent;savedPosition=radio.transform.localPosition;savedRotation=radio.transform.localRotation;
                radio.transform.SetParent(null,true);detached=true;
            }
            if(base.Open(user))return true;
            RestoreParent();return false;
        }
        protected override void OnOpened()
        {
            controls=radio.GetComponentsInChildren<Collider>();colliderStates=new bool[controls.Length];
            for(int i=0;i<controls.Length;i++){colliderStates[i]=controls[i].enabled;controls[i].enabled=true;}
        }
        protected override void OnClosed()
        {
            if(controls!=null)for(int i=0;i<controls.Length;i++)if(controls[i]!=null)controls[i].enabled=colliderStates[i];
            RestoreParent();
        }
        private void RestoreParent()
        {
            if(!detached||radio==null)return;
            radio.transform.SetParent(savedParent,false);radio.transform.localPosition=savedPosition;radio.transform.localRotation=savedRotation;detached=false;
        }
        public override void ActivateControl(int id,int direction)
        {
            if(!IsOpen||radio==null)return;
            if(id==0)radio.Tune(radio.frequency+(direction>0?20:-20));
            else if(id==1)radio.SetVolume(radio.volume+(direction>0?.1f:-.1f));
            else if(id==2)radio.TogglePower();
            else if(id==3)radio.ReplayRecording();
        }
        protected override void TickVisuals()
        {
            Initialize();if(radio==null)return;
            float f=Mathf.InverseLerp(760,1280,radio.frequency),t=1-Mathf.Exp(-14*Time.unscaledDeltaTime);
            if(tuningKnob!=null)tuningKnob.localRotation=Quaternion.Slerp(tuningKnob.localRotation,tuneBase*Quaternion.AngleAxis(Mathf.Lerp(-140,140,f),Vector3.forward),t);
            if(volumeKnob!=null)volumeKnob.localRotation=Quaternion.Slerp(volumeKnob.localRotation,volumeBase*Quaternion.AngleAxis(Mathf.Lerp(-130,130,radio.volume),Vector3.forward),t);
            if(needle!=null)needle.localPosition=Vector3.Lerp(needle.localPosition,needleBase+Vector3.left*Mathf.Lerp(-.075f,.075f,f),t);
            if(readout!=null)readout.text=radio.frequency+" · "+Mathf.RoundToInt(radio.volume*100)+"% · "+(radio.powered?"ON":"OFF");
        }
    }
}
