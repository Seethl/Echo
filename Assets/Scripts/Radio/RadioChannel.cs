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
    }
}
