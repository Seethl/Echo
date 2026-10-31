using Echo.Manor;
using UnityEngine;

[DefaultExecutionOrder(-9700)]
public sealed class ManorGameplayBridge : MonoBehaviour
{
    public GameProgression Progression { get; private set; }
    private ManorProgress manor;

    void Awake()
    {
        manor = GetComponent<ManorProgress>();
        Progression = GetComponent<GameProgression>();
        if (Progression == null) Progression = gameObject.AddComponent<GameProgression>();
        foreach (var enemy in FindObjectsByType<EnemyInvestigator>(FindObjectsInactive.Include))
        {
            if (enemy.gameObject.scene != gameObject.scene) continue;
            var threat = enemy.GetComponent<EnemyThreat>();
            if (threat == null) threat = enemy.gameObject.AddComponent<EnemyThreat>();
            threat.progression = Progression;
        }
        Synchronize();
    }

    void Start() { Synchronize(); }
    void Update() { Synchronize(); }

    public void Synchronize()
    {
        if (manor == null) return;
        if (manor.Has("hall.open")) Progression.Awaken();
        if (manor.Has("library.cache")) Progression.RaiseDanger();
        if (manor.Has("grate.open")) Progression.UnlockSoundTool();
    }
}
