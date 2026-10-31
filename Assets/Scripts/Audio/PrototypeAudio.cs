using System.Collections.Generic;
using UnityEngine;

namespace Echo.Audio
{
    // Explicitly temporary synthesized cues; replace AudioClip fields without changing game logic.
    public static class PrototypeAudio
    {
        public enum Cue { Click,DoubleClick,Thud,LongBell,TwoBell,ThreeBell,Alert,Footstep,Music,Static,Water,Creak }
        private static readonly Dictionary<Cue,AudioClip> cache=new Dictionary<Cue,AudioClip>();
        private static readonly float[] musicNotes={130.81f,155.56f,196,174.61f};
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]private static void Reset()=>cache.Clear();
        public static AudioSource Source(GameObject owner)
        {
            var source=owner.GetComponent<AudioSource>();if(source==null)source=owner.AddComponent<AudioSource>();
            source.playOnAwake=false;source.spatialBlend=1;source.rolloffMode=AudioRolloffMode.Linear;source.minDistance=1;source.maxDistance=26;return source;
        }
        public static void Play(GameObject owner,Cue cue,float volume=.6f,AudioClip clip=null)=>Source(owner).PlayOneShot(clip!=null?clip:Clip(cue),volume);
        public static AudioClip Clip(Cue cue)
        {
            if(cache.TryGetValue(cue,out var existing)&&existing!=null)return existing;
            int sampleRate=22050;float seconds=cue==Cue.Music?20:cue==Cue.Water?2:cue==Cue.Static?1:cue==Cue.LongBell?.85f:cue==Cue.ThreeBell?1:cue==Cue.TwoBell?.65f:cue==Cue.Thud?.45f:.24f;
            var samples=new float[Mathf.CeilToInt(seconds*sampleRate)];var random=new System.Random(100+(int)cue);
            for(int i=0;i<samples.Length;i++)
            {
                float t=(float)i/sampleRate;float value=0;float envelope=Mathf.Clamp01(t/.01f)*Mathf.Clamp01((seconds-t)/.06f);
                switch(cue)
                {
                    case Cue.Music:
                        float f=musicNotes[Mathf.FloorToInt(t/2)%musicNotes.Length];value=(Mathf.Sin(t*f*2*Mathf.PI)+.3f*Mathf.Sin(t*f*3*Mathf.PI))*.13f;break;
                    case Cue.Static:case Cue.Water:value=((float)random.NextDouble()*2-1)*.2f;break;
                    case Cue.Thud:case Cue.Footstep:case Cue.Creak:value=Mathf.Sin(t*(cue==Cue.Creak?140:75)*2*Mathf.PI)*Mathf.Exp(-t*14)*.5f;break;
                    case Cue.TwoBell:case Cue.ThreeBell:
                        bool sound=t%.3f<.13f;value=sound?Mathf.Sin(t*700*2*Mathf.PI)*.3f:0;break;
                    case Cue.LongBell:value=Mathf.Sin(t*440*2*Mathf.PI)*Mathf.Exp(-t*2)*.35f;break;
                    case Cue.DoubleClick:value=(t<.06f||(t>.13f&&t<.2f))?Mathf.Sin(t*1100*2*Mathf.PI)*.3f:0;break;
                    default:value=Mathf.Sin(t*(cue==Cue.Alert?220:900)*2*Mathf.PI)*Mathf.Exp(-t*15)*.4f;break;
                }
                samples[i]=value*envelope;
            }
            var clip=AudioClip.Create("TEMP_Echo_"+cue,samples.Length,1,sampleRate,false);clip.SetData(samples,0);cache[cue]=clip;return clip;
        }
    }
}
