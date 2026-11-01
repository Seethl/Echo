using System;
using System.Collections.Generic;
using System.Linq;
using Echo.Interactions;
using Echo.Menu;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Echo.Manor
{
    [DefaultExecutionOrder(-9800)]
    public sealed class ManorProgress : MonoBehaviour
    {
        public static ManorProgress Active {get;private set;}
        private static Snapshot checkpoint;
        private static bool restorePending;
        private static int runSerial;
        private static int previousCode,previousOrder;
        private int serial;
        public PlayerInteractor player;
        public bool treatmentRoute;
        public string statusSuffix="";
        private ManorZone[] zones;
        private GalleryWindowShutter[] galleryWindows;
        private Dictionary<string,int> values=new Dictionary<string,int>();
        private Dictionary<string,string> clues=new Dictionary<string,string>();
        private int journalPage;
        private int helpStep;
        private bool showHelp;
        [Min(0)]public float libraryOrientationSeconds=25;
        private float libraryEntryTime=-1;
        private bool restoring;
        public ManorArea Area {get;private set;}
        public bool TutorialSafe => Area==ManorArea.Caretaker||Area==ManorArea.Gallery;
        public bool HasCheckpoint => checkpoint!=null;
        public int CheckpointStage => checkpoint!=null?checkpoint.stage:0;
        public Vector3? RestoredEnemyPosition {get;private set;}
        public string GalleryCode=>Get(RunSecrets.GalleryKey).ToString("000");
        public string Objective => treatmentRoute ? TreatmentObjective : !player.HasKey("manor.service-passage")&&!Has("hall.open")?"Знайти службовий ключ у шафці":
            !Has("hall.open")?(Has("gallery.quiet")?(Has("recording.heard.940")?"Набрати отриманий код на замку біля дверей":"Послухати на радіо службову передачу 940"):GalleryWindowObjective):
            !player.HasKey("tool.winch")&&!Has("grate.open")?(Has("library.cache")?"Забрати руків’я з відкритої схованки":Area==ManorArea.Library?"Відкрити схованку: дослідити панелі й радіозапис":"Знайти бібліотеку — два проходи з холу"):
            !Has("grate.open")?(Has("winch.installed")?"Підняти решітку: зафіксувати три ступені":"Встановити руків’я в лебідку на кухні"):
            !Has("yard.open")?(player.HasKey("tool.crowbar")?"Відтиснути засув дверей у двір тричі":"Знайти лом на столі в зимовому саду"):
            !Has("gates.open")?(!player.HasKey("tool.cutters")?"Знайти болторіз в інструментальному сараї":!Has("gates.braced")?"Підперти ворота ломом, щоб розвантажити ланцюг":"Перерізати дві секції ланцюга: утримувати E"):
            "Вийти через відкриті зовнішні ворота";
        private string GalleryWindowObjective => galleryWindows!=null&&galleryWindows.Length>0 ? $"Зачинити всі віконниці ({galleryWindows.Count(w=>w.Closed)}/{galleryWindows.Length})" : "Прибрати гілку й закрити віконницю";
        private string TreatmentObjective => !Has("hall.open") ? (!player.HasKey("manor.service-passage") ? "Знайти службовий ключ" : !Has("gallery.quiet") ? GalleryWindowObjective : !Has("recording.heard.940") ? "Послухати передачу 940" : "Відімкнути прохід до холу") :
            !Has("library.cache") ? "Дослідити бібліотечну схованку" :
            !Has("remedy.recipe.read") ? "Знайти рецепт біля картини нагорі" :
            !player.HasKey("remedy.study-key") && !player.HasKey("remedy.base") ? "Оглянути житлову кімнату нагорі" :
            !player.HasKey("remedy.base") ? "Відкрити аптечну шафку на кухні" :
            !Has("grate.open") ? "Відкрити прохід до саду лебідкою" :
            !Has("yard.open") ? "Відчинити заклинені двері до зовнішнього саду" :
            !player.HasKey("remedy.flower") ? "Знайти квітку з рецепта" :
            !player.HasKey("remedy.spoon") ? "Знайти срібну ложечку серед посуду" :
            Get("remedy.steps")>=3&&!Has("remedy.bottled")&&!player.HasKey("remedy.potion") ? (player.HasKey("remedy.bottle")?"Набрати ліки в пляшечку на кухні":"Знайти порожню пляшечку для ліків") :
            !player.HasKey("remedy.potion") ? "Приготувати мікстуру на кухні" :
            !Has("remedy.served") ? "Залишити ліки в чашці мешканця у холі" : "Відійти за укриття й дочекатися мешканця";
        public bool LibraryOrientationActive=>libraryEntryTime>=0&&!Has("library.orientation.done");
        public bool IsLibraryOrientationPosition(Vector3 position)=>LibraryOrientationActive&&zones.Any(z=>z.area==ManorArea.Library&&z.Contains(position));

        private sealed class Snapshot
        {
            public int stage;public Vector3 spawn,enemy;public Quaternion rotation;
            public Dictionary<string,int> values;public Dictionary<string,string> clues,objects;
            public string[] keys;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Register()
        {
            checkpoint=null;restorePending=false;Active=null;runSerial=0;
            GameMenu.NewGameStarting+=ResetRun;
            GameMenu.SceneLeaving+=_=>{if(Active!=null&&Active.serial==runSerial)Active.RefreshCheckpoint();};
            GameMenu.ContinueAvailable=()=>checkpoint!=null;
            GameMenu.ContinueAction=ResumeCheckpoint;
        }
        private static void ResetRun(){checkpoint=null;restorePending=false;runSerial++;}
        public static void ResumeCheckpoint()
        {
            if(checkpoint==null){GameMenu.StartNewGame();return;}
            restorePending=true;GameMenu.ReloadGameplay();
        }
        private void Awake()
        {
            Active=this;serial=runSerial;zones=FindObjectsByType<ManorZone>();galleryWindows=FindObjectsByType<GalleryWindowShutter>();
            if(restorePending&&checkpoint!=null)
            {values=new Dictionary<string,int>(checkpoint.values);clues=new Dictionary<string,string>(checkpoint.clues);RestoredEnemyPosition=checkpoint.enemy;}
            if(!Has(RunSecrets.GalleryKey)){previousCode=RunSecrets.NewCode(previousCode);values[RunSecrets.GalleryKey]=previousCode;}
            if(!Has(RunSecrets.LibraryKey)){previousOrder=RunSecrets.NewOrder(previousOrder);values[RunSecrets.LibraryKey]=previousOrder;}
        }
        private void Start()
        {
            if(player==null)player=FindAnyObjectByType<PlayerInteractor>();
            if(restorePending&&checkpoint!=null)
            {
                restoring=true;
                player.RestoreKeys(checkpoint.keys);
                foreach(var state in Participants())if(checkpoint.objects.TryGetValue(state.CheckpointId,out var text))state.RestoreState(text);
                var controller=player.GetComponent<CharacterController>();controller.enabled=false;
                player.transform.SetPositionAndRotation(checkpoint.spawn,checkpoint.rotation);controller.enabled=true;
                restorePending=false;
                restoring=false;
            }
            player.InventoryChanged+=RefreshCheckpoint;
            player.ui.ShowToast("Досліди маєток. E — оглянути; R — радіо; J — записи.");
        }
        private void Update()
        {
            if(player==null)return;
            foreach(var zone in zones)if(zone.Contains(player.transform.position))Area=zone.area;
            if(!Has("area."+Area))Set("area."+Area);
            if(Area==ManorArea.Library&&libraryEntryTime<0)libraryEntryTime=Time.time;
            if(LibraryOrientationActive&&Time.time-libraryEntryTime>=libraryOrientationSeconds)Set("library.orientation.done");
            if(Area==ManorArea.Hall&&!Has("tutorial.noise"))
            {
                Set("tutorial.noise");
                const string lesson="Гучні звуки довкола можуть приховати твої дії. Поруч із ворогом навіть тихий звук небезпечний.";
                AddClue("tutorial.noise",lesson+" J — журнал і допомога. У холі килим тихіший за скло.");player.ui.ShowToast(lesson,6);
            }
            player.ui.SetHUD("Ціль: "+Objective+"\n"+statusSuffix);
            if(!GameMenu.BlocksPlayerInput&&Keyboard.current!=null&&Keyboard.current.jKey.wasPressedThisFrame)
            {
                showHelp=false;helpStep=0;
                player.ui.OpenPanel(this,"ЖУРНАЛ І ДОПОМОГА",()=>showHelp?HelpText(helpStep):CluePage(),new[]{
                    new InteractionUI.Choice("Попередній запис",()=>{showHelp=false;journalPage=Mathf.Max(0,journalPage-1);}),
                    new InteractionUI.Choice("Наступний запис",()=>{showHelp=false;journalPage=Mathf.Min(Mathf.Max(0,clues.Count-1),journalPage+1);}),
                    new InteractionUI.Choice("Потрібна підказка",()=>{showHelp=true;helpStep=Mathf.Min(3,helpStep+1);}),
                    new InteractionUI.Choice("Закрити",()=>player.ui.ClosePanel())});
            }
        }
        private string AreaName(){var zone=zones.FirstOrDefault(z=>z.area==Area);return zone!=null?zone.ukrainianName:"Маєток";}
        public bool IsTutorialPosition(Vector3 point)=>zones.Any(z=>z.tutorialSafe&&z.Contains(point));
        public int Get(string id)=>values.TryGetValue(id,out int value)?value:0;
        public bool Has(string id)=>Get(id)>0;
        public void Set(string id,int value=1){values[id]=value;RefreshCheckpoint();}
        public void SetFlag(string id)=>Set(id);
        public void AddClue(string id,string text){if(clues.TryGetValue(id,out var old)&&old==text)return;clues[id]=text;RefreshCheckpoint();}
        public string Journal=>clues.Count==0?"Поки немає записаних підказок.":string.Join("\n\n",clues.Values);
        private string CluePage(){if(clues.Count==0)return Journal;journalPage=Mathf.Clamp(journalPage,0,clues.Count-1);return (journalPage+1)+" / "+clues.Count+"\n"+clues.Values.ElementAt(journalPage);}
        public string HelpText(int step)
        {
            if(treatmentRoute) return "Ціль: "+Objective+(step>=2 ? "\n\nСходи — біля західної стіни холу. Бібліотека — за двома північними проходами. Рецепт прикріплений біля картини у житловій кімнаті нагорі. Аптечна шафка та миска — на кухні. Зовнішній сад — за зимовим садом. J зберігає прочитане." : "");
            string direction,connection;
            if(!Has("hall.open")){direction="Радіо лежить на столі в кімнаті доглядача; частота галереї — на її картці.";connection="Після закривання всіх віконниць налаштуйте радіо на передачу галереї. Код змінюється для кожної нової гри; почутий запис зберігається в J.";}
            else if(!player.HasKey("tool.winch")&&!Has("grate.open")){direction=Area==ManorArea.Library?"Огляньте звукові панелі на лівій стіні та схованку з такими самими знаками.":"Бібліотека має два проходи з дальньої частини холу. Шукайте вказівники.";connection="Зіставте звуки панелей із послідовністю радіопередачі. Введення — на кнопках схованки, не на демонстраційних панелях.";}
            else if(!Has("grate.open")){direction="На кухні лебідка стоїть поруч із решіткою. Інструкція — поряд.";connection="Після подвійного клацання потрібно відпустити Space, щоб стопор зафіксував ступінь.";}
            else if(!Has("yard.open")){direction="Лом лежить на столі ліворуч від входу в зимовий сад; двері у двір — навпроти.";connection="Гуркіт води може прикрити тріск засуву. Прогрес натискань зберігається.";}
            else{direction="Болторіз — у сараї праворуч; місце підпори — біля петлі воріт.";connection="Підіпри стулку ломом. Тримай E на ланцюгу; відпускай, щоб сховатися. Готові зрізи збережуться. Радіо може відвести мешканця.";}
            return "Ціль: "+Objective+(step>=3?"\n\n"+connection:step>=2?"\n\n"+direction:"");
        }
        public void SaveCheckpoint(int stage,Vector3 spawn,Vector3 enemy)
        {
            if(checkpoint!=null&&stage<=checkpoint.stage)return;
            checkpoint=new Snapshot{stage=stage,spawn=spawn,enemy=enemy,rotation=Quaternion.identity};
            RefreshCheckpoint();player.ui.ShowToast("Контрольна точка. Виконані задачі збережено до завершення цього запуску.");
        }
        public void RefreshCheckpoint()
        {
            if(checkpoint==null||player==null||serial!=runSerial||restoring)return;
            checkpoint.values=new Dictionary<string,int>(values);checkpoint.clues=new Dictionary<string,string>(clues);checkpoint.keys=player.GetKeys();
            checkpoint.objects=new Dictionary<string,string>();
            foreach(var state in Participants())
            {
                if(checkpoint.objects.ContainsKey(state.CheckpointId)){Debug.LogError("Duplicate checkpoint id: "+state.CheckpointId);continue;}
                checkpoint.objects.Add(state.CheckpointId,state.CaptureState());
            }
        }
        private IEnumerable<ICheckpointObject> Participants()=>FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include).OfType<ICheckpointObject>().Where(x=>!string.IsNullOrEmpty(x.CheckpointId));
        public void ShowFailure()
        {
            RefreshCheckpoint();
            player.ui.OpenPanel(this,"МЕШКАНЕЦЬ ВАС ЗНАЙШОВ",()=>checkpoint!=null?"Повернутися до останньої контрольної точки. Виконані задачі залишаться виконаними.":"Спробуйте ще раз. Службова частина маєтку безпечна для навчання.",
                new[]{new InteractionUI.Choice(checkpoint!=null?"Від контрольної точки":"Повторити",ResumeCheckpoint),new InteractionUI.Choice("Головне меню",GameMenu.ReturnToMainMenu)},true,null,true,false);
        }
        private void OnDestroy(){if(player!=null)player.InventoryChanged-=RefreshCheckpoint;if(Active==this)Active=null;}
    }
}
