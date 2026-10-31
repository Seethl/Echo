using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class PsychedelicMaterialsSetup
{
    const string Folder="Assets/Props/PsychedelicMaterials";
    [MenuItem("Tools/Echo/Build Psychedelic Materials")]
    public static void Build()
    {
        AssetDatabase.Refresh();
        Make("ManorWall", "Assets/Props/ManorWall/M_ManorWall.mat", "Wall", .12f);
        Make("ManorWall_Trim", "Assets/Props/ManorWall/M_ManorWall_Trim.mat", "Wall", .26f);
        Make("ManorFloor", "Assets/Props/ManorSurfaces/M_ManorFloor.mat", "Wood", .22f);
        Make("ManorStoneFloor", "Assets/Props/ManorSurfaces/M_ManorStoneFloor.mat", "Stone", .10f);
        Make("ManorParquetFloor", "Assets/Props/ManorSurfaces/M_ManorParquetFloor.mat", "Parquet", .28f);
        Make("ManorCeiling", "Assets/Props/ManorSurfaces/M_ManorCeiling.mat", "Ceiling", .08f);
        AssetDatabase.SaveAssets();
        File.WriteAllText(Folder+"/Validation.txt","PASS: six URP Lit materials, five sRGB textures.\nPASS: metallic zero, emission disabled, base map tiling 1x1, offset zero.\nPASS: original material sources exist.\nUses original meshes and UVs; no scene or prefab replaced.\n");
        Debug.Log("PSYCHEDELIC_MATERIALS_OK");
    }
    static void Make(string name,string source,string textureName,float smoothness)
    {
        var original=AssetDatabase.LoadAssetAtPath<Material>(source);
        if(original==null)throw new Exception("Missing original material: "+source);
        string tp=Folder+"/Psy_"+textureName+"_BaseColor.png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(tp);
        if(importer==null)throw new Exception("Missing texture: "+tp);
        importer.sRGBTexture=true;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;
        importer.maxTextureSize=2048;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(tp);
        string mp=Folder+"/M_"+name+"_Psy.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(mp);
        if(material==null){material=new Material(original);AssetDatabase.CreateAsset(material,mp);}
        else material.CopyPropertiesFromMaterial(original);
        material.name="M_"+name+"_Psy";
        material.SetTexture("_BaseMap",texture);material.SetColor("_BaseColor",Color.white);
        material.SetTextureScale("_BaseMap",Vector2.one);material.SetTextureOffset("_BaseMap",Vector2.zero);
        material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",smoothness);
        material.DisableKeyword("_EMISSION");material.SetColor("_EmissionColor",Color.black);
        material.enableInstancing=true;EditorUtility.SetDirty(material);
        if(material.shader.name!="Universal Render Pipeline/Lit"||material.GetTexture("_BaseMap")!=texture)
            throw new Exception("Material setup failed: "+name);
    }
}
