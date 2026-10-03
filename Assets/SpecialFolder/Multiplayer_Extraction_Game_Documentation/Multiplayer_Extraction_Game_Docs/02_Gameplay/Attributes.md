# Attributes — «Гексаграмма»

Шесть основ стоят по кольцу. У каждой основы есть **собственные** характеристики. Между соседними основами есть **рёберная** характеристика, которая считается от среднего геометрического пары `√(A·B)`. Если вкачать одну основу и бросить соседнюю, рёберная характеристика заметно проседает: 20/20 → 20, 30/10 → 17.3.

```
              ПЛОТЬ
   Восстановл. /    \ Натиск
       РЕЗОНАНС      ХВАТКА
     Плетение |      | Подвижность
       РАССУДОК      РЕАКЦИЯ
          Чутьё \    / Ловкость рук
              СНОРОВКА
```

| Основа | Код (`StatType`) |
|---|---|
| Плоть | `Flesh` |
| Хватка | `Grip` |
| Реакция | `Reflex` |
| Сноровка | `Craft` |
| Рассудок | `Insight` |
| Резонанс | `Resonance` |

Сумма основ класса — **90**, нейтральное значение — **15**. Основы прибавляются от класса, экипировки (`StatModifier`), перков и баффов (`StatusEffectKind.Grip`).

## Общая кривая

`k(V)` — кусочно-линейная функция (`DungeonFormulas.Curve`):

| V | 0 | 5 | 15 | 25 | 35 | 100 |
|---|---|---|---|---|---|---|
| k | −45% | −30% | 0 | +30% | +50% | +115% |

Ниже 15 и в диапазоне 15–25 — 3% за очко, 25–35 — 2%, выше 35 — 1%.
Характеристика считается как `база × (1 + w·k)` (`DungeonFormulas.Scale(value, w)`).

## Собственные характеристики

| Основа | Характеристика | Формула | Где работает |
|---|---|---|---|
| Плоть | Max Health | `125 × Scale(Flesh, 1.0) × (1 + бонусы%)` | `HealthComponent.SetMaxHealth` |
| Плоть | Poise | `12 × Scale(Flesh, 1.5)`: удар с уроном ниже Poise не вызывает стаггер | `IHitModifier.ModifyIncomingStagger` |
| Хватка | Physical Power | `Grip + плоские бонусы`, +% физ. урона = `k(PhysicalPower)` | `GetDamageMultiplier` |
| Хватка | Guard | `Scale(Grip, 1.0)`, умножает mitigation блока (не больше 100%) | `ModifyBlockMitigation` |
| Хватка | Load | `2 − Scale(Grip, 1.0)`, множитель штрафов к скорости от брони и оружия | `AdventurerStats` |
| Реакция | Action Speed | `Scale(Reflex, 0.5)` + бонусы | атаки, блок, натяжение лука |
| Реакция | Stagger Recovery | `Scale(Reflex, 0.8)`, длительность полученного стаггера делится на это значение | `ModifyIncomingStagger` |
| Сноровка | Interaction Speed | `Scale(Craft, 1.5)` | двери, сундуки, рычаги, порталы |
| Сноровка | Weakpoint | `Scale(Craft, 0.5)`, множитель урона атакующего по голове | `DamageReceiverComponent.ApplyHit` |
| Рассудок | Cooldown Speed | `Scale(Insight, 0.6)`, откат навыков делится на это значение | `AdventurerComponent.GetCooldownDuration` |
| Рассудок | Control Resistance | `clamp(k(Insight), −50%, 80%)`, ослабляет Slow/Root | `AdventurerStats` (move speed) |
| Рассудок | Concentration | `10 × Scale(Insight, 2.0)`, прибавляется к Poise во время каста | `ModifyIncomingStagger` |
| Резонанс | Magical Power | `Resonance + плоские бонусы`, +% маг. урона = `k`, сила лечения = `1 + k/2` | `GetDamageMultiplier`, `HealScale` |
| Резонанс | Bonus Charges | +1 заряд каждому заклинанию с зарядами на 20 / 30 / 40 | `AdventurerComponent.GetMaxCharges` |

## Рёберные характеристики (`E = √(A·B)`)

| Ребро | Характеристика | Формула | Где работает |
|---|---|---|---|
| Плоть × Хватка | Натиск (Impact) | `Scale(E, 1.0)`, множитель стаггера, который наносит атакующий | `ApplyHit` |
| Хватка × Реакция | Подвижность | `300 × Scale(E, 0.2)` + штрафы × Load, потолок 330 | `MoveSpeedRating` |
| Реакция × Сноровка | Ловкость рук (Handling) | `Scale(E, 1.0)` | смена оружия, перезарядка, зелья, бинты, утилиты |
| Сноровка × Рассудок | Чутьё (Perception) | `Scale(E, 1.0)`: громкость чужих шагов для локального игрока и скорость обнаружения предметов в контейнерах и трупах (`SearchTime(rarity) / Perception`) | `FootstepComponent`, `AdventurerComponent.SimulateSearch` |
| Рассудок × Резонанс | Плетение (Cast Speed) | `Scale(E, 0.8)` | каст заклинаний и спелл-навыков |
| Резонанс × Плоть | Восстановление (Mending) | `Scale(E, 0.6)`: входящее лечение, HoT, зелья, частота регенерации во время отдыха | `AdventurerComponent` |

## Пороги (основа ≥ 30)

| Основа | Пассивка |
|---|---|
| Плоть | Первый стаггер раз в 10 с игнорируется |
| Хватка | Блок гасит 100% урона |
| Реакция | После полученного стаггера +20% Action Speed на 2 с |
| Сноровка | Получение урона не сбивает взаимодействие и применение предметов |
| Рассудок | Каст не сбивается стаггером раз в 15 с |
| Резонанс | Первое заклинание после отдыха у костра (и в начале забега) не тратит заряд |

## Не зависит от основ

- Armor Rating → PDR и Magic Resistance (база 30 + экипировка) → MDR: кривые `DungeonFormulas.ArmorReduction / MagicReduction`, потолок 65%.
- Длительность баффов фиксирована (`AbilityConfig.Duration`).

## Пример: Cleric

| Плоть | Хватка | Реакция | Сноровка | Рассудок | Резонанс |
|---|---|---|---|---|---|
| 16 | 13 | 12 | 12 | 15 | 22 |

Голый клирик: HP 129, Poise 12.5, Move 295, Action Speed −4.5%, Cast Speed +7.6%, Mending +6.8%, +1 заряд.

## Код

- `Dungeon/Config/DungeonFormulas.cs` — кривая, `Scale`, `Edge`, константы, порог.
- `Dungeon/Core/AdventurerStats.cs` — пересчёт всех характеристик (`Recalculate`), `HasThreshold`.
- `Battle/Core/DamageReceiverComponent.cs` — `IHitModifier`: Weakpoint/Impact атакующего, Guard и Poise цели.
- `Battle/Core/CombatComponent.cs` — `ICombatStats.HandlingSpeed` ускоряет состояния `Equip` и `Reload`.
- Статы пересчитываются при смене инвентаря, формы, слота пояса и активных эффектов (`StatusEffectComponent.GetSignature`).
