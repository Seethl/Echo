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
    public static class ManorUpperLibraryPolish
    {
        private const string Marker="Upper corridor, grounded stairs and readable library revision";
        private static Transform root;
        private static Material wood,cloth,dark;
        [MenuItem("Echo/Виправити верхню галерею та бібліотеку")]
        public static void ApplyAndSave()
        {
            if(EditorSceneManager.GetActiveScene().path!=ManorPrototypeBuilder.ScenePath)throw new InvalidOperationException("Open ManorPrototype first.");
            Apply();RefineInputPlaque();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
        public static void Apply()
        {
            if(GameObject.Find(Marker)!=null)return;
            root=Group(Marker);wood=Load("Aged timber");cloth=Load("Linen and pages");dark=Load("Dark iron");
            NarrowHallDoor();EncloseUpperCorridor();GroundStairRails();ReplacePrivacyScreen();LibraryAndAudio();
        }
        private static void RefineInputPlaque()
        {
            var puzzle=Object.FindAnyObjectByType<SymbolPuzzle>();puzzle.inputDisplay.transform.position=new Vector3(9.05f,1.86f,19.65f);puzzle.inputDisplay.characterSize=.014f;
            var plaque=GameObject.Find("Compact library input plaque");plaque.transform.position=new Vector3(9.05f,1.86f,19.71f);plaque.transform.localScale=new Vector3(2.4f,.25f,.045f);
            var board=GameObject.Find("Cache Input Board");board.transform.position=new Vector3(9.2f,1.4f,20.13f);board.transform.localScale=new Vector3(3.1f,1.15f,.15f);
        }
        private static Material Load(string name)=>AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder+"/"+name+".mat");
        private static GameObject LocalBox(string name,Transform parent,Vector3 p,Vector3 size,Material mat,bool solid=false)
        {var go=Box(name,null,p,size,mat,solid);go.transform.SetParent(parent,false);return go;}
        private static void NarrowHallDoor()
        {
            const float width=1.30f,height=2.55f;
            var door=GameObject.Find("Hall Code Door").GetComponent<HingedDoor>();door.hinge.position=new Vector3(4,0,-8+width/2);
            var panel=door.GetComponentsInChildren<BoxCollider>().First(c=>c.name=="Door Panel");panel.transform.localPosition=new Vector3(width/2,height/2,0);panel.transform.localScale=new Vector3(width,height,.13f);
            foreach(var t in door.GetComponentsInChildren<Transform>().Where(t=>t.name=="Repaired door visual"||t.name=="Door leaf model from main").ToArray())t.gameObject.SetActive(false);
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Props/ManorDoor/SM_ManorDoor.fbx").GetComponentsInChildren<Transform>().First(t=>t.name=="DoorLeaf").gameObject;
            Fit(source,door.hinge,"Human-scale hall door visual",new Vector3(width/2,height/2,0),new Vector3(width,height,.10f));
            if(door.navigationBlocker!=null){door.navigationBlocker.center=new Vector3(width/2,height/2,0);door.navigationBlocker.size=new Vector3(width+.05f,height,.35f);}
            GameObject.Find("Fixed frame Hall Code Door").SetActive(false);
            var frame=Group("Narrow fixed hall door frame",root);frame.SetPositionAndRotation(door.hinge.position,door.hinge.rotation);
            foreach(float x in new[]{-.075f,width+.075f})LocalBox("Hall door jamb",frame,new Vector3(x,height/2,0),new Vector3(.15f,height+.14f,.22f),wood);
            LocalBox("Hall door lintel",frame,new Vector3(width/2,height+.07f,0),new Vector3(width+.3f,.14f,.22f),wood);
            float opening=width+.05f,side=(2.4f-opening)/2;
            var blocks=new[]{new Bounds(new Vector3(4,1.35f,-8-(opening+side)/2),new Vector3(.2f,2.7f,side)),new Bounds(new Vector3(4,1.35f,-8+(opening+side)/2),new Vector3(.2f,2.7f,side)),new Bounds(new Vector3(4,2.65f,-8),new Vector3(.2f,.1f,opening))};
            SolidWallShell("Hall doorway infill",blocks,0);
        }
        private static void EncloseUpperCorridor()
        {
            // Enclose only the short west corridor; the landing inside the grand hall retains its balcony view.
            var walls=new[]{new Bounds(new Vector3(-2,6.15f,5.9f),new Vector3(.2f,3.5f,3)),new Bounds(new Vector3(1,6.15f,7.4f),new Vector3(6.2f,3.5f,.2f)),new Bounds(new Vector3(2.5f,6.15f,4.4f),new Vector3(3.0f,3.5f,.2f))};
            SolidWallShell("Upper corridor walls",walls,4.4f);
            Box("Upper corridor ceiling",root,new Vector3(1.05f,7.95f,5.9f),new Vector3(6.3f,.20f,3.2f),AssetDatabase.LoadAssetAtPath<Material>("Assets/Props/ManorSurfaces/M_ManorCeiling.mat"));
            foreach(var t in Object.FindObjectsByType<Transform>().ToArray())
            {
                if(t.name=="Balcony baluster"&&t.position.x<4.12f)t.gameObject.SetActive(false);
                if(t.name!="Balcony handrail"&&t.name!="Balcony guard collider")continue;
                if(Mathf.Abs(t.position.z-7.4f)<.01f){t.position=new Vector3(6.05f,t.position.y,t.position.z);t.localScale=new Vector3(3.9f,t.localScale.y,t.localScale.z);}
                else if(Mathf.Abs(t.position.z-4.4f)<.01f){t.position=new Vector3(4.65f,t.position.y,t.position.z);t.localScale=new Vector3(1.1f,t.localScale.y,t.localScale.z);}
            }
            // A former portrait sat in the open connection. Hang it on the new solid corridor wall.
            foreach(var t in Object.FindObjectsByType<Transform>().Where(t=>t.name=="Model - PF_DecorPortrait"&&Vector3.Distance(t.position,new Vector3(3.75f,6,6.55f))<2).ToArray())
            {t.RotateAround(new Vector3(3.75f,6,6.55f),Vector3.up,90);t.position+=new Vector3(1.8f,6,7.24f)-new Vector3(3.75f,6,6.55f);}
        }
        private static void Beam(string name,Vector3 a,Vector3 b,float width,float height)
        {var beam=Box(name,root,(a+b)/2,new Vector3(width,height,(b-a).magnitude),wood,false);beam.transform.rotation=Quaternion.LookRotation(b-a);}
        private static void GroundStairRails()
        {
            foreach(var t in Object.FindObjectsByType<Transform>().Where(t=>t.name=="Stair baluster"||t.name=="Stair handrail").ToArray())t.gameObject.SetActive(false);
            foreach(float x in new[]{5.32f,7.28f})
            {
                Beam("Supported stair handrail",new Vector3(x,1.10f,-6.55f),new Vector3(x,5.48f,4.40f),.075f,.075f);
                Beam("Stair timber stringer",new Vector3(x,.13f,-6.55f),new Vector3(x,4.51f,4.4f),.13f,.18f);
                for(int i=0;i<22;i++)
                {
                    if(i%2!=0&&i!=21)continue;
                    float z=-6.35f+i*.5f,baseY=(i+1)*.2f-.02f,topY=1.18f+i*.2f;
                    Box("Grounded stair baluster",root,new Vector3(x,(baseY+topY)/2,z),new Vector3(.075f,topY-baseY,.075f),wood,false);
                }
            }
        }
        private static void ReplacePrivacyScreen()
        {
            GameObject.Find("Bedroom privacy screen").SetActive(false);
            var zone=Object.FindObjectsByType<ManorZone>().First(z=>z.area==ManorArea.Study);
            var screen=Group("Folding three-panel bedroom screen",zone.transform);screen.position=new Vector3(-7.45f,4.4f,-3.25f);
            for(int i=0;i<3;i++)
            {
                var panel=Group("Wood framed fabric screen",screen);panel.localPosition=new Vector3((i-1)*.53f,0,i==1?0:-.10f);panel.localRotation=Quaternion.Euler(0,(i-1)*18,0);
                LocalBox("Matte fabric panel",panel,new Vector3(0,.91f,0),new Vector3(.43f,1.34f,.025f),AssetDatabase.LoadAssetAtPath<Material>("Assets/Props/ManorSeating/M_ManorSeating_Burgundy.mat")??cloth);
                foreach(float x in new[]{-.25f,.25f})LocalBox("Screen upright",panel,new Vector3(x,.87f,0),new Vector3(.055f,1.74f,.065f),wood);
                foreach(float y in new[]{.22f,1.61f})LocalBox("Screen crossbar",panel,new Vector3(0,y,0),new Vector3(.55f,.055f,.065f),wood);
                LocalBox("Screen foot",panel,new Vector3(0,.035f,0),new Vector3(.48f,.07f,.32f),wood);
                LocalBox("Screen physical panel",panel,new Vector3(0,.87f,0),new Vector3(.55f,1.74f,.08f),wood,true).GetComponent<Renderer>().enabled=false;
            }
        }
        private static void LibraryAndAudio()
        {
            var puzzle=Object.FindAnyObjectByType<SymbolPuzzle>();puzzle.compactWorldDisplay=true;puzzle.cuePeak=.70f;puzzle.cuePlaybackVolume=.95f;puzzle.radioPlaybackVolume=1;puzzle.sequenceGapSeconds=.75f;
            foreach(var text in Object.FindObjectsByType<TextMesh>().ToArray())
            {
                var button=text.GetComponentInParent<SymbolButton>();
                if(text.name.StartsWith("Symbol Name")&&button!=null)
                {
                    if(!button.demonstration){text.gameObject.SetActive(false);continue;}
                    foreach(Transform child in text.transform)child.gameObject.SetActive(false);
                    text.transform.SetParent(root,true);text.transform.localScale=Vector3.one;text.characterSize=.018f;
                    text.transform.SetPositionAndRotation(button.transform.position+button.transform.rotation*new Vector3(0,-.55f,-.20f),button.transform.rotation);
                }
            }
            var display=puzzle.inputDisplay;foreach(Transform child in display.transform)child.gameObject.SetActive(false);
            display.transform.SetParent(root,true);display.transform.SetPositionAndRotation(new Vector3(9.05f,1.86f,19.65f),Quaternion.identity);display.transform.localScale=Vector3.one;display.characterSize=.014f;display.text="[ — ]  [ — ]  [ — ]";
            Box("Compact library input plaque",root,new Vector3(9.05f,1.86f,19.71f),new Vector3(2.40f,.25f,.045f),wood,false);
            var reset=GameObject.Find("Reset Label").GetComponent<TextMesh>();foreach(Transform child in reset.transform)child.gameObject.SetActive(false);
            reset.transform.SetParent(root,true);reset.transform.SetPositionAndRotation(new Vector3(10.4f,1.3f,19.65f),Quaternion.identity);reset.transform.localScale=Vector3.one;reset.characterSize=.014f;reset.text="Скинути";
            Object.FindAnyObjectByType<Footsteps>().playbackVolume=.5f;
            Object.FindAnyObjectByType<EnemyInvestigator>().footstepPlaybackVolume=.38f;
        }
        private static void SolidWallShell(string name,Bounds[] blocks,float floor)
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,bool lower)
            {int n=v.Count;v.AddRange(new[]{a,b,c,d});float lo=lower?.02f:.44f,hi=lower?.38f:.98f;uv.AddRange(new[]{new Vector2(.015f,lo),new Vector2(.985f,lo),new Vector2(.985f,hi),new Vector2(.015f,hi)});triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
            foreach(var block in blocks)
            {
                var physical=Box(name+" solid",root,block.center,block.size,wood);physical.GetComponent<Renderer>().enabled=false;
                var cuts=new List<float>{block.min.y,block.max.y};if(block.min.y<floor+1.15f&&block.max.y>floor+1.15f)cuts.Add(floor+1.15f);cuts.Sort();
                for(int i=0;i<cuts.Count-1;i++)
                {
                    var a=new Vector3(block.min.x,cuts[i],block.min.z);var b=new Vector3(block.max.x,cuts[i+1],block.max.z);bool low=(a.y+b.y)/2<floor+1.15f;
                    Quad(new Vector3(a.x,a.y,a.z),new Vector3(a.x,a.y,b.z),new Vector3(a.x,b.y,b.z),new Vector3(a.x,b.y,a.z),low);
                    Quad(new Vector3(b.x,a.y,b.z),new Vector3(b.x,a.y,a.z),new Vector3(b.x,b.y,a.z),new Vector3(b.x,b.y,b.z),low);
                    Quad(new Vector3(b.x,a.y,a.z),new Vector3(a.x,a.y,a.z),new Vector3(a.x,b.y,a.z),new Vector3(b.x,b.y,a.z),low);
                    Quad(new Vector3(a.x,a.y,b.z),new Vector3(b.x,a.y,b.z),new Vector3(b.x,b.y,b.z),new Vector3(a.x,b.y,b.z),low);
                    Quad(new Vector3(a.x,b.y,a.z),new Vector3(a.x,b.y,b.z),new Vector3(b.x,b.y,b.z),new Vector3(b.x,b.y,a.z),low);
                    Quad(new Vector3(a.x,a.y,b.z),new Vector3(a.x,a.y,a.z),new Vector3(b.x,a.y,a.z),new Vector3(b.x,a.y,b.z),low);
                }
            }
            var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,MaterialsFolder+"/"+name+".asset");
            var visual=new GameObject(name+" wall finish",typeof(MeshFilter),typeof(MeshRenderer));visual.transform.SetParent(root,false);visual.GetComponent<MeshFilter>().sharedMesh=mesh;visual.GetComponent<Renderer>().sharedMaterial=Load("Continuous wall atlas");
        }
    }
}
