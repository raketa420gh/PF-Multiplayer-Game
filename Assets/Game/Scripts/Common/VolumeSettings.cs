using UnityEngine;

namespace Game.Scripts
{
    /// Master volume of every sound, kept between launches.
    public static class VolumeSettings
    {
        private const string Key = "MasterVolume";

        public static float Volume => AudioListener.volume;

        public static void SetVolume(float volume)
        {
            AudioListener.volume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(Key, AudioListener.volume);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Load()
        {
            AudioListener.volume = PlayerPrefs.GetFloat(Key, 1f);
        }
    }
}
