using TreasureArenaMR.Network;
using TreasureArenaMR.Server;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TreasureArenaMR.UI
{
    public sealed class HudView : MonoBehaviour
    {
        private const float RefreshInterval = 0.5f;
        private const float LogInterval = 2f;
        private const string LogPrefix = "[PicoHudSync]";

        [SerializeField] private Text _debugText;
        [SerializeField] private Text _timeText;

        private NetworkManager _networkManager;
        private NetworkMatchState _networkMatchState;
        private RoomManager _roomManager;

        private float _refreshTimer;
        private float _logTimer;
        private bool _loggedInitialState;
        private bool _loggedMatchStateFound;

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

            LogInitialState();

            string conn;
            if (nm == null) conn = "Net: MISSING";
            else if (!nm.IsRunning) conn = "Net: Not Running";
            else if (nm.IsServer) conn = nm.IsConnected ? "Net: Server OK" : "Net: Server Starting";
            else if (nm.IsClient) conn = nm.IsConnected ? "Net: Client OK" : "Net: Client Connecting";
            else conn = "Net: Host";

            string addr = nm != null ? $"{nm.ServerAddress}:{nm.ServerPort}" : "";
            string room = "-";
            string scores = "";
            string map = "";
            float remainingTime = -1f;
            string source = "None";

            if (ns != null)
            {
                LogMatchStateFound(ns);

                room = ns.RoomState.ToString();
                scores = $"R{ns.RedScore}:B{ns.BlueScore}";
                remainingTime = ns.RemainingTime;
                map = ns.MapConfigured ? $"Map #{ns.MapIndex}" : "Map: -";
                source = "NetworkMatchState";
            }

            if (rm != null)
            {
                if (ns == null)
                {
                    room = rm.CurrentRoomState.ToString();
                    scores = $"R{rm.RedScore}:B{rm.BlueScore}";
                    remainingTime = rm.RemainingTime;
                    source = "RoomManager";
                }

                if (rm.CurrentRoomConfig != null)
                    map = rm.CurrentRoomConfig.map_id;

                map += rm.MapData != null ? " loaded" : " not loaded";
            }

            string timeText = FormatRemainingTime(remainingTime);

            if (_timeText != null)
                _timeText.text = timeText;

            if (_debugText != null)
                _debugText.text = $"{conn}  {addr}\nRoom: {room}  {scores}  {timeText}\n{map}";

            LogHeartbeat(source, conn, room, remainingTime, timeText, ns, rm);
        }

        private void CacheReferences()
        {
            if (_networkManager == null)
                _networkManager = NetworkManager.Instance;
            if (_networkMatchState == null)
                _networkMatchState = FindObjectOfType<NetworkMatchState>();
            if (_roomManager == null)
                _roomManager = FindObjectOfType<RoomManager>();
            if (_debugText == null)
                _debugText = FindText("DebugText");
            if (_timeText == null)
                _timeText = FindText("TimeText");
        }

        private static string FormatRemainingTime(float remainingTime)
        {
            if (remainingTime < 0f)
                return "--:--";

            int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, remainingTime));
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes:00}:{seconds:00}";
        }

        private Text FindText(string objectName)
        {
            Text[] texts = GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].name == objectName)
                    return texts[i];
            }

            return null;
        }

        private void LogInitialState()
        {
            if (_loggedInitialState)
                return;

            _loggedInitialState = true;
            Debug.Log($"{LogPrefix} Start scene={SceneManager.GetActiveScene().name} " +
                $"platform={Application.platform} active={isActiveAndEnabled} " +
                $"debugText={_debugText != null} timeTextBound={_timeText != null}");
            if (_timeText == null)
                Debug.LogWarning($"{LogPrefix} timeText missing: expected GameHudCanvas/TopHud/TimeText");
        }

        private void LogMatchStateFound(NetworkMatchState matchState)
        {
            if (_loggedMatchStateFound)
                return;

            _loggedMatchStateFound = true;
            Debug.Log($"{LogPrefix} NetworkMatchState found " +
                $"roomState={matchState.RoomState} remaining={matchState.RemainingTime:F1} " +
                $"mapConfigured={matchState.MapConfigured} mapIndex={matchState.MapIndex} " +
                $"mapRevision={matchState.MapRevision}");
        }

        private void LogHeartbeat(
            string source,
            string connection,
            string room,
            float remainingTime,
            string timeText,
            NetworkMatchState matchState,
            RoomManager roomManager)
        {
            _logTimer -= RefreshInterval;
            if (_logTimer > 0f)
                return;

            _logTimer = LogInterval;
            Debug.Log($"{LogPrefix} heartbeat source={source} connection=\"{connection}\" " +
                $"room={room} remaining={remainingTime:F1} timeText={timeText} " +
                $"debugTextBound={_debugText != null} timeTextBound={_timeText != null} " +
                $"hasNetworkMatchState={matchState != null} hasRoomManager={roomManager != null}");
        }
    }
}
