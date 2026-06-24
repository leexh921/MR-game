"""Room state machine: manages players, teams, and match lifecycle."""

from __future__ import annotations
import asyncio
import time
import logging
from typing import Optional

from config import RoomConfig
from protocol.types import RoomState, Team, PlayerState, TreasureState, MsgType, PROTOCOL_VERSION
from protocol.types import (
    BROADCAST_MATCH_INTERVAL,
    BROADCAST_POSE_INTERVAL,
    COUNTDOWN_DURATION,
    RESPAWN_COUNTDOWN,
)
from protocol.payloads import (
    JoinRoomResultPayload,
    CommandResultPayload,
    MatchSnapshotPayload,
    PoseSnapshotPayload,
    DisconnectNoticePayload,
    PlayerSnapshot,
    TreasureSnapshot,
    PosePlayerInfo,
    Vec3,
    AttackRequestPayload,
)
from protocol.envelope import SimpleNetworkEnvelope
from .player import Player
from .combat import CombatAuthority
from .treasure import TreasureAuthority

logger = logging.getLogger(__name__)


class Room:
    """A single game room handling the full match lifecycle."""

    def __init__(self, config: RoomConfig):
        self.config = config
        self.room_id = config.room_id
        self.state = RoomState.Waiting
        self.lock = asyncio.Lock()

        # Player registry
        self.players: dict[str, Player] = {}
        self._player_counter = 0
        # Map client-provided player_id -> server-assigned player_id
        self._client_id_map: dict[str, str] = {}

        # Team rosters
        self._red_players: set[str] = set()
        self._blue_players: set[str] = set()

        # Treasures (managed by TreasureAuthority, but owned by Room)
        self.treasures: dict[str, dict] = {}  # treasure_id -> state dict
        self._init_treasures()

        # Scores
        self.red_score: int = 0
        self.blue_score: int = 0

        # Match timer
        self.remaining_time: float = config.match_time
        self._match_start_time: float = 0.0
        self._match_task: asyncio.Task | None = None

        # Broadcast tasks
        self._broadcast_match_task: asyncio.Task | None = None
        self._broadcast_pose_task: asyncio.Task | None = None
        self._broadcast_running: bool = False

        # Position logger task (runs always to show player positions)
        self._pos_log_task: asyncio.Task | None = None
        self._pos_log_running: bool = False

        # TCP sessions: player_id -> Transport (for sending)
        self._tcp_transports: dict[str, asyncio.Transport] = {}

        # UDP transport reference (single instance)
        self._udp_transport: asyncio.DatagramTransport | None = None
        # UDP client addresses: player_id -> (host, port)
        self._udp_addresses: dict[str, tuple[str, int]] = {}

        # Sub-authorities
        self.combat = CombatAuthority(self)
        self.treasure_authority = TreasureAuthority(self)

        # Sequence counter for server broadcasts
        self._seq: int = 0

    # ── Treasure init ─────────────────────────────────────────────────

    def _init_treasures(self) -> None:
        for tc in self.config.treasures:
            self.treasures[tc.treasure_id] = {
                "treasure_id": tc.treasure_id,
                "treasure_type": tc.treasure_type,
                "state": TreasureState.Spawned.value,
                "score_value": self.config.get_treasure_score(tc.treasure_type),
                "position": {"x": tc.position.x, "y": tc.position.y, "z": tc.position.z},
                "carrier_player_id": "",
                "spawn_position": {"x": tc.position.x, "y": tc.position.y, "z": tc.position.z},
            }

    # ── Sequence ──────────────────────────────────────────────────────

    def _next_seq(self) -> int:
        self._seq += 1
        return self._seq

    # ── Player management ─────────────────────────────────────────────

    def _next_player_id(self) -> str:
        self._player_counter += 1
        return f"player_{self._player_counter}"

    def _resolve_id(self, client_or_server_id: str) -> str:
        """Resolve a client-provided player_id to the server-assigned ID.
        If already a server ID (starts with 'player_'), return as-is.
        Falls back to using the mapping table, or returns the input unchanged.
        """
        if client_or_server_id in self.players:
            return client_or_server_id
        mapped = self._client_id_map.get(client_or_server_id)
        if mapped and mapped in self.players:
            return mapped
        return client_or_server_id

    def _get_player(self, client_or_server_id: str) -> Player | None:
        """Get a player by either client or server player_id."""
        resolved = self._resolve_id(client_or_server_id)
        return self.players.get(resolved)

    async def handle_join(
        self,
        env: SimpleNetworkEnvelope,
        transport: asyncio.Transport,
    ) -> None:
        """Handle join_room_request."""
        async with self.lock:
            payload = env.parse_payload()
            nickname = str(payload.get("nickname", "Anonymous"))
            client_type = str(payload.get("client_type", "Unknown"))

            # Check room capacity
            if len(self.players) >= self.config.max_players:
                result = JoinRoomResultPayload(
                    ok=False,
                    error_code="room_full",
                    message="Room is full.",
                )
                self._send_tcp(transport, MsgType.JOIN_ROOM_RESULT, result)
                return

            # Check if this player_id already exists (reconnection)
            existing_id = env.player_id
            if existing_id and existing_id in self.players:
                player = self.players[existing_id]
                player.is_connected = True
                self._tcp_transports[existing_id] = transport
                result = JoinRoomResultPayload(
                    ok=True,
                    message="Rejoined.",
                    player_id=existing_id,
                    room_id=self.room_id,
                    team=player.team.value,
                )
                self._send_tcp(transport, MsgType.JOIN_ROOM_RESULT, result)
                logger.info(f"Player {existing_id} reconnected as '{nickname}'")
                return

            # Create new player
            player_id = self._next_player_id()
            client_id = env.player_id or player_id
            self._client_id_map[client_id] = player_id
            player = Player(
                player_id=player_id,
                nickname=nickname,
                team=Team.None_,
                max_hp=self.config.player_max_hp,
                hp=self.config.player_max_hp,
            )

            # Auto-assign team
            red_count = len(self._red_players)
            blue_count = len(self._blue_players)
            if red_count <= blue_count:
                player.team = Team.Red
                self._red_players.add(player_id)
            else:
                player.team = Team.Blue
                self._blue_players.add(player_id)

            self.players[player_id] = player
            self._tcp_transports[player_id] = transport

            result = JoinRoomResultPayload(
                ok=True,
                message="Joined.",
                player_id=player_id,
                room_id=self.room_id,
                team=player.team.value,
            )
            self._send_tcp(transport, MsgType.JOIN_ROOM_RESULT, result)

            # Check if room is Ready
            self._check_ready()

            logger.info(f"Player {player_id} ({nickname}) joined team {player.team.value}. "
                        f"Room: {len(self.players)}/{self.config.max_players}")

    async def handle_switch_team(self, env: SimpleNetworkEnvelope) -> None:
        """Handle switch_team_request."""
        async with self.lock:
            player = self._get_player(env.player_id)
            if not player:
                return
            player_id = player.player_id

            if self.state != RoomState.Waiting:
                await self._send_command_result(player_id, False, "bad_state",
                                                 "Can only switch teams in Waiting state.")
                return

            payload = env.parse_payload()
            target_team_str = str(payload.get("target_team", "None"))
            try:
                target_team = Team(target_team_str)
            except ValueError:
                await self._send_command_result(player_id, False, "invalid_team",
                                                 f"Invalid team: {target_team_str}")
                return

            if target_team == Team.None_:
                await self._send_command_result(player_id, False, "invalid_team",
                                                 "Cannot switch to None team.")
                return

            # Remove from old team
            if player.team == Team.Red:
                self._red_players.discard(player_id)
            elif player.team == Team.Blue:
                self._blue_players.discard(player_id)

            # Add to new team
            player.team = target_team
            if target_team == Team.Red:
                self._red_players.add(player_id)
            else:
                self._blue_players.add(player_id)

            await self._send_command_result(player_id, True, "", f"Switched to {target_team.value}.")
            self._check_ready()

    async def handle_start_match(self, env: SimpleNetworkEnvelope) -> None:
        """Handle start_match_request."""
        async with self.lock:
            player = self._get_player(env.player_id)
            player_id = player.player_id if player else env.player_id

            if self.state not in (RoomState.Waiting, RoomState.Ready):
                await self._send_command_result(player_id, False, "bad_state",
                                                 "Match cannot be started now.")
                return

            if not self._red_players or not self._blue_players:
                await self._send_command_result(player_id, False, "not_enough_players",
                                                 "Both teams need at least one player.")
                return

            # Set countdown state and release lock before sleeping
            self.state = RoomState.Countdown
            await self._send_command_result(player_id, True, "", "Match starting!")

        # Countdown happens outside the lock (3 seconds)
        await self._start_countdown()

    async def _start_countdown(self) -> None:
        """Begin 3-second countdown before match start.
        Must NOT be called while holding self.lock.
        """
        logger.info(f"Room {self.room_id}: Countdown started")
        await asyncio.sleep(COUNTDOWN_DURATION)

        async with self.lock:
            # Reset match state
            self.state = RoomState.Playing
            self.remaining_time = self.config.match_time
            self.red_score = 0
            self.blue_score = 0
            self._match_start_time = time.monotonic()

            # Reset all players
            for p in self.players.values():
                p.state = PlayerState.Alive
                p.hp = p.max_hp
                p.carried_treasure_id = ""

            # Reset all treasures
            for t in self.treasures.values():
                t["state"] = TreasureState.Spawned.value
                t["carrier_player_id"] = ""
                if "spawn_position" in t:
                    t["position"] = dict(t["spawn_position"])

            # Start broadcast tasks
            self._start_broadcasts()

            # Start match timer
            self._match_task = asyncio.create_task(self._match_timer())

            logger.info(f"Room {self.room_id}: Match started! Duration: {self.config.match_time}s")

    async def _match_timer(self) -> None:
        """Tick the match clock."""
        try:
            while self.state == RoomState.Playing and self.remaining_time > 0:
                await asyncio.sleep(0.1)
                async with self.lock:
                    elapsed = time.monotonic() - self._match_start_time
                    self.remaining_time = max(0.0, self.config.match_time - elapsed)
                    if self.remaining_time <= 0:
                        self._finish_match()
                        break
        except asyncio.CancelledError:
            pass

    def _finish_match(self) -> None:
        """Transition to Finished state."""
        self.state = RoomState.Finished
        self.remaining_time = 0.0
        logger.info(f"Room {self.room_id}: Match finished! "
                     f"Red: {self.red_score}, Blue: {self.blue_score}")
        self._stop_broadcasts()

    # ── Pose handling ─────────────────────────────────────────────────

    def update_player_pose(self, env: SimpleNetworkEnvelope, addr: tuple[str, int]) -> None:
        """Update a player's position from a UDP player_pose message."""
        player = self._get_player(env.player_id)
        if not player:
            return

        payload = env.parse_payload()
        player.update_pose(
            x=float(payload.get("x", 0)),
            y=float(payload.get("y", 0)),
            z=float(payload.get("z", 0)),
            rotation_y=float(payload.get("rotation_y", 0)),
            pose_seq=int(payload.get("pose_seq", 0)),
        )

        # Register UDP address
        self._udp_addresses[player.player_id] = addr

    # ── Attack handling ───────────────────────────────────────────────

    async def handle_attack(self, env: SimpleNetworkEnvelope) -> None:
        """Handle attack_request."""
        async with self.lock:
            player = self._get_player(env.player_id)
            if not player:
                return
            player_id = player.player_id

            if self.state != RoomState.Playing:
                await self._send_command_result(player_id, False, "bad_state",
                                                 "Match is not in progress.")
                return

            payload = env.parse_payload()
            attack = AttackRequestPayload.from_dict(payload)
            result = self.combat.process_attack(player, attack)

            # Send result to attacker
            await self._send_command_result(player_id, result["ok"],
                                             result.get("error_code", ""),
                                             result.get("message", ""))

    # ── Treasure handling ─────────────────────────────────────────────

    async def handle_pickup(self, env: SimpleNetworkEnvelope) -> None:
        """Handle pickup_treasure_request."""
        async with self.lock:
            player = self._get_player(env.player_id)
            if not player:
                return
            player_id = player.player_id

            if self.state != RoomState.Playing:
                await self._send_command_result(player_id, False, "bad_state",
                                                 "Match is not in progress.")
                return

            payload = env.parse_payload()
            treasure_id = str(payload.get("treasure_id", ""))
            result = self.treasure_authority.process_pickup(player, treasure_id)
            await self._send_command_result(player_id, result["ok"],
                                             result.get("error_code", ""),
                                             result.get("message", ""))

    async def handle_submit(self, env: SimpleNetworkEnvelope) -> None:
        """Handle submit_treasure_request."""
        async with self.lock:
            player = self._get_player(env.player_id)
            if not player:
                return
            player_id = player.player_id

            if self.state != RoomState.Playing:
                await self._send_command_result(player_id, False, "bad_state",
                                                 "Match is not in progress.")
                return

            result = self.treasure_authority.process_submit(player)

            # If successful, update score
            if result["ok"] and "score" in result:
                if player.team == Team.Red:
                    self.red_score += result["score"]
                elif player.team == Team.Blue:
                    self.blue_score += result["score"]

            await self._send_command_result(player_id, result["ok"],
                                             result.get("error_code", ""),
                                             result.get("message", ""))

    # ── Disconnect handling ───────────────────────────────────────────

    async def handle_disconnect(self, client_or_server_id: str) -> None:
        """Handle a TCP disconnection."""
        async with self.lock:
            player = self._get_player(client_or_server_id)
            if not player:
                return
            player_id = player.player_id

            player.is_connected = False
            self._tcp_transports.pop(player_id, None)
            self._udp_addresses.pop(player_id, None)

            # Drop carried treasure
            if player.carried_treasure_id:
                tid = player.drop_treasure()
                if tid in self.treasures:
                    self.treasures[tid]["state"] = TreasureState.Spawned.value
                    self.treasures[tid]["carrier_player_id"] = ""
                    sp = self.treasures[tid].get("spawn_position")
                    if sp:
                        self.treasures[tid]["position"] = dict(sp)

            # Broadcast disconnect notice
            notice = DisconnectNoticePayload(player_id=player_id, reason="tcp_disconnected")
            await self._broadcast_tcp(MsgType.DISCONNECT_NOTICE, notice)

            logger.info(f"Player {player_id} disconnected from room {self.room_id}")

    # ── Broadcasting ──────────────────────────────────────────────────

    def _start_broadcasts(self) -> None:
        """Start periodic broadcast tasks."""
        if self._broadcast_running:
            return
        self._broadcast_running = True
        self._broadcast_match_task = asyncio.create_task(self._broadcast_match_loop())
        self._broadcast_pose_task = asyncio.create_task(self._broadcast_pose_loop())
        logger.info(f"Room {self.room_id}: Broadcasts started")

    def _stop_broadcasts(self) -> None:
        """Stop periodic broadcast tasks."""
        self._broadcast_running = False
        if self._broadcast_match_task:
            self._broadcast_match_task.cancel()
            self._broadcast_match_task = None
        if self._broadcast_pose_task:
            self._broadcast_pose_task.cancel()
            self._broadcast_pose_task = None
        logger.info(f"Room {self.room_id}: Broadcasts stopped")

    # ── Position logging ────────────────────────────────────────────────

    def start_position_logger(self) -> None:
        """Start the periodic player position logger."""
        if self._pos_log_running:
            return
        self._pos_log_running = True
        self._pos_log_task = asyncio.create_task(self._position_log_loop())
        logger.info(f"Room {self.room_id}: Position logger started")

    def stop_position_logger(self) -> None:
        """Stop the periodic player position logger."""
        self._pos_log_running = False
        if self._pos_log_task:
            self._pos_log_task.cancel()
            self._pos_log_task = None

    async def _position_log_loop(self) -> None:
        """Log all player positions every 2 seconds."""
        _pos_logger = logging.getLogger("position")
        try:
            while self._pos_log_running:
                await asyncio.sleep(2.0)
                async with self.lock:
                    if not self.players:
                        continue
                    now = time.monotonic()
                    lines = []
                    lines.append(f"--- POSITION LOG | Room: {self.room_id} | State: {self.state.value} | Players: {len(self.players)} ---")
                    for p in self.players.values():
                        age = now - p.last_pose_time if p.last_pose_time > 0 else -1
                        age_str = f"{age:.1f}s ago" if age >= 0 else "never"
                        lines.append(
                            f"  [{p.player_id}] {p.nickname:12s} | "
                            f"Team: {p.team.value:5s} | "
                            f"State: {p.state.value:13s} | "
                            f"HP: {p.hp:3d}/{p.max_hp} | "
                            f"Pos: ({p.position.x:+7.2f}, {p.position.y:+5.2f}, {p.position.z:+7.2f}) | "
                            f"RotY: {p.rotation_y:+6.1f} | "
                            f"PoseAge: {age_str} | "
                            f"Stale: {p.is_stale()} | "
                            f"Carrying: {p.carried_treasure_id or 'None'}"
                        )
                    _pos_logger.info("\n".join(lines))
        except asyncio.CancelledError:
            pass

    async def _broadcast_match_loop(self) -> None:
        """Broadcast match_snapshot at 10Hz over TCP."""
        try:
            while self._broadcast_running:
                await asyncio.sleep(BROADCAST_MATCH_INTERVAL)
                async with self.lock:
                    await self._send_match_snapshot()
        except asyncio.CancelledError:
            pass

    async def _broadcast_pose_loop(self) -> None:
        """Broadcast pose_snapshot at 20Hz over TCP and UDP."""
        try:
            while self._broadcast_running:
                await asyncio.sleep(BROADCAST_POSE_INTERVAL)
                async with self.lock:
                    await self._send_pose_snapshot()
        except asyncio.CancelledError:
            pass

    async def _send_match_snapshot(self) -> None:
        """Build and broadcast match_snapshot."""
        player_snapshots = []
        for p in self.players.values():
            player_snapshots.append(PlayerSnapshot(
                player_id=p.player_id,
                nickname=p.nickname,
                team=p.team.value,
                state=p.state.value,
                hp=p.hp,
                max_hp=p.max_hp,
                is_connected=p.is_connected,
                carried_treasure_id=p.carried_treasure_id,
                position=p.position,
                rotation_y=p.rotation_y,
            ))

        treasure_snapshots = []
        for t in self.treasures.values():
            treasure_snapshots.append(TreasureSnapshot(
                treasure_id=t["treasure_id"],
                treasure_type=t["treasure_type"],
                state=t["state"],
                score_value=t["score_value"],
                position=Vec3(t["position"]["x"], t["position"]["y"], t["position"]["z"]),
                carrier_player_id=t["carrier_player_id"],
            ))

        payload = MatchSnapshotPayload(
            room_id=self.room_id,
            room_state=self.state.value,
            remaining_time=self.remaining_time,
            red_score=self.red_score,
            blue_score=self.blue_score,
            players=player_snapshots,
            treasures=treasure_snapshots,
        )

        await self._broadcast_tcp(MsgType.MATCH_SNAPSHOT, payload)

    async def _send_pose_snapshot(self) -> None:
        """Build and broadcast pose_snapshot over TCP and UDP."""
        now_ms = int(time.time() * 1000)

        pose_players = []
        for p in self.players.values():
            pose_players.append(PosePlayerInfo(
                player_id=p.player_id,
                position=p.position,
                rotation_y=p.rotation_y,
                server_time_ms=now_ms,
                stale=p.is_stale(),
            ))

        payload = PoseSnapshotPayload(
            room_id=self.room_id,
            players=pose_players,
        )

        # Broadcast over TCP
        await self._broadcast_tcp(MsgType.POSE_SNAPSHOT, payload)

        # Broadcast over UDP
        self._broadcast_udp(MsgType.POSE_SNAPSHOT, payload)

    # ── Send helpers ──────────────────────────────────────────────────

    def _send_tcp(self, transport: asyncio.Transport, msg_type: str, payload) -> None:
        """Send a message to a single TCP transport."""
        from protocol.framing import encode_frame

        env = SimpleNetworkEnvelope.response(msg_type, self.room_id, payload, self._next_seq())
        try:
            frame = encode_frame(env.to_json_bytes())
            transport.write(frame)
        except Exception as e:
            logger.error(f"Error sending TCP to transport: {e}")

    async def _broadcast_tcp(self, msg_type: str, payload) -> None:
        """Broadcast a message to all connected TCP clients."""
        from protocol.framing import encode_frame

        env = SimpleNetworkEnvelope.response(msg_type, self.room_id, payload, self._next_seq())
        frame = encode_frame(env.to_json_bytes())

        for player_id, transport in list(self._tcp_transports.items()):
            try:
                transport.write(frame)
            except Exception as e:
                logger.error(f"Error broadcasting TCP to {player_id}: {e}")

    def _broadcast_udp(self, msg_type: str, payload) -> None:
        """Broadcast a message to all known UDP clients."""
        if not self._udp_transport:
            return

        env = SimpleNetworkEnvelope.response(msg_type, self.room_id, payload, self._next_seq())
        json_bytes = env.to_json_bytes()

        for player_id, addr in list(self._udp_addresses.items()):
            try:
                self._udp_transport.sendto(json_bytes, addr)
            except Exception as e:
                logger.error(f"Error broadcasting UDP to {player_id}: {e}")

    async def _send_command_result(
        self, player_id: str, ok: bool, error_code: str, message: str
    ) -> None:
        """Send a command_result to a specific player."""
        transport = self._tcp_transports.get(player_id)
        if not transport:
            return
        payload = CommandResultPayload(ok=ok, error_code=error_code, message=message)
        self._send_tcp(transport, MsgType.COMMAND_RESULT, payload)

    # ── State helpers ─────────────────────────────────────────────────

    def _check_ready(self) -> None:
        """Check if the room should transition to Ready."""
        if self.state == RoomState.Waiting:
            if len(self._red_players) > 0 and len(self._blue_players) > 0:
                self.state = RoomState.Ready
                logger.info(f"Room {self.room_id}: Ready (Red: {len(self._red_players)}, "
                            f"Blue: {len(self._blue_players)})")

    # ── Transport registration ────────────────────────────────────────

    def set_udp_transport(self, transport: asyncio.DatagramTransport) -> None:
        self._udp_transport = transport

    def get_tcp_transport(self, player_id: str) -> asyncio.Transport | None:
        return self._tcp_transports.get(player_id)
