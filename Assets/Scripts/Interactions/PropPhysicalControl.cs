using UnityEngine;
namespace Echo.Interactions
{
    public sealed class PropPhysicalControl : MonoBehaviour
    {
        public PropCloseupView owner;
        public int controlId;
        public bool Highlighted {get;private set;}
        private Renderer surface;
        private MaterialPropertyBlock original,highlight;
        private Material[] sourceMaterials,hoverMaterials;
        public void SetHighlighted(bool value)
        {
            if(Highlighted==value)return;
            if(surface==null)surface=GetComponent<Renderer>();
            if(surface==null)return;
            if(value)
            {
                if(original==null){original=new MaterialPropertyBlock();highlight=new MaterialPropertyBlock();}
                surface.GetPropertyBlock(original);surface.GetPropertyBlock(highlight);
                sourceMaterials=surface.sharedMaterials;
                if(hoverMaterials==null)
                {
                    // EchoPropHover in Resources retains the lit emission shader
                    // variant for these temporary materials in player builds.
                    hoverMaterials=new Material[sourceMaterials.Length];
                    for(int i=0;i<sourceMaterials.Length;i++)
                    {
                        if(sourceMaterials[i]==null)continue;
                        hoverMaterials[i]=new Material(sourceMaterials[i]);
                        hoverMaterials[i].EnableKeyword("_EMISSION");
                    }
                }
                var material=surface.sharedMaterial;
                Color color=highlight.HasColor("_BaseColor")?highlight.GetColor("_BaseColor"):
                    material!=null&&material.HasProperty("_BaseColor")?material.GetColor("_BaseColor"):Color.gray;
                color=Color.Lerp(color,new Color(1,.72f,.3f),.55f);
                highlight.SetColor("_BaseColor",color);highlight.SetColor("_Color",color);
                highlight.SetColor("_EmissionColor",new Color(.22f,.14f,.045f));
                surface.sharedMaterials=hoverMaterials;
                surface.SetPropertyBlock(highlight);
            }
            else {surface.sharedMaterials=sourceMaterials;surface.SetPropertyBlock(original);}
            Highlighted=value;
        }
        private void OnDisable(){SetHighlighted(false);}
        private void OnDestroy()
        {
            SetHighlighted(false);
            if(hoverMaterials!=null)foreach(var material in hoverMaterials)if(material!=null)Destroy(material);
        }
    }
}
