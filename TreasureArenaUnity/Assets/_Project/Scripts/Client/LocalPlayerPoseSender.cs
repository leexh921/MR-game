using TreasureArenaMR.Network;
using UnityEngine;

namespace TreasureArenaMR.Client
{
    public sealed class LocalPlayerPoseSender : MonoBehaviour
    {
        [SerializeField] private SimpleNetworkClient _client;
        [SerializeField] private Transform _poseSource;
        [SerializeField] private float _sendHz = 20f;

        private float _sendTimer;

        private void Awake()
        {
            if (_client == null)
                _client = FindObjectOfType<SimpleNetworkClient>();
            if (_poseSource == null && Camera.main != null)
                _poseSource = Camera.main.transform;
        }

        private void Update()
        {
            if (_client == null || !_client.IsConnected || _poseSource == null)
                return;

            float interval = _sendHz > 0f ? 1f / _sendHz : 0.05f;
            _sendTimer -= Time.deltaTime;
            if (_sendTimer > 0f)
                return;

            _sendTimer = interval;
            _client.SendPlayerPose(_poseSource.position, _poseSource.eulerAngles.y);
        }
    }
}
