using System;
using System.IO;
using System.Linq;
using Echo.Interactions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Echo.Manor.Editor
{
    public static class ManorGardenPolish
    {
        public const string Marker="Closed staircase and mixed daisy garden";
        public const string Folder="Assets/Props/ManorGarden";
        static Transform root;static Material wood,iron,soil,gardenStone;
        static GameObject Find(string name)=>Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).FirstOrDefault(t=>t.name==name)?.gameObject;
        static Material Existing(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Manor/"+name+".mat");
        [MenuItem("Echo/Сходи, двері та сад із ромашками")]
        public static void ApplyAndSave(){Apply();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();}
        public static void Apply()
        {
            if(EditorSceneManager.GetActiveScene().path!=ManorPrototypeBuilder.ScenePath)throw new InvalidOperationException("Open ManorPrototype first");
            Prepare();
            foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if(t.name.StartsWith("Kitchen doorway plaque")||t.name.StartsWith("Library doorway plaque"))t.gameObject.SetActive(false);
            foreach(var door in Object.FindObjectsByType<HingedDoor>()){door.stayOpen=false;EditorUtility.SetDirty(door);}
            var inventory=Object.FindAnyObjectByType<InventorySidebar>();if(inventory!=null){inventory.displayScale=1.17f;EditorUtility.SetDirty(inventory);}
            NotesAndRecipe();
            if(Find(Marker)!=null)return;
            root=new GameObject(Marker).transform;wood=Existing("Aged timber");iron=Existing("Dark iron");soil=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Garden soil.mat");gardenStone=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Garden stone.mat");
            Staircase();Garden();Dressing();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
        static GameObject Box(string n,Vector3 p,Vector3 s,Material m,bool solid=false)=>ManorSetDress.Box(n,root,p,s,m,solid);
        static void Beam(string n,Vector3 a,Vector3 b,float width,float depth)
        {var go=Box(n,(a+b)/2,new Vector3(width,depth,(b-a).magnitude),wood);go.transform.rotation=Quaternion.LookRotation(b-a);}
        static void Staircase()
        {
            Beam("Continuous stair soffit",new Vector3(6.3f,.075f,-6.60f),new Vector3(6.3f,4.275f,4.40f),2.10f,.12f);
            foreach(float x in new[]{5.24f,7.36f})Beam("Closed staircase side fascia",new Vector3(x,.12f,-6.60f),new Vector3(x,4.32f,4.40f),.13f,.34f);
            for(int i=14;i<22;i++)Box("Closed stair riser "+i,new Vector3(6.3f,i*.2f+.10f,-6.35f+i*.5f-.25f),new Vector3(2.08f,.20f,.045f),wood);
        }
        static Bounds Bounds(GameObject go)=>go.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
        static GameObject Model(string type,string name,Vector3 bottom,float scale=1,float yaw=0)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/PF_"+type+".prefab"));go.name=name;go.transform.SetParent(root,false);go.transform.localScale=Vector3.one*scale;go.transform.rotation=Quaternion.Euler(0,yaw,0);var b=Bounds(go);go.transform.position+=new Vector3(bottom.x-b.center.x,bottom.y-b.min.y,bottom.z-b.center.z);return go;
        }
        static void Garden()
        {
            // Independent solid planter shells belonged to the old yard hedges.
            foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).Where(t=>(t.name=="Stone planter rim"||t.name=="Dense low poly planting")&&t.position.z>28).ToArray())t.gameObject.SetActive(false);
            // Replace the old opaque block hedges with planted cover outside
            // the flowerbed, keeping stealth cover and an open picking edge.
            foreach(var pair in new[]{new {name="Yard Hedge West",z=36.8f},new {name="Yard Hedge North",z=43f}})
            {
                var hedge=Find(pair.name);if(hedge==null)continue;hedge.transform.position=new Vector3(25,.6f,pair.z);hedge.transform.localScale=new Vector3(1.8f,1.2f,3.5f);
                foreach(var renderer in hedge.GetComponentsInChildren<Renderer>())renderer.enabled=false;
                for(int i=0;i<3;i++)Model("Shrub","Planted hedge shrub",new Vector3(25,0,pair.z-.9f+i*.9f),1.0f,i*47);
            }
            foreach(string name in new[]{"Moonflower medicinal bed","Blue six-petal bed","Yellow three-petal bed"}){var old=Find(name);if(old!=null)old.SetActive(false);}
            foreach(var t in Object.FindObjectsByType<Transform>().Where(t=>t.name=="Garden shrub"&&t.position.z>28).ToArray())t.gameObject.SetActive(false);
            var ground=ManorSetDress.Shape("Mixed garden soil",root,new Vector3(18,0,35.7f),new Vector3(7.3f,.018f,7.3f),soil,PrimitiveType.Cylinder,true);
            // Unity's cylinder primitive gets a CapsuleCollider: flattening
            // its Y scale leaves a large rounded obstruction. Match the mesh.
            Object.DestroyImmediate(ground.GetComponent<Collider>());var groundCollider=ground.AddComponent<MeshCollider>();groundCollider.sharedMesh=ground.GetComponent<MeshFilter>().sharedMesh;
            var random=new System.Random(1379);
            for(int i=0;i<25;i++)
            {float a=i*Mathf.PI*2/25;var edge=Box("Irregular flowerbed stone",new Vector3(18+Mathf.Cos(a)*3.65f,.055f,35.7f+Mathf.Sin(a)*3.65f),new Vector3(.48f,.11f,.22f),gardenStone);edge.transform.rotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg+90,0);}
            foreach(var t in Object.FindObjectsByType<Transform>().Where(t=>t.name=="Garden stepping stone"&&t.position.z>28))
            {if(Vector2.Distance(new Vector2(t.position.x,t.position.z),new Vector2(18,35.7f))<3.9f)t.gameObject.SetActive(false);else t.GetComponent<Renderer>().sharedMaterial=gardenStone;}
            Physics.SyncTransforms();var used=new System.Collections.Generic.List<Vector2>();
            Vector3[] daisies={new Vector3(20.65f,.02f,34.3f),new Vector3(20.2f,.02f,36.7f),new Vector3(16.7f,.02f,33.4f)};
            for(int i=0;i<daisies.Length;i++)
            {
                var go=Model("Daisy","Gatherable daisy "+i,daisies[i],1.1f+i*.06f,i*37);Gather(go,"daisy","Ромашка: білі вузькі пелюстки навколо жовтої серединки, тонке розсічене листя.");used.Add(new Vector2(daisies[i].x,daisies[i].z));
            }
            string[] species={"Poppy","Lupin","Marigold","Daisy"};
            for(int i=0;i<80;i++)
            {
                Vector2 p=default;bool placed=false;
                for(int attempt=0;attempt<120;attempt++)
                {double angle=random.NextDouble()*Math.PI*2,r=Math.Sqrt(random.NextDouble())*3.1;p=new Vector2(18+(float)(Math.Cos(angle)*r),35.7f+(float)(Math.Sin(angle)*r));if(used.All(q=>Vector2.Distance(p,q)>.44f)&&!Physics.CheckSphere(new Vector3(p.x,.25f,p.y),.18f,~0,QueryTriggerInteraction.Ignore)){placed=true;break;}}
                if(!placed)continue;used.Add(p);string type=species[i%4];var go=Model(type,"Mixed flower "+i+" "+type,new Vector3(p.x,.02f,p.y),.8f+(float)random.NextDouble()*.45f,(float)random.NextDouble()*360);
                if(type=="Daisy")Gather(go,"daisy","Ромашка: білі пелюстки й жовта серединка.");
                else if(i<3)Gather(go,type.ToLowerInvariant(),type=="Poppy"?"Мак: широкі червоні пелюстки й темна серединка.":type=="Lupin"?"Люпин: високе фіолетове суцвіття.":"Чорнобривці: округлі золотаві суцвіття.");
            }
            foreach(var p in new[]{new Vector3(11.7f,0,31.7f),new Vector3(12,0,45.2f),new Vector3(35.5f,0,45.3f)})
            {var go=Model("Tree","Garden branching tree",p,1.1f,random.Next(360));var collider=go.AddComponent<CapsuleCollider>();collider.center=new Vector3(.06f,1.25f,0);collider.height=2.5f;collider.radius=.21f;}
            foreach(var p in new[]{new Vector3(12,0,38.5f),new Vector3(24,0,46.2f),new Vector3(34.8f,0,43.2f),new Vector3(22.5f,0,31.2f)})
            {var go=Model("Shrub","Garden mixed shrub",p,1,random.Next(360));var collider=go.AddComponent<BoxCollider>();collider.center=new Vector3(0,.34f,0);collider.size=new Vector3(.8f,.68f,.7f);}
            foreach(var p in new[]{new Vector3(18,.02f,35.5f),new Vector3(16,.02f,36.5f),new Vector3(18.5f,.02f,38)})Model("Shrub","Flowerbed sage foliage",p,.42f,random.Next(360));
        }
        static void Gather(GameObject go,string species,string description)
        {
            var b=Bounds(go);var box=go.AddComponent<BoxCollider>();box.center=go.transform.InverseTransformPoint(b.center);box.size=new Vector3(.25f,b.size.y/go.transform.lossyScale.y,.25f);
            var plant=go.AddComponent<PlantGathering>();plant.speciesId=species;plant.requiredSpecies="daisy";plant.description=description;plant.displayName="ромашка";plant.pickupPrompt="E — зірвати ромашку";
        }
        static void NotesAndRecipe()
        {
            var catalog=AssetDatabase.LoadAssetAtPath<InventoryCatalog>("Assets/Resources/EchoInventory.asset");
            if(catalog!=null){foreach(var item in catalog.items.Where(i=>i.id=="remedy.flower")){item.displayName="Ромашка";item.hint="Білі пелюстки й жовта серединка. Потрібна для мікстури.";}EditorUtility.SetDirty(catalog);}
            var recipe=Find("Remedy Recipe")?.GetComponent<ClueNote>();
            if(recipe!=null){recipe.text="Основа — в аптечній шафці на кухні. Ключ залишився нагорі.\n\nПотрібна ромашка: білі вузькі пелюстки навколо жовтої серединки. Вона росте на змішаній клумбі у дворі.\n\nНалий основу, додай ромашку, розмішай срібною ложечкою. Готові ліки набери в чисту пляшечку.\n\nПерелий у чашку мешканця в холі та відійди.";EditorUtility.SetDirty(recipe);}
            var herb=Find("Garden Herbarium")?.GetComponent<ClueNote>();
            if(herb!=null){herb.text="Для мікстури потрібна ромашка: жовта серединка, довкола неї вузькі білі пелюстки, тонке розсічене листя.\n\nНа клумбі також є червоні маки, фіолетовий люпин і золотаві чорнобривці. Вони не підходять.\n\nЗірви одну ромашку. Більше для цього рецепта не потрібно.";EditorUtility.SetDirty(herb);}
        }
        static void Dressing()
        {
            PlaceOnSupport("Jug","Caretaker ceramic jug",new Vector3(-14.8f,.82f,-11.0f));PlaceOnSupport("Basket","Gallery wicker basket",new Vector3(1.3f,0,-5.2f));
            PlaceOnSupport("BookStack","Study loose books",new Vector3(-6.3f,5.3f,-6.5f));PlaceOnSupport("Jug","Kitchen ceramic jug",new Vector3(28.25f,.9f,.89f));PlaceOnSupport("Basket","Kitchen storage basket",new Vector3(33.8f,0,1.15f));
            Rug("Caretaker bedside rug",new Vector3(-15.4f,.003f,-6.1f),new Vector2(2.4f,1.35f));Rug("Gallery worn runner",new Vector3(-4,.003f,-8),new Vector2(5.5f,1.8f));Rug("Upper landing runner",new Vector3(3,4.403f,5.9f),new Vector2(5.5f,1.7f));
        }
        static void PlaceOnSupport(string type,string name,Vector3 approximate)
        {
            Physics.SyncTransforms();var hits=Physics.RaycastAll(approximate+Vector3.up*.7f,Vector3.down,1.7f,~0,QueryTriggerInteraction.Ignore).Where(h=>h.normal.y>.7f&&Mathf.Abs(h.point.y-approximate.y)<.45f).OrderBy(h=>Mathf.Abs(h.point.y-approximate.y)).ToArray();
            if(hits.Length==0)throw new InvalidOperationException("No support for "+name);
            Model(type,name,new Vector3(approximate.x,hits[0].point.y+.004f,approximate.z));
        }
        static void Rug(string name,Vector3 position,Vector2 size)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Props/ManorSurfaces/PF_ManorRug_2x3.prefab"));go.name=name;go.transform.SetParent(root,false);go.transform.localScale=new Vector3(size.x/2,1,size.y/3);go.transform.position=position;
            var collider=go.AddComponent<BoxCollider>();collider.center=new Vector3(0,.005f,0);collider.size=new Vector3(2,.01f,3);var surface=go.AddComponent<SurfaceType>();surface.kind=SurfaceType.Kind.Carpet;surface.noiseMultiplier=.22f;surface.loudnessMultiplier=.25f;
        }
        [Serializable] class Specs{public Spec[] materials;}
        [Serializable] class Spec{public string name;public float[] color;}
        static void Prepare()
        {
            foreach(var s in JsonUtility.FromJson<Specs>(File.ReadAllText(Folder+"/palette.json")).materials)
            {string path=Folder+"/"+s.name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",new Color(s.color[0],s.color[1],s.color[2]).gamma);m.SetFloat("_Smoothness",.08f);EditorUtility.SetDirty(m);}
            var stoneMat=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Garden stone.mat");if(stoneMat==null){stoneMat=new Material(Shader.Find("Universal Render Pipeline/Lit"));stoneMat.SetColor("_BaseColor",new Color(.26f,.28f,.25f));stoneMat.SetFloat("_Smoothness",0);AssetDatabase.CreateAsset(stoneMat,Folder+"/Garden stone.mat");}
            var soilMat=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Garden soil.mat");if(soilMat==null){soilMat=new Material(Shader.Find("Universal Render Pipeline/Lit"));soilMat.SetColor("_BaseColor",new Color(.13f,.09f,.06f));soilMat.SetFloat("_Smoothness",0);AssetDatabase.CreateAsset(soilMat,Folder+"/Garden soil.mat");}
            foreach(string type in new[]{"Daisy","Poppy","Lupin","Marigold","Tree","Shrub","Jug","Basket","BookStack"})
            {string path=Folder+"/SM_"+type+".fbx";var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.importAnimation=false;importer.isReadable=true;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;foreach(var m in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>())importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),m.name),AssetDatabase.LoadAssetAtPath<Material>(Folder+"/"+m.name+".mat"));importer.SaveAndReimport();var temp=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));PrefabUtility.SaveAsPrefabAsset(temp,Folder+"/PF_"+type+".prefab");Object.DestroyImmediate(temp);}
            var paper=(TextureImporter)AssetImporter.GetAtPath("Assets/Resources/EchoWritingBoard.png");paper.textureType=TextureImporterType.Sprite;paper.spriteImportMode=SpriteImportMode.Single;paper.spriteBorder=Vector4.one*32;paper.mipmapEnabled=false;paper.SaveAndReimport();
        }
    }
}
