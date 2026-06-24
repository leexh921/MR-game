"""All request and response payload dataclasses."""

from __future__ import annotations
from dataclasses import dataclass, field
from typing import Optional
from .types import RoomState, Team, PlayerState, TreasureType, TreasureState


# ─── Helper: Vector3 ────────────────────────────────────────────────

@dataclass
class Vec3:
    x: float = 0.0
    y: float = 0.0
    z: float = 0.0

    @classmethod
    def from_dict(cls, d: dict) -> Vec3:
        return cls(x=float(d.get("x", 0)), y=float(d.get("y", 0)), z=float(d.get("z", 0)))

    def to_dict(self) -> dict:
        return {"x": self.x, "y": self.y, "z": self.z}

    def distance_to(self, other: Vec3) -> float:
        dx = self.x - other.x
        dy = self.y - other.y
        dz = self.z - other.z
        return (dx * dx + dy * dy + dz * dz) ** 0.5


# ─── Client → Server (TCP) ──────────────────────────────────────────

@dataclass
class JoinRoomRequestPayload:
    nickname: str = ""
    client_type: str = "Unknown"

    @classmethod
    def from_dict(cls, d: dict) -> JoinRoomRequestPayload:
        return cls(nickname=str(d.get("nickname", "")), client_type=str(d.get("client_type", "Unknown")))


@dataclass
class SwitchTeamRequestPayload:
    target_team: str = "None"

    @classmethod
    def from_dict(cls, d: dict) -> SwitchTeamRequestPayload:
        return cls(target_team=str(d.get("target_team", "None")))


@dataclass
class StartMatchRequestPayload:
    @classmethod
    def from_dict(cls, d: dict) -> StartMatchRequestPayload:
        return cls()


@dataclass
class PickupTreasureRequestPayload:
    treasure_id: str = ""

    @classmethod
    def from_dict(cls, d: dict) -> PickupTreasureRequestPayload:
        return cls(treasure_id=str(d.get("treasure_id", "")))


@dataclass
class SubmitTreasureRequestPayload:
    @classmethod
    def from_dict(cls, d: dict) -> SubmitTreasureRequestPayload:
        return cls()


@dataclass
class AttackRequestPayload:
    origin: Vec3 = field(default_factory=Vec3)
    direction: Vec3 = field(default_factory=Vec3)
    client_time: float = 0.0

    @classmethod
    def from_dict(cls, d: dict) -> AttackRequestPayload:
        return cls(
            origin=Vec3.from_dict(d.get("origin", {})),
            direction=Vec3.from_dict(d.get("direction", {})),
            client_time=float(d.get("client_time", 0.0)),
        )


# ─── Client → Server (UDP) ──────────────────────────────────────────

@dataclass
class PlayerPosePayload:
    x: float = 0.0
    y: float = 0.0
    z: float = 0.0
    rotation_y: float = 0.0
    pose_seq: int = 0

    @classmethod
    def from_dict(cls, d: dict) -> PlayerPosePayload:
        return cls(
            x=float(d.get("x", 0)),
            y=float(d.get("y", 0)),
            z=float(d.get("z", 0)),
            rotation_y=float(d.get("rotation_y", 0)),
            pose_seq=int(d.get("pose_seq", 0)),
        )

    def to_vec3(self) -> Vec3:
        return Vec3(self.x, self.y, self.z)


# ─── Server → Client (TCP) ──────────────────────────────────────────

@dataclass
class JoinRoomResultPayload:
    ok: bool = False
    error_code: str = ""
    message: str = ""
    player_id: str = ""
    room_id: str = ""
    team: str = "None"

    def to_dict(self) -> dict:
        return {
            "ok": self.ok,
            "error_code": self.error_code,
            "message": self.message,
            "player_id": self.player_id,
            "room_id": self.room_id,
            "team": self.team,
        }


@dataclass
class CommandResultPayload:
    ok: bool = False
    error_code: str = ""
    message: str = ""

    def to_dict(self) -> dict:
        return {
            "ok": self.ok,
            "error_code": self.error_code,
            "message": self.message,
        }

    @classmethod
    def success(cls, message: str = "") -> CommandResultPayload:
        return cls(ok=True, message=message)

    @classmethod
    def failure(cls, error_code: str, message: str = "") -> CommandResultPayload:
        return cls(ok=False, error_code=error_code, message=message)


# ─── MatchSnapshot sub-structures ────────────────────────────────────

@dataclass
class PlayerSnapshot:
    player_id: str = ""
    nickname: str = ""
    team: str = "None"
    state: str = "Alive"
    hp: int = 100
    max_hp: int = 100
    is_connected: bool = True
    carried_treasure_id: str = ""
    position: Vec3 = field(default_factory=Vec3)
    rotation_y: float = 0.0

    def to_dict(self) -> dict:
        return {
            "player_id": self.player_id,
            "nickname": self.nickname,
            "team": self.team,
            "state": self.state,
            "hp": self.hp,
            "max_hp": self.max_hp,
            "is_connected": self.is_connected,
            "carried_treasure_id": self.carried_treasure_id,
            "position": self.position.to_dict(),
            "rotation_y": self.rotation_y,
        }


@dataclass
class TreasureSnapshot:
    treasure_id: str = ""
    treasure_type: str = "Normal"
    state: str = "Spawned"
    score_value: int = 0
    position: Vec3 = field(default_factory=Vec3)
    carrier_player_id: str = ""

    def to_dict(self) -> dict:
        return {
            "treasure_id": self.treasure_id,
            "treasure_type": self.treasure_type,
            "state": self.state,
            "score_value": self.score_value,
            "position": self.position.to_dict(),
            "carrier_player_id": self.carrier_player_id,
        }


@dataclass
class MatchSnapshotPayload:
    room_id: str = ""
    room_state: str = "Waiting"
    remaining_time: float = 300.0
    red_score: int = 0
    blue_score: int = 0
    players: list[PlayerSnapshot] = field(default_factory=list)
    treasures: list[TreasureSnapshot] = field(default_factory=list)

    def to_dict(self) -> dict:
        return {
            "room_id": self.room_id,
            "room_state": self.room_state,
            "remaining_time": self.remaining_time,
            "red_score": self.red_score,
            "blue_score": self.blue_score,
            "players": [p.to_dict() for p in self.players],
            "treasures": [t.to_dict() for t in self.treasures],
        }


# ─── PoseSnapshot sub-structures ────────────────────────────────────

@dataclass
class PosePlayerInfo:
    player_id: str = ""
    position: Vec3 = field(default_factory=Vec3)
    rotation_y: float = 0.0
    server_time_ms: int = 0
    stale: bool = False

    def to_dict(self) -> dict:
        return {
            "player_id": self.player_id,
            "position": self.position.to_dict(),
            "rotation_y": self.rotation_y,
            "server_time_ms": self.server_time_ms,
            "stale": self.stale,
        }


@dataclass
class PoseSnapshotPayload:
    room_id: str = ""
    players: list[PosePlayerInfo] = field(default_factory=list)

    def to_dict(self) -> dict:
        return {
            "room_id": self.room_id,
            "players": [p.to_dict() for p in self.players],
        }


# ─── Disconnect ─────────────────────────────────────────────────────

@dataclass
class DisconnectNoticePayload:
    player_id: str = ""
    reason: str = "tcp_disconnected"

    def to_dict(self) -> dict:
        return {
            "player_id": self.player_id,
            "reason": self.reason,
        }
