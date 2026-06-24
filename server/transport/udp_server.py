"""UDP server: asyncio.DatagramProtocol for handling UDP messages on port 7778.

Frame format: raw UTF-8 JSON (no length prefix).
"""

from __future__ import annotations
import asyncio
import logging
from typing import TYPE_CHECKING

from protocol.envelope import SimpleNetworkEnvelope

if TYPE_CHECKING:
    from router import MessageRouter

logger = logging.getLogger(__name__)


class UdpGameProtocol(asyncio.DatagramProtocol):
    """Single-instance UDP protocol for all clients.

    Handles player_pose messages at ~20Hz per client.
    """

    def __init__(self, router: MessageRouter):
        self.router = router
        self.transport: asyncio.DatagramTransport | None = None

    def connection_made(self, transport: asyncio.BaseTransport) -> None:
        self.transport = transport  # type: asyncio.DatagramTransport
        # Register with room for outgoing broadcasts
        self.router.room.set_udp_transport(self.transport)
        logger.info("UDP endpoint ready")

    def datagram_received(self, data: bytes, addr: tuple[str, int]) -> None:
        """Called when a UDP datagram arrives."""
        try:
            json_str = data.decode("utf-8")
            env = SimpleNetworkEnvelope.from_json_bytes(data)
        except Exception as e:
            logger.debug(f"Failed to parse UDP datagram from {addr}: {e}")
            return

        # Route synchronously (UDP callbacks are not async)
        self.router.route_udp(env, addr)

    def error_received(self, exc: Exception) -> None:
        logger.error(f"UDP error: {exc}")

    def connection_lost(self, exc: Exception | None) -> None:
        logger.info("UDP endpoint closed")


async def start_udp_server(host: str, port: int, router: MessageRouter) -> asyncio.DatagramTransport:
    """Start the UDP listener on the given host:port.

    Returns the DatagramTransport for sending.
    """
    loop = asyncio.get_event_loop()
    transport, protocol = await loop.create_datagram_endpoint(
        lambda: UdpGameProtocol(router),
        local_addr=(host, port),
    )
    logger.info(f"UDP server listening on {host}:{port}")
    return transport
