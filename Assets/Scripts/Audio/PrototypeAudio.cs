using System.Collections.Generic;
using UnityEngine;

namespace Echo.Audio
{
    // Explicitly temporary synthesized cues; replace AudioClip fields without changing game logic.
    public static class PrototypeAudio
    {
        public enum Cue { Click,DoubleClick,Thud,LongBell,TwoBell,ThreeBell,Alert,Footstep,Music,Static,Water,Creak,NightChime,WoodKnock,BirdCall }
        private static readonly Dictionary<Cue,AudioClip> cache=new Dictionary<Cue,AudioClip>();
        private static readonly Dictionary<string,AudioClip> sequences=new Dictionary<string,AudioClip>();
        private static readonly float[] musicNotes={130.81f,155.56f,196,174.61f};
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]private static void Reset(){cache.Clear();sequences.Clear();}
        public static AudioSource Source(GameObject owner)
        {
            var source=owner.GetComponent<AudioSource>();if(source==null)source=owner.AddComponent<AudioSource>();
            source.playOnAwake=false;source.spatialBlend=1;source.rolloffMode=AudioRolloffMode.Linear;source.minDistance=1;source.maxDistance=26;return source;
        }
        public static void Play(GameObject owner,Cue cue,float volume=.6f,AudioClip clip=null)=>Source(owner).PlayOneShot(clip!=null?clip:Clip(cue),volume);
        public static AudioClip Clip(Cue cue)
        {
            if(cache.TryGetValue(cue,out var existing)&&existing!=null)return existing;
            int sampleRate=22050;float seconds=cue==Cue.NightChime?1.15f:cue==Cue.WoodKnock?.95f:cue==Cue.BirdCall?1.05f:cue==Cue.Music?20:cue==Cue.Water?2:cue==Cue.Static?1:cue==Cue.LongBell?.85f:cue==Cue.ThreeBell?1:cue==Cue.TwoBell?.65f:cue==Cue.Thud?.45f:.24f;
            var samples=new float[Mathf.CeilToInt(seconds*sampleRate)];var random=new System.Random(100+(int)cue);
            for(int i=0;i<samples.Length;i++)
            {
                float t=(float)i/sampleRate;float value=0;float envelope=Mathf.Clamp01(t/.01f)*Mathf.Clamp01((seconds-t)/.06f);
                switch(cue)
                {
                    case Cue.NightChime:
                        value=(Mathf.Sin(t*220*2*Mathf.PI)+.35f*Mathf.Sin(t*440*2*Mathf.PI))*.3f*Mathf.Exp(-t*2.8f);break;
                    case Cue.WoodKnock:
                        float knock=t<.48f?t-.12f:t-.55f;
                        value=knock>=0&&knock<.25f?Mathf.Sin(knock*130*2*Mathf.PI)*Mathf.Exp(-knock*24)*.5f:0;
                        if(t<.3f)value+=Mathf.Sin(2*Mathf.PI*(160*t+180*t*t))*.12f*(1-t/.3f);break;
                    case Cue.BirdCall:
                        float chirp=t<.5f?t-.12f:t-.58f;
                        if(chirp>=0&&chirp<.25f)value=Mathf.Sin(2*Mathf.PI*(1450*chirp+2200*chirp*chirp))*.28f*Mathf.Sin(Mathf.PI*chirp/.25f);break;
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
        public static AudioClip Sequence(string key,AudioClip[] parts,float gapSeconds)
        {
            if(sequences.TryGetValue(key,out var existing)&&existing!=null)return existing;
            const int rate=22050;var result=new List<float>();
            foreach(var part in parts)
            {
                if(part==null)continue;
                var samples=new float[part.samples*part.channels];
                if(!part.GetData(samples,0)){Debug.LogWarning("Radio sequence needs readable clips (Decompress On Load).");return null;}
                int count=Mathf.CeilToInt(part.length*rate);
                for(int i=0;i<count;i++)
                {
                    int frame=Mathf.Min(part.samples-1,Mathf.FloorToInt((float)i*part.frequency/rate));float mono=0;
                    for(int c=0;c<part.channels;c++)mono+=samples[frame*part.channels+c];result.Add(mono/part.channels);
                }
                for(int i=0;i<Mathf.CeilToInt(gapSeconds*rate);i++)result.Add(0);
            }
            var clip=AudioClip.Create("TEMP_Echo_"+key,result.Count,1,rate,false);clip.SetData(result.ToArray(),0);sequences[key]=clip;return clip;
        }
    }
}
