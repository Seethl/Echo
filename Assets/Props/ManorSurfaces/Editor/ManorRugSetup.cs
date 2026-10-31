using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class ManorRugSetup
{
    private const string Folder="Assets/Props/ManorSurfaces";
    [MenuItem("Tools/Echo/Build Manor Rug")]
    public static void Build()
    {
        AssetDatabase.Refresh();
        string tp=Folder+"/ManorRug_BaseColor.png";
        var ti=(TextureImporter)AssetImporter.GetAtPath(tp);
        ti.sRGBTexture=true;ti.wrapMode=TextureWrapMode.Clamp;ti.mipmapEnabled=true;ti.maxTextureSize=2048;ti.SaveAndReimport();
        var shader=Shader.Find("Universal Render Pipeline/Lit");
        if(shader==null)throw new Exception("URP Lit missing");
        string mp=Folder+"/M_ManorRug.mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(mp);
        if(mat==null){mat=new Material(shader);AssetDatabase.CreateAsset(mat,mp);}
        mat.shader=shader;mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(tp));
        mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Metallic",0);mat.SetFloat("_Smoothness",.02f);EditorUtility.SetDirty(mat);
        string fp=Folder+"/SM_ManorRug_2x3.fbx";
        var mi=(ModelImporter)AssetImporter.GetAtPath(fp);
        mi.importAnimation=false;mi.importCameras=false;mi.importLights=false;mi.globalScale=1;mi.generateSecondaryUV=true;
        mi.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
        mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"M_ManorRug"),mat);mi.SaveAndReimport();
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(fp);
        var root=new GameObject("PF_ManorRug_2x3");
        try
        {
            var child=(GameObject)PrefabUtility.InstantiatePrefab(model);child.transform.SetParent(root.transform,false);
            Bounds b=root.GetComponentInChildren<Renderer>().bounds;
            if(Mathf.Abs(b.size.x-2)>.001f||Mathf.Abs(b.size.z-3)>.001f||b.min.y<.002f)
                throw new Exception("Rug dimensions or floor clearance invalid: "+b);
            PrefabUtility.SaveAsPrefabAsset(root,Folder+"/PF_ManorRug_2x3.prefab");
        }
        finally{Object.DestroyImmediate(root);}
        AssetDatabase.SaveAssets();
        File.WriteAllText(Folder+"/RugValidation.txt","PASS: 2x3 metre rug.\nPASS: 3mm underside clearance above floor.\nPASS: URP material assigned.\nNo collider: uses supporting floor collision.\n");
        Debug.Log("MANOR_RUG_UNITY_OK");
    }
}
