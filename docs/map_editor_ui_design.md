# 地图编辑器 UI 设计方案

> 关联文档：`map_editor_design.md`、`json_protocol.md`、`map_export_workflow.md`  
> 设计目标：让地图制作者在 Unity Editor 和 Pico MR 环境中高效摆放地图物件、编辑玩法标记、校验并导出地图 JSON，同时尽量不遮挡 MR 场景视线。

---

## 1. 总体定位

地图编辑器 UI 采用 World-Locked 固定式工作台 UI。

它不是：

```text
屏幕四周 HUD
头部跟随 UI
完全竖直挡在眼前的大面板
```

而是：

```text
固定在世界空间中的一张虚拟工作台
整体位于用户前方斜下方
用户低头或自然垂眼即可看到
按手柄菜单键后可收起、展开或重新定位
```

核心体验：

```text
用户低头在虚拟工作台上选择笔刷和参数。
用户抬头看地图空间并用手柄射线放置对象。
```

UI 不参与地图 JSON 导出，不应挂载 `MapExportMarker`，不新增或修改 JSON 协议字段。

---

## 2. UI 空间位置

整套 UI 初始出现在用户前方斜下方。

建议位置：

```text
距离用户：0.7m～1.1m
高度：胸口到腰部之间
方向：整体朝向用户
角度：整体向上倾斜 20°～35°
状态：固定在世界空间
```

UI 不是垂直墙面，而是类似一张倾斜桌面：

```text
用户视线
   ↓
  ┌────────────────────────┐
  │      虚拟工作台 UI       │
  └────────────────────────┘
```

优点：

```text
1. 不挡住正前方地图视线。
2. 比四周 HUD 更像真实工作台。
3. 用户可以低头操作，也可以抬头看地图。
4. UI 不随头转，空间感更稳定。
5. Pico 手柄射线更容易点击大按钮和卡片。
```

---

## 3. UI 重定位机制

UI 不提供可点击的 `Recenter UI`、`Reset UI Position`、`召回 UI` 按钮。

重定位只通过手柄按键触发：

```text
短按 Menu：
  打开 / 收起工作台 UI

长按 Menu：
  重新定位整套 UI 到用户前方斜下方

如果 Pico 端无法读取 Menu：
  使用 Secondary Button 长按作为 fallback
```

重定位后：

```text
1. 整套 UI 移动到用户当前前方。
2. 位于用户斜下方。
3. 保持工作台角度。
4. 朝向用户。
5. 再次固定在世界空间。
```

UI 重定位是系统级操作，不作为工作台按钮出现。

---

## 4. 整体布局

整套 UI 是一张平面化工作台，分为三块区域：

```text
┌──────────────────────────────────────────────────────────────┐
│ 左侧信息操作区 │              中间主面板              │ 右侧参数区 │
│               │                                     │          │
│ 地图信息       │ 笔刷缩略图                            │ 选中对象   │
│ 模式状态       │ 玩法标记                              │ Transform │
│ 保存/读取      │ 编辑模式                              │ Marker参数 │
│ 校验/导出      │ 当前笔刷提示                           │ 操作按钮   │
└──────────────────────────────────────────────────────────────┘
```

三块区域属于同一块 UI 工作台，不再强调屏幕四周停靠，也不是分散漂浮面板。

---

## 5. 中间主面板：笔刷、玩法标记、编辑模式

中间主面板是最高频操作区。

主要职责：

```text
1. 选择地图物体笔刷。
2. 显示笔刷缩略图。
3. 选择玩法标记。
4. 切换编辑模式。
5. 显示左右手当前笔刷。
6. 显示当前操作手和操作提示。
```

### 5.1 笔刷区

笔刷以缩略图卡片形式展示：

```text
Wall
Floor
Box
Obstacle
Cover
Column
Platform
```

每个笔刷卡片包括：

```text
缩略图或简易占位图
名称
类型说明
当前操作手标记 L / R
```

示例：

```text
┌────────┐
│ 缩略图  │
│  Box   │
│   L    │
└────────┘
```

含义：

```text
Box 被左手选中。
左手当前持有 Box 笔刷。
```

### 5.2 玩法标记区

显示功能性地图标记：

```text
Red Base (Spawn/Respawn/Submit)
Blue Base (Spawn/Respawn/Submit)
Treasure Point
Supply Box
Bounds
```

注意：

```text
Spawn / Respawn / Submit 不拆成独立 JSON 字段。
Red Base 和 Blue Base 仍导出到 team_bases。
不得新增 Revive Area / Spawn Point 独立导出按钮。
```

### 5.3 编辑模式区

显示：

```text
Place
Move
Rotate
Scale
Delete
Clear Brush
```

当前模式高亮，并在状态区显示：

```text
Left Hand: Box
Right Hand: Wall
Active: Left
Hint: Aim at a surface and press Trigger
```

MVP 规则：

```text
左右手都可以各自持有 Brush。
同一时间只显示最近 Active Hand 的幽灵预览。
另一只手的 Brush 状态保留，但不同时显示第二个预览。
```

---

## 6. 左侧信息操作区

左侧面板不放 Recenter UI。

主要职责：

```text
1. 显示地图信息。
2. 显示当前模式状态。
3. 显示保存状态。
4. 执行 Save / Load。
5. 执行 Validate / Export。
6. 显示导出结果。
```

显示字段：

```text
Map Name
Map ID
Version
Object Count
Red Base Count
Blue Base Count
Treasure Point Count
Supply Box Count
Bounds Count
Validation Status
```

操作按钮：

```text
Save
Load
Validate
Export JSON
Preview
Clear Map
Map Settings
```

危险操作：

```text
Clear Map 必须二次确认后再真正清空。
MVP 如未完成二次确认，只显示 TODO 提示，不直接清空。
```

---

## 7. 右侧参数区

右侧参数区用于精确编辑当前选中对象。

未选中对象：

```text
Selected Object: none
Select an object to edit properties
```

选中普通物体：

```text
object_id
prefab_id
position x / y / z
rotation x / y / z
scale x / y / z
has_collider
```

选中玩法标记：

```text
TeamBase：
  base_id
  team
  radius
  position

Treasure Point：
  point_id
  treasure_type
  radius
  position

Supply Box：
  box_id
  supply_type
  refresh_interval
  position

Map Boundary：
  boundary_type = Polygon
  height
  point count
  Draw Bounds / Draw Area 状态
```

MR 参数调整方式：

```text
Stepper：[-] 0.5 [+]
Slider：调整 radius / scale
Dropdown：选择 team / treasure_type
Toggle：has_collider
Button：Reset / Delete / Duplicate
```

---

## 8. Pico 手柄交互

MR 地图编辑器不区分固定的工具手和操作手。左右手柄都可以独立指向 UI、选择笔刷、放置物体、选中对象和移动对象。

核心规则：

```text
哪只手点击笔刷，哪只手持有笔刷。
哪只手持有笔刷，哪只手可以成为 Active Hand。
Active Hand 射线命中可放置表面时显示幽灵预览。
哪只手按 Trigger，哪只手完成放置或选择。
```

Pico 端 UI 点击要求：

```text
1. 工作台 Canvas 使用 World Space。
2. 保留 GraphicRaycaster 和 TrackedDeviceGraphicRaycaster。
3. 所有可点击控件应有 BoxCollider。
4. 所有可点击控件应挂 MapEditorRuntimeUiHitTarget。
5. Trigger 命中 UI 时优先触发 UI，不允许同时放置地图对象。
```

按键建议：

```text
Trigger：
  点击 UI / 放置对象 / 选择对象

Grip 按住：
  Surface Move 移动当前选中对象

Grip 松开：
  确认当前位置

Primary Button：
  删除选中对象

Secondary Button：
  切换编辑模式；如 Menu 不可用，则长按作为 UI 重定位 fallback

Menu 短按：
  打开 / 收起工作台

Menu 长按：
  重新定位工作台
```

---

## 9. 放置逻辑

表面放置规则：

```text
1. 优先使用射线命中的 Collider 表面。
2. 没有命中 Collider 时允许地面平面 fallback。
3. 地面 / Grid 结果应用 Grid Snap。
4. 普通物体表面默认使用 hit point 贴合放置。
```

可放置表面：

```text
地面
网格
平台
箱子顶面
桌面
已有地图物体表面
```

不可放置表面：

```text
UI 面板
玩家身体
手柄模型
地图边界外
非法碰撞体
不允许叠放的对象表面
```

幽灵预览状态：

```text
可放置：蓝色 / 青色半透明
不可放置：红色半透明
吸附表面：显示表面高亮
选中对象：黄色描边
```

MVP 可先实现可放置幽灵预览；非法红色预览可作为后续 TODO。

---

## 10. 单手移动与缩放旋转

单手移动：

```text
Trigger 选中对象。
Grip 按住对象。
对象跟随射线命中表面或空间位置移动。
松开 Grip 确认。
```

移动模式：

```text
Surface Move：沿可放置表面移动。
Grid Move：按网格吸附移动。
Free Move：跟随手柄空间移动。
```

MVP 默认：

```text
Surface Move + Grid Snap。
```

缩放和旋转：

```text
MVP 先使用摇杆左右旋转。
MVP 先使用摇杆上下缩放。
也可以通过右侧参数区精确调整 Rotation / Scale。
双手缩放和双手旋转保留为后续阶段。
```

---

## 11. MVP 实现顺序

### UI-1：固定斜下方工作台

```text
实现三栏平面 UI。
整体向上倾斜 20°～35°。
固定在世界空间。
按菜单键可收起、展开、召回到眼前斜下方。
```

验收：

```text
UI 不跟随头部转动。
UI 位于斜下方，不挡正前方视线。
按菜单键后 UI 回到用户前方斜下方。
左右手柄都可以点击 UI。
```

### UI-2：主面板笔刷选择

```text
笔刷显示缩略图卡片。
玩法标记显示类型说明。
编辑模式可切换。
点击后显示当前操作手 L / R。
```

验收：

```text
左手点 Box，Box 显示 L 标记。
右手点 Wall，Wall 显示 R 标记。
左右手 Brush 状态正确显示。
```

### UI-3：幽灵预览与表面放置

```text
Active Hand 射线显示幽灵预览。
支持地面放置。
支持箱子顶面放置。
支持非法位置提示。
```

验收：

```text
可以在地面放物体。
可以箱子叠箱子。
非法位置不允许放置。
```

### UI-4：对象参数区

```text
选中对象后右侧参数区更新。
支持基础 Transform 修改。
支持玩法标记参数修改。
```

验收：

```text
选中 Box 后显示 Box 参数。
选中 Red Base 后显示 team / radius。
修改参数后对象同步变化。
```

### UI-5：保存、校验、导出

```text
左侧信息操作区显示地图状态。
Validate 检查地图合法性。
Export JSON 导出地图。
显示导出结果。
```

验收：

```text
缺少红蓝基地时校验失败。
校验通过后可以导出 JSON。
导出成功后显示路径。
```

---

## 12. 不做内容

```text
1. 不做多人协同编辑 UI。
2. 不做战斗配置、HP 配置、房间配置 UI。
3. 不做数据库管理 UI。
4. 不在地图编辑器 UI 中展示实时比分、玩家状态或对局结果。
5. 不新增 JSON 协议字段。
6. 不新增正式单人寻宝模式。
```
