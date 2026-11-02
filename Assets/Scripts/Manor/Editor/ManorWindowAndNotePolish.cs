using System.Linq;
using Echo.Interactions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
namespace Echo.Manor.Editor
{
    public static class ManorWindowAndNotePolish
    {
        public const string Folder="Assets/Props/CubbyMusiciansNote";
        public const string NoteName="Hidden musicians note under cubby pillow";
        public const string Message="музиканти здоров, ви попали в гру!!!";

        static void CorrectHallWindow()
        {
            var window=Object.FindObjectsByType<Transform>().Single(t=>t.name=="Moonlit window placeholder"&&Mathf.Abs(t.position.x-23.84f)<.05f&&Mathf.Abs(t.position.y-3.1f)<.05f&&(Mathf.Abs(t.position.z-3.8f)<.05f||Mathf.Abs(t.position.z-.5f)<.05f));
            window.position=new Vector3(23.84f,3.1f,.5f);EditorUtility.SetDirty(window);
            var light=GameObject.Find("Hall north window").GetComponent<Light>();light.transform.SetPositionAndRotation(new Vector3(23.55f,3.1f,.5f),Quaternion.LookRotation(new Vector3(15,.2f,-2.3f)-new Vector3(23.55f,3.1f,.5f)));
            var shaft=Object.FindObjectsByType<Echo.Environment.WindowMoonShaft>(FindObjectsInactive.Include).Single(s=>s.name=="Window haze 2384_310_380"||s.name=="Window haze 2384_310_50");shaft.name="Window haze 2384_310_50";shaft.transform.position=new Vector3(23.68f,3.1f,.5f);shaft.windowLight=light;shaft.Refresh(true);EditorUtility.SetDirty(shaft);EditorUtility.SetDirty(light.transform);
        }
        public static void AlignStudyBeam()
        {
            var light=GameObject.Find("Study west window").GetComponent<Light>();var shaft=Object.FindObjectsByType<Echo.Environment.WindowMoonShaft>(FindObjectsInactive.Include).Single(s=>s.name=="Window haze -984_610_-550");
            if(light.transform.forward.y>=-.01f)throw new System.Exception("Study window light must point down toward its floor");
            var target=light.transform.position+light.transform.forward*((4.4f-light.transform.position.y)/light.transform.forward.y);
            shaft.transform.position=light.transform.position;var local=shaft.transform.InverseTransformDirection(target-shaft.transform.position);
            shaft.length=local.z;shaft.drop=-local.y;shaft.sideDrift=local.x;shaft.alignWithWindowCookie=true;shaft.windowLight=light;shaft.transform.localScale=new Vector3(shaft.width,shaft.height,shaft.length);
            var mesh=shaft.GetComponent<MeshFilter>().sharedMesh;float x=shaft.sideDrift/shaft.width,d=shaft.drop/shaft.height,s=shaft.spread*.5f;
            mesh.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0),new Vector3(x-s,-d-s,1),new Vector3(x+s,-d-s,1),new Vector3(x+s,-d+s,1),new Vector3(x-s,-d+s,1)};mesh.RecalculateNormals();mesh.RecalculateBounds();shaft.Refresh(true);EditorUtility.SetDirty(mesh);EditorUtility.SetDirty(shaft);
        }

        [MenuItem("Echo/Узгодити вікна та записка музикантам")]
        public static void ApplyAndSave(){Apply();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();}
        public static void Apply()
        {
            if(EditorSceneManager.GetActiveScene().path!=ManorPrototypeBuilder.ScenePath)throw new System.InvalidOperationException("Open ManorPrototype first");
            CorrectHallWindow();AlignStudyBeam();
            var importer=(TextureImporter)AssetImporter.GetAtPath(Folder+"/MusiciansNote.png");importer.sRGBTexture=true;importer.wrapMode=TextureWrapMode.Clamp;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=1024;importer.SaveAndReimport();
            var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Musicians note ink.mat");if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,Folder+"/Musicians note ink.mat");}
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/MusiciansNote.png"));material.SetColor("_BaseColor",Color.white);material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",0);EditorUtility.SetDirty(material);
            var pillow=GameObject.Find("Cubby pillow");if(pillow!=null&&pillow.GetComponent<Collider>()==null){var pillowCollider=pillow.AddComponent<BoxCollider>();pillowCollider.size=Vector3.one;EditorUtility.SetDirty(pillowCollider);}
            var room=GameObject.Find(ManorKitchenPolish.Marker);if(room==null)throw new System.Exception("Under-stair room missing");
            var go=Object.FindObjectsByType<ClueNote>(FindObjectsInactive.Include).FirstOrDefault(n=>n.clueId=="easter-egg.musicians")?.gameObject;
            if(go==null){go=GameObject.CreatePrimitive(PrimitiveType.Quad);Object.DestroyImmediate(go.GetComponent<Collider>());go.AddComponent<BoxCollider>();go.AddComponent<ClueNote>();}
            go.name=NoteName;go.transform.SetParent(room.transform,false);go.transform.SetPositionAndRotation(new Vector3(5.975f,.424f,3.65f),Quaternion.Euler(90,0,-8));go.transform.localScale=Vector3.one;
            var filter=go.GetComponent<MeshFilter>();var original=GameObject.CreatePrimitive(PrimitiveType.Quad);filter.sharedMesh=original.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(original);
            go.transform.localScale=new Vector3(.20f,.24f,1);var collider=go.GetComponent<BoxCollider>();collider.size=new Vector3(1,1,.006f);collider.center=Vector3.zero;
            var display=go.GetComponent<MeshRenderer>();display.sharedMaterial=material;display.shadowCastingMode=ShadowCastingMode.Off;
            var note=go.GetComponent<ClueNote>();note.clueId="easter-egg.musicians";note.title="ЗАПИСКА ДЛЯ МУЗИКАНТІВ";note.text=Message;note.frequency=0;note.onReadFlag="";
            EditorUtility.SetDirty(note);EditorUtility.SetDirty(collider);EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }
}
