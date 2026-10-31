using System;
using System.Collections;
using UnityEngine;

/// <summary>Small runtime integration check, invoked only by the separate lab scene.</summary>
public static class SoundProgressionChecks
{
    public static IEnumerator Run()
    {
        // Let the persistent menu finish its scene transition before emitting test noises.
        yield return null;
        yield return null;
        if (Echo.Menu.GameMenu.BlocksPlayerInput) yield break;
        var enemy = new GameObject("Progression Check Listener");
        var clock = new GameObject("Progression Check Clock");
        AudioClip clip = null;
        float savedTimeScale = Time.timeScale;
        int assertions = 0;
        Action<bool, string> check = (condition, message) =>
        {
            if (!condition) throw new InvalidOperationException("Progression check: " + message);
            assertions++;
        };
        try
        {
            enemy.transform.position = new Vector3(1000f, 0f, 1000f);
            clock.transform.position = enemy.transform.position + Vector3.right * 1.5f;
            var progression = enemy.AddComponent<GameProgression>();
            var threat = enemy.AddComponent<EnemyThreat>();
            threat.progression = progression;
            var ears = enemy.AddComponent<NoiseListener>();
            int heard = 0;
            int transitions = 0;
            ears.onNoiseHeard.AddListener(_ => heard++);
            progression.onStageChanged.AddListener(_ => transitions++);
            Vector3 step = enemy.transform.position + Vector3.right * 3f;

            NoiseSystem.Emit(step, 6f, clock);
            check(heard == 0 && !threat.CanCatch, "exploration must be safe");
            progression.Awaken();
            NoiseSystem.Emit(step, 6f, clock);
            check(heard == 1 && !threat.CanCatch, "awakening hears without capture");
            progression.Awaken();
            check(transitions == 1, "repeated puzzle event must be idempotent");

            var audio = clock.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.volume = 0.05f;
            clip = AudioClip.Create("Progression Check Tone", 22050, 1, 22050, false);
            var samples = new float[22050];
            for (int i = 0; i < samples.Length; i++) samples[i] = 0.1f * Mathf.Sin(i * 2f * Mathf.PI * 440f / 22050f);
            clip.SetData(samples, 0);
            audio.clip = clip;
            var cover = clock.AddComponent<NoiseMaskingSource>();
            cover.radius = 10f;
            cover.hearingRadiusMultiplier = 0.25f;
            check(!cover.IsMasking, "stopped audio provides no cover");
            cover.PlayCover();
            yield return null;
            check(cover.IsMasking, "playing audio activates cover");
            NoiseSystem.Emit(step, 6f, clock);
            check(heard == 1, "covered walking is not heard at three metres");
            NoiseSystem.Emit(step, 20f, clock);
            check(heard == 2, "loud noise can overcome cover");
            audio.mute = true;
            NoiseSystem.Emit(step, 6f, clock);
            check(heard == 3, "muted source provides no cover");
            audio.mute = false;
            cover.enabled = false;
            NoiseSystem.Emit(step, 6f, clock);
            check(heard == 4, "disabled cover restores hearing");
            cover.enabled = true;
            check(NoiseMaskingSource.GetMultiplier(step, Vector3.zero) == 1f, "remote listeners are not covered");
            audio.Stop();
            Time.timeScale = 0f;
            NoiseSystem.Emit(step, 6f, clock);
            check(heard == 4, "paused gameplay ignores noise");
            Time.timeScale = savedTimeScale;
            progression.RaiseDanger();
            check(threat.CanCatch, "danger enables capture");
            progression.Awaken();
            check(progression.CurrentStage == GameProgression.Stage.Danger, "old puzzle events cannot lower danger");
            progression.UnlockSoundTool();
            check(progression.SoundToolUnlocked && transitions == 3, "mastery milestone fires once");
            int beforeDecoy = heard;
            NoiseSystem.Emit(step, 8f, clock, NoiseSystem.Kind.Decoy);
            check(heard == beforeDecoy + 1 && ears.LastHeardCategory == NoiseSystem.Kind.Decoy,
                "decoy obtains attention");
            NoiseSystem.Emit(step, 6f, clock, NoiseSystem.Kind.Footstep);
            check(heard == beforeDecoy + 1, "quiet footsteps do not immediately cancel distraction");
            NoiseSystem.Emit(enemy.transform.position + Vector3.right * 0.5f, 6f, clock, NoiseSystem.Kind.Footstep);
            check(heard == beforeDecoy + 2 && ears.LastHeardCategory == NoiseSystem.Kind.Footstep,
                "nearby loud movement breaks distraction");
            var tool = enemy.AddComponent<PlayerSoundTool>();
            tool.keyboardControls = false;
            check(!tool.IsUnlocked && !tool.TryPlace(), "unconfigured tool cannot bypass progression");
            tool.progression = progression;
            check(tool.IsUnlocked && !tool.TryPlace(), "unlocked tool without a prefab fails safely");
            var alarm = clock.AddComponent<AlarmDecoy>();
            alarm.Arm(tool, 1f);
            check(alarm.CurrentState == AlarmDecoy.State.Armed, "alarm starts with countdown, not instant noise");
            alarm.enabled = false;
            check(alarm.CurrentState == AlarmDecoy.State.Spent, "disabling a clock cancels its pending alarm");
            Debug.Log($"Sound progression checks: {assertions} passed.");
        }
        finally
        {
            Time.timeScale = savedTimeScale;
            UnityEngine.Object.Destroy(enemy);
            UnityEngine.Object.Destroy(clock);
            if (clip != null) UnityEngine.Object.Destroy(clip);
        }
        yield return GameplayMechanicsChecks.Run();
        yield return HearingAndSuspicionChecks.Run();
        yield return ManorIntegrationChecks.Run();
    }
}
