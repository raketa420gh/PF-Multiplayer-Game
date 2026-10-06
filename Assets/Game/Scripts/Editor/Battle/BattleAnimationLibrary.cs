using System;
using System.Collections.Generic;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Editor.Battle
{
    internal enum Ease
    {
        Linear,
        In,
        Out,
        InOut,
        /// Velocity runs through the key: the segment eases between the rates of its two keys.
        Flow
    }

    internal struct PoseKey
    {
        public float Time;
        public BodyPose Pose;
        public Ease Ease;
        /// Flow keys only: how fast the motion passes this key, in segments per second.
        public float Rate;
        /// Keys of a cut: on the way to this key the leading edge follows the travel of the point this far up the weapon.
        public float Lead;

        public PoseKey(float time, BodyPose pose, Ease ease = Ease.InOut)
        {
            Time = time;
            Pose = pose;
            Ease = ease;
            Rate = 0f;
            Lead = 0f;
        }

        public static PoseKey Flow(float time, BodyPose pose, float rate)
        {
            return new PoseKey(time, pose, Ease.Flow) { Rate = rate };
        }
    }

    internal sealed class AttackDefinition
    {
        public float Windup;
        public float Active;
        public float Recovery;
        public float ComboStart;
        public float ComboEnd;
        public int Damage;
        public float MoveMultiplier;
        public float Stagger;
        public BodyPose WindupPose;
        public BodyPose MidPose;
        public BodyPose EndPose;
        /// The swing of the series this one is chained to. Its clip then sets off from where that swing ends its active
        /// phase instead of the idle pose: the weapon goes on from the cut into the next windup.
        public AttackDefinition After;
        /// The off hand strikes. The swing is authored as the main hand's, and its states play mirrored.
        public bool IsOffHand;
        /// Overrides the weapon's strike point for this swing; negative strikes with the butt, behind the grip.
        public float Strike;
        /// Poses the weapon passes on its way to the windup pose, which is where the active phase finds it, and after
        /// the active phase on its way back to rest: the first of those is the follow-through, which a swing without
        /// them works out by itself. Flow keys; their rate is a slope, 1 = the pace that takes the weapon from the key
        /// before to the key after in the time between them. The builder gives them the roll the arms are at ease
        /// with: once the cut is over, the weapon is free to turn over in the hands.
        public List<PoseKey> Raise = new();
        public List<PoseKey> Return = new();
        /// The same slope for the windup pose of a swing that is under way by then; 0 = the weapon settles into it.
        public float Launch;

        public float Duration => Windup + Active + Recovery;
    }

    internal sealed class WeaponDefinition
    {
        public string Prefix;
        public string DisplayName;
        public WeaponKind Kind;
        public float Reach;
        public float DeflectDuration;
        public float BladeBase;
        public float BladeTip;
        public float Strike;
        /// Bare hands and claws: the strike point sits in the hand, too close for its path to steer the roll frame by
        /// frame. The knuckles still lead a punch, turning from key to key.
        public bool IsUnarmed;
        /// A round haft (staff): it has no edge to lead a cut with, so the hand keeps the roll the arm gives it.
        public bool IsRound;
        /// The edge faces the heel of the main hand, away from its knuckles: the hand at the butt of a polearm, which
        /// travels against the head, so the knuckles lead the hand's own way while the edge leads the head's.
        public bool IsEdgeBack;
        public BodyPose Idle;
        /// Breathing at rest: the idle sinks into this pose and back once per cycle (seconds of clip); no cycle = a still pose.
        public BodyPose IdleBreath;
        public float IdleCycle;
        public AttackDefinition[] Attacks = Array.Empty<AttackDefinition>();
        /// The answer to a blocked hit: not a part of the series, it sets off from the block pose.
        public AttackDefinition Riposte;

        public bool CanBlock;
        public float BlockRaise;
        public float BlockMitigation;
        public float BlockImpact;
        public float BlockRecovery;
        public float BlockAngle;
        public float BlockMove;
        public BodyPose Block;
        public BodyPose BlockHit;
        public BodyPose BlockLowered;
        public BodyPose DeflectPose;
        public WeaponSocket BlockSocket;
        public Vector3 BlockBoxCenter;
        public Vector3 BlockBoxExtents;

        public BodyPose DrawPose;
        public BodyPose ReleasePose;

        public float DrawTime;
        public float ReloadTime;
        public float ArrowMinSpeed;
        public float ArrowMaxSpeed;
        public int ArrowMinDamage;
        public int ArrowMaxDamage;

        /// Distance from the grip to the part of the weapon that meets the crosshair at the peak of a swing.
        public float StrikePoint => Strike > 0f ? Strike : Mathf.Lerp(BladeBase, BladeTip, 0.65f);

        public float StrikeOf(AttackDefinition attack) => attack != null && attack.Strike != 0f ? attack.Strike : StrikePoint;
    }

    /// Hand-authored key poses (root space, character faces +Z) and timings shared by clips and weapon configs.
    internal static class BattleAnimationLibrary
    {
        public const float FullDrawTime = 0.9f;
        public const float ReloadTime = 0.6f;
        public const float ArrowMinSpeed = 14f;
        public const float ArrowMaxSpeed = 30f;
        public const float ArrowGravity = -9.81f;
        public const float CrouchDrop = 0.45f;
        /// Length of the drinking clip, which is how long a potion takes at the base Action Speed.
        public const float DrinkTime = 129f / BattleAnimationBuilder.FrameRate;
        public const float CastChargeTime = 1f;
        public const float CastReleaseTime = 0.5f;
        /// A belt item is no handle to close the fist on: the fingers lie around it.
        private const float HeldOpen = 0.55f;

        /// Eye of the simulation body. The crosshair is the ray from it along +Z: the upper body bends with the look
        /// pitch as one piece, so a swing keeps its place on the screen wherever the player looks.
        public static readonly Vector3 Eye = new(0f, 1.755f, 0.115f);

        // Swing dynamics as slopes of the segment eases (1 = the average speed of the segment). The raise starts briskly
        // and settles into the windup, the strike accelerates through the peak and is still moving when the active phase
        // ends; the weapon then runs out into a follow-through before it comes back.
        private const float RaiseSlope = 1.2f;
        private const float SettleSlope = 0.25f;
        private const float StrikeSlope = 1.8f;
        private const float EndSlope = 0.8f;
        private const float FollowShare = 0.4f;
        private const float MinFollow = 0.08f;
        private const float MaxFollow = 0.26f;
        private const float FollowSwing = 0.45f;
        private const float FollowThrust = 0.08f;
        /// The torso turns this much ahead of the arms: a swing starts from the body.
        private const float SpineLead = 0.045f;
        /// A strike whose point travels less than this share of its way across the blade is a thrust: there is no cut
        /// for the edge to lead.
        private const float ThrustShare = 0.5f;
        /// Share of the way from the windup to the peak over which the edge takes to the path of the strike point: while
        /// the weapon turns over at the windup, its path has no direction worth following.
        private const float LeadIn = 0.3f;
        /// At the peak the grip is no further from the aim line than this share of the strike distance: the strike point
        /// has to reach the crosshair, and the blade to cross it at an angle instead of lying flat across the view.
        private const float MaxAimOffset = 0.75f;
        /// A fist is held upright: its short blade does lie across the view.
        private const float MaxFistOffset = 0.95f;
        /// An edge that has further than this to turn between two keys is all but turned over: it has two ways round.
        private const float TurnOver = 135f;
        /// How fast the weapon turns over in the hands once the cut is done, in radians a second.
        private const float TurnOverRate = 12f * Mathf.Deg2Rad * BattleAnimationBuilder.FrameRate;

        private static readonly Vector3 s_shieldRest =new(-0.24f, 1.3f, 0.28f);
        private static readonly Vector3 s_shieldRestNormal = new(-0.35f, 0f, 0.94f);
        private static readonly Vector3 s_shieldBack = new(-0.32f, 1.2f, 0.14f);
        private static readonly Vector3 s_shieldBackNormal = new(-0.7f, 0f, 0.7f);
        private static readonly Vector3 s_offHandRest = new(-0.24f, 0.84f, 0.1f);
        private static readonly Vector3 s_mainShoulder = new(0.17f, 1.47f, -0.06f);

        public static WeaponDefinition CreateSwordShield()
        {
            BodyPose idle = SwordShield(new(0.22f, 1.2f, 0.38f), new(-0.15f, 0.75f, 0.64f), s_shieldRest, s_shieldRestNormal);
            BodyPose block = SwordShield(new(0.3f, 1.32f, 0.22f), new(0.15f, 0.7f, 0.7f),
                new(-0.06f, 1.47f, 0.34f), new(0.02f, 0.1f, 1f), 15f);

            return new WeaponDefinition
            {
                Prefix = "SwordShield",
                DisplayName = "Sword & Shield",
                Kind = WeaponKind.OneHanded,
                Reach = 1.4f,
                DeflectDuration = 0.6f,
                BladeBase = 0.12f,
                BladeTip = 0.9f,
                Idle = idle,
                Attacks = new[]
                {
                    new AttackDefinition
                    {
                        Windup = 0.4f, Active = 0.2f, Recovery = 0.5f, ComboStart = 0.5f, ComboEnd = 0.9f,
                        Damage = 22, MoveMultiplier = 0.7f,
                        WindupPose = SwordShield(new(0.46f, 1.56f, 0.14f), new(0.85f, 0.45f, -0.25f), yaw: 25f),
                        MidPose = SwordShield(new(0f, 1.44f, 0.52f), Vector3.forward),
                        EndPose = SwordShield(new(-0.1f, 1.26f, 0.46f), new(-0.35f, -0.1f, 0.93f), yaw: -22f)
                    },
                    new AttackDefinition
                    {
                        Windup = 0.36f, Active = 0.2f, Recovery = 0.5f, ComboStart = 0.46f, ComboEnd = 0.86f,
                        Damage = 22, MoveMultiplier = 0.7f,
                        WindupPose = SwordShield(new(-0.04f, 1.5f, 0.34f), new(-0.85f, 0.35f, 0.15f), yaw: -18f),
                        MidPose = SwordShield(new(0.2f, 1.46f, 0.52f), Vector3.forward),
                        EndPose = SwordShield(new(0.5f, 1.36f, 0.2f), new(0.45f, 0.05f, 0.89f), yaw: 18f)
                    },
                    new AttackDefinition
                    {
                        Windup = 0.45f, Active = 0.2f, Recovery = 0.6f, ComboStart = 0.55f, ComboEnd = 0.95f,
                        Damage = 30, MoveMultiplier = 0.6f, Stagger = 0.2f,
                        WindupPose = SwordShield(new(0.2f, 1.9f, 0.02f), new(0.08f, 0.6f, -0.8f), pitch: -10f),
                        MidPose = SwordShield(new(0.08f, 1.42f, 0.54f), Vector3.forward),
                        EndPose = SwordShield(new(0.1f, 1.14f, 0.38f), new(-0.03f, -0.17f, 0.98f), pitch: 12f)
                    }
                },
                CanBlock = true,
                BlockRaise = 0.18f,
                BlockMitigation = 1f,
                BlockImpact = 0.25f,
                BlockRecovery = 0.35f,
                BlockAngle = 100f,
                BlockMove = 0.6f,
                Block = block,
                BlockHit = SwordShield(new(0.3f, 1.3f, 0.2f), new(0.15f, 0.7f, 0.7f),
                    new(-0.1f, 1.4f, 0.26f), new(-0.25f, 0.35f, 0.9f), 8f, -4f),
                BlockLowered = SwordShield(new(0.3f, 1.3f, 0.2f), new(0.15f, 0.7f, 0.7f),
                    new(-0.14f, 1.34f, 0.3f), new(-0.2f, 0.15f, 0.95f), 8f),
                DeflectPose = SwordShield(new(0.34f, 1.62f, 0.2f), new(0.3f, 0.85f, 0.3f), s_shieldRest, s_shieldRestNormal, 12f, -6f),
                BlockSocket = WeaponSocket.LeftShield,
                BlockBoxCenter = Vector3.zero,
                BlockBoxExtents = new Vector3(0.34f, 0.34f, 0.06f)
            };
        }

        public static WeaponDefinition CreateBow()
        {
            return new WeaponDefinition
            {
                Prefix = "Bow",
                DisplayName = "Bow",
                Kind = WeaponKind.Ranged,
                Reach = 0f,
                Idle = Bow(new(-0.14f, 1.3f, 0.34f), new(0.35f, 0.9f, 0.25f), new(0.2f, -0.1f, 1f), new(-0.08f, 1.3f, 0.2f)),
                DrawPose = Bow(new(-0.06f, 1.6f, 0.44f), new(-0.3f, 0.95f, 0f), new(-0.08f, 0f, 1f), new(0.02f, 1.6f, 0.02f), 25f),
                ReleasePose = Bow(new(-0.06f, 1.58f, 0.44f), new(-0.3f, 0.95f, 0f), new(-0.08f, 0f, 1f), new(0.06f, 1.56f, 0.2f), 20f)
            };
        }

        /// Idle, windup, peak, end of the active phase, follow-through, idle, and whatever poses the swing is given to
        /// pass on the way up and back. The motion never stops between the windup and the follow-through, and the peak
        /// is a key of its own, so its pose is reached exactly.
        public static List<PoseKey> AttackKeys(WeaponDefinition weapon, AttackDefinition attack)
        {
            List<PoseKey> keys = SwingKeys(weapon, attack);
            LeadWithEdge(keys, WindupIndex(attack), weapon.StrikeOf(attack), IsCut(weapon, attack), !weapon.IsUnarmed && !weapon.IsRound,
                weapon.IsEdgeBack ? -1f : 1f);

            return keys;
        }

        private static int WindupIndex(AttackDefinition attack)
        {
            return 1 + attack.Raise.Count;
        }

        /// A cut carries its strike point across the blade through the peak. A thrust runs it along the blade.
        public static bool IsCut(WeaponDefinition weapon, AttackDefinition attack)
        {
            return Across(SwingKeys(weapon, attack), PeakTime(attack), weapon.StrikeOf(attack), out _) >= ThrustShare;
        }

        private static List<PoseKey> SwingKeys(WeaponDefinition weapon, AttackDefinition attack)
        {
            BodyPose peak = Peak(weapon, attack);
            float peakTime = PeakTime(attack);
            float endTime = attack.Windup + attack.Active;
            float followTime = endTime + Mathf.Clamp(attack.Recovery * FollowShare, MinFollow, MaxFollow);
            float raiseTime = attack.Raise.Count > 0 ? attack.Raise[0].Time : attack.Windup;
            float raisedTime = attack.Raise.Count > 0 ? attack.Raise[^1].Time : 0f;
            BodyPose start = attack == weapon.Riposte ? weapon.Block
                : attack.After == null ? weapon.Idle
                : AttackKeys(weapon, attack.After)[WindupIndex(attack.After) + 2].Pose;

            List<PoseKey> keys = new List<PoseKey> { PoseKey.Flow(0f, start, RaiseSlope / raiseTime) };
            keys.AddRange(attack.Raise);
            keys.Add(PoseKey.Flow(attack.Windup, attack.WindupPose,
                attack.Launch > 0f ? attack.Launch * 2f / (peakTime - raisedTime) : SettleSlope / attack.Windup));
            keys.Add(PoseKey.Flow(peakTime, peak, StrikeSlope / (attack.Active * 0.5f)));
            keys.Add(PoseKey.Flow(endTime, attack.EndPose, EndSlope / (endTime - peakTime)));

            if (attack.Return.Count > 0)
                keys.AddRange(attack.Return);
            else
                keys.Add(PoseKey.Flow(followTime, FollowThrough(peak, attack.EndPose), 0f));

            keys.Add(PoseKey.Flow(attack.Duration, weapon.Idle, 0f));
            Pace(keys, 1, attack.Raise.Count);
            Pace(keys, keys.Count - 1 - attack.Return.Count, attack.Return.Count);

            return keys;
        }

        /// Turns the slopes of the keys a swing passes into rates.
        private static void Pace(List<PoseKey> keys, int first, int count)
        {
            for (int i = first; i < first + count; i++)
            {
                PoseKey key = keys[i];
                key.Rate *= 2f / (keys[i + 1].Time - keys[i - 1].Time);
                keys[i] = key;
            }
        }

        /// Turns the weapon about its own axis so that the leading edge faces where the strike point travels: the blade
        /// cuts along its path instead of slapping with the flat. A cut does so all the way from the windup to the end of
        /// the active phase, and its follow-through keeps the roll the cut ended with, or turns from it toward the roll
        /// it comes with. A thrust has no path across the blade to face: the weapon keeps the roll it rests with. Needs
        /// the edge of the rest pose; without it the arm solve rolls the weapon.
        private static void LeadWithEdge(List<PoseKey> keys, int windup, float strike, bool isCut, bool followsPath, float side)
        {
            if (keys[0].Pose.Edge == Vector3.zero)
                return;

            const float step = 1f / BattleAnimationBuilder.FrameRate;
            int peak = windup + 1;
            int end = windup + 2;
            int follow = windup + 3;
            HandPose held = keys[0].Pose.Main;
            held.Up = keys[0].Pose.Edge;

            // The windup is already turned for the cut that sets off from it.
            for (float time = keys[windup].Time + step; isCut && time < keys[peak].Time; time += step)
            {
                Locate(keys, time, out float alpha);

                if (alpha < LeadIn || Across(keys, time, strike, out Vector3 normal) < ThrustShare)
                    continue;

                held = Hand(keys, time);
                held.Up = Vector3.Cross(normal, held.Forward) * side;
                break;
            }

            for (int i = windup; i <= follow; i++)
            {
                PoseKey key = keys[i];
                Vector3 blade = key.Pose.Main.Forward;

                // Where the point runs along the blade, the weapon keeps the roll it had on the key before.
                if (isCut && i > windup && i < follow && Across(keys, key.Time, strike, out Vector3 normal) >= ThrustShare)
                    held = new HandPose(key.Pose.Main.Position, blade, Vector3.Cross(normal, blade) * side);

                Vector3 edge = Carry(held.Forward, held.Up, blade);

                // A follow-through that comes with a roll of its own is where the weapon starts to turn over to it.
                if (i == follow && key.Pose.Edge != Vector3.zero)
                    edge = Vector3.RotateTowards(edge, key.Pose.Edge, TurnOverRate * (key.Time - keys[end].Time), 0f);
                else
                    key.Pose.OffRoll = keys[0].Pose.OffRoll;

                key.Pose.Edge = edge;
                key.Lead = isCut && followsPath && (i == peak || i == end) ? strike * side : 0f;
                keys[i] = key;
            }

            Settle(keys, 0, windup);
            Settle(keys, follow, keys.Count - 1);
        }

        /// The keys between two whose roll is settled turn toward the roll they come with no faster than a weapon is
        /// turned over in the hands, counted from either side. A key that comes with none goes along with those around it.
        private static void Settle(List<PoseKey> keys, int from, int to)
        {
            for (int i = from + 1; i < to; i++)
                Turn(keys, i, i - 1);

            for (int i = to - 1; i > from; i--)
                Turn(keys, i, i + 1);
        }

        private static void Turn(List<PoseKey> keys, int index, int neighbour)
        {
            PoseKey key = keys[index];
            PoseKey other = keys[neighbour];
            Vector3 carried = Carry(other.Pose.Main.Forward, other.Pose.Edge, key.Pose.Main.Forward);

            if (key.Pose.Edge == Vector3.zero)
                key.Pose.OffRoll = other.Pose.OffRoll;

            key.Pose.Edge = key.Pose.Edge == Vector3.zero ? carried
                : Vector3.RotateTowards(carried, key.Pose.Edge, TurnOverRate * Mathf.Abs(key.Time - other.Time), 0f);
            keys[index] = key;
        }

        /// Share of the travel of the point 'strike' metres up the weapon that goes across the blade at the given moment.
        /// The normal is that of the plane the blade sweeps there; its length is the share.
        private static float Across(List<PoseKey> keys, float time, float strike, out Vector3 normal)
        {
            const float step = 0.5f / BattleAnimationBuilder.FrameRate;
            HandPose before = Hand(keys, time - step);
            HandPose after = Hand(keys, time + step);
            Vector3 travel = after.Position + after.Forward * strike - before.Position - before.Forward * strike;
            normal = Vector3.Cross(Hand(keys, time).Forward, travel.normalized);

            return normal.magnitude;
        }

        private static HandPose Hand(List<PoseKey> keys, float time)
        {
            int next = Locate(keys, time, out float alpha);

            return Interpolate(keys[Mathf.Max(next - 2, 0)].Pose.Main, keys[next - 1].Pose.Main, keys[next].Pose.Main,
                keys[Mathf.Min(next + 1, keys.Count - 1)].Pose.Main, alpha, keys[next].Ease == Ease.Flow);
        }

        /// Where the weapon runs out after the active phase: the last part of the swing continued. A thrust barely
        /// travels further, the arms are already stretched.
        private static BodyPose FollowThrough(in BodyPose peak, in BodyPose end)
        {
            Vector3 travel = end.Main.Position - peak.Main.Position;
            float share = Mathf.Lerp(FollowSwing, FollowThrust, Mathf.Abs(Vector3.Dot(travel.normalized, peak.Main.Forward)));
            BodyPose pose = end;
            pose.Spine = end.Spine + (end.Spine - peak.Spine) * share;
            pose.Head = end.Head + (end.Head - peak.Head) * share;
            pose.Main.Position = end.Main.Position + travel * share;
            pose.Main.Forward = Exp(end.Main.Forward, -Log(end.Main.Forward, peak.Main.Forward) * share);

            if (pose.Off.IsAutoRoll)
            {
                pose.Off.Position = pose.Main.Position + pose.Main.Forward * Vector3.Dot(end.Off.Position - end.Main.Position, end.Main.Forward);
                pose.Off.Forward = pose.Main.Forward;
            }

            return pose;
        }

        public static List<PoseKey> BlockKeys(WeaponDefinition weapon)
        {
            return new List<PoseKey>
            {
                new(0f, weapon.Idle),
                new(weapon.BlockRaise, weapon.Block, Ease.Out),
                new(weapon.BlockRaise + 0.1f, weapon.Block)
            };
        }

        public static List<PoseKey> BlockImpactKeys(WeaponDefinition weapon)
        {
            return new List<PoseKey>
            {
                new(0f, weapon.Block),
                new(0.08f, weapon.BlockHit, Ease.Out),
                new(weapon.BlockImpact, weapon.BlockLowered),
                new(weapon.BlockImpact + weapon.BlockRecovery, weapon.Block)
            };
        }

        public static List<PoseKey> DeflectKeys(WeaponDefinition weapon)
        {
            return new List<PoseKey>
            {
                new(0f, AttackKeys(weapon, weapon.Attacks[0])[WindupIndex(weapon.Attacks[0]) + 1].Pose),
                new(0.12f, weapon.DeflectPose, Ease.Out),
                new(weapon.DeflectDuration, weapon.Idle)
            };
        }

        public static List<PoseKey> DrawKeys(WeaponDefinition weapon, float drawTime)
        {
            return new List<PoseKey>
            {
                new(0f, weapon.Idle),
                new(drawTime, weapon.DrawPose, Ease.Out),
                new(drawTime + 0.1f, weapon.DrawPose)
            };
        }

        public static List<PoseKey> ReleaseKeys(WeaponDefinition weapon, float reloadTime)
        {
            return new List<PoseKey>
            {
                new(0f, weapon.DrawPose),
                new(0.06f, weapon.ReleasePose, Ease.Out),
                new(Mathf.Min(0.25f, reloadTime * 0.4f), weapon.ReleasePose),
                new(reloadTime, weapon.Idle)
            };
        }

        /// Middle of the active phase, moved onto the 60 Hz grid: the peak pose is then a recorded frame and a baked trace
        /// sample, not a blend of its neighbours.
        public static float PeakTime(AttackDefinition attack)
        {
            return Mathf.Round((attack.Windup + attack.Active * 0.5f) * BattleAnimationBuilder.FrameRate) / BattleAnimationBuilder.FrameRate;
        }

        /// The pose at the middle of the active phase. Whatever the authored blade direction, the weapon is turned so that
        /// its strike point lies on the crosshair: what the player aims at is what the swing hits.
        public static BodyPose Peak(WeaponDefinition weapon, AttackDefinition attack)
        {
            BodyPose pose = attack.MidPose;
            float strike = weapon.StrikeOf(attack);
            Vector2 offset = Vector2.ClampMagnitude(pose.Main.Position - Eye, Mathf.Abs(strike) * (weapon.IsUnarmed ? MaxFistOffset : MaxAimOffset));
            Vector3 grip = new Vector3(Eye.x + offset.x, Eye.y + offset.y, pose.Main.Position.z);
            Vector3 blade = Aim(grip, Mathf.Abs(strike)).normalized * Mathf.Sign(strike);

            // An off hand without a roll of its own holds the same weapon and follows it.
            if (pose.Off.IsAutoRoll)
            {
                pose.Off.Position = grip + blade * Vector3.Dot(pose.Off.Position - pose.Main.Position, pose.Main.Forward);
                pose.Off.Forward = blade;
            }

            pose.Main.Position = grip;
            pose.Main.Forward = blade;

            return pose;
        }

        /// Direction from the grip that puts the point 'strike' metres up the weapon on the crosshair ray.
        public static Vector3 Aim(Vector3 grip, float strike)
        {
            Vector2 offset = new Vector2(Eye.x - grip.x, Eye.y - grip.y);

            return new Vector3(offset.x, offset.y, Mathf.Sqrt(Mathf.Max(strike * strike - offset.sqrMagnitude, 0f)));
        }

        /// The idle loop: it sinks into the breath pose and back, or holds still for a second.
        public static List<PoseKey> IdleKeys(WeaponDefinition weapon)
        {
            float cycle = weapon.IdleCycle > 0f ? weapon.IdleCycle : 1f;

            return new List<PoseKey>
            {
                new(0f, weapon.Idle),
                new(cycle * 0.5f, weapon.IdleCycle > 0f ? weapon.IdleBreath : weapon.Idle),
                new(cycle, weapon.Idle)
            };
        }

        public static BodyPose Sample(List<PoseKey> keys, float time)
        {
            int next = Locate(keys, time, out float alpha);
            BodyPose before = keys[Mathf.Max(next - 2, 0)].Pose;
            BodyPose after = keys[Mathf.Min(next + 1, keys.Count - 1)].Pose;
            BodyPose a = keys[next - 1].Pose;
            BodyPose b = keys[next].Pose;
            bool isFlow = keys[next].Ease == Ease.Flow;

            BodyPose pose = a;
            pose.Spine = Vector3.Lerp(a.Spine, b.Spine, alpha);
            pose.Head = Vector3.Lerp(a.Head, b.Head, alpha);
            pose.Main = Interpolate(before.Main, a.Main, b.Main, after.Main, alpha, isFlow);
            pose.Off = Interpolate(before.Off, a.Off, b.Off, after.Off, alpha, isFlow);
            pose.Edge = Roll(a, b, pose.Main.Forward, alpha);
            pose.OffRoll = Mathf.LerpAngle(a.OffRoll, b.OffRoll, alpha);
            pose.MainOpen = Mathf.Lerp(a.MainOpen, b.MainOpen, alpha);
            pose.OffOpen = Mathf.Lerp(a.OffOpen, b.OffOpen, alpha);
            pose.Lean = Mathf.Lerp(a.Lean, b.Lean, alpha);

            // A cut keeps its edge on the path of the strike point itself, not only on the keys.
            // A negative lead is an edge on the heel of the hand.
            if (keys[next].Lead != 0f)
            {
                float weight = Mathf.InverseLerp(ThrustShare * 0.5f, ThrustShare, Across(keys, time, Mathf.Abs(keys[next].Lead), out Vector3 normal));

                if (keys[next - 1].Lead == 0f)
                    weight *= Mathf.InverseLerp(0f, LeadIn, alpha);

                pose.Edge = Vector3.Slerp(pose.Edge, Vector3.Cross(normal, pose.Main.Forward).normalized * Mathf.Sign(keys[next].Lead),
                    Mathf.SmoothStep(0f, 1f, weight));
            }

            if (!isFlow)
                return pose;

            // Both hands on one weapon keep their places on it: the off hand follows the main one along the shaft.
            if (a.Off.IsAutoRoll && b.Off.IsAutoRoll)
            {
                float offset = Mathf.Lerp(Vector3.Dot(a.Off.Position - a.Main.Position, a.Main.Forward),
                    Vector3.Dot(b.Off.Position - b.Main.Position, b.Main.Forward), alpha);
                pose.Off.Position = pose.Main.Position + pose.Main.Forward * offset;
                pose.Off.Forward = pose.Main.Forward;
            }

            // The torso is read a moment ahead of the arms. The lead grows from nothing, so the clip starts in its first key.
            float lead = SpineLead * Mathf.SmoothStep(0f, 1f, time / keys[1].Time);
            next = Locate(keys, Mathf.Min(time + lead, keys[keys.Count - 1].Time), out alpha);
            before = keys[Mathf.Max(next - 2, 0)].Pose;
            after = keys[Mathf.Min(next + 1, keys.Count - 1)].Pose;
            a = keys[next - 1].Pose;
            b = keys[next].Pose;
            pose.Spine = CatmullRom(before.Spine, a.Spine, b.Spine, after.Spine, alpha);
            pose.Head = CatmullRom(before.Head, a.Head, b.Head, after.Head, alpha);

            return pose;
        }

        /// Index of the key the time runs towards and the eased progress of that segment.
        private static int Locate(List<PoseKey> keys, float time, out float alpha)
        {
            int next = 1;

            while (next < keys.Count - 1 && time > keys[next].Time)
                next++;

            PoseKey from = keys[next - 1];
            PoseKey to = keys[next];
            float span = to.Time - from.Time;
            alpha = Mathf.InverseLerp(from.Time, to.Time, time);
            alpha = to.Ease == Ease.Flow ? Hermite(alpha, from.Rate * span, to.Rate * span) : ApplyEase(alpha, to.Ease);

            return next;
        }

        /// One-handed weapon without a shield: the off hand rests by the hip.
        internal static BodyPose OneHanded(Vector3 grip, Vector3 blade, float yaw = 0f, float pitch = 0f)
        {
            BodyPose pose = Upper(yaw, pitch);
            pose.Main = new HandPose(grip, blade);
            pose.Off = new HandPose(s_offHandRest, new Vector3(0.2f, -0.15f, 1f), new Vector3(0.05f, -1f, 0.15f));
            pose.OffSocket = WeaponSocket.LeftHand;

            return pose;
        }

        /// Both hands raised in front of the chest for spell casting.
        internal static BodyPose Cast(float spread, float forward, float height, float pitch = 0f)
        {
            BodyPose pose = Upper(0f, pitch);
            Vector3 right = new Vector3(spread, height, forward);
            Vector3 left = new Vector3(-spread, height, forward);
            pose.Main = new HandPose(right, Vector3.up);
            pose.Off = new HandPose(left, Vector3.up);
            pose.OffSocket = WeaponSocket.LeftHand;

            return pose;
        }

        /// The hands gather the spell: the clip ends on the charged pose, which is kept until the cast is released.
        public static List<PoseKey> CastKeys()
        {
            return new List<PoseKey>
            {
                new(0f, Cast(0.22f, 0.3f, 1.25f)),
                new(0.35f, Cast(0.16f, 0.42f, 1.42f, -4f), Ease.Out),
                new(CastChargeTime, Cast(0.14f, 0.5f, 1.45f, -6f))
            };
        }

        /// The charged spell is pushed out and the hands come down.
        public static List<PoseKey> CastReleaseKeys()
        {
            return new List<PoseKey>
            {
                new(0f, Cast(0.14f, 0.5f, 1.45f, -6f)),
                new(0.15f, Cast(0.26f, 0.62f, 1.4f, 4f), Ease.In),
                new(CastReleaseTime, Cast(0.22f, 0.3f, 1.25f))
            };
        }

        /// A belt item waits upright in the main hand, low on the right of the view.
        public static BodyPose Hold()
        {
            return Held(new(0.17f, 1.49f, 0.5f), new(-0.08f, 1f, 0.12f), new(-0.25f, 0.45f, 0.85f));
        }

        /// Drinking as in Dark and Darker: the bottle tips its mouth to the eyes on the way up, the hand turns over it
        /// and pours it out before the face, then it comes back the same way.
        public static List<PoseKey> UseKeys()
        {
            BodyPose tip = Held(new(0.03f, 1.62f, 0.42f), new(0.05f, 0.35f, -0.94f), new(-0.75f, 0.35f, 0.55f));
            BodyPose drink = Held(new(-0.02f, 1.7f, 0.34f), new(0.08f, -0.25f, -0.96f), new(-0.9f, 0.25f, 0.35f), -4f);
            BodyPose drain = Held(new(-0.02f, 1.72f, 0.33f), new(0.08f, -0.45f, -0.89f), new(-0.9f, 0.3f, 0.3f), -7f);

            return new List<PoseKey>
            {
                new(0f, Hold()),
                new(0.3f, tip),
                new(0.65f, drink, Ease.Out),
                new(1.38f, drain),
                new(1.5f, tip),
                new(1.75f, Hold()),
                new(DrinkTime, Hold())
            };
        }

        /// The main hand holds an item by its axis with the forearm pointing a given way; the off hand rests by the hip.
        private static BodyPose Held(Vector3 grip, Vector3 axis, Vector3 forearm, float pitch = 0f)
        {
            BodyPose pose = OneHanded(grip, axis, pitch: pitch);
            pose.Edge = forearm;
            pose.MainOpen = HeldOpen;

            return pose;
        }

        /// Bare hands held alike on both sides: the thumb and the fingers of the main hand say how, the off hand mirrors it.
        internal static BodyPose Guard(Vector3 hand, Vector3 thumb, Vector3 fingers, float open = 0f, float pitch = 0f)
        {
            BodyPose pose = Upper(0f, pitch);
            pose.Main = new HandPose(hand, thumb);
            pose.Edge = fingers;
            pose.Off = new HandPose(Flip(hand), Flip(thumb), Flip(fingers));
            pose.OffSocket = WeaponSocket.LeftHand;
            pose.MainOpen = open;
            pose.OffOpen = open;

            return pose;
        }

        /// A punch: the fist of the main hand is upright, its knuckles turn to where it travels. The off hand, thumb up,
        /// reaches out or waits by the chest.
        internal static BodyPose Punch(Vector3 fist, Vector3 off, Vector3 offFingers, float offOpen, float yaw, float pitch = 0f)
        {
            BodyPose pose = Upper(yaw, pitch);
            pose.Main = new HandPose(fist, Vector3.up);
            pose.Off = new HandPose(off, Vector3.up, offFingers);
            pose.OffSocket = WeaponSocket.LeftHand;
            pose.OffOpen = offOpen;

            return pose;
        }

        private static Vector3 Flip(Vector3 vector)
        {
            return new Vector3(-vector.x, vector.y, vector.z);
        }

        /// Bandaging as the player sees it: the off forearm is held up across the view and the main hand winds the
        /// roll around it, one turn per cycle.
        public static BodyPose Bandage(float phase)
        {
            Vector3 fist = new Vector3(0.1f, 1.61f, 0.52f);
            Vector3 forearm = new Vector3(0.88f, 0.3f, 0.37f).normalized;
            Vector3 side = Vector3.Cross(forearm, Vector3.up).normalized;
            Vector3 over = Vector3.Cross(forearm, side);
            float angle = phase * Mathf.PI * 2f;
            Vector3 sway = Vector3.up * (Mathf.Sin(angle) * 0.012f);

            BodyPose pose = Upper(-6f, 8f);
            pose.Main = new HandPose(fist - forearm * 0.2f + (side * Mathf.Cos(angle) + over * Mathf.Sin(angle)) * 0.09f + sway, forearm);
            // The roll of the held-up hand is given: the elbow search of a free roll would flip it as the hand sways.
            pose.Off = new HandPose(fist + sway, new Vector3(-0.2f, 0.9f, 0.4f), forearm);
            pose.OffSocket = WeaponSocket.LeftHand;

            return pose;
        }

        public static BodyPose Walk(float phase, Vector2 direction, float stride, float lift, float drop, float lean = 0f)
        {
            BodyPose pose = Standing(drop + Mathf.Cos(phase * Mathf.PI * 4f) * 0.012f);
            pose.HipsEuler = new Vector3(lean, 0f, 0f);
            Step(phase, direction, stride, lift, out pose.LeftFoot, out pose.LeftFootEuler);
            Step(Mathf.Repeat(phase + 0.5f, 1f), direction, stride, lift, out pose.RightFoot, out pose.RightFootEuler);

            return pose;
        }

        private static BodyPose Standing(float drop)
        {
            return new BodyPose { Hips = new Vector3(0f, -drop, 0f), ArmDrop = 70f };
        }

        private static void Step(float phase, Vector2 direction, float stride, float lift, out Vector3 offset, out Vector3 euler)
        {
            float along;
            float height = 0f;
            euler = default;

            if (phase < 0.5f)
            {
                along = 1f - phase * 4f;
            }
            else
            {
                float swing = (phase - 0.5f) * 2f;
                along = -1f + Mathf.SmoothStep(0f, 1f, swing) * 2f;
                height = Mathf.Sin(swing * Mathf.PI) * lift;
                euler = new Vector3(Mathf.Sin(swing * Mathf.PI) * 22f * Mathf.Abs(direction.y), 0f, 0f);
            }

            offset = new Vector3(direction.x, 0f, direction.y) * (along * stride * 0.5f) + Vector3.up * height;
        }

        internal static BodyPose SwordShield(Vector3 grip, Vector3 blade, float yaw = 0f, float pitch = 0f)
        {
            return SwordShield(grip, blade, s_shieldBack, s_shieldBackNormal, yaw, pitch);
        }

        internal static BodyPose SwordShield(Vector3 grip, Vector3 blade, Vector3 shield, Vector3 shieldNormal,
            float yaw = 0f, float pitch = 0f)
        {
            BodyPose pose = Upper(yaw, pitch);
            pose.Main = new HandPose(grip, blade);
            pose.Off = new HandPose(shield, shieldNormal, Vector3.up);
            pose.OffSocket = WeaponSocket.LeftShield;

            return pose;
        }

        internal static BodyPose TwoHanded(Vector3 grip, Vector3 blade, float offHand = -0.14f, float yaw = 0f, float pitch = 0f)
        {
            Vector3 offGrip = grip + blade.normalized * offHand;
            BodyPose pose = Upper(yaw, pitch);
            pose.Main = new HandPose(grip, blade);
            pose.Off = new HandPose(offGrip, blade);
            pose.OffSocket = WeaponSocket.LeftHand;

            return pose;
        }

        internal static BodyPose Bow(Vector3 bow, Vector3 stave, Vector3 arrowDirection, Vector3 drawHand, float yaw = 0f)
        {
            BodyPose pose = Upper(yaw, 0f);
            pose.Main = new HandPose(drawHand, Vector3.up);
            pose.Off = new HandPose(bow, stave, arrowDirection);
            pose.OffSocket = WeaponSocket.LeftHand;

            return pose;
        }

        internal static BodyPose Upper(float yaw, float pitch)
        {
            Vector3 spine = new Vector3(pitch, yaw, 0f);

            return new BodyPose { Hips = new Vector3(0f, -0.03f, 0f), Spine = spine, Head = -spine * 0.8f, HasHands = true };
        }

        private static HandPose Interpolate(in HandPose before, in HandPose from, in HandPose to, in HandPose after, float alpha, bool isFlow)
        {
            return new HandPose
            {
                Position = CatmullRom(before.Position, from.Position, to.Position, after.Position, alpha),
                Forward = isFlow ? Arc(before.Forward, from.Forward, to.Forward, after.Forward, alpha) : Vector3.Slerp(from.Forward, to.Forward, alpha),
                Up = from.IsAutoRoll || to.IsAutoRoll ? Vector3.zero : Vector3.Slerp(from.Up, to.Up, alpha),
                Elbow = from.ElbowWeight > 0f && to.ElbowWeight > 0f
                    ? CatmullRom(ElbowOr(before, from), from.Elbow, to.Elbow, ElbowOr(after, to), alpha)
                    : from.ElbowWeight > 0f ? from.Elbow : to.Elbow,
                ElbowWeight = Mathf.Lerp(from.ElbowWeight, to.ElbowWeight, alpha)
            };
        }

        private static Vector3 ElbowOr(in HandPose hand, in HandPose fallback)
        {
            return hand.ElbowWeight > 0f ? hand.Elbow : fallback.Elbow;
        }

        /// Puts the elbows where the footage has them; zero leaves that arm to the solve.
        internal static BodyPose Elbows(BodyPose pose, Vector3 main, Vector3 off = default)
        {
            pose.Main.Elbow = main;
            pose.Main.ElbowWeight = main == Vector3.zero ? 0f : 1f;
            pose.Off.Elbow = off;
            pose.Off.ElbowWeight = off == Vector3.zero ? 0f : 1f;

            return pose;
        }

        /// The edge between two keys that have one. Each key carries its edge along as the blade turns away from it,
        /// without rolling the weapon; the two meet halfway. A swing that stays in one plane then gets no roll of its
        /// own, however far the blade sweeps. An edge that is turned over between the keys has two ways round: it takes
        /// the one that keeps it on the side away from the shoulder, where the forearm is, as the wrist does not bend
        /// the other way.
        private static Vector3 Roll(in BodyPose from, in BodyPose to, Vector3 blade, float alpha)
        {
            if (from.Edge == Vector3.zero || to.Edge == Vector3.zero)
                return Vector3.zero;

            Vector3 edge = Carry(from.Main.Forward, from.Edge, blade);
            float turn = Vector3.SignedAngle(edge, Carry(to.Main.Forward, to.Edge, blade), blade);
            float sense = TurnSense(from, to);

            if (turn * sense < 0f && Mathf.Abs(turn) > 90f)
                turn += 360f * sense;

            return Quaternion.AngleAxis(turn * alpha, blade) * edge;
        }

        /// Which way round the edge goes between two keys that turn it over; zero when it merely turns.
        private static float TurnSense(in BodyPose from, in BodyPose to)
        {
            Vector3 blade = Vector3.Slerp(from.Main.Forward, to.Main.Forward, 0.5f).normalized;
            Vector3 edge = Carry(from.Main.Forward, from.Edge, blade);
            float turn = Vector3.SignedAngle(edge, Carry(to.Main.Forward, to.Edge, blade), blade);

            if (Mathf.Abs(turn) < TurnOver)
                return 0f;

            Vector3 arm = (from.Main.Position + to.Main.Position) * 0.5f - s_mainShoulder;

            return Vector3.Dot(Quaternion.AngleAxis(turn * 0.5f, blade) * edge, arm) < 0f ? -Mathf.Sign(turn) : Mathf.Sign(turn);
        }

        /// The edge of a weapon whose blade turns the short way to a new direction.
        private static Vector3 Carry(Vector3 from, Vector3 edge, Vector3 blade)
        {
            return Vector3.ProjectOnPlane(Quaternion.FromToRotation(from, blade) * edge, blade).normalized;
        }

        /// Catmull-Rom for directions: a cubic Bezier on the unit sphere whose ends share their tangents with the
        /// neighbouring segments, so the weapon turns without a kink when it passes a key.
        private static Vector3 Arc(Vector3 before, Vector3 from, Vector3 to, Vector3 after, float t)
        {
            Vector3 leave = Exp(from, (Log(from, to) - Log(from, before)) / 6f);
            Vector3 arrive = Exp(to, (Log(to, from) - Log(to, after)) / 6f);
            Vector3 a = Vector3.Slerp(from, leave, t);
            Vector3 b = Vector3.Slerp(leave, arrive, t);
            Vector3 c = Vector3.Slerp(arrive, to, t);

            return Vector3.Slerp(Vector3.Slerp(a, b, t), Vector3.Slerp(b, c, t), t).normalized;
        }

        /// Tangent at 'from' pointing along the great circle to 'to'; its length is the angle between them.
        private static Vector3 Log(Vector3 from, Vector3 to)
        {
            float cos = Mathf.Clamp(Vector3.Dot(from, to), -1f, 1f);
            Vector3 direction = to - from * cos;

            return direction.sqrMagnitude < 1e-10f ? Vector3.zero : direction.normalized * Mathf.Acos(cos);
        }

        private static Vector3 Exp(Vector3 from, Vector3 tangent)
        {
            float angle = tangent.magnitude;

            return angle < 1e-5f ? from : from * Mathf.Cos(angle) + tangent / angle * Mathf.Sin(angle);
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                           (3f * p1 - p0 - 3f * p2 + p3) * t3);
        }

        /// Cubic ease from 0 to 1 that leaves with one slope and arrives with another.
        private static float Hermite(float t, float leave, float arrive)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            return (t3 - 2f * t2 + t) * leave + 3f * t2 - 2f * t3 + (t3 - t2) * arrive;
        }

        private static float ApplyEase(float alpha, Ease ease)
        {
            return ease switch
            {
                Ease.In => alpha * alpha,
                Ease.Out => 1f - (1f - alpha) * (1f - alpha),
                Ease.InOut => Mathf.SmoothStep(0f, 1f, alpha),
                _ => alpha
            };
        }
    }
}
