# Large Space Map Workflow

## Coordinate Rule

All runtime gameplay positions are shared-space coordinates. The shared-space origin is the PICO shared spatial anchor pose. Place map objects, team bases, and treasure points relative to that origin, using Unity meters.

Do not treat each Pico device's local XR origin as map space. Player poses are converted into shared-space before being sent to the server.

## Minimum Playable Layout

For a clean two-player test map:

- Add one `TeamBase_Red` marker and one `TeamBase_Blue` marker.
- Add at least one `Treasure_Normal`, `Treasure_Rare`, or `Treasure_Final` marker.
- Keep the first test compact: red and blue bases around 4-8 meters apart, treasures between them.
- Add simple obstacle prefabs only after pickup, submit, and player visibility are verified.

## Export

Use the existing map editor/export flow. The exported JSON must end up at:

```text
Assets/_Project/StreamingAssets/Maps/<map_id>.json
```

If exporting on Pico, copy the exported file from:

```text
/storage/emulated/0/Download/TreasureArenaMR/MapExports/<map_id>.json
```

into the Unity project path above.

## Registry

When adding or replacing map object prefabs, run:

```text
Tools > TreasureArena > 地图编辑器 > 重建 Map Prefab Registry
```

Then rebuild the Pico APK so the same prefabs and map JSON are packaged on device.

## Runtime Checks

Server Console should show:

```text
[RoomManager] MapData loaded treasures=<count>
[SharedSpace] anchor owner assigned player=<player_id>
[SharedSpace] anchor_ready player=<player_id> ready=True
```

Pico logcat should show:

```text
[SharedSpace] anchor ready uuid=<uuid>
[PicoMapSync] MapRoot attached to SharedSpaceRoot
[SimpleRemotePlayer] spawned player=<player_id>
[TreasurePresenter] spawned treasure=<treasure_id>
```
