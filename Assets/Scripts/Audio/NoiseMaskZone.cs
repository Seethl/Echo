using System.Collections.Generic;
using UnityEngine;

namespace Echo.Audio
{
    public sealed class NoiseMaskZone:MonoBehaviour
    {
        private static readonly List<NoiseMaskZone> zones=new List<NoiseMaskZone>();
        public float radius=12f;
        [Range(0,1)] public float strength=.9f;
        public bool active;
        public string description="місцевий фоновий шум";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]private static void Reset()=>zones.Clear();
        private void OnEnable(){if(!zones.Contains(this))zones.Add(this);}
        private void OnDisable()=>zones.Remove(this);
        private float At(Vector3 position)
        {
            var source=GetComponent<AudioSource>();
            if(!active||source==null||!source.enabled||!source.isPlaying||source.mute||source.volume<=0)return 0;
            float distance=Vector3.Distance(position,transform.position);
            float reaching=radius*SoundPropagation.Transmission(transform.position,position,transform,null,Physics.DefaultRaycastLayers);
            if(reaching<=0||distance>reaching)return 0;
            return strength*Mathf.Clamp01((radius-distance)/(radius*.3f));
        }
        public static float CoverAt(Vector3 position){float cover=0;foreach(var zone in zones)if(zone!=null)cover=Mathf.Max(cover,zone.At(position));return cover;}
        public static string DescriptionAt(Vector3 position){NoiseMaskZone best=null;float value=.1f;foreach(var zone in zones)if(zone!=null&&zone.At(position)>value){value=zone.At(position);best=zone;}return best!=null?best.description:"немає";}
    }
}
