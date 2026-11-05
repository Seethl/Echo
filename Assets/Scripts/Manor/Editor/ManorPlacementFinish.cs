using System;
using System.Linq;
using Echo.Interactions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Echo.Manor.Editor
{
    internal static class ManorPlacementFinish
    {
        private const string Marker = "Grounded manor details";

        [MenuItem("Echo/Опори, поручні й осіннє озеленення")]
        public static void ApplyAndSave()
        {
            Apply();
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        internal static void Apply()
        {
            if (EditorSceneManager.GetActiveScene().path != ManorPrototypeBuilder.ScenePath)
                throw new InvalidOperationException("Open ManorPrototype first.");

            MountCupboardHinges();
            var root = Group(null, Marker);
            JoinStairAndLanding(root);
            GroundPlants(root);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        internal static void MountCupboardHinges()
        {
            var cabinet = GameObject.Find("Service Cupboard").transform;
            var door = GameObject.Find("Service Cupboard Door").GetComponent<HingedDoor>();
            var panel = door.GetComponentsInChildren<BoxCollider>().First(c => c.name is "Door Panel" or "Door leaf collider");
            var fixedSide = Group(cabinet, "Cupboard fixed hinge fittings");
            var movingSide = Group(door.hinge, "Cupboard moving hinge fittings");
            var brass = Existing("Dull brass");
            var wood = Existing("Aged timber");
            var pivot = door.hinge.position;

            // A mounting stile bridges the gap between the model's recessed jamb and the pivot.
            Part(fixedSide, "Cupboard hinge stile", pivot + new Vector3(-.047f, panel.size.y / 2, .066f),
                new Vector3(.083f, panel.size.y, .12f), wood);
            for (int index = 0; index < 2; index++)
            {
                float height = panel.size.y * (index == 0 ? .23f : .79f);
                Vector3 axle = pivot + Vector3.up * height;
                Part(fixedSide, "Fixed hinge plate " + index, axle + new Vector3(-.048f, 0, .014f),
                    new Vector3(.105f, .105f, .043f), brass);
                Part(fixedSide, "Hinge barrel " + index, axle, new Vector3(.048f, .075f, .048f), brass,
                    primitive: PrimitiveType.Cylinder);
                var moving = Part(movingSide, "Moving hinge strap " + index, axle,
                    new Vector3(.115f, .075f, .018f), brass);
                moving.transform.localPosition = new Vector3(.055f, height, -panel.size.z / 2 - .008f);
                moving.transform.localRotation = Quaternion.identity;
                moving.GetComponent<Renderer>().enabled = true;
            }
        }

        private static void JoinStairAndLanding(Transform root)
        {
            var wood = Existing("Aged timber");
            var topStep = GameObject.Find("Stair 21").GetComponent<Collider>().bounds;
            var landing = GameObject.Find("Upper landing floor").GetComponent<Collider>().bounds;
            var front = landing.min.z;
            var deck = landing.max.y;
            var railHeight = deck + 1.10f;

            // Return the guards to the edge of the slab, leaving a full-width opening at the stairs.
            float right = topStep.max.x + .027f;
            float end = landing.max.x - .035f;
            Part(root, "Landing east return handrail", new Vector3((right + end) / 2, railHeight, front),
                new Vector3(end - right, .09f, .09f), wood);
            Part(root, "Landing east return guard", new Vector3((right + end) / 2, deck + .55f, front),
                new Vector3(end - right, 1.10f, .08f), wood, true).GetComponent<Renderer>().enabled = false;
            Part(root, "Landing east return baluster", new Vector3((right + end) / 2, deck + .55f, front),
                new Vector3(.07f, 1.10f, .07f), wood);

            foreach (float x in new[] { topStep.min.x + .07f, topStep.max.x - .07f })
                Part(root, $"Stair landing newel {(x < topStep.center.x ? "west" : "east")}",
                    new Vector3(x, deck + .51f, front + .028f), new Vector3(.13f, 1.15f, .14f), wood);

            // The original collider carries the floorboards, which only have a visible top face.
            Part(root, "Landing timber deck", landing.center - Vector3.up * .006f,
                new Vector3(landing.size.x, landing.size.y - .012f, landing.size.z), wood);
            Part(root, "Landing front fascia", new Vector3(landing.center.x, deck - .19f, front + .012f),
                new Vector3(landing.size.x, .30f, .07f), wood);

            var posts = Object.FindObjectsByType<Transform>().Where(t => t.name == "Balcony baluster").ToArray();
            foreach (var duplicates in posts.GroupBy(t => t.position).Where(g => g.Count() > 1))
                foreach (var duplicate in duplicates.OrderBy(t => t.GetSiblingIndex()).Skip(1))
                    duplicate.gameObject.SetActive(false);
        }

        private static void GroundPlants(Transform root)
        {
            var green = Foliage("Autumn olive foliage", new Color(.30f, .37f, .18f));
            var ochre = Foliage("Autumn warm foliage", new Color(.39f, .31f, .14f));
            var loam = Foliage("Autumn loam", new Color(.18f, .124f, .082f));
            var yardFloor = GameObject.Find("Floor Yard");
            float yard = yardFloor.GetComponent<Collider>().bounds.max.y;
            float soil = GameObject.Find("Mixed garden soil").GetComponent<Collider>().bounds.max.y;
            var turf = Foliage("Autumn yard turf", new Color(.28f, .31f, .19f));
            turf.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SourceFiles/Textures/Repeating Tiles/Tile_Grass.png"));
            turf.SetTextureScale("_BaseMap", new Vector2(12, 9));
            yardFloor.GetComponent<Renderer>().sharedMaterial = turf;
            EditorUtility.SetDirty(turf);

            foreach (var legacy in Object.FindObjectsByType<Renderer>().Where(r =>
                r.name is "Garden shrub" or "Dense low poly planting" && r.transform.position.z < 28))
                legacy.enabled = false;

            var planters = Object.FindObjectsByType<Renderer>().Where(r => r.enabled && r.name == "Stone planter rim" &&
                r.transform.position.z < 28).OrderBy(r => r.transform.position.x).ToArray();
            var shrubs = Group(root, "Grounded conservatory shrubs");
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ManorGardenPolish.Folder + "/PF_Shrub.prefab");
            for (int row = 0; row < planters.Length; row++)
            {
                Bounds bed = planters[row].bounds;
                Part(shrubs, "Planter loam " + row, new Vector3(bed.center.x, bed.max.y + .011f, bed.center.z),
                    new Vector3(bed.size.x - .12f, .025f, bed.size.z - .12f), loam);
                for (int index = 0; index < 5; index++)
                {
                    string name = $"Conservatory shrub {row}.{index}";
                    var plant = shrubs.Find(name);
                    if (plant == null)
                    {
                        plant = ((GameObject)PrefabUtility.InstantiatePrefab(model, shrubs)).transform;
                        plant.name = name;
                    }
                    plant.localScale = Vector3.one * (1.57f + (index % 3) * .09f);
                    plant.rotation = Quaternion.Euler(0, 47 * index + 21 * row, 0);
                    var target = new Vector3(bed.center.x + (index % 2 == 0 ? -.12f : .12f), bed.max.y - .16f,
                        Mathf.Lerp(bed.min.z + .55f, bed.max.z - .55f, index / 4f));
                    PlacePlant(plant, target, index % 3 == 0 ? ochre : green);
                }
            }

            var yardBases = Group(root, "Yard shrub soil mounds");
            var outside = Object.FindObjectsByType<Transform>().Where(t =>
                t.name is "Planted hedge shrub" or "Garden mixed shrub" or "Flowerbed sage foliage")
                .OrderBy(t => t.position.x).ThenBy(t => t.position.z).ToArray();
            for (int index = 0; index < outside.Length; index++)
            {
                var plant = outside[index];
                bool border = plant.name is "Planted hedge shrub" or "Garden mixed shrub";
                var bounds = PlantBounds(plant);
                float surface = border ? yard : soil;
                PlacePlant(plant, new Vector3(bounds.center.x, surface - (border ? .12f : .04f), bounds.center.z),
                    index % 3 == 0 ? ochre : green);
                if (border)
                    Part(yardBases, "Shrub root loam " + index, new Vector3(bounds.center.x, surface + .022f, bounds.center.z),
                        new Vector3(bounds.size.x * .84f, .27f, bounds.size.z * .84f), loam, primitive: PrimitiveType.Sphere);
            }
        }

        private static Bounds PlantBounds(Transform plant)
        {
            var filters = plant.GetComponentsInChildren<MeshFilter>();
            var bounds = new Bounds(plant.position, Vector3.zero);
            bool first = true;
            foreach (var filter in filters)
            {
                // The joined shrub mesh is tilted. Its renderer AABB can extend below every actual vertex.
                foreach (Vector3 vertex in filter.sharedMesh.vertices)
                {
                    var world = filter.transform.TransformPoint(vertex);
                    if (first) { bounds = new Bounds(world, Vector3.zero); first = false; }
                    else bounds.Encapsulate(world);
                }
            }
            return bounds;
        }

        private static void PlacePlant(Transform plant, Vector3 target, Material foliage)
        {
            Bounds bounds = PlantBounds(plant);
            plant.position += target - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            foreach (var renderer in plant.GetComponentsInChildren<Renderer>())
            {
                var materials = renderer.sharedMaterials;
                for (int index = 0; index < materials.Length; index++)
                    if (materials[index].name is "Garden_Sage" or "Garden_LeafLight" or "Autumn olive foliage" or "Autumn warm foliage")
                        materials[index] = foliage;
                renderer.sharedMaterials = materials;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(plant);
        }

        private static Material Foliage(string name, Color color)
        {
            string path = $"{ManorExterior.Folder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", .06f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Material Existing(string name) =>
            AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/Manor/{name}.mat");

        private static Transform Group(Transform parent, string name)
        {
            var group = parent != null ? parent.Find(name) : GameObject.Find(name)?.transform;
            if (group != null) return group;
            group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        private static GameObject Part(Transform parent, string name, Vector3 position, Vector3 scale, Material material,
            bool solid = false, PrimitiveType primitive = PrimitiveType.Cube)
        {
            var existing = parent.Find(name);
            var part = existing != null ? existing.gameObject : GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.SetPositionAndRotation(position, Quaternion.identity);
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            part.GetComponent<Collider>().enabled = solid;
            return part;
        }
    }
}
