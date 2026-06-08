using TreasureArenaMR.Map;
using TreasureArenaMR.Shared;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TreasureArenaMR.MapEditor.Editor
{
    public static class MapEditorRuntimeSceneSetup
    {
        private const string RuntimeRootName = "MapEditorRuntime";
        private const string PlacedRootName = "RuntimePlacedObjects";
        private const string RayOriginName = "EditorFallbackRayOrigin";
        private const string PanelName = "MapEditorRuntimePanel";
        private static readonly string[] BrushSearchFolders =
        {
            "Assets/_Project/Prefabs/MapEditor/MapObjects",
            "Assets/_Project/Prefabs/MapEditor/GameplayMarkers"
        };

        [MenuItem("Tools/TreasureArena/地图编辑器/创建 MR 运行时输入控制器")]
        public static void CreateRuntimeInputController()
        {
            GameObject runtimeRoot = GameObject.Find(RuntimeRootName);
            if (runtimeRoot == null)
            {
                runtimeRoot = new GameObject(RuntimeRootName);
                Undo.RegisterCreatedObjectUndo(runtimeRoot, "Create MapEditor Runtime");
            }

            GameObject placedRoot = GameObject.Find(PlacedRootName);
            if (placedRoot == null)
            {
                placedRoot = new GameObject(PlacedRootName);
                Undo.RegisterCreatedObjectUndo(placedRoot, "Create Runtime Placed Objects Root");
            }

            GameObject rayOrigin = GameObject.Find(RayOriginName);
            if (rayOrigin == null)
            {
                rayOrigin = new GameObject(RayOriginName);
                Undo.RegisterCreatedObjectUndo(rayOrigin, "Create Runtime Ray Origin");
                rayOrigin.transform.position = new Vector3(0f, 1.4f, -2f);
                rayOrigin.transform.rotation = Quaternion.Euler(18f, 0f, 0f);
            }

            MapEditorRuntimeController controller = runtimeRoot.GetComponent<MapEditorRuntimeController>();
            if (controller == null)
            {
                controller = Undo.AddComponent<MapEditorRuntimeController>(runtimeRoot);
            }

            MapEditorRuntimeExporter exporter = runtimeRoot.GetComponent<MapEditorRuntimeExporter>();
            if (exporter == null)
            {
                exporter = Undo.AddComponent<MapEditorRuntimeExporter>(runtimeRoot);
            }

            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("rayOrigin").objectReferenceValue = rayOrigin.transform;
            serializedController.FindProperty("fallbackCamera").objectReferenceValue = Camera.main;
            serializedController.FindProperty("placedObjectsRoot").objectReferenceValue = placedRoot.transform;
            SerializedProperty brushes = serializedController.FindProperty("brushes");
            FillDefaultBrushes(brushes);
            if (brushes.arraySize > 0)
            {
                serializedController.FindProperty("activeBrushIndex").intValue = 0;
            }
            serializedController.ApplyModifiedProperties();

            CreateOrUpdateRuntimePanel(controller, exporter);

            Selection.activeGameObject = runtimeRoot;
            EditorUtility.SetDirty(runtimeRoot);
            EditorUtility.SetDirty(placedRoot);
            EditorUtility.SetDirty(rayOrigin);
            Debug.Log("MapEditor MR runtime input controller created. Save the scene to keep it.");
        }

        private static void CreateOrUpdateRuntimePanel(MapEditorRuntimeController controller, MapEditorRuntimeExporter exporter)
        {
            EnsureEventSystem();

            GameObject panel = GameObject.Find(PanelName);
            if (panel == null)
            {
                panel = new GameObject(PanelName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
                Undo.RegisterCreatedObjectUndo(panel, "Create MapEditor Runtime Panel");
            }

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(420f, 360f);
            PlacePanelNearCamera(panel.transform);
            panel.transform.localScale = Vector3.one * 0.004f;

            Canvas canvas = panel.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            CanvasScaler scaler = panel.GetComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;
            Image panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0.08f, 0.09f, 0.1f, 0.85f);

            Text title = EnsureText(panel.transform, "Title", "Map Editor MR", 22, new Vector2(0f, 146f), new Vector2(380f, 32f), TextAnchor.MiddleLeft);
            title.fontStyle = FontStyle.Bold;
            Text modeText = EnsureText(panel.transform, "ModeText", "Mode: Place", 16, new Vector2(0f, 108f), new Vector2(380f, 26f), TextAnchor.MiddleLeft);
            Text selectedText = EnsureText(panel.transform, "SelectedText", "Selected: none", 16, new Vector2(0f, 78f), new Vector2(380f, 26f), TextAnchor.MiddleLeft);
            Text statusText = EnsureText(panel.transform, "StatusText", "Map editor runtime input ready.", 14, new Vector2(0f, -120f), new Vector2(380f, 70f), TextAnchor.UpperLeft);
            Dropdown brushDropdown = EnsureDropdown(panel.transform, "BrushDropdown", new Vector2(0f, 38f), new Vector2(380f, 32f));
            Button clearBrushButton = EnsureButton(panel.transform, "ClearBrushButton", "Clear", new Vector2(-140f, -10f), new Vector2(100f, 32f));
            Button nextModeButton = EnsureButton(panel.transform, "NextModeButton", "Mode", new Vector2(-30f, -10f), new Vector2(100f, 32f));
            Button deleteButton = EnsureButton(panel.transform, "DeleteButton", "Delete", new Vector2(80f, -10f), new Vector2(100f, 32f));
            Button validateButton = EnsureButton(panel.transform, "ValidateButton", "Validate", new Vector2(-85f, -54f), new Vector2(130f, 32f));
            Button exportButton = EnsureButton(panel.transform, "ExportButton", "Export", new Vector2(65f, -54f), new Vector2(130f, 32f));

            MapEditorRuntimeUiBinder binder = panel.GetComponent<MapEditorRuntimeUiBinder>();
            if (binder == null)
            {
                binder = Undo.AddComponent<MapEditorRuntimeUiBinder>(panel);
            }

            SerializedObject serializedBinder = new SerializedObject(binder);
            serializedBinder.FindProperty("controller").objectReferenceValue = controller;
            serializedBinder.FindProperty("exporter").objectReferenceValue = exporter;
            serializedBinder.FindProperty("brushDropdown").objectReferenceValue = brushDropdown;
            serializedBinder.FindProperty("statusText").objectReferenceValue = statusText;
            serializedBinder.FindProperty("selectedText").objectReferenceValue = selectedText;
            serializedBinder.FindProperty("modeText").objectReferenceValue = modeText;
            serializedBinder.FindProperty("clearBrushButton").objectReferenceValue = clearBrushButton;
            serializedBinder.FindProperty("nextModeButton").objectReferenceValue = nextModeButton;
            serializedBinder.FindProperty("deleteButton").objectReferenceValue = deleteButton;
            serializedBinder.FindProperty("validateButton").objectReferenceValue = validateButton;
            serializedBinder.FindProperty("exportButton").objectReferenceValue = exportButton;
            serializedBinder.ApplyModifiedProperties();

            EditorUtility.SetDirty(panel);
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
            EditorUtility.SetDirty(eventSystem);
        }

        private static Text EnsureText(Transform parent, string name, string text, int fontSize, Vector2 position, Vector2 size, TextAnchor anchor)
        {
            GameObject go = EnsureChild(parent, name, typeof(RectTransform), typeof(Text));
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

        private static Dropdown EnsureDropdown(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject go = EnsureChild(parent, name, typeof(RectTransform), typeof(Image), typeof(Dropdown));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.92f, 0.92f, 0.92f, 1f);
            Dropdown dropdown = go.GetComponent<Dropdown>();
            Text label = EnsureText(go.transform, "Label", "No Brush", 14, Vector2.zero, new Vector2(size.x - 30f, size.y), TextAnchor.MiddleLeft);
            label.color = Color.black;
            Text caption = EnsureText(go.transform, "Caption", "", 14, new Vector2(-12f, 0f), new Vector2(size.x - 30f, size.y), TextAnchor.MiddleLeft);
            caption.color = Color.black;
            dropdown.captionText = caption;

            if (dropdown.template == null)
            {
                RectTransform template = CreateDropdownTemplate(go.transform, size.x);
                dropdown.template = template;
                dropdown.itemText = template.Find("Viewport/Content/Item/Item Label")?.GetComponent<Text>();
            }

            return dropdown;
        }

        private static RectTransform CreateDropdownTemplate(Transform dropdownTransform, float width)
        {
            // Template (root of the dropdown list)
            Transform existingTemplate = dropdownTransform.Find("Template");
            GameObject templateGo;
            if (existingTemplate != null)
            {
                templateGo = existingTemplate.gameObject;
            }
            else
            {
                templateGo = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
                Undo.RegisterCreatedObjectUndo(templateGo, "Create Dropdown Template");
                templateGo.transform.SetParent(dropdownTransform, false);
            }

            RectTransform templateRect = templateGo.GetComponent<RectTransform>();
            templateRect.sizeDelta = new Vector2(width, 150f);
            templateRect.anchoredPosition = new Vector2(0f, -160f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            Image templateImage = templateGo.GetComponent<Image>();
            templateImage.color = new Color(0.97f, 0.97f, 0.97f, 1f);
            ScrollRect scrollRect = templateGo.GetComponent<ScrollRect>();

            // Viewport
            RectTransform viewport = EnsureDropdownChild(templateGo.transform, "Viewport",
                new Vector2(0f, 0f), new Vector2(width, 146f),
                new Vector2(0f, 1f), new Vector2(0f, 1f));
            EnsureComponent<Image>(viewport.gameObject);
            EnsureComponent<Mask>(viewport.gameObject).showMaskGraphic = false;

            // Content
            RectTransform content = EnsureDropdownChild(viewport, "Content",
                new Vector2(0f, 0f), new Vector2(width, 0f),
                new Vector2(0f, 1f), new Vector2(0f, 1f));
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);

            // Item (template for each option)
            RectTransform item = EnsureDropdownChild(content, "Item",
                new Vector2(0f, -18f), new Vector2(width, 18f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            Toggle toggle = EnsureComponent<Toggle>(item.gameObject);

            // Item Background
            GameObject itemBgGo = EnsureChild(item, "Item Background", typeof(RectTransform), typeof(Image));
            itemBgGo.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 18f);
            itemBgGo.GetComponent<Image>().color = Color.white;

            // Item Checkmark
            GameObject itemCheckGo = EnsureChild(item, "Item Checkmark", typeof(RectTransform), typeof(Image));
            RectTransform checkRect = itemCheckGo.GetComponent<RectTransform>();
            checkRect.anchoredPosition = new Vector2(10f, 0f);
            checkRect.sizeDelta = new Vector2(16f, 16f);
            checkRect.pivot = new Vector2(0f, 0.5f);
            itemCheckGo.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // Item Label
            Text itemLabel = EnsureText(item, "Item Label", "Option", 14, new Vector2(30f, 0f), new Vector2(width - 40f, 18f), TextAnchor.MiddleLeft);
            itemLabel.color = Color.black;

            toggle.targetGraphic = itemBgGo.GetComponent<Image>();
            toggle.graphic = itemCheckGo.GetComponent<Image>();

            // Scrollbar
            RectTransform scrollbarRect = EnsureDropdownChild(templateGo.transform, "Scrollbar",
                Vector2.zero, new Vector2(16f, 146f),
                new Vector2(1f, 1f), new Vector2(1f, 1f));
            scrollbarRect.anchoredPosition = new Vector2(-8f, 0f);
            EnsureComponent<Image>(scrollbarRect.gameObject).color = new Color(0.9f, 0.9f, 0.9f, 1f);
            Scrollbar scrollbar = EnsureComponent<Scrollbar>(scrollbarRect.gameObject);

            // Sliding Area
            RectTransform slidingArea = EnsureDropdownChild(scrollbarRect, "Sliding Area",
                Vector2.zero, new Vector2(16f, 146f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            // Handle
            GameObject handleGo = EnsureChild(slidingArea, "Handle", typeof(RectTransform), typeof(Image));
            handleGo.GetComponent<RectTransform>().sizeDelta = new Vector2(16f, 30f);
            handleGo.GetComponent<Image>().color = new Color(0.8f, 0.8f, 0.8f, 1f);

            // Wire up ScrollRect and Scrollbar
            scrollbar.targetGraphic = handleGo.GetComponent<Image>();
            scrollbar.handleRect = handleGo.GetComponent<RectTransform>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            return templateRect;
        }

        private static RectTransform EnsureDropdownChild(Transform parent, string name,
            Vector2 position, Vector2 sizeDelta, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject go = EnsureChild(parent, name, typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = sizeDelta;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            return rect;
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            if (comp == null)
            {
                comp = go.AddComponent<T>();
            }

            return comp;
        }

        private static Button EnsureButton(Transform parent, string name, string label, Vector2 position, Vector2 size)
        {
            GameObject go = EnsureChild(parent, name, typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.23f, 0.42f, 0.75f, 1f);
            Button button = go.GetComponent<Button>();
            Text text = EnsureText(go.transform, "Text", label, 14, Vector2.zero, size, TextAnchor.MiddleCenter);
            text.color = Color.white;
            return button;
        }

        private static GameObject EnsureChild(Transform parent, string name, params System.Type[] components)
        {
            Transform child = parent.Find(name);
            GameObject go;
            if (child == null)
            {
                go = new GameObject(name, components);
                Undo.RegisterCreatedObjectUndo(go, "Create " + name);
                go.transform.SetParent(parent, false);
            }
            else
            {
                go = child.gameObject;
                for (int i = 0; i < components.Length; i++)
                {
                    if (go.GetComponent(components[i]) == null)
                    {
                        Undo.AddComponent(go, components[i]);
                    }
                }
            }

            return go;
        }

        private static void FillDefaultBrushes(SerializedProperty brushes)
        {
            brushes.ClearArray();

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", BrushSearchFolders);
            string[] prefabPaths = new string[prefabGuids.Length];
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                prefabPaths[i] = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            }

            System.Array.Sort(prefabPaths, System.StringComparer.Ordinal);
            for (int i = 0; i < prefabPaths.Length; i++)
            {
                AddBrushFromPrefab(brushes, prefabPaths[i]);
            }
        }

        private static void AddBrushFromPrefab(SerializedProperty brushes, string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("MapEditor brush prefab not found: " + prefabPath);
                return;
            }

            MapExportMarker marker = prefab.GetComponent<MapExportMarker>();
            string prefabId = marker != null && !string.IsNullOrEmpty(marker.prefab_id) ? marker.prefab_id : prefab.name;
            int index = brushes.arraySize;
            brushes.InsertArrayElementAtIndex(index);
            SerializedProperty brush = brushes.GetArrayElementAtIndex(index);
            brush.FindPropertyRelative("label").stringValue = ObjectNames.NicifyVariableName(prefab.name);
            brush.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            brush.FindPropertyRelative("prefab_id").stringValue = prefabId;
            brush.FindPropertyRelative("marker_type").enumValueIndex = marker != null ? (int)marker.marker_type : (int)MapExportMarkerType.MapObject;
            brush.FindPropertyRelative("team").enumValueIndex = marker != null ? (int)marker.team : (int)TeamType.None;
            brush.FindPropertyRelative("treasure_type").enumValueIndex = marker != null ? (int)marker.treasure_type : (int)TreasureType.Normal;
            brush.FindPropertyRelative("supply_type").stringValue = marker != null ? marker.supply_type : "WeaponRandom";
            brush.FindPropertyRelative("radius").floatValue = marker != null ? marker.radius : 1f;
            brush.FindPropertyRelative("refresh_interval").floatValue = marker != null ? marker.refresh_interval : 20f;
            brush.FindPropertyRelative("has_collider").boolValue = prefab.GetComponentInChildren<Collider>() != null;
        }

        private static void PlacePanelNearCamera(Transform panel)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                panel.position = new Vector3(-1.8f, 1.6f, -2.2f);
                panel.rotation = Quaternion.Euler(20f, 18f, 0f);
                return;
            }

            Transform cameraTransform = camera.transform;
            panel.position = cameraTransform.position
                + cameraTransform.forward * 2.2f
                - cameraTransform.right * 0.8f
                + cameraTransform.up * 0.1f;
            panel.rotation = Quaternion.LookRotation(panel.position - cameraTransform.position, Vector3.up);
        }
    }
}
