# Задача: шаги по типу обуви × поверхности

Статус: **сделано 2026-10-17** — маркер `FootstepSurfaceTag` (террейн — `FootstepTerrainSurfaceTag` по слоям), Heavy = leather с pitch 0.9, добавлены Water/Grass/WoodStair.

## Цель

Шаги игрока и мобов звучат по паре «обувь × поверхность под ногой», как в Dark and Darker (`Footsteps_1p`,
switch `FootstepsShoesType` = `<Bare|Leather|Plate>_<Surface>`). Сейчас звук выбирается только по обуви.

## Что уже есть

### Сгенерированные звуки (CC0-слои, сверены с DaD по цифрам)

`D:/DaD_Sounds/Generated/player_step_<shoe>_<surface>/player_step_<shoe>_<surface>_01..12.wav`

| shoe \ surface | stone | wood | jail (железная решётка) | rubble (гравий/щебень) |
|---|---|---|---|---|
| bare | ✓ | ✓ | ✓ | ✓ |
| leather | ✓ | ✓ | ✓ | ✓ |
| plate | ✓ | ✓ | ✓ | ✓ |

- Длины: stone 0.65–0.8 с, wood 0.57–0.79 с, jail 1.7–1.8 с (долгий звон), rubble 0.6–0.8 с.
- Пиковые уровни как в DaD: bare_stone −5, leather_stone −10, bare_wood −12, bare_jail −8, leather_jail −10,
  plate_jail −6, остальные −3…−4 dBFS. Нормализовать при импорте **не надо** — разница громкости задумана.
- Отклоняется: `bare_rubble_02` ярче остальных (~6 дБ).
- Рецепты: `.claude/skills/dad-sound-alike/recipes/player_step_*.json` (пересборка — `py -I scripts/layer_build.py <recipe>`).
- Референсы DaD (только для сравнения, не шипить): `D:/DaD_Sounds/Footsteps_1p/<Shoe_Surface>/`, без общего
  слоя шороха — `<Shoe_Surface>_step/`, сам слой — `_Common_Layer/`. Есть ещё поверхности (Grass, Snow, Ice,
  Tar, Carpet, Skeleton, WoodHard, WoodStair, Water*, Blood) — их можно сгенерировать тем же скиллом `dad-sound-alike`.

### Текущий код

- `Assets/Game/Scripts/Dungeon/Config/FootstepSoundConfig.cs` — `enum Footwear { Bare, Light, Heavy, Plate }`,
  массивы клипов по обуви, `Pick(Footwear)`, `FromArmor(boots)`: нет ботинок → Bare, Cloth → Light,
  Leather → Heavy, Plate → Plate.
- `Assets/Game/Scripts/Dungeon/View/FootstepComponent.cs` — `NetworkBehaviour`, шаг по пройденной дистанции в
  `Render()`, громкость от скорости, `DungeonAudioComponent.Play(clip, position, volume, pitch)`.
- `Assets/Game/Scripts/Editor/Battle/CombatSoundBuilder.cs` — собирает `Assets/Game/Configs/Dungeon/Footsteps.asset`
  из `Assets/Game/Audio/Dungeon/Steps/<footwear>_NN.wav` (`bare|light|heavy|plate`, пустые → light),
  проставляет `_sounds`/`_footwear` в префабы мобов (`s_prefabs`).
- Сейчас в `Steps/` лежат старые `heavy_*`, `light_*`.

## Что сделать

1. **Импорт.** Скопировать takes в `Assets/Game/Audio/Dungeon/Steps/<shoe>_<surface>_NN.wav`
   (например `bare_stone_01.wav`). Старые `light_*/heavy_*` удалить после проверки.
2. **Поверхность.** `enum FootstepSurface { Stone, Wood, Jail, Rubble }` (Stone — по умолчанию).
   Определение поверхности под ногой — нужно решить (см. «Открытые вопросы»), без scene-search API.
3. **Конфиг.** `FootstepSoundConfig`: клипы по паре (обувь, поверхность); `Pick(Footwear, FootstepSurface)` с
   фолбэком на Stone, если для поверхности клипов нет.
4. **Компонент.** `FootstepComponent` на каждом шаге определяет поверхность (raycast вниз от позиции ног,
   короткий, по слою окружения) и передаёт её в `Pick`.
5. **Билдер.** `CombatSoundBuilder` собирает клипы по маске `<footwear>_<surface>_NN`.
6. **Уровни.** Разметить поверхности в кит-префабах катакомб (деревянные полы/мосты → Wood, решётки/клетки
   PrisonBlock → Jail, завалы CollapsedHall/BonePit → Rubble).
7. Проверка: `RebuildVerify.Run("Dungeon")`, затем прогон на хосте — шаги меняются при переходе камень → дерево.

## Открытые вопросы (решить через AskUserQuestion в начале)

- **Как помечать поверхность:** `PhysicMaterial` на коллайдере (сопоставление материал → surface в конфиге) /
  компонент-маркер `FootstepSurfaceTag` на кит-префабе / тег слоя.
- **Light vs Heavy:** в DaD ткань и кожа звучат одинаково (`Leather_*`). Варианты: оба → leather-набор;
  Heavy = leather-набор с pitch ниже; сгенерировать отдельный heavy (кожа тяжелее + лёгкий звон снаряжения).
- Нужны ли сразу другие поверхности (Water, Grass, WoodStair…).

## Заметки

- DaD-аудио — только референс, в проект идут только сгенерированные takes.
- Для мобов (скелеты = Bare, Juggernaut = Heavy) поверхность определяется тем же компонентом — отдельной работы не нужно.
