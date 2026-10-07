using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Game.Scripts.Battle;
using Game.Scripts.Editor.Dungeon;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Scripts.Editor.Battle
{
    /// Poses a swing straight from the library, without building clips: the numbers the content builder would complain
    /// about and renders to set beside reference footage. Called from outside the editor while poses are being
    /// authored (.claude/skills/video-to-animation).
    internal static class SwingPreview
    {
        private const int Layer = 31;
        private const float FrameRate = BattleAnimationBuilder.FrameRate;
        // The limits of BattleContentBuilder.CheckPeak and CheckSwing, and what an arm can do.
        private const float MaxPeakMiss = 0.01f;
        private const float MaxRoll = 25f;
        private const float MaxLean = 15f;
        private const float UnderWay = 0.4f;
        private const float MaxWrist = 90f;
        private const float MaxReachMiss = 0.02f;
        // Humanoid muscles run -1..1 between the avatar's limits; the clip clamps past that, and the forearm twists.
        private const float MaxTwist = 1f;
        private const float MaxElbowMiss = 30f;

        private sealed class Motion
        {
            public WeaponDefinition Weapon;
            public AttackDefinition Attack;
            public List<PoseKey> Keys;
            public BodyPose[] Poses;
        }

        /// Swings: numbers of the series, "r" for the riposte, "idle", "block", "impact", "deflect". Views: fp, front, side, top,
        /// or none for the numbers alone. Frames: clip frames to render, or none for the frames of the keys. Writes
        /// report_{swing}.txt and g_{swing}_{view}_{frame}.png into the folder and returns what is out of limits.
        public static string Run(string prefix, string swings, string views, string frames, string folder, int size)
        {
            Directory.CreateDirectory(folder);
            StringBuilder summary = new StringBuilder();

            foreach (string swing in Split(swings))
            {
                summary.AppendLine(Report(prefix, swing, folder, out int[] keyFrames));
                int[] shots = frames.Length > 0 ? Array.ConvertAll(Split(frames), int.Parse) : keyFrames;

                if (views.Length > 0)
                    Shoot(prefix, swing, Split(views), shots, folder, size);
            }

            return summary.ToString();
        }

        private static string[] Split(string list)
        {
            return list.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static Motion Sample(BattlePoseRig rig, string prefix, string swing)
        {
            WeaponDefinition weapon = DungeonWeaponLibrary.Find(DungeonWeaponLibrary.CreateAll(), prefix)
                                      ?? throw new ArgumentException($"No weapon with the prefix '{prefix}'");
            BattleAnimationBuilder.Settle(rig, weapon);
            AttackDefinition attack = swing == "r" ? weapon.Riposte : int.TryParse(swing, out int index) ? weapon.Attacks[index] : null;
            List<PoseKey> keys = swing switch
            {
                "idle" => BattleAnimationLibrary.IdleKeys(weapon),
                "block" => BattleAnimationLibrary.BlockKeys(weapon),
                "impact" => BattleAnimationLibrary.BlockImpactKeys(weapon),
                "deflect" => BattleAnimationLibrary.DeflectKeys(weapon),
                "lower" => BattleAnimationLibrary.BlockLowerKeys(weapon),
                _ => BattleAnimationLibrary.AttackKeys(weapon, attack ?? throw new ArgumentException($"'{prefix}' has no swing '{swing}'"))
            };
            float duration = keys[^1].Time;
            BodyPose[] poses = new BodyPose[Mathf.CeilToInt(duration * FrameRate) + 1];

            for (int frame = 0; frame < poses.Length; frame++)
                poses[frame] = BattleAnimationLibrary.Sample(keys, Mathf.Min(frame / FrameRate, duration));

            return new Motion { Weapon = weapon, Attack = attack, Keys = keys, Poses = poses };
        }

        private static string Report(string prefix, string swing, string folder, out int[] keyFrames)
        {
            using BattlePoseRig rig = new BattlePoseRig();
            Motion motion = Sample(rig, prefix, swing);
            WeaponDefinition weapon = motion.Weapon;
            AttackDefinition attack = motion.Attack;
            BodyPose[] poses = motion.Poses;
            rig.Plan(poses, false);

            Animator animator = rig.Animator;
            Transform socket = rig.Sockets[(int)WeaponSocket.RightHand];
            Transform offSocket = rig.Sockets[(int)poses[0].OffSocket];
            Vector3[] strike = new Vector3[poses.Length];
            Quaternion[] rotations = new Quaternion[poses.Length];
            float[] worst = new float[9];
            int[] worstFrame = new int[9];
            StringBuilder table = new StringBuilder("frame | grip x y z | blade yaw elev | strike point x y z | reach miss main off | wrist main off | roll | elbow main x y z | elbow off x y z | twist main off | muscles main arm fore off arm fore\n");

            for (int frame = 0; frame < poses.Length; frame++)
            {
                rig.Apply(poses[frame], frame);
                Vector3 blade = socket.forward;
                strike[frame] = socket.position + blade * weapon.StrikeOf(attack);
                rotations[frame] = socket.rotation;
                (socket.rotation * Quaternion.Inverse(rotations[Mathf.Max(frame - 1, 0)])).ToAngleAxis(out float angle, out Vector3 axis);

                float[] values =
                {
                    Vector3.Angle(Quaternion.AngleAxis(poses[frame].Lean, socket.right) * socket.up, Forearm(animator, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand)),
                    Vector3.Angle(offSocket.up, Forearm(animator, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand)),
                    frame == 0 ? 0f : Mathf.Abs(Mathf.DeltaAngle(0f, angle) * Vector3.Dot(axis, blade)),
                    Vector3.Distance(socket.position, poses[frame].Main.Position),
                    Vector3.Distance(offSocket.position, poses[frame].Off.Position),
                    Twist(rig, "Right"),
                    Twist(rig, "Left"),
                    ElbowMiss(animator, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, poses[frame].Main),
                    ElbowMiss(animator, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, poses[frame].Off)
                };
                Vector3 elbow = animator.GetBoneTransform(HumanBodyBones.RightLowerArm).position;
                Vector3 offElbow = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm).position;

                for (int i = 0; i < values.Length; i++)
                {
                    if (values[i] <= worst[i])
                        continue;

                    worst[i] = values[i];
                    worstFrame[i] = frame;
                }

                table.AppendLine(FormattableString.Invariant(
                    $"{frame,3} | {socket.position.x,5:0.00} {socket.position.y,5:0.00} {socket.position.z,5:0.00} | {Mathf.Atan2(blade.x, blade.z) * Mathf.Rad2Deg,5:0} {Mathf.Asin(blade.y) * Mathf.Rad2Deg,4:0} | {strike[frame].x,5:0.00} {strike[frame].y,5:0.00} {strike[frame].z,5:0.00} | {values[3]:0.00} {values[4]:0.00} | {values[0],3:0} {values[1],3:0} | {values[2],3:0} | {elbow.x,5:0.00} {elbow.y,5:0.00} {elbow.z,5:0.00} | {offElbow.x,5:0.00} {offElbow.y,5:0.00} {offElbow.z,5:0.00} | {values[5],4:0.00} {values[6],4:0.00} | {Signed(rig, "Right Arm Twist In-Out"),5:0.00} {Signed(rig, "Right Forearm Twist In-Out"),5:0.00} {Signed(rig, "Left Arm Twist In-Out"),5:0.00} {Signed(rig, "Left Forearm Twist In-Out"),5:0.00}"));
            }

            keyFrames = motion.Keys.Select(key => Mathf.RoundToInt(key.Time * FrameRate)).Distinct().ToArray();
            File.WriteAllText($"{folder}/report_{swing}.txt", table.ToString());

            StringBuilder summary = new StringBuilder(FormattableString.Invariant($"{prefix} {swing}: {poses.Length - 1} frames, keys at {string.Join(" ", keyFrames)}"));

            if (attack != null)
            {
                summary.Append(FormattableString.Invariant(
                    $", active {attack.Windup * FrameRate:0.#}-{(attack.Windup + attack.Active) * FrameRate:0.#}, peak {BattleAnimationLibrary.PeakTime(attack) * FrameRate:0}"));
                Flag(summary, "peak misses the crosshair by", PeakMiss(weapon, attack, socket, rig, poses), MaxPeakMiss, -1, "0.000 m");

                if (BattleAnimationLibrary.IsCut(weapon, attack) && !weapon.IsUnarmed && !weapon.IsRound)
                    Flag(summary, "edge off the path of the cut by", Lean(attack, strike, rotations, weapon.IsEdgeBack ? Vector3.down : Vector3.up), MaxLean, -1, "0 deg");
            }

            Flag(summary, "main wrist bent", worst[0], MaxWrist, worstFrame[0], "0 deg");
            Flag(summary, "off wrist bent", worst[1], MaxWrist, worstFrame[1], "0 deg");
            Flag(summary, "weapon spins within a frame", weapon.IsHeldAcross ? 0f : worst[2], MaxRoll, worstFrame[2], "0 deg");
            Flag(summary, "main hand short of its target by", worst[3], MaxReachMiss, worstFrame[3], "0.00 m");
            Flag(summary, "off hand short of its target by", worst[4], MaxReachMiss, worstFrame[4], "0.00 m");
            Flag(summary, "main arm twist muscle at", worst[5], MaxTwist, worstFrame[5], "0.00");
            Flag(summary, "off arm twist muscle at", worst[6], MaxTwist, worstFrame[6], "0.00");
            Flag(summary, "main elbow off the authored one by", worst[7], MaxElbowMiss, worstFrame[7], "0 deg");
            Flag(summary, "off elbow off the authored one by", worst[8], MaxElbowMiss, worstFrame[8], "0 deg");

            return summary.ToString();
        }

        private static WeaponSocket Mirror(WeaponSocket socket)
        {
            return socket switch
            {
                WeaponSocket.RightHand => WeaponSocket.LeftHand,
                WeaponSocket.LeftHand => WeaponSocket.RightHand,
                WeaponSocket.RightShield => WeaponSocket.LeftShield,
                _ => WeaponSocket.RightShield
            };
        }

        private static void Flag(StringBuilder summary, string what, float value, float limit, int frame, string format)
        {
            string at = frame >= 0 ? $" at {frame}" : string.Empty;
            summary.Append($"\n  {(value > limit ? "FAIL" : "ok  ")} {what} {value.ToString(format, System.Globalization.CultureInfo.InvariantCulture)}{at}");
        }

        /// The larger of the arm's and the forearm's twist muscles, as the clip will store them: past 1 the avatar clamps
        /// the roll and the forearm reads as wrung.
        private static float Twist(BattlePoseRig rig, string side)
        {
            float[] muscles = rig.Capture().muscles;

            return Mathf.Max(Mathf.Abs(muscles[Muscle($"{side} Forearm Twist In-Out")]), Mathf.Abs(muscles[Muscle($"{side} Arm Twist In-Out")]));
        }

        private static float Signed(BattlePoseRig rig, string muscle)
        {
            return rig.Capture().muscles[Muscle(muscle)];
        }

        private static int Muscle(string name)
        {
            return Array.IndexOf(HumanTrait.MuscleName, name);
        }

        /// How far round the swivel circle (about the shoulder-to-wrist line) the solved elbow is from the authored one:
        /// the authored elbow need not be at arm's length, only on the right side.
        private static float ElbowMiss(Animator animator, HumanBodyBones upperArm, HumanBodyBones lowerArm, HumanBodyBones hand, in HandPose pose)
        {
            Vector3 shoulder = animator.GetBoneTransform(upperArm).position;
            Vector3 axis = animator.GetBoneTransform(hand).position - shoulder;
            Vector3 authored = Vector3.ProjectOnPlane(pose.Elbow - shoulder, axis);
            Vector3 solved = Vector3.ProjectOnPlane(animator.GetBoneTransform(lowerArm).position - shoulder, axis);

            return pose.ElbowWeight > 0.99f && authored.sqrMagnitude > 1e-4f && solved.sqrMagnitude > 1e-4f ? Vector3.Angle(solved, authored) : 0f;
        }

        private static Vector3 Forearm(Animator animator, HumanBodyBones lowerArm, HumanBodyBones hand)
        {
            return (animator.GetBoneTransform(hand).position - animator.GetBoneTransform(lowerArm).position).normalized;
        }

        /// How far the blade passes from the crosshair ray at the peak, as BattleContentBuilder.CheckPeak measures it.
        private static float PeakMiss(WeaponDefinition weapon, AttackDefinition attack, Transform socket, BattlePoseRig rig, BodyPose[] poses)
        {
            int frame = Mathf.RoundToInt(BattleAnimationLibrary.PeakTime(attack) * FrameRate);
            rig.Apply(poses[frame], frame);
            Vector2 eye = BattleAnimationLibrary.Eye;
            Vector2 start = socket.TransformPoint(0f, 0f, weapon.BladeBase);
            Vector2 blade = (Vector2)socket.TransformPoint(0f, 0f, weapon.BladeTip) - start;
            float along = Mathf.Clamp01(Vector2.Dot(eye - start, blade) / Mathf.Max(blade.sqrMagnitude, 1e-6f));

            return Vector2.Distance(eye, start + blade * along);
        }

        /// How far the leading edge is from the travel of the strike point while the cut is active and under way, as
        /// BattleContentBuilder.CheckSwing measures it.
        private static float Lean(AttackDefinition attack, Vector3[] strike, Quaternion[] rotations, Vector3 edge)
        {
            float fastest = 0f;
            float lean = 0f;

            for (int i = 1; i < strike.Length - 1; i++)
                fastest = Mathf.Max(fastest, IsActive(attack, i) ? (strike[i + 1] - strike[i - 1]).magnitude : 0f);

            for (int i = 1; i < strike.Length - 1; i++)
            {
                Vector3 travel = strike[i + 1] - strike[i - 1];
                Vector3 across = Vector3.ProjectOnPlane(travel, rotations[i] * Vector3.forward);

                if (IsActive(attack, i) && travel.magnitude > fastest * UnderWay && across.magnitude > travel.magnitude * 0.5f)
                    lean = Mathf.Max(lean, Vector3.Angle(rotations[i] * edge, across));
            }

            return lean;
        }

        private static bool IsActive(AttackDefinition attack, int frame)
        {
            float time = frame / FrameRate;

            return time >= attack.Windup && time <= attack.Windup + attack.Active;
        }

        private static void Shoot(string prefix, string swing, string[] views, int[] frames, string folder, int size)
        {
            // Renders of earlier keys would be picked up by footage.py as frames of this motion.
            foreach (string view in views)
            {
                foreach (string stale in Directory.GetFiles(folder, $"g_{swing}_{view}_*.png"))
                    File.Delete(stale);
            }

            WeaponConfig config = AssetDatabase.LoadAssetAtPath<WeaponConfig>($"{BattleEditorUtility.ConfigsFolder}/{prefix}.asset");
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(0f, -0.02f, 0f);
            floor.transform.localScale = new Vector3(3f, 0.04f, 3f);
            List<GameObject> stage = new List<GameObject> { floor, new GameObject("Camera"), CreateLight(1.3f, 160f), CreateLight(0.7f, -40f) };
            bool asyncCompile = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;

            try
            {
                foreach (GameObject item in stage)
                {
                    item.hideFlags = HideFlags.HideAndDontSave;
                    item.layer = Layer;
                }

                Camera camera = stage[1].AddComponent<Camera>();
                camera.nearClipPlane = 0.04f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.32f, 0.36f, 0.42f);
                camera.cullingMask = 1 << Layer;

                foreach (int frame in frames)
                {
                    // Skinning is evaluated once per editor frame, so every moment gets a rig of its own.
                    using BattlePoseRig rig = new BattlePoseRig();
                    BodyPose[] poses = Sample(rig, prefix, swing).Poses;
                    int index = Mathf.Clamp(frame, 0, poses.Length - 1);
                    rig.Plan(poses, false);
                    rig.Apply(poses[index], index);

                    // The clips are authored right-handed: a weapon the game plays mirrored is shown in the other hand, mirrored with it.
                    foreach (WeaponAttachment attachment in config != null ? config.Attachments : Array.Empty<WeaponAttachment>())
                    {
                        bool isMirrored = config.IsMirrored;
                        GameObject instance = Object.Instantiate(attachment.Prefab, rig.Sockets[(int)(isMirrored ? Mirror(attachment.Socket) : attachment.Socket)], false);
                        instance.hideFlags = HideFlags.HideAndDontSave;
                        instance.transform.localScale = new Vector3(isMirrored ? -1f : 1f, 1f, 1f);

                        if (instance.TryGetComponent(out WeaponVisual visual))
                            visual.SetClosed(swing != "idle");
                    }

                    foreach (Transform child in rig.Animator.GetComponentsInChildren<Transform>(true))
                        child.gameObject.layer = Layer;

                    foreach (string view in views)
                    {
                        rig.Animator.GetBoneTransform(HumanBodyBones.Head).localScale = view == "fp" ? Vector3.zero : Vector3.one;
                        Place(camera, view);
                        Render(camera, view == "fp" ? size * 16 / 9 : size, size, $"{folder}/g_{swing}_{view}_{frame}.png");
                    }
                }
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = asyncCompile;

                foreach (GameObject item in stage)
                    Object.DestroyImmediate(item);
            }
        }

        private static GameObject CreateLight(float intensity, float yaw)
        {
            GameObject item = new GameObject("Light");
            Light light = item.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.cullingMask = 1 << Layer;
            light.transform.rotation = Quaternion.Euler(40f, yaw, 0f);

            return item;
        }

        /// The player's own eyes at the game's field of view, or the fighter from the front (it faces the camera, its
        /// right is on the left of the picture), from its right side, or from above with its front down the picture.
        private static void Place(Camera camera, string view)
        {
            camera.fieldOfView = 35f;

            switch (view)
            {
                case "fp":
                    camera.fieldOfView = 75f;
                    camera.transform.SetPositionAndRotation(BattleAnimationLibrary.Eye, Quaternion.identity);
                    break;
                case "front":
                    camera.transform.position = new Vector3(-0.6f, 1.6f, 4.6f);
                    camera.transform.LookAt(new Vector3(0f, 1.15f, 0f));
                    break;
                case "side":
                    camera.transform.position = new Vector3(4.4f, 2.1f, 1.3f);
                    camera.transform.LookAt(new Vector3(0f, 1.15f, 0.2f));
                    break;
                default:
                    camera.transform.SetPositionAndRotation(new Vector3(0f, 7f, 0.5f), Quaternion.LookRotation(Vector3.down, Vector3.back));
                    break;
            }
        }

        private static void Render(Camera camera, int width, int height, string path)
        {
            RenderTexture texture = RenderTexture.GetTemporary(width, height, 24);
            camera.targetTexture = texture;
            camera.Render();
            RenderTexture.active = texture;
            Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            image.Apply();
            RenderTexture.active = null;
            camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(texture);
            File.WriteAllBytes(path, image.EncodeToPNG());
            Object.DestroyImmediate(image);
        }
    }
}
