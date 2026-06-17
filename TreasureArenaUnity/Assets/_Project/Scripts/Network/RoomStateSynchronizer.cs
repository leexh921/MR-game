using TreasureArenaMR.Server;
using TreasureArenaMR.Shared;
using UnityEngine;

namespace TreasureArenaMR.Network
{
    /// <summary>
    /// Periodically writes server room state into NetworkMatchState networked properties,
    /// which Netick replicates to all clients.
    /// </summary>
    public sealed class RoomStateSynchronizer : MonoBehaviour
    {
        private const float StateLogInterval = 1f;
        private const string LogPrefix = "[ServerStateSync]";

        [SerializeField] private float _syncInterval = 0.5f;
        [SerializeField] private RoomManager _roomManager;
        [SerializeField] private NetworkManager _networkManager;

        private NetworkMatchState _networkMatchState;
        private float _syncTimer;
        private float _stateLogTimer;
        private RoomState _lastLoggedRoomState;
        private int _lastLoggedMapIndex = -1;
        private int _lastLoggedRemainingSecond = -1;
        private bool _hasLoggedState;

        private void Start()
        {
            if (_roomManager == null)
                _roomManager = FindObjectOfType<RoomManager>();
            if (_networkManager == null)
                _networkManager = FindObjectOfType<NetworkManager>();

            Debug.Log($"{LogPrefix} Start roomManager={_roomManager != null} " +
                $"networkManager={_networkManager != null}");
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
                Debug.LogWarning($"{LogPrefix} NetworkMatchState not found yet " +
                    $"roomManager={_roomManager != null} networkManager={_networkManager != null}");
                return;
            }

            int mapIndex = -1;
            string mapId = "-";

            _networkMatchState.Sync(
                _roomManager.CurrentRoomState,
                _roomManager.RedScore,
                _roomManager.BlueScore,
                _roomManager.RemainingTime);

            var config = _roomManager.CurrentRoomConfig;
            if (config != null)
            {
                mapId = config.map_id;
                mapIndex = Map.RuntimeMapCatalog.GetIndex(config.map_id);
                _networkMatchState.SetMap(mapIndex, 1);
            }

            LogSyncedState(mapId, mapIndex);
        }

        private void LogSyncedState(string mapId, int mapIndex)
        {
            _stateLogTimer -= _syncInterval;

            int remainingSecond = Mathf.CeilToInt(Mathf.Max(0f, _roomManager.RemainingTime));
            bool stateChanged = !_hasLoggedState || _lastLoggedRoomState != _roomManager.CurrentRoomState;
            bool mapChanged = !_hasLoggedState || _lastLoggedMapIndex != mapIndex;
            bool secondChanged = !_hasLoggedState || _lastLoggedRemainingSecond != remainingSecond;
            bool intervalElapsed = _stateLogTimer <= 0f;

            if (!stateChanged && !mapChanged && !(secondChanged && intervalElapsed))
                return;

            _hasLoggedState = true;
            _stateLogTimer = StateLogInterval;
            _lastLoggedRoomState = _roomManager.CurrentRoomState;
            _lastLoggedMapIndex = mapIndex;
            _lastLoggedRemainingSecond = remainingSecond;

            Debug.Log($"{LogPrefix} State={_roomManager.CurrentRoomState} " +
                $"Time={_roomManager.RemainingTime:F1} Red={_roomManager.RedScore} Blue={_roomManager.BlueScore} " +
                $"Players={_roomManager.Players.Count} mapId={mapId} mapIndex={mapIndex} " +
                $"mapRevision={_networkMatchState.MapRevision} mapConfigured={_networkMatchState.MapConfigured}");
        }
    }
}
