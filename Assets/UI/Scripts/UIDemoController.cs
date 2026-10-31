using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Echo.UI
{
    /// <summary>
    /// Interactive controller for the UI Demo scene (Assets/Scenes/Testing/UIDemo/UIDemo.unity).
    /// Provides an integrated testing workbench for QA and integrators:
    /// - Test Note Panel with standard note, scrollable long lore, and Ukrainian typography pangrams.
    /// - Test Combination Lock with interactive keypad, keyboard bindings (0-9, Numpad, Backspace, Enter, Esc),
    ///   clear, submit, error shake/flash, and success feedback.
    /// - Live screen resolution & aspect ratio monitor (supporting 1280x720, 1920x1080, 2560x1440 verification).
    /// - Self-bootstraps if loaded in an empty scene, ensuring zero friction out-of-the-box.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIDemoController : MonoBehaviour
    {
        [Header("Managed UI Panels")]
        [SerializeField] private NotePanelUI notePanel;
        [SerializeField] private CombinationLockUI combinationLock;

        [Header("Demo Action Buttons")]
        [SerializeField] private Button btnShowStandardNote;
        [SerializeField] private Button btnShowLongNote;
        [SerializeField] private Button btnShowUkrainianPangram;
        [SerializeField] private Button btnShowLock;
        [SerializeField] private Button btnTestDisabledButton;

        [Header("Status & Resolution Display")]
        [SerializeField] private Text resolutionInfoText;
        [SerializeField] private Text logStatusText;

        // Realistic dummy data based on Echo Manor lore
        public const string StandardNoteTitle = "СЛУЖБОВА КАРТКА ДОГЛЯДАЧА";
        public const string StandardNoteContent = 
            "Службова передача: частота 820.\n" +
            "Запасний ключ від службового проходу — у шафці біля дверей.\n\n" +
            "Радіо: R — налаштувати частоту; F — живлення; Q — поставити на стіл або підлогу.\n" +
            "Пам’ятайте: якщо чути гучні кроки в коридорі, присядьте (Ctrl) або залиште ввімкнене радіо як приманку.";

        public const string LongNoteTitle = "ЩОДЕННИК СТАРОГО ДОГЛЯДАЧА (ЗАПИС 47)";
        public const string LongNoteContent = 
            "14 листопада 1924 року.\n\n" +
            "Сьогодні гуркіт з боку галереї знову не вщухав до світанку. Стара віконниця хитається від протягу, " +
            "а суха гілка в’яза застрягла між петлями. Поки її не витягнути, засув не стане на місце. " +
            "Через цей шум службовий радіоприймач на 940 кГц ледь розбирає слова — цифри коду тонуть у тріскоті.\n\n" +
            "Мешканець мандрує між холом і бібліотекою. Його кроки важкі, але він реагує на найменший шерех. " +
            "Якщо ступити на бите скло коло каміна — він миттєво повертає в твій бік. Килим трохи приглушує кроки, " +
            "але бігти не можна в жодному разі.\n\n" +
            "У бібліотеці схованка зачинена на три символи. На стіні висять панелі: Місяць, Дерево та Птах. " +
            "Кожен знак має власний дзвін і ритм. Щоб відчинити тайник з руків’ям лебідки, треба спершу дослухатися " +
            "до службового сигналу 1060, зіставити його з панелями, а потім набрати на щитку.\n\n" +
            "Лебідку на кухні заклинило. Без руків’я решітку не підняти. Кожен оберт лебідки вимагає зусилля — " +
            "треба тримати важіль, доки стопор не клацне двічі, і лише тоді відпускати.\n\n" +
            "Якщо хтось знайде ці рядки — не поспішайте. Слухайте маєток. Він пам’ятає.";

        public const string PangramTitle = "ТЕСТ УКРАЇНСЬКОЇ ТИПОГРАФІКИ";
        public const string PangramContent = 
            "Перевірка символів:\n" +
            "Великі літери: І, Ї, Є, Ґ, А, Б, В, Г, Д, Ж, З, И, К, Л, М, Н, О, П, Р, С, Т, У, Ф, Х, Ц, Ч, Ш, Щ, Ю, Я\n" +
            "Малі літери: і, ї, є, ґ, а, б, в, г, д, ж, з, и, к, л, м, н, о, п, р, с, т, у, ф, х, ц, ч, ш, щ, ю, я\n" +
            "Апострофи й лапки: м’яч, пам’ять, в’юн, сім’я, під’їзд, «Ехо», „цитата“\n" +
            "Цифри й знаки: 0123456789 № % : ; ? * ( ) _ + - = / \\ [ ] { } → • …\n\n" +
            "Панграма 1: «Чуєш, їхній ґедзь б’є юного хробака в яслах?»\n" +
            "Панграма 2: «Фабрикуймо гіпотези: шеф чекає з’їзд юристів, жадібний ґава-пурпур.»\n\n" +
            "Перевірка чисел: 1280×720 · 1920×1080 · 2560×1440 · Код 4-1-7";

        public const string CorrectCode = "417";

        private Font uiFont;

        private void Awake()
        {
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            EnsureEventSystem();
            EnsureCanvasAndCamera();

            if (notePanel == null)
            {
                notePanel = GetComponentInChildren<NotePanelUI>(true);
                if (notePanel == null) notePanel = BuildRuntimeNotePanel(transform);
            }

            if (combinationLock == null)
            {
                combinationLock = GetComponentInChildren<CombinationLockUI>(true);
                if (combinationLock == null) combinationLock = BuildRuntimeCombinationLock(transform);
            }

            if (btnShowStandardNote == null)
            {
                BuildRuntimeSidebar(transform);
            }
        }

        private void Start()
        {
            BindButtons();
            UpdateResolutionDisplay();
            SetLog("Готово до тестування. Оберіть дію на панелі ліворуч.");

            if (combinationLock != null)
            {
                combinationLock.OnSubmitCode += HandleCodeSubmitted;
            }

            if (notePanel != null) notePanel.CloseNote();
            if (combinationLock != null) combinationLock.CloseLock();
        }

        private void Update()
        {
            UpdateResolutionDisplay();
        }

        private void BindButtons()
        {
            if (btnShowStandardNote != null)
            {
                btnShowStandardNote.onClick.RemoveAllListeners();
                btnShowStandardNote.onClick.AddListener(() => ShowNote(StandardNoteTitle, StandardNoteContent));
            }

            if (btnShowLongNote != null)
            {
                btnShowLongNote.onClick.RemoveAllListeners();
                btnShowLongNote.onClick.AddListener(() => ShowNote(LongNoteTitle, LongNoteContent));
            }

            if (btnShowUkrainianPangram != null)
            {
                btnShowUkrainianPangram.onClick.RemoveAllListeners();
                btnShowUkrainianPangram.onClick.AddListener(() => ShowNote(PangramTitle, PangramContent));
            }

            if (btnShowLock != null)
            {
                btnShowLock.onClick.RemoveAllListeners();
                btnShowLock.onClick.AddListener(ShowCombinationLock);
            }

            if (btnTestDisabledButton != null)
            {
                btnTestDisabledButton.interactable = false;
            }
        }

        public void ShowNote(string title, string content)
        {
            if (combinationLock != null && combinationLock.IsVisible)
                combinationLock.CloseLock();

            if (notePanel != null)
            {
                notePanel.ShowNote(title, content, () => SetLog("Записку закрито."));
                SetLog("Відкрито записку: " + title);
            }
        }

        public void ShowCombinationLock()
        {
            if (notePanel != null && notePanel.IsVisible)
                notePanel.CloseNote();

            if (combinationLock != null)
            {
                combinationLock.OpenLock(HandleCodeSubmitted, () => SetLog("Введення коду скасовано."));
                SetLog("Відкрито кодовий замок (Тестовий правильний код: 417).");
            }
        }

        private void HandleCodeSubmitted(string code)
        {
            SetLog("Спроба введення коду: [" + code + "]");

            if (code == CorrectCode)
            {
                combinationLock.ShowSuccess("Код вірний: 417. Замок відчинено!");
                SetLog("УСПІХ: Код " + code + " підійшов.");
            }
            else
            {
                combinationLock.ShowError("Код [" + code + "] невірний. Слухайте частоту 940.");
                SetLog("ПОМИЛКА: Невірний код " + code + ".");
            }
        }

        private void UpdateResolutionDisplay()
        {
            if (resolutionInfoText != null)
            {
                float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
                resolutionInfoText.text = $"Роздільна здатність: {Screen.width}×{Screen.height} ({aspect:F2}:1)";
            }
        }

        private void SetLog(string message)
        {
            if (logStatusText != null)
            {
                logStatusText.text = message;
            }
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                DontDestroyOnLoad(esGo);
            }
        }

        private void EnsureCanvasAndCamera()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
                gameObject.AddComponent<GraphicRaycaster>();
            }

            if (FindAnyObjectByType<Camera>() == null)
            {
                var camGo = new GameObject("UI Camera", typeof(Camera));
                var cam = camGo.GetComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.015f, 0.02f, 0.025f, 1f);
                cam.orthographic = true;
            }
        }

        private void BuildRuntimeSidebar(Transform parent)
        {
            var sidebar = new GameObject("Sidebar", typeof(RectTransform), typeof(Image));
            sidebar.transform.SetParent(parent, false);
            var sbRect = (RectTransform)sidebar.transform;
            sbRect.anchorMin = new Vector2(0, 0);
            sbRect.anchorMax = new Vector2(0, 1);
            sbRect.pivot = new Vector2(0, 0.5f);
            sbRect.anchoredPosition = Vector2.zero;
            sbRect.sizeDelta = new Vector2(380, 0);
            sidebar.GetComponent<Image>().color = new Color(0.025f, 0.035f, 0.04f, 0.98f);

            var sbTitle = CreateText("HeaderTitle", sidebar.transform, "ECHO HORROR", 30, TextAnchor.MiddleCenter, FontStyle.Bold);
            sbTitle.color = UIStyleConfig.AccentAntiqueBronze;
            var sbtRect = sbTitle.rectTransform;
            sbtRect.anchorMin = new Vector2(0, 1);
            sbtRect.anchorMax = new Vector2(1, 1);
            sbtRect.pivot = new Vector2(0.5f, 1);
            sbtRect.anchoredPosition = new Vector2(0, -25);
            sbtRect.sizeDelta = new Vector2(-40, 36);

            var sbSub = CreateText("HeaderSub", sidebar.transform, "UI/UX QA & Verification Hub", 15, TextAnchor.MiddleCenter, FontStyle.Normal);
            sbSub.color = UIStyleConfig.TextSecondary;
            var sbsRect = sbSub.rectTransform;
            sbsRect.anchorMin = new Vector2(0, 1);
            sbsRect.anchorMax = new Vector2(1, 1);
            sbsRect.pivot = new Vector2(0.5f, 1);
            sbsRect.anchoredPosition = new Vector2(0, -65);
            sbsRect.sizeDelta = new Vector2(-40, 22);

            var btnList = new GameObject("ButtonList", typeof(RectTransform), typeof(VerticalLayoutGroup));
            btnList.transform.SetParent(sidebar.transform, false);
            var blRect = (RectTransform)btnList.transform;
            blRect.anchorMin = new Vector2(0, 1);
            blRect.anchorMax = new Vector2(1, 1);
            blRect.pivot = new Vector2(0.5f, 1);
            blRect.anchoredPosition = new Vector2(0, -110);
            blRect.sizeDelta = new Vector2(-40, 380);

            var vlg = btnList.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 14;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            btnShowStandardNote = CreateButton("Btn_StdNote", btnList.transform, "1. Записка (Звичайна)", new Vector2(300, 48), Vector2.zero);
            btnShowLongNote = CreateButton("Btn_LongNote", btnList.transform, "2. Записка (Довгий текст + Scroll)", new Vector2(300, 48), Vector2.zero);
            btnShowUkrainianPangram = CreateButton("Btn_UkrNote", btnList.transform, "3. Укр. символи (І, Ї, Є, Ґ, ’)", new Vector2(300, 48), Vector2.zero);
            btnShowLock = CreateButton("Btn_Lock", btnList.transform, "4. Кодовий замок (Інтерактив)", new Vector2(300, 48), Vector2.zero);
            btnShowLock.GetComponent<Image>().color = new Color(0.18f, 0.28f, 0.22f, 1f);

            btnTestDisabledButton = CreateButton("Btn_Disabled", btnList.transform, "5. Стан Disabled (Вимкнено)", new Vector2(300, 48), Vector2.zero);

            resolutionInfoText = CreateText("ResolutionText", sidebar.transform, "Роздільна здатність: ...", 15, TextAnchor.MiddleLeft, FontStyle.Normal);
            resolutionInfoText.color = UIStyleConfig.TextSecondary;
            var rtRect = resolutionInfoText.rectTransform;
            rtRect.anchorMin = new Vector2(0, 0);
            rtRect.anchorMax = new Vector2(1, 0);
            rtRect.pivot = new Vector2(0.5f, 0);
            rtRect.anchoredPosition = new Vector2(20, 68);
            rtRect.sizeDelta = new Vector2(-40, 26);

            logStatusText = CreateText("LogText", sidebar.transform, "Готово до тестування.", 14, TextAnchor.UpperLeft, FontStyle.Italic);
            logStatusText.color = UIStyleConfig.AccentAntiqueBronze;
            var ltRect = logStatusText.rectTransform;
            ltRect.anchorMin = new Vector2(0, 0);
            ltRect.anchorMax = new Vector2(1, 0);
            ltRect.pivot = new Vector2(0.5f, 0);
            ltRect.anchoredPosition = new Vector2(20, 18);
            ltRect.sizeDelta = new Vector2(-40, 44);
        }

        private NotePanelUI BuildRuntimeNotePanel(Transform parent)
        {
            var root = new GameObject("NotePanel", typeof(RectTransform), typeof(CanvasGroup), typeof(NotePanelUI));
            root.transform.SetParent(parent, false);
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = rootRect.anchorMax = rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = new Vector2(120, 0);
            rootRect.sizeDelta = new Vector2(740, 520);

            var backdrop = new GameObject("ModalBackdrop", typeof(RectTransform), typeof(Image));
            backdrop.transform.SetParent(root.transform, false);
            var backdropRect = (RectTransform)backdrop.transform;
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = new Vector2(-1500, -1500);
            backdropRect.offsetMax = new Vector2(1500, 1500);
            var backdropImg = backdrop.GetComponent<Image>();
            backdropImg.color = UIStyleConfig.BackgroundDim;

            var frame = new GameObject("PanelFrame", typeof(RectTransform), typeof(Image));
            frame.transform.SetParent(root.transform, false);
            var frameRect = (RectTransform)frame.transform;
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = frameRect.offsetMax = Vector2.zero;
            var frameImg = frame.GetComponent<Image>();
            frameImg.color = UIStyleConfig.PanelSurface;

            var accent = new GameObject("AccentTop", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(frame.transform, false);
            var accentRect = (RectTransform)accent.transform;
            accentRect.anchorMin = new Vector2(0, 1);
            accentRect.anchorMax = new Vector2(1, 1);
            accentRect.pivot = new Vector2(0.5f, 1);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(0, 4);
            accent.GetComponent<Image>().color = UIStyleConfig.AccentAntiqueBronze;

            var title = CreateText("NoteTitle", frame.transform, "ЗАПИСКА ДОГЛЯДАЧА", 26, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.color = UIStyleConfig.AccentAntiqueBronze;
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -16);
            titleRect.sizeDelta = new Vector2(-60, 48);

            var scrollView = new GameObject("Scroll View", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollView.transform.SetParent(frame.transform, false);
            var scrollRect = (RectTransform)scrollView.transform;
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(24, 76);
            scrollRect.offsetMax = new Vector2(-24, -68);
            var scrollBg = scrollView.GetComponent<Image>();
            scrollBg.color = new Color(0.02f, 0.03f, 0.035f, 0.6f);

            var sRectComponent = scrollView.GetComponent<ScrollRect>();
            sRectComponent.horizontal = false;
            sRectComponent.vertical = true;
            sRectComponent.movementType = ScrollRect.MovementType.Clamped;
            sRectComponent.scrollSensitivity = 25f;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
            viewport.transform.SetParent(scrollView.transform, false);
            var viewportRect = (RectTransform)viewport.transform;
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(8, 8);
            viewportRect.offsetMax = new Vector2(-28, -8);
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            viewport.GetComponent<Image>().color = Color.white;
            sRectComponent.viewport = viewportRect;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 200);

            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 16, 16);
            vlg.spacing = 10;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = content.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sRectComponent.content = contentRect;

            var body = CreateText("BodyText", content.transform, "Текст записки...", 20, TextAnchor.UpperLeft, FontStyle.Normal);
            body.color = UIStyleConfig.TextPrimary;
            body.lineSpacing = 1.25f;

            var scrollbarGo = new GameObject("Scrollbar Vertical", typeof(RectTransform), typeof(Scrollbar), typeof(Image));
            scrollbarGo.transform.SetParent(scrollView.transform, false);
            var sbRect = (RectTransform)scrollbarGo.transform;
            sbRect.anchorMin = new Vector2(1, 0);
            sbRect.anchorMax = new Vector2(1, 1);
            sbRect.pivot = new Vector2(1, 0.5f);
            sbRect.anchoredPosition = Vector2.zero;
            sbRect.sizeDelta = new Vector2(20, 0);
            scrollbarGo.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.08f, 0.8f);

            var scrollbar = scrollbarGo.GetComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            var slideArea = new GameObject("Sliding Area", typeof(RectTransform));
            slideArea.transform.SetParent(scrollbarGo.transform, false);
            var saRect = (RectTransform)slideArea.transform;
            saRect.anchorMin = Vector2.zero;
            saRect.anchorMax = Vector2.one;
            saRect.offsetMin = new Vector2(2, 2);
            saRect.offsetMax = new Vector2(-2, -2);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(slideArea.transform, false);
            var handleRect = (RectTransform)handle.transform;
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = handleRect.offsetMax = Vector2.zero;
            var handleImg = handle.GetComponent<Image>();
            handleImg.color = UIStyleConfig.AccentAntiqueBronze;

            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImg;
            sRectComponent.verticalScrollbar = scrollbar;
            sRectComponent.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

            var closeBtn = CreateButton("CloseButton", frame.transform, "Закрити", new Vector2(200, 48), new Vector2(0, 36));
            var cbRect = (RectTransform)closeBtn.transform;
            cbRect.anchorMin = cbRect.anchorMax = new Vector2(0.5f, 0);
            cbRect.anchoredPosition = new Vector2(0, 36);

            var noteUI = root.GetComponent<NotePanelUI>();
            return noteUI;
        }

        private CombinationLockUI BuildRuntimeCombinationLock(Transform parent)
        {
            var root = new GameObject("CombinationLock", typeof(RectTransform), typeof(CanvasGroup), typeof(CombinationLockUI));
            root.transform.SetParent(parent, false);
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = rootRect.anchorMax = rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = new Vector2(120, 0);
            rootRect.sizeDelta = new Vector2(560, 620);

            var backdrop = new GameObject("ModalBackdrop", typeof(RectTransform), typeof(Image));
            backdrop.transform.SetParent(root.transform, false);
            var backdropRect = (RectTransform)backdrop.transform;
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = new Vector2(-1500, -1500);
            backdropRect.offsetMax = new Vector2(1500, 1500);
            var backdropImg = backdrop.GetComponent<Image>();
            backdropImg.color = UIStyleConfig.BackgroundDim;

            var frame = new GameObject("LockFrame", typeof(RectTransform), typeof(Image));
            frame.transform.SetParent(root.transform, false);
            var frameRect = (RectTransform)frame.transform;
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = frameRect.offsetMax = Vector2.zero;
            var frameImg = frame.GetComponent<Image>();
            frameImg.color = UIStyleConfig.PanelSurface;

            var accent = new GameObject("AccentTop", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(frame.transform, false);
            var accentRect = (RectTransform)accent.transform;
            accentRect.anchorMin = new Vector2(0, 1);
            accentRect.anchorMax = new Vector2(1, 1);
            accentRect.pivot = new Vector2(0.5f, 1);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(0, 4);
            accent.GetComponent<Image>().color = UIStyleConfig.AccentAntiqueBronze;

            var title = CreateText("Title", frame.transform, "МЕХАНІЧНИЙ ЗАМОК", 26, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.color = UIStyleConfig.AccentAntiqueBronze;
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -16);
            titleRect.sizeDelta = new Vector2(-40, 40);

            var subtitle = CreateText("Instruction", frame.transform, "Введіть 3 цифри коду (миша або 0-9 / Numpad)", 17, TextAnchor.MiddleCenter, FontStyle.Normal);
            subtitle.color = UIStyleConfig.TextSecondary;
            var subRect = subtitle.rectTransform;
            subRect.anchorMin = new Vector2(0, 1);
            subRect.anchorMax = new Vector2(1, 1);
            subRect.pivot = new Vector2(0.5f, 1);
            subRect.anchoredPosition = new Vector2(0, -56);
            subRect.sizeDelta = new Vector2(-40, 30);

            var slotsRoot = new GameObject("DigitSlotsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            slotsRoot.transform.SetParent(frame.transform, false);
            var slotsRect = (RectTransform)slotsRoot.transform;
            slotsRect.anchorMin = new Vector2(0.5f, 1);
            slotsRect.anchorMax = new Vector2(0.5f, 1);
            slotsRect.pivot = new Vector2(0.5f, 1);
            slotsRect.anchoredPosition = new Vector2(0, -96);
            slotsRect.sizeDelta = new Vector2(320, 80);

            var slotsHlg = slotsRoot.GetComponent<HorizontalLayoutGroup>();
            slotsHlg.spacing = 24;
            slotsHlg.childAlignment = TextAnchor.MiddleCenter;
            slotsHlg.childControlWidth = false;
            slotsHlg.childControlHeight = false;

            var digitTexts = new Text[3];
            var digitBorders = new Image[3];

            for (int i = 0; i < 3; i++)
            {
                var slotBox = new GameObject("Slot_" + i, typeof(RectTransform), typeof(Image));
                slotBox.transform.SetParent(slotsRoot.transform, false);
                var sbRect = (RectTransform)slotBox.transform;
                sbRect.sizeDelta = new Vector2(76, 76);
                var borderImg = slotBox.GetComponent<Image>();
                borderImg.color = UIStyleConfig.PanelBorder;
                digitBorders[i] = borderImg;

                var inner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
                inner.transform.SetParent(slotBox.transform, false);
                var inRect = (RectTransform)inner.transform;
                inRect.anchorMin = Vector2.zero;
                inRect.anchorMax = Vector2.one;
                inRect.offsetMin = inRect.offsetMax = new Vector2(3, 3);
                inner.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.035f, 1f);

                var dText = CreateText("DigitText", inner.transform, "—", 40, TextAnchor.MiddleCenter, FontStyle.Bold);
                dText.color = UIStyleConfig.TextPrimary;
                var dtRect = dText.rectTransform;
                dtRect.anchorMin = Vector2.zero;
                dtRect.anchorMax = Vector2.one;
                dtRect.offsetMin = dtRect.offsetMax = Vector2.zero;
                digitTexts[i] = dText;
            }

            var keypadRoot = new GameObject("KeypadGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            keypadRoot.transform.SetParent(frame.transform, false);
            var kpRect = (RectTransform)keypadRoot.transform;
            kpRect.anchorMin = new Vector2(0.5f, 1);
            kpRect.anchorMax = new Vector2(0.5f, 1);
            kpRect.pivot = new Vector2(0.5f, 1);
            kpRect.anchoredPosition = new Vector2(0, -192);
            kpRect.sizeDelta = new Vector2(340, 260);

            var glg = keypadRoot.GetComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(96, 54);
            glg.spacing = new Vector2(16, 12);
            glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
            glg.startAxis = GridLayoutGroup.Axis.Horizontal;
            glg.childAlignment = TextAnchor.MiddleCenter;

            var lockUI = root.GetComponent<CombinationLockUI>();

            // Buttons 1-9
            for (int n = 1; n <= 9; n++)
            {
                int digit = n;
                var btn = CreateKeypadButton("Btn_" + n, keypadRoot.transform, n.ToString(), 24);
                btn.onClick.AddListener(() => lockUI.AppendDigit(digit));
            }

            var clearBtn = CreateKeypadButton("Btn_Clear", keypadRoot.transform, "C", 22);
            clearBtn.GetComponent<Image>().color = new Color(0.24f, 0.12f, 0.12f, 1f);
            clearBtn.onClick.AddListener(lockUI.ClearDigits);

            var zeroBtn = CreateKeypadButton("Btn_0", keypadRoot.transform, "0", 24);
            zeroBtn.onClick.AddListener(() => lockUI.AppendDigit(0));

            var backspaceBtn = CreateKeypadButton("Btn_Backspace", keypadRoot.transform, "⌫", 22);
            backspaceBtn.GetComponent<Image>().color = new Color(0.20f, 0.16f, 0.14f, 1f);
            backspaceBtn.onClick.AddListener(lockUI.RemoveLastDigit);

            var feedback = CreateText("FeedbackText", frame.transform, "Введіть 3 цифри коду", 18, TextAnchor.MiddleCenter, FontStyle.Normal);
            feedback.color = UIStyleConfig.TextPrimary;
            var fbRect = feedback.rectTransform;
            fbRect.anchorMin = new Vector2(0.5f, 0);
            fbRect.anchorMax = new Vector2(0.5f, 0);
            fbRect.pivot = new Vector2(0.5f, 0);
            fbRect.anchoredPosition = new Vector2(0, 96);
            fbRect.sizeDelta = new Vector2(460, 40);

            var actionRow = new GameObject("ActionRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            actionRow.transform.SetParent(frame.transform, false);
            var arRect = (RectTransform)actionRow.transform;
            arRect.anchorMin = new Vector2(0.5f, 0);
            arRect.anchorMax = new Vector2(0.5f, 0);
            arRect.pivot = new Vector2(0.5f, 0);
            arRect.anchoredPosition = new Vector2(0, 32);
            arRect.sizeDelta = new Vector2(440, 52);

            var arHlg = actionRow.GetComponent<HorizontalLayoutGroup>();
            arHlg.spacing = 20;
            arHlg.childAlignment = TextAnchor.MiddleCenter;
            arHlg.childControlWidth = true;
            arHlg.childControlHeight = true;

            var submitBtn = CreateButton("SubmitButton", actionRow.transform, "Підтвердити", new Vector2(210, 52), Vector2.zero);
            submitBtn.GetComponent<Image>().color = new Color(0.18f, 0.30f, 0.22f, 1f);
            submitBtn.onClick.AddListener(lockUI.Submit);

            var closeBtn = CreateButton("CancelButton", actionRow.transform, "Назад", new Vector2(210, 52), Vector2.zero);
            closeBtn.onClick.AddListener(lockUI.CloseLock);

            return lockUI;
        }

        private Text CreateText(string name, Transform parent, string content, int fontSize, TextAnchor alignment, FontStyle style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = uiFont;
            text.fontSize = fontSize;
            text.text = content;
            text.alignment = alignment;
            text.fontStyle = style;
            text.raycastTarget = false;
            text.supportRichText = true;
            return text;
        }

        private Button CreateKeypadButton(string name, Transform parent, string caption, int fontSize)
        {
            var btn = CreateButton(name, parent, caption, Vector2.zero, Vector2.zero, fontSize);
            return btn.GetComponent<Button>();
        }

        private Button CreateButton(string name, Transform parent, string caption, Vector2 size, Vector2 pos, int fontSize = 20)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            if (size != Vector2.zero) rect.sizeDelta = size;
            if (pos != Vector2.zero) rect.anchoredPosition = pos;

            var img = go.GetComponent<Image>();
            img.color = UIStyleConfig.ButtonNormal;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            UIStyleConfig.ApplyStyle(btn);

            var text = CreateText("Caption", go.transform, caption, fontSize, TextAnchor.MiddleCenter, FontStyle.Normal);
            text.color = UIStyleConfig.TextPrimary;
            var tRect = text.rectTransform;
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = tRect.offsetMax = Vector2.zero;

            return btn;
        }
    }
}
