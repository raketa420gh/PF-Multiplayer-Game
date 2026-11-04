# Tripo-пачка: модуль WellRest «Привал у колодца»

Почему он: самый простой модуль атласа (`Atlas/data/WellRest.js`) — 34.2×34.2 м, потолок 6 м, 23 блока стен, 6 платформ (помост колодца 0.85 м, террасы 1 м и 2.5 м), 2 лестницы, 4 парапета, 4 контейнера, 3 источника света, 8 проёмов 2×2.4 м. Монстров нет, комната передышки.

## Как генерировать
- **Имя задания в студии = `Имя` из таблицы** (PascalCase). Тогда бридж положит `Assets/TripoModels/<Имя>/` и приёмка — простой перенос в `Assets/Game/Models/<Категория>/<Имя>/`.
- Промпт из таблицы + общий хвост: `, dark fantasy medieval, game asset, realistic PBR, clean quad topology, low poly, single object, no base, no background`.
- Режим везде text → 3D, текстура PBR, рига нет.
- Топология — **Quad** (Smart Low Poly / Retopology → Quad), Face limit по колонке «Quads» (это квады; в Unity треугольников будет ×2). Текстура: ≤ 1 м — 1K, больше — 2K.
- «Размер» — реальный габарит Ш×В×Г в метрах, масштаб я выставлю в импортёре.
- «Шт.» — сколько экземпляров стоит в комнате (повороты, масштаб ±15 %). Варианты A/B/C — отдельные генерации.

## Не через Tripo
Пол, потолок и плоскости стен остаются блоками кита с тайловыми PBR-материалами (`Ashlar`, `Flagstone`): у Tripo-моделей текстура не тайлится, а стены в модуле нестандартного размера (0.6–7 м). Коробку стен ломают модели из блока A (пилястры, рёбра, цоколь). Паутина, кровь, трещины, грязь, лужи, натёки воска, мох — декали (URP Decal Projector), берём из CC0 (`Free_PBR_Sources.md`).

## A. Архитектура (`Environment/Catacomb`)
| Имя | Промпт | Размер | Quads | Шт. |
|---|---|---|---|---|
| CatacombDoor | heavy dungeon door leaf, dark oak planks with rusted iron bands, ring handle, big hinges, worn and scratched | 1.0×2.35×0.12 | 1k | 8 пар (одна створка, вторую зеркалим) |
| DoorwayArch | stone doorway frame with rounded arch, ashlar blocks, chipped edges, opening 2 m wide 2.4 m high | 2.8×3.0×0.6 | 1.5k | 8 |
| StonePillar | square catacomb pillar, rough ashlar blocks, simple base and capital, cracked, soot stains | 1.2×6×1.2 | 1.5k | 4 |
| StonePillarBroken | square stone pillar broken at mid height, jagged top, fallen blocks at its foot | 1.2×3×1.2 | 1.5k | 1 |
| WallPilaster | flat half pillar attached to a wall, ashlar, worn | 0.8×6×0.3 | 750 | 10 |
| CeilingArchRib | stone vault rib arch segment, curved ashlar voussoirs, cracked | 6×1.5×0.5 | 1k | 6 |
| WallBaseRubble | long low strip of fallen mortar, gravel and small stones lying along a wall base | 3×0.3×0.6 | 500 | 12 |
| BurialNiche | stone wall niche with a crumbling skull and bones inside, carved frame | 1.2×1.0×0.6 | 1k | 4 |
| StoneStairsShort | narrow stone staircase, 4 worn steps, ashlar sides | 2.4×1.0×2.0 | 1k | 1 |
| StoneStairsLong | long stone staircase, 10 worn steps, ashlar side walls, chipped edges | 2.4×2.5×6.0 | 1.5k | 1 |
| StoneParapet | low stone parapet wall segment, ashlar blocks with flat cap stones, chipped | 2.0×1.0×0.4 | 500 | 12 |
| ParapetPost | stone parapet end post, square, cap stone | 0.5×1.1×0.5 | 250 | 6 |
| RetainingWall | stone retaining wall facing of a raised terrace, big rough blocks, moss in seams | 2.0×1.0×0.3 | 500 | 16 |
| StoneWell | round stone well with a wooden roof frame, winch, rope and hanging bucket, old and damp | 1.8×2.6×1.8 | 2k | 1 |
| WellDaisStep | low stone step edge of a square dais, worn ashlar curb | 2.0×0.4×0.5 | 250 | 12 |

## B. Свет (`Environment/Lights`)
| Имя | Промпт | Размер | Quads | Шт. |
|---|---|---|---|---|
| WallTorch | iron wall sconce bracket holding an unlit wooden torch wrapped in cloth, soot | 0.3×0.7×0.4 | 400 | 4 |
| Campfire | campfire ring of soot-blackened stones with charred logs and ashes, unlit | 1.2×0.4×1.2 | 750 | 1 |
| CookingTripod | iron tripod over a fire with a hanging soot-covered cooking pot | 1.0×1.3×1.0 | 750 | 1 |
| CandleClusterA | cluster of five melted tallow candles of different heights, wax puddle, unlit | 0.3×0.3×0.3 | 400 | 4 |
| CandleClusterB | three thick melted candles on a skull, wax dripping over the bone, unlit | 0.25×0.35×0.25 | 500 | 2 |
| IronCandlestick | tall iron floor candlestick with one melted candle, rusted | 0.3×1.4×0.3 | 400 | 2 |
| HangingLantern | old iron lantern hanging on a short chain, cracked glass, unlit | 0.3×0.8×0.3 | 500 | 2 |

## C. Привал и сторож Ханс (`Environment/Camp`)
| Имя | Промпт | Размер | Quads | Шт. |
|---|---|---|---|---|
| BedrollOpen | unrolled bedroll, worn wool blanket on a leather mat, dirty, flat on the floor | 0.8×0.15×2.0 | 500 | 3 |
| BedrollRolled | rolled bedroll tied with leather straps | 0.7×0.3×0.3 | 300 | 2 |
| LogSeat | sawn log used as a seat, cracked bark | 0.5×0.45×0.5 | 300 | 3 |
| AbandonedBackpack | old leather prospector backpack lying on its side, straps, patched canvas | 0.5×0.4×0.3 | 500 | 2 |
| WaterBucket | wooden bucket with iron hoops, half full of dark water | 0.4×0.4×0.4 | 300 | 2 |
| WardenLectern | wooden lectern with a huge open ledger book, quill and ink pot, dusty | 0.7×1.3×0.6 | 1k | 1 |
| WardenStool | simple three-legged wooden stool, worn | 0.4×0.5×0.4 | 250 | 1 |
| RopeCoil | coil of thick old rope lying on the floor | 0.5×0.15×0.5 | 300 | 2 |
| Pickaxe | miner pickaxe with a worn wooden handle and chipped iron head | 0.8×0.1×0.3 | 300 | 2 |
| Shovel | old miner shovel, wooden handle, rusted blade | 0.3×1.3×0.1 | 250 | 1 |

## D. Контейнеры лута (`Environment/Containers`)
| Имя | Промпт | Размер | Quads | Шт. |
|---|---|---|---|---|
| SmallOakChest | small oak chest with iron corners and a lock plate, closed, scratched | 0.9×0.7×0.5 | 750 | 1 |
| SupplyCrate | wooden supply crate with nailed planks, cracked corner | 0.85×0.85×0.85 | 400 | 3 |
| SupplyCrateSmall | small wooden box with rope handles | 0.5×0.4×0.4 | 300 | 2 |
| WoodBarrel | old wooden barrel with rusted iron hoops, closed lid | 0.85×1.0×0.85 | 500 | 2 |
| BrokenBarrel | broken wooden barrel lying on its side, missing staves, spilled planks | 1.0×0.6×0.9 | 600 | 1 |
| GrainSack | full burlap sack tied at the top, dusty | 0.5×0.7×0.4 | 300 | 3 |
| SackPile | pile of three burlap sacks slumped together | 1.0×0.6×0.8 | 600 | 1 |

> Сундук, ящик и бочка сейчас из Medieval Assets Pack — модели D заменят их в едином стиле. Корпус и крышку сундука разрежу сам. Глиняные горшок и ваза уже есть (`TerracottaPot`, `TerracottaVase`).

## E. Мусор и кости — наполненность (`Environment/Debris`)
| Имя | Промпт | Размер | Quads | Шт. |
|---|---|---|---|---|
| RubblePileLarge | large pile of fallen ashlar blocks, broken masonry and gravel | 2.0×0.8×1.6 | 1k | 3 |
| RubblePileSmall | small heap of stone rubble and gravel | 0.8×0.3×0.7 | 400 | 8 |
| FallenBlock | single fallen ashlar block, chipped edges, cracked | 0.8×0.5×0.5 | 200 | 5 |
| RockA | small rough grey stone, angular | 0.3×0.2×0.25 | 100 | 15 |
| RockB | flat broken stone slab fragment | 0.4×0.1×0.3 | 100 | 10 |
| ColumnDrum | broken round column drum lying on the floor, chipped | 0.7×0.5×0.7 | 400 | 2 |
| SkullSingle | human skull, old yellowed bone, cracked | 0.2×0.2×0.25 | 200 | 10 |
| SkullPile | pile of human skulls and long bones heaped together | 1.0×0.6×0.9 | 1.25k | 2 |
| Ribcage | broken human ribcage with spine, old bone | 0.4×0.25×0.6 | 400 | 4 |
| BoneScatter | scattered long human bones, femur and arm bones, lying flat | 0.8×0.1×0.6 | 300 | 12 |
| SkeletonSitting | human skeleton sitting slumped against a wall, head down, rotten rags | 0.6×0.9×0.8 | 1.5k | 2 |
| SkeletonLying | human skeleton lying on its back, partly scattered, rotten rags | 0.7×0.3×1.8 | 1.5k | 1 |
| PotteryShards | broken clay pot shards scattered | 0.5×0.15×0.5 | 200 | 6 |
| PlankDebris | heap of broken wooden planks with rusty nails | 1.2×0.3×0.8 | 400 | 3 |
| WallShackles | iron shackles hanging on a wall plate by short chains, rusted | 0.5×0.8×0.15 | 400 | 3 |
| HangingChain | long rusted iron chain hanging from the ceiling with a hook | 0.15×2.0×0.15 | 300 | 3 |
| OssariteBone | old femur bone overgrown with small pale grey crystals, faint cold glow | 0.5×0.2×0.2 | 400 | 3 |
| OssariteCluster | cluster of pale bluish-grey crystals growing out of bone fragments and stone | 0.5×0.4×0.5 | 500 | 2 |

## Итог
57 моделей, ~250 экземпляров в комнате, ~135k квадов (~270k треугольников в Unity) на модуль (без кита). Порядок генерации: D (сразу заменяет текущий лут) → A → B → C → E.
