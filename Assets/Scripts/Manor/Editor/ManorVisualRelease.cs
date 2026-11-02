using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Echo.Manor.Editor
{
    // Repeatable visual audit and Windows release entry points for batch-mode Unity.
    public static class ManorVisualRelease
    {
        const string Output = "work/visual-release";
        static string Hierarchy(Transform t) => t.parent == null ? t.name : Hierarchy(t.parent) + "/" + t.name;

        public static void Audit()
        {
            Directory.CreateDirectory(Output);
            EditorSceneManager.OpenScene(ManorPrototypeBuilder.ScenePath);
            var lines = new List<string>();
            foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
            {
                var filter = renderer.GetComponent<MeshFilter>();
                var mesh = filter != null ? filter.sharedMesh : (renderer as SkinnedMeshRenderer)?.sharedMesh;
                lines.Add($"RENDERER {Hierarchy(renderer.transform)} active={renderer.gameObject.activeInHierarchy} enabled={renderer.enabled} pos={renderer.transform.position} bounds={renderer.bounds} mesh={mesh?.name} uv={mesh?.uv.Length}");
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null) { lines.Add("  MISSING MATERIAL"); continue; }
                    var texture = material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : material.mainTexture;
                    lines.Add($"  MATERIAL {AssetDatabase.GetAssetPath(material)} shader={material.shader?.name} texture={AssetDatabase.GetAssetPath(texture)} color={(material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor").ToString() : "n/a")}");
                    var serialized = new SerializedObject(material);
                    var environments = serialized.FindProperty("m_SavedProperties.m_TexEnvs");
                    if (environments == null) continue;
                    for (int i = 0; i < environments.arraySize; i++)
                    {
                        var entry = environments.GetArrayElementAtIndex(i);
                        var reference = entry.FindPropertyRelative("second.m_Texture");
                        if (reference.objectReferenceValue == null && !reference.objectReferenceEntityIdValue.Equals(default(UnityEngine.EntityId)))
                            lines.Add("  BROKEN TEXTURE " + entry.FindPropertyRelative("first").stringValue);
                    }
                }
            }
            foreach (var group in Object.FindObjectsByType<MeshFilter>()
                .Where(f => f.sharedMesh != null && f.GetComponent<Renderer>() != null && f.GetComponent<Renderer>().enabled)
                .GroupBy(f => f.sharedMesh.GetEntityId() + ":" + f.transform.localToWorldMatrix.ToString("F4")))
                if (group.Count() > 1) lines.Add("DUPLICATE " + string.Join(" | ", group.Select(f => Hierarchy(f.transform))));
            File.WriteAllLines(Output + "/audit.txt", lines);
            Capture("before");
            Debug.Log("VISUAL_AUDIT_COMPLETE " + lines.Count);
        }

        static readonly (string name, Vector3 eye, Vector3 target)[] Views =
        {
            ("caretaker", new Vector3(-14,1.65f,-6), new Vector3(-17,1,-9)),
            ("gallery", new Vector3(-8,1.65f,-9), new Vector3(6,1.3f,-9)),
            ("hall", new Vector3(11,1.65f,-6), new Vector3(19,1.3f,1)),
            ("library", new Vector3(16,1.65f,13), new Vector3(10,1.1f,20)),
            ("study", new Vector3(0,6.05f,-4), new Vector3(-6,5.4f,-5)),
            ("kitchen", new Vector3(27,1.65f,4), new Vector3(33,1,9)),
            ("conservatory", new Vector3(28,1.65f,17), new Vector3(33,1,25)),
            ("yard", new Vector3(24,1.65f,30), new Vector3(30,1,38))
        };

        static void Capture(string stage)
        {
            var cameraObject = new GameObject("Temporary visual audit camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = .05f; camera.farClipPlane = 100; camera.fieldOfView = 70;
            camera.allowHDR = true;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var target = new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);
            target.Create(); camera.targetTexture = target;
            Directory.CreateDirectory(Output + "/" + stage);
            foreach (var view in Views)
            {
                camera.transform.SetPositionAndRotation(view.eye, Quaternion.LookRotation(view.target-view.eye));
                // Warm the pipeline's per-camera lighting and shadow resources before reading back.
                camera.Render();
                camera.Render();
                var previous = RenderTexture.active; RenderTexture.active = target;
                var texture = new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0); texture.Apply();
                File.WriteAllBytes(Output + "/" + stage + "/" + view.name + ".png", texture.EncodeToPNG());
                RenderTexture.active = previous; Object.DestroyImmediate(texture);
            }
            camera.targetTexture = null; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(cameraObject);
        }

        public static void ApplyAndVerify()
        {
            Directory.CreateDirectory(Output);
            EditorSceneManager.OpenScene(ManorPrototypeBuilder.ScenePath);
            var foliage = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Materials/Manor/Low poly foliage.asset");
            ManorConceptUpgrade.EnsureFoliageUVs(foliage);
            var stone = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Manor/Muted tile.mat");
            stone.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Props/ManorSurfaces/ManorStoneFloor_BaseColor.png"));
            stone.SetTexture("_MainTex", stone.GetTexture("_BaseMap"));
            stone.SetColor("_BaseColor", new Color(.85f,.85f,.80f));
            stone.SetTextureScale("_BaseMap", new Vector2(.24f,.24f));
            stone.SetTextureOffset("_BaseMap", new Vector2(.005f,.005f));
            EditorUtility.SetDirty(stone);
            var leaves = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Manor/Garden leaves.mat");
            leaves.SetColor("_BaseColor", new Color(.60f,.72f,.55f));
            EditorUtility.SetDirty(leaves);
            // A corner post was emitted by both balcony runs. Hide only its coincident visual.
            foreach (var group in Object.FindObjectsByType<MeshFilter>()
                .Where(f => f.name == "Balcony baluster" && f.GetComponent<Renderer>().enabled)
                .GroupBy(f => f.transform.localToWorldMatrix.ToString("F4")))
                foreach (var duplicate in group.OrderBy(f => f.transform.GetSiblingIndex()).Skip(1))
                    duplicate.GetComponent<Renderer>().enabled = false;
            ManorWholeAtmosphere.ApplyAndSave();
            ValidateScene();
            Capture("after");
            Debug.Log("VISUAL_REPAIR_VERIFIED");
        }

        public static void ReleaseWindows()
        {
            ApplyAndVerify();
            BuildWindows();
        }

        static void ValidateScene()
        {
            int renderers = 0, materials = 0;
            foreach (var transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0)
                    throw new InvalidOperationException("Missing script: " + Hierarchy(transform));
            foreach (var renderer in Object.FindObjectsByType<Renderer>())
            {
                if (!renderer.enabled) continue;
                renderers++;
                var filter = renderer.GetComponent<MeshFilter>();
                var mesh = filter != null ? filter.sharedMesh : null;
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null || material.shader == null || !material.shader.isSupported)
                        throw new InvalidOperationException("Missing/unsupported material: " + Hierarchy(renderer.transform));
                    materials++;
                    if (mesh != null && mesh.uv.Length == 0 && material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") != null)
                        throw new InvalidOperationException("Textured mesh has no UVs: " + Hierarchy(renderer.transform));
                }
            }
            File.WriteAllText(Output + "/validation.txt", $"renderers={renderers}\nmaterialSlots={materials}\nmissingScripts=0\nmissingMaterials=0\nunsupportedShaders=0\nambient={RenderSettings.ambientLight}\n");
        }

        public static void BuildWindows()
        {
            Directory.CreateDirectory(Output);
            var scenes = new[] { "Assets/Scenes/MainMenu/MainMenu.unity", ManorPrototypeBuilder.ScenePath };
            Directory.CreateDirectory("Builds/Windows");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes, locationPathName = "Builds/Windows/Echo.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            File.WriteAllText(Output + "/build.txt", $"result={report.summary.result}\nerrors={report.summary.totalErrors}\nwarnings={report.summary.totalWarnings}\nsize={report.summary.totalSize}\ntime={report.summary.totalTime}\n");
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Windows build failed.");
            Debug.Log("ECHO_WINDOWS_BUILD_SUCCEEDED " + report.summary.outputPath);
        }
    }
}
