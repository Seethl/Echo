using Echo.Manor;
using UnityEngine;

namespace Echo.Audio
{
    public sealed class NoiseTelemetry:MonoBehaviour
    {
        public float fallSpeed=.45f;
        [Tooltip("Quiet environmental floor for the HUD only; never changes enemy hearing or sound cover.")]
        [Range(0,1)] public float ambientDisplayFloor=.08f;
        public float PlayerLevel {get;private set;}
        public float AmbientLevel {get;private set;}
        public string SourceLabel {get;private set;}="Тихо";
        private void OnEnable()=>NoiseSystem.Emitted+=OnNoise;
        private void OnDisable()=>NoiseSystem.Emitted-=OnNoise;
        private void OnNoise(NoiseSystem.Noise noise)
        {
            if(Time.timeScale<=0)return;
            bool own=noise.Instigator==gameObject||noise.Source!=null&&(noise.Source.transform==transform||noise.Source.transform.IsChildOf(transform));
            if(!own||noise.Type==NoiseSystem.Kind.Ambient||noise.Type==NoiseSystem.Kind.Enemy)return;
            float value=NoiseSystem.Level(noise);
            if(value>=PlayerLevel*.75f)
            {
                SourceLabel=noise.Type==NoiseSystem.Kind.Radio?"Радіо":noise.Type==NoiseSystem.Kind.Footstep?StepLabel():"Дія";
                PlayerLevel=Mathf.Max(PlayerLevel,value);
            }
        }
        private string StepLabel()
        {
            var cc=GetComponent<CharacterController>();var origin=cc!=null?cc.bounds.center:transform.position;origin.y=cc!=null?cc.bounds.min.y+.15f:origin.y;
            if(Physics.Raycast(origin,Vector3.down,out var hit,2,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
            {
                var surface=hit.collider.GetComponentInParent<SurfaceType>();
                if(surface!=null&&(surface.kind==SurfaceType.Kind.Glass||surface.kind==SurfaceType.Kind.Creaky))return surface.kind==SurfaceType.Kind.Glass?"Скло":"Скрипуча дошка";
            }
            var fps=GetComponent<FPSController>();return fps!=null&&fps.IsRunning?"Біг":fps!=null&&fps.IsCrouching?"Присідання":"Кроки";
        }
        private void Update()
        {
            if(Time.timeScale<=0)return;
            PlayerLevel=Mathf.MoveTowards(PlayerLevel,0,Time.deltaTime*fallSpeed);
            if(PlayerLevel<.01f)SourceLabel="Тихо";
            float cover=NoiseMaskZone.CoverAt(transform.position);
            float target=Mathf.Max(ambientDisplayFloor,cover);
            AmbientLevel=Mathf.MoveTowards(AmbientLevel,target,Time.deltaTime*(target>AmbientLevel?3:fallSpeed));
            var player=GetComponent<Echo.Interactions.PlayerInteractor>();
            if(player!=null&&player.ui!=null)player.ui.SetNoise(PlayerLevel,AmbientLevel,SourceLabel,cover>ambientDisplayFloor?NoiseMaskZone.DescriptionAt(transform.position):"тихий фон");
        }
    }
}
