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
        public string shutterClosedFlag="gallery.quiet";
        public float visualSwingDegrees=18f, initialDelay;
        public AudioClip loudClip,warningClip;
        public AudioClip windClip;
        [Range(0,1)] public float windPlaybackVolume=.24f;
        private AudioSource wind;private GalleryWindowShutter window;
        private NoiseMaskZone mask;
        private float nextSound;
        public bool Loud {get;private set;}
        public bool Warning {get;private set;}
        private void Awake(){mask=GetComponent<NoiseMaskZone>();PrototypeAudio.Source(gameObject);nextSound=Time.time+initialDelay;
            window=GetComponent<GalleryWindowShutter>();
            if(mode==Mode.Shutter&&windClip!=null){var child=new GameObject("Window wind audio");child.transform.SetParent(transform,false);wind=child.AddComponent<AudioSource>();wind.clip=windClip;wind.loop=true;wind.playOnAwake=false;wind.spatialBlend=1;wind.rolloffMode=AudioRolloffMode.Linear;wind.minDistance=1;wind.maxDistance=8;wind.volume=0;wind.Play();}
        }
        private void OnDisable(){if(wind!=null)wind.Stop();}
        private void Update()
        {
            if(Time.timeScale<=0)return;
            float phase=Time.time%Mathf.Max(period,warningSeconds+loudSeconds+1);
            Warning=mode==Mode.Water&&phase>=period-loudSeconds-warningSeconds&&phase<period-loudSeconds;
            Loud=mode==Mode.Shutter?(ManorProgress.Active!=null&&!ManorProgress.Active.Has(shutterClosedFlag)):mode==Mode.Water&&phase>=period-loudSeconds;
            mask.active=Loud;
            if(wind!=null){wind.volume=windPlaybackVolume*(window!=null?window.OpenAmount:Loud?1:0);if(wind.volume>.001f&&!wind.isPlaying)wind.Play();else if(wind.volume<=.001f&&wind.isPlaying)wind.Stop();}
            if(movingVisual!=null)movingVisual.localRotation=Quaternion.Euler(0,0,Loud?Mathf.Sin(Time.time*9)*visualSwingDegrees:Warning?8:0);
            if(Time.time<nextSound)return;
            if(mode==Mode.Shutter&&Loud){PrototypeAudio.Play(gameObject,PrototypeAudio.Cue.Thud,.75f,loudClip);nextSound=Time.time+2.5f;}
            else if(mode==Mode.Water){PrototypeAudio.Play(gameObject,Loud?PrototypeAudio.Cue.Water:PrototypeAudio.Cue.Click,Loud?.8f:Warning?.35f:.12f,Loud?loudClip:warningClip);nextSound=Time.time+(Loud?1.7f:Warning?.3f:1.1f);}
            else if(mode==Mode.Clock){PrototypeAudio.Play(gameObject,PrototypeAudio.Cue.Click,.22f,warningClip);nextSound=Time.time+1;}
        }
    }
}
