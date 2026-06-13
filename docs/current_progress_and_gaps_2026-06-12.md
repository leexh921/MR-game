# TreasureArenaMR 当前进度与缺口梳理

> 日期：2026-06-12  
> 用途：给项目负责人和 PM 讨论“地图编辑器成品”和“游戏成品”剩余逻辑。  
> 依据：已阅读 `README.md`、`PROJECT_CONTEXT.md`、`AGENTS.md`、`docs/project_design.md`、`docs/json_protocol.md`、`docs/database_design.md`、`docs/map_editor_design.md`、`docs/map_editor_ui_design.md`、`docs/map_editor_runtime_guide.md`，并检查当前 Unity 脚本与已打开的 `Game.unity` 状态。

## 1. 总结结论

当前项目已经不是空工程，已经有地图 JSON、MapLoader/MapValidator、MapEditor 运行时编辑、Netick 最小连接、HUD、服务端房间/队伍/宝物/攻击/复活倒计时等代码骨架。

但当前距离“地图编辑器成品”和“游戏成品”还有明显距离：

```text
地图编辑器：能摆放、校验、导出，Pico 端 UI 已可点击并已引入 XR UI Input Module；当前主要缺口是手柄拖动手感、真实地面校准、空间边界绘制流程还未成品化。
游戏部分：Pico 与 Home/Server 的基础 Netick 连接已打通，但完整对局闭环没有打通；地图加载、敌人显示、玩家同步、拾取/提交/攻击/复活/结算/数据库仍需系统集成。
管理端：Home 场景已有运行时 UI 脚本，但房间创建和地图选择仍是 MVP 临时逻辑，不是最终产品逻辑。
```

一句话判断：当前阶段更接近“多模块基础能力已搭好”，还不是“可演示完整多人夺宝游戏”。

## 2. 已实现功能

### 2.1 项目基础与协议

已实现或已建立：

- Unity 单工程、多运行角色方向已确定：`Server / Manager / PicoClient / LocalTest`。
- 地图 JSON 字段已迁移为 `objects / team_bases / treasure_spawn_points / supply_boxes / map_boundary`，其中 `team_bases` 合并出生、复活和提交语义，`map_boundary` 使用 Polygon。
- 房间配置、玩家状态、宝物状态、队伍、对局结果等 Shared 数据结构已存在。
- 数据库设计文档和 SQL schema 已有，但数据库运行时写入尚未接入完整游戏流程。

相关代码/文档：

```text
docs/json_protocol.md
docs/database_design.md
TreasureArenaUnity/Assets/_Project/Scripts/Shared/
```

### 2.2 地图加载与校验

已实现：

- `MapLoader` 可以从 `StreamingAssets/Maps/<map_id>.json` 读取地图 JSON。
- `MapValidator` 会校验 map_id、map_name、map_version、map_boundary、红蓝基地、宝物点、物资箱参数、object_id/prefab_id 和对象能力字段。
- `MapRuntimeBuilder` 可以用基础 Primitive 把地图 JSON 生成可视占位物。
- `MapRuntimeLoader` 可以在场景中加载指定 mapId。
- `RuntimeMapCatalog` 可以列出运行时地图 ID，并把 mapId 转成网络同步用的 index。

当前已有运行时地图：

```text
TreasureArenaUnity/Assets/_Project/StreamingAssets/Maps/map_editor.json
TreasureArenaUnity/Assets/_Project/StreamingAssets/Maps/map_template_gameplay.json
TreasureArenaUnity/Assets/_Project/StreamingAssets/Maps/new_map.json
TreasureArenaUnity/Assets/_Project/StreamingAssets/Maps/pico_map.json
```

注意：

```text
map_samples/normalized/test_map_01.json 仍在样例目录，不在 Unity 运行时 Maps 目录。
```

### 2.3 地图编辑器

已实现：

- `MapEditor.unity` 场景存在。
- 运行时编辑器核心脚本已存在：`MapEditorRuntimeController`。
- 支持 brush/prefab 放置、选择、移动、旋转、缩放、删除、复制、重置 Transform。
- 支持左右手独立 brush 状态、Active Hand、Ghost 预览、0.5m 网格吸附。
- 支持玩法标记：`MapObject / TeamBase / TreasureSpawnPoint / SupplyBox`；地图边界通过 Draw Bounds / Draw Area 生成 `map_boundary`，不再作为 Bounds prefab 导出。
- 支持右侧 Inspector 修改 Transform、team、treasure_type、radius、supply_type、refresh_interval 等。
- 支持 Validate 和 Export，导出前会调用 `MapValidator`。
- Editor 下导出到 `Assets/_Project/StreamingAssets/Maps/<map_id>.json`。
- Pico Runtime 下导出到 `Application.persistentDataPath/MapExports/<map_id>.json`，并尝试复制到 Android Download 公共目录。
- 有运行时说明文档 `docs/map_editor_runtime_guide.md`。

当前明确未成品的问题：

- Pico 端 UI 点击链路已接入 XR UI Input Module，当前不再把“手柄点不到 UI”作为主要阻塞。
- `MapEditorRuntimeUiHitTarget + BoxCollider + Physics.Raycast` 仍可视为兼容/兜底桥接；后续优化重点应放在“点击 UI 时不触发放置”的边界验证，而不是继续把 UI 点击当作未接通问题。
- 拖动物体目前主要是射线平面/表面拖动，缺少成品级手感设计，例如抓取偏移、深度锁定、平滑、阻尼、吸附提示、误触保护。
- 地面 fallback 已改为可由 Calibrate Floor 记录的 `editorFloorY/floorPlane`，后续仍需 Pico 真机体验验收。
- 边界协议已迁移为 `map_boundary` Polygon；后续重点是 Draw Bounds / Draw Area 的真机手感与闭合交互验收。

### 2.4 Pico / Netick 连接

已实现：

- `PicoClient_Netick.unity` 最小接入场景存在。
- `PicoClientNetickBootstrap` 可以配置 Server IP、端口、playerId，并自动连接。
- 项目上下文记录：2026-06-10 已验证 Pico 真机 Build 后可通过局域网连接 PC Netick Server，Server 侧能看到接入日志。
- 当前 Unity MCP 读取到 `Game.unity` 中 `PicoClientNetworkBootstrap` 配置为 `172.18.145.225:7777`，`connectOnStart=true`。

已实现但未形成完整闭环：

- `PicoClientNetickBootstrap` 有根据 `NetworkMatchState.MapIndex/MapRevision` 加载地图视觉的代码。
- 但当前实际体验是连接后主要显示 HUD，说明地图同步/加载/网络运行场景与 Home/Server 流程还未稳定打通。

### 2.5 服务端与游戏逻辑骨架

已实现：

- `ServerBootstrap`：Home 场景中作为 Manager + Server 入口，能启动 server、选择 mapId、开始/停止比赛、调用 SwitchTeam。
- `ServerApp`：启动 Netick Server、创建默认房间、加载地图数据、生成 `NetworkMatchState`、尝试生成宝物。
- `RoomManager`：维护当前房间、玩家列表、红蓝分队、房间状态、计时、比分、复活倒计时。
- `SandboxNetworkListener`：监听 Netick 玩家连接，加入 RoomManager，自动分队，尝试生成玩家对象。
- `NetworkPlayer`：同步队伍、状态、HP、携带宝物、复活倒计时。
- `NetworkMatchState`：同步房间状态、红蓝比分、剩余时间、地图 index/revision。
- `ServerTreasureAuthority`：支持生成宝物、拾取最近宝物、提交得分、掉落宝物。
- `ServerCombatAuthority`：支持服务端射线/半径判定攻击、扣 HP、HP 归零进入 `GhostRetreat`、掉落宝物。
- `PlayerNetInput`：Pico/Editor 输入会转成 Attack/Pickup/Submit，服务端侧处理。
- `HudView`：Pico HUD 能显示 HP、比分、倒计时、携带宝物、GhostRetreat、Respawning、结算面板。

当前仍需注意：

- 这些逻辑是“代码路径存在”，不等于“端到端已验收通过”。
- `ClientRequestSender`、`ClientStatePresenter`、`PicoClientApp`、`PicoInputController` 仍是 placeholder。
- 数据库保存结果尚未接入完整流程。
- `RoomStateSynchronizer` 注释里仍写着 TODO：后续要用 Netick NetworkBehaviour/RPC 完成更正式同步。

## 3. 对你提出问题的直接回答

### 3.1 创建房间是不是写死了三个房间？

从当前代码看，不是真正创建三个服务端房间。

当前情况更像：

```text
服务端实际只有一个 CurrentRoomConfig / 默认房间。
Home UI 上的三个 RoomSlot 按钮看起来像房间槽。
但 ManagerHomeView 运行时把这三个按钮复用成：
  1. Select 当前玩家
  2. Set Red
  3. Set Blue
```

所以你的感觉是对的：UI 表现仍像“房间 A/B/C”，但脚本逻辑已经临时改成“选玩家/设红队/设蓝队”。这会让管理端很难理解，需要重做成明确的玩家列表和队伍分配按钮。

### 3.2 选择地图按钮现在是不是只是切换，不是选择？

是。当前 `ManagerHomeView.SelectNextMap()` 调用 `ServerBootstrap.SelectNextMap()`，逻辑是从 `RuntimeMapCatalog.MapIds` 中循环切到下一张地图。

这不是最终产品应有的“打开地图列表 → 选中某张地图 → 确认使用”。

最终应改为：

```text
地图列表显示所有可用 map_id / map_name
点击某张地图后进入 selected 状态
点击确认后写入 room config
若房间已创建但未开始，可允许更换地图并重置当前准备状态
若对局已开始，不允许更换地图
```

### 3.3 红蓝队现在管理端可分配吗？

代码层面：可以，但 UI 不是最终形态。

当前分队路径：

```text
玩家连接后，RoomManager.AutoAssignTeam() 会自动按人数平衡分到 Red/Blue。
管理端可以通过 ManagerHomeView：
  SelectNextPlayer()
  AssignSelectedPlayerRed()
  AssignSelectedPlayerBlue()
再调用 ServerBootstrap.SwitchTeam() → RoomManager.SwitchTeam()。
```

也就是说：当前可通过临时 UI 分配，但不是清晰的“客户端列表 → 选择玩家 → 分配红/蓝队”成品流程。

### 3.4 计划启动 Home 场景，Server 开始，Pico 接入，再分配队伍，这个方向可行吗？

可行，而且与当前代码方向一致。

当前需要把流程收敛成：

```text
打开 Home.unity
自动或点击启动 Server
创建一个真实 RoomConfig
加载/选择地图
Pico 接入并出现在玩家列表
管理端手动分配 Red/Blue
点击 Start Match
Server 生成地图、玩家、宝物
Pico 客户端显示地图、自己、敌人、宝物、HUD
```

现在的问题不是方向错，而是流程还没有被产品化和端到端验收。

## 4. 距离地图编辑器成品还差什么

### 4.1 必须补齐的核心体验

1. 手柄拖动物体手感重做  
   当前拖动偏工程验证。成品需要定义：
   - Trigger 选中还是 Grip 抓取？
   - 抓住后是否保持相对偏移？
   - 移动是在地面/表面/自由空间？
   - 是否带平滑、吸附、取消、撤销？
   - 拖动时是否锁高度、锁轴、显示目标落点？

2. 真实地面高度校准  
   当前 fallback 地面是 Unity `y=0`，不是真实地面。需要选择：
   - 使用 Pico/AR 平面检测结果作为编辑地面。
   - 或首次进入编辑器时让用户用手柄点地面，设置 `editor_floor_y`。
   - 或使用 XR Origin 校准，把 Unity y=0 对齐真实地面。

3. 空间范围绘制流程  
   你希望“先画一个空间，在空间范围内画边界，然后开始摆放”。当前协议已改为 `map_boundary` Polygon：
   - MVP 通过 Draw Bounds / Draw Area 逐点绘制边界。
   - 边界点贴 `editorFloorY/floorPlane`。
   - Server/Client 后续按 XZ 多边形 + height 判断越界。

4. 编辑顺序产品化  
   建议地图编辑器成品流程改成：

```text
进入 MapEditor
→ 校准真实地面
→ 绘制/确认可编辑空间
→ 绘制/确认地图边界
→ 选择素材并摆放
→ 放置红蓝基地、宝物点、物资箱
→ Validate
→ Export
→ Preview
```

### 4.2 当前可以保留的基础

- `MapExportMarker`、`MapSceneJsonBuilder`、`MapValidator`、`MapEditorRuntimeExporter` 可以保留。
- 现有 `objects / team_bases / treasure_spawn_points / supply_boxes / map_boundary` 协议可以支撑手绘 Polygon 边界版本。
- 右侧 Inspector、Validate、Export、Prefab palette 可以继续迭代，不必推倒重做。

## 5. 距离游戏成品还差什么

### 5.1 管理端/Home

必须补齐：

- 真正的房间创建 UI：不是默认房间，不是静态 RoomSlot。
- RoomConfig 表单：地图、对局时间、最大 HP、武器伤害、复活倒计时、宝物分值、最大人数。
- 地图选择列表：点击选中并确认，不是循环切换。
- 玩家列表：显示 player_id、nickname、连接状态、队伍、HP、准备/在线状态。
- 手动分队：明确按钮或拖拽到红/蓝队。
- 开始比赛前校验：至少 1 红 1 蓝、地图已加载、必要配置有效。
- 对局中状态面板：比分、剩余时间、玩家状态、宝物状态。
- 对局结束结果面板：胜负、比分、玩家数据。

### 5.2 Server/联网

必须补齐：

- Home 启动 Server 的流程固定化：启动即开 Server，还是点击创建房间后开 Server，需要 PM 定。
- 玩家连接后是否自动加入当前房间，需要规则化。
- 队伍切换、开始游戏、地图选择必须广播给客户端并稳定生效。
- 玩家 prefab 生成、位置同步、敌我显示需要端到端验证。
- NetworkMatchState 只同步 match-level 信息，不够完整；玩家/宝物/房间状态需要统一同步策略。
- 客户端断线、重连、房间满、比赛中拒绝加入等基础边界需要明确。

### 5.3 Pico 客户端

必须补齐：

- 连接后加载地图并显示在 MR 空间中。
- 显示自己、敌人、队伍颜色、敌我区分。
- 显示宝物、基地、复活区、物资箱。
- Pico 输入映射需要产品化：攻击、拾取、提交、菜单、回退。
- HUD 需要接真实同步状态，不只是显示面板。
- GhostRetreat 需要明确视觉提示：不能攻击/拾取/提交，提示玩家走回己方复活区。
- 复活区倒计时需要在客户端稳定显示。

### 5.4 核心游戏闭环

必须补齐并验收：

```text
Server 创建房间
→ Pico/PC Editor 客户端接入
→ 管理端分队
→ 管理端选择地图并开始
→ Server 加载地图并生成玩家/宝物
→ 客户端显示地图/玩家/宝物
→ 玩家拾取宝物
→ 玩家带回己方基地提交
→ Server 加分并同步 HUD
→ 玩家攻击敌人
→ HP 归零进入 GhostRetreat
→ 宝物掉落
→ 真实走回复活区
→ Server 倒计时复活
→ 时间结束结算
→ 写入数据库
→ 管理端展示结果
```

当前这些步骤里，部分代码存在，但尚未形成可稳定演示的完整流程。

### 5.5 数据库

必须补齐：

- Unity 运行时数据库连接管理。
- 创建房间时写入 `room_config`。
- 对局结束时写入 `match_result`。
- 写入每个玩家的 `player_match_stat`。
- 管理端读取并显示对局结果。

当前数据库更接近“schema 和设计已准备好”，不是“游戏运行已写库”。

## 6. 建议和 PM 重点讨论的问题

### 6.1 地图编辑器 PM 决策

1. 空间边界到底是矩形盒状，还是手绘多边形？
   - 矩形盒状：不改 JSON 协议，最快。
   - 手绘多边形：需要协议变更，影响 MapValidator、MapLoader、Server 边界判断。

2. 地面校准方式怎么做？
   - 自动使用 Pico 平面检测。
   - 手动点地面校准。
   - 固定 Unity y=0，只用于实验室固定场地。

3. 摆放前是否强制先完成“空间/边界/地面”向导？
   - 强制向导更适合非技术用户。
   - 自由编辑更适合开发调试。

4. 手柄交互标准：
   - Trigger：UI 点击 / 选择 / 放置？
   - Grip：抓取移动？
   - 摇杆：旋转/缩放？
   - 是否需要撤销/重做？

5. 导出地图后如何进入游戏？
   - 直接保存到当前工程 StreamingAssets。
   - Pico 导出到 Download 后由 PC 导入。
   - 管理端扫描可用地图并刷新列表。

### 6.2 游戏 PM 决策

1. Home 场景启动后 Server 是否自动启动？
   - 推荐：MVP 自动启动 Server，但房间需要点击创建。

2. 是否只允许一个当前房间？
   - 推荐：MVP 只做一个当前房间，先不要做多房间列表。

3. Pico 接入后是否自动加入当前房间？
   - 推荐：MVP 自动加入，管理端手动分队。

4. 开始比赛前最小人数？
   - 当前代码可 1 人开始，产品上建议至少 1 红 1 蓝。

5. 地图选择逻辑：
   - 推荐：从地图列表选择一个，点击确认；未开始时可换，开始后不可换。

6. 客户端显示范围：
   - MVP 是否只显示胶囊敌人和简单宝物/基地？
   - 是否需要真实 MR 平面遮挡/空间网格？

7. LocalTest 是否作为正式验收入口？
   - 推荐：只做开发调试，不进入正式玩法入口。

## 7. 建议后续实现顺序

### 第一阶段：先把 Home 流程讲清楚并做稳定

目标：

```text
Home.unity 打开后能启动 Server
管理端创建唯一房间
显示真实连接玩家
可手动把玩家分到红/蓝
可从列表选择地图
可点击 Start Match
```

这一步先不追求完整战斗，只把“房间/地图/队伍/开始”做清楚。

### 第二阶段：打通 Pico 地图与玩家显示

目标：

```text
Pico 连接 Home Server
收到地图选择
加载并显示地图
服务端生成自己和敌人
Pico 能看到敌我玩家位置和队伍颜色
HUD 显示真实同步状态
```

### 第三阶段：夺宝闭环

目标：

```text
生成宝物
拾取
携带
提交
比分同步
时间结束结算
```

### 第四阶段：攻击与 GhostRetreat

目标：

```text
攻击请求
服务端判定命中
扣 HP
进入 GhostRetreat
掉宝
走回复活区
倒计时复活
```

### 第五阶段：数据库与结果

目标：

```text
创建房间写 room_config
对局结束写 match_result 和 player_match_stat
管理端展示结果
```

### 地图编辑器并行阶段

建议并行做：

```text
Pico UI 点击链路修复
拖动手感重做
真实地面校准
空间/边界绘制向导
导出后管理端地图列表刷新
```

## 8. 当前风险

- UI 文案和运行时逻辑不一致：Home 里“房间 A/B/C”与实际“选玩家/设红队/设蓝队”冲突。
- 地图选择是循环切换，用户不可见完整列表，容易误解。
- 当前 Game 场景像 Pico Client 场景，不是完整游戏服务端场景；连接后只看到 HUD 是合理现象。
- Pico UI 已可点击；后续风险主要是 UI 点击与地图放置/拖动之间的输入边界是否稳定，避免点 UI 时误放置或误选物体。
- 如果 PM 坚持“手绘多边形边界”，需要先确认是否允许修改 `docs/json_protocol.md` 中冻结的地图协议。
- 数据库尚未进入真实对局写入流程，不能对外说“结果已保存到数据库”。

## 9. 当前可作为验收事实的内容

可以谨慎对外说：

```text
1. Pico 到 PC Netick Server 的最小连接已验证。
2. 地图 JSON 协议、校验器、加载器已具备基础能力。
3. MapEditor 已具备基础摆放、标记、校验、导出能力。
4. 服务端已有房间、队伍、计时、宝物、攻击、GhostRetreat、复活倒计时代码骨架。
5. Pico HUD 已能读取 NetworkPlayer / NetworkMatchState 的同步状态。
```

不能说成已完成：

```text
1. 完整多人夺宝闭环。
2. 管理端成品房间系统。
3. 管理端成品地图选择系统。
4. Pico 客户端稳定加载地图并显示敌人。
5. 成品级地图编辑器手柄拖动/摆放交互。
6. 真实地面同高和手绘空间边界。
7. 对局结果写入数据库。
```
