# Латный доспех к большому бацинету (Plate Harness)

Пара к `Great Bascinet`: итальянский «белый» доспех ~1420–1450 (та же эпоха, что большой бацинет). Бацинет пристёгивается ремнями к кирасе спереди и сзади — поэтому у кирасы **нет горжета**: шею закрывает сам шлем.

## Состав
Слот **Chest**. Предмет закрывает торс и руки до запястья; перчатки, ноги и сабатоны — следующие предметы (см. «Дальше»).

| Часть | Что это | Кость | Зеркало |
|---|---|---|---|
| Cuirass | Нагрудник + наспинник одним корпусом, ремни-петли для бацинета на груди и спине | `Chest` | — |
| Fauld | 3–4 поясные ламы + две набедренные тассеты | `Hips` | — |
| Pauldron | Наплечник (spaulder) + наплечевой рукав (rerebrace), левый | `LeftUpperArm` | правый — `RightUpperArm`, `IsMirrored` |
| Vambrace | Налокотник (couter) + наруч до запястья, левый | `LeftLowerArm` | правый — `RightLowerArm`, `IsMirrored` |

Жёсткие куски на костях — тот же механизм, что у шлемов (`ArmorPieceSetConfig.Entries`, одна `ArmorVisual` — несколько записей на разных костях). Под латами — **поддоспешник**: скиннованные `PeasantBody` + `PeasantArms` тёмным стёганым материалом (`ArmorPieceSetConfig.Outfits`), чтобы в щелях на сгибах была ткань, а не кожа.

## Статы
| | Veinworker Shirt | **Plate Harness** | Great Bascinet |
|---|---|---|---|
| Тип / слот | Cloth / Chest | **Plate / Chest** | Plate / Head |
| Armor Rating | 25 | **95** | 45 |
| Magic Resistance | 0 | **0** | 0 |
| Move Speed | −1 | **−22** | −16 |
| Бонусы | — | **+2 Vitality, +1 Strength, −1 Dexterity** | +1 Vit, +1 Str, 25 % Headshot Reduction |
| Классы | все | **Mercenary, Confessor** | Mercenary, Confessor |
| Размер в сумке | 2×3 | **2×3** | 2×2 |
| Value | 12 | **150** | 80 |
| Редкость | роллится | **роллится** (+2 AR за тир, аффиксы) | роллится |
| Звуки предмета | — | **`treasure_l`** | `treasure_l` |

Обоснование:
- AR 95 ≈ 2,1× бацинета — то же отношение грудь/голова, что у Veinworker (25/12).
- Комплект бацинет + харнесс + Veinworker штаны/ботинки = AR 168 → **~27 % физ. снижения** против 73 AR → ~12 % у полного Veinworker. Скорость 300 → **262** (−12,7 %) при Agility 15.
- −1 Dexterity — латные рукава сковывают руки (Dex — единственный стат скорости рук); получается цена за защиту, отличная от бацинета (тот режет обзор/скорость).
- Уже работает автоматически: удар в торс звенит как по латам (`GetBodySurface` → `ImpactSurface.Plate`), `IronCalls` (молния по латникам) и `HeavyArmorTraining` (меньше штраф скорости), `LightStep` отключается.
- Описание: *"Breast and back of one steel, arms in plate to the wrist. The great bascinet buckles onto it; the man inside turns like a door."*
- Лут: `rich` — вес 0.2, `ornate` — 0.4 (у бацинета 0.3 / 0.5).

## Промпты для Tripo
Четыре отдельные модели — **не цельный костюм**: цельную оболочку нельзя повесить на кости, она будет рваться на сгибах. Если сейчас генерируется цельный доспех — его стоит использовать как картинку-референс (image → 3D) для кусков ниже. Для единства стиля можно приложить скрин бацинета.

Общий хвост: `early 15th century Italian plate armour matching a great bascinet, polished but worn steel with scratches and small dents, dark leather straps, brass buckles and rivets, dark fantasy medieval, game asset, realistic PBR, clean quad topology, low poly, single object, hollow inside, no mannequin, no body, no base, no background`

| Имя задачи | Промпт (начало + общий хвост) | Поза / ориентация | Размер | Полигоны |
|---|---|---|---|---|
| `PlateCuirass` | `plate cuirass, rounded globular breastplate with a central ridge and a separate lower plackart, matching backplate, open neck without gorget, strap loops at chest and back for a helmet, armholes cut deep` | стоит вертикально, грудь вперёд | 0.55 м высота, 0.42 м ширина | ~6k |
| `PlateFauld` | `plate fauld, four overlapping horizontal steel lames around the hips, two rectangular tassets hanging on straps at the front` | вертикально, перед вперёд | 0.30 м высота | ~3k |
| `PlatePauldron` | `left shoulder armour, rounded spaulder of three overlapping lames joined to a tubular upper arm rerebrace` | рука опущена вдоль тела (A-pose), левая | 0.34 м | ~3k |
| `PlateVambrace` | `left forearm armour, pointed fan-shaped couter elbow cop joined to a tubular vambrace ending at the wrist` | вытянута вниз, левая, локоть сзади | 0.30 м | ~3k |

Текстура — да, риг — нет.

## Встраивание (делает Claude, когда модели лягут в `Assets/TripoModels`)
1. **Приём**: `Assets/Game/Models/Armor/PlateHarness/<Part>/<Part>.fbx` + `Textures/<Part>_Color|Normal|MetallicSmoothness` (через AssetDatabase).
2. **`ArmorVisual.PlateHarness`** — после `GreatBascinet`, до `PeasantHead` (жёсткие визуалы идут первыми; полный ребилд Dungeon перепишет все `_visual`).
3. **`DungeonContentBuilder`**: `s_helmets` → общий список жёстких кусков `(Visual, Model, Bone, Mirrored, Position, Scale)`; `ArmorMapping`/`BuildArmorPieces`/`BuildOutfitModels` строятся из него; префабы `Armor_PlateHarness_<Part>.prefab`.
4. **Equipment Fit** (`EquipmentFitView`): выбор куска внутри визуала, сохранение посадки в корень префаба каждого куска; зеркальные берут посадку левого.
5. **Поддоспешник**: `ArmorOutfitEntry` для `PlateHarness` → `PeasantBody`, `PeasantArms` с новым материалом `ArmingDoublet` (тёмная стёганая ткань).
6. **Предмет** — в конец списка `DungeonItemLibrary` (id = позиция), хелпер `Harness(...)` по образцу `Helmet(...)`, лут-таблицы `rich`/`ornate`, цитата в `Lore.md` → «Снаряжение».
7. **Иконка**: фронтальная сборка (кираса + наплечники + наручи + тассеты) в `DungeonIconBuilder`.
8. **Проверка**: подгонка в сцене AnimationEditor → Equipment Fit; FP — наплечники не лезут в камеру на замахах, наручи в кадре выглядят нормально; превью персонажа, труп/рэгдолл, звон по торсу.

Риски: наплечник при поднятой руке может пробить кирасу — если так, сажать его на `LeftShoulder` (ключица) и делить с рукавом; в FP при необходимости наплечники только с тенью для своего игрока.

## Дальше (тот же комплект)
| Предмет | Слот | AR | Move | Части |
|---|---|---|---|---|
| Plate Gauntlets | Hands | 25 | −4 | крага на `Hand` + пальцевые пластины (сложно: пальцы гнутся; отдельная задача) |
| Plate Legs | Legs | 60 | −12 | набедренник (`UpperLeg`), наколенник + поножи (`LowerLeg`) |
| Plate Sabatons | Feet | 30 | −8 | сабатон на `Foot`; шаги станут `Footwear.Plate` автоматически |
