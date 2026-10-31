using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ManorShuttersSetup
{
    const string Folder = InteractivePropsSetup.Folder;

    public static void BuildAndPreview()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Use the isolated validation project.");
        Build();
        InteractivePropsSetup.RenderPreviews();
    }

    [MenuItem("Tools/Echo/Build Window Shutters")]
    public static void Build()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before building props.");
        AssetDatabase.Refresh();
        var importer = (ModelImporter)AssetImporter.GetAtPath(Folder + "/SM_ManorShutters.fbx");
        importer.globalScale = 1;
        importer.importAnimation = false;
        importer.generateSecondaryUV = true;
        foreach (string material in new[] { "Wood", "Iron" })
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "M_Interactive_" + material),
                AssetDatabase.LoadAssetAtPath<Material>(Folder + "/M_Interactive_" + material + ".mat"));
        importer.SaveAndReimport();
        var root = new GameObject("PF_ManorShutters");
        try
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/SM_ManorShutters.fbx"));
            model.transform.SetParent(root.transform, false);
            foreach (var mesh in root.GetComponentsInChildren<MeshFilter>())
            {
                if (mesh.GetComponent<Renderer>().bounds.size.y < 1)
                    throw new InvalidOperationException("Shutter imported below metre scale: " + mesh.name);
                var collider = mesh.gameObject.AddComponent<BoxCollider>();
                collider.center = mesh.sharedMesh.bounds.center;
                collider.size = mesh.sharedMesh.bounds.size;
            }
            PrefabUtility.SaveAsPrefabAsset(root, Folder + "/PF_ManorShutters.prefab");
            var window = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/PF_ManorWindow.prefab"));
            window.transform.SetParent(root.transform, false);
            root.name = "PF_ManorWindow_Shutters";
            // The panel pivots stay on the hinges. Artists can set their local Y angle.
            foreach (var t in model.GetComponentsInChildren<Transform>())
                if (t.name == "LeftHinge" || t.name == "RightHinge")
                    t.rotation = Quaternion.AngleAxis(t.position.x > 0 ? 120 : -120, Vector3.up) * t.rotation;
            PrefabUtility.SaveAsPrefabAsset(root, Folder + "/PF_ManorWindow_Shutters.prefab");
            AssetDatabase.SaveAssets();
            Debug.Log("MANOR_SHUTTERS_PREFABS_OK");
        }
        finally { Object.DestroyImmediate(root); }
    }
}
