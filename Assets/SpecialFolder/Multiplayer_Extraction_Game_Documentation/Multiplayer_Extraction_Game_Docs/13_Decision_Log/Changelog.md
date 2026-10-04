# Changelog

## v0.4 — 2026-10-04

### Added
- Dark and Darker-style prototype on top of the battle system (Fusion host): three scenes — `LobbyScene` → `DungeonScene` / `BattleScene` (test ground) with loading screen.
- Lobby tabs: Home, Skills, Stash, Merchants (wares bought for coins, dev "Quartermaster" stall).
- Stats view with hexagram chart and hover tooltips; item tooltip; generated ability/perk icons.
- Hexagram attribute system, Barbarian class, item affixes rolled from item seed.
- Loot search: loot is hidden until searched item by item (speed = Perception); monster corpses are containers.
- Holster toggle: gear in weapon/shield/belt slots counts in stats only while in hands.
- Generated weapon meshes with PBR textures, synthesized multi-take audio.

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
