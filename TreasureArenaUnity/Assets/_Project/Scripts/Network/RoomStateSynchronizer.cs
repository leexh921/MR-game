using TreasureArenaMR.Server;
using UnityEngine;

namespace TreasureArenaMR.Network
{
    /// <summary>
    /// Periodically writes server room state into NetworkMatchState networked properties,
    /// which Netick replicates to all clients.
    /// </summary>
    public sealed class RoomStateSynchronizer : MonoBehaviour
    {
        [SerializeField] private float _syncInterval = 0.5f;
        [SerializeField] private RoomManager _roomManager;
        [SerializeField] private NetworkManager _networkManager;

        private NetworkMatchState _networkMatchState;
        private float _syncTimer;

        private void Start()
        {
            if (_roomManager == null)
                _roomManager = FindObjectOfType<RoomManager>();
            if (_networkManager == null)
                _networkManager = FindObjectOfType<NetworkManager>();
        }

        private void Update()
        {
            if (_roomManager == null || _networkManager == null) return;
            if (!_networkManager.IsServer) return;

            _syncTimer += Time.deltaTime;
            if (_syncTimer >= _syncInterval)
            {
                _syncTimer = 0f;
                SyncState();
            }
        }

        private void SyncState()
        {
            if (_networkMatchState == null)
                _networkMatchState = FindObjectOfType<NetworkMatchState>();

            if (_networkMatchState == null)
            {
                Debug.LogWarning("[RoomStateSynchronizer] NetworkMatchState not found yet");
                return;
            }

            _networkMatchState.Sync(
                _roomManager.CurrentRoomState,
                _roomManager.RedScore,
                _roomManager.BlueScore,
                _roomManager.RemainingTime);

            var config = _roomManager.CurrentRoomConfig;
            if (config != null)
            {
                int mapIndex = Map.RuntimeMapCatalog.GetIndex(config.map_id);
                _networkMatchState.SetMap(mapIndex, 1);
            }

            Debug.Log($"[RoomStateSynchronizer] Synced: State={_roomManager.CurrentRoomState}, " +
                $"Red={_roomManager.RedScore}, Blue={_roomManager.BlueScore}, " +
                $"Time={_roomManager.RemainingTime:F0}, Players={_roomManager.Players.Count}");
        }
    }
}
