using UnityEngine;
namespace Echo.Interactions
{
    [ExecuteAlways,RequireComponent(typeof(TextMesh))]
    public sealed class WorldPropTextBinding : MonoBehaviour
    {
        private TextMesh textMesh;
        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock properties;
        private string lastText;
        private void OnEnable(){Font.textureRebuilt+=OnFontRebuilt;Refresh();}
        private void OnDisable(){Font.textureRebuilt-=OnFontRebuilt;}
        private void LateUpdate(){if(textMesh!=null&&lastText!=textMesh.text)Refresh();}
        public void Refresh()
        {
            textMesh=GetComponent<TextMesh>();meshRenderer=GetComponent<MeshRenderer>();
            if(textMesh.font==null)return;
            lastText=textMesh.text;textMesh.font.RequestCharactersInTexture(lastText,textMesh.fontSize,textMesh.fontStyle);BindAtlas();
        }
        private void OnFontRebuilt(Font font){if(textMesh!=null&&font==textMesh.font)BindAtlas();}
        private void BindAtlas()
        {
            if(meshRenderer==null||textMesh.font==null)return;
            if(properties==null)properties=new MaterialPropertyBlock();meshRenderer.GetPropertyBlock(properties);
            properties.SetTexture("_MainTex",textMesh.font.material.mainTexture);properties.SetColor("_Color",textMesh.color);meshRenderer.SetPropertyBlock(properties);
        }
    }
}
