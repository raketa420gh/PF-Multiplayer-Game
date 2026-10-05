using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Short-lived flash of coloured light: prayers, bursts of holy light.
    public sealed class LightFlashEffect : MonoBehaviour
    {
        private const float Lifetime = 0.6f;
        private const float Intensity = 6f;

        private Light _light;
        private float _age;

        private void Update()
        {
            _age += Time.deltaTime;
            float fade = 1f - _age / Lifetime;

            if (fade <= 0f)
            {
                Destroy(gameObject);

                return;
            }

            _light.intensity = Intensity * fade * fade;
        }

        public static void Play(Vector3 position, Color color, float range)
        {
            LightFlashEffect flash = new GameObject("LightFlash").AddComponent<LightFlashEffect>();
            flash.transform.position = position;
            flash._light = flash.gameObject.AddComponent<Light>();
            flash._light.color = color;
            flash._light.range = range;
            flash._light.intensity = Intensity;
            DungeonAudioComponent.Play(DungeonSound.Cast, position, 0.7f, 1.3f);
        }
    }
}
