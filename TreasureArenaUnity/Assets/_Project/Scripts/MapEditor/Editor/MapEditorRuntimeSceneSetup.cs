using TreasureArenaMR.Map;
using TreasureArenaMR.Shared;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace TreasureArenaMR.MapEditor.Editor
{
    public static class MapEditorRuntimeSceneSetup
    {
        private const string RuntimeRootName = "MapEditorRuntime";
        private const string PlacedRootName = "RuntimePlacedObjects";
        private const string RayOriginName = "EditorFallbackRayOrigin";
        private const string PanelName = "MapEditorRuntimePanel";
        private const string DockedCanvasName = "MapEditorDockedCanvas";
        private const string MapEditorPrefabRoot = "Assets/_Project/Prefabs/MapEditor";
        private const string PaletteDir = "Assets/_Project/Prefabs/MapEditor/Palette";
        private const string BasicShapesDir = "Assets/_Project/Prefabs/MapEditor/Palette/Basic Shapes";
        private const string BrushThumbnailDir = "Assets/_Project/Textures/MapEditor/BrushThumbnails";
        private static readonly string[] BrushSearchFolders =
        {
            "Assets/_Project/Prefabs/MapEditor/MapObjects",
            "Assets/_Project/Prefabs/MapEditor/GameplayMarkers",
            "Assets/_Project/Prefabs/MapEditor/Palette"
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
            serializedController.FindProperty("activeBrushIndex").intValue = -1;
            serializedController.ApplyModifiedProperties();

            CreateOrUpdateDockedCanvas(controller, exporter);

            Selection.activeGameObject = runtimeRoot;
            EditorUtility.SetDirty(runtimeRoot);
            EditorUtility.SetDirty(placedRoot);
            EditorUtility.SetDirty(rayOrigin);
            Debug.Log("MapEditor MR runtime input controller created. Save the scene to keep it.");
        }

        private static void CreateOrUpdateDockedCanvas(MapEditorRuntimeController controller, MapEditorRuntimeExporter exporter)
        {
            EnsureEventSystem();

            GameObject oldPanel = GameObject.Find(PanelName);
            if (oldPanel != null)
            {
                Undo.DestroyObjectImmediate(oldPanel);
            }

            bool hadDockedCanvas = false;
            Vector3 savedPosition = Vector3.zero;
            Quaternion savedRotation = Quaternion.identity;
            Vector3 savedScale = Vector3.one;
            Vector2 savedSize = new Vector2(1200f, 560f);
            float savedDistance = 0.95f;
            float savedVerticalOffset = 0.45f;
            float savedTilt = 28f;
            float savedYawOffset = 180f;
            Vector2 savedDesignSize = new Vector2(1200f, 560f);
            float savedWorldScale = 0.001f;

            GameObject[] existingCanvases = GameObject.FindObjectsOfType<GameObject>(true);
            for (int i = 0; i < existingCanvases.Length; i++)
            {
                if (existingCanvases[i].name == DockedCanvasName)
                {
                    if (!hadDockedCanvas)
                    {
                        hadDockedCanvas = true;
                        Transform oldTransform = existingCanvases[i].transform;
                        savedPosition = oldTransform.position;
                        savedRotation = oldTransform.rotation;
                        savedScale = oldTransform.localScale;
                        RectTransform oldRect = existingCanvases[i].GetComponent<RectTransform>();
                        if (oldRect != null)
                        {
                            savedSize = oldRect.sizeDelta;
                        }

                        MapEditorDockedUiController oldUi = existingCanvases[i].GetComponent<MapEditorDockedUiController>();
                        if (oldUi != null)
                        {
                            SerializedObject oldSerialized = new SerializedObject(oldUi);
                            savedDistance = oldSerialized.FindProperty("workbenchDistance").floatValue;
                            savedVerticalOffset = oldSerialized.FindProperty("workbenchVerticalOffset").floatValue;
                            savedTilt = oldSerialized.FindProperty("workbenchTiltDegrees").floatValue;
                            savedYawOffset = oldSerialized.FindProperty("workbenchYawOffset").floatValue;
                            savedDesignSize = oldSerialized.FindProperty("workbenchDesignSize").vector2Value;
                            savedWorldScale = oldSerialized.FindProperty("workbenchWorldScale").floatValue;
                        }
                    }

                    Undo.DestroyObjectImmediate(existingCanvases[i]);
                }
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MapEditorDockedUiPrefabBuilder.PrefabPath);
            if (prefab == null)
            {
                prefab = MapEditorDockedUiPrefabBuilder.CreateOrUpdatePrefab();
            }

            GameObject canvasGo = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (canvasGo != null)
            {
                Undo.RegisterCreatedObjectUndo(canvasGo, "Create MapEditor Docked Canvas");
            }

            if (canvasGo == null)
            {
                Debug.LogError("Failed to create MapEditorDockedCanvas.");
                return;
            }

            if (hadDockedCanvas)
            {
                canvasGo.transform.SetPositionAndRotation(savedPosition, savedRotation);
                canvasGo.transform.localScale = savedScale;
                RectTransform rect = canvasGo.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.sizeDelta = savedSize;
                }

                MapEditorDockedUiController newUi = canvasGo.GetComponent<MapEditorDockedUiController>();
                if (newUi != null)
                {
                    SerializedObject newSerialized = new SerializedObject(newUi);
                    newSerialized.FindProperty("workbenchDistance").floatValue = savedDistance;
                    newSerialized.FindProperty("workbenchVerticalOffset").floatValue = savedVerticalOffset;
                    newSerialized.FindProperty("workbenchTiltDegrees").floatValue = savedTilt;
                    newSerialized.FindProperty("workbenchYawOffset").floatValue = savedYawOffset;
                    newSerialized.FindProperty("workbenchDesignSize").vector2Value = savedDesignSize;
                    newSerialized.FindProperty("workbenchWorldScale").floatValue = savedWorldScale;
                    newSerialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            else
            {
                PlacePanelNearCamera(canvasGo.transform);
                canvasGo.transform.localScale = Vector3.one * 0.001f;
            }

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = Camera.main;
            }

            if (canvasGo.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
            {
                Undo.AddComponent<TrackedDeviceGraphicRaycaster>(canvasGo);
            }

            MapEditorDockedUiController dockedUi = canvasGo.GetComponent<MapEditorDockedUiController>();
            if (dockedUi != null)
            {
                SerializedObject serializedUi = new SerializedObject(dockedUi);
                serializedUi.FindProperty("controller").objectReferenceValue = controller;
                serializedUi.FindProperty("exporter").objectReferenceValue = exporter;
                serializedUi.FindProperty("targetCamera").objectReferenceValue = Camera.main;
                serializedUi.ApplyModifiedProperties();
                EditorUtility.SetDirty(dockedUi);
            }

            MapEditorObjectInspector inspector = canvasGo.GetComponentInChildren<MapEditorObjectInspector>(true);
            if (inspector != null)
            {
                SerializedObject serializedInspector = new SerializedObject(inspector);
                serializedInspector.FindProperty("controller").objectReferenceValue = controller;
                serializedInspector.ApplyModifiedProperties();
                EditorUtility.SetDirty(inspector);
            }

            EditorUtility.SetDirty(canvasGo);
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
            EventSystem eventSystem = UnityEngine.Object.FindObjectOfType<EventSystem>();
            if (eventSystem != null)
            {
                if (eventSystem.GetComponent<XRUIInputModule>() == null)
                {
                    Undo.AddComponent<XRUIInputModule>(eventSystem.gameObject);
                    EditorUtility.SetDirty(eventSystem.gameObject);
                }

                return;
            }

            GameObject eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule), typeof(XRUIInputModule));
            Undo.RegisterCreatedObjectUndo(eventSystemGo, "Create EventSystem");
            EditorUtility.SetDirty(eventSystemGo);
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
            EnsureDefaultPalettePrefabs();
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
            brush.FindPropertyRelative("folder").stringValue = GetBrushFolder(prefabPath);
            brush.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            brush.FindPropertyRelative("thumbnail").objectReferenceValue = GetOrCreateThumbnail(prefab);
            brush.FindPropertyRelative("prefab_id").stringValue = prefabId;
            ApplyBrushMarkerDefaults(brush, prefab.name, marker);
            brush.FindPropertyRelative("supply_type").stringValue = marker != null ? marker.supply_type : "WeaponRandom";
            brush.FindPropertyRelative("radius").floatValue = marker != null ? marker.radius : 1f;
            brush.FindPropertyRelative("refresh_interval").floatValue = marker != null ? marker.refresh_interval : 20f;
            brush.FindPropertyRelative("has_collider").boolValue = prefab.GetComponentInChildren<Collider>() != null;
        }

        private static Sprite GetOrCreateThumbnail(GameObject prefab)
        {
            if (prefab == null)
            {
                return null;
            }

            EnsureThumbnailFolder();
            string safeName = prefab.name.Replace(" ", "_");
            string assetPath = BrushThumbnailDir + "/" + safeName + ".png";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            Texture2D source = AssetPreview.GetAssetPreview(prefab);
            if (source == null)
            {
                source = AssetPreview.GetMiniThumbnail(prefab);
            }

            if (source == null)
            {
                return null;
            }

            Texture2D readable = CopyReadableTexture(source, 128, 128);
            byte[] png = readable.EncodeToPNG();
            Object.DestroyImmediate(readable);
            File.WriteAllBytes(assetPath, png);
            AssetDatabase.ImportAsset(assetPath);

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static Texture2D CopyReadableTexture(Texture2D source, int width, int height)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture temporary = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(source, temporary);
            RenderTexture.active = temporary;
            Texture2D readable = new Texture2D(width, height, TextureFormat.ARGB32, false);
            readable.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            readable.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(temporary);
            return readable;
        }

        private static void EnsureThumbnailFolder()
        {
            EnsureFolder("Assets/_Project/Textures", "MapEditor");
            EnsureFolder("Assets/_Project/Textures/MapEditor", "BrushThumbnails");
        }

        private static void ApplyBrushMarkerDefaults(SerializedProperty brush, string prefabName, MapExportMarker marker)
        {
            foreach (var entry in sGameplayMarkerDefaults)
            {
                if (prefabName == entry.prefabName)
                {
                    brush.FindPropertyRelative("marker_type").enumValueIndex = (int)entry.markerType;
                    brush.FindPropertyRelative("team").enumValueIndex = (int)entry.team;
                    brush.FindPropertyRelative("treasure_type").enumValueIndex = (int)entry.treasureType;
                    return;
                }
            }

            brush.FindPropertyRelative("marker_type").enumValueIndex = marker != null ? (int)marker.marker_type : (int)MapExportMarkerType.MapObject;
            brush.FindPropertyRelative("team").enumValueIndex = marker != null ? (int)marker.team : (int)TeamType.None;
            brush.FindPropertyRelative("treasure_type").enumValueIndex = marker != null ? (int)marker.treasure_type : (int)TreasureType.Normal;
        }

        private static readonly BrushMarkerDefault[] sGameplayMarkerDefaults = new BrushMarkerDefault[]
        {
            new BrushMarkerDefault("TeamBase_Red",    MapExportMarkerType.TeamBase,           TeamType.Red,   TreasureType.Normal),
            new BrushMarkerDefault("TeamBase_Blue",   MapExportMarkerType.TeamBase,           TeamType.Blue,  TreasureType.Normal),
            new BrushMarkerDefault("Treasure_Normal", MapExportMarkerType.TreasureSpawnPoint, TeamType.None,  TreasureType.Normal),
            new BrushMarkerDefault("Treasure_Rare",   MapExportMarkerType.TreasureSpawnPoint, TeamType.None,  TreasureType.Rare),
            new BrushMarkerDefault("Treasure_Final",  MapExportMarkerType.TreasureSpawnPoint, TeamType.None,  TreasureType.Final),
            new BrushMarkerDefault("SupplyBox",       MapExportMarkerType.SupplyBox,          TeamType.None,  TreasureType.Normal),
            new BrushMarkerDefault("Bounds",          MapExportMarkerType.Bounds,             TeamType.None,  TreasureType.Normal),
        };

        private struct BrushMarkerDefault
        {
            public readonly string prefabName;
            public readonly MapExportMarkerType markerType;
            public readonly TeamType team;
            public readonly TreasureType treasureType;
            public BrushMarkerDefault(string prefabName, MapExportMarkerType markerType, TeamType team, TreasureType treasureType)
            {
                this.prefabName = prefabName;
                this.markerType = markerType;
                this.team = team;
                this.treasureType = treasureType;
            }
        }

        private static string GetBrushFolder(string prefabPath)
        {
            string directory = System.IO.Path.GetDirectoryName(prefabPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(directory) || !directory.StartsWith(MapEditorPrefabRoot))
            {
                return "Imported";
            }

            string folder = directory.Substring(MapEditorPrefabRoot.Length).Trim('/');
            if (folder.StartsWith("Palette/"))
            {
                folder = folder.Substring("Palette/".Length);
            }

            return string.IsNullOrEmpty(folder) ? "MapEditor" : folder;
        }

        private static void EnsureDefaultPalettePrefabs()
        {
            EnsureFolder(MapEditorPrefabRoot, "Palette");
            EnsureFolder(PaletteDir, "Basic Shapes");
            EnsurePrimitivePrefab("Basic_Cube", PrimitiveType.Cube, Vector3.one);
            EnsurePrimitivePrefab("Basic_Cylinder", PrimitiveType.Cylinder, Vector3.one);
            EnsurePrimitivePrefab("Basic_Circle", PrimitiveType.Cylinder, new Vector3(1f, 0.04f, 1f));
        }

        private static void EnsureFolder(string parent, string folderName)
        {
            string path = parent + "/" + folderName;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private static void EnsurePrimitivePrefab(string prefabName, PrimitiveType primitiveType, Vector3 scale)
        {
            string path = BasicShapesDir + "/" + prefabName + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                return;
            }

            GameObject go = GameObject.CreatePrimitive(primitiveType);
            go.name = prefabName;
            go.transform.localScale = scale;
            MapExportMarker marker = go.AddComponent<MapExportMarker>();
            marker.marker_type = MapExportMarkerType.MapObject;
            marker.id = prefabName;
            marker.prefab_id = prefabName;
            marker.has_collider = true;
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        private static void PlacePanelNearCamera(Transform panel)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                panel.position = new Vector3(0f, 0.95f, 0.9f);
                panel.rotation = Quaternion.Euler(60f, 180f, 0f);
                return;
            }

            Transform cameraTransform = camera.transform;
            Vector3 flatForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);
            if (flatForward.sqrMagnitude < 0.001f)
            {
                flatForward = Vector3.ProjectOnPlane(cameraTransform.up, Vector3.up);
            }

            flatForward.Normalize();
            panel.position = cameraTransform.position
                + flatForward * 0.95f
                - Vector3.up * 0.45f;
            Quaternion faceUser = Quaternion.LookRotation(cameraTransform.position - panel.position, Vector3.up);
            panel.rotation = faceUser * Quaternion.Euler(28f, 0f, 0f);
        }
    }
}
