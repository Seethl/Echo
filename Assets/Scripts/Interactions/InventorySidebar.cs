using System.Collections.Generic;
using System.Linq;
using Echo.Manor;
using Echo.Menu;
using Echo.Radio;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Echo.Interactions
{
    // Presentation of PlayerInteractor's existing inventory, never a second
    // item store. Checkpoints remain owned by ManorProgress.
    [DefaultExecutionOrder(-8000)]
    [RequireComponent(typeof(PlayerInteractor))]
    public sealed class InventorySidebar : MonoBehaviour
    {
        public InventoryCatalog catalog;
        [Range(3,7)] public int visibleSlots=6;
        [Range(1,1.25f)] public float displayScale=1.17f;
        private bool arrowHeld;
        public int ItemCount=>items.Count;
        public string SelectedId=>items.Count>0?items[Mathf.Clamp(selected,0,items.Count-1)]:null;
        readonly List<string> items=new List<string>();
        readonly List<Image> rows=new List<Image>(),icons=new List<Image>();
        readonly List<Text> labels=new List<Text>();
        PlayerInteractor player;
        PortableRadio radio;
        GameObject panel;
        Text heading;
        Font font;
        int selected;
        string signature;
        float nextRefresh;

        void Awake()
        {
            player=GetComponent<PlayerInteractor>();
            if(catalog==null)catalog=Resources.Load<InventoryCatalog>("EchoInventory");
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvas=player.ui.GetComponentInChildren<Canvas>();
            panel=new GameObject("Inventory sidebar",typeof(RectTransform));panel.transform.SetParent(canvas.transform,false);
            var rect=(RectTransform)panel.transform;rect.anchorMin=rect.anchorMax=new Vector2(1,.5f);rect.pivot=new Vector2(1,.5f);rect.anchoredPosition=new Vector2(-20,-22);rect.sizeDelta=new Vector2(188,visibleSlots*54+34);rect.localScale=Vector3.one*displayScale;
            heading=Label(panel.transform,"ПРЕДМЕТИ",12,new Vector2(0,visibleSlots*27+14),new Vector2(188,20));heading.color=EchoUIStyle.Gold;
            for(int i=0;i<visibleSlots;i++)
            {
                var row=new GameObject("Inventory slot "+i,typeof(RectTransform),typeof(Image));row.transform.SetParent(panel.transform,false);
                var rr=(RectTransform)row.transform;rr.sizeDelta=new Vector2(188,50);rr.anchoredPosition=new Vector2(0,(visibleSlots-1)*27-i*54);
                var image=row.GetComponent<Image>();image.raycastTarget=false;rows.Add(image);
                var icon=new GameObject("Item icon",typeof(RectTransform),typeof(Image)).GetComponent<Image>();icon.transform.SetParent(row.transform,false);
                icon.rectTransform.sizeDelta=new Vector2(34,34);icon.rectTransform.anchoredPosition=new Vector2(-69,0);icon.preserveAspect=true;icon.raycastTarget=false;icons.Add(icon);
                labels.Add(Label(row.transform,"",14,new Vector2(20,0),new Vector2(132,44)));
            }
            player.InventoryChanged+=Refresh;Refresh();
        }
        Text Label(Transform parent,string text,int size,Vector2 pos,Vector2 dimensions)
        {
            var t=new GameObject("Inventory label",typeof(RectTransform),typeof(Text)).GetComponent<Text>();t.transform.SetParent(parent,false);t.font=font;t.fontSize=size;t.text=text;t.color=EchoUIStyle.Paper;t.alignment=TextAnchor.MiddleLeft;t.raycastTarget=false;
            t.rectTransform.anchoredPosition=pos;t.rectTransform.sizeDelta=dimensions;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;
        }
        void Update()
        {
            if(Time.unscaledTime>=nextRefresh){nextRefresh=Time.unscaledTime+.2f;Refresh();}
            bool active=!GameMenu.BlocksPlayerInput&&Time.timeScale>0&&items.Count>0;panel.SetActive(active);
            var keyboard=Keyboard.current;
            int arrow=keyboard!=null&&(keyboard.leftAltKey.isPressed||keyboard.rightAltKey.isPressed)?(keyboard.upArrowKey.isPressed?-1:keyboard.downArrowKey.isPressed?1:0):0;
            if(!active){arrowHeld=arrow!=0;return;}
            if(arrow!=0&&!arrowHeld)SelectRelative(arrow);arrowHeld=arrow!=0;
            float wheel=Mouse.current!=null?Mouse.current.scroll.ReadValue().y:0;
            if(Mathf.Abs(wheel)>.01f)SelectRelative(wheel>0?-1:1);
        }
        InventoryCatalog.Item Entry(string id)=>catalog!=null?catalog.items.FirstOrDefault(i=>i.id==id):null;
        public void SelectRelative(int direction)
        {
            if(GameMenu.BlocksPlayerInput||Time.timeScale<=0||items.Count==0||direction==0)return;
            selected=(selected+(direction>0?1:-1)+items.Count)%items.Count;Draw();
            var entry=Entry(SelectedId);player.ui.ShowToast(entry!=null?entry.displayName+" — "+entry.hint:"Предмет використовується клавішею E біля відповідного об’єкта.",3);
        }
        bool Available(string id)
        {
            var p=ManorProgress.Active;if(p==null)return true;
            if(id=="tool.winch"&&p.Has("winch.installed"))return false;
            if(id=="remedy.base"&&p.Get("remedy.steps")>=1)return false;
            if(id=="remedy.flower"&&p.Get("remedy.steps")>=2)return false;
            if(id=="remedy.bottle"&&(player.HasKey("remedy.potion")||p.Has("remedy.bottled")))return false;
            return id!="remedy.potion"||!p.Has("remedy.served");
        }
        void Refresh()
        {
            if(player==null)return;
            if(radio==null)radio=FindAnyObjectByType<PortableRadio>();
            var ids=player.GetKeys().Where(Available).ToList();if(radio!=null&&radio.Held)ids.Add("portable.radio");
            ids=ids.Distinct().OrderBy(id=>catalog!=null?System.Array.FindIndex(catalog.items,i=>i.id==id):0).ThenBy(id=>id).ToList();
            string current=string.Join("|",ids);if(signature==current)return;
            string old=SelectedId;string added=ids.FirstOrDefault(id=>!items.Contains(id));signature=current;items.Clear();items.AddRange(ids);
            selected=Mathf.Max(0,items.IndexOf(added??old??""));Draw();
        }
        void Draw()
        {
            int first=Mathf.Clamp(selected-visibleSlots/2,0,Mathf.Max(0,items.Count-visibleSlots));
            heading.text="ПРЕДМЕТИ  "+(items.Count>0?(selected+1)+" / "+items.Count:"");
            for(int i=0;i<rows.Count;i++)
            {
                int index=first+i;rows[i].gameObject.SetActive(index<items.Count);if(index>=items.Count)continue;
                var entry=Entry(items[index]);bool chosen=index==selected;
                rows[i].color=chosen?new Color(.22f,.19f,.13f,.96f):new Color(.025f,.035f,.045f,.88f);
                labels[i].text=entry!=null?entry.displayName:"Знайдений предмет";labels[i].color=chosen?new Color(1,.87f,.6f):EchoUIStyle.Paper;
                icons[i].sprite=entry?.icon;icons[i].enabled=icons[i].sprite!=null;icons[i].color=chosen?new Color(1,.84f,.48f):new Color(.7f,.69f,.6f);
            }
        }
        void OnDestroy(){if(player!=null)player.InventoryChanged-=Refresh;}
    }
}
