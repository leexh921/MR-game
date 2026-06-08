# 地图导出工作流说明

本文档说明如何使用 `MapExportMarker` 和 `MapSceneJsonExporter` 从 Unity 场景导出地图 JSON，以及两种 objects 粒度模式的区别。

---

## 1. 核心导出规则

`MapSceneJsonExporter` 遵循以下规则：

```text
1. 只导出挂载了 MapExportMarker 组件的 GameObject。
2. 不递归导出子物体 —— 子物体如果没有自己的 MapExportMarker，不会被导出。
3. 子物体如果挂载了自己的 MapExportMarker，则作为独立条目导出。
4. 该规则同时适用于 objects 和所有玩法标记（team_bases / treasure_spawn_points / supply_boxes / bounds）。
```

这意味着：

```text
父物体 Geometry 挂 MapExportMarker(MapObject)  → 只导出 Geometry 为 1 个 objects 条目
   ├── wall_north （无 MapExportMarker）       → 不导出
   ├── wall_south （无 MapExportMarker）       → 不导出
   └── red_base （有 MapExportMarker TeamBase） → 导出为 team_bases 条目

父物体 StaticMapRoot （无 MapExportMarker）    → 不导出
   ├── obj_wall_001 （有 MapExportMarker MapObject） → 导出为 objects 条目
   ├── obj_wall_002 （有 MapExportMarker MapObject） → 导出为 objects 条目
   └── red_base （有 MapExportMarker TeamBase）      → 导出为 team_bases 条目
```

---

## 2. objects 两种粒度模式

### 2.1 粗粒度：整图整体导出（当前 MVP 推荐）

**做法：**
在 Geometry 或 StaticMapRoot 上挂载一个 `MapExportMarker`，marker_type 设为 `MapObject`。

**效果：**
整个静态地图作为一个 objects 条目导出。JSON 中 objects 数组只有 1 个元素。

**示例 objects 条目：**
```json
{
  "objects": [
    {
      "object_id": "obj_static_map",
      "prefab_id": "static_map_template_01",
      "position": { "x": 0.0, "y": 0.0, "z": 0.0 },
      "rotation": { "x": 0.0, "y": 0.0, "z": 0.0 },
      "scale": { "x": 1.0, "y": 1.0, "z": 1.0 },
      "has_collider": true
    }
  ]
}
```

**适用场景：**
- MVP 阶段，地图由美术在 Unity 中手动搭建为单棵 GameObject 树
- 运行时一次性加载整个地图 prefab / AssetBundle
- 不需要按 prefab 粒度管理地图物件

**命名约定：**
- object_id 推荐：`obj_static_map` / `static_map_template_01`
- prefab_id 推荐：与 AssetBundle 或 Resources 中的 prefab 名称一致

---

### 2.2 细粒度：多 prefab 拼图导出（后续 Pico 编辑器推荐）

**做法：**
每个地图物件（墙、掩体、柱子等）作为独立 prefab 放置在场景中，每个都挂载一个 `MapExportMarker`，marker_type 设为 `MapObject`。

**效果：**
每个 prefab 物件作为独立的 objects 条目导出。JSON 中 objects 数组包含多个元素。

**示例 objects 条目：**
```json
{
  "objects": [
    {
      "object_id": "obj_wall_001",
      "prefab_id": "wall_01",
      "position": { "x": -10.0, "y": 0.0, "z": 0.0 },
      "rotation": { "x": 0.0, "y": 0.0, "z": 0.0 },
      "scale": { "x": 1.0, "y": 1.0, "z": 1.0 },
      "has_collider": true
    },
    {
      "object_id": "obj_wall_002",
      "prefab_id": "wall_01",
      "position": { "x": 10.0, "y": 0.0, "z": 0.0 },
      "rotation": { "x": 0.0, "y": 180.0, "z": 0.0 },
      "scale": { "x": 1.0, "y": 1.0, "z": 1.0 },
      "has_collider": true
    },
    {
      "object_id": "obj_cover_001",
      "prefab_id": "cover_01",
      "position": { "x": 0.0, "y": 0.0, "z": 5.0 },
      "rotation": { "x": 0.0, "y": 0.0, "z": 0.0 },
      "scale": { "x": 1.0, "y": 1.0, "z": 1.0 },
      "has_collider": true
    }
  ]
}
```

**适用场景：**
- Pico 地图编辑器，用户从 prefab 库拖放物件来拼装地图
- 需要按 prefab 粒度管理（增删改查单个物件）
- 需要在 Pico 端动态按 prefab 实例化

**命名约定：**
- object_id 推荐：`obj_wall_001` / `wall_01`、`obj_cover_001` / `cover_01`
- prefab_id 推荐：与 prefab 资源名称一致，如 `wall_01`、`cover_01`

---

## 3. 两种模式对比

| 维度 | 粗粒度（MVP） | 细粒度（编辑器） |
|------|--------------|------------------|
| MapExportMarker 数量 | 1 个（在根节点） | N 个（每 prefab 1 个） |
| objects 条目数 | 1 | N |
| 场景结构 | Geometry 下包含所有子物体 | 每个 prefab 平铺或分组 |
| 子物体导出 | 不导出（无独立 Marker） | 导出（各有 Marker） |
| 运行时加载 | 加载整张地图 | 逐 prefab 实例化 |
| 适合阶段 | MVP、手工搭建 | Pico 编辑器、程序化拼图 |

---

## 4. 玩法标记（必须单独导出）

无论使用哪种 objects 粒度，以下玩法标记必须始终作为独立条目分别挂载 MapExportMarker：

| 标记类型 | marker_type | 导出到 JSON 数组 | 说明 |
|---------|------------|-----------------|------|
| 红队基地 | TeamBase | team_bases | team=Red，作为出生/复活/提交区 |
| 蓝队基地 | TeamBase | team_bases | team=Blue，作为出生/复活/提交区 |
| 宝物刷新点 | TreasureSpawnPoint | treasure_spawn_points | treasure_type=Normal/Rare/Final |
| 物资箱 | SupplyBox | supply_boxes | 可选，MVP 可先占位 |
| 地图边界 | Bounds | bounds | 场景中应只有一个 |

> 注意：这些标记不能作为 MapObject 导出，必须使用正确的 marker_type。

---

## 5. MapExportMarker 标记不依赖命名

导出器判断语义的依据是 `MapExportMarker.marker_type` 字段，而不是 GameObject 名称。这意味着：

```text
GameObject 名叫 "red_base" 但没挂 MapExportMarker       → 不导出
GameObject 名叫 "随便什么" 但挂了 MapExportMarker TeamBase → 导出为 team_bases
```

但建议保留有意义的名称以便在 Unity Editor 中辨认。

---

## 6. 导出步骤

### 6.1 MVP 整图导出

```text
1. 在场景中搭建完整地图，所有静态物体放在 Geometry 或 StaticMapRoot 下。
2. 在 Geometry / StaticMapRoot 上添加 MapExportMarker 组件。
3. 设置 marker_type = MapObject。
4. 设置 object_id（如 obj_static_map）和 prefab_id。
5. 在其他物体（red_base、blue_base、treasure_point_*、supply_*、Bounds）上
   分别添加 MapExportMarker，设置正确的 marker_type。
6. 菜单 Tools → TreasureArena → Export Current Scene Map JSON。
7. 导出文件位于 StreamingAssets/Maps/<map_id>.json。
```

### 6.2 编辑器细粒度导出

```text
1. 从 prefab 库中放置物件到场景。
2. 每个 prefab 根节点自动（或手动）添加 MapExportMarker 组件。
3. 设置 marker_type = MapObject，填写 object_id 和 prefab_id。
4. 玩法标记与 MVP 相同，分别添加 MapExportMarker。
5. 导出步骤同 MVP。
```

---

## 7. 不改变协议

两种 objects 粒度模式使用完全相同的 JSON 结构（`MapObjectJson`），不改变 JSON 协议字段，不影响数据库设计。

后续 Pico 编辑器只需在 `objects` 数组中增加条目，无需修改 protocol 或数据库 schema。

---

## 8. 与 MapLoader 的关系

`MapLoader` 读取 JSON 后，遍历 `objects` 数组加载地图：

```text
粗粒度：遍历 1 个条目 → 加载 1 个整图 prefab → 所有子物体随 prefab 出现
细粒度：遍历 N 个条目 → 加载 N 个独立 prefab → 拼成完整地图
```

MapLoader 不需要区分两种模式 —— 它始终逐条目实例化。
