# Латный доспех к большому бацинету (Plate Harness)

**Статус (2026-11-14):** в игре — цельная модель Tripo в T-pose (раздел «Цельный доспех в T-pose») заскиннена на риг персонажа, предмет `Plate Harness` добавлен. Статы — предложение, не утверждены. Вариант из четырёх жёстких кусков (разделы «Состав», первые промпты, «Встраивание») не использован и оставлен как справка.

## Как встроено (2026-11-14)
- Модель: `Assets/Game/Models/Armor/PlateHarness/PlateHarness.fbx` (9.5k tris, только basecolor); маска металла/гладкости `PlateHarness_MetallicSmoothness.png` получена из цвета (серое — сталь, цветное и чёрное — кожа и ткань), материал `OutfitPlateHarness`.
- Посадка и скиннинг: `BattleCharacterBuilder.PlateHarness.cs`. Торс ×1.2 (линия плеч оболочки — на плечевые суставы: пояс попадает на талию, подмышки на подмышки). Рукава у модели на треть короче тела и согнуты вперёд — каждый сегмент уложен вдоль своей кости от сустава до сустава (край манжеты на суставе запястья), обхват плеча ×1.38, предплечья ×1.5. Юбка расклёшена к подолу ×1.15 (женские бёдра).
- Веса: с ближайшей кожи тела (торс, плечи, бёдра), сглажены по оболочке; ворот держится на груди, а не на шее; от середины плеча латы жёстко сидят на костях `upperarm`/`lowerarm`, сгиб только на налокотнике.
- `OutfitPart.PlateHarness` закрывает торс и плечи тела; предплечье остаётся видимым внутри наруча (чистый стык с кистью). Обмотки (`RangerBracers`) под латным рукавом скрываются.
- Пересборка одной части без пересборки персонажа: `Tools/Game/Battle/Build Plate Harness`; полный `Build Character` собирает её тоже. Константы посадки — в начале файла.
- Не сделано: звено «бацинет пристёгнут к кирасе» визуально не подгонялось; своя посадка под женское тело (сейчас один меш на оба пола).

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

### Картинки для Tripo Multiview (ChatGPT Image)
По части — один чат, три сообщения подряд: сначала front, потом left и back «той же детали» (так ChatGPT держит форму). В Tripo — режим Multiview: front / left / back.

Общий хвост: `Orthographic view, no perspective distortion, camera exactly level with the object's centre. The whole object centred and fully in frame with margin, nothing cropped. Plain flat light grey background, soft even studio lighting, no cast shadows, no reflections of surroundings. No mannequin, no body, no stand, no text, no other objects. Realistic game asset reference sheet for 3D modelling, early 15th century Italian plate armour matching a great bascinet: polished but worn steel with fine scratches and small dents, dark brown leather straps, brass buckles and rivets.`

**PlateCuirass**
1. `Front view of a medieval plate cuirass standing upright as if worn: rounded globular breastplate with a vertical central ridge, a separate lower plackart overlapping it, deep-cut armholes, open round neck with NO gorget, two leather strap loops on the upper chest for a helmet, side hinges and buckles. Hollow inside.` + хвост
2. `The exact same cuirass from the previous image, now seen strictly from its left side (profile): breastplate curve, side hinge and buckle, backplate behind, same proportions, materials and wear.` + хвост
3. `The exact same cuirass, now seen strictly from the back: the backplate with its own lower lames, shoulder straps and a leather strap loop between the shoulder blades for a helmet, same proportions, materials and wear.` + хвост

**PlateFauld**
1. `Front view of a medieval plate fauld standing upright as if worn around the hips: four overlapping horizontal curved steel lames forming a hoop open at the back, two rectangular tassets of three lames each hanging from leather straps over the front of the thighs. Hollow inside.` + хвост
2. `The exact same fauld from the previous image, now seen strictly from its left side: the curve of the lames, a tasset hanging in front, same proportions, materials and wear.` + хвост
3. `The exact same fauld, now seen strictly from the back: the open back of the hoop with the leather belt and buckle, inner side of the tassets visible through the gap, same proportions, materials and wear.` + хвост

**PlatePauldron** (левая рука, опущена вдоль тела)
1. `Front view of a medieval LEFT shoulder armour as if worn on a left arm hanging straight down: a rounded spaulder of three overlapping lames covering the shoulder cap, joined by rivets and an inner leather strap to a tubular rerebrace that encloses the upper arm down to just above the elbow. Hollow inside, a top leather point for tying to the arming doublet.` + хвост
2. `The exact same left shoulder armour from the previous image, now seen strictly from the outer (left) side: the full dome of the spaulder and the rerebrace tube, same proportions, materials and wear.` + хвост
3. `The exact same left shoulder armour, now seen strictly from the back: the rear lames of the spaulder and the hinge and buckle of the rerebrace, same proportions, materials and wear.` + хвост

**PlateVambrace** (левая рука, вытянута вниз, локоть назад)
1. `Front view of a medieval LEFT forearm armour as if worn on a straight left arm hanging down: a pointed fan-shaped couter guarding the elbow at the back, articulated by a lame to a closed tubular vambrace that runs down to the wrist and flares slightly at the cuff. Hollow inside, open at both ends.` + хвост
2. `The exact same left forearm armour from the previous image, now seen strictly from the outer (left) side: the fan-shaped wing of the couter in full profile and the vambrace tube, same proportions, materials and wear.` + хвост
3. `The exact same left forearm armour, now seen strictly from the back: the point of the couter, the hinge line and two buckles of the vambrace, same proportions, materials and wear.` + хвост

### Цельный доспех в T-pose (2026-11-14, запрос пользователя)
Один слоёный предмет вместо четырёх кусков: латы поверх кольчуги поверх стёганого дублета, тело закрыто сплошь от пояса до шеи и запястий, нигде нет прорезей с голой кожей. Референс слоёв — `Docs/Tasks/PlateArmorRef/ref1.png` (можно приложить к первому сообщению: «use only as a reference for layering and coverage, not for the design»). Один чат, три сообщения подряд; в Tripo — Multiview front / back / left.

Общий хвост: `Orthographic view, no perspective distortion, camera level with the chest. The whole garment centred and fully in frame with margin, nothing cropped. Plain flat light grey background, soft even studio lighting, no cast shadows. Displayed on an invisible mannequin: no head, no hands, no legs, no skin, no body visible anywhere — the neck opening, the two cuffs and the bottom hem show only dark empty interior. No helmet, no gauntlets, no leg armour, no weapon, no stand, no text. Realistic game asset reference sheet for 3D modelling, early 15th century Italian plate armour made to be worn with a great bascinet: polished but worn steel with fine scratches and small dents, plate edges lined with small brass rivets, dark brown leather straps with brass buckles, dark riveted steel mail, black quilted wool arming doublet.`

1. **Front** — `Front view of a complete medieval plate upper-body armour worn as one layered garment, in a strict T-pose: both arms stretched straight out sideways, horizontal, perfectly symmetrical, both sleeve ends in frame. Layers from the inside out. (1) A black quilted arming doublet covering the whole torso and both arms without a break: a tall close-fitting standing collar up to the jaw line, long sleeves ending in tight cuffs at the wrists. (2) Riveted mail over the doublet, visible wherever there is no plate: at the armpits, on the inside of the elbows, as a mail collar lying around the base of the neck, and as a short mail skirt with a dagged lower edge hanging from the waist to the top of the thighs. (3) Steel plate on top: a rounded globular breastplate with a vertical central ridge and a lower plackart overlapping it, a fauld of three horizontal lames at the waist, large rounded pauldrons of overlapping lames on both shoulders, tubular rerebraces on the upper arms, fan-shaped couters at the elbows, closed vambraces on the forearms down to the wrists. A leather waist belt with a brass buckle, and a leather strap with a buckle on the upper chest for fastening a helmet. No plate gorget: the neck is covered only by the doublet collar and the mail collar. There are no gaps, slits or cut-outs anywhere: every area between plates is filled with mail or quilted cloth, so nothing of the body could be seen from the hips to the neck and to both wrists.` + хвост
2. **Back** — `The exact same armour from the previous image, same strict T-pose, now seen strictly from the back: the backplate with a shallow spine channel and its own lower lames, the rear lames of both pauldrons, the points of the couters, the hinge lines and buckles of rerebraces and vambraces, the back of the tall doublet collar with the mail collar around its base, a leather strap with a buckle between the shoulder blades for fastening a helmet, the waist belt, the fauld and the dagged mail skirt running all the way round. Mail fills the armpits and every space between the plates; no bare skin, no gaps. Same proportions, materials and wear.` + хвост
3. **Left** — `The exact same armour, same strict T-pose, now seen strictly from its left side, the chest facing the left edge of the image: the left arm points straight at the camera, so the sleeve is seen end-on as concentric rings — the vambrace cuff with dark empty interior in the centre, the couter wing around it, the dome of the left pauldron outermost. Around and below it the profile of the torso: the bulging curve of the breastplate and plackart in front, the flatter backplate behind, side hinges and buckles joining them, the tall standing collar in profile, the fauld lames and the mail skirt hanging to the top of the thigh. Mail fills the armpit below the pauldron; no bare skin, no gaps. Same proportions, materials and wear.` + хвост

Tripo: текстура — да, ~15k полигонов, высота от ворота до подола ≈ 0.75 м, размах по манжетам ≈ 1.45 м. Модель в T-pose скинится на риг персонажа (как тканевый комплект), а не вешается жёсткими кусками на кости.

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
