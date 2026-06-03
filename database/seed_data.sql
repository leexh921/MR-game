-- TreasureArenaMR MVP 初始测试数据
-- 用于本地开发和多人流程验证

-- 测试地图
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

-- 测试玩家
INSERT OR IGNORE INTO player (
    player_id,
    nickname,
    created_at
) VALUES
    ('p_test_red', 'RedTestPlayer', datetime('now')),
    ('p_test_blue', 'BlueTestPlayer', datetime('now'));

-- 测试房间配置
INSERT OR IGNORE INTO room_config (
    room_id,
    room_name,
    map_id,
    game_mode,
    max_players,
    match_time,
    round_count,
    player_max_hp,
    weapon_id,
    weapon_damage,
    weapon_range,
    weapon_cooldown,
    respawn_countdown,
    normal_treasure_score,
    rare_treasure_score,
    final_treasure_score,
    treasure_refresh_interval,
    supply_refresh_interval,
    created_at
) VALUES (
    'room_001',
    '测试房间 001',
    'test_map_01',
    'TeamTreasure',
    4,
    300.0,
    1,
    100,
    'energy_gun',
    25,
    15.0,
    0.5,
    5.0,
    10,
    30,
    50,
    10.0,
    20.0,
    datetime('now')
);

-- 测试对局结果
INSERT OR IGNORE INTO match_result (
    match_id,
    room_id,
    map_id,
    red_score,
    blue_score,
    winner_team,
    duration,
    started_at,
    ended_at
) VALUES (
    'match_001',
    'room_001',
    'test_map_01',
    80,
    60,
    'Red',
    300.0,
    datetime('now', '-5 minutes'),
    datetime('now')
);

-- 测试玩家对局统计
INSERT OR IGNORE INTO player_match_stat (
    match_id,
    player_id,
    team,
    kills,
    deaths,
    treasures_submitted,
    score_contribution
) VALUES
    ('match_001', 'p_test_red', 'Red', 2, 1, 3, 40),
    ('match_001', 'p_test_blue', 'Blue', 1, 2, 2, 30);
