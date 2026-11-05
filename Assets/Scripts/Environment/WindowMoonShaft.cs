using Echo.Manor;
using UnityEngine;
using UnityEngine.Rendering;
namespace Echo.Environment
{
    // Visual mist only: no lights, colliders, audio, or gameplay visibility rules.
    [ExecuteAlways,DisallowMultipleComponent,RequireComponent(typeof(MeshRenderer),typeof(MeshFilter))]
    public sealed class WindowMoonShaft:MonoBehaviour
    {
        [Min(.1f)] public float width=1.45f,height=1.8f,length=5;
        public float drop=1.8f,sideDrift=.35f;
        [Range(1,2)] public float spread=1.35f;
        [Range(0,.1f)]public float density=.035f;
        [Range(0,.2f)]public float opacity=.1f;
        public Color tint=new Color(.52f,.66f,.86f);
        public GalleryWindowShutter shutter;
        public Light windowLight;
        [Tooltip("Use the actual spotlight-cookie projection for aligned light/shadow bands.")]
        public bool alignWithWindowCookie;
        [Tooltip("Use the spotlight cookie as the haze silhouette.")]
        public bool useWindowCookieMask;
        [Min(.1f)]public float occlusionInterval=.3f;
        public float Exposure{get;private set;}
        public Vector4 PaneLengths{get;private set;}=Vector4.one;
        private MeshRenderer display;private MaterialPropertyBlock block;
        private readonly RaycastHit[] hits=new RaycastHit[48];
        private readonly Collider[] contacts=new Collider[24];
        private float nextOcclusion;
        private static readonly int Tint=Shader.PropertyToID("_Tint"),Density=Shader.PropertyToID("_Density"),Opacity=Shader.PropertyToID("_Opacity"),Spread=Shader.PropertyToID("_Spread"),Drift=Shader.PropertyToID("_Drift"),Open=Shader.PropertyToID("_Exposure"),Limits=Shader.PropertyToID("_PaneLengths");
        private void OnEnable(){display=GetComponent<MeshRenderer>();block=new MaterialPropertyBlock();nextOcclusion=0;Refresh(true);}
        private void OnDisable(){if(display!=null)display.enabled=false;}
        private void LateUpdate()=>Refresh(false);
        public void Refresh(bool geometry)
        {
            if(display==null)display=GetComponent<MeshRenderer>();if(block==null)block=new MaterialPropertyBlock();
            Exposure=shutter!=null?Mathf.Pow(shutter.OpenAmount,2):1;
            if(windowLight!=null&&(!windowLight.enabled||!windowLight.gameObject.activeInHierarchy||windowLight.intensity<=0))Exposure=0;
            display.enabled=Exposure>.001f;
            if(geometry||Time.time>=nextOcclusion)
            {UpdateOcclusion();nextOcclusion=Time.time+Mathf.Max(.1f,occlusionInterval);}
            bool projected=alignWithWindowCookie&&windowLight!=null;block.SetFloat("_UseWindowProjection",projected?1:0);
            if(projected){float tangent=Mathf.Tan(windowLight.spotAngle*.5f*Mathf.Deg2Rad);block.SetMatrix("_WindowWorldToCookie",Matrix4x4.Scale(new Vector3(1/tangent,1/tangent,1))*windowLight.transform.worldToLocalMatrix);}
            bool masked = projected && useWindowCookieMask && windowLight.cookie != null;
            block.SetFloat("_UseCookieMask", masked ? 1 : 0);
            if (masked)
                block.SetTexture("_CookieMask", windowLight.cookie);
            block.SetColor(Tint,tint);block.SetFloat(Density,density);block.SetFloat(Opacity,opacity);block.SetFloat(Spread,spread);block.SetVector(Drift,new Vector4(sideDrift/width,-drop/height,0,0));block.SetFloat(Open,Exposure);block.SetVector(Limits,PaneLengths);display.SetPropertyBlock(block);
        }
        public void UpdateOcclusion()
        {
            var limits=Vector4.one;
            for(int i=0;i<4;i++)
            {
                float x=(i%2==0?-.25f:.25f),y=(i<2?-.25f:.25f);
                var from=transform.TransformPoint(new Vector3(x,y,0));var to=transform.TransformPoint(new Vector3(sideDrift/width+x*spread,-drop/height+y*spread,1));var delta=to-from;float full=delta.magnitude;
                int touching=Physics.OverlapSphereNonAlloc(from,.025f,contacts,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);bool blocked=touching==contacts.Length;
                for(int j=0;j<touching;j++)if(contacts[j].GetComponentInParent<FPSController>()==null&&contacts[j].GetComponentInParent<EnemyInvestigator>()==null)blocked=true;
                if(blocked){limits[i]=0;continue;}
                int count=Physics.RaycastNonAlloc(from,delta.normalized,hits,full,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);float nearest=full;
                for(int j=0;j<count;j++)
                {
                    var c=hits[j].collider;
                    if(c.GetComponentInParent<FPSController>()!=null||c.GetComponentInParent<EnemyInvestigator>()!=null)continue;
                    nearest=Mathf.Min(nearest,hits[j].distance);
                }
                limits[i]=Mathf.Clamp01((nearest-.035f)/Mathf.Max(.01f,full));
            }
            PaneLengths=limits;
        }
    }
}
