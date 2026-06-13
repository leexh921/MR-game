using UnityEngine;

namespace TreasureArenaMR.Gameplay
{
    public sealed class OpenableBox : MonoBehaviour
    {
        public string object_id;
        public bool is_opened;
        public string spawned_item_id;
        public string state = "Closed";

        public void Open(string spawnedItemId = null)
        {
            if (is_opened)
            {
                return;
            }

            is_opened = true;
            spawned_item_id = spawnedItemId;
            state = "Opened";
        }
    }
}
