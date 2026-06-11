using TreasureArenaMR.Network;
using TreasureArenaMR.Server;
using TreasureArenaMR.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace TreasureArenaMR.Manager
{
    /// <summary>
    /// Minimal Manager/Server-side match controls for the MVP Netick join flow.
    /// </summary>
    public sealed class MatchControlController : MonoBehaviour
    {
        [SerializeField] private RoomManager roomManager;
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private Button startMatchButton;
        [SerializeField] private Text statusText;
        [SerializeField] private int minPlayersToStart = 1;
        [SerializeField] private bool enableKeyboardShortcut = true;
        [SerializeField] private KeyCode startMatchKey = KeyCode.F5;

        private void Awake()
        {
            if (roomManager == null)
                roomManager = FindObjectOfType<RoomManager>();
            if (networkManager == null)
                networkManager = FindObjectOfType<NetworkManager>();

            if (startMatchButton != null)
            {
                startMatchButton.onClick.RemoveAllListeners();
                startMatchButton.onClick.AddListener(StartMatch);
            }
        }

        private void Update()
        {
            if (enableKeyboardShortcut && Input.GetKeyDown(startMatchKey))
                StartMatch();
        }

        public void StartMatch()
        {
            if (roomManager == null)
            {
                SetStatus("Missing RoomManager");
                Debug.LogError("[MatchControlController] Missing RoomManager.");
                return;
            }

            if (networkManager == null || !networkManager.IsServer)
            {
                SetStatus("Server is not running");
                Debug.LogWarning("[MatchControlController] Cannot start match before Netick server is running.");
                return;
            }

            if (roomManager.CurrentRoomState != RoomState.Waiting)
            {
                SetStatus("Room state is " + roomManager.CurrentRoomState);
                Debug.LogWarning("[MatchControlController] Match cannot start from state: " + roomManager.CurrentRoomState);
                return;
            }

            if (roomManager.MapData == null)
            {
                SetStatus("MapData not loaded");
                Debug.LogWarning("[MatchControlController] Cannot start match before map data is loaded.");
                return;
            }

            int playerCount = roomManager.Players != null ? roomManager.Players.Count : 0;
            if (playerCount < minPlayersToStart)
            {
                SetStatus("Need players: " + playerCount + "/" + minPlayersToStart);
                Debug.LogWarning("[MatchControlController] Not enough players to start: " + playerCount + "/" + minPlayersToStart);
                return;
            }

            roomManager.SetRoomState(RoomState.Playing);
            SetStatus("Match started");
            Debug.Log("[MatchControlController] Match started.");
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = "Match Control\n" + message;
        }
    }
}
