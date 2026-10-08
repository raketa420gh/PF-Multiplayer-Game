# Combat Balance

## Time To Kill
`[Target]`

## Attack Chain
Урон и длительность удара (замах + активная фаза + восстановление) при базовой скорости действий; источник — `Editor/Dungeon/DungeonWeaponLibrary.cs`.

| Weapon | Chain Length | Damage | Attack Time, s | Riposte | Notes |
|---|---:|---|---|---|---|
| Arming Sword | 3 | 27 / 27 / 30 | 1.2 / 1.18 / 1.35 | ×1.5 урона, 1.11 с | без щита |
| Sword & Shield / Sword & Écu | 3 | 22 / 22 / 30 | `[ ]` | `[ ]` | связка выбирается по щиту |
| Morning Star | 3 | 34 / 36 / 37 | 1.13 / 1.13 / 1.13 | 51, 0.9 с | без щита |
| Mace & Shield / Mace & Écu | 3 | 31 / 31 / 31 | `[ ]` | `[ ]` | третий удар — стаггер 0.3 |
| Battle Axe | 3 | 43 / 43 / 50 | 1.84 / 1.79 / 1.83 | 65, 1.49 с | двуручное |
| Magic Staff | 2 | 29 / 32 | 1.2 / 1.53 | 44, 1.01 с | двуручное, фокус |
| Spellbook | 1 | 18 | 1.19 | — | фокус |
| Crossbow | 1 | 30–39 | натяжение 0.3 | — | ручная перезарядка 1.8 с, болты из сумки |

## Block
| Parameter | Value |
|---|---:|
| Block Angle Tolerance | `[TBD]` |
| Block Damage Mitigation | `[TBD]` |
| Block Impact Duration | `[TBD]` |
| Block Recovery | `[TBD]` |
| Partial Block Damage | `[TBD]` |
| Block Stamina Cost | Нет — стамины в игре нет |

### Impact / Stability
Удар с Impact больше Stability блока пробивает блок.

| Weapon | Impact | Stability |
|---|---:|---:|
| Arming Sword | 4 | 3 |
| Morning Star | 6 | 3 |
| Battle Axe | 7 | 4 |
| Magic Staff | 4 | 4 |
| Spellbook | 1 | 1 |
| Crossbow | 5 | 1 |
| Sword & Shield / Mace & Shield | 4 / 6 | 7 |
| Sword & Écu / Mace & Écu | 4 / 6 | 8 |

## Damage
`[ ]`

## Armor
`[ ]`

## Stamina
Стамины нет (решение проекта).

## Weapon Table
| Weapon | Type | Hand | Damage | Speed | Range | Stamina | Block | Chain |
|---|---|---|---:|---:|---:|---:|---|---:|
| `[ ]` | One-handed | Right/Left | `[ ]` | `[ ]` | `[ ]` | `[ ]` | Yes/No | 2–3 |
| `[ ]` | Two-handed | Both | `[ ]` | `[ ]` | `[ ]` | `[ ]` | Yes/No | 2–3 |
| `[ ]` | Shield | One hand | `[ ]` | `[ ]` | `[ ]` | `[ ]` | Yes | N/A |
| `[ ]` | Ranged | Both | `[ ]` | `[ ]` | `[ ]` | `[ ]` | TBD | `[ ]` |

## Test Scenarios
- Vertical attack vs correctly aligned shield.
- Vertical attack vs partially misaligned shield.
- Attack during block impact lockout.
- Right-hand one-handed weapon input mapping.
- Left-hand one-handed weapon input mapping.
- Two-handed weapon input mapping.
- Combo continuation at the edge of the allowed input window.
- Early/late combo input.
