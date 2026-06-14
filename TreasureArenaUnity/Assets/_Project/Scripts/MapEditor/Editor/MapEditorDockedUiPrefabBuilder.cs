using System.IO;
using TreasureArenaMR.Map;
using TreasureArenaMR.Shared;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace TreasureArenaMR.MapEditor.Editor
{
    public static class MapEditorDockedUiPrefabBuilder
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/MapEditor/UI/MapEditorDockedCanvas.prefab";

        private static readonly Color PanelColor = new Color(0.055f, 0.065f, 0.075f, 0.82f);
        private static readonly Color MainPanelColor = new Color(0.065f, 0.075f, 0.085f, 0.86f);
        private static readonly Color ButtonColor = new Color(0.18f, 0.34f, 0.58f, 0.96f);
        private static readonly Color DangerColor = new Color(0.72f, 0.14f, 0.12f, 0.96f);
        private static readonly Color FieldColor = new Color(0.92f, 0.94f, 0.96f, 0.96f);
        private static readonly Color CardColor = new Color(0.12f, 0.17f, 0.22f, 0.96f);

        [MenuItem("Tools/TreasureArena/地图编辑器/创建 Docked UI Prefab")]
        public static GameObject CreateOrUpdatePrefab()
        {
            string folder = Path.GetDirectoryName(PrefabPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
            {
                Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
            }

            GameObject root = new GameObject("MapEditorDockedCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(TrackedDeviceGraphicRaycaster), typeof(MapEditorDockedUiController));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(1200f, 560f);
            root.transform.localScale = Vector3.one * 0.001f;

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            root.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 16f;

            MapEditorValidationPresenter presenter = root.AddComponent<MapEditorValidationPresenter>();

            MapEditorDockedPanel leftPanel = CreatePanel(root.transform, "LeftInfoPanel", "Map", new Vector2(0.5f, 0.5f), new Vector2(280f, 520f), new Vector2(-450f, 0f), PanelColor, out Transform leftExpanded);
            MapEditorDockedPanel mainPanel = CreatePanel(root.transform, "MainBrushPanel", "Brushes", new Vector2(0.5f, 0.5f), new Vector2(520f, 520f), Vector2.zero, MainPanelColor, out Transform mainExpanded);
            MapEditorDockedPanel rightPanel = CreatePanel(root.transform, "RightInspectorPanel", "Inspector", new Vector2(0.5f, 0.5f), new Vector2(340f, 520f), new Vector2(430f, 0f), PanelColor, out Transform rightExpanded);
            CreatePanelMoveBar(root.transform);

            Text mapNameText = CreateText(leftExpanded, "MapNameText", "Map: new_map", 15, new Vector2(0f, 200f), new Vector2(230f, 24f), TextAnchor.MiddleLeft);
            Text mapIdText = CreateText(leftExpanded, "MapIdText", "map_id: new_map", 13, new Vector2(0f, 174f), new Vector2(230f, 22f), TextAnchor.MiddleLeft);
            Text mapVersionText = CreateText(leftExpanded, "MapVersionText", "map_version: 1.0.0", 13, new Vector2(0f, 150f), new Vector2(230f, 22f), TextAnchor.MiddleLeft);
            Text modeText = CreateText(leftExpanded, "ModeText", "Mode: Place", 13, new Vector2(0f, 118f), new Vector2(230f, 22f), TextAnchor.MiddleLeft);
            Text statusText = CreateText(leftExpanded, "StatusText", "Validation: Not validated", 13, new Vector2(0f, 94f), new Vector2(230f, 22f), TextAnchor.MiddleLeft);
            Text countText = CreateText(leftExpanded, "CountText", "Objects: 0", 13, new Vector2(0f, 34f), new Vector2(230f, 86f), TextAnchor.UpperLeft);

            Button saveButton = CreateButton(leftExpanded, "SaveButton", "Save", new Vector2(-62f, -42f), new Vector2(104f, 38f), ButtonColor);
            Button loadButton = CreateButton(leftExpanded, "LoadButton", "Load", new Vector2(62f, -42f), new Vector2(104f, 38f), ButtonColor);
            Button validateButton = CreateButton(leftExpanded, "ValidateButton", "Validate", new Vector2(-62f, -86f), new Vector2(104f, 38f), ButtonColor);
            Button exportButton = CreateButton(leftExpanded, "ExportButton", "Export JSON", new Vector2(62f, -86f), new Vector2(104f, 38f), ButtonColor);
            Button previewButton = CreateButton(leftExpanded, "PreviewButton", "Preview", new Vector2(-62f, -130f), new Vector2(104f, 38f), ButtonColor);
            Button clearMapButton = CreateButton(leftExpanded, "ClearMapButton", "Clear Map", new Vector2(62f, -130f), new Vector2(104f, 38f), DangerColor);
            Button mapSettingsButton = CreateButton(leftExpanded, "MapSettingsButton", "Map Settings", new Vector2(0f, -174f), new Vector2(228f, 38f), ButtonColor);
            Text recentLogText = CreateText(leftExpanded, "RecentLogText", "Ready", 12, new Vector2(0f, -216f), new Vector2(230f, 28f), TextAnchor.UpperLeft);
            Text errorListText = CreateText(leftExpanded, "ErrorListText", "", 11, new Vector2(0f, -244f), new Vector2(230f, 40f), TextAnchor.UpperLeft);

            Text leftHandBrushText = CreateText(mainExpanded, "LeftHandBrushText", "Left Hand: None", 13, new Vector2(-126f, 200f), new Vector2(230f, 22f), TextAnchor.MiddleLeft);
            Text rightHandBrushText = CreateText(mainExpanded, "RightHandBrushText", "Right Hand: None", 13, new Vector2(126f, 200f), new Vector2(230f, 22f), TextAnchor.MiddleLeft);
            Text activeHandText = CreateText(mainExpanded, "ActiveHandText", "Active: Right", 13, new Vector2(-126f, 176f), new Vector2(230f, 22f), TextAnchor.MiddleLeft);
            Text hintText = CreateText(mainExpanded, "HintText", "Hint: Aim at a surface and press Trigger.", 12, new Vector2(126f, 176f), new Vector2(230f, 22f), TextAnchor.MiddleLeft);

            CreateText(mainExpanded, "MapObjectsLabel", "Map Objects", 14, new Vector2(-126f, 144f), new Vector2(230f, 22f), TextAnchor.MiddleLeft);
            CreateText(mainExpanded, "GameplayMarkersLabel", "Gameplay Markers", 14, new Vector2(126f, 144f), new Vector2(230f, 22f), TextAnchor.MiddleLeft);
            Transform mapObjectsList = CreateScrollListRoot(mainExpanded, "MapObjectsList", new Vector2(-126f, 8f), new Vector2(230f, 252f));
            Transform gameplayMarkersList = CreateScrollListRoot(mainExpanded, "GameplayMarkersList", new Vector2(126f, 8f), new Vector2(230f, 252f));
            Button brushButtonTemplate = CreateBrushCardButton(mainExpanded, "BrushButtonTemplate", new Vector2(0f, -250f), new Vector2(220f, 76f));
            brushButtonTemplate.gameObject.SetActive(false);

            Button placeButton = CreateButton(mainExpanded, "PlaceButton", "Place", new Vector2(-204f, -156f), new Vector2(92f, 34f), ButtonColor);
            Button moveButton = CreateButton(mainExpanded, "MoveButton", "Move", new Vector2(-102f, -156f), new Vector2(92f, 34f), ButtonColor);
            Button rotateButton = CreateButton(mainExpanded, "RotateButton", "Rotate", new Vector2(0f, -156f), new Vector2(92f, 34f), ButtonColor);
            Button scaleButton = CreateButton(mainExpanded, "ScaleButton", "Scale", new Vector2(102f, -156f), new Vector2(92f, 34f), ButtonColor);
            Button deleteButton = CreateButton(mainExpanded, "DeleteButton", "Delete", new Vector2(204f, -156f), new Vector2(92f, 34f), DangerColor);
            Button calibrateFloorButton = CreateButton(mainExpanded, "CalibrateFloorButton", "Cal Floor", new Vector2(-126f, -198f), new Vector2(116f, 34f), ButtonColor);
            Toggle gridSnapToggle = CreateToggle(mainExpanded, "GridSnapToggle", "Grid Snap", new Vector2(142f, -198f), new Vector2(120f, 28f));
            Button gridStep01Button = CreateButton(mainExpanded, "GridStep01Button", "0.1", new Vector2(-174f, -236f), new Vector2(60f, 28f), ButtonColor);
            Button gridStep025Button = CreateButton(mainExpanded, "GridStep025Button", "0.25", new Vector2(-106f, -236f), new Vector2(60f, 28f), ButtonColor);
            Button gridStep05Button = CreateButton(mainExpanded, "GridStep05Button", "0.5", new Vector2(-38f, -236f), new Vector2(60f, 28f), ButtonColor);
            Button gridStep1Button = CreateButton(mainExpanded, "GridStep1Button", "1", new Vector2(30f, -236f), new Vector2(60f, 28f), ButtonColor);
            Text gridStepText = CreateText(mainExpanded, "GridStepText", "Grid 0.5m On", 12, new Vector2(110f, -236f), new Vector2(96f, 22f), TextAnchor.MiddleLeft);
            Text rotateStepText = CreateText(mainExpanded, "RotateStepText", "Rot 15", 12, new Vector2(190f, -228f), new Vector2(80f, 18f), TextAnchor.MiddleLeft);
            Text scaleStepText = CreateText(mainExpanded, "ScaleStepText", "Scale 0.1", 12, new Vector2(190f, -248f), new Vector2(80f, 18f), TextAnchor.MiddleLeft);

            MapEditorObjectInspector inspector = rightPanel.gameObject.AddComponent<MapEditorObjectInspector>();
            Text selectedNameText = CreateText(rightExpanded, "SelectedNameText", "Selected: none", 14, new Vector2(0f, 200f), new Vector2(280f, 24f), TextAnchor.MiddleLeft);
            Text selectedTypeText = CreateText(rightExpanded, "SelectedTypeText", "Type: none", 13, new Vector2(0f, 176f), new Vector2(280f, 22f), TextAnchor.MiddleLeft);
            InputField idField = CreateInputField(rightExpanded, "ObjectIdField", "object_id", new Vector2(0f, 140f), new Vector2(280f, 32f));
            InputField prefabField = CreateInputField(rightExpanded, "PrefabIdField", "prefab_id", new Vector2(0f, 104f), new Vector2(280f, 32f));
            InputField posX = CreateInputField(rightExpanded, "PositionX", "pos x", new Vector2(-96f, 64f), new Vector2(86f, 32f));
            InputField posY = CreateInputField(rightExpanded, "PositionY", "y", new Vector2(0f, 64f), new Vector2(86f, 32f));
            InputField posZ = CreateInputField(rightExpanded, "PositionZ", "z", new Vector2(96f, 64f), new Vector2(86f, 32f));
            InputField rotX = CreateInputField(rightExpanded, "RotationX", "rot x", new Vector2(-96f, 26f), new Vector2(86f, 32f));
            InputField rotY = CreateInputField(rightExpanded, "RotationY", "y", new Vector2(0f, 26f), new Vector2(86f, 32f));
            InputField rotZ = CreateInputField(rightExpanded, "RotationZ", "z", new Vector2(96f, 26f), new Vector2(86f, 32f));
            InputField scaleX = CreateInputField(rightExpanded, "ScaleX", "scale x", new Vector2(-96f, -12f), new Vector2(86f, 32f));
            InputField scaleY = CreateInputField(rightExpanded, "ScaleY", "y", new Vector2(0f, -12f), new Vector2(86f, 32f));
            InputField scaleZ = CreateInputField(rightExpanded, "ScaleZ", "z", new Vector2(96f, -12f), new Vector2(86f, 32f));
            Toggle colliderToggle = CreateToggle(rightExpanded, "HasColliderToggle", "has collider", new Vector2(-78f, -52f), new Vector2(134f, 28f));
            Dropdown teamDropdown = CreateDropdown(rightExpanded, "TeamDropdown", "team", new Vector2(78f, -52f), new Vector2(134f, 32f), new[] { TeamType.None.ToString(), TeamType.Red.ToString(), TeamType.Blue.ToString() });
            Dropdown treasureDropdown = CreateDropdown(rightExpanded, "TreasureDropdown", "treasure", new Vector2(0f, -90f), new Vector2(280f, 32f), new[] { TreasureType.Normal.ToString(), TreasureType.Rare.ToString(), TreasureType.Final.ToString() });
            InputField radiusField = CreateInputField(rightExpanded, "RadiusField", "radius", new Vector2(-78f, -128f), new Vector2(134f, 32f));
            InputField supplyField = CreateInputField(rightExpanded, "SupplyTypeField", "supply", new Vector2(78f, -128f), new Vector2(134f, 32f));
            InputField refreshField = CreateInputField(rightExpanded, "RefreshIntervalField", "refresh", new Vector2(0f, -166f), new Vector2(280f, 32f));
            Dropdown objectTypeDropdown = CreateDropdown(rightExpanded, "ObjectTypeDropdown", "object_type", new Vector2(-78f, -200f), new Vector2(134f, 28f), new[] { "StaticFloor", "StaticObstacle", "PhysicsProp", "OpenableObject" });
            Dropdown interactionTypeDropdown = CreateDropdown(rightExpanded, "InteractionTypeDropdown", "interaction", new Vector2(78f, -200f), new Vector2(134f, 28f), new[] { "None", "Grab", "Openable" });
            Toggle movableToggle = CreateToggle(rightExpanded, "MovableToggle", "movable", new Vector2(-104f, -232f), new Vector2(92f, 24f));
            Toggle grabbableToggle = CreateToggle(rightExpanded, "GrabbableToggle", "grab", new Vector2(-8f, -232f), new Vector2(82f, 24f));
            Toggle openableToggle = CreateToggle(rightExpanded, "OpenableToggle", "open", new Vector2(82f, -232f), new Vector2(82f, 24f));
            Toggle shootableToggle = CreateToggle(rightExpanded, "ShootableToggle", "shoot", new Vector2(-104f, -258f), new Vector2(92f, 24f));
            Toggle blocksBulletToggle = CreateToggle(rightExpanded, "BlocksBulletToggle", "block", new Vector2(-8f, -258f), new Vector2(82f, 24f));
            Toggle decalEnabledToggle = CreateToggle(rightExpanded, "DecalEnabledToggle", "decal", new Vector2(82f, -258f), new Vector2(82f, 24f));
            InputField massField = CreateInputField(rightExpanded, "MassField", "mass", new Vector2(0f, -286f), new Vector2(280f, 26f));
            Button duplicateButton = CreateButton(rightExpanded, "DuplicateButton", "Duplicate", new Vector2(-96f, -318f), new Vector2(88f, 28f), ButtonColor);
            Button inspectorDeleteButton = CreateButton(rightExpanded, "InspectorDeleteButton", "Delete", new Vector2(0f, -318f), new Vector2(88f, 28f), DangerColor);
            Button resetTransformButton = CreateButton(rightExpanded, "ResetTransformButton", "Reset", new Vector2(96f, -318f), new Vector2(88f, 28f), ButtonColor);
            Button nudgeXMinusButton = CreateButton(rightExpanded, "NudgeXMinusButton", "X-", new Vector2(-120f, -350f), new Vector2(48f, 24f), ButtonColor);
            Button nudgeXPlusButton = CreateButton(rightExpanded, "NudgeXPlusButton", "X+", new Vector2(-66f, -350f), new Vector2(48f, 24f), ButtonColor);
            Button nudgeYMinusButton = CreateButton(rightExpanded, "NudgeYMinusButton", "Y-", new Vector2(-12f, -350f), new Vector2(48f, 24f), ButtonColor);
            Button nudgeYPlusButton = CreateButton(rightExpanded, "NudgeYPlusButton", "Y+", new Vector2(42f, -350f), new Vector2(48f, 24f), ButtonColor);
            Button nudgeZMinusButton = CreateButton(rightExpanded, "NudgeZMinusButton", "Z-", new Vector2(96f, -350f), new Vector2(48f, 24f), ButtonColor);
            Button nudgeZPlusButton = CreateButton(rightExpanded, "NudgeZPlusButton", "Z+", new Vector2(150f, -350f), new Vector2(48f, 24f), ButtonColor);
            Button rotateMinusButton = CreateButton(rightExpanded, "RotateMinusButton", "R-", new Vector2(-120f, -378f), new Vector2(48f, 24f), ButtonColor);
            Button rotatePlusButton = CreateButton(rightExpanded, "RotatePlusButton", "R+", new Vector2(-66f, -378f), new Vector2(48f, 24f), ButtonColor);
            Button scaleMinusButton = CreateButton(rightExpanded, "ScaleMinusButton", "S-", new Vector2(-12f, -378f), new Vector2(48f, 24f), ButtonColor);
            Button scalePlusButton = CreateButton(rightExpanded, "ScalePlusButton", "S+", new Vector2(42f, -378f), new Vector2(48f, 24f), ButtonColor);
            Button snapToFloorButton = CreateButton(rightExpanded, "SnapToFloorButton", "Floor", new Vector2(104f, -378f), new Vector2(64f, 24f), ButtonColor);
            Button resetRotationButton = CreateButton(rightExpanded, "ResetRotationButton", "Rot 0", new Vector2(176f, -378f), new Vector2(64f, 24f), ButtonColor);

            BindPresenter(presenter, recentLogText, errorListText);
            BindInspector(inspector, selectedNameText, selectedTypeText, idField, prefabField, posX, posY, posZ, rotX, rotY, rotZ, scaleX, scaleY, scaleZ, colliderToggle, objectTypeDropdown, interactionTypeDropdown, movableToggle, grabbableToggle, openableToggle, shootableToggle, blocksBulletToggle, decalEnabledToggle, massField, teamDropdown, treasureDropdown, radiusField, supplyField, refreshField, duplicateButton, inspectorDeleteButton, resetTransformButton);
            BindController(root.GetComponent<MapEditorDockedUiController>(), leftPanel, mainPanel, rightPanel, mapNameText, mapIdText, mapVersionText, modeText, countText, statusText, validateButton, exportButton, saveButton, loadButton, previewButton, clearMapButton, mapSettingsButton, mapObjectsList, gameplayMarkersList, brushButtonTemplate, leftHandBrushText, rightHandBrushText, activeHandText, hintText, placeButton, moveButton, rotateButton, scaleButton, calibrateFloorButton, deleteButton, null, gridSnapToggle, gridStep01Button, gridStep025Button, gridStep05Button, gridStep1Button, nudgeXPlusButton, nudgeXMinusButton, nudgeYPlusButton, nudgeYMinusButton, nudgeZPlusButton, nudgeZMinusButton, rotatePlusButton, rotateMinusButton, scalePlusButton, scaleMinusButton, snapToFloorButton, resetRotationButton, gridStepText, rotateStepText, scaleStepText, presenter, inspector);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return prefab;
        }

        private static MapEditorDockedPanel CreatePanel(Transform parent, string name, string title, Vector2 anchor, Vector2 size, Vector2 position, Color color, out Transform expandedRoot)
        {
            GameObject panelGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(BoxCollider), typeof(MapEditorRuntimeUiHitTarget), typeof(MapEditorDockedPanel));
            panelGo.transform.SetParent(parent, false);
            RectTransform rect = panelGo.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            panelGo.GetComponent<Image>().color = color;
            SetBoxCollider(panelGo, size);

            GameObject expanded = CreateRect(panelGo.transform, "Expanded", Vector2.zero, size);
            expandedRoot = expanded.transform;
            GameObject collapsed = CreateRect(panelGo.transform, "Collapsed", Vector2.zero, new Vector2(Mathf.Min(size.x, 240f), 42f));
            collapsed.SetActive(false);

            Text expandedTitle = CreateText(expanded.transform, "Title", title, 16, new Vector2(-size.x * 0.5f + 58f, size.y * 0.5f - 24f), new Vector2(120f, 28f), TextAnchor.MiddleLeft);
            Image expandedErrorDot = CreateDot(expanded.transform, "ExpandedErrorDot", new Vector2(size.x * 0.5f - 108f, size.y * 0.5f - 24f));

            SerializedObject panel = new SerializedObject(panelGo.GetComponent<MapEditorDockedPanel>());
            panel.FindProperty("panelTitle").stringValue = title;
            panel.FindProperty("canvasGroup").objectReferenceValue = panelGo.GetComponent<CanvasGroup>();
            panel.FindProperty("expandedRoot").objectReferenceValue = expanded;
            panel.FindProperty("collapsedRoot").objectReferenceValue = collapsed;
            panel.FindProperty("titleText").objectReferenceValue = expandedTitle;
            panel.FindProperty("collapsedTitleText").objectReferenceValue = null;
            panel.FindProperty("errorDot").objectReferenceValue = expandedErrorDot;
            panel.FindProperty("collapsedErrorDot").objectReferenceValue = null;
            panel.FindProperty("collapseButton").objectReferenceValue = null;
            panel.FindProperty("expandButton").objectReferenceValue = null;
            panel.FindProperty("pinButton").objectReferenceValue = null;
            panel.FindProperty("pinText").objectReferenceValue = null;
            panel.ApplyModifiedPropertiesWithoutUndo();
            return panelGo.GetComponent<MapEditorDockedPanel>();
        }

        private static GameObject CreateRect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return go;
        }

        private static Transform CreateScrollListRoot(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject scrollGo = CreateRect(parent, name + "Scroll", position, size);
            Image scrollImage = scrollGo.AddComponent<Image>();
            scrollImage.color = new Color(0f, 0f, 0f, 0.14f);
            ScrollRect scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            SetBoxCollider(scrollGo, size);
            scrollGo.AddComponent<MapEditorRuntimeUiHitTarget>();

            GameObject viewportGo = CreateRect(scrollGo.transform, "Viewport", Vector2.zero, size);
            Image viewportImage = viewportGo.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
            viewportGo.AddComponent<Mask>().showMaskGraphic = false;
            RectTransform viewportRect = viewportGo.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            GameObject contentGo = CreateRect(viewportGo.transform, name, Vector2.zero, new Vector2(size.x - 8f, size.y));
            RectTransform contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.offsetMin = new Vector2(4f, contentRect.offsetMin.y);
            contentRect.offsetMax = new Vector2(-4f, 0f);

            VerticalLayoutGroup layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 8f;
            ContentSizeFitter fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            return contentGo.transform;
        }

        private static Button CreateBrushCardButton(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(BoxCollider), typeof(MapEditorRuntimeUiHitTarget));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            go.GetComponent<Image>().color = CardColor;
            Button button = go.GetComponent<Button>();
            Image thumb = CreateImage(go.transform, "Thumbnail", new Vector2(-size.x * 0.5f + 30f, 0f), new Vector2(42f, 42f), new Color(0.25f, 0.37f, 0.45f, 1f));
            thumb.raycastTarget = false;
            Text nameText = CreateText(go.transform, "NameText", "Brush", 13, new Vector2(32f, 12f), new Vector2(size.x - 76f, 24f), TextAnchor.MiddleLeft);
            nameText.raycastTarget = false;
            Text typeText = CreateText(go.transform, "TypeText", "MapObject", 10, new Vector2(32f, -14f), new Vector2(size.x - 76f, 20f), TextAnchor.MiddleLeft);
            typeText.color = new Color(0.8f, 0.88f, 0.95f, 1f);
            typeText.raycastTarget = false;
            Text handText = CreateText(go.transform, "HandMarkerText", "", 16, new Vector2(size.x * 0.5f - 22f, size.y * 0.5f - 20f), new Vector2(34f, 28f), TextAnchor.MiddleCenter);
            handText.color = new Color(0.1f, 1f, 0.85f, 1f);
            handText.raycastTarget = false;
            SetBoxCollider(go, size);
            return button;
        }

        private static Text CreateText(Transform parent, string name, string text, int fontSize, Vector2 position, Vector2 size, TextAnchor anchor)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            Text label = go.GetComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.color = Color.white;
            label.alignment = anchor;
            return label;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(BoxCollider), typeof(MapEditorRuntimeUiHitTarget));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            go.GetComponent<Image>().color = color;
            Button button = go.GetComponent<Button>();
            Text text = CreateText(go.transform, "Text", label, 13, Vector2.zero, size, TextAnchor.MiddleCenter);
            text.raycastTarget = false;
            SetBoxCollider(go, size);
            return button;
        }

        private static InputField CreateInputField(Transform parent, string name, string label, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField), typeof(BoxCollider), typeof(MapEditorRuntimeUiHitTarget));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            go.GetComponent<Image>().color = FieldColor;
            InputField input = go.GetComponent<InputField>();
            Text text = CreateText(go.transform, "Text", "", 13, Vector2.zero, new Vector2(size.x - 10f, size.y), TextAnchor.MiddleLeft);
            text.color = Color.black;
            Text placeholder = CreateText(go.transform, "Placeholder", label, 13, Vector2.zero, new Vector2(size.x - 10f, size.y), TextAnchor.MiddleLeft);
            placeholder.color = new Color(0.35f, 0.38f, 0.42f, 0.75f);
            input.textComponent = text;
            input.placeholder = placeholder;
            SetBoxCollider(go, size);
            return input;
        }

        private static Toggle CreateToggle(Transform parent, string name, string label, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Toggle), typeof(BoxCollider), typeof(MapEditorRuntimeUiHitTarget));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            Image background = CreateImage(go.transform, "Background", new Vector2(-size.x * 0.5f + 12f, 0f), new Vector2(22f, 22f), FieldColor);
            Image check = CreateImage(background.transform, "Checkmark", Vector2.zero, new Vector2(14f, 14f), ButtonColor);
            Text text = CreateText(go.transform, "Label", label, 12, new Vector2(16f, 0f), new Vector2(size.x - 34f, size.y), TextAnchor.MiddleLeft);
            text.raycastTarget = false;
            Toggle toggle = go.GetComponent<Toggle>();
            toggle.targetGraphic = background;
            toggle.graphic = check;
            SetBoxCollider(go, size);
            return toggle;
        }

        private static Dropdown CreateDropdown(Transform parent, string name, string label, Vector2 position, Vector2 size, string[] options)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Dropdown), typeof(BoxCollider), typeof(MapEditorRuntimeUiHitTarget));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            go.GetComponent<Image>().color = FieldColor;
            Dropdown dropdown = go.GetComponent<Dropdown>();
            Text caption = CreateText(go.transform, "Caption", label, 13, Vector2.zero, new Vector2(size.x - 10f, size.y), TextAnchor.MiddleLeft);
            caption.color = Color.black;
            dropdown.captionText = caption;
            RectTransform template = CreateDropdownTemplate(go.transform, size.x);
            dropdown.template = template;
            dropdown.itemText = template.Find("Viewport/Content/Item/Item Label")?.GetComponent<Text>();
            dropdown.options.Clear();
            for (int i = 0; i < options.Length; i++)
            {
                dropdown.options.Add(new Dropdown.OptionData(options[i]));
            }
            SetBoxCollider(go, size);
            return dropdown;
        }

        private static RectTransform CreateDropdownTemplate(Transform parent, float width)
        {
            GameObject templateGo = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            templateGo.transform.SetParent(parent, false);
            templateGo.SetActive(false);
            RectTransform template = templateGo.GetComponent<RectTransform>();
            template.sizeDelta = new Vector2(width, 112f);
            template.anchoredPosition = new Vector2(0f, -72f);
            template.pivot = new Vector2(0.5f, 1f);
            templateGo.GetComponent<Image>().color = FieldColor;
            ScrollRect scroll = templateGo.GetComponent<ScrollRect>();

            GameObject viewportGo = CreateRect(templateGo.transform, "Viewport", Vector2.zero, new Vector2(width, 108f));
            viewportGo.AddComponent<Image>().color = FieldColor;
            viewportGo.AddComponent<Mask>().showMaskGraphic = false;
            RectTransform viewport = viewportGo.GetComponent<RectTransform>();

            GameObject contentGo = CreateRect(viewportGo.transform, "Content", Vector2.zero, new Vector2(width, 108f));
            RectTransform content = contentGo.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);

            GameObject itemGo = CreateRect(contentGo.transform, "Item", new Vector2(0f, -18f), new Vector2(width, 24f));
            Toggle itemToggle = itemGo.AddComponent<Toggle>();
            Image itemBackground = CreateImage(itemGo.transform, "Item Background", Vector2.zero, new Vector2(width, 24f), Color.white);
            Image itemCheck = CreateImage(itemGo.transform, "Item Checkmark", new Vector2(10f, 0f), new Vector2(14f, 14f), ButtonColor);
            Text itemLabel = CreateText(itemGo.transform, "Item Label", "Option", 13, new Vector2(20f, 0f), new Vector2(width - 34f, 24f), TextAnchor.MiddleLeft);
            itemLabel.color = Color.black;
            itemToggle.targetGraphic = itemBackground;
            itemToggle.graphic = itemCheck;

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return template;
        }

        private static Image CreateImage(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            Image image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Image CreateDot(Transform parent, string name, Vector2 position)
        {
            Image dot = CreateImage(parent, name, position, new Vector2(12f, 12f), DangerColor);
            dot.gameObject.SetActive(false);
            return dot;
        }

        private static MapEditorPanelMoveBar CreatePanelMoveBar(Transform parent)
        {
            GameObject go = new GameObject("PanelMoveBar", typeof(RectTransform), typeof(Image), typeof(BoxCollider), typeof(MapEditorRuntimeUiHitTarget), typeof(MapEditorPanelMoveBar));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(520f, 28f);
            rect.anchoredPosition = new Vector2(0f, -286f);

            Image image = go.GetComponent<Image>();
            image.color = new Color(0.10f, 0.16f, 0.22f, 0.95f);

            BoxCollider collider = go.GetComponent<BoxCollider>();
            collider.size = new Vector3(rect.sizeDelta.x, rect.sizeDelta.y, 8f);
            collider.center = Vector3.zero;

            Text label = CreateText(go.transform, "PanelMoveBarLabel", "Move Panel", 12, Vector2.zero, new Vector2(180f, 22f), TextAnchor.MiddleCenter);
            label.color = new Color(0.85f, 0.94f, 1f, 1f);
            return go.GetComponent<MapEditorPanelMoveBar>();
        }

        private static void SetBoxCollider(GameObject go, Vector2 size)
        {
            BoxCollider collider = go.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = go.AddComponent<BoxCollider>();
            }
            collider.size = new Vector3(size.x, size.y, 8f);
            collider.center = Vector3.zero;
        }

        private static void BindPresenter(MapEditorValidationPresenter presenter, Text recentLogText, Text errorListText)
        {
            SerializedObject serialized = new SerializedObject(presenter);
            serialized.FindProperty("recentLogText").objectReferenceValue = recentLogText;
            serialized.FindProperty("errorListText").objectReferenceValue = errorListText;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BindInspector(MapEditorObjectInspector inspector, Text selectedNameText, Text selectedTypeText, InputField idField, InputField prefabField, InputField posX, InputField posY, InputField posZ, InputField rotX, InputField rotY, InputField rotZ, InputField scaleX, InputField scaleY, InputField scaleZ, Toggle colliderToggle, Dropdown objectTypeDropdown, Dropdown interactionTypeDropdown, Toggle movableToggle, Toggle grabbableToggle, Toggle openableToggle, Toggle shootableToggle, Toggle blocksBulletToggle, Toggle decalEnabledToggle, InputField massField, Dropdown teamDropdown, Dropdown treasureDropdown, InputField radiusField, InputField supplyField, InputField refreshField, Button duplicateButton, Button deleteButton, Button resetTransformButton)
        {
            SerializedObject serialized = new SerializedObject(inspector);
            serialized.FindProperty("selectedNameText").objectReferenceValue = selectedNameText;
            serialized.FindProperty("selectedTypeText").objectReferenceValue = selectedTypeText;
            serialized.FindProperty("idInput").objectReferenceValue = idField;
            serialized.FindProperty("prefabIdInput").objectReferenceValue = prefabField;
            serialized.FindProperty("positionXInput").objectReferenceValue = posX;
            serialized.FindProperty("positionYInput").objectReferenceValue = posY;
            serialized.FindProperty("positionZInput").objectReferenceValue = posZ;
            serialized.FindProperty("rotationXInput").objectReferenceValue = rotX;
            serialized.FindProperty("rotationYInput").objectReferenceValue = rotY;
            serialized.FindProperty("rotationZInput").objectReferenceValue = rotZ;
            serialized.FindProperty("scaleXInput").objectReferenceValue = scaleX;
            serialized.FindProperty("scaleYInput").objectReferenceValue = scaleY;
            serialized.FindProperty("scaleZInput").objectReferenceValue = scaleZ;
            serialized.FindProperty("hasColliderToggle").objectReferenceValue = colliderToggle;
            serialized.FindProperty("objectTypeDropdown").objectReferenceValue = objectTypeDropdown;
            serialized.FindProperty("interactionTypeDropdown").objectReferenceValue = interactionTypeDropdown;
            serialized.FindProperty("movableToggle").objectReferenceValue = movableToggle;
            serialized.FindProperty("grabbableToggle").objectReferenceValue = grabbableToggle;
            serialized.FindProperty("openableToggle").objectReferenceValue = openableToggle;
            serialized.FindProperty("shootableToggle").objectReferenceValue = shootableToggle;
            serialized.FindProperty("blocksBulletToggle").objectReferenceValue = blocksBulletToggle;
            serialized.FindProperty("decalEnabledToggle").objectReferenceValue = decalEnabledToggle;
            serialized.FindProperty("massInput").objectReferenceValue = massField;
            serialized.FindProperty("teamDropdown").objectReferenceValue = teamDropdown;
            serialized.FindProperty("treasureTypeDropdown").objectReferenceValue = treasureDropdown;
            serialized.FindProperty("radiusInput").objectReferenceValue = radiusField;
            serialized.FindProperty("supplyTypeInput").objectReferenceValue = supplyField;
            serialized.FindProperty("refreshIntervalInput").objectReferenceValue = refreshField;
            serialized.FindProperty("duplicateButton").objectReferenceValue = duplicateButton;
            serialized.FindProperty("deleteButton").objectReferenceValue = deleteButton;
            serialized.FindProperty("resetTransformButton").objectReferenceValue = resetTransformButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BindController(MapEditorDockedUiController controller, MapEditorDockedPanel leftPanel, MapEditorDockedPanel mainPanel, MapEditorDockedPanel rightPanel, Text mapNameText, Text mapIdText, Text mapVersionText, Text modeText, Text countText, Text statusText, Button validateButton, Button exportButton, Button saveButton, Button loadButton, Button previewButton, Button clearMapButton, Button mapSettingsButton, Transform mapObjectsList, Transform gameplayMarkersList, Button brushButtonTemplate, Text leftHandBrushText, Text rightHandBrushText, Text activeHandText, Text hintText, Button placeButton, Button moveButton, Button rotateButton, Button scaleButton, Button calibrateFloorButton, Button deleteButton, Button clearBrushButton, Toggle gridSnapToggle, Button gridStep01Button, Button gridStep025Button, Button gridStep05Button, Button gridStep1Button, Button nudgeXPlusButton, Button nudgeXMinusButton, Button nudgeYPlusButton, Button nudgeYMinusButton, Button nudgeZPlusButton, Button nudgeZMinusButton, Button rotatePlusButton, Button rotateMinusButton, Button scalePlusButton, Button scaleMinusButton, Button snapToFloorButton, Button resetRotationButton, Text gridStepText, Text rotateStepText, Text scaleStepText, MapEditorValidationPresenter presenter, MapEditorObjectInspector inspector)
        {
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("leftInfoPanel").objectReferenceValue = leftPanel;
            serialized.FindProperty("mainBrushPanel").objectReferenceValue = mainPanel;
            serialized.FindProperty("rightInspectorPanel").objectReferenceValue = rightPanel;
            serialized.FindProperty("mapNameText").objectReferenceValue = mapNameText;
            serialized.FindProperty("mapIdText").objectReferenceValue = mapIdText;
            serialized.FindProperty("versionText").objectReferenceValue = mapVersionText;
            serialized.FindProperty("modeText").objectReferenceValue = modeText;
            serialized.FindProperty("countText").objectReferenceValue = countText;
            serialized.FindProperty("validationStatusText").objectReferenceValue = statusText;
            serialized.FindProperty("validateButton").objectReferenceValue = validateButton;
            serialized.FindProperty("exportButton").objectReferenceValue = exportButton;
            serialized.FindProperty("saveButton").objectReferenceValue = saveButton;
            serialized.FindProperty("loadButton").objectReferenceValue = loadButton;
            serialized.FindProperty("previewButton").objectReferenceValue = previewButton;
            serialized.FindProperty("clearMapButton").objectReferenceValue = clearMapButton;
            serialized.FindProperty("mapSettingsButton").objectReferenceValue = mapSettingsButton;
            serialized.FindProperty("mapObjectsListRoot").objectReferenceValue = mapObjectsList;
            serialized.FindProperty("gameplayMarkersListRoot").objectReferenceValue = gameplayMarkersList;
            serialized.FindProperty("brushButtonTemplate").objectReferenceValue = brushButtonTemplate;
            serialized.FindProperty("leftHandBrushText").objectReferenceValue = leftHandBrushText;
            serialized.FindProperty("rightHandBrushText").objectReferenceValue = rightHandBrushText;
            serialized.FindProperty("activeHandText").objectReferenceValue = activeHandText;
            serialized.FindProperty("hintText").objectReferenceValue = hintText;
            serialized.FindProperty("placeButton").objectReferenceValue = placeButton;
            serialized.FindProperty("moveButton").objectReferenceValue = moveButton;
            serialized.FindProperty("rotateButton").objectReferenceValue = rotateButton;
            serialized.FindProperty("scaleButton").objectReferenceValue = scaleButton;
            serialized.FindProperty("calibrateFloorButton").objectReferenceValue = calibrateFloorButton;
            serialized.FindProperty("deleteButton").objectReferenceValue = deleteButton;
            serialized.FindProperty("clearBrushButton").objectReferenceValue = clearBrushButton;
            serialized.FindProperty("gridSnapToggle").objectReferenceValue = gridSnapToggle;
            serialized.FindProperty("gridStep01Button").objectReferenceValue = gridStep01Button;
            serialized.FindProperty("gridStep025Button").objectReferenceValue = gridStep025Button;
            serialized.FindProperty("gridStep05Button").objectReferenceValue = gridStep05Button;
            serialized.FindProperty("gridStep1Button").objectReferenceValue = gridStep1Button;
            serialized.FindProperty("nudgeXPlusButton").objectReferenceValue = nudgeXPlusButton;
            serialized.FindProperty("nudgeXMinusButton").objectReferenceValue = nudgeXMinusButton;
            serialized.FindProperty("nudgeYPlusButton").objectReferenceValue = nudgeYPlusButton;
            serialized.FindProperty("nudgeYMinusButton").objectReferenceValue = nudgeYMinusButton;
            serialized.FindProperty("nudgeZPlusButton").objectReferenceValue = nudgeZPlusButton;
            serialized.FindProperty("nudgeZMinusButton").objectReferenceValue = nudgeZMinusButton;
            serialized.FindProperty("rotatePlusButton").objectReferenceValue = rotatePlusButton;
            serialized.FindProperty("rotateMinusButton").objectReferenceValue = rotateMinusButton;
            serialized.FindProperty("scalePlusButton").objectReferenceValue = scalePlusButton;
            serialized.FindProperty("scaleMinusButton").objectReferenceValue = scaleMinusButton;
            serialized.FindProperty("snapToFloorButton").objectReferenceValue = snapToFloorButton;
            serialized.FindProperty("resetRotationButton").objectReferenceValue = resetRotationButton;
            serialized.FindProperty("gridStepText").objectReferenceValue = gridStepText;
            serialized.FindProperty("rotateStepText").objectReferenceValue = rotateStepText;
            serialized.FindProperty("scaleStepText").objectReferenceValue = scaleStepText;
            serialized.FindProperty("validationPresenter").objectReferenceValue = presenter;
            serialized.FindProperty("objectInspector").objectReferenceValue = inspector;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
