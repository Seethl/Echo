using System;
using System.Linq;
using Echo.Environment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Echo.Manor.Editor
{
    public static class ManorWindowDress
    {
        public const string Folder = "Assets/Props/ManorWindowDress";
        public const string ArchName = "Grand arched window";
        public const string CurtainName = "Tied velvet curtains";

        private static readonly (float Z, string Light, string Shaft)[] HallWindows =
        {
            (-8.5f, "Hall south window", "Window haze 2384_310_-850"),
            (.5f, "Hall north window", "Window haze 2384_310_50")
        };

        [MenuItem("Echo/Аркові вікна та штори")]
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

            foreach (var model in new[] { ArchName, CurtainName })
                ConfigureModel(model);

            foreach (var item in HallWindows)
            {
                var window = FindHallWindow(item.Z);
                foreach (Transform child in window)
                    if (child.name.StartsWith("Window ", StringComparison.Ordinal))
                        child.gameObject.SetActive(false);

                PlaceModel(window, ArchName, Vector3.one);
                PlaceModel(window, CurtainName, Vector3.one);
            }

            var study = Object.FindObjectsByType<Transform>().Single(t =>
                t.name == "Moonlit window" && Vector3.Distance(t.position, new Vector3(-9.84f, 6.1f, -5.5f)) < .1f);
            PlaceModel(study, CurtainName, new Vector3(.67f, .63f, .8f));
            AlignHallShafts();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        internal static void AlignHallShafts()
        {
            foreach (var item in HallWindows)
            {
                var window = FindHallWindow(item.Z);
                if (window.Find(ArchName) == null)
                    continue;

                var light = GameObject.Find(item.Light).GetComponent<Light>();
                light.cookie = ArchCookie();
                EditorUtility.SetDirty(light);

                var shaft = GameObject.Find(item.Shaft)?.GetComponent<WindowMoonShaft>();
                if (shaft == null)
                    continue;

                // The cookie covers x = ±1.14 and y = -2.19…1.89 in the window model.
                shaft.transform.position = window.position + window.forward * .16f - Vector3.up * .15f;
                Vector3 direction = light.transform.forward;
                float toFloor = (.1f - light.transform.position.y) / direction.y;
                Vector3 end = light.transform.position + direction * toFloor;
                Vector3 offset = shaft.transform.InverseTransformDirection(end - shaft.transform.position);
                shaft.width = 2.28f;
                shaft.height = 4.08f;
                shaft.length = offset.z;
                shaft.drop = -offset.y;
                shaft.sideDrift = offset.x;
                shaft.spread = 1.6f;
                shaft.opacity = .055f;
                shaft.density = .017f;
                shaft.windowLight = light;
                shaft.alignWithWindowCookie = true;
                shaft.useWindowCookieMask = true;
                shaft.transform.localScale = new Vector3(shaft.width, shaft.height, shaft.length);

                var mesh = shaft.GetComponent<MeshFilter>().sharedMesh;
                Vector3[] vertices = mesh.vertices;
                float x = shaft.sideDrift / shaft.width;
                float y = -shaft.drop / shaft.height;
                float half = shaft.spread * .5f;
                vertices[4] = new Vector3(x - half, y - half, 1);
                vertices[5] = new Vector3(x + half, y - half, 1);
                vertices[6] = new Vector3(x + half, y + half, 1);
                vertices[7] = new Vector3(x - half, y + half, 1);
                mesh.vertices = vertices;
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
                shaft.Refresh(true);
                EditorUtility.SetDirty(shaft);
                EditorUtility.SetDirty(mesh);
            }
        }

        private static Transform FindHallWindow(float z)
        {
            var windows = Object.FindObjectsByType<Transform>();
            return windows.Single(t => t.name == "Moonlit window placeholder" &&
                Vector3.Distance(t.position, new Vector3(23.84f, 3.1f, z)) < .1f);
        }

        private static void PlaceModel(Transform window, string name, Vector3 scale)
        {
            var existing = window.Find(name);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{Folder}/{name}.fbx");
            var model = existing != null ? existing : ((GameObject)PrefabUtility.InstantiatePrefab(asset, window)).transform;
            model.name = name;
            model.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            model.localScale = scale;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
            {
                // The paired cookie supplies the glazing bars in the projected light.
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(model);
        }

        private static void ConfigureModel(string name)
        {
            string path = $"{Folder}/{name}.fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            if (name == ArchName)
            {
                Remap(importer, "Weathered limestone", new Color(.27f, .25f, .23f));
                Remap(importer, "Dark carved oak", new Color(.105f, .072f, .055f));
                Remap(importer, "Blue night glass", new Color(.095f, .16f, .24f), .22f, 0, new Color(.018f, .035f, .067f));
            }
            else
            {
                Remap(importer, "Wine velvet", new Color(.44f, .085f, .115f));
                Remap(importer, "Aged brass", new Color(.48f, .30f, .13f), .33f, .28f);
            }
            AssetDatabase.WriteImportSettingsIfDirty(path);
            AssetDatabase.ImportAsset(path);
        }

        private static void Remap(ModelImporter importer, string name, Color color,
            float smoothness = .12f, float metallic = 0, Color emission = default)
        {
            string path = $"{Folder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.SetColor("_BaseColor", color);
                material.SetFloat("_Smoothness", smoothness);
                material.SetFloat("_Metallic", metallic);
                if (emission != default)
                {
                    material.SetColor("_EmissionColor", emission);
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                    material.EnableKeyword("_EMISSION");
                }
                AssetDatabase.CreateAsset(material, path);
            }
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name), material);
        }

        private static Texture2D ArchCookie()
        {
            string path = $"{Folder}/Arched moon cookie.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null)
                return existing;

            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
                { name = "Arched moon cookie", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color[size * size];
            for (int row = 0; row < size; row++)
            for (int column = 0; column < size; column++)
            {
                float x = Mathf.Lerp(-1.14f, 1.14f, (column + .5f) / size);
                float y = Mathf.Lerp(-2.19f, 1.89f, (row + .5f) / size);
                float top = Mathf.Max(0, y - .71f);
                float outline = Mathf.Min(y + 1.98f, .96f - new Vector2(x, top).magnitude);
                float bars = Mathf.Abs(y - .71f);
                if (y < .71f)
                {
                    bars = Mathf.Min(bars, Mathf.Abs(Mathf.Abs(x) - .32f), Mathf.Abs(y + 1.08f), Mathf.Abs(y + .18f));
                }
                else
                {
                    float radius = new Vector2(x, top).magnitude;
                    bars = Mathf.Min(bars, Mathf.Abs(radius - .215f));
                    if (radius > .215f)
                        bars = Mathf.Min(bars, Mathf.Abs(x), Mathf.Abs(Mathf.Abs(x) - top) * .7071f);
                }
                float strength = Mathf.SmoothStep(0, 1, outline / .045f) *
                                 Mathf.SmoothStep(0, 1, (bars - .019f) / .04f);
                pixels[row * size + column] = new Color(strength, strength, strength, 1);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            AssetDatabase.CreateAsset(texture, path);
            return texture;
        }
    }
}
