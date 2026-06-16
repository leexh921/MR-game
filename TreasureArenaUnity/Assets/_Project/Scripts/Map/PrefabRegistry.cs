using System;
using System.Collections.Generic;
using UnityEngine;

namespace TreasureArenaMR.Map
{
    /// <summary>
    /// Maps map JSON prefab IDs to Unity prefab assets.
    /// </summary>
    public sealed class PrefabRegistry : ScriptableObject
    {
        [SerializeField] private List<Entry> entries = new List<Entry>();

        public IReadOnlyList<Entry> Entries => entries;
        public int Count => entries != null ? entries.Count : 0;

        public bool TryGetPrefab(string prefabId, out GameObject prefab)
        {
            prefab = null;
            if (string.IsNullOrEmpty(prefabId) || entries == null)
            {
                return false;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (entry == null || entry.prefab == null)
                {
                    continue;
                }

                if (entry.prefab_id == prefabId)
                {
                    prefab = entry.prefab;
                    return true;
                }
            }

            return false;
        }

        public GameObject GetPrefab(string prefabId)
        {
            TryGetPrefab(prefabId, out GameObject prefab);
            return prefab;
        }

        public void SetEntries(IEnumerable<Entry> nextEntries)
        {
            entries = nextEntries != null
                ? new List<Entry>(nextEntries)
                : new List<Entry>();
        }

        [Serializable]
        public sealed class Entry
        {
            public string prefab_id;
            public GameObject prefab;

            public Entry(string prefabId, GameObject prefab)
            {
                prefab_id = prefabId;
                this.prefab = prefab;
            }
        }
    }
}
