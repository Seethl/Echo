using UnityEngine;
using UnityEngine.Events;

/// <summary>Explicit puzzle milestones, not a timer or an automatic chase escalation.</summary>
public class GameProgression : MonoBehaviour
{
    public enum Stage { Exploration, Awakening, Danger, SoundMastery }
    [SerializeField] private Stage stage = Stage.Exploration;
    public UnityEvent<Stage> onStageChanged = new UnityEvent<Stage>();
    public Stage CurrentStage => stage;
    public bool EnemyCanHear => stage != Stage.Exploration;
    public bool EnemyCanCatch => stage >= Stage.Danger;
    public bool SoundToolUnlocked => stage == Stage.SoundMastery;

    // Suitable for Inspector UnityEvents on future puzzle completion components.
    public void Awaken() { Advance(Stage.Awakening); }
    public void RaiseDanger() { Advance(Stage.Danger); }
    public void UnlockSoundTool() { Advance(Stage.SoundMastery); }

    private void Advance(Stage next)
    {
        if (next <= stage) return;
        stage = next;
        onStageChanged.Invoke(stage);
    }
}
