using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Echo.Menu.Editor
{
    // Make the editor's Play button use the same entry scene as a build.
    [InitializeOnLoad]
    internal static class MenuPlayModeSetup
    {
        private const string StartMenuItem = "Echo/Починати Play з головного меню";
        private static string PreferenceKey => "Echo.StartFromMenu." + Application.dataPath;

        static MenuPlayModeSetup()
        {
            EditorApplication.delayCall += Configure;
            EditorApplication.projectChanged += Configure;
        }

        private static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            bool enabled = EditorPrefs.GetBool(PreferenceKey, true);
            var scene = enabled ? AssetDatabase.LoadAssetAtPath<SceneAsset>(GameMenu.MainMenuScene) : null;
            if (EditorSceneManager.playModeStartScene != scene)
                EditorSceneManager.playModeStartScene = scene;
        }

        [MenuItem(StartMenuItem)]
        private static void ToggleStartScene()
        {
            EditorPrefs.SetBool(PreferenceKey, !EditorPrefs.GetBool(PreferenceKey, true));
            Configure();
        }

        [MenuItem(StartMenuItem, true)]
        private static bool ValidateStartScene()
        {
            UnityEditor.Menu.SetChecked(StartMenuItem, EditorPrefs.GetBool(PreferenceKey, true));
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
