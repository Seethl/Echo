using System;
using System.Linq;
using Echo.Audio;
using Echo.Interactions;
using Echo.Manor;
using Echo.Menu;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Echo.Radio
{
    public sealed class PortableRadio:Interactable,ICheckpointObject
    {
        public RadioChannel[] channels=Array.Empty<RadioChannel>();
        public int frequency=820;
        [Range(0,1)] public float volume=.4f;
        public bool powered;
        public float musicSeconds=20,silenceSeconds=6;
        public AudioClip musicClip,whiteNoiseClip,signalClip;
        public Vector3 placementHalfExtents=new Vector3(.32f,.22f,.2f);
        public bool Held {get;private set;}
        public string CheckpointId=>"portable.radio";
        public override string Prompt=>"E — підняти радіо";
        public string Subtitle {get;private set;}="";
        public bool MusicAudible {get;private set;}
        private PlayerInteractor holder;
        private Collider[] colliders;
        private AudioSource source;
        private float nextPulse,nextRecordingSignal,cycleOrigin;
        private bool lastAudible,signalPlayed;
        private RadioChannel lastChannel;
        private string lastRecording;
        public RadioChannel Channel=>channels!=null?channels.Where(c=>c!=null&&Mathf.Abs(c.frequency-frequency)<=c.receptionHalfWidth).OrderBy(c=>Mathf.Abs(c.frequency-frequency)).FirstOrDefault():null;

        private void Awake(){colliders=GetComponentsInChildren<Collider>();source=PrototypeAudio.Source(gameObject);cycleOrigin=Time.time;holder=FindAnyObjectByType<PlayerInteractor>();}
        public override bool CanInteract(PlayerInteractor player)=>base.CanInteract(player)&&!Held;
        public override void Interact(PlayerInteractor player)
        {
            holder=player;Held=true;foreach(var c in colliders)c.enabled=false;
            transform.SetParent(player.viewCamera.transform,false);transform.localPosition=new Vector3(.32f,-.3f,.65f);transform.localRotation=Quaternion.identity;
            ManorProgress.Active.Set("radio.picked");ManorProgress.Active.RefreshCheckpoint();
            player.ui.ShowToast("Радіо піднято. R — налаштувати; F — увімкнути; Q — поставити. Гучність впливає на те, хто почує передачу.");
        }
        private void Update()
        {
            if(holder==null)return;
            if(!GameMenu.BlocksPlayerInput&&Held&&Keyboard.current!=null)
            {
                if(Keyboard.current.rKey.wasPressedThisFrame)OpenControls();
                else if(Keyboard.current.qKey.wasPressedThisFrame)TryPlace();
                else if(Keyboard.current.fKey.wasPressedThisFrame)TogglePower();
            }
            if(Time.timeScale<=0)return;
            var channel=Channel;
            if(channel!=lastChannel){source.Stop();lastChannel=channel;lastRecording=null;lastAudible=false;signalPlayed=false;cycleOrigin=Time.time;}
            if(!powered||volume<=0){source.Stop();Subtitle="";MusicAudible=false;holder.ui.SetSubtitle("");return;}
            source.volume=volume;
            if(channel==null)
            {
                Subtitle="Радіо "+frequency+": статичний шум. Підказки частот — на картках і в журналі J.";
                if(!source.isPlaying){source.clip=whiteNoiseClip!=null?whiteNoiseClip:PrototypeAudio.Clip(PrototypeAudio.Cue.Static);source.loop=true;source.Play();}
                MusicAudible=false;
            }
            else if(channel.mode==RadioChannel.Mode.Music)UpdateMusic();
            else if(channel.mode==RadioChannel.Mode.WhiteNoise)
            {
                Subtitle="Білий шум — постійне джерело звуку. Можна використати як приманку.";MusicAudible=true;
                if(!source.isPlaying){source.clip=whiteNoiseClip!=null?whiteNoiseClip:PrototypeAudio.Clip(PrototypeAudio.Cue.Static);source.loop=true;source.Play();}
            }
            else
            {
                MusicAudible=true;
                string text=!string.IsNullOrEmpty(channel.clearAfterFlag)&&!ManorProgress.Active.Has(channel.clearAfterFlag)?channel.interruptedTranscript:channel.transcript;
                Subtitle="Запис "+frequency+": "+text;
                if(lastRecording!=text||Time.time>=nextRecordingSignal)
                {
                    PrototypeAudio.Play(gameObject,PrototypeAudio.Cue.DoubleClick,.65f,channel.voiceClip);
                    nextRecordingSignal=Time.time+8;lastRecording=text;ManorProgress.Active.AddClue("recording."+channel.frequency,frequency+": "+text);
                }
            }
            if(Time.time>=nextPulse)
            {
                nextPulse=Time.time+.75f;
                bool audible=channel==null||channel.mode!=RadioChannel.Mode.Music||MusicAudible;
                if(audible)NoiseSystem.Emit(transform.position,Mathf.Lerp(2,20,volume),gameObject,NoiseSystem.Kind.Radio,Mathf.Lerp(.35f,1,volume));
            }
            bool nearby=Held||Vector3.Distance(transform.position,holder.transform.position)<8;
            holder.ui.SetSubtitle(nearby?Subtitle:"");
        }
        private void UpdateMusic()
        {
            float phase=(Time.time-cycleOrigin)%Mathf.Max(1,musicSeconds+silenceSeconds);
            MusicAudible=phase<musicSeconds;
            if(MusicAudible&&!lastAudible){source.clip=musicClip!=null?musicClip:PrototypeAudio.Clip(PrototypeAudio.Cue.Music);source.loop=false;source.Play();signalPlayed=false;}
            if(MusicAudible&&phase>=musicSeconds-.6f&&!signalPlayed){PrototypeAudio.Play(gameObject,PrototypeAudio.Cue.DoubleClick,.9f,signalClip);signalPlayed=true;}
            if(!MusicAudible&&lastAudible)source.Stop();lastAudible=MusicAudible;
            Subtitle=MusicAudible?"Музичний канал. Після сигналу буде пауза.":"Музика замовкла. Мешканець ще може перевіряти місце, звідки її чув.";
        }
        public void TogglePower(){powered=!powered;lastRecording=null;ManorProgress.Active.RefreshCheckpoint();}
        public void Tune(int value){frequency=Mathf.Clamp(value,760,1280);lastRecording=null;ManorProgress.Active.RefreshCheckpoint();}
        public void SetVolume(float value){volume=Mathf.Clamp01(value);ManorProgress.Active.RefreshCheckpoint();}
        public void OpenControls()
        {
            if(!Held)return;
            holder.ui.OpenPanel(this,"БАТАРЕЙНЕ РАДІО",()=>frequency+" · Гучність "+Mathf.RoundToInt(volume*100)+"% · "+(powered?"УВІМКНЕНО":"ВИМКНЕНО")+"\n"+Subtitle+"\nЗнайдені частоти: "+KnownFrequencies(),new[]{
                new InteractionUI.Choice("Частота −20",()=>Tune(frequency-20)),new InteractionUI.Choice("Частота +20",()=>Tune(frequency+20)),
                new InteractionUI.Choice("Гучність −10%",()=>SetVolume(volume-.1f)),new InteractionUI.Choice("Гучність +10%",()=>SetVolume(volume+.1f)),
                new InteractionUI.Choice("Увімкнути / вимкнути",TogglePower),new InteractionUI.Choice("Назад",()=>holder.ui.ClosePanel())},false);
        }
        private string KnownFrequencies()=>string.Join(", ",channels.Where(c=>c!=null&&(ManorProgress.Active.Has("frequency."+c.frequency)||c.mode!=RadioChannel.Mode.Recording)).Select(c=>c.frequency+" "+(c.mode==RadioChannel.Mode.Recording?"запис":c.channelName)));
        public bool TryPlace()
        {
            if(!Held)return false;
            var camera=holder.viewCamera.transform;
            if(!Physics.Raycast(camera.position,camera.forward,out var hit,2.5f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)||hit.normal.y<.65f)
            {holder.ui.ShowToast("Наведіться на вільну підлогу або стіл не далі 2,5 м.");return false;}
            var size=placementHalfExtents;var p=hit.point+Vector3.up*(size.y+.02f);
            if(!FindObjectsByType<ManorZone>().Any(z=>z.Contains(p))||Physics.CheckBox(p,size*.95f,Quaternion.identity,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
            {holder.ui.ShowToast("Тут немає вільного доступного місця для радіо.");return false;}
            Held=false;transform.SetParent(null);transform.SetPositionAndRotation(p,Quaternion.identity);foreach(var c in colliders)c.enabled=true;
            ManorProgress.Active.RefreshCheckpoint();holder.ui.ShowToast("Радіо поставлено. Його можна знову підняти клавішею E.");return true;
        }
        [Serializable]private struct Saved{public bool held,power;public int frequency;public float volume;public Vector3 position;public Quaternion rotation;}
        public string CaptureState()=>JsonUtility.ToJson(new Saved{held=Held,power=powered,frequency=frequency,volume=volume,position=transform.position,rotation=transform.rotation});
        public void RestoreState(string text)
        {
            var data=JsonUtility.FromJson<Saved>(text);frequency=data.frequency;volume=data.volume;powered=data.power;
            if(data.held)Interact(holder);
            else{Held=false;transform.SetParent(null);transform.SetPositionAndRotation(data.position,data.rotation);foreach(var c in colliders)c.enabled=true;}
        }
    }
}
