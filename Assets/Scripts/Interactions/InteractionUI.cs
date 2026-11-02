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
        public string controlsText = "Коліщатко / Alt+↑↓ — предмети · WASD / стрілки — рух · Ctrl — тихо · E — дія · R — радіо · F — живлення · Q — поставити · J — журнал / допомога · Escape — назад/пауза";
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
        private RawImage panelIllustration;
        private Text completionTitle;
        private Text subtitle;
        private Image promptPlate,subtitlePlate,toastPlate;
        private ScrollRect readingScroll;
        private RectTransform readingViewport;
        private string laidOutBody;

        private GameObject objectiveCard;
        private GameObject noisePanel;
        private RectTransform playerNoiseFill,ambientNoiseFill;
        private Text playerNoiseLabel,ambientNoiseLabel;
        public float PlayerNoiseValue {get;private set;}
        public float AmbientNoiseValue {get;private set;}
        public void SetNoise(float own,float ambient,string ownSource,string ambientSource)
        {
            PlayerNoiseValue=Mathf.Clamp01(own);AmbientNoiseValue=Mathf.Clamp01(ambient);
            if(playerNoiseFill==null)return;
            playerNoiseFill.anchorMax=new Vector2(PlayerNoiseValue,1);ambientNoiseFill.anchorMax=new Vector2(AmbientNoiseValue,1);
            playerNoiseLabel.text="Твій шум · "+ownSource;
            ambientNoiseLabel.text="Шум оточення · "+ambientSource;
        }
        public string CurrentSubtitle=>subtitle!=null?subtitle.text:"";
        public void SetSubtitle(string value){if(subtitle!=null)subtitle.text=value;}
        private readonly List<Button> customButtons=new List<Button>();
        private UnityEngine.Object customOwner;
        private Func<string> liveBody;
        private Action onCustomClose;
        public bool IsCustomOpen(UnityEngine.Object owner) => customPanel!=null && customPanel.activeSelf && customOwner==owner;
        public void SetHUD(string value) { if(hud!=null)hud.text=(value??"").Replace("Ціль: ",""); }
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
            prompt = Label("Prompt", root.transform, "", 20, 0, 36);
            Bottom(prompt.rectTransform,142,720,36);promptPlate=EchoUIStyle.Plate("Context plate",root.transform,prompt.rectTransform);
            toast = Label("Toast", root.transform, "", 19, 160, 64);
            toastPlate=EchoUIStyle.Plate("Notice plate",root.transform,toast.rectTransform);
            // Controls remain available through J; no permanent tutorial ribbon.
            hud=Label("Objective and Noise",root.transform,"",18,0,54);
            objectiveCard=new GameObject("Objective Card",typeof(RectTransform),typeof(Image));objectiveCard.transform.SetParent(root.transform,false);
            var objectiveRect=(RectTransform)objectiveCard.transform;objectiveRect.anchorMin=objectiveRect.anchorMax=objectiveRect.pivot=Vector2.one;objectiveRect.anchoredPosition=new Vector2(-20,-18);objectiveRect.sizeDelta=new Vector2(390,94);objectiveCard.GetComponent<Image>().color=new Color(.025f,.037f,.048f,.90f);objectiveCard.GetComponent<Image>().raycastTarget=false;
            hud.transform.SetParent(objectiveCard.transform,false);hud.rectTransform.anchorMin=hud.rectTransform.anchorMax=hud.rectTransform.pivot=new Vector2(0,1);hud.rectTransform.anchoredPosition=new Vector2(18,-33);hud.rectTransform.sizeDelta=new Vector2(352,54);hud.alignment=TextAnchor.UpperLeft;
            var caption=Label("Objective heading",objectiveCard.transform,"ПОТОЧНА МЕТА",11,0,20);caption.color=EchoUIStyle.Gold;caption.alignment=TextAnchor.UpperLeft;caption.rectTransform.anchorMin=caption.rectTransform.anchorMax=caption.rectTransform.pivot=new Vector2(0,1);caption.rectTransform.anchoredPosition=new Vector2(18,-12);caption.rectTransform.sizeDelta=new Vector2(350,20);
            var rule=new GameObject("Objective accent",typeof(RectTransform),typeof(Image));rule.transform.SetParent(objectiveCard.transform,false);var ruleRect=(RectTransform)rule.transform;ruleRect.anchorMin=new Vector2(0,0);ruleRect.anchorMax=new Vector2(0,1);ruleRect.offsetMin=new Vector2(0,10);ruleRect.offsetMax=new Vector2(2,-10);rule.GetComponent<Image>().color=EchoUIStyle.Gold;rule.GetComponent<Image>().raycastTarget=false;
            subtitle=Label("Radio Subtitles",root.transform,"",20,0,92);
            Bottom(subtitle.rectTransform,65,900,92);subtitlePlate=EchoUIStyle.Plate("Transmission plate",root.transform,subtitle.rectTransform,true);
            noisePanel=new GameObject("Noise Bars",typeof(RectTransform),typeof(Image));noisePanel.transform.SetParent(root.transform,false);
            var nr=(RectTransform)noisePanel.transform;nr.anchorMin=nr.anchorMax=new Vector2(0,1);nr.pivot=new Vector2(0,1);nr.anchoredPosition=new Vector2(16,-16);nr.sizeDelta=new Vector2(254,102);
            nr.localScale=Vector3.one*1.25f;
            noisePanel.GetComponent<Image>().color=new Color(.025f,.04f,.06f,.94f);noisePanel.GetComponent<Image>().raycastTarget=false;
            playerNoiseFill=NoiseBar(noisePanel.transform,24,new Color(.95f,.72f,.35f),out playerNoiseLabel);
            ambientNoiseFill=NoiseBar(noisePanel.transform,-20,new Color(.35f,.78f,.8f),out ambientNoiseLabel);
            SetNoise(0,0,"Тихо","немає");

            shade = new GameObject("Modal Shade", typeof(RectTransform), typeof(Image));
            shade.transform.SetParent(root.transform, false);
            var shadeRect = (RectTransform)shade.transform;
            shadeRect.anchorMin = Vector2.zero; shadeRect.anchorMax = Vector2.one;
            shadeRect.offsetMin = shadeRect.offsetMax = Vector2.zero;
            shade.GetComponent<Image>().color = new Color(.012f,.018f,.032f,.91f);
            notePanel = Panel("Note Panel");
            Label("Title", notePanel.transform, "ЗАПИСКА", 30, 110, 50);
            noteText = Label("Note Text", notePanel.transform, "", 25, 15, 140);
            closeButton = Button(notePanel.transform, "Закрити", -115, CloseNote);
            finishPanel = Panel("Completion Panel");
            completionTitle=Label("Title", finishPanel.transform, "Шлях відкрито", 42, 100, 70);
            repeatButton = Button(finishPanel.transform, "Повторити", -5, () => GameMenu.RestartGameplay());
            Button(finishPanel.transform, "Головне меню", -85, () => GameMenu.ReturnToMainMenu());
            customPanel=Panel("Custom Panel");
            customTitle=Label("Title",customPanel.transform,"",29,200,55);
            customBody=Label("Body",customPanel.transform,"",20,80,160);
            var viewport=new GameObject("Reading viewport",typeof(RectTransform),typeof(Image),typeof(RectMask2D),typeof(ScrollRect));viewport.transform.SetParent(customPanel.transform,false);
            readingViewport=(RectTransform)viewport.transform;Place(readingViewport,0,80,650,160);viewport.GetComponent<Image>().color=Color.clear;
            readingScroll=viewport.GetComponent<ScrollRect>();readingScroll.horizontal=false;readingScroll.movementType=ScrollRect.MovementType.Clamped;readingScroll.scrollSensitivity=24;readingScroll.viewport=readingViewport;
            customBody.transform.SetParent(viewport.transform,false);readingScroll.content=customBody.rectTransform;
            customPanel.SetActive(false);
            shade.SetActive(false); notePanel.SetActive(false); finishPanel.SetActive(false);
        }

        private void Update()
        {
            bool blocked = GameMenu.BlocksPlayerInput;
            dot.SetActive(!blocked);
            noisePanel.SetActive(!blocked);
            if (blocked) prompt.text = "";
            if (Time.unscaledTime >= toastUntil) toast.text = "";
            if(customPanel.activeSelf && liveBody!=null)customBody.text=liveBody();
            if(customPanel.activeSelf&&laidOutBody!=customBody.text)LayoutReading();
            objectiveCard.SetActive(!blocked&&!string.IsNullOrWhiteSpace(hud.text));
            promptPlate.gameObject.SetActive(!blocked&&!string.IsNullOrWhiteSpace(prompt.text));
            toastPlate.gameObject.SetActive(!(PropCloseupView.Active!=null&&PropCloseupView.Active.UsesFooter)&&!GameMenu.IsPaused&&!ReadingNote&&!Completed&&!customPanel.activeSelf&&!string.IsNullOrWhiteSpace(toast.text));
            toast.gameObject.SetActive(toastPlate.gameObject.activeSelf);
            float subtitleY=PropCloseupView.Active!=null&&PropCloseupView.Active.UsesFooter?119:65;
            subtitle.rectTransform.anchoredPosition=new Vector2(0,subtitleY);subtitlePlate.rectTransform.anchoredPosition=new Vector2(0,subtitleY);
            bool transmission=!GameMenu.IsPaused&&!ReadingNote&&!Completed&&!customPanel.activeSelf&&!string.IsNullOrWhiteSpace(subtitle.text);
            subtitle.gameObject.SetActive(transmission);subtitlePlate.gameObject.SetActive(transmission);
        }

        public bool OpenPanel(UnityEngine.Object owner,string title,Func<string> body,IEnumerable<Choice> choices,bool freezeWorld=true,Action closed=null,bool replace=false,bool canEscape=true,Texture illustration=null,bool paper=false)
        {
            if(!GameMenu.TryOpenModal(this,canEscape?ClosePanel:(Action)null,freezeWorld,replace))return false;
            onCustomClose?.Invoke();onCustomClose=closed;
            notePanel.SetActive(false);finishPanel.SetActive(false);
            foreach(var button in customButtons){button.gameObject.SetActive(false);Destroy(button.gameObject);}customButtons.Clear();
            customOwner=owner;customTitle.text=title;liveBody=body;customBody.text=body!=null?body():"";
            customBody.fontSize=paper?27:illustration!=null?18:20;
            Place(customTitle.rectTransform,0,paper?240:200,650,55);
            Place(readingViewport,0,illustration!=null?(paper?-135:-110):paper?35:80,650,illustration!=null?(paper?145:110):paper?320:160);
            laidOutBody=null;LayoutReading();readingScroll.verticalNormalizedPosition=1;
            if(panelIllustration==null)
            {
                panelIllustration=new GameObject("Clue Illustration",typeof(RectTransform),typeof(RawImage)).GetComponent<RawImage>();
                panelIllustration.transform.SetParent(customPanel.transform,false);panelIllustration.raycastTarget=false;
                Place(panelIllustration.rectTransform,0,60,400,220);
            }
            panelIllustration.texture=illustration;panelIllustration.gameObject.SetActive(illustration!=null);
            shade.SetActive(true);customPanel.SetActive(true);
            int index=0;var options=new List<Choice>(choices);bool twoColumns=options.Count>5;
            foreach(var choice in options)
            {
                var action=choice.Action;var button=Button(customPanel.transform,choice.Caption,(paper?-258:illustration!=null?-205:-35)-(twoColumns?index/2:index)*48,()=>action?.Invoke());
                if(twoColumns)
                {
                    var rect=(RectTransform)button.transform;rect.anchoredPosition=new Vector2(index%2==0?-175:175,rect.anchoredPosition.y);rect.sizeDelta=new Vector2(330,44);
                    var caption=button.GetComponentInChildren<Text>();caption.rectTransform.sizeDelta=new Vector2(326,44);caption.fontSize=20;
                }
                customButtons.Add(button);index++;
            }
            DocumentStyle(customPanel,paper);LayoutReading();readingScroll.verticalNormalizedPosition=1;
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
        public void ShowToast(string value,float seconds=2.5f)
        {
            toast.text = value;
            toastUntil = Time.unscaledTime + seconds;
        }

        public void ShowNote(string content)
        {
            if (!GameMenu.TryOpenModal(this, CloseNote)) return;
            DocumentStyle(notePanel,true);
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
            Place((RectTransform)go.transform, 0, 0, 740, name=="Custom Panel"?640:400);
            var surface=go.AddComponent<Image>();surface.color=EchoUIStyle.Ink;
            var line=new GameObject("Panel rule",typeof(RectTransform),typeof(Image));line.transform.SetParent(go.transform,false);Place((RectTransform)line.transform,0,name=="Custom Panel"?285:170,60,1);line.GetComponent<Image>().color=EchoUIStyle.Gold;line.GetComponent<Image>().raycastTarget=false;
            return go;
        }

        private RectTransform NoiseBar(Transform parent,float y,Color color,out Text caption)
        {
            caption=Label("Noise Caption",parent,"",13,y+10,22);caption.rectTransform.sizeDelta=new Vector2(224,22);caption.alignment=TextAnchor.MiddleLeft;
            var back=new GameObject("Noise Track",typeof(RectTransform),typeof(Image));back.transform.SetParent(parent,false);
            Place((RectTransform)back.transform,0,y-10,224,5);back.GetComponent<Image>().color=new Color(.15f,.17f,.21f);back.GetComponent<Image>().raycastTarget=false;
            var fill=new GameObject("Noise Fill",typeof(RectTransform),typeof(Image));fill.transform.SetParent(back.transform,false);
            var rect=(RectTransform)fill.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=new Vector2(0,1);rect.offsetMin=rect.offsetMax=Vector2.zero;
            fill.GetComponent<Image>().color=color;fill.GetComponent<Image>().raycastTarget=false;return rect;
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
            EchoUIStyle.Text(text);
            return text;
        }

        private Button Button(Transform parent, string caption, float y, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, 0, y, 400, 44);
            go.GetComponent<Image>().color = new Color(.12f,.14f,.19f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            var label = Label("Label", go.transform, caption, 21, 0, 44);
            label.rectTransform.sizeDelta = new Vector2(400, 44);
            button.onClick.AddListener(action);
            return button;
        }

                private void DocumentStyle(GameObject surface,bool paper)
        {
            var image=surface.GetComponent<Image>();
            image.sprite=paper?Resources.Load<Sprite>("EchoWritingBoard"):null;
            image.type=paper?Image.Type.Sliced:Image.Type.Simple;
            image.color=paper?Color.white:EchoUIStyle.Ink;
            shade.GetComponent<Image>().color=new Color(.012f,.018f,.032f,paper?.62f:.91f);
            foreach(var text in surface.GetComponentsInChildren<Text>(true))
            {
                text.color=paper?new Color(.08f,.055f,.025f):EchoUIStyle.Paper;
                text.lineSpacing=paper?1.1f:1.15f;
                if(text==customBody||text==noteText){text.font=paper?(Resources.Load<Font>("EchoHandwriting")??font):font;if(text==noteText)text.fontSize=paper?27:25;}
                var shadow=text.GetComponent<Shadow>();if(shadow!=null)shadow.enabled=!paper;
            }
            foreach(var button in surface.GetComponentsInChildren<Button>(true))
                button.GetComponent<Image>().color=paper?new Color(.77f,.66f,.46f):new Color(.12f,.14f,.19f);
        }

        private void LayoutReading()
        {
            laidOutBody=customBody.text;
            var rect=customBody.rectTransform;rect.anchorMin=new Vector2(0,1);rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,1);
            rect.sizeDelta=new Vector2(-12,0);rect.sizeDelta=new Vector2(-12,Mathf.Max(readingViewport.rect.height,customBody.preferredHeight+8));rect.anchoredPosition=new Vector2(0,0);
            customBody.alignment=TextAnchor.UpperLeft;
        }
        private static void Bottom(RectTransform rect,float y,float width,float height)
        {
            rect.anchorMin=rect.anchorMax=new Vector2(.5f,0);rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(0,y);rect.sizeDelta=new Vector2(width,height);
        }
        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
