using System;
using System.Linq;
using Echo.Audio;
using Echo.Radio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Echo.Manor.Editor
{
    public static class ManorRadioVoice
    {
        public const string Folder="Assets/Audio/ManorRadioVoice";
        [MenuItem("Echo/Під’єднати голос радіо та вітер")]
        public static void ApplyAndSave()
        {
            if(EditorSceneManager.GetActiveScene().path!=ManorPrototypeBuilder.ScenePath)throw new InvalidOperationException("Open ManorPrototype first.");
            Apply();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
        public static void Apply()
        {
            foreach(string path in System.IO.Directory.GetFiles(Folder,"*.wav").Concat(System.IO.Directory.GetFiles("Assets/Audio/ManorDoors","*.wav")))
            {
                var importer=(AudioImporter)AssetImporter.GetAtPath(path);var sample=importer.defaultSampleSettings;
                sample.loadType=AudioClipLoadType.DecompressOnLoad;sample.compressionFormat=AudioCompressionFormat.PCM;sample.preloadAudioData=true;importer.defaultSampleSettings=sample;importer.forceToMono=true;importer.SaveAndReimport();
            }
            var service=Channel("Service820");service.voiceClip=Clip("Service820");service.voicePlaybackVolume=.9f;EditorUtility.SetDirty(service);
            var gallery=Channel("Gallery940");gallery.voiceClip=null;gallery.interruptedVoiceClip=Clip("GalleryInterrupted");gallery.voicePrefix=Clip("GalleryPrefix");gallery.voiceSuffix=Clip("GallerySuffix");gallery.voicePlaybackVolume=.9f;
            gallery.digitClips=Enumerable.Range(0,10).Select(i=>Clip("Digit"+i)).ToArray();
            gallery.interruptedTranscript="Службовий замок на поточну зміну… [вітер заглушає цифри]. Зачиніть усі віконниці й повторіть запис.";EditorUtility.SetDirty(gallery);
            var library=Channel("Library1060");library.voiceClip=null;library.voicePrefix=Clip("LibraryPrefix");library.voiceSuffix=Clip("LibrarySuffix");
            library.transcript="Слухай порядок: {sounds}. Повтори його на вінілових платівках. Запис можна повторити кнопкою на радіо.";EditorUtility.SetDirty(library);
            foreach(var window in Object.FindObjectsByType<GalleryWindowShutter>())
            {
                var ambient=window.GetComponent<AmbientCycle>();ambient.windClip=Clip("WindowWindLoop");ambient.windPlaybackVolume=.9f;EditorUtility.SetDirty(ambient);
            }
            foreach(var door in Object.FindObjectsByType<Echo.Interactions.HingedDoor>(FindObjectsInactive.Include))
            {
                bool cupboard=door.name.Contains("Cupboard")||door.name.Contains("Cache");
                var feedback=door.GetComponent<HingedDoorAudio>();if(feedback==null)feedback=door.gameObject.AddComponent<HingedDoorAudio>();
                string prefix=cupboard?"Cupboard":"Door";
                feedback.openingClip=DoorClip(prefix+"Open");feedback.closingClip=DoorClip(prefix+"Close");feedback.latchClip=DoorClip(prefix+"Latch");
                feedback.playbackVolume=cupboard?.55f:.65f;feedback.audibleDistance=cupboard?9:14;EditorUtility.SetDirty(feedback);
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
        private static AudioClip DoorClip(string name)
        {
            var clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/ManorDoors/"+name+".wav");
            if(clip==null)throw new InvalidOperationException("Missing door recording: "+name);return clip;
        }
        private static RadioChannel Channel(string name)=>AssetDatabase.LoadAssetAtPath<RadioChannel>("Assets/RadioData/"+name+".asset");
        private static AudioClip Clip(string name)
        {
            var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+"/"+name+".wav");
            if(clip==null)throw new InvalidOperationException("Missing radio recording: "+name);return clip;
        }
    }
}
