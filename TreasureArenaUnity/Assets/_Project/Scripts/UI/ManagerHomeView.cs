using System.Collections.Generic;
using TreasureArenaMR.Network;
using TreasureArenaMR.Server;
using TreasureArenaMR.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace TreasureArenaMR.UI
{
    /// <summary>
    /// Home scene manager UI backed by the local Netick server runtime.
    /// </summary>
    public sealed class ManagerHomeView : MonoBehaviour
    {
        [Header("Runtime")]
        [SerializeField] private ServerBootstrap serverBootstrap;
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private RoomManager roomManager;
        [SerializeField] private int minPlayersToStart = 1;

        [Header("Header")]
        [SerializeField] private Text roleText;
        [SerializeField] private Text connectionText;

        [Header("Room")]
        [SerializeField] private Text roomNameText;
        [SerializeField] private Text roomStateText;
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button[] roomSelectButtons;
        [SerializeField] private Text[] roomSelectLabels;

        [Header("Config")]
        [SerializeField] private Text hpText;
        [SerializeField] private Text damageText;
        [SerializeField] private Text respawnText;
        [SerializeField] private Text matchTimeText;

        [Header("Map")]
        [SerializeField] private Text mapNameText;
        [SerializeField] private Text mapStatusText;
        [SerializeField] private Button selectMapButton;

        [Header("Players")]
        [SerializeField] private Text redTeamListText;
        [SerializeField] private Text blueTeamListText;

        [Header("Controls")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button stopButton;

        [Header("Log")]
        [SerializeField] private Text logText;

        private int selectedPlayerIndex;
        private float nextRefreshTime;
        private const float RefreshInterval = 0.25f;
        private const int MaxLogLines = 50;

        private void Awake()
        {
            AutoBindMissingReferences();
            EnsureRuntimeReferences();
            WireButtons();
            AppendLog("Home manager ready.");
        }

        private void OnEnable()
        {
            EnsureRuntimeReferences();
            if (serverBootstrap != null)
                serverBootstrap.OnStatusChanged += Refresh;
        }

        private void OnDisable()
        {
            if (serverBootstrap != null)
                serverBootstrap.OnStatusChanged -= Refresh;
        }

        private void Update()
        {
            if (Time.time < nextRefreshTime)
                return;

            nextRefreshTime = Time.time + RefreshInterval;
            EnsureRuntimeReferences();
            Refresh();
        }

        public void CreateOrStartRoom()
        {
            EnsureRuntimeReferences();
            if (serverBootstrap == null)
            {
                AppendLog("Missing ServerBootstrap.");
                return;
            }

            serverBootstrap.CreateOrStartRoom();
            AppendLog("Create/start room requested.");
            Refresh();
        }

        public void SelectNextMap()
        {
            EnsureRuntimeReferences();
            if (serverBootstrap == null)
            {
                AppendLog("Missing ServerBootstrap.");
                return;
            }

            serverBootstrap.SelectNextMap();
            AppendLog("Selected map: " + serverBootstrap.SelectedMapId);
            Refresh();
        }

        public void StartMatch()
        {
            EnsureRuntimeReferences();
            if (serverBootstrap != null && serverBootstrap.StartMatch(minPlayersToStart))
                AppendLog("Match started.");
            else
                AppendLog("Start match rejected.");

            Refresh();
        }

        public void StopMatch()
        {
            EnsureRuntimeReferences();
            if (serverBootstrap == null)
                return;

            serverBootstrap.StopMatch();
            AppendLog("Match stopped.");
            Refresh();
        }

        public void SelectNextPlayer()
        {
            List<PlayerInfo> players = GetPlayers();
            if (players.Count == 0)
            {
                selectedPlayerIndex = 0;
                AppendLog("No connected players.");
                Refresh();
                return;
            }

            selectedPlayerIndex = (selectedPlayerIndex + 1) % players.Count;
            AppendLog("Selected player: " + players[selectedPlayerIndex].nickname);
            Refresh();
        }

        public void AssignSelectedPlayerRed()
        {
            AssignSelectedPlayer(TeamType.Red);
        }

        public void AssignSelectedPlayerBlue()
        {
            AssignSelectedPlayer(TeamType.Blue);
        }

        public void Refresh()
        {
            EnsureRuntimeReferences();

            NetworkMatchState matchState = FindObjectOfType<NetworkMatchState>();
            RoomConfig config = roomManager != null ? roomManager.CurrentRoomConfig : null;
            List<PlayerInfo> players = GetPlayers();
            ClampSelectedPlayer(players);

            SetText(roleText, "Role: Home Manager + Server");
            SetText(connectionText, BuildConnectionText(matchState));
            SetText(roomNameText, config != null ? "Room: " + config.room_name : "Room: Not created");
            SetText(roomStateText, roomManager != null ? "State: " + roomManager.CurrentRoomState : "State: No RoomManager");
            SetText(hpText, config != null ? "HP: " + config.player_max_hp : "HP: -");
            SetText(damageText, config?.weapon_config != null ? "Damage: " + config.weapon_config.damage : "Damage: -");
            SetText(respawnText, config != null ? "Respawn Countdown: " + config.respawn_countdown.ToString("F1") : "Respawn Countdown: -");
            SetText(matchTimeText, BuildMatchTimeText(config));
            SetText(mapNameText, BuildMapText(config));
            SetText(mapStatusText, BuildMapStatusText(matchState));
            SetText(redTeamListText, BuildTeamText("Red Team", players, TeamType.Red));
            SetText(blueTeamListText, BuildTeamText("Blue Team", players, TeamType.Blue));
            RefreshTeamButtons(players);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void AssignSelectedPlayer(TeamType team)
        {
            EnsureRuntimeReferences();
            List<PlayerInfo> players = GetPlayers();
            ClampSelectedPlayer(players);
            if (players.Count == 0)
            {
                AppendLog("No player selected.");
                Refresh();
                return;
            }

            PlayerInfo player = players[selectedPlayerIndex];
            if (serverBootstrap != null && serverBootstrap.SwitchTeam(player.player_id, team))
                AppendLog(player.nickname + " -> " + team);
            else
                AppendLog("Switch team failed for " + player.nickname);

            Refresh();
        }

        private void WireButtons()
        {
            Bind(createRoomButton, CreateOrStartRoom);
            Bind(selectMapButton, SelectNextMap);
            Bind(startButton, StartMatch);
            Bind(stopButton, StopMatch);

            if (roomSelectButtons == null)
                return;

            if (roomSelectButtons.Length > 0)
                Bind(roomSelectButtons[0], SelectNextPlayer);
            if (roomSelectButtons.Length > 1)
                Bind(roomSelectButtons[1], AssignSelectedPlayerRed);
            if (roomSelectButtons.Length > 2)
                Bind(roomSelectButtons[2], AssignSelectedPlayerBlue);
        }

        private void EnsureRuntimeReferences()
        {
            if (serverBootstrap == null)
                serverBootstrap = FindObjectOfType<ServerBootstrap>();
            if (serverBootstrap == null)
                serverBootstrap = new GameObject("ServerBootstrap").AddComponent<ServerBootstrap>();

            if (networkManager == null)
                networkManager = serverBootstrap.NetworkManager != null
                    ? serverBootstrap.NetworkManager
                    : NetworkManager.Instance;
            if (networkManager == null)
                networkManager = FindObjectOfType<NetworkManager>();

            if (roomManager == null)
                roomManager = serverBootstrap.RoomManager != null
                    ? serverBootstrap.RoomManager
                    : FindObjectOfType<RoomManager>();
        }

        private List<PlayerInfo> GetPlayers()
        {
            if (roomManager == null || roomManager.Players == null)
                return new List<PlayerInfo>();

            return roomManager.Players;
        }

        private void ClampSelectedPlayer(List<PlayerInfo> players)
        {
            if (players == null || players.Count == 0)
            {
                selectedPlayerIndex = 0;
                return;
            }

            if (selectedPlayerIndex < 0 || selectedPlayerIndex >= players.Count)
                selectedPlayerIndex = 0;
        }

        private string BuildConnectionText(NetworkMatchState matchState)
        {
            if (networkManager == null)
                return "Connection: Missing NetworkManager";

            string state = networkManager.IsServer ? "Server Running" : "Server Stopped";
            return "Connection: " + state
                + " / UDP " + networkManager.ServerPort
                + " / Players " + networkManager.ConnectedPlayerCount
                + " / MatchState " + (matchState != null ? "Ready" : "Missing");
        }

        private string BuildMatchTimeText(RoomConfig config)
        {
            if (config == null)
                return "Match Time: -";

            float remaining = roomManager != null ? roomManager.RemainingTime : config.match_time;
            return "Match Time: " + config.match_time.ToString("F0") + " / Remaining " + remaining.ToString("F1");
        }

        private string BuildMapText(RoomConfig config)
        {
            string selectedMap = serverBootstrap != null ? serverBootstrap.SelectedMapId : "";
            if (config != null && !string.IsNullOrEmpty(config.map_id))
                return "Map: " + config.map_id;

            return string.IsNullOrEmpty(selectedMap) ? "Map: -" : "Map: " + selectedMap;
        }

        private string BuildMapStatusText(NetworkMatchState matchState)
        {
            if (matchState == null || !matchState.MapConfigured)
                return "Map Sync: Waiting";

            return "Map Sync: index " + matchState.MapIndex + " / rev " + matchState.MapRevision;
        }

        private string BuildTeamText(string title, List<PlayerInfo> players, TeamType team)
        {
            string text = title;
            bool hasPlayer = false;
            for (int i = 0; i < players.Count; i++)
            {
                PlayerInfo player = players[i];
                if (player.team != team)
                    continue;

                hasPlayer = true;
                string selectedPrefix = i == selectedPlayerIndex ? "> " : "- ";
                text += "\n" + selectedPrefix + player.nickname + " [" + player.player_id + "] HP " + player.hp;
            }

            return hasPlayer ? text : text + "\n- Empty";
        }

        private void RefreshTeamButtons(List<PlayerInfo> players)
        {
            if (roomSelectButtons == null || roomSelectLabels == null)
                return;

            int slotCount = Mathf.Min(roomSelectButtons.Length, roomSelectLabels.Length);
            for (int i = 0; i < slotCount; i++)
            {
                if (roomSelectButtons[i] != null)
                    roomSelectButtons[i].gameObject.SetActive(true);
            }

            string selected = players.Count > 0 ? players[selectedPlayerIndex].nickname : "None";
            if (slotCount > 0 && roomSelectLabels[0] != null)
                roomSelectLabels[0].text = "Select: " + selected;
            if (slotCount > 1 && roomSelectLabels[1] != null)
                roomSelectLabels[1].text = "Set Red";
            if (slotCount > 2 && roomSelectLabels[2] != null)
                roomSelectLabels[2].text = "Set Blue";
        }

        private void AppendLog(string message)
        {
            if (logText == null)
                return;

            string[] lines = logText.text.Split('\n');
            if (lines.Length >= MaxLogLines)
            {
                var truncated = new string[MaxLogLines - 1];
                System.Array.Copy(lines, 0, truncated, 0, truncated.Length);
                logText.text = "[Home] " + message + "\n" + string.Join("\n", truncated);
            }
            else
            {
                logText.text = "[Home] " + message + "\n" + logText.text;
            }
        }

        private void AutoBindMissingReferences()
        {
            roleText = roleText != null ? roleText : FindText("RoleText");
            connectionText = connectionText != null ? connectionText : FindText("ConnectionText");
            roomNameText = roomNameText != null ? roomNameText : FindText("RoomNameText");
            roomStateText = roomStateText != null ? roomStateText : FindText("RoomStateText");
            createRoomButton = createRoomButton != null ? createRoomButton : FindButton("CreateRoomButton");
            hpText = hpText != null ? hpText : FindText("HpText");
            damageText = damageText != null ? damageText : FindText("DamageText");
            respawnText = respawnText != null ? respawnText : FindText("RespawnText");
            matchTimeText = matchTimeText != null ? matchTimeText : FindText("MatchTimeText");
            mapNameText = mapNameText != null ? mapNameText : FindText("MapNameText");
            mapStatusText = mapStatusText != null ? mapStatusText : FindText("MapStatusText");
            selectMapButton = selectMapButton != null ? selectMapButton : FindButton("SelectMapButton");
            redTeamListText = redTeamListText != null ? redTeamListText : FindText("RedTeamPlayersText");
            blueTeamListText = blueTeamListText != null ? blueTeamListText : FindText("BlueTeamPlayersText");
            startButton = startButton != null ? startButton : FindButton("StartButton");
            stopButton = stopButton != null ? stopButton : FindButton("StopButton");
            logText = logText != null ? logText : FindText("LogText");
        }

        private Text FindText(string objectName)
        {
            Transform child = transform.Find(objectName);
            if (child != null)
                return child.GetComponent<Text>();

            Text[] texts = GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].name == objectName)
                    return texts[i];
            }

            return null;
        }

        private Button FindButton(string objectName)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].name == objectName)
                    return buttons[i];
            }

            return null;
        }

        private static void SetText(Text target, string value)
        {
            if (target != null)
                target.text = value;
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }
    }
}
