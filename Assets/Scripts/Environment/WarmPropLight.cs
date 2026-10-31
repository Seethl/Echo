using UnityEngine;

namespace Echo.Environment
{
    [RequireComponent(typeof(Light))]
    public sealed class WarmPropLight : MonoBehaviour
    {
        public bool lit=true;
        [Range(0, .25f)] public float flickerAmount=.055f;
        [Min(.1f)] public float flickerSpeed=3.5f;
        private Light source;
        private float intensity,seed;
        private void Awake()
        {
            source=GetComponent<Light>();intensity=source.intensity;
            seed=Mathf.Abs(transform.position.x*1.73f+transform.position.y*.91f+transform.position.z*2.31f)%1000;
            Apply(0);
        }
        private void Update(){Apply(Time.time);}
        private void Apply(float time)
        {
            source.enabled=lit;
            source.intensity=intensity*(1+(Mathf.PerlinNoise(seed,time*flickerSpeed)*2-1)*flickerAmount);
        }
        public void SetLit(bool value){lit=value;if(source!=null)Apply(Time.time);}
    }
}
