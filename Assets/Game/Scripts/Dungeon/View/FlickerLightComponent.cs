using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Torch / brazier / candle flicker. Purely visual, runs on every peer.
    public sealed class FlickerLightComponent : MonoBehaviour
    {
        [SerializeField]
        private Light _light;

        [SerializeField]
        private float _baseIntensity = 2f;

        [SerializeField]
        private float _amplitude = 0.6f;

        [SerializeField]
        private float _speed = 9f;

        private float _seed;

        private void Awake()
        {
            _seed = Random.Range(0f, 100f);

            if (_light == null)
                _light = GetComponent<Light>();
        }

        private void Update()
        {
            if (!_light.enabled)
                return;

            float time = Time.time * _speed + _seed;
            float noise = Mathf.PerlinNoise(time, _seed) * 0.7f + Mathf.PerlinNoise(time * 2.3f, _seed + 5f) * 0.3f;
            _light.intensity = _baseIntensity + (noise - 0.5f) * 2f * _amplitude;
        }
    }
}
