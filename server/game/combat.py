"""CombatAuthority — raycast hit detection, damage, cooldown, GhostRetreat."""

from __future__ import annotations
import time
import math
import logging
from typing import TYPE_CHECKING

from protocol.payloads import Vec3, AttackRequestPayload
from protocol.types import PlayerState, Team, TreasureState

if TYPE_CHECKING:
    from .room import Room
    from .player import Player

logger = logging.getLogger(__name__)


class CombatAuthority:
    """Handles attack validation, ray-sphere intersection, and damage application."""

    def __init__(self, room: Room):
        self.room = room

    @property
    def config(self):
        return self.room.config

    @property
    def weapon(self):
        return self.config.weapon_config

    def process_attack(self, attacker: Player, attack: AttackRequestPayload) -> dict:
        """Process an attack request.

        Returns a dict with keys: ok, error_code, message, hit_player_id (optional).
        """
        # Check player can act
        if not attacker.can_act():
            return {"ok": False, "error_code": "bad_state", "message": "Cannot attack in current state."}

        # Check cooldown
        now = time.monotonic()
        cooldown_remaining = self.weapon.cooldown - (now - attacker.last_attack_time)
        if cooldown_remaining > 0:
            return {
                "ok": False,
                "error_code": "cooldown",
                "message": f"Attack on cooldown. Wait {cooldown_remaining:.1f}s.",
            }

        attacker.last_attack_time = now

        # Find the first enemy hit along the ray
        origin = attack.origin
        direction = attack.direction
        hit_player = self._raycast_hit(attacker, origin, direction)

        if hit_player:
            # Apply damage
            damage = self.weapon.damage
            remaining_hp = hit_player.take_damage(damage)

            msg = f"Hit {hit_player.nickname}! {damage} damage dealt."
            logger.info(f"Room {self.room.room_id}: {attacker.player_id} attacked "
                        f"{hit_player.player_id} for {damage} damage (HP: {remaining_hp})")

            if remaining_hp <= 0:
                # Player killed → GhostRetreat
                self._handle_kill(hit_player)
                msg += f" {hit_player.nickname} eliminated!"

            return {"ok": True, "message": msg, "hit_player_id": hit_player.player_id}
        else:
            return {"ok": True, "message": "Attack missed."}

    def _raycast_hit(self, attacker: Player, origin: Vec3, direction: Vec3) -> Player | None:
        """Perform ray-sphere intersection against all opposing-team players.

        The ray starts at `origin` and goes in `direction`.
        Hit radius: HIT_RADIUS (0.5m).
        Max range: weapon_config.range.

        Returns the closest hit player, or None.
        """
        from protocol.types import HIT_RADIUS

        max_range = self.weapon.range
        dir_vec = direction
        dir_len = math.sqrt(dir_vec.x ** 2 + dir_vec.y ** 2 + dir_vec.z ** 2)

        if dir_len < 0.0001:
            # Direction is zero — check if any enemy is within hit radius of origin
            for candidate in self._get_enemies(attacker):
                if origin.distance_to(candidate.position) <= HIT_RADIUS:
                    return candidate
            return None

        # Normalize direction
        dx = dir_vec.x / dir_len
        dy = dir_vec.y / dir_len
        dz = dir_vec.z / dir_len

        closest_dist = float("inf")
        closest_player: Player | None = None

        for candidate in self._get_enemies(attacker):
            hit_dist = self._ray_sphere_intersection(
                origin, dx, dy, dz, candidate.position, HIT_RADIUS
            )
            if hit_dist is not None and 0 <= hit_dist <= max_range:
                if hit_dist < closest_dist:
                    closest_dist = hit_dist
                    closest_player = candidate

        return closest_player

    def _get_enemies(self, attacker: Player) -> list[Player]:
        """Get list of opposing-team players that can be hit."""
        enemies = []
        for p in self.room.players.values():
            if p.player_id == attacker.player_id:
                continue
            if not p.can_act():
                continue
            if p.team == attacker.team:
                continue
            enemies.append(p)
        return enemies

    @staticmethod
    def _ray_sphere_intersection(
        origin: Vec3,
        dx: float, dy: float, dz: float,
        sphere_center: Vec3,
        sphere_radius: float,
    ) -> float | None:
        """Compute ray-sphere intersection distance.

        Returns the distance along the ray to the first intersection point,
        or None if no intersection.
        """
        oc_x = origin.x - sphere_center.x
        oc_y = origin.y - sphere_center.y
        oc_z = origin.z - sphere_center.z

        b = 2.0 * (dx * oc_x + dy * oc_y + dz * oc_z)
        c = oc_x * oc_x + oc_y * oc_y + oc_z * oc_z - sphere_radius * sphere_radius

        discriminant = b * b - 4.0 * c
        if discriminant < 0:
            return None

        sqrt_disc = math.sqrt(discriminant)
        t1 = (-b - sqrt_disc) / 2.0
        t2 = (-b + sqrt_disc) / 2.0

        if t1 >= 0:
            return t1
        if t2 >= 0:
            return t2
        return None  # sphere is behind the ray

    def _handle_kill(self, victim: Player) -> None:
        """Transition a player to GhostRetreat state and drop treasure."""
        victim.state = PlayerState.GhostRetreat
        victim.hp = 0

        # Drop carried treasure
        tid = victim.drop_treasure()
        if tid and tid in self.room.treasures:
            t = self.room.treasures[tid]
            t["state"] = TreasureState.Dropped.value
            t["carrier_player_id"] = ""
            # Drop at current position
            t["position"] = {"x": victim.position.x, "y": victim.position.y, "z": victim.position.z}

        logger.info(f"Room {self.room.room_id}: {victim.player_id} entered GhostRetreat")
