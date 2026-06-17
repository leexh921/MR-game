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
        [SerializeField] private Text _hpText;
        [SerializeField] private Text _treasureText;

        private ClientMatchStateStore _clientStore;
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

            var store = _clientStore;
            var rm = _roomManager;

            LogInitialState();

            string conn = store != null && store.IsConnected ? "Net: Client OK" : "Net: Waiting";
            string addr = store != null ? store.ConnectionStatus : "";
            string room = "-";
            string scores = "";
            string map = "";
            float remainingTime = -1f;
            string hpText = "HP: --";
            string treasureText = "Treasure: -";
            string source = "None";

            if (store != null && store.HasMatchSnapshot)
            {
                LogMatchStateFound(store);

                MatchSnapshotPayload snapshot = store.MatchSnapshot;
                room = snapshot.room_state;
                scores = $"R{snapshot.red_score}:B{snapshot.blue_score}";
                remainingTime = snapshot.remaining_time;
                map = snapshot.map_id + " rev " + snapshot.map_revision;
                source = "ClientMatchStateStore";
                if (store.TryGetLocalPlayer(out PlayerSnapshot localPlayer))
                {
                    hpText = $"HP: {localPlayer.hp}/{localPlayer.max_hp}";
                    treasureText = string.IsNullOrEmpty(localPlayer.carried_treasure_id)
                        ? "Treasure: -"
                        : "Treasure: " + localPlayer.carried_treasure_id;
                }
            }

            if (rm != null)
            {
                if (store == null || !store.HasMatchSnapshot)
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
            if (_hpText != null)
                _hpText.text = hpText;
            if (_treasureText != null)
                _treasureText.text = treasureText;

            if (_debugText != null)
                _debugText.text = $"{conn}  {addr}\nRoom: {room}  {scores}  {timeText}\n{map}";

            LogHeartbeat(source, conn, room, remainingTime, timeText, hpText, treasureText, store, rm);
        }

        private void CacheReferences()
        {
            if (_clientStore == null)
                _clientStore = ClientMatchStateStore.Instance;
            if (_roomManager == null)
                _roomManager = FindObjectOfType<RoomManager>();
            if (_debugText == null)
                _debugText = FindText("DebugText");
            if (_timeText == null)
                _timeText = FindText("TimeText");
            if (_hpText == null)
                _hpText = FindText("HpText");
            if (_treasureText == null)
                _treasureText = FindText("TreasureText");
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
                $"debugText={_debugText != null} timeTextBound={_timeText != null} " +
                $"hpTextBound={_hpText != null} treasureTextBound={_treasureText != null}");
            if (_timeText == null)
                Debug.LogWarning($"{LogPrefix} timeText missing: expected GameHudCanvas/TopHud/TimeText");
        }

        private void LogMatchStateFound(ClientMatchStateStore store)
        {
            if (_loggedMatchStateFound)
                return;

            _loggedMatchStateFound = true;
            MatchSnapshotPayload snapshot = store.MatchSnapshot;
            Debug.Log($"{LogPrefix} ClientMatchStateStore snapshot found " +
                $"roomState={snapshot.room_state} remaining={snapshot.remaining_time:F1} " +
                $"mapId={snapshot.map_id} mapRevision={snapshot.map_revision} " +
                $"players={snapshot.players.Count} treasures={snapshot.treasures.Count}");
        }

        private void LogHeartbeat(
            string source,
            string connection,
            string room,
            float remainingTime,
            string timeText,
            string hpText,
            string treasureText,
            ClientMatchStateStore store,
            RoomManager roomManager)
        {
            _logTimer -= RefreshInterval;
            if (_logTimer > 0f)
                return;

            _logTimer = LogInterval;
            Debug.Log($"{LogPrefix} heartbeat source={source} connection=\"{connection}\" " +
                $"room={room} remaining={remainingTime:F1} timeText={timeText} " +
                $"hpText=\"{hpText}\" treasureText=\"{treasureText}\" " +
                $"debugTextBound={_debugText != null} timeTextBound={_timeText != null} " +
                $"hpTextBound={_hpText != null} treasureTextBound={_treasureText != null} " +
                $"hasClientStore={store != null} hasMatchSnapshot={(store != null && store.HasMatchSnapshot)} " +
                $"hasRoomManager={roomManager != null}");
        }
    }
}
