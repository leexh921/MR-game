# 地图编辑器 MVP 设计文档

> 责任人：李潇涵（审核）+ 待定（开发）  
> 文档状态：ME-0B 设计冻结  
> 关联文档：`json_protocol.md`、`project_design.md`、`map_export_workflow.md`、`map_template_gameplay_guide.md`

---

## 1. 地图编辑器 MVP 目标

地图编辑器 MVP 是一个在 `TreasureArenaUnity` 工程内运行的独立场景/模块，目标：

```text
1. 让地图制作者（C 同学或后续用户）可以在 Unity Editor 或 Pico MR 环境中摆放地图物件。
2. 让地图制作者可以放置所有玩法标记（基地、宝物点、物资箱、边界）。
3. 编辑完成后可导出一份符合当前 JSON 协议的地图 JSON 文件。
4. 导出的 JSON 必须通过 MapValidator 校验。
5. 导出的 JSON 可由 MapLoader 直接加载并在 Game 场景中生成完整地图。
```

编辑器 MVP **不负责**：

```text
1. 战斗判定、HP、武器、宝物拾取/提交 —— 这些是 Server 权威范围。
2. 数据库写入 —— 数据库由 Server/Manager 端负责。
3. 运行时游戏逻辑 —— 编辑器只产出静态地图数据。
4. 多人协同编辑。
5. 跨网络远程编辑。
```

---

## 2. 模块边界

### 2.1 编辑器负责

```text
1. Prefab 库管理（读取可用 prefab 列表）。
2. 物件放置、选择、移动、旋转、缩放、删除。
3. 玩法标记放置与参数设置。
4. 地图元数据编辑（map_id、map_name、map_version、description）。
5. 导出地图 JSON。
6. 调用 MapValidator 校验导出结果。
7. 调用 MapLoader 预览地图加载效果（可选，Editor 模式）。
```

### 2.2 编辑器不负责

```text
1. 战斗判定、HP 管理、武器系统。
2. 宝物拾取/提交/掉落逻辑。
3. 玩家复活/濒死撤离。
4. 数据库写入。
5. 房间管理、网络同步。
6. 实时多人协同编辑。
```

### 2.3 边界图

```text
┌─────────────────────────────────────────────────┐
│              MapEditor 模块                      │
│                                                 │
│  Prefab库 → 放置物件 → 编辑Transform → 标记导出   │
│                         ↓                       │
│                   MapExportMarker               │
│                         ↓                       │
│              MapSceneJsonExporter               │
│                         ↓                       │
│                   MapValidator                  │
│                         ↓                       │
│                   地图 JSON 文件                  │
└──────────────────────┬──────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────┐
│              Game 模块 (Server/Client)            │
│                                                 │
│              MapLoader → 生成场景                 │
│              Server 权威判断                      │
└─────────────────────────────────────────────────┘
```

编辑器产出的是纯数据文件，不参与运行时逻辑。

---

## 3. objects 如何生成

### 3.1 编辑器中放置物件

用户从 prefab 库中选择一个地图物件（墙、掩体、柱子等），放置到场景中。每次放置：

```text
1. 实例化 prefab。
2. 在实例化的 GameObject 上自动添加 MapExportMarker 组件。
3. 设置 marker_type = MapObject。
4. 自动生成 object_id（如 obj_wall_001，按类型递增编号）。
5. 自动设置 prefab_id（从 prefab 资源名获取）。
6. 用户可手动修改 object_id，但编辑器应提示保持唯一性。
7. has_collider 从 prefab 检测或默认为 true。
```

### 3.2 支持两种 objects 粒度

与 `map_export_workflow.md` 一致，编辑器同时支持：

| 模式 | 适用场景 | objects 条目数 |
|------|---------|---------------|
| 粗粒度整图 | 美术手动搭建的完整地图 root prefab | 1 |
| 细粒度拼图 | Pico MR 逐 prefab 拼装 | N |

编辑器默认工作在细粒度模式（逐 prefab 放置），但也允许用户导入一个整图 root prefab 作为单个 objects 条目。

### 3.3 编辑器内 objects 数据结构

每个编辑器中放置的物件在内存中对应：

```text
object_id       : string   // 编辑器自动生成或用户指定
prefab_id       : string   // 从 prefab 资源读取
position        : Vector3  // Unity 世界坐标
rotation        : Vector3  // Euler angles
scale           : Vector3  // localScale
has_collider    : bool     // 默认 true
```

导出时每个物件生成一个 `MapObjectJson` 条目。

---

## 4. team_bases 如何生成

### 4.1 红蓝基地语义

`team_bases` 是当前 JSON 协议中唯一的基础区域字段，同时承担三种语义：

```text
1. 出生区（Spawn Zone）：玩家开局出生位置。
2. 复活区（Respawn Zone）：GhostRetreat 玩家走回此处开始复活倒计时。
3. 宝物提交区（Submit Zone）：玩家携带宝物进入己方基地提交得分。
```

> 不再拆分为独立的 `spawn_zones` 和 `respawn_zones`。Server 端根据 `team_bases` 和队伍归属统一判断。

### 4.2 编辑器中放置基地

```text
1. 用户从标记面板选择"红队基地"或"蓝队基地"。
2. 编辑器中放置一个带 TeamBase 标记的 GameObject（可用圆柱/球体可视化范围）。
3. 自动添加 MapExportMarker：
     - marker_type = TeamBase
     - team = Red（或 Blue）
     - base_id = "red_base"（或 "blue_base"）
     - radius = 默认 1.2（可调）
4. 编辑器中至少需要 1 个红队基地和 1 个蓝队基地。
5. MapValidator 会拒绝缺少红/蓝基地的地图。
```

### 4.3 基地放置约束（编辑器应提示）

```text
1. 红蓝基地必须各至少 1 个。
2. 红蓝基地位置应保持合理距离（不建议紧贴在一起）。
3. radius 应 > 0，建议 ≥ 0.5m。
4. 基地建议有 Collider，以便 Server 做 Trigger 区域检测。
```

---

## 5. treasure_spawn_points 如何生成

### 5.1 编辑器中放置宝物刷新点

```text
1. 用户从标记面板选择宝物刷新点类型：Normal / Rare / Final。
2. 编辑器中放置一个带 TreasureSpawnPoint 标记的 GameObject。
3. 自动添加 MapExportMarker：
     - marker_type = TreasureSpawnPoint
     - point_id = 自动生成（如 treasure_point_001）
     - treasure_type = Normal / Rare / Final
     - radius = 默认 0.5（可调）
4. 至少需要 1 个宝物刷新点。
5. MapValidator 会拒绝缺少宝物刷新点的地图。
```

### 5.2 宝物类型说明

| 类型 | 默认分值 | 说明 |
|------|---------|------|
| Normal | 10 | 普通宝物，可多处放置 |
| Rare | 30 | 稀有宝物，建议 1-2 个 |
| Final | 50 | 终极宝物，MVP 可选 |

---

## 6. supply_boxes 如何生成

### 6.1 编辑器中放置物资箱

```text
1. 用户从标记面板选择"物资箱"。
2. 编辑器中放置一个带 SupplyBox 标记的 GameObject。
3. 自动添加 MapExportMarker：
     - marker_type = SupplyBox
     - box_id = 自动生成（如 supply_001）
     - supply_type = 默认 "WeaponRandom"（可改为具体武器 ID）
     - refresh_interval = 默认 20s（可调）
4. 物资箱在 MVP 中为可选，MapValidator 允许 supply_boxes 为空。
```

### 6.2 物资箱约束

```text
1. supply_type 不为空。
2. refresh_interval > 0。
```

---

## 7. map_boundary 如何生成

### 7.1 编辑器中绘制地图边界

```text
1. 用户进入 Draw Bounds / Draw Area 模式。
2. 用 Trigger 在真实地面上逐点绘制地图边界。
3. 所有边界点贴当前 editorFloorY / floorPlane。
4. 至少 3 个点后可闭合边界。
5. 编辑器保存一个地图级 MapEditorBoundaryData，不通过 MapExportMarker 导出。
6. 导出 JSON 时写入 map_boundary：
     - boundary_type = "Polygon"
     - height = 默认 2.5
     - points = 按绘制顺序排列的边界点，最后一点不重复首点
7. 每张地图最多 1 个 map_boundary。
8. map_boundary 为必填字段，MapValidator 会拒绝缺失或少于 3 个点的地图。
```

### 7.2 map_boundary 用途

```text
1. 定义地图有效活动范围。
2. Server 可用于基于 Polygon XZ 点内检测 + height 判断玩家是否离开地图。
3. 编辑器预览时可视化显示边界。
4. 不再依赖固定 Bounds box prefab。
```

---

## 8. MapExportMarker、MapValidator、MapLoader 的关系

### 8.1 整体流程

```text
编辑器中放置物件/标记
        │
        ▼
每个导出物件/玩法点挂载 MapExportMarker，地图边界保存为 MapEditorBoundaryData
        │
        ▼
编辑器保存按钮 → 调用 MapSceneJsonExporter（或编辑器内等价逻辑）并收集 MapEditorBoundaryData
        │
        ▼
所有 MapExportMarker 被扫描，按 marker_type 分类写入 MapJson 结构；MapEditorBoundaryData 写入 map_boundary
        │
        ▼
MapJson 传入 MapValidator.Validate()
        │
        ├── 失败 → 编辑器显示错误列表，阻止导出
        │
        └── 通过 → 序列化为 JSON 文件 → 写入 StreamingAssets/Maps/
                        │
                        ▼
               MapLoader 在 Game 场景中读取 JSON → 逐条目实例化/生成物体
```

### 8.2 各模块职责

| 模块 | 角色 | 关键动作 |
|------|------|---------|
| MapExportMarker | 数据标记 | 挂在 GameObject 上，存储 object_id / prefab_id / marker_type / transform 等 |
| MapSceneJsonExporter | 数据收集 | 扫描所有 MapExportMarker，按类型写入 MapJson 结构，并写入 map_boundary |
| MapValidator | 数据校验 | 检查 MapJson 是否满足最低要求（红蓝基地、宝物点、地图边界等） |
| MapLoader | 数据消费 | 读取 MapJson，实例化 prefab，读取基地、宝物、物资箱、map_boundary |

### 8.3 编辑器中的调用关系

```text
编辑器不直接调用 MapLoader（除非预览模式）。
编辑器导出流程：
  1. 遍历场景中所有带 MapExportMarker 的 GameObject。
  2. 按 marker_type 构造 MapJson。
  3. 收集 MapEditorBoundaryData 并写入 map_boundary。
  4. 调用 MapValidator.Validate(map)。
  5. 成功则写入 JSON 文件。
```

> 编辑器在 ME-1~ME-3 阶段可复用 `MapSceneJsonExporter.ExportCurrentSceneToDefaultPath()`。
> ME-5 及之后如有特殊需求可编写编辑器专用导出逻辑，但输出格式必须一致。

---

## 9. 动画 prefab 规则

### 9.1 原则

```text
地图 JSON 只描述静态空间信息，不保存运行时动画状态。
```

### 9.2 具体规则

```text
1. 编辑器只保存 prefab_id 和 transform（position / rotation / scale）。
2. 动画状态（AnimationController 参数、Animator state、时间轴位置）不写入地图 JSON。
3. 交互状态（是否已被拾取、是否已刷新）不写入地图 JSON —— 这些由 Server 在运行时管理。
4. has_collider 只表示该物件是否有碰撞体，不表达物理/动画行为。
5. 如果将来需要"动画物件"（如旋转平台、移动门），其动画由 prefab 自身在运行时驱动，
   地图 JSON 只需要 prefab_id 和初始 transform，不额外增加字段。
```

### 9.3 如果确需动画配置（未来扩展）

```text
若未来需要配置动画参数（如"此门初始为打开"），应：
  1. 先提交协议变更建议。
  2. 在 objects 条目中增加可选的 animation_config 字段。
  3. 不影响现有字段和数据库 schema。

当前 MVP 不涉及此项。
```

---

## 10. ME-1 到 ME-7 验收标准

### ME-1：Unity Editor 模拟编辑器

```text
目标：在 Unity Editor 中创建 MapEditor 场景，实现基本物件编辑能力。

验收标准：
  1. MapEditor 场景可运行，有独立 UI 面板。
  2. Prefab 库面板显示可用地图物件列表（墙、掩体、柱子等，可从 StreamingAssets/Prefabs 或 Resources 读取）。
  3. 可点击 prefab 列表中某物件，放置到场景指定位置（射线/点击放置）。
  4. 可选中已放置物件，用 Gizmo 或 UI 控件移动/旋转/缩放。
  5. 可删除选中物件。
  6. 放置的物件自动挂载 MapExportMarker（marker_type=MapObject）。
  7. 编辑器不依赖任何外部网络或 Pico 设备即可运行。
```

### ME-2：玩法标记放置

```text
目标：支持 TeamBase / TreasureSpawnPoint / SupplyBox / Bounds 的放置和参数编辑。

验收标准：
  1. 标记面板显示 5 种标记类型按钮。
  2. 放置 red_base / blue_base：
     - 自动挂 MapExportMarker（marker_type=TeamBase，team=Red/Blue）。
     - 可编辑 radius。
     - 场景中以可视圆柱/球体显示范围。
  3. 放置 treasure spawn point：
     - 可选 Normal/Rare/Final 类型。
     - 可编辑 radius。
  4. 放置 supply box：
     - 可编辑 supply_type 和 refresh_interval。
  5. 绘制 map_boundary：
     - 进入 Draw Bounds / Draw Area 模式逐点绘制 Polygon。
     - 所有边界点贴真实地面。
     - 至少 3 个点后可闭合。
  6. 所有标记的 MapExportMarker 参数可在 Inspector 面板中修改，地图边界不使用 MapExportMarker。
```

### ME-3：地图保存与验证

```text
目标：编辑器可导出地图 JSON，并通过 MapValidator 校验。

验收标准：
  1. 编辑器有"导出"按钮，点击后收集所有 MapExportMarker 数据和 MapEditorBoundaryData。
  2. 构造 MapJson 对象，写入 map_boundary，调用 MapValidator.Validate()。
  3. 校验失败时：
     - 显示具体错误列表（如"缺少红队基地""缺少宝物刷新点"）。
     - 阻止导出，不生成 JSON 文件。
  4. 校验通过时：
     - 生成 JSON 文件到 StreamingAssets/Maps/<map_id>.json。
     - map_id 由用户在导出前填写或从场景名自动生成。
     - 显示导出成功提示及文件路径。
  5. 导出 JSON 格式与 json_protocol.md 完全一致，无额外字段。
```

### ME-4：MapLoader 加载验证

```text
目标：使用 MapLoader 加载编辑器导出的 JSON，验证物件和标记位置正确。

验收标准：
  1. Game 场景可使用编辑器导出的 JSON 作为地图数据。
  2. MapLoader 正确实例化所有 objects 条目中的 prefab，位置/旋转/缩放与编辑器一致。
  3. MapLoader 正确生成 team_bases（红蓝基地区域）。
  4. MapLoader 正确生成 treasure_spawn_points（宝物刷新点）。
  5. MapLoader 正确生成 supply_boxes（物资箱）。
  6. MapLoader 正确读取 map_boundary（地图边界）。
  7. 编辑器导出 → MapLoader 加载 → Play Mode 可视化验证，三者闭环通过。
```

### ME-5：Pico MR 输入适配

```text
目标：编辑器适配 Pico 手柄/射线输入，支持在 MR 环境中放置物件。

验收标准：
  1. Pico 客户端可进入 MapEditor 模式。
  2. 手柄射线可指向 MR 空间中的平面/地面，触发放置。
  3. 手柄按键可选中、移动、旋转、缩放已放置物件。
  4. 手柄按键可删除选中物件。
  5. 标记放置（基地、宝物等）可通过手柄操作完成。
  6. 编辑器 UI 面板在 MR 中可见且可交互。
  7. MR 环境中物件放置位置与导出 JSON 中位置一致。
```

### ME-6：Prefab 动画和交互规范

```text
目标：制定动画 prefab 和交互物件的导出规范，确保编辑器不越界写入运行时状态。

验收标准：
  1. 梳理项目中所有含 Animator / Animation 的 prefab。
  2. 确认每个动画 prefab 的地图 JSON 条目只包含 prefab_id 和 transform。
  3. 动画状态（AnimationController 参数、Animator state、时间轴位置）不写入地图 JSON。
  4. 交互状态（拾取/提交/刷新/开门/触发）不写入地图 JSON —— 由 Server 在运行时管理。
  5. 编写动画 prefab 规范文档（可合并到本文档 §9 或独立文档），供 C 同学后续制作 prefab 时参考。
  6. 如有需要动画初始配置的 prefab，先提交协议变更建议，不直接在编辑器中新增字段。
```

### ME-7：集成验收

```text
目标：编辑器导出的地图 JSON 可被完整多人夺宝闭环正常加载和运行。

验收标准：
  1. 使用编辑器导出的地图 JSON 启动一局完整的 TeamTreasure 对局。
  2. 红蓝玩家在各自基地出生。
  3. 宝物在 treasure_spawn_points 位置生成。
  4. 物资箱在 supply_boxes 位置生成。
  5. 地图边界 map_boundary 生效。
  6. 墙体/掩体 Collider 正常阻挡射线和玩家移动。
  7. GhostRetreat 玩家走回己方基地可触发复活倒计时。
  8. 玩家进入己方基地可提交宝物。
  9. 全程无 JSON 格式错误、无对象 ID 冲突、无加载异常。
```

> **后续可选：编辑器拆分为独立 App。**  
> 在 ME-7 完成后，如需要可将 MapEditor 拆成独立 Unity App，与主游戏工程解耦。  
> 此项属于增强版功能，不作为地图编辑器 MVP 必做项。  
> 验收标准：独立 App 可运行在 Pico 4 上，共享 JSON 协议和 MapValidator，导出 JSON 可直接复制到主游戏使用。

---

## 11. 与其他文档的关系

| 文档 | 关系 |
|------|------|
| `json_protocol.md` | 编辑器输出的 JSON 格式定义，编辑器必须严格遵循 |
| `project_design.md` | 编辑器在项目整体 MVP 中的定位和优先级 |
| `map_export_workflow.md` | 编辑器实现导出时的具体工作流参考 |
| `map_template_gameplay_guide.md` | 正式地图搭建规范，编辑器应引导用户满足这些规范 |
| `database_design.md` | 编辑器不涉及数据库，不影响数据库 schema |

---

## 12. 禁止事项

```text
1. 不得新增 JSON 协议字段。如需扩展，必须先提交协议变更建议并获得确认。
2. 不得修改数据库 schema。
3. 不得在编辑器中实现战斗判定逻辑。
4. 不得将动画状态写入地图 JSON。
5. 不得将交互状态（拾取/提交/刷新）写入地图 JSON。
6. 编辑器导出的 JSON 不得包含当前 MapJson 协议定义字段之外的字段（即仅限 map_id / map_name / map_version / description / editor_origin / floor_calibration / objects / team_bases / treasure_spawn_points / supply_boxes / map_boundary）。
```
