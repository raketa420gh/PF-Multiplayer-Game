using System;
using UnityEngine;

namespace Game.Scripts.Battle
{
    /// One switch for all combat debug drawing: block/shield hitboxes, swing traces and projectile paths.
    public static class BattleDebugSettings
    {
        public static event Action<bool> OnChanged;

        public static bool IsEnabled => s_isEnabled;

        private const string Key = "BattleDebugDraw";

        private static bool s_isEnabled = PlayerPrefs.GetInt(Key, 0) == 1;

        public static void Toggle()
        {
            SetEnabled(!s_isEnabled);
        }

        public static void SetEnabled(bool isEnabled)
        {
            if (s_isEnabled == isEnabled)
                return;

            s_isEnabled = isEnabled;
            PlayerPrefs.SetInt(Key, isEnabled ? 1 : 0);
            OnChanged?.Invoke(isEnabled);
        }
    }
}
