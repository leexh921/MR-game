using TreasureArenaMR.Client;
using TreasureArenaMR.Network;
using TreasureArenaMR.Shared;
using UnityEngine;
using UnityEngine.UI;
using GameNetworkPlayer = TreasureArenaMR.Network.NetworkPlayer;

namespace TreasureArenaMR.UI
{
    /// <summary>
    /// Pico HUD. Reads replicated player and match state only; no client authority.
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

        private PlayerNetInput localInput;
        private GameNetworkPlayer localPlayer;
        private NetworkMatchState matchState;
        private float refreshTimer;

        private void Start()
        {
            HideAllPanels();
        }

        private void Update()
        {
            refreshTimer -= Time.deltaTime;
            if (refreshTimer > 0f) return;
            refreshTimer = refreshInterval;

            if (localPlayer == null) FindLocalPlayer();
            if (matchState == null) matchState = FindObjectOfType<NetworkMatchState>();

            Refresh();
        }

        public void SetLocalPlayer(PlayerNetInput player)
        {
            localInput = player;
            localPlayer = player != null ? player.GetComponent<GameNetworkPlayer>() : null;
            refreshTimer = 0f;
        }

        private void FindLocalPlayer()
        {
            PlayerNetInput[] all = FindObjectsOfType<PlayerNetInput>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].Object != null && all[i].Object.IsInputSource)
                {
                    localInput = all[i];
                    localPlayer = all[i].GetComponent<GameNetworkPlayer>();
                    return;
                }
            }
        }

        private void Refresh()
        {
            RefreshPlayer();
            RefreshMatch();
        }

        private void RefreshPlayer()
        {
            if (localPlayer == null)
            {
                SetText(hpText, "HP: --");
                SetText(treasureStatusText, "");
                SetPanel(ghostRetreatPanel, false);
                SetPanel(respawnPanel, false);
                SetText(respawnCountdownText, "");
                return;
            }

            SetText(hpText, "HP: " + localPlayer.Hp + " / " + localPlayer.MaxHp);
            SetText(treasureStatusText, localPlayer.HasTreasure
                ? "Carrying: " + localPlayer.CarriedTreasureType
                : "");

            bool ghost = localPlayer.State == PlayerState.GhostRetreat;
            bool respawning = localPlayer.State == PlayerState.Respawning;
            SetPanel(ghostRetreatPanel, ghost);
            SetPanel(respawnPanel, respawning);

            if (respawning)
                SetText(respawnCountdownText, Mathf.CeilToInt(localPlayer.RespawnRemaining).ToString());
            else
                SetText(respawnCountdownText, "");
        }

        private void RefreshMatch()
        {
            if (matchState == null)
            {
                SetText(scoreText, "-- : --");
                SetText(timeText, "--:--");
                SetPanel(resultPanel, false);
                return;
            }

            SetText(scoreText, "Red " + matchState.RedScore + " : " + matchState.BlueScore + " Blue");

            float t = Mathf.Max(0f, matchState.RemainingTime);
            int mins = Mathf.FloorToInt(t / 60f);
            int secs = Mathf.FloorToInt(t % 60f);
            SetText(timeText, mins.ToString("D2") + ":" + secs.ToString("D2"));

            bool finished = matchState.RoomState == RoomState.Finished;
            SetPanel(resultPanel, finished);
            if (finished && resultSummaryText != null)
            {
                string winner = matchState.RedScore > matchState.BlueScore ? "Red"
                    : matchState.BlueScore > matchState.RedScore ? "Blue"
                    : "Draw";
                resultSummaryText.text = "Match Over\nRed " + matchState.RedScore
                    + " : " + matchState.BlueScore + " Blue\nWinner: " + winner;
            }
        }

        private void HideAllPanels()
        {
            SetPanel(ghostRetreatPanel, false);
            SetPanel(respawnPanel, false);
            SetPanel(resultPanel, false);
        }

        private static void SetText(Text text, string value)
        {
            if (text != null) text.text = value;
        }

        private static void SetPanel(GameObject panel, bool visible)
        {
            if (panel != null && panel.activeSelf != visible)
                panel.SetActive(visible);
        }
    }
}
