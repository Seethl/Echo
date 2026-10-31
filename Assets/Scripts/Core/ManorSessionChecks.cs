using System;
using System.Collections;
using Echo.Interactions;
using Echo.Manor;
using Echo.Menu;
using Echo.Radio;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ManorSessionChecks
{
    public static IEnumerator Run()
    {
        int checks = 0;
        Action<bool, string> check = (condition, message) =>
        {
            if (!condition) throw new InvalidOperationException("Manor session check: " + message);
            checks++;
        };
        GameMenu.StartNewGame();
        yield return Ready();
        var progress = ManorProgress.Active;
        var player = progress.player;
        var bridge = progress.GetComponent<ManorGameplayBridge>();
        var enemy = UnityEngine.Object.FindAnyObjectByType<EnemyInvestigator>();
        check(SceneManager.GetActiveScene().path == GameMenu.GameplayScene, "new game opens the configured manor");
        check(bridge.Progression.CurrentStage == GameProgression.Stage.Exploration, "new level begins in exploration");
        check(enemy != null && !enemy.GetComponent<EnemyThreat>().CanCatch && !enemy.CanSeeTarget(), "intro cannot begin a chase or capture");
        check(!progress.HasCheckpoint, "new game clears previous checkpoint");
        player.AddKey("manor.service-passage");
        var keypad = UnityEngine.Object.FindAnyObjectByType<KeypadLock>();
        keypad.SetDigits(4, 1, 7);
        check(keypad.Submit(player), "actual manor keypad opens the hall");
        yield return null; yield return null;
        check(bridge.Progression.CurrentStage == GameProgression.Stage.Awakening && !enemy.GetComponent<EnemyThreat>().CanCatch,
            "hall opening is a safe warning stage");
        var puzzle = UnityEngine.Object.FindAnyObjectByType<SymbolPuzzle>();
        foreach (var symbol in puzzle.solution) puzzle.Submit(symbol, player);
        player.AddKey("tool.winch");
        yield return null; yield return null;
        check(progress.Has("library.cache") && bridge.Progression.EnemyCanCatch, "library puzzle advances actual manor danger");
        var radio = UnityEngine.Object.FindAnyObjectByType<PortableRadio>();
        radio.Interact(player);
        radio.frequency = 1140; radio.powered = false;
        var checkpoint = Array.Find(UnityEngine.Object.FindObjectsByType<ManorCheckpoint>(), point => point.stage == 1);
        check(checkpoint != null, "hall checkpoint exists in the integrated scene");
        progress.SaveCheckpoint(checkpoint.stage, checkpoint.playerSpawn, checkpoint.enemySpawn);
        var oldProgress = progress;
        Vector3 savedSpawn = checkpoint.playerSpawn;
        var life = player.GetComponent<PlayerCaught>();
        life.Catch();
        check(life.IsCaught && Time.timeScale == 0f && player.ui.IsCustomOpen(progress), "capture opens the level's failure screen and freezes the world");
        ManorProgress.ResumeCheckpoint();
        yield return WaitFor(() => ManorProgress.Active != null && !ReferenceEquals(ManorProgress.Active, oldProgress), 30f, "checkpoint scene reload");
        yield return Ready();
        progress = ManorProgress.Active; player = progress.player;
        bridge = progress.GetComponent<ManorGameplayBridge>();
        check(!player.GetComponent<PlayerCaught>().IsCaught && player.GetComponent<FPSController>().enabled, "checkpoint restores live player controls");
        check(player.HasKey("manor.service-passage") && player.HasKey("tool.winch"), "checkpoint preserves inventory");
        check(progress.Has("hall.open") && progress.Has("library.cache"), "checkpoint preserves puzzle flags");
        check(bridge.Progression.CurrentStage == GameProgression.Stage.Danger, "checkpoint restores danger instead of returning to the intro");
        check(Vector3.Distance(player.transform.position, savedSpawn) < 0.3f, "checkpoint restores the configured safe spawn");
        radio = UnityEngine.Object.FindAnyObjectByType<PortableRadio>();
        check(radio.Held && radio.transform.IsChildOf(player.transform) && radio.frequency == 1140 && !radio.powered,
            "checkpoint restores held radio and settings");
        progress.Set("grate.open");
        yield return null; yield return null;
        check(bridge.Progression.SoundToolUnlocked, "later route flag restores sound mastery");
        GameMenu.ReturnToMainMenu();
        yield return WaitFor(() => SceneManager.GetActiveScene().path == GameMenu.MainMenuScene, 30f, "return to menu");
        yield return null; yield return null;
        GameMenu.StartNewGame();
        yield return Ready();
        progress = ManorProgress.Active;
        check(!progress.HasCheckpoint && !progress.Has("hall.open") && !progress.player.HasKey("tool.winch"),
            "new game after continue resets inventory, flags and checkpoint");
        check(progress.GetComponent<ManorGameplayBridge>().Progression.CurrentStage == GameProgression.Stage.Exploration && Time.timeScale == 1f,
            "new game restores a safe unpaused opening");
        GameMenu.ReturnToMainMenu();
        yield return WaitFor(() => SceneManager.GetActiveScene().path == GameMenu.MainMenuScene, 30f, "final return to menu");
        Debug.Log($"Manor session checks: {checks} passed.");
    }

    private static IEnumerator Ready()
    {
        yield return WaitFor(() => ManorProgress.Active != null && ManorProgress.Active.player != null &&
            UnityEngine.Object.FindAnyObjectByType<ManorNavigation>() != null && UnityEngine.Object.FindAnyObjectByType<ManorNavigation>().Ready,
            30f, "manor navigation ready");
        yield return null; yield return null;
    }

    private static IEnumerator WaitFor(Func<bool> condition, float timeout, string description)
    {
        float end = Time.realtimeSinceStartup + timeout;
        while (!condition())
        {
            if (Time.realtimeSinceStartup >= end) throw new TimeoutException("Manor session timeout: " + description);
            yield return null;
        }
    }
}
