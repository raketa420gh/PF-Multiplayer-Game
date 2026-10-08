# Unity Project Structure

```text
Assets/
├── _Project/
│   ├── Art/
│   ├── Audio/
│   ├── Animations/
│   ├── Materials/
│   ├── Prefabs/
│   ├── Scenes/
│   ├── Scripts/
│   ├── ScriptableObjects/
│   ├── UI/
│   ├── VFX/
│   └── Data/
├── Plugins/
└── ThirdParty/
```

## Assembly Definitions
`[ ]`

## Scene Conventions
`[ ]`

## Prefab Conventions
`[ ]`

## Current Layout (prototype)
```text
Assets/Game/
├── Animations, Audio, Configs, Materials, Meshes, Prefabs, Textures   # генерируются билдерами
├── Scenes/            # LobbyScene, DungeonScene, BattleScene, SampleScene, AnimationEditor (см. Animation_Test_Scene.md)
└── Scripts/
    ├── Battle/ Dungeon/ Player/ GameObjects/ System/ Common/
    └── Editor/{Battle,Dungeon}/   # билдеры контента, меню Tools/Game/*
```

