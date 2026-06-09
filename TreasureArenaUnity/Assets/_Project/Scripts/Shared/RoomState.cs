namespace TreasureArenaMR.Shared
{
    /// <summary>
    /// Server-owned room lifecycle state.
    /// </summary>
    public enum RoomState
    {
        Waiting,
        Ready,
        Countdown,
        Playing,
        Finished
    }
}
