using UnityEngine;

namespace Echo.Interactions
{
    // Each view owns its small runtime materials; imported/shared assets stay intact.
    internal static class PropFeedbackVisuals
    {
        internal static Material Material(Color color)
        {
            // The Resources material also retains the shader in player builds.
            var template=Resources.Load<Material>("EchoPropFeedback");
            var material=template!=null?new Material(template):new Material(Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Unlit/Color"));
            SetColor(material,color);
            if(material.HasProperty("_Cull"))material.SetFloat("_Cull",0);
            return material;
        }
        internal static void SetColor(Material material,Color color)
        {
            if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",color);
            if(material.HasProperty("_Color"))material.SetColor("_Color",color);
        }
        internal static LineRenderer Line(Transform parent,string name,Material material,float width,params Vector3[] points)
        {
            var line=new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(parent,false);line.useWorldSpace=false;line.sharedMaterial=material;
            line.widthMultiplier=width;line.positionCount=points.Length;line.SetPositions(points);
            line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;
            return line;
        }
    }
}
