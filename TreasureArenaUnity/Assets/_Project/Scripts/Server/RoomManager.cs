using TreasureArenaMR.Map;

namespace TreasureArenaMR.Server
{
    /// <summary>
    /// Placeholder for server-owned room creation, joining, team assignment, and lifecycle state.
    /// </summary>
    public sealed class RoomManager
    {
        public MapData MapData { get; private set; }

        public void SetMapData(MapData mapData)
        {
            MapData = mapData;
        }
    }
}
