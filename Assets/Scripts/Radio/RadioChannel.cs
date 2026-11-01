using UnityEngine;

namespace Echo.Radio
{
    [CreateAssetMenu(menuName="Echo/Radio channel")]
    public sealed class RadioChannel:ScriptableObject
    {
        public enum Mode { Recording,Music,WhiteNoise }
        public enum Payload { Literal,GalleryCode,LibrarySounds }
        public string channelName;
        public int frequency=820;
        [Min(5)] public int receptionHalfWidth=25;
        public Mode mode;
        public Payload payload;
        [TextArea(3,8)] public string transcript;
        public string clearAfterFlag;
        [TextArea(3,8)] public string interruptedTranscript;
        public AudioClip voiceClip;
        [Tooltip("Optional digit recordings 0..9 for a randomized code. Empty keeps legacy behavior.")]
        public AudioClip[] digitClips = System.Array.Empty<AudioClip>();
        private AudioClip cachedCodeClip;private string cachedCode;
        public AudioClip CodeRecording(string code)
        {
            if(string.IsNullOrEmpty(code)||digitClips==null||digitClips.Length!=10)return null;
            if(cachedCodeClip!=null&&cachedCode==code)return cachedCodeClip;
            var parts=new AudioClip[code.Length];for(int i=0;i<code.Length;i++){int digit=code[i]-'0';if(digit<0||digit>9||digitClips[digit]==null)return null;parts[i]=digitClips[digit];}
            if(cachedCodeClip!=null)Object.Destroy(cachedCodeClip);
            cachedCode=code;return cachedCodeClip=Echo.Audio.PrototypeAudio.Sequence("gallery-code."+code,parts,.55f);
        }
    }
}
