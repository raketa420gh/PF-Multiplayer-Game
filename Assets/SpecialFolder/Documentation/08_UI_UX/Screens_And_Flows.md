# Screens & Flows

## Screens
- Main Menu
- Lobby
- Character
- Inventory
- Equipment
- Quest
- Shop
- Matchmaking
- Loading
- HUD
- Map
- Death
- Extraction Result
- Post-Raid
- Settings

## Core Flow
```text
Main Menu → Lobby → Loadout → Matchmaking → Raid → Result → Stash/Upgrade → Lobby
```

## Prototype Flow (current)
```text
LobbyScene (Home / Skills / Stash / Merchants) → Loading → DungeonScene | BattleScene → Result → LobbyScene
```
- Каждая сцена — отдельная Fusion-сессия; переход через `SceneTravel.Load`, экран загрузки — `LoadingView`.
- Кит, стеш и профиль передаются через PlayerPrefs; `BattleScene` ничего не сохраняет.

