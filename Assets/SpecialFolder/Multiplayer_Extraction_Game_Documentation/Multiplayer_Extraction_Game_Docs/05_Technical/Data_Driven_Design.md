# Data-Driven Design

Балансируемые данные не должны быть разбросаны по gameplay-коду.

## Data Types
- WeaponData
- WeaponHandConfiguration
- WeaponAttackChainData
- AttackData
- BlockData
- ItemData
- EnemyData
- LootTable
- QuestData
- ClassData
- AbilityData
- BossData
- MapData
- ExtractionData

## Weapon Data Requirements
Каждый weapon data asset должен как минимум описывать:
- weapon type;
- one-handed / two-handed;
- allowed hand(s);
- attack input mapping derived from active hand;
- attack chain;
- combo continuation windows;
- block availability;
- block angle/tolerance;
- block impact duration;
- block recovery;
- animation references;
- damage/range/speed parameters.

## Source of Truth
`[TBD — рекомендовано ScriptableObject/data assets с versioned gameplay data]`

## Serialization / Versioning
`[ ]`
