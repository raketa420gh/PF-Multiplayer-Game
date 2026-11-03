using UnityEngine;

namespace Game.Scripts
{
    /// Degrees the camera turns per unit of mouse movement, kept between launches.
    public static class MouseSettings
    {
        public const float MinSensitivity = 0.2f;
        public const float MaxSensitivity = 6f;

        public static float Sensitivity => s_sensitivity;

        private const string Key = "MouseSensitivity";
        private const float DefaultSensitivity = 2f;

        private static float s_sensitivity = DefaultSensitivity;

        public static void SetSensitivity(float sensitivity)
        {
            s_sensitivity = Mathf.Clamp(sensitivity, MinSensitivity, MaxSensitivity);
            PlayerPrefs.SetFloat(Key, s_sensitivity);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Load()
        {
            s_sensitivity = PlayerPrefs.GetFloat(Key, DefaultSensitivity);
        }
    }
}
