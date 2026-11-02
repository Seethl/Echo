using System.Linq;
using Echo.Environment;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;
namespace Echo.Manor.Editor
{
    public static class ManorWindowShafts
    {
        public const string Marker="Soft moon haze through actual windows";
        public const string Folder="Assets/Props/WindowMoonShafts";
        [MenuItem("Echo/М'які місячні промені з вікон")]
        public static void ApplyAndSave(){Apply();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();}
        public static void Apply()
        {
            if(EditorSceneManager.GetActiveScene().path!=ManorPrototypeBuilder.ScenePath)throw new System.InvalidOperationException("Open ManorPrototype first");
            System.IO.Directory.CreateDirectory(Folder);AssetDatabase.Refresh();var shader=Shader.Find("Echo/SoftWindowShaft");if(shader==null||!shader.isSupported)throw new System.Exception("Window shaft shader unavailable");
            var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Soft moon haze.mat");if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,Folder+"/Soft moon haze.mat");}
            var owner=GameObject.Find(Marker);if(owner==null)owner=new GameObject(Marker);foreach(Transform t in owner.transform)t.gameObject.SetActive(false);
            var shutters=Object.FindObjectsByType<GalleryWindowShutter>();
            foreach(var shutter in shutters)Make(owner.transform,material,shutter.transform.position,Vector3.forward,shutter,4.7f,1.7f,.16f);
            var windows=Object.FindObjectsByType<Transform>().Where(t=>t.name=="Moonlit window"||t.name=="Moonlit window placeholder").ToArray();
            foreach(var window in windows)
            {
                var glass=window.GetComponentsInChildren<Renderer>().FirstOrDefault(r=>r.enabled&&r.name.ToLowerInvariant().Contains("glass"));if(glass==null)continue;
                var p=glass.bounds.center;float floor=p.y>5?4.4f:0;float length=p.x<0?4.7f:p.z>14?5.8f:6.2f;Make(owner.transform,material,p,window.forward,null,length,p.y-floor-.3f,.16f);
            }
            foreach(var camera in Object.FindObjectsByType<Camera>())
            {var data=camera.GetUniversalAdditionalCameraData();data.requiresDepthTexture=true;EditorUtility.SetDirty(data);if(PrefabUtility.IsPartOfPrefabInstance(data))PrefabUtility.RecordPrefabInstancePropertyModifications(data);}
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
        static void Make(Transform root,Material material,Vector3 p,Vector3 inward,GalleryWindowShutter shutter,float length,float drop,float inset)
        {
            string id="Window haze "+Mathf.RoundToInt(p.x*100)+"_"+Mathf.RoundToInt(p.y*100)+"_"+Mathf.RoundToInt(p.z*100);var old=root.Find(id);var go=old!=null?old.gameObject:new GameObject(id,typeof(MeshFilter),typeof(MeshRenderer),typeof(WindowMoonShaft));go.transform.SetParent(root,false);go.SetActive(true);
            go.transform.SetPositionAndRotation(p+inward*inset,Quaternion.LookRotation(inward,Vector3.up));
            var shaft=go.GetComponent<WindowMoonShaft>();shaft.width=1.45f;shaft.height=1.8f;shaft.length=length;shaft.drop=drop;shaft.sideDrift=.32f;shaft.spread=1.35f;shaft.opacity=.1f;shaft.density=.035f;shaft.shutter=shutter;
            shaft.windowLight=Object.FindObjectsByType<Light>().Where(l=>l.type==LightType.Spot&&l.name.ToLowerInvariant().Contains("window")&&Vector3.Distance(l.transform.position,p)<4).OrderBy(l=>Vector3.Distance(l.transform.position,p)).FirstOrDefault();
            go.transform.localScale=new Vector3(shaft.width,shaft.height,shaft.length);
            string path=Folder+"/"+id+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh==null){mesh=new Mesh{name=id+" soft volume"};AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
            float d=drop/shaft.height,x=shaft.sideDrift/shaft.width,s=shaft.spread*.5f;
            mesh.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0),new Vector3(x-s,-d-s,1),new Vector3(x+s,-d-s,1),new Vector3(x+s,-d+s,1),new Vector3(x-s,-d+s,1)};
            mesh.triangles=new[]{0,3,2,0,2,1,4,5,6,4,6,7,0,1,5,0,5,4,3,7,6,3,6,2,0,4,7,0,7,3,1,2,6,1,6,5};mesh.RecalculateNormals();mesh.RecalculateBounds();go.GetComponent<MeshFilter>().sharedMesh=mesh;
            var display=go.GetComponent<MeshRenderer>();display.sharedMaterial=material;display.shadowCastingMode=ShadowCastingMode.Off;display.receiveShadows=false;display.lightProbeUsage=LightProbeUsage.Off;display.reflectionProbeUsage=ReflectionProbeUsage.Off;shaft.Refresh(true);
            EditorUtility.SetDirty(mesh);EditorUtility.SetDirty(shaft);EditorUtility.SetDirty(display);
        }
    }
}
