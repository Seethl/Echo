using System;
using System.Collections;
using System.Collections.Generic;
using Echo.Puzzles;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Runtime checks used only by the existing sound laboratory, never by the manor scene.</summary>
public static class GameplayMechanicsChecks
{
    public static IEnumerator Run()
    {
        int checks = 0;
        Action<bool, string> check = (condition, message) =>
        {
            if (!condition) throw new InvalidOperationException("Gameplay mechanics check: " + message);
            checks++;
        };
        var root = new GameObject("Gameplay Mechanics Checks");
        root.transform.position = new Vector3(3000f, 0f, 3000f);
        AudioClip low = null;
        AudioClip high = null;
        float savedTimeScale = Time.timeScale;
        try
        {
            var input = new[] { 0, 1, 0 };
            var rules = new SoundSequenceState(input, 2);
            input[0] = 1;
            check(rules.Submit(0) == SoundSequenceState.Result.Correct, "sequence is copied, not mutated by its designer array");
            check(rules.Submit(9) == SoundSequenceState.Result.Rejected && rules.Position == 1, "unknown tone preserves progress");
            check(rules.Submit(0) == SoundSequenceState.Result.Mistake && rules.Position == 0, "wrong tone restarts only the attempt");
            rules.Submit(0); rules.ResetAttempt();
            check(rules.Position == 0, "manual retry clears partial progress");
            rules.Submit(0); rules.Submit(1);
            check(rules.Submit(0) == SoundSequenceState.Result.Solved, "repeated tones are supported");
            rules.ResetAttempt();
            check(rules.Submit(0) == SoundSequenceState.Result.Rejected && rules.IsSolved, "solved puzzle cannot pay rewards twice");
            bool invalid = false;
            try { new SoundSequenceState(new[] { 2 }, 2); } catch (ArgumentException) { invalid = true; }
            check(invalid, "invalid puzzle configuration is rejected");

            var progression = root.AddComponent<GameProgression>();
            var objectives = root.AddComponent<ObjectiveSequence>();
            objectives.progression = progression;
            objectives.steps = new[]
            {
                new ObjectiveSequence.Step { id = "listen", title = "Listen", stageOnCompletion = GameProgression.Stage.Awakening },
                new ObjectiveSequence.Step { id = "escape", title = "Escape", stageOnCompletion = GameProgression.Stage.Danger }
            };
            int completed = 0;
            objectives.onAllCompleted.AddListener(() => completed++);
            bool recursiveCompletion = false;
            objectives.steps[0].onCompleted.AddListener(() => recursiveCompletion = objectives.TryComplete("escape"));
            check(objectives.IsConfigured && objectives.CurrentId == "listen", "objective chain starts at its first step");
            check(!objectives.TryComplete("escape") && objectives.CompletedCount == 0, "future objective cannot skip a puzzle");
            var invalidObject = new GameObject("Invalid Objective Configuration");
            invalidObject.transform.SetParent(root.transform, false);
            var invalidObjectives = invalidObject.AddComponent<ObjectiveSequence>();
            invalidObjectives.steps = new[] { new ObjectiveSequence.Step { id = "same" }, new ObjectiveSequence.Step { id = "same" } };
            check(!invalidObjectives.IsConfigured && !invalidObjectives.IsCompleted, "duplicate objective IDs do not produce a free victory");

            low = MakeTone("Check Low", 330f);
            high = MakeTone("Check High", 660f);
            var puzzle = root.AddComponent<SoundSequencePuzzle>();
            root.GetComponent<AudioSource>().spatialBlend = 1f;
            puzzle.tones = new[] { low, high };
            puzzle.sequence = new[] { 0, 1, 0 };
            puzzle.gapBetweenTones = 0f;
            int solved = 0;
            int mistakes = 0;
            var played = new List<int>();
            puzzle.onTonePlayed.AddListener(tone => played.Add(tone));
            puzzle.onMistake.AddListener(() => mistakes++);
            puzzle.onSolved.AddListener(() => { solved++; objectives.CompleteObjective("listen"); });
            check(puzzle.TrySubmitTone(0) == SoundSequenceState.Result.Rejected, "answer requires listening to the clue first");
            check(puzzle.TryPlayClue(), "configured clue starts");
            check(!puzzle.TryPlayClue() && puzzle.TrySubmitTone(0) == SoundSequenceState.Result.Rejected,
                "clue playback cannot overlap itself or accept an early answer");
            puzzle.enabled = false;
            yield return null;
            check(!puzzle.IsPlayingClue && !puzzle.HasListened, "disabling playback cancels without granting listening credit");
            puzzle.enabled = true;
            played.Clear();
            check(puzzle.TryPlayClue(), "cancelled clue can be replayed");
            yield return WaitFor(() => !puzzle.IsPlayingClue, 3f, "clue finishes");
            check(puzzle.HasListened && played.Count == 3 && played[0] == 0 && played[1] == 1 && played[2] == 0,
                "audio and visual callbacks follow the intended pattern");
            Time.timeScale = 0f;
            check(puzzle.TrySubmitTone(0) == SoundSequenceState.Result.Rejected && !objectives.TryComplete("listen"),
                "paused input cannot solve puzzles or objectives");
            Time.timeScale = savedTimeScale;
            puzzle.TrySubmitTone(0);
            check(puzzle.TrySubmitTone(0) == SoundSequenceState.Result.Mistake && mistakes == 1 && puzzle.CorrectCount == 0,
                "wrong answer sends feedback and permits retry");
            puzzle.TrySubmitTone(0); puzzle.TrySubmitTone(1); puzzle.TrySubmitTone(0);
            check(solved == 1 && objectives.CurrentId == "escape" && progression.CurrentStage == GameProgression.Stage.Awakening,
                "solving the listening puzzle advances its objective and awakens the threat");
            check(!recursiveCompletion && objectives.CompletedCount == 1, "reward callback cannot recursively finish the objective chain");
            puzzle.TrySubmitTone(0);
            check(solved == 1 && !objectives.TryComplete("listen"), "repeated solve signals are idempotent");
            check(objectives.TryComplete("escape") && objectives.IsCompleted && completed == 1 && progression.EnemyCanCatch,
                "final objective completes the chain and advances danger once");
            check(!objectives.TryComplete("escape") && completed == 1, "completion callback cannot be duplicated");

            var machineryObject = new GameObject("Timed Mechanism Checks");
            machineryObject.transform.SetParent(root.transform, false);
            var machinery = machineryObject.AddComponent<TimedNoiseMechanism>();
            machineryObject.GetComponent<AudioSource>().spatialBlend = 1f;
            int machineryCompletions = 0;
            int cancellations = 0;
            machinery.onCompleted.AddListener(() => machineryCompletions++);
            machinery.onCancelled.AddListener(() => cancellations++);
            check(!machinery.TryBegin(), "machinery cannot create silent gameplay noise without its clip");
            machinery.loopClip = low;
            machinery.duration = 0.15f;
            check(machinery.TryBegin() && !machinery.TryBegin(), "running machinery cannot be started twice");
            Time.timeScale = 0f;
            yield return null;
            yield return null;
            check(machinery.IsRunning && machinery.Progress == 0f && machineryCompletions == 0,
                "paused machinery neither progresses nor grants a reward");
            Time.timeScale = savedTimeScale;
            machinery.Cancel();
            check(!machinery.IsRunning && machinery.Progress == 0f && cancellations == 1 && machineryCompletions == 0,
                "cancellation resets work without completing its objective");
            machinery.Cancel();
            check(cancellations == 1 && machinery.TryBegin(), "cancel is idempotent and the action can be retried");
            machinery.enabled = false;
            check(!machinery.IsRunning && cancellations == 2, "disabled machinery does not finish itself in the background");
            machinery.enabled = true;
            UnityEngine.Events.UnityAction disableAtStart = () => machinery.enabled = false;
            machinery.onStarted.AddListener(disableAtStart);
            check(!machinery.TryBegin() && !machinery.IsRunning && cancellations == 3,
                "a scene callback disabling machinery cannot leave it running");
            machinery.onStarted.RemoveListener(disableAtStart);
            machinery.enabled = true;
            check(machinery.TryBegin(), "reactivated machinery can be retried");
            yield return WaitFor(() => machinery.IsCompleted, 3f, "machinery finishes its full duration");
            check(machinery.Progress == 1f && machineryCompletions == 1 && !machinery.TryBegin(),
                "machinery grants its result exactly once after completion");

            yield return CheckEnemy(check);
            Debug.Log($"Gameplay mechanics checks: {checks} passed.");
        }
        finally
        {
            Time.timeScale = savedTimeScale;
            UnityEngine.Object.Destroy(root);
            if (low != null) UnityEngine.Object.Destroy(low);
            if (high != null) UnityEngine.Object.Destroy(high);
        }
    }

    private static AudioClip MakeTone(string name, float frequency)
    {
        const int rate = 22050;
        var samples = new float[441];
        for (int i = 0; i < samples.Length; i++) samples[i] = 0.1f * Mathf.Sin(i * 2f * Mathf.PI * frequency / rate);
        var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static IEnumerator CheckEnemy(Action<bool, string> check)
    {
        var center = new Vector3(4000f, 0f, 4000f);
        var sources = new List<NavMeshBuildSource>
        {
            new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box, size = new Vector3(12f, 0.2f, 12f),
                transform = Matrix4x4.TRS(center - Vector3.up * 0.1f, Quaternion.identity, Vector3.one), area = 0 }
        };
        var data = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(0), sources,
            new Bounds(center, new Vector3(14f, 6f, 14f)), Vector3.zero, Quaternion.identity);
        check(data != null, "test navigation builds without changing any scene asset");
        var navigation = NavMesh.AddNavMeshData(data);
        var actor = new GameObject("Investigation Check Enemy");
        NavMeshAgent agent = null;
        float savedTimeScale = Time.timeScale;
        try
        {
            check(NavMesh.SamplePosition(center, out NavMeshHit spawn, 2f, NavMesh.AllAreas), "test enemy has a walkable spawn");
            actor.transform.position = spawn.position;
            agent = actor.AddComponent<NavMeshAgent>();
            agent.speed = 5f;
            agent.acceleration = 30f;
            agent.stoppingDistance = 0.15f;
            var investigator = actor.AddComponent<EnemyInvestigator>();
            investigator.searchDuration = 0.15f;
            investigator.investigationTimeout = 5f;
            investigator.stuckTimeout = 2f;
            bool searched = false;
            investigator.onStateChanged.AddListener(state => { if (state == EnemyInvestigator.State.Searching) searched = true; });
            investigator.Investigate(center + Vector3.right * 100f);
            check(investigator.CurrentState == EnemyInvestigator.State.Idle, "unreachable noise does not create a partial pursuit");
            var ears = actor.GetComponent<NoiseListener>();
            ears.earOffset = Vector3.zero;
            ears.useOcclusion = false;
            var memory = actor.GetComponent<EnemySuspicion>();
            memory.investigationThreshold = 0.35f;
            Vector3 faintPosition = actor.transform.position + Vector3.right * 1.5f;
            NoiseSystem.Emit(faintPosition, 2f, null, NoiseSystem.Kind.Footstep);
            check(investigator.CurrentState == EnemyInvestigator.State.Idle && memory.CurrentState == EnemySuspicion.State.Wary,
                "navigation does not bypass suspicion on the first faint footstep");
            for (int i = 0; i < 3; i++) NoiseSystem.Emit(faintPosition, 2f, null, NoiseSystem.Kind.Footstep);
            check(investigator.CurrentState == EnemyInvestigator.State.Investigating,
                "accumulated suspicion requests a real navigation path");
            investigator.Investigate(center + Vector3.right * 1.5f);
            check(investigator.CurrentState == EnemyInvestigator.State.Investigating, "reachable noise starts investigation");
            Time.timeScale = 0f;
            var pausedPosition = actor.transform.position;
            yield return null;
            yield return null;
            check((actor.transform.position - pausedPosition).sqrMagnitude < 0.0001f, "pause stops enemy travel");
            Time.timeScale = savedTimeScale;
            yield return WaitFor(() => investigator.CurrentState == EnemyInvestigator.State.Returning, 5f, "enemy finishes local search and returns");
            check(searched, "enemy searches the heard location before returning");
            investigator.Investigate(center - Vector3.right * 1.5f);
            check(investigator.CurrentState == EnemyInvestigator.State.Investigating &&
                Vector3.Distance(investigator.LastInvestigatedPosition, center - Vector3.right * 1.5f) < 0.3f,
                "new noise interrupts returning without tracking the player");
            yield return WaitFor(() => investigator.CurrentState == EnemyInvestigator.State.Idle, 8f, "enemy returns home");
            check(Vector3.Distance(actor.transform.position, investigator.HomePosition) < 0.4f, "finished investigation releases the route and returns home");
            investigator.returnToStart = false;
            investigator.stuckTimeout = 0.15f;
            investigator.Investigate(center + Vector3.right * 2f);
            agent.isStopped = true;
            yield return WaitFor(() => investigator.CurrentState == EnemyInvestigator.State.Idle, 2f, "blocked enemy abandons an unproductive route");
            check(!agent.hasPath, "stuck recovery clears its path");
        }
        finally
        {
            Time.timeScale = savedTimeScale;
            if (agent != null) agent.enabled = false;
            UnityEngine.Object.Destroy(actor);
            if (navigation.valid) navigation.Remove();
            if (data != null) UnityEngine.Object.Destroy(data);
        }
    }

    private static IEnumerator WaitFor(Func<bool> condition, float timeout, string description)
    {
        float until = Time.realtimeSinceStartup + timeout;
        while (!condition())
        {
            if (Time.realtimeSinceStartup >= until) throw new TimeoutException("Gameplay check timeout: " + description);
            yield return null;
        }
    }
}
