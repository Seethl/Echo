using System;
using System.IO;
using System.Reflection;
using Echo.Interactions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ManorDoorSetup
{
    private const string Folder = "Assets/Props/ManorDoor";

    [MenuItem("Tools/Echo/Build Animated Manor Door")]
    public static void Build()
    {
        AssetDatabase.Refresh();
        string texturePath = Folder + "/ManorDoor_BaseColor.png";
        var textureImporter = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        textureImporter.sRGBTexture = true;
        textureImporter.mipmapEnabled = true;
        textureImporter.maxTextureSize = 2048;
        textureImporter.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) throw new Exception("URP Lit shader missing");
        string[] names = { "M_ManorDoor_Wood", "M_ManorDoor_Brass", "M_ManorDoor_Iron", "M_ManorDoor_Wall" };
        Material[] materials = new Material[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            string path = Folder + "/" + names[i] + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
            mat.shader = shader;
            bool textured = i == 0 || i == 3;
            mat.SetTexture("_BaseMap", textured ? texture : null);
            mat.SetColor("_BaseColor", textured ? Color.white : i == 1 ? new Color(.34f,.235f,.085f) : new Color(.055f,.045f,.033f));
            mat.SetFloat("_Metallic", i == 1 ? .82f : i == 2 ? .65f : 0);
            mat.SetFloat("_Smoothness", i == 1 ? .52f : i == 3 ? .12f : .22f);
            EditorUtility.SetDirty(mat); materials[i] = mat;
        }
        string modelPath = Folder + "/SM_ManorDoor.fbx";
        var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
        importer.importAnimation = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.globalScale = 1;
        importer.generateSecondaryUV = true;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        for (int i = 0; i < names.Length; i++)
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), names[i]), materials[i]);
        importer.SaveAndReimport();
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        MakePrefab(model, true);
        MakePrefab(model, false);
        AssetDatabase.SaveAssets();
        File.WriteAllText(Folder + "/Validation.txt",
            "PASS: frame and moving leaf are separate meshes.\n" +
            "PASS: HingedDoor and moving BoxCollider assigned.\n" +
            "PASS: opening reaches 100 degrees; frame remains stationary.\n" +
            "PASS: closing returns leaf to original rotation.\n" +
            "PASS: two prefab variants created.\n" +
            "Interaction uses existing PlayerInteractor (E). No Animator required.\n");
        Debug.Log("MANOR_DOOR_UNITY_OK");
    }

    private static Transform Find(Transform root, string name)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        throw new Exception("Missing model node " + name);
    }

    private static void MakePrefab(GameObject model, bool withWall)
    {
        string name = withWall ? "PF_ManorDoor_Wall_2x3" : "PF_ManorDoor_Frame";
        var root = new GameObject(name);
        try
        {
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
            visual.transform.SetParent(root.transform, false);
            var hinge = Find(visual.transform, "DoorHinge");
            var leaf = Find(visual.transform, "DoorLeaf");
            var frame = Find(visual.transform, "DoorFrame");
            var wall = Find(visual.transform, "DoorWall");
            // Imported Blender X is mirrored in Unity, so derive collider space
            // from the actual imported renderer instead of assuming its sign.
            var leafRenderer = leaf.GetComponent<MeshRenderer>();
            var leafCollider = leaf.gameObject.AddComponent<BoxCollider>();
            Bounds localBounds = leaf.GetComponent<MeshFilter>().sharedMesh.bounds;
            leafCollider.center = localBounds.center;
            leafCollider.size = localBounds.size;
            var door = hinge.gameObject.AddComponent<HingedDoor>();
            door.hinge = hinge;
            door.openAngle = 100;
            door.degreesPerSecond = 110;
            door.requiredKeyId = "";
            door.initiallyLocked = false;
            // Empty checkpoint ID deliberately: assign a UNIQUE ID per placed
            // door if it must participate in checkpoint persistence.
            door.checkpointId = "";
            Bounds fb = frame.GetComponent<Renderer>().bounds;
            float left = fb.min.x, right = fb.max.x;
            // Two upright collision strips and a lintel leave the passage open.
            AddBox(root.transform, "FrameColliderLeft", new Vector3(left+.12f,1.125f,fb.center.z), new Vector3(.24f,2.25f,.355f));
            AddBox(root.transform, "FrameColliderRight", new Vector3(right-.12f,1.125f,fb.center.z), new Vector3(.24f,2.25f,.355f));
            AddBox(root.transform, "FrameColliderTop", new Vector3(fb.center.x,2.375f,fb.center.z), new Vector3(fb.size.x,.25f,.355f));
            if (withWall)
            {
                Bounds wb = wall.GetComponent<Renderer>().bounds;
                AddBox(root.transform,"WallColliderLeft",new Vector3(wb.min.x+.23f,1.5f,wb.center.z),new Vector3(.46f,3,.27f));
                AddBox(root.transform,"WallColliderRight",new Vector3(wb.max.x-.23f,1.5f,wb.center.z),new Vector3(.46f,3,.27f));
                AddBox(root.transform,"WallColliderTop",new Vector3(wb.center.x,2.625f,wb.center.z),new Vector3(1.08f,.75f,.27f));
                if (Mathf.Abs(wb.size.x-2)> .01f || Mathf.Abs(wb.size.y-3)> .01f)
                    throw new Exception("Wall module size invalid: " + wb.size);
            }
            else
            {
                // Keep the FBX connection; disable only the wall for this variant.
                wall.gameObject.SetActive(false);
            }
            typeof(HingedDoor).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(door,null);
            Quaternion closed = hinge.localRotation;
            Matrix4x4 frameBefore = frame.localToWorldMatrix;
            Vector3 leafCentreBefore = leafRenderer.bounds.center;
            door.RestoreState("{\"open\":true,\"locked\":false}");
            if (Mathf.Abs(Quaternion.Angle(closed,hinge.localRotation)-100)>.01f || !door.IsOpen)
                throw new Exception("Door did not open around its hinge");
            if (Vector3.Distance(leafCentreBefore,leafRenderer.bounds.center)<.3f)
                throw new Exception("Door mesh did not move with hinge");
            if (frame.localToWorldMatrix != frameBefore) throw new Exception("Frame moved with door");
            door.RestoreState("{\"open\":false,\"locked\":false}");
            if (Quaternion.Angle(closed,hinge.localRotation)>.01f) throw new Exception("Door did not close");
            PrefabUtility.SaveAsPrefabAsset(root,Folder+"/"+name+".prefab");
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static void AddBox(Transform root,string name,Vector3 center,Vector3 size)
    {
        var ob = new GameObject(name);
        ob.transform.SetParent(root,false);
        var box = ob.AddComponent<BoxCollider>();
        box.center = root.InverseTransformPoint(center);
        box.size = size;
    }
}
