# QA Strategy

## Functional
- Combat
- Inventory
- Loot
- Quests
- Extraction
- Progression
- UI

## Multiplayer
- 2 players
- 3 players
- multiple teams
- PvE + PvP
- boss + players
- simultaneous loot

### Local Setup
- Second editor: ParrelSync clone `PF-Multiplayer-Game_clone_0` (argument `client_0`); clones keep their own account (`clone<N>.` PlayerPrefs prefix) and start without characters.
- Clone as server: argument containing `server` (optionally `solo` / `trio`) makes it a dedicated server in the editor.
- Dedicated path: build the server first (`Tools/Game/Network/Build Dedicated Server`); without it or with a stale build the first player hosts.
- Photon fixed region `eu`, so both editors meet.

### Recipes
- Solo vs solo: both register for Solo → same dungeon, different houses.
- Party: create a party, join by code from the clone, Trio queue → one session, one house.
- Late join: a second player registering within 180 s of the first joins the same dungeon; after the window a new one is opened.
- Portals: escape portals show up at 60 s (pedestal + map mark, hold F, walk in); the cellar grate opens at half the clock and drops into the next floor's session. For quick tests force them via `DungeonDirector.SetEscapePortals` / `SetDescendPortal(0, true)`.
- Descend: health, charges and kit arrive on floor 2; teammates descending later land in the same session.

## Network
- high latency
- packet loss
- disconnect
- reconnect
- late join (180 s window, see Recipes)
- host fallback: the dungeon ends when the hosting player leaves

## Security
- duplication
- currency manipulation
- inventory injection
- movement manipulation
- damage manipulation
- extraction abuse
