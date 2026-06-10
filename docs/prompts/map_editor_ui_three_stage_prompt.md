# Prompt：三阶段实现 MapEditor 四周停靠式 UI

你将接手 TreasureArenaMR 项目的 MapEditor UI 实现。请严格遵守项目文档和协议边界。

## 当前状态

项目是基于 Pico 4 的 MR 多人联机双队夺宝游戏，Unity 工程路径为：

```text
TreasureArenaUnity/
```

地图编辑器是 MVP 子项目，负责摆放 prefab、玩法标记、编辑 transform、导出地图 JSON，并调用 MapValidator 校验。地图编辑器不参与战斗判定、房间同步、数据库写入或服务器权威逻辑。

## 必须先阅读

请先阅读：

```text
README.md
PROJECT_CONTEXT.md
AGENTS.md
docs/project_design.md
docs/json_protocol.md
docs/database_design.md
docs/map_editor_design.md
docs/map_editor_ui_design.md
```

## 本阶段目标

按 `docs/map_editor_ui_design.md` 的三阶段方案实现 MapEditor UI：

```text
UI-1：基础四周布局
UI-2：折叠、展开与固定
UI-3：参数编辑、校验反馈与 MR 优化
```

UI 采用四周停靠式布局，半透明，不遮挡中心地图编辑视野。面板支持折叠、展开、固定。

## 允许修改

优先只修改：

```text
TreasureArenaUnity/Assets/_Project/Scripts/MapEditor/
TreasureArenaUnity/Assets/_Project/Prefabs/MapEditor/
TreasureArenaUnity/Assets/_Project/Scenes/MapEditor.unity
```

如确需新增通用 UI 脚本，可放在：

```text
TreasureArenaUnity/Assets/_Project/Scripts/UI/
```

## 禁止修改

```text
1. 不得修改 docs/json_protocol.md 中已冻结字段。
2. 不得修改 RoomConfig、PlayerState、TeamType 等核心数据结构。
3. 不得新增 JSON 协议字段。
4. 不得修改数据库 schema。
5. 不得在 MapEditor UI 中实现战斗、HP、得分、拾取、提交、复活等服务器权威逻辑。
6. 不得新增正式单人玩法。
7. 不得大量用代码生成固定 UI 层级；固定面板优先 Canvas / Prefab 可视化搭建。
8. 不得删除 .meta 文件。
```

## UI-1：基础四周布局

实现：

```text
1. Top Bar：地图名、当前模式、Validate、Export、状态。
2. Left Panel：Brush 列表 + Gameplay Marker 列表。
3. Right Panel：选中对象名称、类型、基础 Transform。
4. Bottom Bar：Place / Move / Rotate / Scale / Delete / Clear Brush、最近日志。
5. 所有面板半透明，固定在四周，不遮挡中心编辑区域。
```

验收：

```text
1. 打开 MapEditor 场景并进入 Play Mode，四周 UI 可见。
2. 左侧可选择普通 prefab 和玩法标记。
3. Validate / Export 可更新状态。
4. 选中对象后右侧显示基础信息。
5. 中心区域仍可正常射线放置对象。
```

## UI-2：折叠、展开与固定

实现：

```text
1. Top / Left / Right / Bottom 四个面板都支持折叠和展开。
2. 每个面板支持 Pin 固定。
3. 折叠状态显示图标、标题、错误状态。
4. 拖动对象时，未固定面板自动降低透明度。
5. 选中对象时 Right Panel 自动展开。
```

验收：

```text
1. 每个面板可以独立折叠/展开。
2. Pin 后面板不会自动折叠。
3. Validate 失败时，折叠状态仍有红点或错误状态提示。
4. MR 手柄射线或鼠标都能点击折叠/固定按钮。
```

## UI-3：参数编辑、校验反馈与 MR 优化

实现：

```text
1. Right Panel 支持编辑 MapExportMarker 参数。
2. MapObject：object_id、Transform、has_collider。
3. TeamBase：team、radius。
4. TreasureSpawnPoint：treasure_type、radius。
5. SupplyBox：supply_type、refresh_interval。
6. Bounds：position/localScale 对应 bounds center/size。
7. Validate 失败时 Bottom Bar 展开错误列表。
8. Export 成功时显示导出路径。
9. 按钮尺寸适配 Pico MR 手柄射线点击。
10. 面板贴近 Camera.main，并且 UI 不抢占地图放置射线。
```

验收：

```text
1. 各 marker 类型的参数可在 Right Panel 编辑并反映到 MapExportMarker。
2. 缺少红蓝基地或宝物点时 Validate 显示明确错误并阻止导出。
3. 导出的 JSON 不包含协议外字段。
4. Pico MR 下按钮可被射线稳定点击。
5. Console 无 NullReferenceException、MissingReferenceException、error CS。
```

## 完成后输出

请输出：

```text
1. 修改了哪些文件。
2. 每个文件的作用。
3. 完成了 UI-1 / UI-2 / UI-3 中哪些内容。
4. 如何人工验证。
5. 是否实际运行 Unity 编译。
6. 是否修改协议字段。
7. 是否可能影响其他模块。
8. 还剩哪些 TODO。
```

人工验证必须包含：

```text
1. 打开哪个 Unity 场景。
2. 当前 AppRole 应设置为什么。
3. 是否需要同时启动 Server / Manager / PicoClient / LocalTest。
4. Inspector 中需要确认哪些组件或引用。
5. 具体点击或操作步骤。
6. 正常情况下应该看到什么。
7. Console 中应该出现或不应该出现什么。
8. 如果失败优先检查哪些脚本、Prefab、组件或配置。
```
