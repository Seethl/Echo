using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Echo.Interactions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Echo.Manor.Editor
{
    public static class ManorKitchenPolish
    {
        public const string Marker="Kitchen furnishings and under-stair room";
        public const string Folder="Assets/Props/ManorKitchen";
        static Transform root;
        static Material wood,iron,linen;
        static GameObject Find(string name)=>Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).FirstOrDefault(t=>t.name==name)?.gameObject;
        static Material Existing(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Manor/"+name+".mat");
        static void Off(string name){var go=Find(name);if(go!=null)go.SetActive(false);}
        [MenuItem("Echo/Кухня, комірчина та інвентар")]
        public static void ApplyAndSave()
        {
            if(EditorSceneManager.GetActiveScene().path!=ManorPrototypeBuilder.ScenePath)throw new InvalidOperationException("Open ManorPrototype first");
            Apply();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
        public static void Apply()
        {
            PrepareAssets();
            var player=Object.FindAnyObjectByType<PlayerInteractor>();
            var inventory=player.GetComponent<InventorySidebar>()??player.gameObject.AddComponent<InventorySidebar>();
            inventory.catalog=AssetDatabase.LoadAssetAtPath<InventoryCatalog>("Assets/Resources/EchoInventory.asset");EditorUtility.SetDirty(inventory);
            if(PrefabUtility.IsPartOfPrefabInstance(inventory))PrefabUtility.RecordPrefabInstancePropertyModifications(inventory);
            Off("Library ladder");Off("Radio Niche Table");Off("Radio Niche Partition");
            foreach(var t in Object.FindObjectsByType<Transform>())if(t.name=="Radio Niche Table support leg")t.gameObject.SetActive(false);
            // The older modeller placed this table's visible model beside its
            // source collider, under a separate scene root. Hide that copy too.
            foreach(var t in Object.FindObjectsByType<Transform>().Where(t=>t.name.StartsWith("Model - PF_ManorTable")).ToArray())
            {
                if(!t.GetComponentsInChildren<Renderer>().Any(r=>r.enabled))continue;
                if(Vector3.Distance(Bounds(t.gameObject).center,new Vector3(5.3f,.375f,2))<.35f)t.gameObject.SetActive(false);
            }
            var old=Find(Marker);if(old!=null)return;
            root=new GameObject(Marker).transform;wood=Existing("Aged timber");iron=Existing("Dark iron");linen=Existing("Linen and pages");
            Kitchen();Cubby();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
        static Bounds Bounds(GameObject go)=>go.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
        static GameObject Box(string name,Vector3 pos,Vector3 size,Material material,bool solid=true)
            =>ManorSetDress.Box(name,root,pos,size,material,solid);
        static void Sign(string name,string caption,Vector3 position,Quaternion rotation)
        {
            string path=Folder+"/Doorway lettering.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Echo/WorldPropText"));AssetDatabase.CreateAsset(material,path);}
            var sign=new GameObject(name).transform;sign.SetParent(root,false);sign.SetPositionAndRotation(position,rotation);
            var board=GameObject.CreatePrimitive(PrimitiveType.Cube);Object.DestroyImmediate(board.GetComponent<Collider>());board.transform.SetParent(sign,false);board.transform.localScale=new Vector3(1.25f,.30f,.035f);board.GetComponent<Renderer>().sharedMaterial=wood;
            var text=new GameObject("Doorway lettering",typeof(TextMesh)).GetComponent<TextMesh>();text.transform.SetParent(sign,false);text.transform.localPosition=new Vector3(0,0,-.023f);text.text=caption;text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=64;text.characterSize=.025f;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(.84f,.76f,.58f);text.GetComponent<Renderer>().sharedMaterial=material;text.gameObject.AddComponent<WorldPropTextBinding>().Refresh();
        }
        static GameObject Model(string type,string name,Vector3 bottom,float yaw=0)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/PF_"+type+".prefab"));go.name=name;go.transform.SetParent(root,true);go.transform.rotation=Quaternion.Euler(0,yaw,0)*go.transform.rotation;
            var b=Bounds(go);go.transform.position+=new Vector3(bottom.x-b.center.x,bottom.y-b.min.y,bottom.z-b.center.z);return go;
        }
        static void Kitchen()
        {
            var old=Find("Kitchen work and supplies");
            foreach(Transform child in old.transform)
            {
                if(child.name=="Paneled cabinet"||child.name=="Book and storage shelving"||child.name=="Kitchen sink rim"||child.name=="Sink dark basin")child.gameObject.SetActive(false);
            }
            Model("SinkCounter","Kitchen sink counter",new Vector3(27,0,.66f));
            Model("DishDresser","Kitchen crockery dresser",new Vector3(29.35f,0,.65f));
            Model("Icebox","Kitchen unpowered refrigerator",new Vector3(30.8f,0,.65f));
            foreach(var bottle in Object.FindObjectsByType<RemedyBottle>())
            {
                var p=bottle.transform.position;
                if(p.z>1.3f||p.x<25.3f||p.x>28.6f)continue;
                var renderers=bottle.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();if(renderers.Length==0)continue;
                float min=renderers.Min(r=>r.bounds.min.y);bottle.transform.position+=Vector3.up*(.905f-min);
            }
            // Deliberate small clusters, supported by the counter, shelf, island,
            // and a low crate. Keep the sink, spoon and medicine work area clear.
            string[] types={"FlourCarton","TeaTin","PreserveTin"};
            Vector3[] points={new Vector3(25.9f,.905f,.58f),new Vector3(26.2f,.905f,.47f),new Vector3(28.25f,.905f,.55f),
                new Vector3(28.95f,1.866f,.88f),new Vector3(29.70f,1.866f,.88f),new Vector3(32.15f,.852f,5.54f),new Vector3(32.43f,.852f,5.55f),
                new Vector3(35.05f,.65f,6.7f),new Vector3(34.90f,.65f,6.78f)};
            for(int i=0;i<points.Length;i++)Model(types[i%3],"Stored food "+i,points[i],(i%3-1)*8);
            var moon=new GameObject("Kitchen worktop reflected moon",typeof(Light)).GetComponent<Light>();moon.transform.SetParent(root,false);moon.transform.SetPositionAndRotation(new Vector3(28.8f,2.8f,3),Quaternion.LookRotation(new Vector3(28.4f,.6f,.65f)-new Vector3(28.8f,2.8f,3)));moon.type=LightType.Spot;moon.spotAngle=95;moon.innerSpotAngle=65;moon.range=5.8f;moon.intensity=2.2f;moon.color=new Color(.58f,.68f,.93f);moon.shadows=LightShadows.Soft;
        }
        static void Cubby()
        {
            var stairs=Find("West hall staircase - 0.20m rise, 0.50m tread");
            for(int i=14;i<22;i++)
            {
                var tread=stairs.transform.Find("Stair "+i);float top=(i+1)*.2f;
                tread.localScale=new Vector3(2.1f,.16f,.51f);tread.position=new Vector3(6.3f,top-.08f,-6.35f+i*.5f);
            }
            Box("Cubby west wall",new Vector3(5.25f,1.2f,2.4f),new Vector3(.10f,2.4f,3.8f),wood);
            Box("Cubby south wall",new Vector3(6.3f,1.2f,.52f),new Vector3(2.1f,2.4f,.12f),wood);
            Box("Cubby north wall",new Vector3(6.3f,1.2f,4.27f),new Vector3(2.1f,2.4f,.12f),wood);
            Box("Cubby ceiling",new Vector3(6.3f,2.46f,2.4f),new Vector3(2.1f,.12f,3.8f),wood);
            Box("Cubby front south",new Vector3(7.37f,1.2f,.99f),new Vector3(.12f,2.4f,.82f),wood);
            Box("Cubby front north",new Vector3(7.37f,1.2f,3.37f),new Vector3(.12f,2.4f,1.68f),wood);
            Box("Cubby door lintel",new Vector3(7.37f,2.28f,1.98f),new Vector3(.16f,.24f,1.16f),wood);
            var doorRoot=new GameObject("Under-stair door");doorRoot.transform.SetParent(root,false);doorRoot.transform.SetPositionAndRotation(new Vector3(7.37f,0,1.42f),Quaternion.Euler(0,-90,0));
            var hinge=new GameObject("Edge hinge").transform;hinge.SetParent(doorRoot.transform,false);
            var door=doorRoot.AddComponent<HingedDoor>();door.hinge=hinge;door.openAngle=-90;door.degreesPerSecond=115;door.checkpointId="door.under-stair";
            var blocker=doorRoot.AddComponent<UnityEngine.AI.NavMeshObstacle>();blocker.shape=UnityEngine.AI.NavMeshObstacleShape.Box;blocker.center=new Vector3(.55f,1.07f,0);blocker.size=new Vector3(1.10f,2.14f,.14f);blocker.carving=true;blocker.carveOnlyStationary=false;door.navigationBlocker=blocker;
            var originalAudio=Object.FindObjectsByType<Echo.Audio.HingedDoorAudio>().FirstOrDefault(a=>a.openingClip!=null);
            if(originalAudio!=null){var audio=doorRoot.AddComponent<Echo.Audio.HingedDoorAudio>();audio.openingClip=originalAudio.openingClip;audio.closingClip=originalAudio.closingClip;audio.latchClip=originalAudio.latchClip;audio.playbackVolume=.55f;}
            var leaf=GameObject.CreatePrimitive(PrimitiveType.Cube);leaf.name="Cubby door leaf";leaf.transform.SetParent(hinge,false);leaf.transform.localPosition=new Vector3(.55f,1.07f,0);leaf.transform.localScale=new Vector3(1.10f,2.14f,.09f);leaf.GetComponent<Renderer>().sharedMaterial=wood;
            var handle=GameObject.CreatePrimitive(PrimitiveType.Cube);handle.name="Cubby door latch";Object.DestroyImmediate(handle.GetComponent<Collider>());handle.transform.SetParent(hinge,false);handle.transform.localPosition=new Vector3(.95f,.95f,-.065f);handle.transform.localScale=new Vector3(.12f,.035f,.04f);handle.GetComponent<Renderer>().sharedMaterial=iron;
            // Narrow cot, trunk, scarf and round glasses: an optional visual nod.
            Box("Cubby cot base",new Vector3(5.75f,.16f,3.01f),new Vector3(.67f,.32f,1.88f),wood);
            Box("Cubby old mattress",new Vector3(5.75f,.36f,3.01f),new Vector3(.65f,.12f,1.84f),linen,false);
            Box("Cubby pillow",new Vector3(5.75f,.46f,3.66f),new Vector3(.49f,.10f,.34f),linen,false);
            Box("Cubby blanket",new Vector3(5.75f,.435f,2.79f),new Vector3(.66f,.025f,1.05f),Existing("Burgundy rug"),false);
            Box("Cubby old trunk",new Vector3(6.55f,.22f,3.92f),new Vector3(.62f,.44f,.44f),wood);
            for(int i=0;i<6;i++)Box("Cubby striped scarf",new Vector3(6.5f,.456f,3.77f+i*.055f),new Vector3(.18f,.018f,.055f),i%2==0?Existing("Burgundy rug"):Existing("Dull brass"),false);
            foreach(float x in new[]{6.35f,6.44f})
            {
                var ring=ManorSetDress.Shape("Round spectacle lens",root,new Vector3(x,.46f,3.96f),new Vector3(.085f,.006f,.085f),iron,PrimitiveType.Cylinder);ring.GetComponent<Renderer>().sharedMaterial=iron;
                var inset=ManorSetDress.Shape("Lens inset",root,new Vector3(x,.467f,3.96f),new Vector3(.063f,.003f,.063f),Existing("Silver utensils"),PrimitiveType.Cylinder);
            }
            Box("Spectacle bridge",new Vector3(6.395f,.465f,3.96f),new Vector3(.04f,.012f,.012f),iron,false);
            var note=Box("Cubby domestic note",new Vector3(6.71f,.449f,3.95f),new Vector3(.20f,.006f,.16f),linen).AddComponent<ClueNote>();
            note.clueId="cubby.housekeeping";note.title="ЗАПИСКА НА СКРИНІ";note.text="Не залишай черевики на сходах. Ковдру я поклала на ліжко.\n\nЯкщо знову почуєш скрип — це старе дерево, а не хтось за дверима.\n\nМ.";
            var light=new GameObject("Cubby reflected moon",typeof(Light)).GetComponent<Light>();light.transform.SetParent(root,false);light.transform.position=new Vector3(6.7f,2.05f,2.3f);light.type=LightType.Point;light.range=2.5f;light.intensity=.55f;light.color=new Color(.51f,.61f,.85f);light.shadows=LightShadows.Soft;
        }
        [Serializable] class MaterialSpecs {public Spec[] materials;}
        [Serializable] class Spec {public string name;public float[] color;public float metallic,roughness;}
        static void PrepareAssets()
        {
            var specs=JsonUtility.FromJson<MaterialSpecs>(File.ReadAllText(Folder+"/palette.json"));
            foreach(var spec in specs.materials)
            {
                string path=Folder+"/"+spec.name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
                mat.SetColor("_BaseColor",new Color(spec.color[0],spec.color[1],spec.color[2]).gamma);mat.SetFloat("_Metallic",spec.metallic);mat.SetFloat("_Smoothness",1-spec.roughness);EditorUtility.SetDirty(mat);
            }
            foreach(string type in new[]{"Icebox","SinkCounter","DishDresser","FlourCarton","TeaTin","PreserveTin"})
            {
                string path=Folder+"/SM_"+type+".fbx";var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.importAnimation=false;importer.isReadable=true;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
                foreach(var mat in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>())importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),mat.name),AssetDatabase.LoadAssetAtPath<Material>(Folder+"/"+mat.name+".mat"));
                importer.SaveAndReimport();
                var temp=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                var front=temp.GetComponentsInChildren<Renderer>().FirstOrDefault(r=>r.name.IndexOf(type=="Icebox"?"handle":type=="SinkCounter"?"pull":"knob",StringComparison.OrdinalIgnoreCase)>=0);
                if(front!=null&&front.bounds.center.z<Bounds(temp).center.z)temp.transform.Rotate(0,180,0);
                if(type=="Icebox"||type=="SinkCounter"||type=="DishDresser")
                {
                    var collider=temp.AddComponent<BoxCollider>();var b=Bounds(temp);collider.center=temp.transform.InverseTransformPoint(b.center);collider.size=b.size;
                    if(type=="SinkCounter"){collider.center=new Vector3(0,.44f,0);collider.size=new Vector3(3.2f,.88f,.75f);}
                }
                PrefabUtility.SaveAsPrefabAsset(temp,Folder+"/PF_"+type+".prefab");Object.DestroyImmediate(temp);
            }
            var paper=(TextureImporter)AssetImporter.GetAtPath("Assets/Resources/EchoPaper.png");paper.textureType=TextureImporterType.Sprite;paper.spriteImportMode=SpriteImportMode.Single;paper.spriteBorder=Vector4.one*45;paper.mipmapEnabled=false;paper.SaveAndReimport();
            foreach(string path in Directory.GetFiles("Assets/Resources/EchoInventoryIcons","*.png"))
            {var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.SaveAndReimport();}
            string catalogPath="Assets/Resources/EchoInventory.asset";var catalog=AssetDatabase.LoadAssetAtPath<InventoryCatalog>(catalogPath);
            if(catalog==null){catalog=ScriptableObject.CreateInstance<InventoryCatalog>();AssetDatabase.CreateAsset(catalog,catalogPath);}
            string[][] definitions={
                new[]{"portable.radio","Радіо","R — налаштувати, F — живлення.","radio"},
                new[]{"manor.service-passage","Службовий ключ","Відмикає двері зі службової кімнати.","key"},
                new[]{"tool.winch","Руків’я лебідки","Встановлюється в лебідку на кухні.","crank"},
                new[]{"remedy.study-key","Аптечний ключ","Відмикає аптечну шафку на кухні.","key"},
                new[]{"remedy.base","Основа мікстури","Додається до посудини на кухні.","base"},
                new[]{"remedy.spoon","Срібна ложечка","Потрібна, щоб розмішати мікстуру.","spoon"},
                new[]{"tool.crowbar","Лом","Допоможе відтиснути заклинений засув.","crowbar"},
                new[]{"remedy.flower","Бліда квітка","Інгредієнт із рецепта.","flower"},
                new[]{"remedy.bottle","Порожня пляшечка","У неї можна набрати готову мікстуру.","bottle"},
                new[]{"remedy.potion","Пляшечка з ліками","Ліки слід перелити в чашку мешканця.","potion"},
                new[]{"tool.cutters","Болторіз","Інструмент для металевих ланцюгів.","cutters"}};
            catalog.items=definitions.Select(d=>new InventoryCatalog.Item{id=d[0],displayName=d[1],hint=d[2],icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/EchoInventoryIcons/"+d[3]+".png")}).ToArray();EditorUtility.SetDirty(catalog);
        }
    }
}
