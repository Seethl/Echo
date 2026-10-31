using UnityEngine;

/// <summary>Animation events on the visual child's Animator call Footstep().</summary>
public sealed class EnemyFootstepRelay : MonoBehaviour
{
    public EnemyInvestigator enemy;
    private void Awake(){if(enemy==null)enemy=GetComponentInParent<EnemyInvestigator>();}
    public void Footstep(){if(enemy!=null)enemy.PlayFootstep();}
}
