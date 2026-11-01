using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Echo.Manor.Editor
{
    // Editor-only construction helpers. Furniture models can be replaced below these scene roots.
    internal static class ManorSetDress
    {
        internal const string MaterialsFolder = "Assets/Materials/Manor";
        internal static Material timber, plaster, stone, burgundy, gold, silver, linen, leaf, bloom, dark;
        internal static void Materials()
        {
            System.IO.Directory.CreateDirectory(MaterialsFolder); AssetDatabase.Refresh();
            timber = Material("Aged timber", new Color(.75f,.71f,.65f), "ManorDoor_BaseColor");
            // Sample the wood grain inside a panel of the existing atlas, excluding plaster and mouldings.
            timber.SetTextureScale("_BaseMap",new Vector2(.16f,.17f));timber.SetTextureOffset("_BaseMap",new Vector2(.045f,.13f));
            plaster = AssetDatabase.LoadAssetAtPath<Material>("Assets/Props/ManorWall/M_ManorWall.mat");
            stone = Material("Muted tile", new Color(.85f,.85f,.80f), "ManorStoneFloor_BaseColor");
            stone.SetTextureScale("_BaseMap",new Vector2(.24f,.24f));stone.SetTextureOffset("_BaseMap",new Vector2(.005f,.005f));
            burgundy = Material("Burgundy rug", new Color(.21f,.045f,.052f));
            gold = Material("Dull brass", new Color(.39f,.29f,.13f), null, .55f);
            silver = Material("Silver utensils", new Color(.62f,.66f,.68f), null, .8f);
            linen = Material("Linen and pages", new Color(.61f,.57f,.46f));
            leaf = Material("Garden leaves", new Color(.60f,.72f,.55f), "Tile_Grass");
            bloom = Material("Pale garden petals", new Color(.7f,.56f,.84f));
            dark = Material("Dark iron", new Color(.065f,.075f,.08f), null, .5f);
        }
        internal static Material Material(string name, Color color, string textureName=null, float metallic=0)
        {
            string path=MaterialsFolder+"/"+name+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            mat.SetColor("_BaseColor",color);mat.SetFloat("_Metallic",metallic);mat.SetFloat("_Smoothness",metallic>0?.35f:.12f);
            if(textureName!=null)
            {
                string texturePath=AssetDatabase.FindAssets(textureName+" t:Texture2D").Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p=>System.IO.Path.GetFileNameWithoutExtension(p)==textureName);
                if(texturePath!=null)mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
            }
            EditorUtility.SetDirty(mat);return mat;
        }
        internal static GameObject Box(string name,Transform parent,Vector3 position,Vector3 size,Material mat,bool solid=true)
        {return Shape(name,parent,position,size,mat,PrimitiveType.Cube,solid);}
        internal static GameObject Shape(string name,Transform parent,Vector3 position,Vector3 size,Material mat,PrimitiveType type,bool solid=false)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetPositionAndRotation(position,Quaternion.identity);go.transform.localScale=size;go.transform.SetParent(parent,true);go.GetComponent<Renderer>().sharedMaterial=mat;
            if(!solid)Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        internal static Transform Group(string name,Transform parent=null)
        {var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;}
        internal static void Table(string name,Transform parent,Vector3 p,float width,float depth)
        {
            var root=Group(name,parent);Box("Table top",root,p+Vector3.up*.77f,new Vector3(width,.09f,depth),timber);
            foreach(float x in new[]{-1f,1f})foreach(float z in new[]{-1f,1f})Box("Table leg",root,p+new Vector3(x*(width/2-.12f),.37f,z*(depth/2-.12f)),new Vector3(.10f,.74f,.10f),timber);
        }
        internal static void Chair(Transform parent,Vector3 p,float yaw=0)
        {
            var group=Group("Chair",parent);
            Box("Seat",group,p+Vector3.up*.46f,new Vector3(.48f,.08f,.47f),timber);
            Box("Chair back",group,p+new Vector3(0,.82f,.2f),new Vector3(.48f,.65f,.06f),timber);
            foreach(float x in new[]{-.18f,.18f})foreach(float z in new[]{-.18f,.18f})Box("Chair leg",group,p+new Vector3(x,.22f,z),new Vector3(.055f,.44f,.055f),timber);
            group.position=p;foreach(Transform child in group)child.position-=p;group.rotation=Quaternion.Euler(0,yaw,0);
        }
        internal static void Rug(string name,Transform parent,Vector3 center,Vector2 size)
        {
            var rug=Box(name,parent,center,new Vector3(size.x,.018f,size.y),burgundy,false);
            var surface=rug.AddComponent<SurfaceType>();surface.kind=SurfaceType.Kind.Carpet;surface.noiseMultiplier=.22f;surface.loudnessMultiplier=.25f;
            // The surface needs a thin physical contact collider for the existing footsteps ray.
            rug.AddComponent<BoxCollider>();
            foreach(float sign in new[]{-1f,1f})
            {
                Box("Rug woven border",parent,center+new Vector3(sign*(size.x/2-.09f),.011f,0),new Vector3(.08f,.005f,size.y-.08f),gold,false);
                Box("Rug woven border",parent,center+new Vector3(0,.011f,sign*(size.y/2-.09f)),new Vector3(size.x-.08f,.005f,.08f),gold,false);
            }
        }
        internal static void Plate(Transform parent,Vector3 p)
        {Shape("Plate",parent,p,new Vector3(.25f,.012f,.25f),linen,PrimitiveType.Cylinder);}
        internal static void Cup(Transform parent,Vector3 p)
        {
            Shape("Cup",parent,p+Vector3.up*.045f,new Vector3(.085f,.045f,.085f),linen,PrimitiveType.Cylinder);
            Box("Cup handle",parent,p+new Vector3(.055f,.05f,0),new Vector3(.04f,.045f,.018f),linen,false);
        }
        internal static Transform Spoon(Transform parent,Vector3 p)
        {
            var root=Group("Spoon visual",parent);
            Box("Spoon handle",root,p+new Vector3(-.035f,0,0),new Vector3(.105f,.008f,.015f),silver,false);
            Shape("Spoon bowl",root,p+new Vector3(.045f,.002f,0),new Vector3(.065f,.01f,.04f),silver,PrimitiveType.Sphere);
            return root;
        }
        internal static void Flower(Transform parent,Vector3 p)
        {
            Box("Flower stem",parent,p+Vector3.up*.12f,new Vector3(.012f,.24f,.012f),leaf,false);
            for(int i=0;i<5;i++)
            {float a=i*Mathf.PI*2/5;Shape("Five pale petals",parent,p+new Vector3(Mathf.Cos(a)*.045f,.25f,Mathf.Sin(a)*.045f),new Vector3(.065f,.025f,.065f),bloom,PrimitiveType.Sphere);}
            Shape("Flower centre",parent,p+Vector3.up*.26f,new Vector3(.035f,.025f,.035f),gold,PrimitiveType.Sphere);
            var blade=Box("Opposite leaves",parent,p+Vector3.up*.1f,new Vector3(.15f,.015f,.04f),leaf,false);blade.transform.rotation=Quaternion.Euler(0,25,15);
        }
        internal static void Books(Transform parent,Vector3 p,int count)
        {
            for(int i=0;i<count;i++)
            {var book=Box("Book",parent,p+new Vector3(i*.09f,.13f,0),new Vector3(.065f,.26f+(i%3)*.025f,.19f),i%3==0?burgundy:i%3==1?timber:leaf,false);book.transform.rotation=Quaternion.Euler(0,0,i==count-1?12:0);}
        }
        internal static void Floorboards(GameObject floor,Transform parent)
        {
            string name="Floorboards "+floor.name;if(GameObject.Find(name)!=null)return;
            var bounds=floor.GetComponent<Collider>().bounds;
            var vertices=new System.Collections.Generic.List<Vector3>();var uv=new System.Collections.Generic.List<Vector2>();var triangles=new System.Collections.Generic.List<int>();
            int strip=0;
            for(float x=bounds.min.x;x<bounds.max.x-.001f;x+=.3f,strip++)
            for(float z=bounds.min.z;z<bounds.max.z-.001f;z+=2)
            {
                float endX=Mathf.Min(x+.3f,bounds.max.x),endZ=Mathf.Min(z+2,bounds.max.z),y=bounds.max.y+.001f;int v=vertices.Count;
                vertices.AddRange(new[]{new Vector3(x,y,z),new Vector3(x,y,endZ),new Vector3(endX,y,endZ),new Vector3(endX,y,z)});
                float offset=.04f+(strip%3)*.005f;
                uv.AddRange(new[]{new Vector2(offset,.13f),new Vector2(offset,.30f),new Vector2(offset+.16f,.30f),new Vector2(offset+.16f,.13f)});
                triangles.AddRange(new[]{v,v+1,v+2,v,v+2,v+3});
            }
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path=MaterialsFolder+"/"+name+".asset";AssetDatabase.CreateAsset(mesh,path);
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;
            string matPath=MaterialsFolder+"/Floorboard atlas.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(mat==null){mat=new Material(timber);mat.SetTextureScale("_BaseMap",Vector2.one);mat.SetTextureOffset("_BaseMap",Vector2.zero);AssetDatabase.CreateAsset(mat,matPath);}
            go.GetComponent<MeshRenderer>().sharedMaterial=mat;floor.GetComponent<Renderer>().enabled=false;
        }
        internal static void Candies(Transform parent,Vector3 p)
        {
            var candy=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Material_Candy.mat");
            for(int i=0;i<5;i++)
            {var pos=p+new Vector3((i%3)*.055f,0,(i/3)*.06f);Shape("Wrapped sweet (decoration)",parent,pos,new Vector3(.04f,.025f,.025f),candy,PrimitiveType.Sphere);foreach(float x in new[]{-.029f,.029f})Box("Wrapper",parent,pos+Vector3.right*x,new Vector3(.018f,.018f,.026f),linen,false);}
        }
        internal static Bounds BoundsOf(GameObject go)
        {
            // Renderer.bounds may still contain the previous frame's transform in editor migrations.
            bool first=true;var result=new Bounds();
            foreach(var filter in go.GetComponentsInChildren<MeshFilter>())
            {
                if(filter.sharedMesh==null)continue;var b=filter.sharedMesh.bounds;
                for(int i=0;i<8;i++)
                {
                    var p=filter.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
                    if(first){result=new Bounds(p,Vector3.zero);first=false;}else result.Encapsulate(p);
                }
            }
            return result;
        }
        internal static GameObject Fit(GameObject source,Transform parent,string name,Vector3 center,Vector3 size,bool uniform=false)
        {
            var normalized=new GameObject(name);var go=Object.Instantiate(source);go.name="Imported visual";go.transform.SetParent(normalized.transform,false);go.transform.localPosition=Vector3.zero;go.transform.localRotation=Quaternion.identity;go.transform.localScale=source.transform.lossyScale;
            foreach(var c in go.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
            foreach(var c in go.GetComponentsInChildren<MonoBehaviour>())Object.DestroyImmediate(c);
            var bounds=BoundsOf(normalized);var scale=new Vector3(size.x/Mathf.Max(.0001f,bounds.size.x),size.y/Mathf.Max(.0001f,bounds.size.y),size.z/Mathf.Max(.0001f,bounds.size.z));
            if(uniform)scale=Vector3.one*Mathf.Min(scale.x,Mathf.Min(scale.y,scale.z));
            // Scale above imported rotations, in the target axes.
            normalized.transform.localScale=scale;bounds=BoundsOf(normalized);normalized.transform.position=center-bounds.center;
            normalized.transform.SetParent(parent,false);return normalized;
        }
    }
}
