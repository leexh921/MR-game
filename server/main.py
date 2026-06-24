"""
Game Server — Main Entry Point

Implements a multiplayer game server communicating with Unity clients:
- TCP :7777 — Reliable commands (join, attack, pickup, submit, etc.)
- UDP :7778 — High-frequency player pose updates (20Hz)

Protocol: 4-byte BE length prefix + UTF-8 JSON (TCP), raw UTF-8 JSON (UDP)
"""

import asyncio
import logging
import signal
import sys

from config import load_config
from game.room import Room
from router import MessageRouter
from transport.tcp_server import start_tcp_server
from transport.udp_server import start_udp_server

# ── Logging setup ────────────────────────────────────────────────────

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
    datefmt="%H:%M:%S",
)
logger = logging.getLogger("server")

# ── Constants ────────────────────────────────────────────────────────

TCP_HOST = "0.0.0.0"
TCP_PORT = 7777
UDP_HOST = "0.0.0.0"
UDP_PORT = 7778
CONFIG_PATH = "mapdata.json"


async def main() -> None:
    """Initialize and run the game server."""
    logger.info("=== Game Server Starting ===")

    # Load configuration
    config = load_config(CONFIG_PATH)
    logger.info(f"Loaded config: room='{config.room_id}', map='{config.map_id}', "
                f"mode='{config.game_mode}', max_players={config.max_players}")

    # Create room and router
    room = Room(config)
    router = MessageRouter(room)

    # Start position logger (prints player positions every 2s)
    room.start_position_logger()

    # Start TCP server
    tcp_server = await start_tcp_server(TCP_HOST, TCP_PORT, router)

    # Start UDP server
    udp_transport = await start_udp_server(UDP_HOST, UDP_PORT, router)

    logger.info(f"Server ready! TCP: {TCP_HOST}:{TCP_PORT}, UDP: {UDP_HOST}:{UDP_PORT}")
    logger.info(f"Room '{config.room_id}' waiting for players...")

    # Handle graceful shutdown
    stop_event = asyncio.Event()

    def shutdown_handler(sig, frame):
        logger.info(f"Received signal {sig}, shutting down...")
        stop_event.set()

    # Register signal handlers
    try:
        loop = asyncio.get_event_loop()
        for sig in (signal.SIGINT, signal.SIGTERM):
            loop.add_signal_handler(sig, lambda s=sig: shutdown_handler(s, None))  # type: ignore[misc]
    except NotImplementedError:
        # Windows doesn't support add_signal_handler gracefully
        signal.signal(signal.SIGINT, shutdown_handler)
        signal.signal(signal.SIGTERM, shutdown_handler)

    # Run until stopped
    try:
        await stop_event.wait()
    except KeyboardInterrupt:
        logger.info("Keyboard interrupt received")

    # Cleanup
    logger.info("Shutting down...")
    room._stop_broadcasts()
    room.stop_position_logger()

    tcp_server.close()
    await tcp_server.wait_closed()

    udp_transport.close()

    logger.info("Server stopped.")


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        pass
    sys.exit(0)
