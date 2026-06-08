# 地图编辑器 UI 设计方案

> 关联文档：`map_editor_design.md`、`json_protocol.md`、`map_export_workflow.md`  
> 设计目标：让地图制作者在 Unity Editor 和 Pico MR 环境中高效摆放地图物件、编辑玩法标记、校验并导出地图 JSON，同时尽量不遮挡场景视线。

---

## 1. UI 设计原则

```text
1. 四周停靠，不占中心视野。
2. 面板半透明，编辑地图时能看见背后的场景轮廓。
3. 所有面板可折叠、展开、固定。
4. 默认只显示常用工具，详细参数按需展开。
5. MR 中优先大按钮、少层级、可手柄射线点击。
6. Unity Editor 中保留鼠标键盘高效操作。
7. 不在 UI 中实现战斗、HP、得分、拾取等服务器权威逻辑。
8. 不新增或修改地图 JSON 协议字段。
```

中心区域始终留给地图编辑视图。UI 只围绕四边出现，避免遮挡放置点、射线落点和已放置物体。

---

## 2. 整体布局

### 2.1 四周面板布局

```text
┌──────────────────────────────────────────────────────────────┐
│ Top Bar：地图信息 / 模式 / 保存状态 / 校验导出                 │
├──────────────┬──────────────────────────────┬────────────────┤
│ Left Panel   │                              │ Right Panel    │
│ Prefab/Marker│          Scene View          │ Object Inspect │
│ Brush Library│      放置、选择、移动区域       │ Transform/Marker│
│              │                              │ Parameters     │
├──────────────┴──────────────────────────────┴────────────────┤
│ Bottom Bar：快捷工具 / 坐标吸附 / 旋转缩放步进 / 日志提示       │
└──────────────────────────────────────────────────────────────┘
```

### 2.2 面板行为

每个面板都有三种状态：

| 状态 | 表现 | 用途 |
|---|---|---|
| 展开 | 显示完整内容，半透明背景 | 正在使用该面板时 |
| 折叠 | 只显示窄条、图标和标题 | 不挡视野，保留入口 |
| 固定 | 保持展开，不因失焦自动收起 | 需要连续操作时 |

面板默认透明度建议：

```text
展开：75% 背景不透明度
折叠：45% 背景不透明度
悬停/射线指向：90% 背景不透明度
编辑拖拽物体时：非固定面板自动降到 35%
```

---

## 3. Top Bar：地图与导出状态

### 3.1 位置

屏幕顶部横向停靠。MR 中放在视野前方上缘，不压住射线中心。

### 3.2 内容

```text
左侧：
  MapEditor 标题
  当前地图名
  当前编辑模式：Place / Move / Rotate / Scale

中间：
  map_id
  version
  当前对象数量
  红蓝基地数量
  宝物点数量

右侧：
  Validate 按钮
  Export 按钮
  Preview Load 按钮（ME-4 可选）
  保存/校验状态图标
```

### 3.3 状态提示

| 状态 | 显示 |
|---|---|
| 未校验 | 灰色圆点 + `Not validated` |
| 校验通过 | 绿色圆点 + `Valid map` |
| 校验失败 | 红色圆点 + `Fix required` |
| 已导出 | 蓝色圆点 + 最近导出路径简写 |

Top Bar 不展示长错误列表，只展示摘要。详细错误放到底部日志或右侧 Validation 面板。

---

## 4. Left Panel：笔刷库与玩法标记

### 4.1 位置

左侧竖向停靠，可折叠为一列图标。MR 中建议宽度不超过视野宽度的 22%。

### 4.2 Tabs

```text
Map Objects
Gameplay Markers
Recent
Favorites（可选）
```

### 4.3 Map Objects

用于普通地图物体：

```text
Wall
Floor
Cover
Column
Obstacle
Whole Map Root（可选）
```

每个条目显示：

```text
缩略图或简单图标
显示名
prefab_id
是否有 Collider
```

点击条目后：

```text
1. 当前 Brush 切换为该 prefab。
2. 中心场景出现半透明幽灵预览。
3. Top Bar 的模式自动切到 Place。
4. Bottom Bar 显示放置快捷提示。
```

### 4.4 Gameplay Markers

固定显示 5 类玩法标记：

| 标记 | 默认参数 |
|---|---|
| Red Base | `marker_type=TeamBase`、`team=Red`、`radius=1.2` |
| Blue Base | `marker_type=TeamBase`、`team=Blue`、`radius=1.2` |
| Treasure Point | `marker_type=TreasureSpawnPoint`、`treasure_type=Normal`、`radius=0.5` |
| Supply Box | `marker_type=SupplyBox`、`supply_type=WeaponRandom`、`refresh_interval=20` |
| Bounds | `marker_type=Bounds`、`localScale=bounds.size` |

Treasure Point 点击后展开二级选择：

```text
Normal
Rare
Final
```

### 4.5 折叠状态

折叠后只显示：

```text
地图物体图标
玩法标记图标
最近使用图标
展开箭头
固定图钉
```

---

## 5. Right Panel：对象属性面板

### 5.1 位置

右侧竖向停靠。选中对象时自动展开；未选中对象时可折叠。

### 5.2 未选中状态

显示：

```text
Selected: none
提示：选择一个已放置对象以编辑参数
```

### 5.3 选中普通 MapObject

显示字段：

```text
object_id
prefab_id（只读）
position x/y/z
rotation x/y/z
scale x/y/z
has_collider
```

操作按钮：

```text
Duplicate
Delete
Focus
Reset Transform
```

### 5.4 选中 TeamBase

额外显示：

```text
base_id
team：Red / Blue
radius
```

提示规则：

```text
如果缺少 Red Base：显示红色提示
如果缺少 Blue Base：显示红色提示
如果 radius <= 0：显示错误
```

### 5.5 选中 TreasureSpawnPoint

额外显示：

```text
point_id
treasure_type：Normal / Rare / Final
radius
```

### 5.6 选中 SupplyBox

额外显示：

```text
box_id
supply_type
refresh_interval
```

### 5.7 选中 Bounds

额外显示：

```text
bounds center（来自 position）
bounds size（来自 localScale）
```

Bounds 面板显示警告：

```text
场景中建议只保留 1 个 Bounds。
```

---

## 6. Bottom Bar：编辑工具与反馈

### 6.1 位置

底部横向停靠。默认展开为低高度工具条，必要时可上拉展开日志。

### 6.2 工具区

```text
Place
Move
Rotate
Scale
Delete
Clear Brush
Undo（后续可选）
Redo（后续可选）
```

### 6.3 参数区

```text
Grid Snap：On/Off
Grid Size：0.25 / 0.5 / 1.0
Rotate Step：15 / 30 / 45
Scale Step：0.1 / 0.25 / 0.5
```

### 6.4 日志区

默认显示最近 1 条状态，例如：

```text
Placed wall_01_03 at (1.0, 0.0, 2.5)
Selected red_base
Map validation failed: missing blue base
Map exported: .../MapExports/pico_map.json
```

展开后显示最近 10 条操作记录和校验错误列表。

---

## 7. MR 手柄交互设计

### 7.1 基础操作

| 操作 | 建议输入 |
|---|---|
| 射线指向 | 右手柄 forward ray |
| 放置 / 选择 | Trigger |
| 移动选中对象 | Grip + 手柄射线落点 |
| 旋转 | 摇杆左右 |
| 缩放 | 摇杆上下 |
| 切换模式 | Secondary Button |
| 删除 | Primary Button |
| 取消笔刷 | Esc / MR 中使用 Clear Brush 按钮 |

### 7.2 UI 面板交互

MR 中面板按钮需要更大：

```text
最小点击高度：36-44 px 等效视觉高度
按钮间距：至少 8 px
高亮状态：射线 hover 时边框变亮
点击反馈：按钮短暂加亮 + 状态栏显示动作结果
```

### 7.3 避免遮挡策略

```text
1. 用户正在拖动物体时，未固定面板自动降低透明度。
2. 用户射线靠近面板边缘时，面板恢复清晰。
3. 用户长时间不操作某面板时，该面板自动折叠。
4. Pin 后不自动折叠。
```

---

## 8. Unity Editor 交互设计

### 8.1 鼠标键盘

```text
鼠标左键：放置 / 选择
鼠标左键拖拽：Move 模式下移动选中对象
Tab：切换编辑模式
Delete：删除选中对象
Q / E：左旋 / 右旋
- / =：缩小 / 放大
Esc：清除当前笔刷
```

### 8.2 与 MR 输入互斥

```text
当 XR 设备有效时：
  鼠标放置、移动、旋转、缩放输入不生效。
  Esc / Tab 等辅助键可以保留。

当 XR 设备无效时：
  启用 Editor 鼠标键盘回退操作。
```

---

## 9. 校验与导出 UI

### 9.1 Validate 流程

点击 Validate：

```text
1. 收集所有 MapExportMarker。
2. 构造 MapJson。
3. 调用 MapValidator.Validate(map)。
4. Top Bar 显示通过/失败摘要。
5. Bottom Bar 展开错误列表。
```

### 9.2 错误展示

错误按严重程度显示：

| 类型 | 示例 | UI |
|---|---|---|
| Blocking Error | 缺少 Blue Base | 红色，阻止导出 |
| Warning | Bounds 数量超过 1 | 黄色，可继续但建议修复 |
| Info | supply_boxes 为空 | 灰色说明 |

MVP 阶段只要 MapValidator 返回失败，就统一按 Blocking Error 处理。

### 9.3 Export 流程

点击 Export：

```text
1. 自动先执行 Validate。
2. 失败：阻止导出，显示错误。
3. 成功：写入 JSON。
4. 显示导出路径。
```

导出路径：

```text
Unity Editor 导出：Assets/_Project/StreamingAssets/Maps/<map_id>.json
Pico Runtime 导出：Application.persistentDataPath/MapExports/<map_id>.json
```

---

## 10. 地图元数据面板

地图元数据不常改，建议放在 Top Bar 的地图名点击弹窗中，或作为 Right Panel 的 Map Settings 标签页。

字段：

```text
map_id
map_name
version
description
```

规则：

```text
map_id 可从 map_name 自动生成。
version MVP 默认为 1.0.0。
description 可为空，但建议填写。
```

---

## 11. 面板视觉规范

### 11.1 颜色

```text
背景：深灰半透明
文字：白色 / 浅灰
主按钮：低饱和蓝色
危险操作：红色
成功状态：绿色
警告状态：黄色
选中描边：青绿色
```

避免整套 UI 变成单一蓝紫色。Gameplay 标记可用队伍和类型颜色辅助识别：

```text
Red Base：红色标识
Blue Base：蓝色标识
Normal Treasure：白色/浅金
Rare Treasure：紫色小标识
Final Treasure：金色小标识
Supply Box：橙色小标识
Bounds：灰蓝色虚线框
```

### 11.2 半透明与可读性

```text
面板背景必须有模糊或暗色遮罩，避免文字被场景背景吃掉。
按钮 hover / selected 状态必须明显。
折叠条也要显示当前是否有错误，例如红点提示。
```

### 11.3 图标

优先使用简单图标：

```text
房子/旗帜：基地
钻石：宝物点
箱子：物资箱
方框：Bounds
箭头十字：移动
旋转箭头：旋转
缩放角标：缩放
垃圾桶：删除
图钉：固定面板
折叠箭头：折叠/展开
```

---

## 12. MVP UI 三阶段实现建议

### UI-1：基础四周布局

```text
目标：
先做出四周停靠式 UI 骨架，让地图编辑器能在不遮挡中心视野的前提下完成基础操作。

实现内容：
1. Top Bar：显示地图名、当前模式、Validate、Export、校验/导出状态。
2. Left Panel：显示 Brush 列表和 Gameplay Marker 列表。
3. Right Panel：显示当前选中对象名称、类型和基础 Transform。
4. Bottom Bar：显示 Place / Move / Rotate / Scale / Delete / Clear Brush 和最近一条日志。
5. 面板使用半透明背景。
6. 面板布局固定在四周，不覆盖中心编辑区域。

验收标准：
1. MapEditor 场景进入 Play Mode 后能看到四周 UI。
2. 左侧能选择普通 prefab 和玩法标记。
3. 点击 Validate / Export 能更新顶部或底部状态。
4. 选中对象后右侧能显示对象基础信息。
5. UI 不影响中心区域射线放置。
```

### UI-2：折叠、展开与固定

```text
目标：
让四周面板在编辑过程中不挡视野，同时需要时可以快速展开。

实现内容：
1. 四个区域全部可折叠/展开。
2. 支持 Pin 固定。
3. 半透明背景和 hover 高亮。
4. 拖动物体时自动降低非固定面板透明度。
5. 折叠状态保留图标、标题、错误红点或状态提示。
6. 选中对象时 Right Panel 自动展开；取消选择后可自动折叠。

验收标准：
1. 每个面板都能独立折叠和展开。
2. Pin 后面板不会自动折叠。
3. 拖动对象时未固定面板透明度降低。
4. Validate 失败时，即使面板折叠也能看到错误提示状态。
5. MR 手柄射线或鼠标都能点击折叠/固定按钮。
```

### UI-3：参数编辑、校验反馈与 MR 优化

```text
目标：
补齐 MapExportMarker 参数编辑、校验错误反馈和 Pico MR 可用性，让 UI 达到 MVP 完整交付。

实现内容：
1. Right Panel 支持所有 MapExportMarker 参数编辑。
2. Treasure / Supply / Bounds 显示各自专属字段。
3. Validate 错误可点击定位到相关对象。
4. 按钮尺寸适配手柄射线点击。
5. 面板自动贴近 Camera.main。
6. UI 面板不抢占放置射线。
7. 面板折叠/展开动效轻量化。
8. 校验失败时 Bottom Bar 展开错误列表。
9. 导出成功时显示导出路径。

验收标准：
1. 普通 MapObject 可编辑 object_id、Transform、has_collider。
2. TeamBase 可编辑 team 和 radius。
3. TreasureSpawnPoint 可编辑 treasure_type 和 radius。
4. SupplyBox 可编辑 supply_type 和 refresh_interval。
5. Bounds 可编辑 center/size 对应的 position/localScale。
6. 缺少红蓝基地或宝物点时 Validate 显示明确错误，并阻止导出。
7. Pico MR 下按钮可被手柄射线稳定点击。
8. 面板不会被导出到地图 JSON，也不会生成额外协议字段。
```

---

## 13. 不做内容

```text
1. 不做复杂资产商店式浏览器。
2. 不做多人协同编辑 UI。
3. 不做战斗配置、HP 配置、房间配置 UI。
4. 不做数据库管理 UI。
5. 不在地图编辑器 UI 中展示实时比分、玩家状态或对局结果。
6. 不新增 JSON 协议字段。
```

---

## 14. 推荐默认界面

首次进入 MapEditor：

```text
Top Bar：展开
Left Panel：展开，默认停在 Gameplay Markers
Right Panel：折叠，选中对象后展开
Bottom Bar：展开为单行工具条
所有面板：未固定
背景透明度：75%
中心区域：无遮挡
默认模式：Place
默认 Brush：无，要求用户主动选择
```

当用户开始放置或移动对象：

```text
Left Panel 自动折叠。
Right Panel 如果未固定则折叠。
Bottom Bar 保持单行。
Top Bar 保持可见。
```

当用户选中对象：

```text
Right Panel 自动展开。
显示对象基础信息和对应 marker 参数。
```

当 Validate 失败：

```text
Top Bar 显示红色状态。
Bottom Bar 日志自动上拉显示错误列表。
如果错误关联具体对象，点击错误可选中该对象。
```
