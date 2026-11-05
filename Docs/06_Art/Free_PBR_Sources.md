# Бесплатные PBR-текстуры (CC0)

CC0 = public domain: коммерческое использование, любые правки, указание автора не требуется. Всё равно записываем источник в таблицу «Используется в проекте» ниже.

## Источники

| Источник | Что там | Примечание |
|---|---|---|
| [Poly Haven](https://polyhaven.com/textures) | Текстуры до 8K, HDRI, модели | Лучшее качество, сканы. Камень, кирпич, дерево, грунт |
| [ambientCG](https://ambientcg.com) | ~2000 PBR-материалов | Больше всего выбора, карты Color/NormalGL/NormalDX/Roughness/AO/Displacement, есть API |
| [cgbookcase](https://www.cgbookcase.com/textures) | Несколько сотен текстур | Ткани, кожа, кладка. Нормали DirectX, скачивание только из браузера (CDN режет прямые ссылки) |
| [ShareTextures](https://www.sharetextures.com) | Текстуры и модели | CC0, качество неровное |
| [3DTextures.me](https://3dtextures.me) | Стилизованные и реалистичные | CC0, часть процедурные |

Не CC0, но можно в игре без раздачи исходников: Fab (Standard License), Unity Asset Store (Standard EULA), FreePBR.com (своя лицензия). Не использовать: Textures.com, агрегаторы без явной лицензии.

## Подготовка под URP Lit
- Нормаль — OpenGL. DirectX-нормаль (cgbookcase, `_NormalDX`) — импортёр `flipGreenChannel = true`.
- Mask Map: R = Metallic, G = AO, B = Detail mask, A = Smoothness (= 1 − Roughness).
- Тайлинговые ткань/кожа на атласной одежде — detail-карты (`_DetailAlbedoMap` серый ×2, `_DetailNormalMap`), атлас остаётся основой.
- 1K для detail-карт, 2K для поверхностей окружения, 4K+ только для крупного вблизи.

## Используется в проекте

| Материал | Источник | Лицензия | Где лежит | Где применён |
|---|---|---|---|---|
| Fabric 08 (лён) | [cgbookcase](https://www.cgbookcase.com/textures/fabric-08) | CC0 | `Assets/Game/Textures/Cgbookcase/Fabric08` | Veinworker: рубаха, штаны (`OutfitPeasant`) |
| Brown Leather 01 | [cgbookcase](https://www.cgbookcase.com/textures/brown-leather-01) | CC0 | `Assets/Game/Textures/Cgbookcase/BrownLeather01` | Veinworker: сапоги (`OutfitPeasantBoots`), капюшон и обмотки (`OutfitRanger3`) |

| Stone Brick Wall 001 (2.5 м) | [Poly Haven](https://polyhaven.com/a/stone_brick_wall_001) | CC0 | `Assets/Game/Textures/PolyHaven/stone_brick_wall_001` | `CatacombWall`: стены катакомб, стены между комнатами, перемычки |
| Large Sandstone Blocks (3 м) | [Poly Haven](https://polyhaven.com/a/large_sandstone_blocks) | CC0 | `…/PolyHaven/large_sandstone_blocks` | `CatacombBlock`: опоры, помосты, парапеты, косяки и арки проёмов, каменные пропсы |
| Cobblestone Floor 02 (2 м) | [Poly Haven](https://polyhaven.com/a/cobblestone_floor_02) | CC0 | `…/PolyHaven/cobblestone_floor_02` | `CatacombFloor`: пол |
| Slab Tiles (2.37 м) | [Poly Haven](https://polyhaven.com/a/slab_tiles) | CC0 | `…/PolyHaven/slab_tiles` | `CatacombStairs`: лестницы |
| Medieval Wall 02 (3.5 м) | [Poly Haven](https://polyhaven.com/a/medieval_wall_02) | CC0 | `…/PolyHaven/medieval_wall_02` | `CatacombCeiling`: потолок |
| Castle Brick 01 (1.5 м), Mossy Stone Wall (2 м), Monastery Stone Floor (1.8 м) | [Poly Haven](https://polyhaven.com/textures) | CC0 | `…/PolyHaven/<id>` | `CatacombBrick`, `CatacombMossy`, `CatacombSlate`: запасные варианты, пока не назначены |

Poly Haven: качать 2K, из пакета нужны `diff` → `<id>_Color.jpg`, `nor_gl` → `<id>_Normal.png`, `arm` → `<id>_ARM.jpg` (`.blend`/`.gltf` не класть в Assets). Материалы на шейдере `Game/WorldStone` (мировая проекция, Tiling = 1 / размер тайла в метрах), меню `Tools/Game/Dungeon/Catacombs/Apply Stone Surfaces` (`CatacombSurfaceBuilder`).

Металл (B канал ORM) исключён detail-маской `T_*_ORM_DetailMask`.

Добавить материал cgbookcase: скачать zip (Metalness/Roughness, 2K) → распаковать в `Assets/Game/Textures/Cgbookcase/<Имя>/` → прописать в `ClothDetailBuilder.s_sources` → `Tools/Game/Battle/Build Cloth Materials`.
