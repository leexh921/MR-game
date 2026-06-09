-- TreasureArenaMR MVP 数据库建表脚本
-- 目标：MySQL
-- 设计原则：数据库只保存配置和结果，不参与实时战斗判定

CREATE TABLE IF NOT EXISTS player (
    player_id VARCHAR(64) PRIMARY KEY,
    nickname VARCHAR(128) NOT NULL,
    created_at DATETIME NOT NULL
);

CREATE TABLE IF NOT EXISTS map_info (
    map_id VARCHAR(64) PRIMARY KEY,
    map_name VARCHAR(128) NOT NULL,
    json_path VARCHAR(512) NOT NULL,
    description VARCHAR(512),
    created_at DATETIME NOT NULL
);

CREATE TABLE IF NOT EXISTS room_config (
    room_id VARCHAR(64) PRIMARY KEY,
    room_name VARCHAR(128) NOT NULL,
    map_id VARCHAR(64) NOT NULL,
    game_mode VARCHAR(64) NOT NULL,
    max_players INTEGER NOT NULL,
    match_time FLOAT NOT NULL,
    round_count INTEGER NOT NULL,
    player_max_hp INTEGER NOT NULL,
    weapon_id VARCHAR(64) NOT NULL,
    weapon_damage INTEGER NOT NULL,
    weapon_range FLOAT NOT NULL,
    weapon_cooldown FLOAT NOT NULL,
    respawn_countdown FLOAT NOT NULL,
    normal_treasure_score INTEGER NOT NULL,
    rare_treasure_score INTEGER NOT NULL,
    final_treasure_score INTEGER NOT NULL,
    treasure_refresh_interval FLOAT NOT NULL DEFAULT 10.0,
    supply_refresh_interval FLOAT NOT NULL DEFAULT 20.0,
    created_at DATETIME NOT NULL,
    FOREIGN KEY (map_id) REFERENCES map_info(map_id)
);

CREATE TABLE IF NOT EXISTS match_result (
    match_id VARCHAR(64) PRIMARY KEY,
    room_id VARCHAR(64) NOT NULL,
    map_id VARCHAR(64) NOT NULL,
    red_score INTEGER NOT NULL,
    blue_score INTEGER NOT NULL,
    winner_team VARCHAR(16) NOT NULL,
    duration FLOAT NOT NULL,
    started_at DATETIME NOT NULL,
    ended_at DATETIME NOT NULL,
    FOREIGN KEY (room_id) REFERENCES room_config(room_id),
    FOREIGN KEY (map_id) REFERENCES map_info(map_id)
);

CREATE TABLE IF NOT EXISTS player_match_stat (
    id INTEGER PRIMARY KEY AUTO_INCREMENT,
    match_id VARCHAR(64) NOT NULL,
    player_id VARCHAR(64) NOT NULL,
    team VARCHAR(16) NOT NULL,
    kills INTEGER NOT NULL DEFAULT 0,
    deaths INTEGER NOT NULL DEFAULT 0,
    treasures_submitted INTEGER NOT NULL DEFAULT 0,
    score_contribution INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (match_id) REFERENCES match_result(match_id),
    FOREIGN KEY (player_id) REFERENCES player(player_id)
);
