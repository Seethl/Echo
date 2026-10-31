using Echo.Interactions;
using UnityEngine;

namespace Echo.Manor
{
    public sealed class SymbolButton:Interactable
    {
        public SymbolPuzzle puzzle;
        public string symbolId,symbolName,rhythm;
        public bool demonstration;
        public int shortPulses=1;
        public AudioClip rhythmClip;
        public bool useAssociationSound;
        public Echo.Audio.PrototypeAudio.Cue soundCue;
        public AudioClip SoundClip=>rhythmClip!=null?rhythmClip:Echo.Audio.PrototypeAudio.Clip(useAssociationSound?soundCue:shortPulses==1?Echo.Audio.PrototypeAudio.Cue.LongBell:shortPulses==2?Echo.Audio.PrototypeAudio.Cue.TwoBell:Echo.Audio.PrototypeAudio.Cue.ThreeBell);
        public override string Prompt=>"E — "+(demonstration?(useAssociationSound?"послухати панель: ":"послухати дзвіночок: "):"додати символ: ")+symbolName;
        public override bool CanInteract(PlayerInteractor player)=>base.CanInteract(player)&&(demonstration||!puzzle.Solved);
        public override void Interact(PlayerInteractor player)
        {
            if(demonstration){if(!puzzle.TryDemonstrate(this,player))return;}
            else puzzle.Submit(symbolId,player);
            var feedback=GetComponent<SymbolFeedback>();if(feedback!=null)feedback.Pulse();
        }
    }
}
