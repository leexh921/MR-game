using TreasureArenaMR.Client;
using TreasureArenaMR.Server;
using TreasureArenaMR.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace TreasureArenaMR.UI
{
    /// <summary>
    /// Pico HUD that displays player HP, team scores, match timer, treasure status,
    /// and GhostRetreat / Respawn prompts on a screen-space overlay Canvas.
    /// Reads from the local PlayerNetInput (Netick [Networked] properties) and
    /// the server RoomManager for match-level state.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        [Header("Core Texts")]
        [SerializeField] private Text hpText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text timeText;
        [SerializeField] private Text treasureStatusText;

        [Header("Panels")]
        [SerializeField] private GameObject ghostRetreatPanel;
        [SerializeField] private GameObject respawnPanel;
        [SerializeField] private GameObject resultPanel;

        [Header("Result")]
        [SerializeField] private Text resultSummaryText;

        [Header("Respawn Countdown")]
        [SerializeField] private Text respawnCountdownText;

        [Header("Settings")]
        [SerializeField] private float refreshInterval = 0.1f;

        private PlayerNetInput _localPlayer;
        private RoomManager _roomManager;
        private float _refreshTimer;

        private void Start()
        {
            HideAllPanels();
        }

        private void Update()
        {
            _refreshTimer -= Time.deltaTime;
            if (_refreshTimer > 0f) return;
            _refreshTimer = refreshInterval;

            if (_localPlayer == null) FindLocalPlayer();
            if (_roomManager == null) _roomManager = FindObjectOfType<RoomManager>();

            Refresh();
        }

        /// <summary>
        /// Manually bind a specific PlayerNetInput instead of auto-discovery.
        /// </summary>
        public void SetLocalPlayer(PlayerNetInput player)
        {
            _localPlayer = player;
            _refreshTimer = 0f;
        }

        private void FindLocalPlayer()
        {
            PlayerNetInput[] all = FindObjectsOfType<PlayerNetInput>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].Object != null && all[i].Object.IsInputSource)
                {
                    _localPlayer = all[i];
                    return;
                }
            }
        }

        private void Refresh()
        {
            if (_localPlayer == null)
            {
                SetText(hpText, "HP: --");
                SetText(scoreText, "-- : --");
                SetText(timeText, "--:--");
                SetText(treasureStatusText, "");
                return;
            }

            // HP ────────────────────────────────────────────────
            SetText(hpText, $"HP: {_localPlayer.Hp}");

            // Treasure ───────────────────────────────────────────
            string treasure = string.IsNullOrEmpty(_localPlayer.CarriedTreasureId)
                ? ""
                : $"Carrying: {_localPlayer.CarriedTreasureId}";
            SetText(treasureStatusText, treasure);

            // State panels ───────────────────────────────────────
            bool ghost = _localPlayer.State == PlayerState.GhostRetreat;
            bool respawning = _localPlayer.State == PlayerState.Respawning;

            if (ghostRetreatPanel != null)
                ghostRetreatPanel.SetActive(ghost);
            if (respawnPanel != null)
                respawnPanel.SetActive(respawning);

            // Match-level state from RoomManager ──────────────────
            if (_roomManager == null) return;

            // Score
            SetText(scoreText, $"Red {_roomManager.RedScore} : {_roomManager.BlueScore} Blue");

            // Timer
            float t = Mathf.Max(0f, _roomManager.RemainingTime);
            int mins = Mathf.FloorToInt(t / 60f);
            int secs = Mathf.FloorToInt(t % 60f);
            SetText(timeText, $"{mins:D2}:{secs:D2}");

            // Match finished
            if (_roomManager.CurrentRoomState == RoomState.Finished)
            {
                if (resultPanel != null) resultPanel.SetActive(true);

                if (resultSummaryText != null)
                {
                    string winner = _roomManager.RedScore > _roomManager.BlueScore ? "Red"
                        : _roomManager.BlueScore > _roomManager.RedScore ? "Blue"
                        : "Draw";
                    resultSummaryText.text =
                        $"Match Over\nRed {_roomManager.RedScore} : {_roomManager.BlueScore} Blue\nWinner: {winner}";
                }
            }
        }

        private void HideAllPanels()
        {
            if (ghostRetreatPanel != null) ghostRetreatPanel.SetActive(false);
            if (respawnPanel != null) respawnPanel.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(false);
        }

        private static void SetText(Text text, string value)
        {
            if (text != null) text.text = value;
        }
    }
}
