using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TreasureArenaMR.UI
{
    /// <summary>
    /// Local-only manager home UI prototype for Stage 1.5.
    /// It demonstrates room selection and panel refresh without touching networking,
    /// database, map loading, or authoritative gameplay logic.
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

        [Header("Log")]
        [SerializeField] private Text logText;

        private readonly List<MockRoom> rooms = new List<MockRoom>();
        private int selectedRoomIndex;
        private int createdRoomCount = 3;

        private void Awake()
        {
            AutoBindMissingReferences();
            SeedRooms();
            WireButtons();
            SelectRoom(0);
        }

        public void SelectRoom(int roomIndex)
        {
            if (roomIndex < 0 || roomIndex >= rooms.Count)
            {
                return;
            }

            selectedRoomIndex = roomIndex;
            Refresh();
            AppendLog("Selected " + rooms[selectedRoomIndex].Name + ".");
        }

        public void CreateMockRoom()
        {
            createdRoomCount++;
            rooms.Add(new MockRoom(
                "Room " + createdRoomCount,
                "test_map_01",
                "Waiting",
                100,
                25,
                5,
                300,
                new[] { "RedTestPlayer" },
                new[] { "BlueTestPlayer" }));

            selectedRoomIndex = rooms.Count - 1;
            Refresh();
            AppendLog("Created local mock room. No server room was created.");
        }

        public void CycleSelectedRoomMap()
        {
            MockRoom room = GetSelectedRoom();
            if (room == null)
            {
                return;
            }

            room.MapId = room.MapId == "test_map_01" ? "test_map_02_placeholder" : "test_map_01";
            Refresh();
            AppendLog("Changed displayed map for " + room.Name + " to " + room.MapId + ".");
        }

        public void StartSelectedRoom()
        {
            MockRoom room = GetSelectedRoom();
            if (room == null)
            {
                return;
            }

            room.State = "Playing";
            Refresh();
            AppendLog("Start clicked for " + room.Name + ". UI state only.");
        }

        public void StopSelectedRoom()
        {
            MockRoom room = GetSelectedRoom();
            if (room == null)
            {
                return;
            }

            room.State = "Finished";
            Refresh();
            AppendLog("Stop clicked for " + room.Name + ". UI state only.");
        }

        public void Refresh()
        {
            MockRoom room = GetSelectedRoom();
            if (room == null)
            {
                return;
            }

            SetText(roleText, "Role: Manager");
            SetText(connectionText, "Connection: UI Mock");
            SetText(roomNameText, "Room: " + room.Name);
            SetText(roomStateText, "State: " + room.State);
            SetText(hpText, "HP: " + room.Hp);
            SetText(damageText, "Damage: " + room.Damage);
            SetText(respawnText, "Respawn Countdown: " + room.RespawnCountdown);
            SetText(matchTimeText, "Match Time: " + room.MatchTime);
            SetText(mapNameText, "Map: " + room.MapId);
            SetText(mapStatusText, "Preview: placeholder only");
            SetText(redTeamListText, BuildTeamText("Red Team", room.RedPlayers));
            SetText(blueTeamListText, BuildTeamText("Blue Team", room.BluePlayers));
            RefreshRoomButtons();
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

        private void SeedRooms()
        {
            if (rooms.Count > 0)
            {
                return;
            }

            rooms.Add(new MockRoom(
                "Room A",
                "test_map_01",
                "Waiting",
                100,
                25,
                5,
                300,
                new[] { "PicoClient_01" },
                new[] { "EditorClient_01" }));

            rooms.Add(new MockRoom(
                "Room B",
                "test_map_01",
                "Playing",
                120,
                20,
                6,
                240,
                new[] { "Red_01", "Red_02" },
                new[] { "Blue_01", "Blue_02" }));

            rooms.Add(new MockRoom(
                "Room C",
                "test_map_01",
                "Waiting",
                100,
                30,
                5,
                180,
                new[] { "Waiting_Red" },
                new string[0]));
        }

        private void WireButtons()
        {
            if (createRoomButton != null)
            {
                createRoomButton.onClick.RemoveListener(CreateMockRoom);
                createRoomButton.onClick.AddListener(CreateMockRoom);
            }

            if (selectMapButton != null)
            {
                selectMapButton.onClick.RemoveListener(CycleSelectedRoomMap);
                selectMapButton.onClick.AddListener(CycleSelectedRoomMap);
            }

            if (startButton != null)
            {
                startButton.onClick.RemoveListener(StartSelectedRoom);
                startButton.onClick.AddListener(StartSelectedRoom);
            }

            if (stopButton != null)
            {
                stopButton.onClick.RemoveListener(StopSelectedRoom);
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

                int capturedIndex = i;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => SelectRoom(capturedIndex));
            }
        }

        private void RefreshRoomButtons()
        {
            if (roomSelectButtons == null || roomSelectLabels == null)
            {
                return;
            }

            int slotCount = Mathf.Min(roomSelectButtons.Length, roomSelectLabels.Length);
            for (int i = 0; i < slotCount; i++)
            {
                bool hasRoom = i < rooms.Count;
                if (roomSelectButtons[i] != null)
                {
                    roomSelectButtons[i].gameObject.SetActive(hasRoom);
                    roomSelectButtons[i].interactable = hasRoom && i != selectedRoomIndex;
                }

                if (roomSelectLabels[i] != null)
                {
                    roomSelectLabels[i].text = hasRoom
                        ? rooms[i].Name + " / " + rooms[i].State
                        : "Empty";
                }
            }
        }

        private MockRoom GetSelectedRoom()
        {
            if (selectedRoomIndex < 0 || selectedRoomIndex >= rooms.Count)
            {
                return null;
            }

            return rooms[selectedRoomIndex];
        }

        private void AppendLog(string message)
        {
            if (logText == null)
            {
                return;
            }

            logText.text = "[UI Mock] " + message + "\n" + logText.text;
        }

        private static string BuildTeamText(string title, IReadOnlyList<string> players)
        {
            string text = title;
            if (players == null || players.Count == 0)
            {
                return text + "\n- Empty";
            }

            for (int i = 0; i < players.Count; i++)
            {
                text += "\n- " + players[i];
            }

            return text;
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
            Transform child = transform.Find(objectName);
            if (child != null)
            {
                return child.GetComponent<Text>();
            }

            Text[] texts = GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].name == objectName)
                {
                    return texts[i];
                }
            }

            return null;
        }

        private Button FindButton(string objectName)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].name == objectName)
                {
                    return buttons[i];
                }
            }

            return null;
        }

        [Serializable]
        private sealed class MockRoom
        {
            public readonly string Name;
            public string MapId;
            public string State;
            public readonly int Hp;
            public readonly int Damage;
            public readonly int RespawnCountdown;
            public readonly int MatchTime;
            public readonly string[] RedPlayers;
            public readonly string[] BluePlayers;

            public MockRoom(
                string name,
                string mapId,
                string state,
                int hp,
                int damage,
                int respawnCountdown,
                int matchTime,
                string[] redPlayers,
                string[] bluePlayers)
            {
                Name = name;
                MapId = mapId;
                State = state;
                Hp = hp;
                Damage = damage;
                RespawnCountdown = respawnCountdown;
                MatchTime = matchTime;
                RedPlayers = redPlayers;
                BluePlayers = bluePlayers;
            }
        }
    }
}
