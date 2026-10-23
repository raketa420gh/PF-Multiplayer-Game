using Game.Scripts.Battle;
using Game.Scripts.Dungeon;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Playable classes of the "Ashen Vein" lore (Documentation/03_World/Lore.md): base attributes, three subclasses each, shared and
    /// subclass perks, skills and spells, starting kits. A build is 4 perks and 2 skills; a spell memory takes a skill slot. Shared
    /// skills come first in every pool, so the default build (skills 0 and 1) fits any subclass. No subclass keeps stacks.
    internal static class DungeonClassLibrary
    {
        /// Prefab the Trapper's trap skill sets, built by DungeonContentBuilder.
        public const string HuntingTrap = "HuntingTrap";

        public static ClassDef[] CreateClasses()
        {
            return new[]
            {
                CreateMercenary(),
                CreateWizard(),
                CreateAlchemist(),
                CreateConfessor(),
                CreateHuntsman(),
                CreateKnave()
            };
        }

        /// A free blade of a disbanded army: plain steel, loyal to no one.
        private static ClassDef CreateMercenary()
        {
            Color steel = new Color(0.72f, 0.74f, 0.78f);
            Color shield = new Color(0.62f, 0.72f, 0.86f);
            Color hewer = new Color(0.9f, 0.42f, 0.28f);
            Color captain = new Color(0.95f, 0.75f, 0.35f);

            return new ClassDef
            {
                Id = 0, Name = "Mercenary", Description = "A free blade: a soldier of a disbanded army gone below for the coin. Loyal to no one, trusts no ossarite: plain steel, and ossarite is only pay.",
                Stats = new ClassStats(19, 19, 8, 9, 17, 18), Color = steel, Body = Color.white,
                Subclasses = new[]
                {
                    Subclass("Shieldbearer", "Hardest to kill, deals less. Shield only. Held the passes for the guild convoys.", shield, "a battered round shield with a steel boss braced against a storm of arrows"),
                    Subclass("Hewer", "Fast and less sturdy; fights with any weapon. Takes a share of the loot, not a wage.", hewer, "a notched broad blade mid-swing trailing blood droplets"),
                    Subclass("Captain", "Holds the squad together; middling at everything himself. Led a free company that is no more.", captain, "a torn company war banner on a pole above a shield wall")
                },
                Skills = new[]
                {
                    new AbilityDef
                    {
                        Name = "Second Wind", Description = Text("Restore 25% of your health over 5s.", "Breathe. Again."), Kind = AbilityKind.Heal, Magnitude = 25f, Duration = 5f,
                        PercentHealth = true, DamageType = DamageType.Physical, Cooldown = 60f, CastTime = 0.3f, Glyph = "SW", Color = new Color(0.9f, 0.35f, 0.3f), Vfx = VfxStyle.Heal,
                        Art = "a weary soldier catching his breath, steam of breath in cold air, hand on his chest"
                    },
                    new AbilityDef
                    {
                        Name = "Grit Your Teeth", Description = Text("Take 20% less damage for 5s.", "Hurts. Later."), Kind = AbilityKind.Buff, Effect = StatusEffectKind.Guard,
                        Magnitude = 20f, Duration = 5f, Cooldown = 40f, CastTime = 0.2f, Glyph = "GT", Color = steel, Vfx = VfxStyle.Physical,
                        Art = "a clenched jaw of a scarred mercenary, blood on the teeth, steel gorget"
                    },
                    new AbilityDef
                    {
                        Name = "Shield Bash", Description = Text("Swing your shield ahead without a lunge: 8 physical damage to everyone in front and a 1s stun. Needs a shield.", "Down."),
                        Kind = AbilityKind.AreaDamage, Magnitude = 8f, Radius = 2.4f, Cone = 50f, Stagger = 1f, DamageType = DamageType.Physical, Flags = AbilityFlags.RequiresShield,
                        Cooldown = 15f, CastTime = 0.15f, Glyph = "SB", Color = shield, Subclass = 0, Vfx = VfxStyle.Physical,
                        Art = "a round shield boss slamming into a skeleton's skull, bone shards flying"
                    },
                    new AbilityDef
                    {
                        Name = "The Wall", Description = Text("For 3s nothing hurts you from the front, but you cannot move. Needs a shield.", "Not one step."), Kind = AbilityKind.Buff,
                        Effect = StatusEffectKind.Bulwark, Magnitude = 1f, Duration = 3f, SecondEffect = StatusEffectKind.Root, SecondMagnitude = 1f, SecondDuration = 3f,
                        Flags = AbilityFlags.RequiresShield, Cooldown = 30f, CastTime = 0.15f, Glyph = "Wa", Color = shield, Subclass = 0, Vfx = VfxStyle.Holy,
                        Art = "a lone shieldbearer planted in a narrow stone corridor, arrows broken against his raised shield"
                    },
                    new AbilityDef
                    {
                        Name = "Shoulder Blow", Description = Text("Your next weapon hit within 10s deals 50% more damage.", "All the weight. All of it."), Kind = AbilityKind.Buff,
                        Effect = StatusEffectKind.HeavyBlow, Magnitude = 50f, Duration = 10f, Cooldown = 20f, CastTime = 0.15f, Glyph = "SB", Color = hewer, Subclass = 1, Vfx = VfxStyle.Blood,
                        Art = "a heavy axe raised high over the shoulder, muscles straining, about to fall"
                    },
                    new AbilityDef
                    {
                        Name = "Fury", Description = Text("For 8s attack 20% faster and move 15% faster.", "Red. Everything red."), Kind = AbilityKind.Buff, Effect = StatusEffectKind.ActionSpeed,
                        Magnitude = 20f, Duration = 8f, SecondEffect = StatusEffectKind.Haste, SecondMagnitude = 15f, SecondDuration = 8f, Cooldown = 45f, CastTime = 0.2f,
                        Glyph = "Fu", Color = hewer, Subclass = 1, Vfx = VfxStyle.Blood, Art = "a roaring blood-spattered warrior with wild eyes, red haze around him"
                    },
                    new AbilityDef
                    {
                        Name = "Hold the Line!", Description = Text("You and your allies within 8m take 15% less damage for 8s.", "Shields together!"), Kind = AbilityKind.AreaBuff,
                        Effect = StatusEffectKind.Guard, Magnitude = 15f, Duration = 8f, Radius = 8f, Flags = AbilityFlags.Shout, Cooldown = 40f, CastTime = 0.3f,
                        Glyph = "HL", Color = captain, Subclass = 2, Vfx = VfxStyle.Shout, Art = "a row of overlapping battered shields, a captain shouting behind them"
                    },
                    new AbilityDef
                    {
                        Name = "Charge!", Description = Text("You and your allies within 8m attack 15% faster for 8s.", "At them!"), Kind = AbilityKind.AreaBuff,
                        Effect = StatusEffectKind.ActionSpeed, Magnitude = 15f, Duration = 8f, Radius = 8f, Flags = AbilityFlags.Shout, Cooldown = 40f, CastTime = 0.3f,
                        Glyph = "Ch", Color = captain, Subclass = 2, Vfx = VfxStyle.Shout, Art = "a captain pointing his sword forward, mouth open in a battle cry, soldiers surging past"
                    },
                    new AbilityDef
                    {
                        Name = "Banner", Description = Text("Plant your banner for 20s: allies within 5m of it get +3 to every attribute.", "Here we stand."), Kind = AbilityKind.Zone,
                        Zone = ZoneKind.Banner, ZoneRadius = 5f, ZoneDuration = 20f, Effect = StatusEffectKind.AllAttributes, Magnitude = 3f, Cooldown = 60f, CastTime = 0.6f,
                        Glyph = "Bn", Color = captain, Subclass = 2, Vfx = VfxStyle.Holy, Art = "a war banner driven into a crypt floor, its torn cloth glowing faintly gold"
                    }
                },
                Perks = new[]
                {
                    Stats("Hardened", "+10% max health.", "So many scars there is no room for new ones.", "a scarred forearm wrapped in old leather straps", -1, new StatModifier(StatType.MaxHealth, 10f)),
                    Perk("Plate Habit", "Heavy armor slows you half as much.", "Twenty years in iron.", PerkEffect.HeavyArmorTraining, 0.5f, "a dented steel breastplate with straps, worn smooth"),
                    Perk("Used to Wounds", "Bandages heal 30% more.", "Bind it tight. Walk on.", PerkEffect.BandageHealing, 0.3f, "bloody linen bandage tied around a sword arm"),
                    Perk("Steady Stance", "Your block does not slow you down.", "Feet first, then the shield.", PerkEffect.SteadyBlock, 1f, "armored boots planted firmly in mud behind a shield", 0),
                    Perk("Shield Drill", "Your shield block stops 20% more damage.", "The shield is a wall, not a plate.", PerkEffect.ShieldTraining, 0.2f, "a heater shield deflecting a sword blow, sparks flying", 0),
                    Perk("Counterblow", "After a blocked hit your attacks are 25% faster for 2s.", "Your turn is over.", PerkEffect.Retaliation, 25f, "a sword thrusting out from behind a raised shield", 0),
                    Perk("Heavy Bones", "Nothing can knock you back.", "Push. I will wait.", PerkEffect.KnockbackImmune, 1f, "a broad-shouldered warrior standing firm against a wave of dust", 0),
                    Perk("Anything Sharp", "You can wield any weapon.", "A weapon is a weapon.", PerkEffect.AnyWeapon, 1f, "a pile of mismatched weapons: axe, mace, sword, spear", 1),
                    Perk("Light Step", "+15 move speed while you wear no plate.", "Iron is for the dead.", PerkEffect.LightStep, 15f, "a leather boot mid-stride kicking up dust", 1),
                    Perk("Onslaught", "Each hit in a row on the same foe makes your attacks 5% faster, up to 25%.", "Don't let him breathe.", PerkEffect.Onslaught, 5f, "a flurry of blade arcs striking one shield again and again", 1),
                    Stats("Quick Hands", "+5% action speed.", "Faster than the other side.", "calloused hands spinning a short sword", 1, new StatModifier(StatType.ActionSpeed, 5f)),
                    Perk("Under the Banner", "Allies within 6m get +15 armor.", "Stay in sight of the colours.", PerkEffect.BannerAura, 15f, "soldiers huddled under a tattered banner, shields up", 2),
                    Perk("Set the Example", "Allies within 6m move 5% faster.", "Follow me.", PerkEffect.ExampleAura, 5f, "a captain striding ahead of his men through a dark tunnel", 2),
                    Perk("Battle Order", "Allies within 6m deal 5% more damage.", "Each one to his place.", PerkEffect.FormationAura, 5f, "a hand drawing battle lines on a map with a dagger", 2),
                    Perk("Voice", "Your war cries reach 50% farther.", "Heard over any steel.", PerkEffect.Voice, 0.5f, "a war horn of black horn with silver bands", 2)
                },
                Kit = new[] { ("Arming Sword", EquipSlot.Weapon1Main, 1, true), ("Round Shield", EquipSlot.Weapon1Off, 1, true), ("Battle Axe", EquipSlot.Weapon2Main, 1, true),
                    ("Veinworker Shirt", EquipSlot.Chest, 1, true), ("Veinworker Trousers", EquipSlot.Legs, 1, true), ("Veinworker Boots", EquipSlot.Feet, 1, true), ("Bandage", EquipSlot.Utility1, 3, true), ("Torch", EquipSlot.Utility2, 1, true) },
                Weapons = new[] { WeaponClass.Sword, WeaponClass.Axe, WeaponClass.Mace, WeaponClass.Spear, WeaponClass.Dagger, WeaponClass.Shield, WeaponClass.Torch }
            };
        }

        /// An outcast bookman: one spell wheel of five, charges back only by a campfire. Spirit is damage, Knowledge is casting pace and charges.
        private static ClassDef CreateWizard()
        {
            Color arcane = new Color(0.62f, 0.5f, 0.95f);
            Color fire = new Color(1f, 0.45f, 0.12f);
            Color frost = new Color(0.55f, 0.85f, 1f);
            Color storm = new Color(0.72f, 0.78f, 1f);

            return new ClassDef
            {
                Id = 1, Name = "Wizard", Description = "An outcast bookman: the College above teaches magic by the canon, he went for the forbidden books of Mornheim and was thrown out. One wheel of spells that come back only by a campfire. Fragile in melee.",
                Stats = new ClassStats(8, 10, 23, 21, 15, 13), Color = arcane, Body = Color.white,
                Subclasses = new[]
                {
                    Subclass("Flamer", "Area damage and burning. Copied the funeral books of Mornheim: whom the vein would not take, the empire burned.", fire, "an open burning funeral book, flames rising from its pages"),
                    Subclass("Frostbinder", "Control: slows and binds. Found the ice formulas the bone-scholars froze the dead with.", frost, "a frost-covered skull in a block of blue ice with runes"),
                    Subclass("Stormcaller", "Single targets and armor. Learned to call the sparks that crackle in the deep shafts before a Crumble.", storm, "a lightning bolt splitting a dark cave ceiling")
                },
                Skills = new[]
                {
                    Memory("Spell Memory", "Hold to open the spell wheel and ready a spell; hold RMB with a staff or a spellbook in hand and release to cast. The centre of the wheel returns RMB to the weapon.",
                        "SM", arcane, "an old grimoire with a glowing wheel of five sigils over its pages"),
                    new AbilityDef
                    {
                        Name = "Reread", Description = Text("Stand still leafing through the book for 5s, then every spell gets one charge back.", "Where was it... ah, here."), Kind = AbilityKind.RestoreCharges,
                        Magnitude = 1f, Channel = 5f, Cooldown = 120f, Glyph = "Rr", Color = arcane, Vfx = VfxStyle.Arcane, Art = "a candle-lit hand turning yellowed pages covered in arcane script"
                    },
                    new AbilityDef
                    {
                        Name = "Concentration", Description = Text("Cast spells 20% faster for 10s.", "Quiet. I am counting syllables."), Kind = AbilityKind.Buff, Effect = StatusEffectKind.CastSpeed,
                        Magnitude = 20f, Duration = 10f, Cooldown = 60f, CastTime = 0.2f, Glyph = "Co", Color = arcane, Vfx = VfxStyle.Arcane, Art = "a wizard's eye wide open with arcane runes reflected in it"
                    }
                },
                Spells = new[]
                {
                    new AbilityDef
                    {
                        Name = "Lantern", Description = Text("A small light floats over your head for 60s and lights the way like a torch; both hands stay free.", "A torch is for those with nothing to hold a book with."),
                        Kind = AbilityKind.Buff, IsSpell = true, Charges = 3, Cooldown = 1f, CastTime = 0.6f, Effect = StatusEffectKind.Lightbearer, Magnitude = 1f, Duration = 60f, Range = 0f,
                        Glyph = "Ln", Color = new Color(1f, 0.92f, 0.7f), Vfx = VfxStyle.Light, Art = "a small floating orb of warm light above an open palm in a dark crypt"
                    },
                    new AbilityDef
                    {
                        Name = "Prickly Word", Description = Text("A bolt of raw force: 18 magical damage.", "The first line every outcast learns."), Kind = AbilityKind.Projectile, IsSpell = true, Charges = 6,
                        Cooldown = 1f, CastTime = 0.6f, Magnitude = 18f, Speed = 34f, Projectile = ProjectileKind.Magic, Stagger = 0.1f, Glyph = "PW", Color = arcane, Vfx = VfxStyle.Arcane,
                        Art = "a sharp violet shard of force flying from a pointing finger"
                    },
                    new AbilityDef
                    {
                        Name = "Fireball", Description = Text("Explodes within 2m: 26 magical damage and burning for 3s.", "A simple word. A loud one."), Kind = AbilityKind.Projectile, IsSpell = true, Charges = 4,
                        Cooldown = 1f, CastTime = 1f, Magnitude = 26f, Speed = 22f, Projectile = ProjectileKind.Fire, Radius = 2f, HitEffect = StatusEffectKind.Burn, HitMagnitude = 9f, HitDuration = 3f,
                        Stagger = 0.3f, Flags = AbilityFlags.Fire, Glyph = "FB", Color = fire, Subclass = 0, Vfx = VfxStyle.Fire, Art = "a roaring fireball hurtling through a dark stone hall"
                    },
                    new AbilityDef
                    {
                        Name = "Wall of Fire", Description = Text("A 4m strip of fire on the marked spot for 6s: 10 magical damage a second and burning to whoever crosses it.", "No farther."),
                        Kind = AbilityKind.Zone, IsSpell = true, Ground = true, Charges = 2, Cooldown = 1f, CastTime = 1.2f, Zone = ZoneKind.FireWall, ZoneRadius = 4f, ZoneDuration = 6f, ZoneTick = 10f,
                        HitEffect = StatusEffectKind.Burn, HitMagnitude = 6f, HitDuration = 2f, Flags = AbilityFlags.Fire, Glyph = "WF", Color = fire, Subclass = 0, Vfx = VfxStyle.Fire,
                        Art = "a wall of roaring flames blocking a dungeon corridor"
                    },
                    new AbilityDef
                    {
                        Name = "Funeral Pyre", Description = Text("Marks 3m under the crosshair; after 1.5s a pillar of fire strikes it for 50 magical damage.", "A short rite. The dead don't complain."),
                        Kind = AbilityKind.Zone, IsSpell = true, Ground = true, Charges = 1, Cooldown = 1f, CastTime = 1.4f, Zone = ZoneKind.Pyre, ZoneRadius = 3f, ZoneDuration = 2.2f, ZoneDelay = 1.5f,
                        Magnitude = 50f, HitEffect = StatusEffectKind.Burn, HitMagnitude = 10f, HitDuration = 3f, Stagger = 0.6f, Flags = AbilityFlags.Fire, Glyph = "FP", Color = fire, Subclass = 0,
                        Vfx = VfxStyle.Fire, Art = "a towering pillar of fire erupting from a funeral pyre of bones"
                    },
                    new AbilityDef
                    {
                        Name = "Ice Shard", Description = Text("A shard of ice: 18 magical damage and a 30% slow for 2s.", "Sharp. Cold. Enough."), Kind = AbilityKind.Projectile, IsSpell = true, Charges = 6,
                        Cooldown = 1f, CastTime = 0.6f, Magnitude = 18f, Speed = 32f, Projectile = ProjectileKind.Ice, HitEffect = StatusEffectKind.Slow, HitMagnitude = 30f, HitDuration = 2f,
                        Flags = AbilityFlags.Frost, Glyph = "IS", Color = frost, Subclass = 1, Vfx = VfxStyle.Frost, Art = "a jagged shard of blue ice flying, trailing frost mist"
                    },
                    new AbilityDef
                    {
                        Name = "Rime", Description = Text("3m of frost under the crosshair for 6s: everyone in it is slowed by 40%.", "Someone will lie down here."), Kind = AbilityKind.Zone, IsSpell = true, Ground = true,
                        Charges = 2, Cooldown = 1f, CastTime = 1f, Zone = ZoneKind.Frost, ZoneRadius = 3f, ZoneDuration = 6f, HitEffect = StatusEffectKind.Slow, HitMagnitude = 40f, HitDuration = 0.6f,
                        Flags = AbilityFlags.Frost, Glyph = "Ri", Color = frost, Subclass = 1, Vfx = VfxStyle.Frost, Art = "a crypt floor covered in spreading hoarfrost and ice crystals"
                    },
                    new AbilityDef
                    {
                        Name = "Frozen Shackles", Description = Text("The one under the crosshair cannot move for 1.5s.", "That is how they were kept. For centuries."), Kind = AbilityKind.TargetEffect, IsSpell = true,
                        Charges = 2, Cooldown = 1f, CastTime = 0.8f, HitEffect = StatusEffectKind.Root, HitMagnitude = 1f, HitDuration = 1.5f, Flags = AbilityFlags.Frost, Glyph = "FS", Color = frost,
                        Subclass = 1, Vfx = VfxStyle.Frost, Art = "ice chains clamping around a skeleton's ankles"
                    },
                    new AbilityDef
                    {
                        Name = "Lightning", Description = Text("An instant bolt up to 15m: 24 magical damage.", "Stone sparks before a fall. I spark before a blow."), Kind = AbilityKind.ChainLightning,
                        IsSpell = true, Charges = 5, Cooldown = 1f, CastTime = 0.8f, Magnitude = 24f, Range = 15f, Count = 0, Stagger = 0.15f, Flags = AbilityFlags.Lightning, Glyph = "Li", Color = storm,
                        Subclass = 2, Vfx = VfxStyle.Lightning, Art = "a jagged white-blue lightning bolt leaping from fingertips"
                    },
                    new AbilityDef
                    {
                        Name = "Chain Lightning", Description = Text("Strikes the target and leaps to the 2 nearest bodies within 5m, allies too: 20 magical damage, weaker each leap.", "Step away from him. No, farther."),
                        Kind = AbilityKind.ChainLightning, IsSpell = true, Charges = 3, Cooldown = 1f, CastTime = 1f, Magnitude = 20f, Radius = 5f, Count = 2, Range = 15f, Stagger = 0.15f,
                        Flags = AbilityFlags.Lightning, Glyph = "CL", Color = storm, Subclass = 2, Vfx = VfxStyle.Lightning, Art = "lightning arcing between three armored figures in a dark hall"
                    },
                    new AbilityDef
                    {
                        Name = "Thunderstorm", Description = Text("4m under the crosshair: for 4s four bolts strike random spots, 22 magical damage each.", "That is how the vein sounds before a Crumble."),
                        Kind = AbilityKind.Zone, IsSpell = true, Ground = true, Charges = 1, Cooldown = 1f, CastTime = 1.5f, Zone = ZoneKind.Storm, ZoneRadius = 4f, ZoneDuration = 4.2f, Magnitude = 22f,
                        Count = 4, Stagger = 0.4f, Flags = AbilityFlags.Lightning, Glyph = "TS", Color = storm, Subclass = 2, Vfx = VfxStyle.Lightning,
                        Art = "a storm of lightning bolts striking down inside an underground cavern"
                    }
                },
                Perks = new[]
                {
                    Stats("Well-Read", "+3 Knowledge.", "Copied it by hand, so I remember it.", "a stack of worn leather tomes with a quill", -1, new StatModifier(StatType.Knowledge, 3f)),
                    Stats("Firm Word", "+3 Spirit.", "A word weighs what you put in it.", "glowing letters burned into a stone tablet", -1, new StatModifier(StatType.Spirit, 3f)),
                    Perk("Frugal Word", "15% chance a spell does not spend its charge.", "Ink is dear. So is strength.", PerkEffect.FrugalWord, 0.15f, "a nearly empty inkwell with a single drop of glowing ink"),
                    Perk("Hot Flame", "+15% fire damage.", "Mornheim burned its dead to ash. I learned from the best.", PerkEffect.FireMastery, 0.15f, "white-hot flames in a cupped hand", 0),
                    Perk("Long Smoulder", "Your burning lasts 2s longer.", "Embers remember longer than people.", PerkEffect.LongSmoulder, 2f, "glowing embers smouldering in charred bones", 0),
                    Perk("Burnt Hands", "Your own fire hurts you 50% less.", "I burned the first hundred pages with my fingers.", PerkEffect.BlastHabit, 0.5f, "scarred burnt hands holding a flame", 0),
                    Perk("Funeral Heat", "A foe killed by your fire bursts into flame: 8 magical damage to everyone within 2m.", "One pyre lights another.", PerkEffect.FuneralHeat, 8f, "a burning skeleton collapsing and spraying fire", 0),
                    Perk("Piercing Cold", "Your slows are 15% stronger.", "Cold does not strike. Cold waits.", PerkEffect.DeepCold, 0.15f, "frost creeping up a stone pillar", 1),
                    Perk("Brittleness", "Foes you slow take 10% more damage from everyone.", "Frozen bone breaks like glass.", PerkEffect.Brittle, 10f, "a frozen bone shattering into icy shards", 1),
                    Perk("Rime on the Skin", "+10% armor rating.", "I am not cold. I have not been cold for a long time.", PerkEffect.FrostSkin, 0.1f, "a pale hand covered in frost patterns", 1),
                    Perk("Winter Memory", "+1 charge to every frost spell.", "The bone-scholars wrote small. There was room in the margins.", PerkEffect.WinterMemory, 1f, "a frost-rimed page with tiny blue script in the margins", 1),
                    Perk("Dry Air", "+15% lightning damage.", "No storms underground. So all of it is mine.", PerkEffect.DryAir, 0.15f, "crackling static sparks between stone walls", 2),
                    Perk("Iron Calls", "+20% lightning damage to foes in plate.", "Plate is the best lightning rod.", PerkEffect.IronCalls, 0.2f, "lightning striking a knight's steel helmet", 2),
                    Perk("Long Chain", "Chain lightning leaps to one more body.", "Let it find them itself.", PerkEffect.LongChain, 1f, "a long branching chain of lightning", 2),
                    Perk("Quick Word", "Lightning spells are cast 15% faster.", "Thunder does not wait. Nor do I.", PerkEffect.QuickWord, 0.15f, "a mouth speaking a word that crackles with sparks", 2)
                },
                Kit = new[] { ("Spellbook", EquipSlot.Weapon1Main, 1, true), ("Magic Staff", EquipSlot.Weapon2Main, 1, true), ("Veinworker Hood", EquipSlot.Head, 1, true),
                    ("Veinworker Shirt", EquipSlot.Chest, 1, true), ("Veinworker Trousers", EquipSlot.Legs, 1, true), ("Veinworker Boots", EquipSlot.Feet, 1, true), ("Campfire Kit", EquipSlot.Utility1, 1, true),
                    ("Bandage", EquipSlot.Utility2, 2, true), ("Torch", EquipSlot.Utility3, 1, true) },
                Weapons = new[] { WeaponClass.Staff, WeaponClass.Spellbook, WeaponClass.Torch },
                Focus = CastFocus.Magic
            };
        }

        /// A guild alchemist's apprentice thrown out: brews and flasks instead of reagents he can't afford.
        private static ClassDef CreateAlchemist()
        {
            Color amber = new Color(0.85f, 0.62f, 0.25f);
            Color physician = new Color(0.55f, 0.95f, 0.55f);
            Color bomber = new Color(1f, 0.5f, 0.15f);
            Color tester = new Color(0.75f, 0.45f, 0.9f);

            return new ClassDef
            {
                Id = 2, Name = "Alchemist", Description = "A guild alchemist's apprentice thrown out on the street. No money for dear reagents, so he went into the vein for ossarite: above, it buys everything else.",
                Stats = new ClassStats(14, 15, 13, 18, 15, 15), Color = amber, Body = Color.white,
                Subclasses = new[]
                {
                    Subclass("Physician", "Support: heals the squad. Thrown out for treating convicts for free.", physician, "a physician's leather satchel with green glowing vials"),
                    Subclass("Bombardier", "Area damage with thrown flasks. Thrown out after he blew up his master's workshop.", bomber, "an exploding glass flask with fire and shards"),
                    Subclass("Mutagenist", "Fights himself, hardened by mixtures with side effects. Thrown out for experiments on himself.", tester, "a bubbling purple vial held by a trembling veined hand")
                },
                Skills = new[]
                {
                    new AbilityDef
                    {
                        Name = "Healing Brew", Description = Text("From your own flask: restore 20% of your health over 4s, walking slower while you drink. Not a potion.", "Bitter. So it works."),
                        Kind = AbilityKind.Heal, Magnitude = 20f, Duration = 4f, PercentHealth = true, DamageType = DamageType.Physical, SecondEffect = StatusEffectKind.Slow, SecondMagnitude = 30f,
                        SecondDuration = 4f, Cooldown = 60f, CastTime = 0.3f, Glyph = "HB", Color = physician, Vfx = VfxStyle.Heal, Art = "a dented tin hip flask with a dark green brew steaming"
                    },
                    new AbilityDef
                    {
                        Name = "Invigorating Brew", Description = Text("+3 Agility and +3 Dexterity for 20s; drunk on the move.", "Sleep when you're up top."), Kind = AbilityKind.Buff,
                        Effect = StatusEffectKind.Agility, Magnitude = 3f, Duration = 20f, SecondEffect = StatusEffectKind.Dexterity, SecondMagnitude = 3f, SecondDuration = 20f,
                        Cooldown = 75f, CastTime = 0.2f, Glyph = "IB", Color = amber, Vfx = VfxStyle.Alchemy, Art = "a small vial of fizzing amber liquid with rising bubbles"
                    },
                    new AbilityDef
                    {
                        Name = "Splash of Life", Description = Text("Throw a flask in an arc: a healing pool 3m wide for 5s where you and your allies heal 4% of health a second; foes do not.", "Whoever gets it is lucky."),
                        Kind = AbilityKind.Throw, Projectile = ProjectileKind.Flask, Speed = 14f, Gravity = -9.81f, Zone = ZoneKind.HealPool, ZoneRadius = 3f, ZoneDuration = 5f, ZoneTick = 4f,
                        Flags = AbilityFlags.Flask, Cooldown = 45f, CastTime = 0.2f, Glyph = "SL", Color = physician, Subclass = 0, Vfx = VfxStyle.Heal,
                        Art = "a green glass flask shattering on stone, healing light splashing out"
                    },
                    new AbilityDef
                    {
                        Name = "Fortifying Mix", Description = Text("The ally under the crosshair within 2m (you when there is none): +3 Vitality and +15% armor for 20s.", "Drink. You'll need it below."),
                        Kind = AbilityKind.Buff, OnAlly = true, Range = 2.5f, Effect = StatusEffectKind.Vitality, Magnitude = 3f, Duration = 20f, SecondEffect = StatusEffectKind.ArmorPercent,
                        SecondMagnitude = 15f, SecondDuration = 20f, Cooldown = 60f, CastTime = 0.4f, Glyph = "FM", Color = physician, Subclass = 0, Vfx = VfxStyle.Alchemy,
                        Art = "a hand offering a thick red tonic in a stoppered vial"
                    },
                    new AbilityDef
                    {
                        Name = "Blasting Flask", Description = Text("Throw a flask that bursts on impact: 30 damage within 2.5m and everyone thrown away from the centre. Hits you and allies too.", "The recipe is in the ruins of the workshop."),
                        Kind = AbilityKind.Throw, Projectile = ProjectileKind.Flask, Speed = 15f, Gravity = -9.81f, Magnitude = 30f, Radius = 2.5f, Push = 7f, Stagger = 0.4f, DamageType = DamageType.Physical,
                        Flags = AbilityFlags.Flask, Cooldown = 30f, CastTime = 0.2f, Glyph = "BF", Color = bomber, Subclass = 1, Vfx = VfxStyle.Alchemy,
                        Art = "a black powder flask exploding in a burst of fire, smoke and glass"
                    },
                    new AbilityDef
                    {
                        Name = "Fire Flask", Description = Text("Throw a flask: a burning pool 2.5m wide for 5s that sets everyone in it ablaze and lights the place like a torch.", "The dead burn better than the living."),
                        Kind = AbilityKind.Throw, Projectile = ProjectileKind.Flask, Speed = 15f, Gravity = -9.81f, Zone = ZoneKind.FirePool, ZoneRadius = 2.5f, ZoneDuration = 5f, ZoneTick = 8f,
                        HitEffect = StatusEffectKind.Burn, HitMagnitude = 4f, HitDuration = 2f, DamageType = DamageType.Magical, Flags = AbilityFlags.Flask | AbilityFlags.Fire, Cooldown = 40f, CastTime = 0.2f,
                        Glyph = "FF", Color = bomber, Subclass = 1, Vfx = VfxStyle.Fire, Art = "a flask of rendered corpse oil shattering into a pool of flames"
                    },
                    new AbilityDef
                    {
                        Name = "Bull's Blood", Description = Text("+7 Strength for 15s, then -5 Agility for 10s.", "Strength first. The bill later."), Kind = AbilityKind.Buff, Effect = StatusEffectKind.BullBlood,
                        Magnitude = 7f, Duration = 15f, SecondEffect = StatusEffectKind.Sluggish, SecondMagnitude = 5f, SecondDuration = 10f, Flags = AbilityFlags.Mixture, Cooldown = 60f, CastTime = 0.2f,
                        Glyph = "BB", Color = new Color(0.85f, 0.2f, 0.2f), Subclass = 2, Vfx = VfxStyle.Blood, Art = "a vial of thick dark red blood with a bull's horn stopper"
                    },
                    new AbilityDef
                    {
                        Name = "Quicksilver Mix", Description = Text("+15% action speed for 10s; you lose 1 health a second meanwhile (never below 1).", "Faster. Faster still. Until the heart stops."),
                        Kind = AbilityKind.Buff, Effect = StatusEffectKind.Mercury, Magnitude = 15f, Duration = 10f, SecondEffect = StatusEffectKind.Drain, SecondMagnitude = 1f, SecondDuration = 10f,
                        Flags = AbilityFlags.Mixture, Cooldown = 45f, CastTime = 0.2f, Glyph = "QM", Color = new Color(0.8f, 0.85f, 0.9f), Subclass = 2, Vfx = VfxStyle.Alchemy,
                        Art = "liquid mercury swirling inside a glass vial, silver and gleaming"
                    }
                },
                Perks = new[]
                {
                    Perk("Potion Lore", "Potions last 30% longer.", "The guild threw out the apprentice but kept the training.", PerkEffect.PotionDuration, 0.3f, "a rack of labeled potion bottles on a dusty shelf"),
                    Perk("Provident", "+1 potion in every stack.", "Vials packed in straw do not break.", PerkEffect.PotionStack, 1f, "potion bottles nestled in straw inside a wooden crate"),
                    Perk("Iron Stomach", "Poison hurts you 50% less.", "The master called it spoiled reagents. He called it breakfast.", PerkEffect.PoisonResistance, 0.5f, "a hand raising a cup of green bubbling liquid"),
                    Perk("Healthy Spirit", "Allies within 6m get +3 Vitality.", "Gave the convicts a strong infusion before every shift.", PerkEffect.VitalityAura, 3f, "a steaming cauldron of herbal infusion with ladle", 0),
                    Perk("Generous Flask", "A healing potion you drink heals allies within 4m for half its effect.", "One flask for everyone.", PerkEffect.GenerousFlask, 0.5f, "a healing potion's glow spreading to several hands", 0),
                    Perk("Calm Hands", "Allies within 6m drink and bandage 20% faster.", "Don't hurry. You'll make it.", PerkEffect.CalmHandsAura, 20f, "steady hands wrapping a bandage around a wound", 0),
                    Perk("Pure Blood", "Every heal of yours, on you or allies, cures poison.", "Drive the poison out first, then patch.", PerkEffect.PureBlood, 1f, "a drop of clean red blood in a clear vial", 0),
                    Perk("Caustic Mix", "Foes caught in your blast take 10% more damage from everyone for 4s.", "Steel takes a blow. Not acid.", PerkEffect.CausticMix, 10f, "acid eating through a steel plate with green smoke", 1),
                    Perk("Strong Charge", "+20% damage of your flasks, blast and fire.", "A pinch is for cowards.", PerkEffect.StrongCharge, 0.2f, "a hand pouring black powder into a flask", 1),
                    Perk("Wide Spread", "Your blasts and pools are 25% wider.", "Ground glass in the flask: the splinters fly farther.", PerkEffect.WideSpread, 0.25f, "glass shards spraying outward from an explosion", 1),
                    Perk("Used to the Roar", "Your own blasts and fire hurt you 50% less (allies get no discount).", "What? Speak up.", PerkEffect.BlastHabit, 0.5f, "a soot-covered face with singed eyebrows and ringing ears", 1),
                    Perk("Accustomed", "Side effects of your mixtures last half as long.", "Poison is a dose. A dose is a habit.", PerkEffect.Accustomed, 0.5f, "veins glowing faintly purple under pale skin", 2),
                    Perk("Double Dose", "Buffs of your mixtures last 50% longer.", "One to try. The second for real.", PerkEffect.DoubleDose, 0.5f, "two vials clinking together in a toast", 2),
                    Stats("Sinewy", "+3 Strength.", "Lean is not weak.", "a lean sinewy forearm with bulging veins gripping a mace", 2, new StatModifier(StatType.Strength, 3f)),
                    Perk("Buzz", "While any of your mixtures works you take 15% less damage.", "Pain is duller in a daze.", PerkEffect.Buzz, 0.15f, "dilated eyes swirling with violet haze", 2)
                },
                Kit = new[] { ("Arming Sword", EquipSlot.Weapon1Main, 1, true), ("Veinworker Shirt", EquipSlot.Chest, 1, true), ("Veinworker Trousers", EquipSlot.Legs, 1, true),
                    ("Veinworker Boots", EquipSlot.Feet, 1, true), ("Potion of Healing", EquipSlot.Utility1, 2, true), ("Bandage", EquipSlot.Utility2, 2, true), ("Torch", EquipSlot.Utility3, 1, true) },
                Weapons = new[] { WeaponClass.Sword, WeaponClass.Mace, WeaponClass.Dagger, WeaponClass.Staff, WeaponClass.Spear, WeaponClass.Torch }
            };
        }

        /// A burier of the Cathedral of Repose: one wheel of prayers. Spirit is their strength, Knowledge their pace and charges.
        private static ClassDef CreateConfessor()
        {
            Color light = new Color(1f, 0.9f, 0.55f);
            Color mending = new Color(0.6f, 1f, 0.65f);
            Color zeal = new Color(0.95f, 0.65f, 0.3f);
            Color godless = new Color(0.6f, 0.3f, 0.75f);

            return new ClassDef
            {
                Id = 3, Name = "Confessor", Description = "A burier of the Cathedral of Repose: sang the dead of the vein to rest so they would not rise, and gathered the tithe. Came down himself to atone for Mornheim's \"payment of the dead\". One wheel of prayers.",
                Stats = new ClassStats(15, 14, 21, 16, 12, 12), Color = light, Body = Color.white,
                Subclasses = new[]
                {
                    Subclass("Healer", "Heals and shelters the squad. Served the Cathedral's infirmary at the entrance; came down to bury fewer.", mending, "gentle hands glowing with warm light over a wounded chest"),
                    Subclass("Smiter", "A mace against the undead. A temple brother of the graveyard guard, sent down first when the seal broke.", zeal, "a holy mace wreathed in golden light smashing bone"),
                    Subclass("Apostate", "Curses, power paid with his own health. Heard the vein breathe and went to the Sleepers; his prayers are answered, but not by the Cathedral.", godless, "a cracked holy symbol oozing black ichor and violet light")
                },
                Skills = new[]
                {
                    Memory("Prayer Memory", "Hold to open the prayer wheel and ready a prayer; hold RMB with a spellbook or a staff in hand and release to cast. The centre of the wheel returns RMB to the weapon.",
                        "PM", light, "a prayer book with a ring of five golden glowing sigils"),
                    new AbilityDef
                    {
                        Name = "Rosary", Description = Text("Pray on your knees for 5s, then every prayer gets one charge back.", "...and once more. And once more."), Kind = AbilityKind.RestoreCharges,
                        Magnitude = 1f, Channel = 5f, Cooldown = 120f, Glyph = "Ro", Color = light, Vfx = VfxStyle.Holy, Art = "worn wooden prayer beads wrapped around clasped hands"
                    },
                    new AbilityDef
                    {
                        Name = "Holy Water", Description = Text("Sprinkle yourself and allies within 3m: +10% armor for 10s.", "Not against the blade. Against what stands behind it."), Kind = AbilityKind.AreaBuff,
                        Effect = StatusEffectKind.ArmorPercent, Magnitude = 10f, Duration = 10f, Radius = 3f, Cooldown = 60f, CastTime = 0.4f, Glyph = "HW", Color = light, Vfx = VfxStyle.Holy,
                        Art = "a silver aspergillum sprinkling glowing droplets of holy water"
                    }
                },
                Spells = new[]
                {
                    new AbilityDef
                    {
                        Name = "Minor Healing", Description = Text("The ally under the crosshair within 10m (or you): 15% of health over 3s.", "Hold on."), Kind = AbilityKind.Heal, IsSpell = true, Charges = 4,
                        Cooldown = 1f, CastTime = 1f, Magnitude = 15f, Duration = 3f, PercentHealth = true, Range = 10f, Flags = AbilityFlags.Healing, Glyph = "MH", Color = mending,
                        Vfx = VfxStyle.Heal, Art = "a small warm glow cupped between two hands"
                    },
                    new AbilityDef
                    {
                        Name = "Rest", Description = Text("A strike of light at the one under the crosshair within 15m: 22 magical damage, half to the living.", "Lie where you lay."), Kind = AbilityKind.TargetEffect,
                        IsSpell = true, Charges = 5, Cooldown = 1f, CastTime = 0.8f, Magnitude = 22f, Range = 15f, Stagger = 0.2f, Flags = AbilityFlags.Holy | AbilityFlags.HalfVsLiving,
                        Glyph = "Re", Color = light, Vfx = VfxStyle.Holy, Art = "a shaft of golden light striking a rising skeleton back into its grave"
                    },
                    new AbilityDef
                    {
                        Name = "Healing", Description = Text("The ally under the crosshair (or you): after a 2s prayer heals 30% of health at once.", "Stand up. It's not time yet."), Kind = AbilityKind.Heal, IsSpell = true,
                        Charges = 2, Cooldown = 1f, CastTime = 2f, Magnitude = 30f, PercentHealth = true, Range = 15f, Flags = AbilityFlags.Healing, Glyph = "He", Color = mending, Subclass = 0,
                        Vfx = VfxStyle.Heal, Art = "a radiant hand laid on a fallen warrior's forehead, wounds closing"
                    },
                    new AbilityDef
                    {
                        Name = "Circle of Intercession", Description = Text("A 3m circle around you for 6s: allies in it heal 3% of health a second.", "Closer. Everyone closer."), Kind = AbilityKind.Zone, IsSpell = true,
                        Charges = 2, Cooldown = 1f, CastTime = 1.2f, Zone = ZoneKind.Sanctuary, ZoneRadius = 3f, ZoneDuration = 6f, ZoneTick = 3f, Flags = AbilityFlags.Healing, Glyph = "CI",
                        Color = mending, Subclass = 0, Vfx = VfxStyle.Heal, Art = "a glowing golden circle of runes on a crypt floor with kneeling figures"
                    },
                    new AbilityDef
                    {
                        Name = "Blessing", Description = Text("The ally under the crosshair (or you): +2 to every attribute for 30s.", "Go. He is watching."), Kind = AbilityKind.Buff, IsSpell = true, Charges = 2,
                        Cooldown = 1f, CastTime = 0.8f, Effect = StatusEffectKind.AllAttributes, Magnitude = 2f, Duration = 30f, Range = 15f, Glyph = "Bl", Color = light, Subclass = 0, Vfx = VfxStyle.Holy,
                        Art = "a priest's hand raised in benediction over a bowed armored head"
                    },
                    new AbilityDef
                    {
                        Name = "Hallowed Weapon", Description = Text("For 15s your weapon hits deal 6 more magical damage of light, twice that to the undead.", "Bless the steel. I'll do the rest."),
                        Kind = AbilityKind.WeaponEnchant, IsSpell = true, Charges = 2, Cooldown = 1f, CastTime = 0.8f, Magnitude = 6f, Duration = 15f, Range = 0f, Effect = StatusEffectKind.HolyWeapon,
                        Flags = AbilityFlags.Holy, Glyph = "HW", Color = zeal, Subclass = 1, Vfx = VfxStyle.Holy, Art = "a flanged mace blazing with golden holy light"
                    },
                    new AbilityDef
                    {
                        Name = "Sentence", Description = Text("Your next weapon hit within 10s stuns for 1s.", "In the name of the Cathedral."), Kind = AbilityKind.Buff, IsSpell = true, Charges = 3, Cooldown = 1f,
                        CastTime = 0.5f, Effect = StatusEffectKind.Judgement, Magnitude = 1f, Duration = 10f, Range = 0f, Glyph = "Se", Color = zeal, Subclass = 1, Vfx = VfxStyle.Holy,
                        Art = "a judge's gavel of iron crackling with holy light"
                    },
                    new AbilityDef
                    {
                        Name = "Light of the Cathedral", Description = Text("A flash 3m around you: the undead take 18 magical damage and are slowed by 30% for 2s; the living are unharmed.", "Look. It's the last thing you'll see."),
                        Kind = AbilityKind.AreaDamage, IsSpell = true, Charges = 2, Cooldown = 1f, CastTime = 0.8f, Magnitude = 18f, Radius = 3f, Stagger = 0.3f, HitEffect = StatusEffectKind.Slow,
                        HitMagnitude = 30f, HitDuration = 2f, Flags = AbilityFlags.Holy | AbilityFlags.UndeadOnly, Glyph = "LC", Color = light, Subclass = 1, Vfx = VfxStyle.Holy,
                        Art = "a blinding burst of cathedral light through stained glass, skeletons recoiling"
                    },
                    new AbilityDef
                    {
                        Name = "Wither", Description = Text("The one under the crosshair within 15m loses 24 health over 5s; you heal for half of it.", "Yours is mine now."), Kind = AbilityKind.TargetEffect,
                        IsSpell = true, Charges = 4, Cooldown = 1f, CastTime = 0.9f, Magnitude = 24f, Duration = 5f, LifeSteal = 0.5f, Range = 15f, Flags = AbilityFlags.Dark, Glyph = "Wi",
                        Color = godless, Subclass = 2, Vfx = VfxStyle.Dark, Art = "a dark red tendril draining life from a withering hand"
                    },
                    new AbilityDef
                    {
                        Name = "Curse", Description = Text("The one under the crosshair: -3 to every attribute for 10s, and cursed.", "May the Sleeper remember your name."), Kind = AbilityKind.TargetEffect, IsSpell = true,
                        Charges = 2, Cooldown = 1f, CastTime = 0.8f, HitEffect = StatusEffectKind.Cursed, HitMagnitude = 3f, HitDuration = 10f, Range = 20f, Flags = AbilityFlags.Dark | AbilityFlags.Curse,
                        Glyph = "Cu", Color = godless, Subclass = 2, Vfx = VfxStyle.Dark, Art = "a violet curse sigil burning on a victim's forehead"
                    },
                    new AbilityDef
                    {
                        Name = "Whisper of the Sleeper", Description = Text("3m under the crosshair for 5s: everyone in it, allies too, loses 6 health a second and is slowed by 20%. Costs 10% of your health.", "He doesn't sleep. He listens."),
                        Kind = AbilityKind.Zone, IsSpell = true, Ground = true, Charges = 1, Cooldown = 1f, CastTime = 1.2f, Zone = ZoneKind.Whisper, ZoneRadius = 3f, ZoneDuration = 5f, ZoneTick = 6f,
                        HitEffect = StatusEffectKind.Slow, HitMagnitude = 20f, HitDuration = 0.6f, HealthCostPercent = 10f, Flags = AbilityFlags.Dark, Glyph = "WS", Color = godless, Subclass = 2,
                        Vfx = VfxStyle.Dark, Art = "a dark miasma of whispering faces swirling over a crypt floor"
                    }
                },
                Perks = new[]
                {
                    Stats("Firm Faith", "+3 Spirit.", "The words are the same. The voice is mine.", "a worn holy symbol on a leather cord", -1, new StatModifier(StatType.Spirit, 3f)),
                    Perk("Requiem", "+15% damage to the undead.", "Sleep. You've been sung to rest.", PerkEffect.Requiem, 0.15f, "a candle burning on a skull in a crypt niche"),
                    Perk("Altar Boy", "Prayers are cast 10% faster.", "A hundred times a day. The tongue remembers.", PerkEffect.Acolyte, 0.1f, "a young acolyte's lips moving over an open psalter"),
                    Perk("Warm Hands", "+20% to your healing.", "Everything is cold down here. Except the hands.", PerkEffect.WarmHands, 0.2f, "hands glowing warm orange over a cold stone slab", 0),
                    Perk("Shroud", "The one you heal takes 10% less damage for 4s.", "Under my hand they won't touch you.", PerkEffect.Shroud, 10f, "a translucent golden veil over a kneeling warrior", 0),
                    Perk("Patience", "+1 charge to healing prayers.", "Pray longer than it hurts.", PerkEffect.Patience, 1f, "an hourglass with glowing golden sand beside a prayer book", 0),
                    Perk("Last Rites", "Your healing is 30% stronger on allies below 30% health.", "Not today. I'm not ready to bury you.", PerkEffect.LastRites, 0.3f, "a priest holding a dying soldier's hand, light between them", 0),
                    Perk("Heavy Hand", "+10% damage with blunt weapons.", "Bone is not cut. Bone is crushed.", PerkEffect.HeavyHand, 0.1f, "a heavy iron flanged mace resting on a broken skull", 1),
                    Stats("Strength of Faith", "+3 Strength.", "Prayed with a mace in hand. Holds better.", "a muscular arm gripping a mace with a rosary wrapped on the haft", 1, new StatModifier(StatType.Strength, 3f)),
                    Perk("Righteous Wrath", "After you take a hit your next attack deals 20% more damage (at most once in 5s).", "You struck. Here's my answer.", PerkEffect.RighteousWrath, 20f, "a priest's furious eyes glowing gold behind a raised mace", 1),
                    Perk("Blood Tithe", "Every weapon hit heals you for 2% of your health.", "The Cathedral takes a tithe. So do I.", PerkEffect.BloodTithe, 2f, "a tithe bowl filling with drops of blood", 1),
                    Perk("Blood Prayer", "A prayer without charges can be read for 15% of your health.", "He lends. The interest is blood.", PerkEffect.BloodPrayer, 15f, "a bleeding palm pressed onto a black prayer book", 2),
                    Perk("Louder in the Dark", "While below half health your prayers deal 15% more damage.", "The less life in me, the louder He is.", PerkEffect.DarkVoice, 0.15f, "a gaunt figure screaming prayers with violet light from the mouth", 2),
                    Perk("Rot", "Foes you curse take 10% more damage from everyone.", "Rot does not choose.", PerkEffect.Rot, 10f, "rotting flesh with violet mould spreading", 2),
                    Perk("Take Back", "Killing a cursed foe restores 10% of your health.", "Given, and taken back.", PerkEffect.TakeBack, 10f, "a violet wisp of soul flowing from a corpse into an open hand", 2)
                },
                Kit = new[] { ("Morning Star", EquipSlot.Weapon1Main, 1, true), ("Round Shield", EquipSlot.Weapon1Off, 1, true), ("Spellbook", EquipSlot.Weapon2Main, 1, true),
                    ("Veinworker Shirt", EquipSlot.Chest, 1, true), ("Veinworker Trousers", EquipSlot.Legs, 1, true), ("Veinworker Boots", EquipSlot.Feet, 1, true), ("Campfire Kit", EquipSlot.Utility1, 1, true),
                    ("Bandage", EquipSlot.Utility2, 2, true), ("Torch", EquipSlot.Utility3, 1, true) },
                Weapons = new[] { WeaponClass.Mace, WeaponClass.Staff, WeaponClass.Spellbook, WeaponClass.Shield, WeaponClass.Torch },
                Focus = CastFocus.Magic
            };
        }

        /// A hunter of the dead forests: the vein's creatures are his game now.
        private static ClassDef CreateHuntsman()
        {
            Color hunt = new Color(0.45f, 0.72f, 0.35f);
            Color marksman = new Color(0.95f, 0.85f, 0.45f);
            Color tracker = new Color(0.55f, 0.85f, 0.9f);
            Color trapper = new Color(0.75f, 0.55f, 0.35f);

            return new ClassDef
            {
                Id = 4, Name = "Huntsman", Description = "A hunter from the dead forests: dust from the spoil heaps poisoned the woods above and the game left. Now his game is the creatures of the vein, and the House of Scales buys the trophies.",
                Stats = new ClassStats(12, 13, 8, 15, 21, 21), Color = hunt, Body = Color.white,
                Subclasses = new[]
                {
                    Subclass("Marksman", "Damage from afar. The last hunter of his village.", marksman, "a longbow at full draw aimed into darkness"),
                    Subclass("Tracker", "Scouting and support, walks first. Led hunts for the lords while there was anything to hunt.", tracker, "fresh footprints in dust lit by a burning arrow"),
                    Subclass("Trapper", "Control. Set snares for wolves; now traps for the dead.", trapper, "a rusted iron bear trap with jagged teeth")
                },
                Skills = new[]
                {
                    new AbilityDef
                    {
                        Name = "Exhale", Description = Text("Your next shot within 10s deals 50% more damage.", "Breathe out. Then let go."), Kind = AbilityKind.Buff, Effect = StatusEffectKind.Exhale, Magnitude = 50f,
                        Duration = 10f, Cooldown = 30f, CastTime = 0.15f, Glyph = "Ex", Color = hunt, Vfx = VfxStyle.Nature, Art = "a hunter's breath misting in cold air as he aims a bow"
                    },
                    new AbilityDef
                    {
                        Name = "Full Quiver", Description = Text("Draw bows and reload crossbows 30% faster for 8s.", "While they stand, shoot."), Kind = AbilityKind.Buff, Effect = StatusEffectKind.QuickReload,
                        Magnitude = 30f, Duration = 8f, Cooldown = 60f, CastTime = 0.15f, Glyph = "FQ", Color = hunt, Vfx = VfxStyle.Nature, Art = "a leather quiver stuffed with fletched arrows"
                    },
                    new AbilityDef
                    {
                        Name = "Armor-Piercing", Description = Text("Your next shot within 10s ignores armor.", "Plate is the same hide, just louder."), Kind = AbilityKind.Buff, Effect = StatusEffectKind.PiercingShot,
                        Magnitude = 1f, Duration = 10f, Cooldown = 40f, CastTime = 0.15f, Glyph = "AP", Color = marksman, Subclass = 0, Vfx = VfxStyle.Physical,
                        Art = "a bodkin arrowhead punching through a steel breastplate"
                    },
                    new AbilityDef
                    {
                        Name = "Pin Down", Description = Text("Your next shot within 10s slows the target by 40% for 3s.", "The legs. Then take your time."), Kind = AbilityKind.Buff, Effect = StatusEffectKind.PinningShot,
                        Magnitude = 40f, Duration = 10f, Cooldown = 30f, CastTime = 0.15f, Glyph = "PD", Color = marksman, Subclass = 0, Vfx = VfxStyle.Physical,
                        Art = "an arrow pinning a fleeing leg to the ground"
                    },
                    new AbilityDef
                    {
                        Name = "Flare Arrow", Description = Text("Your next shot within 10s burns where it lands and lights the place like a torch for 15s.", "Let's see who's breathing there."), Kind = AbilityKind.Buff,
                        Effect = StatusEffectKind.FlareShot, Magnitude = 1f, Duration = 10f, Cooldown = 30f, CastTime = 0.15f, Glyph = "FA", Color = tracker, Subclass = 1, Vfx = VfxStyle.Fire,
                        Art = "a burning arrow stuck in a crypt wall lighting the darkness"
                    },
                    new AbilityDef
                    {
                        Name = "Tracking", Description = Text("For 8s you see the silhouettes of foes through walls within 15m.", "Quiet. Listen."), Kind = AbilityKind.Buff, Effect = StatusEffectKind.Tracking,
                        Magnitude = 15f, Duration = 8f, Cooldown = 60f, CastTime = 0.3f, Glyph = "Tr", Color = tracker, Subclass = 1, Vfx = VfxStyle.Nature,
                        Art = "glowing outlines of skeletons seen through a stone wall"
                    },
                    new AbilityDef
                    {
                        Name = "Hunting Trap", Description = Text("Set a trap at your feet: the first to step in (not you; allies too) takes 15 damage and cannot move for 2s. Up to 2 at once.", "Watch your step. I warned you."),
                        Kind = AbilityKind.Spawn, SpawnPrefab = HuntingTrap, Radius = 0.9f, Cooldown = 20f, CastTime = 0.8f, Glyph = "HT", Color = trapper, Subclass = 2, Vfx = VfxStyle.Physical,
                        Art = "an iron leg-hold trap snapped shut on a skeletal foot"
                    },
                    new AbilityDef
                    {
                        Name = "Hunting Net", Description = Text("Throw a weighted net: the one it hits is slowed by 50% for 3s.", "Won't kill. Won't let go either."), Kind = AbilityKind.Throw,
                        Projectile = ProjectileKind.Net, Speed = 18f, Gravity = -6f, Magnitude = 2f, DamageType = DamageType.Physical, HitEffect = StatusEffectKind.Slow, HitMagnitude = 50f, HitDuration = 3f,
                        Cooldown = 40f, CastTime = 0.2f, Glyph = "HN", Color = trapper, Subclass = 2, Vfx = VfxStyle.Physical, Art = "a weighted rope net flying through the air"
                    }
                },
                Perks = new[]
                {
                    Perk("Beast Habit", "+10% damage to monsters (not to players).", "A beast is a beast, however much stone grows in it.", PerkEffect.BeastHabit, 0.1f, "a hunter's skinning knife beside a spider's crystal-grown leg"),
                    Stats("Knack", "Draw bows and reload crossbows 15% faster.", "The hand remembers. The head may sleep.", "fingers nocking an arrow in one fluid motion", -1, new StatModifier(StatType.ReloadSpeed, 15f)),
                    Stats("Forest Hardened", "+3 Agility.", "Thirty winters after deer through windfall.", "a hunter leaping over a fallen dead tree", -1, new StatModifier(StatType.Agility, 3f)),
                    Stats("Taut String", "+15% damage of ranged weapons.", "Grandfather's bow. My string.", "a bowstring pulled taut, creaking", 0, new StatModifier(StatType.RangedDamageBonus, 0.15f)),
                    Perk("Long Shot", "Shots hit harder with distance, up to +20% at 20m and beyond.", "Closer is for butchers.", PerkEffect.LongShot, 0.2f, "a distant target seen down the shaft of an arrow", 0),
                    Perk("Finishing Shot", "+20% damage to targets below 30% health.", "A wounded beast is the most dangerous. Give it no time.", PerkEffect.FinishOff, 0.2f, "an arrow striking a wounded fleeing creature", 0),
                    Stats("Steady Hand", "+3 Dexterity.", "Don't shake. He doesn't.", "a perfectly still hand on a crossbow trigger", 0, new StatModifier(StatType.Dexterity, 3f)),
                    Perk("Cat's Eye", "Your torch lights 50% farther.", "Nights are darker in the woods. Used to it.", PerkEffect.CatEye, 0.5f, "a glowing feline eye reflecting torchlight in darkness", 1),
                    Perk("Trap Sense", "Traps hurt you 50% less.", "The same snare, only of stone.", PerkEffect.TrapSense, 0.5f, "a hand carefully lifting a tripwire in a dungeon corridor", 1),
                    Perk("Vein Expert", "Search chests and bodies 30% faster.", "Skin it quick, before they come.", PerkEffect.VeinExpert, 0.3f, "hands rummaging through an opened ironbound chest", 1),
                    Perk("Hunter's Mark", "Your hits mark the target for 5s: everyone deals 10% more damage to it.", "That one. The lame one.", PerkEffect.HuntersMark, 10f, "a red hunter's sigil painted on a target", 1),
                    Perk("Heavy Jaws", "Your trap holds 1s longer.", "The wolf chewed off its paw. These won't.", PerkEffect.HeavyJaws, 1f, "massive iron jaws of a trap with thick springs", 2),
                    Perk("Poisoned Teeth", "Whoever steps into your trap is poisoned for 4s.", "Wolfsbane. The dead don't care, the living have enough.", PerkEffect.PoisonTeeth, 4f, "trap teeth smeared with dark green poison", 2),
                    Perk("Iron Stock", "+1 trap on the ground at once.", "Carries half a pood of iron on his back.", PerkEffect.IronStock, 1f, "a pack loaded with chains and iron traps", 2),
                    Perk("Prey in the Snare", "+15% damage to rooted and slowed targets.", "Caught? Don't struggle.", PerkEffect.SnaredPrey, 0.15f, "a creature tangled in a net struggling", 2)
                },
                Kit = new[] { ("Crossbow", EquipSlot.Weapon1Main, 1, true), ("Arming Sword", EquipSlot.Weapon2Main, 1, true), ("Veinworker Hood", EquipSlot.Head, 1, true),
                    ("Veinworker Shirt", EquipSlot.Chest, 1, true), ("Veinworker Trousers", EquipSlot.Legs, 1, true), ("Veinworker Boots", EquipSlot.Feet, 1, true),
                    ("Throwing Knife", EquipSlot.Utility1, 2, true), ("Bandage", EquipSlot.Utility2, 2, true), ("Torch", EquipSlot.Utility3, 1, true), ("Crossbow Bolts", EquipSlot.Head, 20, false) },
                Weapons = new[] { WeaponClass.Bow, WeaponClass.Crossbow, WeaponClass.Sword, WeaponClass.Spear, WeaponClass.Torch }
            };
        }

        /// A grave robber: the Cathedral chased him off every churchyard; in the vein the same work pays. No stealth in the game.
        private static ClassDef CreateKnave()
        {
            Color shade = new Color(0.55f, 0.6f, 0.55f);
            Color assassin = new Color(0.85f, 0.25f, 0.3f);
            Color venom = new Color(0.5f, 0.85f, 0.3f);
            Color thrower = new Color(0.8f, 0.8f, 0.85f);

            return new ClassDef
            {
                Id = 5, Name = "Knave", Description = "A grave robber: the Cathedral chased him off every churchyard and threatened the stake. The vein is the biggest graveyard in the world, and here the same work pays. Takes what the dead wear.",
                Stats = new ClassStats(11, 12, 8, 15, 22, 22), Color = shade, Body = Color.white,
                Subclasses = new[]
                {
                    Subclass("Assassin", "One target: open it fast, finish it fast. Cut churchyard watchmen at night.", assassin, "a thin stiletto dripping blood in moonlight"),
                    Subclass("Poisoner", "Lingering damage and weakness with poison. Poisoned rats in the crypts, then rival grave robbers.", venom, "a skull-stoppered vial of green poison"),
                    Subclass("Thrower", "Damage with belt knives and axes. Hit the watchdogs with knives over the churchyard fence.", thrower, "a fan of throwing knives in flight")
                },
                Skills = new[]
                {
                    new AbilityDef
                    {
                        Name = "Blade Venom", Description = Text("For 20s your weapon hits poison the target for 3s.", "Rat poison from the watch hut. Enough for people too."), Kind = AbilityKind.Buff,
                        Effect = StatusEffectKind.VenomBlade, Magnitude = 3f, Duration = 20f, Cooldown = 50f, CastTime = 0.3f, Glyph = "BV", Color = venom, Vfx = VfxStyle.Poison,
                        Art = "green venom dripping along a dagger's edge"
                    },
                    new AbilityDef
                    {
                        Name = "Take to Your Heels", Description = Text("+20% move speed for 6s.", "The watchman woke. Run."), Kind = AbilityKind.Buff, Effect = StatusEffectKind.Haste, Magnitude = 20f,
                        Duration = 6f, Cooldown = 45f, CastTime = 0.1f, Glyph = "TH", Color = shade, Vfx = VfxStyle.Smoke, Art = "a cloaked figure sprinting away past gravestones, dust trailing"
                    },
                    new AbilityDef
                    {
                        Name = "Open Up", Description = Text("Your next weapon hit within 10s ignores armor and deals 30% more damage.", "Between the plates. Like a coffin lid."), Kind = AbilityKind.Buff,
                        Effect = StatusEffectKind.Expose, Magnitude = 30f, Duration = 10f, Cooldown = 40f, CastTime = 0.15f, Glyph = "OU", Color = assassin, Subclass = 0, Vfx = VfxStyle.Blood,
                        Art = "a stiletto sliding between two plates of armor"
                    },
                    new AbilityDef
                    {
                        Name = "Finish Him", Description = Text("Within 10s your next weapon hit on a foe below 40% health deals double damage.", "Don't torment. Finish."), Kind = AbilityKind.Buff,
                        Effect = StatusEffectKind.Execute, Magnitude = 2f, Duration = 10f, Cooldown = 30f, CastTime = 0.15f, Glyph = "FH", Color = assassin, Subclass = 0, Vfx = VfxStyle.Blood,
                        Art = "a dagger poised over the throat of a fallen wounded enemy"
                    },
                    new AbilityDef
                    {
                        Name = "Venom Bubble", Description = Text("Throw a flask that breaks on impact: a poison cloud 3m wide for 5s poisons everyone in it, allies too.", "Don't breathe. Free advice."),
                        Kind = AbilityKind.Throw, Projectile = ProjectileKind.Poison, Speed = 15f, Gravity = -9.81f, Zone = ZoneKind.PoisonCloud, ZoneRadius = 3f, ZoneDuration = 5f,
                        HitEffect = StatusEffectKind.Poison, HitMagnitude = 4f, HitDuration = 2f, Flags = AbilityFlags.Poison, Cooldown = 40f, CastTime = 0.2f, Glyph = "VB", Color = venom,
                        Subclass = 1, Vfx = VfxStyle.Poison, Art = "a glass bubble of green gas bursting into a toxic cloud"
                    },
                    new AbilityDef
                    {
                        Name = "Dead Root", Description = Text("Your next weapon hit within 10s poisons hard: 18 damage over 6s and a 20% slow.", "Grows on graves. Guess what it feeds on."), Kind = AbilityKind.Buff,
                        Effect = StatusEffectKind.DeadRoot, Magnitude = 18f, Duration = 10f, Flags = AbilityFlags.Poison, Cooldown = 30f, CastTime = 0.2f, Glyph = "DR", Color = venom, Subclass = 1,
                        Vfx = VfxStyle.Poison, Art = "a black gnarled root growing from a grave, oozing poison"
                    },
                    new AbilityDef
                    {
                        Name = "Spare Knife", Description = Text("+2 Throwing Knives into your belt (not over the stack).", "There's always another. In the boot."), Kind = AbilityKind.GiveItem, Item = "Throwing Knife",
                        Magnitude = 2f, Cooldown = 60f, CastTime = 0.3f, Glyph = "SK", Color = thrower, Subclass = 2, Vfx = VfxStyle.Physical, Art = "a throwing knife pulled from a boot sheath"
                    },
                    new AbilityDef
                    {
                        Name = "Knife Fan", Description = Text("Your next knife throw within 10s flies as a fan of 3 knives (one is spent).", "One of them will find it."), Kind = AbilityKind.Buff,
                        Effect = StatusEffectKind.KnifeFan, Magnitude = 3f, Duration = 10f, Cooldown = 30f, CastTime = 0.15f, Glyph = "KF", Color = thrower, Subclass = 2, Vfx = VfxStyle.Physical,
                        Art = "three throwing knives fanned out between fingers"
                    }
                },
                Perks = new[]
                {
                    Perk("Knows Where to Look", "Search bodies 50% faster (not chests).", "Rings on the fingers. Coins in the mouth.", PerkEffect.GraveSense, 0.5f, "a hand pulling a ring off a skeletal finger"),
                    Stats("Light-Footed", "+5% move speed.", "With a shovel, at a run. Without, faster still.", "a soft leather boot stepping silently on a gravestone", -1, new StatModifier(StatType.MoveSpeed, 15f)),
                    Stats("Nimble Fingers", "+3 Dexterity.", "Take the ring without waking the widow.", "nimble fingers picking a lock", -1, new StatModifier(StatType.Dexterity, 3f)),
                    Perk("First Blood", "+40% damage of the first hit on a foe at full health.", "The first is the one that matters. The second is to be sure.", PerkEffect.FirstBlood, 0.4f, "a single drop of blood falling from a blade tip", 0),
                    Perk("Short Blade", "+15% damage with daggers and throwing knives.", "Long ones are for duels. Short ones for work.", PerkEffect.ShortBlade, 0.15f, "a short curved dagger with a worn grip", 0),
                    Perk("For the Throat", "+20% damage of hits on the head.", "A watchman screams once. If he gets to.", PerkEffect.Throat, 0.2f, "a blade at an exposed throat", 0),
                    Perk("Clean Work", "A kill cuts the current cooldowns of your skills by 5s.", "One less. Next.", PerkEffect.CleanWork, 5f, "a dagger wiped clean on a cloth", 0),
                    Perk("Thick Venom", "Your poison lasts 2s longer.", "Brew slowly. Kill slowly too.", PerkEffect.ThickVenom, 2f, "thick green venom stretching between two fingers", 1),
                    Perk("Caustic Venom", "+25% damage of your poison.", "Rats die at once. People slower. Fixed that.", PerkEffect.CausticVenom, 0.25f, "a vial of bubbling acid-green poison", 1),
                    Perk("Weakness", "Foes you poison deal 10% less damage.", "The hand still holds the knife. Not firmly.", PerkEffect.Weakness, 10f, "a trembling hand dropping a sword", 1),
                    Perk("Rotten Blood", "Foes you poison heal half as well.", "Bandage it. Won't help.", PerkEffect.RotBlood, 50f, "dark rotten blood seeping through a bandage", 1),
                    Perk("Bundle of Knives", "+2 throwing weapons in every belt stack.", "One for the dog. The rest for the master.", PerkEffect.KnifeBundle, 2f, "a leather bandolier full of throwing knives", 2),
                    Perk("True Throw", "+15% damage of throwing weapons.", "Over the fence, in the dark, in the eye.", PerkEffect.TrueThrow, 0.15f, "a throwing axe embedded in a skull", 2),
                    Perk("Quick Hand", "Throwing weapons are thrown 25% faster.", "While you aim, I've already thrown.", PerkEffect.QuickThrow, 0.25f, "a blurred hand releasing a knife", 2),
                    Perk("Oiled Blades", "Blade Venom poisons with throwing weapons too.", "A scratch is enough.", PerkEffect.OiledBlades, 1f, "throwing knives coated in green poison oil", 2)
                },
                Kit = new[] { ("Arming Sword", EquipSlot.Weapon1Main, 1, true), ("Veinworker Hood", EquipSlot.Head, 1, true), ("Veinworker Shirt", EquipSlot.Chest, 1, true),
                    ("Veinworker Trousers", EquipSlot.Legs, 1, true), ("Veinworker Boots", EquipSlot.Feet, 1, true), ("Throwing Knife", EquipSlot.Utility1, 2, true), ("Lockpick", EquipSlot.Utility2, 1, true),
                    ("Bandage", EquipSlot.Utility3, 2, true), ("Torch", EquipSlot.Utility4, 1, true) },
                Weapons = new[] { WeaponClass.Sword, WeaponClass.Dagger, WeaponClass.Crossbow, WeaponClass.Bow, WeaponClass.Spear, WeaponClass.Torch }
            };
        }

        /// Effect line and the flavor quote under it.
        private static string Text(string effect, string flavor)
        {
            return $"{effect}\n<i><color=#a89f8a>\"{flavor}\"</color></i>";
        }

        private static SubclassDef Subclass(string name, string description, Color color, string art)
        {
            return new SubclassDef { Name = name, Description = description, Color = color, Art = art };
        }

        private static PerkDef Perk(string name, string effect, string flavor, PerkEffect perk, float value, string art, int subclass = -1)
        {
            return new PerkDef(name, Text(effect, flavor)) { Effect = perk, Value = value, Art = art, Subclass = subclass };
        }

        private static PerkDef Stats(string name, string effect, string flavor, string art, int subclass, params StatModifier[] modifiers)
        {
            return new PerkDef(name, Text(effect, flavor), modifiers) { Art = art, Subclass = subclass };
        }

        /// Spell memory skills store the wheel they open in the magnitude.
        private static AbilityDef Memory(string name, string description, string glyph, Color color, string art)
        {
            return new AbilityDef { Name = name, Description = description, Kind = AbilityKind.SpellMemory, Cooldown = 0f, Glyph = glyph, Color = color, Art = art };
        }
    }
}
