using TreasureArenaMR.Server;
using UnityEngine;

namespace TreasureArenaMR.Network
{
    /// <summary>
    /// Periodically syncs server-owned room state to all clients.
    /// Phase 3: Uses Debug logging. 
    /// TODO Phase 5: Implement via Netick.NetworkBehaviour with [Networked] properties
    /// or RPCs for automatic state replication instead of manual JSON broadcasting.
    /// </summary>
    public sealed class RoomStateSynchronizer : MonoBehaviour
    {
        [SerializeField] private float _syncInterval = 0.5f;
        [SerializeField] private RoomManager _roomManager;
        [SerializeField] private NetworkManager _networkManager;

        private float _syncTimer;
        private NetworkMatchState _matchState;

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
            if (_matchState == null) _matchState = FindObjectOfType<NetworkMatchState>();

            _syncTimer += Time.deltaTime;
            if (_syncTimer >= _syncInterval)
            {
                _syncTimer = 0f;
                SyncState();
            }
        }

        private void SyncState()
        {
            var config = _roomManager.CurrentRoomConfig;
            if (config == null) return;

            if (_matchState != null)
            {
                _matchState.Sync(
                    _roomManager.CurrentRoomState,
                    _roomManager.RedScore,
                    _roomManager.BlueScore,
                    _roomManager.RemainingTime);
            }

            Debug.Log($"[RoomStateSynchronizer] State={_roomManager.CurrentRoomState}, " +
                $"Red={_roomManager.RedScore}, Blue={_roomManager.BlueScore}, " +
                $"Time={_roomManager.RemainingTime:F1}, Players={_roomManager.Players.Count}");
        }
    }
}
