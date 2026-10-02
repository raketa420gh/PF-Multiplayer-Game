# Dark and Darker — прототип‑клон (ветка `special`)

Прототип extraction‑dungeon по мотивам Dark and Darker на Photon Fusion 2 (host authority) поверх боевой системы из ветки `battle`.
Все ассеты (анимации, меши, текстуры, материалы, конфиги, префабы, сцена, NavMesh) **генерируются редакторскими билдерами**; руками в сгенерированных ассетах ничего не правится.

## Сборка и запуск

| Меню | Что делает |
|---|---|
| `Tools/Game/Dungeon/Build All` | Анимации (весь каталог оружия) → звуки → текстуры → battle‑контент (оружие, Fighter) → dungeon‑контент (предметы, классы, способности, монстры, лут, префабы) → сцена `DungeonScene` |
| `Tools/Game/Dungeon/Build Textures` | Процедурные текстуры (альбедо + нормали) в `Assets/Game/Textures/Dungeon` |
| `Tools/Game/Dungeon/Build Content` | Конфиги и сетевые префабы в `Assets/Game/Configs/Dungeon`, `Assets/Game/Prefabs/Dungeon` |
| `Tools/Game/Dungeon/Build Scene` | `Assets/Game/Scenes/DungeonScene.unity`: два этажа 3×3, свет, NavMesh, UI |

После добавления новых сетевых префабов Fusion может потребовать `Fusion.Editor.NetworkProjectConfigUtilities.RebuildPrefabTable()` (ошибка «guid failed to be translated into a prefab id»).

Запуск: открыть `DungeonScene`, Play. Сессия `Dungeon`, `AutoHostOrClient`, до 6 игроков. Второй клиент — через ParrelSync.

## Игровой цикл (тестовая сцена)

1. **Таверна** (лобби): выбор класса (10 классов DaD), кит (paper‑doll + сумка 10×4), сташ 12×5, профиль (уровень/XP/перки). Кит, сташ и профиль сохраняются локально (PlayerPrefs) и загружаются на хост чанками RPC.
2. **Вход в подземелье**: хост спавнит `Adventurer` с копией кита в одной из spawn‑комнат этажа 1. Первый вошедший запускает матч (12 мин), заселение контейнеров лутом и спавн монстров.
3. **Этаж 1** 3×3 (Spawn ×2, Hall, Crypt, Throne, Library, Armory, Treasury, Shrine): сундуки, гробы, бочки, ящики, книжные полки, двери (одна заперта — отмычка или рычаг в Armory), ловушка‑шипы, алтари, красный **портал спуска** в Treasury (открывается на 60‑й секунде).
4. **Этаж 2** 3×3 (Arrival, Crypt ×2, BonePit, TrapCorridor, Throne, Library, Treasury, Shrine): качающиеся лезвия, шипы, рычаг обезвреживания, больше монстров.
5. **Dark Swarm**: безопасный круг на каждом этаже сжимается стадиями (150/330/510/660 с), вне круга — периодический урон, последняя минута — урон везде, по таймеру — смерть.
6. **Синие порталы побега** открываются на 120‑й секунде (по 2–3 на этаж), удержание F 3 с → экстракция: инвентарь копируется в кит, +XP, экран результата, возврат в таверну.
7. **Смерть**: труп остаётся лутабельным контейнером со всей экипировкой, кит обнуляется, экран «YOU DIED».
8. После матча хост сбрасывает подземелье (деспавн монстров, перезаполнение контейнеров при следующем входе).

## Управление

WASD — бег (спринта нет, как в DaD), Shift — тихий шаг (×0.4), Ctrl/C — присед (×0.65, уклонение корпусом), Space — прыжок, ЛКМ — атака / натяжение лука, ПКМ — блок, 1/2 — наборы оружия, Tab/I — инвентарь (drag&drop, ПКМ — быстрое действие, Shift+ЛКМ — перенос в сундук/сташ), F — взаимодействие (удержание), Q/E — навыки, Z/X/V/R/T — заклинания, 5–8 — пояс, G — отдых (1 HP / 2 с), H — помощь, Esc — курсор.

## Механики DaD, которые реализованы

- Атрибуты STR/VIG/AGI/DEX/WILL/KNOW/RES и кусочно‑линейные кривые wiki (`DungeonFormulas`): HP, Physical/Magical Power → бонус урона, Armor Rating → PDR (cap 65 %), Will → MR → MDR, Agility → Move Speed (base 300, cap 330), Action Speed, Interaction Speed, Cast Speed, Buff Duration.
- Экипировка 17 слотов (голова, грудь, руки, ноги, ступни, плащ, ожерелье, 2 кольца, 2 набора оружия main/off, 4 ячейки пояса). Двуручное занимает обе руки; щит работает в паре с одноручным (комбо‑конфиг «меч+щит», «булава+щит»). Штрафы брони/оружия к скорости, бонусы ботинок.
- Редкость Poor…Unique с цветами и бонусом к урону/броне по тиру; лут‑таблицы (rarity roll → item roll) по типам контейнеров.
- 13 видов оружия (меч, фальшион, длинный меч, цвайхендер, боевой топор, копьё, булава, кинжал, лук, арбалет, посох, факел, круглый щит) со своими анимациями, трассировкой клинка, комбо и блоком; кулаки без оружия.
- Хедшоты ×1.5, ноги ×0.6, блок с углом, частичный блок, стаггер, деflect о стены.
- Расходники: бинт, зелье лечения (HoT), зелье защиты (щит), хирургический набор, эль; утилиты: метательные топор/нож, набор костра, отмычка.
- Навыки/заклинания 10 классов (лечение, баффы, щиты, невидимость, рывок, снаряды с эффектами горения/замедления, AoE), заряды у спеллов, кулдауны у навыков, перки по уровням 1/5/10/15, XP за монстров и экстракцию.
- Монстры: Skeleton Swordsman (меч, блок), Skeleton Archer (лук, держит дистанцию), Zombie (медленный, бьёт лапами, живучий) — тот же боевой стек, что и игрок, NavMesh‑патфайндинг, агро по зрению, присед снижает дистанцию обнаружения, монстры открывают двери, возвращаются на пост, лут и XP за убийство.
- Интерактивы: двери (замок, отмычка), сундуки/гробы/бочки/ящики/полки, рычаги, алтари (Health/Protection/Power/Speed, одноразовые), костёр (отдых + HoT), ловушки (шипы, маятниковые лезвия), порталы, предметы на полу (дроп из инвентаря).
- Освещение: только локальные источники (факелы, жаровни, свечи, факел в руке, посох, порталы), чёрный ambient, экспоненциальный туман, Forward+.

## Не реализовано (по ТЗ или сознательно упрощено)

Рынок/торговцы, квесты, боссы, мимики, мини‑игра отмычки (отмычка расходуется мгновенно), recoverable health, команды/Soul Heart/воскрешение, спектатор, High‑Roller, карта/компас.

## Архитектура (`Assets/Game/Scripts/Dungeon`)

| Слой | Классы |
|---|---|
| Config | `ItemConfig` + `Weapon/Armor/Consumable/Utility/TreasureItemConfig`, `ItemDatabase`, `ClassConfig`, `AbilityConfig`, `MonsterConfig`, `LootTableConfig`, `DungeonConfig`, `DungeonFormulas` |
| Core | `ItemStack` (6 байт, сетевой), `InventoryComponent` (сетка + слоты, `NetworkArray`), `InventoryActionsComponent` (RPC клиент→хост: move/equip/unequip/drop/use/take all/load), `AdventurerStats`, `StatusEffectComponent`, `InteractableComponent`, `MatchComponent` (таймер, Dark Swarm) |
| Content | `AdventurerComponent` (игрок: класс, инвентарь, взаимодействие, способности, смерть, экстракция, спуск), `PlayerSessionComponent` (лобби, кит, сташ, уровень), `MonsterComponent`, `CorpseComponent`, `ContainerComponent`, `DoorComponent`, `PortalComponent`, `ShrineComponent`, `CampfireComponent`, `LeverComponent`, `TrapComponent`, `WorldItemComponent` |
| AI | `MonsterBrainComponent` (`FighterComponent.IInputSource` + NavMesh) |
| System | `DungeonContext` (service locator), `DungeonDirector` (хост: сессии, спавн, заселение, порталы, сброс), `StashService` (PlayerPrefs) |
| View | `DungeonUiRoot`, `LobbyView`, `DungeonHudView`, `InventoryView` + `ItemGridView` + `ItemView` + `EquipSlotView`, `ResultView`, `AdventurerVisualComponent` (броня на костях, факел, невидимость), `FlickerLightComponent` |

Изменения в боевой системе (`Battle`): каталог оружия в `CombatComponent` (слоты → индекс каталога, `SetSlotWeapon`), `ICombatStats` (множители урона, Action Speed, Move Speed), `DamageReceiverComponent.IDefense` (броня/резисты/щиты), `DamageType`, состояние `Busy` (каст/использование/взаимодействие), снаряды с видом/типом урона/AoE/эффектом, бег по умолчанию + тихий шаг, кнопки ввода для взаимодействия/навыков/спеллов/пояса.

Редакторские билдеры (`Assets/Game/Scripts/Editor/Dungeon`): `DungeonWeaponLibrary` (позы/тайминги новых оружий), `DungeonWeaponPrefabBuilder`, `DungeonTextureBuilder`, `DungeonMeshBuilder` + `DungeonPropBuilder` (стены, арки, колонны, сундуки, гробы, бочки, полки, жаровни, факелы, баннеры, черепа, алтари, порталы, ловушки), `DungeonItemLibrary` (данные предметов/классов/способностей/перков), `DungeonContentBuilder`, `DungeonMapBuilder`, `DungeonUiBuilder`, `DungeonSceneBuilder`, `DungeonBuildMenu`.

## Справочные данные с wiki (использованы в прототипе)

Базовые атрибуты классов (lvl 1): Fighter 15/15/15/15/15/15/15 (125 HP), Barbarian 20/25/13/12/18/5/12, Rogue 9/6/25/20/10/10/25, Ranger 12/10/20/18/10/12/23, Wizard 6/7/15/17/20/25/15, Cleric 11/13/12/14/23/20/12, Warlock 11/14/14/15/22/15/14, Bard 13/13/13/20/11/20/15, Druid 12/13/12/12/18/20/18, Sorcerer 10/10/10/18/25/20/12.

Формулы: HP = (curve(STR×0.25+VIG×0.75) + 25); Power bonus: 15 → 0 %, 50 → +35 %, 100 → +50 %; Move Speed add по Agility: 15 → 0, 75 → +36; Action Speed = AGI×0.25+DEX×0.75; Interaction = DEX×0.25+RES×0.75; AR→PDR: 0 → −14.8 %, 100 → 16 %, 300 → 43 %, 600 → 65 %; Will→MR: 15 → 30, 33 → 102; MR→MDR: 85 → 23 %, 280 → 62 %.

Оружие (урон/штраф MS): Longsword 36/−30, Arming Sword 27/−20, Falchion 33/−25, Zweihander 39/−40, Battle Axe 43/−30, Spear 34/−40, Flanged Mace 31/−20, Rondel 16/−10, Recurve 25–31/−40, Crossbow 33–39/−50, Magic Staff 29/−20, Torch 9. Броня: Leather Cap 31/−3, Great Helm 49/−7, Adventurer Tunic 33/−3, Templar 81/−9 (+20 MR), Dark Plate 101/−14, Plate Pants 75/−8, Adventurer Boots 23/+6, Plate Boots 42/+4.

Монстры (Common): Skeleton Footman 117 HP, Skeleton Archer 70 HP (24 за стрелу), Zombie 168 HP (≈39 за удар, скорость 50 %); −22 % phys / −17 % magic DR.

Зоны попадания: голова ×1.5, тело ×1.0, ноги ×0.6. Движение: 300 = 100 %, cap 330; шаг ×0.4, присед ×0.65, назад ×0.6.
