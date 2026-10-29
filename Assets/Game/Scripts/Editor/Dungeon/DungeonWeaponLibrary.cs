using System;
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
        public const string Bow = "Bow";
        public const string SwordShieldLeft = "SwordShieldLeft";
        public const string ArmingSword = "ArmingSword";
        public const string BattleAxe = "BattleAxe";
        public const string Crossbow = "Crossbow";
        public const string Staff = "Staff";
        public const string MaceShield = "MaceShield";
        public const string Spellbook = "Spellbook";
        public const string BearClaws = "BearClaws";
        public const string PantherClaws = "PantherClaws";
        public const string RatBite = "RatBite";
        public const string MorningStar = "MorningStar";
        public const string SwordEcu = "SwordEcu";
        public const string MaceEcu = "MaceEcu";
        public const string VikingSword = "VikingSword";
        public const string VikingShield = "VikingShield";
        public const string VikingEcu = "VikingEcu";
        public const string Torch = "Torch";

        private const float AxeHead = 0.8f;
        /// A long axe is held at the butt with the off hand, this far down the haft from the main hand.
        private const float AxeGrip = -0.42f;
        private const float Frame = 1f / BattleAnimationBuilder.FrameRate;
        /// A frame of 60 fps footage of Dark and Darker. A fight there runs at full pace and ours at the base action
        /// speed of the fighters, so a clip is that much shorter than what it is copied from.
        private const float Footage = 0.75f * Frame;
        /// How much lower on the screen than in the footage the raised bare fists of the block are held.
        private const float RaisedFistDrop = 0.27f;
        private const float StaffHands = -0.55f;
        private const float StaffReach = -0.62f;
        private const float StaffBlockHands = -0.5f;
        private const float StaffLean = 65f;
        private const float StaffButt = -0.9f;
        private const float StaffButtEnd = -1.2f;
        /// From the spine of the shut book to its fore-edge: one hand holds each.
        private const float BookWidth = 0.17f;
        /// The book taken out shut opens on the palm at frame 24 of the footage (D:\spellbook_show_hide_animation.mp4, 156-200),
        /// the one put away is shut by frame 13 of its 36 (440-476) and gone below the view at 33.
        public const float BookEquipOpen = 24f * Frame;
        public const float BookUnequipShut = 13f * Frame;
        /// A weapon taken out from over the shoulder is in the hand there by this time; the whole draw takes DrawTime.
        private const float DrawReach = 13f * Frame;
        private const float DrawTime = 36f * Frame;
        /// The right arm of a draw over the shoulder: elbow, hand and the haft's elevation (over 90 = leant back). The shield
        /// arm does the same mirrored.
        private static readonly (float time, Vector3 elbow, Vector3 hand, float elevation, float slope)[] s_overShoulder =
        {
            (6f * Frame, Cm(40f, 1.3f, -15f), Cm(32f, 1.55f, 5f), 85f, 1f),
            (10f * Frame, Cm(45f, 1.55f, -12f), Cm(30f, 1.85f, -22f), 160f, 1f),
            // The elbow up by the shoulder, the forearm back over it, the fist behind the shoulder thumb down, as a hand
            // closes on a haft there: thumb up wrung the forearm past its limit. The head hangs behind the back.
            (DrawReach, Cm(38f, 1.7f, -5f), Cm(18f, 1.78f, -30f), -89f, 0.5f),
            // Forward past the side of the head, the head swung up behind it.
            (18f * Frame, Cm(50f, 1.55f, -20f), Cm(25f, 1.75f, -5f), 130f, 1f),
            (26f * Frame, Cm(35f, 1.3f, -8f), Cm(25f, 1.5f, 25f), 95f, 1f)
        };

        /// Catalog order = combat catalog index. Battle prefab slots 1-4 map to the first four entries; monsters find their
        /// weapon by name.
        public static readonly string[] CatalogOrder =
        {
            SwordShield, Bow, SwordShieldLeft, Fists, ArmingSword, BattleAxe, Crossbow, Staff, MaceShield, Spellbook, BearClaws, PantherClaws, RatBite, MorningStar, SwordEcu, MaceEcu, VikingSword, VikingShield, VikingEcu, Torch
        };

        /// The entry whose shield is the model of a shield item, by its ShieldIndex: the round shield, the écu.
        public static readonly string[] ShieldModels = { SwordShield, SwordEcu };

        /// Shield items by ShieldIndex: their names and the entries their block, rest and carry come from.
        private static readonly (string name, Func<WeaponDefinition> create)[] s_shields =
        {
            ("Round Shield", BattleAnimationLibrary.CreateSwordShield),
            ("Écu", CreateEcu)
        };

        /// Every one-handed weapon and its entries with each shield in the off hand, by ShieldIndex. A pair swings and
        /// ripostes as the weapon does alone and blocks as the shield does (see WithShield): a new one-handed weapon only
        /// needs a line here.
        public static readonly (string weapon, Func<WeaponDefinition> create, string[] withShield)[] ShieldPairs =
        {
            (ArmingSword, CreateArmingSword, new[] { SwordShield, SwordEcu }),
            (MorningStar, CreateMorningStar, new[] { MaceShield, MaceEcu }),
            (VikingSword, CreateVikingSword, new[] { VikingShield, VikingEcu })
        };

        /// Catalog entries that play another entry's clips (its prefix) with a model, reach and damage of their own.
        /// Their definitions keep the timings of the source, otherwise the shared clips would not match, and their blade
        /// has to cover the strike point of the source, which is what the clips bring to the crosshair.
        private static readonly (string name, string source, string displayName)[] s_variants =
        {
            (RatBite, Fists, "Rat Bite")
        };

        /// Impact of the weapon's hits and Stability of its block, 1..10: a hit with more Impact than the Stability breaks the block.
        private static readonly Dictionary<string, (int impact, int stability)> s_force = new()
        {
            ["Bare Hands"] = (1, 1), ["Spellbook"] = (1, 4), ["Rat Bite"] = (1, 1), ["Arming Sword"] = (4, 3), ["Morning Star"] = (6, 3), ["Magic Staff"] = (4, 4),
            ["Round Shield"] = (1, 7), ["Écu"] = (1, 8), ["Battle Axe"] = (7, 4), ["Bow"] = (3, 1), ["Crossbow"] = (5, 1), ["Panther Claws"] = (4, 2),
            ["Bear Claws"] = (7, 4), ["Viking Sword"] = (5, 3), ["Torch"] = (2, 1)
        };

        /// A weapon with a shield hits with the weapon's Impact and blocks with the shield's Stability.
        public static (int impact, int stability) Force(string displayName)
        {
            string[] parts = displayName.Split(" & ");
            (int impact, int stability) force = s_force.TryGetValue(parts[0], out (int, int) found) ? found : (3, 3);

            return parts.Length > 1 ? (force.impact, s_force[parts[1]].stability) : force;
        }

        /// The entries a one-handed weapon is fought with per shield in the off hand, by ShieldIndex; null for the rest.
        public static string[] WithShield(string weapon)
        {
            foreach ((string name, Func<WeaponDefinition> _, string[] withShield) in ShieldPairs)
            {
                if (name == weapon)
                    return withShield;
            }

            return null;
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
            List<WeaponDefinition> all = new()
            {
                CreateBow(),
                CreateFists(),
                CreateArmingSword(),
                CreateBattleAxe(),
                CreateCrossbow(),
                CreateStaff(),
                CreateSpellbook(),
                CreateBearClaws(),
                CreatePantherClaws(),
                CreateRatBite(),
                CreateMorningStar(),
                CreateVikingSword(),
                CreateTorch()
            };

            // Every pair is built from a definition of its own: WithShield changes the poses it is given.
            foreach ((string _, Func<WeaponDefinition> create, string[] withShield) in ShieldPairs)
            {
                for (int i = 0; i < withShield.Length; i++)
                    all.Add(WithShield(create(), s_shields[i].create(), withShield[i], s_shields[i].name));
            }

            return all.ToArray();
        }

        /// The morning star of Dark and Darker, swung in the right hand with the left one hanging free. It is carried
        /// upright at the right side. The first and the third swing lay it level over the head, turn it round to the back
        /// and bring it down from the right: the first almost straight, the third on a wider slant. Between them comes a
        /// backhand, laid back over the left shoulder and whipped flat to the front. A blocked hit is answered with the
        /// widest slant, straight from the guard before the forehead. Every swing stops dead ahead with the arm long and
        /// the haft laid along it, sinks to the belly and comes back up into rest elbow first; the free arm is thrown
        /// back behind the hip by it.
        private static WeaponDefinition CreateMorningStar()
        {
            // The middle of the ball.
            const float head = 0.55f;
            const int damage = 34;
            // How far the haft is laid over in the hand along a long arm.
            const float along = 70f;
            // How far the haft stands up from the arm at the stop, turned about the ball: a fist cannot hold it along the forearm.
            const float lift = 30f;

            // The pose with the ball where it is and the haft raised about it: the hand and its elbow go down.
            BodyPose Lift(float raise, Vector3 grip, float yaw, float elevation, float torso, float pitch, Vector3 elbow, Vector3 offHand, float lean)
            {
                Vector3 drop = Quaternion.Euler(-elevation, yaw, 0f) * Vector3.forward * head - Quaternion.Euler(-elevation - raise, yaw, 0f) * Vector3.forward * head;

                return At(grip + drop, yaw, elevation + raise, torso, pitch, elbow + drop, offHand, lean);
            }

            BodyPose idle = At(Cm(22f, 1.2f, 35f), 0f, 82f, 0f, 0f, Cm(30f, 1f, 5f), Cm(-30f, 1f, 15f));
            // Level over the head pointing left, the elbow out at shoulder height: the overhead swings turn it back from here.
            BodyPose over = At(Cm(20f, 1.9f, 10f), -90f, 5f, 10f, 0f, Cm(40f, 1.5f, 5f), Cm(-40f, 0.95f, 0f), 15f);
            // Where a swing sinks to after its stop, the arm still long.
            BodyPose sunk = Lift(lift, Cm(5f, 1.23f, 46f), -8f, -3f, -30f, 15f, Cm(26f, 1.17f, 20f), Cm(-38f, 0.95f, -25f), along);
            BodyPose sunkLeft = Lift(lift, Cm(-2f, 1.23f, 46f), -12f, -5f, -30f, 15f, Cm(22f, 1.16f, 18f), Cm(-38f, 0.95f, -25f), along);
            // The way back to rest: the elbow goes out to the right, then the head comes up.
            BodyPose elbowOut = Lift(15f, Cm(12f, 1.18f, 40f), -20f, 22f, -15f, 8f, Cm(38f, 1.1f, 15f), Cm(-38f, 1f, -8f), 40f);
            BodyPose rising = At(Cm(20f, 1.18f, 35f), -20f, 55f, -5f, 0f, Cm(32f, 1.02f, 8f), Cm(-33f, 1f, 10f), 15f);
            BodyPose guard = At(Cm(15f, 1.72f, 28f), -50f, -20f, 0f, 0f, Cm(40f, 1.6f, 5f), Cm(-30f, 1f, 10f), 45f);

            // Over the head to the left, round to the back, then down past the right of the head, 15 degrees off upright.
            AttackDefinition chop = new AttackDefinition
            {
                Windup = 43 * Footage, Active = 10 * Footage, Recovery = 37 * Footage, Damage = damage, MoveMultiplier = 0.7f, Launch = 1f,
                Raise = new()
                {
                    Via(5, At(Cm(22f, 1.4f, 25f), -90f, 50f, 5f, 0f, Cm(38f, 1.15f, 5f), Cm(-35f, 0.95f, 10f), 25f)),
                    Via(15, over),
                    Via(25, At(Cm(18f, 1.95f, 10f), -115f, 0f, 15f, -5f, Cm(40f, 1.5f, 5f), Cm(-40f, 0.95f, 0f))),
                    Via(35, At(Cm(15f, 1.95f, 10f), -160f, 0f, 15f, -5f, Cm(40f, 1.55f, 0f), Cm(-40f, 0.95f, 0f))),
                    Via(40, At(Cm(12f, 1.95f, 20f), 165f, 20f, 5f, 0f, Cm(35f, 1.6f, 15f), Cm(-36f, 0.93f, -15f), 15f))
                },
                // The elbow comes forward under the hand first, the forearm upright.
                WindupPose = At(Cm(10f, 1.78f, 40f), 135f, 80f, -15f, 10f, Cm(20f, 1.45f, 30f), Cm(-33f, 0.92f, -30f), 50f),
                MidPose = Peak(Cm(2f, 1.28f, 55f), -38f, 20f, Cm(12f, 1.15f, 25f), Cm(-35f, 0.9f, -42f), 50f),
                EndPose = Lift(lift, Cm(0f, 1.28f, 56f), -6f, -4f, -45f, 22f, Cm(4f, 1.3f, 26f), Cm(-35f, 0.9f, -45f), along),
                Return = new() { Via(63, sunk, 0.5f), Via(72, elbowOut), Via(82, rising) }
            };
            // The forearm folds across the throat to the left temple, the head hangs behind the left shoulder; the hand
            // goes out ahead first and the head whips round after it, level, to stop dead ahead.
            AttackDefinition backhand = new AttackDefinition
            {
                Windup = 48 * Footage, Active = 7 * Footage, Recovery = 35 * Footage, Damage = Mathf.RoundToInt(damage * 1.05f), MoveMultiplier = 0.7f, Launch = 1f,
                Raise = new()
                {
                    Via(5, Lift(lift, Cm(0f, 1.22f, 40f), -25f, 0f, -28f, 10f, Cm(28f, 1.2f, 18f), Cm(-38f, 0.95f, -15f), along)),
                    Via(12, Lift(25f, Cm(-3f, 1.3f, 35f), -50f, 8f, -12f, 5f, Cm(28f, 1.28f, 15f), Cm(-36f, 0.95f, -10f), 55f)),
                    Via(18, Lift(25f, Cm(-15f, 1.42f, 28f), -100f, 5f, -15f, 0f, Cm(18f, 1.4f, 25f), Cm(-33f, 0.95f, -20f), 45f)),
                    Via(24, At(Cm(-19f, 1.56f, 24f), -150f, -12f, -20f, 0f, Cm(10f, 1.45f, 30f), Cm(-32f, 0.93f, -30f), 30f)),
                    Via(31, At(Cm(-20f, 1.69f, 18f), -160f, -25f, -20f, 0f, Cm(10f, 1.42f, 35f), Cm(-32f, 0.92f, -38f), 25f), 0.4f),
                    Via(40, At(Cm(-19f, 1.7f, 20f), -156f, -20f, -20f, 2f, Cm(10f, 1.42f, 35f), Cm(-32f, 0.92f, -38f), 25f), 0.5f),
                    Via(43, At(Cm(-12f, 1.66f, 28f), -135f, 0f, -21f, 5f, Cm(10f, 1.42f, 35f), Cm(-32f, 0.92f, -38f), 25f)),
                    Via(46, At(Cm(0f, 1.6f, 42f), -95f, 25f, -25f, 8f, Cm(12f, 1.45f, 33f), Cm(-33f, 0.91f, -40f), 15f))
                },
                WindupPose = At(Cm(12f, 1.52f, 53f), -88f, 30f, -30f, 10f, Cm(12f, 1.43f, 28f), Cm(-34f, 0.9f, -42f), 5f),
                MidPose = Peak(Cm(16f, 1.29f, 52f), -38f, 17f, Cm(18f, 1.15f, 22f), Cm(-35f, 0.9f, -45f), 55f),
                EndPose = Lift(lift, Cm(20f, 1.41f, 50f), 1f, 4f, -40f, 18f, Cm(20f, 1.37f, 20f), Cm(-35f, 0.9f, -45f), along),
                Return = new()
                {
                    Via(66, Lift(lift, Cm(20f, 1.28f, 42f), 0f, -3f, -30f, 12f, Cm(30f, 1.2f, 18f), Cm(-38f, 0.95f, -25f), along), 0.5f),
                    Via(76, Lift(15f, Cm(20f, 1.2f, 40f), -10f, 30f, -10f, 5f, Cm(32f, 1.05f, 10f), Cm(-35f, 1f, -5f), 25f)),
                    Via(84, At(Cm(21f, 1.2f, 37f), -5f, 62f, -3f, 0f, Cm(31f, 1.02f, 7f), Cm(-31f, 1f, 12f), 10f))
                },
                After = chop
            };
            // The chop again out of the backhand's stop, coming out wider round the right: 30 degrees off upright.
            AttackDefinition slant = new AttackDefinition
            {
                Windup = 46 * Footage, Active = 10 * Footage, Recovery = 34 * Footage, Damage = Mathf.RoundToInt(damage * 1.1f), MoveMultiplier = 0.6f, Stagger = 0.35f, Launch = 1f,
                Raise = new()
                {
                    Via(7, Lift(35f, Cm(20f, 1.46f, 35f), -45f, 30f, -10f, 5f, Cm(38f, 1.2f, 10f), Cm(-37f, 0.95f, -15f), 50f)),
                    Via(12, At(Cm(22f, 1.72f, 25f), -75f, 20f, 0f, 0f, Cm(40f, 1.38f, 5f), Cm(-39f, 0.95f, -5f), 40f)),
                    Via(17, over),
                    Via(27, At(Cm(18f, 1.95f, 10f), -120f, 0f, 15f, -5f, Cm(40f, 1.5f, 5f), Cm(-40f, 0.95f, 0f))),
                    Via(37, At(Cm(15f, 1.95f, 10f), -170f, 0f, 15f, -5f, Cm(40f, 1.55f, 0f), Cm(-40f, 0.95f, 0f))),
                    Via(42, At(Cm(15f, 1.95f, 15f), 175f, 10f, 10f, 0f, Cm(38f, 1.6f, 10f), Cm(-37f, 0.94f, -12f))),
                    Via(44, At(Cm(15f, 1.9f, 25f), 140f, 45f, 5f, 5f, Cm(32f, 1.55f, 20f), Cm(-35f, 0.93f, -22f), 15f))
                },
                WindupPose = At(Cm(15f, 1.8f, 37f), 100f, 65f, -10f, 10f, Cm(25f, 1.48f, 28f), Cm(-33f, 0.92f, -30f), 30f),
                MidPose = Peak(Cm(0f, 1.28f, 56f), -35f, 20f, Cm(12f, 1.15f, 25f), Cm(-35f, 0.9f, -42f), 50f),
                EndPose = Lift(lift, Cm(-8f, 1.3f, 55f), -10f, -7f, -45f, 22f, Cm(4f, 1.3f, 26f), Cm(-35f, 0.9f, -45f), along),
                Return = new() { Via(64, sunkLeft, 0.5f), Via(74, elbowOut), Via(83, rising) },
                After = backhand
            };
            // From the guard the head goes on up and round the back without a pause and comes down on the widest slant.
            AttackDefinition riposte = new AttackDefinition
            {
                Windup = 30 * Footage, Active = 10 * Footage, Recovery = 32 * Footage, Damage = Mathf.RoundToInt(damage * 1.5f), MoveMultiplier = 0.5f, Stagger = 0.4f, Launch = 1f,
                Raise = new()
                {
                    Via(4, At(Cm(18f, 1.8f, 22f), -60f, -12f, 5f, 0f, Cm(40f, 1.6f, 5f), Cm(-30f, 1f, 10f), 30f)),
                    Via(8, At(Cm(20f, 1.88f, 15f), -80f, -8f, 10f, 0f, Cm(40f, 1.55f, 5f), Cm(-35f, 0.95f, 5f), 15f)),
                    Via(12, At(Cm(20f, 1.93f, 10f), -95f, -5f, 10f, 0f, Cm(40f, 1.55f, 5f), Cm(-38f, 0.95f, 0f), 5f)),
                    Via(20, At(Cm(18f, 1.95f, 10f), -155f, 0f, 15f, -5f, Cm(40f, 1.55f, 0f), Cm(-40f, 0.95f, 0f))),
                    Via(24, At(Cm(15f, 1.95f, 15f), 175f, 5f, 10f, 0f, Cm(38f, 1.6f, 10f), Cm(-38f, 0.95f, -8f))),
                    Via(28, At(Cm(15f, 1.88f, 30f), 135f, 40f, 5f, 5f, Cm(32f, 1.55f, 20f), Cm(-35f, 0.93f, -22f), 20f))
                },
                // The hand stays high while the head comes round it, then the arm drops and straightens.
                WindupPose = At(Cm(15f, 1.82f, 40f), 95f, 40f, -10f, 10f, Cm(25f, 1.5f, 28f), Cm(-33f, 0.92f, -30f), 35f),
                MidPose = Peak(Cm(0f, 1.29f, 56f), -35f, 20f, Cm(12f, 1.15f, 25f), Cm(-35f, 0.9f, -42f), 50f),
                EndPose = Lift(lift, Cm(-8f, 1.28f, 55f), -15f, -7f, -45f, 22f, Cm(4f, 1.3f, 26f), Cm(-35f, 0.9f, -45f), along),
                Return = new() { Via(48, sunkLeft, 0.5f), Via(56, elbowOut), Via(64, rising) }
            };

            foreach (AttackDefinition attack in new[] { chop, backhand, slant, riposte })
            {
                attack.ComboStart = attack.Windup + attack.Active * 0.5f;
                attack.ComboEnd = attack.Windup + attack.Active + attack.Recovery * 0.65f;
            }

            return new WeaponDefinition
            {
                Prefix = MorningStar,
                DisplayName = "Morning Star",
                Kind = WeaponKind.OneHanded,
                IsRound = true,
                PalmLean = 25f,
                Reach = 1.2f,
                DeflectDuration = 0.6f,
                BladeBase = 0.1f,
                BladeTip = 0.65f,
                Strike = head,
                Idle = idle,
                EquipVia = OverShoulder(idle),
                EquipShown = DrawReach,
                Attacks = new[] { chop, backhand, slant },
                Riposte = riposte,
                CanBlock = true,
                BlockRaise = 0.2f,
                BlockMitigation = 0.7f,
                BlockImpact = 0.28f,
                BlockRecovery = 0.4f,
                BlockAngle = 80f,
                BlockMove = 0.6f,
                // The first frame of the riposte in the footage: the haft slanting down to the left before the forehead.
                Block = guard,
                BlockHit = At(Cm(14f, 1.64f, 21f), -55f, -14f, 0f, -5f, Cm(40f, 1.5f, 3f), Cm(-30f, 1f, 8f), 45f),
                BlockLowered = At(Cm(14f, 1.6f, 25f), -50f, -18f, 0f, 0f, Cm(40f, 1.45f, 5f), Cm(-30f, 1f, 10f), 45f),
                DeflectPose = At(Cm(34f, 1.62f, 10f), 45f, 63f, 12f, -6f, Cm(42f, 1.35f, 0f), Cm(-30f, 1f, 5f), 50f),
                BlockSocket = WeaponSocket.RightHand,
                BlockBoxCenter = new Vector3(0f, 0f, 0.3f),
                BlockBoxExtents = new Vector3(0.08f, 0.08f, 0.35f)
            };
        }

        /// A burning torch from the belt, struck with LMB in the left hand that carries it (RMB throws it): two short vertical
        /// blows, the first coming down a little right of the middle, the second a little left. Both are authored in the right hand
        /// and played mirrored (IsOffHand); the idle is the torch hold of the belt, so the rest pose here is its mirror image. The
        /// model is the belt item in the hand: no attachment, no block.
        private static WeaponDefinition CreateTorch()
        {
            const int damage = 14;

            BodyPose idle = At(Cm(29f, 1.43f, 51.5f), -122f, 85f, 0f, 0f, Cm(36.5f, 1.3f, 26f), Cm(-12f, 1.5f, 35f));
            AttackDefinition right = new AttackDefinition
            {
                Windup = 30 * Footage, Active = 8 * Footage, Recovery = 30 * Footage, Damage = damage, MoveMultiplier = 0.85f, Launch = 1f, IsOffHand = true,
                Raise = new() { Via(12, At(Cm(26f, 1.7f, 30f), 10f, 70f, 5f, 0f, Cm(34f, 1.45f, 12f), Cm(-12f, 1.35f, 32f))) },
                WindupPose = At(Cm(20f, 1.86f, 22f), 10f, 105f, 10f, -5f, Cm(30f, 1.6f, 5f), Cm(-12f, 1.3f, 30f)),
                MidPose = Peak(Cm(8f, 1.5f, 50f), -8f, 10f, Cm(18f, 1.35f, 25f), Cm(-15f, 1.25f, 30f), 20f),
                EndPose = At(Cm(4f, 1.3f, 52f), -5f, -40f, -12f, 15f, Cm(15f, 1.25f, 25f), Cm(-15f, 1.2f, 28f), 30f),
                Return = new() { Via(52, At(Cm(18f, 1.3f, 45f), -60f, 30f, -5f, 8f, Cm(28f, 1.2f, 22f), Cm(-12f, 1.35f, 32f))) }
            };
            AttackDefinition left = new AttackDefinition
            {
                Windup = 30 * Footage, Active = 8 * Footage, Recovery = 30 * Footage, Damage = damage, MoveMultiplier = 0.85f, Launch = 1f, IsOffHand = true,
                Raise = new() { Via(12, At(Cm(8f, 1.62f, 42f), -10f, 60f, 5f, 0f, Cm(20f, 1.42f, 20f), Cm(-12f, 1.3f, 30f))) },
                WindupPose = At(Cm(-4f, 1.86f, 25f), -15f, 105f, 15f, -5f, Cm(12f, 1.6f, 8f), Cm(-14f, 1.3f, 30f)),
                MidPose = Peak(Cm(-2f, 1.5f, 50f), 10f, 10f, Cm(10f, 1.35f, 25f), Cm(-16f, 1.25f, 30f), 20f),
                EndPose = At(Cm(-7f, 1.3f, 52f), 5f, -40f, 10f, 15f, Cm(8f, 1.25f, 25f), Cm(-16f, 1.2f, 28f), 30f),
                Return = new() { Via(52, At(Cm(18f, 1.3f, 45f), -60f, 30f, -5f, 8f, Cm(28f, 1.2f, 22f), Cm(-12f, 1.35f, 32f))) },
                After = right
            };

            foreach (AttackDefinition attack in new[] { right, left })
            {
                attack.ComboStart = attack.Windup + attack.Active * 0.5f;
                attack.ComboEnd = attack.Windup + attack.Active + attack.Recovery * 0.65f;
            }

            return new WeaponDefinition
            {
                Prefix = Torch,
                DisplayName = "Torch",
                Kind = WeaponKind.OneHanded,
                IsRound = true,
                Reach = 1.05f,
                DeflectDuration = 0.5f,
                BladeBase = 0.05f,
                BladeTip = 0.42f,
                Strike = 0.35f,
                Idle = idle,
                Attacks = new[] { right, left }
            };
        }

        /// The spellbook of Dark and Darker, authored in the right hand and baked mirrored (IsLeftHanded): in the game it is
        /// the left hand's. At rest it lies open on the palm, low in the view, its spine running ahead and the pages up. To block,
        /// the other hand folds it shut and both stand it up before the eyes, pinching its sides, the forearms rising from
        /// the bottom corners of the view. It never strikes: the book only blocks and casts.
        private static WeaponDefinition CreateSpellbook()
        {
            Vector3 across = Vector3.left;
            // Open on the palm of the book hand raised before the chest, the spine steep and the pages to the eye, its head
            // just under the crosshair; the other hand rests low in the far corner of the view. The book breathes by a centimetre.
            BodyPose idle = OpenBook(new(0.085f, 1.54f, 0.47f), -3f, 40f, -2f, new(0.24f, 1.2f, 0.22f), new(-0.2f, 1.45f, 0.46f));
            BodyPose breath = OpenBook(new(0.085f, 1.55f, 0.47f), -3f, 42f, -2f, new(0.24f, 1.21f, 0.22f), new(-0.2f, 1.46f, 0.47f));
            BodyPose block = ShutBook(new(0.085f, 1.7f, 0.445f), across, Vector3.up, 0f);
            // Casting as in the footage: the book sinks to the bottom corner of the view while the free hand comes up from
            // the other one and holds the spell in its clawed fingers, palm forward; the release pulls the spell back low,
            // throws it forward with the hand open and lets the hand fall as the book comes back up.
            BodyPose lowered = OpenBook(new(0.1f, 1.3f, 0.42f), -3f, 25f, -2f, new(0.26f, 0.98f, 0.18f), Vector3.zero);
            BodyPose sinking = OpenBook(new(0.09f, 1.42f, 0.45f), -3f, 32f, -2f, new(0.25f, 1.1f, 0.2f), Vector3.zero);
            BodyPose charged = Casting(lowered, new(-0.22f, 1.67f, 0.44f), new(0.97f, -0.05f, -0.1f), new(0.05f, 0.9f, 0.4f), 0.6f);

            // The shut book between both hands, its head leant back by 'lean' and over to the book hand's other side by 'roll',
            // the fore-edge turned away from the eye by 'yaw'.
            BodyPose Shut(Vector3 hand, float lean, float roll, float yaw = 0f)
            {
                Quaternion turn = Quaternion.Euler(-lean, yaw, roll);

                return ShutBook(hand, turn * across, turn * Vector3.up, 0f);
            }

            return new WeaponDefinition
            {
                Prefix = Spellbook,
                DisplayName = "Spellbook",
                IsLeftHanded = true,
                Kind = WeaponKind.TwoHanded,
                Reach = 1f,
                DeflectDuration = 0.4f,
                BladeBase = 0f,
                BladeTip = BookWidth,
                Strike = BookWidth * 0.5f,
                IsHeldAcross = true,
                // A book has no edge to lead a cut with: it keeps the turn of the footage.
                IsRound = true,
                Idle = idle,
                IdleBreath = breath,
                IdleCycle = 160 * Footage,
                // As in the footage (mirrored, the book hand is the left one there): the book comes up shut from below the view
                // past the middle, stands up before the chest between both hands, opens on the palm and settles into the idle.
                EquipVia = new()
                {
                    new(0f, Shut(new(-0.06f, 1.18f, 0.32f), 10f, 25f)),
                    PoseKey.Flow(12 * Frame, Shut(new(0.055f, 1.505f, 0.42f), 5f, 8f, 20f), 1f),
                    PoseKey.Flow(18 * Frame, Shut(new(0.085f, 1.54f, 0.43f), 12f, 0f, 25f), 0.7f),
                    PoseKey.Flow(30 * Frame, OpenBook(new(0.11f, 1.52f, 0.46f), -3f, 50f, -2f, new(0.24f, 1.2f, 0.22f), new(-0.2f, 1.4f, 0.44f)), 1f),
                    new(44 * Frame, idle)
                },
                // Back the same way: lifted a little, folded shut by the other hand, stood up and laid over sideways down
                // into the far bottom corner and out of the view.
                UnequipVia = new()
                {
                    new(0f, idle),
                    PoseKey.Flow(6 * Frame, OpenBook(new(0.09f, 1.57f, 0.47f), -3f, 46f, -2f, new(0.24f, 1.22f, 0.22f), new(-0.16f, 1.5f, 0.48f)), 1f),
                    PoseKey.Flow(14 * Frame, Shut(new(0.1f, 1.55f, 0.43f), 10f, 0f, 20f), 1f),
                    PoseKey.Flow(20 * Frame, Shut(new(0.11f, 1.58f, 0.42f), 5f, 10f, 15f), 1f),
                    PoseKey.Flow(28 * Frame, Shut(new(0f, 1.53f, 0.4f), 5f, 40f, 0f), 1.2f),
                    new(36 * Frame, Shut(new(0f, 1.22f, 0.32f), 10f, 55f), Ease.Out)
                },
                UnequipHidden = 33 * Frame,
                CastVia = new()
                {
                    new(0f, idle),
                    new(16 * Frame, Casting(sinking, new(-0.29f, 1.42f, 0.42f), new(0.9f, -0.3f, 0.3f), new(0.3f, 0.85f, 0.4f), 0.4f)),
                    new(28 * Frame, Casting(lowered, new(-0.25f, 1.58f, 0.44f), new(0.95f, -0.2f, 0.1f), new(0.2f, 0.9f, 0.35f), 0.55f)),
                    new(48 * Frame, charged, Ease.Out)
                },
                CastReleaseVia = new()
                {
                    new(0f, charged),
                    new(8 * Frame, Casting(sinking, new(-0.18f, 1.62f, 0.4f), new(0f, 1f, -0.2f), new(0.2f, 0.2f, 0.95f), 0.35f)),
                    new(21 * Frame, Casting(sinking, new(-0.15f, 1.69f, 0.62f), new(1f, 0f, -0.15f), new(0.1f, 0.8f, 0.6f), 1f), Ease.Out),
                    new(34 * Frame, Casting(idle, new(-0.2f, 1.6f, 0.56f), new(1f, 0f, -0.12f), new(0.1f, 0.6f, 0.8f), 0.9f)),
                    new(47 * Frame, Casting(idle, new(-0.25f, 1.35f, 0.42f), new(1f, 0f, 0f), new(0f, 0.5f, 0.85f), 0.6f)),
                    new(58 * Frame, idle)
                },
                CanBlock = true,
                BlockRaise = 16 * Footage,
                BlockVia = new()
                {
                    // Folded shut low before the chest, then straight up past the hold, a little over it, and settled.
                    new(5 * Footage, ShutBook(new(0.148f, 1.6f, 0.445f), across, Vector3.up, 0f), Ease.Linear),
                    new(8 * Footage, ShutBook(new(0.12f, 1.665f, 0.445f), across, Vector3.up, 0f), Ease.Linear),
                    new(11 * Footage, ShutBook(new(0.09f, 1.725f, 0.43f), across, Vector3.up, 0f), Ease.Out)
                },
                BlockLower = 18 * Footage,
                BlockMitigation = 0.5f,
                BlockImpact = 0.25f,
                BlockRecovery = 0.3f,
                BlockAngle = 80f,
                BlockMove = 0.6f,
                Block = block,
                BlockHit = ShutBook(new(0.085f, 1.72f, 0.405f), across, new(0f, 1f, -0.12f), -4f),
                BlockLowered = ShutBook(new(0.085f, 1.68f, 0.435f), across, Vector3.up, 0f),
                DeflectPose = ShutBook(new(0.085f, 1.54f, 0.395f), across, new(0f, 1f, -0.2f), -4f),
                BlockSocket = WeaponSocket.RightHand,
                BlockBoxCenter = new Vector3(0f, -0.03f, BookWidth * 0.5f),
                BlockBoxExtents = new Vector3(0.04f, 0.13f, BookWidth * 0.5f)
            };
        }

        /// Taken out from over the right shoulder: the hand goes up on a bent elbow behind the shoulder, closes on the grip
        /// there (the weapon appears at DrawReach), brings it forward past the head, the head up, and down into the idle.
        /// The haft stands as upright as it can square to the forearm and the knuckles go on along it: an upright haft
        /// left to the solve bent the wrist and twisted the arm round.
        private static List<PoseKey> OverShoulder(BodyPose idle)
        {
            List<PoseKey> keys = new() { new(0f, idle) };

            foreach ((float time, Vector3 elbow, Vector3 hand, float elevation, float slope) in s_overShoulder)
            {
                Vector3 forearm = hand - elbow;
                Vector3 haft = Vector3.ProjectOnPlane(Quaternion.Euler(-elevation, 0f, 0f) * Vector3.forward, forearm);
                BodyPose pose = Pose(hand, haft, 0f, 0f, elbow, idle.Off.Position, 0f);
                pose.Main.Up = forearm;
                keys.Add(PoseKey.Flow(time, pose, slope));
            }

            keys.Add(new PoseKey(DrawTime, idle));

            return keys;
        }

        /// The shield arm of a draw over the shoulders, at the keys of OverShoulder: the forearm with the shield on it
        /// carried as one piece from its rest to run from 'elbow' to 'hand', the mirror of the weapon arm, the fist turned
        /// about it as the weapon hand's is (its thumb upright at rest goes where the haft would).
        private static BodyPose ShieldOverShoulder(BodyPose rest, Vector3 elbow, Vector3 hand, float elevation)
        {
            Vector3 restElbow = rest.Off.ElbowWeight > 0f ? rest.Off.Elbow : new Vector3(-0.29f, 1.16f, -0.03f);
            HandPose off = rest.Off;
            Vector3 restForearm = off.Position - restElbow;
            Vector3 forearm = hand - elbow;
            Vector3 thumb = Vector3.ProjectOnPlane(Quaternion.Euler(-elevation, 0f, 0f) * Vector3.forward, forearm);
            Quaternion turn = Quaternion.LookRotation(forearm, thumb) * Quaternion.Inverse(Quaternion.LookRotation(restForearm,
                Vector3.ProjectOnPlane(Vector3.up, restForearm)));
            off.Position = elbow + turn * (off.Position - restElbow);
            off.Forward = turn * off.Forward;
            off.Up = turn * off.Up;
            off.Elbow = elbow;
            off.ElbowWeight = 1f;
            rest.Off = off;

            return rest;
        }

        /// The book open on the palm of the book hand: the spine along the fingers at that yaw and elevation, the pages
        /// facing up and back, rolled about the spine toward the outer board. The other hand is a loose fist, thumb up.
        private static BodyPose OpenBook(Vector3 hand, float yaw, float elevation, float roll, Vector3 elbow, Vector3 offHand)
        {
            Quaternion turn = Quaternion.Euler(-elevation, yaw, roll);
            Vector3 fingers = turn * Vector3.forward;
            BodyPose pose = Pose(hand, Vector3.Cross(turn * Vector3.up, fingers), 0f, 0f, elbow, offHand, 0f);
            pose.Edge = fingers;
            pose.Off.Forward = Vector3.ProjectOnPlane(Vector3.up, pose.Off.Up);
            pose.OffOpen = 0.3f;

            return pose;
        }

        /// The book pose with the free hand somewhere else: thumb and fingers as straightened fingers would point, 'open' 0 = a fist.
        private static BodyPose Casting(BodyPose book, Vector3 hand, Vector3 thumb, Vector3 fingers, float open)
        {
            book.Off = new HandPose(hand, thumb, fingers);
            book.OffOpen = open;

            return book;
        }

        /// The shut book between both hands pinching its sides, the book hand on the spine and the other one on the
        /// fore-edge 'across' from it, the head of the book toward 'top': thumbs on the front cover slanting up and in, the
        /// fingers round the back one, so a hand's knuckles point where the back cover faces.
        private static BodyPose ShutBook(Vector3 hand, Vector3 across, Vector3 top, float pitch)
        {
            Vector3 back = Vector3.Cross(top, across).normalized;
            Vector3 side = across.normalized * Mathf.Cos(DungeonWeaponPrefabBuilder.BookThumb * Mathf.Deg2Rad);
            Vector3 up = top.normalized * Mathf.Sin(DungeonWeaponPrefabBuilder.BookThumb * Mathf.Deg2Rad);
            Vector3 offHand = hand + across.normalized * BookWidth;
            // The forearms run on behind the knuckles, a little out and down, as the footage has them rise from the bottom
            // corners of the view: an elbow out to the side bent the wrists past 85 degrees.
            Vector3 drop = new Vector3(0f, -0.12f, 0f) - back * 0.24f - across.normalized * 0.1f;
            BodyPose pose = BattleAnimationLibrary.Elbows(BattleAnimationLibrary.TwoHanded(hand, side + up, 0f, 0f, pitch), hand + drop,
                offHand + Vector3.Reflect(drop, across.normalized));
            pose.Edge = back;
            pose.Main.Up = back;
            pose.Off.Position = offHand;
            pose.Off.Forward = up - side;
            pose.Off.Up = back;

            return pose;
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
            // A loose brawler's guard: half fists below the chin, the left one leading, elbows flared, the back rounded.
            // It breathes: the hands sink and draw back 2 cm with the chest once every 98 video frames.
            BodyPose idle = BattleAnimationLibrary.Elbows(
                BattleAnimationLibrary.Guard(new(0.22f, 1.49f, 0.4f), new(-0.7f, 0.7f, 0f), new(-0.25f, 0.35f, 0.9f), 0.1f, 12f),
                new(0.33f, 1.17f, 0.28f), new(-0.33f, 1.17f, 0.28f));
            idle.Off.Position = new Vector3(-0.2f, 1.48f, 0.44f);
            // The block of D:are_hands_start_block_hold_end.mp4: fists shut low, swing up to vertical fists (thumbs up, knuckles at
            // the enemy) before the forehead, the forearms two columns from the bottom edge; let go, they drop and open on the way down.
            Vector3 upThumb = new Vector3(1f, 0.05f, -0.1f);
            Vector3 upFingers = new Vector3(0f, 1f, 0.1f);
            BodyPose hold = Fists2(0.535f, 0.05f, 0.455f, 0.09f, upThumb, upFingers, 0f, 0.23f);
            BodyPose breath = idle;
            breath.Main.Position += new Vector3(0f, -0.018f, -0.018f);
            breath.Off.Position += new Vector3(0f, -0.018f, -0.018f);
            breath.Spine.x -= 1f;

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
                IdleBreath = breath,
                IdleCycle = 98 * Footage,
                // The left hook is authored as a right one and played mirrored, so its screen positions are mirrored too.
                Attacks = new[]
                {
                    // Falls through the low left corner; the left one stays level and leaves through the right edge.
                    Hook(false, 26, 51, 12, new(0.065f, 1.58f, 0.52f), new(0.33f, 1.7f, 0.42f), new(-0.4f, 1.58f, 0.4f),
                        new(-0.1f, 1.56f, 0.36f), 35f, -65f),
                    Hook(true, 14, 26, 14, new(-0.03f, 1.55f, 0.52f), new(0.35f, 1.67f, 0.42f), new(-0.45f, 1.64f, 0.42f),
                        new(-0.11f, 1.58f, 0.36f), 35f, -75f)
                },
                CanBlock = true,
                BlockRaise = 16 * Footage,
                BlockVia = new()
                {
                    new(2 * Footage, Fists2(0.63f, 0.84f, 0.38f, 0.84f, new(-1f, 0f, 0f), new(0f, 0.5f, 0.85f), 0f, 0.3f), Ease.Linear),
                    new(4 * Footage, Fists2(0.6f, 0.7f, 0.37f, 0.72f, new(-0.2f, 0.5f, -0.85f), new(0f, 0.85f, 0.5f), 0f, 0.27f), Ease.Linear),
                    new(6 * Footage, Fists2(0.57f, 0.31f, 0.4f, 0.3f, new(0.6f, 0.2f, -0.75f), upFingers, 0f, 0.22f), Ease.Linear),
                    new(8 * Footage, Fists2(0.55f, 0.07f, 0.43f, 0.08f, new(0.85f, 0.1f, -0.5f), upFingers, 0f, 0.235f), Ease.Linear),
                    new(10 * Footage, Fists2(0.54f, 0.04f, 0.45f, 0.06f, upThumb, upFingers, 0f, 0.23f), Ease.Linear),
                    new(13 * Footage, Fists2(0.535f, 0.01f, 0.455f, 0.04f, upThumb, upFingers, 0f, 0.23f), Ease.Out)
                },
                BlockLower = 16 * Footage,
                BlockLowerVia = new()
                {
                    new(4 * Footage, Fists2(0.56f, 0.34f, 0.4f, 0.34f, upThumb, upFingers, 0.1f, 0.21f), Ease.Linear),
                    new(6 * Footage, Fists2(0.59f, 0.6f, 0.37f, 0.57f, new(-0.7f, 0.4f, -0.6f), new(0f, 0.8f, 0.6f), 0.2f, 0.25f), Ease.Linear),
                    new(8 * Footage, Fists2(0.6f, 0.72f, 0.37f, 0.7f, new(-0.9f, 0.3f, -0.3f), new(0f, 0.6f, 0.8f), 0.3f, 0.28f), Ease.Linear),
                    new(10 * Footage, Fists2(0.62f, 0.85f, 0.37f, 0.82f, new(-0.9f, 0.3f, -0.3f), new(-0.1f, 0.5f, 0.85f), 0.3f, 0.3f), Ease.Linear)
                },
                BlockMitigation = 0.4f,
                BlockImpact = 0.25f,
                BlockRecovery = 0.3f,
                BlockAngle = 80f,
                BlockMove = 0.7f,
                Block = hold,
                BlockHit = Shifted(hold, new(0f, 0f, -0.05f)),
                BlockLowered = Shifted(hold, new(0f, 0f, -0.02f)),
                DeflectPose = idle,
                BlockSocket = WeaponSocket.LeftHand,
                BlockBoxCenter = Vector3.zero,
                BlockBoxExtents = new Vector3(0.14f, 0.14f, 0.08f)
            };
        }

        /// Both fists placed by their screen centres (u right, v down) that far before the eye; the grip sits 0.1 m past the
        /// fist's centre along the fingers. The left hand mirrors the right one's hold.
        private static BodyPose Fists2(float rightU, float rightV, float leftU, float leftV, Vector3 thumb, Vector3 fingers, float open, float depth)
        {
            // The game's view shows raised hands higher than the rig's eye: drop them, or the fists leave the top edge.
            float drop = rightV < 0.5f ? RaisedFistDrop : 0f;
            rightV += drop;
            leftV += drop;
            BodyPose pose = BattleAnimationLibrary.Guard(OnScreen(rightU, rightV, depth) + fingers.normalized * 0.1f, thumb, fingers, open);
            pose.Off.Position = OnScreen(leftU, leftV, depth) + Vector3.Scale(fingers.normalized, new Vector3(-1f, 1f, 1f)) * 0.1f;
            pose.Main.Up = fingers.normalized;

            // Raised, the forearms close together up to the elbows; on the way up the elbows come in from the sides. An elbow hint level
            // with the shoulder (0.36 m under the fist since Eye is 1.67) flips the solve to straight arms.
            float side = Mathf.Lerp(0.03f, 0.15f, Mathf.InverseLerp(0.45f, 0.6f, rightV));

            return rightV < 0.5f
                ? BattleAnimationLibrary.Elbows(pose, pose.Main.Position + new Vector3(side, -0.3f, -0.14f), pose.Off.Position + new Vector3(-side, -0.3f, -0.14f))
                : pose;
        }

        private static Vector3 OnScreen(float u, float v, float depth)
        {
            Vector3 eye = BattleAnimationLibrary.Eye;

            return new Vector3(eye.x + (2f * u - 1f) * 1.364f * depth, eye.y - (2f * v - 1f) * 0.767f * depth, eye.z + depth);
        }

        /// A one-handed sword read off the screen: the crossguard at (u, v) that far ahead of the eye, the tip on the ray through
        /// (tipU, tipV), the edge turned from square to the view (the flat to the camera, 0) to along it (edge on, 1),
        /// and on past the edge to the other flat (-1).
        private static BodyPose Sword(float u, float v, float depth, float tipU, float tipV, float edgeOn, float torso, Vector3 elbow, Vector3 offHand)
        {
            const float guardToTip = 0.8f;
            const float gripToGuard = 0.1f;

            Vector3 eye = BattleAnimationLibrary.Eye;
            Vector3 guard = OnScreen(u, v, depth);
            Vector3 ray = OnScreen(tipU, tipV, 1f) - eye;
            Vector3 toEye = eye - guard;
            float b = Vector3.Dot(toEye, ray);
            float t = (-b + Mathf.Sqrt(b * b - ray.sqrMagnitude * (toEye.sqrMagnitude - guardToTip * guardToTip))) / ray.sqrMagnitude;
            Vector3 blade = (eye + ray * t - guard).normalized;
            Vector3 view = guard - eye;
            BodyPose pose = Pose(guard - blade * gripToGuard, blade, torso, 5f, elbow, offHand, 0f);
            pose.Edge = Vector3.Slerp(Vector3.Cross(view, blade).normalized * (edgeOn < 0f ? -1f : 1f), Vector3.ProjectOnPlane(view, blade).normalized, edgeOn < 0f ? 1f + edgeOn : edgeOn);

            return pose;
        }

        private static BodyPose Shifted(BodyPose pose, Vector3 offset)
        {
            pose.Main.Position += offset;
            pose.Off.Position += offset;

            return pose;
        }

        /// A haymaker. The free hand reaches out open at the target while the fist swings out of view low on its side; the
        /// free hand sinks away and the fist comes in from the edge of the view, palm down with the forearm level and the
        /// elbow out at its height, the chest turning with it. Past the crosshair the fist rolls over, palm under, the
        /// elbow above it, and leaves past the other edge. Times in video frames of D:\bare_hands_attack_1_2.mp4
        /// (attack 1 at 46, attack 2 at 136).
        private static AttackDefinition Hook(bool isOffHand, int reachAt, int windup, int active, Vector3 reach, Vector3 enter, Vector3 exit,
            Vector3 exitElbow, float exitRoll, float turn)
        {
            const float recovery = 40 * Footage;
            Vector3 low = new Vector3(-0.2f, 1.3f, 0.3f);
            Vector3 fingers = new Vector3(0.35f, 0.45f, 0.8f);
            float windupTime = windup * Footage;
            float activeTime = active * Footage;

            // The fist at the end of the forearm from 'elbow', knuckles along it or along 'knuckles'; 'roll' 0 = palm down,
            // 90 = thumb up.
            BodyPose Fist(Vector3 fist, Vector3 elbow, float roll, float yaw, Vector3 knuckles = default)
            {
                Vector3 forearm = (fist - elbow).normalized;
                Vector3 flat = Vector3.Cross(forearm, Vector3.up).normalized;
                BodyPose pose = BattleAnimationLibrary.Elbows(BattleAnimationLibrary.Punch(fist, low, fingers, 0.3f, yaw, 8f), elbow, Vector3.zero);
                pose.Main.Forward = flat * Mathf.Cos(roll * Mathf.Deg2Rad) + Vector3.Cross(flat, forearm) * Mathf.Sin(roll * Mathf.Deg2Rad);
                pose.Edge = knuckles == Vector3.zero ? forearm : knuckles.normalized;

                return pose;
            }

            return new AttackDefinition
            {
                Windup = windupTime, Active = activeTime, Recovery = recovery,
                ComboStart = windupTime + activeTime * 0.5f, ComboEnd = windupTime + activeTime + recovery * 0.65f,
                Damage = 8, MoveMultiplier = 0.8f, IsOffHand = isOffHand, Launch = 1f,
                Raise = new()
                {
                    Via(reachAt, BattleAnimationLibrary.Punch(new(0.36f, 1.34f, 0.1f), reach, fingers, 1f, 15f), 0.5f),
                    Via(windup - 6, Fist(new(0.45f, 1.52f, 0.12f), new(0.45f, 1.42f, -0.16f), 30f, 30f))
                },
                WindupPose = Fist(enter, new(0.44f, 1.6f, 0.05f), 0f, 35f),
                // The knuckles lead level, not up along the rising forearm.
                MidPose = Fist(new(0.12f, 1.72f, 0.5f), new(0.25f, 1.6f, 0.25f), 0f, -15f, new(-0.4f, 0f, 0.9f)),
                EndPose = Fist(exit, exitElbow, exitRoll, turn),
                Return = new() { Via(windup + active + 6, Fist(new(-0.35f, 1.25f, 0.3f), new(-0.05f, 1.3f, 0.15f), exitRoll, turn + 10f), 0.6f) }
            };
        }

        /// The arming sword of Dark and Darker, carried upright before the right hip with the left hand hanging free. The
        /// series is a forehand from behind the head over the right shoulder to the left hip, a backhand from beside the
        /// face over the left shoulder down to the right, and a thrust from the arm held out to the right. A blocked hit is
        /// answered from the low guard with an overhead cut from behind the head down the right side.
        private static WeaponDefinition CreateArmingSword()
        {
            return CreateArmingSword("Arming Sword", 0.12f, 0.9f, 1.4f, 27);
        }

        private static WeaponDefinition CreateArmingSword(string name, float bladeBase, float bladeTip, float reach, int damage)
        {
            Vector3 offHand = Cm(-25f, 1f, 5f);
            Vector3 lowElbow = new(0.55f, 1.3f, 0.35f);
            Vector3 blockElbow = new(0.4f, 1.3f, 0.3f);
            // The footage's first frame: the blade upright at the right of the view, flat to the camera, the fist under the bottom edge.
            BodyPose idle = Sword(0.72f, 0.8f, 0.45f, 0.72f, -0.3f, 0.3f, 0f, lowElbow, offHand);
            BodyPose upright = At(Cm(20f, 1.1f, 35f), 0f, 78f, 0f, 5f, Cm(25f, 1f, 10f), Cm(-25f, 1f, 5f), 10f);
            BodyPose levelLow = At(Cm(20f, 1f, 30f), 0f, 0f, 5f, 5f, Cm(25f, 1f, 5f), Cm(-25f, 1f, 5f), 30f);
            // The arm hangs out to the right and back after a cut down the right side.
            BodyPose outBack = At(Cm(48f, 1f, -25f), 115f, -15f, 30f, 10f, Cm(34f, 1.22f, -24f), Cm(-40f, 1f, 10f), 55f);
            BodyPose lowForward = At(Cm(28f, 0.95f, 15f), 35f, -15f, 15f, 5f, Cm(28f, 1f, 0f), Cm(-30f, 1f, 5f), 30f);

            // Cocked behind the head, round over the right shoulder and down across the body to the left hip.
            AttackDefinition forehand = new AttackDefinition
            {
                Windup = 25 * Footage, Active = 11 * Footage, Recovery = 60 * Footage, Damage = damage, MoveMultiplier = 0.7f, Launch = 1f,
                Raise = new()
                {
                    Via(6, At(Cm(25f, 1.45f, 25f), -150f, 45f, 10f, 0f, Cm(40f, 1.2f, 10f), Cm(-35f, 1f, 5f))),
                    Via(12, At(Cm(28f, 1.83f, 12f), -130f, 5f, 25f, -5f, Cm(45f, 1.5f, 0f), Cm(-40f, 1f, 10f)), 0.5f),
                    Via(22, At(Cm(25f, 1.83f, 15f), 180f, 15f, 25f, -5f, Cm(45f, 1.5f, 0f), Cm(-40f, 1f, 10f)), 0.5f)
                },
                WindupPose = At(Cm(22f, 1.8f, 25f), 160f, 50f, 15f, 0f, Cm(38f, 1.5f, 10f), Cm(-35f, 1.05f, 0f)),
                MidPose = Peak(Cm(10f, 1.44f, 45f), -25f, 10f, Cm(20f, 1.35f, 25f), Cm(-35f, 1.05f, -45f), 50f),
                EndPose = At(Cm(-10f, 1f, 50f), -45f, -40f, -45f, 15f, Cm(5f, 1.15f, 25f), Cm(-38f, 1f, -35f), 60f),
                Return = new()
                {
                    Via(42, At(Cm(-25f, 0.95f, 25f), -115f, -30f, -50f, 15f, Cm(-5f, 1.1f, 18f), Cm(-38f, 1f, -32f), 50f), 0.5f),
                    Via(56, At(Cm(-25f, 0.97f, 25f), -120f, 45f, -45f, 10f, Cm(-5f, 1.1f, 15f), Cm(-30f, 1.05f, -10f), 20f)),
                    Via(66, At(Cm(0f, 1f, 35f), -60f, 75f, -15f, 5f, Cm(10f, 1f, 15f), Cm(-25f, 1f, 0f))),
                    Via(78, upright)
                }
            };
            // Up the left side to beside the face, the blade level behind the head, then over the left shoulder down to the right.
            AttackDefinition backhand = new AttackDefinition
            {
                Windup = 30 * Footage, Active = 8 * Footage, Recovery = 56 * Footage, Damage = damage, MoveMultiplier = 0.7f, Launch = 1f,
                Raise = new()
                {
                    Via(6, At(Cm(-22f, 0.95f, 25f), -90f, -20f, -40f, 10f, Cm(0f, 1.1f, 15f), Cm(-35f, 0.95f, -30f), 30f)),
                    Via(10, At(Cm(-15f, 1.2f, 30f), -120f, 60f, -45f, 5f, Cm(0f, 1.1f, 20f), Cm(-35f, 1f, 0f))),
                    Via(16, At(Cm(-20f, 1.55f, 25f), 180f, 5f, -70f, -5f, Cm(-5f, 1.38f, 18f), Cm(-50f, 0.95f, 5f)), 0.5f),
                    Via(28, At(Cm(-20f, 1.57f, 25f), 180f, 10f, -70f, -5f, Cm(-5f, 1.38f, 18f), Cm(-50f, 0.95f, 5f)), 0.5f)
                },
                WindupPose = At(Cm(-15f, 1.57f, 25f), -160f, 15f, -65f, 0f, Cm(-5f, 1.38f, 18f), Cm(-50f, 0.95f, 5f)),
                MidPose = Peak(Cm(12f, 1.44f, 45f), -15f, 5f, Cm(15f, 1.4f, 25f), Cm(-50f, 1f, 10f), 50f),
                EndPose = At(Cm(30f, 1.05f, 35f), 60f, -45f, 20f, 10f, Cm(27f, 1.22f, 15f), Cm(-50f, 1f, 10f), 55f),
                Return = new()
                {
                    Via(40, At(Cm(42f, 0.97f, 0f), 90f, -45f, 25f, 10f, Cm(34f, 1.2f, -10f), Cm(-48f, 1f, 10f), 65f), 0.5f),
                    Via(56, outBack, 0.5f),
                    Via(74, lowForward),
                    Via(80, levelLow),
                    Via(87, upright)
                },
                After = forehand
            };
            // The arm swings out level to the right with the blade turned forward, holds, and sweeps it in before the shoulder.
            AttackDefinition thrust = new AttackDefinition
            {
                Windup = 43 * Footage, Active = 9 * Footage, Recovery = 56 * Footage, Damage = Mathf.RoundToInt(damage * 1.1f), MoveMultiplier = 0.6f, Stagger = 0.2f, Launch = 1f,
                Raise = new()
                {
                    Via(8, At(Cm(48f, 1.15f, -5f), 95f, -5f, 30f, 5f, Cm(36f, 1.28f, -10f), Cm(-35f, 1f, 5f), 50f)),
                    Via(14, At(Cm(55f, 1.4f, 0f), 60f, 5f, 30f, 0f, Cm(35f, 1.38f, -8f), Cm(-25f, 1f, 0f), 35f)),
                    Via(22, At(Cm(55f, 1.45f, 5f), 12f, 5f, 30f, 0f, Cm(35f, 1.38f, -8f), Cm(-20f, 1f, 0f)), 0.5f),
                    Via(37, At(Cm(55f, 1.45f, 5f), 12f, 5f, 30f, 0f, Cm(35f, 1.45f, -5f), Cm(-20f, 1f, 0f)), 0.5f)
                },
                WindupPose = At(Cm(22f, 1.38f, 24f), -16f, 24f, 10f, 5f, Cm(25f, 1.15f, 0f), Cm(-22f, 1f, 0f), 55f),
                MidPose = Peak(Cm(15f, 1.5f, 50f), -5f, 10f, Cm(18f, 1.42f, 25f), Cm(-25f, 0.95f, -10f), 60f),
                EndPose = At(Cm(13f, 1.53f, 57f), -16f, 24f, -10f, 15f, Cm(16f, 1.32f, 33f), Cm(-25f, 0.95f, -15f), 65f),
                Return = new()
                {
                    Via(70, At(Cm(14f, 1.47f, 52f), -14f, 26f, -5f, 10f, Cm(15f, 1.35f, 30f), Cm(-25f, 1f, -5f), 60f), 0.5f),
                    Via(80, At(Cm(15f, 1.2f, 45f), 0f, 35f, 0f, 5f, Cm(20f, 1.2f, 20f), Cm(-25f, 1f, 5f), 40f)),
                    Via(92, At(Cm(20f, 1.15f, 40f), 0f, 60f, 0f, 5f, Cm(25f, 1.05f, 15f), Cm(-25f, 1f, 5f), 15f))
                },
                After = backhand
            };
            // From the low guard up past the left of the face to behind the head, then over the top steeply down the right.
            AttackDefinition riposte = new AttackDefinition
            {
                Windup = 25 * Footage, Active = 8 * Footage, Recovery = 56 * Footage, Damage = Mathf.RoundToInt(damage * 1.5f), MoveMultiplier = 0.5f, Stagger = 0.4f, Launch = 1f,
                Raise = new()
                {
                    Via(6, At(Cm(0f, 1.3f, 35f), -90f, 70f, -25f, 5f, Cm(15f, 1.1f, 15f), Cm(-20f, 1f, 5f))),
                    Via(12, At(Cm(-10f, 1.6f, 30f), -160f, 40f, -40f, 0f, Cm(10f, 1.4f, 20f), Cm(-30f, 0.95f, 0f))),
                    Via(18, At(Cm(-20f, 1.9f, 15f), -170f, 15f, -60f, -5f, Cm(10f, 1.62f, 5f), Cm(-30f, 0.95f, 0f)), 0.5f),
                    Via(22, At(Cm(-18f, 1.9f, 18f), 180f, 20f, -55f, -5f, Cm(10f, 1.62f, 5f), Cm(-30f, 0.95f, 0f)), 0.5f)
                },
                WindupPose = At(Cm(0f, 1.75f, 28f), -130f, 55f, -40f, 0f, Cm(20f, 1.5f, 12f), Cm(-30f, 0.95f, 0f), 40f),
                MidPose = Peak(Cm(12f, 1.44f, 45f), 0f, 10f, Cm(20f, 1.38f, 25f), Cm(-35f, 1f, 5f), 50f),
                EndPose = At(Cm(25f, 1f, 35f), 30f, -70f, 20f, 15f, Cm(25f, 1.15f, 15f), Cm(-35f, 1f, 5f), 65f),
                Return = new()
                {
                    Via(38, At(Cm(28f, 0.95f, 5f), 85f, -55f, 25f, 15f, Cm(26f, 1.18f, -10f), Cm(-40f, 1f, 5f), 65f), 0.5f),
                    Via(50, outBack, 0.5f),
                    Via(66, lowForward),
                    Via(72, levelLow),
                    Via(80, upright)
                }
            };

            foreach (AttackDefinition attack in new[] { forehand, backhand, thrust, riposte })
            {
                attack.ComboStart = attack.Windup + attack.Active * 0.5f;
                attack.ComboEnd = attack.Windup + attack.Active + attack.Recovery * 0.65f;
            }

            return new WeaponDefinition
            {
                Prefix = ArmingSword,
                DisplayName = name,
                Kind = WeaponKind.OneHanded,
                Reach = reach,
                DeflectDuration = 0.6f,
                BladeBase = bladeBase,
                BladeTip = bladeTip,
                Idle = idle,
                Attacks = new[] { forehand, backhand, thrust },
                Riposte = riposte,
                CanBlock = true,
                BlockRaise = 14 * Footage,
                // The fist rises out of the bottom right while the blade sweeps like a clock hand from 12 to 9 through the upper
                // left, its tip above the guard until it lies level; the fist turns its knuckles up on the way, so the blade rolls
                // over its edge from one flat to the other and holds with the crossguard upright.
                BlockVia = new()
                {
                    PoseKey.Flow(2 * Footage, Sword(0.76f, 0.8f, 0.45f, 0.68f, -0.3f, 0.6f, 0f, lowElbow, offHand), 1f),
                    PoseKey.Flow(4 * Footage, Sword(0.74f, 0.71f, 0.45f, 0.56f, -0.2f, 0.9f, -3f, lowElbow, offHand), 1f),
                    PoseKey.Flow(7 * Footage, Sword(0.72f, 0.61f, 0.46f, 0.29f, 0.25f, -0.5f, -6f, new(0.45f, 1.3f, 0.2f), offHand), 1f),
                    PoseKey.Flow(9 * Footage, Sword(0.74f, 0.5f, 0.45f, 0.2f, 0.38f, -1f, -10f, blockElbow, offHand), 1f),
                    PoseKey.Flow(11 * Footage, Sword(0.76f, 0.45f, 0.45f, 0.27f, 0.49f, -1f, -10f, blockElbow, offHand), 0.6f)
                },
                BlockLower = 16 * Footage,
                // The raise in reverse: the flat blade swings back up through the upper left to upright as the fist drops.
                BlockLowerVia = new()
                {
                    PoseKey.Flow(3 * Footage, Sword(0.74f, 0.5f, 0.45f, 0.2f, 0.38f, -1f, -10f, blockElbow, offHand), 1f),
                    PoseKey.Flow(6 * Footage, Sword(0.72f, 0.61f, 0.46f, 0.29f, 0.25f, -0.5f, -6f, new(0.45f, 1.3f, 0.2f), offHand), 1f),
                    PoseKey.Flow(9 * Footage, Sword(0.74f, 0.71f, 0.45f, 0.56f, -0.2f, 0.9f, -3f, lowElbow, offHand), 1f),
                    PoseKey.Flow(12 * Footage, Sword(0.76f, 0.78f, 0.45f, 0.68f, -0.3f, 0.6f, 0f, lowElbow, offHand), 1f),
                    PoseKey.Flow(14 * Footage, Sword(0.73f, 0.8f, 0.45f, 0.71f, -0.3f, 0.3f, 0f, lowElbow, offHand), 0.6f)
                },
                BlockMitigation = 0.7f,
                BlockImpact = 0.28f,
                BlockRecovery = 0.4f,
                BlockAngle = 80f,
                BlockMove = 0.6f,
                // The footage's hold: the blade level across the view at eye height, its flat to the eye with the crossguard
                // upright, the tip left of the crosshair.
                Block = Sword(0.765f, 0.46f, 0.45f, 0.29f, 0.505f, -1f, -10f, blockElbow, offHand),
                BlockHit = Sword(0.765f, 0.49f, 0.45f, 0.29f, 0.535f, -1f, -10f, blockElbow, offHand),
                BlockLowered = Sword(0.765f, 0.48f, 0.45f, 0.29f, 0.525f, -1f, -10f, blockElbow, offHand),
                DeflectPose = At(Cm(34f, 1.62f, 10f), 45f, 63f, 12f, -6f, Cm(42f, 1.35f, 0f), Cm(-25f, 1f, 5f)),
                BlockSocket = WeaponSocket.RightHand,
                BlockBoxCenter = new Vector3(0f, 0f, (bladeBase + bladeTip) * 0.5f),
                BlockBoxExtents = new Vector3(0.08f, 0.08f, (bladeTip - bladeBase) * 0.5f)
            };
        }

        /// The viking sword of Dark and Darker: carried, blocked and riposted like the arming sword, with a series of its own.
        /// Three chops: straight down from behind the head over the right shoulder, a backhand from over the left shoulder
        /// down to the right, and the first again. Each stops low before the belly, and the next one starts from there.
        private static WeaponDefinition CreateVikingSword()
        {
            const int damage = 31;

            WeaponDefinition sword = CreateArmingSword("Viking Sword", 0.11f, 0.88f, 1.4f, damage);
            BodyPose upright = At(Cm(20f, 1.1f, 35f), 0f, 78f, 0f, 5f, Cm(25f, 1f, 10f), Cm(-25f, 1f, 5f), 10f);
            BodyPose cocked = At(Cm(23f, 1.83f, 15f), 175f, 25f, 25f, -5f, Cm(40f, 1.5f, -5f), Cm(-35f, 1f, 5f));
            BodyPose over = At(Cm(17f, 1.95f, 30f), 170f, 75f, 10f, 0f, Cm(35f, 1.55f, 5f), Cm(-30f, 1f, 5f));
            BodyPose peak = Peak(Cm(3f, 1.47f, 50f), -5f, 10f, Cm(18f, 1.25f, 25f), Cm(-32f, 1f, 0f), 40f);
            BodyPose low = At(Cm(5f, 1.1f, 45f), -5f, -10f, -10f, 15f, Cm(20f, 1.15f, 20f), Cm(-35f, 1f, 0f), 50f);
            List<PoseKey> back = new()
            {
                Via(56, low, 0.5f),
                Via(70, At(Cm(15f, 1.05f, 40f), -30f, 40f, -5f, 10f, Cm(25f, 1.05f, 15f), Cm(-30f, 1f, 5f), 30f)),
                Via(82, upright)
            };

            // Up the right side with the tip falling to the left, laid level over the head and round behind it.
            AttackDefinition first = new AttackDefinition
            {
                Windup = 35 * Footage, Active = 13 * Footage, Recovery = 42 * Footage, Damage = damage, MoveMultiplier = 0.7f, Launch = 1f,
                Raise = new()
                {
                    Via(4, At(Cm(25f, 1.3f, 38f), -40f, 55f, 5f, 5f, Cm(32f, 1.12f, 12f), Cm(-25f, 1f, 5f))),
                    Via(8, At(Cm(28f, 1.6f, 35f), -60f, 45f, 10f, 0f, Cm(40f, 1.35f, 10f), Cm(-35f, 1f, 5f))),
                    Via(12, At(Cm(28f, 1.9f, 28f), -115f, 12f, 15f, -5f, Cm(45f, 1.6f, 5f), Cm(-35f, 1f, 5f))),
                    Via(20, cocked, 0.5f),
                    Via(28, cocked, 0.5f)
                },
                WindupPose = over,
                MidPose = peak,
                EndPose = low,
                Return = back
            };
            // Out of the low stop the tip swings up to the left, the arm folds across over the left shoulder and chops down to the right.
            AttackDefinition backhand = new AttackDefinition
            {
                Windup = 40 * Footage, Active = 10 * Footage, Recovery = 50 * Footage, Damage = damage, MoveMultiplier = 0.7f, Launch = 1f,
                Raise = new()
                {
                    Via(8, At(Cm(5f, 1.2f, 45f), -45f, 40f, -10f, 10f, Cm(20f, 1.15f, 20f), Cm(-35f, 1f, 0f), 30f)),
                    Via(14, At(Cm(5f, 1.42f, 45f), -90f, 8f, -25f, 5f, Cm(20f, 1.3f, 15f), Cm(-40f, 1f, 0f))),
                    Via(24, At(Cm(-25f, 1.6f, 33f), -150f, 10f, -40f, 0f, Cm(10f, 1.45f, 20f), Cm(-45f, 0.95f, 5f))),
                    Via(32, At(Cm(-20f, 1.85f, 22f), -150f, 45f, -45f, -5f, Cm(10f, 1.55f, 25f), Cm(-45f, 0.95f, 5f)), 0.5f)
                },
                WindupPose = At(Cm(-15f, 1.78f, 40f), -150f, 70f, -45f, 0f, Cm(12f, 1.5f, 30f), Cm(-45f, 0.95f, 5f)),
                MidPose = Peak(Cm(8f, 1.47f, 50f), 15f, 10f, Cm(30f, 1.25f, 20f), Cm(-35f, 1f, 0f), 40f),
                EndPose = At(Cm(22f, 1.05f, 40f), 15f, -15f, 15f, 15f, Cm(27f, 1.15f, 15f), Cm(-35f, 1f, 0f), 50f),
                Return = new()
                {
                    Via(62, At(Cm(22f, 1.05f, 40f), 15f, -15f, 15f, 15f, Cm(27f, 1.15f, 15f), Cm(-35f, 1f, 0f), 50f), 0.5f),
                    Via(76, At(Cm(20f, 1.05f, 38f), -20f, 40f, 5f, 10f, Cm(26f, 1.05f, 15f), Cm(-30f, 1f, 5f), 30f)),
                    Via(90, upright)
                },
                After = first
            };
            // From the low right the blade swings up level to the left, over the head and round behind it, then the first chop again.
            AttackDefinition last = new AttackDefinition
            {
                Windup = 35 * Footage, Active = 13 * Footage, Recovery = 42 * Footage, Damage = damage, MoveMultiplier = 0.7f, Launch = 1f,
                Raise = new()
                {
                    Via(4, At(Cm(25f, 1.3f, 40f), -75f, 10f, 0f, 5f, Cm(32f, 1.15f, 15f), Cm(-35f, 1f, 0f))),
                    Via(8, At(Cm(25f, 1.7f, 35f), -95f, 10f, 10f, 0f, Cm(40f, 1.4f, 10f), Cm(-35f, 1f, 5f))),
                    Via(12, At(Cm(25f, 1.9f, 25f), -130f, 5f, 15f, -5f, Cm(45f, 1.6f, 5f), Cm(-35f, 1f, 5f))),
                    Via(20, cocked, 0.5f),
                    Via(30, cocked, 0.5f)
                },
                WindupPose = over,
                MidPose = peak,
                EndPose = low,
                Return = back,
                After = backhand
            };

            foreach (AttackDefinition attack in new[] { first, backhand, last })
            {
                attack.ComboStart = attack.Windup + attack.Active * 0.5f;
                attack.ComboEnd = attack.Windup + attack.Active + attack.Recovery * 0.65f;
            }

            sword.Prefix = VikingSword;
            sword.Attacks = new[] { first, backhand, last };

            return sword;
        }

        private static BodyPose At(Vector3 grip, float yaw, float elevation, float torso, float pitch, Vector3 elbow, Vector3 offHand, float lean = 0f)
        {
            return Pose(grip, Quaternion.Euler(-elevation, yaw, 0f) * Vector3.forward, torso, pitch, elbow, offHand, lean);
        }

        private static BodyPose Peak(Vector3 grip, float torso, float pitch, Vector3 elbow, Vector3 offHand, float lean)
        {
            return Pose(grip, Vector3.forward, torso, pitch, elbow, offHand, lean);
        }

        // The empty hand swings free where the footage has it: half open, the thumb ahead, square to its forearm,
        // which comes from an elbow that hangs out behind the left shoulder as it turns with the torso.
        private static BodyPose Pose(Vector3 grip, Vector3 blade, float torso, float pitch, Vector3 elbow, Vector3 offHand, float lean)
        {
            const float upperArm = 0.3f;
            const float forearm = 0.4f;

            Vector3 shoulder = new Vector3(0f, 1.05f, 0f) + Quaternion.Euler(pitch, torso, 0f) * new Vector3(-0.17f, 0.42f, -0.06f);
            Vector3 reach = offHand - shoulder;
            float length = Mathf.Min(reach.magnitude, upperArm + forearm);
            float near = (upperArm * upperArm - forearm * forearm + length * length) / (2f * length);
            Vector3 bend = Vector3.ProjectOnPlane(new Vector3(-0.2f, -0.5f, -0.3f), reach).normalized;
            Vector3 offElbow = shoulder + reach.normalized * near + bend * Mathf.Sqrt(Mathf.Max(upperArm * upperArm - near * near, 0f));

            BodyPose pose = BattleAnimationLibrary.Elbows(BattleAnimationLibrary.OneHanded(grip, blade, torso, pitch), elbow);
            pose.Off = new HandPose(offHand, Vector3.ProjectOnPlane(new Vector3(0.2f, 0f, 1f), offHand - offElbow), offHand - offElbow);
            pose.OffOpen = 0.5f;
            pose.Lean = lean;

            return pose;
        }

        /// A one-handed weapon with a shield on the other arm. The swings and the riposte are the weapon's own; the shield
        /// arm keeps the shield where the shield entry carries it through a swing, turned with the torso, and at rest where
        /// it rests. The block is the shield's, the weapon hand where the shield entry has it, back to the weapon's own rest.
        private static WeaponDefinition WithShield(WeaponDefinition weapon, WeaponDefinition shield, string prefix, string shieldName)
        {
            BodyPose carry = shield.Attacks[0].MidPose;
            List<AttackDefinition> attacks = new(weapon.Attacks);
            // Both are taken out at once, the weapon over the right shoulder and the shield over the left.
            List<PoseKey> draw = weapon.EquipVia.Count > 0 ? weapon.EquipVia : OverShoulder(weapon.Idle);
            Vector3 mirror = new(-1f, 1f, 1f);

            for (int i = 0; i < draw.Count; i++)
            {
                PoseKey key = draw[i];
                bool isRest = i == 0 || i == draw.Count - 1;
                key.Pose = Carry(key.Pose, isRest ? shield.Idle : ShieldOverShoulder(shield.Idle, Vector3.Scale(s_overShoulder[i - 1].elbow, mirror),
                    Vector3.Scale(s_overShoulder[i - 1].hand, mirror), s_overShoulder[i - 1].elevation));
                draw[i] = key;
            }

            weapon.EquipVia = draw;
            weapon.EquipShown = DrawReach;

            if (weapon.Riposte != null)
                attacks.Add(weapon.Riposte);

            foreach (AttackDefinition attack in attacks)
            {
                attack.WindupPose = Carry(attack.WindupPose, carry);
                attack.MidPose = Carry(attack.MidPose, carry);
                attack.EndPose = Carry(attack.EndPose, carry);
                CarryKeys(attack.Raise, carry);
                CarryKeys(attack.Return, carry);
            }

            weapon.Prefix = prefix;
            weapon.DisplayName = $"{weapon.DisplayName} & {shieldName}";
            weapon.Idle = Carry(weapon.Idle, shield.Idle);
            weapon.IdleBreath = Carry(weapon.IdleBreath, shield.Idle);
            weapon.DeflectPose = Carry(weapon.DeflectPose, shield.Idle);
            weapon.CanBlock = shield.CanBlock;
            weapon.BlockRaise = shield.BlockRaise;
            weapon.BlockMitigation = shield.BlockMitigation;
            weapon.BlockImpact = shield.BlockImpact;
            weapon.BlockRecovery = shield.BlockRecovery;
            weapon.BlockAngle = shield.BlockAngle;
            weapon.BlockMove = shield.BlockMove;
            weapon.Block = shield.Block;
            weapon.BlockVia = shield.BlockVia;
            weapon.BlockLower = shield.BlockLower;
            weapon.BlockLowerVia = shield.BlockLowerVia;
            weapon.BlockHit = shield.BlockHit;
            weapon.BlockLowered = shield.BlockLowered;
            weapon.BlockSocket = shield.BlockSocket;
            weapon.BlockBoxCenter = shield.BlockBoxCenter;
            weapon.BlockBoxExtents = shield.BlockBoxExtents;

            // Let go, the weapon comes back up into its own rest once the shield is on its way down.
            for (int i = 1; i < weapon.BlockLowerVia.Count; i++)
            {
                PoseKey key = weapon.BlockLowerVia[i];
                key.Pose.Main = weapon.Idle.Main;
                key.Pose.Edge = weapon.Idle.Edge;
                key.Pose.Lean = weapon.Idle.Lean;
                weapon.BlockLowerVia[i] = key;
            }

            return weapon;
        }

        private static void CarryKeys(List<PoseKey> keys, BodyPose shield)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                PoseKey key = keys[i];
                key.Pose = Carry(key.Pose, shield);
                keys[i] = key;
            }
        }

        /// The weapon pose with the shield arm of 'shield', turned about the spine from the torso of 'shield' to its own.
        private static BodyPose Carry(BodyPose pose, BodyPose shield)
        {
            if (!pose.HasHands)
                return pose;

            Vector3 spine = new(0f, 1.05f, 0f);
            Quaternion turn = Quaternion.Euler(pose.Spine) * Quaternion.Inverse(Quaternion.Euler(shield.Spine));
            HandPose off = shield.Off;
            off.Position = spine + turn * (off.Position - spine);
            off.Forward = turn * off.Forward;
            off.Up = turn * off.Up;
            off.Elbow = off.ElbowWeight > 0f ? spine + turn * (off.Elbow - spine) : off.Elbow;
            pose.Off = off;
            pose.OffSocket = shield.OffSocket;
            pose.OffOpen = shield.OffOpen;

            return pose;
        }

        /// The écu of Dark and Darker: its rest and its block; through a swing it rides on the arm like the round shield,
        /// behind the arm and out of the view.
        /// The footage carries a mace: at rest it stands upright before the right chest with the écu low at the left, its
        /// top just in the corner of the view. The block lifts the écu in a straight line to cover the face up to the eyes,
        /// face ahead and point down, while the weapon drops out of the view by the right hip; let go, it comes down the
        /// same way, slower, sinks a little below rest and settles.
        private static WeaponDefinition CreateEcu()
        {
            Vector3 restShield = new(-0.3f, 1.33f, 0.52f);
            Vector3 restNormal = new(-0.14f, 0f, 0.99f);
            Vector3 restTop = new(-0.09f, 1f, 0f);
            Vector3 rightElbow = new(0.25f, 1.15f, 0.1f);
            Vector3 dropped = new(0.26f, 1f, 0.12f);
            Vector3 droppedBlade = new(0.1f, 0.97f, 0.12f);
            Vector3 face = new(0f, 0.09f, 1f);
            Vector3 top = new(0f, 1f, -0.09f);

            WeaponDefinition definition = BattleAnimationLibrary.CreateSwordShield();
            definition.Idle = EcuPose(new(0.12f, 1.38f, 0.42f), new(0.06f, 0.99f, 0.15f), restShield, restNormal, restTop, rightElbow);
            definition.BlockRaise = 12 * Footage;
            definition.BlockVia = new()
            {
                new(2 * Footage, EcuPose(new(0.2f, 1.25f, 0.36f), new(0.1f, 0.95f, 0.3f), new(-0.21f, 1.4f, 0.53f), face, top, rightElbow), Ease.Linear),
                new(4 * Footage, EcuPose(new(0.23f, 1.12f, 0.27f), new(0f, 0.9f, 0.43f), new(-0.15f, 1.44f, 0.53f), face, top, new(0.27f, 1.12f, 0.03f)), Ease.Linear),
                new(6 * Footage, EcuPose(dropped, droppedBlade, new(-0.07f, 1.46f, 0.53f), face, top, new(0.28f, 1.15f, 0f)), Ease.Linear)
            };
            definition.Block = EcuPose(dropped, droppedBlade, new(0.02f, 1.49f, 0.56f), face, top, new(0.28f, 1.15f, 0f));
            definition.BlockHit = EcuPose(dropped, droppedBlade, new(0.02f, 1.48f, 0.52f), new(0f, 0.2f, 1f), new(0f, 1f, -0.2f), new(0.28f, 1.15f, 0f));
            definition.BlockLowered = EcuPose(dropped, droppedBlade, new(0.02f, 1.46f, 0.54f), face, top, new(0.28f, 1.15f, 0f));
            definition.BlockLower = 30 * Footage;
            definition.BlockLowerVia = new()
            {
                new(9 * Footage, EcuPose(new(0.18f, 1.2f, 0.3f), new(-0.26f, 0.95f, 0.2f), new(-0.21f, 1.36f, 0.53f), face, top, rightElbow), Ease.Linear),
                new(14 * Footage, EcuPose(new(0.14f, 1.33f, 0.4f), new(-0.2f, 0.96f, 0.15f), new(-0.3f, 1.31f, 0.52f), restNormal, restTop, rightElbow)),
                new(20 * Footage, EcuPose(new(0.13f, 1.36f, 0.41f), new(-0.05f, 0.99f, 0.15f), new(-0.27f, 1.35f, 0.52f), restNormal, restTop, rightElbow))
            };

            return definition;
        }

        /// The off hand on the écu's grip with its board's middle at 'shield', the face toward 'normal' and its top toward
        /// 'top'. The straps set the forearm, so the elbow is where they put it.
        private static BodyPose EcuPose(Vector3 grip, Vector3 blade, Vector3 shield, Vector3 normal, Vector3 top, Vector3 elbow)
        {
            const float forearm = 0.3f;
            const float backOfHand = 0.07f;
            Quaternion board = Quaternion.LookRotation(normal, top);
            Quaternion socket = board * Quaternion.Inverse(DungeonWeaponPrefabBuilder.EcuTurn);
            Vector3 fist = shield + board * DungeonWeaponPrefabBuilder.EcuFist;
            BodyPose pose = BattleAnimationLibrary.SwordShield(grip, blade, shield, normal);
            pose = BattleAnimationLibrary.Elbows(pose, elbow, fist - socket * (Vector3.forward * backOfHand + Vector3.right * forearm));
            pose.Off.Position = fist;
            pose.Off.Forward = socket * Vector3.forward;
            pose.Off.Up = socket * Vector3.up;

            return pose;
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

        /// A point of the footage in metres: centimetres to the right of the body line, height, centimetres ahead of the body.
        private static Vector3 Cm(float side, float height, float depth)
        {
            return new Vector3(side * 0.01f, height, 0.1f + depth * 0.01f);
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

        private static WeaponDefinition CreateStaff()
        {
            // Carried like a walking stick in the right hand, a third of the way down from the head; only the riposte
            // takes it in both hands and strikes with the butt.
            BodyPose idle = PoseAt(new(0.25f, 1.18f, 0.38f), 0f, 80f, elbow: new(0.2f, 1f, 0.05f));
            // The round haft slides obliquely in the fist where it points along the forearm.
            BodyPose Leaned(BodyPose pose, float lean)
            {
                pose.Lean = lean;

                return pose;
            }

            BodyPose parry = PoseAt(new(0.17f, 1.72f, 0.4f), 96f, 15f, offHand: StaffBlockHands, elbow: new(0.2f, 1.4f, 0.22f), offElbow: new(-0.3f, 1.3f, 0.15f));

            AttackDefinition backhand = new AttackDefinition
            {
                Windup = 34 * Footage, Active = 7 * Footage, Recovery = 55 * Footage, Damage = 29, MoveMultiplier = 0.5f, Stagger = 0.25f, Launch = 1f,
                Raise = new()
                {
                    Via(8, PoseAt(new(0.02f, 1.32f, 0.3f), -40f, 72f, -10f, elbow: new(0.15f, 1.1f, 0.1f))),
                    Via(13, PoseAt(new(-0.04f, 1.47f, 0.33f), -60f, 65f, -20f, elbow: new(0.12f, 1.35f, 0.2f))),
                    Via(26, Leaned(PoseAt(new(-0.25f, 1.6f, 0.1f), -160f, 60f, -35f, elbow: new(0f, 1.4f, 0.25f)), StaffLean), 0.4f),
                    Via(32, Leaned(PoseAt(new(-0.25f, 1.62f, 0.08f), -160f, 62f, -35f, elbow: new(0f, 1.42f, 0.25f)), StaffLean), 0.5f)
                },
                WindupPose = Leaned(PoseAt(new(-0.1f, 1.55f, 0.22f), -110f, 58f, -25f, elbow: new(0.05f, 1.3f, 0.25f)), StaffLean * 0.5f),
                MidPose = BattleAnimationLibrary.Elbows(BattleAnimationLibrary.OneHanded(new(0.1f, 1.44f, 0.6f), Vector3.forward, 10f), new(0.15f, 1.45f, 0.3f)),
                EndPose = Leaned(PoseAt(new(0.38f, 1.42f, 0.52f), -10f, 15f, 15f, elbow: new(0.35f, 1.45f, 0.25f)), StaffLean),
                Return = new()
                {
                    Via(44, Leaned(PoseAt(new(0.4f, 1.43f, 0.52f), 0f, 10f, 15f, elbow: new(0.35f, 1.45f, 0.25f)), StaffLean), 0.5f), Via(70, Leaned(PoseAt(new(0.4f, 1.43f, 0.5f), 0f, 12f, 10f, elbow: new(0.35f, 1.45f, 0.25f)), StaffLean), 0.5f),
                    Via(80, PoseAt(new(0.38f, 1.3f, 0.48f), 0f, 20f, 8f, elbow: new(0.28f, 1.1f, 0.1f))), Via(88, PoseAt(new(0.36f, 1.24f, 0.42f), 0f, 55f, 5f, elbow: new(0.24f, 1.03f, 0.06f)))
                }
            };
            AttackDefinition chop = new AttackDefinition
            {
                Windup = 55 * Footage, Active = 9 * Footage, Recovery = 58 * Footage, Damage = 32, MoveMultiplier = 0.5f, Stagger = 0.25f, Launch = 1f,
                Raise = new()
                {
                    // The hand rises in front of the right side of the crown with the head hanging forward-left, then the
                    // head swings up and over to lie behind the head.
                    Via(8, Leaned(PoseAt(new(0.33f, 1.56f, 0.42f), -10f, 0f, 5f, elbow: new(0.3f, 1.38f, 0.2f)), StaffLean)),
                    Via(14, Leaned(PoseAt(new(0.27f, 1.7f, 0.36f), -25f, -8f, elbow: new(0.3f, 1.42f, 0.2f)), StaffLean)),
                    Via(20, PoseAt(new(0.24f, 1.78f, 0.37f), -40f, -12f, elbow: new(0.32f, 1.45f, 0.18f))),
                    Via(28, PoseAt(new(0.23f, 1.79f, 0.37f), -46f, -15f, elbow: new(0.32f, 1.45f, 0.18f)), 0.4f),
                    Via(41, PoseAt(new(0.14f, 1.88f, 0.24f), -55f, 0f, elbow: new(0.42f, 1.6f, 0.15f))),
                    Via(46, PoseAt(new(0.1f, 1.88f, 0f), -70f, 10f, elbow: new(0.45f, 1.68f, 0.2f)), 0.4f),
                    Via(49, PoseAt(new(0.12f, 1.88f, 0.02f), -68f, 8f, elbow: new(0.45f, 1.68f, 0.2f)), 0.5f),
                    Via(52.5f, PoseAt(new(0.2f, 1.98f, 0.12f), -75f, 30f, elbow: new(0.42f, 1.65f, 0.2f))), Via(54, PoseAt(new(0.2f, 1.85f, 0.25f), -50f, 70f, elbow: new(0.3f, 1.75f, 0.1f)))
                },
                WindupPose = PoseAt(new(0.22f, 1.7f, 0.4f), 0f, 80f, elbow: new(0.25f, 1.78f, 0.05f)),
                MidPose = BattleAnimationLibrary.Elbows(BattleAnimationLibrary.OneHanded(new(0.12f, 1.44f, 0.5f), Vector3.forward), new(0.15f, 1.32f, 0.25f)),
                EndPose = Leaned(PoseAt(new(0f, 1.38f, 0.53f), -10f, 20f, elbow: new(0.1f, 1.25f, 0.25f)), StaffLean),
                Return = new()
                {
                    Via(68, Leaned(PoseAt(new(0f, 1.37f, 0.53f), -12f, 18f, elbow: new(0.1f, 1.24f, 0.25f)), StaffLean), 0.5f), Via(92, Leaned(PoseAt(new(0.02f, 1.28f, 0.52f), -15f, 5f, elbow: new(0.1f, 1.15f, 0.25f)), StaffLean), 0.5f),
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
                BlockHit = PoseAt(new(0.16f, 1.69f, 0.35f), 96f, 17f, pitch: -5f, offHand: StaffBlockHands, elbow: new(0.2f, 1.38f, 0.17f), offElbow: new(-0.3f, 1.27f, 0.1f)),
                BlockLowered = PoseAt(new(0.17f, 1.65f, 0.4f), 96f, 10f, offHand: StaffBlockHands, elbow: new(0.2f, 1.35f, 0.22f), offElbow: new(-0.3f, 1.22f, 0.15f)),
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

        /// The recurve bow of D:\recurvebow_*.mp4, first person (Docs/Tasks/BowAnimations.md). Carried low in the left fist, the upper
        /// limb leaning right up through the view, the right hand off it. Taken up from the hip on the quiver side and swung up into
        /// the idle; blocked flat across the chest, the right fist round the upper limb by the tip. A draw lowers and flattens the bow
        /// to nock, then pulls it up canted to the anchor; a shot keeps the bow still a moment and lowers it back to the idle.
        private static WeaponDefinition CreateBow()
        {
            float draw = BattleAnimationLibrary.FullDrawTime / 56f;
            float release = BattleAnimationLibrary.ReloadTime / 44f;
            Vector3 rest = new(0.2f, 1.05f, 0.15f);
            Vector3 anchor = new(0.06f, 1.5f, 0.1f);
            Vector3 low = new(-0.35f, 1.25f, 0.25f);
            Vector3 drawElbow = new(0.32f, 1.55f, -0.15f);
            Vector3 bowElbow = new(-0.3f, 1.45f, 0.3f);
            Vector3 pullElbow = new(-0.34f, 1.4f, 0.28f);
            Vector3 blockElbow = new(0.42f, 1.66f, 0.25f);
            Vector3 blockOffElbow = new(-0.38f, 1.35f, 0.28f);
            Vector3 quiverElbow = new(0.42f, 1.35f, 0.2f);
            Vector3 reachElbow = new(0.4f, 1.5f, 0.2f);
            Vector3 hipElbow = new(-0.42f, 1.25f, 0.05f);
            BodyPose idle = BowPose(OnScreen(0.468f, 0.869f, 0.396f), 43.5f, 15f, rest, default, low);
            BodyPose drawn = BowPose(OnScreen(0.51f, 0.55f, 0.435f), 38f, 0f, anchor, drawElbow, bowElbow);
            BodyPose loosed = BowPose(OnScreen(0.505f, 0.56f, 0.435f), 35f, 0f, new(0.18f, 1.38f, 0.2f), default, bowElbow, open: 0.8f);

            WeaponDefinition definition = BattleAnimationLibrary.CreateBow();
            definition.Idle = idle;
            definition.EquipShown = Footage;
            definition.EquipVia = new()
            {
                new(0f, BowPose(new(0.05f, 1f, 0.35f), 70f, 30f, rest, default, low)),
                PoseKey.Flow(2 * Footage, BowPose(OnScreen(0.45f, 1.19f, 0.42f), 55f, 15f, rest, default, low), 1f),
                PoseKey.Flow(8 * Footage, BowPose(OnScreen(0.31f, 1.19f, 0.366f), 54f, 15f, rest, default, low), 1f),
                PoseKey.Flow(12 * Footage, BowPose(OnScreen(0.464f, 0.956f, 0.417f), 51f, 15f, rest, default, low), 1f),
                PoseKey.Flow(16 * Footage, BowPose(OnScreen(0.47f, 0.871f, 0.417f), 48f, 15f, rest, default, low), 0.8f),
                PoseKey.Flow(22 * Footage, BowPose(OnScreen(0.465f, 0.837f, 0.418f), 51f, 15f, rest, default, low), 0.5f),
                new(32 * Footage, idle)
            };
            definition.CanBlock = true;
            definition.BlockRaise = 20 * Footage;
            definition.BlockLower = 26 * Footage;
            definition.BlockMitigation = 0.6f;
            definition.BlockImpact = 0.3f;
            definition.BlockRecovery = 0.4f;
            definition.BlockAngle = 70f;
            definition.BlockMove = 0.5f;
            definition.Block = BowPose(OnScreen(0.255f, 0.669f, 0.32f), 61f, 14f, default, blockElbow, blockOffElbow, 0.5f);
            definition.BlockHit = BowPose(OnScreen(0.255f, 0.69f, 0.3f), 61f, 14f, default, blockElbow, blockOffElbow, 0.5f);
            definition.BlockLowered = BowPose(OnScreen(0.26f, 0.7f, 0.31f), 61f, 14f, default, blockElbow, blockOffElbow, 0.5f);
            // The bow pivots round the left fist, which dips and goes left-up, before the right hand closes on the limb.
            definition.BlockVia = new()
            {
                PoseKey.Flow(6 * Footage, BowPose(OnScreen(0.422f, 0.863f, 0.322f), 42f, 18f, new(0.32f, 1.35f, 0.25f), default, pullElbow), 1f),
                PoseKey.Flow(12 * Footage, BowPose(OnScreen(0.279f, 0.751f, 0.314f), 60f, 14f, default, blockElbow, blockOffElbow, 0.5f), 1f)
            };
            definition.BlockLowerVia = new()
            {
                PoseKey.Flow(4 * Footage, BowPose(OnScreen(0.353f, 0.766f, 0.292f), 48f, 17.5f, new(0.3f, 1.62f, 0.4f), default, bowElbow, open: 0.8f), 1f),
                PoseKey.Flow(10 * Footage, BowPose(OnScreen(0.459f, 0.861f, 0.43f), 24.5f, 15f, rest, default, low), 1f),
                PoseKey.Flow(18 * Footage, BowPose(OnScreen(0.447f, 0.866f, 0.377f), 45f, 16f, rest, default, low), 0.5f)
            };
            definition.BlockSocket = WeaponSocket.LeftHand;
            definition.BlockBoxCenter = new Vector3(0f, -0.05f, 0.2f);
            definition.BlockBoxExtents = new Vector3(0.15f, 0.15f, 0.5f);
            definition.DrawPose = drawn;
            definition.ReleasePose = loosed;
            definition.DrawVia = new()
            {
                PoseKey.Flow(6 * draw, BowPose(OnScreen(0.47f, 1f, 0.36f), 62f, 15f, new(0.2f, 1.35f, 0.3f), default, low), 1f),
                PoseKey.Flow(12 * draw, BowPose(OnScreen(0.45f, 1.1f, 0.36f), 72f, 12f, OnScreen(0.64f, 0.41f, 0.3f), default, low), 1f),
                PoseKey.Flow(20 * draw, BowPose(OnScreen(0.51f, 0.81f, 0.38f), 65f, 15f, OnScreen(0.59f, 0.58f, 0.3f), default, new(-0.33f, 1.36f, 0.28f)), 1f),
                PoseKey.Flow(30 * draw, BowPose(OnScreen(0.49f, 0.69f, 0.38f), 60f, 8f, new(0.14f, 1.5f, 0.28f), drawElbow, pullElbow), 1f),
                PoseKey.Flow(40 * draw, BowPose(OnScreen(0.51f, 0.65f, 0.42f), 48f, 3f, new(0.09f, 1.5f, 0.15f), drawElbow, pullElbow), 0.8f)
            };
            definition.ReleaseVia = new()
            {
                new(0f, drawn),
                new(4 * release, loosed, Ease.Out),
                new(10 * release, loosed),
                PoseKey.Flow(14 * release, BowPose(OnScreen(0.5f, 0.6f, 0.381f), 48f, 15f, rest, default, low), 1f),
                PoseKey.Flow(28 * release, BowPose(OnScreen(0.507f, 0.778f, 0.45f), 36f, 15f, rest, default, low), 0.6f),
                new(44 * release, idle)
            };
            // D:\bow_reload.mp4 from frame 26: the bow drops low and upright, the right hand reaches over the shoulder into the back
            // quiver, pulls a fistful of arrows out past the right of the view and lowers them down to the hip; the bow swings back up.
            BodyPose Lowered(Vector3 right, Vector3 elbow = default) => BowPose(new(-0.46f, 1.11f, 0.35f), 36f, 30f, right, elbow, hipElbow);
            // The raised fist points back at the quiver, square to the upright forearm.
            BodyPose Reach(BodyPose pose)
            {
                pose.Main.Forward = new Vector3(0f, 0.3f, -1f);

                return pose;
            }

            definition.QuiverSize = 10;
            definition.ReloadVia = new()
            {
                new(0f, idle),
                PoseKey.Flow(4 * Footage, BowPose(OnScreen(0.44f, 0.93f, 0.4f), 38f, 15f, rest, default, low), 1f),
                PoseKey.Flow(10 * Footage, BowPose(new(-0.45f, 1.17f, 0.35f), 36f, 25f, new(0.32f, 1.45f, 0.2f), default, hipElbow), 1f),
                PoseKey.Flow(18 * Footage, BowPose(new(-0.48f, 1.12f, 0.35f), 38f, 32f, new(0.33f, 1.5f, 0.38f), quiverElbow, hipElbow), 1f),
                PoseKey.Flow(24 * Footage, Reach(Lowered(new(0.26f, 1.78f, 0.05f), reachElbow)), 1f),
                PoseKey.Flow(27 * Footage, Lowered(OnScreen(0.7f, 0.3f, 0.25f), new(0.32f, 1.52f, 0.4f)), 1f),
                PoseKey.Flow(32 * Footage, Lowered(OnScreen(0.72f, 0.65f, 0.3f), quiverElbow), 1f),
                PoseKey.Flow(36 * Footage, Lowered(OnScreen(0.72f, 0.95f, 0.3f), quiverElbow), 0.6f),
                PoseKey.Flow(60 * Footage, Lowered(new(0.25f, 1.05f, 0.2f)), 0.6f),
                PoseKey.Flow(100 * Footage, Lowered(rest), 0.5f),
                PoseKey.Flow(128 * Footage, Lowered(rest), 0.5f),
                PoseKey.Flow(148 * Footage, BowPose(OnScreen(0.38f, 0.95f, 0.4f), 40f, 15f, rest, default, low), 1f),
                new(159 * Footage, idle)
            };

            return definition;
        }

        /// A bow pose: the left fist on the grip, the stave leaned ahead by lean and rolled by roll, + the top to the right, the
        /// string toward the eye. The right hand at the given point, or with hold > 0 round the upper limb at that length from the
        /// grip, the bow then lying with its string down.
        private static BodyPose BowPose(Vector3 grip, float roll, float lean, Vector3 right, Vector3 elbow = default, Vector3 offElbow = default,
            float hold = 0f, float open = 0.3f)
        {
            Vector3 stave = Quaternion.AngleAxis(-roll, Vector3.forward) * Quaternion.AngleAxis(lean, Vector3.right) * Vector3.up;
            Vector3 face = hold > 0f ? new Vector3(0f, 0.6f, 1f) : Vector3.forward;
            BodyPose pose = BattleAnimationLibrary.Bow(grip, stave, Vector3.ProjectOnPlane(face, stave).normalized, hold > 0f ? grip + stave * hold : right);

            if (hold > 0f)
                pose.Main.Forward = -stave;

            pose.MainOpen = hold > 0f ? 0f : open;

            return BattleAnimationLibrary.Elbows(pose, elbow, offElbow);
        }

        /// The crossbow of D:\crossbow_*.mp4, first person. Carried level low in the right hand, the prod across the bottom of
        /// the view and the left hand off it. Taken up from the lower right rolled over and levelled down into the idle;
        /// blocked with the prod stood on end at the left of the view, the left hand on the fore-stock. A shot kicks it back
        /// a little; the reload tips the muzzle down, the left hand draws the string from the prod back to the nut and lays
        /// a bolt in the groove. In the footage the right hand cocks it, here it keeps the grip the weapon hangs on.
        private static WeaponDefinition CreateCrossbow()
        {
            float reload = 1.8f / 139f;
            BodyPose idle = CrossbowPose(new(0.07f, 1.42f, 0.33f), -2f, 2f, 0f);
            Vector3 rightElbow = new(0.25f, 1.25f, 0.1f);
            Vector3 leftElbow = new(-0.2f, 1.25f, 0.3f);
            BodyPose Cocking(float along) => CrossbowPose(new(0.04f, 1.52f, 0.25f), -8f, -20f, 12f, along, rightElbow, leftElbow);

            WeaponDefinition definition = BattleAnimationLibrary.CreateBow();
            definition.Prefix = Crossbow;
            definition.DisplayName = "Crossbow";
            definition.Idle = idle;
            definition.DrawPose = idle;
            definition.ReleasePose = idle;
            definition.EquipShown = 10 * Footage;
            definition.EquipVia = new()
            {
                new(0f, CrossbowPose(new(0.25f, 1.05f, 0.2f), -10f, -25f, 40f)),
                PoseKey.Flow(10 * Footage, CrossbowPose(new(0.28f, 1.18f, 0.22f), -10f, 15f, 65f), 1f),
                PoseKey.Flow(22 * Footage, CrossbowPose(new(0.22f, 1.42f, 0.3f), -10f, 35f, 65f), 1f),
                PoseKey.Flow(30 * Footage, CrossbowPose(new(0.14f, 1.45f, 0.33f), -5f, 15f, 40f), 1f),
                PoseKey.Flow(40 * Footage, CrossbowPose(new(0.09f, 1.44f, 0.33f), -3f, 4f, 12f), 0.8f),
                PoseKey.Flow(48 * Footage, CrossbowPose(new(0.07f, 1.42f, 0.33f), -2f, 1f, 3f), 0.6f),
                new(59 * Footage, idle)
            };
            definition.CanBlock = true;
            definition.BlockRaise = 18 * Footage;
            definition.BlockLower = 18 * Footage;
            definition.BlockMitigation = 0.6f;
            definition.BlockImpact = 0.3f;
            definition.BlockRecovery = 0.4f;
            definition.BlockAngle = 70f;
            definition.BlockMove = 0.5f;
            definition.Block = CrossbowPose(new(0.15f, 1.52f, 0.37f), -78f, 22f, -90f, 0.42f, default, new(-0.25f, 1.45f, 0.35f));
            definition.BlockHit = CrossbowPose(new(0.15f, 1.48f, 0.33f), -78f, 20f, -90f, 0.42f, default, new(-0.25f, 1.42f, 0.32f));
            definition.BlockLowered = CrossbowPose(new(0.15f, 1.45f, 0.35f), -78f, 18f, -90f, 0.42f, default, new(-0.25f, 1.4f, 0.33f));
            // On the way up the left hand turns its knuckles from the floor to the front before it closes on the stock.
            BodyPose rising = CrossbowPose(new(0.11f, 1.47f, 0.36f), -40f, 12f, -45f, 0.3f, default, new(-0.25f, 1.3f, 0.3f));
            rising.Off.Up = Vector3.ProjectOnPlane(new Vector3(0f, 0.3f, 1f), rising.Off.Forward).normalized;
            definition.BlockVia = new() { PoseKey.Flow(8 * Footage, rising, 1.2f) };
            definition.BlockSocket = WeaponSocket.RightHand;
            definition.BlockBoxCenter = new Vector3(0f, 0.175f, -0.1f);
            definition.BlockBoxExtents = new Vector3(0.25f, 0.3f, 0.25f);
            definition.ReleaseVia = new()
            {
                new(0f, idle),
                new(2 * Frame, CrossbowPose(new(0.07f, 1.45f, 0.33f), -2f, 4f, 0f), Ease.Out),
                new(6 * Frame, CrossbowPose(new(0.07f, 1.425f, 0.3f), -2f, 1f, 0f)),
                new(CombatComponent.RecoilTime, idle)
            };
            definition.ReloadVia = new()
            {
                new(0f, idle),
                PoseKey.Flow(14 * reload, Cocking(0.3f), 0.6f),
                PoseKey.Flow(24 * reload, Cocking(0.34f), 0.6f),
                PoseKey.Flow(40 * reload, Cocking(0.25f), 1f),
                PoseKey.Flow(60 * reload, Cocking(0.12f), 1f),
                PoseKey.Flow(77 * reload, Cocking(0.08f), 0.5f),
                PoseKey.Flow(89 * reload, WithBolt(CrossbowPose(new(0.09f, 1.47f, 0.3f), -4f, -10f, 0f), new(-0.08f, 1.28f, 0.3f)), 1f),
                PoseKey.Flow(112 * reload, WithBolt(CrossbowPose(new(0.08f, 1.46f, 0.31f), -3f, -8f, 0f), new(0f, 1.47f, 0.42f)), 0.6f),
                PoseKey.Flow(126 * reload, WithBolt(CrossbowPose(new(0.07f, 1.45f, 0.32f), -2f, -2f, 0f), new(0.03f, 1.49f, 0.52f)), 0.6f),
                new(139 * reload, idle)
            };
            definition.DrawTime = 0.3f;
            definition.ReloadTime = 139 * reload;
            definition.IsManualReload = true;
            definition.ArrowMinSpeed = 36f;
            definition.ArrowMaxSpeed = 40f;
            definition.ArrowMinDamage = 30;
            definition.ArrowMaxDamage = 39;

            return definition;
        }

        /// A crossbow pose from its stock: the grip point, the muzzle as yaw and elevation, and roll about the stock, + raising
        /// the right tip of the prod (the top then faces left). The fist holds the grip as DungeonWeaponPrefabBuilder.CrossbowInHand
        /// has it; its forearm comes up from an elbow below and behind it. An off hand that far up the fore-stock wraps it from
        /// above, the thumb toward the butt.
        private static BodyPose CrossbowPose(Vector3 grip, float yaw, float elevation, float roll, float offHand = 0f, Vector3 elbow = default,
            Vector3 offElbow = default)
        {
            Vector3 stock = Quaternion.Euler(-elevation, yaw, 0f) * Vector3.forward;
            Vector3 top = Quaternion.AngleAxis(roll, stock) * Vector3.ProjectOnPlane(Vector3.up, stock).normalized;
            float angle = DungeonWeaponPrefabBuilder.CrossbowGrip * Mathf.Deg2Rad;
            Vector3 fist = grip - top * 0.035f;
            BodyPose pose = BattleAnimationLibrary.OneHanded(fist, -Mathf.Sin(angle) * stock + Mathf.Cos(angle) * top, 0f, 0f);
            pose.Edge = Mathf.Cos(angle) * stock + Mathf.Sin(angle) * top;

            pose.Off = offHand > 0f
                ? new HandPose(grip + stock * offHand + top * 0.03f, -stock, Vector3.ProjectOnPlane(Vector3.up, stock).normalized)
                : new HandPose(pose.Off.Position, new Vector3(0.15f, 0f, 1f), new Vector3(0f, -1f, 0.15f));

            return BattleAnimationLibrary.Elbows(pose, elbow == default ? fist + new Vector3(0.13f, -0.24f, -0.23f) : elbow, offElbow);
        }

        /// The free left hand pinching a bolt at the given point, palm down, the fingers forward.
        private static BodyPose WithBolt(BodyPose pose, Vector3 hand)
        {
            pose.Off = new HandPose(hand, new Vector3(0.95f, 0f, -0.33f), new Vector3(0.3f, 0.4f, 0.85f));
            pose.OffOpen = 0.3f;

            return pose;
        }
    }
}
