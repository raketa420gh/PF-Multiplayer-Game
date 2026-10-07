# PvE

## Goals
PvE должен:
- создавать угрозу;
- защищать valuable loot;
- контролировать пространство;
- создавать шум;
- провоцировать PvP;
- поддерживать progression.

## Enemy Archetypes
- Melee
- Ranged
- Tank
- Assassin
- Support
- Elite
- Mini-boss
- Boss

## Current Enemies
Конфиги — `Configs/Dungeon/Monsters/*.asset` (`MonsterConfig`). Скелеты — модель Stylized Skeleton поверх скрытого боевого рига (`RetargetedModelComponent`). Skeleton Champion (босс) удалён.

| Enemy | Role | HP | Damage | Move Speed | Aggro, м | Notes |
|---|---|---:|---|---:|---:|---|
| Skeleton Swordsman | Melee | 117 | Arming Sword ×1.5 | 220 | 10 | Блокирует, скорость действий 0.7 |
| Skeleton Archer | Ranged | 70 | Bow ×1 | 210 | 14 | Стоит на месте, пока цель в радиусе атаки; скорость действий 0.85 |
| Flying Head | Assassin / charger | 60 | Таран ×1 | 230 | 12 | Без гуманоидной модели, рывок (`ChargeSpeed` 900), визг перед атакой |

Все трое — нежить, 25–30 опыта, общий лут-тейбл.

### Placement
- Этаж 1: фиксированно Skeleton Archer у часовни, Skeleton Swordsman у подвала за часовней, Flying Head у подвала на болоте; плюс точки спауна монстров в зонах и на площади деревни.
- Этаж 2 (Great Hall): Skeleton Archer, Skeleton Swordsman у западного входа в святилище, Flying Head.

## Enemy Template
- Name:
- Role:
- HP:
- Damage:
- Movement:
- AI states:
- Attacks:
- Telegraphs:
- Weaknesses:
- Loot:
- Network requirements:
