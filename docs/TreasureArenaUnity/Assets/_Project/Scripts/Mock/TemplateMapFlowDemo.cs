using System.Collections.Generic;
using System.Text;
using TreasureArenaMR.Gameplay;
using TreasureArenaMR.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace TreasureArenaMR.Mock
{
    /// <summary>
    /// Scene-facing LocalTest driver for the primitive map template. It wires placeholder objects and UI to pure rule services.
    /// </summary>
    public sealed class TemplateMapFlowDemo : MonoBehaviour
    {
        [Header("Scene Objects")]
        public Transform redPlayerView;
        public Transform bluePlayerView;
        public Transform treasureView;
        public Transform redBaseView;
        public Transform blueBaseView;

        [Header("HUD")]
        public Text statusText;
        public Text scoreText;
        public Text redHpText;
        public Text blueHpText;
        public Text treasureText;
        public Text promptText;
        public Text logText;
        public GameObject resultPanel;
        public Text resultText;

        [Header("Controls")]
        public Button resetButton;
        public Button startButton;
        public Button redPickupButton;
        public Button blueAttackButton;
        public Button redBaseButton;
        public Button respawnButton;
        public Button bluePickupButton;
        public Button blueSubmitButton;
        public Button finishButton;
        public Button runFullButton;

        private readonly TeamTreasureRuleService rules = new TeamTreasureRuleService();
        private readonly List<PlayerRuntimeState> players = new List<PlayerRuntimeState>();
        private readonly List<TreasureRuntimeState> treasures = new List<TreasureRuntimeState>();
        private readonly StringBuilder logBuilder = new StringBuilder();

        private RoomConfig roomConfig;
        private RoomState roomState;
        private PlayerRuntimeState redPlayer;
        private PlayerRuntimeState bluePlayer;
        private TreasureRuntimeState treasure;
        private int redScore;
        private int blueScore;
        private float respawnRemaining;

        private void Awake()
        {
            BindButtons();
        }

        private void Start()
        {
            ResetScenario();
        }

        private void BindButtons()
        {
            BindButton(resetButton, ResetScenario);
            BindButton(startButton, StartMatch);
            BindButton(redPickupButton, RedPickupTreasure);
            BindButton(blueAttackButton, BlueAttackRed);
            BindButton(redBaseButton, RedEnterRespawnZone);
            BindButton(respawnButton, CompleteRespawnNow);
            BindButton(bluePickupButton, BluePickupDroppedTreasure);
            BindButton(blueSubmitButton, BlueSubmitTreasure);
            BindButton(finishButton, FinishMatch);
            BindButton(runFullButton, RunFullScenarioButton);
        }

        public void ResetScenario()
        {
            roomConfig = CreateRoomConfig();
            roomState = RoomState.Waiting;
            redScore = 0;
            blueScore = 0;
            respawnRemaining = 0f;

            redPlayer = CreatePlayer("red_player_001", TeamType.Red, new Vector3(-4f, 0f, 0f));
            bluePlayer = CreatePlayer("blue_player_001", TeamType.Blue, new Vector3(4f, 0f, 0f));
            treasure = new TreasureRuntimeState
            {
                treasure_id = "treasure_001",
                treasure_type = TreasureType.Normal,
                state = TreasureState.Spawned,
                score_value = roomConfig.treasure_scores.Normal,
                position = new Vector3(0f, 0.35f, 0f)
            };

            players.Clear();
            players.Add(redPlayer);
            players.Add(bluePlayer);
            treasures.Clear();
            treasures.Add(treasure);
            logBuilder.Length = 0;

            SetTransform(redPlayerView, redPlayer.position);
            SetTransform(bluePlayerView, bluePlayer.position);
            SetTransform(treasureView, treasure.position);
            if (resultPanel != null)
            {
                resultPanel.SetActive(false);
            }

            AddLog("Reset template scenario.");
            RefreshHud();
        }

        public void StartMatch()
        {
            TeamTreasureRuleService.TeamTreasureRuleResult result = rules.StartMatch(players, ref roomState);
            AddLog(result.ok ? "Match started." : "Start failed: " + result.failureReason);
            RefreshHud();
        }

        public void RedPickupTreasure()
        {
            MovePlayer(redPlayer, redPlayerView, treasure.position + Vector3.left * 0.6f);
            TreasureRuleService.TreasureRuleResult result = rules.TreasureRules.Pickup(redPlayer, treasure);
            if (result.ok)
            {
                SetTransform(treasureView, redPlayer.position + Vector3.up * 1.2f);
            }

            AddLog(result.ok ? "Red picked treasure_001." : "Red pickup failed: " + result.failureReason);
            RefreshHud();
        }

        public void BlueAttackRed()
        {
            MovePlayer(bluePlayer, bluePlayerView, redPlayer.position + Vector3.right * 1.5f);
            DamageRuleService.DamageRuleResult result = rules.ApplyDamageAndDropTreasure(
                bluePlayer,
                redPlayer,
                roomConfig.weapon_config,
                treasures);

            if (result.targetEnteredGhostRetreat)
            {
                SetTransform(treasureView, treasure.position);
            }

            AddLog(result.ok
                ? "Blue attacked Red: HP " + result.targetHpBefore + " -> " + result.targetHpAfter + "."
                : "Blue attack failed: " + result.failureReason);
            RefreshHud();
        }

        public void RedEnterRespawnZone()
        {
            MovePlayer(redPlayer, redPlayerView, redBaseView != null ? redBaseView.position : new Vector3(-4f, 0f, 0f));
            RespawnRuleService.RespawnRuleResult result = rules.RespawnRules.StartRespawn(redPlayer, true);
            if (result.ok)
            {
                respawnRemaining = roomConfig.respawn_countdown;
            }

            AddLog(result.ok ? "Red entered red_base and started respawn." : "Respawn start failed: " + result.failureReason);
            RefreshHud();
        }

        public void TickRespawn(float deltaTime)
        {
            if (redPlayer.state != PlayerState.Respawning)
            {
                AddLog("Tick ignored: Red is not respawning.");
                RefreshHud();
                return;
            }

            respawnRemaining -= deltaTime;
            if (respawnRemaining > 0f)
            {
                AddLog("Respawn ticking: " + respawnRemaining.ToString("0.0") + "s left.");
                RefreshHud();
                return;
            }

            RespawnRuleService.RespawnRuleResult result = rules.RespawnRules.CompleteRespawn(redPlayer, roomConfig);
            respawnRemaining = 0f;
            AddLog(result.ok ? "Red respawned with HP " + result.hpAfter + "." : "Respawn complete failed: " + result.failureReason);
            RefreshHud();
        }

        public void CompleteRespawnNow()
        {
            TickRespawn(roomConfig.respawn_countdown);
        }

        public void BluePickupDroppedTreasure()
        {
            MovePlayer(bluePlayer, bluePlayerView, treasure.position + Vector3.right * 0.6f);
            TreasureRuleService.TreasureRuleResult result = rules.TreasureRules.Pickup(bluePlayer, treasure);
            if (result.ok)
            {
                SetTransform(treasureView, bluePlayer.position + Vector3.up * 1.2f);
            }

            AddLog(result.ok ? "Blue picked dropped treasure." : "Blue pickup failed: " + result.failureReason);
            RefreshHud();
        }

        public void BlueSubmitTreasure()
        {
            MovePlayer(bluePlayer, bluePlayerView, blueBaseView != null ? blueBaseView.position : new Vector3(4f, 0f, 0f));
            TreasureRuleService.TreasureSubmitResult result = rules.SubmitTreasure(
                bluePlayer,
                treasure,
                true,
                ref redScore,
                ref blueScore);

            if (result.ok)
            {
                SetTransform(treasureView, bluePlayer.position + Vector3.up * 0.35f);
            }

            AddLog(result.ok ? "Blue submitted treasure for +" + result.scoreAdded + "." : "Blue submit failed: " + result.failureReason);
            RefreshHud();
        }

        public void FinishMatch()
        {
            TeamTreasureRuleService.TeamTreasureRuleResult result = rules.FinishMatch(ref roomState);
            MatchResult matchResult = rules.CreateMatchResult(
                "template_match_001",
                roomConfig,
                redScore,
                blueScore,
                roomConfig.match_time,
                CreateStats());

            AddLog(result.ok ? "Match finished. Winner: " + matchResult.winner_team + "." : "Finish failed: " + result.failureReason);
            if (resultPanel != null)
            {
                resultPanel.SetActive(true);
            }

            SetText(resultText, "Result\nRed " + redScore + " : " + blueScore + " Blue\nWinner: " + matchResult.winner_team);
            RefreshHud();
        }

        public string RunFullScenario()
        {
            ResetScenario();
            StartMatch();
            RedPickupTreasure();
            BlueAttackRed();
            RedEnterRespawnZone();
            CompleteRespawnNow();
            BluePickupDroppedTreasure();
            BlueSubmitTreasure();
            FinishMatch();
            return logBuilder.ToString();
        }

        private void RunFullScenarioButton()
        {
            RunFullScenario();
        }

        private static RoomConfig CreateRoomConfig()
        {
            return new RoomConfig
            {
                room_id = "template_room_001",
                room_name = "Template Room",
                game_mode = GameMode.TeamTreasure,
                map_id = "map_template_gameplay",
                max_players = 4,
                match_time = 300f,
                round_count = 1,
                player_max_hp = 100,
                respawn_countdown = 5f,
                weapon_config = new WeaponConfig
                {
                    weapon_id = "energy_gun",
                    damage = 100,
                    range = 15f,
                    cooldown = 0.5f
                },
                treasure_scores = new TreasureScores
                {
                    Normal = 10,
                    Rare = 30,
                    Final = 50
                },
                treasure_refresh_interval = 10f,
                supply_refresh_interval = 20f
            };
        }

        private static PlayerRuntimeState CreatePlayer(string playerId, TeamType team, Vector3 position)
        {
            return new PlayerRuntimeState
            {
                player_id = playerId,
                team = team,
                state = PlayerState.Alive,
                hp = 100,
                position = position
            };
        }

        private List<PlayerMatchStat> CreateStats()
        {
            return new List<PlayerMatchStat>
            {
                new PlayerMatchStat { player_id = redPlayer.player_id, team = TeamType.Red, deaths = 1 },
                new PlayerMatchStat
                {
                    player_id = bluePlayer.player_id,
                    team = TeamType.Blue,
                    kills = 1,
                    treasures_submitted = blueScore > 0 ? 1 : 0,
                    score_contribution = blueScore
                }
            };
        }

        private void RefreshHud()
        {
            SetText(statusText, "Room: " + roomState + "\nMap: " + roomConfig.map_id);
            SetText(scoreText, "Red " + redScore + " : " + blueScore + " Blue");
            SetText(redHpText, "Red HP: " + redPlayer.hp + " / " + roomConfig.player_max_hp + "\nState: " + redPlayer.state);
            SetText(blueHpText, "Blue HP: " + bluePlayer.hp + " / " + roomConfig.player_max_hp + "\nState: " + bluePlayer.state);
            SetText(treasureText, "Treasure: " + treasure.state + "\nCarrier: " + (string.IsNullOrEmpty(treasure.carrier_player_id) ? "None" : treasure.carrier_player_id));
            SetText(promptText, BuildPromptText());
            SetText(logText, logBuilder.ToString());
        }

        private string BuildPromptText()
        {
            if (redPlayer.state == PlayerState.GhostRetreat)
            {
                return "Red GhostRetreat: walk back to red_base.";
            }

            if (redPlayer.state == PlayerState.Respawning)
            {
                return "Respawning: " + respawnRemaining.ToString("0.0") + "s";
            }

            if (roomState == RoomState.Finished)
            {
                return "Finished. Check ResultPanel.";
            }

            return "Use buttons to run the full TeamTreasure loop.";
        }

        private void MovePlayer(PlayerRuntimeState player, Transform view, Vector3 position)
        {
            player.position = position;
            SetTransform(view, position);
        }

        private static void SetTransform(Transform target, Vector3 position)
        {
            if (target != null)
            {
                target.position = position;
            }
        }

        private void AddLog(string message)
        {
            logBuilder.AppendLine(message);
        }

        private static void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }
    }
}
