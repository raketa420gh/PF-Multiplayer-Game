# Classes

В игре шесть классов из `03_World/Lore.md`: **Mercenary** (`Id = 0`), **Wizard** (1), **Alchemist** (2), **Confessor** (3), **Huntsman** (4), **Knave** (5). Данные классов лежат в `Editor/Dungeon/DungeonClassLibrary.cs`, ассеты генерируются в `Configs/Dungeon/Classes` через `Tools/Game/Dungeon/Build Content`. Лор, цитаты и описания подклассов — в `Lore.md`, здесь только игровые данные.

Атрибуты описаны в `Attributes.md` («Гексаграмма»): 6 атрибутов, сумма 90.

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
- У каждого класса три подкласса (`ClassConfig.Subclasses`), выбираются вкладками на странице Skills; сохраняются под ключом `subclass`.
- Каждый навык, заклинание и перк либо общий (`Subclass = -1`), либо принадлежит одному подклассу; при смене подкласса билд обрезается до доступного (`FitBuild`).
- Ресурс-стеки подклассов (`AdventurerComponent.Resource`, `ResourceMax`) в коде есть, но сейчас выключены: у всех подклассов `ResourceMax = 0`.
- Стамины нет.

## Weapons
- Доступ к оружию задаётся по `WeaponClass` (`ClassConfig.AllowedWeapons`); предмет может дополнительно ограничить список классов (`WeaponItemConfig.Classes`): Magic Staff — Wizard и Confessor, Viking Sword — только Mercenary.
- Метательное (Francisca Axe, Throwing Knife) и факел — утилиты, их доступ классом не ограничен.
- Перк Mercenary **Anything Sharp** (Hewer) снимает ограничение по оружию.

## Mercenary
- Identity: солдат распущенной армии, пошёл в жилу за деньги; простая сталь, без магии.
- Attributes: 19 / 19 / 8 / 9 / 17 / 18.
- Weapons: мечи, топоры, булавы, копья, кинжалы, щиты, факел. Стартовый набор: Arming Sword + Round Shield, Battle Axe, Bandage ×3, Torch.
- Subclasses: **Shieldbearer** (только щит, живучий), **Hewer** (быстрый, любое оружие), **Captain** (поддержка отряда).
- Skills: общие Second Wind, Grit Your Teeth; Shield Bash, The Wall (Shieldbearer); Shoulder Blow, Fury (Hewer); Hold the Line!, Charge!, Banner (Captain).
- Perks: общие Hardened, Plate Habit, Used to Wounds; Steady Stance, Shield Drill, Counterblow, Heavy Bones / Anything Sharp, Light Step, Onslaught, Quick Hands / Under the Banner, Set the Example, Battle Order, Voice.

## Wizard
- Identity: изгнанный книжник, одно колесо из пяти заклинаний, заряды возвращаются у костра. Хрупок в ближнем бою.
- Attributes: 8 / 10 / 23 / 21 / 15 / 13.
- Weapons: посохи, книга заклинаний, факел. Стартовый набор: Spellbook, Magic Staff, Campfire Kit, Bandage ×2, Torch.
- Subclasses: **Flamer**, **Frostbinder**, **Stormcaller**.
- Skills: Reread, Concentration.
- Spells: общие Lantern, Prickly Word; Fireball, Wall of Fire, Funeral Pyre (Flamer); Ice Shard, Rime, Frozen Shackles (Frostbinder); Lightning, Chain Lightning, Thunderstorm (Stormcaller).
- Perks: общие Well-Read, Firm Word, Frugal Word; Hot Flame, Long Smoulder, Burnt Hands, Funeral Heat / Piercing Cold, Brittleness, Rime on the Skin, Winter Memory / Dry Air, Iron Calls, Long Chain, Quick Word.

## Alchemist
- Identity: выгнанный подмастерье гильдии алхимиков, добывает оссарит на реагенты; варева из своей фляги и бросаемые колбы.
- Attributes: 14 / 15 / 13 / 18 / 15 / 15.
- Weapons: мечи, булавы, кинжалы, посохи, копья, факел. Стартовый набор: Arming Sword, Potion of Healing ×2, Bandage ×2, Torch.
- Subclasses: **Physician**, **Bombardier**, **Mutagenist**.
- Skills: общие Healing Brew, Invigorating Brew; Splash of Life, Fortifying Mix (Physician); Blasting Flask, Fire Flask (Bombardier); Bull's Blood, Quicksilver Mix (Mutagenist).
- Perks: общие Potion Lore, Provident, Iron Stomach; Healthy Spirit, Generous Flask, Calm Hands, Pure Blood / Caustic Mix, Strong Charge, Wide Spread, Used to the Roar / Accustomed, Double Dose, Sinewy, Buzz.

## Confessor
- Identity: погребальщик Собора Упокоения, отпевает мёртвых жилы; булава, щит и одно колесо молитв.
- Attributes: 15 / 14 / 21 / 16 / 12 / 12.
- Weapons: булавы, посохи, книга заклинаний, щиты, факел. Стартовый набор: Morning Star + Round Shield, Spellbook, Campfire Kit, Bandage ×2, Torch.
- Subclasses: **Healer**, **Smiter**, **Apostate**.
- Skills: Rosary, Holy Water.
- Spells: общие Minor Healing, Rest; Healing, Circle of Intercession, Blessing (Healer); Hallowed Weapon, Sentence, Light of the Cathedral (Smiter); Wither, Curse, Whisper of the Sleeper (Apostate).
- Perks: общие Firm Faith, Requiem, Altar Boy; Warm Hands, Shroud, Patience, Last Rites / Heavy Hand, Strength of Faith, Righteous Wrath, Blood Tithe / Blood Prayer, Louder in the Dark, Rot, Take Back.

## Huntsman
- Identity: охотник из мёртвых лесов, теперь охотится на тварей жилы; арбалет и лук, меч на крайний случай. Без магии.
- Attributes: 12 / 13 / 8 / 15 / 21 / 21.
- Weapons: луки, арбалеты, армейский меч, копья, факел. Стартовый набор: Crossbow, Arming Sword, Throwing Knife ×2, Crossbow Bolts ×20, Bandage ×2, Torch.
- Subclasses: **Marksman**, **Tracker**, **Trapper**.
- Skills: общие Exhale, Full Quiver; Armor-Piercing, Pin Down (Marksman); Flare Arrow, Tracking (Tracker); Hunting Trap, Hunting Net (Trapper).
- Perks: общие Beast Habit, Knack, Forest Hardened; Taut String, Long Shot, Finishing Shot, Steady Hand / Cat's Eye, Trap Sense, Vein Expert, Hunter's Mark / Heavy Jaws, Poisoned Teeth, Iron Stock, Prey in the Snare.
- Луков в игре пока нет (Изогнутый лук — отдельная задача).

## Knave
- Identity: расхититель могил, которого Собор гонял с кладбищ; жила — самое большое кладбище, здесь та же работа оплачивается. Яды, ножи, удар в спину.
- Attributes: 11 / 12 / 8 / 15 / 22 / 22.
- Weapons: мечи, кинжалы, арбалеты, луки, копья, факел. Стартовый набор: Arming Sword, Throwing Knife ×2, Lockpick, Bandage ×2, Torch.
- Subclasses: **Assassin**, **Poisoner**, **Thrower**.
- Skills: общие Blade Venom, Take to Your Heels; Open Up, Finish Him (Assassin); Venom Bubble, Dead Root (Poisoner); Spare Knife, Knife Fan (Thrower).
- Perks: общие Knows Where to Look, Light-Footed, Nimble Fingers; First Blood, Short Blade, For the Throat, Clean Work / Thick Venom, Caustic Venom, Weakness, Rotten Blood / Bundle of Knives, True Throw, Quick Hand, Oiled Blades.

## Spell Rules
- Лечащие, защитные и усиливающие заклинания (Heal, Shield, Buff) любого класса действуют на искателя под прицелом, при промахе — на самого заклинателя.
- Заклинания на зарядах не запускают откат после каста: следующий каст доступен сразу, пока есть заряды.
- Наземные заклинания показывают круг на земле под прицелом во время каста (`SpellMarkerView`), не дальше `Range`. Тип наведения — `AbilityConfig.Targeting`: Self / Hitscan / Ground / Projectile / Aura.

## Class List
| Class | Role | Status |
|---|---|---|
| Mercenary | Weapon generalist / Tank / Support | Prototype |
| Wizard | Caster | Prototype |
| Alchemist | Support / Thrower | Prototype |
| Confessor | Healer / Frontline | Prototype |
| Huntsman | Ranged / Trapper | Prototype |
| Knave | Assassin / Poisoner | Prototype |
