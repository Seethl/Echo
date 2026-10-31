using Echo.Manor;
using UnityEngine;

namespace Echo.Audio
{
    public sealed class NoiseTelemetry:MonoBehaviour
    {
        private float level;
        private void OnEnable()=>NoiseSystem.Emitted+=OnNoise;
        private void OnDisable()=>NoiseSystem.Emitted-=OnNoise;
        private void OnNoise(NoiseSystem.Noise noise)
        {if(noise.Source!=null&&(noise.Source.transform==transform||noise.Source.transform.IsChildOf(transform)||noise.Type==NoiseSystem.Kind.Mechanism&&Vector3.Distance(transform.position,noise.Position)<3))level=Mathf.Max(level,noise.Radius);}
        private void Update()
        {
            level=Mathf.MoveTowards(level,0,Time.deltaTime*6);
            if(ManorProgress.Active!=null)ManorProgress.Active.statusSuffix="Власний шум: "+(level<2?"тихо":level<8?"помітно":"гучно")+" · Прикриття: "+NoiseMaskZone.DescriptionAt(transform.position);
        }
    }
}
