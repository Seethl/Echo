using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ManorSurfacesSetup
{
    private const string Folder = "Assets/Props/ManorSurfaces";

    [MenuItem("Tools/Echo/Build Floor And Ceiling")]
    public static void Build()
    {
        AssetDatabase.Refresh();
        Make("ManorFloor", .12f, .22f, false);
        Make("ManorCeiling", .10f, .08f, true);
        Make("ManorStoneFloor", .12f, .10f, false);
        Make("ManorParquetFloor", .12f, .28f, false);
        AssetDatabase.SaveAssets();
        File.WriteAllText(Folder+"/Validation.txt", "PASS: all four prefab bounds are 2 x 2 metres.\nPASS: thickness and visible surface elevation verified.\nPASS: URP materials and BoxColliders assigned.\nBlender verifies closed meshes and matching UVs on both pairs of edges.\nSnap X/Z by 2 metres. Floor Y=0; ceiling underside Y=3 for 3m walls.\n");
        Debug.Log("MANOR_SURFACES_UNITY_OK");
    }

    private static void Make(string name, float thickness, float smoothness, bool ceiling)
    {
        string texturePath=Folder+"/"+name+"_BaseColor.png";
        var ti=(TextureImporter)AssetImporter.GetAtPath(texturePath);
        ti.sRGBTexture=true;ti.mipmapEnabled=true;ti.wrapMode=TextureWrapMode.Repeat;ti.maxTextureSize=2048;
        ti.SaveAndReimport();
        var shader=Shader.Find("Universal Render Pipeline/Lit");
        if(shader==null)throw new Exception("URP Lit missing");
        string matName="M_"+name,matPath=Folder+"/"+matName+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if(mat==null){mat=new Material(shader);AssetDatabase.CreateAsset(mat,matPath);}
        mat.shader=shader;mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
        mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Metallic",0);mat.SetFloat("_Smoothness",smoothness);
        mat.enableInstancing=true;EditorUtility.SetDirty(mat);
        string modelPath=Folder+"/SM_"+name+"_2x2.fbx";
        var mi=(ModelImporter)AssetImporter.GetAtPath(modelPath);
        mi.importAnimation=false;mi.importCameras=false;mi.importLights=false;mi.globalScale=1;mi.generateSecondaryUV=true;
        mi.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
        mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),matName),mat);mi.SaveAndReimport();
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        var root=new GameObject("PF_"+name+"_2x2");
        try
        {
            var child=(GameObject)PrefabUtility.InstantiatePrefab(model);child.transform.SetParent(root.transform,false);
            var renderer=root.GetComponentInChildren<Renderer>();
            if(renderer==null)throw new Exception("No renderer");
            var bounds=renderer.bounds;
            if(Mathf.Abs(bounds.size.x-2)>.001f||Mathf.Abs(bounds.size.z-2)>.001f||Mathf.Abs(bounds.size.y-thickness)>.001f)
                throw new Exception("Unexpected imported dimensions: "+bounds.size);
            float visibleY=ceiling?bounds.min.y:bounds.max.y;
            if(Mathf.Abs(visibleY)>.001f)throw new Exception("Visible surface must be Y=0");
            var collider=root.AddComponent<BoxCollider>();collider.center=root.transform.InverseTransformPoint(bounds.center);collider.size=bounds.size;
            PrefabUtility.SaveAsPrefabAsset(root,Folder+"/"+root.name+".prefab");
        }
        finally{Object.DestroyImmediate(root);}
    }
}
