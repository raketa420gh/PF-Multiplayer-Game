# Classes

В игре шесть классов из `03_World/Lore.md`: **Mercenary** (`Id = 0`), **Librarian** (1), **Alchemist** (2), **Confessor** (3), **Huntsman** (4), **Knave** (5). Данные классов лежат в `Editor/Dungeon/DungeonClassLibrary.cs`, ассеты генерируются в `Configs/Dungeon/Classes` через `Tools/Game/Dungeon/Build Content`. Лор, цитаты и описания подклассов — в `Lore.md`, здесь только игровые данные.

Атрибуты описаны в `Attributes.md` («Гексаграмма»): 6 атрибутов, сумма 64, нейтраль 10.

## Class Framework
Для каждого класса:

- Identity:
- Role:
- Attributes (Сила / Живучесть / Дух / Знание / Ловкость / Проворство):
- Strengths:
- Weaknesses:
- Weapons:
- Subclasses:
- Abilities:
- Passive:
- Team synergy:
- Solo viability:
- Counterplay:

## Subclasses
- У каждого класса три подкласса (`ClassConfig.Subclasses`), выбираются вкладками на странице Skills; сохраняются под ключом `subclass`. Открываются на 30/40/50 уровне, до этого — только общее (см. `Progression.md`).
- Каждый навык, заклинание и перк либо общий (`Subclass = -1`), либо принадлежит одному подклассу; при смене подкласса билд обрезается до доступного (`FitBuild`).
- Ресурс-стеки подклассов (`AdventurerComponent.Resource`, `ResourceMax`) в коде есть, но сейчас выключены: у всех подклассов `ResourceMax = 0`.
- Стамины нет.

## Weapons
- Доступ к оружию задаётся по `WeaponClass` (`ClassConfig.AllowedWeapons`); предмет может дополнительно ограничить список классов (`WeaponItemConfig.Classes`): Magic Staff — Librarian и Confessor, Viking Sword — только Mercenary.
- Метательное (Francisca Axe, Throwing Knife) и факел — утилиты, их доступ классом не ограничен.
- Перк Mercenary **Anything Sharp** (Hewer) снимает ограничение по оружию.
- **Любимое оружие** (2026-11-03) — подсказка игроку, с чем класс играется лучше всего; доступ к оружию оно не меняет. Mercenary — Arming Sword + Écu, Librarian — Magic Staff, Alchemist — не решено, Confessor — Morning Star, Huntsman — луки и арбалеты, Knave — кинжал (ещё не сделан).

## Mercenary
- Identity: солдат распущенной армии, пошёл в жилу за деньги; простая сталь, без магии.
- Attributes: 12 / 15 / 10 / 5 / 10 / 12 (задал пользователь 2026-11-14).
- Weapons: мечи, топоры, булавы, копья, кинжалы, щиты, факел. Стартовый набор: Arming Sword + Écu, Battle Axe, Bandage ×3, Torch.
- Любимое оружие: Arming Sword + Écu.
- Subclasses: **Shieldbearer** (только щит, живучий), **Hewer** (быстрый, любое оружие), **Captain** (поддержка отряда).
- Skills, общие (утверждены 2026-11-13):
  - **Second Wind** — как в DaD: 40% HP за 12 с, один заряд; возвращается только отдыхом у костра за 15 с (серый костёр горит 18 с, как junk Campfire Kit в DaD; 2 с уходит на то, чтобы сесть). У навыков с флагом `AbilityFlags.Campfire` `Cooldown` = время восстановления у костра.
  - **Grit Your Teeth** — +15 п. к Physical Reduction на 6 с, перезарядка 60 с (эффект `StatusEffectKind.PhysicalReduction`).
  - **Over Here!** — крик-провокация: монстры в 8 м выбирают тебя целью 4 с (`StatusEffectKind.Taunt`, монстр видит тебя в 10 раз ближе), все враги в 8 м (монстры и игроки) на 6 с наносят на 10% меньше физического урона (`StatusEffectKind.PhysicalWeakness`: −10 п. п. бонуса физического урона; изменено 2026-11-16, было замедление 20% на 4 с). Перезарядка 30 с, каст 0,3 с, флаг `Shout`. Стоит последним в списке навыков класса — билды хранят навыки по индексу.
- Skills подклассов (Shieldbearer переделан 2026-11-14):
  - **Shield Bash** — удар щитом на месте, без рывка: 8 физ. урона в конусе 40° на 2,2 м, отброс 3, стаггер 0,3 с, перезарядка 16 с, нужен щит.
  - **Brace** (вместо The Wall) — 6 с: блок не пробивается (Impact не ломает guard), блок не замедляет, каждый заблокированный удар ближнего боя отбрасывает атакующего (сила 4). Перезарядка 30 с, нужен щит (`StatusEffectKind.Brace`).
  - **Fury** (Hewer, изменён 2026-11-14) — 8 с: +20% скорости действий и +5% скорости передвижения (было +15%), перезарядка 45 с.
  - **Shoulder Blow** (Hewer, изменён 2026-11-14) — следующий физический удар оружием в течение 10 с получает +40 п. п. к бонусу физического урона (складывается с бонусом от Physical Power и аффиксов, а не умножает итог; было ×1,5 к итогу). Перезарядка 20 с (`StatusEffectKind.HeavyBlow`).
  - **Hold the Line!** (Captain, изменён 2026-11-14) — ты и союзники в 8 м получают +15 п. к Physical Reduction и +15 п. к Magical Reduction на 8 с (было −15% любого урона через `Guard`). Перезарядка 40 с, флаг `Shout`. PDR-часть — тот же `StatusEffectKind.PhysicalReduction`, что у Grit Your Teeth: они не складываются. MDR — `StatusEffectKind.MagicalReduction`; второй эффект у `AreaBuff` задаётся через `SecondEffect`.
  - Остальные: Charge!, Banner (Captain).
- Perks: общие Hardened, Plate Habit, Used to Wounds, Head Down, Long Breath; Steady Stance, Ready Again, Counterblow, Heavy Bones / Anything Sharp, Light Step, Onslaught, Quick Hands / Under the Banner, Set the Example, Battle Order, Voice.
  - **Head Down** (2026-11-16, вместо Drill Arms) — +20% Projectile Reduction (`StatType.ProjectileReduction`): меньше урона от стрел, болтов и метательного оружия.
  - **Long Breath** (2026-11-16, вместо Field Rations) — +25% Buff Duration (`StatType.BuffDuration`): баффы и крики своих навыков действуют дольше (Grit Your Teeth 7,5 с, Fury 10 с, Hold the Line! 10 с на всех союзниках, провокация Over Here! 5 с). Не удлиняет Second Wind, зоны (Banner) и эффекты на врагах.
  - **Steady Stance** (Shieldbearer, изменён 2026-11-16) — блок замедляет вдвое меньше (множитель скорости в блоке 0,55 → 0,775) и поднимается на 20% быстрее: в него влит Shield Drill. Полное снятие замедления осталось только у Brace. Два эффекта у одного перка — `PerkDefinition.SecondEffect`.
  - **Ready Again** (Shieldbearer, 2026-11-16, на месте Shield Drill) — +15% Cooldown Recovery (`StatType.CooldownRecovery`): Shield Bash 16 → 13,9 с, Brace 30 → 26,1 с. Перки в билдах хранятся маской по индексу — перк заменён на месте, список не сдвигался.

## Librarian
- Identity: изгнанный библиотекарь Коллегии, одно колесо из пяти заклинаний, заряды возвращаются у костра. Хрупок в ближнем бою.
- Attributes: 6 / 7 / 16 / 15 / 11 / 9.
- Weapons: посохи, книга заклинаний, факел. Стартовый набор: Spellbook, Magic Staff, Campfire Kit, Bandage ×2, Torch.
- Любимое оружие: Magic Staff.
- Subclasses: **Flamer**, **Frostbinder**, **Stormcaller**.
- Skills: Reread, Concentration.
- Spells: общие Lantern, Prickly Word; Fireball, Wall of Fire, Funeral Pyre (Flamer); Ice Shard, Rime, Frozen Shackles (Frostbinder); Lightning, Chain Lightning, Thunderstorm (Stormcaller).
- Perks: общие Well-Read, Firm Word, Frugal Word, Night Copyist, Long Hours; Hot Flame, Long Smoulder, Burnt Hands, Funeral Heat / Piercing Cold, Brittleness, Rime on the Skin, Winter Memory / Dry Air, Iron Calls, Long Chain, Quick Word.

## Alchemist
- Identity: выгнанный подмастерье гильдии алхимиков, добывает оссарит на реагенты; варева из своей фляги и бросаемые колбы.
- Attributes: 10 / 11 / 9 / 13 / 10 / 11.
- Weapons: мечи, булавы, кинжалы, посохи, копья, факел. Стартовый набор: Arming Sword, Potion of Healing ×2, Bandage ×2, Torch.
- Любимое оружие: не решено.
- Subclasses: **Physician**, **Bombardier**, **Mutagenist**.
- Skills: общие Healing Brew, Invigorating Brew; Splash of Life, Fortifying Mix (Physician); Blasting Flask, Fire Flask (Bombardier); Bull's Blood, Quicksilver Mix (Mutagenist).
- Perks: общие Potion Lore, Provident, Iron Stomach, Reagent Sense, Steady Pour; Healthy Spirit, Generous Flask, Calm Hands, Pure Blood / Caustic Mix, Strong Charge, Wide Spread, Used to the Roar / Accustomed, Double Dose, Sinewy, Buzz.

## Confessor
- Identity: погребальщик Собора Упокоения, отпевает мёртвых жилы; булава, щит и одно колесо молитв.
- Attributes: 11 / 10 / 15 / 11 / 8 / 9.
- Weapons: булавы, посохи, книга заклинаний, щиты, факел. Стартовый набор: Morning Star + Round Shield, Spellbook, Campfire Kit, Bandage ×2, Torch.
- Любимое оружие: Morning Star.
- Subclasses: **Infirmarian** (Лазаретник, до 2026-11-17 Healer), **Smiter**, **Apostate**.
- Skills (изменены 2026-11-16, заменены на месте — билды хранят навыки по индексу):
  - **Psalm by Heart** (вместо Rosary) — следующая молитва в течение 10 с читается мгновенно, перезарядка 45 с (`AbilityKind.QuickCast`).
  - **Absolution** (вместо Holy Water) — снимает Slow, Burn, Poison и Cursed с себя и союзников в 3 м, перезарядка 45 с, каст 0,4 с (`AbilityKind.Cleanse`, `StatusEffectComponent.Cleanse`). Root, Silence, Vulnerable, Weakened и побочки микстур не снимает.
- Spells: общие Minor Healing, Rest, Holy Light, Bind, Holy Bolt; Healing Touch (Целебное касание: удержание, союзник в 1,5 м, 2%/с без лимита, кастер стоит), Circle of Intercession, Blessing (Infirmarian); Hallowed Weapon, Sentence, Light of the Cathedral (Smiter); Wither, Curse, Whisper of the Sleeper (Apostate).
  - **Rest** (2026-11-17, механика прежняя, новое описание) — 11 магического урона светом, по нежити вдвое больше.
  - **Holy Light** (2026-11-17) — светящийся шар над точкой под прицелом до 15 м на 60 с, 2 заряда (`ZoneKind.HolyLight`).
  - **Bind** (2026-11-17) — цель под прицелом до 15 м не может сделать шаг 1,5 с (Root), 2 заряда.
  - **Holy Bolt** (2026-11-17) — снаряд света, 15 магического урона, нежить отбрасывает, 4 заряда.
  - Новые молитвы добавлены в конец списка — раскладки колёс хранят молитвы по индексу.
- Perks: общие Firm Faith, Requiem, Pilgrim, Scourge, Clean Conscience; Warm Hands, Shroud, Patience, Last Rites / Heavy Hand, Strength of Faith, Righteous Wrath, Blood Tithe / Blood Prayer, Louder in the Dark, Rot, Take Back.
  - **Scourge / Бичевание** (2026-11-16 как Hair Shirt, вместо Gravedigger's Back; переименован 2026-11-17) — Flat Reduction 3 (`StatType.FlatReduction`): на 3 урона меньше от любого источника, но не меньше 1; горение и яд теряют 3 от всего урона каждого наложения.
  - **Clean Conscience** (2026-11-16, вместо Night Vigil) — +20% Debuff Reduction (`StatType.DebuffReduction`): вредные эффекты на тебе длятся на 20% меньше.
  - **Firm Faith** (2026-11-17) — +5 Spirit, −2 Strength (было +3 Spirit).
  - **Requiem** (2026-11-17) — нежить наносит тебе на 25% меньше урона (было +15% урона по нежити): `PerkEffect.Requiem` теперь режет входящий урон, если `HitRequest.Attacker` — нежить.
  - **Pilgrim / Паломник** (2026-11-17, вместо Altar Boy) — штраф скорости ходьбы от книги и посоха в руках вдвое меньше (`PerkEffect.Pilgrim` 0.5).

## Huntsman
- Identity: охотник из мёртвых лесов, теперь охотится на тварей жилы; арбалет и лук, меч на крайний случай. Без магии.
- Attributes: 8 / 9 / 6 / 11 / 15 / 15.
- Weapons: луки, арбалеты, армейский меч, копья, факел. Стартовый набор: Crossbow, Arming Sword, Throwing Knife ×2, Crossbow Bolts ×20, Bandage ×2, Torch.
- Любимое оружие: луки и арбалеты.
- Subclasses: **Marksman**, **Tracker**, **Trapper**.
- Skills: общие Exhale, Full Quiver; Armor-Piercing, Pin Down (Marksman); Flare Arrow, Tracking (Tracker); Hunting Trap, Hunting Net (Trapper).
- Perks: общие Beast Habit, Knack, Forest Hardened, Lean Winters, Clean Kill; Taut String, Long Shot, Finishing Shot, Steady Hand / Cat's Eye, Trap Sense, Vein Expert, Hunter's Mark / Heavy Jaws, Poisoned Teeth, Iron Stock, Prey in the Snare.
- Из луков в игре есть Recurve Bow.

## Knave
- Identity: расхититель могил, которого Собор гонял с кладбищ; жила — самое большое кладбище, здесь та же работа оплачивается. Яды, ножи, удар в спину.
- Attributes: 8 / 8 / 6 / 10 / 16 / 16.
- Weapons: мечи, кинжалы, арбалеты, луки, копья, факел. Стартовый набор: Arming Sword, Throwing Knife ×2, Lockpick, Bandage ×2, Torch.
- Любимое оружие: кинжал (ещё не сделан; пока в наборе Arming Sword).
- Subclasses: **Assassin**, **Poisoner**, **Thrower**.
- Skills: общие Blade Venom, Take to Your Heels; Open Up, Finish Him (Assassin); Venom Bubble, Dead Root (Poisoner); Spare Knife, Knife Fan (Thrower).
- Perks: общие Knows Where to Look, Light-Footed, Nimble Fingers, Graveyard Feet, Gallows Luck; First Blood, Short Blade, For the Throat, Clean Work / Thick Venom, Caustic Venom, Weakness, Rotten Blood / Bundle of Knives, True Throw, Quick Hand, Oiled Blades.

## Spell Rules
- Лечащие, защитные и усиливающие заклинания (Heal, Shield, Buff) любого класса действуют на искателя под прицелом, при промахе — на самого заклинателя.
- Заклинания на зарядах не запускают откат после каста: следующий каст доступен сразу, пока есть заряды.
- Наземные заклинания показывают круг на земле под прицелом во время каста (`SpellMarkerView`), не дальше `Range`. Тип наведения — `AbilityConfig.Targeting`: Self / Hitscan / Ground / Projectile / Aura.

## Class List
| Class | Role | Status |
|---|---|---|
| Mercenary | Weapon generalist / Tank / Support | Prototype |
| Librarian | Caster | Prototype |
| Alchemist | Support / Thrower | Prototype |
| Confessor | Healer / Frontline | Prototype |
| Huntsman | Ranged / Trapper | Prototype |
| Knave | Assassin / Poisoner | Prototype |
