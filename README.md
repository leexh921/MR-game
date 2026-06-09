# TreasureArenaMR

## 项目简介

TreasureArenaMR 是一个基于 Pico 4 的 MR 多人联机双队夺宝竞技项目。

玩家分为红队和蓝队，在由地图编辑器生成的 MR 场景中寻找、争夺、携带并提交宝物获得分数。玩家可以使用武器攻击对手，被击败的玩家不会被虚拟传送，而是进入更适合 MR 的“濒死撤离状态”：玩家需要在真实空间中走回己方复活区，并在复活倒计时结束后重新加入对局。

项目采用 Unity 一个工程、多运行角色的方式开发：

- Unity 管理端：创建房间、选择地图、配置规则、分配队伍、控制开始和结束。
- Unity / Netick 服务端：运行服务器权威逻辑，处理房间、地图、宝物、攻击、HP、濒死撤离、得分和结算。
- Pico 4 客户端：负责 MR 画面显示、玩家输入、交互表现和 UI 展示。
- 地图编辑端：通过头盔上的地图编辑器摆放预制体并导出 JSON 地图文件。
- 数据库：保存玩家、地图、房间配置、对局结果和玩家统计。

本项目当前目标不是先做完整美术成品，而是先做出一个无完整素材资产但可运行的多人核心闭环版本。

---

## 当前玩法方向

### 核心玩法

```text
Unity 管理端创建多人房间
→ 玩家通过 Pico 4 客户端接入
→ 管理端将玩家分配到红队和蓝队
→ 管理端选择地图并配置规则
→ 服务端加载地图并生成宝物
→ 红蓝两队争夺宝物
→ 玩家拾取宝物并携带回己方基地
→ 提交成功后队伍得分
→ 玩家可使用武器攻击敌方玩家
→ HP 归零后进入濒死撤离状态
→ 携带宝物会掉落
→ 玩家走回己方复活区
→ 倒计时结束后复活
→ 时间结束后分数高的一方获胜
```

---

## 关键设计原则

1. 本项目直接做多人双队夺宝模式，不再设计单人寻宝模式。
2. Pico 4 只作为客户端，不承担服务器权威逻辑。
3. 客户端只发送操作意图，例如移动、拾取、攻击、提交。
4. 服务端负责关键判定，例如拾取是否成功、攻击是否命中、HP 是否归零、是否进入复活区、是否得分。
5. 管理端使用 Unity 实现，不做 Web 管理端。
6. 使用一个 Unity 工程，通过运行角色区分 Server、Manager、PicoClient 和 LocalTest。
7. LocalTest 只用于本地调试多人流程，例如用 Editor 模拟客户端，不作为正式单人玩法。
8. HP、武器伤害、复活倒计时、宝物分值、对局时间等参数应由管理端配置。
9. 不使用死亡后传送复活点的方式，而使用 MR 友好的濒死撤离机制。
10. 数据库只保存配置和结果，不参与实时战斗判定。
11. 先用 Cube、Capsule 和简单 UI 跑通多人流程，再替换正式素材。

---

## 项目目录结构

```text
TreasureArenaMR/
│
├── README.md
├── PROJECT_CONTEXT.md
├── AGENTS.md
├── .gitignore
│
├── docs/
│   ├── project_design.md
│   ├── json_protocol.md
│   ├── database_design.md
│   ├── map_editor_design.md
│   └── map_editor_ui_design.md
│
├── TreasureArenaUnity/
│   ├── Assets/
│   ├── Packages/
│   └── ProjectSettings/
│
├── database/
│   ├── schema.sql
│   ├── seed_data.sql
│   └── README_DATABASE.md
│
├── map_samples/
│   ├── raw_from_editor/
│   ├── normalized/
│   └── README_MAPS.md
│
├── config_samples/
│   ├── room_config_example.json
│   ├── weapon_config_example.json
│   └── treasure_config_example.json
│
├── builds/
│   ├── manager_server_windows/
│   ├── pico_client_android/
│   └── README_BUILD.md
│
└── meeting_notes/
    ├── teacher_meeting_01.md
    └── project_review.md
```

---

## Unity 工程目录结构

```text
TreasureArenaUnity/
│
├── Assets/
│   └── _Project/
│       ├── Scenes/
│       ├── Scripts/
│       ├── Prefabs/
│       ├── ScriptableObjects/
│       ├── Materials/
│       ├── Models/
│       ├── Textures/
│       ├── Audio/
│       └── StreamingAssets/
│
├── Packages/
└── ProjectSettings/
```

### 主要场景

```text
Boot.unity
启动场景。判断当前运行角色，进入 Server、Manager、PicoClient 或 LocalTest 流程。

Home.unity
Unity 管理端主场景。用于设备列表、房间列表、创建房间、配置规则、分队和控制开始。

Game.unity
实际游戏场景。用于加载地图、生成玩家、宝物、基地、复活区并运行多人对局。

MapPreview.unity
地图预览场景。用于测试地图编辑器导出的 JSON 是否能正确生成场景。

MapEditor.unity
地图编辑器场景。用于在 Unity Editor 或 Pico MR 环境中摆放 prefab、玩法标记和 bounds，并导出地图 JSON。
```

---

## 运行角色

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

运行在 PC 上，负责 Unity 管理端界面。MVP 阶段可以与 Server 合并在同一个 Windows 程序中运行。

### PicoClient

运行在 Pico 4 上，负责 MR 显示、输入、交互和 UI。

### LocalTest

本地测试角色，用于没有多台 Pico 4 时辅助调试多人流程，例如：

```text
PC Editor 模拟一个或多个客户端
Mock 多个玩家连接
调试管理端创建房间、分队、开始对局
调试拾取、提交、攻击、濒死撤离和结算
```

LocalTest 不是正式单人玩法。

---

## 成员分工

```text
李潇涵：
项目负责人，负责项目架构、Unity 核心逻辑、多人夺宝规则、Pico 客户端基础接入、Mock 多人调试、集成验收。

崔国庆：
负责 Netick 服务端、服务器权威、房间同步、玩家连接、数据库、对局结果保存。

A：
负责 Unity 管理端 UI，包括房间创建、配置面板、地图选择、分队、开始和结束按钮。

B：
负责 Pico 客户端 HUD 和游戏内 UI，包括 HP、比分、倒计时、宝物提示、濒死撤离提示和结算面板。

C：
负责场景搭建、Prefab、地图组件、美术资源，包括宝物、武器、基地、复活区、物资箱和场景表现。
```

---

## MVP 目标

最小可运行版本应完成：

```text
1. Unity 管理端可以创建多人房间。
2. 管理端可以选择地图 JSON。
3. 管理端可以配置 HP、武器伤害、复活倒计时、对局时间和宝物分值。
4. 至少两个玩家客户端可以接入房间；设备不足时允许用 Pico 4 + PC Editor 模拟客户端测试。
5. 管理端可以将玩家分配到红队和蓝队。
6. 服务端可以加载地图并生成宝物。
7. 玩家可以拾取宝物。
8. 玩家可以提交宝物并为队伍得分。
9. 玩家可以攻击敌方玩家。
10. 服务端可以权威处理攻击、扣血、HP 归零、宝物掉落、濒死撤离和复活。
11. HP 归零后玩家进入 GhostRetreat 状态，宝物掉落。
12. 玩家走回己方复活区后开始复活倒计时。
13. 时间结束后服务端结算红蓝队胜负。
14. 对局结束后写入数据库。
15. 管理端显示本局结果。
16. MapEditor 可以摆放地图物件和玩法标记。
17. MapEditor 可以导出符合协议的地图 JSON，并通过 MapValidator 校验。
```

---

## 暂不做内容

MVP 阶段暂不做：

```text
1. Web 管理端。
2. 正式单人寻宝模式。
3. 复杂账号注册登录。
4. 大量武器种类。
5. 复杂背包系统。
6. 复杂弹孔和材质破坏系统。
7. AI 怪物。
8. 对局运行中的实时地图修改。
9. 真实物理抢夺。
10. 跨公网联机。
11. 复杂排行榜筛选。
```

---

## 开发顺序建议

```text
第 1 阶段：项目骨架
建立 Unity 工程、目录结构、Boot/Home/Game/MapPreview 场景、AppRole、RoomConfig、PlayerState 和 Mock 多人数据。

第 2 阶段：地图 JSON
从地图编辑器导出样例 JSON，完成 MapJsonModels、MapLoader、PrefabRegistry 和 MapValidator。

地图编辑器 MVP 与多人闭环并行推进：
MapEditor 先完成 prefab/玩法标记摆放、四周停靠式基础 UI、JSON 导出和 MapValidator 校验。

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
```

---

## 当前最小演示闭环

```text
Unity 管理端创建多人房间
→ 选择地图 JSON
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
