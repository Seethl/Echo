using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Small ordered objective chain. No UI ownership or persistent global state.</summary>
public class ObjectiveSequence : MonoBehaviour
{
    [Serializable]
    public class Step
    {
        public string id;
        public string title;
        public GameProgression.Stage stageOnCompletion;
        public UnityEvent onCompleted = new UnityEvent();
    }

    public Step[] steps = Array.Empty<Step>();
    public GameProgression progression;
    public UnityEvent<string> onObjectiveChanged = new UnityEvent<string>();
    public UnityEvent onAllCompleted = new UnityEvent();

    private Step[] configuredSteps;
    private int current;
    private bool notifying;
    public bool IsConfigured => EnsureConfigured();
    public bool IsCompleted => EnsureConfigured() && current == configuredSteps.Length;
    public int CompletedCount => current;
    public string CurrentId => EnsureConfigured() && !IsCompleted ? configuredSteps[current].id : "";
    public string CurrentTitle => EnsureConfigured() && !IsCompleted ? configuredSteps[current].title : "";

    private bool EnsureConfigured()
    {
        if (configuredSteps != null) return true;
        if (steps == null || steps.Length == 0) return false;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var step in steps)
            if (step == null || string.IsNullOrWhiteSpace(step.id) || !ids.Add(step.id)) return false;
        configuredSteps = new Step[steps.Length];
        for (int i = 0; i < steps.Length; i++)
            configuredSteps[i] = new Step { id = steps[i].id, title = steps[i].title,
                stageOnCompletion = steps[i].stageOnCompletion, onCompleted = steps[i].onCompleted };
        return true;
    }

    void Start() { if (EnsureConfigured()) onObjectiveChanged.Invoke(CurrentId); }

    public void CompleteObjective(string id) { TryComplete(id); }

    public bool TryComplete(string id)
    {
        if (!isActiveAndEnabled || notifying || Echo.Menu.GameMenu.BlocksPlayerInput || Time.timeScale <= 0f ||
            !EnsureConfigured() || IsCompleted || !string.Equals(CurrentId, id, StringComparison.Ordinal)) return false;
        var completed = configuredSteps[current++];
        notifying = true;
        try
        {
            // Advance state before notification; callbacks cannot complete the whole chain recursively.
            if (progression != null)
            {
                switch (completed.stageOnCompletion)
                {
                    case GameProgression.Stage.Awakening: progression.Awaken(); break;
                    case GameProgression.Stage.Danger: progression.RaiseDanger(); break;
                    case GameProgression.Stage.SoundMastery: progression.UnlockSoundTool(); break;
                }
            }
            completed.onCompleted?.Invoke();
            onObjectiveChanged.Invoke(CurrentId);
            if (IsCompleted) onAllCompleted.Invoke();
        }
        finally { notifying = false; }
        return true;
    }
}
