using System;
using System.Linq;
using System.Collections.Generic;
using Echo.Interactions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
using static Echo.Manor.Editor.ManorSetDress;

namespace Echo.Manor.Editor
{
    // Assembles the ten concept locations around the team's existing interaction and progression systems.
    public static class ManorConceptUpgrade
    {
        private const string Marker="Manor concept locations v3 - main 129c483";
        private static Transform root;
        private static int modelCount;
        private static Mesh leafMesh;
        [MenuItem("Echo/Зібрати локації за low-poly концептами")]
        public static void ApplyAndSave()
        {
            if(EditorSceneManager.GetActiveScene().path!=ManorPrototypeBuilder.ScenePath)throw new InvalidOperationException("Open ManorPrototype first.");
            Apply();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
        public static void Apply()
        {
            if(GameObject.Find(Marker)!=null)return;
            Materials();root=Group(Marker);modelCount=0;
            ModelizeFurniture();ModelizeSmallProps();SetSurfaces();MakeHallSeating();MakeRadio();MakeKitchen();MakeGardenPuzzle();MakeUpperGallery();MakeDetails();EnhanceScene();PolishScene();
            foreach(var label in Object.FindObjectsByType<TextMesh>())if(label.GetComponent<WorldLabelVisibility>()==null)label.gameObject.AddComponent<WorldLabelVisibility>();
            foreach(var readout in Object.FindObjectsByType<MechanismReadout>())if(readout.barrier!=null&&readout.barrier.toolId=="tool.cutters")readout.gameObject.SetActive(false);
            var enemy=Object.FindAnyObjectByType<EnemyInvestigator>(FindObjectsInactive.Include);enemy.gameObject.SetActive(true);enemy.enabled=true;enemy.GetComponent<EnemyCatchPlayer>().enabled=true;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());Debug.Log("CONCEPT_LOCATIONS_BUILT models="+modelCount+"; resident enabled");
        }
        private static string Table(bool desk=false)=>"Assets/Props/ManorTables/PF_Manor"+(desk?"Desk":"Table")+".prefab";
        private static string Seat(string type)=>"Assets/Props/ManorSeating/PF_Manor"+type+".prefab";
        private static string Store(string type)=>"Assets/Props/ManorStorage/PF_Manor"+type+".prefab";
        private static string Decor(string type)=>"Assets/Props/ManorDecor/PF_Decor"+type+".prefab";
        private static Bounds LocalBounds(Transform parent,IEnumerable<MeshFilter> filters)
        {
            bool first=true;Bounds b=new Bounds();
            foreach(var f in filters)
            {
                var r=f.GetComponent<Renderer>();if(f.sharedMesh==null||r==null||!r.enabled||!f.gameObject.activeInHierarchy)continue;
                var own=f.GetComponentInParent<Interactable>();if(own!=null&&own.transform!=parent)continue;
                var bounds=f.sharedMesh.bounds;
                for(int i=0;i<8;i++)
                {
                    var point=parent.InverseTransformPoint(f.transform.TransformPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));
                    if(first){b=new Bounds(point,Vector3.zero);first=false;}else b.Encapsulate(point);
                }
            }
            return b;
        }
        private static GameObject FitPrefab(string path,Transform parent,Vector3 centre,Vector3 size,float yaw=0,bool openCabinet=false)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(prefab==null)throw new Exception("Missing main prefab: "+path);
            var normalized=new GameObject("Model - "+System.IO.Path.GetFileNameWithoutExtension(path));
            var visual=Object.Instantiate(prefab);visual.name="Imported asset - replace visually";visual.transform.SetParent(normalized.transform,false);visual.transform.localPosition=Vector3.zero;visual.transform.localRotation=Quaternion.Euler(0,yaw,0);
            foreach(var c in visual.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
            foreach(var script in visual.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(script);
            if(openCabinet)foreach(var t in visual.GetComponentsInChildren<Transform>())if(t.name.StartsWith("DoorHinge_"))t.gameObject.SetActive(false);
            var bounds=VisibleBounds(normalized);normalized.transform.localScale=new Vector3(size.x/Mathf.Max(.001f,bounds.size.x),size.y/Mathf.Max(.001f,bounds.size.y),size.z/Mathf.Max(.001f,bounds.size.z));
            bounds=VisibleBounds(normalized);normalized.transform.position=centre-bounds.center;normalized.transform.SetParent(parent,false);modelCount++;
            var actual=LocalBounds(parent,normalized.GetComponentsInChildren<MeshFilter>());
            if(Vector3.Distance(actual.center,centre)>.015f||(actual.size-size).magnitude>.03f)throw new Exception("Fitted model bounds mismatch: "+path);
            return normalized;
        }
        private static Bounds VisibleBounds(GameObject go)
        {
            bool first=true;Bounds b=new Bounds();
            foreach(var f in go.GetComponentsInChildren<MeshFilter>())
            {
                var r=f.GetComponent<Renderer>();if(r==null||!r.enabled||f.sharedMesh==null)continue;var s=f.sharedMesh.bounds;
                for(int i=0;i<8;i++){var p=f.transform.TransformPoint(s.center+Vector3.Scale(s.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);}
            }
            return b;
        }
        private static void Replace(GameObject go,string path,float yaw=180,bool open=false)
        {
            var filters=go.GetComponentsInChildren<MeshFilter>();var b=LocalBounds(go.transform,filters);if(b.size.sqrMagnitude<.001f)return;
            foreach(var f in filters){var r=f.GetComponent<Renderer>();var own=f.GetComponentInParent<Interactable>();if(r!=null&&(own==null||own.gameObject==go))r.enabled=false;}
            FitPrefab(path,go.transform,b.center,b.size,yaw,open);
        }
        private static void ModelizeFurniture()
        {
            foreach(var t in Object.FindObjectsByType<Transform>().ToArray())
            {
                if(t.GetComponent<Interactable>()!=null)continue;
                if(t.name=="Chair")Replace(t.gameObject,Seat("Chair"),180);
                else if(t.name=="Paneled cabinet"||t.name=="Study wardrobe")Replace(t.gameObject,Store("Wardrobe"));
                else if(t.name=="Book and storage shelving")Replace(t.gameObject,Store("Bookcase"));
                else if(t.name=="Upholstered bench")Replace(t.gameObject,Seat("Sofa"));
                else if(t.name=="Travel chest")Replace(t.gameObject,Decor("Suitcase"),0);
                else if(t.name=="Storage crate")Replace(t.gameObject,Decor("Crate"),0);
                else if(t.name.Contains("table",StringComparison.OrdinalIgnoreCase)||t.name.Contains("desk",StringComparison.OrdinalIgnoreCase)||t.name.Contains("washstand",StringComparison.OrdinalIgnoreCase)||t.name=="Gallery console")
                {
                    if(t.Cast<Transform>().Any(c=>c.name=="Table top"))Replace(t.gameObject,Table(t.name.Contains("writing",StringComparison.OrdinalIgnoreCase)||t.name.Contains("desk",StringComparison.OrdinalIgnoreCase)),180);
                }
            }
            foreach(string name in new[]{"Service Cupboard","Library Cache","Kitchen apothecary cabinet"}){var go=GameObject.Find(name);if(go!=null)Replace(go,Store("Wardrobe"),180,true);}
            foreach(string name in new[]{"Gallery Entry Cover","Kitchen Cover"}){var go=GameObject.Find(name);if(go!=null)Replace(go,Store("Wardrobe"));}
            var desk=GameObject.Find("Caretaker Desk");var b=desk.GetComponent<Collider>().bounds;
            desk.GetComponent<Renderer>().enabled=false;foreach(var r in Object.FindObjectsByType<Renderer>().Where(r=>r.name=="Desk Leg"))r.enabled=false;
            FitPrefab(Table(true),root,new Vector3(b.center.x,.44f,b.center.z),new Vector3(b.size.x,.88f,b.size.z),180);
            foreach(string name in new[]{"Garden Tool Bench","Shed Tool Table","Radio Niche Table"})
            {
                var go=GameObject.Find(name);if(go==null)continue;var bounds=go.GetComponent<Collider>().bounds;go.GetComponent<Renderer>().enabled=false;
                foreach(var r in Object.FindObjectsByType<Renderer>().Where(r=>r.name.StartsWith(name+" support")))r.enabled=false;
                FitPrefab(Table(),root,new Vector3(bounds.center.x,bounds.max.y/2,bounds.center.z),new Vector3(bounds.size.x,bounds.max.y,bounds.size.z),180);
            }
        }
        private static void ModelizeSmallProps()
        {
            var mapping=new Dictionary<string,string>{{"Book","Book"},{"Storage bottle","Bottle"},{"Plant pot","Jar"},{"Cup","Mug"},{"Plate","Plate"},{"Wrapped sweet (decoration)","Parcel"}};
            foreach(var t in Object.FindObjectsByType<Transform>().ToArray())
            {
                if(!mapping.TryGetValue(t.name,out string kind))continue;
                if(t.GetComponentInParent<KeyPickup>()!=null||t.GetComponentInParent<PlantGathering>()!=null)continue;
                if(t.GetComponent<Renderer>()!=null)Replace(t.gameObject,Decor(kind),0);
            }
            foreach(var r in Object.FindObjectsByType<Renderer>())if(r.name=="Cup handle"||r.name=="Bottle cork")r.enabled=false;
        }
        private static void SetSurfaces()
        {
            foreach(var f in Object.FindObjectsByType<MeshFilter>().Where(f=>f.name.StartsWith("Floorboards ")))
            {
                var mesh=f.sharedMesh;var vertices=mesh.vertices;var uv=new Vector2[vertices.Length];for(int i=0;i<uv.Length;i++)uv[i]=new Vector2(vertices[i].x/2,vertices[i].z/2);mesh.uv=uv;EditorUtility.SetDirty(mesh);
                string type=f.name.Contains("Hall")||f.name.Contains("Library")||f.name.Contains("Study")?"ParquetFloor":"Floor";
                f.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Props/ManorSurfaces/M_Manor"+type+".mat");
            }
            foreach(var r in Object.FindObjectsByType<MeshRenderer>())
            {
                if(r.name.StartsWith("Ceiling ")||r.name=="High hall ceiling"||r.name=="Study ceiling")r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Props/ManorSurfaces/M_ManorCeiling.mat");
                if(r.name.Contains("rug",StringComparison.OrdinalIgnoreCase)&&r.GetComponent<SurfaceType>()!=null)r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Props/ManorSurfaces/M_ManorRug.mat");
            }
            foreach(string name in new[]{"Kitchen","Conservatory"})
            {var go=GameObject.Find("Floor "+name);var original=AssetDatabase.LoadAssetAtPath<Material>("Assets/Props/ManorSurfaces/M_ManorStoneFloor.mat");var mat=new Material(original);mat.name=name+" stone mapping v3";mat.SetTextureScale("_BaseMap",new Vector2(go.transform.lossyScale.x/2,go.transform.lossyScale.z/2));AssetDatabase.CreateAsset(mat,MaterialsFolder+"/"+mat.name+".mat");go.GetComponent<Renderer>().sharedMaterial=mat;}
        }
        private static GameObject StaticModel(string path,string name,Vector3 feet,Vector3 size,float yaw=0)
        {
            var go=Group(name,root);go.position=feet;var bounds=go.gameObject.AddComponent<BoxCollider>();bounds.center=Vector3.up*size.y/2;bounds.size=size;
            FitPrefab(path,go,Vector3.up*size.y/2,size,yaw);return go.gameObject;
        }
        private static void MakeHallSeating()
        {
            var cabinet=GameObject.Find("Hall Central Cover");cabinet.transform.position=new Vector3(12.3f,.9f,.3f);cabinet.transform.localScale=new Vector3(2.6f,1.8f,.25f);cabinet.transform.rotation=Quaternion.Euler(0,25,0);cabinet.GetComponent<Renderer>().sharedMaterial=timber;
            foreach(var r in Object.FindObjectsByType<Renderer>())if(r.name=="Central cabinet panel"||r.name=="Central cabinet pull")r.enabled=false;
            StaticModel(Seat("Sofa"),"Hall olive sofa",new Vector3(15.4f,0,-2.7f),new Vector3(2.65f,1.05f,1.0f),0);
            StaticModel(Seat("Armchair"),"Hall reading armchair",new Vector3(18,0,-.5f),new Vector3(.88f,1.05f,.9f),-80);
            StaticModel(Seat("Armchair"),"Hall west armchair",new Vector3(13.1f,0,-4.1f),new Vector3(.88f,1.05f,.9f),30);
            StaticModel(Table(),"Hall low coffee table",new Vector3(15.5f,0,-.7f),new Vector3(1.25f,.46f,.65f),0);
            FitPrefab(Decor("BookStack"),root,new Vector3(15.2f,.52f,-.7f),new Vector3(.24f,.12f,.22f));
            FitPrefab(Decor("Mug"),root,new Vector3(15.8f,.52f,-.7f),new Vector3(.13f,.12f,.12f));
        }
        private static void MakeRadio()
        {
            var radio=Object.FindAnyObjectByType<Echo.Radio.PortableRadio>();var p=radio.transform;
            var face=Box("Radio cream dial face",null,Vector3.zero,new Vector3(.82f,.72f,.025f),linen,false);face.transform.SetParent(p,false);face.transform.localPosition=new Vector3(0,0,-.52f);
            for(int i=0;i<7;i++){var grille=Box("Radio speaker grille",null,Vector3.zero,new Vector3(.025f,.50f,.025f),dark,false);grille.transform.SetParent(p,false);grille.transform.localPosition=new Vector3(-.32f+i*.07f,0,-.55f);}
            foreach(float y in new[]{-.23f,.23f}){var knob=Shape("Radio tuning knob",null,Vector3.zero,new Vector3(.18f,.18f,.06f),gold,PrimitiveType.Cube);knob.transform.SetParent(p,false);knob.transform.localPosition=new Vector3(.3f,y,-.55f);}
        }
        private static void MakeKitchen()
        {
            var note=GameObject.Find("Remedy Recipe").GetComponent<ClueNote>();note.onReadFlag="remedy.recipe.read";
            Object.FindAnyObjectByType<TreatmentWorkbench>().recipeFlag="remedy.recipe.read";
            FitPrefab(Decor("Kettle"),root,new Vector3(34.6f,1.08f,1),new Vector3(.34f,.32f,.3f));
            FitPrefab(Decor("Jar"),root,new Vector3(32.8f,.99f,6.7f),new Vector3(.15f,.23f,.15f));
            var hoist=Group("Service grille manual hoist - placeholder replace as one model",root);
            Box("Hoist wall upright",hoist,new Vector3(34.1f,1.6f,13.75f),new Vector3(.18f,3.2f,.18f),timber);
            Box("Hoist crossbeam",hoist,new Vector3(32.8f,3.15f,13.75f),new Vector3(2.8f,.16f,.18f),timber);
            Box("Hoist visible cable",hoist,new Vector3(33.6f,2.2f,13.68f),new Vector3(.025f,1.7f,.025f),dark,false);
            Box("Counterweight",hoist,new Vector3(34.9f,1.1f,13.5f),new Vector3(.42f,.7f,.35f),stone);
        }
        private static void MakeGardenPuzzle()
        {
            var old=GameObject.Find("Remedy Flower");old.SetActive(false);
            var oldPot=GameObject.Find("Remedy flower pot");if(oldPot!=null)oldPot.SetActive(false);
            MakeFlowerBed("Moonflower medicinal bed",new Vector3(19.2f,0,33.2f),"moonflower",5,bloom,"П’ять блідих пелюсток і супротивні листки.");
            MakeFlowerBed("Blue six-petal bed",new Vector3(16.2f,0,34.0f),"blue-star",6,AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Material_Simple_BlueDark.mat"),"Шість синіх пелюсток, вузькі листки.");
            MakeFlowerBed("Yellow three-petal bed",new Vector3(19.2f,0,38.0f),"yellow-bell",3,gold,"Три жовті пелюстки, листки по черзі.");
            var herb=Box("Garden Herbarium",root,new Vector3(31.05f,.86f,40.8f),new Vector3(.28f,.035f,.25f),linen).AddComponent<ClueNote>();herb.clueId="garden.herbarium";herb.onReadFlag="garden.herbarium.read";herb.title="ГЕРБАРІЙ САДІВНИКА";
            herb.text="Для мікстури: п’ять блідих пелюсток, два листки навпроти одне одного. Клумба надворі.\nСині квіти мають шість пелюсток; жовті — три. Вони не підходять.\nРослини можна оглядати; помилка не псує зібране.";
            var oldNote=GameObject.Find("Remedy Recipe").GetComponent<ClueNote>();oldNote.text="Основа — у шафці на кухні; ключ — нагорі.\nКвітка: п’ять блідих пелюсток, супротивні листки. Клумби надворі; гербарій — у садівничій.\nНалий основу, додай квітку, розмішай срібною ложечкою з холу.\nЗалиш у його чашці та відійди.";
            var fountain=Group("Garden basin placeholder",root);Shape("Octagonal basin",fountain,new Vector3(18,.45f,42),new Vector3(2.4f,.45f,2.4f),stone,PrimitiveType.Cylinder,true);Shape("Basin dark water",fountain,new Vector3(18,.91f,42),new Vector3(2.05f,.01f,2.05f),dark,PrimitiveType.Cylinder);Box("Basin centre",fountain,new Vector3(18,1.1f,42),new Vector3(.45f,.6f,.45f),stone);
            foreach(var t in Object.FindObjectsByType<Transform>().Where(t=>t.name=="Five pale petals"&&t.GetComponentInParent<PlantGathering>()==null).ToArray())t.GetComponent<Renderer>().sharedMaterial=leaf;
        }
        private static void MakeFlowerBed(string name,Vector3 p,string species,int petals,Material colour,string description)
        {
            var bed=Group(name,root);Box("Raised bed timber",bed,p+Vector3.up*.30f,new Vector3(1.65f,.6f,1.35f),timber);Box("Bed soil",bed,p+Vector3.up*.61f,new Vector3(1.5f,.025f,1.2f),dark,false);
            var plant=Group(species=="moonflower"?"Remedy Flower":"Inspect plant "+species,bed);plant.position=p+Vector3.up*.85f;
            var collider=plant.gameObject.AddComponent<BoxCollider>();collider.size=new Vector3(.38f,.55f,.38f);
            var gather=plant.gameObject.AddComponent<PlantGathering>();gather.speciesId=species;gather.description=description;
            Box("Plant stem",plant,plant.position+Vector3.up*.04f,new Vector3(.025f,.45f,.025f),leaf,false);
            for(int i=0;i<petals;i++){float a=Mathf.PI*2*i/petals;LowPoly("Flower petal",plant,plant.position+new Vector3(Mathf.Cos(a)*.11f,.30f,Mathf.Sin(a)*.11f),new Vector3(.15f,.05f,.15f),colour);}
            foreach(float sign in new[]{-1f,1f})LowPoly("Opposite leaf",plant,plant.position+new Vector3(sign*.11f,0,0),new Vector3(.23f,.035f,.08f),leaf);
            for(int i=0;i<4;i++)LowPoly("Bed foliage",bed,p+new Vector3(i%2==0?-.55f:.55f,.78f,i<2?-.4f:.4f),new Vector3(.35f,.32f,.3f),leaf);
        }
        private static void MakeUpperGallery()
        {
            StaticModel(Seat("Armchair"),"Upper gallery reading chair",new Vector3(1.3f,4.4f,6.45f),new Vector3(.7f,.95f,.7f),90);
            FitPrefab(Decor("RolledRug"),root,new Vector3(3.2f,4.65f,6.95f),new Vector3(.85f,.25f,.25f));
            FitPrefab(Decor("Portrait"),root,new Vector3(3.75f,6.0f,6.55f),new Vector3(.12f,.7f,.50f),90);
            FitPrefab(Decor("Suitcase"),root,new Vector3(-.1f,4.58f,7.0f),new Vector3(.65f,.36f,.4f));
            StaticModel(Seat("Armchair"),"Study olive reading chair",new Vector3(-7.1f,4.4f,1.25f),new Vector3(.9f,1.0f,.85f),30);
        }
        private static void MakeDetails()
        {
            foreach(var f in Object.FindObjectsByType<MeshFilter>())
                if(f.name=="Foliage leaf"||f.name=="Garden shrub"||f.name=="Five pale petals")f.sharedMesh=LeafMesh();
            foreach(var p in new[]{new Vector3(-16.1f,.95f,-9.7f),new Vector3(18.9f,.94f,-4.2f),new Vector3(31.5f,.97f,40.8f)})
                FitPrefab(Decor("Candle"),root,p+Vector3.up*.18f,new Vector3(.12f,.36f,.12f));
            foreach(var p in new[]{new Vector3(-11.5f,.3f,-11.3f),new Vector3(2.7f,.3f,-10.8f),new Vector3(22,.3f,20.8f),new Vector3(35.2f,.3f,40.7f)})
                FitPrefab(Decor("Parcel"),root,p,new Vector3(.5f,.6f,.45f));
        }
        public static void RefineAndSave()
        {
            Materials();root=GameObject.Find(Marker).transform;EnhanceScene();PolishScene();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
        private static void EnhanceScene()
        {
            const string detail="Concept room details v4";if(GameObject.Find(detail)!=null)return;var details=Group(detail,root);
            foreach(string name in new[]{"Library Long Shelf","Library North Shelf"})
            {
                var go=GameObject.Find(name);go.GetComponent<Renderer>().enabled=false;
                bool longShelf=name.Contains("Long");int count=longShelf?3:2;
                for(int i=0;i<count;i++)
                    FitPrefab(Store("Bookcase"),go.transform,longShelf?new Vector3(0,0,(i-1)*.325f):new Vector3((i-.5f)*.495f,0,0),longShelf?new Vector3(1,1,.315f):new Vector3(.485f,1,1),longShelf?90:180);
                foreach(var child in go.GetComponentsInChildren<Transform>())if(child.name=="Shelf book spine"||child.name=="Shelf horizontal board")child.gameObject.SetActive(false);
            }
            foreach(string name in new[]{"West Plant Bed","East Plant Bed","Yard Hedge West","Yard Hedge North"})
            {
                var go=GameObject.Find(name);var b=go.GetComponent<Collider>().bounds;go.GetComponent<Renderer>().enabled=false;
                Box("Stone planter rim",details,new Vector3(b.center.x,.28f,b.center.z),new Vector3(b.size.x,.56f,b.size.z),stone);
                int nx=Mathf.Max(2,Mathf.CeilToInt(b.size.x/.9f)),nz=Mathf.Max(2,Mathf.CeilToInt(b.size.z/.9f));
                for(int x=0;x<nx;x++)for(int z=0;z<nz;z++)LowPoly("Dense low poly planting",details,new Vector3(Mathf.Lerp(b.min.x+.35f,b.max.x-.35f,(float)x/(nx-1)),1.25f,Mathf.Lerp(b.min.z+.35f,b.max.z-.35f,(float)z/(nz-1))),new Vector3(.95f,1.8f,.95f),leaf);
            }
            var yard=GameObject.Find("Floor Yard");var ground=Material("Muted garden ground",new Color(.19f,.22f,.15f));yard.GetComponent<Renderer>().sharedMaterial=ground;
            foreach(float x in new[]{27.3f,20.0f})for(int i=0;i<6;i++)Box("Garden stepping stone",details,new Vector3(x,.012f,30.5f+i*1.5f),new Vector3(1.1f,.02f,1.1f),stone,false);
            var sky=new Material(Shader.Find("Skybox/Procedural")){name="Manor low-poly night sky"};sky.SetFloat("_AtmosphereThickness",.1f);sky.SetFloat("_Exposure",.32f);sky.SetColor("_SkyTint",new Color(.04f,.07f,.14f));sky.SetColor("_GroundColor",new Color(.025f,.03f,.045f));sky.SetFloat("_SunSize",.015f);AssetDatabase.CreateAsset(sky,MaterialsFolder+"/Manor night sky.mat");RenderSettings.skybox=sky;
            foreach(var light in Object.FindObjectsByType<Light>())if(light.name.Contains("moon bounce"))
            {light.transform.position-=Vector3.up*.65f;light.intensity*=.65f;}
        }

        private static void PolishScene()
        {
            const string marker="Concept visual finish v5";if(GameObject.Find(marker)!=null)return;var g=Group(marker,root);
            var masonry=Material("Unpolished manor stone",new Color(.34f,.36f,.35f));
            foreach(var r in Object.FindObjectsByType<MeshRenderer>())if(r.name=="Stone planter rim"||r.name=="Fireplace surround"||r.name=="Octagonal basin"||r.name=="Basin centre")r.sharedMaterial=masonry;
            foreach(string name in new[]{"Library Long Shelf","Library North Shelf"})
            {
                var owner=GameObject.Find(name).transform;var box=owner.GetComponent<Collider>().bounds;box.Expand(.25f);
                foreach(var t in Object.FindObjectsByType<Transform>().Where(t=>t.name=="Book"&&box.Contains(t.position)).ToArray())t.gameObject.SetActive(false);
                foreach(var model in owner.Cast<Transform>().Where(t=>t.name.StartsWith("Model -")).ToArray())
                {
                    var before=LocalBounds(owner,model.GetComponentsInChildren<MeshFilter>());var source=model.GetChild(0);source.localRotation*=Quaternion.Euler(0,180,0);
                    var after=LocalBounds(owner,model.GetComponentsInChildren<MeshFilter>());model.localPosition+=before.center-after.center;
                }
            }
            foreach(string name in new[]{"Hall Glass","Garden Glass"})
            {
                var patch=GameObject.Find(name);patch.GetComponent<Renderer>().enabled=false;var b=patch.GetComponent<Collider>().bounds;
                var shardMat=Material("Moonlit broken glass",new Color(.36f,.49f,.52f),null,.25f);
                for(int i=0;i<33;i++)
                {
                    float x=b.min.x+.15f+Mathf.Repeat(i*.713f,Mathf.Max(.3f,b.size.x-.3f)),z=b.min.z+.15f+Mathf.Repeat(i*.437f,Mathf.Max(.3f,b.size.z-.3f));
                    var shard=Box("Visible broken glass fragment",g,new Vector3(x,b.max.y+.002f,z),new Vector3(.10f+(i%3)*.045f,.012f,.04f+(i%4)*.028f),shardMat,false);shard.transform.rotation=Quaternion.Euler(0,i*37,0);
                }
            }
            MoonWindow(g,new Vector3(37.86f,2.3f,18.5f),-90);MoonWindow(g,new Vector3(37.86f,2.3f,22),-90);MoonWindow(g,new Vector3(37.86f,2.3f,25.5f),-90);
            MoonWindow(g,new Vector3(23.84f,3.1f,-8.5f),-90);MoonWindow(g,new Vector3(23.84f,3.1f,3.8f),-90);
            MoonWindow(g,new Vector3(-19.84f,2.2f,-8.2f),90);
            var glassRoof=Material("Conservatory dark glass roof",new Color(.10f,.16f,.19f));
            GameObject.Find("Ceiling Conservatory").GetComponent<Renderer>().sharedMaterial=glassRoof;
            for(int x=25;x<=37;x+=2)Box("Conservatory roof iron rib",g,new Vector3(x,4.39f,21),new Vector3(.07f,.07f,14),dark,false);
            for(int z=15;z<=27;z+=3)Box("Conservatory roof cross rib",g,new Vector3(31,4.36f,z),new Vector3(14,.07f,.06f),dark,false);
        }
        private static void MoonWindow(Transform parent,Vector3 p,float yaw)
        {
            var window=Group("Moonlit window placeholder",parent);var pane=Material("Moonlit glass",new Color(.14f,.22f,.30f));
            pane.EnableKeyword("_EMISSION");pane.SetColor("_EmissionColor",new Color(.07f,.12f,.17f));
            Box("Window dark blue glass",window,Vector3.zero,new Vector3(1.6f,2.0f,.035f),pane,false);
            foreach(float x in new[]{-.85f,0,.85f})Box("Window timber mullion",window,Vector3.right*x,new Vector3(.09f,2.1f,.07f),timber,false);
            foreach(float y in new[]{-1.05f,0,1.05f})Box("Window timber crossbar",window,Vector3.up*y,new Vector3(1.8f,.09f,.07f),timber,false);
            window.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));
        }

        private static void LowPoly(string name,Transform parent,Vector3 p,Vector3 size,Material mat)
        {var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,true);go.transform.position=p;go.transform.localScale=size;go.GetComponent<MeshFilter>().sharedMesh=LeafMesh();go.GetComponent<Renderer>().sharedMaterial=mat;}
        private static Mesh LeafMesh()
        {
            if(leafMesh!=null)return leafMesh;string path=MaterialsFolder+"/Low poly foliage.asset";leafMesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(leafMesh!=null)return leafMesh;
            var points=new[]{Vector3.up*.5f,Vector3.down*.5f,Vector3.left*.5f,Vector3.right*.5f,Vector3.forward*.5f,Vector3.back*.5f};int[] indices={0,4,3,0,2,4,0,5,2,0,3,5,1,3,4,1,4,2,1,2,5,1,5,3};var v=indices.Select(i=>points[i]).ToArray();
            leafMesh=new Mesh{name="Low poly foliage"};leafMesh.vertices=v;leafMesh.triangles=Enumerable.Range(0,v.Length).ToArray();leafMesh.RecalculateNormals();leafMesh.RecalculateBounds();AssetDatabase.CreateAsset(leafMesh,path);return leafMesh;
        }
    }
}
