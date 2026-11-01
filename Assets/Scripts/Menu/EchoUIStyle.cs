using UnityEngine;
using UnityEngine.UI;

namespace Echo.Menu
{
    // Shared presentation only; menu/modal ownership stays in the existing components.
    public static class EchoUIStyle
    {
        public static readonly Color Ink=new Color(.035f,.045f,.067f,.96f);
        public static readonly Color Gold=new Color(.72f,.59f,.36f);
        public static readonly Color Paper=new Color(.91f,.89f,.82f);
        public static void Text(Text text)
        {
            text.color=Paper;text.lineSpacing=1.15f;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;
            var shadow=text.GetComponent<Shadow>()??text.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.65f);shadow.effectDistance=new Vector2(0,-1);
        }
        public static Image Plate(string name,Transform parent,RectTransform target,bool accent=false)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=target.anchorMin;rect.anchorMax=target.anchorMax;rect.pivot=target.pivot;rect.anchoredPosition=target.anchoredPosition;rect.sizeDelta=target.sizeDelta+new Vector2(28,12);
            var image=go.GetComponent<Image>();image.color=Ink;image.raycastTarget=false;go.transform.SetSiblingIndex(target.GetSiblingIndex());
            var edge=new GameObject("Gold rule",typeof(RectTransform),typeof(Image));edge.transform.SetParent(go.transform,false);
            var er=(RectTransform)edge.transform;er.anchorMin=new Vector2(0,1);er.anchorMax=Vector2.one;er.pivot=new Vector2(.5f,1);er.offsetMin=new Vector2(0,-1);er.offsetMax=Vector2.zero;
            edge.GetComponent<Image>().color=new Color(Gold.r,Gold.g,Gold.b,accent ? .65f : .25f);edge.GetComponent<Image>().raycastTarget=false;
            return image;
        }
    }
}
