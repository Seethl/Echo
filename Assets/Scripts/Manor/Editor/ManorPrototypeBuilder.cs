using System.Collections.Generic;
using System.IO;
using Echo.Interactions;
using Echo.Menu;
using Echo.Audio;
using Echo.Radio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine.AI;
using UnityEngine;
using UnityEngine.Rendering;

namespace Echo.Manor.Editor
{
    public static class ManorPrototypeBuilder
    {
        public const string ScenePath = "Assets/Scenes/ManorPrototype/ManorPrototype.unity";
        private static readonly Dictionary<ManorArea, Transform> rooms = new Dictionary<ManorArea, Transform>();
        private static readonly Dictionary<string, Transform> anchors = new Dictionary<string, Transform>();
        private static Material stone, wood, dark, paper, brass, carpet;
        private static PlayerInteractor player;
        private static InteractionUI ui;

        [MenuItem("Echo/Відкрити основний прототип маєтку")]
        public static void Open()
        {
            if (File.Exists(ScenePath)) { if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath); return; }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) Build(false);
        }

        public static void BuildRoute() => Build(true);
        public static void BuildFull() => Build(false);
        private static void Build(bool routeOnly)
        {
            if (File.Exists(ScenePath) && PlayerSettings.productName != "EchoPrototypeVerification")
                throw new IOException("Refusing to overwrite an existing team scene. Open the saved scene instead.");
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            Directory.CreateDirectory("Assets/Resources"); AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            rooms.Clear(); anchors.Clear();
            stone=Mat("Material_Checkerboard"); wood=Mat("Material_SandLumpy"); dark=Mat("Material_Simple_BlueDark");
            paper=Mat("Material_WhiteGrid"); brass=Mat("Material_Simple_Yellow"); carpet=Mat("Material_Simple_Green");
            Room(ManorArea.Caretaker,"Кімната доглядача",-20,-10,-12,-4,3.6f,true,
                new[]{new Opening('E',-8)});
            Room(ManorArea.Gallery,"Галерея",-10,4,-12,-4,4,true,
                new[]{new Opening('W',-8),new Opening('E',-8)});
            Room(ManorArea.Hall,"Центральний хол",4,24,-12,8,5.6f,false,
                new[]{new Opening('W',-8),new Opening('N',8),new Opening('N',20),new Opening('E',4)});
            Room(ManorArea.Library,"Бібліотека",4,24,8,22,4.6f,false,
                new[]{new Opening('S',8),new Opening('S',20),new Opening('E',11)});
            Room(ManorArea.Kitchen,"Кухня",24,36,0,14,3.8f,false,
                new[]{new Opening('W',4),new Opening('W',11),new Opening('N',30)});
            Room(ManorArea.Conservatory,"Зимовий сад",24,38,14,28,4.5f,false,
                new[]{new Opening('S',30),new Opening('N',30)});
            Room(ManorArea.Yard,"Задній двір",10,38,28,48,2.6f,false,
                new[]{new Opening('S',30),new Opening('N',17)},false);

            var c=rooms[ManorArea.Caretaker];
            Cube("Caretaker Desk",c,new Vector3(-17,0.8f,-9.5f),new Vector3(2.2f,.15f,1),wood);
            foreach(float x in new[]{-17.8f,-16.2f}) foreach(float z in new[]{-9.8f,-9.2f})
                Cube("Desk Leg",c,new Vector3(x,.4f,z),new Vector3(.12f,.8f,.12f),dark);
            Anchor("radio",new Vector3(-17.5f,1,-9.4f)); Anchor("service.card",new Vector3(-16.7f,.92f,-9.4f));
            Cupboard(c,"Service Cupboard",new Vector3(-12,0,-5),1.5f,2.2f);
            Anchor("service.key",new Vector3(-12,1.2f,-5));
            Anchor("service.door",new Vector3(-10,0,-8));
            Anchor("clock",new Vector3(-19.8f,2,-7));

            var g=rooms[ManorArea.Gallery];
            Cube("Window Outline",g,new Vector3(-3,2,-11.86f),new Vector3(3,1.8f,.08f),paper);
            Anchor("shutter",new Vector3(-3,1.5f,-11.65f));
            Anchor("branch",new Vector3(-2.6f,1.25f,-11.4f));
            Anchor("gallery.card",new Vector3(1,1.3f,-4.25f));
            Anchor("code.lock",new Vector3(3.7f,1.25f,-6.1f));
            Anchor("hall.door",new Vector3(4,0,-8));

            var h=rooms[ManorArea.Hall];
            foreach(float x in new[]{11f,21f}) foreach(float z in new[]{-7f,4f})
                Cube("Hall Pillar",h,new Vector3(x,2.7f,z),new Vector3(.9f,5.4f,.9f),paper);
            Cube("Hall Central Cover",h,new Vector3(15,1.1f,-2),new Vector3(3,2.2f,3),wood);
            Cube("Gallery Entry Cover",h,new Vector3(10.5f,1.2f,-5),new Vector3(2,2.4f,1.2f),dark);
            Cube("Radio Niche Partition",h,new Vector3(7,1.3f,2),new Vector3(.25f,2.6f,3),wood);
            Cube("Radio Niche Table",h,new Vector3(5.3f,.65f,2),new Vector3(1.4f,.2f,1.2f),wood);
            FloorPatch("Hall Carpet",h,new Vector3(5.7f,.025f,-.5f),new Vector3(2.4f,.05f,12),SurfaceType.Kind.Carpet,.22f,carpet);
            FloorPatch("Hall Glass",h,new Vector3(13,.025f,-6.5f),new Vector3(2.5f,.05f,2),SurfaceType.Kind.Glass,3.2f,paper);
            FloorPatch("Creaky Board",h,new Vector3(9.3f,.04f,-3.5f),new Vector3(.65f,.08f,1.8f),SurfaceType.Kind.Creaky,2.3f,brass);
            Anchor("music.niche",new Vector3(5.3f,1.1f,2));
            Anchor("checkpoint.hall",new Vector3(6.5f,1,-8));

            var l=rooms[ManorArea.Library];
            Cube("Library Long Shelf",l,new Vector3(13,1.4f,15),new Vector3(1.2f,2.8f,8),wood);
            Cube("Library North Shelf",l,new Vector3(20,1.4f,18.5f),new Vector3(5,2.8f,1.1f),wood);
            for(int i=0;i<3;i++) Anchor("bell."+i,new Vector3(8+i*2.1f,1.5f,21.75f));
            Anchor("library.card",new Vector3(17,1.4f,21.75f));
            Cupboard(l,"Library Cache",new Vector3(6,0,20.5f),1.7f,2.1f);
            Anchor("winch.handle",new Vector3(6,1.2f,20.5f));
            for(int i=0;i<3;i++) Anchor("symbol."+i,new Vector3(5.4f+i*.6f,.55f,19.8f));

            var k=rooms[ManorArea.Kitchen];
            Cube("Kitchen Preparation Island",k,new Vector3(32,.9f,6),new Vector3(3,1.8f,2),wood);
            Cube("Kitchen Cover",k,new Vector3(26.8f,1.3f,8),new Vector3(1.2f,2.6f,3),dark);
            Anchor("shortcut.door",new Vector3(24,0,4));
            Anchor("shortcut.bolt",new Vector3(24.3f,1.2f,4));
            Anchor("winch",new Vector3(33,1,12));
            Anchor("winch.card",new Vector3(35.75f,1.3f,10));
            Anchor("grate",new Vector3(30,0,14));
            Anchor("checkpoint.garden",new Vector3(30,1,16));

            var garden=rooms[ManorArea.Conservatory];
            Cube("West Plant Bed",garden,new Vector3(26.5f,1,21),new Vector3(2.4f,2,5),carpet);
            Cube("East Plant Bed",garden,new Vector3(34.5f,1,22),new Vector3(2.4f,2,5),carpet);
            Cube("Garden Tool Bench",garden,new Vector3(26,0.8f,16.5f),new Vector3(2,.16f,1.2f),wood);
            Anchor("crowbar",new Vector3(26,.95f,16.5f));
            Anchor("water",new Vector3(36,2.4f,25));
            Anchor("yard.door",new Vector3(30,0,28));
            FloorPatch("Garden Glass",garden,new Vector3(32.2f,.025f,25),new Vector3(2,.05f,2.2f),SurfaceType.Kind.Glass,3.2f,paper);
            Anchor("checkpoint.yard",new Vector3(30,1,30));

            var y=rooms[ManorArea.Yard];
            Cube("Yard Hedge West",y,new Vector3(22,1.1f,35),new Vector3(4,2.2f,2),carpet);
            Cube("Yard Hedge North",y,new Vector3(25,1.1f,43),new Vector3(2,2.2f,4),carpet);
            Cube("Yard Crate Cover",y,new Vector3(15,1.05f,38),new Vector3(2.5f,2.1f,2),wood);
            Cube("Shed South",y,new Vector3(32.5f,1.5f,35),new Vector3(7,3,.2f),wood);
            Cube("Shed North",y,new Vector3(32.5f,1.5f,42),new Vector3(7,3,.2f),wood);
            Cube("Shed East",y,new Vector3(36,1.5f,38.5f),new Vector3(.2f,3,7),wood);
            Wall(y,'W',29,35,42,3,new[]{37f},wood);
            Cube("Shed Roof",y,new Vector3(32.5f,3.1f,38.5f),new Vector3(7,.2f,7),dark);
            Cube("Shed Tool Table",y,new Vector3(33,.8f,38),new Vector3(2,.16f,1.2f),wood);
            Anchor("cutters",new Vector3(33,.98f,38));
            Anchor("gates",new Vector3(17,0,48));
            Anchor("finish",new Vector3(17,1.2f,50));
            Cube("Escape Path",y,new Vector3(17,-.15f,50),new Vector3(4,.3f,5),wood);
            Cube("Blocked Front Entrance Rubble",h,new Vector3(20,1.2f,-11),new Vector3(4.5f,2.4f,1.7f),stone);

            var moon=new GameObject("Moonlight - no mains power",typeof(Light)).GetComponent<Light>();
            moon.type=LightType.Directional; moon.color=new Color(.65f,.75f,1); moon.intensity=1.25f;
            moon.shadows=LightShadows.None; moon.transform.rotation=Quaternion.Euler(40,120,0);
            RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=new Color(.32f,.37f,.45f);
            RenderSettings.skybox=AssetDatabase.LoadAssetAtPath<Material>("Assets/Skyboxes/SkyNight.mat");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            if(prefab==null) throw new FileNotFoundException("Existing Player prefab required.");
            var actor=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
            actor.name="Player"; actor.transform.SetPositionAndRotation(new Vector3(-15,1,-8),Quaternion.Euler(0,-105,0));
            ui=new GameObject("Interaction UI",typeof(InteractionUI)).GetComponent<InteractionUI>();
            player=actor.AddComponent<PlayerInteractor>(); player.viewCamera=actor.GetComponent<FPSController>().playerCamera.GetComponent<Camera>(); player.ui=ui;
            var steps=actor.AddComponent<Footsteps>();
            steps.woodFootstepSound=Clip("Wooden"); steps.carpetFootstepSound=Clip("Prototype_CarpetStep"); steps.glassFootstepSound=Clip("Prototype_GlassStep"); steps.defaultFootstepSound=steps.woodFootstepSound;
            new GameObject("Manor Navigation",typeof(ManorNavigation));
            if(!routeOnly) ConfigureGameplay();
            foreach(var item in Object.FindObjectsByType<Interactable>())SeparateVisual(item.gameObject);
            if(!routeOnly)ManorUsabilityUpgrade.ApplyToScene();
            if(!routeOnly)ManorRadioPuzzleUpgrade.ApplyToScene();
            var flow=AssetDatabase.LoadAssetAtPath<GameFlowSettings>("Assets/Resources/EchoGameFlow.asset");
            if(flow==null) {flow=ScriptableObject.CreateInstance<GameFlowSettings>();AssetDatabase.CreateAsset(flow,"Assets/Resources/EchoGameFlow.asset");}
            flow.gameplayScene=ScenePath; EditorUtility.SetDirty(flow);
            EditorSceneManager.SaveScene(scene,ScenePath); AssetDatabase.SaveAssets();
            var scenes=new List<EditorBuildSettingsScene>{new EditorBuildSettingsScene(GameMenu.MainMenuScene,true),new EditorBuildSettingsScene(ScenePath,true),new EditorBuildSettingsScene(GameMenu.InteractionTestScene,true),new EditorBuildSettingsScene(GameMenu.PlayerTestScene,true)};
            if(File.Exists("Assets/Scenes/Testing/PlayerTest/SurfaceTest.unity"))scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/Testing/PlayerTest/SurfaceTest.unity",true));
            EditorBuildSettings.scenes=scenes.ToArray();
            Debug.Log("ECHO_PROTOTYPE_BUILT: stage="+(routeOnly?1:2));
        }

        private static void ConfigureGameplay()
        {
            var progress=new GameObject("Manor Progress",typeof(ManorProgress)).GetComponent<ManorProgress>();progress.player=player;
            var service=Door("Service Door",anchors["service.door"].position,true,"door.service",false);
            service.requiredKeyId="manor.service-passage";
            UnityEventTools.AddStringPersistentListener(service.onOpened,progress.SetFlag,"service.open");
            Door("Service Cupboard Door",anchors["Service Cupboard.hinge"].position+Vector3.right*.75f,false,"cupboard.service",false,1.5f,2.2f);
            Pickup("Service Key",anchors["service.key"].position,"manor.service-passage","службовий ключ","E — забрати ключ",new Vector3(.3f,.14f,.15f));
            var careNote=Note("Service Card",anchors["service.card"].position,"service.card",820,
                "Службова передача: 820. Запасний ключ від службового проходу — у шафці біля дверей.\nРадіо: R — налаштувати; F — живлення; Q — поставити. Без витрати батарейок.");
            var hallDoor=Door("Hall Code Door",anchors["hall.door"].position,true,"door.hall",true);
            hallDoor.initiallyLocked=true;hallDoor.missingKeyMessage="Механічний замок. Кодовий механізм розташовано поруч.";
            Bind(hallDoor,"hall.open");
            var keypad=Cube("Three Digit Lock",null,anchors["code.lock"].position,new Vector3(.5f,.55f,.2f),brass).AddComponent<KeypadLock>();keypad.code="417";
            var galleryNote=Note("Gallery Frequency Card",anchors["gallery.card"].position,"gallery.card",940,
                "Службова передача: 940.\n«Код проходу: чотири… [гуркіт заглушає решту]».\nГілка заважає зафіксувати віконницю. Приберіть гілку, закрийте віконницю та повторіть запис або перегляньте цю картку.");
            galleryNote.fullTextFlag="gallery.quiet";galleryNote.fullText="Службова передача 940: «Код проходу: 4 — 1 — 7». Після тиші власні кроки чутніші. У холі рухайтеся килимом і за укриттями.";
            var branch=Cube("Jammed Branch",null,anchors["branch"].position,new Vector3(.8f,.12f,.12f),wood).AddComponent<FlagInteraction>();
            branch.prompt="E — вийняти гілку";branch.setFlag="gallery.branch";branch.hideWhenDone=true;branch.doneMessage="Гілку прибрано. Тепер віконницю можна зафіксувати.";
            var latch=Cube("Shutter Latch",null,anchors["shutter"].position,new Vector3(.55f,.55f,.15f),dark).AddComponent<FlagInteraction>();
            // The full visible window forwards ray hits to the existing latch through its parent.
            GameObject.Find("Window Outline").transform.SetParent(latch.transform,true);
            latch.prompt="E — закрити й зафіксувати віконницю";latch.setFlag="gallery.quiet";latch.requiredFlag="gallery.branch";latch.missingMessage="Гілка не дає закрити віконницю.";latch.doneMessage="Гуркіт стих. Повторіть передачу 940. Тепер чути кроки з холу.";

            var cache=Door("Library Cache Door",anchors["Library Cache.hinge"].position+Vector3.right*.85f,false,"cache.library",true,1.7f,2.1f);cache.initiallyLocked=true;cache.missingKeyMessage="Три символи керують схованкою.";Bind(cache,"library.cache");
            var puzzle=new GameObject("Library Rhythm Combination",typeof(SymbolPuzzle)).GetComponent<SymbolPuzzle>();
            string[] symbols={"moon","tree","bird"},names={"Місяць","Дерево","Птах"},rhythms={"один довгий","два короткі","три короткі"};
            for(int i=0;i<3;i++)
            {
                foreach(bool demo in new[]{true,false})
                {
                    var button=Cube((demo?"Rhythm Panel ":"Cache Symbol ")+symbols[i],null,anchors[(demo?"bell.":"symbol.")+i].position,demo?new Vector3(.65f,.65f,.15f):new Vector3(.3f,.3f,.15f),i==0?paper:i==1?carpet:brass).AddComponent<SymbolButton>();
                    button.puzzle=puzzle;button.symbolId=symbols[i];button.symbolName=names[i];button.rhythm=rhythms[i];button.demonstration=demo;button.shortPulses=i==0?1:i+1;
                    WorldLabel(names[i],button.transform.position+new Vector3(0,.45f,-.1f),Quaternion.identity,.06f);
                }
            }
            Note("Library Frequency Card",anchors["library.card"].position,"library.card",1060,"Службовий запис: 1060.\nДва короткі → один довгий → три короткі.\nПослухайте настінні панелі, щоб зіставити ритми із символами. Після помилки скидається лише поточний порядок.");
            Pickup("Winch Handle",anchors["winch.handle"].position,"tool.winch","руків’я лебідки","E — забрати руків’я",new Vector3(.4f,.14f,.25f));
            var shortcut=Door("Kitchen Shortcut Door",anchors["shortcut.door"].position,true,"door.shortcut",true);shortcut.initiallyLocked=true;shortcut.missingKeyMessage="Засув з боку кухні.";Bind(shortcut,"kitchen.shortcut");
            var bolt=Cube("Kitchen Side Bolt",null,anchors["shortcut.bolt"].position,new Vector3(.15f,.25f,.7f),brass).AddComponent<FlagInteraction>();
            bolt.prompt="E — зняти засув";bolt.allowedSide=Vector3.right;bolt.setFlag="kitchen.shortcut";bolt.hideWhenDone=true;bolt.doneMessage="Скорочення між кухнею та холом відкрите.";
            Note("Winch Instructions",anchors["winch.card"].position,"winch.instructions",0,"Установи руків’я. Утримуй Space до подвійного клацання. Відпусти, щоб стопор сів у паз.\nПотрібні три цикли; вікно реакції — 1,5 секунди. Вже зафіксовані ступені не втрачаються. Escape — відійти.");
            var grateRoot=anchors["grate"];
            var blocker=grateRoot.gameObject.AddComponent<NavMeshObstacle>();blocker.shape=NavMeshObstacleShape.Box;blocker.center=Vector3.up*1.35f;blocker.size=new Vector3(2.5f,2.7f,.4f);blocker.carving=true;blocker.carveOnlyStationary=false;
            var grate=Cube("Heavy Raised Grate",grateRoot,grateRoot.position+Vector3.up*1.35f,new Vector3(2.35f,2.7f,.15f),dark);
            Object.DestroyImmediate(grate.GetComponent<MeshRenderer>());
            for(int i=0;i<6;i++)Cube("Grate Rail",grate.transform,grateRoot.position+new Vector3(-1.1f+i*.44f,1.35f,0),new Vector3(.065f,2.7f,.08f),dark);
            foreach(float yRail in new[]{.3f,2.35f})Cube("Grate Crossbar",grate.transform,grateRoot.position+Vector3.up*yRail,new Vector3(2.35f,.08f,.1f),dark);
            var winch=Cube("Manual Winch",null,anchors["winch"].position,new Vector3(.7f,.7f,.7f),brass).AddComponent<WinchMechanism>();winch.grate=grate.transform;winch.navigationBlocker=blocker;

            Pickup("Crowbar",anchors["crowbar"].position,"tool.crowbar","лом","E — забрати лом",new Vector3(.65f,.1f,.1f));
            var yardDoor=Door("Jammed Garden Door",anchors["yard.door"].position,false,"door.yard",true);yardDoor.initiallyLocked=true;yardDoor.missingKeyMessage="Заклинений засув. Потрібен лом і три натискання.";Bind(yardDoor,"yard.open");
            var pry=Cube("Jammed Latch",null,anchors["yard.door"].position+new Vector3(0,1.2f,-.2f),new Vector3(.5f,.3f,.15f),brass).AddComponent<ToolBarrier>();
            pry.requiredActions=3;pry.actionName="відтиснути засув";
            Pickup("Bolt Cutters",anchors["cutters"].position,"tool.cutters","болторіз","E — забрати болторіз",new Vector3(.65f,.1f,.3f));
            var gates=Door("Exterior Gates",anchors["gates"].position,false,"door.gates",true);gates.initiallyLocked=true;gates.missingKeyMessage="Ворота тримає ланцюг. Болторіз лежить у сараї.";Bind(gates,"gates.open");
            var chain=Cube("Gate Chain",null,anchors["gates"].position+new Vector3(0,1.2f,-.2f),new Vector3(.7f,.2f,.15f),brass).AddComponent<ToolBarrier>();
            chain.toolId="tool.cutters";chain.counterId="gates.cuts";chain.openFlag="gates.open";chain.requiredActions=2;chain.actionName="перекусити ланцюг";chain.missingMessage="Потрібен болторіз із сараю.";chain.noiseRadius=22;
            var finish=new GameObject("Escape Finish",typeof(BoxCollider),typeof(Rigidbody),typeof(EpisodeExit));finish.transform.position=anchors["finish"].position;
            finish.GetComponent<BoxCollider>().isTrigger=true;finish.GetComponent<BoxCollider>().size=new Vector3(3,2.4f,.8f);finish.GetComponent<Rigidbody>().isKinematic=true;finish.GetComponent<Rigidbody>().useGravity=false;finish.GetComponent<EpisodeExit>().completionMessage="Ви залишили маєток";
            Checkpoint("Hall Checkpoint",1,"hall.open",anchors["checkpoint.hall"].position,new Vector3(18,0,3),new Vector3(5,3,4));
            Checkpoint("Garden Checkpoint",2,"grate.open",anchors["checkpoint.garden"].position,new Vector3(15,0,-3),new Vector3(4,3,3));
            Checkpoint("Yard Checkpoint",3,"yard.open",anchors["checkpoint.yard"].position,new Vector3(28,0,10),new Vector3(4,3,3));
            ConfigureAudioAndRadio();
            ConfigureEnemy(progress);
        }

        private static void ConfigureEnemy(ManorProgress progress)
        {
            var root=new GameObject("Manor Resident",typeof(NoiseListener),typeof(NavMeshAgent));root.transform.position=new Vector3(18,0,3);
            var agent=root.GetComponent<NavMeshAgent>();agent.enabled=false;agent.radius=.35f;agent.height=1.8f;agent.speed=1.6f;agent.acceleration=8;agent.angularSpeed=300;agent.stoppingDistance=.55f;
            var enemy=root.AddComponent<EnemyInvestigator>();enemy.target=player.GetComponent<FPSController>();enemy.searchDuration=9;enemy.sightRange=12;enemy.fieldOfView=100;enemy.suspicionSeconds=1.1f;enemy.footstepsClip=Clip("Wooden");
            var positions=new[]{new Vector3(18,0,-7),new Vector3(20,0,4),new Vector3(9,0,4),new Vector3(18,0,11),new Vector3(30,0,9),new Vector3(30,0,23),new Vector3(27,0,33)};
            enemy.patrolPoints=new Transform[positions.Length];for(int i=0;i<positions.Length;i++)enemy.patrolPoints[i]=Anchor("Patrol "+i,positions[i]);
            enemy.patrolUnlockFlags=new[]{"","","","area.Library","area.Kitchen","grate.open","yard.open"};
            var body=GameObject.CreatePrimitive(PrimitiveType.Capsule);body.name="Visual - replace with resident model";body.transform.SetParent(root.transform,false);body.transform.localPosition=Vector3.up*.95f;body.transform.localScale=new Vector3(.75f,.95f,.75f);body.GetComponent<Renderer>().sharedMaterial=Mat("Material_Simple_Red");Object.DestroyImmediate(body.GetComponent<Collider>());
            var caught=player.gameObject.AddComponent<PlayerCaught>();caught.allowKeyboardRestart=false;caught.externalFailureScreen=true;UnityEventTools.AddPersistentListener(caught.onCaught,progress.ShowFailure);
            var catchPlayer=root.AddComponent<EnemyCatchPlayer>();catchPlayer.target=caught;catchPlayer.eyeHeight=1.4f;catchPlayer.catchDistance=1.05f;
        }

        private static void SeparateVisual(GameObject root)
        {
            var mesh=root.GetComponent<MeshFilter>();var renderer=root.GetComponent<MeshRenderer>();if(mesh==null||renderer==null)return;
            var visual=new GameObject("Visual - replace model",typeof(MeshFilter),typeof(MeshRenderer));visual.transform.SetParent(root.transform,false);visual.GetComponent<MeshFilter>().sharedMesh=mesh.sharedMesh;visual.GetComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;
            Object.DestroyImmediate(renderer);Object.DestroyImmediate(mesh);
        }

        private static void ConfigureAudioAndRadio()
        {
            Directory.CreateDirectory("Assets/RadioData");AssetDatabase.Refresh();
            var service=Channel("Service820",820,RadioChannel.Mode.Recording,"Запасний ключ від службового проходу — у шафці біля дверей");
            var code=Channel("Gallery940",940,RadioChannel.Mode.Recording,"Код проходу: 4 — 1 — 7.");
            code.clearAfterFlag="gallery.quiet";code.interruptedTranscript="Код проходу: чотири… [гуркіт заглушає цифри]";EditorUtility.SetDirty(code);
            var library=Channel("Library1060",1060,RadioChannel.Mode.Recording,"Два короткі → один довгий → три короткі. Повтор запису доступний.");
            var music=Channel("Music1140",1140,RadioChannel.Mode.Music,"");music.channelName="Музика";
            var white=Channel("White1220",1220,RadioChannel.Mode.WhiteNoise,"");white.channelName="Білий шум";
            var radio=Cube("Portable Radio",null,anchors["radio"].position+Vector3.up*.12f,new Vector3(.64f,.44f,.4f),dark).AddComponent<PortableRadio>();
            radio.channels=new[]{service,code,library,music,white};radio.frequency=820;radio.volume=.4f;radio.powered=false;
            foreach(var pair in new[]{new{objectName="Service Card",channel=service},new{objectName="Gallery Frequency Card",channel=code},new{objectName="Library Frequency Card",channel=library}})
            {var note=GameObject.Find(pair.objectName).GetComponent<ClueNote>();note.radioChannel=pair.channel;note.text=note.text.Replace(pair.channel.frequency.ToString(),"{frequency}");if(note.fullText!=null)note.fullText=note.fullText.Replace(pair.channel.frequency.ToString(),"{frequency}");}
            player.gameObject.AddComponent<NoiseTelemetry>();
            var shutter=anchors["shutter"].gameObject;
            var shutterMask=shutter.AddComponent<NoiseMaskZone>();shutterMask.radius=12;shutterMask.description="гуркіт віконниці";shutterMask.strength=.9f;
            var banging=shutter.AddComponent<AmbientCycle>();banging.mode=AmbientCycle.Mode.Shutter;
            var leaf=Cube("Loose Window Shutter",shutter.transform,shutter.transform.position+new Vector3(0,.35f,-.1f),new Vector3(2.5f,1.6f,.08f),wood);Object.DestroyImmediate(leaf.GetComponent<Collider>());banging.movingVisual=leaf.transform;
            var water=anchors["water"].gameObject;var waterMask=water.AddComponent<NoiseMaskZone>();waterMask.radius=12;waterMask.strength=.95f;waterMask.description="скидання води";
            var reservoir=Cube("Tipping Water Reservoir",water.transform,water.transform.position,new Vector3(1,.6f,.8f),dark);
            var cycle=water.AddComponent<AmbientCycle>();cycle.mode=AmbientCycle.Mode.Water;cycle.movingVisual=reservoir.transform;cycle.period=16;cycle.warningSeconds=3;cycle.loudSeconds=5;
            var clock=anchors["clock"].gameObject;clock.AddComponent<NoiseMaskZone>().strength=0;clock.AddComponent<AmbientCycle>().mode=AmbientCycle.Mode.Clock;
            var cue=new GameObject("Distant Steps After Service Door",typeof(AtmosphereCue)).GetComponent<AtmosphereCue>();cue.transform.position=new Vector3(-6,1,-8);cue.footstepsClip=Clip("Wooden");
        }
        private static RadioChannel Channel(string name,int frequency,RadioChannel.Mode mode,string transcript)
        {
            string path="Assets/RadioData/"+name+".asset";var data=AssetDatabase.LoadAssetAtPath<RadioChannel>(path);
            if(data==null){data=ScriptableObject.CreateInstance<RadioChannel>();AssetDatabase.CreateAsset(data,path);}
            data.frequency=frequency;data.mode=mode;data.transcript=transcript;data.channelName=name;data.receptionHalfWidth=25;EditorUtility.SetDirty(data);return data;
        }

        private static HingedDoor Door(string name,Vector3 center,bool verticalWall,string id,bool permanent,float width=2.35f,float height=2.65f)
        {
            var pivot=new GameObject(name).transform;
            pivot.position=center+(verticalWall?new Vector3(0,0,width/2):new Vector3(-width/2,0,0));
            pivot.rotation=Quaternion.Euler(0,verticalWall?90:0,0);
            var panel=GameObject.CreatePrimitive(PrimitiveType.Cube);panel.name="Door Panel";panel.transform.SetParent(pivot,false);panel.transform.localPosition=new Vector3(width/2,height/2,0);panel.transform.localScale=new Vector3(width,height,.13f);panel.GetComponent<Renderer>().sharedMaterial=wood;
            var door=pivot.gameObject.AddComponent<HingedDoor>();door.hinge=pivot;door.checkpointId=id;door.stayOpen=permanent;door.openAngle=100;
            if(width>2)
            {var blocker=pivot.gameObject.AddComponent<NavMeshObstacle>();blocker.shape=NavMeshObstacleShape.Box;blocker.center=new Vector3(width/2,height/2,0);blocker.size=new Vector3(width+.1f,height,.45f);blocker.carving=true;blocker.carveOnlyStationary=false;door.navigationBlocker=blocker;}
            return door;
        }
        private static void Bind(HingedDoor door,string flag){var binding=door.gameObject.AddComponent<DoorProgressBinding>();binding.door=door;binding.openFlag=flag;}
        private static KeyPickup Pickup(string name,Vector3 p,string id,string caption,string prompt,Vector3 size)
        {var key=Cube(name,null,p,size,brass).AddComponent<KeyPickup>();key.keyId=id;key.displayName=caption;key.pickupPrompt=prompt;return key;}
        private static ClueNote Note(string name,Vector3 p,string id,int frequency,string text)
        {var note=Cube(name,null,p,new Vector3(.6f,.06f,.4f),paper).AddComponent<ClueNote>();note.clueId=id;note.frequency=frequency;note.text=text;return note;}
        private static void Checkpoint(string name,int stage,string flag,Vector3 spawn,Vector3 enemy,Vector3 size)
        {var root=new GameObject(name,typeof(BoxCollider),typeof(Rigidbody),typeof(ManorCheckpoint));root.transform.position=spawn;root.GetComponent<BoxCollider>().isTrigger=true;root.GetComponent<BoxCollider>().size=size;root.GetComponent<Rigidbody>().isKinematic=true;root.GetComponent<Rigidbody>().useGravity=false;var checkpoint=root.GetComponent<ManorCheckpoint>();checkpoint.stage=stage;checkpoint.requiredFlag=flag;checkpoint.playerSpawn=spawn;checkpoint.enemySpawn=enemy;}
        private static void WorldLabel(string text,Vector3 p,Quaternion rotation,float size)
        {var label=new GameObject("Label "+text,typeof(TextMesh)).GetComponent<TextMesh>();label.text=text;label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=48;label.characterSize=size;label.anchor=TextAnchor.MiddleCenter;label.color=Color.white;label.transform.SetPositionAndRotation(p,rotation);label.GetComponent<Renderer>().sharedMaterial=label.font.material;}

        private readonly struct Opening { public readonly char Side; public readonly float Center; public Opening(char side,float center){Side=side;Center=center;} }
        private static void Room(ManorArea area,string name,float x0,float x1,float z0,float z1,float height,bool safe,Opening[] openings,bool roof=true)
        {
            var root=new GameObject(name).transform; rooms.Add(area,root);
            var zone=root.gameObject.AddComponent<ManorZone>();zone.area=area;zone.ukrainianName=name;zone.center=new Vector3((x0+x1)/2,2,(z0+z1)/2);zone.size=new Vector3(x1-x0,7,z1-z0);zone.tutorialSafe=safe;
            var floor=Cube("Floor "+area,root,new Vector3((x0+x1)/2,-.15f,(z0+z1)/2),new Vector3(x1-x0,.3f,z1-z0),area==ManorArea.Kitchen?stone:wood);
            var surface=floor.AddComponent<SurfaceType>();surface.kind=area==ManorArea.Kitchen||area==ManorArea.Conservatory?SurfaceType.Kind.Stone:SurfaceType.Kind.Wood;surface.noiseMultiplier=surface.kind==SurfaceType.Kind.Stone?1.2f:1;
            foreach(char side in new[]{'W','E','S','N'})
            {
                var centers=new List<float>();foreach(var opening in openings)if(opening.Side==side)centers.Add(opening.Center);
                bool horizontal=side=='S'||side=='N';
                Wall(root,side,side=='W'?x0:side=='E'?x1:side=='S'?z0:z1,horizontal?x0:z0,horizontal?x1:z1,height,centers.ToArray(),stone);
            }
            if(roof)Cube("Ceiling "+area,root,new Vector3((x0+x1)/2,height+.1f,(z0+z1)/2),new Vector3(x1-x0,.2f,z1-z0),dark);
        }
        private static void Wall(Transform root,char side,float fixedAxis,float start,float end,float height,float[] holes,Material material)
        {
            System.Array.Sort(holes);float from=start;
            foreach(float center in holes)
            {
                WallPart(root,side,fixedAxis,from,center-1.2f,height,0,material);
                if(height>2.7f)WallPart(root,side,fixedAxis,center-1.2f,center+1.2f,height-2.7f,2.7f,material);
                from=center+1.2f;
            }
            WallPart(root,side,fixedAxis,from,end,height,0,material);
        }
        private static void WallPart(Transform root,char side,float fixedAxis,float start,float end,float height,float bottom,Material material)
        {
            if(end-start<.01f)return;bool horizontal=side=='N'||side=='S';
            Cube("Wall "+side,root,horizontal?new Vector3((start+end)/2,bottom+height/2,fixedAxis):new Vector3(fixedAxis,bottom+height/2,(start+end)/2),horizontal?new Vector3(end-start,height,.2f):new Vector3(.2f,height,end-start),material);
        }
        private static void Cupboard(Transform room,string name,Vector3 p,float width,float height)
        {
            var root=new GameObject(name).transform;root.SetParent(room,false);
            Cube(name+" Back",root,p+new Vector3(0,height/2,.38f),new Vector3(width,height,.12f),wood);
            foreach(float side in new[]{-1f,1f})Cube(name+" Side",root,p+new Vector3(side*width/2,height/2,0),new Vector3(.1f,height,.85f),wood);
            foreach(float y in new[]{.1f,1.05f,height})Cube(name+" Shelf",root,p+new Vector3(0,y,0),new Vector3(width,.1f,.85f),wood);
            Anchor(name+".hinge",p+new Vector3(-width/2,0,-.48f));
        }
        private static void FloorPatch(string name,Transform root,Vector3 p,Vector3 size,SurfaceType.Kind kind,float noise,Material mat)
        {var go=Cube(name,root,p,size,mat);var surface=go.AddComponent<SurfaceType>();surface.kind=kind;surface.noiseMultiplier=noise;surface.loudnessMultiplier=kind==SurfaceType.Kind.Carpet?.25f:1.2f;surface.minimumNoiseRadius=kind==SurfaceType.Kind.Glass?6.5f:0;}
        private static Transform Anchor(string id,Vector3 p)
        {var go=new GameObject(id);go.transform.position=p;anchors.Add(id,go.transform);return go.transform;}
        private static Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/"+name+".mat");
        private static AudioClip Clip(string name)=>AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/"+name+".wav");
        private static GameObject Cube(string name,Transform parent,Vector3 p,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.position=p;
            var scale=parent!=null?parent.lossyScale:Vector3.one;go.transform.localScale=new Vector3(size.x/scale.x,size.y/scale.y,size.z/scale.z);
            if(material!=null)go.GetComponent<Renderer>().sharedMaterial=material;return go;
        }
    }
}
