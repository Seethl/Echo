using System;
using System.Linq;
using Echo.Interactions;
using Echo.Radio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Echo.Manor.Editor
{
    public static class ManorPlacementRepair
    {
        const string LeafName = "Aligned door leaf";

        [MenuItem("Echo/Вирівняти радіо та двері")]
        public static void ApplyAndSave()
        {
            Apply();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before repairing the saved scene.");
            if (EditorSceneManager.GetActiveScene().path != ManorPrototypeBuilder.ScenePath)
                throw new InvalidOperationException("Open ManorPrototype before repairing placements.");
            GroundRadio();
            // Leave the existing gameplay roots, keys, events and checkpoint IDs intact.
            Cabinet("Service Cupboard", "Service Cupboard Door");
            ManorPlacementFinish.MountCupboardHinges();
            Cabinet("Library Cache", "Library Cache Door");
            AlignDoor("Study Door", new Vector3(-1.675f, 4.41f, 4.275f), 1.35f, 2.28f, .10f, 90);
            ClearUpperDoorSwing();
            AlignDoor("Kitchen Shortcut Door", new Vector3(23.87f, 0, 5.175f), 2.35f, 2.65f, .13f, 90);
            AlignDoor("Jammed Garden Door", new Vector3(28.825f, 0, 27.87f), 2.35f, 2.65f, .13f, 90);
            Physics.SyncTransforms();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        static void GroundRadio()
        {
            var radio = Object.FindAnyObjectByType<PortableRadio>();
            if (radio == null || radio.physicalView == null)
                throw new InvalidOperationException("The physical manor radio is missing.");
            var desk = GameObject.Find("Caretaker Desk").GetComponent<BoxCollider>();
            Physics.SyncTransforms();
            float top = desk.bounds.max.y;
            var point = VisibleBounds(radio.transform).center;
            // The fitted desk mesh is a few millimetres above the original collision slab.
            foreach (var filter in Object.FindObjectsByType<MeshFilter>())
            {
                if (filter.transform.IsChildOf(radio.transform) || !Visible(filter)) continue;
                var bounds = Bounds(filter, null);
                if (point.x < bounds.min.x || point.x > bounds.max.x || point.z < bounds.min.z || point.z > bounds.max.z ||
                    bounds.max.y < top - .06f || bounds.min.y > top + .06f) continue;
                var vertices = filter.sharedMesh.vertices;
                var indices = filter.sharedMesh.triangles;
                for (int i = 0; i < indices.Length; i += 3)
                {
                    var a = filter.transform.TransformPoint(vertices[indices[i]]);
                    var b = filter.transform.TransformPoint(vertices[indices[i + 1]]);
                    var c = filter.transform.TransformPoint(vertices[indices[i + 2]]);
                    if (Vector3.Cross(b - a, c - a).normalized.y < .99f || Mathf.Abs(a.y - desk.bounds.max.y) > .06f) continue;
                    if (InsideXZ(point, a, b, c)) top = Mathf.Max(top, a.y);
                }
            }
            var feet = VisibleBounds(radio.transform).min.y;
            radio.transform.position += Vector3.up * (top + .002f - feet);
        }

        static bool InsideXZ(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector2 v0 = new Vector2(b.x - a.x, b.z - a.z), v1 = new Vector2(c.x - a.x, c.z - a.z), v2 = new Vector2(p.x - a.x, p.z - a.z);
            float determinant = v0.x * v1.y - v1.x * v0.y;
            if (Mathf.Abs(determinant) < .000001f) return false;
            float u = (v2.x * v1.y - v1.x * v2.y) / determinant;
            float v = (v0.x * v2.y - v2.x * v0.y) / determinant;
            return u >= -.0001f && v >= -.0001f && u + v <= 1.0001f;
        }

        static void Cabinet(string cabinetName, string doorName)
        {
            var cabinet = GameObject.Find(cabinetName);
            var body = cabinet.GetComponentsInChildren<MeshFilter>().First(f => f.name == "CabinetBody" && Visible(f));
            var bounds = Bounds(body, null);
            // Fit inside the native body's stiles, above its plinth and below its crown.
            float width = bounds.size.x * .815f, height = bounds.size.y * .835f;
            var pivot = new Vector3(bounds.center.x - width / 2, bounds.min.y + bounds.size.y * .112f, bounds.min.z - .055f);
            AlignDoor(doorName, pivot, width, height, .065f, 90);
        }

        static void ClearUpperDoorSwing()
        {
            var pot = Object.FindObjectsByType<Collider>().FirstOrDefault(c => c.name == "Plant pot" &&
                c.bounds.center.y > 4.4f && Mathf.Abs(c.bounds.center.x + 1.8f) < 1 && Mathf.Abs(c.bounds.center.z - 3.5f) < .1f);
            if (pot == null || pot.transform.parent.name != "Potted foliage") return;
            pot.transform.parent.position += Vector3.right * (-2.35f - pot.bounds.center.x);
        }

        static void AlignDoor(string name, Vector3 pivot, float width, float height, float depth, float angle)
        {
            var door = GameObject.Find(name).GetComponent<HingedDoor>();
            var panel = door.GetComponentsInChildren<BoxCollider>().First(c => c.name == "Door Panel" || c.name == "Door leaf collider");
            door.hinge.position = pivot;
            door.openAngle = angle;
            panel.transform.localPosition = Vector3.zero;
            panel.transform.localRotation = Quaternion.identity;
            panel.transform.localScale = Vector3.one;
            panel.center = new Vector3(width / 2, height / 2, 0);
            panel.size = new Vector3(width, height, depth);
            var old = door.hinge.Find(LeafName);
            foreach (var renderer in door.GetComponentsInChildren<Renderer>(true))
                if (old == null || !renderer.transform.IsChildOf(old)) renderer.enabled = false;
            foreach (var label in door.GetComponentsInChildren<WorldLabelVisibility>(true)) label.enabled = false;
            if (old == null)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Props/ManorDoor/SM_ManorDoor.fbx");
                var source = model.GetComponentsInChildren<Transform>().First(t => t.name == "DoorLeaf").gameObject;
                old = ManorSetDress.Fit(source, door.hinge, LeafName, new Vector3(width / 2, height / 2, 0), new Vector3(width, height, depth)).transform;
                // The FBX's mirrored X axis puts the visible hinges at the far edge.
                // Match them to the actual pivot, while keeping the fitted silhouette centred.
                var center = VisibleBounds(old).center;
                old.RotateAround(center, door.hinge.up, 180);
                old.position += center - VisibleBounds(old).center;
            }
            // Refitting an existing leaf also keeps this repair safe after a doorway is resized.
            var local = Bounds(old.GetComponentInChildren<MeshFilter>(), door.hinge);
            if ((local.size - new Vector3(width, height, depth)).sqrMagnitude > .0000000001f)
                old.localScale = Vector3.Scale(old.localScale, new Vector3(width / local.size.x, height / local.size.y, depth / local.size.z));
            old.position += panel.transform.TransformPoint(panel.center) - VisibleBounds(old).center;
            if (door.navigationBlocker != null)
            {
                door.navigationBlocker.center = panel.center;
                door.navigationBlocker.size = new Vector3(width + .05f, height, .35f);
            }
            var actual = VisibleBounds(old);
            if (Vector3.Distance(actual.center, panel.transform.TransformPoint(panel.center)) > .005f)
                throw new InvalidOperationException("Door leaf and collision diverged: " + name);
            EditorUtility.SetDirty(door);
        }

        static bool Visible(MeshFilter filter) => filter.sharedMesh != null && filter.gameObject.activeInHierarchy && filter.GetComponent<Renderer>() != null && filter.GetComponent<Renderer>().enabled;
        public static Bounds VisibleBounds(Transform root)
        {
            bool first = true; Bounds result = default;
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>().Where(Visible))
            {
                var bounds = Bounds(filter, null);
                if (first) { result = bounds; first = false; } else result.Encapsulate(bounds);
            }
            if (first) throw new InvalidOperationException("No visible mesh under " + root.name);
            return result;
        }
        static Bounds Bounds(MeshFilter filter, Transform relativeTo)
        {
            var source = filter.sharedMesh.bounds; Bounds result = default;
            for (int i = 0; i < 8; i++)
            {
                var point = filter.transform.TransformPoint(source.center + Vector3.Scale(source.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                if (relativeTo != null) point = relativeTo.InverseTransformPoint(point);
                if (i == 0) result = new Bounds(point, Vector3.zero); else result.Encapsulate(point);
            }
            return result;
        }
    }
}
