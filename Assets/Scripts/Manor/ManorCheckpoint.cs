using Echo.Interactions;
using UnityEngine;

namespace Echo.Manor
{
    public sealed class ManorCheckpoint:MonoBehaviour
    {
        public int stage;
        public string requiredFlag;
        public Vector3 playerSpawn,enemySpawn;
        private void OnTriggerEnter(Collider other)
        {
            if(other.GetComponentInParent<PlayerInteractor>()==null)return;
            var progress=ManorProgress.Active;
            if(progress!=null&&(string.IsNullOrEmpty(requiredFlag)||progress.Has(requiredFlag)))progress.SaveCheckpoint(stage,playerSpawn,enemySpawn);
        }
    }
}
