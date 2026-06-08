using System.IO;
using TreasureArenaMR.Map;
using TreasureArenaMR.Shared;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TreasureArenaMR.MapEditor.Editor
{
    public static class MapEditorDockedUiPrefabBuilder
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/MapEditor/UI/MapEditorDockedCanvas.prefab";

        private static readonly Color PanelColor = new Color(0.06f, 0.07f, 0.08f, 0.78f);
        private static readonly Color ButtonColor = new Color(0.20f, 0.36f, 0.60f, 0.95f);
        private static readonly Color DangerColor = new Color(0.72f, 0.14f, 0.12f, 0.95f);
        private static readonly Color FieldColor = new Color(0.92f, 0.94f, 0.96f, 0.96f);

        [MenuItem("Tools/TreasureArena/地图编辑器/创建 Docked UI Prefab")]
        public static GameObject CreateOrUpdatePrefab()
        {
            string folder = Path.GetDirectoryName(PrefabPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
            {
                Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
            }

            GameObject root = new GameObject("MapEditorDockedCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MapEditorDockedUiController));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(1200f, 720f);
            root.transform.localScale = Vector3.one * 0.0024f;

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            root.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 12f;

            MapEditorValidationPresenter presenter = root.AddComponent<MapEditorValidationPresenter>();

            MapEditorDockedPanel topPanel = CreatePanel(root.transform, "TopBar", "Map", new Vector2(0.5f, 1f), new Vector2(1180f, 82f), new Vector2(0f, -48f), out Transform topExpanded);
            MapEditorDockedPanel leftPanel = CreatePanel(root.transform, "LeftPanel", "Brushes", new Vector2(0f, 0.5f), new Vector2(260f, 520f), new Vector2(150f, 0f), out Transform leftExpanded);
            MapEditorDockedPanel rightPanel = CreatePanel(root.transform, "RightPanel", "Inspector", new Vector2(1f, 0.5f), new Vector2(300f, 520f), new Vector2(-170f, 0f), out Transform rightExpanded);
            MapEditorDockedPanel bottomPanel = CreatePanel(root.transform, "BottomBar", "Tools", new Vector2(0.5f, 0f), new Vector2(1180f, 132f), new Vector2(0f, 76f), out Transform bottomExpanded);

            Text mapNameText = CreateText(topExpanded, "MapNameText", "Map: new_map", 16, new Vector2(-520f, 8f), new Vector2(190f, 24f), TextAnchor.MiddleLeft);
            Text mapIdText = CreateText(topExpanded, "MapIdText", "id: new_map", 14, new Vector2(-330f, 8f), new Vector2(170f, 24f), TextAnchor.MiddleLeft);
            Text mapVersionText = CreateText(topExpanded, "MapVersionText", "v1", 14, new Vector2(-190f, 8f), new Vector2(70f, 24f), TextAnchor.MiddleLeft);
            Text modeText = CreateText(topExpanded, "ModeText", "Mode: Place", 14, new Vector2(-90f, 8f), new Vector2(120f, 24f), TextAnchor.MiddleLeft);
            Text countText = CreateText(topExpanded, "CountText", "Objects: 0", 14, new Vector2(80f, 8f), new Vector2(260f, 24f), TextAnchor.MiddleLeft);
            Text statusText = CreateText(topExpanded, "StatusText", "Ready", 14, new Vector2(360f, 8f), new Vector2(250f, 24f), TextAnchor.MiddleLeft);
            Button validateButton = CreateButton(topExpanded, "ValidateButton", "Validate", new Vector2(470f, -22f), new Vector2(112f, 40f), ButtonColor);
            Button exportButton = CreateButton(topExpanded, "ExportButton", "Export", new Vector2(592f, -22f), new Vector2(96f, 40f), ButtonColor);

            CreateText(leftExpanded, "MapObjectsLabel", "Folders", 15, new Vector2(0f, 192f), new Vector2(220f, 24f), TextAnchor.MiddleLeft);
            Transform mapObjectsList = CreateScrollListRoot(leftExpanded, "MapObjectsList", new Vector2(0f, -8f), new Vector2(220f, 392f));
            CreateText(leftExpanded, "GameplayMarkersLabel", "", 15, new Vector2(0f, -224f), new Vector2(220f, 24f), TextAnchor.MiddleLeft);
            Transform gameplayMarkersList = CreateListRoot(leftExpanded, "GameplayMarkersList", new Vector2(0f, -240f), new Vector2(220f, 1f));
            Button brushButtonTemplate = CreateButton(leftExpanded, "BrushButtonTemplate", "Brush", new Vector2(0f, -246f), new Vector2(220f, 40f), ButtonColor);
            brushButtonTemplate.gameObject.SetActive(false);

            MapEditorObjectInspector inspector = rightPanel.gameObject.AddComponent<MapEditorObjectInspector>();
            Text selectedNameText = CreateText(rightExpanded, "SelectedNameText", "Selected: none", 14, new Vector2(0f, 198f), new Vector2(250f, 24f), TextAnchor.MiddleLeft);
            Text selectedTypeText = CreateText(rightExpanded, "SelectedTypeText", "Type: none", 14, new Vector2(0f, 174f), new Vector2(250f, 24f), TextAnchor.MiddleLeft);
            InputField idField = CreateInputField(rightExpanded, "ObjectIdField", "id", new Vector2(0f, 138f), new Vector2(250f, 32f));
            InputField prefabField = CreateInputField(rightExpanded, "PrefabIdField", "prefab", new Vector2(0f, 102f), new Vector2(250f, 32f));
            InputField posX = CreateInputField(rightExpanded, "PositionX", "pos x", new Vector2(-86f, 60f), new Vector2(78f, 32f));
            InputField posY = CreateInputField(rightExpanded, "PositionY", "y", new Vector2(0f, 60f), new Vector2(78f, 32f));
            InputField posZ = CreateInputField(rightExpanded, "PositionZ", "z", new Vector2(86f, 60f), new Vector2(78f, 32f));
            InputField rotX = CreateInputField(rightExpanded, "RotationX", "rot x", new Vector2(-86f, 22f), new Vector2(78f, 32f));
            InputField rotY = CreateInputField(rightExpanded, "RotationY", "y", new Vector2(0f, 22f), new Vector2(78f, 32f));
            InputField rotZ = CreateInputField(rightExpanded, "RotationZ", "z", new Vector2(86f, 22f), new Vector2(78f, 32f));
            InputField scaleX = CreateInputField(rightExpanded, "ScaleX", "scale x", new Vector2(-86f, -16f), new Vector2(78f, 32f));
            InputField scaleY = CreateInputField(rightExpanded, "ScaleY", "y", new Vector2(0f, -16f), new Vector2(78f, 32f));
            InputField scaleZ = CreateInputField(rightExpanded, "ScaleZ", "z", new Vector2(86f, -16f), new Vector2(78f, 32f));
            Toggle colliderToggle = CreateToggle(rightExpanded, "HasColliderToggle", "has collider", new Vector2(-68f, -54f), new Vector2(118f, 28f));
            Dropdown teamDropdown = CreateDropdown(rightExpanded, "TeamDropdown", "team", new Vector2(70f, -54f), new Vector2(118f, 32f), new[] { TeamType.None.ToString(), TeamType.Red.ToString(), TeamType.Blue.ToString() });
            Dropdown treasureDropdown = CreateDropdown(rightExpanded, "TreasureDropdown", "treasure", new Vector2(0f, -92f), new Vector2(250f, 32f), new[] { TreasureType.Normal.ToString(), TreasureType.Rare.ToString(), TreasureType.Final.ToString() });
            InputField radiusField = CreateInputField(rightExpanded, "RadiusField", "radius", new Vector2(-66f, -130f), new Vector2(118f, 32f));
            InputField supplyField = CreateInputField(rightExpanded, "SupplyTypeField", "supply", new Vector2(70f, -130f), new Vector2(118f, 32f));
            InputField refreshField = CreateInputField(rightExpanded, "RefreshIntervalField", "refresh", new Vector2(0f, -168f), new Vector2(250f, 32f));

            Button placeButton = CreateButton(bottomExpanded, "PlaceButton", "Place", new Vector2(-500f, 22f), new Vector2(92f, 40f), ButtonColor);
            Button moveButton = CreateButton(bottomExpanded, "MoveButton", "Move", new Vector2(-400f, 22f), new Vector2(92f, 40f), ButtonColor);
            Button rotateButton = CreateButton(bottomExpanded, "RotateButton", "Rotate", new Vector2(-300f, 22f), new Vector2(92f, 40f), ButtonColor);
            Button scaleButton = CreateButton(bottomExpanded, "ScaleButton", "Scale", new Vector2(-200f, 22f), new Vector2(92f, 40f), ButtonColor);
            Button deleteButton = CreateButton(bottomExpanded, "DeleteButton", "Delete", new Vector2(-100f, 22f), new Vector2(92f, 40f), DangerColor);
            Button clearBrushButton = CreateButton(bottomExpanded, "ClearBrushButton", "Clear Brush", new Vector2(20f, 22f), new Vector2(124f, 40f), ButtonColor);
            Text gridStepText = CreateText(bottomExpanded, "GridStepText", "Grid 0.5m", 13, new Vector2(-500f, -26f), new Vector2(100f, 24f), TextAnchor.MiddleLeft);
            Text rotateStepText = CreateText(bottomExpanded, "RotateStepText", "Rotate 15 deg", 13, new Vector2(-390f, -26f), new Vector2(130f, 24f), TextAnchor.MiddleLeft);
            Text scaleStepText = CreateText(bottomExpanded, "ScaleStepText", "Scale 0.1", 13, new Vector2(-250f, -26f), new Vector2(100f, 24f), TextAnchor.MiddleLeft);
            Text recentLogText = CreateText(bottomExpanded, "RecentLogText", "Ready", 14, new Vector2(320f, 24f), new Vector2(430f, 36f), TextAnchor.MiddleLeft);
            Text errorListText = CreateText(bottomExpanded, "ErrorListText", "", 13, new Vector2(300f, -32f), new Vector2(520f, 58f), TextAnchor.UpperLeft);

            BindPresenter(presenter, recentLogText, errorListText);
            BindInspector(inspector, selectedNameText, selectedTypeText, idField, prefabField, posX, posY, posZ, rotX, rotY, rotZ, scaleX, scaleY, scaleZ, colliderToggle, teamDropdown, treasureDropdown, radiusField, supplyField, refreshField);
            BindController(root.GetComponent<MapEditorDockedUiController>(), topPanel, leftPanel, rightPanel, bottomPanel, mapNameText, mapIdText, mapVersionText, modeText, countText, statusText, validateButton, exportButton, mapObjectsList, gameplayMarkersList, brushButtonTemplate, placeButton, moveButton, rotateButton, scaleButton, deleteButton, clearBrushButton, gridStepText, rotateStepText, scaleStepText, presenter, inspector);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return prefab;
        }

        private static MapEditorDockedPanel CreatePanel(Transform parent, string name, string title, Vector2 anchor, Vector2 size, Vector2 position, out Transform expandedRoot)
        {
            GameObject panelGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(BoxCollider), typeof(MapEditorRuntimeUiHitTarget), typeof(MapEditorDockedPanel));
            panelGo.transform.SetParent(parent, false);
            RectTransform rect = panelGo.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            panelGo.GetComponent<Image>().color = PanelColor;
            SetBoxCollider(panelGo, size);

            GameObject expanded = CreateRect(panelGo.transform, "Expanded", Vector2.zero, size);
            expandedRoot = expanded.transform;
            GameObject collapsed = CreateRect(panelGo.transform, "Collapsed", Vector2.zero, new Vector2(Mathf.Min(size.x, 260f), 42f));
            collapsed.SetActive(false);

            Text expandedTitle = CreateText(expanded.transform, "Title", title, 16, new Vector2(-size.x * 0.5f + 70f, size.y * 0.5f - 24f), new Vector2(120f, 28f), TextAnchor.MiddleLeft);
            Button collapseButton = CreateButton(expanded.transform, "CollapseButton", "-", new Vector2(size.x * 0.5f - 72f, size.y * 0.5f - 24f), new Vector2(40f, 34f), ButtonColor);
            Button pinButton = CreateButton(expanded.transform, "PinButton", "Pin", new Vector2(size.x * 0.5f - 26f, size.y * 0.5f - 24f), new Vector2(48f, 34f), ButtonColor);
            Text pinText = pinButton.GetComponentInChildren<Text>();
            Image expandedErrorDot = CreateDot(expanded.transform, "ExpandedErrorDot", new Vector2(size.x * 0.5f - 116f, size.y * 0.5f - 24f));

            Text collapsedTitle = CreateText(collapsed.transform, "CollapsedTitle", title, 14, new Vector2(12f, 0f), new Vector2(150f, 32f), TextAnchor.MiddleLeft);
            Button expandButton = CreateButton(collapsed.transform, "ExpandButton", "+", new Vector2(100f, 0f), new Vector2(40f, 34f), ButtonColor);
            Image collapsedErrorDot = CreateDot(collapsed.transform, "CollapsedErrorDot", new Vector2(132f, 0f));

            SerializedObject panel = new SerializedObject(panelGo.GetComponent<MapEditorDockedPanel>());
            panel.FindProperty("panelTitle").stringValue = title;
            panel.FindProperty("canvasGroup").objectReferenceValue = panelGo.GetComponent<CanvasGroup>();
            panel.FindProperty("expandedRoot").objectReferenceValue = expanded;
            panel.FindProperty("collapsedRoot").objectReferenceValue = collapsed;
            panel.FindProperty("titleText").objectReferenceValue = expandedTitle;
            panel.FindProperty("collapsedTitleText").objectReferenceValue = collapsedTitle;
            panel.FindProperty("errorDot").objectReferenceValue = expandedErrorDot;
            panel.FindProperty("collapsedErrorDot").objectReferenceValue = collapsedErrorDot;
            panel.FindProperty("collapseButton").objectReferenceValue = collapseButton;
            panel.FindProperty("expandButton").objectReferenceValue = expandButton;
            panel.FindProperty("pinButton").objectReferenceValue = pinButton;
            panel.FindProperty("pinText").objectReferenceValue = pinText;
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

        private static Transform CreateListRoot(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject list = CreateRect(parent, name, position, size);
            VerticalLayoutGroup layout = list.AddComponent<VerticalLayoutGroup>();
            layout.childControlHeight = false;
            layout.childControlWidth = false;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;
            layout.spacing = 8f;
            return list.transform;
        }

        private static Transform CreateScrollListRoot(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject scrollGo = CreateRect(parent, name + "Scroll", position, size);
            Image scrollImage = scrollGo.AddComponent<Image>();
            scrollImage.color = new Color(0f, 0f, 0f, 0.12f);
            ScrollRect scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

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
            Text text = CreateText(go.transform, "Text", label, 14, Vector2.zero, size, TextAnchor.MiddleCenter);
            text.raycastTarget = false;
            SetBoxCollider(go, size);
            BindUiHitTarget(go.GetComponent<MapEditorRuntimeUiHitTarget>(), button);
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

        private static void BindUiHitTarget(MapEditorRuntimeUiHitTarget target, Button button)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty("button").objectReferenceValue = button;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BindPresenter(MapEditorValidationPresenter presenter, Text recentLogText, Text errorListText)
        {
            SerializedObject serialized = new SerializedObject(presenter);
            serialized.FindProperty("recentLogText").objectReferenceValue = recentLogText;
            serialized.FindProperty("errorListText").objectReferenceValue = errorListText;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BindInspector(MapEditorObjectInspector inspector, Text selectedNameText, Text selectedTypeText, InputField idField, InputField prefabField, InputField posX, InputField posY, InputField posZ, InputField rotX, InputField rotY, InputField rotZ, InputField scaleX, InputField scaleY, InputField scaleZ, Toggle colliderToggle, Dropdown teamDropdown, Dropdown treasureDropdown, InputField radiusField, InputField supplyField, InputField refreshField)
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
            serialized.FindProperty("teamDropdown").objectReferenceValue = teamDropdown;
            serialized.FindProperty("treasureTypeDropdown").objectReferenceValue = treasureDropdown;
            serialized.FindProperty("radiusInput").objectReferenceValue = radiusField;
            serialized.FindProperty("supplyTypeInput").objectReferenceValue = supplyField;
            serialized.FindProperty("refreshIntervalInput").objectReferenceValue = refreshField;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BindController(MapEditorDockedUiController controller, MapEditorDockedPanel topPanel, MapEditorDockedPanel leftPanel, MapEditorDockedPanel rightPanel, MapEditorDockedPanel bottomPanel, Text mapNameText, Text mapIdText, Text mapVersionText, Text modeText, Text countText, Text statusText, Button validateButton, Button exportButton, Transform mapObjectsList, Transform gameplayMarkersList, Button brushButtonTemplate, Button placeButton, Button moveButton, Button rotateButton, Button scaleButton, Button deleteButton, Button clearBrushButton, Text gridStepText, Text rotateStepText, Text scaleStepText, MapEditorValidationPresenter presenter, MapEditorObjectInspector inspector)
        {
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("topPanel").objectReferenceValue = topPanel;
            serialized.FindProperty("leftPanel").objectReferenceValue = leftPanel;
            serialized.FindProperty("rightPanel").objectReferenceValue = rightPanel;
            serialized.FindProperty("bottomPanel").objectReferenceValue = bottomPanel;
            serialized.FindProperty("mapNameText").objectReferenceValue = mapNameText;
            serialized.FindProperty("mapIdText").objectReferenceValue = mapIdText;
            serialized.FindProperty("versionText").objectReferenceValue = mapVersionText;
            serialized.FindProperty("modeText").objectReferenceValue = modeText;
            serialized.FindProperty("countText").objectReferenceValue = countText;
            serialized.FindProperty("validationStatusText").objectReferenceValue = statusText;
            serialized.FindProperty("validateButton").objectReferenceValue = validateButton;
            serialized.FindProperty("exportButton").objectReferenceValue = exportButton;
            serialized.FindProperty("mapObjectsListRoot").objectReferenceValue = mapObjectsList;
            serialized.FindProperty("gameplayMarkersListRoot").objectReferenceValue = gameplayMarkersList;
            serialized.FindProperty("brushButtonTemplate").objectReferenceValue = brushButtonTemplate;
            serialized.FindProperty("placeButton").objectReferenceValue = placeButton;
            serialized.FindProperty("moveButton").objectReferenceValue = moveButton;
            serialized.FindProperty("rotateButton").objectReferenceValue = rotateButton;
            serialized.FindProperty("scaleButton").objectReferenceValue = scaleButton;
            serialized.FindProperty("deleteButton").objectReferenceValue = deleteButton;
            serialized.FindProperty("clearBrushButton").objectReferenceValue = clearBrushButton;
            serialized.FindProperty("gridStepText").objectReferenceValue = gridStepText;
            serialized.FindProperty("rotateStepText").objectReferenceValue = rotateStepText;
            serialized.FindProperty("scaleStepText").objectReferenceValue = scaleStepText;
            serialized.FindProperty("validationPresenter").objectReferenceValue = presenter;
            serialized.FindProperty("objectInspector").objectReferenceValue = inspector;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
