using System;
using System.Collections;
using Echo.Audio;
using Echo.Interactions;
using Echo.Manor;
using Echo.Radio;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ManorIntegrationChecks
{
    public static IEnumerator Run()
    {
        if (ManorProgress.Active != null) yield break;
        int count = 0;
        Action<bool, string> check = (condition, message) =>
        {
            if (!condition) throw new InvalidOperationException("Manor integration check: " + message);
            count++;
        };
        var scene = SceneManager.CreateScene("Manor Integration Checks");
        var root = new GameObject("Manor Integration Fixture");
        root.transform.position = new Vector3(7000f, 0f, 7000f);
        SceneManager.MoveGameObjectToScene(root, scene);
        AudioClip clip = null;
        RadioChannel channel = null;
        Action<NoiseSystem.Noise> track = null;
        try
        {
            var playerObject = Child(root, "Fixture Player", Vector3.zero);
            Child(playerObject, "Fixture Camera", Vector3.up).AddComponent<Camera>();
            var player = playerObject.AddComponent<PlayerInteractor>();
            player.ui = playerObject.AddComponent<InteractionUI>();
            var manor = root.AddComponent<ManorProgress>();
            manor.enabled = false; // Exercise its state without running a second level/HUD loop.
            manor.player = player;
            var bridge = root.GetComponent<ManorGameplayBridge>();
            check(bridge.Progression.CurrentStage == GameProgression.Stage.Exploration, "new manor begins safely");
            manor.Set("hall.open"); bridge.Synchronize();
            check(bridge.Progression.EnemyCanHear && !bridge.Progression.EnemyCanCatch, "hall opening gives a safe awakening");
            manor.Set("library.cache"); bridge.Synchronize();
            check(bridge.Progression.CurrentStage == GameProgression.Stage.Danger, "library puzzle advances danger");
            manor.Set("grate.open"); bridge.Synchronize();
            check(bridge.Progression.SoundToolUnlocked, "later mechanism unlocks sound mastery");
            bridge.Synchronize();
            check(bridge.Progression.CurrentStage == GameProgression.Stage.SoundMastery, "repeated synchronization preserves progress");

            clip = AudioClip.Create("Integration Short Music", 2205, 1, 22050, false);
            var samples = new float[2205];
            for (int i = 0; i < samples.Length; i++) samples[i] = 0.1f * Mathf.Sin(i * 2f * Mathf.PI * 440f / 22050f);
            clip.SetData(samples, 0);
            channel = ScriptableObject.CreateInstance<RadioChannel>();
            channel.mode = RadioChannel.Mode.Music; channel.frequency = 1140;
            var radio = Child(root, "Fixture Radio", Vector3.zero).AddComponent<PortableRadio>();
            radio.channels = new[] { channel }; radio.frequency = 1140; radio.musicClip = clip;
            radio.musicSeconds = 20f; radio.silenceSeconds = 6f;
            radio.Interact(player);
            radio.powered = true;
            var source = radio.GetComponent<AudioSource>();
            int pulses = 0;
            track = noise => { if (noise.Source == radio.gameObject) pulses++; };
            NoiseSystem.Emitted += track;
            yield return WaitFor(() => source.isPlaying, 2f, "music starts");
            check(radio.MusicAudible && source.loop, "short custom music loops through the declared music phase");
            radio.TogglePower();
            yield return null; yield return null;
            check(!source.isPlaying && !radio.MusicAudible, "power off stops both sound and audible state");
            int silentPulses = pulses;
            yield return null;
            check(pulses == silentPulses, "powered-off radio emits no gameplay sound");
            radio.TogglePower();
            yield return WaitFor(() => source.isPlaying && radio.MusicAudible, 2f, "music resumes after power on");
            check(source.isPlaying, "power cycling within one music phase restores real audio");
            radio.SetVolume(0f);
            yield return null; yield return null;
            check(!source.isPlaying && !radio.MusicAudible, "zero radio volume stops the broadcast");
            radio.SetVolume(0.4f);
            yield return WaitFor(() => source.isPlaying && radio.MusicAudible, 2f, "music resumes from zero volume");
            check(source.isPlaying, "volume recovery restores real audio");
            yield return WaitFor(() => pulses > silentPulses, 2f, "audible radio produces a gameplay pulse");
            check(pulses > silentPulses, "radio and shared noise contract are connected");

            var shutter = Child(root, "Fixture Shutter", Vector3.right * 40f);
            var ambient = shutter.AddComponent<AmbientCycle>();
            ambient.mode = AmbientCycle.Mode.Shutter; ambient.loudClip = clip;
            var mask = shutter.GetComponent<NoiseMaskZone>();
            yield return WaitFor(() => mask.active, 2f, "shutter produces a cover window");
            check(NoiseMaskZone.CoverAt(shutter.transform.position) > 0f, "audible shutter provides local cover");
            yield return WaitFor(() => !mask.active, 2f, "short shutter sound ends");
            check(NoiseMaskZone.CoverAt(shutter.transform.position) == 0f, "silence between shutter impacts provides no cover");
            ambient.enabled = false;
            check(!mask.active && !shutter.GetComponent<AudioSource>().isPlaying, "disabled ambient source clears cover and audio");
            Debug.Log($"Manor integration checks: {count} passed.");
        }
        finally
        {
            if (track != null) NoiseSystem.Emitted -= track;
            UnityEngine.Object.Destroy(root);
            if (clip != null) UnityEngine.Object.Destroy(clip);
            if (channel != null) UnityEngine.Object.Destroy(channel);
        }
        yield return SceneManager.UnloadSceneAsync(scene);
    }

    private static GameObject Child(GameObject parent, string name, Vector3 position)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent.transform, false);
        child.transform.localPosition = position;
        return child;
    }

    private static IEnumerator WaitFor(Func<bool> condition, float timeout, string description)
    {
        float end = Time.realtimeSinceStartup + timeout;
        while (!condition())
        {
            if (Time.realtimeSinceStartup >= end) throw new TimeoutException("Integration timeout: " + description);
            yield return null;
        }
    }
}
