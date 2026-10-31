using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(FPSController))]
public class PlayerCaught : MonoBehaviour
{
    public UnityEvent onCaught = new UnityEvent();
    public bool allowKeyboardRestart = true;
    public bool IsCaught { get; private set; }
    private bool restarting;
    private bool ownsMenuModal;

    public void Catch()
    {
        if (IsCaught || Echo.Menu.GameMenu.BlocksPlayerInput || Time.timeScale <= 0f) return;
        IsCaught = true;
        GetComponent<FPSController>().enabled = false;
        var steps = GetComponent<Footsteps>();
        if (steps != null) steps.enabled = false;
        ownsMenuModal = Echo.Menu.GameMenu.TryOpenModal(this, null);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log("Player caught. Press R to restart.", this);
        onCaught.Invoke();
    }

    void Update()
    {
        if (IsCaught && allowKeyboardRestart && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            Restart();
    }

    // Public for a future UI restart button as well as the prototype R shortcut.
    public void Restart()
    {
        if (!IsCaught || restarting) return;
        var scene = gameObject.scene;
        restarting = true;
        if (ownsMenuModal)
        {
            Echo.Menu.GameMenu.RestartGameplay();
            return;
        }
#if UNITY_EDITOR
        if (scene.buildIndex < 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path,
                new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif
        if (scene.buildIndex < 0)
        {
            restarting = false;
            Debug.LogError("Add the gameplay scene to the Build Profile scene list to enable restart in a build.", this);
            return;
        }
        SceneManager.LoadSceneAsync(scene.buildIndex, LoadSceneMode.Single);
    }

    void OnDestroy()
    {
        if (ownsMenuModal) Echo.Menu.GameMenu.CloseModal(this);
    }
}
