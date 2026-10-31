using System;
using System.Collections.Generic;
using System.Linq;
using Echo.Audio;
using Echo.Interactions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
using static Echo.Manor.Editor.ManorSetDress;

namespace Echo.Manor.Editor
{
    public static class ManorFirstRoomsPolish
    {
        private const string Marker="Caretaker and three-window gallery revision";
        private static Transform root;
        private static Material wood,cloth,iron,glass;
        [MenuItem("Echo/Оновити перші кімнати та віконниці")]
        public static void ApplyAndSave()
        {
            if(EditorSceneManager.GetActiveScene().path!=ManorPrototypeBuilder.ScenePath)throw new InvalidOperationException("Open ManorPrototype first.");
            Apply();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
        public static void Apply()
        {
            if(GameObject.Find(Marker)!=null){RefreshHints();return;}
            root=Group(Marker);wood=Load("Aged timber");cloth=Load("Linen and pages");iron=Load("Dark iron");glass=Load("Moonlit glass");
            CoatAndLuggage();FillCupboard();ResizeServiceDoor();GalleryWindows();RefreshHints();
        }
        private static void RefreshHints()
        {
            foreach(var window in Object.FindObjectsByType<GalleryWindowShutter>())
            {var mask=window.GetComponent<NoiseMaskZone>();mask.description="гуркіт віконниць";EditorUtility.SetDirty(mask);}
            var channel=AssetDatabase.LoadAssetAtPath<Echo.Radio.RadioChannel>("Assets/RadioData/Gallery940.asset");
            if(channel!=null){channel.interruptedTranscript="Службовий замок на поточну зміну… [гуркіт заглушає цифри]. Зачиніть усі віконниці й повторіть запис.";EditorUtility.SetDirty(channel);}
        }
        private static Material Load(string name)=>AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder+"/"+name+".mat");
        private static GameObject LocalBox(string name,Transform parent,Vector3 p,Vector3 size,Material mat,bool solid=false)
        {var go=Box(name,null,p,size,mat,solid);go.transform.SetParent(parent,false);return go;}
        private static Bounds MeshBounds(MeshFilter f)
        {
            var b=f.sharedMesh.bounds;var result=new Bounds();bool first=true;
            for(int i=0;i<8;i++){var p=f.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));if(first){result=new Bounds(p,Vector3.zero);first=false;}else result.Encapsulate(p);}return result;
        }
        private static Bounds VisibleBounds(Transform t)
        {
            bool first=true;var b=new Bounds();foreach(var f in t.GetComponentsInChildren<MeshFilter>())
            {var r=f.GetComponent<Renderer>();if(r==null||!r.enabled||!r.gameObject.activeInHierarchy)continue;var next=MeshBounds(f);if(first){b=next;first=false;}else b.Encapsulate(next);}return b;
        }
        private static GameObject Decor(string kind,Transform parent,Vector3 centre,Vector3 size)
        {return Fit(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Props/ManorDecor/PF_Decor"+kind+".prefab"),parent,"Shelf decoration - "+kind,centre,size);}
        private static void CoatAndLuggage()
        {
            var rail=GameObject.Find("Wall coat rail");rail.transform.position=new Vector3(-19.74f,1.8f,-6.0f);
            GameObject.Find("Hanging coat").SetActive(false);
            var coat=Group("Recognisable coat beside the bed",root);coat.position=new Vector3(-19.58f,1.2f,-6.0f);
            LocalBox("Coat body",coat,Vector3.zero,new Vector3(.16f,.86f,.48f),iron);
            LocalBox("Coat shoulders",coat,new Vector3(0,.43f,0),new Vector3(.17f,.15f,.70f),iron);
            foreach(float side in new[]{-1f,1f})
            {var sleeve=LocalBox("Coat sleeve",coat,new Vector3(0,.16f,side*.33f),new Vector3(.14f,.60f,.16f),iron);sleeve.transform.localRotation=Quaternion.Euler(side*18,0,0);}
            LocalBox("Coat collar",coat,new Vector3(.09f,.45f,0),new Vector3(.025f,.12f,.14f),cloth);
            foreach(var t in Object.FindObjectsByType<Transform>().Where(t=>t.parent!=null&&t.parent.name=="Caretaker daily life"&&t.name=="Storage crate"))t.position+=new Vector3(-.45f,0,0);
            // Move both parts of the gallery crate stack away from the first open shutter.
            foreach(var t in Object.FindObjectsByType<Transform>().Where(t=>t.parent!=null&&t.parent.name=="Gallery furnishings"&&t.name=="Storage crate"))t.position+=new Vector3(-.65f,0,.15f);
        }
        private static void FillCupboard()
        {
            var cupboard=GameObject.Find("Service Cupboard").transform;
            // The FBX body is a single mesh. Find its horizontal shelf tops without assuming mesh-node names.
            var horizontal=new Dictionary<float,Bounds>();
            foreach(var f in cupboard.GetComponentsInChildren<MeshFilter>())
            {
                var renderer=f.GetComponent<Renderer>();if(renderer==null||!renderer.enabled||!f.gameObject.activeInHierarchy)continue;
                var points=f.sharedMesh.vertices;var indices=f.sharedMesh.triangles;
                for(int i=0;i<indices.Length;i+=3)
                {
                    var a=f.transform.TransformPoint(points[indices[i]]);var b=f.transform.TransformPoint(points[indices[i+1]]);var c=f.transform.TransformPoint(points[indices[i+2]]);
                    var normal=Vector3.Cross(b-a,c-a);if(normal.y<=0||normal.normalized.y<.98f||normal.magnitude<.06f||a.y<.30f||a.y>1.95f)continue;
                    float y=Mathf.Round(a.y*1000)/1000;var bounds=new Bounds(a,Vector3.zero);bounds.Encapsulate(b);bounds.Encapsulate(c);
                    if(horizontal.TryGetValue(y,out var before)){before.Encapsulate(bounds);horizontal[y]=before;}else horizontal.Add(y,bounds);
                }
            }
            var shelves=horizontal.Values.Where(b=>b.size.x>.7f).OrderBy(b=>b.center.y).ToArray();
            if(shelves.Length<2)throw new InvalidOperationException("Cannot locate native cupboard shelves for grounded decoration.");
            Debug.Log("CUPBOARD_SHELF_TOPS "+string.Join(",",shelves.Select(b=>b.max.y.ToString("F3"))));
            var contents=Group("Caretaker cupboard contents",cupboard);
            for(int i=0;i<shelves.Length;i++)
            {
                float y=shelves[i].max.y;
                Decor("BookStack",contents,new Vector3(-12.43f,y+.065f,-4.98f),new Vector3(.30f,.13f,.24f));
                Decor(i%2==0?"Jar":"Mug",contents,new Vector3(-11.53f,y+.10f,-4.98f),new Vector3(.16f,.20f,.16f));
                if(i==shelves.Length-1)LocalBox("Folded linen",root,new Vector3(-11.93f,y+.045f,-4.88f),new Vector3(.27f,.09f,.25f),cloth);
            }
            var key=GameObject.Find("Service Key");float keyY=shelves.OrderBy(b=>Mathf.Abs(b.max.y-key.transform.position.y)).First().max.y+.035f;
            key.transform.position=new Vector3(key.transform.position.x,keyY,key.transform.position.z);
        }
        private static void ResizeServiceDoor()
        {
            const float width=1.25f,height=2.45f;
            var door=GameObject.Find("Service Door").GetComponent<HingedDoor>();door.hinge.position=new Vector3(-10,0,-8+width/2);
            var panel=door.GetComponentsInChildren<BoxCollider>().First(c=>c.name=="Door Panel");panel.transform.localPosition=new Vector3(width/2,height/2,0);panel.transform.localScale=new Vector3(width,height,.13f);
            foreach(var t in door.GetComponentsInChildren<Transform>().Where(t=>t.name=="Repaired door visual"||t.name=="Door leaf model from main").ToArray())t.gameObject.SetActive(false);
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Props/ManorDoor/SM_ManorDoor.fbx").GetComponentsInChildren<Transform>().First(t=>t.name=="DoorLeaf").gameObject;
            Fit(source,door.hinge,"Human-scale service door visual",new Vector3(width/2,height/2,0),new Vector3(width,height,.10f));
            if(door.navigationBlocker!=null){door.navigationBlocker.center=new Vector3(width/2,height/2,0);door.navigationBlocker.size=new Vector3(width+.05f,height,.35f);}
            GameObject.Find("Fixed frame Service Door").SetActive(false);
            var frame=Group("Narrow fixed service door frame",root);frame.SetPositionAndRotation(door.hinge.position,door.hinge.rotation);
            foreach(float x in new[]{-.075f,width+.075f})LocalBox("Door jamb",frame,new Vector3(x,height/2,0),new Vector3(.15f,height+.14f,.22f),wood);
            LocalBox("Door lintel trim",frame,new Vector3(width/2,height+.07f,0),new Vector3(width+.3f,.14f,.22f),wood);
            var blocks=new[]{new Bounds(new Vector3(-10,1.35f,-8.925f),new Vector3(.2f,2.7f,.55f)),new Bounds(new Vector3(-10,1.35f,-7.075f),new Vector3(.2f,2.7f,.55f)),new Bounds(new Vector3(-10,2.6f,-8),new Vector3(.2f,.2f,1.3f))};
            foreach(var b in blocks){var block=Box("Service doorway solid infill",root,b.center,b.size,wood);block.GetComponent<Renderer>().enabled=false;}
            InfillMesh(blocks);
        }
        private static void InfillMesh(Bounds[] blocks)
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,bool lower)
            {int n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});float lo=lower?.02f:.44f,hi=lower?.38f:.98f;uv.AddRange(new[]{new Vector2(.015f,lo),new Vector2(.985f,lo),new Vector2(.985f,hi),new Vector2(.015f,hi)});triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
            foreach(var block in blocks)
            {
                var cuts=new List<float>{block.min.y,block.max.y};if(block.min.y<1.15f&&block.max.y>1.15f)cuts.Add(1.15f);cuts.Sort();
                for(int i=0;i<cuts.Count-1;i++)
                {
                    var a=new Vector3(block.min.x,cuts[i],block.min.z);var b=new Vector3(block.max.x,cuts[i+1],block.max.z);bool woodBand=(a.y+b.y)/2<1.15f;
                    Quad(new Vector3(a.x,a.y,a.z),new Vector3(a.x,a.y,b.z),new Vector3(a.x,b.y,b.z),new Vector3(a.x,b.y,a.z),woodBand);
                    Quad(new Vector3(b.x,a.y,b.z),new Vector3(b.x,a.y,a.z),new Vector3(b.x,b.y,a.z),new Vector3(b.x,b.y,b.z),woodBand);
                    Quad(new Vector3(b.x,a.y,a.z),new Vector3(a.x,a.y,a.z),new Vector3(a.x,b.y,a.z),new Vector3(b.x,b.y,a.z),woodBand);
                    Quad(new Vector3(a.x,a.y,b.z),new Vector3(b.x,a.y,b.z),new Vector3(b.x,b.y,b.z),new Vector3(a.x,b.y,b.z),woodBand);
                    Quad(new Vector3(a.x,b.y,a.z),new Vector3(a.x,b.y,b.z),new Vector3(b.x,b.y,b.z),new Vector3(b.x,b.y,a.z),woodBand);
                    Quad(new Vector3(a.x,a.y,b.z),new Vector3(a.x,a.y,a.z),new Vector3(b.x,a.y,a.z),new Vector3(b.x,a.y,b.z),woodBand);
                }
            }
            var mesh=new Mesh{name="Narrow service doorway wall finish"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,MaterialsFolder+"/Service doorway infill.asset");
            var go=new GameObject("Service doorway continuous wall finish",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=Load("Continuous wall atlas");
        }
        private static void GalleryWindows()
        {
            foreach(string name in new[]{"Jammed Branch","Shutter Latch","Loose Window Shutter","shutter","branch"}){var old=GameObject.Find(name);if(old!=null)old.SetActive(false);}
            string[] flags={"gallery.window.1.closed","gallery.window.2.closed","gallery.window.3.closed"};
            for(int i=0;i<3;i++)
            {
                var window=Group("Gallery Window "+(i+1),root);window.position=new Vector3(-6.8f+i*4.0f,2.05f,-11.78f);
                var collider=window.gameObject.AddComponent<BoxCollider>();collider.size=new Vector3(1.7f,2.0f,.08f);
                var interaction=window.gameObject.AddComponent<GalleryWindowShutter>();interaction.closedFlag=flags[i];interaction.allWindowFlags=flags;
                LocalBox("Moonlit window pane",window,new Vector3(0,0,-.04f),new Vector3(1.7f,2,.035f),glass);
                foreach(float x in new[]{-.9f,0,.9f})LocalBox("Window mullion",window,new Vector3(x,0,0),new Vector3(.09f,2.18f,.10f),wood);
                foreach(float y in new[]{-1.05f,0,1.05f})LocalBox("Window crossbar",window,new Vector3(0,y,0),new Vector3(1.9f,.09f,.10f),wood);
                LocalBox("Window sill",window,new Vector3(0,-1.1f,.09f),new Vector3(2.0f,.10f,.36f),wood);
                for(int side=0;side<2;side++)
                {
                    float sign=side==0?1:-1;var hinge=Group(side==0?"Left shutter hinge":"Right shutter hinge",window);hinge.localPosition=new Vector3(-sign*.86f,0,.05f);
                    var leaf=Group("Wooden shutter leaf",hinge);LocalBox("Shutter boards",leaf,new Vector3(sign*.42f,0,0),new Vector3(.82f,1.99f,.065f),wood);
                    foreach(float y in new[]{-.65f,.65f})LocalBox("Shutter iron strap",leaf,new Vector3(sign*.42f,y,.04f),new Vector3(.77f,.07f,.025f),iron);
                    LocalBox("Shutter handle",leaf,new Vector3(sign*.73f,-.25f,.055f),new Vector3(.045f,.17f,.04f),iron);
                    if(side==0)interaction.leftHinge=hinge;else interaction.rightHinge=hinge;hinge.localRotation=Quaternion.Euler(0,-sign*interaction.openAngle,0);
                }
                var mask=window.gameObject.AddComponent<NoiseMaskZone>();mask.radius=7;mask.strength=.75f;mask.description="гуркіт віконниць";
                var ambient=window.gameObject.AddComponent<AmbientCycle>();ambient.mode=AmbientCycle.Mode.Shutter;ambient.shutterClosedFlag=flags[i];ambient.movingVisual=interaction.leftHinge.GetChild(0);ambient.visualSwingDegrees=2.0f;ambient.initialDelay=i*.8f;
            }
            var card=GameObject.Find("Gallery Frequency Card").GetComponent<ClueNote>();card.text="Службова передача: 940. Гуркіт відкритих віконниць заглушає цифри.\nЗачини всі три вікна в галереї та знову послухай радіо. R — налаштувати; F — живлення; J — записи.";card.fullTextFlag="";card.fullText="";
            // Furniture occupies the wall bays while the central walkway and all interaction approaches stay clear.
            var console=Group("Gallery window console",root);console.position=new Vector3(-4.8f,0,-11.15f);
            LocalBox("Console collision",console,new Vector3(0,.40f,0),new Vector3(1.25f,.8f,.45f),wood,true).GetComponent<Renderer>().enabled=false;
            Fit(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Props/ManorTables/PF_ManorTable.prefab"),console,"Gallery console model",new Vector3(0,.4f,0),new Vector3(1.25f,.8f,.45f));
            Decor("Jar",console,new Vector3(-.35f,.92f,0),new Vector3(.15f,.24f,.15f));Decor("BookStack",console,new Vector3(.27f,.865f,0),new Vector3(.30f,.13f,.23f));
            var rug=Box("Gallery central runner",root,new Vector3(-3.2f,.024f,-8.0f),new Vector3(8.4f,.018f,1.9f),AssetDatabase.LoadAssetAtPath<Material>("Assets/Props/ManorSurfaces/M_ManorRug.mat"));var surface=rug.AddComponent<SurfaceType>();surface.kind=SurfaceType.Kind.Carpet;surface.noiseMultiplier=.22f;surface.loudnessMultiplier=.25f;
        }
    }
}
