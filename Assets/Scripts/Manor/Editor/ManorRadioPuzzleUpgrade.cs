using Echo.Audio;
using Echo.Interactions;
using Echo.Radio;
using UnityEditor;
using UnityEngine;

namespace Echo.Manor.Editor
{
    public static class ManorRadioPuzzleUpgrade
    {
        public static void ApplyToScene()
        {
            // Configure existing objects/assets without changing their identities or GUIDs.
            var keypad=Object.FindAnyObjectByType<KeypadLock>();keypad.useRunCode=true;
            var puzzle=Object.FindAnyObjectByType<SymbolPuzzle>();puzzle.useRunOrder=true;
            var resident=Object.FindAnyObjectByType<EnemyInvestigator>();
            if(resident.patrolUnlockFlags==null||resident.patrolUnlockFlags.Length!=resident.patrolPoints.Length)
            {
                resident.patrolUnlockFlags=new string[resident.patrolPoints.Length];
                var zones=Object.FindObjectsByType<ManorZone>();
                for(int i=0;i<resident.patrolPoints.Length;i++)
                {
                    string flag="";
                    foreach(var zone in zones)if(resident.patrolPoints[i]!=null&&zone.Contains(resident.patrolPoints[i].position))
                        flag=zone.area==ManorArea.Library?"library.orientation.done":zone.area==ManorArea.Kitchen?"area.Kitchen":zone.area==ManorArea.Conservatory?"grate.open":zone.area==ManorArea.Yard?"yard.open":"";
                    resident.patrolUnlockFlags[i]=flag;
                }
            }
            var gallery=AssetDatabase.LoadAssetAtPath<RadioChannel>("Assets/RadioData/Gallery940.asset");
            gallery.payload=RadioChannel.Payload.GalleryCode;
            gallery.transcript="Службовий замок на поточну зміну: {code}. Повторіть ці цифри на механічному замку.";
            gallery.interruptedTranscript="Службовий замок на поточну зміну… [гуркіт заглушає цифри]. Закрийте віконницю й повторіть запис.";
            EditorUtility.SetDirty(gallery);
            var library=AssetDatabase.LoadAssetAtPath<RadioChannel>("Assets/RadioData/Library1060.asset");
            library.payload=RadioChannel.Payload.LibrarySounds;
            library.transcript="Послідовність схованки: {sounds}. Повторіть ці звуки символами. R — повторити запис.";
            EditorUtility.SetDirty(library);
            var galleryCard=GameObject.Find("Gallery Frequency Card").GetComponent<ClueNote>();
            galleryCard.text="Частота {frequency}. Новий код передають по радіо на кожну зміну.\nПрибери гілку й закрий віконницю, щоб розібрати цифри.\nРадіо — на столі доглядача. R — частота/повтор; F — живлення; J — почуті записи.";
            galleryCard.fullTextFlag="";galleryCard.fullText="";
            var libraryCard=GameObject.Find("Library Frequency Card").GetComponent<ClueNote>();
            libraryCard.text="Частота {frequency}. Порядок звуків змінюється щозміни.\nПрослухай панелі, потім передачу. Повтори звуки символами на щитку.\nR — повтор запису. Почуті підказки зберігаються в J.";
            libraryCard.fullTextFlag="";libraryCard.fullText="";
            foreach(var button in Object.FindObjectsByType<SymbolButton>())
            {
                button.useAssociationSound=true;
                button.soundCue=button.symbolId=="moon"?PrototypeAudio.Cue.NightChime:button.symbolId=="tree"?PrototypeAudio.Cue.WoodKnock:PrototypeAudio.Cue.BirdCall;
                button.rhythm=button.symbolId=="moon"?"низький передзвін із довгим відлунням":button.symbolId=="tree"?"сухий скрип і два глухі удари":"два високі посвисти";
            }
            SetText("Bell Principle","ЗВУКОВІ ПАНЕЛІ · E — ПОСЛУХАТИ\nЗістав характер звуку з радіозаписом.\nТі самі символи — на схованці.");
            SetText("Cache Principle","СХОВАНКА З РУКІВ’ЯМ\nПослухай 1060 на радіо. Повтори звуки символами.");
            SetText("Yard Arrival Direction","ВОРОТА ↖ · САРАЙ →\nЛанцюг натягнуто. Потрібні підпора й болторіз.");
            var gate=GameObject.Find("Gate Chain").GetComponent<ToolBarrier>();
            gate.holdSeconds=3;gate.requiredFlag="gates.braced";
            gate.preparationMessage="Ланцюг натягнуто вагою стулки. Підіпри ворота ломом біля правої петлі, потім ріж.";
            if(GameObject.Find("Radio Puzzle Pass v1")!=null)return;
            var root=new GameObject("Radio Puzzle Pass v1").transform;
            var brass=Mat("Material_Simple_Yellow");var dark=Mat("Material_Simple_BlueDark");var wood=Mat("Material_SandLumpy");
            var brace=Box("Gate Brace Socket",root,new Vector3(18.65f,1.1f,47.6f),new Vector3(.45f,.5f,.25f),brass,true).AddComponent<FlagInteraction>();
            brace.prompt="E — підперти стулку ломом";brace.requiredKeyId="tool.crowbar";brace.setFlag="gates.braced";brace.hideWhenDone=true;brace.noiseRadius=5;
            brace.missingMessage="Потрібен лом із зимового саду. Він розвантажить натягнутий ланцюг.";
            brace.doneMessage="Стулку підперто, ланцюг розвантажено. Тримай E на ланцюгу для зрізу. Можна відступити після кожної секції.";
            var wedge=new GameObject("Crowbar supporting gate").transform;wedge.SetParent(root,false);
            var strut=Box("Supporting crowbar shaft",wedge,new Vector3(18.1f,.55f,47.35f),new Vector3(.08f,1.15f,.08f),brass,false);strut.transform.rotation=Quaternion.Euler(-25,0,18);
            Box("Supporting crowbar foot",wedge,new Vector3(18.3f,.07f,47.1f),new Vector3(.4f,.1f,.3f),dark,false);
            brace.completedVisual=wedge.gameObject;wedge.gameObject.SetActive(false);
            Sign(root,"Brace instruction","ПІДПОРА ЛОМОМ · E\nСпершу розвантаж стулку, потім ріж ланцюг",new Vector3(18.9f,2.15f,47.5f),.075f);
            Sign(root,"Cutting instruction","БОЛТОРІЗ: УТРИМУЙ E\nКожен зріз потребує часу й створює шум.\nВідпусти й сховайся; готові зрізи зберігаються.",new Vector3(17,2.75f,47.7f),.073f);
            Box("Gate retreat cover",root,new Vector3(13.7f,1.1f,43.8f),new Vector3(2.2f,2.2f,1.3f),wood,true);
        }
        private static void SetText(string name,string content)
        {var go=GameObject.Find(name);if(go!=null)go.GetComponent<TextMesh>().text=content;}
        private static Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/"+name+".mat");
        private static GameObject Box(string name,Transform parent,Vector3 p,Vector3 size,Material mat,bool solid)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.position=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;if(!solid)Object.DestroyImmediate(go.GetComponent<Collider>());return go;}
        private static void Sign(Transform parent,string name,string content,Vector3 p,float size)
        {
            var label=new GameObject(name,typeof(TextMesh)).GetComponent<TextMesh>();label.transform.SetParent(parent,false);label.transform.position=p;
            label.text=content;label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=64;label.characterSize=size;label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;
            label.GetComponent<Renderer>().sharedMaterial=label.font.material;
            Box(name+" backing",parent,p+Vector3.forward*.04f,new Vector3(3.8f,.65f,.025f),Mat("Material_Simple_BlueDark"),false);
        }
    }
}
