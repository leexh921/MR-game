using System;
using System.Collections.Generic;
using UnityEngine;

namespace TreasureArenaMR.Map
{
    /// <summary>
    /// Maps frozen map JSON prefab IDs to Unity prefabs included in the client build.
    /// </summary>
    [CreateAssetMenu(menuName = "TreasureArena/Map/Prefab Registry", fileName = "MapPrefabRegistry")]
    public sealed class PrefabRegistry : ScriptableObject
    {
        [SerializeField] private List<Entry> entries = new List<Entry>();
        private Dictionary<string, GameObject> lookup;

        public IReadOnlyList<Entry> Entries => entries;

        public bool TryGetPrefab(string prefabId, out GameObject prefab)
        {
            EnsureLookup();
            if (string.IsNullOrEmpty(prefabId))
            {
                prefab = null;
                return false;
            }

            return lookup.TryGetValue(prefabId, out prefab) && prefab != null;
        }

        public void SetEntries(IEnumerable<Entry> newEntries)
        {
            entries.Clear();
            if (newEntries != null)
            {
                entries.AddRange(newEntries);
            }

            lookup = null;
        }

        private void EnsureLookup()
        {
            if (lookup != null)
            {
                return;
            }

            lookup = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.prefab_id) || entry.prefab == null)
                {
                    continue;
                }

                lookup[entry.prefab_id] = entry.prefab;
            }
        }

        [Serializable]
        public sealed class Entry
        {
            public string prefab_id;
            public GameObject prefab;

            public Entry(string prefabId, GameObject prefab)
            {
                this.prefab_id = prefabId;
                this.prefab = prefab;
            }
        }
    }
}
