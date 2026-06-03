# TreasureArenaMR 数据库设计文档

> 责任人：崔国庆，李潇涵审核  
> 文档作用：定义 MVP 阶段数据库表、字段、关系、写入时机和建表 SQL。  
> 设计原则：数据库只保存配置和结果，不参与实时战斗判定。

---

## 1. 数据库定位

数据库用于保存：

```text
1. 玩家信息
2. 地图信息
3. 房间配置
4. 对局结果
5. 玩家对局统计
```

数据库不用于保存：

```text
1. 玩家实时位置
2. 实时 HP
3. 每一帧状态
4. 每一次攻击的实时过程
5. 每一发子弹
6. 房间实时同步状态
```

实时状态由 Unity / Netick 服务端在内存中维护。  
数据库只在关键节点写入，例如创建房间、对局结束、保存结果。

---

## 2. MVP 表结构

MVP 阶段建议使用 5 张核心表：

```text
player
map_info
room_config
match_result
player_match_stat
```

可选增强表：

```text
weapon_config
treasure_config
```

---

## 3. 表关系概览

```text
player
  └── player_match_stat.player_id

map_info
  ├── room_config.map_id
  └── match_result.map_id

room_config
  └── match_result.room_id

match_result
  └── player_match_stat.match_id
```

---

## 4. player 表

### 4.1 作用

保存玩家基础信息。

### 4.2 字段

| 字段 | 类型 | 说明 |
|---|---|---|
| player_id | TEXT / VARCHAR | 玩家 ID，主键 |
| nickname | TEXT / VARCHAR | 玩家昵称 |
| created_at | DATETIME | 创建时间 |

### 4.3 示例

```text
player_id: p_001
nickname: Player01
created_at: 2026-06-02 20:00:00
```

---

## 5. map_info 表

### 5.1 作用

保存地图信息。MVP 阶段地图由项目组手动制作或手动编写 JSON，不开发地图编辑器。

### 5.2 字段

| 字段 | 类型 | 说明 |
|---|---|---|
| map_id | TEXT / VARCHAR | 地图 ID，主键 |
| map_name | TEXT / VARCHAR | 地图名称 |
| json_path | TEXT / VARCHAR | 地图 JSON 文件路径 |
| description | TEXT | 地图说明 |
| created_at | DATETIME | 创建时间 |

### 5.3 示例

```text
map_id: test_map_01
map_name: 测试地图 01
json_path: StreamingAssets/Maps/test_map_01.json
description: MVP 测试用小型地图
```

---

## 6. room_config 表

### 6.1 作用

保存房间创建时的配置。  
这张表记录的是“创建房间时的规则快照”，不负责实时同步房间状态。

### 6.2 字段

| 字段 | 类型 | 说明 |
|---|---|---|
| room_id | TEXT / VARCHAR | 房间 ID，主键 |
| room_name | TEXT / VARCHAR | 房间名称 |
| map_id | TEXT / VARCHAR | 地图 ID |
| game_mode | TEXT / VARCHAR | 游戏模式，MVP 固定 TeamTreasure |
| max_players | INTEGER | 最大玩家数 |
| match_time | REAL / FLOAT | 对局时间，秒 |
| round_count | INTEGER | 回合数，MVP 可固定 1 |
| player_max_hp | INTEGER | 玩家最大 HP |
| weapon_id | TEXT / VARCHAR | 当前房间武器 ID |
| weapon_damage | INTEGER | 武器伤害 |
| weapon_range | REAL / FLOAT | 武器射程 |
| weapon_cooldown | REAL / FLOAT | 武器冷却 |
| respawn_countdown | REAL / FLOAT | 复活倒计时 |
| normal_treasure_score | INTEGER | 普通宝物分值 |
| rare_treasure_score | INTEGER | 稀有宝物分值 |
| final_treasure_score | INTEGER | 最终宝物分值 |
| created_at | DATETIME | 创建时间 |

### 6.3 示例

```text
room_id: room_001
room_name: 测试房间
map_id: test_map_01
game_mode: TeamTreasure
match_time: 300
player_max_hp: 100
weapon_damage: 25
respawn_countdown: 5
normal_treasure_score: 10
rare_treasure_score: 30
final_treasure_score: 50
```

---

## 7. match_result 表

### 7.1 作用

保存一局对局的最终结果。

### 7.2 字段

| 字段 | 类型 | 说明 |
|---|---|---|
| match_id | TEXT / VARCHAR | 对局 ID，主键 |
| room_id | TEXT / VARCHAR | 房间 ID |
| map_id | TEXT / VARCHAR | 地图 ID |
| red_score | INTEGER | 红队得分 |
| blue_score | INTEGER | 蓝队得分 |
| winner_team | TEXT / VARCHAR | Red、Blue、Draw |
| duration | REAL / FLOAT | 实际对局时长 |
| started_at | DATETIME | 开始时间 |
| ended_at | DATETIME | 结束时间 |

### 7.3 示例

```text
match_id: match_001
room_id: room_001
map_id: test_map_01
red_score: 80
blue_score: 60
winner_team: Red
duration: 300
```

---

## 8. player_match_stat 表

### 8.1 作用

保存玩家在某一局对局中的统计数据。

### 8.2 字段

| 字段 | 类型 | 说明 |
|---|---|---|
| id | INTEGER | 自增主键 |
| match_id | TEXT / VARCHAR | 对局 ID |
| player_id | TEXT / VARCHAR | 玩家 ID |
| team | TEXT / VARCHAR | Red 或 Blue |
| kills | INTEGER | 击倒次数 |
| deaths | INTEGER | 进入濒死撤离次数 |
| treasures_submitted | INTEGER | 提交宝物数量 |
| score_contribution | INTEGER | 个人贡献分数 |

### 8.3 示例

```text
match_id: match_001
player_id: p_001
team: Red
kills: 2
deaths: 1
treasures_submitted: 3
score_contribution: 40
```

---

## 9. 可选表：weapon_config

MVP 可以不单独建表，直接把武器参数保存在 room_config 中。  
如果后续需要多种武器，可以增加此表。

| 字段 | 类型 | 说明 |
|---|---|---|
| weapon_id | TEXT / VARCHAR | 武器 ID，主键 |
| weapon_name | TEXT / VARCHAR | 武器名称 |
| damage | INTEGER | 伤害 |
| range_value | REAL / FLOAT | 射程 |
| cooldown | REAL / FLOAT | 冷却 |
| description | TEXT | 说明 |
| is_active | INTEGER / BOOLEAN | 是否启用 |

---

## 10. 可选表：treasure_config

MVP 可以不单独建表，直接把宝物分值保存在 room_config 中。  
如果后续需要多种宝物配置，可以增加此表。

| 字段 | 类型 | 说明 |
|---|---|---|
| treasure_type | TEXT / VARCHAR | 宝物类型，Normal、Rare、Final |
| score_value | INTEGER | 分值 |
| refresh_interval | REAL / FLOAT | 刷新间隔 |
| description | TEXT | 说明 |
| is_active | INTEGER / BOOLEAN | 是否启用 |

---

## 11. 写入时机

### 11.1 创建玩家

当客户端第一次连接并提交昵称时：

```text
写入或更新 player 表
```

### 11.2 导入地图

当项目添加地图 JSON 时：

```text
写入 map_info 表
```

MVP 阶段也可以先手动插入地图记录。

### 11.3 创建房间

管理端创建房间时：

```text
写入 room_config 表
```

### 11.4 对局结束

服务端完成胜负结算后：

```text
写入 match_result 表
写入 player_match_stat 表
```

---

## 12. SQLite 建表 SQL

MVP 阶段建议优先使用 SQLite，部署简单，适合课程项目。  
如果老师要求 MySQL，后续可以调整字段类型。

```sql
CREATE TABLE IF NOT EXISTS player (
    player_id TEXT PRIMARY KEY,
    nickname TEXT NOT NULL,
    created_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS map_info (
    map_id TEXT PRIMARY KEY,
    map_name TEXT NOT NULL,
    json_path TEXT NOT NULL,
    description TEXT,
    created_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS room_config (
    room_id TEXT PRIMARY KEY,
    room_name TEXT NOT NULL,
    map_id TEXT NOT NULL,
    game_mode TEXT NOT NULL,
    max_players INTEGER NOT NULL,
    match_time REAL NOT NULL,
    round_count INTEGER NOT NULL,
    player_max_hp INTEGER NOT NULL,
    weapon_id TEXT NOT NULL,
    weapon_damage INTEGER NOT NULL,
    weapon_range REAL NOT NULL,
    weapon_cooldown REAL NOT NULL,
    respawn_countdown REAL NOT NULL,
    normal_treasure_score INTEGER NOT NULL,
    rare_treasure_score INTEGER NOT NULL,
    final_treasure_score INTEGER NOT NULL,
    created_at TEXT NOT NULL,
    FOREIGN KEY (map_id) REFERENCES map_info(map_id)
);

CREATE TABLE IF NOT EXISTS match_result (
    match_id TEXT PRIMARY KEY,
    room_id TEXT NOT NULL,
    map_id TEXT NOT NULL,
    red_score INTEGER NOT NULL,
    blue_score INTEGER NOT NULL,
    winner_team TEXT NOT NULL,
    duration REAL NOT NULL,
    started_at TEXT NOT NULL,
    ended_at TEXT NOT NULL,
    FOREIGN KEY (room_id) REFERENCES room_config(room_id),
    FOREIGN KEY (map_id) REFERENCES map_info(map_id)
);

CREATE TABLE IF NOT EXISTS player_match_stat (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    match_id TEXT NOT NULL,
    player_id TEXT NOT NULL,
    team TEXT NOT NULL,
    kills INTEGER NOT NULL DEFAULT 0,
    deaths INTEGER NOT NULL DEFAULT 0,
    treasures_submitted INTEGER NOT NULL DEFAULT 0,
    score_contribution INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (match_id) REFERENCES match_result(match_id),
    FOREIGN KEY (player_id) REFERENCES player(player_id)
);
```

---

## 13. 初始测试数据

```sql
INSERT OR IGNORE INTO map_info (
    map_id,
    map_name,
    json_path,
    description,
    created_at
) VALUES (
    'test_map_01',
    '测试地图 01',
    'StreamingAssets/Maps/test_map_01.json',
    'MVP 测试用小型对称地图',
    datetime('now')
);

INSERT OR IGNORE INTO player (
    player_id,
    nickname,
    created_at
) VALUES
('p_test_red', 'RedTestPlayer', datetime('now')),
('p_test_blue', 'BlueTestPlayer', datetime('now'));
```

---

## 14. 数据库访问模块建议

Unity 中建议对应以下脚本：

```text
Scripts/Database/DatabaseManager.cs
负责数据库连接、初始化、关闭。

Scripts/Database/PlayerRepository.cs
负责 player 表读写。

Scripts/Database/RoomRepository.cs
负责 room_config 表读写。

Scripts/Database/MatchRepository.cs
负责 match_result 和 player_match_stat 表读写。
```

---

## 15. 注意事项

```text
1. 数据库不参与实时战斗。
2. 实时战斗状态由服务端内存维护。
3. 对局结束后再批量写入结果。
4. 房间配置应保存快照，避免后续管理端修改配置导致历史对局不一致。
5. player_match_stat 用于展示个人表现，不影响实时对局逻辑。
6. MVP 可以先使用 SQLite，减少部署成本。
```
