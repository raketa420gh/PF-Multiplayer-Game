# Animation Test Scene

Офлайн-просмотрщик анимаций бойца: любой стейт `Fighter.controller` на голом `Character.prefab` с любым оружием каталога,
без сетевой сессии Fusion. Нужен, чтобы проверять сгенерированные клипы (позы, тайминги, вид от первого лица) без запуска игры.

- Сцена: `Assets/Game/Scenes/AnimationTestScene.unity` (в Build Settings не входит)
- Сборка: `Tools/Game/Battle/Build Animation Test Scene`
- Код: `Scripts/Battle/View/AnimationTestView.cs`, `Scripts/Editor/Battle/BattleAnimationTestSceneBuilder.cs`

## Схема

```text
 EDITOR — сборка сцены
┌───────────────────────────── генерируемые ассеты ─────────────────────────────┐
│ Character.prefab     Fighter.controller    Configs/Battle/*.asset   Body.asset │
│ BattleCharacter-     BattleAnimation-      WeaponConfig, каталог    BodyConfig │
│ Builder              Builder               BattleContentBuilder     (EyePoint) │
└─────────┬───────────────────┬───────────────────────┬───────────────────┬─────┘
          └───────────────────┴───────────┬───────────┴───────────────────┘
                                          ▼
              BattleAnimationTestSceneBuilder.Build()
                • InstantiatePrefab(Character) + контроллер
                • BattlePoseRig.CreateSockets(animator) → 4 сокета
                • FindAssets("t:WeaponConfig") → _weapons
                • BattleEditorUtility.Set(...) → ссылки во вьюху
                                          ▼
┌────────────────────────── AnimationTestScene.unity ───────────────────────────┐
│ Directional Light · [Volume] · Ground · [Camera] · Character · [System]       │
└────────────────────────────────────────────────────────────────────────────────┘

 RUNTIME — один MonoBehaviour
┌────────────────────────────── AnimationTestView ──────────────────────────────┐
│                                                                                │
│  OnGUI ──┬─ оружие ──── SelectWeapon(i) ──┐                                    │
│          ├─ стейты ──── Play(layer, s) ───┼──▶ состояние                       │
│          └─ плейбек ─── Seek(t), флаги ───┘    _weaponIndex   _attack          │
│                                                _current[3]    _layer           │
│                                                _isPaused _speed _pitch …       │
│                                                     │                          │
│  Update ◀───────────────────────────────────────────┘                          │
│    • animator.speed, MoveX / MoveY / Crouch / Mirror / ActionSpeed             │
│    • веса слоёв: Death → Upper 0, Hit 0.7 пока клип идёт                       │
│    • автоповтор one-shot стейтов, ввод орбитальной камеры                      │
└──────────────┬─────────────────────────────────────────────────────────────────┘
               ▼
   Animator (Fighter.controller)
   ├─ 0 Base   Locomotion (blend tree) · Air · Jump · Land · Rest · Death · *_AttackLegsN
   ├─ 1 Upper  *_Idle · *_AttackN · *_Block · … · Cast · Use · …     маска UpperBody
   └─ 2 Hit    HitChest · HitHead · HitStagger                       маска Torso
               │ поза скелета
               ▼
   LateUpdate
     • питч → Spine / Chest / UpperChest, голова scale 0 в FP
     • WeaponVisual: трейл в фазе Active, тетива и стрела в Draw
     • камера: орбита | FP из BodyConfig.EyePoint
```

## Как устроено

### Сборка сцены
`BattleAnimationTestSceneBuilder` ничего не генерирует сам — только собирает сцену из уже готовых ассетов боевого пайплайна
и прошивает ссылки в `AnimationTestView` через `BattleEditorUtility.Set`. Сокеты оружия создаёт тот же
`BattlePoseRig.CreateSockets`, что и у `Fighter.prefab`, поэтому оружие сидит в руках так же, как в игре.

### Почему отдельная вьюха, а не игровые компоненты
`FighterAnimComponent` и `WeaponViewComponent` — `NetworkBehaviour`: они читают сетевое состояние `CombatComponent` и без
раннера не живут. `AnimationTestView` повторяет минимум их логики (параметры аниматора, веса слоёв, изгиб позвоночника по
питчу, скрытие головы, трейл и тетива), а имена стейтов, суффиксы и параметры берёт из констант `FighterAnimComponent` —
разъехаться с контроллером они не могут.

### Откуда берётся список стейтов
- Оружейные: `WeaponConfig.AnimationPrefix` + суффикс (`_Idle`, `_AttackN`, `_Block`, `_BlockImpact`, `_Deflect`, `_Draw`,
  `_Release`), в список попадают только те, для которых `Animator.HasState` вернул true.
- Общие: статический `s_states`, по массиву на слой.
- Оружие с общим префиксом (`Rapier` → `ArmingSword`) показывает те же клипы со своей моделью.

### Плейбек
Аниматор играет сам, вьюха хранит только имя текущего стейта на каждом слое (`_current[3]`) и слой в фокусе (`_layer`).

| Действие | Реализация |
|---|---|
| Запуск стейта | `Animator.Play(state, layer, normalizedTime)`, без кроссфейда |
| Пауза | `animator.speed = 0` |
| Скраб / шаг кадра | `Seek`: пауза + `Play` в нужное нормализованное время (шаг = 1/60 с) |
| Текущее время | `normalizedTime` × длина клипа из `GetCurrentAnimatorClipInfo` |
| Автоповтор | one-shot стейт перезапускается через `_repeatPause` после конца |

Длина берётся из клипа, а не из `AnimatorStateInfo.length`: на паузе (`speed = 0`) тот возвращает бесконечность.

### Связка атаки и ног
`Play` на верхнем слое для `X_AttackN` запускает на базовом `X_AttackLegsN` с тем же нормализованным временем (оба клипа
длятся `attack.Duration`), любой другой стейт возвращает ноги в `Locomotion`. `_attack = weapon.Attacks[N]` даёт фазу
(`Windup` / `Active` / `Recovery`) по `ActiveStart` / `ActiveEnd` — она выводится в панели и включает трейл.

### Камера
- Орбита: ПКМ — вращение, колесо — зум, FOV 45.
- First person: позиция глаза = `BodyConfig.TransformPoint(EyePoint, pitch, crouch)` — та же формула, что в
  `FighterBodyComponent.GetEyePosition`, FOV 75, голова скрыта.

## Ограничения
- Старт без кроссфейда: в игре `CrossFadeInFixedTime` 0.1 с, первые кадры атаки там — бленд из idle.
- Цепочки ударов (`AttackDefinition.After`) подряд не проигрываются, только по одному стейту.
- Для blend tree `Locomotion` длина — это длина первого клипа дерева, скраб приблизительный.
- Сырые клипы UAL, не заведённые в контроллер, не показываются.

## Когда пересобирать сцену
- Добавлено или удалено оружие — список `_weapons` сериализован в сцене.
- После `Build Character` — сокеты висят на костях инстанса префаба.
- Новый общий стейт (не оружейный) — дописать в `s_states` в `AnimationTestView`, пересборка не нужна.

Перегенерация клипов и контроллера пересборки не требует: GUID контроллера стабилен.
