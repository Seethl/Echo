using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Echo.Manor.Editor
{
    public static class ManorExterior
    {
        public const string RootName = "Victorian manor exterior";
        public const string Folder = "Assets/Props/ManorExterior";
        private const string ModelPath = Folder + "/Manor exterior.fbx";

        [MenuItem("Echo/Зовнішній вигляд маєтку")]
        public static void ApplyAndSave()
        {
            Apply();
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        public static void Apply()
        {
            if (EditorSceneManager.GetActiveScene().path != ManorPrototypeBuilder.ScenePath)
                throw new InvalidOperationException("Open ManorPrototype first.");

            ConfigureModel();
            var root = GameObject.Find(RootName) ?? new GameObject(RootName);
            if (root.transform.Find("Architectural shell") == null)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                instance.name = "Architectural shell";
            }

            var soil = Material("Estate ground", new Color(.24f, .27f, .20f), .04f,
                "Assets/SourceFiles/Textures/Repeating Tiles/Tile_Grass.png", new Vector2(39, 42));
            Ground(root.transform, "Estate base", new Vector3(9, -.25f, 13), new Vector3(116, .4f, 124), soil, true);
            var gravel = Material("Forecourt gravel", new Color(.22f, .19f, .155f), .06f);
            Ground(root.transform, "Entry forecourt", new Vector3(14, -.048f, -19.45f), new Vector3(9.2f, .025f, 9.7f), gravel, false);
            Ground(root.transform, "Carriage approach", new Vector3(14, -.05f, -34.22f), new Vector3(4.4f, .02f, 20.0f), gravel, false);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        private static void ConfigureModel()
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            if (importer == null)
                throw new InvalidOperationException("The exterior FBX must be imported before authoring.");

            importer.addCollider = false;
            importer.importCameras = false;
            importer.importLights = false;
            Remap(importer, "Old brick", Color.white, .11f, Folder + "/Weathered brick.png");
            Remap(importer, "Slate", Color.white, .18f, Folder + "/Weathered slate.png");
            Remap(importer, "Limestone", new Color(.45f, .41f, .36f), .09f);
            Remap(importer, "Oak", new Color(.14f, .097f, .071f), .15f);
            Remap(importer, "Plaster", new Color(.43f, .39f, .32f), .08f);
            Remap(importer, "Night glazing", new Color(.084f, .128f, .166f), .39f);
            Remap(importer, "Verdigris", new Color(.16f, .25f, .22f), .24f);
            Remap(importer, "Clay", new Color(.32f, .15f, .1f), .14f);
            if (AssetDatabase.WriteImportSettingsIfDirty(ModelPath))
                AssetDatabase.ImportAsset(ModelPath);
        }

        private static void Remap(ModelImporter importer, string name, Color color, float smoothness, string texturePath = null)
        {
            var material = Material(name, color, smoothness, texturePath);
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name), material);
        }

        private static Material Material(string name, Color color, float smoothness, string texturePath = null,
            Vector2 tiling = default)
        {
            string path = $"{Folder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;

            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            if (texturePath != null)
            {
                material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
                if (tiling != default)
                    material.SetTextureScale("_BaseMap", tiling);
            }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void Ground(Transform root, string name, Vector3 center, Vector3 size, Material material, bool solid)
        {
            var existing = root.Find(name);
            var piece = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = name;
            piece.transform.SetParent(root, false);
            piece.transform.SetLocalPositionAndRotation(center, Quaternion.identity);
            piece.transform.localScale = size;
            piece.GetComponent<MeshRenderer>().sharedMaterial = material;
            piece.GetComponent<BoxCollider>().enabled = solid;
        }
    }
}
