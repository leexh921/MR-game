# MapEditor 当前运行逻辑与手柄手感优化说明

> 日期：2026-06-12  
> 用途：给后续优化 Pico 手柄操作手感使用。  
> 重点：说明当前地图编辑器怎么处理手柄射线、UI、放置、选择、拖动、旋转、缩放、导出，以及哪些点最适合改。

## 1. 当前地图编辑器入口

当前运行时地图编辑器主要入口是：

```text
TreasureArenaUnity/Assets/_Project/Scenes/MapEditor.unity
```

核心运行时代码：

```text
TreasureArenaUnity/Assets/_Project/Scripts/MapEditor/MapEditorRuntimeController.cs
TreasureArenaUnity/Assets/_Project/Scripts/MapEditor/MapEditorDockedUiController.cs
TreasureArenaUnity/Assets/_Project/Scripts/MapEditor/MapEditorRuntimeUiHitTarget.cs
TreasureArenaUnity/Assets/_Project/Scripts/MapEditor/MapEditorObjectInspector.cs
TreasureArenaUnity/Assets/_Project/Scripts/MapEditor/MapEditorRuntimeExporter.cs
```

相关地图导出/校验代码：

```text
TreasureArenaUnity/Assets/_Project/Scripts/Map/MapExportMarker.cs
TreasureArenaUnity/Assets/_Project/Scripts/Map/MapSceneJsonBuilder.cs
TreasureArenaUnity/Assets/_Project/Scripts/Map/MapValidator.cs
```

Editor 菜单装配入口：

```text
Tools > TreasureArena > 地图编辑器
Tools > TreasureArena > 地图编辑器 > 创建 MR 运行时输入控制器
Tools > TreasureArena > 地图编辑器 > 创建 Docked UI Prefab
```

`创建 MR 运行时输入控制器` 会装配运行时控制器、放置根节点、fallback 鼠标射线、brush 列表和地图编辑器 UI。

## 2. 当前核心对象关系

### 2.1 MapEditorRuntimeController

这是当前手柄/鼠标编辑逻辑的核心。它负责：

```text
1. 读取 Pico 左/右手柄射线。
2. 没有 XR 输入时，用鼠标作为 Editor fallback。
3. 处理 UI 点击拦截。
4. 处理 Place / Move / Rotate / Scale 模式。
5. 显示射线 LineRenderer。
6. 显示绿色半透明 Ghost 预览。
7. 放置 prefab。
8. 选中对象。
9. Grip 拖动物体。
10. 摇杆旋转/缩放。
11. Primary Button 删除选中对象。
12. Menu 短按收起/展开工作台，长按重定位工作台。
```

### 2.2 MapEditorRuntimeBrush

每个可放置项都是一个 brush。关键字段：

```text
label：UI 显示名称。
folder：UI 中显示在哪个文件夹。
prefab：实际实例化的 prefab。
thumbnail：缩略图。
prefab_id：导出 JSON 时写入 objects.prefab_id。
marker_type：MapObject / TeamBase / TreasureSpawnPoint / SupplyBox / Bounds。
team：TeamBase 使用，Red / Blue。
treasure_type：TreasureSpawnPoint 使用，Normal / Rare / Final。
supply_type：SupplyBox 使用。
radius：TeamBase / TreasureSpawnPoint 使用。
refresh_interval：SupplyBox 使用。
has_collider：MapObject 使用。
```

### 2.3 HandState

当前每只手都有独立状态：

```text
Hand：Left / Right。
Node：XRNode.LeftHand / XRNode.RightHand。
BrushIndex：这只手当前持有的 brush。
EditMode：这只手当前模式。
PreviousButtons：上一帧按钮状态，用来判断按下瞬间。
MenuDownTime / MenuLongPressFired：判断菜单键短按/长按。
DraggingObject：当前拖动对象。
DragSurfaceCollider：拖动时锁定的表面 collider。
DragPlane：拖动平面。
DragOffset：抓取点到对象中心的偏移。
DragUsesSurface：是否沿原始表面拖动。
DragSnapToGrid：拖动时是否网格吸附。
```

重要点：

```text
左右手可以各自持有 brush。
同一时间只有 activeHand 显示射线、Ghost 预览和模式状态。
哪只手点击 UI 或触发操作，哪只手会变成 activeHand。
```

### 2.4 MapExportMarker

所有导出的对象都靠这个组件表达语义，不靠名字猜。

导出类型：

```text
MapObject：普通地图物体，导出到 objects。
TeamBase：红/蓝基地，导出到 team_bases。
TreasureSpawnPoint：宝物点，导出到 treasure_spawn_points。
SupplyBox：物资箱，导出到 supply_boxes。
Map Boundary：地图级多边形边界，导出到 map_boundary。
```

## 3. 每帧输入主流程

当前 `MapEditorRuntimeController.Update()` 的主流程是：

```text
1. 初始化本帧状态：
   anyXrValid = false
   activePreviewUpdated = false
   draggingNow = false

2. 处理左手：
   ProcessHand(leftHand)

3. 处理右手：
   ProcessHand(rightHand)

4. 如果本帧没有任何有效 XR 手柄：
   ProcessEditorFallback()

5. 如果 active 手没有更新预览：
   销毁 previewGhost
   销毁 moveGhost
   关闭 rayLine

6. 更新 isDragging，并通知 UI 面板进入/退出拖动状态。
```

这个结构的含义：

```text
Pico 上优先用左右手柄。
Editor 里没有 XR 输入时才用鼠标 fallback。
拖动状态会影响 Docked UI 面板透明度。
```

## 4. Pico 手柄输入映射

当前代码通过 `UnityEngine.XR.InputDevices.GetDeviceAtXRNode()` 读取按钮。

### 4.1 射线来源

每只手的射线来自：

```text
CommonUsages.devicePosition
CommonUsages.deviceRotation
```

生成方式：

```text
ray = new Ray(devicePosition, deviceRotation * Vector3.forward)
```

因此当前射线完全取决于手柄设备姿态，没有额外做：

```text
射线平滑
射线偏移校准
射线长度动态调节
射线吸附
近距离抓取
```

### 4.2 按键映射

当前按键含义：

```text
Trigger：
  - 点 UI
  - Place 模式下放置对象
  - 非 Place 或无 brush 时选择对象

Grip：
  - 按住拖动当前选中对象
  - 松开确认位置

Primary Button：
  - 删除选中对象

Secondary Button：
  - 当 menuButton 可用时，用于切换编辑模式
  - 当 menuButton 不可用时，会被当成 menu fallback

Menu 短按：
  - 打开/收起工作台 UI

Menu 长按：
  - 重定位工作台 UI 到用户前方斜下方

Primary 2D Axis / 摇杆：
  - 左右：按步进旋转选中对象
  - 上下：按步进缩放选中对象
```

当前按键风险：

```text
Trigger 同时承担 UI 点击、放置、选择，容易误触。
Grip 只要有 selectedObject 就能拖，不要求当前模式必须是 Move。
Primary Button 直接删除，没有二次确认，Pico 上可能误删。
Secondary/Menu fallback 逻辑容易和模式切换冲突。
```

## 5. 当前 UI 点击逻辑

### 5.1 设计意图

每帧手柄射线会先检查 UI，再检查地图放置。

目标流程是：

```text
手柄射线
→ 先判断是否点到 MapEditor UI
→ 如果点到 UI，只触发 UI，不放置/选择地图对象
→ 如果没点到 UI，再执行场景 Physics.Raycast 放置/选择/拖动
```

### 5.2 当前实现方式

当前 Pico 端 UI 已可以点击，并且 XR UI Input Module 已经引入。也就是说，`Button / Dropdown / InputField` 的主链路不再是“还没接通”的状态。

当前仍保留的兼容/兜底逻辑包括：

```text
MapEditorRuntimeUiHitTarget
BoxCollider
Physics.Raycast / RaycastAll
Button.onClick.Invoke()
Toggle / Dropdown / InputField 的简单手动触发
```

`MapEditorRuntimeUiHitTarget.Activate(hand)` 的行为：

```text
1. 如果注册了 HandActivated 事件：
   调用 HandActivated(hand)

2. 否则如果找到 Button 且可点击：
   调用 button.onClick.Invoke()

3. 否则如果是 Toggle：
   切换 isOn

4. 否则如果是 Dropdown：
   调用 Show()

5. 否则如果是 InputField：
   ActivateInputField()
```

当前需要注意的问题：

```text
XR UI Input Module 已接入，Pico 可以点击 UI。
但项目中仍存在 MapEditorRuntimeUiHitTarget / BoxCollider 这条兼容桥接链路。
后续优化时要确认 UI 点击优先级始终高于地图放置。
如果 UI 命中状态判断和地图射线没有彻底隔离，仍可能出现点 UI 时误放置、误选或 Ghost 预览干扰。
```

所以后续重点不是“让 UI 能点”，而是“点 UI 时地图编辑射线完全暂停”，并把文档、Prefab 和场景配置统一到 XR UI Input Module 作为主方案。

## 6. 当前放置逻辑

### 6.1 放置触发

在 `ProcessHand()` 中：

```text
如果 Trigger 本帧刚按下：
  设置当前手为 activeHand

  如果当前手是 Place 模式，并且有 brush，并且射线有可放置目标：
    PlaceObject()
  否则：
    SelectFromHit()
```

### 6.2 目标点计算

当前通过 `TryGetPlacementPose()` 计算放置点：

```text
1. Physics.RaycastAll(ray, maxRayDistance, placementMask, QueryTriggerInteraction.Ignore)
2. 按距离从近到远排序。
3. 跳过非法 collider。
4. 找到第一个合法 hit。
5. 判断是否需要网格吸附。
6. 根据 prefab bounds 和 hit normal 计算表面 offset。
7. 得到最终 position。
```

如果没有命中任何合法 collider：

```text
使用 Plane(Vector3.up, Vector3.zero) 作为地面 fallback。
```

这意味着当前默认地面永远是：

```text
Unity 世界坐标 y = 0
```

不是 Pico 识别到的真实地面。

### 6.3 网格吸附规则

当前 `gridSize = 0.5f`。

只有这些情况会吸附：

```text
命中的 collider 名字包含 ground
命中的 collider 名字包含 grid
命中的 tag 包含 ground
命中的 tag 包含 grid
fallback 到 y=0 地面时
```

其他表面，比如箱子顶面、墙面、普通 prefab 表面：

```text
默认不网格吸附，直接用 hit.point。
```

### 6.4 表面 offset

放置时会根据 prefab 的 Renderer/Collider bounds 计算 offset：

```text
offset = dot(bounds.extents, abs(surfaceNormal))
position = hitPoint + normal * offset
```

这能让物体“贴在表面外侧”，例如放在地面上时不会一半陷进地面。

但这里有一个潜在手感问题：

```text
bounds 是从 prefab 当前资源的 world bounds 读出来的。
如果 prefab pivot 不规范、层级复杂、缩放特殊，offset 可能不符合用户直觉。
```

### 6.5 放置后的状态

放置对象时会：

```text
1. Instantiate 当前 brush.prefab。
2. 设置 parent 到 placedObjectsRoot。
3. 生成唯一名字。
4. 设置 position / rotation / scale。
5. 确保有 MapExportMarker。
6. 写入 marker_type、prefab_id、team、treasure_type、supply_type、radius、refresh_interval、has_collider。
7. 选中新对象。
8. 消耗当前手的 brush。
```

注意：

```text
当前 brush 放一次就会被清空。
这能防误放，但如果用户想连续摆多个同类物体，体验会慢。
```

## 7. 当前选择逻辑

如果 Trigger 按下时不是“Place + 有 brush + 有目标”，就会走选择。

选择逻辑：

```text
1. 从当前 raycast hit 的 collider 往父级找 MapExportMarker。
2. 找到 marker 就选中 marker.gameObject。
3. 找不到就取消选中。
4. SelectionChanged 事件通知右侧 Inspector 刷新。
```

当前问题：

```text
选择必须打到 collider。
如果对象视觉可见但 collider 很小/没有 collider，就不好选。
没有选中高亮描边，只有右侧面板变化和 Ghost/MoveGhost 反馈。
没有“锁定选择”或“穿透选择列表”，复杂场景里容易选错。
```

## 8. 当前拖动逻辑

### 8.1 拖动触发

当前拖动条件：

```text
只要 selectedObject != null 且 Grip 按住，就开始/持续拖动。
```

注意：

```text
Pico 手柄拖动不强制要求当前 editMode == Move。
也就是说即使 UI 显示 Place/Rotate/Scale，只要有选中对象，按 Grip 仍会移动。
```

这对开发调试方便，但对成品手感可能不清晰。

### 8.2 BeginDrag 做了什么

开始拖动时：

```text
1. 记录 state.DraggingObject = selectedObject。
2. 默认使用一个水平平面：
   planeNormal = Vector3.up
   planePoint = selectedObject.transform.position

3. 如果当前射线 placement.valid，且选中对象不是 Bounds：
   - 如果命中表面法线 y > 0.35，用该表面法线作为拖动平面法线。
   - 否则仍用 Vector3.up。
   - planePoint = placement.hitPoint。
   - 如果命中了 collider 且不是 ground fallback：
     DragUsesSurface = true。
     DragSurfaceCollider = placement.collider。
     DragSnapToGrid = placement.snapToGrid。

4. 计算 DragOffset：
   selectedObject.position - 当前射线在拖动平面上的 anchor。
```

这说明当前拖动不是“拿住物体本身”，而是：

```text
射线和平面求交
→ 得到 anchor
→ 对象位置 = anchor + 初始 DragOffset
```

### 8.3 UpdateDrag 做了什么

持续拖动时：

```text
1. 如果 DragUsesSurface 为 true：
   再次 TryGetPlacementPose()
   只有当当前命中的 collider 还是 DragSurfaceCollider 时，沿这个表面拖。

2. 否则：
   用手柄射线和 DragPlane 求交。

3. 如果 DragSnapToGrid 为 true：
   对 anchor 做 SnapToGrid。

4. selectedObject.position = anchor + DragOffset。
```

当前拖动手感不好的主要原因：

```text
1. 拖动平面在 BeginDrag 那一刻固定，用户手柄角度变化后，深度感会不自然。
2. 如果开始拖动时命中了某个表面，后续只有还打到同一个 collider 才沿表面拖；稍微偏出表面就回退到平面拖动。
3. 对象位置每帧直接跳到计算结果，没有平滑/阻尼。
4. 网格吸附是瞬时吸附，可能出现跳格感。
5. Grip 按下即可拖动，没有“确认抓到对象”的反馈。
6. selectedObject 是全局的，但每只手都有 DraggingObject；双手同时操作时没有明确互斥。
7. Bounds 特殊处理为水平平面，不适合之后做“画空间边界”。
```

## 9. 当前旋转与缩放逻辑

### 9.1 按钮/键盘旋转缩放

Public 方法：

```text
RotateSelected(float degrees)
ScaleSelected(float delta)
```

当前行为：

```text
旋转：绕世界 Y 轴旋转。
缩放：x/y/z 三轴一起增加或减少。
最小 scale = 0.1。
```

### 9.2 Pico 摇杆旋转缩放

当前读取：

```text
CommonUsages.primary2DAxis
```

逻辑：

```text
axis.x 绝对值 > 0.6：
  每 repeatDelay 秒旋转 rotateStepDegrees，默认 15 度。

axis.y 绝对值 > 0.6：
  每 repeatDelay 秒缩放 scaleStep，默认 0.1。
```

当前问题：

```text
旋转和缩放都是离散步进，不是连续手感。
没有长按加速/精细模式。
缩放是三轴统一缩放，不适合 Bounds 的 x/z 平面拉框。
没有用摇杆方向和当前对象类型区分操作。
```

## 10. 当前 Ghost 预览逻辑

当前有两种临时可视对象：

```text
previewGhost：Place 模式下显示当前 brush 将要放置的位置。
moveGhost：拖动时显示移动预览。
```

视觉材质：

```text
Standard shader
绿色半透明
alpha = 0.35
ZWrite = 0
renderQueue = 3000
```

当前预览问题：

```text
只有“可放置”的绿色预览。
没有非法位置红色预览。
没有吸附点/边界/地面高度提示。
没有对象选中描边。
没有拖动目标位置与实际对象之间的缓动区别。
```

## 11. 当前导出逻辑

导出入口：

```text
MapEditorRuntimeExporter.ExportCurrentMap()
```

流程：

```text
1. FindObjectsOfType<MapExportMarker>(true)
2. MapSceneJsonBuilder.BuildFromMarkers(mapName, mapDescription, markers)
3. MapValidator.Validate(map)
4. 校验失败：返回失败，不写 JSON
5. 校验成功：
   - Editor：写到 Assets/_Project/StreamingAssets/Maps/<map_id>.json
   - Android/Pico：写到 Application.persistentDataPath/TreasureArenaMR/MapExports/<map_id>.json
   - Android/Pico：尝试复制到 /storage/emulated/0/Download/TreasureArenaMR/MapExports
```

`MapSceneJsonBuilder` 当前映射：

```text
TeamBase：
  team_bases[].base_id
  team_bases[].team
  team_bases[].position
  team_bases[].radius

TreasureSpawnPoint：
  treasure_spawn_points[].point_id
  treasure_spawn_points[].treasure_type
  treasure_spawn_points[].position
  treasure_spawn_points[].radius

SupplyBox：
  supply_boxes[].box_id
  supply_boxes[].position
  supply_boxes[].supply_type
  supply_boxes[].refresh_interval

Map Boundary：
  map_boundary.boundary_type = "Polygon"
  map_boundary.height = boundary height
  map_boundary.points = Draw Bounds / Draw Area 绘制出的地面点

MapObject：
  objects[].object_id
  objects[].prefab_id
  objects[].position
  objects[].rotation
  objects[].scale
  objects[].has_collider
```

对手感优化的影响：

```text
当前 JSON 只支持盒状 Bounds。
如果要做“手绘空间边界/多边形”，需要先决定是否改协议。
如果不改协议，可以把手绘结果近似成一个 Bounds box。
```

## 12. 当前逻辑中最影响手柄手感的问题

### 12.1 UI 已可点击，但仍要确认与地图放置输入彻底分离

当前 Pico UI 点击已经可用，XR UI Input Module 已引入。剩余风险是：同一根手柄射线既能点 UI，也能执行地图放置/选择/拖动，如果优先级判断不严，Trigger 仍可能在 UI 操作后继续影响地图对象。

建议：

```text
保持 XR UI Input Module 作为 UI 主链路。
地图放置射线和 UI 射线明确分层。
新增或确认统一的 IsPointerOverRuntimeUi(hand) 判断。
```

目标：

```text
如果手柄正在指 UI：
  Trigger 只点击 UI。
  不显示地图 Ghost。
  不执行 Place / Select / Drag。
```

### 12.2 Grip 拖动太宽松

当前只要有选中对象，任何模式按 Grip 都能拖。

建议：

```text
成品模式下只允许 Move 模式拖动。
或 Grip 第一次按下时必须射线命中 selectedObject，才算抓住。
```

这样用户会更可控，不会“明明在选素材/放置，手一握物体就跑了”。

### 12.3 拖动缺少“抓住感”

当前是射线和平面求交，没有近距离抓取/弹性/阻尼。

建议增加：

```text
dragSmoothing：对象向目标点 SmoothDamp。
dragDeadZone：手柄轻微抖动不移动。
dragLockMode：
  GroundPlane
  Surface
  FreeSpace
dragAnchorMode：
  ObjectCenter
  HitPointOffset
```

MVP 推荐：

```text
默认 GroundPlane + HitPointOffset + SmoothDamp + Grid 可开关。
```

### 12.4 地面不是 Pico 真实地面

当前 fallback 地面是 Unity y=0。

你希望“地面与真实地面同高”，需要新增一个编辑器地面基准：

```text
editorFloorY
```

获取方式建议二选一：

```text
方案 A：进入编辑器后手柄点真实地面，设置 editorFloorY。
方案 B：使用 Pico MR 平面检测，将检测到的地面作为 Ground Plane。
```

然后把当前：

```text
Plane(Vector3.up, Vector3.zero)
```

改成：

```text
Plane(Vector3.up, new Vector3(0, editorFloorY, 0))
```

### 12.5 Bounds 不适合“画空间”

当前 Bounds 是一个普通 marker prefab，导出时取：

```text
center = transform.position
size = transform.localScale
```

旧盒状 Bounds 方案已废弃，边界应由用户用手柄逐点绘制房间范围。

当前推荐做成：

```text
Draw Bounds / Draw Area：
  Trigger 逐点添加边界点
  所有点贴 editorFloorY / floorPlane
  至少 3 个点后闭合
  导出 map_boundary.boundary_type = "Polygon"
  导出 map_boundary.height = 默认 2.5
  导出 map_boundary.points = 绘制点列表
```

## 13. 推荐的手柄手感优化路线

### 阶段 1：先做 UI 与地图编辑输入隔离回归

目标：

```text
点 UI 时绝不放置对象。
点 UI 时不显示地图 Ghost。
点 UI 时不改变 selectedObject。
```

建议改动：

```text
1. 确认 MapEditorDockedCanvas 使用 XR UI Input Module / XR Ray Interactor 作为主输入链路。
2. 在 MapEditorRuntimeController 中增加或统一 IsPointerOverRuntimeUi(hand)。
3. ProcessHand 开头先判断 UI 状态：
   如果指向 UI：
     - 关闭 Ghost
     - 不执行 TryGetPlacementPose / Place / Select / Drag
     - 只让 UI 系统处理点击
```

### 阶段 2：重做拖动模型

推荐成品交互：

```text
Trigger：
  选择对象 / 放置对象

Grip 按住：
  只在 Move 模式下抓取移动选中对象

Grip 松开：
  放下对象

摇杆左右：
  Rotate 模式下旋转

摇杆上下：
  Scale 模式下缩放
```

建议新增参数：

```text
bool requireMoveModeForGripDrag = true;
bool requireGripStartOnSelectedObject = true;
float dragSmoothTime = 0.06f;
float dragDeadZone = 0.015f;
float floorY = 0f;
bool useGridSnapWhileDragging = true;
```

拖动伪流程：

```text
GripDown：
  如果不是 Move 模式，return
  如果射线没有命中 selectedObject，return
  记录 hitPointOffset = object.position - hit.point
  记录 dragPlane = floor plane 或当前 surface plane

GripHold：
  ray 与 dragPlane 求交得到 target
  target += hitPointOffset
  如果开 GridSnap，target = SnapToGrid(target)
  object.position = SmoothDamp(object.position, target)

GripUp：
  object.position = 最终 target
  清理 drag 状态
```

### 阶段 3：做真实地面校准

推荐 MVP：

```text
新增 Calibrate Floor 模式。
用户用手柄射线点真实地面。
记录 floorY 或 floorPlane。
后续所有 fallback ground / bounds / map object 默认落在该地面。
```

如果 Pico 平面检测稳定，再升级为：

```text
自动选择最大水平地面平面。
允许用户手动重新校准。
```

### 阶段 4：做空间/边界绘制

推荐版本：

```text
Step 1：进入 Draw Bounds / Draw Area 模式。
Step 2：Trigger 逐点添加 Polygon 顶点。
Step 3：移动手柄显示当前边线预览。
Step 4：至少 3 个点后靠近首点并 Trigger 闭合。
Step 5：导出 map_boundary：
        boundary_type = "Polygon"
        height = 默认高度，例如 2.5m
        points = 按绘制顺序保存，最后一点不重复首点
Step 6：后续放置对象时检查目标点是否在 map_boundary 内。
```

这符合当前 JSON 的 `map_boundary` 字段。

## 14. 建议优先修改的文件

### 14.1 手柄拖动手感

优先改：

```text
TreasureArenaUnity/Assets/_Project/Scripts/MapEditor/MapEditorRuntimeController.cs
```

重点方法：

```text
ProcessHand()
BeginDrag()
UpdateDrag()
EndDrag()
TryGetPlacementPose()
SnapToGrid()
HandleHeldAxis()
```

### 14.2 UI 点击稳定性

优先改：

```text
MapEditorRuntimeController.cs
MapEditorRuntimeUiHitTarget.cs
MapEditorDockedUiController.cs
MapEditorDockedCanvas.prefab
```

如果接正式 XR UI，重点不是继续堆 BoxCollider，而是让 Pico 手柄射线走 uGUI EventSystem。

### 14.3 真实地面与边界

优先改：

```text
MapEditorRuntimeController.cs
MapEditorObjectInspector.cs
MapEditorRuntimeBrush.cs
MapSceneJsonBuilder.cs
MapValidator.cs
```

多边形边界已经要求改：

```text
docs/json_protocol.md
MapJsonModels.cs
MapSceneJsonBuilder.cs
MapValidator.cs
MapLoader.cs
Server 边界判断逻辑
```

## 15. 推荐的第一版优化目标

为了尽快改善你说的“手柄拖动手感不好”，建议第一版不要大改协议，先做这个范围：

```text
1. UI 点击和地图放置彻底分离，并以当前已接入的 XR UI Input Module 为准。
2. Grip 只有 Move 模式才能拖动。
3. Grip 开始时必须射线命中选中对象，才允许拖。
4. 拖动目标位置加 SmoothDamp。
5. 增加拖动死区，降低手柄抖动。
6. 增加 floorY 校准值，把地面 fallback 从 y=0 改为真实地面高度。
7. Draw Bounds / Draw Area 导出 map_boundary Polygon。
8. 拖动/放置时如果目标点在 map_boundary 外，显示红色预览并禁止放置。
```

这版能解决最直接的体验问题，同时不修改冻结 JSON 协议。

## 16. 人工验证方式

打开场景：

```text
TreasureArenaUnity/Assets/_Project/Scenes/MapEditor.unity
```

AppRole：

```text
不需要 Server / Manager / PicoClient / LocalTest。
地图编辑器是独立模块。
```

Inspector 需要确认：

```text
1. 场景中有 MapEditorRuntimeController。
2. placedObjectsRoot 指向 RuntimePlacedObjects。
3. brushes 列表里有 MapObjects / GameplayMarkers / Palette prefab。
4. 场景中有 MapEditorDockedCanvas。
5. UI 上有 EventSystem / GraphicRaycaster / XRUIInputModule。
6. Pico 手柄可点击 UI，并且点 UI 时不会触发地图放置/选择/拖动。
```

测试步骤：

```text
1. 进入 Play Mode。
2. 用手柄或鼠标选择一个普通物体 brush。
3. 指向地面，看到 Ghost 预览。
4. Trigger 放置，物体生成并被选中。
5. 切到 Move 模式。
6. Grip 按住并移动，物体应平滑跟随，不应跳动。
7. 松开 Grip，物体停在当前位置。
8. 指向 UI 按钮并按 Trigger，应只触发 UI，不应在场景中放置物体。
9. 点击 Validate，缺少红/蓝基地或宝物点时应显示错误。
10. 补齐 Red Base、Blue Base、Treasure Point 后 Export，应生成 JSON。
```

Console 不应出现：

```text
NullReferenceException
MissingReferenceException
error CS
```
