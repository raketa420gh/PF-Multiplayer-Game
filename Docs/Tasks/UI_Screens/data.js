// Example state of every view: strings follow the format strings of the views (DungeonHudView, HubBoardView, StatsView, ...).
const P = n => `../../../Assets/Game/Textures/Icons/Painted/${n}.png`, A = n => `../../../Assets/Game/Textures/Icons/Abilities/${n}.png`;
const good = t => `<color=#c8e632>${t}</color>`, bad = t => `<color=#e04a3a>${t}</color>`, neu = t => `<color=#f2ede0>${t}</color>`;
const rar = { Poor: '#8C8C8C', Common: '#D9D9D9', Uncommon: '#59BF59', Rare: '#4D80F2', Epic: '#A659E6', Legendary: '#F2A633', Unique: '#F25940' };

const ST = (name, c, edge) => ({ name, color: hex(c), edge: !!edge });
const D = {
  rarity: Object.fromEntries(Object.entries(rar).map(([k, v]) => [k, { frame: hex(v), bg: [.09, .08, .07, .95] }])),
  stats: {
    Strength: ST('Strength', '#ED9942'), Vitality: ST('Vitality', '#E05447'), Spirit: ST('Spirit', '#B875EB'),
    Knowledge: ST('Knowledge', '#619EF5'), Agility: ST('Agility', '#52CC9E'), Dexterity: ST('Dexterity', '#D1D652'),
    PhysicalHealing: ST('Physical Healing', '#E77745', 1), MagicalHealing: ST('Magical Healing', '#CC6599', 1), MagicalInteraction: ST('Magical Interaction', '#8C8AF0', 1),
    Perception: ST('Perception', '#59B5C9', 1), InteractionSpeed: ST('Interaction Speed', '#91D178', 1), Handling: ST('Handling', '#DFB84A', 1),
    PhysicalPower: ST('Physical Power', '#DE4B1D'), PhysicalDamage: ST('Physical Damage', '#FCDC6C'), Health: ST('Health', '#E05447'),
    MagicalPower: ST('Magical Power', '#774DDB'), MagicalDamage: ST('Magical Damage', '#EBA2FA'), CastSpeed: ST('Cast Speed', '#38AFE6'),
    BonusCharges: ST('Spell Memory', '#8B9CFF'), MoveSpeed: ST('Move Speed', '#52CC9E'), ActionSpeed: ST('Action Speed', '#D1D652'),
    ArmorRating: ST('Armor Rating', '#CCD1E0'), PhysicalReduction: ST('Physical Reduction', '#9EA8C7'), MagicResistance: ST('Magic Resistance', '#80CCF5'),
    MagicalReduction: ST('Magical Reduction', '#5C99E6'), CooldownSpeed: ST('Cooldown Recovery', '#9EE6DB'), ProjectileReduction: ST('Projectile Reduction', '#C7B38F'),
    BuffDuration: ST('Buff Duration', '#F2C76B'), DebuffReduction: ST('Debuff Reduction', '#BD99E0'), FlatReduction: ST('Flat Reduction', '#B3B8A8'), BonusHealing: ST('Bonus Healing', '#99EB80'),
  },
  attributes: ['Strength', 'Vitality', 'Spirit', 'Knowledge', 'Agility', 'Dexterity'],
  edge: ['PhysicalHealing', 'MagicalHealing', 'MagicalInteraction', 'Perception', 'InteractionSpeed', 'Handling'],
  own: { Strength: ['PhysicalPower', 'PhysicalDamage'], Vitality: ['Health'], Spirit: ['MagicalPower', 'MagicalDamage'], Knowledge: ['CastSpeed', 'BonusCharges'], Agility: ['MoveSpeed'], Dexterity: ['ActionSpeed'] },
  result: { PhysicalPower: 'PhysicalDamage', MagicalPower: 'MagicalDamage', ArmorRating: 'PhysicalReduction', MagicResistance: 'MagicalReduction' },
  listTop: ['Health', 'MoveSpeed', 'ActionSpeed', 'CastSpeed', 'BonusCharges', 'CooldownSpeed', 'BuffDuration'],
  listDamage: ['PhysicalPower', 'PhysicalDamage', 'MagicalPower', 'MagicalDamage'],
  listDefense: ['ArmorRating', 'PhysicalReduction', 'MagicResistance', 'MagicalReduction', 'ProjectileReduction', 'FlatReduction', 'DebuffReduction', 'BonusHealing'],
  values: {
    Strength: neu(18), Vitality: neu(20), Spirit: neu(9), Knowledge: neu(8), Agility: neu(14), Dexterity: neu(16),
    Health: neu(128), MoveSpeed: bad(291), ActionSpeed: good('+4%'), CastSpeed: bad('-6%'), BonusCharges: neu('+0'), CooldownSpeed: neu('0%'), BuffDuration: neu('0%'),
    PhysicalHealing: good('+6%'), MagicalHealing: bad('-3%'), MagicalInteraction: bad('-8%'), Perception: neu('0%'), InteractionSpeed: good('+3%'), Handling: good('+5%'),
    PhysicalPower: neu(18), PhysicalDamage: good('+6%'), MagicalPower: neu(9), MagicalDamage: bad('-8%'),
    ArmorRating: neu(42), PhysicalReduction: neu('12%'), MagicResistance: neu(0), MagicalReduction: neu('0%'), ProjectileReduction: neu('0%'), FlatReduction: neu(0), DebuffReduction: neu('0%'), BonusHealing: neu('0%'),
  },
  chart: { Strength: 18 / 25, Vitality: 20 / 25, Spirit: 9 / 25, Knowledge: 8 / 25, Agility: 14 / 25, Dexterity: 16 / 25 },

  classes: [
    { name: 'Mercenary', desc: 'A free blade: a soldier of a disbanded army gone below for the coin. Loyal to no one, trusts no ossarite: plain steel, and ossarite is only pay.' },
    { name: 'Confessor' }, { name: 'Librarian' }, { name: 'Huntsman' }, { name: 'Knave' }, { name: 'Alchemist' },
  ],
  select: {
    name: 'Grimwald  <color=#c8e632>Lv. 7</color> Mercenary', selected: 0,
    slots: ['Grimwald\n<size=80%><color=#c8e632>Lv. 7</color>  Mercenary</size>', 'Ysolde\n<size=80%><color=#c8e632>Lv. 3</color>  Librarian</size>',
      '<color=#6f6a5f>+  Create Character</color>', '<color=#6f6a5f>+  Create Character</color>', '<color=#6f6a5f>+  Create Character</color>', '<color=#6f6a5f>+  Create Character</color>'],
  },
  create: { name: 'Mercenary', selected: 0, nickname: 'Grimwald',
    description: 'A free blade: a soldier of a disbanded army gone below for the coin. Loyal to no one, trusts no ossarite: plain steel, and ossarite is only pay.' },

  loadingTitles: { select: 'Characters', hub: 'The Mouth', dungeon: 'The Dungeon · Trio' },
  loadingStatus: ['Loading...', 'Opening a new dungeon...'],

  hud: {
    compass: (() => { const head = 20, out = []; for (let d = -60; d <= 60; d++) { const a = ((head + d) % 360 + 360) % 360; if (a % 15) continue;
      const card = { 0: 'N', 90: 'E', 180: 'S', 270: 'W' }[a]; out.push({ label: card || String(a), x: d * 4.5, size: card ? 20 : 11, color: card ? hex('#F2E6BF') : hex('#B3A68C'), alpha: 1 - Math.abs(d) / 62 }); } return out; })(),
    prompt: '[F] Open Small Oak Chest', cast: 'Fireball',
    health: 87 / 128, recoverable: 101 / 128, healthText: '87 / 128', name: 'Grimwald  ·  Shieldbearer Mercenary 7',
    q: { icon: A('Aegis'), charges: '3' }, e: { icon: P('Banner'), charges: '38', cd: .63 },
    readied: '<color=#FF7320>Fireball</color>  [RMB] hold and release: missile, 30m   3/4',
    belt: [{ icon: 'PotionofHealing', count: '2', active: true }, { icon: 'Bandage', count: '3' }, {}, { icon: 'Torch', fuel: .6 }, { icon: 'Lockpick' }, { icon: 'ThrowingKnife', count: '4' }],
    effects: 'Bleed 4s\nHaste 12s', weaponSlot: 0,
    weapon: 'Arming Sword  <size=70%>Attack 2</size>', timer: '11:42', swarm: 'Next portal in 2:18', module: 'Alchemy Lab',
    floor: 'Tangled Catacombs', kills: 'Kills 4   XP +86\n<color=#9ab8d8>Late entry open 1:12</color>', boss: 'Iron Juggernaut',
  },
  minimap: ['Crypt', 'Ossuary', 'BonePit', 'Armory', 'AlchemyLab', 'Forge', 'Chapel', 'BellTower', 'Mint', 'GreatHall', 'Labyrinth', 'Aqueduct', 'Storeroom', 'Tribunal', 'Refectory', 'Mausoleum', 'GuardRoom', 'PillaredHall'].map(n => 'Catacomb' + n),
  floor: ['CornerVault', 'Crypt', 'BurialNiches', 'Ossuary', 'BonePit', 'Mausoleum', 'CornerWatch', 'Aqueduct', 'Armory', 'Forge', 'AlchemyLab', 'Mint', 'SpikeGallery', 'Chapel',
    'Labyrinth', 'BellTower', 'Tribunal', 'Refectory', 'PrisonBlock', 'GuardRoom', 'Storeroom', 'BladeCorridors', 'MinersRow', 'GreatHall', 'GreatHall', 'GreatHall', 'Arena', 'FungusTerraces',
    'BoneArchive', 'PumpWorks', 'GreatHall', 'GreatHall', 'GreatHall', 'SunkenPit', 'StopeWorkings', 'FloodedCistern', 'LordsTomb', 'ArcherGalleries', 'Checkpoint', 'RitualCircle', 'WellRest', 'SurveyHall',
    'CornerShrine', 'GraveFurrows', 'CollapsedHall', 'HaulageSpiral', 'AnatomyTheatre', 'Barbican', 'CornerBarracks'].map(n => 'Catacomb' + n),
  moduleName: m => m.replace('Catacomb', '').replace(/([a-z])([A-Z])/g, '$1 $2'),
  map: { title: 'Tangled Catacombs' },

  hub: { prompt: '[F] Open the Book of Descents', notice: 'The party host left, reconnecting...' },
  rank: 'Lv\n<size=170%>7</size>',
  titles: { board: 'Party Board', descent: 'Book of Descents', skills: 'Skill Rack', kit: 'Weapon Rack · Stash', merchant: 'Jutta Kolb · Weaponsmith' },
  board: {
    name: '<size=65%><color=#b9b29a>• XP 340/600</color>\n<color=#7fd4c8>• 412 Gear Value</color></size>\n<color=#c8e632>Lv. 7</color> Grimwald',
    partyTitle: 'Party  <color=#ffd98c>#4172</color>', inParty: true,
    slots: ['<color=#ffd98c>Grimwald</color>\n<size=80%>7 Mercenary</size>', '<color=#f2ede0>Ysolde</color>\n<size=80%>3 Librarian</size>', '<color=#6f6a5f><size=75%>Party Slot</size>\n- Open -</color>'],
    queue: 'Queue: <color=#ffd98c>Trio</color>  <size=75%><color=#9a9283>(party of up to 3)</color></size>',
    mapTitle: 'The Dungeon', mapInfo: 'The Tangled Catacombs · 7x7 rooms laid out anew every run around the Great Hall · dark crypts, skeletons, trapped corridors · 15 min · blue portals at 7:00, 10:00, 12:00, 14:30',
    notice: 'The party leader starts the run', result: 'Last run:  loot 184g  ·  kills 6  ·  XP +142',
  },
  skills: {
    subclassText: 'Hardest to kill, deals less. Shield only.', subclass: 0,
    subclasses: [{ name: 'Shieldbearer', icon: A('Aegis') }, { name: 'Hewer', icon: A('AxeMastery') }, { name: 'Captain  Lv 10', icon: A('BattleOrder') }],
    perkSlots: [{ icon: P('Accustomed') }, { icon: P('Counterblow') }, { badge: 'Lv 5' }, { badge: 'Lv 9' }],
    skillSlots: [{ icon: A('Aegis'), badge: 'Q' }, { icon: P('Banner'), badge: 'E' }],
    perks: ['Accustomed', 'Counterblow', 'Brace', 'BullsBlood', 'CalmHands', 'Concentration', 'FirmFaith', 'CleanWork'].map((n, i) => ({ name: n, icon: P(n), sprite: 'Diamond', size: 104, frame: i < 2 ? hex('#FFDB8C') : undefined })),
    skills: [{ icon: A('Aegis'), badge: 'Q' }, { icon: P('Banner'), badge: 'E' }, { icon: P('BattleOrder') }, { icon: P('Charge!') }].map(s => ({ ...s, sprite: 'Square', size: 88 })),
    spells: ['Fireball', 'ChainLightning', 'DarkBolt', 'Curse', 'Blessing', 'Absolution', 'CircleofIntercession'].map(n => ({ icon: P(n), sprite: 'Square', size: 88 })),
    tooltip: '<b>Counterblow</b>\nA block right before a hit opens the attacker: your next swing within 1s deals +20% damage.\n<size=80%><color=#9a927f></color></size>',
    wheelSize: 5, wheelTitles: ['Spell Memory (Q)', 'Second Memory (E)'], wheelInactive: [false, true],
    wheels: [[{ icon: P('Fireball'), frame: hex('#FFDB8C') }, { icon: P('ChainLightning'), frame: hex('#FFDB8C') }, { icon: P('DarkBolt'), frame: hex('#FFDB8C') }, {}, {}], [{}, {}, {}, {}, {}]],
  },
  merchant: {
    title: '<color=#E08A3D>Jutta Kolb · Weaponsmith</color>', filter: 1, filterOn: [.05, .045, .04, .9],
    description: 'Blades, shields, arrows and bolts, quenched on ossarite. A single coin apiece.   <color=#ffd98c>1 gold each</color>',
    wares: ['ArmingSword', 'VikingSword', 'Zweihander', 'BattleAxe', 'FranciscaAxe', 'Club', 'FlangedMace', 'MorningStar', 'Poniard', 'Misericorde', 'RecurveBow', 'Crossbow', 'Arrows', 'CrossbowBolts', 'RoundShield', 'Ecu', 'ThrowingKnife', 'MagicStaff', 'Spellbook', 'Torch']
      .map((n, i) => ({ icon: n, price: '1g', hover: i === 3 })),
    coins: '<color=#ffd98c>37</color> <size=70%>gold</size>', detailsIcon: 'BattleAxe',
    details: `<color=${rar.Common}><b>Battle Axe</b></color>  <size=80%>Common Weapon</size>\nTwo-handed Axe\nClass: Mercenary, Huntsman\nDamage 38  (Slash)\nMove Speed -10\n<i><size=80%>Heavy head on a long haft. Slow, but it splits shields.</size></i>\n<size=80%>Value 50g   Sells for 12g   2x4</size>\n\n<color=#ffd98c>Price: 1 gold</color>`,
    status: '<color=#C7E633>Bought Arming Sword: look in the stash</color>',
  },
  wheel: {
    title: 'Spell Memory', radius: 150, selected: 0, cursor: V(0, 120), center: 'Fireball',
    slots: [{ name: 'Fireball\n<size=70%>3/4</size>', icon: P('Fireball'), fill: .75 }, { name: 'Chain Lightning\n<size=70%>1/2</size>', icon: P('ChainLightning'), fill: .5 },
      { name: 'Dark Bolt\n<size=70%>12s cd</size>', icon: P('DarkBolt'), fill: .3 }, { name: 'Curse\n<size=70%>0/2</size>', icon: P('Curse'), fill: 0, charges: 0 }, { name: 'Weapon', fill: 1 }],
  },
  result: {
    escaped: { title: 'ESCAPED', color: hex('#66CCFF'), details: 'Loot value 184g\nKills 6\nExperience +142' },
    died: { title: 'YOU DIED', color: hex('#E63326'), details: 'Your body and everything on it stays in the dungeon.\nKills 2\nExperience +38' },
  },
  pause: { volume: 'Volume 80%', volumeValue: .8, sensitivity: 'Mouse sensitivity 1.25', sensitivityValue: .4 },
  help: '<b>Controls</b>\nWASD move · Shift walk (quiet) · Space jump · Ctrl/C crouch (duck under swings)\nLMB attack / draw · RMB block, or hold and release to cast a readied spell · 1 / 2 weapon sets · X put the weapon away · Tab inventory\n3 / 4 belt item in hand (press again for the next of three), LMB use, RMB put away · F interact (hold; chests and corpses open at once) · Q / E skills · G rest · M map · H help\nCasters need a staff, spellbook or crystal ball in hand; bards need an instrument. Rest at a campfire to recover charges.',
};

const it = (icon, x, y, w = 1, h = 1, o = {}) => ({ icon, name: icon, x, y, w, h, ...o });
const EQUIP = {
  Weapon1Main: it('ArmingSword'), Weapon1Off: it('RoundShield'), Weapon2Main: it('RecurveBow', 0, 0, 1, 1, { rarity: 'Uncommon' }),
  Head: it('VeinworkerHood'), Chest: it('VeinworkerShirt'), Legs: it('VeinworkerTrousers'), Hands: it('VeinworkerWraps'), Feet: it('VeinworkerBoots'),
  Pouch1: it('TatteredPouch'), Utility1: it('PotionofHealing', 0, 0, 1, 1, { count: 2 }), Utility2: it('Bandage', 0, 0, 1, 1, { count: 3 }), Utility4: it('Torch'), Utility5: it('Lockpick'),
};
D.kit = {
  title: 'Mercenary · Kit', equip: EQUIP, value: 'Gear value 412g', pouch: 4,
  bag: [it('Arrows', 0, 0, 1, 3, { count: 20 }), it('CampfireKit', 1, 0, 2, 2), it('PotionofProtection', 3, 0), it('Bandage', 3, 1, 1, 1, { count: 2 }), it('GoldCoins', 4, 0, 1, 1, { count: 37 }), it('SurgicalKit', 5, 0, 2, 1), it('ThrowingKnife', 0, 5, 1, 1, { count: 4 })],
  other: { title: 'Stash · page 1', rows: 10, coins: '120 gold', sell: 'Drop an item here to sell it',
    items: [it('BattleAxe', 0, 0, 2, 4), it('Crossbow', 2, 0, 2, 3), it('CrossbowBolts', 4, 0, 1, 3, { count: 15 }), it('GreatBascinet', 5, 0, 2, 2, { rarity: 'Rare' }), it('PlateHarness', 7, 0, 2, 3, { rarity: 'Epic' }),
      it('MagicStaff', 9, 0, 1, 4), it('Zweihander', 2, 3, 1, 5, { rarity: 'Uncommon' }), it('GoldCrown', 5, 2, 2, 2, { rarity: 'Legendary' }), it('Ruby', 7, 3), it('Sapphire', 8, 3), it('GoldIngot', 4, 4),
      it('PotionofHealing', 0, 6, 1, 1, { count: 3 }), it('TrollsBlood', 1, 6), it('SimpleCloth', 0, 8), it('ThickLeather', 1, 8), it('MetalIngot', 3, 8)] },
};
D.inv = {
  title: 'Mercenary', equip: EQUIP, value: 'Gear value 412g', pouch: 4,
  bag: [it('Arrows', 0, 0, 1, 3, { count: 20 }), it('CampfireKit', 1, 0, 2, 2), it('GoldCoinBag', 3, 0, 2, 2, { rarity: 'Uncommon' }), it('GoldChalice', 5, 0, 1, 2), it('Emerald', 6, 0, 1, 1, { rarity: 'Rare' }), it('ThrowingKnife', 0, 5, 1, 1, { count: 4 })],
  other: { title: 'Small Oak Chest', rows: 5,
    items: [it('VikingSword', 0, 0, 1, 3, { rarity: 'Rare' }), it('PearlNecklace', 1, 0, 1, 1, { rarity: 'Epic' }), it('SilverChalice', 2, 0, 1, 2), it('PotionofInvisibility', 3, 0), it(null, 4, 0, 2, 2, { hidden: .4, name: '?' }), it(null, 6, 0, 1, 3, { hidden: 0, name: '?' }), it(null, 3, 2, 1, 1, { hidden: 0, name: '?' })] },
  tooltip: { pos: V(1214, -500), h: 230, text: `<color=${rar.Rare}><b>Viking Sword</b></color>  <size=80%>Rare Weapon</size>\nOne-handed Sword\nClass: Mercenary, Knave\nDamage 31  (Slash)\n<color=#8fd>+2 Strength</color>\n<color=#6cf>+3% Action Speed</color>\n<color=#6cf>+5 Armor Rating</color>\n<i><size=80%>Broad blade, short grip.</size></i>\n<size=80%>Value 80g   Sells for 20g   1x3</size>` },
};

// Swing hint (BattleEditorUtility.CreateSwingHint): an arrow along the swing, centred on the crosshair.
D.swing = { pos: V(0, 0), size: V(0, 0), draw: r => {
  const c = [1, .9, .55, .9], g = rect('Arrow', r, CENTER, V(0, 0), V(0, 0)); g.el.style.transform = 'rotate(-35deg)';
  img('Shaft', g, CENTER, V(19, 0), V(20, 2), c);
  [150, -150].forEach(a => { const h = img('Head', g, CENTER, V(29, 0), V(8, 2), c, { pivot: LEFT }); h.el.style.transform = `rotate(${-a}deg)`; h.el.style.transformOrigin = '0 50%'; });
} };
