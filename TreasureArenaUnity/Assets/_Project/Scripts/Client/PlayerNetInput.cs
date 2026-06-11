using Netick;
using Netick.Unity;
using UnityEngine;
using NetickPlayer = Netick.NetworkPlayer;

namespace TreasureArenaMR.Client
{
    /// <summary>
    /// Drives the locally-owned network player from the Pico/XR tracking pose.
    /// Server-authoritative gameplay state still lives on the server.
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

        public override void NetworkFixedUpdate()
        {
            if (!IsInputSource)
                return;

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
