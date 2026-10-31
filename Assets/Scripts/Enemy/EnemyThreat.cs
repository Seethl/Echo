using UnityEngine;

/// <summary>Optional link: legacy test enemies keep their previous behaviour without it.</summary>
public class EnemyThreat : MonoBehaviour
{
    public GameProgression progression;
    public bool CanHear => progression != null && progression.EnemyCanHear;
    public bool CanCatch => progression != null && progression.EnemyCanCatch;
}
