using Echo.Radio;
using Echo.Manor;
using UnityEngine;

namespace Echo.Audio
{
    // A scene-owned score, not a world sound or a hearing/masking source.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class ManorAmbience : MonoBehaviour
    {
        public AudioClip music;
        [Range(0, 1)] public float playbackVolume = .1f;
        [Range(0, 1)] public float listeningMultiplier = .25f;
        [Min(.1f)] public float fadeInSeconds = 2f;
        [Min(.1f)] public float duckSeconds = .35f;
        private AudioSource source;
        private PortableRadio radio;
        private SymbolPuzzle library;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0;
            source.dopplerLevel = 0;
            source.ignoreListenerPause = false;
            source.priority = 180;
            radio = FindAnyObjectByType<PortableRadio>();
            library = FindAnyObjectByType<SymbolPuzzle>();
        }

        private void OnEnable()
        {
            source.clip = music;
            source.volume = 0;
            if (music != null) source.Play();
        }

        private void Update()
        {
            if (Time.timeScale <= 0) return;
            // Local records already suspend the radio through CueSilenced.
            // Duck for those cues as well as powered radio channels.
            bool listening = library != null && library.CuePlaying ||
                radio != null && (radio.CueSilenced || radio.powered && radio.volume > .001f);
            float target = playbackVolume * (listening ? listeningMultiplier : 1);
            float seconds = target < source.volume ? duckSeconds : fadeInSeconds;
            source.volume = Mathf.MoveTowards(source.volume, target,
                Time.deltaTime * playbackVolume / Mathf.Max(.1f, seconds));
        }

        private void OnDisable()
        {
            if (source != null) source.Stop();
        }
    }
}
