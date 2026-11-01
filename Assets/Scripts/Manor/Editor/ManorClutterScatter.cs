using System;
using System.Collections.Generic;
using System.Linq;
using Echo.Interactions;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
using static Echo.Manor.Editor.ManorSetDress;

namespace Echo.Manor.Editor
{
    // Editor-only seeded clustered scatter. No runtime spawning, physics simulation or random gameplay items.
    public static class ManorClutterScatter
    {
        private static readonly HashSet<string> Furniture=new HashSet<string>{"Service Cupboard","Library Cache","Kitchen apothecary cabinet","Book and storage shelving","Paneled cabinet","Study wardrobe","Gallery Entry Cover","Kitchen Cover","Library Long Shelf","Library North Shelf"};
        private static readonly string[] Kinds={"BookStack","Jar","Mug","Bottle","Parcel"};
        private static List<Bounds> reserved;
        private static Transform root;
        private static ManorClutterProfile profile;
        public static void Generate(Transform parent)
        {
            const string path=ManorLivedInPass.Folder+"/ClutterProfile.asset";
            profile=AssetDatabase.LoadAssetAtPath<ManorClutterProfile>(path);
            if(profile==null){profile=ScriptableObject.CreateInstance<ManorClutterProfile>();AssetDatabase.CreateAsset(profile,path);}
            root=Group("Seeded shelf contents and paper clusters",parent);
            var previous=GameObject.Find("Caretaker cupboard contents");if(previous!=null)foreach(Transform child in previous.transform)child.gameObject.SetActive(false);
            reserved=Object.FindObjectsByType<Interactable>().Where(i=>i is KeyPickup||i is ClueNote||i is TreatmentWorkbench||i is RemedyDelivery).Select(i=>new Bounds(i.transform.position,new Vector3(.55f,.45f,.55f))).ToList();
            int shelves=0,props=0,papers=0;
            foreach(var owner in Object.FindObjectsByType<Transform>().Where(t=>Furniture.Contains(t.name)).ToArray())
            {
                var triangles=new List<Bounds>();var planes=new Dictionary<int,Bounds>();
                foreach(var f in owner.GetComponentsInChildren<MeshFilter>())
                {
                    var renderer=f.GetComponent<Renderer>();if(renderer==null||!renderer.enabled||!renderer.gameObject.activeInHierarchy||f.sharedMesh==null)continue;
                    var vertices=f.sharedMesh.vertices;var indices=f.sharedMesh.triangles;
                    for(int i=0;i<indices.Length;i+=3)
                    {
                        var a=f.transform.TransformPoint(vertices[indices[i]]);var b=f.transform.TransformPoint(vertices[indices[i+1]]);var c=f.transform.TransformPoint(vertices[indices[i+2]]);var bounds=new Bounds(a,Vector3.zero);bounds.Encapsulate(b);bounds.Encapsulate(c);triangles.Add(bounds);
                        var normal=Vector3.Cross(b-a,c-a);if(normal.y<=0||normal.normalized.y<.98f||normal.magnitude*.5f<.075f)continue;
                        int key=Mathf.RoundToInt(a.y*1000);if(planes.TryGetValue(key,out var old)){old.Encapsulate(bounds);planes[key]=old;}else planes.Add(key,bounds);
                    }
                }
                float floor=triangles.Count>0&&triangles.Min(b=>b.min.y)>3?4.4f:0;
                float top=triangles.Count>0?triangles.Max(b=>b.max.y):0;
                var tops=planes.Values.Where(b=>b.center.y>floor+.25f&&b.center.y<top-.10f&&b.size.x>.25f&&b.size.z>.25f).OrderBy(b=>b.center.y).ToArray();
                var centre=triangles.Count>0?(new Vector3(triangles.Min(b=>b.min.x),floor,triangles.Min(b=>b.min.z))+new Vector3(triangles.Max(b=>b.max.x),floor,triangles.Max(b=>b.max.z)))/2:owner.position;
                var rng=new System.Random(StableSeed(owner.name+"/"+owner.parent?.name,centre));
                foreach(var shelf in tops)
                {
                    shelves++;int placed=0;var accepted=new List<Bounds>();float ceiling=tops.Where(b=>b.center.y>shelf.center.y+.1f).Select(b=>b.center.y).DefaultIfEmpty(top).Min();
                    for(int attempt=0;attempt<65&&placed<profile.propsPerShelf;attempt++)
                    {
                        string kind=Kinds[rng.Next(Kinds.Length)];float height=kind=="BookStack"?.07f+(float)rng.NextDouble()*.08f:kind=="Parcel"?.12f:.15f+(float)rng.NextDouble()*.12f;
                        height=Mathf.Min(height,ceiling-shelf.center.y-.08f);if(height<.055f)continue;
                        var size=kind=="BookStack"?new Vector3(.26f,height,.18f):kind=="Parcel"?new Vector3(.23f,height,.18f):new Vector3(.12f,height,.12f);
                        var p=new Vector3(Mathf.Lerp(shelf.min.x+.17f,shelf.max.x-.17f,(float)rng.NextDouble()),shelf.center.y+.009f,Mathf.Lerp(shelf.min.z+.13f,shelf.max.z-.13f,(float)rng.NextDouble()));
                        var box=new Bounds(p+Vector3.up*(height/2+.015f),new Vector3(size.x*1.42f,height+.025f,size.z*1.42f));
                        if(box.min.x<shelf.min.x+.05f||box.max.x>shelf.max.x-.05f||box.min.z<shelf.min.z+.035f||box.max.z>shelf.max.z-.035f)continue;
                        if(reserved.Any(b=>b.Intersects(box))||triangles.Any(b=>b.Intersects(box))||accepted.Any(b=>b.Intersects(box)))continue;
                        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Props/ManorDecor/PF_Decor"+kind+".prefab");if(prefab==null)continue;
                        var visual=Fit(prefab,root,"Scattered shelf "+kind,p+Vector3.up*height/2,size);visual.transform.RotateAround(p+Vector3.up*height/2,Vector3.up,-24+(float)rng.NextDouble()*48);
                        var spacing=box;spacing.Expand(profile.minimumPropGap);accepted.Add(spacing);placed++;props++;
                    }
                }
            }
            var paper=AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder+"/Linen and pages.mat");
            foreach(var zone in Object.FindObjectsByType<ManorZone>())
            {
                float floor=zone.area==ManorArea.Study||zone.area==ManorArea.UpperGallery?4.4f:0;var rng=new System.Random(StableSeed(zone.name,zone.center));
                var min=zone.center-zone.size/2;var max=zone.center+zone.size/2;int count=0;var used=new List<Vector3>();
                // Neyman–Scott style clusters: Gaussian offsets around a few centres, plus rejection against surfaces and routes.
                var centres=new Vector2[3];for(int i=0;i<centres.Length;i++)centres[i]=new Vector2(Mathf.Lerp(min.x+.7f,max.x-.7f,(float)rng.NextDouble()),Mathf.Lerp(min.z+.7f,max.z-.7f,(float)rng.NextDouble()));
                for(int attempt=0;attempt<150&&count<profile.papersPerRoom;attempt++)
                {
                    var c=centres[attempt%centres.Length];var offset=Gaussian(rng)*profile.clusterSpread;var p=new Vector3(c.x+offset.x,floor+.012f,c.y+offset.y);
                    if(p.x<min.x+.45f||p.x>max.x-.45f||p.z<min.z+.45f||p.z>max.z-.45f||ReservedRoute(p))continue;
                    if(reserved.Any(b=>b.Contains(p))||used.Any(q=>Vector3.Distance(q,p)<.20f))continue;
                    if(!Physics.Raycast(p+Vector3.up*.16f,Vector3.down,out var hit,.30f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)||Mathf.Abs(hit.point.y-floor)>.08f)continue;
                    if(Physics.OverlapBox(p+Vector3.up*.10f,new Vector3(.13f,.055f,.15f),Quaternion.identity,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Any(col=>col.GetComponentInParent<ManorZone>()==null||!col.name.StartsWith("Floor ")))continue;
                    var sheet=Box("Scattered blank paper - decoration",root,p,new Vector3(.17f+(float)rng.NextDouble()*.09f,.002f,.23f+(float)rng.NextDouble()*.09f),paper,false);sheet.transform.rotation=Quaternion.Euler(0,(float)rng.NextDouble()*360,0);used.Add(p);count++;papers++;
                }
            }
            Debug.Log("LIVED_IN_SCATTER seed="+profile.seed+" shelves="+shelves+" props="+props+" papers="+papers);
            if(props<15)throw new InvalidOperationException("Shelf scatter produced too few grounded props; inspect surface detection.");
        }
        private static int StableSeed(string name,Vector3 p)
        {unchecked{int hash=profile.seed;foreach(char c in name)hash=hash*31+c;hash=hash*31+Mathf.RoundToInt(p.x*100);hash=hash*31+Mathf.RoundToInt(p.z*100);return hash&int.MaxValue;}}
        private static Vector2 Gaussian(System.Random r)
        {float radius=Mathf.Sqrt(-2*Mathf.Log(Mathf.Max(.0001f,(float)r.NextDouble()))),a=(float)r.NextDouble()*Mathf.PI*2;return new Vector2(radius*Mathf.Cos(a),radius*Mathf.Sin(a));}
        private static bool ReservedRoute(Vector3 p)
        {
            if(p.y>3)return (p.z>4.4f&&p.z<6.6f)||(Mathf.Abs(p.x+1)<.8f&&p.z>2.4f);
            return Mathf.Abs(p.z+8)<.70f&&p.x>-12&&p.x<9||Mathf.Abs(p.x-30)<.75f&&p.z>10&&p.z<32;
        }
    }
}
