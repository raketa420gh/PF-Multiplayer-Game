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

        private const float MaceHead = 0.6f;
        private const float AxeHead = 0.8f;
        /// A long axe is held at the butt with the off hand, this far down the haft from the main hand.
        private const float AxeGrip = -0.42f;
        private const float Frame = 1f / BattleAnimationBuilder.FrameRate;
        private const float SpearHead = 1.8f;
        private const float StaffGrip = -0.33f;

        /// Catalog order = combat catalog index. Battle prefab slots 1-4 map to the first four entries; monsters refer to
        /// their weapon by index, so new entries go to the end.
        public static readonly string[] CatalogOrder =
        {
            SwordShield, Greatsword, Bow, SwordShieldLeft, Fists, ArmingSword, Falchion, Longsword, BattleAxe, Spear, Mace, Dagger, Crossbow, Staff, Torch, MaceShield,
            Spellbook, Lute, BearClaws, PantherClaws, RatBite,
            ShortSword, Rapier, VikingSword, Hatchet, MorningStar, CastillonDagger, Stiletto, FellingAxe, WarMaul, Halberd
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
            ["War Maul"] = (9, 5), ["Bow"] = (3, 1), ["Crossbow"] = (5, 1), ["Panther Claws"] = (4, 2), ["Bear Claws"] = (7, 4)
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
                CreateHalberd()
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

        private static WeaponDefinition CreateFists()
        {
            // A fist is a short blade that runs across it, from the palm to the thumb: held upright, it is carried forward
            // by the forearm, knuckles first.
            BodyPose idle = BattleAnimationLibrary.OneHanded(new(0.2f, 1.32f, 0.3f), new(-0.3f, 0.9f, 0.3f));

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
                Attacks = new[]
                {
                    new AttackDefinition
                    {
                        Windup = 0.22f, Active = 0.12f, Recovery = 0.3f, ComboStart = 0.3f, ComboEnd = 0.6f,
                        Damage = 8, MoveMultiplier = 0.8f,
                        WindupPose = BattleAnimationLibrary.OneHanded(new(0.26f, 1.42f, 0.14f), new(-0.2f, 0.95f, 0.2f), yaw: 16f),
                        MidPose = BattleAnimationLibrary.OneHanded(new(0f, 1.68f, 0.54f), Vector3.up),
                        EndPose = BattleAnimationLibrary.OneHanded(new(-0.02f, 1.69f, 0.6f), new(-0.1f, 0.98f, 0.15f), yaw: -12f)
                    },
                    new AttackDefinition
                    {
                        Windup = 0.22f, Active = 0.12f, Recovery = 0.3f, ComboStart = 0.3f, ComboEnd = 0.6f,
                        Damage = 8, MoveMultiplier = 0.8f,
                        WindupPose = BattleAnimationLibrary.OneHanded(new(0.4f, 1.5f, 0.2f), new(0f, 1f, 0.1f), yaw: 22f),
                        MidPose = BattleAnimationLibrary.OneHanded(new(0.04f, 1.68f, 0.54f), Vector3.up),
                        EndPose = BattleAnimationLibrary.OneHanded(new(-0.14f, 1.62f, 0.42f), new(-0.2f, 0.95f, 0.2f), yaw: -20f)
                    }
                },
                CanBlock = true,
                BlockRaise = 0.15f,
                BlockMitigation = 0.4f,
                BlockImpact = 0.25f,
                BlockRecovery = 0.3f,
                BlockAngle = 80f,
                BlockMove = 0.7f,
                Block = BattleAnimationLibrary.Cast(0.18f, 0.36f, 1.5f),
                BlockHit = BattleAnimationLibrary.Cast(0.2f, 0.3f, 1.44f, -4f),
                BlockLowered = BattleAnimationLibrary.Cast(0.2f, 0.3f, 1.36f),
                DeflectPose = idle,
                BlockSocket = WeaponSocket.LeftHand,
                BlockBoxCenter = Vector3.zero,
                BlockBoxExtents = new Vector3(0.14f, 0.14f, 0.08f)
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
        /// over the head. Each swing sets off from where the one before ends, and the off hand slides up the haft to
        /// the main one as a cut runs out to the side.
        private static WeaponDefinition CreateBattleAxe()
        {
            BodyPose idle = Axe(new(0.1f, 1.46f, 0.5f), new(0.52f, 0.82f, 0.25f), 8f);
            idle.Edge = Vector3.left;

            AttackDefinition cut = new AttackDefinition
            {
                Windup = 59 * Frame, Active = 12 * Frame, Recovery = 0.9f, Damage = 43, MoveMultiplier = 0.5f, Stagger = 0.3f,
                WindupPose = Axe(new(0.4f, 1.83f, 0.235f), new(0.75f, 0.42f, -0.5f), 28f),
                MidPose = Axe(new(-0.14f, 1.44f, 0.5f), Vector3.forward, -10f),
                EndPose = Axe(new(-0.3f, 1.24f, 0.4f), new(-0.55f, 0f, 0.83f), -30f, 8f, -0.3f)
            };
            AttackDefinition back = new AttackDefinition
            {
                Windup = 71 * Frame, Active = 16 * Frame, Recovery = 0.9f, Damage = 43, MoveMultiplier = 0.5f, Stagger = 0.3f,
                WindupPose = Axe(new(-0.34f, 1.3f, 0.14f), new(-0.95f, -0.2f, -0.2f), -45f, 6f),
                MidPose = Axe(new(0.22f, 1.5f, 0.5f), Vector3.forward, 10f),
                EndPose = Axe(new(0.55f, 1.48f, 0.22f), new(0.2f, 0.12f, 0.97f), 60f, 0f, -0.13f),
                After = cut
            };
            AttackDefinition chop = new AttackDefinition
            {
                Windup = 81 * Frame, Active = 12 * Frame, Recovery = 1f, Damage = 50, MoveMultiplier = 0.4f, Stagger = 0.45f,
                WindupPose = Axe(new(0.22f, 1.86f, 0.1f), new(0.25f, 0.5f, -0.83f), 12f, -12f),
                MidPose = Axe(new(0.03f, 1.42f, 0.5f), Vector3.forward),
                EndPose = Axe(new(0f, 1.12f, 0.38f), new(-0.05f, -0.3f, 0.95f), -4f, 14f),
                After = back
            };
            AttackDefinition[] attacks = { cut, back, chop };

            foreach (AttackDefinition attack in attacks)
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
                Attacks = attacks,
                CanBlock = true,
                BlockRaise = 0.25f,
                BlockMitigation = 0.7f,
                BlockImpact = 0.3f,
                BlockRecovery = 0.45f,
                BlockAngle = 80f,
                BlockMove = 0.5f,
                // The haft is held out across the view, the head up by the right shoulder.
                Block = Axe(new(0.14f, 1.84f, 0.39f), new(0.84f, 0.5f, -0.2f), pitch: -4f),
                BlockHit = Axe(new(0.13f, 1.76f, 0.32f), new(0.86f, 0.45f, -0.25f), pitch: -7f),
                BlockLowered = Axe(new(0.13f, 1.7f, 0.34f), new(0.86f, 0.42f, -0.2f), pitch: -4f),
                DeflectPose = Axe(new(0.24f, 1.62f, 0.22f), new(0.35f, 0.9f, 0.1f), 15f, -8f),
                BlockSocket = WeaponSocket.RightHand,
                BlockBoxCenter = new Vector3(0f, 0f, 0.25f),
                BlockBoxExtents = new Vector3(0.08f, 0.08f, 0.7f)
            };
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
            WeaponDefinition definition = BattleAnimationLibrary.CreateGreatsword();
            definition.Prefix = Staff;
            definition.DisplayName = "Magic Staff";
            definition.BladeBase = 0.3f;
            definition.BladeTip = 1.3f;
            definition.Reach = 1.8f;
            definition.Idle = BattleAnimationLibrary.TwoHanded(new(0.2f, 1.3f, 0.3f), new(0.3f, 0.9f, 0.3f), StaffGrip, 15f);
            definition.Attacks = new[] { definition.Attacks[0], definition.Attacks[1] };

            // A staff is swung with the hands as far apart as it is carried.
            Spread(ref definition.DeflectPose);

            foreach (AttackDefinition attack in definition.Attacks)
            {
                Spread(ref attack.WindupPose);
                Spread(ref attack.MidPose);
                Spread(ref attack.EndPose);
            }
            definition.Attacks[0].Damage = 29;
            definition.Attacks[1].Damage = 32;
            definition.BlockMitigation = 0.6f;
            definition.BlockBoxCenter = new Vector3(0f, 0f, 0.6f);
            definition.BlockBoxExtents = new Vector3(0.06f, 0.06f, 0.6f);

            return definition;
        }

        private static void Spread(ref BodyPose pose)
        {
            pose.Off.Position = pose.Main.Position + pose.Main.Forward * StaffGrip;
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
