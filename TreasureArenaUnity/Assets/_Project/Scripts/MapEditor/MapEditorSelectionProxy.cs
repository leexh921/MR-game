using UnityEngine;

namespace TreasureArenaMR.MapEditor
{
    public sealed class MapEditorSelectionProxy : MonoBehaviour
    {
        [SerializeField] private GameObject target;

        public GameObject Target
        {
            get => target;
            set => target = value;
        }
    }
}
