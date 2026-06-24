"""MessageRouter — dispatch by envelope.type to the appropriate handler."""

from __future__ import annotations
import asyncio
import logging
from typing import TYPE_CHECKING

from protocol.types import MsgType

if TYPE_CHECKING:
    from game.room import Room
    from protocol.envelope import SimpleNetworkEnvelope

logger = logging.getLogger(__name__)


class MessageRouter:
    """Routes incoming messages to room handlers based on type and transport."""

    def __init__(self, room: Room):
        self.room = room

        # TCP handler map
        self._tcp_handlers = {
            MsgType.JOIN_ROOM_REQUEST: self._handle_join,
            MsgType.SWITCH_TEAM_REQUEST: self._handle_switch_team,
            MsgType.START_MATCH_REQUEST: self._handle_start_match,
            MsgType.PICKUP_TREASURE_REQUEST: self._handle_pickup,
            MsgType.SUBMIT_TREASURE_REQUEST: self._handle_submit,
            MsgType.ATTACK_REQUEST: self._handle_attack,
            MsgType.HEARTBEAT: self._handle_heartbeat,
        }

        # UDP handler map
        self._udp_handlers = {
            MsgType.PLAYER_POSE: self._handle_pose,
        }

    # ── TCP dispatch ──────────────────────────────────────────────────

    async def route_tcp(
        self,
        env: SimpleNetworkEnvelope,
        transport: asyncio.Transport,
    ) -> None:
        """Route a TCP message to its handler."""
        handler = self._tcp_handlers.get(env.type)
        if handler:
            try:
                await handler(env, transport)
            except Exception as e:
                logger.error(f"Error handling TCP message type '{env.type}': {e}", exc_info=True)
        else:
            logger.warning(f"Unknown TCP message type: '{env.type}' from {env.player_id}")

    # ── UDP dispatch ──────────────────────────────────────────────────

    def route_udp(
        self,
        env: SimpleNetworkEnvelope,
        addr: tuple[str, int],
    ) -> None:
        """Route a UDP message to its handler (synchronous, no await in DatagramProtocol)."""
        handler = self._udp_handlers.get(env.type)
        if handler:
            try:
                handler(env, addr)
            except Exception as e:
                logger.error(f"Error handling UDP message type '{env.type}': {e}", exc_info=True)
        else:
            logger.debug(f"Unknown UDP message type: '{env.type}' from {addr}")

    # ── TCP handlers ──────────────────────────────────────────────────

    async def _handle_join(
        self, env: SimpleNetworkEnvelope, transport: asyncio.Transport
    ) -> None:
        await self.room.handle_join(env, transport)

    async def _handle_switch_team(self, env: SimpleNetworkEnvelope, _) -> None:
        await self.room.handle_switch_team(env)

    async def _handle_start_match(self, env: SimpleNetworkEnvelope, _) -> None:
        await self.room.handle_start_match(env)

    async def _handle_pickup(self, env: SimpleNetworkEnvelope, _) -> None:
        await self.room.handle_pickup(env)

    async def _handle_submit(self, env: SimpleNetworkEnvelope, _) -> None:
        await self.room.handle_submit(env)

    async def _handle_attack(self, env: SimpleNetworkEnvelope, _) -> None:
        await self.room.handle_attack(env)

    async def _handle_heartbeat(self, env: SimpleNetworkEnvelope, _) -> None:
        # Heartbeat is a no-op; just keeps the connection alive
        pass

    # ── UDP handlers ──────────────────────────────────────────────────

    def _handle_pose(self, env: SimpleNetworkEnvelope, addr: tuple[str, int]) -> None:
        self.room.update_player_pose(env, addr)
