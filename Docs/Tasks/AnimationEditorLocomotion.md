# Задача: просмотр локомоции в редакторе анимаций

Статус: **выполнено** 2026-10-18. Падение = `Air` (решение пользователя), п.5 (движение превью) — не делаем.

## Цель

В редакторе анимаций (`Scenes/AnimationEditor.unity`) удобно смотреть и править все анимации ног/тела вне атак:
бег, ходьба, спринт, присед и ходьба в приседе, прыжок, прыжок с приседанием (tuck), падение, приземление,
отдых, смерть, — и цепочки этих состояний так, как они идут в игре.

## Что уже есть

- `Assets/Game/Scripts/Battle/View/AnimationTestView.cs` — списки и проигрывание. Состояния заданы фиксированными
  списками имён (`s_states`), проигрывание — `_animator.Play(state, layer, t)` + `animator.speed`, перемотка `Seek`.
  Группа **Legs** уже содержит `Locomotion, Air, Jump, Land, Rest, Death`; слайдеры Move X/Y и Crouch.
- `Assets/Game/Scripts/Battle/View/AnimationEditorView.cs` — позирование ключей поверх аниматора
  (`GetEdit(Resolve(_test.GetClip(_editLayer)))`, `ApplyEdits` в LateUpdate), кнопки слоёв Legs / Upper body.
- `Assets/Game/Scripts/Battle/Config/AnimationEditBaker.cs` — правки хранятся в `Configs/Battle/AnimationEdits.asset`,
  `IsEditable` — только `.anim` вне `Animations/BattleSource`.
- Контроллер `Animations/Battle/Fighter.controller` (генерирует `Scripts/Editor/Battle/BattleAnimationBuilder.cs`):
  - `Locomotion` — 1D по Crouch: «Stand» (Idle, WalkForward/Back/Left/Right, Run, Sprint) и
    «Crouch» (CrouchIdle, CrouchForward/Back/Left/Right), оба FreeformCartesian2D по MoveX/MoveY.
  - `Air`, `Jump`, `Land` — 1D по Crouch: свой клип ↔ CrouchIdle (`AddTuckedState`) — это и есть прыжок с приседанием.
  - `Rest` — одиночный клип, `Death` — библиотечный Death01 (не редактируется).
- В игре: `Scripts/Battle/View/FighterAnimComponent.cs` `UpdateLocomotion` — MoveX/MoveY = скорость / walk speed,
  Crouch = `CrouchAmount`, переходы `CrossFadeInFixedTime` между Jump/Air/Land/Locomotion/Rest/AttackLegs.

## Чего не хватает

1. **Отдельные пункты для каждого клипа локомоции.** Сейчас `Locomotion` — один пункт, конкретный клип выбирается
   только подбором слайдеров, а `GetClip` берёт клип с максимальным весом. Нужны пункты-пресеты: Idle, Walk F/B/L/R,
   Run, Sprint, CrouchIdle, Crouch F/B/L/R — выставляют MoveX/MoveY/Crouch так, чтобы клип был единственным (вес 1).
2. **Прыжок целиком.** Пункт «Jump sequence» (и «Crouch jump» с Crouch = 1): Jump → Air → Land → Locomotion
   с теми же кроссфейдами и длительностями, что в `FighterAnimComponent` (вынести тайминги в общее место, не копировать).
   Таймлайн показывает текущий сегмент; пауза/перемотка работают по всей цепочке.
3. **Свой клип для tuck.** Сейчас поджатые ноги в воздухе = CrouchIdle, правка tuck ломает стойку в приседе.
   Сделать отдельный `AirTuck.anim` (стартовая копия CrouchIdle) и подставить его в `AddTuckedState` вместо CrouchIdle.
4. **Падение.** Отдельного клипа нет — падение играет `Air` (Jump_Loop). Решить: оставить или добавить `Fall`
   (по скорости вниз) — **спросить пользователя**.
5. **Просмотр в движении (опционально).** Превью на месте (root motion нет, бёдра закреплены). Тумблер «двигать
   персонажа» со скоростью из конфига движения — чтобы видеть скольжение ног.
6. **Остальное.** Проверить, что в списках есть всё, что играет base-слой в игре (AttackLegs, Rest, Death, hit-реакции),
   и что при FP-виде локомоция корректно видна (ноги в кадре при взгляде вниз).

## Ограничения

- Полная пересборка `BattleAnimationBuilder.Build` перегенерирует клипы и заново запекает правки из
  `AnimationEdits.asset` — новый `AirTuck` должен проходить тот же путь.
- Зеркальные клипы (Flip / левая рука) показываются запечёнными и только для чтения — не менять.
- Без `Find*`-API, только сериализованные ссылки; Views — от `DisplayableView`.

## Проверка

`Game.Scripts.Editor.RebuildVerify.Run("Battle")`, затем в сцене AnimationEditor: каждый пресет даёт вес 1 нужному
клипу (`GetClip` == ожидаемый), цепочка прыжка проходит все состояния, правка `AirTuck` не меняет `CrouchIdle`.
