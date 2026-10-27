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
| Fabric 08 (лён) | [cgbookcase](https://www.cgbookcase.com/textures/fabric-08) | CC0 | `Assets/SpecialFolder/Textures/Cgbookcase/Fabric08` | Veinworker: рубаха, штаны (`OutfitPeasant`) |
| Brown Leather 01 | [cgbookcase](https://www.cgbookcase.com/textures/brown-leather-01) | CC0 | `Assets/SpecialFolder/Textures/Cgbookcase/BrownLeather01` | Veinworker: сапоги (`OutfitPeasantBoots`), капюшон и обмотки (`OutfitRanger3`) |

Металл (B канал ORM) исключён detail-маской `T_*_ORM_DetailMask`.

Добавить материал cgbookcase: скачать zip (Metalness/Roughness, 2K) → распаковать в `Assets/SpecialFolder/Textures/Cgbookcase/<Имя>/` → прописать в `ClothDetailBuilder.s_sources` → `Tools/Game/Battle/Build Cloth Materials`.
