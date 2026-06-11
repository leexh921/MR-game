using Netick;
using Netick.Unity;
using TreasureArenaMR.Server;
using TreasureArenaMR.Shared;
using UnityEngine;
using UnityEngine.XR;
using NetickPlayer = Netick.NetworkPlayer;

namespace TreasureArenaMR.Client
{
    [Networked]
    public struct PlayerBattleInput : INetworkInput
    {
        public NetworkBool AttackPressed;
        public NetworkBool PickupPressed;
        public NetworkBool SubmitPressed;
    }

    /// <summary>
    /// Drives the locally-owned network player from the Pico/XR tracking pose.
    /// Handles position sync (Pico headset), yaw, and combat input.
    /// Server-authoritative gameplay state via [Networked] properties.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(NetworkRigidbody))]
    public sealed class PlayerNetInput : NetworkBehaviour
    {
        [Header("Tracking")]
        [SerializeField] private Transform trackingTarget;
        [SerializeField] private bool useMainCameraFallback = true;
        [SerializeField] private bool preserveSpawnHeight = true;
        [SerializeField] private bool syncYaw = true;

        private Rigidbody cachedRigidbody;
        private Transform resolvedTrackingTarget;
        private bool hasCalibration;
        private Vector3 calibrationTrackingPosition;
        private Vector3 calibrationPlayerPosition;
        private float calibrationPlayerY;

        // ---- Server-authoritative gameplay state (synced to all clients) ----

        [Networked] public int Hp { get; set; } = 100;
        [Networked] public PlayerState State { get; set; } = PlayerState.Alive;
        [Networked] public TeamType Team { get; set; } = TeamType.None;
        [Networked] public string CarriedTreasureId { get; set; } = "";

        [Header("Combat")]
        [SerializeField] private float _attackCooldown = 0.5f;

        private ServerCombatAuthority _combatAuth;
        private ServerTreasureAuthority _treasureAuth;
        private RoomManager _roomManager;
        private PlayerBattleInput _lastInput;

        private void Awake()
        {
            CacheReferences();
            ConfigureRigidbody();
        }

        public override void NetworkAwake()
        {
            CacheReferences();
            ConfigureRigidbody();
        }

        public override void OnInputSourceChanged(NetickPlayer previous)
        {
            hasCalibration = false;
        }

        // ---- Input reading (runs on input source) ----

        public override void NetworkUpdate()
        {
            if (!IsInputSource || !Sandbox.InputEnabled)
                return;

            var input = Sandbox.GetInput<PlayerBattleInput>();

            var rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (rightHand != null)
            {
                if (rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool trigger) && trigger)
                    input.AttackPressed = true;

                if (rightHand.TryGetFeatureValue(CommonUsages.gripButton, out bool grip) && grip)
                    input.PickupPressed = true;

                if (rightHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool primary) && primary)
                    input.SubmitPressed = true;
            }

            Sandbox.SetInput(input);
        }

        // ---- Tracking + combat processing (runs on input source + server) ----

        public override void NetworkFixedUpdate()
        {
            FetchInput(out _lastInput);

            if (IsInputSource)
            {
                ApplyTracking();
            }

            if (IsServer)
            {
                ProcessServerInput();
            }
        }

        // ---- Tracking (李潇涵 code, slightly adapted to avoid duplicate set) ----

        private void ApplyTracking()
        {
            Transform target = ResolveTrackingTarget();
            if (target == null)
                return;

            if (!hasCalibration)
                Calibrate(target);

            Vector3 trackingDelta = target.position - calibrationTrackingPosition;
            Vector3 desiredPosition = calibrationPlayerPosition + new Vector3(trackingDelta.x, 0f, trackingDelta.z);
            if (preserveSpawnHeight)
                desiredPosition.y = calibrationPlayerY;
            else
                desiredPosition.y = target.position.y;

            Quaternion desiredRotation = transform.rotation;
            if (syncYaw)
                desiredRotation = Quaternion.Euler(0f, target.eulerAngles.y, 0f);

            if (cachedRigidbody != null)
            {
                cachedRigidbody.MovePosition(desiredPosition);
                cachedRigidbody.MoveRotation(desiredRotation);
            }
            else
            {
                transform.SetPositionAndRotation(desiredPosition, desiredRotation);
            }
        }

        // ---- Server-authoritative combat processing ----

        private float _lastAttackProcessedTime;

        private void ProcessServerInput()
        {
            if (State != PlayerState.Alive)
                return;

            if (_lastInput.AttackPressed)
            {
                if (Time.time - _lastAttackProcessedTime >= _attackCooldown)
                {
                    _lastAttackProcessedTime = Time.time;
                    HandleAttack();
                }
            }

            if (_lastInput.PickupPressed)
            {
                HandlePickup();
            }

            if (_lastInput.SubmitPressed)
            {
                HandleSubmit();
            }
        }

        private void HandleAttack()
        {
            CacheServerRefs();
            if (_roomManager == null || _combatAuth == null) return;

            var cfg = _roomManager.CurrentRoomConfig;
            if (cfg == null) return;

            var weaponCfg = cfg.weapon_config;
            string attackerId = Object.InputSourcePlayerId.ToString();

            var result = _combatAuth.ProcessAttack(
                attackerId,
                weaponCfg.weapon_id,
                transform.position + transform.forward * 0.5f,
                transform.forward,
                weaponCfg.damage,
                weaponCfg.range
            );

            if (result.hit)
            {
                int newHp = _combatAuth.ApplyDamage(result.target_player_id, result.damage, _roomManager);
                var allPlayers = FindObjectsOfType<PlayerNetInput>();
                foreach (var p in allPlayers)
                {
                    if (p.Object.InputSourcePlayerId.ToString() == result.target_player_id)
                        p.Hp = newHp;
                }
            }
        }

        private void HandlePickup()
        {
            CacheServerRefs();
            if (_treasureAuth == null || _roomManager == null) return;
            // TODO: need nearest treasure position and id from map/gameplay layer
            // string playerId = Object.InputSourcePlayerId.ToString();
            // _treasureAuth.TryPickup(playerId, treasureId, treasurePosition, _roomManager);
        }

        private void HandleSubmit()
        {
            CacheServerRefs();
            if (_treasureAuth == null || _roomManager == null) return;
            string playerId = Object.InputSourcePlayerId.ToString();
            _treasureAuth.TrySubmit(playerId, _roomManager);
        }

        private void CacheServerRefs()
        {
            if (_roomManager == null)
                _roomManager = FindObjectOfType<RoomManager>();
            if (_combatAuth == null)
                _combatAuth = FindObjectOfType<ServerCombatAuthority>();
            if (_treasureAuth == null)
                _treasureAuth = FindObjectOfType<ServerTreasureAuthority>();
        }

        // ---- GhostRetreat trigger on HP zero ----

        [OnChanged(nameof(Hp), invokeDuringResimulation: true)]
        private void OnHpChanged(OnChangedData data)
        {
            if (IsServer && Hp <= 0 && State == PlayerState.Alive)
            {
                State = PlayerState.GhostRetreat;
                Debug.Log($"[PlayerNetInput] Player HP 0, entering GhostRetreat");
            }
        }

        public void SetTrackingTarget(Transform target)
        {
            trackingTarget = target;
            resolvedTrackingTarget = target;
            hasCalibration = false;
        }

        public void Recalibrate()
        {
            hasCalibration = false;
        }

        private void CacheReferences()
        {
            if (cachedRigidbody == null)
                cachedRigidbody = GetComponent<Rigidbody>();
        }

        private void ConfigureRigidbody()
        {
            if (cachedRigidbody == null)
                return;

            cachedRigidbody.isKinematic = true;
            cachedRigidbody.useGravity = false;
            cachedRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        }

        private Transform ResolveTrackingTarget()
        {
            if (trackingTarget != null)
                return trackingTarget;

            if (resolvedTrackingTarget != null)
                return resolvedTrackingTarget;

            if (!useMainCameraFallback)
                return null;

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
                return null;

            resolvedTrackingTarget = mainCamera.transform;
            return resolvedTrackingTarget;
        }

        private void Calibrate(Transform target)
        {
            calibrationTrackingPosition = target.position;
            calibrationPlayerPosition = transform.position;
            calibrationPlayerY = transform.position.y;
            hasCalibration = true;
        }
    }
}
