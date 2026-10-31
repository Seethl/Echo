using System.Collections;
using Echo.Manor;
using Echo.Radio;
using UnityEngine;

namespace Echo.Audio
{
    public sealed class AtmosphereCue:MonoBehaviour
    {
        public string afterFlag="service.open";
        public AudioClip footstepsClip;
        private bool played;
        private void Start(){played=ManorProgress.Active!=null&&ManorProgress.Active.Has(afterFlag);}
        private void Update()
        {
            if(played||Time.timeScale<=0||ManorProgress.Active==null||!ManorProgress.Active.Has(afterFlag))return;
            played=true;StartCoroutine(Steps());
            var radio=FindAnyObjectByType<PortableRadio>();
            if(radio!=null&&radio.volume>.8f)ManorProgress.Active.player.ui.ShowToast("Радіо дуже гучне. R — налаштування гучності. За дверима хтось ходить.");
        }
        private IEnumerator Steps(){for(int i=0;i<3;i++){PrototypeAudio.Play(gameObject,PrototypeAudio.Cue.Footstep,.7f,footstepsClip);yield return new WaitForSeconds(.65f);}}
    }
}
