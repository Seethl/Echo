using System.IO;
using Echo.Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Echo.Interactions.Editor
{
    public static class ManorInteractionSceneBuilder
    {
        [MenuItem("Echo/Створити тестову кімнату взаємодії")]
        public static void BuildFromMenu()
        {
            if (File.Exists(GameMenu.GameplayScene))
            {
                Debug.Log("ManorInteractionTest already exists; open it from Assets/Scenes/Testing/ManorInteractionTest.");
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) Build();
        }

        // Also used in the isolated verification project. Never overwrites a scene.
        public static void Build()
        {
            if (File.Exists(GameMenu.GameplayScene)) throw new IOException("Scene already exists; refusing to overwrite.");
            Directory.CreateDirectory(Path.GetDirectoryName(GameMenu.GameplayScene));
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var stone = Material("Material_Checkerboard");
            var wood = Material("Material_SandLumpy");
            var dark = Material("Material_Simple_BlueDark");
            var paper = Material("Material_WhiteGrid");
            var brass = Material("Material_Simple_Yellow");
            var room = new GameObject("Caretaker Room").transform;
            Cube("Room Floor", room, new Vector3(0, -0.15f, 0), new Vector3(8.2f, 0.3f, 6.2f), wood);
            Cube("South Wall", room, new Vector3(0, 1.6f, -3.1f), new Vector3(8.2f, 3.2f, 0.2f), stone);
            Cube("East Wall", room, new Vector3(4.1f, 1.6f, 0), new Vector3(0.2f, 3.2f, 6.2f), stone);
            Cube("Ceiling", room, new Vector3(0, 3.3f, 0), new Vector3(8.2f, 0.2f, 6.2f), dark);
            Cube("Window Wall South", room, new Vector3(-4.1f, 1.6f, -2.1f), new Vector3(0.2f, 3.2f, 1.8f), stone);
            Cube("Window Wall North", room, new Vector3(-4.1f, 1.6f, 2.1f), new Vector3(0.2f, 3.2f, 1.8f), stone);
            Cube("Window Sill", room, new Vector3(-4.1f, 0.6f, 0), new Vector3(0.2f, 1.2f, 2.4f), stone);
            Cube("Window Lintel", room, new Vector3(-4.1f, 2.9f, 0), new Vector3(0.2f, 0.6f, 2.4f), stone);
            Cube("Window Frame", room, new Vector3(-4.1f, 1.9f, 0), new Vector3(0.15f, 1.4f, 0.09f), dark);
            Cube("North Wall Left", room, new Vector3(-1.4f, 1.6f, 3.1f), new Vector3(5.2f, 3.2f, 0.2f), stone);
            Cube("North Wall Right", room, new Vector3(3.4f, 1.6f, 3.1f), new Vector3(1.2f, 3.2f, 0.2f), stone);
            Cube("Door Lintel", room, new Vector3(2, 2.9f, 3.1f), new Vector3(1.6f, 0.6f, 0.2f), stone);

            var corridor = new GameObject("Service Corridor").transform;
            Cube("Corridor Floor", corridor, new Vector3(2, -0.15f, 5.6f), new Vector3(1.6f, 0.3f, 5.2f), wood);
            Cube("Corridor Left", corridor, new Vector3(1.1f, 1.6f, 5.6f), new Vector3(0.2f, 3.2f, 5.2f), stone);
            Cube("Corridor Right", corridor, new Vector3(2.9f, 1.6f, 5.6f), new Vector3(0.2f, 3.2f, 5.2f), stone);
            Cube("Corridor End", corridor, new Vector3(2, 1.6f, 8.3f), new Vector3(2, 3.2f, 0.2f), stone);
            Cube("Corridor Ceiling", corridor, new Vector3(2, 3.3f, 5.6f), new Vector3(2, 0.2f, 5.2f), dark);

            var table = new GameObject("Caretaker Desk").transform;
            Cube("Desktop", table, new Vector3(-1.9f, 0.83f, -0.4f), new Vector3(1.6f, 0.12f, 0.9f), wood);
            foreach (float x in new[] { -2.5f, -1.3f })
                foreach (float z in new[] { -0.7f, -0.1f })
                    Cube("Desk Leg", table, new Vector3(x, 0.4f, z), new Vector3(0.1f, 0.8f, 0.1f), dark);
            var note = Cube("Caretaker Note", table, new Vector3(-1.6f, 0.91f, -0.3f), new Vector3(0.55f, 0.035f, 0.4f), paper);
            note.AddComponent<NoteInteractable>();

            var cupboard = new GameObject("Key Cupboard").transform;
            Cube("Cupboard Back", cupboard, new Vector3(-0.6f, 1f, 2.94f), new Vector3(1.4f, 2f, 0.1f), wood);
            Cube("Cupboard Left", cupboard, new Vector3(-1.3f, 1f, 2.6f), new Vector3(0.1f, 2f, 0.7f), wood);
            Cube("Cupboard Right", cupboard, new Vector3(0.1f, 1f, 2.6f), new Vector3(0.1f, 2f, 0.7f), wood);
            Cube("Cupboard Top", cupboard, new Vector3(-0.6f, 2f, 2.6f), new Vector3(1.4f, 0.1f, 0.7f), wood);
            Cube("Cupboard Base", cupboard, new Vector3(-0.6f, 0.1f, 2.6f), new Vector3(1.4f, 0.2f, 0.7f), wood);
            Cube("Key Shelf", cupboard, new Vector3(-0.6f, 1.05f, 2.6f), new Vector3(1.3f, 0.08f, 0.6f), wood);
            var cupboardDoor = Door("Cupboard Hinge", cupboard, new Vector3(-1.25f, 0, 2.2f), 1.3f, 2f, wood);
            cupboardDoor.openAngle = 105;
            var key = Cube("Service Key", cupboard, new Vector3(-0.6f, 1.17f, 2.54f), new Vector3(0.35f, 0.12f, 0.15f), brass);
            key.AddComponent<KeyPickup>();
            Cube("Key Tooth", key.transform, new Vector3(-0.49f, 1.23f, 2.54f), new Vector3(0.06f, 0.1f, 0.15f), brass);
            var exitDoor = Door("Service Door Hinge", room, new Vector3(1.22f, 0, 3.03f), 1.56f, 2.6f, dark);
            exitDoor.requiredKeyId = "manor.service-passage";
            exitDoor.openAngle = 100;
            var exit = new GameObject("Episode Finish", typeof(BoxCollider), typeof(Rigidbody), typeof(EpisodeExit));
            exit.transform.position = new Vector3(2, 1.2f, 7);
            exit.GetComponent<BoxCollider>().isTrigger = true;
            exit.GetComponent<BoxCollider>().size = new Vector3(1.6f, 2.4f, 0.7f);
            var body = exit.GetComponent<Rigidbody>();
            body.isKinematic = true; body.useGravity = false;

            var moon = new GameObject("Moonlight - no electricity", typeof(Light)).GetComponent<Light>();
            moon.type = LightType.Directional;
            moon.color = new Color(0.61f, 0.73f, 1f); moon.intensity = 1.2f;
            moon.shadows = LightShadows.None;
            moon.transform.rotation = Quaternion.Euler(35, 90, 0);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f, 0.37f, 0.48f);
            RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>("Assets/Skyboxes/SkyNight.mat");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            if (prefab == null) throw new FileNotFoundException("The existing Player prefab is required.");
            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            player.name = "Player";
            player.transform.SetPositionAndRotation(new Vector3(0, 1f, -1.9f), Quaternion.Euler(0, -35, 0));
            var ui = new GameObject("Interaction UI", typeof(InteractionUI)).GetComponent<InteractionUI>();
            var interactor = player.AddComponent<PlayerInteractor>();
            interactor.viewCamera = player.GetComponent<FPSController>().playerCamera.GetComponent<Camera>();
            interactor.ui = ui;
            EditorSceneManager.SaveScene(scene, GameMenu.GameplayScene);
            AssetDatabase.SaveAssets();
            Debug.Log("ECHO_MANOR_SCENE_BUILT: " + GameMenu.GameplayScene);
        }

        private static Material Material(string name) => AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/" + name + ".mat");

        private static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(parent, false);
            // Set world dimensions before parenting beneath scaled visual objects.
            go.transform.position = position;
            var p = parent != null ? parent.lossyScale : Vector3.one;
            go.transform.localScale = new Vector3(scale.x / p.x, scale.y / p.y, scale.z / p.z);
            if (material != null) go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private static HingedDoor Door(string name, Transform parent, Vector3 position, float width, float height, Material material)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false); pivot.position = position;
            Cube("Door Panel", pivot, position + new Vector3(width / 2, height / 2, 0), new Vector3(width, height, 0.12f), material);
            Cube("Handle", pivot, position + new Vector3(width - 0.12f, height * 0.55f, -0.1f), new Vector3(0.07f, 0.12f, 0.12f), Material("Material_Simple_Yellow"));
            var door = pivot.gameObject.AddComponent<HingedDoor>();
            door.hinge = pivot;
            return door;
        }
    }
}
