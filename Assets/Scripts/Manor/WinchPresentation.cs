using UnityEngine;

namespace Echo.Manor
{
    // Cosmetic only. Progress, timing, input and sounds remain owned by WinchMechanism.
    public sealed class WinchPresentation : MonoBehaviour
    {
        public WinchMechanism mechanism;
        public Transform crank;
        private void Update()
        {
            if(crank==null||mechanism==null||ManorProgress.Active==null)return;
            crank.gameObject.SetActive(ManorProgress.Active.Has("winch.installed"));
            if(mechanism.IsCranking)crank.Rotate(Vector3.forward,100*Time.deltaTime,Space.World);
        }
    }
}
