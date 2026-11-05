# Tripo-пачка: модуль WellRest «Привал у колодца»

Почему он: самый простой модуль атласа (`Atlas/data/WellRest.js`) — 34.2×34.2 м, потолок 6 м, 23 блока стен, 6 платформ (помост колодца 0.85 м, террасы 1 м и 2.5 м), 2 лестницы, 4 парапета, 4 контейнера, 3 источника света, 8 проёмов 2×2.4 м. Монстров нет, комната передышки.

## Как генерировать
- **Имя задания в студии = `Имя` из таблицы** (PascalCase). Бридж кладёт `Assets/TripoModels/<Имя>/`, приёмку делаю я.
- Сначала картинка, потом 3D по трём видам. Промпт картинки = промпт из таблицы + общий хвост:
  `, asset for a realistic dark fantasy dungeon game, character sheet of one object in three orthographic views side by side: front view, left side view, back view, same scale and alignment in every view, for 3D modeling, realistic PBR materials, worn and dirty, neutral grey background, even soft lighting, no shadows, no base, no ground, no text`
- 3D: text → 3D по картинке (multiview), текстура PBR, рига нет. Объект должен стоять в мировых осях: верх вверх, лежачие предметы — лёжа (в прошлой пачке скелет, рёбра, кости и плита вышли стоя, поворачиваю в префабе, но лучше сразу).
- Топология — **Quad** (Smart Low Poly / Retopology → Quad), Face limit по колонке «Quads» **строго**: прошлая пачка пришла в 2–7 раз тяжелее (34 штуки уже ~426k треугольников на комнату). Текстура: ≤ 1 м — 1K, больше — 2K.
- «Размер» — реальный габарит Ш×В×Г в метрах; масштаб ставлю в импортёре по наибольшей стороне, пропорции модели должны совпадать с ним.
- «Шт.» — сколько экземпляров стоит в комнате. Варианты A/B/C — отдельные генерации.

## Не через Tripo
Пол, потолок и плоскости стен остаются блоками кита с тайловыми PBR-материалами (`Ashlar`, `Flagstone`). Паутина, кровь, трещины, грязь, лужи, натёки воска, мох — декали (URP Decal Projector) из CC0 (`Free_PBR_Sources.md`).

## Осталось сделать

### A. Архитектура (`Environment/Catacomb`)
| Имя | Промпт | Размер | Quads | Шт. |
|---|---|---|---|---|
| CatacombDoor | **переделать:** tall narrow dungeon door leaf, height more than twice the width, dark oak vertical planks with rusted iron bands, ring handle, big hinges on one side, worn and scratched | 1.0×2.35×0.12 | 1k | 8 пар |
| DoorwayArch | stone doorway frame with rounded arch, ashlar blocks, chipped edges, empty opening 2 m wide 2.4 m high | 2.8×3.0×0.6 | 1.5k | 8 |
| StonePillarBroken | square stone pillar broken at mid height, jagged top, fallen blocks at its foot | 1.2×3×1.2 | 1.5k | 1 |
| WallPilaster | flat half pillar attached to a wall, flat back, ashlar, worn | 0.8×6×0.3 | 750 | 10 |
| CeilingArchRib | stone vault rib arch segment, curved ashlar voussoirs, cracked | 6×1.5×0.5 | 1k | 6 |
| WallBaseRubble | long low strip of fallen mortar, gravel and small stones lying along a wall base | 3×0.3×0.6 | 500 | 12 |
| BurialNiche | stone wall niche with a crumbling skull and bones inside, carved frame, flat back | 1.2×1.0×0.6 | 1k | 4 |
| StoneStairsShort | narrow stone staircase, 4 worn steps, ashlar sides | 2.4×1.0×2.0 | 1k | 1 |
| StoneStairsLong | long stone staircase, 10 worn steps, ashlar side walls, chipped edges | 2.4×2.5×6.0 | 1.5k | 1 |
| StoneParapet | low stone parapet wall segment, ashlar blocks with flat cap stones, chipped | 2.0×1.0×0.4 | 500 | 12 |
| ParapetPost | stone parapet end post, square, cap stone | 0.5×1.1×0.5 | 250 | 6 |
| RetainingWall | stone retaining wall facing of a raised terrace, big rough blocks, moss in seams, flat back | 2.0×1.0×0.3 | 500 | 16 |

### B. Свет (`Environment/Lights`)
| Имя | Промпт | Размер | Quads | Шт. |
|---|---|---|---|---|
| CookingTripod | iron tripod with a hanging soot-covered cooking pot on a chain, standing alone | 1.0×1.3×1.0 | 750 | 1 |

### C. Привал и сторож Ханс (`Environment/Camp`)
| Имя | Промпт | Размер | Quads | Шт. |
|---|---|---|---|---|
| BedrollOpen | unrolled bedroll lying flat, worn wool blanket on a leather mat, dirty | 0.8×0.15×2.0 | 500 | 3 |
| BedrollRolled | rolled bedroll lying on its side, tied with leather straps | 0.7×0.3×0.3 | 300 | 2 |
| LogSeat | short sawn log standing upright used as a seat, cracked bark | 0.5×0.45×0.5 | 300 | 3 |
| AbandonedBackpack | old leather prospector backpack lying on its side, straps, patched canvas | 0.5×0.4×0.3 | 500 | 2 |
| WaterBucket | wooden bucket with iron hoops, half full of dark water | 0.4×0.4×0.4 | 300 | 2 |
| WardenLectern | wooden lectern with a huge open ledger book, quill and ink pot, dusty | 0.7×1.3×0.6 | 1k | 1 |
| WardenStool | simple three-legged wooden stool, worn | 0.4×0.5×0.4 | 250 | 1 |
| RopeCoil | coil of thick old rope lying flat on the floor | 0.5×0.15×0.5 | 300 | 2 |
| Pickaxe | miner pickaxe lying flat, worn wooden handle, chipped iron head | 0.8×0.1×0.3 | 300 | 2 |
| Shovel | old miner shovel standing upright, wooden handle, rusted blade | 0.3×1.3×0.1 | 250 | 1 |

### D. Контейнеры (`Environment/Containers`)
| Имя | Промпт | Размер | Quads | Шт. |
|---|---|---|---|---|
| SmallOakChestBody | **переделать двумя моделями:** small oak chest box without lid, open top, thick walls with visible inner sides and bottom, iron corners and a lock plate on the front | 0.9×0.45×0.5 | 600 | 1 |
| SmallOakChestLid | separate lid of a small oak chest, slightly domed, iron bands and corners, hinge straps on the back edge | 0.9×0.2×0.5 | 300 | 1 |
| SupplyCrateSmall | по желанию переделать: small closed wooden box with nailed lid and rope handles (вышел открытый лоток) | 0.5×0.4×0.4 | 300 | 2 |

> Tripo-сундук (`SmallOakChest`) — пустая оболочка без внутренних стенок: крышка при разрезе рвётся, поэтому лутовый сундук пока остаётся из Medieval Assets Pack. Бочка и ящик-контейнер уже Tripo.

### E. Мусор и кости (`Environment/Debris`)
| Имя | Промпт | Размер | Quads | Шт. |
|---|---|---|---|---|
| RubblePileLarge | large pile of fallen ashlar blocks, broken masonry and gravel | 2.0×0.8×1.6 | 1k | 3 |
| SkullSingle | single human skull, old yellowed bone, cracked | 0.2×0.2×0.25 | 200 | 10 |
| SkeletonSitting | human skeleton sitting on the floor slumped back, legs stretched forward, head down, rotten rags | 0.6×0.9×0.8 | 1.5k | 2 |

Осталось 32 модели (из них переделки: дверь и сундук двумя частями) + 1 по желанию (`SupplyCrateSmall`).

## Готово (32) — где стоят
Модели: `Assets/Game/Models/Environment/<Категория>/<Имя>/` (`<Имя>.fbx`, `.mat`, `Textures/<Имя>_Color|Normal|Metallic|Roughness|MetallicSmoothness`). Префабы: `Assets/Game/Prefabs/Dungeon/Tripo/<Имя>.prefab`, меню `Tools/Game/Art/Build Tripo Props` (масштаб, поворот, опора, коллайдер — `TripoPropBuilder`). Расстановка — `CatacombRoomBuilder.Dressing.cs`, меню `Tools/Game/Dungeon/Catacombs/Dress Rooms` (запускается и после Apply Mockups / Build Missing Rooms), дочерний `Dressing` в префабе комнаты; недостающее стоит болванками (конторка, табурет, брёвна, спальники).

| Модель | Применение |
|---|---|
| StonePillar | облицовка трёх квадратных опор зала |
| WallTorch, WallShackles, HangingChain, HangingLantern, IronCandlestick | декор (без собственного света) |
| Campfire | костёр `CatacombFire` во всех комнатах вместо брусков |
| WoodBarrel, SupplyCrate | лутовые `Barrel` и `Crate` во всём данже + декор |
| SmallOakChest | только префаб (см. D) |
| CatacombDoor | только префаб: пропорции 1:1.4 вместо 1:2.35 (см. A) |
| SupplyCrateSmall, BrokenBarrel, GrainSack, SackPile, ColumnDrum, FallenBlock, RockA, RockB, RubblePileSmall, SkullPile, Ribcage, BoneScatter, SkeletonLying, PotteryShards, PlankDebris, OssariteBone, OssariteCluster | наполнение WellRest |
| StoneWell | колодец на помосте WellRest (сплошной) |
| WellDaisStep | бордюр помоста колодца, 12 шт. по краю, верх на 2 см выше помоста |
| CandleClusterA, CandleClusterB | огарки (без света): у конторки, у спальников, на обеих террасах, у груд черепов |
