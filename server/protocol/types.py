"""All enumerated types and constants for the game protocol."""

from enum import Enum


class RoomState(str, Enum):
    Waiting = "Waiting"
    Ready = "Ready"
    Countdown = "Countdown"
    Playing = "Playing"
    Finished = "Finished"


class Team(str, Enum):
    None_ = "None"
    Red = "Red"
    Blue = "Blue"

    @classmethod
    def _missing_(cls, value):
        if value == "None" or value is None:
            return cls.None_
        return None


class PlayerState(str, Enum):
    Alive = "Alive"
    GhostRetreat = "GhostRetreat"
    Respawning = "Respawning"


class TreasureType(str, Enum):
    Normal = "Normal"
    Rare = "Rare"
    Final = "Final"


class TreasureState(str, Enum):
    Spawned = "Spawned"
    Carried = "Carried"
    Dropped = "Dropped"
    Submitted = "Submitted"


# Message type strings
class MsgType:
    # Client -> Server (TCP)
    JOIN_ROOM_REQUEST = "join_room_request"
    SWITCH_TEAM_REQUEST = "switch_team_request"
    START_MATCH_REQUEST = "start_match_request"
    PICKUP_TREASURE_REQUEST = "pickup_treasure_request"
    SUBMIT_TREASURE_REQUEST = "submit_treasure_request"
    ATTACK_REQUEST = "attack_request"
    HEARTBEAT = "heartbeat"

    # Client -> Server (UDP)
    PLAYER_POSE = "player_pose"

    # Server -> Client (TCP)
    JOIN_ROOM_RESULT = "join_room_result"
    COMMAND_RESULT = "command_result"
    MATCH_SNAPSHOT = "match_snapshot"
    POSE_SNAPSHOT = "pose_snapshot"
    DISCONNECT_NOTICE = "disconnect_notice"


# Protocol constants
PROTOCOL_VERSION = 1
MAX_FRAME_SIZE = 256 * 1024  # 256 KB

# Timing constants (seconds)
MATCH_DURATION = 300
BROADCAST_MATCH_INTERVAL = 0.1    # 10Hz
BROADCAST_POSE_INTERVAL = 0.05    # 20Hz
COUNTDOWN_DURATION = 3
RESPAWN_COUNTDOWN = 5
POSE_STALE_THRESHOLD = 1.0        # 1 second

# Gameplay constants
PICKUP_DISTANCE = 2.0             # meters
SUBMIT_DISTANCE = 3.0             # meters
HIT_RADIUS = 0.5                  # meters
