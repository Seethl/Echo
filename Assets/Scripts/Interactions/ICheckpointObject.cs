namespace Echo.Interactions
{
    // Small in-memory contract; participants retain their own Inspector data and visuals.
    public interface ICheckpointObject
    {
        string CheckpointId { get; }
        string CaptureState();
        void RestoreState(string state);
    }
}
