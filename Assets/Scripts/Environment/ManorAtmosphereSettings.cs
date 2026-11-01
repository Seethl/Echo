using UnityEngine;

namespace Echo.Environment
{
    [CreateAssetMenu(menuName="Echo/Manor atmosphere")]
    public sealed class ManorAtmosphereSettings : ScriptableObject
    {
        [Header("Rest of the manor")]
        [Range(0,2)] public float hallLightScale=1;
        [Range(0,2)] public float libraryLightScale=1;
        [Range(0,2)] public float upperFloorLightScale=1;
        [Range(0,2)] public float kitchenLightScale=1;
        [Range(0,2)] public float gardenLightScale=1;
        [Range(0,2)] public float yardLightScale=1;
        [Header("Common base and first rooms")]
        [Range(0,1)] public float candleBloom=.28f;
        public Color candleColor=new Color(1,.75f,.36f);
        public Color ambient = new Color(.105f,.12f,.145f);
        public Color moon = new Color(.77f,.82f,.93f);
        [Range(0,2)] public float moonIntensity=.55f;
        public Color haze = new Color(.12f,.14f,.17f);
        [Range(0,.06f)] public float hazeDensity=.018f;
        [Min(0)] public float galleryWindowIntensity=28;
        [Min(0)] public float caretakerWindowIntensity=14;
        [Range(0,3)] public float lanternIntensity=1.3f;
        [Range(0,8)] public float dustPerSecond=3;
    }
}
