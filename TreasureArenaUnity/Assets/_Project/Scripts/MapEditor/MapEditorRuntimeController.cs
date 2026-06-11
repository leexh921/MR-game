using System;
using System.Collections.Generic;
using TreasureArenaMR.Map;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR;

namespace TreasureArenaMR.MapEditor
{
    public enum MapEditorHand
    {
        Left,
        Right
    }

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
        [SerializeField] private Transform rayOrigin;
        [SerializeField] private Camera fallbackCamera;
        [SerializeField] private LayerMask placementMask = ~0;
        [SerializeField] private float maxRayDistance = 20f;
        [SerializeField] private LineRenderer rayLine;

        [Header("Editing")]
        [SerializeField] private Transform placedObjectsRoot;
        [SerializeField] private List<MapEditorRuntimeBrush> brushes = new List<MapEditorRuntimeBrush>();
        [SerializeField] private int activeBrushIndex = -1;
        [SerializeField] private MapEditorRuntimeEditMode editMode = MapEditorRuntimeEditMode.Place;
        [SerializeField] private float gridSize = 0.5f;
        [SerializeField] private float rotateStepDegrees = 15f;
        [SerializeField] private float scaleStep = 0.1f;
        [SerializeField] private float repeatDelay = 0.18f;
        [SerializeField] private float menuLongPressSeconds = 0.75f;

        [Header("Editor Fallback Keys")]
        [SerializeField] private KeyCode deleteKey = KeyCode.Delete;
        [SerializeField] private KeyCode nextModeKey = KeyCode.Tab;
        [SerializeField] private KeyCode clearBrushKey = KeyCode.Escape;
        [SerializeField] private KeyCode rotateLeftKey = KeyCode.Q;
        [SerializeField] private KeyCode rotateRightKey = KeyCode.E;
        [SerializeField] private KeyCode scaleDownKey = KeyCode.Minus;
        [SerializeField] private KeyCode scaleUpKey = KeyCode.Equals;

        private readonly HandState leftHand = new HandState(MapEditorHand.Left, XRNode.LeftHand);
        private readonly HandState rightHand = new HandState(MapEditorHand.Right, XRNode.RightHand);
        private MapEditorHand activeHand = MapEditorHand.Right;
        private GameObject previewGhost;
        private GameObject moveGhost;
        private GameObject selectedObject;
        private readonly Dictionary<string, int> nameCounters = new Dictionary<string, int>();
        private static Material sharedGhostMaterial;
        private bool isDragging;
        private float nextRepeatTime;
        private string statusMessage = "Map editor runtime input ready.";

        public event Action<string> StatusChanged;
        public event Action<int> BrushChanged;
        public event Action<MapEditorHand, int> HandBrushChanged;
        public event Action<GameObject> SelectionChanged;
        public event Action<MapEditorRuntimeEditMode> EditModeChanged;
        public event Action<MapEditorHand, MapEditorRuntimeEditMode> HandEditModeChanged;
        public event Action<MapEditorHand> ActiveHandChanged;
        public event Action<bool> DraggingChanged;
        public event Action WorkbenchToggleRequested;
        public event Action WorkbenchRecenterRequested;

        public IReadOnlyList<MapEditorRuntimeBrush> Brushes => brushes;
        public MapEditorHand ActiveHand => activeHand;
        public int ActiveBrushIndex => GetHandState(activeHand).BrushIndex;
        public int LeftBrushIndex => leftHand.BrushIndex;
        public int RightBrushIndex => rightHand.BrushIndex;
        public GameObject SelectedObject => selectedObject;
        public MapEditorRuntimeEditMode EditMode => GetHandState(activeHand).EditMode;
        public string StatusMessage => statusMessage;
        public float GridSize => gridSize;
        public float RotateStepDegrees => rotateStepDegrees;
        public float ScaleStep => scaleStep;

        public MapEditorRuntimeBrush ActiveBrush => GetBrush(ActiveBrushIndex);

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

            leftHand.BrushIndex = -1;
            rightHand.BrushIndex = activeBrushIndex >= 0 && activeBrushIndex < brushes.Count ? activeBrushIndex : -1;
            rightHand.EditMode = editMode;
            leftHand.EditMode = editMode;
            activeBrushIndex = rightHand.BrushIndex;

            RebuildNameCounters();
            SetStatus(statusMessage);
        }

        private void OnDisable()
        {
            DestroyPreviewGhost();
            DestroyMoveGhost();
        }

        private void Update()
        {
            bool anyXrValid = false;
            bool activePreviewUpdated = false;
            bool draggingNow = false;

            ProcessHand(leftHand, ref anyXrValid, ref activePreviewUpdated, ref draggingNow);
            ProcessHand(rightHand, ref anyXrValid, ref activePreviewUpdated, ref draggingNow);

            if (!anyXrValid)
            {
                ProcessEditorFallback(ref activePreviewUpdated, ref draggingNow);
            }

            if (!activePreviewUpdated)
            {
                DestroyPreviewGhost();
                DestroyMoveGhost();
                SetRayLine(false, Vector3.zero, Vector3.zero);
            }

            UpdateDragging(draggingNow);
        }

        public void SelectBrush(int index)
        {
            SelectBrush(index, activeHand);
        }

        public void SelectBrush(int index, MapEditorHand hand)
        {
            HandState state = GetHandState(hand);
            DestroyPreviewGhost();

            if (index < 0 || index >= brushes.Count)
            {
                state.BrushIndex = -1;
                SetStatus(HandLabel(hand) + " brush cleared.");
            }
            else
            {
                state.BrushIndex = index;
                state.EditMode = MapEditorRuntimeEditMode.Place;
                SetStatus(HandLabel(hand) + " brush selected: " + brushes[index].DisplayName);
            }

            SetActiveHand(hand);
            activeBrushIndex = state.BrushIndex;
            editMode = state.EditMode;
            BrushChanged?.Invoke(activeBrushIndex);
            HandBrushChanged?.Invoke(hand, state.BrushIndex);
            EditModeChanged?.Invoke(editMode);
            HandEditModeChanged?.Invoke(hand, state.EditMode);
        }

        public void ClearBrush()
        {
            ClearBrush(activeHand);
        }

        public void ClearBrush(MapEditorHand hand)
        {
            SelectBrush(-1, hand);
        }

        public void SetEditMode(MapEditorRuntimeEditMode mode)
        {
            SetEditMode(mode, activeHand);
        }

        public void SetEditMode(MapEditorRuntimeEditMode mode, MapEditorHand hand)
        {
            HandState state = GetHandState(hand);
            state.EditMode = mode;
            SetActiveHand(hand);
            editMode = mode;
            SetStatus(HandLabel(hand) + " edit mode: " + mode);
            EditModeChanged?.Invoke(mode);
            HandEditModeChanged?.Invoke(hand, mode);
        }

        public void CycleEditMode()
        {
            CycleEditMode(activeHand);
        }

        public void CycleEditMode(MapEditorHand hand)
        {
            HandState state = GetHandState(hand);
            MapEditorRuntimeEditMode next = state.EditMode == MapEditorRuntimeEditMode.Scale
                ? MapEditorRuntimeEditMode.Place
                : (MapEditorRuntimeEditMode)((int)state.EditMode + 1);
            SetEditMode(next, hand);
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

        public void DuplicateSelected()
        {
            if (selectedObject == null)
            {
                SetStatus("No selected object to duplicate.");
                return;
            }

            GameObject instance = Instantiate(selectedObject, selectedObject.transform.parent);
            MapExportMarker marker = instance.GetComponent<MapExportMarker>();
            string baseName = marker != null && !string.IsNullOrEmpty(marker.prefab_id)
                ? marker.prefab_id
                : selectedObject.name;
            instance.name = CreateUniqueName(baseName);
            instance.transform.position = selectedObject.transform.position + Vector3.right * gridSize;
            if (marker != null)
            {
                marker.id = instance.name;
            }

            SelectObject(instance);
            SetStatus("Duplicated " + instance.name);
        }

        public void ResetSelectedTransform()
        {
            if (selectedObject == null)
            {
                SetStatus("No selected object to reset.");
                return;
            }

            selectedObject.transform.rotation = Quaternion.identity;
            selectedObject.transform.localScale = Vector3.one;
            SelectionChanged?.Invoke(selectedObject);
            SetStatus("Reset transform for " + selectedObject.name);
        }

        public void RotateSelected(float degrees)
        {
            if (selectedObject == null)
            {
                SetStatus("No selected object to rotate.");
                return;
            }

            selectedObject.transform.Rotate(Vector3.up, degrees, Space.World);
            SelectionChanged?.Invoke(selectedObject);
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
            const float minScale = 0.1f;
            scale.x = Mathf.Max(minScale, scale.x + delta);
            scale.y = Mathf.Max(minScale, scale.y + delta);
            scale.z = Mathf.Max(minScale, scale.z + delta);
            selectedObject.transform.localScale = scale;
            SelectionChanged?.Invoke(selectedObject);
            SetStatus("Scaled " + selectedObject.name);
        }

        public string GetBrushDisplayName(MapEditorHand hand)
        {
            MapEditorRuntimeBrush brush = GetBrush(GetHandState(hand).BrushIndex);
            return brush != null ? brush.DisplayName : "None";
        }

        public int GetBrushIndex(MapEditorHand hand)
        {
            return GetHandState(hand).BrushIndex;
        }

        private void ProcessHand(HandState state, ref bool anyXrValid, ref bool activePreviewUpdated, ref bool draggingNow)
        {
            if (!TryGetHandPointerRay(state.Node, out Ray ray))
            {
                state.PreviousButtons = default;
                return;
            }

            anyXrValid = true;
            RuntimeButtons buttons = ReadButtons(state.Node);
            HandleMenuButton(state, buttons);

            bool hasTarget = TryGetPlacementPoint(ray, out Vector3 hitPoint, out RaycastHit hit);
            bool isActive = state.Hand == activeHand;
            Vector3 placementPosition = GetPlacementPosition(hitPoint, hit);

            if (isActive)
            {
                Vector3 endPoint = hasTarget ? hitPoint : ray.origin + ray.direction * maxRayDistance;
                SetRayLine(true, ray.origin, endPoint);
            }

            if (TryHandleRuntimeUi(ray, state, buttons))
            {
                if (isActive)
                {
                    DestroyPreviewGhost();
                    DestroyMoveGhost();
                    activePreviewUpdated = true;
                }

                state.PreviousButtons = buttons;
                return;
            }

            if (buttons.triggerPressed && !state.PreviousButtons.triggerPressed)
            {
                SetActiveHand(state.Hand);
                if (state.EditMode == MapEditorRuntimeEditMode.Place && GetBrush(state.BrushIndex) != null && hasTarget)
                {
                    PlaceObject(placementPosition, state);
                }
                else
                {
                    SelectFromHit(hit);
                }
            }

            if (selectedObject != null && buttons.gripPressed && hasTarget)
            {
                if (!state.PreviousButtons.gripPressed)
                {
                    SetActiveHand(state.Hand);
                    SetStatus(HandLabel(state.Hand) + " moving " + selectedObject.name);
                }

                selectedObject.transform.position = placementPosition;
                SelectionChanged?.Invoke(selectedObject);
                draggingNow = true;
            }

            if (selectedObject != null && !buttons.gripPressed && state.PreviousButtons.gripPressed)
            {
                SetStatus("Moved " + selectedObject.name + " to " + selectedObject.transform.position);
                SelectionChanged?.Invoke(selectedObject);
            }

            if (buttons.primaryPressed && !state.PreviousButtons.primaryPressed)
            {
                SetActiveHand(state.Hand);
                DeleteSelected();
            }

            if (buttons.menuAvailable && buttons.secondaryPressed && !state.PreviousButtons.secondaryPressed)
            {
                CycleEditMode(state.Hand);
            }

            HandleHeldAxis(state, buttons);

            if (state.Hand == activeHand)
            {
                UpdateActivePreview(state, hasTarget, placementPosition, buttons.gripPressed && selectedObject != null, ref activePreviewUpdated);
            }

            state.PreviousButtons = buttons;
        }

        private void ProcessEditorFallback(ref bool activePreviewUpdated, ref bool draggingNow)
        {
            if (fallbackCamera == null)
            {
                fallbackCamera = Camera.main;
            }

            Ray ray;
            if (fallbackCamera != null)
            {
                ray = fallbackCamera.ScreenPointToRay(Input.mousePosition);
            }
            else if (rayOrigin != null)
            {
                ray = new Ray(rayOrigin.position, rayOrigin.forward);
            }
            else
            {
                return;
            }

            bool hasTarget = TryGetPlacementPoint(ray, out Vector3 hitPoint, out RaycastHit hit);
            Vector3 placementPosition = GetPlacementPosition(hitPoint, hit);
            Vector3 endPoint = hasTarget ? hitPoint : ray.origin + ray.direction * maxRayDistance;
            SetRayLine(true, ray.origin, endPoint);

            bool pointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            HandState state = GetHandState(activeHand);

            if (Input.GetKeyDown(clearBrushKey))
            {
                ClearBrush(activeHand);
            }

            if (Input.GetKeyDown(nextModeKey))
            {
                CycleEditMode(activeHand);
            }

            if (Input.GetMouseButtonDown(0) && hasTarget && !pointerOverUi)
            {
                if (state.EditMode == MapEditorRuntimeEditMode.Place && GetBrush(state.BrushIndex) != null)
                {
                    PlaceObject(placementPosition, state);
                }
                else
                {
                    SelectFromHit(hit);
                }
            }

            if (Input.GetMouseButton(0) && selectedObject != null && state.EditMode == MapEditorRuntimeEditMode.Move && hasTarget && !pointerOverUi)
            {
                selectedObject.transform.position = placementPosition;
                SelectionChanged?.Invoke(selectedObject);
                draggingNow = true;
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

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f && selectedObject != null && !pointerOverUi)
            {
                ScaleSelected(Mathf.Sign(scroll) * scaleStep);
            }

            if (!pointerOverUi)
            {
                UpdateActivePreview(state, hasTarget, placementPosition, draggingNow, ref activePreviewUpdated);
            }
        }

        private void HandleMenuButton(HandState state, RuntimeButtons buttons)
        {
            if (!buttons.menuPressed && !state.PreviousButtons.menuPressed)
            {
                state.MenuLongPressFired = false;
                return;
            }

            if (buttons.menuPressed && !state.PreviousButtons.menuPressed)
            {
                state.MenuDownTime = Time.time;
                state.MenuLongPressFired = false;
                return;
            }

            if (buttons.menuPressed && !state.MenuLongPressFired && Time.time - state.MenuDownTime >= menuLongPressSeconds)
            {
                state.MenuLongPressFired = true;
                SetActiveHand(state.Hand);
                WorkbenchRecenterRequested?.Invoke();
                SetStatus("Workbench recentered.");
                return;
            }

            if (!buttons.menuPressed && state.PreviousButtons.menuPressed && !state.MenuLongPressFired)
            {
                SetActiveHand(state.Hand);
                WorkbenchToggleRequested?.Invoke();
                SetStatus("Workbench toggled.");
            }
        }

        private void HandleHeldAxis(HandState state, RuntimeButtons buttons)
        {
            if (selectedObject == null || Time.time < nextRepeatTime)
            {
                return;
            }

            if (Mathf.Abs(buttons.axis.x) > 0.6f)
            {
                SetActiveHand(state.Hand);
                RotateSelected(Mathf.Sign(buttons.axis.x) * rotateStepDegrees);
                nextRepeatTime = Time.time + repeatDelay;
            }

            if (Mathf.Abs(buttons.axis.y) > 0.6f)
            {
                SetActiveHand(state.Hand);
                ScaleSelected(Mathf.Sign(buttons.axis.y) * scaleStep);
                nextRepeatTime = Time.time + repeatDelay;
            }
        }

        private bool TryHandleRuntimeUi(Ray ray, HandState state, RuntimeButtons buttons)
        {
            RaycastHit[] hits = Physics.RaycastAll(ray, maxRayDistance, ~0, QueryTriggerInteraction.Collide);
            if (hits == null || hits.Length == 0)
            {
                return false;
            }

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                MapEditorRuntimeUiHitTarget target = hits[i].collider.GetComponentInParent<MapEditorRuntimeUiHitTarget>();
                if (target != null)
                {
                    SetRayLine(true, ray.origin, hits[i].point);
                    DestroyPreviewGhost();
                    DestroyMoveGhost();
                    if (buttons.triggerPressed && !state.PreviousButtons.triggerPressed)
                    {
                        SetActiveHand(state.Hand);
                        target.Activate(state.Hand);
                    }

                    return true;
                }

                MapEditorDockedPanel panel = hits[i].collider.GetComponentInParent<MapEditorDockedPanel>();
                if (panel != null)
                {
                    SetRayLine(true, ray.origin, hits[i].point);
                    DestroyPreviewGhost();
                    DestroyMoveGhost();
                    return true;
                }
            }

            return false;
        }

        private void UpdateActivePreview(HandState state, bool hasTarget, Vector3 placementPosition, bool moving, ref bool activePreviewUpdated)
        {
            if (moving)
            {
                DestroyPreviewGhost();
                DestroyMoveGhost();
                activePreviewUpdated = true;
                return;
            }

            if (state.EditMode == MapEditorRuntimeEditMode.Place && GetBrush(state.BrushIndex) != null && hasTarget)
            {
                DestroyMoveGhost();
                UpdatePreviewGhost(placementPosition, GetBrush(state.BrushIndex));
                activePreviewUpdated = true;
            }
            else if (state.EditMode == MapEditorRuntimeEditMode.Move && selectedObject != null && hasTarget)
            {
                DestroyPreviewGhost();
                UpdateMoveGhost(placementPosition);
                activePreviewUpdated = true;
            }
            else
            {
                DestroyPreviewGhost();
                DestroyMoveGhost();
                activePreviewUpdated = true;
            }
        }

        private void PlaceObject(Vector3 position, HandState state)
        {
            MapEditorRuntimeBrush brush = GetBrush(state.BrushIndex);
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
            if (selectedObject == null)
            {
                DestroyMoveGhost();
            }

            SelectionChanged?.Invoke(selectedObject);
            SetStatus(selectedObject == null ? "Selection cleared." : "Selected " + selectedObject.name);
        }

        private void UpdatePreviewGhost(Vector3 position, MapEditorRuntimeBrush brush)
        {
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

        private void UpdateMoveGhost(Vector3 position)
        {
            if (selectedObject == null)
            {
                DestroyMoveGhost();
                return;
            }

            if (moveGhost == null)
            {
                moveGhost = Instantiate(selectedObject);
                moveGhost.name = "MapEditorMoveGhost";
                SetLayerRecursive(moveGhost, Physics.IgnoreRaycastLayer);
                SetPreviewMaterial(moveGhost);
                SetCollidersEnabled(moveGhost, false);
            }

            moveGhost.transform.position = position;
            moveGhost.transform.rotation = selectedObject.transform.rotation;
            moveGhost.transform.localScale = selectedObject.transform.localScale;
        }

        private void DestroyMoveGhost()
        {
            if (moveGhost != null)
            {
                Destroy(moveGhost);
                moveGhost = null;
            }
        }

        private bool TryGetHandPointerRay(XRNode node, out Ray ray)
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(node);
            if (device.isValid
                && device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position)
                && device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
            {
                ray = new Ray(position, rotation * Vector3.forward);
                return true;
            }

            ray = default;
            return false;
        }

        private RuntimeButtons ReadButtons(XRNode node)
        {
            RuntimeButtons buttons = default;
            InputDevice device = InputDevices.GetDeviceAtXRNode(node);
            if (!device.isValid)
            {
                return buttons;
            }

            device.TryGetFeatureValue(CommonUsages.triggerButton, out buttons.triggerPressed);
            device.TryGetFeatureValue(CommonUsages.gripButton, out buttons.gripPressed);
            device.TryGetFeatureValue(CommonUsages.primaryButton, out buttons.primaryPressed);
            device.TryGetFeatureValue(CommonUsages.secondaryButton, out buttons.secondaryPressed);
            device.TryGetFeatureValue(CommonUsages.primary2DAxis, out buttons.axis);
            buttons.menuAvailable = device.TryGetFeatureValue(CommonUsages.menuButton, out buttons.menuPressed);
            if (!buttons.menuAvailable)
            {
                buttons.menuPressed = buttons.secondaryPressed;
            }

            return buttons;
        }

        private bool TryGetPlacementPoint(Ray ray, out Vector3 point, out RaycastHit hit)
        {
            RaycastHit[] hits = Physics.RaycastAll(ray, maxRayDistance, placementMask, QueryTriggerInteraction.Ignore);
            if (hits != null && hits.Length > 0)
            {
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                for (int i = 0; i < hits.Length; i++)
                {
                    Collider collider = hits[i].collider;
                    if (collider == null || IsRuntimeUiCollider(collider))
                    {
                        continue;
                    }

                    if (selectedObject != null
                        && (collider.transform == selectedObject.transform
                            || collider.transform.IsChildOf(selectedObject.transform)))
                    {
                        continue;
                    }

                    hit = hits[i];
                    point = hit.point;
                    return true;
                }
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

        private Vector3 GetPlacementPosition(Vector3 hitPoint, RaycastHit hit)
        {
            return ShouldSnapPlacement(hit) ? SnapToGrid(hitPoint) : hitPoint;
        }

        private bool ShouldSnapPlacement(RaycastHit hit)
        {
            if (hit.collider == null)
            {
                return true;
            }

            string objectName = hit.collider.gameObject.name.ToLowerInvariant();
            string tagName = hit.collider.tag.ToLowerInvariant();
            return objectName.Contains("ground")
                || objectName.Contains("grid")
                || tagName.Contains("ground")
                || tagName.Contains("grid");
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

        private void SetActiveHand(MapEditorHand hand)
        {
            bool changed = activeHand != hand;
            activeHand = hand;
            HandState state = GetHandState(hand);
            activeBrushIndex = state.BrushIndex;
            editMode = state.EditMode;
            if (changed)
            {
                ActiveHandChanged?.Invoke(activeHand);
                BrushChanged?.Invoke(activeBrushIndex);
                EditModeChanged?.Invoke(editMode);
            }
        }

        private void UpdateDragging(bool value)
        {
            if (isDragging == value)
            {
                return;
            }

            isDragging = value;
            DraggingChanged?.Invoke(isDragging);
        }

        private MapEditorRuntimeBrush GetBrush(int index)
        {
            if (index < 0 || index >= brushes.Count)
            {
                return null;
            }

            return brushes[index];
        }

        private HandState GetHandState(MapEditorHand hand)
        {
            return hand == MapEditorHand.Left ? leftHand : rightHand;
        }

        private static string HandLabel(MapEditorHand hand)
        {
            return hand == MapEditorHand.Left ? "Left hand" : "Right hand";
        }

        private static bool IsRuntimeUiCollider(Collider collider)
        {
            return collider.GetComponentInParent<MapEditorRuntimeUiHitTarget>() != null
                || collider.GetComponentInParent<MapEditorDockedPanel>() != null;
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

        private sealed class HandState
        {
            public readonly MapEditorHand Hand;
            public readonly XRNode Node;
            public int BrushIndex = -1;
            public MapEditorRuntimeEditMode EditMode = MapEditorRuntimeEditMode.Place;
            public RuntimeButtons PreviousButtons;
            public float MenuDownTime;
            public bool MenuLongPressFired;

            public HandState(MapEditorHand hand, XRNode node)
            {
                Hand = hand;
                Node = node;
            }
        }

        private struct RuntimeButtons
        {
            public bool triggerPressed;
            public bool gripPressed;
            public bool primaryPressed;
            public bool secondaryPressed;
            public bool menuPressed;
            public bool menuAvailable;
            public Vector2 axis;
        }
    }
}
