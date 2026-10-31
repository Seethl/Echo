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
        public override string Prompt=>"E — "+(demonstration?"послухати: ":"натиснути: ")+symbolName;
        public override void Interact(PlayerInteractor player)
        {
            Echo.Audio.PrototypeAudio.Play(gameObject,shortPulses==1?Echo.Audio.PrototypeAudio.Cue.LongBell:shortPulses==2?Echo.Audio.PrototypeAudio.Cue.TwoBell:Echo.Audio.PrototypeAudio.Cue.ThreeBell,.7f,rhythmClip);
            NoiseSystem.Emit(transform.position,10,gameObject,NoiseSystem.Kind.Mechanism);
            if(demonstration)player.ui.ShowToast(symbolName+": "+rhythm);
            else puzzle.Submit(symbolId,player);
        }
    }
}
