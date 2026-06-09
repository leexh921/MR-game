# MapEditor 运行时说明与调试手册

本文档给 Pico / Unity 联调同学使用，说明当前地图编辑器由哪些代码控制、运行逻辑是什么、素材放在哪里、想修改某个功能应该改哪个文件。

当前地图编辑器只负责地图摆放、玩法标记参数编辑、JSON 导出和校验。它不负责战斗判定、HP、得分、拾取、提交、房间同步、数据库写入或服务器权威逻辑。

## 当前联调重点

当前不要再反复排查旧问题。已知历史问题和当前状态：

- 四周 Docked UI 已接入 `MapEditor.unity`。
- 左侧 Palette 已支持按 `Prefabs/MapEditor/Palette/<文件夹>/` 显示文件夹。
- Editor 鼠标点击 UI 仍可能穿透到地面，这是当前实现使用 Physics collider 拦截 uGUI 的局限。
- 当前 Pico 侧主要问题是：**Pico 手柄射线点不到 UI**。

当前建议把问题拆成两条输入链路：

```text
地图放置射线：物理射线，打地面/物体，用于 Place/Select/Move。
UI 点击射线：uGUI / XR UI 射线，打 Canvas Graphic，用于 Button/InputField/Dropdown。
```

不要继续依赖“给 UI 加 BoxCollider 再用 Physics.Raycast 点按钮”作为最终方案。它容易出现：

- 鼠标看起来点到 UI，但 Physics 射线没有命中 collider。
- Pico 手柄射线能打到空间对象，但不能正确触发 uGUI Button。
- UI collider 和 World Space Canvas 缩放/位置不一致。
- 点击 UI 后地图放置射线继续执行，导致误放置。

推荐后续正式修法：

```text
PC Editor 鼠标：先用 EventSystem / GraphicRaycaster 判断是否点到 UI。
Pico 手柄：接入 XR UI Input Module 或 Pico/XR Ray Interactor 触发 uGUI。
地图放置：只有在“不指向 UI”时才运行 Physics.Raycast 放置逻辑。
```

当前要改这个问题，优先看：

```text
Assets/_Project/Scripts/MapEditor/MapEditorRuntimeController.cs
Assets/_Project/Scripts/MapEditor/MapEditorRuntimeUiHitTarget.cs
Assets/_Project/Scripts/MapEditor/Editor/MapEditorDockedUiPrefabBuilder.cs
Assets/_Project/Prefabs/MapEditor/UI/MapEditorDockedCanvas.prefab
```

更推荐新增/调整的位置：

```text
MapEditorRuntimeController.TryHandleRuntimeUi()
MapEditorRuntimeController.Update()
MapEditorDockedCanvas.prefab 上的 EventSystem / GraphicRaycaster / XR UI 配置
```

## 使用入口

Unity 场景：

```text
TreasureArenaUnity/Assets/_Project/Scenes/MapEditor.unity
```

Editor 菜单：

```text
Tools > TreasureArena > 地图编辑器
Tools > TreasureArena > 地图编辑器 > 创建 MR 运行时输入控制器
Tools > TreasureArena > 地图编辑器 > 创建 Docked UI Prefab
```

其中：

- `地图编辑器`：打开传统 EditorWindow 版本，主要用于 SceneView 编辑。
- `创建 MR 运行时输入控制器`：刷新/装配 MapEditor 运行时场景，重新扫描素材、创建运行时控制器和四周 UI。
- `创建 Docked UI Prefab`：重新生成四周停靠式 UI prefab，通常只有改 UI prefab builder 后才需要点。

## 目录结构

核心脚本：

```text
Assets/_Project/Scripts/MapEditor/
```

地图导出/加载/校验支持类：

```text
Assets/_Project/Scripts/Map/
```

MapEditor prefab：

```text
Assets/_Project/Prefabs/MapEditor/
```

地图 JSON 输出：

```text
Assets/_Project/StreamingAssets/Maps/
```

运行时 UI prefab：

```text
Assets/_Project/Prefabs/MapEditor/UI/MapEditorDockedCanvas.prefab
```

## 主要脚本职责

### Editor 侧

`MapEditorWindow.cs`

传统 Unity EditorWindow 地图编辑器。通过 `Tools > TreasureArena > 地图编辑器` 打开。它在 SceneView 里提供地图信息、Prefab 面板、放置、验证、导出、加载等功能。

适合修改：

- EditorWindow 的按钮、文案和布局。
- SceneView 编辑器模式的放置体验。
- 传统工具窗口里的 prefab 分类展示。

不适合修改：

- Pico / Play Mode 运行时输入。
- 四周停靠式 World Space UI。

`MapEditorRuntimeSceneSetup.cs`

运行时地图编辑器的一键装配工具。执行 `创建 MR 运行时输入控制器` 时会运行它。

它做的事：

- 创建或复用 `MapEditorRuntime`。
- 创建或复用 `RuntimePlacedObjects`。
- 创建 fallback 鼠标射线原点 `EditorFallbackRayOrigin`。
- 给 `MapEditorRuntime` 绑定 `MapEditorRuntimeController` 和 `MapEditorRuntimeExporter`。
- 扫描 `MapObjects`、`GameplayMarkers`、`Palette` 目录下的 prefab，写入 brush 列表。
- 创建默认 `Palette/Basic Shapes` 基础素材。
- 实例化 `MapEditorDockedCanvas.prefab`。
- 绑定 UI、Camera、controller、exporter。
- 删除旧的 `MapEditorRuntimePanel`。

适合修改：

- 素材扫描目录。
- 默认基础 prefab 的生成。
- 运行时场景里哪些对象自动创建。
- Docked UI prefab 的实例化与引用绑定。

`MapEditorDockedUiPrefabBuilder.cs`

用代码生成 `MapEditorDockedCanvas.prefab` 的 Editor 工具。当前固定 UI 层级不是 Play Mode 临时生成，而是通过这个工具保存成 prefab。

适合修改：

- Top / Left / Right / Bottom 四个面板的初始尺寸和位置。
- 按钮、文本、InputField、Dropdown 的 prefab 层级。
- 默认按钮尺寸、颜色、半透明背景。
- 运行时 UI 组件引用绑定。

注意：修改这个脚本后，需要重新执行：

```text
Tools > TreasureArena > 地图编辑器 > 创建 Docked UI Prefab
Tools > TreasureArena > 地图编辑器 > 创建 MR 运行时输入控制器
```

### Runtime 侧

`MapEditorRuntimeController.cs`

运行时地图编辑器的核心输入控制器。鼠标和 Pico 手柄射线最终都由它处理。

它负责：

- 鼠标 fallback ray。
- XR 右手柄 ray。
- Place / Move / Rotate / Scale 模式。
- 0.5m 网格吸附。
- Ghost 预览。
- 点击地面放置 prefab。
- 点击已有对象选中。
- 删除选中对象。
- 移动、旋转、缩放选中对象。
- UI hit target 拦截。
- 触发状态事件：brush、selection、mode、dragging、status。

适合修改：

- 鼠标或手柄输入键位。
- 放置、移动、旋转、缩放操作。
- 网格大小、旋转步进、缩放步进。
- UI 点击是否阻止地图放置。
- 选中逻辑。

当前重要字段：

```text
gridSize = 0.5
rotateStepDegrees = 15
scaleStep = 0.1
```

`MapEditorRuntimeBrush.cs`

运行时 brush 数据结构。每一个可放置素材都会变成一个 brush。

关键字段：

```text
label
folder
prefab
prefab_id
marker_type
team
treasure_type
supply_type
radius
refresh_interval
has_collider
```

`folder` 决定左侧文件管理器式 Palette 里显示在哪个文件夹。

`MapEditorDockedUiController.cs`

四周停靠式运行时 UI 控制器。

它负责：

- 绑定 controller / exporter。
- 刷新 Top Bar 的地图名、map_id、version、模式、对象数量、红蓝基地数量、宝物点数量、状态。
- 构建左侧文件夹式 Palette。
- 绑定 Bottom Bar 的 Place / Move / Rotate / Scale / Delete / Clear Brush。
- 触发 Validate / Export。
- Validate 失败时显示错误状态并展开 Bottom Bar。
- 选中对象时展开 Right Panel。
- 拖拽物体时降低未 Pin 面板透明度。
- 根据 Camera aspect / FOV 自适应 UI 尺寸和位置。

适合修改：

- 四周 UI 的运行时刷新逻辑。
- 左侧文件夹显示方式。
- Validate / Export 按钮行为。
- 面板随相机自适应规则。

`MapEditorDockedPanel.cs`

单个停靠面板的通用逻辑。

它负责：

- Collapse / Expand。
- Pin。
- 错误红点显示。
- 拖拽时透明度降低。
- 面板展开/折叠时刷新 Physics hit collider 大小。

适合修改：

- 折叠行为。
- Pin 行为。
- 面板透明度。
- 错误状态显示。

`MapEditorObjectInspector.cs`

右侧 Inspector 面板逻辑。

它负责读取和写入当前选中对象的：

- `MapExportMarker.id`
- Transform position / rotation / scale
- `has_collider`
- `team`
- `treasure_type`
- `radius`
- `supply_type`
- `refresh_interval`

按 marker 类型启用字段：

```text
MapObject：id、Transform、has_collider
TeamBase：id、team、radius
TreasureSpawnPoint：id、treasure_type、radius
SupplyBox：id、supply_type、refresh_interval
Bounds：position、localScale
```

适合修改：

- 右侧参数字段。
- 不同 marker 类型显示哪些字段。
- 输入解析和数值限制。

`MapEditorValidationPresenter.cs`

Bottom Bar 的日志和错误列表显示。

它负责：

- 最近日志。
- Validate 结果。
- Export 成功/失败路径显示。

`MapEditorRuntimeExporter.cs`

运行时导出器。

它负责：

- 从场景里的 `MapExportMarker` 构建 `MapJsonModels.MapJson`。
- 调用 `MapValidator.Validate()`。
- 校验失败时阻止导出。
- Editor 下导出到：

```text
Assets/_Project/StreamingAssets/Maps/<map_id>.json
```

- Pico Runtime 下导出到：

```text
Application.persistentDataPath/MapExports/<map_id>.json
```

适合修改：

- 导出路径。
- 导出前校验策略。
- 导出成功/失败日志。

不要在这里新增 JSON 协议外字段。JSON 字段应遵守：

```text
docs/json_protocol.md
```

`MapEditorRuntimeUiHitTarget.cs`

UI 点击拦截辅助组件。挂在 World Space UI 的按钮或面板 collider 上。

作用：

- 被 `MapEditorRuntimeController` 的 Physics Raycast 命中时，阻止地图射线继续放置。
- 如果绑定了 Button，则触发 Button 点击。

当前已知问题：

- 鼠标点击 World Space uGUI 时，如果 Physics collider 没有和 UI 视觉完全对齐，仍可能出现“看起来点到 UI，但实际穿透到地面放置”的情况。
- 更稳的方案是 PC 使用 `GraphicRaycaster.Raycast()` 或 `EventSystem.current.IsPointerOverGameObject()`；Pico 端使用 XR UI Input Module / XR Ray Interactor 对 uGUI 做正式 UI 点击，而不是只靠 Physics collider。

## 地图支持类

`MapExportMarker.cs`

所有可导出对象的语义标记组件。

关键类型：

```text
MapObject
TeamBase
TreasureSpawnPoint
SupplyBox
Bounds
```

导出 JSON 时只认 `MapExportMarker`，不靠 GameObject 名称判断玩法语义。

`MapSceneJsonBuilder.cs`

从场景里的 `MapExportMarker` 生成 JSON 数据结构。

适合修改：

- 场景标记到 JSON 字段的映射。

不应修改：

- 已冻结协议字段名。

`MapValidator.cs`

地图合法性校验。

当前会检查：

- map_id / map_name / version。
- 红蓝基地是否存在。
- 宝物刷新点是否存在。
- base radius 是否有效。
- treasure_type 是否有效。
- supply box 字段是否有效。
- map object 是否有 object_id / prefab_id。

`MapRuntimeBuilder.cs`

从 JSON 反向生成运行时场景对象。

适合 Game 场景加载地图时使用，也可用于 MapEditor 加载已有 JSON 后回显。

## 素材放置规则

左侧面板按文件夹显示素材。扫描目录包括：

```text
Assets/_Project/Prefabs/MapEditor/MapObjects/
Assets/_Project/Prefabs/MapEditor/GameplayMarkers/
Assets/_Project/Prefabs/MapEditor/Palette/
```

推荐把项目素材放到：

```text
Assets/_Project/Prefabs/MapEditor/Palette/<你的文件夹名>/
```

例如：

```text
Assets/_Project/Prefabs/MapEditor/Palette/Library/
Assets/_Project/Prefabs/MapEditor/Palette/Props/
Assets/_Project/Prefabs/MapEditor/Palette/Walls/
```

左侧显示规则：

- `Palette/Basic Shapes/Basic_Cube.prefab` 显示在 `Basic Shapes` 文件夹。
- `Palette/Prefabs/book_1.prefab` 显示在 `Prefabs` 文件夹。
- `MapObjects/Wall.prefab` 显示在 `MapObjects` 文件夹。
- `GameplayMarkers/TeamBase_Red.prefab` 显示在 `GameplayMarkers` 文件夹。

注意：

- 当前只扫描 `.prefab`。
- `.fbx`、`.obj`、贴图、材质不会直接显示。
- 模型需要先做成 Unity prefab，再放入上面的目录。
- 放入新 prefab 后，需要执行一次 `创建 MR 运行时输入控制器`，把新素材写入场景 brush 列表。

## 运行流程

### 启动运行时编辑器

1. 打开 `MapEditor.unity`。
2. 如新增或移动过 prefab，执行：

```text
Tools > TreasureArena > 地图编辑器 > 创建 MR 运行时输入控制器
```

3. 进入 Play Mode。
4. 左侧选择文件夹里的 prefab。
5. 中心区域点击地面放置。
6. 选中对象后右侧编辑参数。
7. 点击 Validate。
8. 校验通过后点击 Export。

### 放置逻辑

`MapEditorRuntimeController` 每帧做：

1. 获取 XR 右手柄射线；如果没有 XR 输入，则用鼠标屏幕点生成相机射线。
2. 先检测是否命中运行时 UI。
3. 如果命中 UI，则触发 UI 或吞掉输入。
4. 如果没命中 UI，再检测场景/地面。
5. Place 模式下显示 Ghost 预览。
6. Trigger / 鼠标点击时实例化当前 brush prefab。
7. 自动挂载或刷新 `MapExportMarker`。
8. 自动命名并选中新对象。

### 导出逻辑

`MapEditorRuntimeExporter.ExportCurrentMap()`：

1. 调 `BuildMapFromScene()` 收集所有 `MapExportMarker`。
2. 调 `MapValidator.Validate(map)`。
3. 校验失败：返回失败，不写 JSON。
4. 校验成功：写入 JSON。

Editor 输出路径：

```text
Assets/_Project/StreamingAssets/Maps/<map_id>.json
```

Pico Runtime 输出路径：

```text
Application.persistentDataPath/MapExports/<map_id>.json
```

## 想改某个功能时改哪里

### 想改左侧素材文件夹显示

优先改：

```text
MapEditorDockedUiController.cs
MapEditorRuntimeBrush.cs
MapEditorRuntimeSceneSetup.cs
```

常见需求：

- 想按二级目录显示：改 `MapEditorRuntimeSceneSetup.GetBrushFolder()`。
- 想显示图标/缩略图：改 `MapEditorDockedUiController.BuildBrushButtons()` 和 `MapEditorDockedUiPrefabBuilder`。
- 想运行时刷新 Palette：给 `MapEditorDockedUiController` 加 Refresh Palette 按钮，并重新读取 controller brush 或重新扫描资产。

### 想改素材扫描目录

改：

```text
MapEditorRuntimeSceneSetup.cs
```

字段：

```text
BrushSearchFolders
PaletteDir
BasicShapesDir
```

### 想改 UI 位置、大小、自适应

运行时自适应改：

```text
MapEditorDockedUiController.RefreshCameraLayout()
```

Prefab 初始布局改：

```text
MapEditorDockedUiPrefabBuilder.cs
```

改完 builder 后执行：

```text
Tools > TreasureArena > 地图编辑器 > 创建 Docked UI Prefab
Tools > TreasureArena > 地图编辑器 > 创建 MR 运行时输入控制器
```

### 想改鼠标/Pico 输入

改：

```text
MapEditorRuntimeController.cs
```

重点看：

```text
Update()
TryGetRuntimeRay()
ReadRuntimeButtons()
HandleButtonEdges()
HandleMouseShortcuts()
TryHandleRuntimeUi()
```

### 想修“点击 UI 也会放置”

当前临时方案：

```text
MapEditorRuntimeUiHitTarget + Physics collider
```

更推荐的正式方案：

- PC 鼠标：用 `GraphicRaycaster.Raycast()` 检测 uGUI。
- Pico 手柄：接入 XR UI Input Module / XR Ray Interactor。
- 放置射线只在“不指向 UI”时执行。

建议修改位置：

```text
MapEditorRuntimeController.TryHandleRuntimeUi()
MapEditorDockedUiPrefabBuilder.cs
MapEditorRuntimeUiHitTarget.cs
```

### 想改右侧参数

改：

```text
MapEditorObjectInspector.cs
MapEditorDockedUiPrefabBuilder.cs
```

如果只是改字段行为，优先改 `MapEditorObjectInspector.cs`。

如果要新增输入框、Dropdown、Toggle，需要同时改 `MapEditorDockedUiPrefabBuilder.cs` 并重新生成 prefab。

### 想改校验错误展示

改：

```text
MapEditorValidationPresenter.cs
MapEditorDockedUiController.ValidateMap()
MapEditorDockedUiController.ExportMap()
```

地图规则本身改：

```text
MapValidator.cs
```

### 想改 JSON 字段

先不要直接改。必须先确认：

```text
docs/json_protocol.md
```

当前协议字段已冻结，MapEditor 不应新增协议外字段。

## Pico 联调注意事项

当前运行时编辑器目标是支持 Pico MR，但 PC Editor 鼠标仍是 fallback 调试路径。

Pico 端重点确认：

- `Camera.main` 是否存在且位置正确。
- 右手柄射线 origin 是否正确。
- Trigger 是否映射为放置/选中。
- Grip 是否用于 Move。
- 摇杆是否用于 Rotate / Scale。
- UI 点击是否走正式 XR UI 输入。

当前风险：

- 如果 Pico 端也只靠 Physics collider 点击 World Space UI，可能出现 UI 穿透放置。
- 推荐后续接入 XR UI Input Module，让 UI 点击走 GraphicRaycaster / EventSystem，而地图放置继续走物理射线。

### Pico 射线点不到 UI 的专项排查

先确认问题属于哪一种：

```text
A. Pico 射线能看到/打到 UI，但 Button 没有 onClick。
B. Pico 射线完全没有和 UI 交互反馈。
C. 点 UI 时没有触发 UI，但触发了地图放置。
D. 点 UI 时既触发 UI，又触发地图放置。
```

当前最可能是 B 或 C，因为 MapEditor 现在的 UI 拦截主要靠 Physics collider，不是完整 XR uGUI 输入链路。

建议排查顺序：

1. 场景里必须有 `EventSystem`。
2. `MapEditorDockedCanvas` 必须是 World Space Canvas。
3. `MapEditorDockedCanvas` 上必须有 `GraphicRaycaster`。
4. Pico 项目如果使用 XR Interaction Toolkit，需要场景里有 XR UI 输入模块，例如 `XRUIInputModule`，而不是只用 `StandaloneInputModule`。
5. 右手柄射线对象需要有能和 uGUI 交互的 Ray Interactor / UI Interactor。
6. Button 的 onClick 是否正常绑定：先用鼠标或 Unity EventSystem 测。
7. 地图放置逻辑必须在 UI 判断之后执行。

当前代码里输入顺序在：

```text
MapEditorRuntimeController.Update()
```

里面应保持这样的顺序：

```text
1. 读取鼠标/手柄射线。
2. 先判断是否命中 UI。
3. 如果命中 UI，则只处理 UI，不进入地图放置/选中。
4. 如果没有命中 UI，再执行地面/对象 Physics.Raycast。
```

如果 Pico UI 改成 XR UI Input Module，建议新增一个明确的 UI blocking 判断，例如：

```text
IsPointerOverRuntimeUi()
```

它只回答一个问题：

```text
当前鼠标/手柄是否正在指向 MapEditor UI？
```

然后在 `Update()` 中：

```text
if (IsPointerOverRuntimeUi())
{
    DestroyPreviewGhost();
    return;
}
```

注意：`EventSystem.current.IsPointerOverGameObject()` 对 PC 鼠标常用，但 Pico 手柄通常需要传 pointer id 或通过 XR UI 模块/Interactor 判断，不能直接照搬鼠标逻辑。

### 不建议继续扩展的临时方案

当前 `MapEditorRuntimeUiHitTarget` 是临时桥接方案：

```text
UI GameObject + BoxCollider + MapEditorRuntimeUiHitTarget
```

它可以作为 PC/MR 早期调试兜底，但不建议继续把所有 Pico UI 交互都建立在它上面。原因：

- World Space Canvas 自适应缩放后，collider 容易和视觉 UI 不完全重合。
- uGUI Dropdown / InputField 不适合用 Physics collider 手动模拟点击。
- Pico 手柄 UI hover、press、drag、scroll 都应该走 XR UI 输入系统。
- 地图物理射线和 UI 物理射线混用，容易互相误触。

所以 Pico 同学优先做的是：

```text
让 Pico 手柄射线真正驱动 uGUI Button/InputField/Dropdown。
```

而不是继续调 BoxCollider 大小。

## 常见问题排查

### 新素材放进去后左侧不显示

检查：

1. 是否是 `.prefab`。
2. 是否放在：

```text
Assets/_Project/Prefabs/MapEditor/Palette/<文件夹>/
```

3. 是否执行了：

```text
Tools > TreasureArena > 地图编辑器 > 创建 MR 运行时输入控制器
```

4. 场景里的 `MapEditorRuntime > MapEditorRuntimeController > brushes` 是否出现新条目。
5. Play Mode 是否重新进入过。

### 点击 UI 仍然在地上放置

说明 UI 命中判断没有先于地图放置生效。

临时检查：

- UI 按钮/面板是否有 `BoxCollider`。
- UI 按钮/面板是否有 `MapEditorRuntimeUiHitTarget`。
- Collider 是否和视觉 UI 对齐。
- `MapEditorRuntimeController.TryHandleRuntimeUi()` 是否在放置逻辑之前执行。

推荐修法：

- PC 鼠标改为 GraphicRaycaster 检测 UI。
- Pico 端改为 XR UI Input Module。

### Validate 失败

常见错误：

```text
missing_team_bases
missing_red_base
missing_blue_base
missing_treasure_spawn_points
invalid_base_radius
invalid_treasure_type
missing_prefab_id
```

处理：

- 至少放一个 Red Base。
- 至少放一个 Blue Base。
- 至少放一个 Treasure Spawn Point。
- 半径必须大于 0。
- MapObject 必须有 `prefab_id`。

### Export 没生成 JSON

检查：

- Validate 是否失败。
- Bottom Bar 错误列表。
- Console 是否有明确错误。
- Editor 下路径是否是：

```text
Assets/_Project/StreamingAssets/Maps/<map_id>.json
```

### 右侧参数改了但导出不对

检查：

- 选中对象上是否有 `MapExportMarker`。
- Right Panel 是否改的是 marker 字段，不只是 GameObject 名称。
- `MapSceneJsonBuilder.cs` 是否读取了对应字段。

## 当前已知 TODO

- UI 点击拦截应从 Physics collider 升级为正式 uGUI / XR UI raycast。
- Left Panel 文件夹目前是一级文件夹树，后续可支持多级目录。
- Left Panel 当前显示文本按钮，后续可加缩略图、搜索、筛选。
- Right Panel 当前由 prefab builder 创建固定字段，后续可做更清晰的按 marker 类型分组显示。
- Pico 真机 UI 点击和手柄输入需要实机验收。

## 人工验证清单

1. 打开：

```text
Assets/_Project/Scenes/MapEditor.unity
```

2. 如刚导入素材，执行：

```text
Tools > TreasureArena > 地图编辑器 > 创建 MR 运行时输入控制器
```

3. 进入 Play Mode。
4. 左侧应显示文件夹，例如：

```text
Basic Shapes
GameplayMarkers
MapObjects
Prefabs
```

5. 展开文件夹，点击 prefab。
6. 在中心地图区域点击，应该放置对象。
7. 点击 UI 面板本身，不应该放置对象。
8. 选中对象，Right Panel 应显示名称、类型、Transform 和 marker 参数。
9. 不放红蓝基地或宝物点时 Validate 应显示错误并阻止 Export。
10. 补齐红蓝基地和宝物点后 Export 应生成 JSON。
11. Console 不应出现：

```text
NullReferenceException
MissingReferenceException
error CS
```

如果失败，优先检查：

```text
MapEditorRuntimeSceneSetup.cs
MapEditorRuntimeController.cs
MapEditorDockedUiController.cs
MapEditorDockedCanvas.prefab
MapExportMarker
MapValidator.cs
```
