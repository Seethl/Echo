using UnityEngine;

namespace Echo.Manor
{
    // A separate visual keeps replacement models independent of interaction colliders.
    public sealed class SymbolFeedback:MonoBehaviour
    {
        public Transform face;
        public Renderer indicator;
        public float pulseSeconds=.45f;
        private Vector3 restScale;
        private float until;
        private MaterialPropertyBlock properties;
        private void Awake(){if(face!=null)restScale=face.localScale;properties=new MaterialPropertyBlock();}
        public void Pulse()=>until=Time.time+pulseSeconds;
        private void Update()
        {
            float pulse=Mathf.Clamp01((until-Time.time)/pulseSeconds);
            if(face!=null)face.localScale=restScale*(1+.1f*pulse);
            if(indicator==null)return;
            indicator.GetPropertyBlock(properties);
            var color=Color.Lerp(new Color(.33f,.43f,.45f),new Color(1,.8f,.35f),pulse);
            properties.SetColor("_Color",color);properties.SetColor("_BaseColor",color);indicator.SetPropertyBlock(properties);
        }
    }
}
