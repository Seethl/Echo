using UnityEngine;

namespace Echo.Manor
{
    [CreateAssetMenu(menuName="Echo/Manor clutter profile")]
    public sealed class ManorClutterProfile : ScriptableObject
    {
        public int seed=7421;
        [Range(1,5)]public int propsPerShelf=3;
        [Range(0,24)]public int papersPerRoom=10;
        [Min(.2f)]public float clusterSpread=.70f;
        [Min(.08f)]public float minimumPropGap=.10f;
    }
}
