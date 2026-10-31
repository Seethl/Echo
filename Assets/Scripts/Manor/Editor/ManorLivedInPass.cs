using System;
using System.Linq;
using Echo.Interactions;
using Echo.Radio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
using static Echo.Manor.Editor.ManorSetDress;

namespace Echo.Manor.Editor
{
    public static class ManorLivedInPass
    {
        private const string Marker="Lived-in manor and animal vinyl revision";
        internal const string Folder="Assets/Props/ManorLivedIn";
        private static Transform root;
        private static Material wood,inkMetal;
        [MenuItem("Echo/Наповнити маєток та оновити платівки")]
        public static void ApplyAndSave()
        {
            if(EditorSceneManager.GetActiveScene().path!=ManorPrototypeBuilder.ScenePath)throw new InvalidOperationException("Open ManorPrototype first.");
            Apply();ConfigureCamera();ConfigurePickupTarget();RefineRecordPositions();foreach(var png in System.IO.Directory.GetFiles(Folder,"*.png")){string name=System.IO.Path.GetFileNameWithoutExtension(png);TextureMaterial(name,name.StartsWith("Vinyl_"));}
            ManorVinylTablePolish.Apply();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
        [MenuItem("Echo/Перерозкласти декоративний безлад (Undo)")]
        public static void RegenerateDecorations()
        {
            if(EditorSceneManager.GetActiveScene().path!=ManorPrototypeBuilder.ScenePath)throw new InvalidOperationException("Open ManorPrototype first.");
            var owner=GameObject.Find(Marker);if(owner==null)throw new InvalidOperationException("Apply the lived-in pass first.");
            var old=owner.transform.Find("Seeded shelf contents and paper clusters");if(old!=null)Undo.DestroyObjectImmediate(old.gameObject);
            ManorClutterScatter.Generate(owner.transform);var created=owner.transform.Find("Seeded shelf contents and paper clusters");Undo.RegisterCreatedObjectUndo(created.gameObject,"Regenerate manor decorations");EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
        public static void Apply()
        {
            if(GameObject.Find(Marker)!=null)return;
            root=Group(Marker);wood=AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder+"/Aged timber.mat");inkMetal=AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder+"/Dark iron.mat");
            FixCameraAndDoor();ConfigurePickupTarget();PlaceBeds();MountNotes();MakeVinylPuzzle();ManorClutterScatter.Generate(root);
        }
        private static void FixCameraAndDoor()
        {
            ConfigureCamera();
            var door=GameObject.Find("Study Door").GetComponent<HingedDoor>();door.openAngle=-90;door.hinge.position+=Vector3.forward*.12f;
        }
        private static void RefineRecordPositions()
        {
            foreach(var record in Object.FindObjectsByType<SymbolButton>().Where(b=>b.playOnSubmit))
            {int slot=record.symbolId=="bird"?0:record.symbolId=="wolf"?1:2;record.transform.position=new Vector3(8.2f+slot*.7f,1.3f,19.9f);}
        }
        private static void ConfigurePickupTarget()
        {
            // A small visual key should not require a 4cm-high aim target. Keep its depth behind the closed painting.
            var key=GameObject.Find("Apothecary Key");var collider=key.GetComponent<BoxCollider>();collider.size=new Vector3(.20f,.20f,.12f);EditorUtility.SetDirty(collider);
        }
        private static void ConfigureCamera()
        {
            var controller=Object.FindAnyObjectByType<FPSController>();var camera=controller.GetComponentInChildren<Camera>();camera.nearClipPlane=.03f;controller.crouchingCameraY=-.08f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(controller);PrefabUtility.RecordPrefabInstancePropertyModifications(camera);EditorUtility.SetDirty(controller);EditorUtility.SetDirty(camera);
        }
        private static void PlaceBeds()
        {
            foreach(var t in Object.FindObjectsByType<Transform>().ToArray())
            {
                if(t.name=="Bed base"||t.name=="Bed mattress"||t.name=="Folded bed blanket"||(t.name=="Pillow"&&t.position.y<2))t.position+=new Vector3(-.4f,0,.35f);
                if(t.name=="Bed frame"||t.name=="Mattress"||t.name=="Bed headboard"||(t.name=="Pillow"&&t.position.y>4))t.position+=Vector3.left*1.05f;
            }
            var coat=GameObject.Find("Recognisable coat beside the bed");if(coat!=null){coat.transform.position=new Vector3(-16.2f,1.2f,-4.22f);coat.transform.rotation=Quaternion.Euler(0,90,0);}
            var rail=GameObject.Find("Wall coat rail");rail.transform.position=new Vector3(-16.2f,1.8f,-4.15f);rail.transform.rotation=Quaternion.Euler(0,90,0);
        }
        private static Material TextureMaterial(string name,bool cutout=false)
        {
            string path=Folder+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};AssetDatabase.CreateAsset(mat,path);}
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/"+name+".png");if(texture==null)throw new InvalidOperationException("Clue texture not imported: "+name);
            mat.SetTexture("_BaseMap",texture);mat.SetTexture("_MainTex",texture);mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Smoothness",.08f);mat.SetFloat("_Metallic",0);
            if(cutout){mat.SetFloat("_AlphaClip",1);mat.EnableKeyword("_ALPHATEST_ON");mat.renderQueue=2450;}
            EditorUtility.SetDirty(mat);return mat;
        }
        private static GameObject LocalBox(string name,Transform parent,Vector3 pos,Vector3 size,Material mat,bool solid=false)
        {var go=Box(name,null,pos,size,mat,solid);go.transform.SetParent(parent,false);return go;}
        private static void Mount(ClueNote note,Vector3 pos,Quaternion rotation,string texture,bool wall)
        {
            note.transform.SetParent(root,true);note.transform.SetPositionAndRotation(pos,rotation);note.transform.localScale=Vector3.one;
            foreach(var r in note.GetComponentsInChildren<Renderer>())r.enabled=false;
            foreach(var c in note.GetComponentsInChildren<Collider>())c.enabled=false;
            var collider=note.GetComponent<BoxCollider>();if(collider==null)collider=note.gameObject.AddComponent<BoxCollider>();collider.enabled=true;collider.center=Vector3.zero;collider.size=new Vector3(.25f,.34f,.018f);
            LocalBox("Written clue paper",note.transform,Vector3.zero,new Vector3(.25f,.34f,.008f),AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder+"/Linen and pages.mat"));
            var face=GameObject.CreatePrimitive(PrimitiveType.Quad);face.name="Visible ink - real clue";Object.DestroyImmediate(face.GetComponent<Collider>());face.transform.SetParent(note.transform,false);face.transform.localPosition=Vector3.back*.006f;face.transform.localScale=new Vector3(.25f,.34f,1);face.GetComponent<Renderer>().sharedMaterial=TextureMaterial(texture);
            if(wall)foreach(float x in new[]{-.105f,.105f})LocalBox("Paper pin",note.transform,new Vector3(x,.14f,-.013f),new Vector3(.012f,.012f,.008f),inkMetal);
        }
        private static void MountNotes()
        {
            foreach(var note in Object.FindObjectsByType<ClueNote>())
            {
                switch(note.name)
                {
                    case "Service Card":Mount(note,new Vector3(-16.9f,.892f,-9.65f),Quaternion.Euler(90,0,8),"Note820",false);break;
                    case "Gallery Frequency Card":Mount(note,new Vector3(1,1.48f,-4.125f),Quaternion.identity,"Note940",true);break;
                    case "Library Frequency Card":Mount(note,new Vector3(17.1f,.835f,13.75f),Quaternion.Euler(90,0,-9),"Note1060",false);note.text="Фонотека: {frequency}.\nДослухай звуки тварин на радіо й повтори порядок на вінілових платівках схованки. Кожна платівка також відтворює свій звук.\nR — повтор запису; J — почуті підказки.";break;
                    case "Winch Instructions":Mount(note,new Vector3(35.875f,1.55f,10),Quaternion.Euler(0,90,0),"Winch",true);break;
                    case "Garden Herbarium":Mount(note,new Vector3(31.05f,.835f,40.8f),Quaternion.Euler(90,0,12),"Herbarium",false);break;
                    case "Remedy Recipe":Mount(note,new Vector3(-5.55f,6.55f,-9.875f),Quaternion.Euler(0,180,0),"Recipe",true);note.title="РЕЦЕПТ МІКСТУРИ";break;
                }
            }
        }
        private static void MakeVinylPuzzle()
        {
            foreach(string file in new[]{"WolfHowl","MonkeyCall","BirdChirp"})
            {
                var importer=(AudioImporter)AssetImporter.GetAtPath("Assets/Audio/ManorAnimals/"+file+".wav");var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;settings.preloadAudioData=true;importer.defaultSampleSettings=settings;importer.forceToMono=true;importer.SaveAndReimport();
            }
            var puzzle=Object.FindAnyObjectByType<SymbolPuzzle>();puzzle.useRunOrder=false;puzzle.solution=new[]{"wolf","monkey","bird"};puzzle.symbolIds=new[]{"wolf","monkey","bird"};puzzle.symbolNames=new[]{"Вовк","Мавпа","Пташка"};puzzle.sequenceGapSeconds=.70f;
            foreach(var text in Object.FindObjectsByType<TextMesh>().Where(t=>t.name.StartsWith("Symbol Name")).ToArray())text.gameObject.SetActive(false);
            foreach(var button in Object.FindObjectsByType<SymbolButton>())
            {
                if(button.demonstration){button.gameObject.SetActive(false);continue;}
                string id=button.symbolId=="moon"?"wolf":button.symbolId=="tree"?"monkey":"bird";int i=Array.IndexOf(puzzle.symbolIds,id);button.symbolId=id;button.symbolName=puzzle.symbolNames[i];button.name="Vinyl "+id;button.playOnSubmit=true;
                button.rhythm=id=="wolf"?"виття вовка":id=="monkey"?"вигуки мавпи":"цвірінькання пташки";
                string file=id=="wolf"?"WolfHowl":id=="monkey"?"MonkeyCall":"BirdChirp";button.rhythmClip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/ManorAnimals/"+file+".wav");
                if(button.rhythmClip==null)throw new InvalidOperationException("Missing animal recording: "+file);
                foreach(var child in button.transform.Cast<Transform>().ToArray())child.gameObject.SetActive(false);
                var oldRenderer=button.GetComponent<Renderer>();if(oldRenderer!=null)oldRenderer.enabled=false;
                button.transform.localScale=Vector3.one;button.transform.SetPositionAndRotation(new Vector3(8.2f+(id=="bird"?0:id=="wolf"?1:2)*.70f,1.3f,19.9f),Quaternion.identity);
                var collider=button.GetComponent<BoxCollider>();collider.center=Vector3.zero;collider.size=new Vector3(.54f,.54f,.08f);
                var visual=Group("Vinyl record visual",button.transform);
                var body=Shape("Black vinyl rim",null,Vector3.zero,new Vector3(.54f,.012f,.54f),inkMetal,PrimitiveType.Cylinder);body.transform.SetParent(visual,false);body.transform.localRotation=Quaternion.Euler(90,0,0);
                var face=GameObject.CreatePrimitive(PrimitiveType.Quad);face.name="Grooves and animal label";Object.DestroyImmediate(face.GetComponent<Collider>());face.transform.SetParent(visual,false);face.transform.localPosition=Vector3.back*.018f;face.transform.localScale=new Vector3(.52f,.52f,1);face.GetComponent<Renderer>().sharedMaterial=TextureMaterial("Vinyl_"+id,true);
                var feedback=button.GetComponent<SymbolFeedback>();feedback.face=visual;feedback.indicator=body.GetComponent<Renderer>();
            }
            var channel=AssetDatabase.LoadAssetAtPath<RadioChannel>("Assets/RadioData/Library1060.asset");channel.payload=RadioChannel.Payload.LibrarySounds;channel.voiceClip=null;channel.transcript="Слухай порядок: {sounds}. Повтори його на вінілових платівках. R — повторити запис.";EditorUtility.SetDirty(channel);
        }
    }
}
