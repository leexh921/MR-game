using System.Collections.Generic;
using UnityEngine;

namespace TreasureArenaMR.MapEditor
{
    public sealed class MapEditorBoundaryData : MonoBehaviour
    {
        [SerializeField] private string boundaryType = "Polygon";
        [SerializeField] private float height = 2.5f;
        [SerializeField] private List<Vector3> points = new List<Vector3>();

        public string BoundaryType => string.IsNullOrEmpty(boundaryType) ? "Polygon" : boundaryType;
        public float Height
        {
            get => height;
            set => height = Mathf.Max(0.01f, value);
        }

        public IReadOnlyList<Vector3> Points => points;
        public bool IsClosed => points.Count >= 3;

        public void Clear()
        {
            points.Clear();
        }

        public void AddPoint(Vector3 point)
        {
            points.Add(point);
        }

        public void SetPoints(IEnumerable<Vector3> values)
        {
            points.Clear();
            if (values == null)
            {
                return;
            }

            foreach (Vector3 value in values)
            {
                points.Add(value);
            }
        }

        public List<Vector3> CopyPoints()
        {
            return new List<Vector3>(points);
        }
    }
}
