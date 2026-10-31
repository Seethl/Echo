using Echo.Manor;
using UnityEngine;

namespace Echo.Audio
{
    [RequireComponent(typeof(NoiseMaskZone))]
    public sealed class AmbientCycle:MonoBehaviour
    {
        public enum Mode { Shutter,Water,Clock }
        public Mode mode;
        public float period=16,warningSeconds=3,loudSeconds=5;
        public Transform movingVisual;
        public AudioClip loudClip,warningClip;
        private NoiseMaskZone mask;
        private float nextSound;
        private float coverUntil;
        public bool Loud {get;private set;}
        public bool Warning {get;private set;}
        private void Awake(){mask=GetComponent<NoiseMaskZone>();PrototypeAudio.Source(gameObject);}
        private void Update()
        {
            if(Time.timeScale<=0)return;
            float phase=Time.time%Mathf.Max(period,warningSeconds+loudSeconds+1);
            Warning=mode==Mode.Water&&phase>=period-loudSeconds-warningSeconds&&phase<period-loudSeconds;
            Loud=mode==Mode.Shutter?(ManorProgress.Active!=null&&!ManorProgress.Active.Has("gallery.quiet")):mode==Mode.Water&&phase>=period-loudSeconds;
            mask.active=Loud&&Time.time<coverUntil;
            if(movingVisual!=null)movingVisual.localRotation=Quaternion.Euler(0,0,Loud?Mathf.Sin(Time.time*9)*18:Warning?8:0);
            if(Time.time<nextSound)return;
            if(mode==Mode.Shutter&&Loud){var clip=loudClip!=null?loudClip:PrototypeAudio.Clip(PrototypeAudio.Cue.Thud);PrototypeAudio.Play(gameObject,PrototypeAudio.Cue.Thud,.75f,clip);coverUntil=Time.time+clip.length;mask.active=true;nextSound=Time.time+2.5f;}
            else if(mode==Mode.Water){var cue=Loud?PrototypeAudio.Cue.Water:PrototypeAudio.Cue.Click;var clip=Loud?loudClip:warningClip;clip=clip!=null?clip:PrototypeAudio.Clip(cue);PrototypeAudio.Play(gameObject,cue,Loud?.8f:Warning?.35f:.12f,clip);if(Loud){coverUntil=Time.time+clip.length;mask.active=true;}nextSound=Time.time+(Loud?1.7f:Warning?.3f:1.1f);}
            else if(mode==Mode.Clock){PrototypeAudio.Play(gameObject,PrototypeAudio.Cue.Click,.22f,warningClip);nextSound=Time.time+1;}
        }
        private void OnDisable(){if(mask!=null)mask.active=false;coverUntil=0;var source=GetComponent<AudioSource>();if(source!=null)source.Stop();}
    }
}
