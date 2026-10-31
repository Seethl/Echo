#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Echo.UI.Editor
{
    public static class UIDemoBuilder
    {
        private const string PrefabsDir = "Assets/UI/Prefabs";
        private const string DemoSceneDir = "Assets/Scenes/Testing/UIDemo";
        private const string DemoScenePath = "Assets/Scenes/Testing/UIDemo/UIDemo.unity";
        private const string NotePanelPrefabPath = "Assets/UI/Prefabs/NotePanel.prefab";
        private const string CombinationLockPrefabPath = "Assets/UI/Prefabs/CombinationLock.prefab";

        private static Font defaultFont;

        private static Font GetDefaultFont()
        {
            if (defaultFont == null)
            {
                defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (defaultFont == null)
                {
                    defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
            }
            return defaultFont;
        }

        [MenuItem("Echo/UI/Build Demo Scene and Prefabs")]
        public static void BuildAll()
        {
            EnsureDirectories();
            var notePrefab = BuildNotePanelPrefab();
            var lockPrefab = BuildCombinationLockPrefab();
            BuildDemoScene(notePrefab, lockPrefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green>[Echo UI]</color> Demo scene and prefabs built successfully!");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(PrefabsDir)) Directory.CreateDirectory(PrefabsDir);
            if (!Directory.Exists(DemoSceneDir)) Directory.CreateDirectory(DemoSceneDir);
            AssetDatabase.Refresh();
        }

        public static GameObject BuildNotePanelPrefab()
        {
            var root = new GameObject("NotePanel", typeof(RectTransform), typeof(CanvasGroup), typeof(NotePanelUI));
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = rootRect.anchorMax = rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = new Vector2(740, 500);

            // Modal Dimmer Backdrop
            var backdrop = new GameObject("ModalBackdrop", typeof(RectTransform), typeof(Image));
            backdrop.transform.SetParent(root.transform, false);
            var backdropRect = (RectTransform)backdrop.transform;
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = new Vector2(-1500, -1500);
            backdropRect.offsetMax = new Vector2(1500, 1500);
            var backdropImg = backdrop.GetComponent<Image>();
            backdropImg.color = UIStyleConfig.BackgroundDim;

            // Panel Background Frame
            var frame = new GameObject("PanelFrame", typeof(RectTransform), typeof(Image));
            frame.transform.SetParent(root.transform, false);
            var frameRect = (RectTransform)frame.transform;
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = frameRect.offsetMax = Vector2.zero;
            var frameImg = frame.GetComponent<Image>();
            frameImg.color = UIStyleConfig.PanelSurface;

            // Accent Top Line
            var accent = new GameObject("AccentTop", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(frame.transform, false);
            var accentRect = (RectTransform)accent.transform;
            accentRect.anchorMin = new Vector2(0, 1);
            accentRect.anchorMax = new Vector2(1, 1);
            accentRect.pivot = new Vector2(0.5f, 1);
            accentRect.anchoredPosition = new Vector2(0, 0);
            accentRect.sizeDelta = new Vector2(0, 4);
            accent.GetComponent<Image>().color = UIStyleConfig.AccentAntiqueBronze;

            // Header Title
            var title = CreateText("NoteTitle", frame.transform, "ЗАПИСКА ДОГЛЯДАЧА", 26, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.color = UIStyleConfig.AccentAntiqueBronze;
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -16);
            titleRect.sizeDelta = new Vector2(-60, 48);

            // Scroll View Container
            var scrollView = new GameObject("Scroll View", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollView.transform.SetParent(frame.transform, false);
            var scrollRect = (RectTransform)scrollView.transform;
            scrollRect.anchorMin = new Vector2(0, 0);
            scrollRect.anchorMax = new Vector2(1, 1);
            scrollRect.offsetMin = new Vector2(24, 76);
            scrollRect.offsetMax = new Vector2(-24, -68);
            var scrollBg = scrollView.GetComponent<Image>();
            scrollBg.color = new Color(0.02f, 0.03f, 0.035f, 0.6f);

            var sRectComponent = scrollView.GetComponent<ScrollRect>();
            sRectComponent.horizontal = false;
            sRectComponent.vertical = true;
            sRectComponent.movementType = ScrollRect.MovementType.Clamped;
            sRectComponent.scrollSensitivity = 25f;

            // Viewport
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
            viewport.transform.SetParent(scrollView.transform, false);
            var viewportRect = (RectTransform)viewport.transform;
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(8, 8);
            viewportRect.offsetMax = new Vector2(-28, -8);
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            var vpImg = viewport.GetComponent<Image>();
            vpImg.color = Color.white;
            sRectComponent.viewport = viewportRect;

            // Content
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

            // Body Text
            var body = CreateText("BodyText", content.transform, "Текст записки...", 20, TextAnchor.UpperLeft, FontStyle.Normal);
            body.color = UIStyleConfig.TextPrimary;
            body.lineSpacing = 1.25f;

            // Vertical Scrollbar
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

            // Close Button
            var closeBtn = CreateButton("CloseButton", frame.transform, "Закрити", new Vector2(200, 48), new Vector2(0, -220));
            var cbRect = (RectTransform)closeBtn.transform;
            cbRect.anchorMin = cbRect.anchorMax = new Vector2(0.5f, 0);
            cbRect.anchoredPosition = new Vector2(0, 36);

            // Bind to NotePanelUI
            var noteUI = root.GetComponent<NotePanelUI>();
            var so = new SerializedObject(noteUI);
            so.FindProperty("panelRoot").objectReferenceValue = root;
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("bodyText").objectReferenceValue = body;
            so.FindProperty("scrollRect").objectReferenceValue = sRectComponent;
            so.FindProperty("verticalScrollbar").objectReferenceValue = scrollbar;
            so.FindProperty("closeButton").objectReferenceValue = closeBtn.GetComponent<Button>();
            so.FindProperty("modalBackdrop").objectReferenceValue = backdropImg;
            so.ApplyModifiedProperties();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, NotePanelPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        public static GameObject BuildCombinationLockPrefab()
        {
            var root = new GameObject("CombinationLock", typeof(RectTransform), typeof(CanvasGroup), typeof(CombinationLockUI));
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = rootRect.anchorMax = rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = new Vector2(560, 620);

            // Modal Dimmer Backdrop
            var backdrop = new GameObject("ModalBackdrop", typeof(RectTransform), typeof(Image));
            backdrop.transform.SetParent(root.transform, false);
            var backdropRect = (RectTransform)backdrop.transform;
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = new Vector2(-1500, -1500);
            backdropRect.offsetMax = new Vector2(1500, 1500);
            var backdropImg = backdrop.GetComponent<Image>();
            backdropImg.color = UIStyleConfig.BackgroundDim;

            // Panel Frame
            var frame = new GameObject("LockFrame", typeof(RectTransform), typeof(Image));
            frame.transform.SetParent(root.transform, false);
            var frameRect = (RectTransform)frame.transform;
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = frameRect.offsetMax = Vector2.zero;
            var frameImg = frame.GetComponent<Image>();
            frameImg.color = UIStyleConfig.PanelSurface;

            // Accent Top Line
            var accent = new GameObject("AccentTop", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(frame.transform, false);
            var accentRect = (RectTransform)accent.transform;
            accentRect.anchorMin = new Vector2(0, 1);
            accentRect.anchorMax = new Vector2(1, 1);
            accentRect.pivot = new Vector2(0.5f, 1);
            accentRect.anchoredPosition = new Vector2(0, 0);
            accentRect.sizeDelta = new Vector2(0, 4);
            accent.GetComponent<Image>().color = UIStyleConfig.AccentAntiqueBronze;

            // Title
            var title = CreateText("Title", frame.transform, "МЕХАНІЧНИЙ ЗАМОК", 26, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.color = UIStyleConfig.AccentAntiqueBronze;
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -16);
            titleRect.sizeDelta = new Vector2(-40, 40);

            // Instruction / Subtitle
            var subtitle = CreateText("Instruction", frame.transform, "Введіть 3 цифри коду (миша або 0-9 / Numpad)", 17, TextAnchor.MiddleCenter, FontStyle.Normal);
            subtitle.color = UIStyleConfig.TextSecondary;
            var subRect = subtitle.rectTransform;
            subRect.anchorMin = new Vector2(0, 1);
            subRect.anchorMax = new Vector2(1, 1);
            subRect.pivot = new Vector2(0.5f, 1);
            subRect.anchoredPosition = new Vector2(0, -56);
            subRect.sizeDelta = new Vector2(-40, 30);

            // Digit Slots Container
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

            // Keypad Grid Container (3 columns: 1-9, C, 0, Backspace)
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

            var numberButtons = new Button[10];
            Button clearBtn = null;
            Button backspaceBtn = null;

            // Buttons 1-9
            for (int n = 1; n <= 9; n++)
            {
                var btn = CreateKeypadButton("Btn_" + n, keypadRoot.transform, n.ToString(), 24);
                numberButtons[n] = btn;
            }

            // Row 4: Clear ("C"), 0, Backspace ("⌫")
            clearBtn = CreateKeypadButton("Btn_Clear", keypadRoot.transform, "C", 22);
            clearBtn.GetComponent<Image>().color = new Color(0.24f, 0.12f, 0.12f, 1f);

            var zeroBtn = CreateKeypadButton("Btn_0", keypadRoot.transform, "0", 24);
            numberButtons[0] = zeroBtn;

            backspaceBtn = CreateKeypadButton("Btn_Backspace", keypadRoot.transform, "⌫", 22);
            backspaceBtn.GetComponent<Image>().color = new Color(0.20f, 0.16f, 0.14f, 1f);

            // Feedback Text
            var feedback = CreateText("FeedbackText", frame.transform, "Введіть 3 цифри коду", 18, TextAnchor.MiddleCenter, FontStyle.Normal);
            feedback.color = UIStyleConfig.TextPrimary;
            var fbRect = feedback.rectTransform;
            fbRect.anchorMin = new Vector2(0.5f, 0);
            fbRect.anchorMax = new Vector2(0.5f, 0);
            fbRect.pivot = new Vector2(0.5f, 0);
            fbRect.anchoredPosition = new Vector2(0, 96);
            fbRect.sizeDelta = new Vector2(460, 40);

            // Bottom Action Row
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

            var closeBtn = CreateButton("CancelButton", actionRow.transform, "Назад", new Vector2(210, 52), Vector2.zero);

            // Bind to CombinationLockUI
            var lockUI = root.GetComponent<CombinationLockUI>();
            var so = new SerializedObject(lockUI);
            so.FindProperty("panelRoot").objectReferenceValue = root;
            so.FindProperty("modalBackdrop").objectReferenceValue = backdropImg;
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("instructionText").objectReferenceValue = subtitle;
            so.FindProperty("feedbackText").objectReferenceValue = feedback;

            var digitTextsProp = so.FindProperty("digitSlotTexts");
            digitTextsProp.arraySize = 3;
            for (int i = 0; i < 3; i++) digitTextsProp.GetArrayElementAtIndex(i).objectReferenceValue = digitTexts[i];

            var digitBordersProp = so.FindProperty("digitSlotBorders");
            digitBordersProp.arraySize = 3;
            for (int i = 0; i < 3; i++) digitBordersProp.GetArrayElementAtIndex(i).objectReferenceValue = digitBorders[i];

            var numberButtonsProp = so.FindProperty("numberButtons");
            numberButtonsProp.arraySize = 10;
            for (int i = 0; i < 10; i++) numberButtonsProp.GetArrayElementAtIndex(i).objectReferenceValue = numberButtons[i];

            so.FindProperty("clearButton").objectReferenceValue = clearBtn;
            so.FindProperty("backspaceButton").objectReferenceValue = backspaceBtn;
            so.FindProperty("submitButton").objectReferenceValue = submitBtn.GetComponent<Button>();
            so.FindProperty("closeButton").objectReferenceValue = closeBtn.GetComponent<Button>();
            so.ApplyModifiedProperties();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, CombinationLockPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        public static void BuildDemoScene(GameObject notePrefab, GameObject lockPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            var camGo = new GameObject("UI Camera", typeof(Camera));
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.015f, 0.02f, 0.025f, 1f);
            cam.orthographic = true;

            // Event System
            var eventGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            // Canvas
            var canvasGo = new GameObject("Demo Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // Controller
            var controller = canvasGo.AddComponent<UIDemoController>();

            // Left Navigation Sidebar
            var sidebar = new GameObject("Sidebar", typeof(RectTransform), typeof(Image));
            sidebar.transform.SetParent(canvasGo.transform, false);
            var sbRect = (RectTransform)sidebar.transform;
            sbRect.anchorMin = new Vector2(0, 0);
            sbRect.anchorMax = new Vector2(0, 1);
            sbRect.pivot = new Vector2(0, 0.5f);
            sbRect.anchoredPosition = Vector2.zero;
            sbRect.sizeDelta = new Vector2(380, 0);
            sidebar.GetComponent<Image>().color = new Color(0.025f, 0.035f, 0.04f, 0.98f);

            // Sidebar Header
            var sbTitle = CreateText("HeaderTitle", sidebar.transform, "ECHO HORROR", 32, TextAnchor.MiddleCenter, FontStyle.Bold);
            sbTitle.color = UIStyleConfig.AccentAntiqueBronze;
            var sbtRect = sbTitle.rectTransform;
            sbtRect.anchorMin = new Vector2(0, 1);
            sbtRect.anchorMax = new Vector2(1, 1);
            sbtRect.pivot = new Vector2(0.5f, 1);
            sbtRect.anchoredPosition = new Vector2(0, -30);
            sbtRect.sizeDelta = new Vector2(-40, 40);

            var sbSub = CreateText("HeaderSub", sidebar.transform, "UI/UX QA & Verification Hub", 16, TextAnchor.MiddleCenter, FontStyle.Normal);
            sbSub.color = UIStyleConfig.TextSecondary;
            var sbsRect = sbSub.rectTransform;
            sbsRect.anchorMin = new Vector2(0, 1);
            sbsRect.anchorMax = new Vector2(1, 1);
            sbsRect.pivot = new Vector2(0.5f, 1);
            sbsRect.anchoredPosition = new Vector2(0, -74);
            sbsRect.sizeDelta = new Vector2(-40, 24);

            // Buttons Container
            var btnList = new GameObject("ButtonList", typeof(RectTransform), typeof(VerticalLayoutGroup));
            btnList.transform.SetParent(sidebar.transform, false);
            var blRect = (RectTransform)btnList.transform;
            blRect.anchorMin = new Vector2(0, 1);
            blRect.anchorMax = new Vector2(1, 1);
            blRect.pivot = new Vector2(0.5f, 1);
            blRect.anchoredPosition = new Vector2(0, -120);
            blRect.sizeDelta = new Vector2(-40, 400);

            var vlg = btnList.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 14;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var btnStdNote = CreateButton("Btn_StdNote", btnList.transform, "1. Записка (Звичайна)", new Vector2(300, 50), Vector2.zero);
            var btnLongNote = CreateButton("Btn_LongNote", btnList.transform, "2. Записка (Довгий текст + Scroll)", new Vector2(300, 50), Vector2.zero);
            var btnUkrNote = CreateButton("Btn_UkrNote", btnList.transform, "3. Укр. символи (І, Ї, Є, Ґ, ’)", new Vector2(300, 50), Vector2.zero);
            var btnLock = CreateButton("Btn_Lock", btnList.transform, "4. Кодовий замок (Інтерактив)", new Vector2(300, 50), Vector2.zero);
            btnLock.GetComponent<Image>().color = new Color(0.18f, 0.28f, 0.22f, 1f);

            var btnDisabled = CreateButton("Btn_Disabled", btnList.transform, "5. Стан Disabled (Вимкнено)", new Vector2(300, 50), Vector2.zero);

            // Sidebar Footer: Resolution & Log
            var resText = CreateText("ResolutionText", sidebar.transform, "Роздільна здатність: ...", 16, TextAnchor.MiddleLeft, FontStyle.Normal);
            resText.color = UIStyleConfig.TextSecondary;
            var rtRect = resText.rectTransform;
            rtRect.anchorMin = new Vector2(0, 0);
            rtRect.anchorMax = new Vector2(1, 0);
            rtRect.pivot = new Vector2(0.5f, 0);
            rtRect.anchoredPosition = new Vector2(20, 70);
            rtRect.sizeDelta = new Vector2(-40, 30);

            var logText = CreateText("LogText", sidebar.transform, "Готово до тестування.", 15, TextAnchor.UpperLeft, FontStyle.Italic);
            logText.color = UIStyleConfig.AccentAntiqueBronze;
            var ltRect = logText.rectTransform;
            ltRect.anchorMin = new Vector2(0, 0);
            ltRect.anchorMax = new Vector2(1, 0);
            ltRect.pivot = new Vector2(0.5f, 0);
            ltRect.anchoredPosition = new Vector2(20, 20);
            ltRect.sizeDelta = new Vector2(-40, 45);

            // Instantiate Note Panel in center
            var noteInstance = (GameObject)PrefabUtility.InstantiatePrefab(notePrefab, canvasGo.transform);
            noteInstance.name = "NotePanel";
            noteInstance.SetActive(false);

            // Instantiate Lock in center
            var lockInstance = (GameObject)PrefabUtility.InstantiatePrefab(lockPrefab, canvasGo.transform);
            lockInstance.name = "CombinationLock";
            lockInstance.SetActive(false);

            // Wire Controller
            var so = new SerializedObject(controller);
            so.FindProperty("notePanel").objectReferenceValue = noteInstance.GetComponent<NotePanelUI>();
            so.FindProperty("combinationLock").objectReferenceValue = lockInstance.GetComponent<CombinationLockUI>();
            so.FindProperty("btnShowStandardNote").objectReferenceValue = btnStdNote.GetComponent<Button>();
            so.FindProperty("btnShowLongNote").objectReferenceValue = btnLongNote.GetComponent<Button>();
            so.FindProperty("btnShowUkrainianPangram").objectReferenceValue = btnUkrNote.GetComponent<Button>();
            so.FindProperty("btnShowLock").objectReferenceValue = btnLock.GetComponent<Button>();
            so.FindProperty("btnTestDisabledButton").objectReferenceValue = btnDisabled.GetComponent<Button>();
            so.FindProperty("resolutionInfoText").objectReferenceValue = resText;
            so.FindProperty("logStatusText").objectReferenceValue = logText;
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, DemoScenePath);
        }

        private static Text CreateText(string name, Transform parent, string content, int fontSize, TextAnchor alignment, FontStyle style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = GetDefaultFont();
            text.fontSize = fontSize;
            text.text = content;
            text.alignment = alignment;
            text.fontStyle = style;
            text.raycastTarget = false;
            text.supportRichText = true;
            return text;
        }

        private static Button CreateKeypadButton(string name, Transform parent, string caption, int fontSize)
        {
            var btn = CreateButton(name, parent, caption, Vector2.zero, Vector2.zero, fontSize);
            return btn.GetComponent<Button>();
        }

        private static GameObject CreateButton(string name, Transform parent, string caption, Vector2 size, Vector2 pos, int fontSize = 20)
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

            return go;
        }
    }
}
#endif

