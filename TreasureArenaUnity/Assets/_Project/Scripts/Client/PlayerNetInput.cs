using Netick;
using Netick.Unity;
using TreasureArenaMR.Network;
using TreasureArenaMR.Server;
using TreasureArenaMR.Shared;
using UnityEngine;
using UnityEngine.XR;
using GameNetworkPlayer = TreasureArenaMR.Network.NetworkPlayer;
using NetickPlayer = Netick.NetworkPlayer;

namespace TreasureArenaMR.Client
{
    public struct PlayerBattleInput : INetworkInput
    {
        public NetworkBool AttackPressed;
        public NetworkBool PickupPressed;
        public NetworkBool SubmitPressed;
    }

    /// <summary>
    /// Drives the locally-owned player from Pico/XR tracking pose and forwards gameplay input.
    /// Server-owned systems consume the input and write replicated state.
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

        [Header("Editor Input")]
        [SerializeField] private KeyCode editorAttackKey = KeyCode.F;
        [SerializeField] private KeyCode editorPickupKey = KeyCode.E;
        [SerializeField] private KeyCode editorSubmitKey = KeyCode.Q;

        private Rigidbody cachedRigidbody;
        private Transform resolvedTrackingTarget;
        private GameNetworkPlayer networkPlayer;

        private bool hasCalibration;
        private Vector3 calibrationTrackingPosition;
        private Vector3 calibrationPlayerPosition;
        private float calibrationPlayerY;

        private bool previousTrigger;
        private bool previousGrip;
        private bool previousPrimary;
        private float lastAttackProcessedTime;

        private ServerCombatAuthority combatAuthority;
        private ServerTreasureAuthority treasureAuthority;
        private RoomManager roomManager;

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

        public override void NetworkUpdate()
        {
            if (!IsInputSource || Sandbox == null || !Sandbox.InputEnabled)
                return;

            PlayerBattleInput input = default;
            FillPicoInput(ref input);
            FillEditorInput(ref input);
            Sandbox.SetInput(input);
        }

        public override void NetworkFixedUpdate()
        {
            if (IsInputSource)
                ApplyTracking();

            if (!IsServer)
                return;

            PlayerBattleInput input = default;
            FetchInput(out input);
            ProcessServerInput(input);
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

        private void ProcessServerInput(PlayerBattleInput input)
        {
            CacheServerRefs();
            if (roomManager == null || networkPlayer == null)
                return;
            if (networkPlayer.State != PlayerState.Alive)
                return;

            string playerId = networkPlayer.PlayerId;
            if (string.IsNullOrEmpty(playerId))
                return;

            if (input.AttackPressed && combatAuthority != null)
            {
                float cooldown = roomManager.CurrentRoomConfig?.weapon_config?.cooldown ?? 0.5f;
                if (Time.time - lastAttackProcessedTime >= cooldown)
                {
                    lastAttackProcessedTime = Time.time;
                    var weapon = roomManager.CurrentRoomConfig?.weapon_config;
                    int damage = weapon != null ? weapon.damage : 25;
                    float range = weapon != null ? weapon.range : 15f;

                    Vector3 origin = transform.position + Vector3.up * 1.2f + transform.forward * 0.25f;
                    combatAuthority.ProcessAttack(
                        playerId,
                        origin,
                        transform.forward,
                        damage,
                        range,
                        roomManager,
                        treasureAuthority);
                }
            }

            if (input.PickupPressed && treasureAuthority != null)
                treasureAuthority.TryPickupNearest(playerId, roomManager);

            if (input.SubmitPressed && treasureAuthority != null)
                treasureAuthority.TrySubmit(playerId, roomManager);
        }

        private void FillPicoInput(ref PlayerBattleInput input)
        {
            InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (!rightHand.isValid)
                return;

            bool trigger = false;
            bool grip = false;
            bool primary = false;
            rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out trigger);
            rightHand.TryGetFeatureValue(CommonUsages.gripButton, out grip);
            rightHand.TryGetFeatureValue(CommonUsages.primaryButton, out primary);

            if (trigger && !previousTrigger)
                input.AttackPressed = true;
            if (grip && !previousGrip)
                input.PickupPressed = true;
            if (primary && !previousPrimary)
                input.SubmitPressed = true;

            previousTrigger = trigger;
            previousGrip = grip;
            previousPrimary = primary;
        }

        private void FillEditorInput(ref PlayerBattleInput input)
        {
#if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(editorAttackKey))
                input.AttackPressed = true;
            if (Input.GetKeyDown(editorPickupKey))
                input.PickupPressed = true;
            if (Input.GetKeyDown(editorSubmitKey))
                input.SubmitPressed = true;
#endif
        }

        private void ApplyTracking()
        {
            Transform target = ResolveTrackingTarget();
            if (target == null)
                return;

            if (!hasCalibration)
                Calibrate(target);

            Vector3 trackingDelta = target.position - calibrationTrackingPosition;
            Vector3 desiredPosition = calibrationPlayerPosition + new Vector3(trackingDelta.x, 0f, trackingDelta.z);
            desiredPosition.y = preserveSpawnHeight ? calibrationPlayerY : target.position.y;

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

        private void CacheReferences()
        {
            if (cachedRigidbody == null)
                cachedRigidbody = GetComponent<Rigidbody>();
            if (networkPlayer == null)
                networkPlayer = GetComponent<GameNetworkPlayer>();
        }

        private void CacheServerRefs()
        {
            if (roomManager == null)
                roomManager = FindObjectOfType<RoomManager>();
            if (combatAuthority == null)
                combatAuthority = FindObjectOfType<ServerCombatAuthority>();
            if (treasureAuthority == null)
                treasureAuthority = FindObjectOfType<ServerTreasureAuthority>();
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
