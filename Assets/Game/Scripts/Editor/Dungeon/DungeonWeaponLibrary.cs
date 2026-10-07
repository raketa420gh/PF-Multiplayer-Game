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

        private const float MaceHead = 0.6f;
        private const float AxeHead = 0.8f;
        /// A long axe is held at the butt with the off hand, this far down the haft from the main hand.
        private const float AxeGrip = -0.42f;
        private const float Frame = 1f / BattleAnimationBuilder.FrameRate;
        /// A frame of 60 fps footage of Dark and Darker. A fight there runs at full pace and ours at the base action
        /// speed of the fighters, so a clip is that much shorter than what it is copied from.
        private const float Footage = 0.75f * Frame;
        private const float StaffHands = -0.55f;
        private const float StaffReach = -0.62f;
        private const float StaffButt = -0.9f;
        private const float StaffButtEnd = -1.2f;
        /// From the spine of the shut book to its fore-edge: one hand holds each.
        private const float BookWidth = 0.17f;

        /// Catalog order = combat catalog index. Battle prefab slots 1-4 map to the first four entries; monsters find their
        /// weapon by name.
        public static readonly string[] CatalogOrder =
        {
            SwordShield, Bow, SwordShieldLeft, Fists, ArmingSword, BattleAxe, Crossbow, Staff, MaceShield, Spellbook, BearClaws, PantherClaws, RatBite, MorningStar, SwordEcu, MaceEcu
        };

        /// The entry whose shield is the model of a shield item, by its ShieldIndex: the round shield, the écu.
        public static readonly string[] ShieldModels = { SwordShield, SwordEcu };

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
            ["Sword & Shield"] = (4, 7), ["Mace & Shield"] = (6, 7), ["Battle Axe"] = (7, 4), ["Bow"] = (3, 1), ["Crossbow"] = (5, 1), ["Panther Claws"] = (4, 2),
            ["Bear Claws"] = (7, 4), ["Sword & Écu"] = (4, 8), ["Mace & Écu"] = (6, 8)
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
                BattleAnimationLibrary.CreateBow(),
                CreateFists(),
                CreateArmingSword("Arming Sword", 0.12f, 0.9f, 1.4f, 27),
                CreateBattleAxe(),
                CreateCrossbow(),
                CreateStaff(),
                CreateMaceShield(),
                CreateSpellbook(),
                CreateBearClaws(),
                CreatePantherClaws(),
                CreateRatBite(),
                CreateMorningStar(),
                CreateEcu(BattleAnimationLibrary.CreateSwordShield(), SwordEcu, "Sword & Écu"),
                CreateEcu(CreateMaceShield(), MaceEcu, "Mace & Écu")
            };
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
            const float head = 0.7f;
            const int damage = 34;
            // How far the haft is laid over in the hand along a long arm.
            const float along = 70f;

            BodyPose idle = At(Cm(22f, 1.2f, 35f), 0f, 82f, 0f, 0f, Cm(30f, 1f, 5f), Cm(-30f, 1f, 15f));
            // Level over the head pointing left, the elbow out at shoulder height: the overhead swings turn it back from here.
            BodyPose over = At(Cm(20f, 1.9f, 10f), -90f, 5f, 10f, 0f, Cm(40f, 1.5f, 5f), Cm(-40f, 0.95f, 0f), 15f);
            // Where a swing sinks to after its stop, the arm still long.
            BodyPose sunk = At(Cm(5f, 1.23f, 46f), -8f, -3f, -30f, 15f, Cm(26f, 1.17f, 20f), Cm(-38f, 0.95f, -25f), along);
            BodyPose sunkLeft = At(Cm(-2f, 1.23f, 46f), -12f, -5f, -30f, 15f, Cm(22f, 1.16f, 18f), Cm(-38f, 0.95f, -25f), along);
            // The way back to rest: the elbow goes out to the right, then the head comes up.
            BodyPose elbowOut = At(Cm(12f, 1.18f, 40f), -20f, 22f, -15f, 8f, Cm(38f, 1.1f, 15f), Cm(-38f, 1f, -8f), 40f);
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
                MidPose = Peak(Cm(2f, 1.44f, 59f), -38f, 20f, Cm(10f, 1.4f, 28f), Cm(-35f, 0.9f, -42f), 50f),
                EndPose = At(Cm(0f, 1.28f, 60f), -6f, -4f, -45f, 22f, Cm(8f, 1.3f, 28f), Cm(-35f, 0.9f, -45f), along),
                Return = new() { Via(63, sunk, 0.5f), Via(72, elbowOut), Via(82, rising) }
            };
            // The forearm folds across the throat to the left temple, the head hangs behind the left shoulder; the hand
            // goes out ahead first and the head whips round after it, level, to stop dead ahead.
            AttackDefinition backhand = new AttackDefinition
            {
                Windup = 48 * Footage, Active = 7 * Footage, Recovery = 35 * Footage, Damage = Mathf.RoundToInt(damage * 1.05f), MoveMultiplier = 0.7f, Launch = 1f,
                Raise = new()
                {
                    Via(5, At(Cm(0f, 1.22f, 40f), -25f, 0f, -28f, 10f, Cm(28f, 1.2f, 18f), Cm(-38f, 0.95f, -15f), along)),
                    Via(12, At(Cm(-3f, 1.3f, 35f), -50f, 8f, -12f, 5f, Cm(28f, 1.28f, 15f), Cm(-36f, 0.95f, -10f), 55f)),
                    Via(18, At(Cm(-15f, 1.42f, 28f), -100f, 5f, -15f, 0f, Cm(18f, 1.4f, 25f), Cm(-33f, 0.95f, -20f), 45f)),
                    Via(24, At(Cm(-19f, 1.56f, 24f), -150f, -12f, -20f, 0f, Cm(10f, 1.45f, 30f), Cm(-32f, 0.93f, -30f), 30f)),
                    Via(31, At(Cm(-20f, 1.69f, 18f), -160f, -25f, -20f, 0f, Cm(10f, 1.42f, 35f), Cm(-32f, 0.92f, -38f), 25f), 0.4f),
                    Via(40, At(Cm(-19f, 1.7f, 20f), -156f, -20f, -20f, 2f, Cm(10f, 1.42f, 35f), Cm(-32f, 0.92f, -38f), 25f), 0.5f),
                    Via(43, At(Cm(-12f, 1.66f, 28f), -135f, 0f, -21f, 5f, Cm(10f, 1.42f, 35f), Cm(-32f, 0.92f, -38f), 25f)),
                    Via(46, At(Cm(0f, 1.6f, 42f), -95f, 25f, -25f, 8f, Cm(12f, 1.45f, 33f), Cm(-33f, 0.91f, -40f), 15f))
                },
                WindupPose = At(Cm(12f, 1.52f, 53f), -88f, 30f, -30f, 10f, Cm(12f, 1.43f, 28f), Cm(-34f, 0.9f, -42f), 5f),
                MidPose = Peak(Cm(16f, 1.45f, 52f), -38f, 17f, Cm(16f, 1.4f, 24f), Cm(-35f, 0.9f, -45f), 55f),
                EndPose = At(Cm(20f, 1.41f, 50f), 1f, 4f, -40f, 18f, Cm(20f, 1.37f, 20f), Cm(-35f, 0.9f, -45f), along),
                Return = new()
                {
                    Via(66, At(Cm(20f, 1.28f, 42f), 0f, -3f, -30f, 12f, Cm(30f, 1.2f, 18f), Cm(-38f, 0.95f, -25f), along), 0.5f),
                    Via(76, At(Cm(20f, 1.2f, 40f), -10f, 30f, -10f, 5f, Cm(32f, 1.05f, 10f), Cm(-35f, 1f, -5f), 25f)),
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
                    Via(7, At(Cm(20f, 1.46f, 35f), -45f, 30f, -10f, 5f, Cm(38f, 1.2f, 10f), Cm(-37f, 0.95f, -15f), 50f)),
                    Via(12, At(Cm(22f, 1.72f, 25f), -75f, 20f, 0f, 0f, Cm(40f, 1.38f, 5f), Cm(-39f, 0.95f, -5f), 40f)),
                    Via(17, over),
                    Via(27, At(Cm(18f, 1.95f, 10f), -120f, 0f, 15f, -5f, Cm(40f, 1.5f, 5f), Cm(-40f, 0.95f, 0f))),
                    Via(37, At(Cm(15f, 1.95f, 10f), -170f, 0f, 15f, -5f, Cm(40f, 1.55f, 0f), Cm(-40f, 0.95f, 0f))),
                    Via(42, At(Cm(15f, 1.95f, 15f), 175f, 10f, 10f, 0f, Cm(38f, 1.6f, 10f), Cm(-37f, 0.94f, -12f))),
                    Via(44, At(Cm(15f, 1.9f, 25f), 140f, 45f, 5f, 5f, Cm(32f, 1.55f, 20f), Cm(-35f, 0.93f, -22f), 15f))
                },
                WindupPose = At(Cm(15f, 1.8f, 37f), 100f, 65f, -10f, 10f, Cm(25f, 1.48f, 28f), Cm(-33f, 0.92f, -30f), 30f),
                MidPose = Peak(Cm(0f, 1.44f, 56f), -35f, 20f, Cm(10f, 1.4f, 27f), Cm(-35f, 0.9f, -42f), 50f),
                EndPose = At(Cm(-8f, 1.3f, 55f), -10f, -7f, -45f, 22f, Cm(4f, 1.3f, 26f), Cm(-35f, 0.9f, -45f), along),
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
                MidPose = Peak(Cm(0f, 1.45f, 56f), -35f, 20f, Cm(10f, 1.4f, 27f), Cm(-35f, 0.9f, -42f), 50f),
                EndPose = At(Cm(-8f, 1.28f, 55f), -15f, -7f, -45f, 22f, Cm(4f, 1.3f, 26f), Cm(-35f, 0.9f, -45f), along),
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
                Reach = 1.35f,
                DeflectDuration = 0.6f,
                BladeBase = 0.1f,
                BladeTip = 0.8f,
                Strike = head,
                Idle = idle,
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
                BlockBoxCenter = new Vector3(0f, 0f, 0.45f),
                BlockBoxExtents = new Vector3(0.08f, 0.08f, 0.35f)
            };
        }

        /// The spellbook of Dark and Darker, authored in the right hand and played mirrored: in the game it is the left
        /// hand's. At rest it lies open on the palm, low in the view, its spine running ahead and the pages up. To block,
        /// the other hand folds it shut and both stand it up before the eyes, pinching its sides, the forearms rising from
        /// the bottom corners of the view. The one strike of the series takes it shut in both hands past the face to over
        /// the head, holds it there and brings it down upright on the crosshair at arm's length; the book hand alone brings
        /// it back and opens it.
        private static WeaponDefinition CreateSpellbook()
        {
            Vector3 across = Vector3.left;
            // Open on the palm of the book hand raised before the chest, the spine steep and the pages to the eye, its head
            // just under the crosshair; the other hand rests low in the far corner of the view. The book breathes by a centimetre.
            BodyPose idle = OpenBook(new(0.085f, 1.54f, 0.47f), -3f, 40f, -2f, new(0.24f, 1.2f, 0.22f), new(-0.2f, 1.45f, 0.46f));
            BodyPose breath = OpenBook(new(0.085f, 1.55f, 0.47f), -3f, 42f, -2f, new(0.24f, 1.21f, 0.22f), new(-0.2f, 1.46f, 0.47f));
            BodyPose block = ShutBook(new(0.085f, 1.7f, 0.445f), across, Vector3.up, 0f);
            BodyPose held = ShutBook(new(0.076f, 1.52f, 0.445f), across, Vector3.up, 0f);
            held.Off = idle.Off;
            held.OffOpen = idle.OffOpen;

            return new WeaponDefinition
            {
                Prefix = Spellbook,
                DisplayName = "Spellbook",
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
                Attacks = new[]
                {
                    new AttackDefinition
                    {
                        Windup = 47 * Footage, Active = 7 * Footage, Recovery = 47 * Footage,
                        // The strikes of the footage are single ones: the next may only start as this one is back at the open book.
                        ComboStart = 94 * Footage, ComboEnd = 101 * Footage,
                        Damage = 18, MoveMultiplier = 0.7f, Stagger = 0.15f,
                        Raise = new()
                        {
                            // Folded shut low before the chest, swung to the other side as both hands take it, then up past the
                            // face to over the head, where it waits.
                            Via(10, ShutBook(new(0.05f, 1.45f, 0.45f), across, Vector3.up, 0f)),
                            Via(14, ShutBook(new(0f, 1.52f, 0.42f), across, Vector3.up, 0f)),
                            Via(18, ShutBook(new(0.03f, 1.72f, 0.32f), across, new(0f, 0.95f, -0.3f), 0f)),
                            Via(22, ShutBook(new(0.085f, 2f, 0.1f), across, new(0f, 0.5f, -0.87f), -5f), 0.5f),
                            Via(42, ShutBook(new(0.085f, 2.05f, -0.03f), across, new(0f, 0.45f, -0.9f), -8f), 0.5f)
                        },
                        WindupPose = ShutBook(new(0.085f, 1.93f, 0.4f), across, new(0f, 0.6f, -0.8f), 0f),
                        MidPose = ShutBook(new(0.06f, 1.671f, 0.5f), across, new(0f, 0.97f, -0.25f), 8f),
                        EndPose = ShutBook(new(0.085f, 1.4f, 0.48f), across, new(0f, 0.85f, -0.5f), 8f),
                        Return = new()
                        {
                            // Down out of the view, then back up shut in the book hand alone, held, and laid open on the palm.
                            Via(62, ShutBook(new(0.085f, 1.22f, 0.42f), across, Vector3.up, 4f), 0.6f),
                            Via(78, held, 0.5f),
                            Via(86, held, 0.5f)
                        },
                        Launch = 1f
                    }
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

        /// The arming sword of Dark and Darker, carried upright before the right hip with the left hand hanging free. The
        /// series is a forehand from behind the head over the right shoulder to the left hip, a backhand from beside the
        /// face over the left shoulder down to the right, and a thrust from the arm held out to the right. A blocked hit is
        /// answered from the low guard with an overhead cut from behind the head down the right side.
        private static WeaponDefinition CreateArmingSword(string name, float bladeBase, float bladeTip, float reach, int damage)
        {
            BodyPose idle = At(Cm(20f, 1.15f, 40f), 0f, 85f, 0f, 5f, Cm(25f, 1f, 10f), Cm(-25f, 1f, 5f));
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
                BlockRaise = 0.2f,
                BlockMitigation = 0.7f,
                BlockImpact = 0.28f,
                BlockRecovery = 0.4f,
                BlockAngle = 80f,
                BlockMove = 0.6f,
                // The first frame of the riposte in the footage: low before the belly, the blade across the body to the left.
                Block = At(Cm(15f, 1.1f, 40f), -55f, 25f, -15f, 5f, Cm(25f, 1f, 10f), Cm(-20f, 1f, 5f)),
                BlockHit = At(Cm(13f, 1.06f, 34f), -58f, 20f, -15f, 0f, Cm(25f, 0.98f, 5f), Cm(-20f, 1f, 3f)),
                BlockLowered = At(Cm(14f, 1.05f, 36f), -50f, 15f, -12f, 5f, Cm(25f, 0.97f, 8f), Cm(-22f, 1f, 5f)),
                DeflectPose = At(Cm(34f, 1.62f, 10f), 45f, 63f, 12f, -6f, Cm(42f, 1.35f, 0f), Cm(-25f, 1f, 5f)),
                BlockSocket = WeaponSocket.RightHand,
                BlockBoxCenter = new Vector3(0f, 0f, (bladeBase + bladeTip) * 0.5f),
                BlockBoxExtents = new Vector3(0.08f, 0.08f, (bladeTip - bladeBase) * 0.5f)
            };
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

        /// The écu of Dark and Darker on the arm of a one-handed weapon: the swings are the weapon's with the round shield,
        /// the block is the écu's own.
        /// The footage carries a mace: at rest it stands upright before the right chest with the écu low at the left, its
        /// top just in the corner of the view. The block lifts the écu in a straight line to cover the face up to the eyes,
        /// face ahead and point down, while the weapon drops out of the view by the right hip; let go, it comes down the
        /// same way, slower, sinks a little below rest and settles.
        private static WeaponDefinition CreateEcu(WeaponDefinition definition, string prefix, string displayName)
        {
            Vector3 restShield = new(-0.3f, 1.33f, 0.52f);
            Vector3 restNormal = new(-0.14f, 0f, 0.99f);
            Vector3 restTop = new(-0.09f, 1f, 0f);
            Vector3 rightElbow = new(0.25f, 1.15f, 0.1f);
            Vector3 dropped = new(0.26f, 1f, 0.12f);
            Vector3 droppedBlade = new(0.1f, 0.97f, 0.12f);
            Vector3 face = new(0f, 0.09f, 1f);
            Vector3 top = new(0f, 1f, -0.09f);
            bool isMace = prefix == MaceEcu;

            definition.Prefix = prefix;
            definition.DisplayName = displayName;
            OnEcu(ref definition.Idle);
            OnEcu(ref definition.IdleBreath);
            // The swings keep the shield hand of the round shield: the écu is behind the arm then, out of the view, and
            // turned on the arm like at rest it bent the wrist past 130 degrees.
            OnEcu(ref definition.DeflectPose);

            BodyPose idle = definition.Idle;
            definition.Idle = isMace
                ? EcuPose(new(0.12f, 1.38f, 0.42f), new(0.06f, 0.99f, 0.15f), restShield, restNormal, restTop, rightElbow)
                : EcuPose(idle.Main.Position, idle.Main.Forward, restShield, restNormal, restTop, idle.Main.Elbow);
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

            if (!isMace)
            {
                // The sword drops out of the view the same way; only the way back up ends in its own rest.
                for (int i = 0; i < definition.BlockLowerVia.Count; i++)
                {
                    PoseKey key = definition.BlockLowerVia[i];
                    key.Pose.Main = i == 0 ? key.Pose.Main : definition.Idle.Main;
                    definition.BlockLowerVia[i] = key;
                }
            }

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

        /// A pose of the round shield with the écu on the arm instead: what was the thumb of the shield hand is the écu's top.
        private static void OnEcu(ref BodyPose pose)
        {
            if (!pose.HasHands || pose.OffSocket != WeaponSocket.LeftShield)
                return;

            Quaternion socket = Quaternion.LookRotation(pose.Off.Forward, pose.Off.Up) * Quaternion.Inverse(DungeonWeaponPrefabBuilder.EcuTurn);
            pose.Off.Forward = socket * Vector3.forward;
            pose.Off.Up = socket * Vector3.up;
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
            definition.IsManualReload = true;
            definition.ArrowMinSpeed = 36f;
            definition.ArrowMaxSpeed = 40f;
            definition.ArrowMinDamage = 30;
            definition.ArrowMaxDamage = 39;

            return definition;
        }
    }
}
