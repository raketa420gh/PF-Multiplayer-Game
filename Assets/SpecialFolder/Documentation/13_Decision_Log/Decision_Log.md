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
**Status:** Superseded by DD-009

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

## DD-009 — Hexagram Rework: Simple Names, Separate Speeds
**Date:** 2026-10-05
**Status:** Accepted

**Context**
Названия основ (Плоть, Хватка, Реакция…) были вычурными. Move Speed и Action Speed росли от общих основ, билд «быстро бегает» и «быстро действует» не разделялся. Weakpoint позволял атрибутами усиливать урон в голову. Лечение было одним статом Mending.

**Decision**
Кольцо: Сила – Живучесть – Дух – Знание – Ловкость – Проворство. Move Speed — только от Ловкости, Action Speed — только от Проворства. Маг выбирает Дух (урон, лечение) или Знание (скорость чтения, память). Рёбра: Physical Healing, Magical Healing, Magical Interaction, Perception, Interaction Speed, Handling. Weakpoint, Control Resistance и Toughness удалены; Cooldown Recovery даёт только экипировка. Подробности — `02_Gameplay/Attributes.md`.

**Consequences**
`StatType`, `StatId`, `ClassStats` переименованы; классы, предметы и сцены пересобраны. `IHitModifier.WeakpointMultiplier` удалён, урон в голову меняет только экипировка. `InteractableComponent.IsMagical` переключает удержание F на Magical Interaction (алтари). Порог Реакции (+Action Speed после стаггера) удалён.

## DD-010 — First Dungeon: Two Floors
**Date:** 2026-10-06
**Status:** Accepted

**Context**
Нужна структура первого подземелья.

**Decision**
Два этажа. Этаж 1 «Проклятая деревня» (название рабочее) — открытая карта 5×5 с фиксированной раскладкой: деревня, ферма, кладбище, болото. Этаж 2 «Запутанные гробницы» — сетка 5×5 заранее смоделированных квадратных комнат, расставляемых случайно (аналог Forgotten Castle из Dark and Darker); в каждой комнате по 2 проёма на сторону ближе к углам, комнаты стыкуются как пазл. Спуск на этаж 2 — гробницы на кладбище (каменная дверь с решёткой) и подвал фермы (решётка); оба нужно открыть.

**Consequences**
Все комнаты гробниц делаются на одном модуле с одинаковыми позициями проёмов. Нужен генератор раскладки этажа 2 и переход между этажами. Подробности — `03_World/Map_Design.md`.

## DD-011 — Release on Dedicated Servers, Parties, Late-Join Window
**Date:** 2026-10-07
**Status:** Accepted

**Context**
Релиз будет на выделенных серверах. Нужно сетевое тестирование: соло против соло в одном данже, пати для трио, общий данж для игроков, пришедших не одновременно.

**Decision**
Данж — сессия Fusion `GameMode.Server`. Очереди Solo (до 8 игроков) и Trio (до 12, команды до 3, вдвоём тоже можно). Пати — скрытая сессия таверны `party-<code>` с четырёхзначным кодом, лидер записывает всех. Матчмейкер (`DungeonTicket`) берёт самую заполненную открытую сессию очереди с местом для всей пати, иначе выделяет новый сервер. Окно позднего входа — 180 с от первого игрока, затем сессия закрывается. Каждая команда стартует в своём доме деревни, тиммейты — вместе. Без серверной сборки игрок хостит данж сам (Host fallback).

**Consequences**
`LaunchPlan` (`DungeonTicket`, `ServerLaunch`, `PartyLaunch`) передаётся между сценами через `NetworkLaunch`; `DungeonAdmission` ведёт команды и окно; `GameServer` читает аргументы `-dedicatedServer -session -mode -floor`. Серверная сборка должна совпадать со сценами клиента. В Host fallback сессия умирает с уходом хоста. Подробности — `04_Multiplayer/Matchmaking.md`.

## DD-012 — Every Floor Is a Separate Session
**Date:** 2026-10-07
**Status:** Accepted

**Context**
Этажи одного данжа жили в одной сессии; уточняет переход между этажами из DD-010.

**Decision**
Каждый этаж — отдельная сессия, подбираемая как новый забег. Портал спуска переносит авантюриста в сессию следующего этажа: снаряжение сохраняется как при побеге, здоровье, заряды заклинаний, убийства и опыт переносятся. Внизу предпочитается сессия своей пати, иначе самая заполненная открытая — игроки из разных верхних данжей могут встретиться или нет. Решётка подвала открывается сама на половине таймера этажа; портал побега поднимается на пьедестале через 60 с, отмечен на карте и открывается удержанием F.

**Consequences**
`FloorTransfer`, состояние сессии `Descending`, свойство сессии `floor`, аргумент сервера `-floor`. Неудачный спуск — возврат в таверну с сохранённым снаряжением. Статус-эффекты и стаки ресурсов не переносятся. В Host fallback спустившийся хост завершает этаж для всех.

## DD-013 — Dark Swarm Only in the Second Half
**Date:** 2026-10-07
**Status:** Accepted

**Decision**
Рой начинает сжиматься со второй половины таймера этажа (стадии на 50/62.5/75/87.5% длительности, при 720 с — 360/450/540/630 с) и наносит 3 урона/с вместо 6. Зона роя рисуется на карте и миникарте.

**Consequences**
Стадии роя в `DungeonContentBuilder` задаются долями длительности матча.

## DD-014 — One Animation Set for First and Third Person, Anatomy-Limited Arms
**Date:** 2026-10-07
**Status:** Accepted

**Context**
Вид от первого и от третьего лица должен совпадать; сгенерированные позы не должны выходить за пределы анатомии.

**Decision**
Все проигрывают first-person клипы действий и удерживают верхнюю часть тела. В сгенерированных клипах мышцы ограничены человеческими диапазонами, хваты дотягиваются руками, путь оружия сохраняется запечёнными кривыми сокета (`SocketMirror` для зеркальных видов оружия).

**Consequences**
`FighterAnimComponent` у всех играет first-person клипы; ограничения суставов — `JointLimits`, `BattlePoseRig`, сборка — `BattleAnimationBuilder`.

## DD-015 — Reduced Weapon Set, Huntsman Class
**Date:** 2026-10-07
**Status:** Accepted

**Decision**
Остаются восемь видов оружия: Arming Sword, Morning Star, Battle Axe, Crossbow, Spellbook, Magic Staff, Round Shield, Écu; остальные (включая Halberd) удалены, id в сохранениях мигрированы. Новый класс Huntsman (Marksman, Trapper, Skirmisher). Арбалет перезаряжается вручную болтами из сумки (R). Спеллбук — отдельное оружие; комбо одноручного оружия выбирается по щиту во второй руке (Round Shield / Écu).

**Consequences**
Классы и стартовые наборы пересобраны под новый набор оружия. Подробности — документы классов и боя.

## DD-016 — Skeleton Enemy Roster, Lore-Based Loot
**Date:** 2026-10-08
**Status:** Accepted

**Decision**
Запланированные враги: Skeleton Swordsman (одноручный меч), Skeleton Archer (лук), Skeleton Crossbowman (арбалет), Skeleton Warrior (одноручный меч + щит), Skeleton Mage (посох + магические способности), Flying Skull (рабочее название; голова с рывком). Лут каждого врага соответствует его лору: лучник — луки и стрелы, мечник — мечи, маг — посохи и книги и т. д. Универсальный лут у всех: монеты, бинты, расходники, экипировка, драгоценности.

**Consequences**
Общий лут-тейбл монстров делится на лорную и универсальную части в `MonsterConfig`. Лук и стрелы возвращаются в каталог предметов (DD-015 их убрал). Новые конфиги и модели: Crossbowman, Warrior, Mage; Flying Head переименовывается во Flying Skull.

## DD-017 — Floor 1: Farm Start, Rich Village, Windmill, Red Portals
**Date:** 2026-10-08
**Status:** Accepted

**Decision**
Главные точки интереса этажа 1: красные порталы на этаж 2 (кладбище на северо-востоке, остров болота на юге), центральная деревня (сложное PvE, хороший лут), заброшенная ветряная мельница (средний лут, многоуровневый геймплей). Команды появляются в случайном доме фермы на северо-западе; на ферме PvE и лут попроще. Ожидаемый маршрут: ферма → портал на кладбище или на болоте, по пути — решение, рисковать ли деревней.

**Consequences**
Каменные подвалы с решёткой заменены красными порталами на круге из камней (открываются, как и раньше, на середине часов). Мельница заменила водяную мельницу Old Mill на северной дороге; декоративная мельница фермы убрана, на ферме добавлен седьмой дом. Дома деревни больше не точки старта — в них монстры и лут уровня Rich. Префаб `CellarGrate` больше не строится (файл на диске устарел).

## DD-018 — Version, Build, Stage and Steam Branch Are Separate
**Date:** 2026-10-08
**Status:** Accepted

**Decision**
Сборка описывается четырьмя независимыми идентификаторами: `Game Version` (`MAJOR.MINOR.PATCH`, `PlayerSettings.bundleVersion`, git tag `v0.7.0`), `Build` (сквозной растущий номер бинарника, генерируется при сборке), `Stage` (Pre-Alpha / Alpha / Beta / Early Access / Release Candidate / Release) и Steam branch (`default`, `playtest`, `experimental`, `internal`). Стадия в цифры не зашивается. Игроку — `v0.7.0` + мелко `Build 1842`; QA — всегда `v0.7.0 — Build 1842`. До релиза — `0.x.y`, включая Early Access; `1.0.0` — по решению о полноценном релизе.

**Reason**
Одну версию пересобирают много раз — без Build баг-репорт «сломалась эвакуация на 0.4.0» не указывает на конкретный бинарник. Стадия и аудитория меняются независимо от номера.

**Consequences**
Прежний формат `Major.Minor.Patch.Build` отменён. Нужны: генерация Build при сборке клиента и dedicated server, вывод версии в главном меню и логах, проверка совпадения Version/Build клиента и сервера. Детали — `05_Technical/Build_And_Release.md`.

## DD-019 — One-Handed Animations Are Universal Across Shields
**Date:** 2026-10-09
**Status:** Accepted

**Decision**
Любая анимация одноручного оружия работает с любым щитом и не привязана к конкретной паре. Пара «оружие + щит» = удары, рипост и стойка правой руки от оружия + положение щита на левой руке, блок, опускание блока и Stability от щита. Щит в замахе едет вместе с корпусом (поворачивается вокруг позвоночника с торсом позы), в покое стоит там, где его держит щит.

**Reason**
Раньше каждая пара (Sword & Shield, Mace & Écu…) была отдельной ручной записью со своими, устаревшими ударами, а Viking Sword со щитом не работал вовсе.

**Consequences**
`DungeonWeaponLibrary.ShieldPairs` — единственная таблица одноручного оружия и его записей каталога по `ShieldIndex`; `WithShield` собирает пары, `BattleContentBuilder` — их модели, `DungeonItemLibrary` — ссылки предмета на пары. Новое одноручное оружие = одна строка в `ShieldPairs`. Sword & Shield / Mace & Shield / Sword & Écu / Mace & Écu теперь бьют сериями Arming Sword / Morning Star из футажа (урон, тайминги, рипост — их); добавлены Viking Sword & Round Shield и Viking Sword & Écu (новые записи каталога в конце). Имена пар: `<Оружие> & <Щит>`.

**Related systems**
Combat, Weapons, Animation pipeline (`video-to-animation`).

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
