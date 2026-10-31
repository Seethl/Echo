using System;
using System.Collections.Generic;
using System.Linq;
using Echo.Interactions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
using static Echo.Manor.Editor.ManorSetDress;

namespace Echo.Manor.Editor
{
    // Collision remains owned by the existing saved walls; this repairs only their visual shell.
    public static class ManorVisualRepair
    {
        private const string Marker="Manor visual repair and lived-in rooms v2";
        private static Transform root;
        private static Material wallMat;
        private struct Span {public Bounds b;public float floor;public bool xPlane;}
        [MenuItem("Echo/Виправити стіни та наповнити кімнати")]
        public static void ApplyAndSave()
        {
            if(EditorSceneManager.GetActiveScene().path!=ManorPrototypeBuilder.ScenePath)throw new InvalidOperationException("Open ManorPrototype first.");
            if(GameObject.Find(Marker)==null)Apply();else{Materials();root=GameObject.Find(Marker).transform;DoorFrames();}
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
        public static void Apply()
        {
            if(GameObject.Find(Marker)!=null)return;
            Materials();root=Group(Marker);
            RepairWalls();RepairFloors();DoorFrames();FillCaretaker();FillGallery();FillHall();FillLibrary();FillKitchen();FillGarden();FillYard();FillStudy();
            foreach(var enemy in Object.FindObjectsByType<EnemyInvestigator>(FindObjectsInactive.Include))
            {enemy.gameObject.SetActive(true);enemy.enabled=true;var catcher=enemy.GetComponent<EnemyCatchPlayer>();if(catcher!=null)catcher.enabled=true;}
            Debug.Log("MANOR_VISUAL_REPAIR_APPLIED resident active; collision walls preserved");
        }
        private static Bounds BoxBounds(BoxCollider c)
        {
            bool first=true;Bounds bounds=new Bounds();
            for(int i=0;i<8;i++)
            {var p=c.transform.TransformPoint(c.center+Vector3.Scale(c.size*.5f,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}
            return bounds;
        }
        private static void RepairWalls()
        {
            // Do not stretch complete decorative FBX modules over small lintels or stack skirting vertically.
            var spans=new List<Span>();
            foreach(var c in Object.FindObjectsByType<BoxCollider>())
            {
                if(!c.enabled||!c.name.StartsWith("Wall "))continue;
                var b=BoxBounds(c);if(b.size.y<.1f)continue;
                spans.Add(new Span{b=b,floor=c.name.StartsWith("Wall study")?4.4f:0,xPlane=b.size.x<b.size.z});
                var r=c.GetComponent<Renderer>();if(r!=null)r.enabled=false;
            }
            foreach(var t in Object.FindObjectsByType<Transform>())if(t.name=="Textured wall module")t.gameObject.SetActive(false);
            wallMat=Material("Continuous wall atlas",Color.white,"ManorWall_BaseColor");
            var groups=spans.GroupBy(s=>(s.xPlane?"X":"Z")+":"+Mathf.RoundToInt((s.xPlane?s.b.center.x:s.b.center.z)*1000)+":"+Mathf.RoundToInt(s.floor*1000)).OrderBy(g=>g.Key);
            int index=0;
            foreach(var group in groups)
            {
                var values=group.ToArray();bool xp=values[0].xPlane;float floor=values[0].floor;
                float fixedAxis=xp?values[0].b.center.x:values[0].b.center.z;
                float thick=values.Max(s=>xp?s.b.size.x:s.b.size.z);
                var ts=new SortedSet<float>();var ys=new SortedSet<float>();
                foreach(var s in values)
                {ts.Add(R(xp?s.b.min.z:s.b.min.x));ts.Add(R(xp?s.b.max.z:s.b.max.x));ys.Add(R(s.b.min.y));ys.Add(R(s.b.max.y));}
                // Small UV cells repeat only the relevant region of the existing plaster/wood atlas.
                for(float t=ts.Min;t<ts.Max;t+=2)ts.Add(R(t));
                for(float y=ys.Min;y<ys.Max;y+=1.5f)ys.Add(R(y));
                foreach(float y in new[]{floor+1.05f,floor+1.15f})if(y>ys.Min&&y<ys.Max)ys.Add(R(y));
                var tt=ts.ToArray();var yy=ys.ToArray();var occupied=new bool[tt.Length-1,yy.Length-1];
                for(int i=0;i<tt.Length-1;i++)for(int j=0;j<yy.Length-1;j++)
                {
                    float t=(tt[i]+tt[i+1])/2,y=(yy[j]+yy[j+1])/2;
                    occupied[i,j]=values.Any(s=>t>(xp?s.b.min.z:s.b.min.x)-.001f&&t<(xp?s.b.max.z:s.b.max.x)+.001f&&y>s.b.min.y-.001f&&y<s.b.max.y+.001f);
                }
                var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
                for(int i=0;i<tt.Length-1;i++)for(int j=0;j<yy.Length-1;j++)
                {
                    if(!occupied[i,j])continue;
                    float a=tt[i],b=tt[i+1],y0=yy[j],y1=yy[j+1],lo=fixedAxis-thick/2,hi=fixedAxis+thick/2;
                    Vector3 p0=P(xp,a,y0,lo),p1=P(xp,b,y0,lo),p2=P(xp,b,y1,lo),p3=P(xp,a,y1,lo);
                    Vector3 q0=P(xp,a,y0,hi),q1=P(xp,b,y0,hi),q2=P(xp,b,y1,hi),q3=P(xp,a,y1,hi);
                    bool wood=(y0+y1)/2<floor+1.15f;var rect=wood?new Rect(.015f,.02f,.97f,.36f):new Rect(.015f,.44f,.97f,.54f);
                    // Both faces have one layer even where two rooms share a collision wall.
                    Quad(vertices,uv,triangles,p0,p1,p2,p3,rect);Quad(vertices,uv,triangles,q3,q2,q1,q0,rect);
                    if(i==0||!occupied[i-1,j])Quad(vertices,uv,triangles,q0,p0,p3,q3,rect);
                    if(i==tt.Length-2||!occupied[i+1,j])Quad(vertices,uv,triangles,p1,q1,q2,p2,rect);
                    if(j==0||!occupied[i,j-1])Quad(vertices,uv,triangles,q0,q1,p1,p0,rect);
                    if(j==yy.Length-2||!occupied[i,j+1])Quad(vertices,uv,triangles,p3,p2,q2,q3,rect);
                }
                if(!xp)for(int i=0;i<triangles.Count;i+=3){int swap=triangles[i+1];triangles[i+1]=triangles[i+2];triangles[i+2]=swap;}
                wallMat.SetFloat("_Cull",2);
                SaveMesh("Continuous wall "+index++,vertices,uv,triangles,wallMat);
            }
        }
        private static float R(float x)=>Mathf.Round(x*1000)/1000;
        private static Vector3 P(bool xp,float t,float y,float f)=>xp?new Vector3(f,y,t):new Vector3(t,y,f);
        private static void Quad(List<Vector3> v,List<Vector2> uv,List<int> tr,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Rect r)
        {int n=v.Count;v.AddRange(new[]{a,b,c,d});uv.AddRange(new[]{new Vector2(r.xMin,r.yMin),new Vector2(r.xMax,r.yMin),new Vector2(r.xMax,r.yMax),new Vector2(r.xMin,r.yMax)});tr.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
        private static void SaveMesh(string name,List<Vector3> vertices,List<Vector2> uv,List<int> triangles,Material mat)
        {
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path=MaterialsFolder+"/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);mesh=old;EditorUtility.SetDirty(mesh);}
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=mat;
        }
        private static void RepairFloors()
        {
            foreach(var filter in Object.FindObjectsByType<MeshFilter>())
            {
                if(!filter.name.StartsWith("Floorboards "))continue;
                string sourceName=filter.name.Substring("Floorboards ".Length);var floor=GameObject.Find(sourceName);if(floor==null)continue;
                var b=BoxBounds(floor.GetComponent<BoxCollider>());var v=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();int row=0;
                for(float x=b.min.x;x<b.max.x-.001f;x+=.3f,row++)for(float z=b.min.z;z<b.max.z-.001f;z+=2)
                {
                    float x1=Mathf.Min(x+.3f,b.max.x),z1=Mathf.Min(z+2,b.max.z),y=b.max.y+.002f;
                    Quad(v,uv,triangles,new Vector3(x,y,z),new Vector3(x,y,z1),new Vector3(x1,y,z1),new Vector3(x1,y,z),new Rect(.045f+(row%3)*.004f,.13f,.15f,.17f));
                }
                var mesh=filter.sharedMesh;mesh.Clear();mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            }
        }
        private static void DoorFrames()
        {
            foreach(var door in Object.FindObjectsByType<HingedDoor>())
            {
                if(door.name.Contains("Cupboard")||door.name.Contains("Cache")||door.name.Contains("Apothecary"))continue;
                var collider=door.GetComponentsInChildren<BoxCollider>().FirstOrDefault();if(collider==null)continue;
                var panel=collider.transform;float width=Vector3.Scale(collider.size,panel.localScale).x,height=Vector3.Scale(collider.size,panel.localScale).y;
                if(GameObject.Find("Fixed frame "+door.name)==null)
                {
                    var frame=Group("Fixed frame "+door.name,root);frame.SetPositionAndRotation(door.hinge.position,door.hinge.rotation);
                    foreach(float x in new[]{-.075f,width+.075f})LocalBox("Door jamb",frame,new Vector3(x,height/2,0),new Vector3(.15f,height+.14f,.22f),timber);
                    LocalBox("Door lintel trim",frame,new Vector3(width/2,height+.07f,0),new Vector3(width+.30f,.14f,.22f),timber);
                }
                foreach(var visual in door.GetComponentsInChildren<Transform>().Where(t=>t.name=="Door leaf model from main"||t.name=="Repaired door visual").ToArray())visual.gameObject.SetActive(false);
                var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Props/ManorDoor/SM_ManorDoor.fbx");var source=model.GetComponentsInChildren<Transform>().First(t=>t.name=="DoorLeaf").gameObject;
                var fitted=Fit(source,door.hinge,"Repaired door visual",door.hinge.InverseTransformPoint(BoxBounds(collider).center),new Vector3(width,height,.10f));
                var bounds=BoundsOf(fitted);var expected=BoxBounds(collider);
                if(Vector3.Distance(bounds.center,expected.center)>.005f||Mathf.Abs(bounds.size.y-height)>.005f||Mathf.Max(bounds.size.x,bounds.size.z)>width+.02f)
                    throw new InvalidOperationException("Door visual does not match collider: "+door.name+" "+bounds);
            }
        }
        private static GameObject LocalBox(string name,Transform parent,Vector3 position,Vector3 size,Material mat,bool solid=false)
        {var go=Box(name,null,position,size,mat,solid);go.transform.SetParent(parent,false);return go;}
        private static void Crate(Transform parent,Vector3 p,Vector3 size,int variant=0)
        {
            var group=Group("Storage crate",parent);Box("Crate body",group,p+Vector3.up*size.y/2,size,timber);
            foreach(float x in new[]{-.36f,.36f})Box("Crate strap",group,p+new Vector3(x*size.x,size.y+.009f,0),new Vector3(.04f,.015f,size.z+.015f),dark,false);
            Box("Crate front slat",group,p+new Vector3(0,size.y*.4f,-size.z/2-.01f),new Vector3(size.x,.055f,.02f),gold,false);
        }
        private static void Chest(Transform parent,Vector3 p,float w=1.3f)
        {
            var g=Group("Travel chest",parent);Box("Chest body",g,p+Vector3.up*.33f,new Vector3(w,.66f,.65f),timber);
            Box("Chest lid",g,p+Vector3.up*.69f,new Vector3(w+.04f,.08f,.69f),timber,false);
            Box("Chest clasp",g,p+new Vector3(0,.51f,-.34f),new Vector3(.08f,.17f,.025f),gold,false);
        }
        private static void Wardrobe(Transform parent,Vector3 p,Vector3 size)
        {
            var g=Group("Paneled cabinet",parent);Box("Cabinet body",g,p+Vector3.up*size.y/2,size,timber);
            for(int i=0;i<2;i++)
            {Box("Cabinet door panel",g,p+new Vector3((i==0?-.25f:.25f)*size.x,size.y*.52f,-size.z/2-.025f),new Vector3(size.x*.45f,size.y*.85f,.025f),timber,false);Box("Brass cabinet handle",g,p+new Vector3((i==0?-.05f:.05f)*size.x,size.y*.5f,-size.z/2-.06f),new Vector3(.035f,.15f,.045f),gold,false);}
            Box("Cabinet crown",g,p+Vector3.up*(size.y+.04f),new Vector3(size.x+.12f,.1f,size.z+.05f),timber,false);
        }
        private static void Shelf(Transform parent,Vector3 p,float w=2.0f)
        {
            var g=Group("Book and storage shelving",parent);Box("Shelf back",g,p+new Vector3(0,1.15f,.19f),new Vector3(w,2.3f,.06f),timber);
            foreach(float x in new[]{-w/2,w/2})Box("Shelf upright",g,p+new Vector3(x,1.15f,0),new Vector3(.10f,2.3f,.48f),timber);
            for(int i=0;i<4;i++)
            {Box("Shelf board",g,p+Vector3.up*(.15f+i*.68f),new Vector3(w,.08f,.48f),timber);Books(g,p+new Vector3(-w/2+.15f,.20f+i*.68f,-.1f),Mathf.Min(9,Mathf.FloorToInt(w/.12f)));}
        }
        private static void Bench(Transform parent,Vector3 p,float w=2)
        {
            var g=Group("Upholstered bench",parent);Box("Bench seat",g,p+Vector3.up*.48f,new Vector3(w,.2f,.65f),burgundy);
            Box("Bench back",g,p+new Vector3(0,.9f,.27f),new Vector3(w,.7f,.12f),timber);
            foreach(float x in new[]{-w/2+.15f,w/2-.15f})Box("Bench foot",g,p+new Vector3(x,.20f,0),new Vector3(.14f,.4f,.5f),timber);
        }
        private static void Bottle(Transform parent,Vector3 p)
        {Shape("Storage bottle",parent,p+Vector3.up*.12f,new Vector3(.1f,.12f,.1f),leaf,PrimitiveType.Cylinder);Box("Bottle cork",parent,p+Vector3.up*.25f,new Vector3(.04f,.04f,.04f),timber,false);}
        private static void TableLegs(string name)
        {
            var table=GameObject.Find(name);if(table==null)return;var b=BoxBounds(table.GetComponent<BoxCollider>());
            foreach(float x in new[]{b.min.x+.12f,b.max.x-.12f})foreach(float z in new[]{b.min.z+.12f,b.max.z-.12f})Box(name+" support leg",root,new Vector3(x,b.min.y/2,z),new Vector3(.09f,b.min.y,.09f),timber);
        }
        private static void Plant(Transform parent,Vector3 p,float height=1)
        {
            var g=Group("Potted foliage",parent);Shape("Plant pot",g,p+Vector3.up*.18f,new Vector3(.40f,.18f,.40f),timber,PrimitiveType.Cylinder,true);
            Box("Plant stem",g,p+Vector3.up*(.4f+height/2),new Vector3(.035f,height,.035f),leaf,false);
            for(int i=0;i<7;i++)
            {var frond=Shape("Foliage leaf",g,p+new Vector3(Mathf.Sin(i*2.3f)*.18f,.43f+i*height/8,Mathf.Cos(i*2.3f)*.18f),new Vector3(.42f,.065f,.18f),leaf,PrimitiveType.Sphere);frond.transform.rotation=Quaternion.Euler(0,i*130,20);}
        }
        private static void Frame(Transform parent,Vector3 p,float width=1,float height=.7f)
        {
            var g=Group("Framed wall keepsake",parent);Box("Picture frame",g,p,new Vector3(width,height,.055f),timber,false);
            Box("Faded picture",g,p+Vector3.back*.032f,new Vector3(width-.10f,height-.10f,.008f),linen,false);
            Shape("Faded portrait",g,p+new Vector3(0,.06f,-.045f),new Vector3(width*.22f,height*.4f,.008f),dark,PrimitiveType.Sphere);
        }
        private static void FillCaretaker()
        {
            var g=Group("Caretaker daily life",root);
            Box("Bed base",g,new Vector3(-18.7f,.27f,-5.6f),new Vector3(1.35f,.54f,2.15f),timber);
            Box("Bed mattress",g,new Vector3(-18.7f,.6f,-5.6f),new Vector3(1.30f,.16f,2.05f),linen);
            Box("Folded bed blanket",g,new Vector3(-18.7f,.7f,-5.85f),new Vector3(1.28f,.06f,1.2f),burgundy,false);
            Box("Pillow",g,new Vector3(-18.7f,.73f,-4.9f),new Vector3(.75f,.15f,.4f),linen,false);
            Wardrobe(g,new Vector3(-18.6f,0,-11.25f),new Vector3(1.7f,2.2f,.65f));
            Table("Caretaker washstand",g,new Vector3(-14.6f,0,-11.15f),1.5f,.7f);
            Shape("Wash basin",g,new Vector3(-14.6f,.87f,-11.15f),new Vector3(.48f,.05f,.35f),linen,PrimitiveType.Cylinder);Bottle(g,new Vector3(-14.0f,.82f,-11.15f));
            Chest(g,new Vector3(-11.9f,0,-10.95f),1.2f);Crate(g,new Vector3(-12.8f,0,-11.15f),new Vector3(.55f,.55f,.55f));
            Rug("Caretaker central worn rug",g,new Vector3(-15.5f,.023f,-7.1f),new Vector2(3.4f,2.7f));
            Chair(g,new Vector3(-17,0,-8.35f));Books(g,new Vector3(-17.9f,.90f,-9.75f),4);
            Box("Wall coat rail",g,new Vector3(-19.74f,1.8f,-7.2f),new Vector3(.10f,.12f,1.1f),timber,false);
            Box("Hanging coat",g,new Vector3(-19.55f,1.17f,-7.2f),new Vector3(.16f,.95f,.48f),dark,false);
            Frame(g,new Vector3(-15.3f,2.0f,-4.13f),1.2f,.8f);
            Plant(g,new Vector3(-11.2f,0,-4.8f),.7f);
        }
        private static void FillGallery()
        {
            var g=Group("Gallery furnishings",root);
            Table("Gallery console",g,new Vector3(-6.5f,0,-4.65f),2.2f,.65f);Bottle(g,new Vector3(-6,.82f,-4.65f));Books(g,new Vector3(-7.3f,.82f,-4.65f),5);
            Bench(g,new Vector3(-2.7f,0,-4.6f),2.4f);Chest(g,new Vector3(1.3f,0,-10.95f));
            Crate(g,new Vector3(-8.5f,0,-10.8f),new Vector3(.8f,.7f,.7f));Crate(g,new Vector3(-8.4f,.7f,-10.8f),new Vector3(.6f,.5f,.6f));
            foreach(float x in new[]{-7.5f,-5f,-.2f})Frame(g,new Vector3(x,2.1f,-4.13f));
            Plant(g,new Vector3(2.8f,0,-4.8f),.8f);
        }
        private static void FillHall()
        {
            var g=Group("Grand hall lived-in groups",root);
            Wardrobe(g,new Vector3(14,0,-11.3f),new Vector3(3.4f,1.05f,.75f));Bottle(g,new Vector3(13,1.12f,-11.3f));Books(g,new Vector3(14,1.12f,-11.3f),6);
            Bench(g,new Vector3(11,0,5.9f),2.1f);Chair(g,new Vector3(15.8f,0,5.6f),20);
            Box("Fireplace surround",g,new Vector3(14,1.15f,7.5f),new Vector3(2.7f,2.3f,.50f),stone);
            Box("Cold fireplace opening",g,new Vector3(14,.75f,7.23f),new Vector3(1.65f,1.4f,.04f),dark,false);
            Box("Fireplace mantel",g,new Vector3(14,2.36f,7.42f),new Vector3(3.0f,.15f,.75f),timber,false);
            Frame(g,new Vector3(14,3.2f,7.15f),1.5f,1.0f);
            Plant(g,new Vector3(22.9f,0,-10.5f),1.4f);Plant(g,new Vector3(22.8f,0,6.6f),1.4f);
            Chest(g,new Vector3(5.1f,0,-10.8f));Crate(g,new Vector3(21.5f,0,-10.7f),new Vector3(.8f,.8f,.75f));
            TableLegs("Radio Niche Table");
            var cover=GameObject.Find("Hall Central Cover");var b=BoxBounds(cover.GetComponent<BoxCollider>());
            foreach(float x in new[]{b.center.x-.55f,b.center.x+.55f})
            {Box("Central cabinet panel",g,new Vector3(x,1.15f,b.min.z-.018f),new Vector3(1.0f,1.9f,.03f),timber,false);Box("Central cabinet pull",g,new Vector3(x,1.1f,b.min.z-.055f),new Vector3(.04f,.17f,.04f),gold,false);}
        }
        private static void FillLibrary()
        {
            var g=Group("Library research and storage",root);Shelf(g,new Vector3(15,0,21.6f),3.1f);
            Table("Library side reading desk",g,new Vector3(22.3f,0,15.6f),1.5f,.9f);Chair(g,new Vector3(22.3f,0,14.55f),180);Books(g,new Vector3(21.75f,.83f,15.8f),6);
            Crate(g,new Vector3(22.4f,0,20.9f),new Vector3(1.0f,.65f,.65f));Crate(g,new Vector3(22.4f,.65f,20.9f),new Vector3(.75f,.4f,.55f));
            var ladder=Group("Library ladder",g);foreach(float x in new[]{23.05f,23.65f})Box("Ladder side",ladder,new Vector3(x,1.4f,18.7f),new Vector3(.07f,2.8f,.10f),timber,false);
            for(int i=0;i<8;i++)Box("Ladder rung",ladder,new Vector3(23.35f,.25f+i*.34f,18.7f),new Vector3(.6f,.06f,.1f),timber,false);
            Chest(g,new Vector3(16.5f,0,20.8f));
        }
        private static void FillKitchen()
        {
            var g=Group("Kitchen work and supplies",root);Wardrobe(g,new Vector3(27,0,.65f),new Vector3(3.3f,.83f,1.0f));
            Shape("Kitchen sink rim",g,new Vector3(27,.86f,.65f),new Vector3(.6f,.025f,.45f),silver,PrimitiveType.Cylinder);
            Box("Sink dark basin",g,new Vector3(27,.89f,.65f),new Vector3(.43f,.025f,.28f),dark,false);
            foreach(float x in new[]{25.8f,26.1f,28.0f})Bottle(g,new Vector3(x,.88f,.65f));
            Shelf(g,new Vector3(30.2f,0,.45f),1.4f);Crate(g,new Vector3(35.05f,0,6.7f),new Vector3(.75f,.65f,.85f));
            for(int i=0;i<4;i++){Plate(g,new Vector3(31.5f,.86f+i*.015f,6.4f));}
            Bottle(g,new Vector3(33.1f,.85f,6.6f));Box("Folded kitchen cloth",g,new Vector3(32.5f,.86f,6.5f),new Vector3(.4f,.035f,.3f),linen,false);
            var wheel=Shape("Manual winch wheel",g,new Vector3(33,1,11.58f),new Vector3(.55f,.035f,.55f),dark,PrimitiveType.Cylinder);wheel.transform.rotation=Quaternion.Euler(90,0,0);
            foreach(float angle in new[]{0f,60f,120f}){var spoke=Box("Winch spoke",g,new Vector3(33,1,11.53f),new Vector3(.48f,.04f,.04f),gold,false);spoke.transform.rotation=Quaternion.Euler(0,0,angle);}
        }
        private static void FillGarden()
        {
            var g=Group("Conservatory gardening life",root);TableLegs("Garden Tool Bench");
            Plant(g,new Vector3(25.5f,0,24.8f),1.2f);Plant(g,new Vector3(36.9f,0,19),1.3f);Plant(g,new Vector3(25.5f,0,18.4f),.8f);
            Bench(g,new Vector3(35.8f,0,26.5f),2);Crate(g,new Vector3(36.7f,0,16),new Vector3(.75f,.6f,.85f));
            Shape("Watering can",g,new Vector3(25.4f,1.0f,16.4f),new Vector3(.22f,.12f,.22f),dark,PrimitiveType.Cylinder);
            Box("Plant label pile",g,new Vector3(26.6f,.9f,16.5f),new Vector3(.2f,.03f,.3f),linen,false);
        }
        private static void FillYard()
        {
            var g=Group("Yard and potting stores",root);TableLegs("Shed Tool Table");
            foreach(float x in new[]{34.6f,35.4f})Crate(g,new Vector3(x,0,40.9f),new Vector3(.65f,.7f,.7f));
            Crate(g,new Vector3(34.6f,.7f,40.9f),new Vector3(.6f,.45f,.6f));Chest(g,new Vector3(30.7f,0,35.7f),1.1f);
            foreach(float x in new[]{34.3f,35.1f})Shape("Rain barrel",g,new Vector3(x,.6f,30.6f),new Vector3(.65f,.6f,.65f),timber,PrimitiveType.Cylinder,true);
            Plant(g,new Vector3(12.1f,0,32),1.3f);Plant(g,new Vector3(12.1f,0,42.5f),1.5f);Plant(g,new Vector3(36.5f,0,44.5f),1.3f);
            for(int i=0;i<5;i++)Box("Stacked firewood",g,new Vector3(16+i*.26f,.25f,29),new Vector3(.24f,.5f,.65f),timber);
            Bench(g,new Vector3(19.5f,0,46.5f),2.2f);
        }
        private static void FillStudy()
        {
            var g=Group("Study belongings",root);Shelf(g,new Vector3(-8,4.4f,3.8f),2.2f);
            Chest(g,new Vector3(-7.2f,4.4f,-7.8f));Wardrobe(g,new Vector3(-.5f,4.4f,-2.8f),new Vector3(1,.85f,.65f));
            Bottle(g,new Vector3(-.6f,5.3f,-2.8f));Crate(g,new Vector3(-8.9f,4.4f,-8.6f),new Vector3(.7f,.7f,.7f));
            Box("Bedroom privacy screen",g,new Vector3(-7.25f,5.25f,-3.7f),new Vector3(1.6f,1.7f,.12f),linen);
            Plant(g,new Vector3(-1.8f,4.4f,3.5f),.8f);Frame(g,new Vector3(-7.6f,6.4f,4.23f),1.0f,.75f);
        }
    }
}
