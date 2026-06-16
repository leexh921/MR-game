using TreasureArenaMR.Network;
using TreasureArenaMR.Server;
using TreasureArenaMR.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace TreasureArenaMR.UI
{
    public sealed class HudView : MonoBehaviour
    {
        private const float RefreshInterval = 0.5f;

        [SerializeField] private Text _debugText;

        private NetworkManager _networkManager;
        private NetworkMatchState _networkMatchState;
        private RoomManager _roomManager;

        private float _refreshTimer;

        private void Update()
        {
            _refreshTimer -= Time.deltaTime;
            if (_refreshTimer > 0f) return;
            _refreshTimer = RefreshInterval;
            Refresh();
        }

        private void Refresh()
        {
            CacheReferences();

            var nm = _networkManager;
            var ns = _networkMatchState;
            var rm = _roomManager;

            if (_debugText == null) return;

            string conn;
            if (nm == null) conn = "Net: MISSING";
            else if (!nm.IsRunning) conn = "Net: Not Running";
            else if (nm.IsServer) conn = nm.IsConnected ? "Net: Server OK" : "Net: Server Starting";
            else if (nm.IsClient) conn = nm.IsConnected ? "Net: Client OK" : "Net: Client Connecting";
            else conn = "Net: Host";

            string addr = nm != null ? $"{nm.ServerAddress}:{nm.ServerPort}" : "";

            string room = "—";
            string scores = "";
            string time = "";
            string map = "";

            if (ns != null)
            {
                room = ns.RoomState.ToString();
                scores = $"R{ns.RedScore}:B{ns.BlueScore}";
                if (ns.RemainingTime > 0f) time = $"{ns.RemainingTime:F0}s";
                map = ns.MapConfigured ? $"Map #{ns.MapIndex}" : "Map: —";
            }

            if (rm != null)
            {
                if (ns == null)
                {
                    room = rm.CurrentRoomState.ToString();
                    scores = $"R{rm.RedScore}:B{rm.BlueScore}";
                    if (rm.RemainingTime > 0f) time = $"{rm.RemainingTime:F0}s";
                }
                if (rm.CurrentRoomConfig != null)
                    map = rm.CurrentRoomConfig.map_id;
                if (rm.MapData != null) map += " ✓";
                else map += " ✗";
            }

            _debugText.text = $"{conn}  {addr}\nRoom: {room}  {scores}  {time}\n{map}";
        }

        private void CacheReferences()
        {
            if (_networkManager == null)
                _networkManager = NetworkManager.Instance;
            if (_networkMatchState == null)
                _networkMatchState = FindObjectOfType<NetworkMatchState>();
            if (_roomManager == null)
                _roomManager = FindObjectOfType<RoomManager>();
        }
    }
}
