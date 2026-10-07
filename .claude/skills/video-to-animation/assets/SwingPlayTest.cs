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
        /// and strikes back. Writes play_{mode}.txt and screenshots into the folder; it is over after 9 seconds.
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

            if (s_mode == "combo")
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

            if (combat.State != CombatState.Attack || Mathf.Abs(frame - s_lastShot) < 6)
                return;

            s_lastShot = frame;
            ScreenCapture.CaptureScreenshot($"{s_folder}/play_{s_mode}_{(combat.IsRiposte ? "r" : combat.AttackIndex.ToString())}_{frame:000}.png");
        }
    }
}
