# Changelog

## v0.6 — 2026-10-05

### Added
- Sellsword class (Fighter analogue): every weapon but the magical ones, shields, skills Rally and Onslaught, perks Bulwark, Weapon Drill, Fleet Footwork, Quick Hands.
- Chaplain class (Cleric analogue): blunt weapons, shields, one wheel of five prayers (Mending Prayer, Aegis, Benediction, Sunlance, Circle of Dawn), skills Prayer Memory, Hallow Weapon, Rebuke, perks Devotion, Litany, Zealot, Iron Vow.
- Plate armor type and the Ironclad outfit (Sellsword; the Chaplain wears its cuirass, greaves and sabatons); Devout cloth outfit (Chaplain).
- Heal, Shield and Buff spells target the adventurer under the crosshair, the caster on a miss.

### Changed
- Magic Staff is limited to Wizard and Chaplain.

## v0.5 — 2026-10-05

### Changed
- Hexagram attributes renamed and reworked: Strength, Vitality, Spirit, Knowledge, Agility, Dexterity (see `02_Gameplay/Attributes.md`).
- Move Speed comes only from Agility, Action Speed only from Dexterity.
- Mending split into Physical Healing (bandages, surgical kits, campfires, resting) and Magical Healing (potions, spells).
- Interaction Speed (doors, levers, portals) and new Magical Interaction (altars).
- Barbarian, Wizard, outfits, jewelry, affixes and perks moved to the new attributes.

- Character sheet: minor stats are dots on the hexagram sides, a Dark and Darker style list of all stats below; power → damage % and armor/resistance → reduction pairs are linked.
- Physical / magical damage % grows only from Physical / Magical Power (plus gear % bonuses); the Strength and Spirit thresholds give +5 power instead of +10% damage.
- Perception no longer changes how loud footsteps are; it only speeds up searching.

### Removed
- Weakpoint: head damage depends only on hit zone and gear.
- Control Resistance and Toughness: debuffs last their full duration, slows are ignored at Agility 30.
- Cooldown Recovery no longer grows from Knowledge; it comes from gear and perks only (`StatType.CooldownRecovery`).

## v0.4 — 2026-10-04

### Added
- Dark and Darker-style prototype on top of the battle system (Fusion host): three scenes — `LobbyScene` → `DungeonScene` / `BattleScene` (test ground) with loading screen.
- Lobby tabs: Home, Skills, Stash, Merchants (wares bought for coins, dev "Quartermaster" stall).
- Stats view with hexagram chart and hover tooltips; item tooltip; generated ability/perk icons.
- Hexagram attribute system, Barbarian class, item affixes rolled from item seed.
- Loot search: loot is hidden until searched item by item (speed = Perception); monster corpses are containers.
- Holster toggle: gear in weapon/shield/belt slots counts in stats only while in hands.
- Generated weapon meshes with PBR textures, synthesized multi-take audio.
- `AnimationTestScene`: offline browser of every fighter controller state with any catalog weapon (scrub, frame step, orbit / first-person camera), see `05_Technical/Animation_Test_Scene.md`.

### Changed
- Swing peak of every melee attack lies on the screen crosshair (aimed hit always registers).
- Melee swings rework: blade edge follows the cut trajectory, elbow path is planned per clip, no arm/weapon shake.
- Round shields use disc block hitboxes.

### Removed
- Zombie monster; all classes from the original clone except Barbarian.

## v0.3 — 2026-10-02

### Added
- First-person body/aim model.
- Camera at eye level between the eyes.
- Full-body yaw and independent upper-body pitch.
- ±90° vertical look limit.
- Independent crouch for legs/lower body.
- Visible first-person hands.
- Weapon-driven attack animations.
- Weapon categories: one-handed, two-handed, shield, ranged/two-handed.
- Hand-dependent attack input.
- Weapon-specific 2–3 hit chains.
- Combo continuation timing window.
- Directional shield/weapon blocking.
- Partial block behavior.
- Shield block impact and temporary recovery/lockout.
- Network state requirements for attack/block validation.

### Left as TBD
- Exact damage formula.
- Exact block angle thresholds.
- Exact block mitigation and stamina costs.
- Hit detection implementation and Fusion authority model.
- Early/late combo input behavior.
- Detailed ranged combat rules.
