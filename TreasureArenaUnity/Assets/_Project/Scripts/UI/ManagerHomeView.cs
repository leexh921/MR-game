using System;
using System.Collections.Generic;
using TreasureArenaMR.Server;
using TreasureArenaMR.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace TreasureArenaMR.UI
{
    /// <summary>
    /// PC manager home for the combined Manager + Server MVP.
    /// Creates the real Netick server room and lets the manager assign Red/Blue teams.
    /// </summary>
    public sealed class ManagerHomeView : MonoBehaviour
    {
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
        [SerializeField] private int minPlayersToStart = 1;

        [Header("Runtime")]
        [SerializeField] private ServerBootstrap serverBootstrap;
        [SerializeField] private RoomManager roomManager;

        [Header("Log")]
        [SerializeField] private Text logText;

        private readonly List<GameObject> teamRows = new List<GameObject>();
        private RectTransform teamSwitchPanel;
        private float refreshTimer;

        private void Awake()
        {
            AutoBindMissingReferences();
            EnsureServerReferences();
            WireButtons();
            SubscribeRoomEvents(true);
            Refresh();
        }

        private void OnEnable()
        {
            SubscribeRoomEvents(true);
            Refresh();
        }

        private void OnDisable()
        {
            SubscribeRoomEvents(false);
        }

        private void Update()
        {
            refreshTimer -= Time.deltaTime;
            if (refreshTimer > 0f)
            {
                return;
            }

            refreshTimer = 0.5f;
            Refresh();
        }

        public void CreateRoomAndStartServer()
        {
            EnsureServerReferences();
            if (serverBootstrap == null)
            {
                AppendLog("Create room failed: ServerBootstrap is missing.");
                return;
            }

            serverBootstrap.CreateOrStartRoom();
            roomManager = serverBootstrap.RoomManager;
            SubscribeRoomEvents(true);
            Refresh();
            AppendLog("Server room created or refreshed.");
        }

        public void SelectNextMap()
        {
            EnsureServerReferences();
            if (serverBootstrap == null)
            {
                AppendLog("Select map failed: ServerBootstrap is missing.");
                return;
            }

            serverBootstrap.SelectNextMap();
            Refresh();
            AppendLog("Selected map: " + serverBootstrap.SelectedMapId + ".");
        }

        public void StartSelectedRoom()
        {
            EnsureServerReferences();
            if (serverBootstrap == null)
            {
                AppendLog("Start failed: ServerBootstrap is missing.");
                return;
            }

            bool started = serverBootstrap.StartMatch(minPlayersToStart);
            Refresh();
            AppendLog(started ? "Match started." : "Match did not start. Check server, map, players, and room state.");
        }

        public void StopSelectedRoom()
        {
            EnsureServerReferences();
            if (serverBootstrap == null)
            {
                AppendLog("Stop failed: ServerBootstrap is missing.");
                return;
            }

            serverBootstrap.StopMatch();
            Refresh();
            AppendLog("Match marked as finished.");
        }

        public void Refresh()
        {
            EnsureServerReferences();

            RoomConfig config = roomManager != null ? roomManager.CurrentRoomConfig : null;
            string selectedMap = serverBootstrap != null ? serverBootstrap.SelectedMapId : "None";
            bool serverRunning = serverBootstrap != null && serverBootstrap.IsServerRunning;

            SetText(roleText, "Role: Manager + Server");
            SetText(connectionText, "Server: " + (serverRunning ? "Running" : "Stopped"));
            SetText(roomNameText, config != null ? "Room: " + config.room_name : "Room: Not Created");
            SetText(roomStateText, roomManager != null ? "State: " + roomManager.CurrentRoomState : "State: Offline");
            SetText(hpText, config != null ? "HP: " + config.player_max_hp : "HP: -");
            SetText(damageText, config != null && config.weapon_config != null ? "Damage: " + config.weapon_config.damage : "Damage: -");
            SetText(respawnText, config != null ? "Respawn Countdown: " + config.respawn_countdown : "Respawn Countdown: -");
            SetText(matchTimeText, config != null ? "Match Time: " + config.match_time : "Match Time: -");
            SetText(mapNameText, config != null ? "Map: " + config.map_id : "Map: " + selectedMap);
            SetText(mapStatusText, roomManager != null && roomManager.MapData != null ? "MapData: Loaded" : "MapData: Not Loaded");
            SetText(redTeamListText, BuildTeamText("Red Team", TeamType.Red));
            SetText(blueTeamListText, BuildTeamText("Blue Team", TeamType.Blue));

            RefreshRoomButtons(config, serverRunning);
            RefreshTeamControls();
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

        private void WireButtons()
        {
            if (createRoomButton != null)
            {
                createRoomButton.onClick.RemoveAllListeners();
                createRoomButton.onClick.AddListener(CreateRoomAndStartServer);
            }

            if (selectMapButton != null)
            {
                selectMapButton.onClick.RemoveAllListeners();
                selectMapButton.onClick.AddListener(SelectNextMap);
            }

            if (startButton != null)
            {
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(StartSelectedRoom);
            }

            if (stopButton != null)
            {
                stopButton.onClick.RemoveAllListeners();
                stopButton.onClick.AddListener(StopSelectedRoom);
            }

            if (roomSelectButtons == null)
            {
                return;
            }

            for (int i = 0; i < roomSelectButtons.Length; i++)
            {
                Button button = roomSelectButtons[i];
                if (button == null)
                {
                    continue;
                }

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(Refresh);
            }
        }

        private void SubscribeRoomEvents(bool subscribe)
        {
            EnsureServerReferences(false);
            if (serverBootstrap != null)
            {
                serverBootstrap.OnStatusChanged -= Refresh;
                if (subscribe)
                {
                    serverBootstrap.OnStatusChanged += Refresh;
                }
            }

            if (roomManager == null)
            {
                return;
            }

            roomManager.OnRoomStateChanged -= HandleRoomStateChanged;
            roomManager.OnPlayerJoined -= HandlePlayerChanged;
            roomManager.OnPlayerLeft -= HandlePlayerChanged;
            roomManager.OnTeamChanged -= HandleTeamChanged;

            if (subscribe)
            {
                roomManager.OnRoomStateChanged += HandleRoomStateChanged;
                roomManager.OnPlayerJoined += HandlePlayerChanged;
                roomManager.OnPlayerLeft += HandlePlayerChanged;
                roomManager.OnTeamChanged += HandleTeamChanged;
            }
        }

        private void HandleRoomStateChanged(RoomState state)
        {
            Refresh();
        }

        private void HandlePlayerChanged(PlayerInfo player)
        {
            Refresh();
        }

        private void HandleTeamChanged(PlayerInfo player, TeamType team)
        {
            Refresh();
        }

        private void RefreshRoomButtons(RoomConfig config, bool serverRunning)
        {
            if (roomSelectButtons == null || roomSelectLabels == null)
            {
                return;
            }

            int slotCount = Mathf.Min(roomSelectButtons.Length, roomSelectLabels.Length);
            for (int i = 0; i < slotCount; i++)
            {
                bool isCurrentRoomSlot = i == 0 && config != null;
                if (roomSelectButtons[i] != null)
                {
                    roomSelectButtons[i].gameObject.SetActive(isCurrentRoomSlot);
                    roomSelectButtons[i].interactable = false;
                }

                if (roomSelectLabels[i] != null)
                {
                    roomSelectLabels[i].text = isCurrentRoomSlot
                        ? config.room_name + " / " + (serverRunning ? "Server Running" : "Server Stopped")
                        : "Empty";
                }
            }
        }

        private void RefreshTeamControls()
        {
            EnsureTeamSwitchPanel();

            for (int i = 0; i < teamRows.Count; i++)
            {
                if (teamRows[i] != null)
                {
                    Destroy(teamRows[i]);
                }
            }

            teamRows.Clear();

            IReadOnlyList<PlayerInfo> players = roomManager != null ? roomManager.Players : null;
            if (players == null || players.Count == 0)
            {
                teamRows.Add(CreateTeamLabelRow("No connected players."));
                return;
            }

            for (int i = 0; i < players.Count; i++)
            {
                PlayerInfo player = players[i];
                if (player == null)
                {
                    continue;
                }

                teamRows.Add(CreateTeamButtonRow(player, i));
            }
        }

        private GameObject CreateTeamLabelRow(string label)
        {
            GameObject row = CreateRowContainer("TeamInfoRow", 0);
            Text text = CreateText(row.transform, "TeamInfoText", label, 16, TextAnchor.MiddleLeft);
            RectTransform rect = text.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8f, 0f);
            rect.offsetMax = new Vector2(-8f, 0f);
            return row;
        }

        private GameObject CreateTeamButtonRow(PlayerInfo player, int index)
        {
            GameObject row = CreateRowContainer("TeamPlayerRow_" + player.player_id, index);
            CreateText(row.transform, "PlayerLabel", player.nickname + " [" + player.player_id + "]  " + player.team, 14, TextAnchor.MiddleLeft);
            CreateTeamButton(row.transform, "RedButton", "Red", new Vector2(350f, 0f), () => SwitchPlayerTeam(player.player_id, TeamType.Red));
            CreateTeamButton(row.transform, "BlueButton", "Blue", new Vector2(430f, 0f), () => SwitchPlayerTeam(player.player_id, TeamType.Blue));
            return row;
        }

        private GameObject CreateRowContainer(string name, int index)
        {
            GameObject row = new GameObject(name, typeof(RectTransform));
            row.transform.SetParent(teamSwitchPanel, false);

            RectTransform rect = row.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 30f);
            rect.anchoredPosition = new Vector2(0f, -index * 34f);
            return row;
        }

        private Text CreateText(Transform parent, string name, string value, int fontSize, TextAnchor alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = alignment;
            text.text = value;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(8f, 0f);
            rect.offsetMax = new Vector2(-180f, 0f);
            return text;
        }

        private void CreateTeamButton(Transform parent, string name, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            Image image = go.GetComponent<Image>();
            image.color = label == "Red" ? new Color(0.75f, 0.18f, 0.18f, 0.9f) : new Color(0.18f, 0.32f, 0.75f, 0.9f);

            Button button = go.GetComponent<Button>();
            button.onClick.AddListener(action);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(72f, 26f);
            rect.anchoredPosition = anchoredPosition;

            Text text = CreateText(go.transform, "Text", label, 13, TextAnchor.MiddleCenter);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        private void SwitchPlayerTeam(string playerId, TeamType team)
        {
            EnsureServerReferences();
            bool changed = serverBootstrap != null && serverBootstrap.SwitchTeam(playerId, team);
            Refresh();
            AppendLog(changed ? "Player " + playerId + " -> " + team + "." : "Switch team failed for " + playerId + ".");
        }

        private void EnsureTeamSwitchPanel()
        {
            if (teamSwitchPanel != null)
            {
                return;
            }

            Transform parent = FindChildRecursive(transform, "PlayerPanel") ?? transform;
            Transform existing = FindChildRecursive(parent, "TeamSwitchPanel");
            if (existing != null)
            {
                teamSwitchPanel = existing as RectTransform;
                return;
            }

            GameObject panel = new GameObject("TeamSwitchPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);

            Image image = panel.GetComponent<Image>();
            image.color = new Color(0.04f, 0.04f, 0.04f, 0.55f);

            teamSwitchPanel = panel.GetComponent<RectTransform>();
            teamSwitchPanel.anchorMin = new Vector2(0f, 0f);
            teamSwitchPanel.anchorMax = new Vector2(1f, 0f);
            teamSwitchPanel.pivot = new Vector2(0.5f, 0f);
            teamSwitchPanel.anchoredPosition = new Vector2(0f, 12f);
            teamSwitchPanel.sizeDelta = new Vector2(-24f, 120f);
        }

        private string BuildTeamText(string title, TeamType team)
        {
            string text = title;
            IReadOnlyList<PlayerInfo> players = roomManager != null ? roomManager.Players : null;
            if (players == null || players.Count == 0)
            {
                return text + "\n- Empty";
            }

            bool any = false;
            for (int i = 0; i < players.Count; i++)
            {
                PlayerInfo player = players[i];
                if (player == null || player.team != team)
                {
                    continue;
                }

                any = true;
                text += "\n- " + player.nickname + " (" + player.player_id + ")";
            }

            return any ? text : text + "\n- Empty";
        }

        private void AppendLog(string message)
        {
            if (logText == null)
            {
                return;
            }

            logText.text = "[Manager] " + message + "\n" + logText.text;
        }

        private void EnsureServerReferences(bool createIfMissing = true)
        {
            if (serverBootstrap == null)
            {
                serverBootstrap = FindObjectOfType<ServerBootstrap>();
            }

            if (serverBootstrap == null && createIfMissing)
            {
                GameObject go = new GameObject("ManagerServerBootstrap");
                serverBootstrap = go.AddComponent<ServerBootstrap>();
            }

            if (roomManager == null)
            {
                roomManager = serverBootstrap != null && serverBootstrap.RoomManager != null
                    ? serverBootstrap.RoomManager
                    : FindObjectOfType<RoomManager>();
            }
        }

        private static void SetText(Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
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
            Transform child = FindChildRecursive(transform, objectName);
            return child != null ? child.GetComponent<Text>() : null;
        }

        private Button FindButton(string objectName)
        {
            Transform child = FindChildRecursive(transform, objectName);
            return child != null ? child.GetComponent<Button>() : null;
        }

        private static Transform FindChildRecursive(Transform parent, string objectName)
        {
            if (parent == null)
            {
                return null;
            }

            if (parent.name == objectName)
            {
                return parent;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindChildRecursive(parent.GetChild(i), objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
