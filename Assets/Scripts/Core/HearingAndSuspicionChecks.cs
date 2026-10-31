using System;
using System.Collections;
using UnityEngine;

/// <summary>Temporary physics fixtures in the existing lab, never persisted to a scene.</summary>
public static class HearingAndSuspicionChecks
{
    public static IEnumerator Run()
    {
        int count = 0;
        Action<bool, string> check = (condition, message) =>
        {
            if (!condition) throw new InvalidOperationException("Hearing check: " + message);
            count++;
        };
        yield return CheckPropagation(check);
        yield return CheckSuspicion(check);
        Debug.Log($"Hearing and suspicion checks: {count} passed.");
    }

    private static IEnumerator CheckPropagation(Action<bool, string> check)
    {
        var root = new GameObject("Acoustic Physics Checks");
        root.transform.position = new Vector3(5000f, 0f, 5000f);
        AudioClip clip = null;
        try
        {
            var emitter = Child(root, "Noise Emitter", Vector3.zero);
            var listener = Child(root, "Listener", Vector3.right * 8f);
            var ears = listener.AddComponent<NoiseListener>();
            ears.earOffset = Vector3.zero;
            int heard = 0;
            ears.onNoiseHeard.AddListener(_ => heard++);
            NoiseSystem.Emit(new Vector3(float.NaN, 0f, 0f), 10f, emitter);
            check(heard == 0, "invalid positions are rejected before physics queries");
            NoiseSystem.Emit(emitter.transform.position, 10f, emitter);
            check(heard == 1 && Mathf.Abs(ears.LastAudibility - 0.2f) < 0.001f,
                "open-path hearing weakens with distance");

            var wall = Child(root, "Wall", Vector3.right * 4f);
            var collider = wall.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.2f, 3f, 3f);
            Physics.SyncTransforms();
            NoiseSystem.Emit(emitter.transform.position, 10f, emitter);
            check(heard == 1, "default solid wall muffles otherwise audible footsteps");
            NoiseSystem.Emit(emitter.transform.position, 20f, emitter);
            check(heard == 2, "a loud action can still be heard through one wall");
            var profile = wall.AddComponent<AcousticObstacle>();
            profile.transmission = 0.9f;
            NoiseSystem.Emit(emitter.transform.position, 10f, emitter);
            check(heard == 3, "material profile overrides default transmission");
            wall.transform.localPosition += Vector3.forward * 5f;
            Physics.SyncTransforms();
            NoiseSystem.Emit(emitter.transform.position, 10f, emitter);
            check(heard == 4 && Mathf.Abs(ears.LastAudibility - 0.2f) < 0.001f,
                "physically opened door restores the clear sound path");
            wall.transform.localPosition = Vector3.right * 4f;
            profile.transmission = 0f;
            Physics.SyncTransforms();
            NoiseSystem.Emit(emitter.transform.position, 100f, emitter);
            check(heard == 4, "fully blocking material stops even a loud source");
            ears.useOcclusion = false;
            NoiseSystem.Emit(emitter.transform.position, 10f, emitter);
            check(heard == 5, "propagation can be disabled for authoring comparison");
            ears.useOcclusion = true;

            profile.transmission = 0.5f;
            var secondWall = Child(root, "Second Wall", Vector3.right * 6f);
            secondWall.AddComponent<BoxCollider>().size = new Vector3(0.2f, 3f, 3f);
            secondWall.AddComponent<AcousticObstacle>().transmission = 0.5f;
            Physics.SyncTransforms();
            float transmission = SoundPropagation.Transmission(emitter.transform.position, listener.transform.position,
                emitter.transform, listener.transform, Physics.DefaultRaycastLayers);
            check(Mathf.Abs(transmission - 0.25f) < 0.001f, "separate barriers attenuate cumulatively");
            NoiseSystem.Emit(emitter.transform.position, 20f, emitter);
            check(heard == 5, "two walls prevent a noise that passes through one");
            NoiseSystem.Emit(emitter.transform.position, 40f, emitter);
            check(heard == 6, "stacked attenuation preserves a finite loudness threshold");
            wall.SetActive(false);
            secondWall.SetActive(false);

            var composite = Child(root, "Composite Door", Vector3.zero);
            composite.AddComponent<AcousticObstacle>().transmission = 0.5f;
            Child(composite, "Door Panel A", Vector3.right * 3f).AddComponent<BoxCollider>().size = new Vector3(0.2f, 3f, 3f);
            Child(composite, "Door Panel B", Vector3.right * 4f).AddComponent<BoxCollider>().size = new Vector3(0.2f, 3f, 3f);
            Physics.SyncTransforms();
            transmission = SoundPropagation.Transmission(emitter.transform.position, listener.transform.position,
                emitter.transform, listener.transform, Physics.DefaultRaycastLayers);
            check(Mathf.Abs(transmission - 0.5f) < 0.001f, "multi-collider door is counted once per acoustic profile");
            check(SoundPropagation.Transmission(emitter.transform.position, listener.transform.position,
                emitter.transform, listener.transform, 0) == 1f, "empty layer mask bypasses obstacles");
            composite.SetActive(false);

            emitter.AddComponent<BoxCollider>();
            listener.AddComponent<BoxCollider>();
            Physics.SyncTransforms();
            check(SoundPropagation.Transmission(emitter.transform.position, listener.transform.position,
                emitter.transform, listener.transform, Physics.DefaultRaycastLayers) == 1f,
                "source and listener body colliders do not muffle themselves");
            wall.SetActive(true);
            collider.isTrigger = true;
            Physics.SyncTransforms();
            check(SoundPropagation.Transmission(emitter.transform.position, listener.transform.position,
                emitter.transform, listener.transform, Physics.DefaultRaycastLayers) == 1f,
                "gameplay trigger volumes do not block hearing");
            collider.isTrigger = false;
            wall.layer = 2; // Unity's Ignore Raycast layer.
            Physics.SyncTransforms();
            check(SoundPropagation.Transmission(emitter.transform.position, listener.transform.position,
                emitter.transform, listener.transform, Physics.DefaultRaycastLayers) == 1f,
                "configured obstacle layers are respected");
            wall.layer = 0;
            profile.transmission = 0f;

            var clock = Child(root, "Cover Clock", Vector3.forward * 0.2f);
            var audio = clock.AddComponent<AudioSource>();
            audio.spatialBlend = 1f;
            audio.volume = 0.05f;
            clip = AudioClip.Create("Occluded Cover Check", 22050, 1, 22050, false);
            var samples = new float[22050];
            for (int i = 0; i < samples.Length; i++) samples[i] = 0.1f * Mathf.Sin(i * 2f * Mathf.PI * 440f / 22050f);
            clip.SetData(samples, 0);
            audio.clip = clip;
            audio.loop = true;
            var cover = clock.AddComponent<NoiseMaskingSource>();
            cover.radius = 20f;
            cover.hearingRadiusMultiplier = 0.25f;
            cover.PlayCover();
            Physics.SyncTransforms();
            yield return null;
            check(cover.IsMasking, "cover fixture has an actively playing source");
            check(NoiseMaskingSource.GetMultiplier(emitter.transform.position, listener.transform.position,
                listener.transform, Physics.DefaultRaycastLayers) == 1f,
                "an inaudible clock behind a blocking wall provides no cover");
            wall.SetActive(false);
            Physics.SyncTransforms();
            check(Mathf.Abs(NoiseMaskingSource.GetMultiplier(emitter.transform.position, listener.transform.position,
                listener.transform, Physics.DefaultRaycastLayers) - 0.25f) < 0.001f,
                "opening the sound path restores clock cover");
        }
        finally
        {
            UnityEngine.Object.Destroy(root);
            if (clip != null) UnityEngine.Object.Destroy(clip);
        }
        yield return null;
    }

    private static IEnumerator CheckSuspicion(Action<bool, string> check)
    {
        var root = new GameObject("Suspicion Checks");
        root.transform.position = new Vector3(6000f, 0f, 6000f);
        float savedTimeScale = Time.timeScale;
        try
        {
            var actor = Child(root, "Memory Listener", Vector3.zero);
            var emitter = Child(root, "Remembered Source", Vector3.right * 8f);
            var ears = actor.AddComponent<NoiseListener>();
            ears.earOffset = Vector3.zero;
            ears.useOcclusion = false;
            var memory = actor.AddComponent<EnemySuspicion>();
            int requests = 0;
            int warySignals = 0;
            int alarmSignals = 0;
            memory.onInvestigationRequested.AddListener(_ => requests++);
            memory.onStateChanged.AddListener(state =>
            {
                if (state == EnemySuspicion.State.Wary) warySignals++;
                if (state == EnemySuspicion.State.Alarmed) alarmSignals++;
            });
            NoiseSystem.Emit(emitter.transform.position, 10f, emitter, NoiseSystem.Kind.Footstep);
            check(memory.CurrentState == EnemySuspicion.State.Wary && memory.Amount > 0f && requests == 0,
                "one faint sound warns without starting a pursuit");
            Quaternion beforeTurn = actor.transform.rotation;
            yield return WaitFor(() => Quaternion.Angle(actor.transform.rotation, beforeTurn) > 0.1f,
                2f, "idle creature turns toward remembered noise");
            check(Vector3.Distance(actor.transform.position, root.transform.position) < 0.001f,
                "warning turns the creature without moving it toward the player");
            memory.orientWhenIdle = false;
            for (int i = 0; i < 7; i++) NoiseSystem.Emit(emitter.transform.position, 10f, emitter, NoiseSystem.Kind.Footstep);
            check(memory.CurrentState == EnemySuspicion.State.Alarmed && requests == 1,
                "repeated faint sounds accumulate into an investigation");
            check(warySignals == 1 && alarmSignals == 1, "state cues do not spam for every footstep");
            NoiseSystem.Emit(emitter.transform.position, 10f, emitter, NoiseSystem.Kind.Footstep);
            check(requests == 1, "ordinary repeated sounds respect request cooldown");
            Vector3 loudPosition = actor.transform.position + Vector3.right * 0.2f;
            NoiseSystem.Emit(loudPosition, 6f, emitter, NoiseSystem.Kind.Footstep);
            check(requests == 2 && memory.LastKnownPosition == loudPosition,
                "a close loud action overrides cooldown immediately");
            emitter.transform.position += Vector3.forward * 30f;
            check(memory.LastKnownPosition == loudPosition, "memory does not follow a silent source transform");
            emitter.transform.localPosition = Vector3.right * 8f;
            memory.ClearMemory();
            check(!memory.HasMemory && memory.CurrentState == EnemySuspicion.State.Calm,
                "explicit memory reset clears suspicion and attention");
            memory.calmDelay = 0.05f;
            memory.decayPerSecond = 10f;
            NoiseSystem.Emit(emitter.transform.position, 10f, emitter);
            float pausedAmount = memory.Amount;
            Time.timeScale = 0f;
            yield return null;
            yield return null;
            check(memory.Amount == pausedAmount && memory.HasMemory, "pause freezes suspicion decay");
            Time.timeScale = savedTimeScale;
            yield return WaitFor(() => !memory.HasMemory, 3f, "silence calms the creature");
            check(memory.CurrentState == EnemySuspicion.State.Calm && memory.LastKnownPosition == Vector3.zero,
                "quiet period forgets the location instead of searching forever");
            memory.enabled = false;
            NoiseSystem.Emit(loudPosition, 6f, emitter);
            check(memory.Amount == 0f, "disabled memory does not receive hidden noise events");
            memory.enabled = true;
            var progression = actor.AddComponent<GameProgression>();
            actor.AddComponent<EnemyThreat>().progression = progression;
            int beforeDormant = requests;
            NoiseSystem.Emit(loudPosition, 6f, emitter);
            check(memory.Amount == 0f && requests == beforeDormant, "exploration phase cannot accidentally awaken suspicion");
            progression.Awaken();
            NoiseSystem.Emit(emitter.transform.position, 10f, emitter, NoiseSystem.Kind.Decoy);
            check(memory.CurrentState == EnemySuspicion.State.Alarmed && requests == beforeDormant + 1,
                "an audible decoy can deliberately attract attention");
            check(!progression.EnemyCanCatch, "warning-phase investigation still cannot capture the player");
        }
        finally
        {
            Time.timeScale = savedTimeScale;
            UnityEngine.Object.Destroy(root);
        }
    }

    private static GameObject Child(GameObject parent, string name, Vector3 localPosition)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent.transform, false);
        child.transform.localPosition = localPosition;
        return child;
    }

    private static IEnumerator WaitFor(Func<bool> condition, float timeout, string description)
    {
        float until = Time.realtimeSinceStartup + timeout;
        while (!condition())
        {
            if (Time.realtimeSinceStartup >= until) throw new TimeoutException("Hearing check timeout: " + description);
            yield return null;
        }
    }
}
