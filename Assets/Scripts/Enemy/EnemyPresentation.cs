using UnityEngine;
using UnityEngine.Events;

/// <summary>Optional visual/audio adapter. Navigation and hearing stay in EnemyInvestigator.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyInvestigator))]
public sealed class EnemyPresentation : MonoBehaviour
{
    [Tooltip("Optional Animator on the visual child. Navigation owns movement; use in-place clips.")]
    public Animator animator;
    public bool disableRootMotion=true;
    [Min(0)] public float animationDamping=.12f;
    [Header("Optional Animator parameters (missing parameters are ignored)")]
    public string speedParameter="Speed",suspicionParameter="Suspicion",stateParameter="State";
    public GameObject drinkingProp;
    public string attackParameter="IsAttacking",drinkingParameter="IsDrinking";
    public bool IsAttacking {get;private set;}
    public bool IsDrinking {get;private set;}
    private float doorUntil;
    public bool IsUsingDoor=>Time.time<doorUntil;
    public void PlayDoorMotion(bool closing,float duration)
    {
        doorUntil=Time.time+duration;
        if(animator==null)return;
        string state=closing?"DoorClose":"DoorOpen";
        if(animator.HasState(0,Animator.StringToHash(state)))animator.CrossFadeInFixedTime(state,.12f,0);
    }
    public void SetAttacking(bool value){IsAttacking=value;}
    public void SetDrinking(bool value){IsDrinking=value;}
    public string chaseParameter="IsChasing",searchParameter="IsSearching";
    [Header("Optional recorded audio; no new synthetic fallback for these cues")]
    public AudioClip heardNoiseClip,searchClip,calmClip;
    public AudioClip[] footstepVariations=System.Array.Empty<AudioClip>();
    [Range(0,1)] public float cueVolume=.65f,stepVolume=.7f;
    [Tooltip("Enable only after foot-contact animation events are installed.")]
    public bool useAnimationFootsteps;
    [Header("Hooks for future visual effects")]
    public UnityEvent onHeardNoise=new UnityEvent();
    public UnityEvent onSuspicious=new UnityEvent();
    public UnityEvent onChaseStarted=new UnityEvent();
    public UnityEvent onSearchStarted=new UnityEvent();
    public UnityEvent onReturnedToPatrol=new UnityEvent();

    private EnemyInvestigator brain;
    private AudioSource steps,cues;
    private int lastStep=-1;
    private Animator cachedAnimator;
    private RuntimeAnimatorController cachedController;
    private bool hasSpeed,hasSuspicion,hasState,hasChase,hasSearch,hasAttack,hasDrinking;
    private float nextAnimatorLookup;

    private void OnEnable()
    {
        brain=GetComponent<EnemyInvestigator>();brain.StateChanged+=OnStateChanged;brain.NoiseReacted+=OnNoiseReacted;
        RefreshAnimator();ApplyAnimator();
    }
    private void OnDisable(){if(brain!=null){brain.StateChanged-=OnStateChanged;brain.NoiseReacted-=OnNoiseReacted;}}
    private void Update()
    {
        if(animator==null&&Time.unscaledTime>=nextAnimatorLookup){animator=GetComponentInChildren<Animator>();nextAnimatorLookup=Time.unscaledTime+1;}
        if(cachedAnimator!=animator||cachedController!=(animator!=null?animator.runtimeAnimatorController:null))RefreshAnimator();
        if(Time.timeScale>0)ApplyAnimator();
    }
    public void RefreshAnimator()
    {
        if(animator==null)animator=GetComponentInChildren<Animator>();
        cachedAnimator=animator;cachedController=animator!=null?animator.runtimeAnimatorController:null;
        hasAttack=Has(attackParameter,AnimatorControllerParameterType.Bool);hasDrinking=Has(drinkingParameter,AnimatorControllerParameterType.Bool);
        hasSpeed=Has(speedParameter,AnimatorControllerParameterType.Float);
        hasSuspicion=Has(suspicionParameter,AnimatorControllerParameterType.Float);
        hasState=Has(stateParameter,AnimatorControllerParameterType.Int);
        hasChase=Has(chaseParameter,AnimatorControllerParameterType.Bool);
        hasSearch=Has(searchParameter,AnimatorControllerParameterType.Bool);
        if(animator!=null&&disableRootMotion)animator.applyRootMotion=false;
    }
    private bool Has(string parameter,AnimatorControllerParameterType type)
    {
        if(animator==null||cachedController==null||string.IsNullOrEmpty(parameter))return false;
        foreach(var p in animator.parameters)if(p.name==parameter&&p.type==type)return true;
        return false;
    }
    private void ApplyAnimator()
    {
        if(drinkingProp!=null&&drinkingProp.activeSelf!=IsDrinking)drinkingProp.SetActive(IsDrinking);
        if(animator==null||cachedController==null||brain==null)return;
        if(hasAttack)animator.SetBool(attackParameter,IsAttacking);
        if(hasDrinking)animator.SetBool(drinkingParameter,IsDrinking);
        if(hasSpeed)animator.SetFloat(speedParameter,IsUsingDoor?0:brain.Movement01,animationDamping,Time.deltaTime);
        if(hasSuspicion)animator.SetFloat(suspicionParameter,brain.Suspicion01,animationDamping,Time.deltaTime);
        if(hasState)animator.SetInteger(stateParameter,(int)brain.CurrentState);
        if(hasChase)animator.SetBool(chaseParameter,brain.CurrentState==EnemyInvestigator.State.Chasing);
        if(hasSearch)animator.SetBool(searchParameter,brain.CurrentState==EnemyInvestigator.State.Searching);
    }
    private void OnStateChanged(EnemyInvestigator.State state)
    {
        switch(state)
        {
            case EnemyInvestigator.State.Suspicious:onSuspicious.Invoke();break;
            case EnemyInvestigator.State.Chasing:onChaseStarted.Invoke();break;
            case EnemyInvestigator.State.Searching:PlayCue(searchClip);onSearchStarted.Invoke();break;
            case EnemyInvestigator.State.Returning:PlayCue(calmClip);onReturnedToPatrol.Invoke();break;
        }
        ApplyAnimator();
    }
    private void OnNoiseReacted(Vector3 position){PlayCue(heardNoiseClip);onHeardNoise.Invoke();}
    private AudioSource Source(ref AudioSource source,string label)
    {
        if(source!=null)return source;
        var child=new GameObject(label);child.transform.SetParent(transform,false);
        source=child.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=1;
        source.rolloffMode=AudioRolloffMode.Linear;source.minDistance=1;source.maxDistance=26;return source;
    }
    private void PlayCue(AudioClip clip)
    {if(clip!=null&&Time.timeScale>0)Source(ref cues,"Enemy state cues").PlayOneShot(clip,cueVolume);}
    public bool TryPlayFootstep()
    {
        if(Time.timeScale<=0||footstepVariations==null)return false;
        int count=0;for(int i=0;i<footstepVariations.Length;i++)if(footstepVariations[i]!=null&&(i!=lastStep))count++;
        if(count==0)
        {
            if(lastStep<0||lastStep>=footstepVariations.Length||footstepVariations[lastStep]==null)return false;
            Source(ref steps,"Enemy footsteps").PlayOneShot(footstepVariations[lastStep],stepVolume);return true;
        }
        int pick=Random.Range(0,count);
        for(int i=0;i<footstepVariations.Length;i++)
            if(footstepVariations[i]!=null&&i!=lastStep&&pick--==0)
            {lastStep=i;Source(ref steps,"Enemy footsteps").PlayOneShot(footstepVariations[i],stepVolume);return true;}
        return false;
    }
}
