# Fusion Architecture

## Topology
- Dungeon: dedicated server (`GameMode.Server`, no local player), one session per floor (DD-011). Development fallback: the first player hosts (`GameMode.Host`, same server code with a local player).
- Tavern: party session `party-<code>` (`AutoHostOrClient`, hidden, 3 players, the first member back hosts); outside a party the tavern starts with the scene defaults.
- Launch: a `LaunchPlan` (`DungeonTicket`, `ServerLaunch`, `PartyLaunch`) is handed between scenes by `NetworkLaunch` and started by `BattleBootstrapper`.
- Server process: `GameServer` reads `-dedicatedServer -session <name> -mode <QueueMode> -floor <n>` (`-account <name>` for client PlayerPrefs); a dedicated server goes straight to `DungeonScene`, 60 fps target.

## Authority
- Player:
- Combat:
- AI:
- Loot:
- Inventory:
- Extraction: server — portal zones pass adventurers on the state authority (`PortalComponent.FixedUpdateNetwork`).
- Match state: server — `DungeonDirector` (floor clock, portals, spawns), `DungeonAdmission` (queue, teams, late-join window, session properties).

## Simulation
- Tick rate:
- Fixed update:
- State authority:
- Input authority:

## Prediction
`[ ]`

## Lag Compensation
`[ ]`

## Interest Management
`[ ]`

## Disconnect / Reconnect
- A leaving player is dropped from its team; no reconnect yet.
- Dedicated server lifecycle: an allocated server (`-session`) quits when its closed dungeon empties or nobody comes within 120 s; a long-lived one opens a fresh dungeon.
- Host fallback: the session ends when the host leaves.
