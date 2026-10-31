using Echo.Audio;
using Echo.Interactions;
using UnityEditor;
using UnityEngine;

namespace Echo.Manor.Editor
{
    // An additive, idempotent scene migration. Never regenerates the team's saved scene.
    public static class ManorUsabilityUpgrade
    {
        private static Transform root;
        private static Material dark,light,brass,wood;
        public static void ApplyToScene()
        {
            if(GameObject.Find("Usability Guidance v1")!=null)return;
            root=new GameObject("Usability Guidance v1").transform;
            dark=Mat("Material_Simple_BlueDark");light=Mat("Material_WhiteGrid");brass=Mat("Material_Simple_Yellow");wood=Mat("Material_SandLumpy");
            MakeLibrary();MakeWayfinding();MakeMechanisms();WidenApproaches();
        }
        private static void MakeLibrary()
        {
            var puzzle=Object.FindAnyObjectByType<SymbolPuzzle>();
            puzzle.transform.position=new Vector3(8.9f,1.3f,19.9f);
            foreach(var old in Object.FindObjectsByType<TextMesh>())
                if(old.name.StartsWith("Label ")&&(old.text=="Місяць"||old.text=="Дерево"||old.text=="Птах"))Object.DestroyImmediate(old.gameObject);
            var board=Box("Cache Input Board",root,new Vector3(9.25f,1.25f,20.13f),new Vector3(3.7f,1.55f,.15f),dark,true);
            Sign("Cache Principle","СХОВАНКА З РУКІВ’ЯМ\nПочуй послідовність. Повтори її символами.",new Vector3(9.1f,2.95f,20.02f),Quaternion.identity,.075f);
            puzzle.inputDisplay=Sign("Cache Input Slots","[ — ]  [ — ]  [ — ]",new Vector3(9.1f,2.35f,20.02f),Quaternion.identity,.075f);
            foreach(var button in Object.FindObjectsByType<SymbolButton>())
            {
                int index=System.Array.IndexOf(puzzle.symbolIds,button.symbolId);
                button.transform.SetPositionAndRotation(button.demonstration?new Vector3(4.25f,1.5f,13+index*2.1f):new Vector3(8.2f+index*.7f,1.3f,19.9f),button.demonstration?Quaternion.Euler(0,-90,0):Quaternion.identity);
                DecorateSymbol(button);
            }
            var reset=Box("Cache Reset",root,new Vector3(10.4f,1.3f,19.9f),new Vector3(.5f,.5f,.16f),dark,true).AddComponent<SymbolReset>();reset.puzzle=puzzle;
            Sign("Reset Label","СКИНУТИ",new Vector3(10.4f,1.3f,19.80f),Quaternion.identity,.047f);
            Sign("Bell Principle","ДЗВІНОЧКИ · E — ПОСЛУХАТИ\nКожен знак має власний ритм.\nТакі самі знаки — на схованці.",new Vector3(4.23f,2.6f,15.1f),Quaternion.Euler(0,-90,0),.075f);
            var card=GameObject.Find("Library Frequency Card");card.transform.position=new Vector3(11.5f,1.35f,19.9f);
            Sign("Library Card Label","КАРТКА 1060\nE — прочитати / J — згадати",new Vector3(11.5f,1.95f,20.02f),Quaternion.identity,.055f);
            var enemy=Object.FindAnyObjectByType<EnemyInvestigator>();
            enemy.GetComponent<NoiseListener>().usePowerScale=true;
            for(int i=0;i<enemy.patrolUnlockFlags.Length;i++)if(enemy.patrolUnlockFlags[i]=="area.Library")enemy.patrolUnlockFlags[i]="library.orientation.done";
        }
        private static void DecorateSymbol(SymbolButton button)
        {
            var face=new GameObject("Symbol face - replace independently").transform;
            face.SetPositionAndRotation(button.transform.position,button.transform.rotation);face.SetParent(button.transform,true);
            var plate=Box("Pressable rim",face,button.transform.position,button.demonstration?new Vector3(.85f,.85f,.07f):new Vector3(.58f,.58f,.07f),dark,false);
            // A collider on the rim forwards hits to the existing interaction, like the shutter.
            var rim=plate.AddComponent<BoxCollider>();rim.size=Vector3.one;
            plate.transform.rotation=button.transform.rotation;
            var icon=new GameObject("Matching symbol "+button.symbolId).transform;icon.SetParent(face,false);icon.localPosition=new Vector3(0,0,-.16f);icon.localScale=Vector3.one*(button.demonstration?.56f:.40f);
            if(button.symbolId=="moon")
            {
                Vector2[] points={new Vector2(.20f,.45f),new Vector2(-.18f,.37f),new Vector2(-.40f,.12f),new Vector2(-.37f,-.22f),new Vector2(-.1f,-.43f),new Vector2(.23f,-.38f),new Vector2(.04f,-.18f),new Vector2(-.02f,.1f),new Vector2(.20f,.45f)};
                for(int i=1;i<points.Length;i++)Stroke(icon,points[i-1],points[i]);
            }
            else if(button.symbolId=="tree")
            {
                Stroke(icon,new Vector2(0,-.45f),new Vector2(0,.35f));
                Stroke(icon,new Vector2(0,.45f),new Vector2(-.4f,-.15f));Stroke(icon,new Vector2(-.4f,-.15f),new Vector2(.4f,-.15f));Stroke(icon,new Vector2(.4f,-.15f),new Vector2(0,.45f));
            }
            else
            {
                Stroke(icon,new Vector2(-.48f,.2f),new Vector2(-.2f,.08f));Stroke(icon,new Vector2(-.2f,.08f),new Vector2(0,-.2f));Stroke(icon,new Vector2(0,-.2f),new Vector2(.2f,.08f));Stroke(icon,new Vector2(.2f,.08f),new Vector2(.48f,.2f));
            }
            var title=Sign("Symbol Name "+button.symbolId,button.symbolName,button.transform.position+button.transform.rotation*new Vector3(0,button.demonstration?-.62f:-.48f,-.12f),button.transform.rotation,.065f);title.transform.SetParent(face,true);
            var feedback=button.gameObject.AddComponent<SymbolFeedback>();feedback.face=face;feedback.indicator=plate.GetComponent<Renderer>();
        }
        private static void Stroke(Transform parent,Vector2 a,Vector2 b)
        {
            var stroke=GameObject.CreatePrimitive(PrimitiveType.Cube);stroke.name="Glyph stroke";stroke.transform.SetParent(parent,false);Object.DestroyImmediate(stroke.GetComponent<Collider>());
            stroke.transform.localPosition=new Vector3((a.x+b.x)/2,(a.y+b.y)/2,0);stroke.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);
            stroke.transform.localScale=new Vector3(Vector2.Distance(a,b)+.04f,.08f,.035f);stroke.GetComponent<Renderer>().sharedMaterial=light;
        }
        private static void MakeWayfinding()
        {
            Sign("Hall First Direction","БІБЛІОТЕКА ←\nДва проходи в дальній стіні\nКилим тихіший. J — журнал і допомога.",new Vector3(9.35f,2.4f,-5.1f),Quaternion.Euler(0,90,0),.085f);
            foreach(float x in new[]{8f,20f})Sign("Library Door Sign "+x,"БІБЛІОТЕКА",new Vector3(x,3.1f,7.82f),Quaternion.identity,.12f);
            Sign("Kitchen Direction","КУХНЯ →\nЛебідка відкриває прохід у сад",new Vector3(23.8f,2.55f,11),Quaternion.Euler(0,90,0),.085f);
            Sign("Niche Tutorial","МІСЦЕ ДЛЯ РАДІО\n1140 — музика · Q — поставити\nОбійди джерело шуму іншим боком",new Vector3(5.25f,1.6f,2.6f),Quaternion.identity,.07f);
            Sign("Shortcut Sign","ДО ХОЛУ\nЗніми засув із цього боку",new Vector3(24.2f,2.3f,4),Quaternion.Euler(0,-90,0),.08f);
            Sign("Winch Instructions Readable","ЛЕБІДКА → РЕШІТКА\nУтримуй Space до подвійного клацання.\nВідпусти: стопор зафіксує ступінь.\nТри ступені · Escape — відійти",new Vector3(33,2.85f,13.75f),Quaternion.identity,.08f);
            Sign("Crowbar Label","ЛОМ\nДля заклиненого засуву навпроти",new Vector3(26,1.7f,17.05f),Quaternion.identity,.07f);
            Sign("Shed Tools Sign","ІНСТРУМЕНТИ\nБОЛТОРІЗ",new Vector3(28.86f,2.25f,37),Quaternion.Euler(0,90,0),.11f);
            Sign("Exit Arch Sign","ВИХІД З МАЄТКУ",new Vector3(17,3.2f,47.8f),Quaternion.identity,.14f);
            Sign("Yard Arrival Direction","ВОРОТА ↖ · САРАЙ →\nЛанцюг потребує болторіза",new Vector3(30,2.6f,32.2f),Quaternion.identity,.10f);
            Box("Exit arch",root,new Vector3(17,3.55f,48),new Vector3(4.2f,.22f,.25f),light,false);
        }
        private static void MakeMechanisms()
        {
            var winch=Object.FindAnyObjectByType<WinchMechanism>();
            var readout=new GameObject("Winch socket status",typeof(MechanismReadout)).GetComponent<MechanismReadout>();readout.transform.SetParent(root,false);readout.winch=winch;
            readout.display=Sign("Winch Socket Label","",new Vector3(33,1.9f,12.2f),Quaternion.identity,.07f);
            Box("Empty dark socket",root,new Vector3(32.8f,1.15f,11.63f),new Vector3(.17f,.17f,.035f),dark,false);
            readout.handleVisual=Box("Installed handle",root,new Vector3(32.8f,1.15f,11.5f),new Vector3(.32f,.08f,.11f),brass,false).transform;
            foreach(var barrier in Object.FindObjectsByType<ToolBarrier>())
            {
                bool gates=barrier.toolId=="tool.cutters";
                var status=new GameObject(gates?"Chain cut status":"Latch damage status",typeof(MechanismReadout)).GetComponent<MechanismReadout>();status.transform.SetParent(root,false);status.barrier=barrier;
                status.display=Sign(gates?"Gate Chain Readout":"Garden Latch Readout","",barrier.transform.position+new Vector3(0,1.0f,-.15f),Quaternion.identity,.075f);
                status.remainingSections=new Transform[barrier.requiredActions];
                foreach(var renderer in barrier.GetComponentsInChildren<Renderer>())renderer.enabled=false;
                for(int i=0;i<status.remainingSections.Length;i++)
                {
                    var piece=Box("Remaining section "+i,root,barrier.transform.position+new Vector3((i-(status.remainingSections.Length-1)*.5f)*.22f,0,-.10f),new Vector3(.18f,gates?.25f:.12f,.08f),brass,false);
                    piece.transform.rotation=Quaternion.Euler(0,0,gates?(i%2==0?25:-25):0);status.remainingSections[i]=piece.transform;
                }
                if(!gates)barrier.missingMessage="Потрібен лом: він лежить на столі біля входу в сад. Тріск засуву буде гучним.";
            }
            foreach(var cycle in Object.FindObjectsByType<AmbientCycle>())if(cycle.mode==AmbientCycle.Mode.Water)
            {
                var status=new GameObject("Water cycle status",typeof(MechanismReadout)).GetComponent<MechanismReadout>();status.transform.SetParent(root,false);status.water=cycle;
                status.display=Sign("Water Warning Label","",new Vector3(35.8f,1.5f,25.2f),Quaternion.identity,.075f);
            }
            MakeTool("Crowbar",false);MakeTool("Bolt Cutters",true);
        }
        private static void MakeTool(string name,bool cutters)
        {
            var tool=GameObject.Find(name);foreach(var r in tool.GetComponentsInChildren<Renderer>())r.enabled=false;
            var p=tool.transform.position;
            if(cutters)
            {
                for(int i=0;i<2;i++)
                {var part=Box("Cutter handle",tool.transform,p+new Vector3(i==0?-.13f:.13f,0,0),new Vector3(.085f,.07f,.48f),brass,false);part.transform.rotation=Quaternion.Euler(0,i==0?-20:20,0);}
                Box("Cutter jaws",tool.transform,p+new Vector3(0,0,.24f),new Vector3(.22f,.09f,.16f),dark,false);
            }
            else
            {Box("Crowbar shaft",tool.transform,p,new Vector3(.6f,.07f,.07f),brass,false);Box("Crowbar hooked end",tool.transform,p+new Vector3(-.27f,0,.08f),new Vector3(.07f,.07f,.22f),brass,false);}
        }
        private static void WidenApproaches()
        {
            Resize("Library Long Shelf",new Vector3(13,1.4f,15.3f),new Vector3(1.2f,2.8f,7.4f));
            Resize("Library North Shelf",new Vector3(19.7f,1.4f,18.5f),new Vector3(5,2.8f,1.1f));
            Resize("Kitchen Cover",new Vector3(26.8f,1.3f,7.7f),new Vector3(1.2f,2.6f,3));
            Resize("Kitchen Preparation Island",new Vector3(32.3f,.9f,6),new Vector3(2.6f,1.8f,2));
            Resize("Hall Central Cover",new Vector3(15,1.1f,-2),new Vector3(2.4f,2.2f,2.4f));
            Resize("West Plant Bed",new Vector3(26.5f,1,21),new Vector3(1.3f,2,4.2f));
            Resize("East Plant Bed",new Vector3(34.5f,1,22),new Vector3(1.3f,2,4.2f));
        }
        private static void Resize(string name,Vector3 position,Vector3 size)
        {var go=GameObject.Find(name);go.transform.position=position;go.transform.localScale=size;}
        private static Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/"+name+".mat");
        private static GameObject Box(string name,Transform parent,Vector3 position,Vector3 size,Material material,bool solid)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.position=position;go.transform.localScale=size;go.transform.SetParent(parent,true);go.GetComponent<Renderer>().sharedMaterial=material;if(!solid)Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        private static TextMesh Sign(string name,string content,Vector3 position,Quaternion rotation,float size)
        {
            var go=new GameObject(name,typeof(TextMesh));go.transform.SetParent(root,false);go.transform.SetPositionAndRotation(position,rotation);
            var text=go.GetComponent<TextMesh>();text.text=content;text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=64;text.characterSize=size;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=Color.white;go.GetComponent<Renderer>().sharedMaterial=text.font.material;
            var backing=Box(name+" contrast plate",go.transform,position+rotation*Vector3.forward*.04f,new Vector3(string.IsNullOrEmpty(content)?4:Mathf.Max(1.4f,LongestLine(content)*size*.43f),string.IsNullOrEmpty(content)?.7f:Mathf.Max(.4f,content.Split('\n').Length*size*1.2f),.03f),dark,false);backing.transform.rotation=rotation;
            return text;
        }
        private static int LongestLine(string text){int max=0;foreach(var line in text.Split('\n'))max=Mathf.Max(max,line.Length);return max;}
    }
}
