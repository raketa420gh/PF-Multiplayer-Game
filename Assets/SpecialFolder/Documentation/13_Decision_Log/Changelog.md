# Changelog

## Unreleased — 2026-10-08

### Changed
- Floor 1 (DD-017): teams start in a random farm house (7 houses); the farm has two weak monsters and poor loot. The village is the high-risk centre: a monster in every other house, Iron Juggernaut with two archers and two swordsmen on the square, rich house loot and a golden chest on the square.
- Ways down to floor 2 are red portals on stone-ring shrines behind the chapel and on Crimson Isle in the swamp (were stone cellars with grates); they still open halfway through the clock.
- The Old Mill POI became the Abandoned Windmill: three storeys with inner stairs, a gallery round the middle floor with a ramp from the yard, monsters on every level, medium loot (best chest at the top).
- Versioning (DD-018): `Major.Minor.Patch.Build` replaced by separate Game Version (`MAJOR.MINOR.PATCH`), Build number, Stage and Steam branch; roadmap phases mapped to stages and version ranges (see `05_Technical/Build_And_Release.md`, `12_Roadmap/README.md`).

### Planned
- Skeleton enemy roster: Swordsman, Archer, Crossbowman, Warrior (sword + shield), Mage (staff + spells), Flying Skull (charging head) — see `03_World/PvE.md`, DD-016.
- Lore-based enemy loot: each enemy drops gear of its own kind (bows and arrows from archers, swords from swordsmen, staffs and spellbooks from mages) plus universal loot — coins, bandages, consumables, gear, jewelry (see `03_World/Loot.md`).

## v0.8 — 2026-10-07

### Added
- Dedicated server topology: matchmaking for Solo (up to 8) and Trio (up to 12) queues, server allocation, Host fallback without a server build (see `04_Multiplayer/Matchmaking.md`, DD-011).
- Parties: tavern party by four-digit code (up to 3), the leader registers everyone; a party of two may queue for Trio.
- Late-join window: 180 s after the first player the dungeon closes for new registrations.
- House spawns: every team starts in its own village house, teammates together.
- Dedicated server build (`Tools/Game/Network/Build Dedicated Server`), ParrelSync clone accounts (`clone<N>.` save prefix).
- Huntsman class (Marksman, Trapper, Skirmisher), Stalker outfit, single-use hunting trap.
- Crossbow with manual reload: fires on press, R loads a Crossbow Bolt from the bag.
- Spellbook as its own weapon (open on the palm, shut for block and a two-handed slam); Écu heater shield with Sword & Écu / Mace & Écu combos — the combo is picked by the shield in the off hand; block lowering clip.
- Floor 1 "Cursed Village": 540 m terrain from the reference map, zone-based layout (village, farmstead, graveyard, swamp, wild POIs) with ridges and rim walls, misty night look.
- Medieval props and Stylized Skeleton bodies for skeleton enemies.
- Spell marker: ground circle where an area spell will land while it is cast.

### Changed
- Every floor is its own session: descending transfers the adventurer to the next floor's session with health, spell charges, kills and XP (DD-012).
- Portals: escape portal rises on a pedestal at 60 s, is marked on the map and opens after holding F; the cellar grate opens by itself at half the clock (360 s); an open portal takes whoever steps in.
- Dark Swarm starts in the second half of the floor clock (360 s), 3 dmg/s instead of 6, drawn on the map and minimap (DD-013).
- One set of animations for first and third person; generated clips keep arms within human joint ranges (DD-014).
- Charge-based spells no longer start a cooldown after a cast.

### Removed
- Every weapon except eight: Arming Sword, Morning Star, Battle Axe, Crossbow, Spellbook, Magic Staff, Round Shield, Écu (Halberd included); saved item ids migrated.
- Skeleton Champion boss.

## v0.7 — 2026-10-06

### Added
- First dungeon structure: two floors (see `03_World/Map_Design.md`, DD-010).
- Floor 1 "Cursed Village" (working name): fixed open 5×5 map — village, farm, graveyard, swamp.
- Floor 2 "Tangled Tombs": 5×5 grid of premade square rooms placed at random (Forgotten Castle analogue), two doorways per side near the corners.
- Floor transitions: graveyard tombs (stone door with a grate) and farm cellar (grate), both have to be opened.

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
- `AnimationEditor`: offline browser of every fighter controller state with any catalog weapon (scrub, frame step, orbit / first-person camera), see `05_Technical/Animation_Test_Scene.md`.

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
