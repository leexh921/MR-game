using System.Collections.Generic;
using System.Text;
using TreasureArenaMR.Gameplay;
using TreasureArenaMR.Map;
using TreasureArenaMR.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace TreasureArenaMR.Mock
{
    /// <summary>
    /// Local no-network gameplay driver for validating map loading, registry-based map building, gameplay rules, and HUD refresh.
    /// </summary>
    public sealed class LocalTestGameplayDriver : MonoBehaviour
    {
        public string map_id = "map_template_gameplay";
        public PrefabRegistry prefabRegistry;

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
        public Button runFullButton;

        private readonly TeamTreasureRuleService rules = new TeamTreasureRuleService();
        private readonly List<PlayerRuntimeState> players = new List<PlayerRuntimeState>();
        private readonly List<TreasureRuntimeState> treasures = new List<TreasureRuntimeState>();
        private readonly StringBuilder logBuilder = new StringBuilder();

        private MapJsonModels.MapJson map;
        private GameObject mapRoot;
        private GameObject actorRoot;
        private Transform redPlayerView;
        private Transform bluePlayerView;
        private Transform treasureView;
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
            BindButton(resetButton, ResetScenario);
            BindButton(runFullButton, RunFullScenarioButton);
        }

        private void Start()
        {
            ResetScenario();
        }

        public void ResetScenario()
        {
            ClearRuntimeObjects();
            logBuilder.Length = 0;
            redScore = 0;
            blueScore = 0;
            respawnRemaining = 0f;
            roomState = RoomState.Waiting;
            roomConfig = CreateRoomConfig(map_id);

            MapLoadResult loadResult = new MapLoader().LoadFromMapId(map_id);
            if (!loadResult.ok)
            {
                AddLog("Map load failed: " + loadResult.error);
                RefreshHud();
                return;
            }

            map = loadResult.map;
            MapInstantiationResult instantiationResult = new MapLoader().InstantiateObjects(map, prefabRegistry, null);
            if (instantiationResult == null || !instantiationResult.ok)
            {
                AddLog("Map instantiate failed: " + (instantiationResult != null ? instantiationResult.error : "null_result"));
                RefreshHud();
                return;
            }

            mapRoot = instantiationResult.root;
            actorRoot = new GameObject("LocalTestRuntimeActors");
            CreateRuntimeStatesFromMap();
            CreateActorViews();

            if (resultPanel != null)
            {
                resultPanel.SetActive(false);
            }

            AddLog("Reset LocalTest with map: " + map.map_id);
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

            AddLog(result.ok ? "Red picked " + treasure.treasure_id + "." : "Red pickup failed: " + result.failureReason);
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

        public void RedEnterBase()
        {
            MovePlayer(redPlayer, redPlayerView, FindBasePosition("Red"));
            RespawnRuleService.RespawnRuleResult result = rules.RespawnRules.StartRespawn(redPlayer, true);
            if (result.ok)
            {
                respawnRemaining = roomConfig.respawn_countdown;
            }

            AddLog(result.ok ? "Red entered red base and started respawn." : "Respawn start failed: " + result.failureReason);
            RefreshHud();
        }

        public void CompleteRespawn()
        {
            if (redPlayer.state != PlayerState.Respawning)
            {
                AddLog("Respawn ignored: Red is not respawning.");
                RefreshHud();
                return;
            }

            RespawnRuleService.RespawnRuleResult result = rules.RespawnRules.CompleteRespawn(redPlayer, roomConfig);
            respawnRemaining = 0f;
            AddLog(result.ok ? "Red respawned with HP " + result.hpAfter + "." : "Respawn failed: " + result.failureReason);
            RefreshHud();
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
            MovePlayer(bluePlayer, bluePlayerView, FindBasePosition("Blue"));
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
                "localtest_match_001",
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
            RedEnterBase();
            CompleteRespawn();
            BluePickupDroppedTreasure();
            BlueSubmitTreasure();
            FinishMatch();
            return logBuilder.ToString();
        }

        private void RunFullScenarioButton()
        {
            RunFullScenario();
        }

        private void CreateRuntimeStatesFromMap()
        {
            Vector3 redBase = FindBasePosition("Red");
            Vector3 blueBase = FindBasePosition("Blue");
            MapJsonModels.TreasureSpawnPointJson point = FindTreasurePoint("Normal");
            TreasureType treasureType = ParseTreasureType(point.treasure_type);

            redPlayer = CreatePlayer("red_local_001", TeamType.Red, redBase);
            bluePlayer = CreatePlayer("blue_local_001", TeamType.Blue, blueBase);
            treasure = new TreasureRuntimeState
            {
                treasure_id = "treasure_local_001",
                treasure_type = treasureType,
                state = TreasureState.Spawned,
                score_value = GetTreasureScore(treasureType),
                position = ToVector3(point.position),
                carrier_player_id = null
            };

            players.Clear();
            players.Add(redPlayer);
            players.Add(bluePlayer);
            treasures.Clear();
            treasures.Add(treasure);
        }

        private void CreateActorViews()
        {
            redPlayerView = CreateActor("red_player_runtime", PrimitiveType.Capsule, redPlayer.position + Vector3.up, new Color(0.9f, 0.15f, 0.12f, 1f)).transform;
            bluePlayerView = CreateActor("blue_player_runtime", PrimitiveType.Capsule, bluePlayer.position + Vector3.up, new Color(0.1f, 0.3f, 0.95f, 1f)).transform;
            treasureView = CreateActor("treasure_runtime", PrimitiveType.Sphere, treasure.position, new Color(1f, 0.72f, 0.08f, 1f)).transform;
        }

        private GameObject CreateActor(string name, PrimitiveType type, Vector3 position, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(actorRoot.transform, false);
            go.transform.position = position;
            SetColor(go, color);
            return go;
        }

        private Vector3 FindBasePosition(string team)
        {
            for (int i = 0; i < map.team_bases.Count; i++)
            {
                MapJsonModels.TeamBaseJson teamBase = map.team_bases[i];
                if (teamBase.team == team)
                {
                    return ToVector3(teamBase.position);
                }
            }

            return team == "Red" ? new Vector3(-4f, 0f, 0f) : new Vector3(4f, 0f, 0f);
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

        private RoomConfig CreateRoomConfig(string mapId)
        {
            return new RoomConfig
            {
                room_id = "localtest_room_001",
                room_name = "LocalTest Room",
                game_mode = GameMode.TeamTreasure,
                map_id = mapId,
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

        private int GetTreasureScore(TreasureType type)
        {
            if (type == TreasureType.Rare)
            {
                return roomConfig.treasure_scores.Rare;
            }

            if (type == TreasureType.Final)
            {
                return roomConfig.treasure_scores.Final;
            }

            return roomConfig.treasure_scores.Normal;
        }

        private static TreasureType ParseTreasureType(string value)
        {
            if (value == "Rare")
            {
                return TreasureType.Rare;
            }

            if (value == "Final")
            {
                return TreasureType.Final;
            }

            return TreasureType.Normal;
        }

        private void RefreshHud()
        {
            SetText(statusText, roomConfig == null ? "Room: not ready" : "Room: " + roomState + "\nMap: " + roomConfig.map_id);
            SetText(scoreText, "Red " + redScore + " : " + blueScore + " Blue");
            SetText(redHpText, redPlayer == null ? "Red HP: -" : "Red HP: " + redPlayer.hp + " / 100\nState: " + redPlayer.state);
            SetText(blueHpText, bluePlayer == null ? "Blue HP: -" : "Blue HP: " + bluePlayer.hp + " / 100\nState: " + bluePlayer.state);
            SetText(treasureText, treasure == null ? "Treasure: -" : "Treasure: " + treasure.state + "\nCarrier: " + (string.IsNullOrEmpty(treasure.carrier_player_id) ? "None" : treasure.carrier_player_id));
            SetText(promptText, BuildPromptText());
            SetText(logText, logBuilder.ToString());
        }

        private string BuildPromptText()
        {
            if (redPlayer != null && redPlayer.state == PlayerState.GhostRetreat)
            {
                return "Red GhostRetreat: walk back to red base.";
            }

            if (redPlayer != null && redPlayer.state == PlayerState.Respawning)
            {
                return "Respawning: " + respawnRemaining.ToString("0.0") + "s";
            }

            if (roomState == RoomState.Finished)
            {
                return "Finished. Check result.";
            }

            return "LocalTest no-network TeamTreasure flow.";
        }

        private void MovePlayer(PlayerRuntimeState player, Transform view, Vector3 position)
        {
            player.position = position;
            SetTransform(view, position + Vector3.up);
        }

        private void ClearRuntimeObjects()
        {
            DestroyObject(mapRoot);
            DestroyObject(actorRoot);
            mapRoot = null;
            actorRoot = null;
        }

        private static void DestroyObject(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(go);
            }
            else
            {
                DestroyImmediate(go);
            }
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

        private static void SetColor(GameObject go, Color color)
        {
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            Material material = new Material(Shader.Find("Standard"));
            material.color = color;
            renderer.sharedMaterial = material;
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

        private static Vector3 ToVector3(MapJsonModels.Vector3Json value)
        {
            if (value == null)
            {
                return Vector3.zero;
            }

            return new Vector3(value.x, value.y, value.z);
        }

        private MapJsonModels.TreasureSpawnPointJson FindTreasurePoint(string treasureType)
        {
            for (int i = 0; i < map.treasure_spawn_points.Count; i++)
            {
                MapJsonModels.TreasureSpawnPointJson point = map.treasure_spawn_points[i];
                if (point.treasure_type == treasureType)
                {
                    return point;
                }
            }

            return map.treasure_spawn_points[0];
        }
    }
}
