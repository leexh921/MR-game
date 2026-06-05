using System.Data;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var connStr = builder.Configuration.GetConnectionString("treasure_arena")
    ?? throw new Exception("Connection string 'treasure_arena' not found");

// ---- startup: ensure DB + tables exist ----

using (var conn = new MySqlConnection(connStr.Replace("Database=treasure_arena", "")))
{
    await conn.OpenAsync();
    using var cmd = new MySqlCommand(
        "CREATE DATABASE IF NOT EXISTS treasure_arena DEFAULT CHARACTER SET utf8mb4;", conn);
    await cmd.ExecuteNonQueryAsync();
}

using (var conn = new MySqlConnection(connStr))
{
    await conn.OpenAsync();
    using var cmd = new MySqlCommand(@"
        CREATE TABLE IF NOT EXISTS player (
            player_id   VARCHAR(64) PRIMARY KEY,
            nickname    VARCHAR(128) NOT NULL,
            created_at  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
        );
        CREATE TABLE IF NOT EXISTS map_info (
            map_id      VARCHAR(64) PRIMARY KEY,
            map_name    VARCHAR(128) NOT NULL,
            json_path   VARCHAR(256) NOT NULL,
            description VARCHAR(512),
            created_at  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
        );
        CREATE TABLE IF NOT EXISTS room_config (
            room_id                 VARCHAR(64) PRIMARY KEY,
            room_name               VARCHAR(128) NOT NULL,
            map_id                  VARCHAR(64) NOT NULL,
            game_mode               VARCHAR(32) NOT NULL DEFAULT 'TeamTreasure',
            max_players             INT NOT NULL DEFAULT 4,
            match_time              FLOAT NOT NULL DEFAULT 300.0,
            round_count             INT NOT NULL DEFAULT 1,
            player_max_hp           INT NOT NULL DEFAULT 100,
            weapon_id               VARCHAR(64) NOT NULL DEFAULT 'energy_gun',
            weapon_damage           INT NOT NULL DEFAULT 25,
            weapon_range            FLOAT NOT NULL DEFAULT 15.0,
            weapon_cooldown         FLOAT NOT NULL DEFAULT 0.5,
            respawn_countdown       FLOAT NOT NULL DEFAULT 5.0,
            normal_treasure_score   INT NOT NULL DEFAULT 10,
            rare_treasure_score     INT NOT NULL DEFAULT 30,
            final_treasure_score    INT NOT NULL DEFAULT 50,
            created_at              DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            FOREIGN KEY (map_id) REFERENCES map_info(map_id)
        );
        CREATE TABLE IF NOT EXISTS match_result (
            match_id    VARCHAR(64) PRIMARY KEY,
            room_id     VARCHAR(64) NOT NULL,
            map_id      VARCHAR(64) NOT NULL,
            red_score   INT NOT NULL DEFAULT 0,
            blue_score  INT NOT NULL DEFAULT 0,
            winner_team VARCHAR(16) NOT NULL DEFAULT 'Draw',
            duration    FLOAT NOT NULL,
            started_at  DATETIME NOT NULL,
            ended_at    DATETIME NOT NULL
        );
        CREATE TABLE IF NOT EXISTS player_match_stat (
            id                  INT PRIMARY KEY AUTO_INCREMENT,
            match_id            VARCHAR(64) NOT NULL,
            player_id           VARCHAR(64) NOT NULL,
            team                VARCHAR(16) NOT NULL,
            kills               INT NOT NULL DEFAULT 0,
            deaths              INT NOT NULL DEFAULT 0,
            treasures_submitted INT NOT NULL DEFAULT 0,
            score_contribution  INT NOT NULL DEFAULT 0
        );
    ", conn);
    await cmd.ExecuteNonQueryAsync();
}

Console.WriteLine("[DB API] Listening on http://0.0.0.0:5500");

// ---- Health ----
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// ---- Player ----
app.MapPut("/player/{id}", async (string id, [FromBody] PlayerBody body) =>
{
    using var conn = new MySqlConnection(connStr);
    await conn.OpenAsync();
    using var cmd = new MySqlCommand(@"
        INSERT INTO player (player_id, nickname, created_at)
        VALUES (@id, @nickname, NOW())
        ON DUPLICATE KEY UPDATE nickname = @nickname;", conn);
    cmd.Parameters.AddWithValue("@id", id);
    cmd.Parameters.AddWithValue("@nickname", body.nickname);
    await cmd.ExecuteNonQueryAsync();
    return Results.Ok(new { player_id = id, nickname = body.nickname });
});

app.MapGet("/player/{id}", async (string id) =>
{
    using var conn = new MySqlConnection(connStr);
    await conn.OpenAsync();
    using var cmd = new MySqlCommand(
        "SELECT player_id, nickname, created_at FROM player WHERE player_id = @id;", conn);
    cmd.Parameters.AddWithValue("@id", id);
    using var reader = await cmd.ExecuteReaderAsync();
    if (await reader.ReadAsync())
        return Results.Ok(new { player_id = reader.GetString(0), nickname = reader.GetString(1), created_at = reader.GetDateTime(2).ToString("o") });
    return Results.NotFound();
});

app.MapGet("/player/{id}/exists", async (string id) =>
{
    using var conn = new MySqlConnection(connStr);
    await conn.OpenAsync();
    using var cmd = new MySqlCommand("SELECT COUNT(1) FROM player WHERE player_id = @id;", conn);
    cmd.Parameters.AddWithValue("@id", id);
    var count = (long)(await cmd.ExecuteScalarAsync())!;
    return Results.Ok(new { exists = count > 0 });
});

// ---- Room ----
app.MapPost("/room", async ([FromBody] RoomBody body) =>
{
    using var conn = new MySqlConnection(connStr);
    await conn.OpenAsync();
    using var cmd = new MySqlCommand(@"
        INSERT INTO room_config (
            room_id, room_name, map_id, game_mode,
            max_players, match_time, round_count,
            player_max_hp, weapon_id, weapon_damage,
            weapon_range, weapon_cooldown, respawn_countdown,
            normal_treasure_score, rare_treasure_score, final_treasure_score,
            created_at
        ) VALUES (
            @room_id, @room_name, @map_id, @game_mode,
            @max_players, @match_time, @round_count,
            @player_max_hp, @weapon_id, @weapon_damage,
            @weapon_range, @weapon_cooldown, @respawn_countdown,
            @normal_treasure_score, @rare_treasure_score, @final_treasure_score,
            NOW()
        );", conn);
    cmd.Parameters.AddWithValue("@room_id", body.room_id);
    cmd.Parameters.AddWithValue("@room_name", body.room_name);
    cmd.Parameters.AddWithValue("@map_id", body.map_id);
    cmd.Parameters.AddWithValue("@game_mode", body.game_mode);
    cmd.Parameters.AddWithValue("@max_players", body.max_players);
    cmd.Parameters.AddWithValue("@match_time", body.match_time);
    cmd.Parameters.AddWithValue("@round_count", body.round_count);
    cmd.Parameters.AddWithValue("@player_max_hp", body.player_max_hp);
    cmd.Parameters.AddWithValue("@weapon_id", body.weapon_id);
    cmd.Parameters.AddWithValue("@weapon_damage", body.weapon_damage);
    cmd.Parameters.AddWithValue("@weapon_range", body.weapon_range);
    cmd.Parameters.AddWithValue("@weapon_cooldown", body.weapon_cooldown);
    cmd.Parameters.AddWithValue("@respawn_countdown", body.respawn_countdown);
    cmd.Parameters.AddWithValue("@normal_treasure_score", body.normal_treasure_score);
    cmd.Parameters.AddWithValue("@rare_treasure_score", body.rare_treasure_score);
    cmd.Parameters.AddWithValue("@final_treasure_score", body.final_treasure_score);
    await cmd.ExecuteNonQueryAsync();
    return Results.Ok(new { room_id = body.room_id, status = "created" });
});

app.MapGet("/room/{id}", async (string id) =>
{
    using var conn = new MySqlConnection(connStr);
    await conn.OpenAsync();
    using var cmd = new MySqlCommand(
        "SELECT * FROM room_config WHERE room_id = @id;", conn);
    cmd.Parameters.AddWithValue("@id", id);
    using var reader = await cmd.ExecuteReaderAsync();
    if (await reader.ReadAsync())
        return Results.Ok(MapRoomRecord(reader));
    return Results.NotFound();
});

app.MapGet("/room", async () =>
{
    using var conn = new MySqlConnection(connStr);
    await conn.OpenAsync();
    using var cmd = new MySqlCommand(
        "SELECT * FROM room_config ORDER BY created_at DESC;", conn);
    using var reader = await cmd.ExecuteReaderAsync();
    var list = new List<object>();
    while (await reader.ReadAsync())
        list.Add(MapRoomRecord(reader));
    return Results.Ok(list);
});

app.MapDelete("/room/{id}", async (string id) =>
{
    using var conn = new MySqlConnection(connStr);
    await conn.OpenAsync();
    using var cmd = new MySqlCommand(
        "DELETE FROM room_config WHERE room_id = @id;", conn);
    cmd.Parameters.AddWithValue("@id", id);
    await cmd.ExecuteNonQueryAsync();
    return Results.Ok(new { deleted = id });
});

// ---- Match ----
app.MapPost("/match", async ([FromBody] MatchBody body) =>
{
    using var conn = new MySqlConnection(connStr);
    await conn.OpenAsync();
    using var tx = await conn.BeginTransactionAsync();
    using var cmd = new MySqlCommand(@"
        INSERT INTO match_result (
            match_id, room_id, map_id, red_score, blue_score,
            winner_team, duration, started_at, ended_at
        ) VALUES (
            @match_id, @room_id, @map_id, @red_score, @blue_score,
            @winner_team, @duration, @started_at, @ended_at
        );", conn, tx);
    cmd.Parameters.AddWithValue("@match_id", body.match_id);
    cmd.Parameters.AddWithValue("@room_id", body.room_id);
    cmd.Parameters.AddWithValue("@map_id", body.map_id);
    cmd.Parameters.AddWithValue("@red_score", body.red_score);
    cmd.Parameters.AddWithValue("@blue_score", body.blue_score);
    cmd.Parameters.AddWithValue("@winner_team", body.winner_team);
    cmd.Parameters.AddWithValue("@duration", body.duration);
    cmd.Parameters.AddWithValue("@started_at", body.started_at);
    cmd.Parameters.AddWithValue("@ended_at", body.ended_at);
    await cmd.ExecuteNonQueryAsync();

    foreach (var stat in body.player_stats)
    {
        using var cmdStat = new MySqlCommand(@"
            INSERT INTO player_match_stat (
                match_id, player_id, team, kills, deaths,
                treasures_submitted, score_contribution
            ) VALUES (
                @match_id, @player_id, @team, @kills, @deaths,
                @treasures_submitted, @score_contribution
            );", conn, tx);
        cmdStat.Parameters.AddWithValue("@match_id", body.match_id);
        cmdStat.Parameters.AddWithValue("@player_id", stat.player_id);
        cmdStat.Parameters.AddWithValue("@team", stat.team);
        cmdStat.Parameters.AddWithValue("@kills", stat.kills);
        cmdStat.Parameters.AddWithValue("@deaths", stat.deaths);
        cmdStat.Parameters.AddWithValue("@treasures_submitted", stat.treasures_submitted);
        cmdStat.Parameters.AddWithValue("@score_contribution", stat.score_contribution);
        await cmdStat.ExecuteNonQueryAsync();
    }

    await tx.CommitAsync();
    return Results.Ok(new { match_id = body.match_id, status = "saved" });
});

app.MapGet("/match/{id}", async (string id) =>
{
    using var conn = new MySqlConnection(connStr);
    await conn.OpenAsync();
    using var cmd = new MySqlCommand(
        "SELECT * FROM match_result WHERE match_id = @id;", conn);
    cmd.Parameters.AddWithValue("@id", id);
    using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync()) return Results.NotFound();
    var match = MapMatchRecord(reader);
    await reader.DisposeAsync();

    using var cmd2 = new MySqlCommand(
        "SELECT * FROM player_match_stat WHERE match_id = @id;", conn);
    cmd2.Parameters.AddWithValue("@id", id);
    using var reader2 = await cmd2.ExecuteReaderAsync();
    var stats = new List<object>();
    while (await reader2.ReadAsync())
        stats.Add(MapStatRecord(reader2));

    return Results.Ok(new { match, player_stats = stats });
});

app.MapGet("/match", async () =>
{
    using var conn = new MySqlConnection(connStr);
    await conn.OpenAsync();
    using var cmd = new MySqlCommand(
        "SELECT * FROM match_result ORDER BY ended_at DESC;", conn);
    using var reader = await cmd.ExecuteReaderAsync();
    var list = new List<object>();
    while (await reader.ReadAsync())
        list.Add(MapMatchRecord(reader));
    return Results.Ok(list);
});

await app.RunAsync();

// ---- Helpers ----

static object MapRoomRecord(MySqlDataReader r) => new
{
    room_id = r.GetString("room_id"),
    room_name = r.GetString("room_name"),
    map_id = r.GetString("map_id"),
    game_mode = r.GetString("game_mode"),
    max_players = r.GetInt32("max_players"),
    match_time = r.GetFloat("match_time"),
    player_max_hp = r.GetInt32("player_max_hp"),
    weapon_damage = r.GetInt32("weapon_damage"),
    respawn_countdown = r.GetFloat("respawn_countdown"),
    normal_treasure_score = r.GetInt32("normal_treasure_score"),
    rare_treasure_score = r.GetInt32("rare_treasure_score"),
    final_treasure_score = r.GetInt32("final_treasure_score"),
    created_at = r.GetDateTime("created_at").ToString("o")
};

static object MapMatchRecord(MySqlDataReader r) => new
{
    match_id = r.GetString("match_id"),
    room_id = r.GetString("room_id"),
    map_id = r.GetString("map_id"),
    red_score = r.GetInt32("red_score"),
    blue_score = r.GetInt32("blue_score"),
    winner_team = r.GetString("winner_team"),
    duration = r.GetFloat("duration"),
    started_at = r.GetDateTime("started_at").ToString("o"),
    ended_at = r.GetDateTime("ended_at").ToString("o")
};

static object MapStatRecord(MySqlDataReader r) => new
{
    player_id = r.GetString("player_id"),
    team = r.GetString("team"),
    kills = r.GetInt32("kills"),
    deaths = r.GetInt32("deaths"),
    treasures_submitted = r.GetInt32("treasures_submitted"),
    score_contribution = r.GetInt32("score_contribution")
};

// ---- DTOs ----
record PlayerBody(string nickname);
record RoomBody(
    string room_id, string room_name, string map_id, string game_mode,
    int max_players, float match_time, int round_count, int player_max_hp,
    string weapon_id, int weapon_damage, float weapon_range, float weapon_cooldown,
    float respawn_countdown, int normal_treasure_score, int rare_treasure_score,
    int final_treasure_score);
record MatchStatBody(
    string player_id, string team, int kills, int deaths,
    int treasures_submitted, int score_contribution);
record MatchBody(
    string match_id, string room_id, string map_id,
    int red_score, int blue_score, string winner_team,
    float duration, string started_at, string ended_at,
    List<MatchStatBody> player_stats);
