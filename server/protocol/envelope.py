"""SimpleNetworkEnvelope — the universal message wrapper for both TCP and UDP."""

from __future__ import annotations
import json
import time
from dataclasses import dataclass, field
from typing import Any, Optional
from .types import PROTOCOL_VERSION


@dataclass
class SimpleNetworkEnvelope:
    protocol_version: int = PROTOCOL_VERSION
    type: str = ""
    seq: int = 0
    room_id: str = ""
    player_id: str = ""
    sent_at_ms: int = 0
    payload_json: str = "{}"

    # ── Serialization ────────────────────────────────────────────────

    def to_json_bytes(self) -> bytes:
        """Serialize envelope to UTF-8 JSON bytes."""
        d = {
            "protocol_version": self.protocol_version,
            "type": self.type,
            "seq": self.seq,
            "room_id": self.room_id,
            "player_id": self.player_id,
            "sent_at_ms": self.sent_at_ms,
            "payload_json": self.payload_json,
        }
        return json.dumps(d, ensure_ascii=False).encode("utf-8")

    @classmethod
    def from_json_bytes(cls, data: bytes) -> SimpleNetworkEnvelope:
        """Deserialize envelope from UTF-8 JSON bytes."""
        d = json.loads(data.decode("utf-8"))
        return cls(
            protocol_version=int(d.get("protocol_version", 1)),
            type=str(d.get("type", "")),
            seq=int(d.get("seq", 0)),
            room_id=str(d.get("room_id", "")),
            player_id=str(d.get("player_id", "")),
            sent_at_ms=int(d.get("sent_at_ms", 0)),
            payload_json=json.dumps(d.get("payload_json", {}), ensure_ascii=False)
            if isinstance(d.get("payload_json"), dict)
            else str(d.get("payload_json", "{}")),
        )

    # ── Payload helpers ──────────────────────────────────────────────

    def parse_payload(self) -> dict:
        """Parse payload_json into a dict."""
        return json.loads(self.payload_json)

    def set_payload(self, payload: Any) -> None:
        """Set payload_json from a dict or dataclass."""
        if hasattr(payload, "to_dict"):
            self.payload_json = json.dumps(payload.to_dict(), ensure_ascii=False)
        elif isinstance(payload, dict):
            self.payload_json = json.dumps(payload, ensure_ascii=False)
        else:
            self.payload_json = json.dumps(payload, ensure_ascii=False, default=str)

    # ── Builder for server responses ─────────────────────────────────

    @classmethod
    def response(cls, msg_type: str, room_id: str, payload: Any, seq: int = 0) -> SimpleNetworkEnvelope:
        """Create a server-to-client response envelope."""
        env = cls(
            protocol_version=PROTOCOL_VERSION,
            type=msg_type,
            seq=seq,
            room_id=room_id,
            player_id="server",
            sent_at_ms=int(time.time() * 1000),
        )
        env.set_payload(payload)
        return env
