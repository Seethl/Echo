using Echo.Interactions;
using Echo.Audio;
using UnityEngine;
using UnityEngine.AI;

namespace Echo.Manor
{
    // Uses the existing investigator and reachable navigation. Never teleports the resident.
    public sealed class RemedyDelivery : Interactable
    {
        public EnemyInvestigator resident;
        public Transform approachPoint;
        public GameObject filledCup;
        public string potionId = "remedy.potion", servedFlag = "remedy.served";
        public float approachDistance = 1.6f, drinkingSeconds = 2.5f;
        private float nextRequest, drinking;
        private bool completed;
        private PlayerInteractor player;
        public override string Prompt => "E — залишити мікстуру в чашці";
        public override bool CanInteract(PlayerInteractor p) => base.CanInteract(p) && !ManorProgress.Active.Has(servedFlag);
        private void Start() { player = FindAnyObjectByType<PlayerInteractor>(); }
        public override void Interact(PlayerInteractor p)
        {
            if (!p.HasKey(potionId))
            {
                p.ui.ShowToast("Його чашка. Тут можна залишити готову мікстуру.");
                ManorProgress.Active.AddClue("resident.routine", "Мешканець повертається до своєї чашки у холі. Залиши ліки й відійди з поля зору."); return;
            }
            ManorProgress.Active.Set(servedFlag);
            PrototypeAudio.Play(gameObject, PrototypeAudio.Cue.Click, .4f);
            p.ui.ShowToast("Ліки в чашці. Відійди з поля зору: мешканець має спокійно підійти до столика.", 7);
        }
        private void Update()
        {
            var progress = ManorProgress.Active;
            if (progress == null) return;
            bool served = progress.Has(servedFlag);
            if (filledCup != null) filledCup.SetActive(served);
            if (!served || completed || Time.timeScale <= 0 || resident == null || !resident.isActiveAndEnabled || player == null) return;
            var life = player.GetComponent<PlayerCaught>();
            if (life != null && life.IsCaught) return;
            if (resident.CanSeeTarget() || resident.CurrentState == EnemyInvestigator.State.Chasing) { drinking = 0; return; }
            Vector3 goal = approachPoint != null ? approachPoint.position : transform.position;
            if (Vector3.Distance(resident.transform.position, goal) > approachDistance)
            {
                drinking = 0;
                if (Time.time >= nextRequest) { nextRequest = Time.time + 4; resident.Investigate(goal); }
                return;
            }
            // Stay by the cup while drinking, rather than starting the investigator search loop.
            // A brief renewal lets sight or leaving the interaction release movement.
            resident.HoldForInteraction(.2f);
            drinking += Time.deltaTime;
            if (drinking < drinkingSeconds) return;
            if (!player.ui.ShowCompletion("Мешканець заспокоївся. Ліки подіяли.")) return;
            completed = true; progress.Set("remedy.complete");
            var agent = resident.GetComponent<NavMeshAgent>();
            if (agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
            var catchPlayer = resident.GetComponent<EnemyCatchPlayer>();
            if (catchPlayer != null) catchPlayer.enabled = false;
            resident.enabled = false;
        }
    }
}
