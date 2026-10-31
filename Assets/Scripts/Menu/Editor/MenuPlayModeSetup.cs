using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Echo.Menu.Editor
{
    // Test the open scene by default; starting from the menu is an explicit opt-in.
    [InitializeOnLoad]
    internal static class MenuPlayModeSetup
    {
        private const string StartMenuItem = "Echo/Починати Play з головного меню";
        // New key deliberately drops the old automatic/default-on behaviour.
        private static string PreferenceKey => "Echo.StartFromMenu.OptIn." + Application.dataPath;

        static MenuPlayModeSetup()
        {
            EditorApplication.delayCall += Configure;
            EditorApplication.projectChanged += Configure;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode) Configure();
        }

        private static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            bool enabled = EditorPrefs.GetBool(PreferenceKey, false);
            var scene = enabled ? AssetDatabase.LoadAssetAtPath<SceneAsset>(GameMenu.MainMenuScene) : null;
            if (EditorSceneManager.playModeStartScene != scene)
                EditorSceneManager.playModeStartScene = scene;
        }

        [MenuItem(StartMenuItem)]
        private static void ToggleStartScene()
        {
            EditorPrefs.SetBool(PreferenceKey, !EditorPrefs.GetBool(PreferenceKey, false));
            Configure();
        }

        [MenuItem(StartMenuItem, true)]
        private static bool ValidateStartScene()
        {
            UnityEditor.Menu.SetChecked(StartMenuItem, EditorPrefs.GetBool(PreferenceKey, false));
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        [MenuItem("Echo/Відкрити головне меню")]
        private static void OpenMainMenu()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(GameMenu.MainMenuScene);
        }

        [MenuItem("Echo/Відкрити головне меню", true)]
        private static bool ValidateOpenMainMenu() => !EditorApplication.isPlayingOrWillChangePlaymode;
    }
}
