# TreasureArenaMR 项目总设计文档

> 责任人：李潇涵  
> 文档作用：说明项目系统架构、玩法规则、MVP 范围、服务器权威边界、Unity 管理端范围、成员分工和开发路线。

---

## 1. 项目定位

TreasureArenaMR 是一个基于 Pico 4 的 MR 多人联机双队夺宝竞技游戏。

玩家分为红队和蓝队，在同一 MR 场景中争夺宝物。玩家可以拾取宝物并带回己方基地提交得分，也可以使用武器攻击敌方玩家。玩家 HP 归零后不会被虚拟传送，而是进入适合 MR 空间体验的“濒死撤离状态”，需要真实走回己方复活区，并在倒计时结束后重新参与游戏。

项目目标不是一开始做完整美术成品，而是先完成一个无正式素材但可运行的多人闭环版本。

---

## 2. 当前重要修正

老师提到的“地图编辑 / 地图绘制软件”不是当前已有工具给我们直接使用，而是可以作为后续扩展功能开发。

因此 MVP 阶段不开发地图编辑器。

MVP 阶段地图处理方式改为：

```text
1. 开发组先手动制作测试地图。
2. 地图可以先用 Unity 场景中的基础物体搭建。
3. 同时定义我们自己的地图 JSON 协议。
4. 地图 JSON 用于描述基地区（同时作为出生区和复活区）、宝物刷新点、物资箱和地图物体。
5. 后续如果时间允许，再开发地图编辑器或地图导出工具。
```

当前优先级：

```text
先做完整多人双队夺宝闭环
→ 再完善地图 JSON 加载
→ 最后再考虑地图编辑器功能
```

---

## 3. 总体架构

项目采用一个 Unity 工程、多运行角色的架构。

```text
TreasureArenaMR
│
├── Unity 管理端 Manager
│   ├── 创建房间
│   ├── 选择地图
│   ├── 配置 HP、武器伤害、复活时间、对局时间
│   ├── 分配红蓝队
│   └── 控制开始 / 结束游戏
│
├── Unity / Netick 服务端 Server
│   ├── 房间管理
│   ├── 玩家连接
│   ├── 队伍分配
│   ├── 地图加载
│   ├── 宝物生成
│   ├── 拾取 / 提交 / 攻击 / 扣血 / 复活判定
│   └── 结算和写入数据库
│
├── Pico 4 客户端 PicoClient
│   ├── MR 画面显示
│   ├── 头显和手柄输入
│   ├── 玩家交互
│   ├── HUD 和提示 UI
│   └── 向服务端发送操作请求
│
├── 地图 JSON
│   ├── 由项目组自行定义
│   ├── MVP 阶段手动编写或由简单工具生成
│   └── 后续可接地图编辑器
│
└── 数据库
    ├── 保存玩家
    ├── 保存地图信息
    ├── 保存房间配置
    ├── 保存对局结果
    └── 保存玩家统计
```

---

## 4. Unity 运行角色

项目使用一个 Unity 工程，通过运行角色区分不同端。

```csharp
public enum AppRole
{
    Server,
    Manager,
    PicoClient,
    LocalTest
}
```

### Server

运行在 PC 上，负责 Netick 服务端和服务器权威逻辑。

### Manager

运行在 PC 上，负责 Unity 管理端 UI。MVP 阶段可以与 Server 合并运行，形成：

```text
PC Unity 程序 = Manager + Server
Pico 4 = Client
PC Editor = 模拟客户端 / 调试客户端
```

### PicoClient

运行在 Pico 4 上，负责 MR 显示、输入、交互和 UI。

### LocalTest

仅用于开发调试多人流程，例如 PC Editor 模拟客户端，不作为正式单人玩法。

---

## 5. 玩法规则

### 5.1 基本流程

```text
1. 管理端创建多人房间。
2. 管理端选择地图。
3. 管理端配置对局参数。
4. Pico 4 玩家客户端连接房间。
5. 管理端分配红队和蓝队。
6. 管理端开始游戏。
7. 服务端加载地图并初始化宝物、基地、复活区。
8. 玩家在场景中争夺宝物。
9. 玩家拾取宝物后带回己方基地提交得分。
10. 玩家可以使用武器攻击敌方玩家。
11. HP 归零的玩家进入濒死撤离状态。
12. 濒死玩家携带的宝物掉落。
13. 濒死玩家走回己方复活区。
14. 进入复活区后开始倒计时。
15. 倒计时结束后恢复 HP 并重新参与游戏。
16. 时间结束后服务端结算红蓝队分数。
17. 对局结果写入数据库。
18. 管理端显示结果。
```

### 5.2 队伍规则

```text
1. 玩家分为 Red 和 Blue 两队。
2. 玩家可以由管理端手动分队。
3. 后续可增加自动分队。
4. 每个玩家只能属于一个队伍。
5. 玩家只能向己方基地提交宝物。
```

### 5.3 宝物规则

MVP 阶段宝物类型建议：

```text
NormalTreasure：普通宝物
RareTreasure：稀有宝物
FinalTreasure：最终宝物，可选
```

规则：

```text
1. 宝物由服务端生成。
2. 每个宝物有唯一 treasure_id。
3. 玩家靠近宝物并发送拾取请求。
4. 服务端判断玩家是否可以拾取。
5. 每个玩家同一时间最多携带一个宝物。
6. 玩家携带宝物进入己方基地后可以提交。
7. 提交成功后队伍增加对应分数。
8. 玩家进入濒死撤离状态时，携带宝物掉落在当前位置。
```

### 5.4 武器和 HP 规则

MVP 阶段先做一种基础武器，例如能量枪 / 射线枪。

可配置项：

```text
player_max_hp：玩家最大 HP
weapon_damage：武器伤害
weapon_range：武器射程
weapon_cooldown：攻击冷却
```

规则：

```text
1. 玩家初始 HP 由房间配置决定。
2. 攻击请求由客户端发送。
3. 是否命中由服务端判定。
4. 伤害由服务端根据房间配置计算。
5. HP 小于等于 0 时进入 GhostRetreat 状态。
```

### 5.5 MR 濒死撤离规则

本项目不使用“死亡后虚拟传送回基地”的设计。

原因：

```text
这是 MR 项目，玩家真实身体位置需要和虚拟角色位置保持一致。
如果只把虚拟角色传送回基地，真实玩家仍在原地，会导致体验割裂。
```

正式规则：

```text
HP 归零
→ 进入 GhostRetreat 状态
→ 携带宝物掉落
→ 禁止攻击、拾取、提交和触发物资箱
→ 不能被攻击
→ 玩家真实走回己方复活区
→ 服务端检测进入复活区
→ 开始复活倒计时
→ 倒计时结束后恢复 HP
→ 状态变回 Alive
```

玩家状态：

```csharp
public enum PlayerState
{
    Alive,
    GhostRetreat,
    Respawning
}
```

### 5.6 胜负规则

MVP 阶段采用限时积分制：

```text
1. 每局有固定对局时间。
2. 时间由管理端配置。
3. 提交宝物获得队伍分数。
4. 时间结束后分数高的一队获胜。
5. 分数相同则平局。
```

---

## 6. MVP 范围

### 6.1 MVP 必须完成

```text
1. 一个 Unity 工程。
2. Boot、Home、Game、MapPreview 场景。
3. Unity 管理端可以创建多人房间。
4. 管理端可以选择地图。
5. 管理端可以配置 HP、武器伤害、复活倒计时、对局时间和宝物分值。
6. 至少两个客户端可以接入同一房间。
7. 设备不足时允许 Pico 4 + PC Editor 模拟客户端测试。
8. 管理端可以分配红队和蓝队。
9. 服务端可以加载地图并生成宝物。
10. 玩家可以拾取、携带、提交宝物。
11. 玩家可以攻击敌方玩家。
12. 服务端可以权威处理攻击、扣血、HP 归零、濒死撤离、复活和得分。
13. 时间结束后服务端结算红蓝队胜负。
14. 对局结果写入数据库。
15. 管理端显示结果。
```

### 6.2 MVP 暂不做

```text
1. Web 管理端。
2. 正式单人寻宝模式。
3. 地图编辑器 / 地图绘制软件。
4. 复杂账号注册登录。
5. 多种复杂武器。
6. 复杂背包。
7. 复杂弹孔和材质破坏。
8. AI 怪物。
9. 实时地图编辑。
10. 真实物理抢夺。
11. 跨公网联机。
12. 复杂排行榜筛选。
```

### 6.3 增强版功能

```text
1. 地图编辑器或地图绘制工具。
2. 多种武器。
3. 物资箱随机刷新武器和道具。
4. 更多地图模板。
5. 历史战绩查询。
6. 排行榜。
7. 更完整的观战 / 管理端状态面板。
8. 更好的 MR 视觉效果。
```

---

## 7. 服务器权威说明

### 7.1 核心原则

```text
客户端发送意图，服务器决定结果。
```

客户端只负责：

```text
1. 采集 Pico 4 头显和手柄输入。
2. 显示 MR 画面。
3. 播放动画和音效。
4. 显示 HUD 和提示 UI。
5. 发送移动、拾取、攻击、提交等请求。
```

服务器负责：

```text
1. 房间创建。
2. 房间状态。
3. 玩家连接。
4. 队伍分配。
5. 地图加载。
6. 宝物生成。
7. 宝物拾取判定。
8. 宝物提交判定。
9. 攻击命中判定。
10. HP 扣减。
11. GhostRetreat 状态切换。
12. 死亡掉宝。
13. 复活区检测。
14. 复活倒计时。
15. 分数计算。
16. 胜负结算。
17. 对局结果写入数据库。
```

### 7.2 客户端禁止直接决定的内容

```text
1. 自己拾取成功。
2. 自己提交得分。
3. 自己攻击命中。
4. 敌方玩家死亡。
5. 自己复活成功。
6. 队伍得分变化。
7. 对局胜负。
```

---

## 8. Unity 管理端范围

MVP 管理端功能：

```text
1. 显示玩家 / 设备列表。
2. 创建房间。
3. 显示房间列表。
4. 选择地图。
5. 配置对局时间。
6. 配置玩家最大 HP。
7. 配置武器伤害。
8. 配置复活倒计时。
9. 配置宝物分值。
10. 分配红蓝队。
11. 开始游戏。
12. 结束游戏。
13. 显示对局结果。
```

暂不做：

```text
1. 复杂权限系统。
2. Web 管理后台。
3. 在线编辑地图。
4. 复杂数据分析图表。
5. 复杂排行榜筛选。
```

---

## 9. 成员分工

```text
李潇涵：
项目负责人，负责项目架构、Unity 核心逻辑、多人夺宝规则、Pico 客户端基础接入、Mock 多人调试、集成验收。

崔国庆：
负责 Netick 服务端、服务器权威、房间同步、玩家连接、数据库、对局结果保存。

A：
负责 Unity 管理端 UI，包括房间创建、房间配置、地图选择、分队、开始和结束按钮。

B：
负责 Pico 客户端 HUD 和游戏内 UI，包括 HP、比分、倒计时、宝物提示、濒死撤离提示、结算面板。

C：
负责场景搭建、Prefab、地图组件、美术资源，包括宝物、武器、基地、复活区、物资箱和场景表现。
```

---

## 10. 推荐开发路线

```text
第 1 阶段：项目骨架
建立 Unity 工程、目录结构、Boot/Home/Game/MapPreview 场景、AppRole、RoomConfig、PlayerState 和 Mock 多人数据。

第 2 阶段：地图与配置
手动制作测试地图，定义地图 JSON 协议，完成 MapJsonModels、MapLoader、PrefabRegistry 和 MapValidator。

第 3 阶段：多人房间基础
完成管理端创建房间、玩家连接、红蓝分队、开始对局、房间状态同步。

第 4 阶段：夺宝闭环
完成地图加载、宝物生成、拾取、携带、提交、红蓝队计分和时间结算。

第 5 阶段：Netick 多人联机
完成 PC Server / Manager 与 Pico Client 的连接，使用 Editor 模拟第二客户端。

第 6 阶段：战斗和濒死撤离
完成攻击请求、服务端命中判定、扣血、进入 GhostRetreat、掉落宝物、复活区倒计时复活。

第 7 阶段：数据库
完成建表、保存房间配置、保存玩家、保存对局结果和管理端结果显示。

第 8 阶段：UI / 场景 / Prefab 美化
A、B、C 完成管理端 UI、Pico HUD、地图组件、宝物、武器、基地、复活区和展示效果。
```

---

## 11. 当前最小演示闭环

```text
Unity 管理端创建多人房间
→ 选择测试地图
→ 设置 HP、武器伤害、复活倒计时和对局时间
→ Pico 4 客户端加入房间
→ PC Editor 模拟另一个客户端加入房间
→ 管理端分配红队和蓝队
→ 服务端加载地图并生成宝物
→ 玩家拾取宝物
→ 玩家提交宝物得分
→ 玩家可攻击对手
→ HP 归零进入濒死撤离状态
→ 玩家走回复活区并倒计时复活
→ 时间结束服务器结算胜负
→ 对局结果写入数据库
→ 管理端显示结果
```

---

## 12. 开发阶段记录

### 阶段 2A：协议冻结与样例数据设计

完成时间：2026-06-03
负责人：AI / Codex（李潇涵审核）
本阶段目标：冻结地图 JSON、房间配置 JSON、数据库字段映射和样例数据，不实现 Unity C# 逻辑。

修改文件：
- docs/json_protocol.md（match_finished 增加 match_id 和 map_id）
- docs/database_design.md（room_config 增加 treasure_refresh_interval 和 supply_refresh_interval）
- docs/project_design.md（追加本阶段记录）
- map_samples/normalized/test_map_01.json（新建）
- config_samples/room_config_example.json（新建）
- config_samples/weapon_config_example.json（新建）
- config_samples/treasure_config_example.json（新建）
- database/schema.sql（新建）
- database/seed_data.sql（新建）

已完成功能：
- 地图 JSON 协议完整：team_bases（合并出生区/复活区/基地）/ treasure_spawn_points / supply_boxes / bounds / objects
- 房间配置 JSON 完整：game_mode / map_id / match_time / player_max_hp / respawn_countdown / weapon_config / treasure_scores
- 网络消息完整：join_room / switch_team / start_match / pickup_treasure / submit_treasure / attack / ghost_retreat / respawn_countdown_started / player_respawned / room_state_update / match_finished
- 错误码完整：覆盖房间、玩家、宝物、武器、复活等所有操作
- 数据库 5 张核心表：player / map_info / room_config / match_result / player_match_stat
- room_config 表字段覆盖 RoomConfig JSON 全部字段
- match_result + player_match_stat 覆盖 match_finished 消息全部字段
- 样例地图 test_map_01.json（对称小地图，红蓝各一个基地，4 个宝物点，1 个物资箱）
- 样例配置 3 份（room_config / weapon_config / treasure_config）
- schema.sql 和 seed_data.sql 可执行

未完成功能：
- MapLoader / MapJsonModels / PrefabRegistry / MapValidator（阶段 2B）
- 服务端加载地图 JSON 逻辑
- Unity 中实际生成 Cube 占位物体

验收结果：
- docs/json_protocol.md 字段完整且自洽 ✓
- map_samples/normalized/test_map_01.json 合法 JSON ✓
- config_samples 下 3 个 JSON 均合法 ✓
- 数据库字段能映射 RoomConfig 和 MatchResult ✓
- 没有新增正式单人模式 ✓
- 没有修改 Unity C# 代码 ✓

发现问题：
- 无阻塞性问题
- RoomState 枚举比任务要求多了 Countdown 状态，属于合理扩展，保留

下一阶段建议：
- 阶段 2B：MapJsonModels + MapLoader + MapValidator
- 在 Unity 中用 Cube/Capsule 加载 test_map_01.json 验证地图生成

是否修改协议字段：是（match_finished 增加 match_id 和 map_id）
是否影响数据库：是（room_config 增加 treasure_refresh_interval 和 supply_refresh_interval）
是否影响服务器权威：否
