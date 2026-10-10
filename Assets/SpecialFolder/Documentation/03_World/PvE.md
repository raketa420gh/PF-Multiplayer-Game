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
Конфиги — `Configs/Dungeon/Monsters/*.asset` (`MonsterConfig`). Скелеты — модель Stylized Skeleton поверх скрытого боевого рига (`RetargetedModelComponent`); Skeleton Warrior — пак Zombie Warrior (`Models/Monsters/ZombieWarrior`), статичные меши пака склеиваются и скинятся на гуманоидный скелет генератором `DungeonWarriorBuilder`. Skeleton Champion (босс) удалён.

| Enemy | Role | HP | Damage | Move Speed | Aggro, м | Notes |
|---|---|---:|---|---:|---:|---|
| Skeleton Swordsman | Melee | 117 | Arming Sword ×1.5 | 220 | 10 | Блокирует, скорость действий 0.7 |
| Skeleton Archer | Ranged | 70 | Bow ×1 | 210 | 14 | Стоит на месте, пока цель в радиусе атаки; скорость действий 0.85 |
| Skeleton Warrior | Tank | 165 | Sword & Écu ×1.3 | 200 | 10 | Блокирует щитом, броня +15 % (у остальных −22 %), скорость действий 0.65, 40 опыта, свой лут-тейбл `Warrior` |
| Flying Head | Assassin / charger | 60 | Таран ×1 | 230 | 12 | Без гуманоидной модели, рывок (`ChargeSpeed` 900), визг перед атакой |

Все — нежить. Swordsman, Archer и Flying Head — 25–30 опыта и общий лут-тейбл `Monster`; Warrior — лорный `Warrior` (общий + мечи, щиты, Ironclad).

Агр: монстр берёт целью видимого игрока в радиусе Aggro (у присевшего — вдвое меньше). Игрока, который только что появился и ещё не шевелился (до 20 с), монстры не замечают; ближе 18 м к стартам монстры не ставятся — см. `Map_Design.md`.

### Placement
- Этаж 1 (DD-017), сложность по зонам:
  - Ферма (старт) — без случайных точек; фиксированно Skeleton Swordsman в амбаре и Flying Head над дальним полем.
  - Деревня — случайная точка в каждом втором доме и на площади; фиксированно Iron Juggernaut, 2 Skeleton Archer и 2 Skeleton Swordsman вокруг площади.
  - Заброшенная мельница — случайные точки на каждом уровне и на галерее, ещё две во дворе.
  - Порталы — Skeleton Warrior у портала за часовней (плюс Skeleton Archer у часовни), Flying Head у портала на болоте.
  - Остальные постройки, кладбище, лесные POI и острова болота — случайные точки как прежде.
  - Случайная точка занимается с шансом `_monsterSpawnChance` (0.8), тип — по весам `DungeonDirector._monsters`.
- Этаж 2 (Great Hall): Skeleton Archer, Skeleton Swordsman у западного входа в святилище, Flying Head.

## Planned Roster
Решение DD-016. Все — нежить; оружие врага определяет его лут (см. `03_World/Loot.md`).

| Enemy | Role | Weapon | Lore loot | Status |
|---|---|---|---|---|
| Skeleton Swordsman | Melee | Одноручный меч | Мечи | Есть |
| Skeleton Archer | Ranged | Лук | Луки, стрелы | Есть (лук вернуть в каталог) |
| Skeleton Crossbowman | Ranged | Арбалет | Арбалеты, болты | Новый |
| Skeleton Warrior | Tank | Одноручный меч + щит | Мечи, щиты | Есть |
| Skeleton Mage | Caster / Support | Посох + магические способности | Посохи, книги заклинаний | Новый |
| Flying Skull (рабочее название) | Assassin / charger | — (рывок головой) | Только универсальный | Есть как Flying Head |

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
