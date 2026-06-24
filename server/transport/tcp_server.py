"""TCP server: asyncio.Protocol for handling client connections on port 7777.

Frame format: 4-byte big-endian length prefix + UTF-8 JSON body.
"""

from __future__ import annotations
import asyncio
import logging
from typing import TYPE_CHECKING

from protocol.framing import FrameBuffer
from protocol.envelope import SimpleNetworkEnvelope

if TYPE_CHECKING:
    from router import MessageRouter

logger = logging.getLogger(__name__)


class TcpGameProtocol(asyncio.Protocol):
    """Per-connection TCP protocol handler.

    Each connected game client gets one instance of this protocol.
    """

    def __init__(self, router: MessageRouter):
        self.router = router
        self.buffer = FrameBuffer()
        self.transport: asyncio.Transport | None = None
        self._player_id: str = ""
        self._closed: bool = False

    def connection_made(self, transport: asyncio.BaseTransport) -> None:
        self.transport = transport  # type: asyncio.Transport
        peername = self.transport.get_extra_info("peername")
        logger.info(f"TCP connection from {peername}")

    def data_received(self, data: bytes) -> None:
        """Called when data arrives. Accumulates and extracts complete frames."""
        try:
            frames = self.buffer.feed(data)
            for frame_bytes in frames:
                self._handle_frame(frame_bytes)
        except Exception as e:
            logger.error(f"Error processing TCP data: {e}", exc_info=True)
            self._close()

    def _handle_frame(self, frame_bytes: bytes) -> None:
        """Process a complete frame: parse envelope, extract player_id, route."""
        try:
            env = SimpleNetworkEnvelope.from_json_bytes(frame_bytes)
        except Exception as e:
            logger.error(f"Failed to parse envelope: {e}")
            return

        # Track player_id from first join or subsequent messages
        if env.type == "join_room_request" or not self._player_id:
            self._player_id = env.player_id

        # Schedule the async handler on the event loop
        asyncio.ensure_future(self._dispatch(env))

    async def _dispatch(self, env: SimpleNetworkEnvelope) -> None:
        """Dispatch the envelope to the router."""
        if self._closed or not self.transport:
            return
        try:
            await self.router.route_tcp(env, self.transport)
        except Exception as e:
            logger.error(f"Error dispatching TCP message: {e}", exc_info=True)

    def connection_lost(self, exc: Exception | None) -> None:
        """Clean up when the client disconnects."""
        self._closed = True
        if exc:
            logger.info(f"TCP connection lost with error: {exc}")
        else:
            logger.info(f"TCP connection closed cleanly")

        # Notify room of disconnect
        if self._player_id:
            asyncio.ensure_future(self.router.room.handle_disconnect(self._player_id))

        self.buffer.reset()

    def _close(self) -> None:
        """Force-close the connection."""
        if self.transport and not self._closed:
            self._closed = True
            self.transport.close()


class TcpServerFactory:
    """Factory for creating TcpGameProtocol instances."""

    def __init__(self, router: MessageRouter):
        self.router = router

    def __call__(self) -> TcpGameProtocol:
        return TcpGameProtocol(self.router)


async def start_tcp_server(host: str, port: int, router: MessageRouter) -> asyncio.AbstractServer:
    """Start the TCP server on the given host:port."""
    factory = TcpServerFactory(router)
    server = await asyncio.get_event_loop().create_server(factory, host, port)
    logger.info(f"TCP server listening on {host}:{port}")
    return server
