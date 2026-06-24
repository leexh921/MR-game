"""TCP frame codec: 4-byte big-endian length prefix + UTF-8 JSON body."""

import struct
from .types import MAX_FRAME_SIZE


def encode_frame(json_bytes: bytes) -> bytes:
    """Encode JSON bytes into a TCP frame with 4-byte BE length prefix.

    Args:
        json_bytes: UTF-8 encoded JSON payload.

    Returns:
        Length-prefixed frame bytes ready to send over TCP.
    """
    length = len(json_bytes)
    header = struct.pack(">I", length)  # big-endian uint32
    return header + json_bytes


def decode_frame_header(data: bytes) -> int:
    """Decode the 4-byte BE length header.

    Args:
        data: Exactly 4 bytes of the length header.

    Returns:
        Payload length in bytes.

    Raises:
        ValueError: If the length exceeds MAX_FRAME_SIZE.
    """
    if len(data) < 4:
        raise ValueError(f"Need 4 bytes for header, got {len(data)}")
    length = struct.unpack(">I", data)[0]
    if length > MAX_FRAME_SIZE:
        raise ValueError(f"Frame too large: {length} bytes (max {MAX_FRAME_SIZE})")
    return length


class FrameBuffer:
    """Stateful buffer that accumulates TCP bytes and yields complete frames.

    Usage::

        buf = FrameBuffer()
        for frame_bytes in buf.feed(incoming_data):
            handle(frame_bytes)
    """

    def __init__(self):
        self._buffer = bytearray()

    def feed(self, data: bytes) -> list[bytes]:
        """Feed received data into the buffer. Returns list of complete frame bodies."""
        self._buffer.extend(data)
        frames = []

        while True:
            if len(self._buffer) < 4:
                break

            try:
                payload_len = struct.unpack(">I", bytes(self._buffer[:4]))[0]
            except struct.error:
                # Malformed header, discard
                self._buffer = bytearray()
                break

            if payload_len > MAX_FRAME_SIZE:
                # Protocol violation — discard everything
                self._buffer = bytearray()
                break

            total_needed = 4 + payload_len
            if len(self._buffer) < total_needed:
                break

            # Extract the JSON body
            frame_body = bytes(self._buffer[4:total_needed])
            frames.append(frame_body)

            # Remove processed bytes
            self._buffer = self._buffer[total_needed:]

        return frames

    def reset(self) -> None:
        """Clear the buffer (e.g., on connection close)."""
        self._buffer = bytearray()
