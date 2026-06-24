"""TreasureAuthority — pickup validation, submit validation, scoring."""

from __future__ import annotations
import logging
from typing import TYPE_CHECKING

from protocol.types import TreasureState, Team
from protocol.payloads import Vec3

if TYPE_CHECKING:
    from .room import Room
    from .player import Player

logger = logging.getLogger(__name__)


class TreasureAuthority:
    """Handles treasure pickup and submit logic."""

    def __init__(self, room: Room):
        self.room = room

    @property
    def config(self):
        return self.room.config

    def process_pickup(self, player: Player, treasure_id: str) -> dict:
        """Validate and execute a treasure pickup.

        Returns a dict with keys: ok, error_code, message.
        """
        # Check player can act
        if not player.can_act():
            return {"ok": False, "error_code": "bad_state",
                    "message": "Cannot pick up treasure in current state."}

        # Check if already carrying
        if player.carried_treasure_id:
            return {"ok": False, "error_code": "already_carrying",
                    "message": "Already carrying a treasure."}

        # Find treasure
        treasure = self.room.treasures.get(treasure_id)
        if not treasure:
            return {"ok": False, "error_code": "not_found",
                    "message": f"Treasure '{treasure_id}' not found."}

        # Check treasure state
        if treasure["state"] != TreasureState.Spawned.value:
            return {"ok": False, "error_code": "wrong_state",
                    "message": f"Treasure is {treasure['state']}, not Spawned."}

        # Check distance
        from protocol.types import PICKUP_DISTANCE

        treasure_pos = Vec3(
            treasure["position"]["x"],
            treasure["position"]["y"],
            treasure["position"]["z"],
        )
        dist = player.position.distance_to(treasure_pos)

        if dist > PICKUP_DISTANCE:
            return {"ok": False, "error_code": "too_far",
                    "message": f"Too far from treasure ({dist:.1f}m > {PICKUP_DISTANCE}m)."}

        # Execute pickup
        treasure["state"] = TreasureState.Carried.value
        treasure["carrier_player_id"] = player.player_id
        player.carry_treasure(treasure_id)

        logger.info(f"Room {self.room.room_id}: {player.player_id} picked up "
                    f"{treasure_id} ({treasure['treasure_type']})")
        return {"ok": True, "message": f"Picked up {treasure_id}."}

    def process_submit(self, player: Player) -> dict:
        """Validate and execute a treasure submission.

        Returns a dict with keys: ok, error_code, message, score (if ok).
        """
        # Check player can act
        if not player.can_act():
            return {"ok": False, "error_code": "bad_state",
                    "message": "Cannot submit treasure in current state."}

        # Check if carrying
        tid = player.carried_treasure_id
        if not tid:
            return {"ok": False, "error_code": "not_carrying",
                    "message": "Not carrying any treasure."}

        # Find treasure
        treasure = self.room.treasures.get(tid)
        if not treasure:
            return {"ok": False, "error_code": "not_found",
                    "message": f"Treasure '{tid}' not found."}

        # Check treasure state
        if treasure["state"] != TreasureState.Carried.value:
            return {"ok": False, "error_code": "wrong_state",
                    "message": f"Treasure is {treasure['state']}, not Carried."}

        # Check distance to own base
        from protocol.types import SUBMIT_DISTANCE

        base = self.config.get_base(player.team.value)
        if not base:
            return {"ok": False, "error_code": "no_base",
                    "message": f"No base defined for team {player.team.value}."}

        dist = player.position.distance_to(base.position)
        max_dist = SUBMIT_DISTANCE + base.radius

        if dist > max_dist:
            return {"ok": False, "error_code": "too_far",
                    "message": f"Too far from base ({dist:.1f}m > {max_dist:.1f}m)."}

        # Execute submission
        score = treasure["score_value"]
        treasure["state"] = TreasureState.Submitted.value
        treasure["carrier_player_id"] = ""
        player.carried_treasure_id = ""

        # Return treasure to spawn point
        if "spawn_position" in treasure:
            treasure["position"] = dict(treasure["spawn_position"])
            # Re-spawn after drop
            treasure["state"] = TreasureState.Spawned.value

        logger.info(f"Room {self.room.room_id}: {player.player_id} submitted "
                    f"{tid} for {score} points (team {player.team.value})")
        return {"ok": True, "message": f"Submitted {tid}! +{score} points.", "score": score}
