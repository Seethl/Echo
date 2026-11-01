using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Echo.Menu
{
    // Run before FPSController so Escape blocks input in the very same frame.
    [DefaultExecutionOrder(-10000)]
    public sealed class GameMenu : MonoBehaviour
    {
        public const string MainMenuScene = "Assets/Scenes/MainMenu/MainMenu.unity";
        // Change this one path back to PlayerTestScene to restore the team's test start.
        public const string PlayerTestScene = "Assets/Scenes/Testing/PlayerTest/PlayerTest.unity";
        public const string InteractionTestScene = "Assets/Scenes/Testing/ManorInteractionTest/ManorInteractionTest.unity";
        public static string GameplayScene
        {
            get
            {
                var settings = Resources.Load<GameFlowSettings>("EchoGameFlow");
                return settings != null && !string.IsNullOrEmpty(settings.gameplayScene) ? settings.gameplayScene : InteractionTestScene;
            }
        }

        private static GameMenu instance;
        public static bool BlocksPlayerInput => instance != null &&
            (instance.inMainMenu || instance.paused || instance.loading || instance.modalActive || Time.frameCount <= instance.resumeFrame);

        public static bool IsPaused => instance != null && instance.paused;
        public static event System.Action NewGameStarting;
        public static event System.Action<string> SceneLeaving;
        public static System.Func<bool> ContinueAvailable;
        public static System.Action ContinueAction;
        private bool modalFreezesWorld;
        private bool modalActive;
        private Object modalOwner;
        private System.Action modalEscape;

        // Scene-owned panels borrow the existing time/cursor/EventSystem owner.
        public static bool TryOpenModal(Object owner, System.Action onEscape, bool freezeWorld = true, bool replace = false)
        {
            if (instance == null || owner == null || !instance.inGameplay || instance.paused || instance.loading) return false;
            if (BlocksPlayerInput && !replace) return false;
            if (replace) instance.RestoreModal();
            instance.previousTimeScale = Time.timeScale;
            instance.previousAudioPause = AudioListener.pause;
            instance.modalActive = true;
            instance.modalOwner = owner;
            instance.modalEscape = onEscape;
            instance.modalFreezesWorld = freezeWorld;
            if (freezeWorld) { Time.timeScale = 0f; AudioListener.pause = true; }
            UnlockCursor();
            instance.RefreshPanels();
            return true;
        }

        public static void CloseModal(Object owner)
        {
            if (instance == null || !instance.modalActive || instance.modalOwner != owner) return;
            instance.RestoreModal();
            instance.resumeFrame = Time.frameCount;
            instance.RefreshPanels();
            if (instance.inGameplay && !instance.loading) LockCursor();
        }

        private void RestoreModal()
        {
            if (!modalActive) return;
            if (modalFreezesWorld) { Time.timeScale = previousTimeScale; AudioListener.pause = previousAudioPause; }
            modalActive = false;
            modalOwner = null;
            modalEscape = null;
        }

        public static void RestartGameplay()
        {
            if (instance != null && instance.inGameplay)
            {
                NewGameStarting?.Invoke();
                instance.StartCoroutine(instance.LoadScene(SceneManager.GetActiveScene().path));
            }
        }

        public static void StartNewGame()
        {
            if (instance == null) return;
            NewGameStarting?.Invoke();
            instance.StartCoroutine(instance.LoadScene(GameplayScene));
        }

        public static void ReloadGameplay()
        {
            if (instance != null) instance.StartCoroutine(instance.LoadScene(GameplayScene));
        }

        public static void ReturnToMainMenu()
        {
            if (instance != null) instance.StartCoroutine(instance.LoadScene(MainMenuScene));
        }

        private bool inMainMenu;
        private bool inGameplay;
        private bool paused;
        private bool loading;
        private bool showingSettings;
        private float previousTimeScale = 1f;
        private bool previousAudioPause;
        private int resumeFrame = -1;
        private Canvas canvas;
        private EventSystem menuEventSystem;
        private Image background;
        private RawImage manorBackdrop;
        private Texture2D menuVeilTexture;
        private GameObject mainPanel;
        private GameObject pausePanel;
        private GameObject settingsPanel;
        private Button newGameButton;
        private Button continueButton;
        private Button resumeButton;
        private Slider volumeSlider;
        private Slider sensitivitySlider;
        private Toggle fullscreenToggle;
        private Text volumeLabel;
        private Text sensitivityLabel;
        private Text statusLabel;
        private Font font;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            NewGameStarting = null; SceneLeaving=null; ContinueAvailable = null; ContinueAction = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            new GameObject("Echo Menu").AddComponent<GameMenu>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            GameSettings.Load();
            BuildInterface();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Additive)
                return;

            RestoreModal();
            RestorePause();
            inMainMenu = scene.path == MainMenuScene;
            inGameplay = scene.path == GameplayScene || scene.path == PlayerTestScene || scene.path == InteractionTestScene || scene.name == "SurfaceTest" || scene.name == "InteractivePropsTest";
            loading = false;
            showingSettings = false;
            statusLabel.text = "";
            RefreshPanels();
            if (inMainMenu)
                UnlockCursor();
            else if (inGameplay)
                LockCursor();
        }

        private void Update()
        {
            if (loading || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
                return;

            // An Escape which closes a note is consumed before the pause handler.
            if (modalActive)
            {
                modalEscape?.Invoke();
                return;
            }

            if (showingSettings)
                CloseSettings();
            else if (inGameplay)
            {
                if (paused)
                    Resume();
                else
                    Pause();
            }
        }

        private void Pause()
        {
            if (!inGameplay || paused || loading || modalActive)
                return;

            previousTimeScale = Time.timeScale;
            previousAudioPause = AudioListener.pause;
            paused = true;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            UnlockCursor();
            RefreshPanels();
        }

        private void Resume()
        {
            if (!paused || loading)
                return;

            RestorePause();
            showingSettings = false;
            // Do not pass a Resume click (or its mouse delta) into the controller.
            resumeFrame = Time.frameCount;
            RefreshPanels();
            LockCursor();
        }

        private void RestorePause()
        {
            if (!paused)
                return;
            Time.timeScale = previousTimeScale;
            AudioListener.pause = previousAudioPause;
            paused = false;
        }

        private void OpenSettings()
        {
            volumeSlider.SetValueWithoutNotify(GameSettings.MasterVolume);
            sensitivitySlider.SetValueWithoutNotify(GameSettings.SensitivityMultiplier);
            fullscreenToggle.SetIsOnWithoutNotify(GameSettings.Fullscreen);
            UpdateSettingLabels();
            showingSettings = true;
            RefreshPanels();
        }

        private void CloseSettings()
        {
            showingSettings = false;
            RefreshPanels();
        }

        private void RefreshPanels()
        {
            continueButton.interactable = ContinueAvailable != null && ContinueAvailable();
            continueButton.GetComponentInChildren<Text>().color = continueButton.interactable ? new Color(.87f,.86f,.8f) : new Color(.45f,.47f,.44f);
            bool visible = inMainMenu || paused || loading;
            canvas.gameObject.SetActive(visible);
            // Other scenes can keep their own EventSystem without a duplicate active module.
            menuEventSystem.gameObject.SetActive(visible || modalActive);
            mainPanel.SetActive(inMainMenu && !showingSettings && !loading);
            pausePanel.SetActive(paused && !showingSettings && !loading);
            settingsPanel.SetActive(showingSettings && !loading);
            if(manorBackdrop!=null)manorBackdrop.gameObject.SetActive(inMainMenu);
            background.color = new Color(.018f,.025f,.045f,inMainMenu ? .35f : .93f);
            if (visible && !loading)
            {
                GameObject selected = showingSettings ? volumeSlider.gameObject :
                    inMainMenu ? newGameButton.gameObject : resumeButton.gameObject;
                menuEventSystem.SetSelectedGameObject(null);
                menuEventSystem.SetSelectedGameObject(selected);
            }
        }

        private IEnumerator LoadScene(string path)
        {
            if (loading)
                yield break;
            if (!Application.CanStreamedLevelBeLoaded(path))
            {
                statusLabel.text = "Сцену не додано до Build Profiles: " + path;
                Debug.LogError(statusLabel.text);
                yield break;
            }

            loading = true;
            SceneLeaving?.Invoke(path);
            showingSettings = false;
            RestoreModal();
            RestorePause();
            statusLabel.text = "Завантаження…";
            RefreshPanels();
            UnlockCursor();
            yield return SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                Pause();
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused)
                Pause();
        }

        private void OnDestroy()
        {
            if (instance != this)
                return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            RestoreModal();
            RestorePause();
            UnlockCursor();
            if(menuVeilTexture!=null)Destroy(menuVeilTexture);
            instance = null;
        }

        private void Quit()
        {
            RestorePause();
            PlayerPrefs.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void BuildInterface()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject = new GameObject("Menu Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            manorBackdrop=new GameObject("Manor Artwork",typeof(RectTransform),typeof(RawImage)).GetComponent<RawImage>();
            manorBackdrop.transform.SetParent(canvas.transform,false);Stretch(manorBackdrop.rectTransform);
            manorBackdrop.texture=Resources.Load<Texture2D>("EchoMenuBackdrop");manorBackdrop.uvRect=new Rect(1,0,-1,1);manorBackdrop.raycastTarget=false;
            background = MakeImage("Backdrop", canvas.transform, Color.black);Stretch(background.rectTransform);
            var veil=new GameObject("Left reading veil",typeof(RectTransform),typeof(RawImage)).GetComponent<RawImage>();veil.transform.SetParent(manorBackdrop.transform,false);
            menuVeilTexture=new Texture2D(128,1,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            for(int i=0;i<128;i++)menuVeilTexture.SetPixel(i,0,new Color(.015f,.022f,.038f,.94f*(1-Mathf.SmoothStep(0,1,i/127f))));menuVeilTexture.Apply();veil.texture=menuVeilTexture;
            Stretch(veil.rectTransform);veil.rectTransform.anchorMax=new Vector2(.82f,1);veil.raycastTarget=false;
            mainPanel = MakePanel("Main Menu");
            Place((RectTransform)mainPanel.transform,-310,0,540,600);
            MakeText("Title", mainPanel.transform, "E C H O", 78, 205f, 105f);
            var rule=MakeImage("Title rule",mainPanel.transform,EchoUIStyle.Gold);Place(rule.rectTransform,0,140,62,1);rule.raycastTarget=false;
            MakeText("Subtitle", mainPanel.transform, "МАЄТОК ПАМ’ЯТАЄ", 15, 115f, 28f);
            newGameButton = MakeButton(mainPanel.transform, "Нова гра", 38f,
                StartNewGame);
            // There is no save-game system yet. Settings preferences are not a saved game.
            continueButton = MakeButton(mainPanel.transform, "Продовжити", -24f, () => ContinueAction?.Invoke());
            continueButton.interactable = false;
            continueButton.GetComponentInChildren<Text>().color = new Color(0.45f, 0.47f, 0.44f);
            MakeButton(mainPanel.transform, "Налаштування", -86f, OpenSettings);
            MakeButton(mainPanel.transform, "Вихід", -148f, Quit);
            MakeText("Save Status", mainPanel.transform, "Продовження доступне з контрольної точки\nпід час поточного запуску.",
                14, -221f, 52f);

            pausePanel = MakePanel("Pause Menu");
            MakeText("Title", pausePanel.transform, "ПАУЗА", 46, 180f, 70f);
            resumeButton = MakeButton(pausePanel.transform, "Продовжити", 75f, Resume);
            MakeButton(pausePanel.transform, "Налаштування", 9f, OpenSettings);
            MakeButton(pausePanel.transform, "Головне меню", -57f,
                () => StartCoroutine(LoadScene(MainMenuScene)));
            MakeText("Hint", pausePanel.transform, "Escape — повернутися до гри", 17, -144f, 40f);

            settingsPanel = MakePanel("Settings");
            MakeText("Title", settingsPanel.transform, "НАЛАШТУВАННЯ", 38, 210f, 70f);
            volumeLabel = MakeText("Volume Label", settingsPanel.transform, "", 23, 126f, 35f);
            volumeSlider = MakeSlider(settingsPanel.transform, "Master Volume", 81f, 0f, 1f);
            volumeSlider.onValueChanged.AddListener(value =>
            {
                GameSettings.SetMasterVolume(value);
                UpdateSettingLabels();
            });
            sensitivityLabel = MakeText("Sensitivity Label", settingsPanel.transform, "", 23, 19f, 35f);
            sensitivitySlider = MakeSlider(settingsPanel.transform, "Mouse Sensitivity", -26f, 0.1f, 3f);
            sensitivitySlider.onValueChanged.AddListener(value =>
            {
                GameSettings.SetSensitivity(value);
                UpdateSettingLabels();
            });
            fullscreenToggle = MakeToggle(settingsPanel.transform, "Повноекранний режим", -99f);
            fullscreenToggle.onValueChanged.AddListener(GameSettings.SetFullscreen);
            MakeButton(settingsPanel.transform, "Назад", -181f, CloseSettings);
            MakeText("Hint", settingsPanel.transform, "Зміни зберігаються автоматично · Escape — назад", 16, -241f, 36f);

            statusLabel = MakeText("Status", canvas.transform, "", 20, -315f, 42f);
            var eventObject = new GameObject("Menu EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventObject.transform.SetParent(transform, false);
            menuEventSystem = eventObject.GetComponent<EventSystem>();
            canvasObject.SetActive(false);
            eventObject.SetActive(false);
        }

        private void UpdateSettingLabels()
        {
            volumeLabel.text = $"Загальна гучність: {Mathf.RoundToInt(GameSettings.MasterVolume * 100f)}%";
            sensitivityLabel.text = $"Чутливість миші: {GameSettings.SensitivityMultiplier:0.0}×";
        }

        private GameObject MakePanel(string name)
        {
            var panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(canvas.transform, false);
            Place((RectTransform)panel.transform, 0f, 0f, 620f, 600f);
            if(name!="Main Menu")
            {
                var plate=MakeImage("Panel Surface",panel.transform,EchoUIStyle.Ink);Stretch(plate.rectTransform);plate.transform.SetAsFirstSibling();plate.raycastTarget=false;
                var rule=MakeImage("Panel Rule",panel.transform,EchoUIStyle.Gold);Place(rule.rectTransform,0,267,54,1);rule.raycastTarget=false;
            }
            return panel;
        }

        private Text MakeText(string name, Transform parent, string content, int size, float y, float height)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(parent, false);
            Place(label.rectTransform, 0f, y, 580f, height);
            label.font = font;
            label.fontSize = size;
            label.text = content;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.87f, 0.86f, 0.8f);
            label.raycastTarget = false;
            EchoUIStyle.Text(label);
            return label;
        }

        private Button MakeButton(Transform parent, string label, float y, UnityEngine.Events.UnityAction action)
        {
            var image = MakeImage(label, parent, new Color(.11f,.13f,.17f));
            Place(image.rectTransform, 0f, y, 440f, 52f);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.6f,1.45f,1.2f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.65f);
            button.colors = colors;
            var edge=MakeImage("Button Rule",button.transform,new Color(.72f,.59f,.36f,.7f));Place(edge.rectTransform,-219,0,2,22);edge.raycastTarget=false;
            var text = MakeText("Label", button.transform, label, 22, 0f, 52f);
            Stretch(text.rectTransform);
            if (action != null)
                button.onClick.AddListener(action);
            return button;
        }

        private Slider MakeSlider(Transform parent, string name, float y, float min, float max)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Slider));
            root.transform.SetParent(parent, false);
            Place((RectTransform)root.transform, 0f, y, 440f, 36f);
            var track = MakeImage("Track", root.transform, new Color(0.18f, 0.22f, 0.19f));
            Stretch(track.rectTransform);
            track.rectTransform.offsetMin = new Vector2(0f, 12f);
            track.rectTransform.offsetMax = new Vector2(0f, -12f);
            var handleArea = new GameObject("Handle Area", typeof(RectTransform)).GetComponent<RectTransform>();
            handleArea.SetParent(root.transform, false);
            Stretch(handleArea);
            handleArea.offsetMin = new Vector2(12f, 0f);
            handleArea.offsetMax = new Vector2(-12f, 0f);
            var handle = MakeImage("Handle", handleArea, EchoUIStyle.Gold);
            handle.rectTransform.sizeDelta = new Vector2(24f, 32f);
            var slider = root.GetComponent<Slider>();
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            return slider;
        }

        private Toggle MakeToggle(Transform parent, string label, float y)
        {
            var image = MakeImage(label, parent, new Color(.11f,.13f,.17f));
            Place(image.rectTransform, 0f, y, 440f, 48f);
            var toggle = image.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = image;
            var box=MakeImage("Checkbox frame",image.transform,new Color(.25f,.28f,.33f));Place(box.rectTransform,-183,0,26,26);box.raycastTarget=false;
            var check = MakeImage("Check", image.transform, EchoUIStyle.Gold);
            Place(check.rectTransform, -183f, 0f, 22f, 22f);
            toggle.graphic = check;
            var text = MakeText("Label", image.transform, label, 22, 0f, 48f);
            Place(text.rectTransform, 20f, 0f, 380f, 48f);
            return toggle;
        }

        private static Image MakeImage(string name, Transform parent, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            return image;
        }

        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
