using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Echo.Interactions;
using Echo.Manor;
using Echo.Menu;
using Echo.Radio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class PhysicalPropInputChecks
{
    const string Pending = "Echo.PhysicalInputChecks";
    static Mouse mouse;
    static Keyboard keyboard;
    static Camera camera;
    static PhysicalPropInputChecks() { EditorApplication.update += Poll; }
    public static void ValidateAssets()
    {
        var unlit=Resources.Load<Material>("EchoPropFeedback");
        var hover=Resources.Load<Material>("EchoPropHover");
        Assert(unlit!=null&&unlit.shader.isSupported,"Unlit feedback material included in Resources");
        Assert(hover!=null&&hover.shader.isSupported&&hover.IsKeywordEnabled("_EMISSION"),"Hover emission shader variant retained for builds");
    }
    public static void Begin()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Use the isolated validation project.");
        EditorSceneManager.playModeStartScene = null;
        EditorSceneManager.OpenScene(GameMenu.MainMenuScene);
        EditorApplication.LockReloadAssemblies();
        Application.runInBackground = true;
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }
    static void Poll()
    {
        if (!SessionState.GetBool(Pending, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        var menu = Object.FindAnyObjectByType<GameMenu>();
        if (menu == null) return;
        SessionState.SetBool(Pending, false);
        menu.StartCoroutine(Guard(Run()));
    }
    static IEnumerator Guard(IEnumerator routine)
    {
        var stack = new Stack<IEnumerator>(); stack.Push(routine);
        while (stack.Count > 0)
        {
            object next = null; Exception failure = null;
            try { if (!stack.Peek().MoveNext()) { stack.Pop(); continue; } next = stack.Peek().Current; }
            catch (Exception error) { failure = error; }
            if (failure != null) { Debug.LogException(failure); EditorApplication.Exit(1); yield break; }
            if (next is IEnumerator nested) stack.Push(nested); else yield return next;
        }
        Debug.Log("PHYSICAL_PROP_MOUSE_CHECKS_OK"); EditorApplication.Exit(0);
    }
    static IEnumerator Until(Func<bool> ready, string label, float timeout = 10)
    {
        float end = Time.realtimeSinceStartup + timeout;
        while (!ready()) { if(Time.realtimeSinceStartup >= end)throw new Exception("Timeout: " + label); yield return null; }
    }
    static IEnumerator Run()
    {
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        foreach (var device in InputSystem.devices.OfType<Mouse>().ToArray()) InputSystem.DisableDevice(device);
        foreach (var device in InputSystem.devices.OfType<Keyboard>().ToArray()) InputSystem.DisableDevice(device);
        mouse = InputSystem.AddDevice<Mouse>(); mouse.MakeCurrent();
        keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
        GameMenu.StartNewGame();
        yield return Until(() => ManorProgress.Active != null && Object.FindAnyObjectByType<ManorNavigation>()?.Ready == true && !GameMenu.BlocksPlayerInput, "New Game", 90);
        var player = Object.FindAnyObjectByType<PlayerInteractor>(); camera = player.viewCamera;
        float near = camera.nearClipPlane, fov = camera.fieldOfView;
        Vector3 position = camera.transform.localPosition; Quaternion rotation = camera.transform.localRotation;
        var keypad = Object.FindObjectsByType<KeypadLock>().First(k => k.unlockFlag == "hall.open");
        keypad.Interact(player);
        yield return new WaitForSecondsRealtime(.65f);
        Assert(keypad.physicalView.IsOpen && Time.timeScale == 0, "Lock inspection opens and pauses world");
        var zero = keypad.physicalView.drums[0].GetComponentsInChildren<TextMesh>().First(t => t.text == "0");
        yield return Hover(zero.transform.position);
        Assert(keypad.physicalView.drums[0].GetComponent<PropPhysicalControl>().Highlighted&&keypad.physicalView.CurrentControlHint.Contains("Цифра 1"),"Hovered drum highlights with localized hint");
        Capture("LockHover");
        yield return Click(zero.transform.position, 0);
        yield return new WaitForSecondsRealtime(.25f);
        Assert(keypad.GetDigit(0) == 1, "Mouse click on visible zero turns the first drum while paused");
        yield return Turn(keypad, 0, -1);
        Assert(keypad.GetDigit(0) == 0, "Right mouse turns backward");
        yield return Turn(keypad, 0, -1);
        Assert(keypad.GetDigit(0) == 9, "Backward turn wraps zero to nine");
        yield return Turn(keypad, 0, 1);
        Assert(keypad.GetDigit(0) == 0, "Forward turn wraps nine to zero");
        yield return Scroll(DigitPoint(keypad, 1), 120);
        Assert(keypad.GetDigit(1) == 1, "Wheel increments paused lock");
        yield return Scroll(DigitPoint(keypad, 1), -120);
        Assert(keypad.GetDigit(1) == 0, "Wheel decrements paused lock");
        yield return Click(keypad.physicalView.submitButton.GetComponent<Collider>().bounds.center, 0);
        Assert(keypad.physicalView.IsOpen && !keypad.IsUnlocked, "Actual OK button rejects wrong code");
        Assert(keypad.physicalView.ShowingFailure,"Wrong code has visible feedback while paused");
        yield return null;yield return null;
        Capture("LockWrongCode");
        string code = keypad.EffectiveCode;
        for (int i = 0; i < 3; i++)
        {
            for (int digit = 0; digit < code[i] - '0'; digit++) yield return Turn(keypad, i, 1);
            Assert(keypad.GetDigit(i) == code[i] - '0', "Mouse enters current run digit " + i);
        }
        yield return Click(keypad.physicalView.submitButton.GetComponent<Collider>().bounds.center, 0);
        Assert(keypad.physicalView.ShowingSuccess&&keypad.IsUnlocked,"Correct code stays in view for the opening animation");
        yield return new WaitForSecondsRealtime(.3f);Capture("LockOpened");
        yield return Until(()=>!keypad.physicalView.IsOpen,"Lock opening animation",2);
        yield return null;
        Assert(keypad.IsUnlocked && ManorProgress.Active.Has("hall.open") && !keypad.physicalView.IsOpen, "Physical OK button opens current run lock");
        Assert(keypad.physicalView.CurrentControlHint==""&&!keypad.GetComponentsInChildren<PropPhysicalControl>().Any(c=>c.Highlighted),"Closing lock clears highlight and hint");
        Assert(Time.timeScale == 1 && !AudioListener.pause && !GameMenu.BlocksPlayerInput, "Unlock restores world and input");
        Assert(Mathf.Approximately(camera.nearClipPlane, near) && Mathf.Approximately(camera.fieldOfView, fov) && Vector3.Distance(camera.transform.localPosition, position) < .001f && Quaternion.Angle(camera.transform.localRotation, rotation) < .01f, "Unlock restores camera clipping, zoom and pose");

        var radio = Object.FindAnyObjectByType<PortableRadio>(); radio.Interact(player); radio.OpenControls();
        yield return new WaitForSecondsRealtime(.65f);
        var view = radio.physicalView;
        Assert(view.IsOpen && Time.timeScale == 1, "Radio inspection keeps world running");
        radio.Tune(900); radio.SetVolume(.4f);
        Vector3 knob = view.tuningKnob.GetComponent<Collider>().bounds.center;
        yield return Hover(knob);
        Assert(view.tuningKnob.GetComponent<PropPhysicalControl>().Highlighted&&view.CurrentControlHint.Contains("Частота"),"Hovered radio knob highlights with hint");
        radio.powered=false;yield return new WaitForSecondsRealtime(.25f);Capture("RadioOff");
        radio.powered=true;yield return new WaitForSecondsRealtime(.25f);Capture("RadioOn");
        yield return Click(knob, 0); Assert(radio.frequency == 920, "Left click increases radio tuning");
        yield return Click(knob, 1); Assert(radio.frequency == 900, "Right click decreases radio tuning");
        yield return Hold(knob, 0, .8f);
        int raised = radio.frequency; Assert(raised >= 980, "Holding left continuously tunes upward");
        yield return new WaitForSecondsRealtime(.2f);
        Assert(radio.frequency == raised, "Release stops tuning");
        yield return Hold(knob, 1, .8f); Assert(radio.frequency <= raised - 80, "Holding right continuously tunes downward");
        radio.Tune(900);
        yield return Scroll(knob, 120); Assert(radio.frequency == 920, "Wheel tunes upward");
        yield return Scroll(knob, -120); Assert(radio.frequency == 900, "Wheel tunes downward");
        Vector3 volume = view.volumeKnob.GetComponent<Collider>().bounds.center;
        yield return Hold(volume, 0, .8f); Assert(radio.volume > .7f, "Volume increases continuously");
        yield return Hold(volume, 1, .8f); Assert(radio.volume < .5f, "Volume decreases continuously");
        radio.Tune(1280); yield return Hold(knob, 0, .4f); Assert(radio.frequency == 1280, "Held tuning respects upper limit");
        radio.Tune(760); yield return Hold(knob, 1, .4f); Assert(radio.frequency == 760, "Held tuning respects lower limit");
        var power = radio.GetComponentsInChildren<PropPhysicalControl>().First(c => c.controlId == 2).GetComponent<Collider>();
        bool powered = radio.powered;
        Vector3 buttonBase=power.transform.localPosition;
        InputSystem.QueueStateEvent(mouse,new MouseState{position=camera.WorldToScreenPoint(power.bounds.center)}.WithButton(MouseButton.Left));
        yield return Until(()=>Vector3.Distance(buttonBase,power.transform.localPosition)>.001f,"Power button depression",2);
        Assert(Vector3.Distance(buttonBase,power.transform.localPosition)>.001f,"Power button visibly depresses");
        InputSystem.QueueStateEvent(mouse,new MouseState{position=camera.WorldToScreenPoint(power.bounds.center)});yield return null;
        yield return new WaitForSecondsRealtime(.2f);
        Assert(Vector3.Distance(buttonBase,power.transform.localPosition)<.0001f,"Power button returns to rest");
        powered=radio.powered;
        yield return Hold(power.bounds.center, 0, .8f); Assert(radio.powered != powered, "Holding power toggles exactly once");
        yield return Escape();
        Assert(!view.IsOpen && !GameMenu.BlocksPlayerInput && radio.transform.parent == camera.transform && Mathf.Approximately(camera.nearClipPlane, near), "Escape closes radio, restores attachment and camera");
        Assert(view.CurrentControlHint==""&&!radio.GetComponentsInChildren<PropPhysicalControl>().Any(c=>c.Highlighted),"Escape clears radio hover feedback");
    }
    static IEnumerator Hover(Vector3 point)
    {
        InputSystem.QueueStateEvent(mouse,new MouseState{position=camera.WorldToScreenPoint(point)});
        yield return null;yield return null;
    }
    static void Capture(string name)
    {
        Directory.CreateDirectory("work/interactive_props/feedback");
        var hint=Object.FindObjectsByType<Canvas>().FirstOrDefault(c=>c.name=="Prop hover hint"&&c.GetComponentInChildren<UnityEngine.UI.Text>()?.gameObject.activeInHierarchy==true);
        if(hint!=null){hint.renderMode=RenderMode.ScreenSpaceCamera;hint.worldCamera=camera;hint.planeDistance=.05f;}
        var target=new RenderTexture(1280,720,24);var previousTarget=camera.targetTexture;var previousActive=RenderTexture.active;float previousAspect=camera.aspect;
        camera.targetTexture=target;Canvas.ForceUpdateCanvases();
        if(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline!=null)
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=target});
        else camera.Render();
        RenderTexture.active=target;
        var picture=new Texture2D(1280,720,TextureFormat.RGB24,false);picture.ReadPixels(new Rect(0,0,1280,720),0,0);picture.Apply();
        File.WriteAllBytes("work/interactive_props/feedback/"+name+".png",picture.EncodeToPNG());
        camera.targetTexture=previousTarget;camera.aspect=previousAspect;RenderTexture.active=previousActive;
        if(hint!=null)hint.renderMode=RenderMode.ScreenSpaceOverlay;
        Object.Destroy(target);Object.Destroy(picture);
    }
    static Vector3 DigitPoint(KeypadLock keypad, int index) => keypad.physicalView.drums[index].GetComponentsInChildren<TextMesh>().First(t => t.text == keypad.GetDigit(index).ToString()).transform.position;
    static IEnumerator Turn(KeypadLock keypad, int index, int direction)
    {
        yield return Click(DigitPoint(keypad, index), direction > 0 ? 0 : 1);
        yield return new WaitForSecondsRealtime(.25f);
    }
    static IEnumerator Scroll(Vector3 point, float amount)
    {
        Vector2 pixel = camera.WorldToScreenPoint(point);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = pixel, scroll = new Vector2(0, amount) });
        yield return null;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = pixel });
        yield return new WaitForSecondsRealtime(.25f);
    }
    static IEnumerator Hold(Vector3 point, int button, float seconds)
    {
        Vector2 pixel = camera.WorldToScreenPoint(point);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = pixel }.WithButton(button == 0 ? MouseButton.Left : MouseButton.Right));
        yield return new WaitForSecondsRealtime(seconds);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = pixel });
        yield return null;
    }
    static IEnumerator Escape()
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
    }
    static IEnumerator Click(Vector3 point, int button)
    {
        Vector2 pixel = camera.WorldToScreenPoint(point);
        Physics.Raycast(camera.ScreenPointToRay(pixel), out var hit, 3);
        Debug.Log("PHYSICAL_MOUSE_HIT " + (hit.collider != null ? hit.collider.name : "none") + " pixel=" + pixel + " near="+camera.nearClipPlane+" time=" + Time.timeScale + " updateMode=" + InputSystem.settings.updateMode);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = pixel }.WithButton(button == 0 ? MouseButton.Left : MouseButton.Right));
        yield return null;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = pixel });
        yield return null;
    }
    static void Assert(bool condition, string label) { if (!condition) throw new Exception("Physical mouse check failed: " + label); Debug.Log("PHYSICAL_MOUSE_PASS " + label); }
}
