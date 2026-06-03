namespace TreasureArenaMR.Shared
{
    /// <summary>
    /// Runtime player state, including the MR GhostRetreat flow.
    /// </summary>
    public enum PlayerState
    {
        Alive,
        GhostRetreat,
        Respawning
    }
}
