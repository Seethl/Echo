using System;
using System.Linq;
using Echo.Interactions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Echo.Manor.Editor
{
    public static class ManorNextDetails
    {
        const string Marker="Upper room, removable crank and remedy bottles";
        const string Mechanisms="Assets/Props/ManorMechanisms/";
        [MenuItem("Echo/Руків’я, пляшечки та житлова кімната")]
        public static void ApplyAndSave()
        {
            if(EditorSceneManager.GetActiveScene().path!=ManorPrototypeBuilder.ScenePath)throw new InvalidOperationException("Open ManorPrototype first.");
            Apply();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
        public static void Apply()
        {
            Handle();
            if(GameObject.Find(Marker)==null)Furnish();
            Bottles();
            var puzzle=Object.FindAnyObjectByType<SymbolPuzzle>();puzzle.animalNoiseRadius=36;EditorUtility.SetDirty(puzzle);
            var bench=Object.FindAnyObjectByType<TreatmentWorkbench>();bench.requiresBottle=true;EditorUtility.SetDirty(bench);
            var delivery=Object.FindAnyObjectByType<RemedyDelivery>();delivery.requiresBottle=true;EditorUtility.SetDirty(delivery);
            var recipe=GameObject.Find("Remedy Recipe").GetComponent<ClueNote>();
            const string addition="\nГотові ліки перелий у чисту порожню пляшечку, потім — у його чашку в холі.";
            if(!recipe.text.Contains("порожню пляшечку"))recipe.text+=addition;EditorUtility.SetDirty(recipe);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
        static Bounds Bounds(GameObject root)=>root.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
        static GameObject Fit(string path,Transform parent,string name,Vector3 bottom,Vector3 size,float yaw=0)
        {
            var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));go.name=name;go.transform.rotation=Quaternion.Euler(0,yaw,0);
            foreach(var c in go.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
            var b=Bounds(go);go.transform.localScale=Vector3.Scale(go.transform.localScale,new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z));
            b=Bounds(go);go.transform.position+=bottom+Vector3.up*(size.y/2)-b.center;go.transform.SetParent(parent,true);return go;
        }
        static void Handle()
        {
            string path=Mechanisms+"SM_WinchHandle.fbx";var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.isReadable=true;importer.importAnimation=false;
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            foreach(var mat in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>())importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),mat.name),AssetDatabase.LoadAssetAtPath<Material>(Mechanisms+mat.name+".mat"));
            importer.SaveAndReimport();
            var pickup=GameObject.Find("Winch Handle");var existing=pickup.transform.Find("Blender loose crank");
            foreach(var old in pickup.GetComponentsInChildren<Renderer>())if(existing==null||!old.transform.IsChildOf(existing))old.enabled=false;
            if(existing!=null)return;
            var original=pickup.transform.position;
            Physics.SyncTransforms();
            var supports=Physics.RaycastAll(original+Vector3.up*.35f,Vector3.down,1.6f,~0,QueryTriggerInteraction.Ignore).Where(h=>!h.transform.IsChildOf(pickup.transform)&&h.normal.y>.8f).OrderBy(h=>h.distance).ToArray();
            float y=supports.Length>0?supports[0].point.y:original.y-.07f;
            var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));model.name="Blender loose crank";
            var bounds=Bounds(model);model.transform.position+=new Vector3(original.x-bounds.center.x,y+.005f-bounds.min.y,original.z-bounds.center.z);model.transform.SetParent(pickup.transform,true);
            bounds=Bounds(model);var box=pickup.GetComponent<BoxCollider>();box.center=pickup.transform.InverseTransformPoint(bounds.center);var scale=pickup.transform.lossyScale;
            box.size=new Vector3((bounds.size.x+.015f)/scale.x,(bounds.size.y+.02f)/scale.y,(bounds.size.z+.015f)/scale.z);
            var temp=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));PrefabUtility.SaveAsPrefabAsset(temp,Mechanisms+"PF_WinchHandle.prefab");Object.DestroyImmediate(temp);
        }
        static Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Manor/"+name+".mat");
        static GameObject Box(string name,Transform parent,Vector3 p,Vector3 size,Material mat,bool solid=true)=>ManorSetDress.Box(name,parent,p,size,mat,solid);
        static void Table(Transform root,string name,Vector3 p,Vector3 size)
        {
            var group=new GameObject(name).transform;group.SetParent(root,false);
            Fit("Assets/Props/ManorTables/PF_ManorTable.prefab",group,"Table model",p,size,180);
            Box("Solid table top",group,p+Vector3.up*(size.y-.045f),new Vector3(size.x,.09f,size.z),Mat("Aged timber")).GetComponent<Renderer>().enabled=false;
            foreach(float x in new[]{-1f,1f})foreach(float z in new[]{-1f,1f})Box("Solid table leg",group,p+new Vector3(x*(size.x/2-.10f),size.y/2,z*(size.z/2-.10f)),new Vector3(.1f,size.y-.09f,.1f),Mat("Aged timber")).GetComponent<Renderer>().enabled=false;
        }
        static void Furnish()
        {
            var root=new GameObject(Marker).transform;
            Table(root,"Study north console",new Vector3(-5.5f,4.4f,3.82f),new Vector3(2.6f,.88f,.65f));
            Table(root,"Study bedside table",new Vector3(-8.85f,4.4f,1.60f),new Vector3(1.35f,.66f,.75f));
            var wardrobe=Fit("Assets/Props/ManorStorage/PF_ManorWardrobe.prefab",root,"Study east linen wardrobe",new Vector3(.43f,4.4f,-2.1f),new Vector3(.72f,2.3f,1.8f),90);
            AddBox(wardrobe);
            var wood=Mat("Aged timber");var linen=Mat("Linen and pages");
            Box("Bed footboard",root,new Vector3(-9.05f,4.94f,-1.65f),new Vector3(1.47f,.82f,.09f),wood);
            Box("Bed burgundy quilt",root,new Vector3(-9.05f,5.09f,-.83f),new Vector3(1.37f,.07f,1.40f),Mat("Burgundy rug"),false);
            Box("Folded linen on quilt",root,new Vector3(-9.05f,5.14f,-1.25f),new Vector3(1.20f,.05f,.35f),linen,false);
            foreach(float x in new[]{-9.75f,-8.35f})foreach(float z in new[]{-1.64f,.64f})Box("Bed carved post",root,new Vector3(x,4.99f,z),new Vector3(.09f,1.18f,.09f),wood);
            Decor(root,"BookStack",new Vector3(-6.2f,5.28f,3.83f),new Vector3(.32f,.12f,.22f),-9);
            Decor(root,"Mug",new Vector3(-5.35f,5.28f,3.74f),new Vector3(.13f,.14f,.13f));
            Decor(root,"Bottle",new Vector3(-4.7f,5.28f,3.80f),new Vector3(.13f,.25f,.13f));
            Decor(root,"BookStack",new Vector3(-9.0f,5.06f,1.65f),new Vector3(.30f,.11f,.21f),12);
            Decor(root,"Mug",new Vector3(-8.48f,5.06f,1.56f),new Vector3(.12f,.13f,.12f));
            // A guaranteed accessible kitchen bottle prevents any dependency on cupboard clutter.
            var kitchenBottle=Decor(root,"Bottle",new Vector3(31.75f,1.01f,5.75f),new Vector3(.14f,.26f,.14f));
            kitchenBottle.name="Kitchen empty bottle";Physics.SyncTransforms();
            var kb=Bounds(kitchenBottle);var hits=Physics.RaycastAll(kb.center+Vector3.up*.25f,Vector3.down,.9f,~0,QueryTriggerInteraction.Ignore).Where(h=>h.normal.y>.8f&&!h.transform.IsChildOf(kitchenBottle.transform)).OrderBy(h=>h.distance).ToArray();
            if(hits.Length==0)throw new InvalidOperationException("Kitchen bottle has no counter support");
            kitchenBottle.transform.position+=Vector3.up*(hits[0].point.y+.003f-kb.min.y);
        }
        static GameObject Decor(Transform root,string kind,Vector3 bottom,Vector3 size,float yaw=0)=>Fit("Assets/Props/ManorDecor/PF_Decor"+kind+".prefab",root,"Useful decor "+kind,bottom,size,yaw);
        static void AddBox(GameObject owner)
        {
            bool first=true;var bounds=new Bounds();
            foreach(var mesh in owner.GetComponentsInChildren<MeshFilter>())
            {
                var renderer=mesh.GetComponent<Renderer>();if(renderer==null||!renderer.enabled||!mesh.gameObject.activeInHierarchy)continue;
                var b=mesh.sharedMesh.bounds;
                for(int i=0;i<8;i++)
                {
                    var p=owner.transform.InverseTransformPoint(mesh.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));
                    if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);
                }
            }
            var col=owner.AddComponent<BoxCollider>();col.center=bounds.center;col.size=bounds.size;
        }
        static string PathOf(Transform t)=>t.parent==null?t.name+"["+t.GetSiblingIndex()+"]":PathOf(t.parent)+"/"+t.name+"["+t.GetSiblingIndex()+"]";
        static void Bottles()
        {
            var all=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            foreach(var t in all.Where(t=>t.gameObject.activeInHierarchy&&(t.name=="Storage bottle"||t.name=="Scattered shelf Bottle"||t.name=="Model - PF_DecorBottle"||t.name=="Useful decor Bottle"||t.name=="Kitchen empty bottle")))
            {
                if(t.GetComponent<RemedyBottle>()!=null||t.GetComponentInParent<Interactable>()!=null)continue;
                var b=Bounds(t.gameObject);if(b.size.y<.07f||b.size.y>1)continue;
                var bottle=t.gameObject.AddComponent<RemedyBottle>();bottle.bottleId="remedy.bottle."+PathOf(t);
                if(t.GetComponent<Collider>()==null)AddBox(t.gameObject);
            }
        }
    }
}
