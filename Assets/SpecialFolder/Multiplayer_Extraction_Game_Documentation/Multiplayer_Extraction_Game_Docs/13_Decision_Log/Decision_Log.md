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
