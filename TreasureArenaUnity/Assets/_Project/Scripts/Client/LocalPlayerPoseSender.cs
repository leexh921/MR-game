using TreasureArenaMR.Network;
using UnityEngine;

namespace TreasureArenaMR.Client
{
    public sealed class LocalPlayerPoseSender : MonoBehaviour
    {
        private const string LogPrefix = "[SimplePoseSender]";
        private const float LogInterval = 2f;

        [SerializeField] private SimpleNetworkClient _client;
        [SerializeField] private Transform _poseSource;
        [SerializeField] private SharedSpaceManager _sharedSpace;
        [SerializeField] private float _sendHz = 20f;

        private float _sendTimer;
        private float _logTimer;

        private void Awake()
        {
            CacheReferences();
        }

        private void Update()
        {
            CacheReferences();
            if (_client == null || !_client.IsConnected || _poseSource == null)
                return;

            if (_sharedSpace == null || !_sharedSpace.IsReady)
            {
                LogWaitingForSharedSpace();
                return;
            }

            float interval = _sendHz > 0f ? 1f / _sendHz : 0.05f;
            _sendTimer -= Time.deltaTime;
            if (_sendTimer > 0f)
                return;

            _sendTimer = interval;
            Vector3 sharedPosition = _sharedSpace.WorldToSharedPosition(_poseSource.position);
            float sharedYaw = _sharedSpace.WorldYawToSharedYaw(_poseSource.eulerAngles.y);
            _client.SendPlayerPose(sharedPosition, sharedYaw);
        }

        private void CacheReferences()
        {
            if (_client == null)
                _client = FindObjectOfType<SimpleNetworkClient>();
            if (_poseSource == null && Camera.main != null)
                _poseSource = Camera.main.transform;
            if (_sharedSpace == null)
                _sharedSpace = SharedSpaceManager.Instance != null
                    ? SharedSpaceManager.Instance
                    : FindObjectOfType<SharedSpaceManager>();
        }

        private void LogWaitingForSharedSpace()
        {
            _logTimer -= Time.deltaTime;
            if (_logTimer > 0f)
                return;

            _logTimer = LogInterval;
            Debug.Log($"{LogPrefix} waiting for shared space ready sharedSpace={_sharedSpace != null}");
        }
    }
}
