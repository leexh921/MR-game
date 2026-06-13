using UnityEngine;
using UnityEngine.UI;

namespace TreasureArenaMR.MapEditor
{
    public sealed class MapEditorPanelMoveBar : MonoBehaviour
    {
        [SerializeField] private Transform panelRoot;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Image highlightImage;
        [SerializeField] private Color normalColor = new Color(0.10f, 0.16f, 0.22f, 0.95f);
        [SerializeField] private Color hoverColor = new Color(0.16f, 0.42f, 0.66f, 0.98f);
        [SerializeField] private float minDistance = 0.55f;
        [SerializeField] private float maxDistance = 1.8f;
        [SerializeField] private float minHeight = 0.55f;
        [SerializeField] private float maxHeight = 1.65f;
        [SerializeField] private float tiltDegrees = 28f;
        [SerializeField] private float yawOffset = 180f;

        private bool dragging;

        public void BeginDrag(Ray ray, Camera cameraOverride)
        {
            dragging = true;
            targetCamera = cameraOverride != null ? cameraOverride : targetCamera;
            SetHover(true);
            UpdateDrag(ray);
        }

        public void UpdateDrag(Ray ray)
        {
            Transform root = GetPanelRoot();
            Camera camera = GetCamera();
            if (root == null || camera == null)
            {
                return;
            }

            Vector3 head = camera.transform.position;
            Vector3 closest = ClosestPointOnRay(ray, head);
            Vector3 offset = closest - head;
            if (offset.sqrMagnitude < 0.01f)
            {
                offset = camera.transform.forward;
            }

            float distance = Mathf.Clamp(offset.magnitude, minDistance, maxDistance);
            Vector3 direction = offset.normalized;
            Vector3 targetPosition = head + direction * distance;
            targetPosition.y = Mathf.Clamp(targetPosition.y, minHeight, maxHeight);
            root.position = targetPosition;

            Quaternion faceUser = Quaternion.LookRotation(head - root.position, Vector3.up);
            root.rotation = faceUser * Quaternion.Euler(tiltDegrees, yawOffset, 0f);
        }

        public void EndDrag()
        {
            dragging = false;
            SetHover(false);
        }

        public void SetHover(bool value)
        {
            if (highlightImage == null)
            {
                highlightImage = GetComponent<Image>();
            }

            if (highlightImage != null)
            {
                highlightImage.color = value || dragging ? hoverColor : normalColor;
            }
        }

        private Transform GetPanelRoot()
        {
            if (panelRoot != null)
            {
                return panelRoot;
            }

            MapEditorDockedUiController controller = GetComponentInParent<MapEditorDockedUiController>();
            panelRoot = controller != null ? controller.transform : transform.root;
            return panelRoot;
        }

        private Camera GetCamera()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            return targetCamera;
        }

        private static Vector3 ClosestPointOnRay(Ray ray, Vector3 point)
        {
            float t = Vector3.Dot(point - ray.origin, ray.direction);
            return ray.origin + ray.direction * Mathf.Max(0f, t);
        }
    }
}
