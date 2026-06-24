"""Room configuration loaded from mapdata.json."""

from __future__ import annotations
import json
from pathlib import Path
from dataclasses import dataclass, field
from protocol.payloads import Vec3
from protocol.types import TreasureType


@dataclass
class WeaponConfig:
    weapon_id: str = "energy_gun"
    damage: int = 25
    range: float = 15.0
    cooldown: float = 0.5

    @classmethod
    def from_dict(cls, d: dict) -> WeaponConfig:
        return cls(
            weapon_id=str(d.get("weapon_id", "energy_gun")),
            damage=int(d.get("damage", 25)),
            range=float(d.get("range", 15)),
            cooldown=float(d.get("cooldown", 0.5)),
        )


@dataclass
class BaseConfig:
    position: Vec3 = field(default_factory=Vec3)
    radius: float = 3.0

    @classmethod
    def from_dict(cls, d: dict) -> BaseConfig:
        return cls(
            position=Vec3.from_dict(d),
            radius=float(d.get("radius", 3.0)),
        )


@dataclass
class RespawnZoneConfig:
    position: Vec3 = field(default_factory=Vec3)
    radius: float = 2.0

    @classmethod
    def from_dict(cls, d: dict) -> RespawnZoneConfig:
        return cls(
            position=Vec3.from_dict(d),
            radius=float(d.get("radius", 2.0)),
        )


@dataclass
class TreasureConfig:
    treasure_id: str = ""
    treasure_type: str = "Normal"
    position: Vec3 = field(default_factory=Vec3)

    @classmethod
    def from_dict(cls, d: dict) -> TreasureConfig:
        return cls(
            treasure_id=str(d.get("treasure_id", "")),
            treasure_type=str(d.get("treasure_type", "Normal")),
            position=Vec3.from_dict(d.get("position", {})),
        )


@dataclass
class RoomConfig:
    room_id: str = "room_default"
    room_name: str = "默认房间"
    game_mode: str = "TeamTreasure"
    map_id: str = "test_map_01"
    max_players: int = 4
    match_time: float = 300.0
    round_count: int = 1
    player_max_hp: int = 100
    respawn_countdown: float = 5.0
    weapon_config: WeaponConfig = field(default_factory=WeaponConfig)
    treasure_scores: dict[str, int] = field(default_factory=lambda: {"Normal": 10, "Rare": 30, "Final": 50})
    treasure_refresh_interval: float = 10.0
    supply_refresh_interval: float = 20.0
    bases: dict[str, BaseConfig] = field(default_factory=dict)
    respawn_zones: list[RespawnZoneConfig] = field(default_factory=list)
    treasures: list[TreasureConfig] = field(default_factory=list)

    def get_treasure_score(self, treasure_type: str) -> int:
        return self.treasure_scores.get(treasure_type, 0)

    def get_base(self, team: str) -> BaseConfig | None:
        return self.bases.get(team)

    @classmethod
    def from_dict(cls, d: dict) -> RoomConfig:
        bases = {}
        if "bases" in d:
            for team, base_d in d["bases"].items():
                bases[team] = BaseConfig.from_dict(base_d)

        respawn_zones = [RespawnZoneConfig.from_dict(rz) for rz in d.get("respawn_zones", [])]
        treasures = [TreasureConfig.from_dict(t) for t in d.get("treasures", [])]

        return cls(
            room_id=str(d.get("room_id", "room_default")),
            room_name=str(d.get("room_name", "默认房间")),
            game_mode=str(d.get("game_mode", "TeamTreasure")),
            map_id=str(d.get("map_id", "test_map_01")),
            max_players=int(d.get("max_players", 4)),
            match_time=float(d.get("match_time", 300)),
            round_count=int(d.get("round_count", 1)),
            player_max_hp=int(d.get("player_max_hp", 100)),
            respawn_countdown=float(d.get("respawn_countdown", 5)),
            weapon_config=WeaponConfig.from_dict(d.get("weapon_config", {})),
            treasure_scores={str(k): int(v) for k, v in d.get("treasure_scores", {"Normal": 10, "Rare": 30, "Final": 50}).items()},
            treasure_refresh_interval=float(d.get("treasure_refresh_interval", 10)),
            supply_refresh_interval=float(d.get("supply_refresh_interval", 20)),
            bases=bases,
            respawn_zones=respawn_zones,
            treasures=treasures,
        )


def load_config(path: str = "mapdata.json") -> RoomConfig:
    """Load room configuration from a JSON file."""
    config_path = Path(path)
    if not config_path.is_absolute():
        # Try relative to the current working directory, then relative to this file
        if not config_path.exists():
            config_path = Path(__file__).parent / path

    with open(config_path, "r", encoding="utf-8") as f:
        data = json.load(f)

    return RoomConfig.from_dict(data)
