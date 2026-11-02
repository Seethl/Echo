using Echo.Interactions;
using UnityEngine;
namespace Echo.Radio
{
    public sealed class PhysicalRadioView : PropCloseupView
    {
        public override bool UsesFooter=>true;
        protected override string IdleHint=>"Наведи на ручку: ЛКМ + / ПКМ − або колесо миші · Escape — назад";
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
        private Transform powerButton,replayButton;
        private Vector3 powerBase,replayBase;
        private float powerPress,replayPress,glow;
        private Material dialMaterial,markerMaterial;
        private Mesh dialMesh;
        private void Initialize()
        {
            if(initialized)return;
            if(radio==null)radio=GetComponent<PortableRadio>();
            if(tuningKnob!=null)tuneBase=tuningKnob.localRotation;
            if(volumeKnob!=null)volumeBase=volumeKnob.localRotation;
            if(needle!=null)needleBase=needle.localPosition;
            foreach(var control in GetComponentsInChildren<PropPhysicalControl>(true))
            {
                if(control.controlId==2){powerButton=control.transform;powerBase=powerButton.localPosition;}
                if(control.controlId==3){replayButton=control.transform;replayBase=replayButton.localPosition;}
            }
            CreateFeedback();
            initialized=true;
        }
        private void CreateFeedback()
        {
            markerMaterial=PropFeedbackVisuals.Material(new Color(.95f,.8f,.5f));
            foreach(var knob in new[]{tuningKnob,volumeKnob})
            {
                if(knob==null)continue;
                var mesh=knob.GetComponent<MeshFilter>();if(mesh==null)continue;
                var bounds=mesh.sharedMesh.bounds;
                float side=cameraAnchor!=null&&knob.InverseTransformPoint(cameraAnchor.position).z<bounds.center.z?-1:1;
                Vector3 front=bounds.center+Vector3.forward*(bounds.extents.z+.001f)*side;
                PropFeedbackVisuals.Line(knob,"Knob position mark",markerMaterial,.0022f,front+Vector3.up*.009f,front+Vector3.up*.021f);
            }
            if(needle==null)return;
            dialMaterial=PropFeedbackVisuals.Material(new Color(.12f,.065f,.018f));
            dialMesh=new Mesh {name="Radio illuminated dial"};
            dialMesh.vertices=new[]{new Vector3(-.09f,-.04f,0),new Vector3(.09f,-.04f,0),new Vector3(.09f,.04f,0),new Vector3(-.09f,.04f,0)};
            dialMesh.triangles=new[]{0,1,2,0,2,3};dialMesh.RecalculateNormals();dialMesh.RecalculateBounds();
            var face=new GameObject("Powered dial",typeof(MeshFilter),typeof(MeshRenderer));face.transform.SetParent(needle.parent,false);
            face.transform.localPosition=needleBase+Vector3.back*.003f;
            face.GetComponent<MeshFilter>().sharedMesh=dialMesh;face.GetComponent<MeshRenderer>().sharedMaterial=dialMaterial;
            face.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        public override bool Open(PlayerInteractor user)
        {
            Initialize();
            if(IsOpen||radio==null)return false;
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
            // Controls use the reserved footer, leaving the radio unobstructed.
            controls=radio.GetComponentsInChildren<Collider>();colliderStates=new bool[controls.Length];
            for(int i=0;i<controls.Length;i++)
            {
                colliderStates[i]=controls[i].enabled;
                var control=controls[i].GetComponent<PropPhysicalControl>();
                if(control!=null&&control.owner==this)controls[i].enabled=true;
            }
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
        protected override bool CanHoldControl(int id)=>id==0||id==1;
        protected override string ControlHint(int id)
        {
            if(id==0)return "Частота\nЗатисни ЛКМ + / ПКМ − або крути колесо";
            if(id==1)return "Гучність\nЗатисни ЛКМ + / ПКМ − або крути колесо";
            if(id==2)return radio!=null&&radio.powered?"Вимкнути радіо · ЛКМ":"Увімкнути радіо · ЛКМ";
            return id==3?"Повторити запис · ЛКМ":"";
        }
        public override void ActivateControl(int id,int direction)
        {
            if(!IsOpen||radio==null)return;
            if(id==0)radio.Tune(radio.frequency+(direction>0?20:-20));
            else if(id==1)radio.SetVolume(radio.volume+(direction>0?.1f:-.1f));
            else if(id==2){powerPress=.16f;if(powerButton!=null)powerButton.localPosition=powerBase+Vector3.back*.0035f;radio.TogglePower();}
            else if(id==3){replayPress=.16f;if(replayButton!=null)replayButton.localPosition=replayBase+Vector3.back*.0035f;radio.ReplayRecording();}
        }
        protected override void TickVisuals()
        {
            Initialize();if(radio==null)return;
            powerPress=Mathf.Max(0,powerPress-Time.unscaledDeltaTime);replayPress=Mathf.Max(0,replayPress-Time.unscaledDeltaTime);
            if(powerButton!=null)powerButton.localPosition=powerBase+Vector3.back*(powerPress>0?.0035f:0);
            if(replayButton!=null)replayButton.localPosition=replayBase+Vector3.back*(replayPress>0?.0035f:0);
            glow=Mathf.MoveTowards(glow,radio.powered?1:0,Time.unscaledDeltaTime*6);
            if(dialMaterial!=null)PropFeedbackVisuals.SetColor(dialMaterial,Color.Lerp(new Color(.12f,.065f,.018f),new Color(1,.58f,.16f),glow));
            float f=Mathf.InverseLerp(760,1280,radio.frequency),t=1-Mathf.Exp(-14*Time.unscaledDeltaTime);
            if(tuningKnob!=null)tuningKnob.localRotation=Quaternion.Slerp(tuningKnob.localRotation,tuneBase*Quaternion.AngleAxis(Mathf.Lerp(-140,140,f),Vector3.forward),t);
            if(volumeKnob!=null)volumeKnob.localRotation=Quaternion.Slerp(volumeKnob.localRotation,volumeBase*Quaternion.AngleAxis(Mathf.Lerp(-130,130,radio.volume),Vector3.forward),t);
            if(needle!=null)needle.localPosition=Vector3.Lerp(needle.localPosition,needleBase+Vector3.left*Mathf.Lerp(-.075f,.075f,f),t);
            if(readout!=null)readout.text=radio.frequency+" · "+Mathf.RoundToInt(radio.volume*100)+"% · "+(radio.powered?"ON":"OFF");
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if(dialMaterial!=null)Destroy(dialMaterial);
            if(markerMaterial!=null)Destroy(markerMaterial);
            if(dialMesh!=null)Destroy(dialMesh);
        }
    }
}
