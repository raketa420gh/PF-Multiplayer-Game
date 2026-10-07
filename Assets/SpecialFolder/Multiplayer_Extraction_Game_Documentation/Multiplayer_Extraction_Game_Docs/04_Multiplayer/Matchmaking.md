# Matchmaking

Code: `Assets/Game/Scripts/Dungeon/Network` (`DungeonTicket`, `DungeonAdmission`, `GameServer`, `PartyService`, `FloorTransfer`). See DD-011.

## Modes
### Solo
Solo adventurers only; a party cannot register. Up to 8 players per dungeon, every player is a team of one.

### Trio
Teams of up to 3 (a party of two may register; a lone player is a team of one). Up to 12 players per dungeon. Members of one party share a team and a spawn house.

## Flow
1. Tavern: the party is a hidden Fusion session `party-<code>` (four-digit code, lobby `parties`, capacity 3, `AutoHostOrClient` — the first member back from a run hosts it). Join by code.
2. The leader picks the queue and starts: `PlayerSessionComponent.RpcQueue` → `RpcDepart` → `PartyService.Depart` sends every member to `DungeonScene` with the same `DungeonTicket` (queue, party id, size).
3. Leader / solo: joins custom lobby `dungeons` and takes the fullest open session of the queue that has room for the whole party; members wait (up to 90 s) for a session listing their party id, then join it.
4. No session → a new dungeon server is allocated (dev: `LocalServer` starts `Builds/DedicatedServer/DungeonServer.exe`); without the build (or with a stale one in the editor) the player hosts the dungeon itself (Host fallback).
5. An idle server (queue `Any`, floor 0) takes the queue and floor of its first player.

## Late-Join Window
- Opens with the first player: 180 s (`GameServer.LateJoinWindow`). Until it closes the session stays open and listed; then `IsOpen = false`, nobody else gets in.
- Sessions with less than 10 s of window left are skipped.
- The loading screen shows how many are inside and when late entry closes.

## Deeper Floors
Every floor is its own session. A descend portal hands the adventurer over to `DungeonTicket.Deeper(floor)` (same queue and party, size 1). On a deeper floor the session listing the player's party is preferred, else the fullest open one — players from different upper dungeons may or may not meet. Health, spell charges, run kills and XP ride along; the kit is saved like after an escape. Status effects and resource stacks are not carried.

## Session Properties
- Region: Photon fixed region `eu` (dev).
- Map: — (one dungeon).
- Mode: `mode` — `QueueMode` (Solo 0, Trio 1, Any 255).
- Party size: in the connection token (7 bytes: mode, size, party id int32, floor).
- `until`: unix time the late-join window closes (0 = no player yet).
- `parties`: `,id,id,` — parties inside, for members and descending teammates.
- `floor`: floor of the session (0 = idle server).
- Version / Build / Ruleset: `[ ]`

## Rules
- Maximum players: Solo 8, Trio 12 (`GameServer.Capacity`).
- Team count: Solo — one per player; Trio — one per party.
- Ping: `[ ]` (single fixed region for now).
- Skill matching: none.
- Gear matching: none.
- Queue timeout: 4 s to find a session (leader), 90 s to boot a server, 90 s for members to see their party's session.

## Failure Cases
- Session full or just closed (`GameIsFull` / `GameClosed`): search again, up to 3 attempts, then back to the tavern with a notice.
- Leader never reached a dungeon: members return to the tavern ("The party leader did not reach a dungeon").
- Party session full or gone: back to the tavern, party forgotten.
- Way down failed: back to the tavern with the kit saved as after an escape (`FloorTransfer.Forget`).
- Host fallback: the session dies when the hosting player leaves; a host who descends ends its floor for everyone.
