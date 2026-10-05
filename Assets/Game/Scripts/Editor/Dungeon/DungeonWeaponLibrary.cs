using System.Collections.Generic;
using Game.Scripts.Battle;
using Game.Scripts.Editor.Battle;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Full weapon catalog for the dungeon. The first four entries keep the battle scene's slot layout intact.
    internal static class DungeonWeaponLibrary
    {
        public const string Fists = "Fists";
        public const string SwordShield = "SwordShield";
        public const string Greatsword = "Greatsword";
        public const string Bow = "Bow";
        public const string SwordShieldLeft = "SwordShieldLeft";
        public const string ArmingSword = "ArmingSword";
        public const string Falchion = "Falchion";
        public const string Longsword = "Longsword";
        public const string BattleAxe = "BattleAxe";
        public const string Spear = "Spear";
        public const string Mace = "Mace";
        public const string Dagger = "Dagger";
        public const string Crossbow = "Crossbow";
        public const string Staff = "Staff";
        public const string Torch = "Torch";
        public const string MaceShield = "MaceShield";
        public const string Spellbook = "Spellbook";
        public const string Lute = "Lute";
        public const string BearClaws = "BearClaws";
        public const string PantherClaws = "PantherClaws";
        public const string RatBite = "RatBite";
        public const string ShortSword = "ShortSword";
        public const string Rapier = "Rapier";
        public const string VikingSword = "VikingSword";
        public const string Hatchet = "Hatchet";
        public const string MorningStar = "MorningStar";
        public const string CastillonDagger = "CastillonDagger";
        public const string Stiletto = "Stiletto";
        public const string FellingAxe = "FellingAxe";
        public const string WarMaul = "WarMaul";
        public const string Halberd = "Halberd";
        public const string HorsemansAxe = "HorsemansAxe";

        private const float MaceHead = 0.6f;
        private const float AxeHead = 0.8f;
        /// Middle of the bearded blade of the Horseman's Axe along the haft, from the grip.
        private const float HorsemanHead = 0.7f;
        /// A long axe is held at the butt with the off hand, this far down the haft from the main hand.
        private const float AxeGrip = -0.42f;
        private const float Frame = 1f / BattleAnimationBuilder.FrameRate;
        /// A frame of 60 fps footage of Dark and Darker. A fight there runs at full pace and ours at the base action
        /// speed of the fighters, so a clip is that much shorter than what it is copied from.
        private const float Footage = 0.75f * Frame;
        private const float SpearHead = 1.8f;
        private const float StaffHands = -0.55f;
        private const float StaffReach = -0.62f;
        private const float StaffButt = -0.9f;
        private const float StaffButtEnd = -1.2f;

        /// Catalog order = combat catalog index. Battle prefab slots 1-4 map to the first four entries; monsters refer to
        /// their weapon by index, so new entries go to the end.
        public static readonly string[] CatalogOrder =
        {
            SwordShield, Greatsword, Bow, SwordShieldLeft, Fists, ArmingSword, Falchion, Longsword, BattleAxe, Spear, Mace, Dagger, Crossbow, Staff, Torch, MaceShield,
            Spellbook, Lute, BearClaws, PantherClaws, RatBite,
            ShortSword, Rapier, VikingSword, Hatchet, MorningStar, CastillonDagger, Stiletto, FellingAxe, WarMaul, Halberd,
            HorsemansAxe
        };

        /// Catalog entries that play another entry's clips (its prefix) with a model, reach and damage of their own.
        /// Their definitions keep the timings of the source, otherwise the shared clips would not match, and their blade
        /// has to cover the strike point of the source, which is what the clips bring to the crosshair.
        private static readonly (string name, string source, string displayName)[] s_variants =
        {
            (Spellbook, Fists, "Spellbook"), (Lute, Fists, "Lute"), (RatBite, Fists, "Rat Bite"),
            (ShortSword, ArmingSword, "Short Sword"), (Rapier, ArmingSword, "Rapier"), (VikingSword, Falchion, "Viking Sword"),
            (MorningStar, Mace, "Morning Star"), (CastillonDagger, Dagger, "Castillon Dagger"),
            (Stiletto, Dagger, "Stiletto Dagger"), (FellingAxe, BattleAxe, "Felling Axe"), (Halberd, Spear, "Halberd")
        };

        /// Impact of the weapon's hits and Stability of its block, 1..10: a hit with more Impact than the Stability breaks the block.
        private static readonly Dictionary<string, (int impact, int stability)> s_force = new()
        {
            ["Bare Hands"] = (1, 1), ["Spellbook"] = (1, 1), ["Lute"] = (1, 1), ["Rat Bite"] = (1, 1), ["Torch"] = (2, 1),
            ["Rondel Dagger"] = (2, 1), ["Castillon Dagger"] = (2, 1), ["Stiletto Dagger"] = (2, 1), ["Rapier"] = (2, 2),
            ["Short Sword"] = (3, 3), ["Arming Sword"] = (4, 3), ["Falchion"] = (4, 3), ["Hatchet"] = (4, 2), ["Viking Sword"] = (5, 3),
            ["Flanged Mace"] = (6, 3), ["Morning Star"] = (6, 3), ["Longsword"] = (5, 5), ["Spear"] = (4, 4), ["Magic Staff"] = (4, 4),
            ["Sword & Shield"] = (4, 7), ["Mace & Shield"] = (6, 7),
            ["Greatsword"] = (7, 5), ["Battle Axe"] = (7, 4), ["Felling Axe"] = (8, 4), ["Halberd"] = (7, 4),
            ["War Maul"] = (9, 5), ["Horseman's Axe"] = (6, 3), ["Bow"] = (3, 1), ["Crossbow"] = (5, 1), ["Panther Claws"] = (4, 2), ["Bear Claws"] = (7, 4)
        };

        public static (int impact, int stability) Force(string displayName)
        {
            return s_force.TryGetValue(displayName, out (int, int) force) ? force : (3, 3);
        }

        public static string SharedPrefix(string name)
        {
            if (name == SwordShieldLeft)
                return SwordShield;

            foreach ((string variant, string source, string _) in s_variants)
            {
                if (variant == name)
                    return source;
            }

            return name;
        }

        /// Display name of the definition a variant entry is built from; null for entries with clips of their own.
        public static string VariantName(string name)
        {
            foreach ((string variant, string _, string displayName) in s_variants)
            {
                if (variant == name)
                    return displayName;
            }

            return null;
        }

        public static bool IsVariant(WeaponDefinition definition)
        {
            foreach ((string _, string _, string displayName) in s_variants)
            {
                if (displayName == definition.DisplayName)
                    return true;
            }

            return false;
        }

        public static WeaponDefinition[] CreateAll()
        {
            return new[]
            {
                BattleAnimationLibrary.CreateSwordShield(),
                CreateZweihander(),
                BattleAnimationLibrary.CreateBow(),
                CreateFists(),
                CreateArmingSword(),
                CreateFalchion(),
                CreateLongsword(),
                CreateBattleAxe(),
                CreateSpear(),
                CreateMace(),
                CreateDagger(),
                CreateCrossbow(),
                CreateStaff(),
                CreateTorch(),
                CreateMaceShield(),
                CreateSpellbook(),
                CreateLute(),
                CreateBearClaws(),
                CreatePantherClaws(),
                CreateRatBite(),
                OneHandedSword(ArmingSword, "Short Sword", 0.1f, 0.72f, 1.22f, 24, 0.4f, 0.2f, 0.5f),
                OneHandedSword(ArmingSword, "Rapier", 0.12f, 1f, 1.5f, 25, 0.4f, 0.2f, 0.5f),
                OneHandedSword(Falchion, "Viking Sword", 0.12f, 0.92f, 1.42f, 36, 0.65f, 0.17f, 0.4f),
                OneHandedSword(Hatchet, "Hatchet", 0.4f, 0.6f, 1.15f, 31, 0.65f, 0.17f, 0.4f),
                CreateMorningStar(),
                CreateDaggerVariant("Castillon Dagger", 0.5f, 1.12f, 18),
                CreateDaggerVariant("Stiletto Dagger", 0.46f, 1.08f, 14),
                CreateAxeVariant("Felling Axe", 0.68f, 0.94f, 1.6f, 37, 44, 0.3f, 0.4f),
                CreateWarMaul(),
                CreateHalberd(),
                CreateHorsemansAxe()
            };
        }

        private static WeaponDefinition CreateMorningStar()
        {
            WeaponDefinition definition = OneHandedSword(Mace, "Morning Star", 0.1f, 0.7f, 1.25f, 34, 0.55f, 0.18f, 0.5f);
            definition.Strike = MaceHead;
            definition.Attacks[2].Stagger = 0.35f;

            return definition;
        }

        private static WeaponDefinition CreateDaggerVariant(string name, float bladeTip, float reach, int damage)
        {
            WeaponDefinition definition = OneHandedSword(Dagger, name, 0.06f, bladeTip, reach, damage, 0.3f, 0.12f, 0.3f);
            definition.Attacks[2].Damage = damage;
            definition.Attacks[2].Stagger = 0f;

            return definition;
        }

        private static WeaponDefinition CreateAxeVariant(string name, float bladeBase, float bladeTip, float reach, int damage, int heavyDamage,
            float stagger, float heavyStagger)
        {
            WeaponDefinition definition = CreateBattleAxe();
            definition.DisplayName = name;
            definition.BladeBase = bladeBase;
            definition.BladeTip = bladeTip;
            definition.Reach = reach;

            foreach (AttackDefinition attack in definition.Attacks)
            {
                attack.Damage = damage;
                attack.Stagger = stagger;
            }

            definition.Attacks[2].Damage = heavyDamage;
            definition.Attacks[2].Stagger = heavyStagger;
            definition.Riposte.Damage = Mathf.RoundToInt(damage * 1.5f);
            definition.Riposte.Stagger = heavyStagger;

            return definition;
        }

        /// Both swings are horizontal: from the right, then back from the left.
        private static WeaponDefinition CreateZweihander()
        {
            WeaponDefinition definition = BattleAnimationLibrary.CreateGreatsword();
            definition.Attacks = new[] { definition.Attacks[0], definition.Attacks[1] };

            return definition;
        }

        /// Clips of its own: the swings of the zweihander at a heavier pace.
        private static WeaponDefinition CreateWarMaul()
        {
            WeaponDefinition definition = CreateZweihander();
            AttackDefinition[] swings = definition.Attacks;
            definition.Prefix = WarMaul;
            definition.DisplayName = "War Maul";
            definition.BladeBase = 0.82f;
            definition.BladeTip = 1.15f;
            definition.Reach = 1.75f;
            definition.DeflectDuration = 0.9f;
            definition.BlockMitigation = 0.7f;
            definition.BlockBoxCenter = new Vector3(0f, 0f, 0.5f);
            definition.BlockBoxExtents = new Vector3(0.08f, 0.08f, 0.45f);
            swings[0].Damage = 47;
            swings[0].Windup = 0.75f;
            swings[0].Stagger = 0.4f;
            swings[1].Damage = 56;
            swings[1].Windup = 0.8f;
            swings[1].Stagger = 0.55f;

            foreach (AttackDefinition swing in swings)
            {
                swing.ComboStart = swing.Windup + swing.Active * 0.5f;
                swing.ComboEnd = swing.Windup + swing.Active + swing.Recovery * 0.65f;
            }

            return definition;
        }

        private static WeaponDefinition CreateHalberd()
        {
            WeaponDefinition definition = CreateSpear();
            definition.DisplayName = "Halberd";
            definition.BladeBase = 1.4f;
            definition.Attacks[0].Damage = 37;
            definition.Attacks[1].Damage = 37;
            definition.Attacks[2].Damage = 42;

            return definition;
        }

        private static WeaponDefinition CreateSpellbook()
        {
            WeaponDefinition definition = CreateFists();
            definition.DisplayName = "Spellbook";
            definition.Attacks[0].Damage = 18;
            definition.Attacks[1].Damage = 18;

            return definition;
        }

        private static WeaponDefinition CreateLute()
        {
            WeaponDefinition definition = CreateFists();
            definition.DisplayName = "Lute";
            definition.Attacks[0].Damage = 12;
            definition.Attacks[1].Damage = 12;

            return definition;
        }

        private static WeaponDefinition CreateRatBite()
        {
            WeaponDefinition definition = CreateFists();
            definition.DisplayName = "Rat Bite";
            definition.Attacks[0].Damage = 2;
            definition.Attacks[1].Damage = 2;
            definition.CanBlock = false;

            return definition;
        }

        private static WeaponDefinition CreateBearClaws()
        {
            WeaponDefinition definition = CreateFists();
            definition.Prefix = BearClaws;
            definition.DisplayName = "Bear Claws";
            definition.Reach = 1.5f;
            definition.BladeTip = 0.35f;
            definition.CanBlock = false;

            foreach (AttackDefinition attack in definition.Attacks)
            {
                attack.Damage = 27;
                attack.Windup = 0.5f;
                attack.Active = 0.18f;
                attack.Recovery = 0.55f;
                attack.ComboStart = 0.6f;
                attack.ComboEnd = 1.1f;
                attack.Stagger = 0.3f;
                attack.MoveMultiplier = 0.7f;
            }

            return definition;
        }

        private static WeaponDefinition CreatePantherClaws()
        {
            WeaponDefinition definition = CreateFists();
            definition.Prefix = PantherClaws;
            definition.DisplayName = "Panther Claws";
            definition.Reach = 1.2f;
            definition.BladeTip = 0.25f;
            definition.CanBlock = false;

            foreach (AttackDefinition attack in definition.Attacks)
            {
                attack.Damage = 23;
                attack.Windup = 0.18f;
                attack.Active = 0.1f;
                attack.Recovery = 0.22f;
                attack.ComboStart = 0.2f;
                attack.ComboEnd = 0.48f;
                attack.MoveMultiplier = 0.9f;
            }

            return definition;
        }

        public static WeaponDefinition Find(IEnumerable<WeaponDefinition> definitions, string prefix)
        {
            foreach (WeaponDefinition definition in definitions)
            {
                if (definition.Prefix == prefix)
                    return definition;
            }

            return null;
        }

        /// Bare hands of Dark and Darker. Open hands wait low in the view; a hook comes round from the right, then one
        /// from the left, each while the other hand reaches out at the target; the block shuts both forearms before
        /// the face. A fist is a short blade that runs across it, from the palm to the thumb.
        private static WeaponDefinition CreateFists()
        {
            BodyPose idle = BattleAnimationLibrary.Guard(new(0.2f, 1.47f, 0.48f), new(-0.3f, 0.95f, 0f), new(-0.35f, 0.45f, 0.8f), 0.6f);

            return new WeaponDefinition
            {
                Prefix = Fists,
                DisplayName = "Bare Hands",
                Kind = WeaponKind.OneHanded,
                Reach = 0.9f,
                DeflectDuration = 0.4f,
                BladeBase = 0f,
                BladeTip = 0.12f,
                IsUnarmed = true,
                Idle = idle,
                Attacks = new[] { Hook(34 * Frame, false), Hook(22 * Frame, true) },
                CanBlock = true,
                BlockRaise = 0.15f,
                BlockMitigation = 0.4f,
                BlockImpact = 0.25f,
                BlockRecovery = 0.3f,
                BlockAngle = 80f,
                BlockMove = 0.7f,
                Block = BattleAnimationLibrary.Guard(new(0.045f, 1.87f, 0.29f), new(1f, 0f, -0.2f), Vector3.up),
                BlockHit = BattleAnimationLibrary.Guard(new(0.05f, 1.83f, 0.24f), new(1f, 0f, -0.2f), new(0f, 1f, -0.15f), pitch: -4f),
                BlockLowered = BattleAnimationLibrary.Guard(new(0.05f, 1.81f, 0.27f), new(1f, 0f, -0.2f), Vector3.up),
                DeflectPose = idle,
                BlockSocket = WeaponSocket.LeftHand,
                BlockBoxCenter = Vector3.zero,
                BlockBoxExtents = new Vector3(0.14f, 0.14f, 0.08f)
            };
        }

        /// The fist is drawn back to the side while the free hand reaches out, then swings level across the view.
        private static AttackDefinition Hook(float windup, bool isOffHand)
        {
            const float active = 9 * Frame;
            const float recovery = 0.5f;
            Vector3 guard = new Vector3(-0.2f, 1.36f, 0.3f);
            Vector3 ahead = new Vector3(0.2f, 0.25f, 0.95f);

            return new AttackDefinition
            {
                Windup = windup, Active = active, Recovery = recovery,
                ComboStart = windup + active * 0.5f, ComboEnd = windup + active + recovery * 0.65f,
                Damage = 8, MoveMultiplier = 0.8f, IsOffHand = isOffHand,
                WindupPose = BattleAnimationLibrary.Punch(new(0.44f, 1.56f, 0.1f), new(-0.02f, 1.57f, 0.47f), ahead, 1f, 30f),
                MidPose = BattleAnimationLibrary.Punch(new(0f, 1.68f, 0.5f), guard, ahead, 0f, -38f, 8f),
                EndPose = BattleAnimationLibrary.Punch(new(-0.34f, 1.6f, 0.44f), guard, ahead, 0f, -50f, 8f)
            };
        }

        private static WeaponDefinition CreateArmingSword()
        {
            return OneHandedSword(ArmingSword, "Arming Sword", 0.12f, 0.9f, 1.4f, 27, 0.4f, 0.2f, 0.5f);
        }

        private static WeaponDefinition CreateFalchion()
        {
            return OneHandedSword(Falchion, "Falchion", 0.12f, 0.95f, 1.45f, 33, 0.65f, 0.17f, 0.4f);
        }

        private static WeaponDefinition CreateMace()
        {
            WeaponDefinition mace = OneHandedSword(Mace, "Flanged Mace", 0.1f, 0.7f, 1.25f, 31, 0.55f, 0.18f, 0.5f);
            mace.Strike = MaceHead;
            mace.Attacks[2].Stagger = 0.3f;

            return mace;
        }

        private static WeaponDefinition CreateDagger()
        {
            WeaponDefinition dagger = OneHandedSword(Dagger, "Rondel Dagger", 0.06f, 0.42f, 1.05f, 16, 0.3f, 0.12f, 0.3f);
            dagger.Attacks[2].Damage = 16;
            dagger.Attacks[2].Stagger = 0f;

            return dagger;
        }

        private static WeaponDefinition CreateTorch()
        {
            WeaponDefinition torch = OneHandedSword(Torch, "Torch", 0.1f, 0.6f, 1.15f, 9, 0.4f, 0.16f, 0.45f);
            torch.CanBlock = false;
            torch.Attacks = new[] { torch.Attacks[0], torch.Attacks[1] };
            torch.Idle = BattleAnimationLibrary.OneHanded(new(0.26f, 1.28f, 0.38f), new(0.05f, 0.95f, 0.25f));

            return torch;
        }

        private static WeaponDefinition CreateMaceShield()
        {
            WeaponDefinition definition = BattleAnimationLibrary.CreateSwordShield();
            definition.Prefix = MaceShield;
            definition.DisplayName = "Mace & Shield";
            definition.BladeTip = 0.7f;
            definition.Strike = MaceHead;
            definition.Reach = 1.25f;

            foreach (AttackDefinition attack in definition.Attacks)
            {
                attack.Damage = 31;
                attack.Windup += 0.12f;
            }

            definition.Attacks[2].Stagger = 0.3f;

            return definition;
        }

        private static WeaponDefinition OneHandedSword(string prefix, string name, float bladeBase, float bladeTip, float reach, int damage,
            float windup, float active, float recovery)
        {
            BodyPose idle = BattleAnimationLibrary.OneHanded(new(0.22f, 1.2f, 0.38f), new(-0.15f, 0.75f, 0.64f));

            return new WeaponDefinition
            {
                Prefix = prefix,
                DisplayName = name,
                Kind = WeaponKind.OneHanded,
                Reach = reach,
                DeflectDuration = 0.6f,
                BladeBase = bladeBase,
                BladeTip = bladeTip,
                Idle = idle,
                Attacks = new[]
                {
                    new AttackDefinition
                    {
                        Windup = windup, Active = active, Recovery = recovery, ComboStart = windup + active * 0.5f, ComboEnd = windup + active + recovery * 0.8f,
                        Damage = damage, MoveMultiplier = 0.7f,
                        WindupPose = BattleAnimationLibrary.OneHanded(new(0.46f, 1.56f, 0.14f), new(0.85f, 0.45f, -0.25f), yaw: 25f),
                        MidPose = BattleAnimationLibrary.OneHanded(new(-0.06f, 1.44f, 0.52f), Vector3.forward),
                        EndPose = BattleAnimationLibrary.OneHanded(new(-0.2f, 1.24f, 0.34f), new(-0.4f, -0.1f, 0.92f), yaw: -22f)
                    },
                    new AttackDefinition
                    {
                        Windup = windup * 0.9f, Active = active, Recovery = recovery, ComboStart = windup * 0.9f + active * 0.5f, ComboEnd = windup * 0.9f + active + recovery * 0.8f,
                        Damage = damage, MoveMultiplier = 0.7f,
                        WindupPose = BattleAnimationLibrary.OneHanded(new(-0.1f, 1.45f, 0.28f), new(-0.85f, 0.3f, -0.3f), yaw: -18f),
                        MidPose = BattleAnimationLibrary.OneHanded(new(0.2f, 1.46f, 0.52f), Vector3.forward),
                        EndPose = BattleAnimationLibrary.OneHanded(new(0.5f, 1.36f, 0.2f), new(0.45f, 0.05f, 0.89f), yaw: 18f)
                    },
                    new AttackDefinition
                    {
                        Windup = windup * 1.1f, Active = active, Recovery = recovery * 1.2f, ComboStart = windup * 1.1f + active * 0.5f, ComboEnd = windup * 1.1f + active + recovery,
                        Damage = Mathf.RoundToInt(damage * 1.1f), MoveMultiplier = 0.6f, Stagger = 0.2f,
                        WindupPose = BattleAnimationLibrary.OneHanded(new(0.2f, 1.9f, 0.02f), new(0.08f, 0.6f, -0.8f), pitch: -10f),
                        MidPose = BattleAnimationLibrary.OneHanded(new(0.08f, 1.42f, 0.54f), Vector3.forward),
                        EndPose = BattleAnimationLibrary.OneHanded(new(0.1f, 1.14f, 0.38f), new(-0.03f, -0.17f, 0.98f), pitch: 12f)
                    }
                },
                CanBlock = true,
                BlockRaise = 0.2f,
                BlockMitigation = 0.7f,
                BlockImpact = 0.28f,
                BlockRecovery = 0.4f,
                BlockAngle = 80f,
                BlockMove = 0.6f,
                Block = BattleAnimationLibrary.OneHanded(new(0.16f, 1.42f, 0.4f), new(-0.9f, 0.3f, 0.1f), 10f),
                BlockHit = BattleAnimationLibrary.OneHanded(new(0.14f, 1.3f, 0.32f), new(-0.9f, 0.25f, -0.1f), 10f, -5f),
                BlockLowered = BattleAnimationLibrary.OneHanded(new(0.12f, 1.26f, 0.34f), new(-0.9f, 0.2f, 0f), 10f),
                DeflectPose = BattleAnimationLibrary.OneHanded(new(0.34f, 1.62f, 0.2f), new(0.3f, 0.85f, 0.3f), 12f, -6f),
                BlockSocket = WeaponSocket.RightHand,
                BlockBoxCenter = new Vector3(0f, 0f, (bladeBase + bladeTip) * 0.5f),
                BlockBoxExtents = new Vector3(0.08f, 0.08f, (bladeTip - bladeBase) * 0.5f)
            };
        }

        private static WeaponDefinition CreateLongsword()
        {
            WeaponDefinition definition = BattleAnimationLibrary.CreateGreatsword();
            definition.Prefix = Longsword;
            definition.DisplayName = "Longsword";
            definition.BladeTip = 1.05f;
            definition.Reach = 1.7f;
            definition.DeflectDuration = 0.65f;

            for (int i = 0; i < definition.Attacks.Length; i++)
            {
                AttackDefinition attack = definition.Attacks[i];
                attack.Windup *= 0.85f;
                attack.Recovery *= 0.8f;
                attack.ComboStart *= 0.85f;
                attack.ComboEnd *= 0.85f;
                attack.Damage = i == 2 ? 42 : 36;
                attack.MoveMultiplier = 0.6f;
            }

            definition.BlockMitigation = 0.85f;

            return definition;
        }

        /// The double axe of Dark and Darker. It is carried upright by the right shoulder, bits across the view, and
        /// swung in a series of three: a cut down from the right shoulder, a level cut back from the left and a chop
        /// over the head. Before each of them the axe is laid back over a shoulder, and each sets off from where the
        /// one before runs out to; a swing that is not followed up lifts the axe upright again on its way to rest.
        /// A blocked hit is answered with a chop that goes up over the head straight from the block. The off hand
        /// slides up the haft to the main one as the axe goes out to a side.
        private static WeaponDefinition CreateBattleAxe()
        {
            // Port arms: the haft across the chest, the head up past the right shoulder, the elbows hanging at the sides.
            BodyPose idle = PoseAt(new(0.05f, 1.25f, 0.3f), 70f, 50f, offHand: AxeGrip, elbow: new(0.2f, 1f, 0.05f), offElbow: new(-0.2f, 1.05f, 0.05f));
            idle.Edge = Vector3.left;
            // The haft upright on the left, the right forearm level across the chest.
            BodyPose upright = PoseAt(new(-0.25f, 1.3f, 0.3f), -30f, 85f, -25f, offHand: -0.3f, elbow: new(0.15f, 1.3f, 0.05f), offElbow: new(-0.25f, 1f, 0.05f));
            // Where a cut to the left stops: both arms straight down across to the left hip.
            BodyPose left = PoseAt(new(-0.25f, 1f, 0.4f), -90f, -10f, -45f, offHand: -0.25f, elbow: new(-0.03f, 1f, 0.3f), offElbow: new(-0.3f, 0.95f, 0f));
            BodyPose leftLow = PoseAt(new(-0.1f, 1f, 0.38f), -40f, -45f, -25f, 25f, -0.25f, new(0.05f, 1f, 0.3f), new(-0.25f, 0.95f, 0.05f));
            BodyPose right = PoseAt(new(0.28f, 1.05f, 0.28f), 120f, 0f, 45f, offHand: -0.22f, elbow: new(0.3f, 1.05f, 0.05f), offElbow: new(0.05f, 1.1f, 0.3f));

            // Each windup bunches the hands near the butt by the right ear, the right elbow out to the side below them.
            AttackDefinition cut = new AttackDefinition
            {
                Windup = 60 * Footage, Active = 9 * Footage, Recovery = 78 * Footage, Damage = 43, MoveMultiplier = 0.5f, Stagger = 0.3f, Launch = 1f,
                Raise = new()
                {
                    Via(6, PoseAt(new(0.15f, 1.35f, 0.35f), 90f, 30f, 15f, offHand: -0.35f, elbow: new(0.25f, 1.1f, 0.1f), offElbow: new(-0.1f, 1.15f, 0.25f))),
                    Via(12, PoseAt(new(0.15f, 1.4f, 0.35f), 90f, 0f, 20f, offHand: -0.35f, elbow: new(0.25f, 1.25f, 0.1f), offElbow: new(-0.05f, 1.2f, 0.3f))),
                    Via(18, PoseAt(new(0.3f, 1.55f, 0.05f), 160f, 20f, 30f, offHand: -0.15f, elbow: new(0.4f, 1.3f, 0.1f), offElbow: new(0f, 1.3f, 0.3f))),
                    Via(40, PoseAt(new(0.3f, 1.6f, 0f), 170f, 15f, 35f, offHand: -0.12f, elbow: new(0.4f, 1.3f, 0.1f), offElbow: new(0.05f, 1.3f, 0.3f)), 0.4f),
                    Via(54, PoseAt(new(0.3f, 1.62f, 0f), 170f, 25f, 38f, offHand: -0.12f, elbow: new(0.4f, 1.3f, 0.1f), offElbow: new(0.05f, 1.3f, 0.3f)), 0.5f),
                    Via(57, PoseAt(new(0.25f, 1.65f, 0.03f), 170f, 50f, 36f, offHand: -0.15f, elbow: new(0.4f, 1.38f, 0.08f), offElbow: new(0.05f, 1.35f, 0.25f)))
                },
                WindupPose = E(Axe(new(0.05f, 1.4f, 0.35f), new(0.82f, 0.52f, -0.22f), 10f, 0f, -0.25f), new(0.2f, 1.3f, 0.3f), new(-0.15f, 1.2f, 0.25f)),
                MidPose = E(Axe(new(-0.1f, 1.2f, 0.45f), Vector3.forward, -20f, 0f, -0.25f), new(0.05f, 1.1f, 0.3f), new(-0.25f, 1.05f, 0.2f)),
                EndPose = E(Axe(new(-0.22f, 1.03f, 0.42f), new(-0.8f, -0.05f, 0.6f), -40f, 8f, -0.25f), new(-0.03f, 1f, 0.3f), new(-0.3f, 0.95f, 0f)),
                Return = new()
                {
                    Via(78, left, 0.5f),
                    Via(90, PoseAt(new(-0.25f, 1.1f, 0.35f), -60f, 50f, -35f, offHand: -0.3f, elbow: new(0.05f, 1.2f, 0.3f), offElbow: new(-0.25f, 0.95f, 0.1f))),
                    Via(110, upright, 0.6f)
                }
            };
            AttackDefinition back = new AttackDefinition
            {
                Windup = 62 * Footage, Active = 9 * Footage, Recovery = 72 * Footage, Damage = 43, MoveMultiplier = 0.5f, Stagger = 0.3f, Launch = 1f,
                Raise = new()
                {
                    Via(12, PoseAt(new(-0.25f, 1.2f, 0.35f), -45f, 50f, -30f, offHand: -0.25f, elbow: new(0.1f, 1.2f, 0.3f), offElbow: new(-0.25f, 1f, 0.05f))),
                    Via(21, PoseAt(new(-0.25f, 1.35f, 0.3f), -20f, 80f, -25f, offHand: -0.25f, elbow: new(0.15f, 1.3f, 0.05f), offElbow: new(-0.25f, 1.05f, 0.05f))),
                    Via(36, PoseAt(new(-0.2f, 1.45f, 0.25f), -150f, 30f, -35f, offHand: -0.15f, elbow: new(0.35f, 1.4f, 0.15f), offElbow: new(-0.3f, 1.15f, -0.1f))),
                    Via(48, PoseAt(new(-0.05f, 1.45f, 0.3f), -120f, 0f, -40f, offHand: -0.15f, elbow: new(0.2f, 1.3f, 0.1f), offElbow: new(-0.3f, 1.25f, -0.1f)), 0.4f),
                    Via(58, PoseAt(new(-0.05f, 1.46f, 0.3f), -115f, 0f, -40f, offHand: -0.15f, elbow: new(0.2f, 1.3f, 0.1f), offElbow: new(-0.3f, 1.25f, -0.1f)), 0.5f)
                },
                WindupPose = E(Axe(new(0.05f, 1.45f, 0.45f), new(-0.99f, 0.05f, 0.08f), -15f, 0f, -0.15f), new(0.2f, 1.3f, 0.3f), new(-0.15f, 1.3f, 0.3f)),
                MidPose = E(Axe(new(0.15f, 1.45f, 0.5f), Vector3.forward, 10f, 0f, -0.15f), new(0.25f, 1.38f, 0.3f), new(-0.05f, 1.35f, 0.3f)),
                EndPose = E(Axe(new(0.3f, 1.4f, 0.32f), new(0.99f, 0.1f, 0.09f), 40f, 0f, -0.2f), new(0.35f, 1.3f, 0.3f), new(0.05f, 1.3f, 0.3f)),
                Return = new()
                {
                    Via(77, right, 0.5f),
                    Via(99, PoseAt(new(0.25f, 1.1f, 0.3f), 75f, 60f, 20f, offHand: -0.35f, elbow: new(0.25f, 1.05f, 0.05f), offElbow: new(-0.1f, 1f, 0.3f)), 0.6f)
                },
                After = cut
            };
            AttackDefinition chop = new AttackDefinition
            {
                Windup = 67 * Footage, Active = 9 * Footage, Recovery = 70 * Footage, Damage = 50, MoveMultiplier = 0.4f, Stagger = 0.45f, Launch = 1f,
                Raise = new()
                {
                    Via(4, PoseAt(new(0.3f, 1.05f, 0.25f), 110f, 20f, 35f, offHand: -0.24f, elbow: new(0.3f, 1.05f, 0.05f), offElbow: new(-0.05f, 1.05f, 0.3f)), 0.6f),
                    Via(15, PoseAt(new(0.25f, 1.2f, 0.3f), 80f, 60f, 20f, offHand: -0.3f, elbow: new(0.25f, 1.1f, 0.05f), offElbow: new(-0.05f, 1.1f, 0.3f))),
                    Via(30, PoseAt(new(0.2f, 1.55f, 0.15f), 160f, 30f, 25f, offHand: -0.15f, elbow: new(0.3f, 1.3f, 0.05f), offElbow: new(0f, 1.3f, 0.3f))),
                    Via(44, PoseAt(new(0.25f, 1.85f, 0f), 179f, 5f, 20f, offHand: -0.15f, elbow: new(0.4f, 1.5f, 0f), offElbow: new(0.05f, 1.45f, 0.3f)), 0.4f),
                    Via(60, PoseAt(new(0.25f, 1.85f, 0f), 179f, 10f, 20f, offHand: -0.15f, elbow: new(0.4f, 1.5f, 0f), offElbow: new(0.05f, 1.45f, 0.3f)), 0.5f),
                    Via(65, PoseAt(new(0.1f, 1.85f, 0.2f), 179f, 75f, 15f, offHand: -0.2f, elbow: new(0.25f, 1.55f, 0.15f), offElbow: new(0f, 1.5f, 0.35f)))
                },
                WindupPose = PoseAt(new(0.1f, 1.6f, 0.4f), 0f, 80f, 10f, offHand: -0.25f, elbow: new(0.2f, 1.4f, 0.3f), offElbow: new(-0.1f, 1.35f, 0.35f)),
                MidPose = E(Axe(new(0.05f, 1.3f, 0.5f), Vector3.forward, 0f, 0f, -0.25f), new(0.15f, 1.3f, 0.3f), new(-0.15f, 1.15f, 0.3f)),
                EndPose = PoseAt(new(0f, 1.02f, 0.38f), -30f, -50f, -20f, 25f, -0.25f, new(0.1f, 1f, 0.3f), new(-0.2f, 1f, 0.05f)),
                Return = new() { Via(88, leftLow, 0.5f), Via(112, upright, 0.6f), Via(136, idle, 0.6f) },
                After = back
            };
            // From the haft held level across the shoulders, up behind the head, then straight down the middle.
            AttackDefinition riposte = new AttackDefinition
            {
                Windup = 50 * Footage, Active = 7 * Footage, Recovery = 62 * Footage, Damage = 65, MoveMultiplier = 0.4f, Stagger = 0.45f, Launch = 1f,
                Raise = new()
                {
                    Via(10, PoseAt(new(0.3f, 1.55f, 0.2f), 90f, 10f, 10f, offHand: -0.6f, elbow: new(0.35f, 1.3f, 0.05f), offElbow: new(-0.35f, 1.15f, 0.05f)), 0.8f),
                    Via(18, PoseAt(new(0.28f, 1.7f, 0.05f), 120f, 30f, 10f, offHand: -0.5f, elbow: new(0.35f, 1.38f, 0.05f), offElbow: new(-0.3f, 1.3f, 0.05f))),
                    Via(30, PoseAt(new(0.2f, 1.8f, -0.05f), 160f, 30f, 15f, offHand: -0.3f, elbow: new(0.4f, 1.6f, -0.05f), offElbow: new(-0.35f, 1.55f, -0.1f)), 0.4f),
                    Via(44, PoseAt(new(0.2f, 1.8f, -0.05f), 160f, 30f, 15f, offHand: -0.3f, elbow: new(0.4f, 1.6f, -0.05f), offElbow: new(-0.35f, 1.55f, -0.1f)), 0.5f),
                    Via(48, PoseAt(new(0.05f, 1.9f, 0.15f), 0f, 89f, 0f, offHand: -0.25f, elbow: new(0.4f, 1.65f, 0.05f), offElbow: new(-0.15f, 1.4f, 0.3f)))
                },
                WindupPose = PoseAt(new(0.03f, 1.7f, 0.35f), 0f, 60f, offHand: -0.25f, elbow: new(0.2f, 1.3f, 0.3f), offElbow: new(-0.15f, 1.3f, 0.3f)),
                MidPose = E(Axe(new(0f, 1.4f, 0.5f), Vector3.forward, 0f, 0f, -0.25f), new(0.2f, 1.3f, 0.3f), new(-0.15f, 1.25f, 0.3f)),
                EndPose = PoseAt(new(0f, 1.05f, 0.42f), 0f, -45f, 0f, 28f, -0.25f, new(0.15f, 1.1f, 0.3f), new(-0.15f, 1.05f, 0.3f)),
                Return = new()
                {
                    Via(64, PoseAt(new(0f, 1f, 0.36f), 0f, -60f, 0f, 30f, -0.25f, new(0.1f, 1f, 0.3f), new(-0.15f, 0.95f, 0.3f)), 0.5f),
                    Via(80, PoseAt(new(-0.15f, 0.95f, 0.38f), -45f, -30f, -20f, 20f, -0.25f, new(0.1f, 1f, 0.3f), new(-0.25f, 0.95f, 0.05f)), 0.6f),
                    Via(95, upright, 0.6f)
                }
            };
            foreach (AttackDefinition attack in new[] { cut, back, chop, riposte })
            {
                attack.ComboStart = attack.Windup + attack.Active * 0.5f;
                attack.ComboEnd = attack.Windup + attack.Active + attack.Recovery * 0.65f;
            }

            return new WeaponDefinition
            {
                Prefix = BattleAxe,
                DisplayName = "Battle Axe",
                Kind = WeaponKind.TwoHanded,
                Reach = 1.65f,
                DeflectDuration = 0.9f,
                BladeBase = 0.66f,
                BladeTip = 0.94f,
                Strike = AxeHead,
                Idle = idle,
                Attacks = new[] { cut, back, chop },
                Riposte = riposte,
                CanBlock = true,
                BlockRaise = 0.25f,
                BlockMitigation = 0.7f,
                BlockImpact = 0.3f,
                BlockRecovery = 0.45f,
                BlockAngle = 80f,
                BlockMove = 0.5f,
                // The haft held level across the shoulders, hands wide, the head to the right: where the riposte sets off.
                Block = PoseAt(new(0.3f, 1.55f, 0.25f), 90f, 10f, 10f, offHand: -0.6f, elbow: new(0.35f, 1.3f, 0.05f), offElbow: new(-0.35f, 1.15f, 0.05f)),
                BlockHit = PoseAt(new(0.28f, 1.5f, 0.2f), 90f, 10f, 10f, -5f, -0.6f, new(0.35f, 1.27f, 0.03f), new(-0.35f, 1.12f, 0.03f)),
                BlockLowered = PoseAt(new(0.28f, 1.45f, 0.22f), 90f, 8f, 10f, offHand: -0.6f, elbow: new(0.35f, 1.25f, 0.03f), offElbow: new(-0.35f, 1.1f, 0.03f)),
                DeflectPose = Axe(new(0.24f, 1.62f, 0.22f), new(0.35f, 0.9f, 0.1f), 15f, -8f),
                BlockSocket = WeaponSocket.RightHand,
                BlockBoxCenter = new Vector3(0f, 0f, 0.25f),
                BlockBoxExtents = new Vector3(0.08f, 0.08f, 0.7f)
            };
        }

        /// The long one-handed axe of Dark and Darker: a bearded blade, a top spike and a back fluke on a slim haft held at the
        /// butt, the off hand free. Carried upright at the belly. The series is a chop straight over the head, then twice the
        /// same cut: the axe circles round the head (left, back, right) and comes down diagonally from the right. The riposte
        /// goes from the high cross guard round the head without a pause into that cut. Every swing runs out forward and low,
        /// rolls across the belly to the left and rises upright again.
        private static WeaponDefinition CreateHorsemansAxe()
        {
            BodyPose idle = PoseAt(new(0.25f, 1.05f, 0.35f), 10f, 80f, elbow: new(0.25f, 1.05f, 0.05f));
            // Where every cut stops, then the roll across the belly and the rise back to upright.
            BodyPose stop = PoseAt(new(0.07f, 1.02f, 0.45f), -20f, -15f, -10f, 12f, elbow: new(0.15f, 1.32f, 0.3f));
            BodyPose roll = PoseAt(new(0.15f, 1.05f, 0.5f), -40f, -5f, -5f, 10f, elbow: new(0.25f, 1.1f, 0.2f));
            BodyPose rise = PoseAt(new(0.2f, 1.1f, 0.45f), -35f, 45f, 0f, 5f, elbow: new(0.25f, 1.05f, 0.1f));
            BodyPose guard = PoseAt(new(0.2f, 1.62f, 0.38f), -45f, -25f, -10f, -15f, elbow: new(0.45f, 1.52f, 0.2f));
            BodyPose peak = PoseAt(new(0.08f, 1.44f, 0.52f), 0f, 0f, -10f, 12f, elbow: new(0.2f, 1.62f, 0.3f));
            // The riposte runs out a little higher than the series.
            BodyPose high = PoseAt(new(0.05f, 1.18f, 0.5f), -5f, 0f, -10f, 15f, elbow: new(0.15f, 1.48f, 0.35f));
            BodyPose end = PoseAt(new(0.04f, 1.1f, 0.5f), -12f, -5f, -10f, 15f, elbow: new(0.15f, 1.42f, 0.35f));

            AttackDefinition chop = new AttackDefinition
            {
                Windup = 36 * Footage, Active = 8 * Footage, Recovery = 28 * Footage, Damage = 36, MoveMultiplier = 0.6f, Launch = 1f,
                Raise = new()
                {
                    Via(3, PoseAt(new(0.25f, 1.35f, 0.25f), 180f, 60f, 5f, elbow: new(0.35f, 1.25f, 0f))),
                    Via(12, PoseAt(new(0.18f, 1.75f, 0.1f), 180f, 20f, 10f, -5f, elbow: new(0.35f, 1.5f, 0f)), 0.4f),
                    Via(33, PoseAt(new(0.15f, 1.8f, 0.05f), 175f, 10f, 10f, -5f, elbow: new(0.35f, 1.5f, 0f)), 0.5f),
                    Via(35, PoseAt(new(0.15f, 1.85f, 0.15f), 20f, 95f, 5f, elbow: new(0.25f, 1.75f, -0.12f)))
                },
                WindupPose = PoseAt(new(0.15f, 1.8f, 0.3f), 20f, 70f, 0f, 5f, elbow: new(0.22f, 1.8f, 0f)),
                MidPose = peak,
                EndPose = end,
                Return = new() { Via(48, stop, 0.5f), Via(54, roll), Via(63, rise) }
            };
            AttackDefinition circle = Circle(chop, 9, 36);
            AttackDefinition again = Circle(circle, 8, 36);
            again.Stagger = 0.2f;

            // From the guard the axe goes up level over the head pointing left, round behind it and out to the right.
            AttackDefinition riposte = new AttackDefinition
            {
                Windup = 27 * Footage, Active = 9 * Footage, Recovery = 36 * Footage, Damage = 54, MoveMultiplier = 0.5f, Stagger = 0.3f, Launch = 1f,
                Raise = new()
                {
                    Via(4, PoseAt(new(0.22f, 1.72f, 0.32f), -60f, -10f, -5f, -10f, elbow: new(0.45f, 1.52f, 0.12f))),
                    Via(8, PoseAt(new(0.25f, 1.85f, 0.2f), -80f, 0f, 0f, -5f, elbow: new(0.4f, 1.55f, 0.05f))),
                    Via(12, PoseAt(new(0.22f, 1.85f, 0.15f), -110f, 5f, 5f, -5f, elbow: new(0.38f, 1.55f, 0.05f))),
                    Via(16, PoseAt(new(0.2f, 1.85f, 0.1f), -150f, 10f, 10f, -5f, elbow: new(0.35f, 1.55f, 0f))),
                    Via(22, PoseAt(new(0.22f, 1.85f, 0.1f), 175f, 30f, 10f, -5f, elbow: new(0.38f, 1.55f, 0f))),
                    Via(25, PoseAt(new(0.28f, 1.8f, 0.2f), 130f, 80f, 5f, elbow: new(0.4f, 1.5f, 0.05f)))
                },
                WindupPose = PoseAt(new(0.3f, 1.7f, 0.3f), 100f, 70f, 0f, 5f, elbow: new(0.38f, 1.45f, 0.1f)),
                MidPose = peak,
                EndPose = high,
                Return = new() { Via(42, stop, 0.5f), Via(54, roll), Via(64, rise) }
            };

            foreach (AttackDefinition attack in new[] { chop, circle, again, riposte })
            {
                attack.ComboStart = attack.Windup + attack.Active * 0.5f;
                attack.ComboEnd = attack.Windup + attack.Active + attack.Recovery * 0.65f;
            }

            return new WeaponDefinition
            {
                Prefix = HorsemansAxe,
                DisplayName = "Horseman's Axe",
                Kind = WeaponKind.OneHanded,
                Reach = 1.5f,
                DeflectDuration = 0.65f,
                BladeBase = 0.55f,
                BladeTip = 0.92f,
                Strike = HorsemanHead,
                Idle = idle,
                Attacks = new[] { chop, circle, again },
                Riposte = riposte,
                CanBlock = true,
                BlockRaise = 0.2f,
                BlockMitigation = 0.7f,
                BlockImpact = 0.28f,
                BlockRecovery = 0.4f,
                BlockAngle = 80f,
                BlockMove = 0.6f,
                // The high cross guard: the hand by the right temple, the haft slanting down across the face.
                Block = guard,
                BlockHit = PoseAt(new(0.19f, 1.56f, 0.33f), -45f, -25f, -10f, -18f, elbow: new(0.45f, 1.47f, 0.16f)),
                BlockLowered = PoseAt(new(0.2f, 1.54f, 0.36f), -45f, -30f, -10f, -10f, elbow: new(0.45f, 1.45f, 0.17f)),
                DeflectPose = PoseAt(new(0.3f, 1.6f, 0.3f), 20f, 80f, 12f, -6f, elbow: new(0.4f, 1.35f, 0.1f)),
                BlockSocket = WeaponSocket.RightHand,
                BlockBoxCenter = new Vector3(0f, 0f, 0.4f),
                BlockBoxExtents = new Vector3(0.06f, 0.06f, 0.52f)
            };

            // The cut of the second and the third swing: up from the last one's stop, round the head and down from the right.
            AttackDefinition Circle(AttackDefinition after, int active, int damage)
            {
                return new AttackDefinition
                {
                    Windup = 41 * Footage, Active = active * Footage, Recovery = 30 * Footage, Damage = damage, MoveMultiplier = 0.6f, Launch = 1f,
                    Raise = new()
                    {
                        Via(3, PoseAt(new(0.06f, 1.06f, 0.46f), -15f, -10f, -10f, 12f, elbow: new(0.15f, 1.35f, 0.3f))),
                        Via(9, PoseAt(new(0f, 1.25f, 0.45f), -40f, 60f, -5f, 5f, elbow: new(0.2f, 1.1f, 0.2f))),
                        Via(15, PoseAt(new(0.2f, 1.55f, 0.3f), -120f, 45f, 0f, elbow: new(0.35f, 1.35f, 0.1f))),
                        Via(19, PoseAt(new(0.2f, 1.8f, 0.15f), -150f, 15f, 5f, -5f, elbow: new(0.35f, 1.5f, 0.05f))),
                        Via(25, PoseAt(new(0.18f, 1.85f, 0.1f), 180f, 5f, 10f, -5f, elbow: new(0.35f, 1.55f, 0f)), 0.4f),
                        Via(35, PoseAt(new(0.25f, 1.85f, 0.1f), 160f, 30f, 10f, -5f, elbow: new(0.4f, 1.55f, 0f)), 0.5f),
                        Via(38, PoseAt(new(0.28f, 1.8f, 0.2f), 130f, 80f, 5f, elbow: new(0.4f, 1.5f, 0.05f)))
                    },
                    WindupPose = PoseAt(new(0.3f, 1.68f, 0.3f), 100f, 65f, 0f, 5f, elbow: new(0.38f, 1.45f, 0.1f)),
                    MidPose = peak,
                    EndPose = end,
                    Return = new() { Via(58, stop, 0.5f), Via(64, roll), Via(71, rise) },
                    After = after
                };
            }
        }

        /// A pose a swing passes at a frame of the footage.
        private static PoseKey Via(float frame, BodyPose pose, float slope = 1f)
        {
            return PoseKey.Flow(frame * Footage, pose, slope);
        }

        private static BodyPose E(BodyPose pose, Vector3 elbow, Vector3 offElbow)
        {
            return BattleAnimationLibrary.Elbows(pose, elbow, offElbow);
        }

        private static BodyPose Axe(Vector3 grip, Vector3 blade, float yaw = 0f, float pitch = 0f, float offHand = AxeGrip)
        {
            return BattleAnimationLibrary.TwoHanded(grip, blade, offHand, yaw, pitch);
        }

        private static WeaponDefinition CreateSpear()
        {
            BodyPose idle = BattleAnimationLibrary.TwoHanded(new(0.26f, 1.2f, 0.2f), new(-0.08f, 0.15f, 0.98f), -0.42f, 20f);

            return new WeaponDefinition
            {
                Prefix = Spear,
                DisplayName = "Spear",
                Kind = WeaponKind.TwoHanded,
                Reach = 2.4f,
                DeflectDuration = 0.7f,
                BladeBase = 1.5f,
                BladeTip = 2.0f,
                Strike = SpearHead,
                Idle = idle,
                Attacks = new[]
                {
                    Thrust(34, 0.7f, 0.18f, 0.5f, 1.38f),
                    Thrust(34, 0.65f, 0.18f, 0.5f, 1.48f),
                    Thrust(38, 0.8f, 0.2f, 0.6f, 1.3f, 0.25f)
                },
                CanBlock = true,
                BlockRaise = 0.25f,
                BlockMitigation = 0.65f,
                BlockImpact = 0.3f,
                BlockRecovery = 0.45f,
                BlockAngle = 75f,
                BlockMove = 0.55f,
                Block = BattleAnimationLibrary.TwoHanded(new(0.35f, 1.4f, 0.4f), new(-0.95f, 0.25f, 0.1f), 0.5f, 8f),
                BlockHit = BattleAnimationLibrary.TwoHanded(new(0.33f, 1.28f, 0.32f), new(-0.95f, 0.2f, -0.1f), 0.5f, 8f, -5f),
                BlockLowered = BattleAnimationLibrary.TwoHanded(new(0.33f, 1.24f, 0.34f), new(-0.95f, 0.15f, 0f), 0.5f, 8f),
                DeflectPose = BattleAnimationLibrary.TwoHanded(new(0.3f, 1.5f, 0.1f), new(-0.2f, 0.8f, 0.5f), -0.3f, 18f, -8f),
                BlockSocket = WeaponSocket.RightHand,
                BlockBoxCenter = new Vector3(0f, 0f, 0.6f),
                BlockBoxExtents = new Vector3(0.07f, 0.07f, 0.7f)
            };
        }

        private static AttackDefinition Thrust(int damage, float windup, float active, float recovery, float height, float stagger = 0f)
        {
            return new AttackDefinition
            {
                Windup = windup, Active = active, Recovery = recovery, ComboStart = windup + active * 0.5f, ComboEnd = windup + active + recovery * 0.85f,
                Damage = damage, MoveMultiplier = 0.6f, Stagger = stagger,
                WindupPose = Lunge(new(0.3f, height, 0.06f), 28f, -0.3f),
                MidPose = Lunge(new(0.2f, height + 0.05f, 0.38f), 5f, -0.45f),
                EndPose = Lunge(new(0.14f, height + 0.07f, 0.5f), -8f, -0.5f)
            };
        }

        /// The shaft points at the crosshair all the way, so the head travels along the aim line. It slides through the
        /// rear hand, which cannot follow the leading one that far.
        private static BodyPose Lunge(Vector3 grip, float yaw, float offHand)
        {
            return BattleAnimationLibrary.TwoHanded(grip, BattleAnimationLibrary.Aim(grip, SpearHead), offHand, yaw);
        }

        private static WeaponDefinition CreateStaff()
        {
            // Carried like a walking stick in the right hand, a third of the way down from the head; only the riposte
            // takes it in both hands and strikes with the butt.
            BodyPose idle = PoseAt(new(0.25f, 1.18f, 0.38f), 0f, 80f, elbow: new(0.2f, 1f, 0.05f));
            BodyPose parry = PoseAt(new(0.2f, 1.45f, 0.38f), 75f, 40f, offHand: StaffHands, elbow: new(0.25f, 1.2f, 0.05f), offElbow: new(-0.2f, 1.05f, 0.05f));

            AttackDefinition backhand = new AttackDefinition
            {
                Windup = 34 * Footage, Active = 7 * Footage, Recovery = 55 * Footage, Damage = 29, MoveMultiplier = 0.5f, Stagger = 0.25f, Launch = 1f,
                Raise = new()
                {
                    Via(8, PoseAt(new(0.05f, 1.3f, 0.3f), -40f, 70f, -10f, elbow: new(0.2f, 1.15f, 0f))),
                    Via(14, PoseAt(new(-0.1f, 1.38f, 0.12f), -150f, 30f, -20f, elbow: new(0.15f, 1.25f, 0.1f))),
                    Via(20, PoseAt(new(-0.15f, 1.45f, 0.06f), -160f, 15f, -25f, offHand: 0.12f, elbow: new(0.1f, 1.4f, 0.28f), offElbow: new(-0.2f, 1.3f, 0.12f)), 0.4f),
                    Via(30, PoseAt(new(-0.16f, 1.46f, 0.05f), -165f, 15f, -25f, offHand: 0.12f, elbow: new(0.1f, 1.4f, 0.28f), offElbow: new(-0.2f, 1.3f, 0.12f)), 0.5f)
                },
                WindupPose = PoseAt(new(-0.1f, 1.5f, 0.35f), -100f, 75f, -15f, elbow: new(0.15f, 1.45f, 0.25f)),
                MidPose = BattleAnimationLibrary.Elbows(BattleAnimationLibrary.OneHanded(new(0.1f, 1.44f, 0.6f), Vector3.forward, 10f), new(0.15f, 1.45f, 0.3f)),
                EndPose = PoseAt(new(0.42f, 1.42f, 0.55f), -10f, 15f, 15f, elbow: new(0.35f, 1.45f, 0.25f)),
                Return = new()
                {
                    Via(44, PoseAt(new(0.45f, 1.43f, 0.55f), 0f, 10f, 15f, elbow: new(0.35f, 1.45f, 0.25f)), 0.5f), Via(70, PoseAt(new(0.45f, 1.43f, 0.53f), 0f, 12f, 10f, elbow: new(0.35f, 1.45f, 0.25f)), 0.5f),
                    Via(80, PoseAt(new(0.38f, 1.3f, 0.48f), 0f, 20f, 8f, elbow: new(0.28f, 1.1f, 0.1f))), Via(88, PoseAt(new(0.36f, 1.24f, 0.42f), 0f, 55f, 5f, elbow: new(0.24f, 1.03f, 0.06f)))
                }
            };
            AttackDefinition chop = new AttackDefinition
            {
                Windup = 55 * Footage, Active = 9 * Footage, Recovery = 58 * Footage, Damage = 32, MoveMultiplier = 0.5f, Stagger = 0.25f, Launch = 1f,
                Raise = new()
                {
                    Via(10, PoseAt(new(0.42f, 1.62f, 0.3f), 0f, 15f, 5f, elbow: new(0.38f, 1.45f, 0.15f))),
                    Via(18, PoseAt(new(0.22f, 1.95f, 0.12f), -60f, 5f, elbow: new(0.4f, 1.65f, 0.12f))),
                    Via(25, PoseAt(new(0.2f, 2f, 0.1f), -80f, -10f, elbow: new(0.45f, 1.68f, 0.2f)), 0.4f),
                    Via(50, PoseAt(new(0.2f, 2f, 0.09f), -75f, -15f, elbow: new(0.45f, 1.68f, 0.2f)), 0.5f),
                    Via(52.5f, PoseAt(new(0.2f, 1.98f, 0.12f), -75f, 30f, elbow: new(0.42f, 1.65f, 0.2f))), Via(54, PoseAt(new(0.16f, 1.82f, 0.26f), -50f, 70f, elbow: new(0.3f, 1.55f, 0.22f)))
                },
                WindupPose = PoseAt(new(0.12f, 1.62f, 0.42f), 0f, 80f, elbow: new(0.15f, 1.5f, 0.25f)),
                MidPose = BattleAnimationLibrary.Elbows(BattleAnimationLibrary.OneHanded(new(0.12f, 1.44f, 0.5f), Vector3.forward), new(0.15f, 1.32f, 0.25f)),
                EndPose = PoseAt(new(0.04f, 1.18f, 0.55f), 0f, 25f, elbow: new(0.1f, 1.2f, 0.25f)),
                Return = new()
                {
                    Via(68, PoseAt(new(0.03f, 1.18f, 0.55f), -10f, 15f, elbow: new(0.1f, 1.2f, 0.25f)), 0.5f), Via(95, PoseAt(new(0.04f, 1.19f, 0.53f), -15f, 18f, elbow: new(0.1f, 1.2f, 0.25f)), 0.5f),
                    Via(108, PoseAt(new(0.25f, 1.2f, 0.38f), 0f, 60f, elbow: new(0.15f, 1f, 0.05f)))
                },
                After = backhand
            };
            // The butt sweeps from the left through the crosshair to the right, driven by the torso turning to the right.
            AttackDefinition riposte = new AttackDefinition
            {
                Windup = 22 * Footage, Active = 12 * Footage, Recovery = 47 * Footage, Damage = 44, MoveMultiplier = 0.4f, Stagger = 0.4f, Launch = 1f,
                Strike = StaffButt,
                Raise = new()
                {
                    Via(8, PoseAt(new(0.22f, 1.42f, 0.38f), 80f, 15f, -10f, offHand: StaffHands, elbow: new(0.25f, 1.2f, 0.05f), offElbow: new(-0.2f, 1.1f, 0.05f))),
                    Via(16, PoseAt(new(0.2f, 1.55f, 0.38f), 90f, 0f, -20f, offHand: StaffHands, elbow: new(0.25f, 1.25f, 0.05f), offElbow: new(-0.2f, 1.25f, 0.05f)), 0.4f)
                },
                WindupPose = PoseAt(new(0.18f, 1.48f, 0.38f), 100f, -5f, -5f, offHand: StaffHands, elbow: new(0.25f, 1.05f, 0f), offElbow: new(-0.15f, 1.1f, 0.1f)),
                MidPose = BattleAnimationLibrary.Elbows(BattleAnimationLibrary.TwoHanded(new(0.12f, 1.05f, 0.15f), Vector3.back, StaffHands, 50f), new(0.25f, 1f, -0.1f), new(-0.1f, 1.05f, 0.25f)),
                EndPose = PoseAt(new(0.1f, 0.98f, -0.04f), -155f, -10f, 70f, offHand: StaffReach, elbow: new(0.15f, 1f, -0.2f), offElbow: new(-0.08f, 1.05f, 0.15f)),
                Return = new()
                {
                    Via(38, PoseAt(new(0.1f, 0.98f, -0.05f), -165f, -5f, 75f, offHand: StaffReach, elbow: new(0.15f, 1f, -0.22f), offElbow: new(-0.08f, 1.05f, 0.15f)), 0.5f),
                    Via(44, PoseAt(new(0.1f, 0.98f, -0.05f), -170f, -5f, 70f, offHand: StaffReach, elbow: new(0.15f, 1f, -0.22f), offElbow: new(-0.08f, 1.05f, 0.15f)), 0.5f),
                    Via(52, PoseAt(new(0.12f, 1.1f, 0.25f), 150f, 30f, 45f, elbow: new(0.25f, 1f, 0.05f))),
                    Via(64, PoseAt(new(0.18f, 1.15f, 0.33f), 160f, 50f, 30f, elbow: new(0.22f, 1.02f, 0.05f))), Via(72, PoseAt(new(0.25f, 1.2f, 0.38f), 20f, 75f, 10f, elbow: new(0.22f, 1.02f, 0.05f)))
                }
            };
            foreach (AttackDefinition attack in new[] { backhand, chop, riposte })
            {
                attack.ComboStart = attack.Windup + attack.Active * 0.5f;
                attack.ComboEnd = attack.Windup + attack.Active + attack.Recovery * 0.65f;
            }

            return new WeaponDefinition
            {
                Prefix = Staff,
                DisplayName = "Magic Staff",
                Kind = WeaponKind.TwoHanded,
                IsRound = true,
                Reach = 1.3f,
                DeflectDuration = 0.8f,
                BladeBase = StaffButtEnd,
                BladeTip = 0.4f,
                Strike = 0.3f,
                Idle = idle,
                Attacks = new[] { backhand, chop },
                Riposte = riposte,
                CanBlock = true,
                BlockRaise = 0.25f,
                BlockMitigation = 0.6f,
                BlockImpact = 0.3f,
                BlockRecovery = 0.45f,
                BlockAngle = 80f,
                BlockMove = 0.5f,
                // The parry hold the riposte sets off from: across the chest, head up to the right.
                Block = parry,
                BlockHit = PoseAt(new(0.19f, 1.4f, 0.33f), 75f, 40f, pitch: -5f, offHand: StaffHands, elbow: new(0.25f, 1.15f, 0.03f), offElbow: new(-0.2f, 1f, 0.03f)),
                BlockLowered = PoseAt(new(0.2f, 1.35f, 0.36f), 75f, 30f, offHand: StaffHands, elbow: new(0.25f, 1.12f, 0.03f), offElbow: new(-0.2f, 1f, 0.03f)),
                DeflectPose = PoseAt(new(0.3f, 1.55f, 0.15f), 20f, 75f, 15f, -8f),
                BlockSocket = WeaponSocket.RightHand,
                BlockBoxCenter = new Vector3(0f, 0f, (StaffButtEnd + 0.4f) * 0.5f),
                BlockBoxExtents = new Vector3(0.05f, 0.05f, (0.4f - StaffButtEnd) * 0.5f)
            };
        }

        /// The weapon's head direction as yaw (+ to the right) and elevation in degrees; no off hand keeps it one-handed.
        private static BodyPose PoseAt(Vector3 grip, float yaw, float elevation, float torso = 0f, float pitch = 0f, float offHand = 0f,
            Vector3 elbow = default, Vector3 offElbow = default)
        {
            Vector3 blade = Quaternion.Euler(-elevation, yaw, 0f) * Vector3.forward;
            BodyPose pose = offHand == 0f
                ? BattleAnimationLibrary.OneHanded(grip, blade, torso, pitch)
                : BattleAnimationLibrary.TwoHanded(grip, blade, offHand, torso, pitch);

            return BattleAnimationLibrary.Elbows(pose, elbow, offElbow);
        }

        private static WeaponDefinition CreateCrossbow()
        {
            WeaponDefinition definition = BattleAnimationLibrary.CreateBow();
            definition.Prefix = Crossbow;
            definition.DisplayName = "Crossbow";
            definition.Idle = BattleAnimationLibrary.TwoHanded(new(0.2f, 1.25f, 0.4f), new(0f, 0.15f, 1f), -0.28f, 10f);
            definition.DrawPose = BattleAnimationLibrary.TwoHanded(new(0.16f, 1.52f, 0.36f), new(0f, 0.02f, 1f), -0.3f, 4f);
            definition.ReleasePose = BattleAnimationLibrary.TwoHanded(new(0.16f, 1.5f, 0.3f), new(0f, 0.08f, 1f), -0.3f, 4f);
            definition.DrawTime = 0.3f;
            definition.ReloadTime = 1.8f;
            definition.ArrowMinSpeed = 36f;
            definition.ArrowMaxSpeed = 40f;
            definition.ArrowMinDamage = 30;
            definition.ArrowMaxDamage = 39;

            return definition;
        }
    }
}
