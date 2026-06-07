# AGENTS.md

本文档用于约束 AI / Codex / 自动化代码助手在 TreasureArenaMR 项目中的开发行为。

所有 AI 辅助开发任务都必须先阅读：

```text
README.md
PROJECT_CONTEXT.md
AGENTS.md
docs/project_design.md
docs/json_protocol.md
docs/database_design.md
```

如果某些文档尚未创建，则先根据已有文件和当前任务范围执行，不要凭空补全大规模架构。

---

## 项目基本信息

项目名称：

```text
TreasureArenaMR
```

项目类型：

```text
基于 Pico 4 的 MR 多人联机双队夺宝竞技游戏
```

核心架构：

```text
一个 Unity 工程
多运行角色：Server / Manager / PicoClient / LocalTest
Unity 管理端
Netick 服务端
Pico 4 客户端
地图编辑器导出 JSON
数据库保存配置和结果
```

重要范围约束：

```text
本项目直接做多人双队夺宝模式，不再设计正式单人寻宝模式。
LocalTest 只用于开发调试多人流程，不作为正式玩法。
```

Unity 工程路径：

```text
TreasureArenaUnity/
```

主要项目代码路径：

```text
TreasureArenaUnity/Assets/_Project/
```

---

## 总体开发原则

1. 先保证多人可运行闭环，再做美化。
2. 先用 Cube、Capsule、简单 UI 跑通逻辑，不等待正式素材。
3. 不要一次性生成完整复杂工程。
4. 每次任务只改与当前目标相关的文件。
5. 不要擅自重构无关代码。
6. 不要擅自修改协议字段。
7. 不要破坏已有可运行流程。
8. 不要把未实现功能写成已完成。
9. 不确定的地方必须写 TODO 或说明待确认。
10. 服务端权威逻辑不能放到 Pico 客户端中。
11. 固定 UI 优先 Canvas / Prefab 可视化搭建，不要大量用代码生成 UI 层级。
12. 动态列表可以使用 Prefab 实例化，例如房间列表、玩家列表、结果条目。
13. 不要为正式单人寻宝模式创建独立玩法系统。
14. 不要过度编写防御性代码、兜底代码或多层备用分支。MVP 阶段优先写清晰、可验证的主流程。
15. 对协议、配置、场景引用等前置条件，优先在任务说明、命名规范、模板场景和验收标准中明确；代码中只保留必要的边界检查和明确错误提示。
16. 如果任务是边界清楚的简单代码工作，且不需要 Unity MCP、场景编辑或复杂上下文，优先输出可交给其他 AI 执行的 prompt；由其他 AI 完成后，再由 Codex 负责复核、验收和集成建议。

---

## 运行角色约定

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

负责：

```text
Netick Server
房间管理
玩家连接
队伍分配
服务器权威判定
对局结算
数据库写入
```

### Manager

负责：

```text
Unity 管理端 UI
创建多人房间
选择地图
配置规则
分队
开始和结束游戏
查看结果
```

### PicoClient

负责：

```text
Pico 4 输入
MR 显示
玩家交互
动画音效
HUD
向服务器发送请求
```

### LocalTest

负责：

```text
本地 Mock 多人测试
PC Editor 模拟客户端
无多台 Pico 时验证多人房间、分队、夺宝、攻击、濒死撤离和结算流程
```

LocalTest 不是正式单人玩法。

---

## 目录约定

项目根目录：

```text
TreasureArenaMR/
```

Unity 工程：

```text
TreasureArenaUnity/
```

核心代码：

```text
TreasureArenaUnity/Assets/_Project/Scripts/
```

场景：

```text
TreasureArenaUnity/Assets/_Project/Scenes/
```

预制体：

```text
TreasureArenaUnity/Assets/_Project/Prefabs/
```

运行时地图 JSON：

```text
TreasureArenaUnity/Assets/_Project/StreamingAssets/Maps/
```

运行时配置 JSON：

```text
TreasureArenaUnity/Assets/_Project/StreamingAssets/Configs/
```

数据库脚本：

```text
database/
```

文档：

```text
docs/
```

---

## 脚本目录职责

```text
Scripts/Core
启动、运行角色、全局常量。

Scripts/Shared
三端共用数据结构，例如 TeamType、PlayerState、RoomConfig、PlayerInfo、MatchResult。

Scripts/Map
地图 JSON 解析、地图加载、Prefab 映射、地图合法性检查。

Scripts/Gameplay
核心玩法逻辑，例如双队夺宝、HP、武器、宝物、濒死撤离、计分。

Scripts/Network
Netick 网络同步，例如网络玩家、网络宝物、房间状态、客户端输入转发。

Scripts/Server
服务端权威逻辑，例如房间管理、拾取判定、攻击判定、复活判定、结算。

Scripts/Client
Pico 客户端逻辑，例如 XR 初始化、输入采集、客户端表现、濒死撤离表现。

Scripts/Manager
Unity 管理端逻辑，例如房间 UI、配置 UI、分队 UI、开始结束控制。

Scripts/Database
数据库连接和仓储接口。

Scripts/UI
通用 UI，例如 HUD、结算面板、提示弹窗。

Scripts/Mock
Mock 多人测试数据和假服务器流程。
```

---

## 服务器权威规则

客户端只能发送请求，不能决定结果。

客户端可以发送：

```text
移动输入
攻击请求
拾取请求
提交请求
加入房间请求
切换队伍请求
```

服务器必须判定：

```text
是否允许加入房间
是否拾取成功
是否提交成功
攻击是否命中
扣多少 HP
是否进入 GhostRetreat
宝物是否掉落
是否进入复活区
是否复活成功
队伍得分
胜负结果
```

禁止在 Pico 客户端中直接写：

```text
hp -= damage
score += value
enemy.Die()
treasure.PickupSuccess()
player.RespawnSuccess()
```

这些结果必须由服务端确认后同步。

---

## MR 濒死撤离规则

本项目不使用死亡后传送回基地的方式。

正确规则：

```text
HP 归零
→ 进入 GhostRetreat 状态
→ 携带宝物掉落
→ 禁止攻击、拾取、提交、触发物资箱
→ 不能被攻击
→ 玩家真实走回己方复活区
→ 服务端检测进入复活区
→ 开始复活倒计时
→ 倒计时结束后恢复 HP
→ 状态变回 Alive
```

相关状态：

```csharp
public enum PlayerState
{
    Alive,
    GhostRetreat,
    Respawning
}
```

不要实现：

```text
死亡后把玩家模型直接传送到复活点
死亡后强制移动真实玩家
死亡后让幽灵状态还能拾取宝物或攻击
```

---

## 多人模式规则

正式游戏模式只有多人双队夺宝：

```text
红队和蓝队
宝物争夺
武器攻击
HP 扣减
GhostRetreat
复活区倒计时
队伍得分
胜负结算
```

设备不足时，测试方式是：

```text
一个 Pico 4 客户端 + 一个或多个 PC Editor 模拟客户端
```

不要新增正式 SoloTreasure 玩法、单人排行榜或独立单人流程。

---

## UI 开发规范

固定界面优先在 Unity Editor 中用 Canvas / Prefab 搭建。

推荐：

```text
Home 管理端界面
HUD
ResultPanel
MessagePopup
房间配置面板
玩家状态面板
```

代码职责：

```text
绑定引用
刷新文本
控制显隐
处理按钮事件
发送请求
接收状态后更新 UI
```

不要大量使用代码创建固定 UI 层级，例如：

```csharp
GameObject panel = new GameObject("Panel");
panel.AddComponent<Image>();
panel.AddComponent<Button>();
```

动态列表可以用代码实例化 Prefab，例如：

```text
RoomListItem
PlayerListItem
ResultPlayerItem
```

---

## Git 协作规则

推荐分支：

```text
main：稳定演示版本
develop：日常集成分支
feature/xxx：个人功能分支
backup/xxx：关键节点备份
```

规则：

```text
1. 队友从 develop 创建 feature 分支。
2. 每个人只改自己负责的模块。
3. 一次只合并一个 feature 到 develop。
4. 每次合并后必须打开 Unity 测试。
5. 测试通过后再 push develop。
6. 演示前再合并 develop 到 main。
```

Unity 协作注意：

```text
.meta 文件必须提交。
Library / Temp / Obj / Logs 不要提交。
同一时间不要多人修改同一个 .unity 场景。
能改 Prefab 就不要改 Scene。
能替换素材就不要改脚本。
```

---

## AI 任务输入模板

给 AI / Codex 派任务时，应使用以下格式：

```text
当前状态：
说明项目目前能运行到哪一步，相关文件有哪些。

本阶段目标：
说明这次只要完成什么，不要扩展。

允许修改：
列出允许修改的文件或目录。

禁止修改：
列出不能修改的文件、协议、数据结构或场景。

需要先检查的文件：
列出必须先阅读的文件。

具体任务：
逐条写清楚要实现什么。

测试要求：
说明如何验证，例如 Unity 能否编译、哪个场景能跑、控制台应输出什么。

完成后输出：
要求说明修改了哪些文件、每个文件改了什么、如何测试、是否有 TODO。
```

---

## AI 输出要求

每次修改完成后，必须输出：

```text
1. 修改了哪些文件。
2. 每个文件的作用。
3. 这次实现了什么。
4. 如何测试。
5. 是否通过编译。
6. 还有哪些 TODO。
7. 有没有修改协议字段。
8. 有没有可能影响其他模块。
```

如果不能确认编译通过，必须明确写：

```text
未实际运行 Unity 编译，需人工验证。
```

不能假装已经测试通过。

---

## 禁止事项

禁止：

```text
1. 不经确认修改 docs/json_protocol.md 中已确定字段。
2. 不经确认修改 RoomConfig、PlayerState 等核心数据结构。
3. 新增正式单人寻宝模式或独立 SoloTreasure 玩法分支。
4. 在 Pico 客户端中直接做权威判定。
5. 把数据库用于实时战斗状态同步。
6. 大量用代码生成固定 UI。
7. 在错误目录创建新 Unity 工程或新 Assets 目录。
8. 删除 .meta 文件。
9. 修改无关场景。
10. 同时重构多个模块。
11. 把未完成的功能写进 README 作为已完成。
```

---

## 当前成员职责

```text
李潇涵：
项目负责人、Unity 核心逻辑、多人夺宝规则、Pico 客户端基础接入、Mock 多人调试、集成验收。

崔国庆：
Netick 服务端、服务器权威、房间同步、玩家连接、数据库、对局结果保存。

A：
Unity 管理端 UI，房间创建、配置、地图选择、分队、开始结束按钮。

B：
Pico 客户端 HUD，HP、比分、倒计时、宝物提示、濒死撤离提示、结算面板。

C：
场景搭建、Prefab、地图组件、美术资源、宝物、武器、基地、复活区、物资箱。
```

---

## 当前最小目标

先做出一个没有正式素材但能运行的多人版本：

```text
PC 管理端创建多人房间
→ Pico 客户端加入
→ PC Editor 模拟另一个客户端加入
→ 地图 JSON 加载
→ 管理端分配红蓝队
→ 拾取宝物
→ 提交得分
→ 武器攻击
→ HP 归零进入 GhostRetreat
→ 玩家走回复活区
→ 倒计时复活
→ 对局结束
→ 结果写入数据库
```
