using UnityEngine;

namespace Echo.Menu
{
    [CreateAssetMenu(menuName = "Echo/Game flow settings")]
    public sealed class GameFlowSettings : ScriptableObject
    {
        [Tooltip("Scene asset path; the scene must also be enabled in the Build Profile.")]
        public string gameplayScene = "Assets/Scenes/ManorPrototype/ManorPrototype.unity";
    }
}
