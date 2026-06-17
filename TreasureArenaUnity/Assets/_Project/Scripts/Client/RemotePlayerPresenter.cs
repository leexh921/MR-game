using System.Collections.Generic;
using TreasureArenaMR.Network;
using UnityEngine;

namespace TreasureArenaMR.Client
{
    public sealed class RemotePlayerPresenter : MonoBehaviour
    {
        private const string LogPrefix = "[SimpleRemotePlayer]";
        private const float InterpolationDelaySeconds = 0.1f;
        private const float StaleSeconds = 1f;

        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private string _resourcesPrefabPath = "PlayerPrefab";

        private readonly Dictionary<string, RemotePlayer> _players = new Dictionary<string, RemotePlayer>();
        private ClientMatchStateStore _store;
        private Transform _root;

        private void Awake()
        {
            _store = ClientMatchStateStore.Instance;
            if (_store == null)
                _store = new GameObject("ClientMatchStateStore").AddComponent<ClientMatchStateStore>();

            if (_playerPrefab == null)
                _playerPrefab = Resources.Load<GameObject>(_resourcesPrefabPath);

            _root = new GameObject("RemotePlayers").transform;
            _root.SetParent(transform, false);

            if (_playerPrefab == null)
                Debug.LogError($"{LogPrefix} Player prefab missing. Expected Resources/{_resourcesPrefabPath}.prefab");
        }

        private void Update()
        {
            if (_store == null || !_store.HasPoseSnapshot)
                return;

            PoseSnapshotPayload snapshot = _store.PoseSnapshot;
            for (int i = 0; i < snapshot.players.Count; i++)
            {
                PlayerPoseSnapshot pose = snapshot.players[i];
                if (pose == null || string.IsNullOrEmpty(pose.player_id))
                    continue;
                if (pose.player_id == _store.LocalPlayerId)
                    continue;

                RemotePlayer remote = GetOrCreateRemote(pose.player_id);
                if (remote == null)
                    continue;

                remote.AddSample(pose);
            }

            float renderTime = Time.time - InterpolationDelaySeconds;
            foreach (RemotePlayer remote in _players.Values)
                remote.Render(renderTime);
        }

        private RemotePlayer GetOrCreateRemote(string playerId)
        {
            if (_players.TryGetValue(playerId, out RemotePlayer remote))
                return remote;

            if (_playerPrefab == null)
                return null;

            GameObject instance = Instantiate(_playerPrefab, _root);
            instance.name = "RemotePlayer_" + playerId;
            remote = new RemotePlayer(playerId, instance.transform);
            _players[playerId] = remote;
            Debug.Log($"{LogPrefix} spawned player={playerId} prefab={_playerPrefab.name}");
            return remote;
        }

        private sealed class RemotePlayer
        {
            private readonly string _playerId;
            private readonly Transform _transform;
            private readonly List<Sample> _samples = new List<Sample>();
            private bool _loggedStale;

            public RemotePlayer(string playerId, Transform transform)
            {
                _playerId = playerId;
                _transform = transform;
            }

            public void AddSample(PlayerPoseSnapshot pose)
            {
                _samples.Add(new Sample
                {
                    receivedTime = Time.time,
                    position = pose.position != null ? pose.position.ToVector3() : Vector3.zero,
                    rotationY = pose.rotation_y,
                    stale = pose.stale
                });

                while (_samples.Count > 8)
                    _samples.RemoveAt(0);
            }

            public void Render(float renderTime)
            {
                if (_samples.Count == 0)
                    return;

                Sample latest = _samples[_samples.Count - 1];
                bool stale = latest.stale || Time.time - latest.receivedTime > StaleSeconds;
                if (stale)
                {
                    if (!_loggedStale)
                    {
                        _loggedStale = true;
                        Debug.LogWarning($"{LogPrefix} stale player={_playerId}");
                    }
                    return;
                }

                _loggedStale = false;
                if (_samples.Count == 1)
                {
                    Apply(latest);
                    return;
                }

                Sample before = _samples[0];
                Sample after = latest;
                for (int i = 0; i < _samples.Count - 1; i++)
                {
                    if (_samples[i].receivedTime <= renderTime && _samples[i + 1].receivedTime >= renderTime)
                    {
                        before = _samples[i];
                        after = _samples[i + 1];
                        break;
                    }
                }

                float range = after.receivedTime - before.receivedTime;
                float t = range > 0.0001f ? Mathf.Clamp01((renderTime - before.receivedTime) / range) : 1f;
                _transform.position = Vector3.Lerp(before.position, after.position, t);
                _transform.rotation = Quaternion.Euler(0f, Mathf.LerpAngle(before.rotationY, after.rotationY, t), 0f);
            }

            private void Apply(Sample sample)
            {
                _transform.position = sample.position;
                _transform.rotation = Quaternion.Euler(0f, sample.rotationY, 0f);
            }
        }

        private struct Sample
        {
            public float receivedTime;
            public Vector3 position;
            public float rotationY;
            public bool stale;
        }
    }
}
