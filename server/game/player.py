"""Player state and state machine."""

from __future__ import annotations
import asyncio
import time
from dataclasses import dataclass, field
from protocol.payloads import Vec3
from protocol.types import PlayerState, Team


@dataclass
class Player:
    player_id: str
    nickname: str = ""
    team: Team = Team.None_
    state: PlayerState = PlayerState.Alive
    hp: int = 100
    max_hp: int = 100
    is_connected: bool = True

    # Position tracking
    position: Vec3 = field(default_factory=Vec3)
    rotation_y: float = 0.0
    pose_seq: int = 0
    last_pose_time: float = 0.0  # monotonic time of last UDP pose

    # Carry state
    carried_treasure_id: str = ""

    # Combat
    last_attack_time: float = 0.0  # monotonic time

    # Respawn
    respawn_task: asyncio.Task | None = field(default=None, repr=False)

    def is_alive(self) -> bool:
        return self.state == PlayerState.Alive

    def is_ghost(self) -> bool:
        return self.state == PlayerState.GhostRetreat

    def is_respawning(self) -> bool:
        return self.state == PlayerState.Respawning

    def can_act(self) -> bool:
        """Can the player perform game actions (attack, pickup, submit)?"""
        return self.state == PlayerState.Alive

    def is_stale(self, threshold: float = 1.0) -> bool:
        """Check if the player's pose data is stale."""
        if self.last_pose_time == 0.0:
            return True
        return time.monotonic() - self.last_pose_time > threshold

    def update_pose(self, x: float, y: float, z: float, rotation_y: float, pose_seq: int) -> None:
        """Update position from a player_pose message."""
        self.position = Vec3(x, y, z)
        self.rotation_y = rotation_y
        self.pose_seq = pose_seq
        self.last_pose_time = time.monotonic()

    def take_damage(self, damage: int) -> int:
        """Apply damage. Returns remaining HP."""
        if not self.can_act():
            return self.hp
        self.hp = max(0, self.hp - damage)
        return self.hp

    def drop_treasure(self) -> str:
        """Drop carried treasure. Returns the treasure_id that was dropped."""
        tid = self.carried_treasure_id
        self.carried_treasure_id = ""
        return tid

    def carry_treasure(self, treasure_id: str) -> None:
        self.carried_treasure_id = treasure_id

    def to_snapshot(self) -> dict:
        """Convert to a dict suitable for PlayerSnapshot."""
        return {
            "player_id": self.player_id,
            "nickname": self.nickname,
            "team": self.team.value,
            "state": self.state.value,
            "hp": self.hp,
            "max_hp": self.max_hp,
            "is_connected": self.is_connected,
            "carried_treasure_id": self.carried_treasure_id,
            "position": self.position.to_dict(),
            "rotation_y": self.rotation_y,
        }

    def to_pose_info(self, server_time_ms: int, stale_threshold: float = 1.0) -> dict:
        """Convert to a dict suitable for PosePlayerInfo."""
        return {
            "player_id": self.player_id,
            "position": self.position.to_dict(),
            "rotation_y": self.rotation_y,
            "server_time_ms": server_time_ms,
            "stale": self.is_stale(stale_threshold),
        }
