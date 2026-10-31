using Echo.Menu;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace Echo.Interactions
{
    [DefaultExecutionOrder(-9500)]
    public sealed class InteractionUI : MonoBehaviour
    {
        private Canvas canvas;
        private Text prompt;
        private Text toast;
        private Text noteText;
        private GameObject dot;
        private GameObject shade;
        private GameObject notePanel;
        private GameObject finishPanel;
        private Button closeButton;
        private Button repeatButton;
        private Font font;
        private float toastUntil;
        private GameObject customPanel;
        private Text customTitle,customBody,hud;
        private Text completionTitle;
        private Text subtitle;
        public string CurrentSubtitle=>subtitle!=null?subtitle.text:"";
        public void SetSubtitle(string value){if(subtitle!=null)subtitle.text=value;}
        private readonly List<Button> customButtons=new List<Button>();
        private UnityEngine.Object customOwner;
        private Func<string> liveBody;
        private Action onCustomClose;
        public bool IsCustomOpen(UnityEngine.Object owner) => customPanel!=null && customPanel.activeSelf && customOwner==owner;
        public void SetHUD(string value) { if(hud!=null)hud.text=value; }
        public sealed class Choice
        {
            public string Caption; public Action Action;
            public Choice(string caption,Action action){Caption=caption;Action=action;}
        }
        public bool ReadingNote => notePanel != null && notePanel.activeSelf;
        public bool Completed => finishPanel != null && finishPanel.activeSelf;
        public string CurrentPrompt => prompt != null ? prompt.text : "";
        public string CurrentToast => toast != null ? toast.text : "";

        private void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var root = new GameObject("Interaction Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            dot = Label("Aim Dot", root.transform, "•", 24, 0, 30).gameObject;
            prompt = Label("Prompt", root.transform, "", 23, -65, 45);
            toast = Label("Toast", root.transform, "", 24, 235, 65);
            Label("Controls", root.transform, "WASD — рух · Ctrl — тихо · E — взаємодія · R — радіо · F — живлення · Q — поставити · J — підказки · Escape — назад/пауза", 15, -320, 38);
            hud=Label("Objective and Noise",root.transform,"",19,285,100);
            subtitle=Label("Radio Subtitles",root.transform,"",20,-210,105);
            subtitle.rectTransform.sizeDelta=new Vector2(900,105);

            shade = new GameObject("Modal Shade", typeof(RectTransform), typeof(Image));
            shade.transform.SetParent(root.transform, false);
            var shadeRect = (RectTransform)shade.transform;
            shadeRect.anchorMin = Vector2.zero; shadeRect.anchorMax = Vector2.one;
            shadeRect.offsetMin = shadeRect.offsetMax = Vector2.zero;
            shade.GetComponent<Image>().color = new Color(0.015f, 0.025f, 0.035f, 0.96f);
            notePanel = Panel("Note Panel");
            Label("Title", notePanel.transform, "ЗАПИСКА ДОГЛЯДАЧА", 30, 110, 50);
            noteText = Label("Note Text", notePanel.transform, "", 25, 15, 140);
            closeButton = Button(notePanel.transform, "Закрити", -115, CloseNote);
            finishPanel = Panel("Completion Panel");
            completionTitle=Label("Title", finishPanel.transform, "Шлях відкрито", 42, 100, 70);
            repeatButton = Button(finishPanel.transform, "Повторити", -5, () => GameMenu.RestartGameplay());
            Button(finishPanel.transform, "Головне меню", -85, () => GameMenu.ReturnToMainMenu());
            customPanel=Panel("Custom Panel");
            customTitle=Label("Title",customPanel.transform,"",29,200,55);
            customBody=Label("Body",customPanel.transform,"",22,80,160);
            customPanel.SetActive(false);
            shade.SetActive(false); notePanel.SetActive(false); finishPanel.SetActive(false);
        }

        private void Update()
        {
            bool blocked = GameMenu.BlocksPlayerInput;
            dot.SetActive(!blocked);
            if (blocked) prompt.text = "";
            if (Time.unscaledTime >= toastUntil) toast.text = "";
            if(customPanel.activeSelf && liveBody!=null)customBody.text=liveBody();
        }

        public bool OpenPanel(UnityEngine.Object owner,string title,Func<string> body,IEnumerable<Choice> choices,bool freezeWorld=true,Action closed=null,bool replace=false,bool canEscape=true)
        {
            if(!GameMenu.TryOpenModal(this,canEscape?ClosePanel:(Action)null,freezeWorld,replace))return false;
            onCustomClose?.Invoke();onCustomClose=closed;
            notePanel.SetActive(false);finishPanel.SetActive(false);
            foreach(var button in customButtons){button.gameObject.SetActive(false);Destroy(button.gameObject);}customButtons.Clear();
            customOwner=owner;customTitle.text=title;liveBody=body;customBody.text=body!=null?body():"";
            shade.SetActive(true);customPanel.SetActive(true);
            int index=0;
            foreach(var choice in choices)
            {
                var action=choice.Action;var button=Button(customPanel.transform,choice.Caption,-35-index*54,()=>action?.Invoke());
                customButtons.Add(button);index++;
            }
            if(customButtons.Count>0 && EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(customButtons[0].gameObject);
            return true;
        }
        public void ClosePanel()
        {
            if(customPanel==null||!customPanel.activeSelf)return;
            customPanel.SetActive(false);shade.SetActive(false);customOwner=null;liveBody=null;
            var closed=onCustomClose;onCustomClose=null;closed?.Invoke();GameMenu.CloseModal(this);
        }

        public void SetPrompt(string value) => prompt.text = value ?? "";
        public void ShowToast(string value)
        {
            toast.text = value;
            toastUntil = Time.unscaledTime + 2.5f;
        }

        public void ShowNote(string content)
        {
            if (!GameMenu.TryOpenModal(this, CloseNote)) return;
            noteText.text = content;
            shade.SetActive(true); notePanel.SetActive(true);
            EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
        }

        public void CloseNote()
        {
            if (!ReadingNote) return;
            notePanel.SetActive(false); shade.SetActive(false);
            GameMenu.CloseModal(this);
        }

        public bool ShowCompletion(string message="Шлях відкрито")
        {
            if (!GameMenu.TryOpenModal(this, null)) return false;
            completionTitle.text=message;
            shade.SetActive(true); finishPanel.SetActive(true);
            EventSystem.current.SetSelectedGameObject(repeatButton.gameObject);
            return true;
        }

        private void OnDestroy() => GameMenu.CloseModal(this);

        private GameObject Panel(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            Place((RectTransform)go.transform, 0, 0, 700, 360);
            return go;
        }

        private Text Label(string name, Transform parent, string content, int size, float y, float height)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            Place(text.rectTransform, 0, y, 650, height);
            text.font = font; text.fontSize = size; text.text = content;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.9f, 0.9f, 0.84f);
            text.raycastTarget = false;
            return text;
        }

        private Button Button(Transform parent, string caption, float y, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, 0, y, 400, 55);
            go.GetComponent<Image>().color = new Color(0.17f, 0.23f, 0.22f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            var label = Label("Label", go.transform, caption, 25, 0, 55);
            label.rectTransform.sizeDelta = new Vector2(400, 55);
            button.onClick.AddListener(action);
            return button;
        }

        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
