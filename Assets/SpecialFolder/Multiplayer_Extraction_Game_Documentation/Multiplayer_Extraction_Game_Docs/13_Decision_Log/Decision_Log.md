# Design & Architecture Decision Log

## DD-001 — Combat Uses Weapon-Defined Attack Chains
**Date:** 2026-10-02  
**Status:** Accepted

**Context**
Ближний бой должен быть weapon-driven, а не единым универсальным набором атак.

**Decision**
Каждое melee-оружие имеет собственную серию, обычно из 2–3 ударов. Следующий удар серии запускается через отдельное окно продолжения в конце текущего замаха.

**Consequences**
WeaponData должен хранить chain и timing windows; animation state machine должна поддерживать переходы между атаками.

## DD-002 — Input Depends on Active Hand
**Date:** 2026-10-02  
**Status:** Accepted

**Decision**
Одноручное оружие в правой руке атакует ЛКМ, в левой — ПКМ. В паре с щитом в противоположной руке вторая кнопка используется для блока. Двуручное оружие: ЛКМ — атака, ПКМ — блок.

**Consequences**
Input mapping должен определяться экипировкой и не быть жёстко привязан к конкретному классу персонажа.

## DD-003 — Block Is Directional and Has Recovery
**Date:** 2026-10-02  
**Status:** Accepted

**Decision**
Блок зависит от угла столкновения. Возможна частичная защита, когда оружие одновременно задевает защитный предмет и тело. Попадание по щиту вызывает небольшой impact и временный lockout перед возвратом в защитное положение.

**Consequences**
Hit detection и сетевой authority должны учитывать ориентацию блока, активность блока и lockout/recovery.

## DD-004 — First-Person Body Uses Independent Upper-Body Pitch
**Date:** 2026-10-02  
**Status:** Accepted

**Decision**
Горизонтальный yaw вращает весь гуманоид. Вертикальный pitch в диапазоне 90° вверх/вниз управляет верхней частью тела; ноги остаются ориентированы по yaw. В crouch ноги приседают независимо от верхней части тела.

**Consequences**
Нужна отдельная animation/rig logic для upper-body pitch и crouch, а также согласованное first-/third-person представление.

## DD-005 — Classes Are Redesigned From Scratch
**Date:** 2026-10-03  
**Status:** Accepted

**Decision**
Все классы, перенесённые из клона Dark and Darker, удалены, кроме Cleric (`Id = 0`). Новые классы придумываются с нуля.

**Consequences**
Механики удалённых классов (shapeshift, spawn-ловушки, smoke) остаются в коде и доступны новым классам.

## DD-006 — Hexagram Attribute System
**Date:** 2026-10-03  
**Status:** Accepted

**Context**
Семь атрибутов Dark and Darker (STR/VIG/AGI/DEX/WILL/KNOW/RES) повторяли оригинал.

**Decision**
Шесть основ по кольцу: Плоть, Хватка, Реакция, Сноровка, Рассудок, Резонанс. Собственные характеристики основ считаются по одной общей кривой. Рёберные — от среднего геометрического двух соседей. Основа ≥ 30 открывает пассивку-порог. Подробности — `02_Gameplay/Attributes.md`.

**Consequences**
Атрибуты влияют на Poise, Guard, Impact, Weakpoint, Handling, Perception, Mending и заряды заклинаний. `DamageReceiverComponent.IDefense` заменён на `IHitModifier`, в `ICombatStats` добавлен `HandlingSpeed`.

## DD-007 — Swing Peak Lies on the Crosshair
**Date:** 2026-10-04  
**Status:** Accepted

**Context**
При прицельном ударе оружие в кадре не всегда находилось на прицеле, попадание не было гарантировано.

**Decision**
В пике каждого melee-замаха (любое оружие) `StrikePoint` оружия лежит на луче из глаз. Клип-вариант оружия обязан содержать strike point исходника, иначе у него собственный префикс клипов. Лезвие следует траектории удара, оружие и руки не трясутся (эталон — Dark and Darker).

**Consequences**
Пик проверяется при сборке (`CheckPeak` — ошибка выше 1 см, `CheckSwing`). Правила авторинга поз — в `BattleAnimationLibrary`.

## DD-008 — Generated Content Pipeline
**Date:** 2026-10-04  
**Status:** Accepted

**Context**
Ассеты боя и данжа многочисленны и часто меняются; ручная правка не масштабируется.

**Decision**
Анимации, меши оружия, PBR-текстуры, звуки, иконки, конфиги, префабы и сцены генерируются editor-билдерами (`Tools/Game/Battle/Build All`, `Tools/Game/Dungeon/Build All`). Данные правятся в `*Library`-классах, ручные правки сгенерированных ассетов перезаписываются.

**Consequences**
Новые предметы добавляются в конец `DungeonItemLibrary.CreateItems` (id = позиция, сохранения ссылаются на них). Новое оружие — вариант в `DungeonWeaponLibrary.s_variants`.

## Template

### DD-XXX — `[Title]`
**Date:** `[ ]`  
**Status:** Proposed / Accepted / Superseded

**Context**
`[ ]`

**Problem**
`[ ]`

**Options**
- A — `[ ]`
- B — `[ ]`
- C — `[ ]`

**Decision**
`[ ]`

**Reason**
`[ ]`

**Consequences**
`[ ]`

**Related systems**
`[ ]`
