# MapEditor Prefab 导入与 Pico 导出说明

本文档说明 MapEditor 可摆放 prefab 的唯一导入路径、缩略图生成、地图 JSON 导出路径，以及 Game/Pico 端如何根据 `prefab_id` 显示地图物体。

## 1. Prefab 导入路径

所有可在 Pico MapEditor 中选择和放置的正式地图 prefab 必须放在：

```text
TreasureArenaUnity/Assets/_Project/Prefabs/MapEditor/Palette/Prefabs/
```

示例：

```text
TreasureArenaUnity/Assets/_Project/Prefabs/MapEditor/Palette/Prefabs/box_wood_01.prefab
TreasureArenaUnity/Assets/_Project/Prefabs/MapEditor/Palette/Prefabs/platform_metal_01.prefab
TreasureArenaUnity/Assets/_Project/Prefabs/MapEditor/Palette/Prefabs/table_round_01.prefab
```

`prefab_id` 使用 prefab 文件名去掉 `.prefab`，例如：

```text
box_wood_01.prefab -> box_wood_01
```

## 2. Prefab 必备组件

每个 prefab 根节点必须挂：

```text
MapExportMarker
```

字段要求：

```text
marker_type = MapObject
prefab_id = prefab 文件名去掉 .prefab
has_collider = true
id 可留空
```

每个 prefab 至少需要一个 Collider，用于：

```text
1. Pico 手柄射线命中
2. 表面放置
3. Game/Pico 加载后碰撞
```

复杂模型优先加简化 `BoxCollider`、`CapsuleCollider` 或低成本 `MeshCollider`。MVP 阶段不要直接给高面数模型使用复杂非 Convex MeshCollider。

## 3. 刷新 Brush 和缩略图

导入或修改 prefab 后，在 Unity Editor 执行：

```text
Tools/TreasureArena/地图编辑器/重新生成 Brush 缩略图
Tools/TreasureArena/地图编辑器/创建 MR 运行时输入控制器
```

缩略图生成位置：

```text
TreasureArenaUnity/Assets/_Project/Textures/MapEditor/BrushThumbnails/<prefab_id>.png
```

MapEditor 工作台 Brush 卡片会优先显示 `MapEditorRuntimeBrush.thumbnail`。如果 Unity 暂时无法生成真实 prefab preview，卡片会使用颜色 fallback，不会把默认图标写死成缩略图。

## 4. 重建 Prefab Registry

导入或删除 prefab 后，执行：

```text
Tools/TreasureArena/地图编辑器/重建 Map Prefab Registry
```

生成或更新：

```text
TreasureArenaUnity/Assets/_Project/ScriptableObjects/Map/MapPrefabRegistry.asset
```

`MapPrefabRegistry.asset` 保存：

```text
prefab_id -> GameObject prefab
```

Game / Pico 客户端加载地图 JSON 时，会用 JSON 里的 `objects[].prefab_id` 到该 registry 中查找 prefab 并实例化。

## 5. 地图 JSON 导出路径

Unity Editor 中点击 `Export JSON`：

```text
TreasureArenaUnity/Assets/_Project/StreamingAssets/Maps/<map_id>.json
```

Pico 真机中点击 `Export JSON`，主路径为：

```text
/storage/emulated/0/Android/data/com.DefaultCompany.TreasureArenaUnity/files/TreasureArenaMR/MapExports/<map_id>.json
```

如果 Pico 系统允许写入公开 Download 目录，还会额外写一份到：

```text
/storage/emulated/0/Download/TreasureArenaMR/MapExports/<map_id>.json
```

导出后 MapEditor 状态日志和 Console 会显示实际写入路径。

## 6. JSON 协议规则

地图 JSON 不保存 prefab 文件路径、缩略图路径、材质路径或模型路径。

普通地图物体只保存：

```text
object_id
prefab_id
position
rotation
scale
has_collider
```

因此同一个 `prefab_id` 必须同时存在于：

```text
1. MapEditor Brush 列表
2. MapPrefabRegistry.asset
3. Pico build 内置资源
```

否则 Game/Pico 加载时会报：

```text
Missing map prefab: <prefab_id>
```
