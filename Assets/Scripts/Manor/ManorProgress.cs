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
        private int serial;
        public PlayerInteractor player;
        public string statusSuffix="";
        private ManorZone[] zones;
        private Dictionary<string,int> values=new Dictionary<string,int>();
        private Dictionary<string,string> clues=new Dictionary<string,string>();
        private int journalPage;
        private bool restoring;
        public ManorArea Area {get;private set;}
        public bool TutorialSafe => Area==ManorArea.Caretaker||Area==ManorArea.Gallery;
        public bool HasCheckpoint => checkpoint!=null;
        public int CheckpointStage => checkpoint!=null?checkpoint.stage:0;
        public Vector3? RestoredEnemyPosition {get;private set;}
        public string Objective => !player.HasKey("manor.service-passage")&&!Has("hall.open")?"Знайти службовий ключ":
            !Has("hall.open")?"Почути код і відкрити хол":!player.HasKey("tool.winch")&&!Has("grate.open")?"Пройти до бібліотеки й знайти руків’я":
            !Has("grate.open")?"Підняти решітку на кухні":!Has("yard.open")?"Відтиснути засув дверей у двір":!Has("gates.open")?"Знайти болторіз і відкрити ворота":"Залишити територію через ворота";

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
            Active=this;serial=runSerial;zones=FindObjectsByType<ManorZone>();
            if(restorePending&&checkpoint!=null)
            {values=new Dictionary<string,int>(checkpoint.values);clues=new Dictionary<string,string>(checkpoint.clues);RestoredEnemyPosition=checkpoint.enemy;}
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
            player.ui.ShowToast("Знайти вихід. E — оглянути предмет; Ctrl — рухатися тихіше.");
        }
        private void Update()
        {
            if(player==null)return;
            foreach(var zone in zones)if(zone.Contains(player.transform.position))Area=zone.area;
            if(!Has("area."+Area))Set("area."+Area);
            player.ui.SetHUD(AreaName()+"\nЦіль: "+Objective+"\n"+statusSuffix);
            if(!GameMenu.BlocksPlayerInput&&Keyboard.current!=null&&Keyboard.current.jKey.wasPressedThisFrame)
                player.ui.OpenPanel(this,"ЗНАЙДЕНІ ПІДКАЗКИ",CluePage,new[]{new InteractionUI.Choice("Попередня",()=>journalPage=Mathf.Max(0,journalPage-1)),new InteractionUI.Choice("Наступна",()=>journalPage=Mathf.Min(Mathf.Max(0,clues.Count-1),journalPage+1)),new InteractionUI.Choice("Закрити",()=>player.ui.ClosePanel())});
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
