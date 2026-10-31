using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Echo.Puzzles
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundSequencePuzzle : MonoBehaviour
    {
        public AudioClip[] tones = Array.Empty<AudioClip>();
        public int[] sequence = { 0, 1, 2 };
        public bool requireListeningFirst = true;
        [Min(0f)] public float gapBetweenTones = 0.2f;
        [Min(0f), Tooltip("Zero keeps the introductory puzzle safe; independent of audio volume.")]
        public float gameplayNoiseRadius;

        public UnityEvent onClueStarted = new UnityEvent();
        public UnityEvent onClueFinished = new UnityEvent();
        public UnityEvent<int> onTonePlayed = new UnityEvent<int>();
        public UnityEvent<int> onProgressChanged = new UnityEvent<int>();
        public UnityEvent onMistake = new UnityEvent();
        public UnityEvent onSolved = new UnityEvent();

        public bool IsPlayingClue { get; private set; }
        public bool HasListened { get; private set; }
        public bool IsSolved => rules != null && rules.IsSolved;
        public int CorrectCount => rules != null ? rules.Position : 0;
        private SoundSequenceState rules;
        private AudioSource source;
        private AudioClip[] configuredTones;
        private Coroutine playback;
        private bool notifying;
        private bool CanAct => isActiveAndEnabled && !Echo.Menu.GameMenu.BlocksPlayerInput && Time.timeScale > 0f;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
        }

        private bool EnsureConfigured()
        {
            if (rules != null) return true;
            if (tones == null || tones.Length == 0) return false;
            foreach (var tone in tones) if (tone == null) return false;
            try { rules = new SoundSequenceState(sequence, tones.Length); }
            catch (ArgumentException) { return false; }
            configuredTones = (AudioClip[])tones.Clone();
            return true;
        }

        // Void methods can be bound to any lever/button's Inspector UnityEvent.
        public void PlayClue() { TryPlayClue(); }
        public void SubmitTone(int tone) { TrySubmitTone(tone); }

        public bool TryPlayClue()
        {
            if (!CanAct || notifying || IsSolved || IsPlayingClue || !source.enabled || !EnsureConfigured()) return false;
            rules.ResetAttempt();
            IsPlayingClue = true;
            playback = StartCoroutine(PlaySequence());
            return true;
        }

        private IEnumerator PlaySequence()
        {
            // Assign playback before callbacks can disable the component.
            yield return null;
            bool finished = false;
            try
            {
                onClueStarted.Invoke();
                if (!isActiveAndEnabled) yield break;
                onProgressChanged.Invoke(0);
                for (int i = 0; i < rules.Length; i++)
                {
                    if (!isActiveAndEnabled || !source.enabled) yield break;
                    while (!CanAct) yield return null;
                    int tone = rules.ToneAt(i);
                    if (configuredTones[tone] == null) yield break;
                    PlayTone(tone);
                    float remaining = configuredTones[tone].length + Mathf.Max(0f, gapBetweenTones);
                    while (remaining > 0f)
                    {
                        yield return null;
                        if (CanAct) remaining -= Time.deltaTime;
                    }
                }
                HasListened = true;
                finished = true;
            }
            finally { IsPlayingClue = false; playback = null; }
            if (finished) onClueFinished.Invoke();
        }

        public SoundSequenceState.Result TrySubmitTone(int tone)
        {
            if (!CanAct || notifying || IsPlayingClue || !source.enabled || !EnsureConfigured() ||
                (requireListeningFirst && !HasListened)) return SoundSequenceState.Result.Rejected;
            var result = rules.Submit(tone);
            if (result == SoundSequenceState.Result.Rejected) return result;
            notifying = true;
            try
            {
                PlayTone(tone);
                onProgressChanged.Invoke(rules.Position);
                if (result == SoundSequenceState.Result.Mistake) onMistake.Invoke();
                if (result == SoundSequenceState.Result.Solved) onSolved.Invoke();
            }
            finally { notifying = false; }
            return result;
        }

        private void PlayTone(int tone)
        {
            source.PlayOneShot(configuredTones[tone]);
            // Visual/accessibility feedback can use the exact same tone identifier.
            onTonePlayed.Invoke(tone);
            if (isActiveAndEnabled)
                NoiseSystem.Emit(transform.position, Mathf.Max(0f, gameplayNoiseRadius), gameObject);
        }

        public void ResetAttempt()
        {
            if (!CanAct || notifying || IsPlayingClue || IsSolved || rules == null) return;
            rules.ResetAttempt();
            notifying = true;
            try { onProgressChanged.Invoke(0); }
            finally { notifying = false; }
        }

        void OnDisable()
        {
            if (playback != null) StopCoroutine(playback);
            playback = null;
            IsPlayingClue = false;
            if (source != null) source.Stop();
        }
    }
}
