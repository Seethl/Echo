using Echo.Interactions;
using UnityEngine;

namespace Echo.Audio
{
    // Presentation only: hearing events continue to come from HingedDoor, once per action.
    [DisallowMultipleComponent, RequireComponent(typeof(HingedDoor))]
    public sealed class HingedDoorAudio : MonoBehaviour
    {
        public AudioClip openingClip, closingClip, latchClip;
        [Range(0,1)] public float playbackVolume=.65f;
        [Min(1)] public float audibleDistance=14f;
        public bool HasMovementAudio => isActiveAndEnabled && openingClip!=null && closingClip!=null;
        private HingedDoor door;
        private AudioSource source;
        private Quaternion closedRotation;
        private bool observedOpen, movementPlaying;

        private void Start()
        {
            door=GetComponent<HingedDoor>();closedRotation=door.hinge.localRotation;observedOpen=door.IsOpen;
            var child=new GameObject("Door movement audio");child.transform.SetParent(door.hinge,false);
            source=child.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=1;
            source.rolloffMode=AudioRolloffMode.Linear;source.minDistance=1.5f;source.maxDistance=audibleDistance;source.dopplerLevel=0;
        }
        private void LateUpdate()
        {
            if(source==null||Time.timeScale<=0)return;
            if(observedOpen!=door.IsOpen)
            {
                observedOpen=door.IsOpen;source.Stop();movementPlaying=false;
                // Checkpoint restoration snaps directly to its final rotation and must stay silent.
                if(door.IsMoving)
                {
                    var target=closedRotation*Quaternion.Euler(0,door.IsOpen?door.openAngle:0,0);
                    float duration=Quaternion.Angle(door.hinge.localRotation,target)/Mathf.Max(1,door.degreesPerSecond);
                    var clip=door.IsOpen?openingClip:closingClip;
                    if(clip!=null){source.clip=clip;source.volume=playbackVolume;source.pitch=Mathf.Clamp(clip.length/Mathf.Max(.1f,duration),.7f,2f);source.Play();movementPlaying=true;}
                }
            }
            if(movementPlaying&&!door.IsMoving)
            {
                movementPlaying=false;source.Stop();
                if(!door.IsOpen&&latchClip!=null){source.pitch=1;source.clip=latchClip;source.volume=playbackVolume;source.Play();}
            }
        }
        private void OnDisable(){if(source!=null)source.Stop();}
    }
}
