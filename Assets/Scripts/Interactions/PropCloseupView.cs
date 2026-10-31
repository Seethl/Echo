using Echo.Menu;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Echo.Interactions
{
    public abstract class PropCloseupView : MonoBehaviour
    {
        public Transform cameraAnchor;
        public float zoomSeconds=.35f,closeupFov=42;
        public bool freezeWorld=true;
        public bool IsOpen {get;private set;}
        protected PlayerInteractor player;
        private Camera viewCamera;
        private Vector3 savedLocalPosition,startPosition;
        private Quaternion savedLocalRotation,startRotation;
        private float savedFov,elapsed;
        public virtual bool Open(PlayerInteractor user)
        {
            if(IsOpen||user==null||user.viewCamera==null||cameraAnchor==null)return false;
            if(!GameMenu.TryOpenModal(this,Close,freezeWorld))return false;
            player=user;viewCamera=user.viewCamera;savedLocalPosition=viewCamera.transform.localPosition;
            savedLocalRotation=viewCamera.transform.localRotation;startPosition=viewCamera.transform.position;
            startRotation=viewCamera.transform.rotation;savedFov=viewCamera.fieldOfView;elapsed=0;IsOpen=true;
            OnOpened();return true;
        }
        protected virtual void OnOpened(){}
        protected virtual void OnClosed(){}
        protected virtual void TickVisuals(){}
        protected virtual void LateUpdate()
        {
            TickVisuals();
            if(!IsOpen)return;
            if(viewCamera==null||cameraAnchor==null){Close();return;}
            elapsed+=Time.unscaledDeltaTime;
            float t=Mathf.SmoothStep(0,1,elapsed/Mathf.Max(.01f,zoomSeconds));
            viewCamera.transform.SetPositionAndRotation(Vector3.Lerp(startPosition,cameraAnchor.position,t),Quaternion.Slerp(startRotation,cameraAnchor.rotation,t));
            viewCamera.fieldOfView=Mathf.Lerp(savedFov,closeupFov,t);
            if(t<.999f||Mouse.current==null)return;
            int direction=Mouse.current.leftButton.wasPressedThisFrame?1:Mouse.current.rightButton.wasPressedThisFrame?-1:0;
            if(direction!=0&&Physics.Raycast(viewCamera.ScreenPointToRay(Mouse.current.position.ReadValue()),out var hit,3,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
            {
                var control=hit.collider.GetComponent<PropPhysicalControl>();
                if(control!=null&&control.owner==this)ActivateControl(control.controlId,direction);
            }
        }
        public abstract void ActivateControl(int id,int direction);
        public void Close()
        {
            if(!IsOpen)return;
            IsOpen=false;
            if(viewCamera!=null){viewCamera.transform.localPosition=savedLocalPosition;viewCamera.transform.localRotation=savedLocalRotation;viewCamera.fieldOfView=savedFov;}
            OnClosed();GameMenu.CloseModal(this);player=null;
        }
        private void OnDisable(){Close();}
        private void OnDestroy(){Close();}
    }
}
