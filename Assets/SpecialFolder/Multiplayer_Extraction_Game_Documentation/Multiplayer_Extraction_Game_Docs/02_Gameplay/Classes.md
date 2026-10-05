# Classes

Классы придумываются с нуля. В игре четыре класса: **Barbarian** (`Id = 0`), **Wizard** (1), **Sellsword** (2), **Chaplain** (3). Данные классов лежат в `Editor/Dungeon/DungeonClassLibrary.cs`, ассеты генерируются в `Configs/Dungeon/Classes` через `Tools/Game/Dungeon/Build Content`.

Атрибуты описаны в `Attributes.md` («Гексаграмма»): 6 атрибутов, сумма 90.

## Class Framework
Для каждого класса:

- Identity:
- Role:
- Attributes (Сила / Живучесть / Дух / Знание / Ловкость / Проворство):
- Strengths:
- Weaknesses:
- Weapons:
- Abilities:
- Passive:
- Team synergy:
- Solo viability:
- Counterplay:

## Barbarian
- Identity: живучий боец с двуручным топором, крики вместо магии.
- Attributes: 21 / 22 / 8 / 9 / 15 / 15.
- Weapons: топоры, мечи, булавы, копья, кинжалы, факел.
- Abilities: Battle Roar, Unyielding Shout. Перки: Thick Hide, Giant's Constitution, Axe Mastery, Bloodlust.
- Outfits: Marauder (Сила), Berserker (Живучесть).

## Wizard
- Identity: мастер заклинаний, два колеса по пять заклинаний, заряды возвращаются только у костра.
- Attributes: 8 / 10 / 23 / 21 / 15 / 13.
- Weapons: посохи, книга заклинаний, хрустальный шар, кинжалы, мечи.
- Abilities: Spell Memory I/II, Arcane Shield, Quick Chant; 10 заклинаний.
- Outfits: Mystic (Дух), Occultist (Знание).

## Sellsword
- Identity: наёмник, аналог Fighter из Dark and Darker. Владеет любым оружием, кроме магического; магии нет вообще.
- Attributes: 18 / 18 / 9 / 10 / 17 / 18.
- Weapons: мечи, топоры, булавы, кинжалы, копья, луки, арбалеты, боевой посох (Quarterstaff), щиты, факел. Magic Staff, книга и шар недоступны.
- Armor: единственный класс, носящий латы целиком (комплект Ironclad).
- Skills: **Rally** — 40 здоровья за 8 с (растёт от Physical Healing), откат 45 с. **Onslaught** — +20% Action Speed на 8 с, откат 35 с.
- Perks: **Bulwark** (+35 брони, +10% здоровья), **Weapon Drill** (+6 физ. силы, +8% физ. урона), **Fleet Footwork** (+14 скорости передвижения), **Quick Hands** (+10% Action Speed).
- Builds: танк (Ironclad + щит + Bulwark + Rally), урон (двуручное оружие + Weapon Drill), скорость (лёгкая броня + Fleet Footwork + Quick Hands + Onslaught).
- Outfit: Ironclad — латы, 305 брони, −24 скорости, +6 Живучести.

## Chaplain
- Identity: боевой священник рассвета, аналог Cleric из Dark and Darker. Лёгкий хилер в ткани или медленный «паладин» в латах.
- Attributes: 15 / 14 / 21 / 16 / 12 / 12.
- Weapons: дробящее (булавы, моргенштерн, боевой молот), посохи, книга заклинаний, щиты, факел.
- Armor: из лат Ironclad носит кирасу, поножи и сабатоны; койф и наручи — только Sellsword.
- Skills (два из трёх): **Prayer Memory** — колесо молитв (одно колесо на 5 заклинаний, читаются с книгой или магическим посохом в руках). **Hallow Weapon** — свет на своём оружии или оружии союзника под прицелом: +8 маг. урона за удар на 12 с, откат 35 с. **Rebuke** — вспышка света: 25 маг. урона и стаггер всем в 4,5 м, откат 30 с.
- Spells: **Mending Prayer** (лечит союзника под прицелом или себя на 22, 4 заряда), **Aegis** (щит на 25 урона на 15 с, 3), **Benediction** (+5 Силы на 30 с, 3), **Sunlance** (снаряд света, 22 маг. урона и стаггер, 4), **Circle of Dawn** (все в 5 м, включая себя, лечатся на 30 за 6 с, 2).
- Perks: **Devotion** (+6 Духа), **Litany** (+6 Знания: +1 заряд, быстрее каст), **Zealot** (+6 физ. силы, +8% физ. урона), **Iron Vow** (+20 брони, +15 маг. сопротивления).
- Builds: хилер (Devout + Prayer Memory + Devotion/Litany), паладин (булава, щит, латы + Hallow Weapon + Rebuke или Prayer Memory, Zealot + Iron Vow).
- Outfit: Devout — ткань, +9 Духа (21 → 30).

Лечащие, защитные и усиливающие заклинания (Heal, Shield, Buff) любого класса действуют на искателя под прицелом, при промахе — на самого заклинателя.

## Class List
| Class | Role | Status |
|---|---|---|
| Barbarian | Bruiser | Prototype |
| Wizard | Caster | Prototype |
| Sellsword | Weapon generalist / Tank | Prototype |
| Chaplain | Healer / Frontline | Prototype |
