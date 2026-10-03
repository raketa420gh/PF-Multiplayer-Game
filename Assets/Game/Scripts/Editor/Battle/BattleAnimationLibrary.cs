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
        InOut
    }

    internal struct PoseKey
    {
        public float Time;
        public BodyPose Pose;
        public Ease Ease;

        public PoseKey(float time, BodyPose pose, Ease ease = Ease.InOut)
        {
            Time = time;
            Pose = pose;
            Ease = ease;
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
        public BodyPose Idle;
        public AttackDefinition[] Attacks = Array.Empty<AttackDefinition>();

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

        private static readonly Vector3 s_shieldRest = new(-0.24f, 1.3f, 0.28f);
        private static readonly Vector3 s_shieldRestNormal = new(-0.35f, 0f, 0.94f);
        private static readonly Vector3 s_shieldBack = new(-0.32f, 1.2f, 0.14f);
        private static readonly Vector3 s_shieldBackNormal = new(-0.7f, 0f, 0.7f);
        private static readonly Vector3 s_offHandRest = new(-0.24f, 0.84f, 0.1f);

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
                        WindupPose = SwordShield(new(0.36f, 1.7f, 0.1f), new(0.5f, 0.7f, -0.5f), yaw: 20f),
                        MidPose = SwordShield(new(0.2f, 1.56f, 0.46f), new(0.18f, 0.46f, 0.87f)),
                        EndPose = SwordShield(new(0f, 1.44f, 0.46f), new(-0.46f, -0.39f, 0.79f), yaw: -18f)
                    },
                    new AttackDefinition
                    {
                        Windup = 0.36f, Active = 0.2f, Recovery = 0.5f, ComboStart = 0.46f, ComboEnd = 0.86f,
                        Damage = 22, MoveMultiplier = 0.7f,
                        WindupPose = SwordShield(new(0f, 1.38f, 0.3f), new(-0.85f, 0.15f, 0.25f), yaw: -15f),
                        MidPose = SwordShield(new(0.14f, 1.38f, 0.48f), new(0f, 0.1f, 1f)),
                        EndPose = SwordShield(new(0.38f, 1.38f, 0.32f), new(0.7f, 0.05f, 0.7f), yaw: 15f)
                    },
                    new AttackDefinition
                    {
                        Windup = 0.45f, Active = 0.2f, Recovery = 0.6f, ComboStart = 0.55f, ComboEnd = 0.95f,
                        Damage = 30, MoveMultiplier = 0.6f, Stagger = 0.2f,
                        WindupPose = SwordShield(new(0.22f, 1.88f, 0f), new(0.1f, 0.5f, -0.85f), pitch: -10f),
                        MidPose = SwordShield(new(0.2f, 1.78f, 0.38f), new(-0.05f, 0.6f, 0.8f)),
                        EndPose = SwordShield(new(0.14f, 1.44f, 0.46f), new(-0.08f, -0.2f, 0.98f), pitch: 12f)
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

        public static WeaponDefinition CreateGreatsword()
        {
            return new WeaponDefinition
            {
                Prefix = "Greatsword",
                DisplayName = "Greatsword",
                Kind = WeaponKind.TwoHanded,
                Reach = 1.95f,
                DeflectDuration = 0.8f,
                BladeBase = 0.18f,
                BladeTip = 1.35f,
                Idle = TwoHanded(new(0.14f, 1.2f, 0.36f), new(-0.08f, 0.78f, 0.62f), yaw: 12f),
                Attacks = new[]
                {
                    new AttackDefinition
                    {
                        Windup = 0.6f, Active = 0.26f, Recovery = 0.7f, ComboStart = 0.75f, ComboEnd = 1.2f,
                        Damage = 38, MoveMultiplier = 0.5f, Stagger = 0.25f,
                        WindupPose = TwoHanded(new(0.3f, 1.45f, 0.12f), new(0.85f, 0.2f, -0.5f), yaw: 35f),
                        MidPose = TwoHanded(new(0.1f, 1.4f, 0.44f), new(-0.1f, 0.08f, 1f)),
                        EndPose = TwoHanded(new(-0.18f, 1.4f, 0.32f), new(-0.75f, -0.05f, 0.65f), yaw: -35f)
                    },
                    new AttackDefinition
                    {
                        Windup = 0.55f, Active = 0.26f, Recovery = 0.7f, ComboStart = 0.7f, ComboEnd = 1.15f,
                        Damage = 38, MoveMultiplier = 0.5f, Stagger = 0.25f,
                        WindupPose = TwoHanded(new(-0.18f, 1.45f, 0.2f), new(-0.85f, 0.2f, -0.45f), yaw: -30f),
                        MidPose = TwoHanded(new(0.06f, 1.4f, 0.44f), new(0.1f, 0.08f, 1f)),
                        EndPose = TwoHanded(new(0.32f, 1.4f, 0.24f), new(0.75f, -0.05f, 0.65f), yaw: 30f)
                    },
                    new AttackDefinition
                    {
                        Windup = 0.7f, Active = 0.26f, Recovery = 0.85f, ComboStart = 0.85f, ComboEnd = 1.3f,
                        Damage = 52, MoveMultiplier = 0.4f, Stagger = 0.4f,
                        WindupPose = TwoHanded(new(0.06f, 1.86f, 0.04f), new(0f, 0.55f, -0.83f), pitch: -12f),
                        MidPose = TwoHanded(new(0.06f, 1.74f, 0.36f), new(0f, 0.6f, 0.8f)),
                        EndPose = TwoHanded(new(0.06f, 1.4f, 0.42f), new(0f, -0.3f, 0.95f), pitch: 12f)
                    }
                },
                CanBlock = true,
                BlockRaise = 0.25f,
                BlockMitigation = 0.75f,
                BlockImpact = 0.3f,
                BlockRecovery = 0.45f,
                BlockAngle = 80f,
                BlockMove = 0.5f,
                Block = TwoHanded(new(0.4f, 1.42f, 0.34f), new(-0.9f, 0.35f, 0.05f), 0.6f),
                BlockHit = TwoHanded(new(0.38f, 1.24f, 0.26f), new(-0.9f, 0.3f, -0.15f), 0.6f, pitch: -5f),
                BlockLowered = TwoHanded(new(0.36f, 1.2f, 0.3f), new(-0.9f, 0.2f, 0f), 0.6f),
                DeflectPose = TwoHanded(new(0.2f, 1.6f, 0.2f), new(0.3f, 0.9f, 0.2f), yaw: 15f, pitch: -8f),
                BlockSocket = WeaponSocket.RightHand,
                BlockBoxCenter = new Vector3(0f, 0f, 0.55f),
                BlockBoxExtents = new Vector3(0.09f, 0.09f, 0.5f)
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

        public static List<PoseKey> AttackKeys(WeaponDefinition weapon, AttackDefinition attack)
        {
            return new List<PoseKey>
            {
                new(0f, weapon.Idle),
                new(attack.Windup, attack.WindupPose, Ease.Out),
                new(attack.Windup + attack.Active * 0.5f, attack.MidPose, Ease.In),
                new(attack.Windup + attack.Active, attack.EndPose, Ease.Linear),
                new(attack.Duration, weapon.Idle)
            };
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
                new(0f, weapon.Attacks[0].MidPose),
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

        public static BodyPose Sample(List<PoseKey> keys, float time)
        {
            int next = 1;

            while (next < keys.Count - 1 && time > keys[next].Time)
                next++;

            PoseKey from = keys[next - 1];
            PoseKey to = keys[next];
            float alpha = ApplyEase(Mathf.InverseLerp(from.Time, to.Time, time), to.Ease);

            BodyPose before = keys[Mathf.Max(next - 2, 0)].Pose;
            BodyPose after = keys[Mathf.Min(next + 1, keys.Count - 1)].Pose;
            BodyPose a = from.Pose;
            BodyPose b = to.Pose;

            BodyPose pose = a;
            pose.Spine = Vector3.Lerp(a.Spine, b.Spine, alpha);
            pose.Head = Vector3.Lerp(a.Head, b.Head, alpha);
            pose.Main = Interpolate(before.Main, a.Main, b.Main, after.Main, alpha);
            pose.Off = Interpolate(before.Off, a.Off, b.Off, after.Off, alpha);

            return pose;
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

        public static List<PoseKey> CastKeys()
        {
            return new List<PoseKey>
            {
                new(0f, Cast(0.22f, 0.3f, 1.25f)),
                new(0.35f, Cast(0.16f, 0.42f, 1.42f, -4f), Ease.Out),
                new(1f, Cast(0.14f, 0.5f, 1.45f, -6f)),
                new(1.4f, Cast(0.26f, 0.62f, 1.4f, 4f), Ease.In),
                new(2f, Cast(0.22f, 0.3f, 1.25f))
            };
        }

        public static List<PoseKey> UseKeys()
        {
            return new List<PoseKey>
            {
                new(0f, OneHanded(new(0.26f, 1.05f, 0.3f), new(0.2f, 0.4f, 0.9f))),
                new(0.4f, OneHanded(new(0.08f, 1.52f, 0.22f), new(-0.3f, 0.95f, 0.1f), pitch: 6f), Ease.Out),
                new(1.6f, OneHanded(new(0.06f, 1.55f, 0.2f), new(-0.3f, 0.95f, 0.1f), pitch: 8f)),
                new(2f, OneHanded(new(0.26f, 1.05f, 0.3f), new(0.2f, 0.4f, 0.9f)))
            };
        }

        public static List<PoseKey> InteractKeys()
        {
            return new List<PoseKey>
            {
                new(0f, OneHanded(new(0.26f, 1.05f, 0.3f), new(0.2f, 0.4f, 0.9f))),
                new(0.3f, OneHanded(new(0.2f, 1.12f, 0.62f), new(0.1f, -0.5f, 0.85f), pitch: 10f), Ease.Out),
                new(1.7f, OneHanded(new(0.16f, 1.08f, 0.66f), new(0.1f, -0.5f, 0.85f), pitch: 12f)),
                new(2f, OneHanded(new(0.26f, 1.05f, 0.3f), new(0.2f, 0.4f, 0.9f)))
            };
        }

        public static BodyPose Idle(float time)
        {
            return Standing(0.03f + Mathf.Sin(time * Mathf.PI) * 0.004f);
        }

        public static BodyPose Walk(float phase, Vector2 direction, float stride, float lift, float drop, float lean = 0f)
        {
            BodyPose pose = Standing(drop + Mathf.Cos(phase * Mathf.PI * 4f) * 0.012f);
            pose.HipsEuler = new Vector3(lean, 0f, 0f);
            Step(phase, direction, stride, lift, out pose.LeftFoot, out pose.LeftFootEuler);
            Step(Mathf.Repeat(phase + 0.5f, 1f), direction, stride, lift, out pose.RightFoot, out pose.RightFootEuler);

            return pose;
        }

        public static BodyPose Air()
        {
            BodyPose pose = Standing(0f);
            pose.LeftFoot = new Vector3(0f, 0.12f, 0.14f);
            pose.RightFoot = new Vector3(0f, 0.22f, -0.1f);
            pose.RightFootEuler = new Vector3(25f, 0f, 0f);

            return pose;
        }

        public static BodyPose Death(float alpha)
        {
            float fall = Mathf.SmoothStep(0f, 1f, alpha);
            BodyPose pose = Standing(0f);
            pose.Hips = new Vector3(0f, -0.84f * fall, -0.45f * fall);
            pose.HipsEuler = new Vector3(-82f * fall, 0f, 6f * fall);
            pose.Spine = new Vector3(-6f * fall, 0f, 0f);
            pose.LeftFoot = new Vector3(-0.08f * fall, 0f, 0.25f * fall);
            pose.RightFoot = new Vector3(0.06f * fall, 0f, 0.1f * fall);
            pose.LeftFootEuler = pose.RightFootEuler = new Vector3(-50f * fall, 0f, 0f);
            pose.ArmDrop = Mathf.Lerp(70f, 25f, fall);

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

        private static HandPose Interpolate(in HandPose before, in HandPose from, in HandPose to, in HandPose after, float alpha)
        {
            return new HandPose
            {
                Position = CatmullRom(before.Position, from.Position, to.Position, after.Position, alpha),
                Forward = Vector3.Slerp(from.Forward, to.Forward, alpha),
                Up = from.IsAutoRoll || to.IsAutoRoll ? Vector3.zero : Vector3.Slerp(from.Up, to.Up, alpha)
            };
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                           (3f * p1 - p0 - 3f * p2 + p3) * t3);
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
