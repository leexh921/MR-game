# Map Export / Pico Game 场景不显示地图 — 排查进度

> 日期: 2026-06-17
> 分支: `feature/client-hud`

## 问题描述

在 Pico 端，MapEditor 导出的地图 JSON 在 Game 场景中不可见（含圆柱体基地、球体宝藏点等胶囊体），但 Home 场景中可见。Unity Editor 中两个场景都正常。

## 数据流

```
MapEditor.unity
  └─ MapEditorRuntimeExporter.ExportCurrentMap()
       └─ 写入 JSON 文件
            Editor:  Assets/_Project/StreamingAssets/Maps/{map_id}.json
            Device:  Application.persistentDataPath/MapExports/{map_id}.json

Game.unity / Home.unity
  └─ MapSceneSpawner.Update()
       ├─ ResolveMapId() → mapId
       └─ SpawnMap(mapId)
            └─ MapLoader.LoadFromMapId(mapId)
                 └─ 读取 JSON → 实例化地图物体
```

MapSceneSpawner 在 Game 场景中位于 **PicoClientNetworkBootstrap.prefab** 的根节点上，与 NetworkManager、PicoClientBootstrap 共用 GameObject。

## 已修复的问题

### 1. MapSceneSpawner 挂在 Canvas 上（已由用户手动修复）

- **问题**: Game.unity 中 MapSceneSpawner 原挂在 `GameHudCanvas` 上，导致生成的 3D MapRoot 成为 Canvas 子物体，Pico XR 渲染异常
- **修复**: 移到独立空对象上

### 2. 重复的 MapSceneSpawner（已由用户手动修复）

- **问题**: Game 场景中存在两个 MapSceneSpawner，一个 defaultMapId 为 `pico_map`，另一个为 `pico_map1`
- **修复**: 删除多余的那个

### 3. `_spawned` 加载失败后锁死（已改代码）

- **文件**: `Assets/_Project/Scripts/Map/MapSceneSpawner.cs`
- **原逻辑**:
  ```csharp
  SpawnMap(mapId);    // 失败也往下走
  _spawned = true;    // 永远锁死，不会重试
  ```
- **新逻辑**:
  ```csharp
  if (SpawnMap(mapId))           // SpawnMap 改成了返回 bool
  {
      _spawned = true;
      _lastSpawnedMapId = mapId; // 记录已加载的 mapId
  }
  // mapId 变化时重新加载，失败时下一帧重试
  ```

### 4. MapLoader Android 路径不可用（已改代码）

- **文件**: `Assets/_Project/Scripts/Map/MapLoader.cs`
- **原逻辑**: 只查 `Application.dataPath/_Project/StreamingAssets/Maps/`，Android 上不可用
- **新逻辑**: 按优先级尝试多个路径
  1. `Resources.Load<TextAsset>("Maps/" + mapId)` — 全平台通用
  2. `Application.persistentDataPath/MapExports/` — 设备私有存储
  3. `Application.dataPath/_Project/StreamingAssets/Maps/` — Editor 回退
- **新增** `SafeFileExists()` 方法包裹 `File.Exists`，防止 Android 存储权限异常

### 5. Download 路径导致 UnauthorizedAccessException（已改代码）

- **问题**: 曾添加硬编码 `/storage/emulated/0/Download/TreasureArenaMR/MapExports/` 路径，Android 10+ 无权访问外部存储
- **修复**: 已移除该路径

### 6. 添加调试日志（已改代码）

- `MapSceneSpawner.SpawnMap()` 入口打 `[MapSceneSpawner] SpawnMap ENTER: {mapId}`
- `MapLoader.LoadFromMapId()` 每个成功路径打 `[MapLoader] Loaded from ...`
- `MapLoader.LoadFromMapId()` 全部路径失败打 `[MapLoader] Not found in any path: {mapId}`

## 还需用户完成的操作

### P0: 将地图 JSON 放入 Resources

将 `Assets/_Project/StreamingAssets/Maps/*.json` 全部复制到 `Assets/_Project/Resources/Maps/`，确保 `Resources.Load` 能在 Pico 上读到。

现有 JSON 文件:
- `pico_map.json`
- `game.json`
- `map_editor.json`
- `map_template_gameplay.json`
- `new_map.json`

### P1: Clean Build + 重新部署

1. Unity: `File → Build Settings`，勾选 **Clean Build**
2. Build APK
3. 确认 APK 覆盖安装到 Pico

### P2: 验证部署

在 Logcat 中确认以下日志出现，说明新代码生效:
```
[MapSceneSpawner] SpawnMap ENTER: ...
[MapLoader] Loaded from ...
```

## 涉及的代码文件

| 文件 | 改动 |
|------|------|
| `Assets/_Project/Scripts/Map/MapSceneSpawner.cs` | `_spawned` 不提前锁死；`SpawnMap` 返回 `bool`；支持 mapId 变化重加载；加调试日志 |
| `Assets/_Project/Scripts/Map/MapLoader.cs` | 多路径尝试（Resources → persistentDataPath → Editor dataPath）；`SafeFileExists` 防异常；加调试日志 |
| `Assets/_Project/Scenes/Game.unity` | 用户手动：MapSceneSpawner 从 GameHudCanvas 移到独立对象；删除重复的 Spawner |


Write(MapExport_DEBUG_Progress.md)
  ⎿  Wrote 113 lines to
     MapExport_DEBUG_Progress.md
         示地图 — 排查进度co Game 场景不显
       2 
       4 > 分支: `feature/client-hud`
       5 
       7 ## 问题描述
       8 在 Pico 端，MapEditor 导出的地图 J
         基地、球体宝藏点等胶囊体），但 Hom
         e 场景中可见。Unity Editor 中两个
       9 场景都正常。
      10 ## 数据流
     … +160 lines (ctrl+o to expand)

  Thought for 2s (ctrl+o to expand)

● 已保存到 D:\MR\TreasureArenaUnity\MapExport_DEBU
  G_Progress.md。

  下一步：把 JSON 文件复制到
  Resources/Maps/，Clean Build，部署到
  Pico，然后看 Logcat 里有没有 SpawnMap ENTER 和
  MapLoader Loaded。