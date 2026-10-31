using Echo.Interactions;
using UnityEngine;

namespace Echo.Manor
{
    public sealed class SymbolButton:Interactable
    {
        public SymbolPuzzle puzzle;
        public string symbolId,symbolName,rhythm;
        public bool demonstration;
        public bool playOnSubmit;
        public int shortPulses=1;
        public AudioClip rhythmClip;
        public bool useAssociationSound;
        public Echo.Audio.PrototypeAudio.Cue soundCue;
        public AudioClip SoundClip
        {
            get
            {
                var raw=rhythmClip!=null?rhythmClip:Echo.Audio.PrototypeAudio.Clip(useAssociationSound?soundCue:shortPulses==1?Echo.Audio.PrototypeAudio.Cue.LongBell:shortPulses==2?Echo.Audio.PrototypeAudio.Cue.TwoBell:Echo.Audio.PrototypeAudio.Cue.ThreeBell);
                return puzzle!=null&&puzzle.cuePeak>0?Echo.Audio.PrototypeAudio.LevelledClip(raw,puzzle.cuePeak):raw;
            }
        }
        public override string Prompt=>"E — "+(demonstration?(useAssociationSound?"послухати панель: ":"послухати дзвіночок: "):"додати символ: ")+symbolName;
        public override bool CanInteract(PlayerInteractor player)=>base.CanInteract(player)&&(demonstration||!puzzle.Solved)&&(!playOnSubmit||!puzzle.CuePlaying);
        public override void Interact(PlayerInteractor player)
        {
            if(demonstration){if(!puzzle.TryDemonstrate(this,player))return;}
            else if(!puzzle.Submit(symbolId,player,playOnSubmit?this:null))return;
            var feedback=GetComponent<SymbolFeedback>();if(feedback!=null)feedback.Pulse();
        }
    }
}
