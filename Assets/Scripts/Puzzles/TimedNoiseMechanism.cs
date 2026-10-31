using UnityEngine;
using UnityEngine.Events;

namespace Echo.Puzzles
{
    /// <summary>Timed machinery for a noisy final action; does not own doors, radio or player input.</summary>
    [RequireComponent(typeof(AudioSource))]
    public class TimedNoiseMechanism : MonoBehaviour
    {
        public AudioClip loopClip;
        [Min(0.1f)] public float duration = 4f;
        [Min(0f)] public float noiseRadius = 12f;
        [Min(0.1f)] public float pulseInterval = 1f;
        public UnityEvent onStarted = new UnityEvent();
        public UnityEvent<float> onProgressChanged = new UnityEvent<float>();
        public UnityEvent onCancelled = new UnityEvent();
        public UnityEvent onCompleted = new UnityEvent();
        public bool IsRunning { get; private set; }
        public bool IsCompleted { get; private set; }
        public float Progress => IsCompleted ? 1f : elapsed / runDuration;

        private AudioSource source;
        private float elapsed;
        private float runDuration = 1f;
        private float nextPulse;
        private bool pausedAudio;
        private bool notifying;
        private bool CanAct => isActiveAndEnabled && !Echo.Menu.GameMenu.BlocksPlayerInput && Time.timeScale > 0f;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
        }

        public void Begin() { TryBegin(); }

        public bool TryBegin()
        {
            if (!CanAct || notifying || IsRunning || IsCompleted || !source.enabled || loopClip == null ||
                float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0f) return false;
            elapsed = 0f;
            runDuration = Mathf.Max(0.1f, duration);
            IsRunning = true;
            source.clip = loopClip;
            source.loop = true;
            source.Play();
            Pulse();
            if (!IsRunning) return false;
            Notify(() => { onStarted.Invoke(); if (IsRunning) onProgressChanged.Invoke(0f); });
            return IsRunning;
        }

        void Update()
        {
            if (!IsRunning) return;
            if (!CanAct)
            {
                if (source.isPlaying) { source.Pause(); pausedAudio = true; }
                return;
            }
            if (pausedAudio) { source.UnPause(); pausedAudio = false; }
            if (!source.enabled) { Cancel(); return; }
            elapsed = Mathf.Min(runDuration, elapsed + Time.deltaTime);
            if (elapsed >= runDuration)
            {
                IsRunning = false;
                IsCompleted = true;
                StopAudio();
                Notify(() => { onProgressChanged.Invoke(1f); onCompleted.Invoke(); });
                return;
            }
            if (elapsed >= nextPulse) Pulse();
            Notify(() => onProgressChanged.Invoke(Progress));
        }

        private void Pulse()
        {
            NoiseSystem.Emit(transform.position, noiseRadius, gameObject);
            nextPulse = elapsed + Mathf.Max(0.1f, pulseInterval);
        }

        public void Cancel()
        {
            if (!IsRunning) return;
            IsRunning = false;
            elapsed = 0f;
            StopAudio();
            Notify(() => { onProgressChanged.Invoke(0f); onCancelled.Invoke(); });
        }

        private void StopAudio() { source.Stop(); pausedAudio = false; }
        private void Notify(System.Action action)
        {
            bool previous = notifying;
            notifying = true;
            try { action(); } finally { notifying = previous; }
        }

        void OnDisable() { Cancel(); }
    }
}
