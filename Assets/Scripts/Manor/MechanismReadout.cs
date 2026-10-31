using Echo.Audio;
using UnityEngine;

namespace Echo.Manor
{
    // Presentation only: the original mechanism still owns colliders and progression.
    public sealed class MechanismReadout:MonoBehaviour
    {
        public TextMesh display;
        public WinchMechanism winch;
        public ToolBarrier barrier;
        public AmbientCycle water;
        public Transform handleVisual;
        public Transform[] remainingSections;
        private void Update()
        {
            var progress=ManorProgress.Active;if(progress==null)return;
            if(winch!=null)
            {
                bool installed=progress.Has("winch.installed");
                if(handleVisual!=null)handleVisual.gameObject.SetActive(installed);
                if(display!=null)display.text=progress.Has("grate.open")?"РЕШІТКУ ПІДНЯТО\nПрохід у зимовий сад":installed?"ЛЕБІДКА · "+winch.Steps+" / "+winch.cyclesRequired+"\nE — працювати; тримай і відпускай Space":"ПОРОЖНЄ ГНІЗДО\nПотрібне руків’я з бібліотеки";
            }
            if(barrier!=null)
            {
                int count=progress.Get(barrier.counterId);
                if(remainingSections!=null)for(int i=0;i<remainingSections.Length;i++)if(remainingSections[i]!=null)remainingSections[i].gameObject.SetActive(i>=count);
                if(display!=null)display.text=progress.Has(barrier.openFlag)?"ПРОХІД ВІДКРИТО":barrier.toolId=="tool.cutters"?
                    "ЛАНЦЮГ · "+count+" / "+barrier.requiredActions+"\n"+(!string.IsNullOrEmpty(barrier.requiredFlag)&&!progress.Has(barrier.requiredFlag)?"Натягнуто! Підіпри стулку ломом біля петлі.":barrier.Working?"РІЗАННЯ · "+Mathf.RoundToInt(barrier.HoldProgress*100)+"% · відпусти E, щоб відійти":"Утримуй E з болторізом. Зріз створить гучний звук."):
                    "ЗАКЛИНЕНИЙ ЗАСУВ · "+count+" / "+barrier.requiredActions+"\nПотрібен лом. Тріск буде гучним.";
            }
            if(water!=null&&display!=null)
                display.text=water.Loud?"СКИДАННЯ ВОДИ\nГуркіт може прикрити дії":water.Warning?"РЕЗЕРВУАР ПОВНИЙ\nЗараз буде гучне скидання":"ВОДА НАБИРАЄТЬСЯ\nПрискорене капання попереджає про скид";
        }
    }
}
