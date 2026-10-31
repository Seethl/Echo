using Echo.Menu;
using UnityEditor;
using UnityEngine;

public static class ManorSessionCheckMenu
{
    [MenuItem("Echo/Перевірити інтегрований сеанс (Play)")]
    private static void Run()
    {
        var menu = Object.FindAnyObjectByType<GameMenu>();
        if (menu != null) menu.StartCoroutine(ManorSessionChecks.Run());
    }

    [MenuItem("Echo/Перевірити інтегрований сеанс (Play)", true)]
    private static bool Available() => EditorApplication.isPlaying && Object.FindAnyObjectByType<GameMenu>() != null;
}
