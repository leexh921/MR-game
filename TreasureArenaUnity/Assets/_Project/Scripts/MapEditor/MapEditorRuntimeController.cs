using System;
using System.Collections.Generic;
using TreasureArenaMR.Map;
using UnityEngine;
using UnityEngine.XR;

namespace TreasureArenaMR.MapEditor
{
    public enum MapEditorRuntimeEditMode
    {
        Place,
        Move,
        Rotate,
        Scale
    }

    public sealed class MapEditorRuntimeController : MonoBehaviour
    {
        [Header("Ray Input")]
        [SerializeField] private XRNode controllerNode = XRNode.RightHand;
        [SerializeField] private Transform rayOrigin;
        [SerializeField] private Camera fallbackCamera;
        [SerializeField] private LayerMask placementMask = ~0;
        [SerializeField] private float maxRayDistance = 20f;
        [SerializeField] private LineRenderer rayLine;

        [Header("Editing")]
        [SerializeField] private Transform placedObjectsRoot;
        [SerializeField] private List<MapEditorRuntimeBrush> brushes = new List<MapEditorRuntimeBrush>();
        [SerializeField] private int activeBrushIndex;
        [SerializeField] private MapEditorRuntimeEditMode editMode = MapEditorRuntimeEditMode.Place;
        [SerializeField] private float gridSize = 0.5f;
        [SerializeField] private float rotateStepDegrees = 15f;
        [SerializeField] private float scaleStep = 0.1f;
        [SerializeField] private float repeatDelay = 0.18f;

        [Header("Editor Fallback Keys")]
        [SerializeField] private KeyCode deleteKey = KeyCode.Delete;
        [SerializeField] private KeyCode nextModeKey = KeyCode.Tab;
        [SerializeField] private KeyCode clearBrushKey = KeyCode.Escape;
        [SerializeField] private KeyCode rotateLeftKey = KeyCode.Q;
        [SerializeField] private KeyCode rotateRightKey = KeyCode.E;
        [SerializeField] private KeyCode scaleDownKey = KeyCode.Minus;
        [SerializeField] private KeyCode scaleUpKey = KeyCode.Equals;

        private GameObject previewGhost;
        private GameObject selectedObject;
        private readonly Dictionary<string, int> nameCounters = new Dictionary<string, int>();
        private static Material sharedGhostMaterial;
        private bool triggerWasPressed;
        private bool gripWasPressed;
        private bool primaryWasPressed;
        private bool secondaryWasPressed;
        private float nextRepeatTime;
        private string statusMessage = "Map editor runtime input ready.";

        public event Action<string> StatusChanged;
        public event Action<int> BrushChanged;
        public event Action<GameObject> SelectionChanged;
        public event Action<MapEditorRuntimeEditMode> EditModeChanged;

        public IReadOnlyList<MapEditorRuntimeBrush> Brushes => brushes;
        public int ActiveBrushIndex => activeBrushIndex;
        public GameObject SelectedObject => selectedObject;
        public MapEditorRuntimeEditMode EditMode => editMode;
        public string StatusMessage => statusMessage;

        public MapEditorRuntimeBrush ActiveBrush
        {
            get
            {
                if (activeBrushIndex < 0 || activeBrushIndex >= brushes.Count)
                {
                    return null;
                }

                return brushes[activeBrushIndex];
            }
        }

        private void Awake()
        {
            if (fallbackCamera == null)
            {
                fallbackCamera = Camera.main;
            }

            if (placedObjectsRoot == null)
            {
                placedObjectsRoot = transform;
            }

            if (brushes.Count == 0)
            {
                activeBrushIndex = -1;
            }

            RebuildNameCounters();
            SetStatus(statusMessage);
        }

        private void OnDisable()
        {
            DestroyPreviewGhost();
        }

        private void Update()
        {
            if (!TryGetPointerRay(out Ray ray, out bool isXrValid))
            {
                SetRayLine(false, Vector3.zero, Vector3.zero);
                return;
            }

            bool hasTarget = TryGetPlacementPoint(ray, out Vector3 hitPoint, out RaycastHit hit);
            Vector3 endPoint = hasTarget ? hitPoint : ray.origin + ray.direction * maxRayDistance;
            SetRayLine(true, ray.origin, endPoint);

            if (editMode == MapEditorRuntimeEditMode.Place && ActiveBrush != null && hasTarget)
            {
                UpdatePreviewGhost(SnapToGrid(hitPoint));
            }
            else
            {
                DestroyPreviewGhost();
            }

            RuntimeButtons buttons = ReadButtons();
            HandleButtonEdges(buttons, hasTarget, hitPoint, hit);
            HandleHeldButtons(buttons, hasTarget, hitPoint);
            HandleEditorFallback(hasTarget, hitPoint, hit, !isXrValid);
        }

        public void SelectBrush(int index)
        {
            DestroyPreviewGhost();

            if (index < 0 || index >= brushes.Count)
            {
                activeBrushIndex = -1;
                SetStatus("Brush cleared.");
            }
            else
            {
                activeBrushIndex = index;
                editMode = MapEditorRuntimeEditMode.Place;
                SetStatus("Brush selected: " + brushes[index].DisplayName);
            }

            BrushChanged?.Invoke(activeBrushIndex);
            EditModeChanged?.Invoke(editMode);
        }

        public void ClearBrush()
        {
            SelectBrush(-1);
        }

        public void SetEditMode(MapEditorRuntimeEditMode mode)
        {
            editMode = mode;
            SetStatus("Edit mode: " + editMode);
            EditModeChanged?.Invoke(editMode);
        }

        public void CycleEditMode()
        {
            MapEditorRuntimeEditMode next = editMode == MapEditorRuntimeEditMode.Scale
                ? MapEditorRuntimeEditMode.Place
                : (MapEditorRuntimeEditMode)((int)editMode + 1);
            SetEditMode(next);
        }

        public void DeleteSelected()
        {
            if (selectedObject == null)
            {
                SetStatus("No selected object to delete.");
                return;
            }

            string deletedName = selectedObject.name;
            Destroy(selectedObject);
            selectedObject = null;
            SelectionChanged?.Invoke(null);
            SetStatus("Deleted " + deletedName);
        }

        public void RotateSelected(float degrees)
        {
            if (selectedObject == null)
            {
                SetStatus("No selected object to rotate.");
                return;
            }

            selectedObject.transform.Rotate(Vector3.up, degrees, Space.World);
            SetStatus("Rotated " + selectedObject.name);
        }

        public void ScaleSelected(float delta)
        {
            if (selectedObject == null)
            {
                SetStatus("No selected object to scale.");
                return;
            }

            Vector3 scale = selectedObject.transform.localScale;
            float minScale = 0.1f;
            scale.x = Mathf.Max(minScale, scale.x + delta);
            scale.y = Mathf.Max(minScale, scale.y + delta);
            scale.z = Mathf.Max(minScale, scale.z + delta);
            selectedObject.transform.localScale = scale;
            SetStatus("Scaled " + selectedObject.name);
        }

        private void HandleButtonEdges(RuntimeButtons buttons, bool hasTarget, Vector3 hitPoint, RaycastHit hit)
        {
            if (buttons.triggerPressed && !triggerWasPressed)
            {
                if (editMode == MapEditorRuntimeEditMode.Place && ActiveBrush != null && hasTarget)
                {
                    PlaceObject(SnapToGrid(hitPoint));
                }
                else
                {
                    SelectFromHit(hit);
                }
            }

            if (buttons.primaryPressed && !primaryWasPressed)
            {
                DeleteSelected();
            }

            if (buttons.secondaryPressed && !secondaryWasPressed)
            {
                CycleEditMode();
            }

            triggerWasPressed = buttons.triggerPressed;
            gripWasPressed = buttons.gripPressed;
            primaryWasPressed = buttons.primaryPressed;
            secondaryWasPressed = buttons.secondaryPressed;
        }

        private void HandleHeldButtons(RuntimeButtons buttons, bool hasTarget, Vector3 hitPoint)
        {
            if (selectedObject == null)
            {
                return;
            }

            if (buttons.gripPressed && hasTarget && editMode == MapEditorRuntimeEditMode.Move)
            {
                selectedObject.transform.position = SnapToGrid(hitPoint);
                if (!gripWasPressed)
                {
                    SetStatus("Moving " + selectedObject.name);
                }
            }

            if (Time.time < nextRepeatTime)
            {
                return;
            }

            if (Mathf.Abs(buttons.axis.x) > 0.6f)
            {
                RotateSelected(Mathf.Sign(buttons.axis.x) * rotateStepDegrees);
                nextRepeatTime = Time.time + repeatDelay;
            }

            if (Mathf.Abs(buttons.axis.y) > 0.6f)
            {
                ScaleSelected(Mathf.Sign(buttons.axis.y) * scaleStep);
                nextRepeatTime = Time.time + repeatDelay;
            }
        }

        private void HandleEditorFallback(bool hasTarget, Vector3 hitPoint, RaycastHit hit, bool allowEditInput)
        {
            if (Input.GetKeyDown(clearBrushKey))
            {
                ClearBrush();
            }

            if (Input.GetKeyDown(nextModeKey))
            {
                CycleEditMode();
            }

            if (!allowEditInput)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0) && hasTarget)
            {
                if (editMode == MapEditorRuntimeEditMode.Place && ActiveBrush != null)
                {
                    PlaceObject(SnapToGrid(hitPoint));
                }
                else
                {
                    SelectFromHit(hit);
                }
            }

            if (Input.GetMouseButton(0) && selectedObject != null && editMode == MapEditorRuntimeEditMode.Move && hasTarget)
            {
                selectedObject.transform.position = SnapToGrid(hitPoint);
            }

            if (Input.GetKeyDown(deleteKey))
            {
                DeleteSelected();
            }

            if (Input.GetKeyDown(rotateLeftKey))
            {
                RotateSelected(-rotateStepDegrees);
            }

            if (Input.GetKeyDown(rotateRightKey))
            {
                RotateSelected(rotateStepDegrees);
            }

            if (Input.GetKeyDown(scaleDownKey))
            {
                ScaleSelected(-scaleStep);
            }

            if (Input.GetKeyDown(scaleUpKey))
            {
                ScaleSelected(scaleStep);
            }
        }

        private void PlaceObject(Vector3 position)
        {
            MapEditorRuntimeBrush brush = ActiveBrush;
            if (brush == null || brush.prefab == null)
            {
                SetStatus("Cannot place: active brush has no prefab.");
                return;
            }

            GameObject instance = Instantiate(brush.prefab, position, Quaternion.identity, placedObjectsRoot);
            instance.name = CreateUniqueName(brush.PrefabId);
            ApplyMarker(instance, brush);
            SelectObject(instance);
            SetStatus("Placed " + instance.name + " at " + position);
        }

        private void ApplyMarker(GameObject instance, MapEditorRuntimeBrush brush)
        {
            MapExportMarker marker = instance.GetComponent<MapExportMarker>();
            if (marker == null)
            {
                marker = instance.AddComponent<MapExportMarker>();
            }

            marker.id = instance.name;
            marker.prefab_id = brush.PrefabId;
            marker.marker_type = brush.marker_type;
            marker.team = brush.team;
            marker.treasure_type = brush.treasure_type;
            marker.supply_type = brush.supply_type;
            marker.refresh_interval = brush.refresh_interval;
            marker.radius = brush.radius;
            marker.has_collider = brush.has_collider && instance.GetComponentInChildren<Collider>() != null;
        }

        private void SelectFromHit(RaycastHit hit)
        {
            if (hit.collider == null)
            {
                SelectObject(null);
                return;
            }

            MapExportMarker marker = hit.collider.GetComponentInParent<MapExportMarker>();
            SelectObject(marker != null ? marker.gameObject : null);
        }

        private void SelectObject(GameObject target)
        {
            selectedObject = target;
            SelectionChanged?.Invoke(selectedObject);
            SetStatus(selectedObject == null ? "Selection cleared." : "Selected " + selectedObject.name);
        }

        private void UpdatePreviewGhost(Vector3 position)
        {
            MapEditorRuntimeBrush brush = ActiveBrush;
            if (brush == null || brush.prefab == null)
            {
                DestroyPreviewGhost();
                return;
            }

            if (previewGhost == null)
            {
                previewGhost = Instantiate(brush.prefab);
                previewGhost.name = "MapEditorPreviewGhost";
                SetLayerRecursive(previewGhost, Physics.IgnoreRaycastLayer);
                SetPreviewMaterial(previewGhost);
                SetCollidersEnabled(previewGhost, false);
            }

            previewGhost.transform.position = position;
        }

        private void DestroyPreviewGhost()
        {
            if (previewGhost != null)
            {
                Destroy(previewGhost);
                previewGhost = null;
            }
        }

        private bool TryGetPointerRay(out Ray ray, out bool isXrValid)
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(controllerNode);
            if (device.isValid
                && device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position)
                && device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
            {
                ray = new Ray(position, rotation * Vector3.forward);
                isXrValid = true;
                return true;
            }

            Transform origin = rayOrigin;
            if (origin != null)
            {
                ray = new Ray(origin.position, origin.forward);
                isXrValid = false;
                return true;
            }

            if (fallbackCamera == null)
            {
                fallbackCamera = Camera.main;
            }

            if (fallbackCamera != null)
            {
                ray = fallbackCamera.ScreenPointToRay(Input.mousePosition);
                isXrValid = false;
                return true;
            }

            ray = default;
            isXrValid = false;
            return false;
        }

        private RuntimeButtons ReadButtons()
        {
            RuntimeButtons buttons = default;
            InputDevice device = InputDevices.GetDeviceAtXRNode(controllerNode);
            if (!device.isValid)
            {
                return buttons;
            }

            device.TryGetFeatureValue(CommonUsages.triggerButton, out buttons.triggerPressed);
            device.TryGetFeatureValue(CommonUsages.gripButton, out buttons.gripPressed);
            device.TryGetFeatureValue(CommonUsages.primaryButton, out buttons.primaryPressed);
            device.TryGetFeatureValue(CommonUsages.secondaryButton, out buttons.secondaryPressed);
            device.TryGetFeatureValue(CommonUsages.primary2DAxis, out buttons.axis);
            return buttons;
        }

        private bool TryGetPlacementPoint(Ray ray, out Vector3 point, out RaycastHit hit)
        {
            if (Physics.Raycast(ray, out hit, maxRayDistance, placementMask, QueryTriggerInteraction.Ignore))
            {
                point = hit.point;
                return true;
            }

            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float distance) && distance <= maxRayDistance)
            {
                point = ray.GetPoint(distance);
                hit = default;
                return true;
            }

            point = default;
            hit = default;
            return false;
        }

        private Vector3 SnapToGrid(Vector3 value)
        {
            if (gridSize <= 0f)
            {
                return value;
            }

            return new Vector3(
                Mathf.Round(value.x / gridSize) * gridSize,
                Mathf.Round(value.y / gridSize) * gridSize,
                Mathf.Round(value.z / gridSize) * gridSize);
        }

        private string CreateUniqueName(string baseName)
        {
            string safeBaseName = string.IsNullOrEmpty(baseName) ? "map_object" : baseName;
            int index = nameCounters.TryGetValue(safeBaseName, out int currentIndex) ? currentIndex + 1 : 1;
            nameCounters[safeBaseName] = index;
            return safeBaseName + "_" + index.ToString("D2");
        }

        private void SetRayLine(bool visible, Vector3 start, Vector3 end)
        {
            if (rayLine == null)
            {
                return;
            }

            rayLine.enabled = visible;
            if (!visible)
            {
                return;
            }

            rayLine.positionCount = 2;
            rayLine.SetPosition(0, start);
            rayLine.SetPosition(1, end);
        }

        private void SetStatus(string value)
        {
            statusMessage = value;
            StatusChanged?.Invoke(statusMessage);
        }

        private static void SetCollidersEnabled(GameObject root, bool enabled)
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = enabled;
            }
        }

        private void RebuildNameCounters()
        {
            nameCounters.Clear();
            MapExportMarker[] markers = placedObjectsRoot.GetComponentsInChildren<MapExportMarker>(true);
            for (int i = 0; i < markers.Length; i++)
            {
                RegisterExistingName(markers[i]);
            }
        }

        private void RegisterExistingName(MapExportMarker marker)
        {
            if (marker == null)
            {
                return;
            }

            string id = marker.GetDefaultId();
            int separatorIndex = id.LastIndexOf('_');
            if (separatorIndex <= 0 || separatorIndex >= id.Length - 1)
            {
                return;
            }

            string suffix = id.Substring(separatorIndex + 1);
            if (!int.TryParse(suffix, out int index))
            {
                return;
            }

            string baseName = id.Substring(0, separatorIndex);
            if (!nameCounters.TryGetValue(baseName, out int currentIndex) || index > currentIndex)
            {
                nameCounters[baseName] = index;
            }
        }

        private static void SetLayerRecursive(GameObject root, int layer)
        {
            root.layer = layer;
            Transform transform = root.transform;
            for (int i = 0; i < transform.childCount; i++)
            {
                SetLayerRecursive(transform.GetChild(i).gameObject, layer);
            }
        }

        private static void SetPreviewMaterial(GameObject root)
        {
            Material material = GetSharedGhostMaterial();
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sharedMaterial = material;
            }
        }

        private static Material GetSharedGhostMaterial()
        {
            if (sharedGhostMaterial != null)
            {
                return sharedGhostMaterial;
            }

            sharedGhostMaterial = new Material(Shader.Find("Standard"));
            sharedGhostMaterial.color = new Color(0f, 1f, 0.5f, 0.35f);
            sharedGhostMaterial.SetFloat("_Mode", 3f);
            sharedGhostMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            sharedGhostMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            sharedGhostMaterial.SetInt("_ZWrite", 0);
            sharedGhostMaterial.DisableKeyword("_ALPHATEST_ON");
            sharedGhostMaterial.EnableKeyword("_ALPHABLEND_ON");
            sharedGhostMaterial.renderQueue = 3000;
            return sharedGhostMaterial;
        }

        private struct RuntimeButtons
        {
            public bool triggerPressed;
            public bool gripPressed;
            public bool primaryPressed;
            public bool secondaryPressed;
            public Vector2 axis;
        }
    }
}
