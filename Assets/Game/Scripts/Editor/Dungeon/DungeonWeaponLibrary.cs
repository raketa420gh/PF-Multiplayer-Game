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

        /// Catalog order = combat catalog index. Battle prefab slots 1-4 map to the first four entries.
        public static readonly string[] CatalogOrder =
        {
            SwordShield, Greatsword, Bow, SwordShieldLeft, Fists, ArmingSword, Falchion, Longsword, BattleAxe, Spear, Mace, Dagger, Crossbow, Staff, Torch, MaceShield,
            Spellbook, Lute, BearClaws, PantherClaws, RatBite
        };

        /// Catalog entries that reuse another definition's animation clips (same prefix) with their own damage.
        public static string SharedPrefix(string name)
        {
            return name switch
            {
                Spellbook or Lute or RatBite => Fists,
                SwordShieldLeft => SwordShield,
                _ => name
            };
        }

        public static WeaponDefinition[] CreateAll()
        {
            return new[]
            {
                BattleAnimationLibrary.CreateSwordShield(),
                BattleAnimationLibrary.CreateGreatsword(),
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
                CreateRatBite()
            };
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
            BodyPose idle = BattleAnimationLibrary.OneHanded(new(0.2f, 1.3f, 0.3f), new(-0.3f, 0.3f, 0.9f));

            return new WeaponDefinition
            {
                Prefix = Fists,
                DisplayName = "Bare Hands",
                Kind = WeaponKind.OneHanded,
                Reach = 0.9f,
                DeflectDuration = 0.4f,
                BladeBase = 0f,
                BladeTip = 0.12f,
                Idle = idle,
                Attacks = new[]
                {
                    new AttackDefinition
                    {
                        Windup = 0.22f, Active = 0.12f, Recovery = 0.3f, ComboStart = 0.3f, ComboEnd = 0.6f,
                        Damage = 8, MoveMultiplier = 0.8f,
                        WindupPose = BattleAnimationLibrary.OneHanded(new(0.3f, 1.3f, 0.1f), new(0f, 0.3f, 0.95f), yaw: 18f),
                        MidPose = BattleAnimationLibrary.OneHanded(new(0.12f, 1.38f, 0.62f), new(0f, 0.1f, 1f)),
                        EndPose = BattleAnimationLibrary.OneHanded(new(0.06f, 1.4f, 0.7f), new(0f, 0f, 1f), yaw: -12f)
                    },
                    new AttackDefinition
                    {
                        Windup = 0.22f, Active = 0.12f, Recovery = 0.3f, ComboStart = 0.3f, ComboEnd = 0.6f,
                        Damage = 8, MoveMultiplier = 0.8f,
                        WindupPose = BattleAnimationLibrary.OneHanded(new(0.2f, 1.25f, 0.2f), new(0.3f, 0.3f, 0.9f), yaw: -14f),
                        MidPose = BattleAnimationLibrary.OneHanded(new(-0.02f, 1.36f, 0.6f), new(-0.2f, 0.1f, 1f)),
                        EndPose = BattleAnimationLibrary.OneHanded(new(-0.1f, 1.38f, 0.66f), new(-0.3f, 0f, 1f), yaw: 12f)
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
                        WindupPose = BattleAnimationLibrary.OneHanded(new(0.36f, 1.7f, 0.1f), new(0.5f, 0.7f, -0.5f), yaw: 20f),
                        MidPose = BattleAnimationLibrary.OneHanded(new(0.2f, 1.56f, 0.46f), new(0.18f, 0.46f, 0.87f)),
                        EndPose = BattleAnimationLibrary.OneHanded(new(0f, 1.44f, 0.46f), new(-0.46f, -0.39f, 0.79f), yaw: -18f)
                    },
                    new AttackDefinition
                    {
                        Windup = windup * 0.9f, Active = active, Recovery = recovery, ComboStart = windup * 0.9f + active * 0.5f, ComboEnd = windup * 0.9f + active + recovery * 0.8f,
                        Damage = damage, MoveMultiplier = 0.7f,
                        WindupPose = BattleAnimationLibrary.OneHanded(new(0f, 1.38f, 0.3f), new(-0.85f, 0.15f, 0.25f), yaw: -15f),
                        MidPose = BattleAnimationLibrary.OneHanded(new(0.14f, 1.38f, 0.48f), new(0f, 0.1f, 1f)),
                        EndPose = BattleAnimationLibrary.OneHanded(new(0.38f, 1.38f, 0.32f), new(0.7f, 0.05f, 0.7f), yaw: 15f)
                    },
                    new AttackDefinition
                    {
                        Windup = windup * 1.1f, Active = active, Recovery = recovery * 1.2f, ComboStart = windup * 1.1f + active * 0.5f, ComboEnd = windup * 1.1f + active + recovery,
                        Damage = Mathf.RoundToInt(damage * 1.1f), MoveMultiplier = 0.6f, Stagger = 0.2f,
                        WindupPose = BattleAnimationLibrary.OneHanded(new(0.22f, 1.88f, 0f), new(0.1f, 0.5f, -0.85f), pitch: -10f),
                        MidPose = BattleAnimationLibrary.OneHanded(new(0.2f, 1.78f, 0.38f), new(-0.05f, 0.6f, 0.8f)),
                        EndPose = BattleAnimationLibrary.OneHanded(new(0.14f, 1.44f, 0.46f), new(-0.08f, -0.2f, 0.98f), pitch: 12f)
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

        private static WeaponDefinition CreateBattleAxe()
        {
            WeaponDefinition definition = BattleAnimationLibrary.CreateGreatsword();
            definition.Prefix = BattleAxe;
            definition.DisplayName = "Battle Axe";
            definition.BladeBase = 0.75f;
            definition.BladeTip = 1.15f;
            definition.Reach = 1.75f;
            definition.DeflectDuration = 0.9f;
            definition.Attacks = new[] { definition.Attacks[0], definition.Attacks[2] };
            definition.Attacks[0].Damage = 43;
            definition.Attacks[0].Windup = 0.75f;
            definition.Attacks[0].Stagger = 0.3f;
            definition.Attacks[1].Damage = 50;
            definition.Attacks[1].Windup = 0.85f;
            definition.Attacks[1].Stagger = 0.45f;
            definition.BlockMitigation = 0.7f;
            definition.BlockBoxCenter = new Vector3(0f, 0f, 0.5f);
            definition.BlockBoxExtents = new Vector3(0.08f, 0.08f, 0.45f);

            return definition;
        }

        private static WeaponDefinition CreateSpear()
        {
            BodyPose idle = BattleAnimationLibrary.TwoHanded(new(0.26f, 1.2f, 0.2f), new(-0.08f, 0.15f, 0.98f), -0.55f, 20f);

            return new WeaponDefinition
            {
                Prefix = Spear,
                DisplayName = "Spear",
                Kind = WeaponKind.TwoHanded,
                Reach = 2.4f,
                DeflectDuration = 0.7f,
                BladeBase = 1.5f,
                BladeTip = 2.0f,
                Idle = idle,
                Attacks = new[]
                {
                    Thrust(34, 0.7f, 0.18f, 0.5f, 1.3f),
                    Thrust(34, 0.65f, 0.18f, 0.5f, 1.42f),
                    Thrust(38, 0.8f, 0.2f, 0.6f, 1.2f, 0.25f)
                },
                CanBlock = true,
                BlockRaise = 0.25f,
                BlockMitigation = 0.65f,
                BlockImpact = 0.3f,
                BlockRecovery = 0.45f,
                BlockAngle = 75f,
                BlockMove = 0.55f,
                Block = BattleAnimationLibrary.TwoHanded(new(0.35f, 1.4f, 0.4f), new(-0.95f, 0.25f, 0.1f), -0.6f, 8f),
                BlockHit = BattleAnimationLibrary.TwoHanded(new(0.33f, 1.28f, 0.32f), new(-0.95f, 0.2f, -0.1f), -0.6f, 8f, -5f),
                BlockLowered = BattleAnimationLibrary.TwoHanded(new(0.33f, 1.24f, 0.34f), new(-0.95f, 0.15f, 0f), -0.6f, 8f),
                DeflectPose = BattleAnimationLibrary.TwoHanded(new(0.3f, 1.5f, 0.1f), new(-0.2f, 0.8f, 0.5f), -0.55f, 18f, -8f),
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
                WindupPose = BattleAnimationLibrary.TwoHanded(new(0.34f, height, -0.25f), new(-0.08f, -0.05f, 1f), -0.5f, 28f),
                MidPose = BattleAnimationLibrary.TwoHanded(new(0.2f, height + 0.05f, 0.45f), new(-0.02f, -0.05f, 1f), -0.5f, 5f),
                EndPose = BattleAnimationLibrary.TwoHanded(new(0.12f, height + 0.08f, 0.75f), new(0f, -0.08f, 1f), -0.5f, -8f)
            };
        }

        private static WeaponDefinition CreateStaff()
        {
            WeaponDefinition definition = BattleAnimationLibrary.CreateGreatsword();
            definition.Prefix = Staff;
            definition.DisplayName = "Magic Staff";
            definition.BladeBase = 0.3f;
            definition.BladeTip = 1.3f;
            definition.Reach = 1.8f;
            definition.Idle = BattleAnimationLibrary.TwoHanded(new(0.22f, 1.25f, 0.3f), new(0.1f, 0.95f, 0.25f), -0.5f, 15f);
            definition.Attacks = new[] { definition.Attacks[0], definition.Attacks[1] };
            definition.Attacks[0].Damage = 29;
            definition.Attacks[1].Damage = 32;
            definition.BlockMitigation = 0.6f;
            definition.BlockBoxCenter = new Vector3(0f, 0f, 0.6f);
            definition.BlockBoxExtents = new Vector3(0.06f, 0.06f, 0.6f);

            return definition;
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
