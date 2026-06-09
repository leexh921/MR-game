# MapTemplate_Gameplay 使用说明

本文档说明 Unity 场景 `MapTemplate_Gameplay.unity` 的用途、使用方法、已模拟流程，以及地图同学后续搭建正式地图时必须保留的结构和命名。

场景路径：

```text
TreasureArenaUnity/Assets/_Project/Scenes/MapTemplate_Gameplay.unity
```

## 1. 这个场景是做什么的

`MapTemplate_Gameplay` 是一张 MVP 模板地图。

它不是正式美术地图，而是用 Unity 原始物体搭出来的“最低可运行标准场景”。它的作用有三个：

```text
1. 给地图同学 C 一个参考：正式地图至少要有哪些东西。
2. 给程序同学一个参考：哪些物体名字会被后续逻辑识别。
3. 用占位 UI 和 Mock 流程跑通一次双队夺宝玩法闭环。
```

它当前使用的都是基础物体：

```text
Plane：地面
Cube：墙、掩体、物资箱
Cylinder：红蓝基地范围
Capsule：红蓝玩家、出生/复活标记
Sphere：宝物、宝物刷新点
```

后续正式地图可以替换模型、材质、美术表现，但不要随便改关键命名。

## 2. 场景里模拟了什么

这个场景通过 `LocalTestFlowDriver` 上挂载的 `TemplateMapFlowDemo` 脚本，模拟了一条完整的 TeamTreasure 流程：

```text
1. Reset：重置房间、玩家、宝物、比分。
2. Start：开始比赛。
3. Red Pickup：红队玩家拾取宝物。
4. Blue Attack：蓝队玩家攻击红队玩家。
5. 红队 HP 从 100 变成 0。
6. 红队进入 GhostRetreat 状态。
7. 红队携带的宝物掉落。
8. Red Base：红队玩家回到 red_base，开始复活。
9. Respawn：复活倒计时完成，红队恢复 Alive，HP 回到 100。
10. Blue Pickup：蓝队拾取掉落宝物。
11. Blue Submit：蓝队回到 blue_base 提交宝物。
12. Finish：比赛结束，蓝队得 10 分并获胜。
```

最终结果应该是：

```text
Red 0 : 10 Blue
Winner: Blue
```

注意：这不是正式 Netick 联机流程，也不写数据库。它只是本地 Mock 流程，用来验证玩法规则和 UI 占位显示。

## 3. 如何使用这个场景

### 3.1 打开场景

在 Unity 中打开：

```text
Assets/_Project/Scenes/MapTemplate_Gameplay.unity
```

进入 Play Mode 后，看屏幕下方按钮。

### 3.2 一键跑完整流程

点击：

```text
Run Full
```

它会自动执行：

```text
Reset
Start
Red Pickup
Blue Attack
Red Base
Respawn
Blue Pickup
Blue Submit
Finish
```

右侧 `LogPanel` 会显示每一步日志。

### 3.3 手动单步测试

也可以按顺序点击底部按钮：

```text
Reset
Start
Red Pickup
Blue Attack
Red Base
Respawn
Blue Pickup
Blue Submit
Finish
```

这样可以逐步观察：

```text
红队 HP 变化
红队状态变化
宝物状态变化
比分变化
结算面板变化
```

## 4. 主要 UI 区域说明

场景里有一个占位 HUD：

```text
TemplateHudCanvas
```

它包含：

```text
TopHud：顶部状态栏，显示房间状态、地图 ID、比分、提示。
PlayerStatePanel：左侧玩家状态，显示红蓝玩家 HP 和状态。
TreasureText：显示宝物状态和携带者。
LogPanel：右侧流程日志。
ResultPanelRoot：结算面板，比赛结束后显示。
ControlPanel：底部按钮区。
```

这些 UI 都是色块占位，不是最终美术。A/B 同学可以参考这里的字段和状态，后续接正式 HUD。

## 5. 地图同学必须保留的关键物体

正式地图可以换模型，但下面这些名字建议保留或按同样规则命名。

### 5.1 地图根节点

```text
MapTemplateRoot
```

下面分组：

```text
Geometry
Zones_team_bases
TreasureSpawnPoints
SupplyBoxes
Players_RuntimeMock
Treasures_RuntimeMock
Labels
```

正式地图至少应保留类似分组，方便程序查找和验收。

### 5.2 红蓝基地

```text
red_base
blue_base
```

含义：

```text
1. 队伍基地。
2. 出生区参考。
3. 复活区参考。
4. 宝物提交区。
```

当前地图 JSON 协议中对应：

```text
team_bases
```

不要再单独拆成 `spawn_zones` 和 `respawn_zones`，当前协议已经合并为 `team_bases`。

### 5.3 出生和复活标记

```text
red_spawn_marker
red_respawn_marker
blue_spawn_marker
blue_respawn_marker
```

这些是给人看的辅助标记，表示出生/复活推荐位置。

当前 MVP 中真正的逻辑区仍然以：

```text
red_base
blue_base
```

为准。

### 5.4 宝物刷新点

```text
treasure_point_001_Normal
treasure_point_002_Rare
treasure_point_003_Final
```

命名建议：

```text
treasure_point_编号_类型
```

例如：

```text
treasure_point_001_Normal
treasure_point_002_Rare
```

当前地图 JSON 协议中对应：

```text
treasure_spawn_points
```

### 5.5 物资箱

```text
supply_001
```

当前地图 JSON 协议中对应：

```text
supply_boxes
```

MVP 阶段物资箱可以先只是占位，不一定要实现刷新武器。

### 5.6 墙体和掩体

示例：

```text
wall_north
wall_south
wall_east
wall_west
cover_center_column
cover_mid_left
cover_mid_right
```

这些对应地图 JSON 的：

```text
objects
```

正式地图中墙体、掩体、障碍物都要有 Collider，保证射线和碰撞检测能命中。

### 5.7 MapExportMarker 标记导出（替换命名依赖）

从本版本开始，地图 JSON 导出不再依赖 GameObject 命名，而是通过 `MapExportMarker` 组件标记每个需要导出的物体。

**objects 支持两种粒度：**

1. **整图整体导出（当前 MVP 推荐）：**
   - 在 Geometry 或 StaticMapRoot 上挂一个 `MapExportMarker`（marker_type = MapObject）。
   - 导出 1 个 objects 条目，表示整张静态地图。
   - 子物体不需要额外挂 Marker。

2. **多 prefab 拼图导出（后续 Pico 编辑器推荐）：**
   - 每个墙体、掩体 prefab 各挂一个 `MapExportMarker`（marker_type = MapObject）。
   - 导出 N 个 objects 条目，每个条目对应一个独立 prefab。

详见 [docs/map_export_workflow.md](map_export_workflow.md)。

**玩法点必须单独标记导出：**

| 标记类型 | 用途 |
|---------|------|
| TeamBase | red_base / blue_base → team_bases |
| TreasureSpawnPoint | treasure_point_* → treasure_spawn_points |
| SupplyBox | supply_* → supply_boxes |
| Bounds | 地图边界 → bounds |

> 不改变 JSON 协议，不改变数据库字段。

## 6. 当前场景没有模拟什么

这个模板场景目前没有做：

```text
1. 真实 Pico 输入。
2. 真实 XR 射线攻击。
3. 真实 Trigger 自动检测玩家是否进入基地。
4. Netick 多人同步。
5. 数据库写入。
6. 地图 JSON 加载。
7. 真实宝物刷新。
8. 真实物资箱刷新。
9. 正式 UI 美术。
```

这些功能后续会由 Server、Network、MapLoader、Client、UI 模块逐步接入。

当前场景只负责：

```text
用基础物体和按钮，把玩法主流程可视化跑通。
```

## 7. C 同学正式搭地图时的验收清单

地图同学交付正式地图前，至少检查：

```text
1. 场景中有 red_base（挂 MapExportMarker，marker_type=TeamBase，team=Red）。
2. 场景中有 blue_base（挂 MapExportMarker，marker_type=TeamBase，team=Blue）。
3. 红蓝基地位置清晰，玩家能走进去。
4. red_base 和 blue_base 有 Collider，后续可以作为 Trigger 检测区域。
5. 至少有 1 个 treasure_point（挂 MapExportMarker，marker_type=TreasureSpawnPoint）。
6. 推荐至少有 Normal 和 Rare 两类宝物点。
7. 至少有 1 个 supply 点，MVP 可先占位。
8. 地图有边界或明显活动范围（如挂 MapExportMarker，marker_type=Bounds）。
9. 墙体、掩体、障碍物有 Collider。
10. 射线可以打到墙、掩体、玩家占位物。
11. Geometry 或 StaticMapRoot 挂 MapExportMarker（marker_type=MapObject）用于整图导出。
12. 不要把关键物体命名改成无法识别的随意名字。
13. 不要删除 .meta 文件。
14. 导出前确认所有需要导出的物体都已挂 MapExportMarker。
```

## 8. 后续接入方向

后续可以按这个顺序推进：

```text
1. MapTemplate_Gameplay 作为人工模板。
2. C 同学按这个模板搭正式地图。
3. MapLoader 读取 JSON 后生成同样命名/同样语义的物体。
4. Server 根据 team_bases 判断提交和复活。
5. Server 根据 treasure_spawn_points 生成宝物。
6. UI 根据规则结果刷新 HUD。
7. Netick 接入后，把 Mock 玩家替换成网络玩家。
```

这个模板场景的价值在于：先把“必须有什么”固定下来，再让美术和程序分别替换表现和逻辑。

