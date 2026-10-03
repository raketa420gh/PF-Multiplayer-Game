# Classes

Классы придумываются с нуля. В игре пока остался только **Cleric** (`Id = 0`). Данные классов лежат в `Editor/Dungeon/DungeonClassLibrary.cs`, ассеты генерируются в `Configs/Dungeon/Classes` через `Tools/Game/Dungeon/Build Content`.

Атрибуты описаны в `Attributes.md` («Гексаграмма»): 6 основ, сумма 90.

## Class Framework
Для каждого класса:

- Identity:
- Role:
- Attributes (Плоть / Хватка / Реакция / Сноровка / Рассудок / Резонанс):
- Strengths:
- Weaknesses:
- Weapons:
- Abilities:
- Passive:
- Team synergy:
- Solo viability:
- Counterplay:

## Cleric
- Identity: святой целитель в тяжёлой броне, бич нежити.
- Attributes: 16 / 13 / 12 / 12 / 15 / 22.
- Weapons: булавы, посох, щит, факел, книга заклинаний, хрустальный шар. Любая броня.
- Abilities: Spell Memory, Divine Protection, Holy Purification, Judgement, Smite, Confession. Заклинания: Bless (+2 Хватки), Protection, Lesser Heal, Holy Strike, Holy Light, Sanctuary, Bind, Divine Strike.
- Status: прототип, перенесён из клона DaD — подлежит переработке.

## Class List
| Class | Role | Status |
|---|---|---|
| Cleric | Healer / Frontline | Prototype |
| `[TBD]` | `[ ]` | Idea |
