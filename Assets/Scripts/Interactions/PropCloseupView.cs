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
        private float savedFov,savedNearClip,elapsed;
        private int heldControl=-1,heldDirection;
        private float repeatAt;
        public virtual bool Open(PlayerInteractor user)
        {
            if(IsOpen||user==null||user.viewCamera==null||cameraAnchor==null)return false;
            if(!GameMenu.TryOpenModal(this,Close,freezeWorld))return false;
            player=user;viewCamera=user.viewCamera;savedLocalPosition=viewCamera.transform.localPosition;
            savedLocalRotation=viewCamera.transform.localRotation;startPosition=viewCamera.transform.position;
            startRotation=viewCamera.transform.rotation;savedFov=viewCamera.fieldOfView;savedNearClip=viewCamera.nearClipPlane;
            // A lock's drums sit closer than the FPS camera's normal near plane.
            // ScreenPointToRay otherwise starts inside them and hits the case.
            viewCamera.nearClipPlane=Mathf.Min(savedNearClip,.01f);
            elapsed=0;IsOpen=true;heldControl=-1;
            OnOpened();return true;
        }
        protected virtual void OnOpened(){}
        protected virtual void OnClosed(){}
        protected virtual void TickVisuals(){}
        protected virtual bool CanHoldControl(int id)=>false;
        protected virtual float HoldInterval=>.09f;
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
            var mouse=Mouse.current;
            // The world can be paused while the camera and controls still animate.
            // FixedUpdate will not synchronize their colliders in that case.
            Physics.SyncTransforms();
            PropPhysicalControl control=null;
            if(Physics.Raycast(viewCamera.ScreenPointToRay(mouse.position.ReadValue()),out var hit,3,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
            {
                var candidate=hit.collider.GetComponent<PropPhysicalControl>();
                if(candidate!=null&&candidate.owner==this)control=candidate;
            }
            int direction=mouse.leftButton.wasPressedThisFrame?1:mouse.rightButton.wasPressedThisFrame?-1:0;
            if(direction!=0)
            {
                heldControl=-1;
                if(control!=null)
                {
                    if(CanHoldControl(control.controlId)){heldControl=control.controlId;heldDirection=direction;repeatAt=Time.unscaledTime+.3f;}
                    ActivateControl(control.controlId,direction);
                }
            }
            if(!IsOpen)return;
            bool held=heldDirection>0?mouse.leftButton.isPressed:mouse.rightButton.isPressed;
            if(!held)heldControl=-1;
            if(heldControl>=0&&Time.unscaledTime>=repeatAt)
            {
                repeatAt=Time.unscaledTime+Mathf.Max(.02f,HoldInterval);
                ActivateControl(heldControl,heldDirection);
            }
            float scroll=mouse.scroll.ReadValue().y;
            if(control!=null&&CanHoldControl(control.controlId)&&Mathf.Abs(scroll)>.01f)
            {
                heldControl=-1;
                ActivateControl(control.controlId,scroll>0?1:-1);
            }
        }
        public abstract void ActivateControl(int id,int direction);
        public void Close()
        {
            if(!IsOpen)return;
            IsOpen=false;heldControl=-1;
            if(viewCamera!=null){viewCamera.transform.localPosition=savedLocalPosition;viewCamera.transform.localRotation=savedLocalRotation;viewCamera.fieldOfView=savedFov;viewCamera.nearClipPlane=savedNearClip;}
            OnClosed();GameMenu.CloseModal(this);player=null;
        }
        private void OnDisable(){Close();}
        private void OnDestroy(){Close();}
    }
}
