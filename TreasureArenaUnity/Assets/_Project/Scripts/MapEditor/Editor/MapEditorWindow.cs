using System;
using System.Collections.Generic;
using System.IO;
using TreasureArenaMR.Map;
using TreasureArenaMR.Map.Editor;
using TreasureArenaMR.Shared;
using UnityEditor;
using UnityEngine;

namespace TreasureArenaMR.MapEditor
{
    public enum MapEditorCategory
    {
        Geometry,
        TeamBase,
        TreasureSpawn,
        SupplyBox
    }

    [Serializable]
    public sealed class MapEditorPrefabEntry
    {
        public string label;
        public string prefabName;
        public MapEditorCategory category;
        public MapExportMarkerType markerType;
        public TeamType team;
        public TreasureType treasureType;
        public string supplyType = "WeaponRandom";
        public bool isMarkerOnly;
    }

    public sealed class MapEditorWindow : EditorWindow
    {
        private const string MapObjectsDir = "Assets/_Project/Prefabs/MapEditor/MapObjects";
        private const string GameplayMarkersDir = "Assets/_Project/Prefabs/MapEditor/GameplayMarkers";

        private string mapName = "NewMap";
        private string mapDescription = "";
        private bool showMapInfo = true;
        private bool showGeometry = true;
        private bool showTeamBases = true;
        private bool showTreasure = true;
        private bool showSupply = true;
        private Vector2 paletteScroll;
        private Vector2 objectListScroll;

        private List<string> availableMaps = new List<string>();
        private int selectedMapIndex = -1;

        private Dictionary<string, GameObject> prefabLookup = new Dictionary<string, GameObject>();
        private List<MapEditorPrefabEntry> prefabEntries;
        private MapEditorPrefabEntry activeBrush;
        private GameObject previewGhost;
        private bool isPlacing;
        private string statusMessage;
        private double statusClearTime;

        [MenuItem("Tools/TreasureArena/地图编辑器")]
        public static void ShowWindow()
        {
            MapEditorWindow window = GetWindow<MapEditorWindow>("地图编辑器");
            window.minSize = new Vector2(360, 520);
            window.Show();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            RefreshPrefabCache();
            BuildPrefabEntries();
            RefreshMapList();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            EndPlacement();
        }

        private void RefreshPrefabCache()
        {
            prefabLookup.Clear();
            string[] allGuids = AssetDatabase.FindAssets("t:Prefab", new[] { MapObjectsDir, GameplayMarkersDir });
            foreach (string guid in allGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    prefabLookup[prefab.name] = prefab;
                }
            }
        }

        private void BuildPrefabEntries()
        {
            prefabEntries = new List<MapEditorPrefabEntry>();

            // Auto-scan MapObjects directory — users add their own prefabs here
            string[] mapObjectGuids = AssetDatabase.FindAssets("t:Prefab", new[] { MapObjectsDir });
            foreach (string guid in mapObjectGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    prefabEntries.Add(new MapEditorPrefabEntry
                    {
                        label = prefab.name,
                        prefabName = prefab.name,
                        category = MapEditorCategory.Geometry,
                        markerType = MapExportMarkerType.MapObject
                    });
                }
            }

            // Gameplay markers — fixed semantics, not auto-generated from folder scan
            prefabEntries.Add(new MapEditorPrefabEntry { label = "红队基地",           prefabName = "TeamBase_Red",    category = MapEditorCategory.TeamBase,       markerType = MapExportMarkerType.TeamBase,           team = TeamType.Red });
            prefabEntries.Add(new MapEditorPrefabEntry { label = "蓝队基地",          prefabName = "TeamBase_Blue",   category = MapEditorCategory.TeamBase,       markerType = MapExportMarkerType.TeamBase,           team = TeamType.Blue });
            prefabEntries.Add(new MapEditorPrefabEntry { label = "普通宝物点",  prefabName = "Treasure_Normal", category = MapEditorCategory.TreasureSpawn,  markerType = MapExportMarkerType.TreasureSpawnPoint,  treasureType = TreasureType.Normal });
            prefabEntries.Add(new MapEditorPrefabEntry { label = "稀有宝物点",    prefabName = "Treasure_Rare",   category = MapEditorCategory.TreasureSpawn,  markerType = MapExportMarkerType.TreasureSpawnPoint,  treasureType = TreasureType.Rare });
            prefabEntries.Add(new MapEditorPrefabEntry { label = "最终宝物点",   prefabName = "Treasure_Final",  category = MapEditorCategory.TreasureSpawn,  markerType = MapExportMarkerType.TreasureSpawnPoint,  treasureType = TreasureType.Final });
            prefabEntries.Add(new MapEditorPrefabEntry { label = "物资箱",         prefabName = "SupplyBox",       category = MapEditorCategory.SupplyBox,      markerType = MapExportMarkerType.SupplyBox,           supplyType = "WeaponRandom" });
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawStatusBar();
            EditorGUILayout.Space();
            DrawMapInfo();
            EditorGUILayout.Space();
            DrawPlacementBrush();
            EditorGUILayout.Space();
            DrawPalette();
            EditorGUILayout.Space();
            DrawActions();
            EditorGUILayout.Space();
            DrawObjectList();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("TreasureArena 地图编辑器", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshPrefabCache();
                RefreshMapList();
                SetStatus("Prefab 缓存和地图列表已刷新。");
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStatusBar()
        {
            if (!string.IsNullOrEmpty(statusMessage) && EditorApplication.timeSinceStartup < statusClearTime)
            {
                EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
            }
        }

        private void DrawMapInfo()
        {
            showMapInfo = EditorGUILayout.BeginFoldoutHeaderGroup(showMapInfo, "地图信息");
            if (!showMapInfo)
            {
                EditorGUILayout.EndFoldoutHeaderGroup();
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Map file selector
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("已有地图", GUILayout.Width(60));
            if (availableMaps.Count == 0)
            {
                EditorGUILayout.LabelField("（无已保存的地图）", EditorStyles.miniLabel);
            }
            else
            {
                string[] mapNames = availableMaps.ToArray();
                selectedMapIndex = EditorGUILayout.Popup(selectedMapIndex, mapNames);
                Color oldBg = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.9f, 0.6f, 0.2f);
                if (GUILayout.Button("加载", GUILayout.Width(50), GUILayout.Height(18)))
                {
                    if (selectedMapIndex >= 0 && selectedMapIndex < availableMaps.Count)
                        LoadMapById(availableMaps[selectedMapIndex]);
                }
                GUI.backgroundColor = oldBg;
            }
            if (GUILayout.Button("刷新", EditorStyles.miniButton, GUILayout.Width(40), GUILayout.Height(18)))
            {
                RefreshMapList();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            mapName = EditorGUILayout.TextField("地图名称", mapName);
            mapDescription = EditorGUILayout.TextField("地图描述", mapDescription);
            string mapId = MapSceneJsonExporter.ToMapId(mapName);
            EditorGUILayout.LabelField("地图 ID（自动）", mapId);
            EditorGUILayout.LabelField("版本", "1.0.0");
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawPlacementBrush()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("当前笔刷", EditorStyles.boldLabel);
            if (activeBrush != null)
            {
                string modeLabel = isPlacing ? "（放置中：在 Scene 视图点击）" : "（已选择）";
                Color oldColor = GUI.color;
                GUI.color = isPlacing ? Color.green : Color.white;
                EditorGUILayout.LabelField($"  {activeBrush.label} {modeLabel}");
                GUI.color = oldColor;
            }
            else
            {
                EditorGUILayout.LabelField("  （无：请在下方选择 prefab）");
            }

            if (isPlacing && GUILayout.Button("取消放置", GUILayout.Height(24)))
            {
                EndPlacement();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawPalette()
        {
            EditorGUILayout.LabelField("Prefab 面板", EditorStyles.boldLabel);
            paletteScroll = EditorGUILayout.BeginScrollView(paletteScroll, GUILayout.Height(200));

            showGeometry   = DrawCategory("地图物体",           showGeometry,   MapEditorCategory.Geometry);
            showTeamBases  = DrawCategory("玩法点 - 红蓝基地",         showTeamBases,  MapEditorCategory.TeamBase);
            showTreasure   = DrawCategory("玩法点 - 宝物刷新点",    showTreasure,   MapEditorCategory.TreasureSpawn);
            showSupply     = DrawCategory("玩法点 - 物资箱",       showSupply,     MapEditorCategory.SupplyBox);

            EditorGUILayout.EndScrollView();
        }

        private bool DrawCategory(string title, bool expanded, MapEditorCategory category)
        {
            expanded = EditorGUILayout.Foldout(expanded, title, true);
            if (expanded)
            {
                EditorGUI.indentLevel++;
                foreach (MapEditorPrefabEntry entry in prefabEntries)
                {
                    if (entry.category != category) continue;
                    bool isActive = (activeBrush == entry);
                    Color oldBg = GUI.backgroundColor;
                    if (isActive) GUI.backgroundColor = new Color(0.3f, 0.7f, 0.3f);
                    if (GUILayout.Button(entry.label, GUILayout.Height(24)))
                    {
                        SelectBrush(entry);
                    }
                    GUI.backgroundColor = oldBg;
                }
                EditorGUI.indentLevel--;
            }
            return expanded;
        }

        private void SelectBrush(MapEditorPrefabEntry entry)
        {
            if (isPlacing) EndPlacement();
            activeBrush = entry;
            isPlacing = true;
            Tools.hidden = true;
            SetStatus($"正在放置 {entry.label}。在 Scene 视图点击放置，右键或 Esc 取消。");
            SceneView.RepaintAll();
        }

        private void EndPlacement()
        {
            if (previewGhost != null)
            {
                DestroyImmediate(previewGhost);
                previewGhost = null;
            }
            isPlacing = false;
            activeBrush = null;
            Tools.hidden = false;
            SceneView.RepaintAll();
            Repaint();
        }

        // ---- Scene View placement ----

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!isPlacing || activeBrush == null) return;

            Event e = Event.current;
            int controlId = GUIUtility.GetControlID(FocusType.Passive);

            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (!groundPlane.Raycast(ray, out float distance)) return;

            Vector3 hitPoint = ray.GetPoint(distance);
            Vector3 snapped = SnapToGrid(hitPoint);

            switch (e.type)
            {
                case EventType.MouseMove:
                    UpdatePreviewGhost(snapped);
                    sceneView.Repaint();
                    break;
                case EventType.MouseDown:
                    if (e.button == 0)
                    {
                        PlaceObject(snapped);
                        GUIUtility.hotControl = controlId;
                        e.Use();
                    }
                    else if (e.button == 1)
                    {
                        EndPlacement();
                        e.Use();
                    }
                    break;
                case EventType.KeyDown:
                    if (e.keyCode == KeyCode.Escape)
                    {
                        EndPlacement();
                        e.Use();
                    }
                    break;
                case EventType.Layout:
                    HandleUtility.AddDefaultControl(controlId);
                    break;
            }
        }

        private void UpdatePreviewGhost(Vector3 position)
        {
            if (previewGhost == null && prefabLookup.TryGetValue(activeBrush.prefabName, out GameObject prefab))
            {
                previewGhost = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                previewGhost.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
                Color ghostCol = new Color(0f, 1f, 0.5f, 0.4f);
                SetObjectMaterial(previewGhost, ghostCol);
            }

            if (previewGhost != null)
            {
                previewGhost.transform.position = position;
            }
        }

        private static void SetObjectMaterial(GameObject go, Color color)
        {
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
            {
                // Clone the object's existing material (which already uses the correct
                // RP shader for the current platform) instead of hardcoding Shader.Find("Standard").
                Material mat = new Material(r.sharedMaterial);
                mat.color = color;
                mat.SetFloat("_Mode", 3);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.renderQueue = 3000;
                r.sharedMaterial = mat;
            }
        }

        private void PlaceObject(Vector3 position)
        {
            if (!prefabLookup.TryGetValue(activeBrush.prefabName, out GameObject prefab)) return;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = position;

            string baseName = activeBrush.prefabName;
            int count = CountObjectsWithPrefix(baseName);
            instance.name = $"{baseName}_{count + 1:D2}";

            MapExportMarker marker = instance.GetComponent<MapExportMarker>();
            if (marker == null) marker = instance.AddComponent<MapExportMarker>();

            marker.id = instance.name;
            marker.marker_type = activeBrush.markerType;
            marker.team = activeBrush.team;
            marker.treasure_type = activeBrush.treasureType;
            marker.prefab_id = activeBrush.prefabName;
            if (!string.IsNullOrEmpty(activeBrush.supplyType)) marker.supply_type = activeBrush.supplyType;

            Undo.RegisterCreatedObjectUndo(instance, $"Place {activeBrush.label}");
            Selection.activeGameObject = instance;
            SetStatus($"已放置 {instance.name}，位置 {position}");
        }

        private static int CountObjectsWithPrefix(string prefix)
        {
            int c = 0;
            foreach (MapExportMarker m in FindObjectsOfType<MapExportMarker>(true))
                if (m.name.StartsWith(prefix)) c++;
            return c;
        }

        private static Vector3 SnapToGrid(Vector3 p)
        {
            const float g = 0.5f;
            return new Vector3(Mathf.Round(p.x / g) * g, 0f, Mathf.Round(p.z / g) * g);
        }

        // ---- Actions ----

        private void DrawActions()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            Color oldBg = GUI.backgroundColor;

            GUI.backgroundColor = new Color(0.3f, 0.7f, 0.3f);
            if (GUILayout.Button("保存地图 JSON", GUILayout.Height(32))) SaveMap();
            GUI.backgroundColor = new Color(0.3f, 0.5f, 0.9f);
            if (GUILayout.Button("验证地图", GUILayout.Height(32))) ValidateCurrentScene();
            GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f);
            if (GUILayout.Button("清空全部", GUILayout.Height(32))) ClearAllMarkers();

            GUI.backgroundColor = oldBg;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void SaveMap()
        {
            if (isPlacing) EndPlacement();

            MapJsonModels.MapJson map = BuildMapFromScene();
            MapValidationResult validation = new MapValidator().Validate(map);
            if (!validation.ok)
            {
                EditorUtility.DisplayDialog("保存失败", validation.GetErrorText(), "确定");
                SetStatus($"保存失败：{validation.GetErrorText()}");
                return;
            }

            string directory = Path.Combine(Application.dataPath, "_Project/StreamingAssets/Maps");
            Directory.CreateDirectory(directory);
            string mapId = MapSceneJsonExporter.ToMapId(mapName);
            string path = Path.Combine(directory, mapId + ".json");
            File.WriteAllText(path, JsonUtility.ToJson(map, true));
            AssetDatabase.Refresh();
            Debug.Log("Map JSON exported: " + path);
            RefreshMapList();
            EditorUtility.DisplayDialog("保存成功", path, "确定");
            SetStatus($"已保存：{path}");
        }

        private void ValidateCurrentScene()
        {
            if (isPlacing) EndPlacement();
            MapJsonModels.MapJson map = BuildMapFromScene();
            MapValidationResult r = new MapValidator().Validate(map);
            if (r.ok) { EditorUtility.DisplayDialog("地图验证", "地图合法。", "确定"); SetStatus("验证通过。"); }
            else { EditorUtility.DisplayDialog("地图验证失败", r.GetErrorText(), "确定"); SetStatus($"验证失败：{r.GetErrorText()}"); }
        }

        private MapJsonModels.MapJson BuildMapFromScene()
        {
            MapJsonModels.MapJson map = MapSceneJsonBuilder.BuildFromMarkers(mapName, mapDescription, FindObjectsOfType<MapExportMarker>(true));
            return map;
        }

        private void RefreshMapList()
        {
            availableMaps.Clear();
            string directory = Path.Combine(Application.dataPath, "_Project/StreamingAssets/Maps");
            if (!Directory.Exists(directory)) return;

            string[] files = Directory.GetFiles(directory, "*.json");
            foreach (string f in files)
            {
                string name = Path.GetFileNameWithoutExtension(f);
                if (!string.IsNullOrEmpty(name))
                    availableMaps.Add(name);
            }
            availableMaps.Sort();
        }

        private void LoadMapById(string mapId)
        {
            if (isPlacing) EndPlacement();
            string path = Path.Combine(Application.dataPath, "_Project/StreamingAssets/Maps", mapId + ".json");
            if (!File.Exists(path)) { EditorUtility.DisplayDialog("加载失败", $"未找到文件：{path}", "确定"); return; }

            MapLoadResult loadResult = new MapLoader().LoadFromPath(path);
            if (!loadResult.ok) { EditorUtility.DisplayDialog("加载失败", loadResult.error, "确定"); return; }

            // Populate UI fields from loaded map
            mapName = loadResult.map.map_name;
            mapDescription = loadResult.map.description ?? "";

            ClearAllMarkersSilent();
            MapRuntimeBuilder builder = new MapRuntimeBuilder();
            GameObject root = builder.Build(loadResult.map);
            AddMarkersToBuiltObjects(root, loadResult.map);
            SetStatus($"已加载：{mapId}");
            Repaint();
        }

        private static void AddMarkersToBuiltObjects(GameObject root, MapJsonModels.MapJson map)
        {
            Transform t;
            t = root.transform.Find("Objects");
            if (t != null && map.objects != null)
                for (int i = 0; i < t.childCount && i < map.objects.Count; i++)
                {
                    var d = map.objects[i];
                    var m = t.GetChild(i).gameObject.AddComponent<MapExportMarker>();
                    m.marker_type = MapExportMarkerType.MapObject; m.id = d.object_id; m.prefab_id = d.prefab_id; m.has_collider = d.has_collider;
                }
            t = root.transform.Find("TeamBases");
            if (t != null && map.team_bases != null)
                for (int i = 0; i < t.childCount && i < map.team_bases.Count; i++)
                {
                    var d = map.team_bases[i];
                    var m = t.GetChild(i).gameObject.AddComponent<MapExportMarker>();
                    m.marker_type = MapExportMarkerType.TeamBase; m.id = d.base_id; m.radius = d.radius; m.team = d.team == "Red" ? TeamType.Red : TeamType.Blue;
                }
            t = root.transform.Find("TreasureSpawnPoints");
            if (t != null && map.treasure_spawn_points != null)
                for (int i = 0; i < t.childCount && i < map.treasure_spawn_points.Count; i++)
                {
                    var d = map.treasure_spawn_points[i];
                    var m = t.GetChild(i).gameObject.AddComponent<MapExportMarker>();
                    m.marker_type = MapExportMarkerType.TreasureSpawnPoint; m.id = d.point_id; m.radius = d.radius;
                    m.treasure_type = d.treasure_type == "Normal" ? TreasureType.Normal : d.treasure_type == "Rare" ? TreasureType.Rare : TreasureType.Final;
                }
            t = root.transform.Find("SupplyBoxes");
            if (t != null && map.supply_boxes != null)
                for (int i = 0; i < t.childCount && i < map.supply_boxes.Count; i++)
                {
                    var d = map.supply_boxes[i];
                    var m = t.GetChild(i).gameObject.AddComponent<MapExportMarker>();
                    m.marker_type = MapExportMarkerType.SupplyBox; m.id = d.box_id; m.supply_type = d.supply_type; m.refresh_interval = d.refresh_interval;
                }
        }

        private void ClearAllMarkersSilent()
        {
            foreach (MapExportMarker m in FindObjectsOfType<MapExportMarker>(true))
                Undo.DestroyObjectImmediate(m.gameObject);
        }

        private void ClearAllMarkers()
        {
            if (!EditorUtility.DisplayDialog("清空全部", "是否移除场景中的所有地图对象？", "确认", "取消")) return;
            if (isPlacing) EndPlacement();
            ClearAllMarkersSilent();
            SetStatus("已清空所有地图对象。");
        }

        // ---- Object list ----

        private void DrawObjectList()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("场景中的对象", EditorStyles.boldLabel);
            MapExportMarker[] markers = FindObjectsOfType<MapExportMarker>(true);

            if (markers.Length == 0) { EditorGUILayout.LabelField("  （无）", EditorStyles.miniLabel); EditorGUILayout.EndVertical(); return; }

            EditorGUILayout.LabelField($"  总数：{markers.Length}");
            objectListScroll = EditorGUILayout.BeginScrollView(objectListScroll, GUILayout.Height(150));
            foreach (MapExportMarker m in markers)
            {
                if (m == null) continue;
                EditorGUILayout.BeginHorizontal();
                Rect r = GUILayoutUtility.GetRect(12, 12, GUILayout.Width(12));
                EditorGUI.DrawRect(r, TypeColor(m.marker_type));
                EditorGUILayout.LabelField(m.GetDefaultId(), GUILayout.Width(120));
                EditorGUILayout.LabelField(m.marker_type.ToString(), EditorStyles.miniLabel, GUILayout.Width(90));
                if (GUILayout.Button("选中", EditorStyles.miniButtonLeft, GUILayout.Width(42))) { Selection.activeGameObject = m.gameObject; SceneView.FrameLastActiveSceneView(); }
                if (GUILayout.Button("删", EditorStyles.miniButtonRight, GUILayout.Width(24))) { Undo.DestroyObjectImmediate(m.gameObject); SetStatus($"已删除 {m.GetDefaultId()}"); }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private static Color TypeColor(MapExportMarkerType t)
        {
            switch (t)
            {
                case MapExportMarkerType.MapObject:          return new Color(0.5f, 0.5f, 0.5f);
                case MapExportMarkerType.TeamBase:           return new Color(0.9f, 0.3f, 0.3f);
                case MapExportMarkerType.TreasureSpawnPoint: return new Color(1f, 0.75f, 0.1f);
                case MapExportMarkerType.SupplyBox:          return new Color(0.2f, 0.8f, 0.3f);
                default: return Color.white;
            }
        }

        private void SetStatus(string msg) { statusMessage = msg; statusClearTime = EditorApplication.timeSinceStartup + 5.0; Repaint(); }
    }
}
