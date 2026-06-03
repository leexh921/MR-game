-- TreasureArenaMR MVP 数据库建表脚本
-- 目标：SQLite（MVP 阶段），后续可迁移至 MySQL
-- 设计原则：数据库只保存配置和结果，不参与实时战斗判定

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
    treasure_refresh_interval REAL NOT NULL DEFAULT 10.0,
    supply_refresh_interval REAL NOT NULL DEFAULT 20.0,
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
