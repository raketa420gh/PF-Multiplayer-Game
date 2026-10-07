# Classes

Классы придумываются с нуля. В игре пять классов: **Barbarian** (`Id = 0`), **Wizard** (1), **Warrior** (2), **Confessor** (3), **Huntsman** (4). Данные классов лежат в `Editor/Dungeon/DungeonClassLibrary.cs`, ассеты генерируются в `Configs/Dungeon/Classes` через `Tools/Game/Dungeon/Build Content`.

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
- Каждый подкласс копит свой ресурс-стеки (`AdventurerComponent.Resource`): набирается от событий (`ResourceSource`), со временем спадает. Навыки тратят стеки (`ResourceCost` / `StackBonus`), перки с `PerStack` дают бонус за каждый стек.
- Стамины нет.

## Barbarian
- Identity: живучий боец с двуручным топором, крики вместо магии; на дистанции — только метательные топоры.
- Attributes: 21 / 22 / 8 / 9 / 15 / 15.
- Weapons: топоры, мечи, булавы, копья, кинжалы, факел. Стартовый набор: Battle Axe, Morning Star, Francisca Axe.
- Subclasses (ресурс, максимум): **Berserker** — Fury, 5 (удары и полученный урон); **Juggernaut** — Grit, 5 (полученные и заблокированные удары); **Warchief** — Command, 4 (удары оружием).
- Skills: общие Battle Roar, Reckless Charge. Berserker: Blood Frenzy, Savage Blow. Juggernaut: Unyielding Shout, Iron Hide. Warchief: War Cry, Intimidating Roar.
- Perks: общие Axe Mastery, Giant's Constitution, Thick Hide; Bloodlust, Rage Within / Iron Skin, Colossus / Commanding Presence, Banner Bearer.
- Outfits: Marauder (Сила), Berserker (Живучесть).

## Wizard
- Identity: мастер заклинаний, два колеса по пять заклинаний, заряды возвращаются только у костра.
- Attributes: 8 / 10 / 23 / 21 / 15 / 13.
- Weapons: посохи, книга заклинаний, кинжалы, мечи. Стартовый набор: Spellbook, Magic Staff.
- Subclasses: **Pyromancer** — Heat, 5 (попадания заклинаниями); **Cryomancer** — Rime, 3 (полученные и заблокированные удары); **Stormcaller** — Static, 3 (попадания заклинаниями).
- Skills: Spell Memory I/II, Arcane Shield; Combustion (Pyromancer), Frost Nova (Cryomancer), Overload (Stormcaller).
- Spells: общие Magic Missile, Blink, Haste, Invisibility; Fireball, Ignite Weapon (Pyromancer); Ice Bolt, Frost Weapon (Cryomancer); Chain Lightning, Lightning Strike (Stormcaller).
- Outfits: Mystic (Дух), Occultist (Знание).

## Warrior
- Identity: наёмник, аналог Fighter из Dark and Darker. Владеет любым оружием, кроме магического; магии нет вообще. Блоки и рипосты у всех подклассов одинаковы, подкласс решает, что за них даётся.
- Attributes: 18 / 18 / 9 / 10 / 17 / 18.
- Weapons: мечи, топоры, булавы, кинжалы, копья, луки, арбалеты, щиты, факел; Magic Staff и книга недоступны. Стартовый набор: Arming Sword + Round Shield, Battle Axe.
- Armor: единственный класс, носящий латы целиком (комплект Ironclad).
- Subclasses: **Duelist** — Tempo, 3 (блоки и рипосты; меч без щита); **Guardian** — Resolve, 5 (удары, остановленные щитом); **Ravager** — Momentum, 4 (удары, в том числе в блок; боевой топор).
- Skills: общие **Rally** (40 здоровья за 8 с, откат 45 с), **Sprint** (+25% скорости на 6 с, откат 30 с); Lunge, Flurry (Duelist); Iron Stance, Shield Bash (Guardian); Sunder, Wide Sweep (Ravager).
- Perks: общие Weapon Drill, Fleet Footwork, Quick Hands; Flow, Light Feet / Steadfast, **Bulwark** (+35 брони, +10% здоровья) / Rolling Strikes, Heavy Hands.
- Outfit: Ironclad — латы, 305 брони, −24 скорости, +6 Живучести.

## Confessor
- Identity: боевой священник, аналог Cleric из Dark and Darker: булава, щит и молитвы, переносящие жизнь между союзником и врагом. Наполовину хилер, наполовину боец.
- Attributes: 15 / 14 / 21 / 16 / 12 / 12.
- Weapons: дробящее (Morning Star), посохи, книга заклинаний, щиты, факел. Стартовый набор: Morning Star + Round Shield, Spellbook.
- Armor: из лат Ironclad носит кирасу, поножи и сабатоны; койф и наручи — только Warrior.
- Subclasses: **Inquisitor** — Conviction, 5 (удары оружием; мало лечения); **Absolver** — Grace, 5 (лечение, щиты, благословения); **Exorcist** — Sigils, 3 (магические попадания).
- Skills: **Prayer Memory** — одно колесо молитв на 5 заклинаний (читаются с книгой или магическим посохом в руках). **Hallow Weapon** — +8 маг. урона за удар на 12 с своему оружию или оружию союзника под прицелом, откат 35 с. Verdict, Absolution (Inquisitor); Miracle, Vow of Protection (Absolver); Seal of Renunciation, Break the Seals (Exorcist).
- Spells: общие **Mending Prayer** (лечит на 22, 4 заряда), **Aegis** (щит на 25 урона на 15 с, 3), **Sunlance** (22 маг. урона и стаггер, 4); Benediction, Penance (Inquisitor); Circle of Dawn (наземная область до 20 м, 30 лечения за 6 с в 5 м, 2), Last Blessing (Absolver); Circle of Penance (аура 10 с), Banishing Light (Exorcist).
- Perks: общие **Devotion** (+6 Духа), **Litany** (+6 Знания), **Iron Vow** (+20 брони, +15 маг. сопротивления); Zealot, Righteous Fury / State of Grace, Martyr's Resolve / Sigil Ward, Ritualist.
- Outfit: Devout — ткань, +9 Духа (21 → 30).

## Huntsman
- Identity: охотник глубин, аналог Ranger из Dark and Darker: арбалет и лук, метательные ножи и топоры, меч на крайний случай. Без магии, мало здоровья, самые быстрые руки и ноги.
- Attributes: 12 / 13 / 8 / 15 / 21 / 21.
- Weapons: луки, арбалеты, мечи, кинжалы, копья, факел. Стартовый набор: Crossbow, Arming Sword, Throwing Knife ×2, Crossbow Bolts ×20.
- Subclasses: **Marksman** — Focus, 5 (попадания издалека, хедшот даёт вдвое больше); **Trapper** — Quarry, 4 (попадания издалека); **Skirmisher** — Stride, 4 (любые попадания оружием).
- Skills: общие **Volley** (следующий выстрел за 8 с — 3 снаряда веером по 70% урона, откат 18 с), **Rapid Reload** (перезарядка на 60% быстрее на 10 с, откат 30 с), **Field Ration** (30 здоровья за 10 с, откат 40 с). **Aimed Shot** (Marksman, от 2 Focus: следующий выстрел почти вдвое быстрее и +10 урона, +8 за стек, откат 12 с). **Hunting Trap** (Trapper: одноразовый капкан в 1,5 м — 15 урона и замедление 70% на 3 с, владельца не ловит, живёт 60 с, откат 20 с). **Disengage** (Skirmisher: отскок на 5 м назад, +15% скорости, +10% за стек на 4 с, откат 12 с).
- Perks: общие Fletcher (+12% урона стрел, болтов и метательного), Practised Hands (перезарядка на 25% быстрее), Light Tread (+12 скорости), Hawk Eye (+20% урона хедшотов); Steady Aim, Deadeye / Predator, Woodcraft / Fleet Hunter, Snap Shot.
- Новые статы: `RangedDamageBonus`, `ReloadSpeed`, `HeadshotDamage`; модификаторы выстрела (Volley / QuickReload / AimedShot) применяются через `CombatComponent.IShotModifier`.
- Outfit: Stalker — кожа, +9 Проворства (21 → 30), бонусы к урону хедшотов, дальнему урону и перезарядке.

## Spell Rules
- Лечащие, защитные и усиливающие заклинания (Heal, Shield, Buff) любого класса действуют на искателя под прицелом, при промахе — на самого заклинателя.
- Заклинания на зарядах не запускают откат после каста: следующий каст доступен сразу, пока есть заряды.
- Наземные заклинания (Lightning Strike, Circle of Dawn) показывают круг на земле под прицелом во время каста (`SpellMarkerView`), не дальше `Range` (20 м). Тип наведения — `AbilityConfig.Targeting`: Self / Hitscan / Ground / Projectile / Aura.

## Class List
| Class | Role | Status |
|---|---|---|
| Barbarian | Bruiser | Prototype |
| Wizard | Caster | Prototype |
| Warrior | Weapon generalist / Tank | Prototype |
| Confessor | Healer / Frontline | Prototype |
| Huntsman | Ranged / Trapper | Prototype |
