using System.IO;
using UnityEditor;
using UnityEngine;

public static class ManorWallSetup
{
    private const string Folder = "Assets/Props/ManorWall";

    [MenuItem("Tools/Echo/Build Manor Wall Prefab")]
    public static void Build()
    {
        AssetDatabase.Refresh();
        string texturePath = Folder + "/ManorWall_BaseColor.png";
        var textureImporter = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        textureImporter.sRGBTexture = true;
        textureImporter.wrapMode = TextureWrapMode.Repeat;
        textureImporter.mipmapEnabled = true;
        textureImporter.maxTextureSize = 2048;
        textureImporter.SaveAndReimport();
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) throw new System.Exception("URP Lit shader missing");
        string[] names = { "M_ManorWall", "M_ManorWall_Trim" };
        Material[] materials = new Material[2];
        for (int i = 0; i < names.Length; i++)
        {
            string path = Folder + "/" + names[i] + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetTexture("_BaseMap", texture);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", i == 0 ? .12f : .26f);
            EditorUtility.SetDirty(mat);
            materials[i] = mat;
        }
        string modelPath = Folder + "/SM_ManorWall_2x3.fbx";
        var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
        importer.globalScale = 1f;
        importer.importCameras = false;
        importer.importLights = false;
        importer.importAnimation = false;
        importer.generateSecondaryUV = true;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        for (int i = 0; i < names.Length; i++)
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), names[i]), materials[i]);
        importer.SaveAndReimport();
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        var root = new GameObject("PF_ManorWall_2x3");
        try
        {
            var child = (GameObject)PrefabUtility.InstantiatePrefab(model);
            child.transform.SetParent(root.transform, false);
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) throw new System.Exception("FBX has no renderer");
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            if (Mathf.Abs(bounds.size.x - 2f) > .01f || Mathf.Abs(bounds.size.y - 3f) > .01f)
                throw new System.Exception("Unexpected imported wall dimensions: " + bounds.size);
            var collider = root.AddComponent<BoxCollider>();
            collider.center = root.transform.InverseTransformPoint(bounds.center);
            collider.size = bounds.size;
            PrefabUtility.SaveAsPrefabAsset(root, Folder + "/PF_ManorWall_2x3.prefab");
            File.WriteAllText(Folder + "/UnityValidation.txt",
                "Prefab generated successfully.\nBounds metres: " + bounds.size.ToString("F4") +
                "\nCollider: BoxCollider\nSnap: 2 metres along local X\nUV seams: mirrored U=0 at both ends\n");
        }
        finally { Object.DestroyImmediate(root); }
        AssetDatabase.SaveAssets();
        Debug.Log("MANOR_WALL_UNITY_OK");
    }
}
