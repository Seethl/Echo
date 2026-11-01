using System;
using Echo.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Echo.Manor.Editor
{
    public static class ManorAmbienceSetup
    {
        public const string ClipPath = "Assets/Audio/ManorAmbience/ManorAmbientLoop.wav";

        [MenuItem("Echo/Під’єднати тихий ембієнт")]
        public static void ApplyAndSave()
        {
            if (EditorSceneManager.GetActiveScene().path != ManorPrototypeBuilder.ScenePath)
                throw new InvalidOperationException("Open ManorPrototype first.");
            Apply();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
        }

        public static void Apply()
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath(ClipPath);
            if (importer == null) throw new InvalidOperationException("Missing manor ambience recording.");
            var sample = importer.defaultSampleSettings;
            sample.loadType = AudioClipLoadType.Streaming;
            sample.compressionFormat = AudioCompressionFormat.Vorbis;
            sample.quality = .65f;
            sample.preloadAudioData = false;
            importer.defaultSampleSettings = sample;
            importer.forceToMono = false;
            importer.loadInBackground = true;
            importer.SaveAndReimport();
            var ambience = Object.FindAnyObjectByType<ManorAmbience>(FindObjectsInactive.Include);
            if (ambience == null) ambience = new GameObject("Manor ambient music").AddComponent<ManorAmbience>();
            ambience.music = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath);
            ambience.playbackVolume = .1f;
            ambience.listeningMultiplier = .25f;
            EditorUtility.SetDirty(ambience);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }
}
