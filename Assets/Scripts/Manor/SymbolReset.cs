using Echo.Interactions;

namespace Echo.Manor
{
    public sealed class SymbolReset:Interactable
    {
        public SymbolPuzzle puzzle;
        public override string Prompt=>"E — скинути введені символи";
        public override bool CanInteract(PlayerInteractor player)=>base.CanInteract(player)&&!puzzle.Solved;
        public override void Interact(PlayerInteractor player)=>puzzle.ResetInput(player);
    }
}
