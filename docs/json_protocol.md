# TreasureArenaMR JSON 协议文档

> 责任人：李潇涵 + 崔国庆  
> 文档作用：定义地图 JSON、房间配置 JSON、玩家、武器、宝物、网络请求字段和错误码。  
> 注意：MVP 阶段不开发地图编辑器，地图 JSON 协议由项目组自行确定，测试地图可手动编写或由简单工具生成。

---

## 1. 通用约定

### 1.1 命名规则

```text
JSON 字段使用 snake_case。
C# 字段可以使用 camelCase 或 PascalCase，但需要在解析层明确映射。
枚举值使用字符串，例如 "Red"、"Blue"、"Alive"。
```

### 1.2 坐标规则

MVP 阶段统一使用 Unity 世界坐标：

```text
x：Unity 世界坐标 X
y：Unity 世界坐标 Y
z：Unity 世界坐标 Z
rotation_y：绕 Y 轴旋转角度，单位为 degree
```

除非特殊说明，距离单位统一为 Unity unit。

### 1.3 时间规则

```text
match_time：秒
respawn_countdown：秒
weapon_cooldown：秒
treasure_refresh_interval：秒
```

### 1.4 ID 规则

```text
map_id：地图 ID，例如 "test_map_01"
room_id：房间 ID，例如 "room_001"
player_id：玩家 ID，例如 "p_001"
treasure_id：宝物 ID，例如 "t_001"
weapon_id：武器 ID，例如 "energy_gun"
prefab_id：预制体 ID，例如 "wall_01"
```

---

## 2. 枚举定义

### 2.1 TeamType

```json
["None", "Red", "Blue"]
```

### 2.2 PlayerState

```json
["Alive", "GhostRetreat", "Respawning"]
```

### 2.3 TreasureType

```json
["Normal", "Rare", "Final"]
```

### 2.4 TreasureState

```json
["Spawned", "Carried", "Dropped", "Submitted"]
```

### 2.5 RoomState

```json
["Waiting", "Ready", "Countdown", "Playing", "Finished"]
```

### 2.6 GameMode

MVP 阶段正式模式只有：

```json
["TeamTreasure"]
```

LocalTest 只作为开发调试角色，不作为正式玩法模式。

---

## 3. 地图 JSON 协议

### 3.1 设计说明

MVP 阶段不开发地图编辑器，地图 JSON 由项目组自行定义。  
测试地图可以通过手写 JSON、Unity 中手动摆放后导出、或后续简单编辑工具生成。

地图 JSON 至少要描述：

```text
1. 地图基本信息
2. 地图物体
3. 红蓝出生区
4. 红蓝复活区
5. 红蓝基地区
6. 宝物刷新点
7. 物资箱点，可选
8. 地图边界，可选
```

### 3.2 MapJson 示例

```json
{
  "map_id": "test_map_01",
  "map_name": "测试地图 01",
  "version": "1.0.0",
  "description": "MVP 测试用小型对称地图",
  "objects": [
    {
      "object_id": "obj_wall_001",
      "prefab_id": "wall_01",
      "position": { "x": 0.0, "y": 0.0, "z": 2.0 },
      "rotation": { "x": 0.0, "y": 90.0, "z": 0.0 },
      "scale": { "x": 1.0, "y": 1.0, "z": 1.0 },
      "has_collider": true
    }
  ],
  "spawn_zones": [
    {
      "zone_id": "red_spawn",
      "team": "Red",
      "position": { "x": -4.0, "y": 0.0, "z": 0.0 },
      "radius": 1.0
    },
    {
      "zone_id": "blue_spawn",
      "team": "Blue",
      "position": { "x": 4.0, "y": 0.0, "z": 0.0 },
      "radius": 1.0
    }
  ],
  "respawn_zones": [
    {
      "zone_id": "red_respawn",
      "team": "Red",
      "position": { "x": -4.5, "y": 0.0, "z": 0.0 },
      "radius": 1.2
    },
    {
      "zone_id": "blue_respawn",
      "team": "Blue",
      "position": { "x": 4.5, "y": 0.0, "z": 0.0 },
      "radius": 1.2
    }
  ],
  "team_bases": [
    {
      "base_id": "red_base",
      "team": "Red",
      "position": { "x": -5.0, "y": 0.0, "z": 0.0 },
      "radius": 1.2
    },
    {
      "base_id": "blue_base",
      "team": "Blue",
      "position": { "x": 5.0, "y": 0.0, "z": 0.0 },
      "radius": 1.2
    }
  ],
  "treasure_spawn_points": [
    {
      "point_id": "treasure_point_001",
      "treasure_type": "Normal",
      "position": { "x": 0.0, "y": 0.5, "z": 0.0 },
      "radius": 0.5
    },
    {
      "point_id": "treasure_point_002",
      "treasure_type": "Rare",
      "position": { "x": 0.0, "y": 0.5, "z": 3.0 },
      "radius": 0.5
    }
  ],
  "supply_boxes": [
    {
      "box_id": "supply_001",
      "position": { "x": 0.0, "y": 0.0, "z": -3.0 },
      "supply_type": "WeaponRandom",
      "refresh_interval": 20.0
    }
  ],
  "bounds": {
    "center": { "x": 0.0, "y": 0.0, "z": 0.0 },
    "size": { "x": 12.0, "y": 3.0, "z": 8.0 }
  }
}
```

### 3.3 地图字段说明

| 字段 | 类型 | 必填 | 说明 |
|---|---|---|---|
| map_id | string | 是 | 地图唯一 ID |
| map_name | string | 是 | 地图显示名称 |
| version | string | 是 | 地图协议版本 |
| objects | array | 否 | 普通地图物体 |
| spawn_zones | array | 是 | 出生区，至少红蓝各一个 |
| respawn_zones | array | 是 | 复活区，至少红蓝各一个 |
| team_bases | array | 是 | 提交宝物的基地，至少红蓝各一个 |
| treasure_spawn_points | array | 是 | 宝物刷新点 |
| supply_boxes | array | 否 | 物资箱点 |
| bounds | object | 否 | 地图边界 |

---

## 4. 房间配置 JSON

### 4.1 RoomConfig 示例

```json
{
  "room_id": "room_001",
  "room_name": "测试房间 001",
  "game_mode": "TeamTreasure",
  "map_id": "test_map_01",
  "max_players": 4,
  "match_time": 300.0,
  "round_count": 1,
  "player_max_hp": 100,
  "respawn_countdown": 5.0,
  "weapon_config": {
    "weapon_id": "energy_gun",
    "damage": 25,
    "range": 15.0,
    "cooldown": 0.5
  },
  "treasure_scores": {
    "Normal": 10,
    "Rare": 30,
    "Final": 50
  },
  "treasure_refresh_interval": 10.0,
  "supply_refresh_interval": 20.0
}
```

### 4.2 RoomConfig 字段说明

| 字段 | 类型 | 说明 |
|---|---|---|
| room_id | string | 房间 ID |
| room_name | string | 房间名称 |
| game_mode | string | MVP 固定为 TeamTreasure |
| map_id | string | 使用的地图 ID |
| max_players | int | 最大玩家数 |
| match_time | float | 对局时长，秒 |
| round_count | int | 回合数，MVP 可固定 1 |
| player_max_hp | int | 玩家最大 HP |
| respawn_countdown | float | 进入复活区后的复活倒计时 |
| weapon_config | object | 当前房间使用的武器参数 |
| treasure_scores | object | 各类宝物分值 |
| treasure_refresh_interval | float | 宝物刷新间隔 |
| supply_refresh_interval | float | 物资刷新间隔 |

---

## 5. 玩家数据

### 5.1 PlayerInfo 示例

```json
{
  "player_id": "p_001",
  "nickname": "Player01",
  "team": "Red",
  "state": "Alive",
  "hp": 100,
  "max_hp": 100,
  "is_connected": true,
  "carried_treasure_id": null
}
```

### 5.2 PlayerRuntimeState 示例

```json
{
  "player_id": "p_001",
  "team": "Red",
  "state": "Alive",
  "hp": 75,
  "position": { "x": -1.0, "y": 0.0, "z": 2.0 },
  "rotation_y": 90.0,
  "carried_treasure_id": "t_001"
}
```

---

## 6. 宝物数据

### 6.1 TreasureRuntimeState 示例

```json
{
  "treasure_id": "t_001",
  "treasure_type": "Normal",
  "state": "Carried",
  "score_value": 10,
  "position": { "x": 0.0, "y": 0.5, "z": 1.0 },
  "carrier_player_id": "p_001"
}
```

### 6.2 宝物状态说明

```text
Spawned：宝物在刷新点或场景中，等待拾取。
Carried：宝物被玩家携带。
Dropped：宝物因玩家进入 GhostRetreat 而掉落。
Submitted：宝物已提交并计分。
```

---

## 7. 网络请求和响应

即使使用 Netick，也需要统一关键操作请求字段，便于管理端、客户端和服务端对齐。

### 7.1 加入房间请求

客户端发送：

```json
{
  "type": "join_room_request",
  "room_id": "room_001",
  "nickname": "Player01",
  "client_type": "PicoClient"
}
```

服务端返回：

```json
{
  "type": "join_room_result",
  "ok": true,
  "player_id": "p_001",
  "room_id": "room_001",
  "team": "Red"
}
```

### 7.2 切换队伍请求

管理端或客户端发送：

```json
{
  "type": "switch_team_request",
  "room_id": "room_001",
  "player_id": "p_001",
  "target_team": "Blue"
}
```

服务端返回：

```json
{
  "type": "switch_team_result",
  "ok": true,
  "player_id": "p_001",
  "team": "Blue"
}
```

### 7.3 开始游戏请求

管理端发送：

```json
{
  "type": "start_match_request",
  "room_id": "room_001"
}
```

服务端广播：

```json
{
  "type": "match_started",
  "room_id": "room_001",
  "map_id": "test_map_01",
  "room_config": {
    "player_max_hp": 100,
    "weapon_damage": 25,
    "respawn_countdown": 5.0,
    "match_time": 300.0
  }
}
```

### 7.4 拾取宝物请求

客户端发送：

```json
{
  "type": "pickup_treasure_request",
  "room_id": "room_001",
  "player_id": "p_001",
  "treasure_id": "t_001"
}
```

服务端返回或广播：

```json
{
  "type": "pickup_treasure_result",
  "ok": true,
  "player_id": "p_001",
  "treasure_id": "t_001"
}
```

### 7.5 提交宝物请求

客户端发送：

```json
{
  "type": "submit_treasure_request",
  "room_id": "room_001",
  "player_id": "p_001"
}
```

服务端广播：

```json
{
  "type": "submit_treasure_result",
  "ok": true,
  "player_id": "p_001",
  "team": "Red",
  "treasure_id": "t_001",
  "score_added": 10,
  "red_score": 40,
  "blue_score": 20
}
```

### 7.6 攻击请求

客户端发送：

```json
{
  "type": "attack_request",
  "room_id": "room_001",
  "attacker_player_id": "p_001",
  "weapon_id": "energy_gun",
  "origin": { "x": -1.0, "y": 1.4, "z": 0.0 },
  "direction": { "x": 1.0, "y": 0.0, "z": 0.0 },
  "client_time": 12.35
}
```

服务端广播命中结果：

```json
{
  "type": "attack_result",
  "ok": true,
  "attacker_player_id": "p_001",
  "target_player_id": "p_002",
  "damage": 25,
  "target_hp_after": 75
}
```

未命中：

```json
{
  "type": "attack_result",
  "ok": true,
  "attacker_player_id": "p_001",
  "target_player_id": null,
  "damage": 0
}
```

### 7.7 进入濒死撤离

服务端广播：

```json
{
  "type": "player_ghost_retreat",
  "player_id": "p_002",
  "dropped_treasure_id": "t_003",
  "position": { "x": 1.2, "y": 0.0, "z": 2.5 }
}
```

### 7.8 进入复活区

客户端可发送当前位置，服务端判断是否进入复活区。  
如果服务端判定成功，广播：

```json
{
  "type": "respawn_countdown_started",
  "player_id": "p_002",
  "respawn_countdown": 5.0
}
```

### 7.9 复活完成

服务端广播：

```json
{
  "type": "player_respawned",
  "player_id": "p_002",
  "hp": 100,
  "state": "Alive"
}
```

### 7.10 房间状态同步

服务端定期同步：

```json
{
  "type": "room_state_update",
  "room_id": "room_001",
  "room_state": "Playing",
  "remaining_time": 188.5,
  "red_score": 40,
  "blue_score": 20,
  "players": [
    {
      "player_id": "p_001",
      "team": "Red",
      "state": "Alive",
      "hp": 100,
      "position": { "x": -1.0, "y": 0.0, "z": 2.0 },
      "carried_treasure_id": "t_001"
    }
  ],
  "treasures": [
    {
      "treasure_id": "t_002",
      "treasure_type": "Rare",
      "state": "Spawned",
      "position": { "x": 0.0, "y": 0.5, "z": 3.0 },
      "carrier_player_id": null
    }
  ]
}
```

### 7.11 对局结束

服务端广播：

```json
{
  "type": "match_finished",
  "room_id": "room_001",
  "red_score": 80,
  "blue_score": 60,
  "winner_team": "Red",
  "duration": 300.0,
  "player_stats": [
    {
      "player_id": "p_001",
      "team": "Red",
      "kills": 2,
      "deaths": 1,
      "treasures_submitted": 3,
      "score_contribution": 40
    }
  ]
}
```

---

## 8. 错误码

| 错误码 | 说明 |
|---|---|
| room_not_found | 房间不存在 |
| room_full | 房间已满 |
| match_already_started | 对局已经开始 |
| invalid_team | 队伍无效 |
| player_not_found | 玩家不存在 |
| player_not_alive | 玩家不是 Alive 状态 |
| player_in_ghost_retreat | 玩家处于濒死撤离状态 |
| treasure_not_found | 宝物不存在 |
| treasure_already_carried | 宝物已被携带 |
| already_carrying_treasure | 玩家已经携带宝物 |
| not_in_pickup_range | 不在拾取范围内 |
| not_carrying_treasure | 玩家没有携带宝物 |
| not_in_team_base | 不在己方基地范围内 |
| weapon_on_cooldown | 武器冷却中 |
| target_not_found | 目标不存在 |
| target_not_alive | 目标不是 Alive 状态 |
| not_in_respawn_zone | 不在己方复活区 |
| invalid_room_state | 当前房间状态不允许该操作 |

错误返回示例：

```json
{
  "type": "pickup_treasure_result",
  "ok": false,
  "error_code": "not_in_pickup_range",
  "message": "玩家不在宝物拾取范围内"
}
```

---

## 9. 数据库字段映射建议

| JSON 字段 | 数据库字段 |
|---|---|
| player_id | player.player_id |
| nickname | player.nickname |
| map_id | map_info.map_id |
| map_name | map_info.map_name |
| room_id | room_config.room_id |
| room_name | room_config.room_name |
| player_max_hp | room_config.player_max_hp |
| weapon_config.damage | room_config.weapon_damage |
| respawn_countdown | room_config.respawn_countdown |
| red_score | match_result.red_score |
| blue_score | match_result.blue_score |
| winner_team | match_result.winner_team |
| kills | player_match_stat.kills |
| deaths | player_match_stat.deaths |
| treasures_submitted | player_match_stat.treasures_submitted |
| score_contribution | player_match_stat.score_contribution |
