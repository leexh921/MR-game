# TreasureArenaMR JSON 协议文档

> 责任人：李潇涵 + 崔国庆  
> 文档作用：定义地图 JSON、房间配置 JSON、玩家、武器、宝物、网络请求字段和错误码。  
> 注意：地图 JSON 协议已冻结。地图编辑器作为必做 MVP 子项目，必须按本协议导出地图 JSON，不得新增/改名字段。如确需变更协议，必须先提交协议变更建议并等待确认。测试地图可由 MapEditor 导出、Unity 标记导出工具生成或手动编写。

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

地图 JSON 协议已冻结，可由以下方式生成：

```text
1. Pico 端 MR 地图编辑器（MapEditor）导出。
2. Unity Editor 中通过 MapExportMarker 标记后使用 MapSceneJsonExporter 导出。
3. 开发期手写 JSON。
```

地图 JSON 至少描述：

```text
1. 地图基本信息
2. 地图物体（objects：整图或逐 prefab）
3. 红蓝基地区（team_bases：合并出生区、复活区和提交区语义）
4. 宝物刷新点（treasure_spawn_points）
5. 物资箱点（supply_boxes），可选
6. 地图边界（map_boundary）：由 Draw Bounds / Draw Area 绘制出的地图级多边形边界
```

> 动画物体只保存 `prefab_id` 和 transform，动画状态、交互状态不写入地图 JSON。

### 3.2 MapJson 示例

```json
{
  "map_id": "test_map_01",
  "map_name": "测试地图 01",
  "map_version": "1.0.0",
  "description": "MVP 测试用小型对称地图",
  "editor_origin": {
    "position": { "x": 0.0, "y": 0.0, "z": 0.0 },
    "rotation": { "x": 0.0, "y": 0.0, "z": 0.0 }
  },
  "floor_calibration": {
    "is_calibrated": true,
    "floor_y": 0.0,
    "source": "Manual"
  },
  "objects": [
    {
      "object_id": "obj_wall_001",
      "prefab_id": "wall_01",
      "object_type": "StaticObstacle",
      "interaction_type": "None",
      "position": { "x": 0.0, "y": 0.0, "z": 2.0 },
      "rotation": { "x": 0.0, "y": 90.0, "z": 0.0 },
      "scale": { "x": 1.0, "y": 1.0, "z": 1.0 },
      "has_collider": true,
      "is_movable": false,
      "is_grabbable": false,
      "is_openable": false,
      "is_shootable": true,
      "blocks_bullet": true,
      "decal_enabled": true,
      "mass": 0.0
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
  "map_boundary": {
    "boundary_type": "Polygon",
    "height": 2.5,
    "points": [
      { "x": -6.0, "y": 0.0, "z": -4.0 },
      { "x": 6.0, "y": 0.0, "z": -4.0 },
      { "x": 6.0, "y": 0.0, "z": 4.0 },
      { "x": -6.0, "y": 0.0, "z": 4.0 }
    ]
  }
}
```

### 3.3 地图字段说明

| 字段 | 类型 | 必填 | 说明 |
|---|---|---|---|
| map_id | string | 是 | 地图唯一 ID |
| map_name | string | 是 | 地图显示名称 |
| map_version | string | 是 | 地图协议版本 |
| editor_origin | object | 是 | 编辑器坐标原点，供 MapRoot 对齐 |
| floor_calibration | object | 是 | 真实地面对齐信息 |
| objects | array | 否 | 普通地图物体 |
| team_bases | array | 是 | 基地区（同时作为出生区、复活区和提交区），至少红蓝各一个 |
| treasure_spawn_points | array | 是 | 宝物刷新点 |
| supply_boxes | array | 否 | 物资箱点 |
| map_boundary | object | 是 | 地图级边界数据，MVP 固定为 Polygon 多边形 |

### 3.4 map_boundary 字段说明

`map_boundary` 不再表示一个盒状 prefab，而是每张地图自己绘制出的边界数据。

| 字段 | 类型 | 必填 | 说明 |
|---|---|---|---|
| boundary_type | string | 是 | MVP 固定为 `"Polygon"` |
| height | float | 是 | 边界垂直高度，默认 2.5m，必须 > 0 |
| points | array | 是 | 多边形顶点，至少 3 个，按绘制顺序排列，最后一点不重复首点 |

注意：

```text
1. 旧 bounds.center / bounds.size 字段已废弃，不再兼容。
2. MapEditor 的 Draw Bounds / Draw Area 模式负责生成 map_boundary。
3. map_boundary 是地图级数据，不作为 objects 或玩法标记导出。
```

### 3.5 objects 字段说明

| 字段 | 类型 | 必填 | 说明 |
|---|---|---|---|
| object_id | string | 是 | 地图内对象唯一 ID |
| prefab_id | string | 是 | PrefabRegistry 中注册的 prefab ID |
| object_type | string | 是 | StaticFloor / StaticObstacle / PhysicsProp / OpenableObject |
| interaction_type | string | 是 | None / Grab / Openable |
| position | object | 是 | MapRoot 局部坐标 |
| rotation | object | 是 | Euler rotation |
| scale | object | 是 | localScale |
| has_collider | bool | 是 | 是否有 Collider |
| is_movable | bool | 是 | 是否可移动 |
| is_grabbable | bool | 是 | 是否可抓取 |
| is_openable | bool | 是 | 是否可打开 |
| is_shootable | bool | 是 | 是否可被子弹命中 |
| blocks_bullet | bool | 是 | 是否阻挡子弹 |
| decal_enabled | bool | 是 | 是否生成统一弹孔 |
| mass | float | 是 | 可移动物体质量，非移动物体可为 0 |

MVP 对象类型：

```text
StaticFloor：地面 / 放置表面 / 真实地面对齐参考。
StaticObstacle：固定障碍物，包括墙、柱、掩体、固定装饰。
PhysicsProp：可移动 / 可抓取物体。
OpenableObject：可打开物体。
```

弹孔规则：

```text
1. 不做材质分类。
2. 不区分 Concrete / Metal / Wood / Glass。
3. 统一使用 bullet_hole_default。
4. 对象只通过 is_shootable / blocks_bullet / decal_enabled 控制射击表现。
```

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

运行时网络层使用项目自研 TCP + UDP，不再依赖 Netick 做玩家生成、房间状态或位姿同步。

### 7.0 运行时传输 envelope

TCP 使用端口 `7777`，每条消息为 `4-byte big-endian length + UTF-8 JSON`。TCP 承载可靠命令、加入房间、地图、倒计时、HP、宝物、比分和权威状态快照。

UDP 使用端口 `7778`，一包一个 UTF-8 JSON。UDP 承载 `player_pose` 和 `pose_snapshot` 位姿同步。

所有运行时消息外层 envelope 固定为：

```json
{
  "protocol_version": 1,
  "type": "match_snapshot",
  "seq": 12,
  "room_id": "room_001",
  "player_id": "p_001",
  "sent_at_ms": 1781630000000,
  "payload_json": "{}"
}
```

字段说明：

| 字段 | 类型 | 必填 | 说明 |
|---|---|---|---|
| protocol_version | int | 是 | 当前固定为 `1`，不匹配必须拒绝连接或丢弃 UDP 包 |
| type | string | 是 | 消息类型，例如 `join_room_request`、`match_snapshot` |
| seq | int | 是 | 发送方递增序号 |
| room_id | string | 是 | 房间 ID |
| player_id | string | 是 | 发送方玩家 ID；服务端广播使用 `"server"` |
| sent_at_ms | long | 是 | UTC Unix milliseconds |
| payload_json | string | 是 | 具体 payload 的 JSON 字符串 |

> 当前 Unity 运行时代码使用 `JsonUtility`，因此 envelope 内部字段名为 `payload_json`，值是严格类型 payload 的 JSON 字符串；不做旧 Netick 消息兼容，也不做协议版本自动降级。

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

自研网络层实际广播类型为 `match_snapshot`，频率 10 Hz，承载同一组权威房间数据。下例为展开 payload 后的可读视图；实际传输时该对象会序列化为 envelope 的 `payload_json` 字符串：

```json
{
  "type": "match_snapshot",
  "payload_json": {
    "room_id": "room_001",
    "room_state": "Playing",
    "map_id": "test_map_01",
    "map_revision": 2,
    "remaining_time": 188.5,
    "red_score": 40,
    "blue_score": 20,
    "players": [
      {
        "player_id": "p_001",
        "nickname": "Player01",
        "team": "Red",
        "state": "Alive",
        "hp": 100,
        "max_hp": 100,
        "is_connected": true,
        "carried_treasure_id": "",
        "position": { "x": -1.0, "y": 0.0, "z": 2.0 },
        "rotation_y": 90.0
      }
    ],
    "treasures": [
      {
        "treasure_id": "treasure_point_001",
        "treasure_type": "Normal",
        "state": "Spawned",
        "score_value": 10,
        "position": { "x": 0.0, "y": 0.5, "z": 0.0 },
        "carrier_player_id": ""
      }
    ]
  }
}
```

### 7.11 玩家位姿同步

客户端 20 Hz 通过 UDP 发送。下例为展开 payload 后的可读视图：

```json
{
  "type": "player_pose",
  "payload_json": {
    "x": -1.0,
    "y": 1.4,
    "z": 2.0,
    "rotation_y": 90.0,
    "pose_seq": 321
  }
}
```

服务端 20 Hz 广播 `pose_snapshot`。下例为展开 payload 后的可读视图：

```json
{
  "type": "pose_snapshot",
  "payload_json": {
    "room_id": "room_001",
    "players": [
      {
        "player_id": "p_001",
        "position": { "x": -1.0, "y": 1.4, "z": 2.0 },
        "rotation_y": 90.0,
        "server_time_ms": 1781630000100,
        "stale": false
      }
    ]
  }
}
```

客户端显示远端玩家时只做固定 100ms snapshot interpolation；超过 1 秒没有新 pose 时标记 `stale=true`，不做预测、不做外推。

### 7.12 心跳和断开

心跳：

```json
{
  "type": "heartbeat",
  "payload_json": {}
}
```

断开通知：

```json
{
  "type": "disconnect_notice",
  "payload_json": {
    "player_id": "p_001",
    "reason": "tcp_disconnected"
  }
}
```

### 7.13 对局结束

服务端广播：

```json
{
  "type": "match_finished",
  "match_id": "match_001",
  "room_id": "room_001",
  "map_id": "test_map_01",
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
