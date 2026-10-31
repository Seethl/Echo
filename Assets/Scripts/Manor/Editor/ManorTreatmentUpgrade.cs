using System.Linq;
using Echo.Interactions;
using Echo.Radio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using static Echo.Manor.Editor.ManorSetDress;

namespace Echo.Manor.Editor
{
    public static class ManorTreatmentUpgrade
    {
        private const string Marker="Manor Treatment and Furnishing v1";
        private const string RecipeText="Основа — у шафці на кухні; ключ — нагорі.\nКвітка: п’ять блідих пелюсток, протилежні листки. Шукай у зовнішньому саду.\nНалий основу, додай квітку, розмішай срібною ложечкою зі столу в холі.\nЗалиш у його чашці в холі й відійди.";
        private const string PhotoText="На старому фото картина з місяцем відсунута від темної ніші. Зараз вона закриває це місце.\nТут безпечно. Двері можна зачинити.";
        private static Transform root,study;
        [MenuItem("Echo/Оновити маєток: кімнати та мікстура")]
        public static void ApplyAndSave()
        {
            if(EditorSceneManager.GetActiveScene().path!=ManorPrototypeBuilder.ScenePath)
                throw new System.InvalidOperationException("Open ManorPrototype before applying this migration.");
            ApplyToScene();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
        public static void ApplyToScene()
        {
            if(GameObject.Find(Marker)!=null)return;
            Materials();root=Group(Marker);
            var progress=Object.FindAnyObjectByType<ManorProgress>();progress.treatmentRoute=true;progress.statusSuffix="";
            Object.FindAnyObjectByType<InteractionUI>().controlsText="WASD — рух · Ctrl — тихо · E — дія · R — радіо · F — живлення · J — записи · Escape — назад / пауза";
            ReduceSigns();UpperFloor();DressRooms();TreatmentRoute();RemapLegacyMaterials();ImportVisuals();
            Object.FindAnyObjectByType<PortableRadio>().allowPlacement=false;
            RefineLightingAndTextures();
            var radio=Object.FindAnyObjectByType<PortableRadio>();radio.transform.localScale=new Vector3(.32f,.22f,.14f);
            GameObject.Find("Service Card").GetComponent<ClueNote>().text="Службова передача: {frequency}. Запасний ключ — у шафці біля дверей.\nR — налаштувати радіо; F — живлення; J — записи.\nМешканцеві потрібна допомога. Його старі дослідження залишилися в бібліотеці.";
            Debug.Log("MANOR_TREATMENT_UPGRADED");
        }
        public static void RefineLightingAndTextures()
        {
            Materials();
            Material("Moon painting",new Color(.68f,.65f,.56f),"Moon_Albedo");
            foreach(string area in new[]{"Caretaker","Gallery","Hall","Library","Kitchen","Conservatory","Yard"})
            {
                var mat=AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder+"/"+area+" floor tiling.mat");
                if(mat==null)continue;var source=area=="Kitchen"||area=="Conservatory"?stone:area=="Yard"?leaf:timber;
                mat.SetTexture("_BaseMap",source.GetTexture("_BaseMap"));EditorUtility.SetDirty(mat);
            }
            var parent=GameObject.Find(Marker).transform;
            GameObject.Find("Remedy Recipe").GetComponent<ClueNote>().text=RecipeText;
            GameObject.Find("Study Photograph").GetComponent<ClueNote>().text=PhotoText;
            foreach(string name in new[]{"Floor Caretaker","Floor Gallery","Floor Hall","Floor Library","Study upper floor","Upper landing floor"})
            {var floor=GameObject.Find(name);if(floor!=null)Floorboards(floor,parent);}
            // Keep the study doorway clear: the balcony rail only guards the exposed hall edge.
            foreach(var t in parent.GetComponentsInChildren<Transform>())
            {
                if(Mathf.Abs(t.position.z-4.4f)>.01f)continue;
                if(t.name=="Balcony handrail"||t.name=="Balcony guard collider")
                {t.position=new Vector3(3.1f,t.position.y,t.position.z);t.localScale=new Vector3(4.2f,t.localScale.y,t.localScale.z);}
                if(t.name=="Balcony baluster"&&t.position.x<1)t.gameObject.SetActive(false);
            }
            var studyRoot=GameObject.Find("Житлова кімната нагорі").transform;
            var studyZone=Object.FindObjectsByType<ManorZone>().First(z=>z.area==ManorArea.Study);
            if(studyZone.transform!=studyRoot&&studyZone.transform.parent!=studyRoot)
            {
                studyZone.transform.SetParent(studyRoot,true);
                foreach(var child in studyRoot.Cast<Transform>().ToArray())if(child!=studyZone.transform)child.SetParent(studyZone.transform,true);
            }
            var apothecary=GameObject.Find("Apothecary Door");if(apothecary!=null)apothecary.GetComponent<HingedDoor>().openAngle=105;
            MoonBounce(parent,"Hall west moon bounce",new Vector3(9,4,0),18,21);
            MoonBounce(parent,"Hall window moon bounce",new Vector3(22,4,-2),14,20);
            MoonBounce(parent,"Study window moon bounce",new Vector3(-7.5f,6.8f,-4),8,14);
            MoonBounce(parent,"Kitchen moon bounce",new Vector3(33,3,6),8,15);
            MoonBounce(parent,"Garden moon bounce",new Vector3(30,3,21),8,16);
            MoonBounce(parent,"Potting shed moon bounce",new Vector3(32,2.6f,38),5,10);
        }
        private static void MoonBounce(Transform parent,string name,Vector3 position,float intensity,float range)
        {
            var go=GameObject.Find(name);if(go==null)go=new GameObject(name,typeof(Light));
            go.transform.SetParent(parent,true);go.transform.position=position;
            var light=go.GetComponent<Light>();light.type=LightType.Point;light.color=new Color(.63f,.73f,1);light.intensity=intensity;light.range=range;light.shadows=LightShadows.None;
        }
        private static void ReduceSigns()
        {
            var puzzle=Object.FindAnyObjectByType<SymbolPuzzle>();
            foreach(var label in Object.FindObjectsByType<TextMesh>())
                if(label!=puzzle.inputDisplay&&!label.name.StartsWith("Symbol Name")&&label.name!="Reset Label")label.gameObject.SetActive(false);
            foreach(var readout in Object.FindObjectsByType<MechanismReadout>())
                if(readout.display!=null)readout.display.gameObject.SetActive(false);
            foreach(string name in new[]{"Brace instruction backing","Cutting instruction backing"})Off(name);
            var reset=GameObject.Find("Reset Label");if(reset!=null)reset.GetComponent<TextMesh>().text="↺";
        }
        private static void UpperFloor()
        {
            var hall=GameObject.Find("Центральний хол").transform;
            Off("Ceiling Hall");Box("High hall ceiling",root,new Vector3(14,9.1f,-2),new Vector3(20,.2f,20),dark);
            // Extend existing wall spans. Cut a REAL upper doorway through the west wall.
            foreach(Transform child in hall.Cast<Transform>().ToArray())
            {
                if(!child.name.StartsWith("Wall "))continue;
                var collider=child.GetComponent<Collider>();if(collider==null)continue;var b=collider.bounds;
                if(child.name=="Wall W"&&b.size.z>10)
                {
                    child.gameObject.SetActive(false);
                    Box("Wall upper-stair foundation",root,new Vector3(4,2.2f,b.center.z),new Vector3(.2f,4.4f,b.size.z),plaster);
                    Box("Wall west upper south",root,new Vector3(4,6.7f,(b.min.z+4.4f)/2),new Vector3(.2f,4.6f,4.4f-b.min.z),plaster);
                    Box("Wall west upper north",root,new Vector3(4,6.7f,7.7f),new Vector3(.2f,4.6f,.6f),plaster);
                    Box("Wall west upper lintel",root,new Vector3(4,8.2f,5.9f),new Vector3(.2f,1.6f,3),plaster);
                }
                else Box("Wall hall upper extension",root,new Vector3(b.center.x,7.3f,b.center.z),new Vector3(b.size.x,3.4f,b.size.z),plaster);
            }
            foreach(var pillar in hall.Cast<Transform>().Where(t=>t.name=="Hall Pillar"))
            {pillar.position=new Vector3(pillar.position.x,4.45f,pillar.position.z);pillar.localScale=new Vector3(.9f,8.9f,.9f);Box("Pillar capital",root,pillar.position+Vector3.up*4.2f,new Vector3(1.12f,.20f,1.12f),linen);}
            var stairs=Group("West hall staircase - 0.20m rise, 0.50m tread",root);
            for(int i=0;i<22;i++)Box("Stair "+i,stairs,new Vector3(6.3f,(i+1)*.1f,-6.35f+i*.5f),new Vector3(2.1f,(i+1)*.2f,.51f),timber);
            Box("Upper landing floor",root,new Vector3(3,4.25f,5.9f),new Vector3(10,.3f,3),timber);
            Rail(new Vector3(3,4.4f,7.4f),10,true);Rail(new Vector3(3.1f,4.4f,4.4f),4.2f,true);Rail(new Vector3(8,4.4f,5.9f),3,false);
            // Continuous rails flank the stairs; their slope follows the physical steps.
            foreach(float x in new[]{5.18f,7.42f})
            {
                var guard=Box("Stair side guard",root,new Vector3(x,2.75f,-1.1f),new Vector3(.08f,1.05f,11.9f),timber);
                guard.transform.rotation=Quaternion.Euler(-21.8f,0,0);guard.GetComponent<Renderer>().enabled=false;
                var rail=Box("Stair handrail",root,new Vector3(x,3.3f,-1.1f),new Vector3(.065f,.065f,11.9f),timber);
                rail.transform.rotation=Quaternion.Euler(-21.8f,0,0);
                for(int i=0;i<6;i++)Box("Stair baluster",root,new Vector3(x,1.0f+i*.8f,-6.1f+i*2),new Vector3(.07f,1.0f,.07f),timber);
            }
            AddZone("Верхня галерея",ManorArea.UpperGallery,new Vector3(3,6.2f,5.9f),new Vector3(10,3.6f,3),false);
            study=Group("Житлова кімната нагорі",root);
            Box("Study upper floor",study,new Vector3(-4.5f,4.25f,-2.8f),new Vector3(11,.3f,14.4f),timber);
            Box("Study ceiling",study,new Vector3(-4.5f,7.95f,-2.8f),new Vector3(11,.2f,14.4f),dark);
            Box("Wall study west",study,new Vector3(-10,6.15f,-2.8f),new Vector3(.2f,3.5f,14.4f),plaster);
            Box("Wall study east",study,new Vector3(1,6.15f,-2.8f),new Vector3(.2f,3.5f,14.4f),plaster);
            Box("Wall study south",study,new Vector3(-4.5f,6.15f,-10),new Vector3(11,3.5f,.2f),plaster);
            Box("Wall study north left",study,new Vector3(-5.85f,6.15f,4.4f),new Vector3(8.3f,3.5f,.2f),plaster);
            Box("Wall study north right",study,new Vector3(.35f,6.15f,4.4f),new Vector3(1.3f,3.5f,.2f),plaster);
            Box("Wall study door lintel",study,new Vector3(-1,7.3f,4.4f),new Vector3(1.4f,1.2f,.2f),plaster);
            MakeDoor("Study Door",study,new Vector3(-1.7f,4.4f,4.4f),1.4f,2.3f,"door.study");
            AddZone("Житлова кімната",ManorArea.Study,new Vector3(-4.5f,6.2f,-2.8f),new Vector3(10.8f,3.6f,14.2f),true);
            foreach(var zone in Object.FindObjectsByType<ManorZone>())
                if(zone.area==ManorArea.Caretaker||zone.area==ManorArea.Gallery){zone.center.y=1.8f;zone.size.y=3.6f;}
            Rug("Study central rug",study,new Vector3(-4.5f,4.42f,-2),new Vector2(5.5f,6));
            Box("Bed frame",study,new Vector3(-8,4.65f,-.5f),new Vector3(1.45f,.5f,2.25f),timber);
            Box("Mattress",study,new Vector3(-8,4.97f,-.5f),new Vector3(1.4f,.18f,2.15f),linen);
            Box("Bed headboard",study,new Vector3(-8,5.25f,.62f),new Vector3(1.5f,1.25f,.12f),timber);
            Box("Pillow",study,new Vector3(-8,5.1f,.2f),new Vector3(.7f,.15f,.38f),linen,false);
            Table("Study writing desk",study,new Vector3(-5.5f,4.4f,-6.3f),2.3f,1.1f);Chair(study,new Vector3(-5.5f,4.4f,-5.25f));
            Books(study,new Vector3(-6.2f,5.24f,-6.5f),5);Candies(study,new Vector3(-5,5.24f,-6.35f));
            Window(study,new Vector3(-9.84f,6.1f,-5.5f),Quaternion.Euler(0,90,0));
            Box("Study wardrobe",study,new Vector3(-.1f,5.45f,-7.4f),new Vector3(1.3f,2.1f,.65f),timber);
            Table("Study reading table",study,new Vector3(-2.2f,4.4f,.3f),1.0f,.8f);
            Chair(study,new Vector3(-2.2f,4.4f,1.4f));Chair(study,new Vector3(-2.2f,4.4f,-.8f),180);
            Cup(study,new Vector3(-2.2f,5.23f,.3f));Books(study,new Vector3(-2.5f,5.23f,.4f),3);
            for(int i=0;i<4;i++)Box("Fallen pages",study,new Vector3(-3+i*.2f,4.415f,-5+i*.17f),new Vector3(.22f,.01f,.3f),linen,false);
        }
        private static void DressRooms()
        {
            // Remove the former narrow edge carpet: the main hall now has a centred rug.
            Off("Hall Carpet");Rug("Hall centred rug",root,new Vector3(14,.025f,-2),new Vector2(8,10));
            Table("Hall dining table",root,new Vector3(18,0,-4.2f),2.8f,1.25f);
            foreach(float x in new[]{17.2f,18.8f}){Chair(root,new Vector3(x,0,-5.3f),180);Plate(root,new Vector3(x,.83f,-4.4f));Cup(root,new Vector3(x+.25f,.83f,-4.1f));}
            Candies(root,new Vector3(17.8f,.84f,-4.1f));
            Table("Resident tea table",root,new Vector3(18.7f,0,2),1.1f,.75f);Cup(root,new Vector3(18.7f,.83f,2));
            Chair(root,new Vector3(19.5f,0,2),90);
            foreach(var shelf in new[]{GameObject.Find("Library Long Shelf"),GameObject.Find("Library North Shelf")})
            {
                var b=shelf.GetComponent<Collider>().bounds;shelf.GetComponent<Renderer>().sharedMaterial=timber;
                for(int row=0;row<4;row++)for(int block=0;block<5;block++)
                {
                    var p=shelf.name.Contains("Long")?new Vector3(b.min.x-.01f,.45f+row*.57f,b.min.z+.35f+block*1.35f):new Vector3(b.min.x+.4f+block*.85f,.45f+row*.57f,b.min.z-.05f);
                    Books(root,p,5);
                }
            }
            Table("Library reading table",root,new Vector3(17,0,14),2.0f,1.0f);Chair(root,new Vector3(17,0,12.9f),180);Books(root,new Vector3(16.3f,.83f,14.1f),4);
            Rug("Library central aisle rug",root,new Vector3(17,.025f,15.2f),new Vector2(3.4f,6));
            var island=GameObject.Find("Kitchen Preparation Island");island.transform.position=new Vector3(32.3f,.42f,6);island.transform.localScale=new Vector3(2.6f,.84f,2);island.GetComponent<Renderer>().sharedMaterial=timber;
            foreach(float x in new[]{31.7f,32.9f})Box("Island drawer handle",root,new Vector3(x,.62f,4.98f),new Vector3(.26f,.025f,.035f),gold,false);
            Box("Cold stove",root,new Vector3(34.6f,.45f,1),new Vector3(1.4f,.9f,1.3f),dark);
            foreach(float x in new[]{34.3f,34.9f})Shape("Stove ring",root,new Vector3(x,.915f,1),new Vector3(.35f,.015f,.35f),silver,PrimitiveType.Cylinder);
            Box("Kitchen wall rack",root,new Vector3(35.75f,1.65f,7),new Vector3(.12f,.8f,2.5f),timber);
            foreach(float z in new[]{6.2f,6.7f,7.2f})Plate(root,new Vector3(35.4f,1.25f,z));
            foreach(string area in new[]{"Caretaker","Gallery","Hall","Library","Kitchen","Conservatory","Yard"})
            {
                var floor=GameObject.Find("Floor "+area);if(floor==null)continue;
                var mat=area=="Kitchen"?stone:area=="Conservatory"?stone:area=="Yard"?leaf:timber;
                var clone=new Material(mat);clone.name=area+" floor tiling";clone.SetTextureScale("_BaseMap",new Vector2(floor.transform.lossyScale.x/2,floor.transform.lossyScale.z/2));
                AssetDatabase.CreateAsset(clone,MaterialsFolder+"/"+clone.name+".mat");floor.GetComponent<Renderer>().sharedMaterial=clone;
            }
            foreach(var renderer in Object.FindObjectsByType<MeshRenderer>())
                if(renderer.gameObject.name.Contains("Desk Leg")||renderer.gameObject.name.Contains("Plant Bed"))renderer.sharedMaterial=renderer.gameObject.name.Contains("Plant")?leaf:timber;
            for(int i=0;i<6;i++)
            {Shape("Garden shrub",root,new Vector3(i%2==0?26.5f:34.5f,1.65f,19.8f+i*.65f),new Vector3(1.0f,1.1f,1.0f),leaf,PrimitiveType.Sphere);}
            // The old shed becomes an inhabited potting space, not a single tool pickup.
            Table("Potting worktable",root,new Vector3(31,0,40.8f),2.2f,.8f);Books(root,new Vector3(30.4f,.83f,40.8f),3);
            for(int i=0;i<4;i++)
            {Shape("Terracotta pot",root,new Vector3(31.2f+i*.3f,.96f,40.8f),new Vector3(.23f,.13f,.23f),timber,PrimitiveType.Cylinder);Flower(root,new Vector3(31.2f+i*.3f,1.1f,40.8f));}
            Window(root,new Vector3(23.84f,2,-2.5f),Quaternion.Euler(0,-90,0));
            Window(root,new Vector3(23.84f,2.5f,16),Quaternion.Euler(0,-90,0));
        }
        private static void TreatmentRoute()
        {
            foreach(string name in new[]{"Bolt Cutters","Gate Chain","Gate Brace Socket","Crowbar supporting gate","Escape Finish"})Off(name);
            var gates=GameObject.Find("Exterior Gates").GetComponent<HingedDoor>();gates.enabled=false;
            foreach(var status in Object.FindObjectsByType<MechanismReadout>())
                if(status.barrier!=null&&status.barrier.toolId=="tool.cutters")foreach(var section in status.remainingSections)if(section!=null)section.gameObject.SetActive(false);
            var picture=Group("Sliding study painting",study);picture.position=new Vector3(-4.5f,6.15f,-9.5f);
            var panel=Box("Framed painting panel",picture,picture.position,new Vector3(1.2f,.9f,.12f),timber);
            var interaction=picture.gameObject.AddComponent<SlidingPicture>();interaction.panel=panel.transform;interaction.closedPosition=panel.transform.localPosition;
            var moon=Material("Moon painting",new Color(.68f,.65f,.56f),"Moon_Albedo");
            Box("Painting image",panel.transform,picture.position+Vector3.forward*.066f,new Vector3(1.02f,.72f,.008f),moon,false);
            Box("Dark key niche",study,new Vector3(-4.5f,6.15f,-9.71f),new Vector3(.25f,.18f,.035f),dark,false);
            Pickup("Apothecary Key",study,new Vector3(-4.5f,6.15f,-9.65f),"remedy.study-key","ключ від аптечної шафки",new Vector3(.11f,.04f,.055f));
            var photo=Note("Study Photograph",study,new Vector3(-5.4f,5.24f,-6.3f),"study.photo","СТАРА ФОТОГРАФІЯ","На фотографії — ця кімната до занепаду. Картина з місяцем стоїть убік від темної ніші. Зараз картина закриває її.\nДвері цієї житлової кімнати можна зачинити. Тут безпечно роздивитися речі.");
            Box("Photograph frame",study,new Vector3(-5.4f,5.225f,-6.3f),new Vector3(.32f,.025f,.24f),timber,false);
            var cabinet=Group("Kitchen apothecary cabinet",root);cabinet.position=new Vector3(35,0,4);cabinet.rotation=Quaternion.Euler(0,90,0);
            LocalBox("Apothecary back",cabinet,new Vector3(0,.85f,.35f),new Vector3(1.2f,1.7f,.1f),timber);
            foreach(float side in new[]{-.6f,.6f})LocalBox("Apothecary side",cabinet,new Vector3(side,.85f,0),new Vector3(.08f,1.7f,.7f),timber);
            foreach(float y in new[]{.1f,.9f,1.7f})LocalBox("Apothecary shelf",cabinet,new Vector3(0,y,0),new Vector3(1.2f,.08f,.7f),timber);
            var door=MakeDoor("Apothecary Door",cabinet,new Vector3(-.6f,0,-.39f),1.2f,1.7f,"door.apothecary",true);door.requiredKeyId="remedy.study-key";door.missingKeyMessage="Аптечна шафка замкнена. Ключ зберігався у житловій кімнаті нагорі.";
            var bottle=Pickup("Remedy Base",cabinet,cabinet.TransformPoint(new Vector3(0,1.03f,0)),"remedy.base","основа мікстури",new Vector3(.1f,.21f,.1f));
            Shape("Glass bottle",bottle.transform,bottle.transform.position,new Vector3(.07f,.10f,.07f),leaf,PrimitiveType.Cylinder);
            Box("Bottle stopper",bottle.transform,bottle.transform.position+Vector3.up*.11f,new Vector3(.04f,.035f,.04f),timber,false);
            var spoon=Pickup("Silver Spoon",root,new Vector3(18.55f,.848f,-4.15f),"remedy.spoon","срібна ложечка",new Vector3(.19f,.035f,.07f));Spoon(spoon.transform,spoon.transform.position);
            var flower=Pickup("Remedy Flower",root,new Vector3(33,.98f,38),"remedy.flower","бліда садова квітка",new Vector3(.16f,.3f,.16f));Flower(flower.transform,flower.transform.position-Vector3.up*.12f);
            Shape("Remedy flower pot",root,new Vector3(33,.89f,38),new Vector3(.22f,.12f,.22f),timber,PrimitiveType.Cylinder);
            Note("Remedy Recipe",root,new Vector3(6,1.28f,20.5f),"remedy.recipe","ОСОБИСТІ НОТАТКИ","Він усе ще повертається до своєї чашки в холі. Можливо, звичка пережила хворобу.\nОснова — в аптечній шафці на кухні; ключ — у житловій кімнаті нагорі. Додати бліду квітку з п’ятьма пелюстками й протилежними листками із зовнішнього саду. Розмішати срібною ложечкою. Посуд лишився на столі в холі.\nГотовий засіб залишити в його чашці та відійти, щоб він спокійно наблизився.");
            var bowl=Shape("Remedy mixing bowl",root,new Vector3(32.3f,.95f,5.7f),new Vector3(.35f,.10f,.35f),linen,PrimitiveType.Cylinder,true);
            var workbench=bowl.AddComponent<TreatmentWorkbench>();
            workbench.preparedVisual=Shape("Prepared mixture",root,new Vector3(32.3f,1.055f,5.7f),new Vector3(.30f,.008f,.30f),bloom,PrimitiveType.Cylinder);workbench.preparedVisual.SetActive(false);
            var cup=Box("Resident medicine cup",root,new Vector3(18.7f,.92f,2),new Vector3(.16f,.16f,.16f),linen,false);
            cup.GetComponent<Renderer>().enabled=false;cup.AddComponent<BoxCollider>();var delivery=cup.AddComponent<RemedyDelivery>();delivery.resident=Object.FindAnyObjectByType<EnemyInvestigator>();
            var approach=Group("Resident tea approach",root);approach.position=new Vector3(18.7f,0,.95f);delivery.approachPoint=approach;
            delivery.filledCup=Shape("Filled medicine cup",root,new Vector3(18.7f,.927f,2),new Vector3(.07f,.008f,.07f),bloom,PrimitiveType.Cylinder);delivery.filledCup.SetActive(false);
        }
        private static void RemapLegacyMaterials()
        {
            foreach(var renderer in Object.FindObjectsByType<MeshRenderer>())
            {
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    if(materials[i]==null)continue;
                    switch(materials[i].name)
                    {
                        case "Material_Simple_BlueDark": materials[i]=dark;break;
                        case "Material_WhiteGrid": materials[i]=linen;break;
                        case "Material_Simple_Yellow": materials[i]=gold;break;
                        case "Material_Simple_Green": materials[i]=leaf;break;
                        case "Material_SandLumpy": materials[i]=timber;break;
                        case "Material_Checkerboard": materials[i]=stone;break;
                    }
                }
                renderer.sharedMaterials=materials;
            }
        }
        private static void ImportVisuals()
        {
            var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Props/Key1/Key1.fbx");
            foreach(var key in Object.FindObjectsByType<KeyPickup>())
            {
                if(key.keyId!="manor.service-passage"&&key.keyId!="remedy.study-key")continue;
                foreach(var r in key.GetComponentsInChildren<Renderer>())r.enabled=false;
                key.transform.localScale=Vector3.one;var box=key.GetComponent<BoxCollider>();box.size=new Vector3(.11f,.035f,.06f);
                var visual=Fit(model,key.transform,"Key model from main - 9.5cm",Vector3.zero,new Vector3(.095f,.03f,.05f),true);
                var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Props/Key1/Materials/rust_gold.mat");foreach(var r in visual.GetComponentsInChildren<Renderer>())r.sharedMaterial=material;
            }
            var doorModel=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Props/ManorDoor/SM_ManorDoor.fbx");
            var leafSource=doorModel.GetComponentsInChildren<Transform>().First(t=>t.name=="DoorLeaf").gameObject;
            foreach(var door in Object.FindObjectsByType<HingedDoor>())
            {
                if(door.name.Contains("Cupboard")||door.name.Contains("Cache")||door.name.Contains("Apothecary"))continue;
                var collider=door.GetComponentsInChildren<BoxCollider>().FirstOrDefault(c=>!c.isTrigger);if(collider==null)continue;
                var mesh=collider.GetComponent<Renderer>();if(mesh!=null)mesh.enabled=false;
                foreach(var r in collider.GetComponentsInChildren<Renderer>())r.enabled=false;
                Vector3 size=Vector3.Scale(collider.size,collider.transform.localScale);
                Vector3 center=door.hinge.InverseTransformPoint(collider.bounds.center);
                Fit(leafSource,door.hinge,"Door leaf model from main",center,size);
            }
            // Tile real wall meshes over the saved collision spans; collision geometry stays authoritative.
            var wallModel=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Props/ManorWall/SM_ManorWall_2x3.fbx");
            foreach(var wall in Object.FindObjectsByType<MeshRenderer>().Where(r=>r.name.StartsWith("Wall ")).ToArray())
            {
                var b=wall.bounds;if(b.size.y<.2f)continue;wall.enabled=false;
                bool horizontal=b.size.x>b.size.z;float length=horizontal?b.size.x:b.size.z;
                int columns=Mathf.CeilToInt(length/2),rows=Mathf.CeilToInt(b.size.y/3);
                for(int c=0;c<columns;c++)for(int row=0;row<rows;row++)
                {
                    float w=Mathf.Min(2,length-c*2),h=Mathf.Min(3,b.size.y-row*3);
                    var center=new Vector3(horizontal?b.min.x+c*2+w/2:b.center.x,b.min.y+row*3+h/2,horizontal?b.center.z:b.min.z+c*2+w/2);
                    var visual=Fit(wallModel,null,"Textured wall module",Vector3.zero,new Vector3(w,h,.2f));visual.transform.rotation=Quaternion.Euler(0,horizontal?0:90,0);visual.transform.position+=center;visual.transform.SetParent(root,true);
                }
            }
        }
        private static HingedDoor MakeDoor(string name,Transform parent,Vector3 hingePosition,float width,float height,string id,bool local=false)
        {
            var hinge=Group(name,parent);if(local)hinge.localPosition=hingePosition;else hinge.position=hingePosition;
            var panel=LocalBox("Door leaf collider",hinge,new Vector3(width/2,height/2,0),new Vector3(width,height,.10f),timber);
            var door=hinge.gameObject.AddComponent<HingedDoor>();door.hinge=hinge;door.checkpointId=id;door.openAngle=-105;
            return door;
        }
        private static GameObject LocalBox(string name,Transform parent,Vector3 center,Vector3 size,Material mat)
        {var go=Box(name,null,center,size,mat);go.transform.SetParent(parent,false);return go;}
        private static KeyPickup Pickup(string name,Transform parent,Vector3 p,string id,string caption,Vector3 size)
        {
            var go=Group(name,parent).gameObject;go.transform.position=p;var collider=go.AddComponent<BoxCollider>();collider.size=size;
            var pickup=go.AddComponent<KeyPickup>();pickup.keyId=id;pickup.displayName=caption;pickup.pickupPrompt="E — забрати "+caption;return pickup;
        }
        private static ClueNote Note(string name,Transform parent,Vector3 p,string id,string title,string content)
        {var go=Box(name,parent,p,new Vector3(.28f,.012f,.20f),linen);var note=go.AddComponent<ClueNote>();note.clueId=id;note.title=title;note.text=content;return note;}
        private static void AddZone(string name,ManorArea area,Vector3 center,Vector3 size,bool safe)
        {var zone=Group(name+" zone",root).gameObject.AddComponent<ManorZone>();zone.area=area;zone.ukrainianName=name;zone.center=center;zone.size=size;zone.tutorialSafe=safe;}
        private static void Rail(Vector3 p,float length,bool horizontal)
        {
            Box("Balcony handrail",root,p+Vector3.up*1.1f,horizontal?new Vector3(length,.09f,.09f):new Vector3(.09f,.09f,length),timber);
            for(float v=-length/2;v<=length/2;v+=.5f)Box("Balcony baluster",root,p+new Vector3(horizontal?v:0,.55f,horizontal?0:v),new Vector3(.07f,1.1f,.07f),timber);
            // A continuous rail collider prevents falling through the decorative balusters.
            var guard=Box("Balcony guard collider",root,p+Vector3.up*.55f,horizontal?new Vector3(length,1.1f,.08f):new Vector3(.08f,1.1f,length),timber);guard.GetComponent<Renderer>().enabled=false;
        }
        private static void Window(Transform parent,Vector3 p,Quaternion rotation)
        {
            var group=Group("Moonlit window",parent);
            var glass=Material("Moonlit glass",new Color(.14f,.22f,.30f));glass.EnableKeyword("_EMISSION");glass.SetColor("_EmissionColor",new Color(.09f,.14f,.20f));
            Box("Window glass",group,p,new Vector3(1.6f,2.0f,.035f),glass,false);
            foreach(float x in new[]{-.85f,0,.85f})Box("Window upright",group,p+Vector3.right*x,new Vector3(.09f,2.1f,.07f),timber,false);
            foreach(float y in new[]{-1.05f,0,1.05f})Box("Window crossbar",group,p+Vector3.up*y,new Vector3(1.8f,.09f,.07f),timber,false);
            group.position=p;foreach(Transform child in group)child.position-=p;group.rotation=rotation;
        }
        private static void Off(string name){var go=GameObject.Find(name);if(go!=null)go.SetActive(false);}
    }
}
