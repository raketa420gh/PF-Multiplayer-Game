using System;
using System.IO;
using System.Reflection;
using System.Text;
using Fusion;
using Game.Scripts.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Battle
{
    /// TEMPORARY, from .claude/skills/video-to-animation: copy into Assets/Game/Scripts/Editor/Battle for a play test in
    /// BattleScene, delete it (and its .meta) afterwards. Holds the buttons for the local fighter and logs what combat does.
    internal static class SwingPlayTest
    {
        private static string s_mode;
        private static string s_folder;
        private static double s_start;
        private static bool s_isHit;
        private static int s_lastShot;
        private static int s_lastTick;
        private static StringBuilder s_log;

        /// Mode "combo" holds the attack button for the whole series; "riposte" raises the block, takes a blocked hit
        /// and strikes back; "block" holds the block for 4 seconds and logs how each visible weapon part stands in the
        /// view (its axes in camera space: right, up, forward; the screen point of its middle). Writes play_{mode}.txt,
        /// play_{mode}_idle.png (first second, at rest) and screenshots into the folder; it is over after 9 seconds.
        /// "no local fighter yet" for more than ~20 s: the session did not start — stop play, enter it again in a
        /// separate call (not in the call that opens the scene) and retry.
        public static string Begin(string weaponName, string mode, string folder)
        {
            FighterComponent fighter = BattleContext.Instance != null ? BattleContext.Instance.LocalFighter : null;

            if (fighter == null)
                return "no local fighter yet";

            CombatComponent combat = fighter.Combat;
            int index = Array.FindIndex(combat.Catalog, weapon => weapon.DisplayName == weaponName);

            if (index < 0)
                return $"no weapon named '{weaponName}' in the catalog";

            combat.SetSlotWeapon(combat.WeaponSlot, index);
            Directory.CreateDirectory(folder);
            s_mode = mode;
            s_folder = folder;
            s_start = EditorApplication.timeSinceStartup;
            s_isHit = false;
            s_lastShot = -100;
            s_lastTick = -1;
            s_log = new StringBuilder();
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            Animator animator = fighter.GetComponentInChildren<Animator>();

            return $"begin {mode}: catalog {index}, attacks {combat.Weapon.Attacks.Length}, riposte {combat.Weapon.HasRiposte}, " +
                   $"action speed {combat.ActionSpeed:0.000}, animator layers {animator.layerCount}";
        }

        private static void Tick()
        {
            FighterComponent fighter = EditorApplication.isPlaying && BattleContext.Instance != null ? BattleContext.Instance.LocalFighter : null;
            float time = (float)(EditorApplication.timeSinceStartup - s_start);

            if (fighter == null || time > (s_mode == "combo" ? 9f : 5.5f))
            {
                EditorApplication.update -= Tick;
                File.WriteAllText($"{s_folder}/play_{s_mode}.txt", s_log.ToString());

                return;
            }

            CombatComponent combat = fighter.Combat;
            int bits = 0;

            if (time > 0.6f && s_lastShot == -100)
            {
                s_lastShot = -99;
                ScreenCapture.CaptureScreenshot($"{s_folder}/play_{s_mode}_idle.png");
            }

            if (s_mode == "block")
            {
                if (time > 1f && time < 5f)
                    bits |= 1 << (int)PlayerInputButtons.Secondary;
            }
            else if (s_mode == "combo")
            {
                if (time > 1f && time < 7.5f)
                    bits |= 1 << (int)PlayerInputButtons.Primary;
            }
            else
            {
                if (time > 1f && time < 2.7f)
                    bits |= 1 << (int)PlayerInputButtons.Secondary;

                if (time > 2.2f && !s_isHit)
                {
                    s_isHit = true;
                    ((DamageReceiverComponent.IOwner)combat).OnHitReceived(HitResult.Blocked, 0f, 1);
                }

                if (time > 2.3f && time < 2.5f)
                    bits |= 1 << (int)PlayerInputButtons.Primary;
            }

            BattleInputPolling input = BattleContext.Instance.Input;
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(BattleInputPolling).GetField("_buttons", flags).SetValue(input, new NetworkButtons(bits));
            typeof(BattleInputPolling).GetField("_resetButtons", flags).SetValue(input, false);

            if (combat.Runner.Tick == s_lastTick)
                return;

            s_lastTick = combat.Runner.Tick;
            int frame = Mathf.RoundToInt(combat.StateTime * 60f);
            s_log.AppendLine(FormattableString.Invariant($"{time:0.00} {combat.State} attack {combat.AttackIndex} riposte {combat.IsRiposte} frame {frame} {combat.Phase}"));

            if (s_mode == "block" && combat.State == CombatState.Block)
            {
                LogWeapon(fighter);

                if (frame - s_lastShot >= 60)
                {
                    s_lastShot = frame;
                    ScreenCapture.CaptureScreenshot($"{s_folder}/play_block_{frame:000}.png");
                }

                return;
            }

            if (combat.State != CombatState.Attack || Mathf.Abs(frame - s_lastShot) < 6)
                return;

            s_lastShot = frame;
            ScreenCapture.CaptureScreenshot($"{s_folder}/play_{s_mode}_{(combat.IsRiposte ? "r" : combat.AttackIndex.ToString())}_{frame:000}.png");
        }

        /// Mesh axes only: a part whose mesh was turned when it was built (a book pinched at an angle) needs that turn
        /// undone by hand. Never read a weapon's direction off Renderer.bounds — that is the box of the turned local box.
        private static void LogWeapon(FighterComponent fighter)
        {
            Transform camera = Camera.main.transform;

            foreach (WeaponVisual visual in fighter.GetComponentsInChildren<WeaponVisual>())
            {
                foreach (MeshRenderer part in visual.GetComponentsInChildren<MeshRenderer>())
                {
                    Transform t = part.transform;
                    Vector3 right = camera.InverseTransformDirection(t.right);
                    Vector3 up = camera.InverseTransformDirection(t.up);
                    Vector3 forward = camera.InverseTransformDirection(t.forward);
                    Vector3 view = Camera.main.WorldToViewportPoint(part.bounds.center);
                    s_log.AppendLine(FormattableString.Invariant(
                        $"  {part.name} right {right.x:0.00} {right.y:0.00} {right.z:0.00} up {up.x:0.00} {up.y:0.00} {up.z:0.00} forward {forward.x:0.00} {forward.y:0.00} {forward.z:0.00} view {view.x:0.00} {1f - view.y:0.00}"));
                }
            }
        }
    }
}
