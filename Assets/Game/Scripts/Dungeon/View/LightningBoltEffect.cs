using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Short-lived jagged bolt with a flash of light: chain lightning between bodies, a lightning strike from the sky.
    public sealed class LightningBoltEffect : MonoBehaviour
    {
        private const float Lifetime = 0.3f;
        private const float SegmentLength = 0.6f;

        private static Material s_material;

        private LineRenderer _line;
        private Light _light;
        private float _age;
        private float _width;
        private float _intensity;

        private void Update()
        {
            _age += Time.deltaTime;
            float fade = 1f - _age / Lifetime;

            if (fade <= 0f)
            {
                Destroy(gameObject);

                return;
            }

            _line.widthMultiplier = _width * fade;
            _light.intensity = _intensity * fade;
        }

        public static void Play(Vector3 from, Vector3 to, bool isStrike)
        {
            s_material ??= new Material(Shader.Find("Sprites/Default")) { color = new Color(0.75f, 0.85f, 1f) };

            LightningBoltEffect bolt = new GameObject("LightningBolt").AddComponent<LightningBoltEffect>();
            bolt._width = isStrike ? 0.22f : 0.07f;
            bolt._intensity = isStrike ? 14f : 4f;
            bolt._line = bolt.gameObject.AddComponent<LineRenderer>();
            bolt._line.sharedMaterial = s_material;
            bolt._line.numCapVertices = 2;
            bolt._line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            int count = Mathf.Max(2, Mathf.CeilToInt(Vector3.Distance(from, to) / SegmentLength) + 1);
            float jitter = isStrike ? 0.5f : 0.18f;
            bolt._line.positionCount = count;

            for (int i = 0; i < count; i++)
            {
                float t = i / (count - 1f);
                Vector3 offset = i == 0 || i == count - 1 ? Vector3.zero : Random.insideUnitSphere * jitter;
                bolt._line.SetPosition(i, Vector3.Lerp(from, to, t) + offset);
            }

            bolt._light = new GameObject("Flash").AddComponent<Light>();
            bolt._light.transform.SetParent(bolt.transform);
            bolt._light.transform.position = to + Vector3.up * 0.5f;
            bolt._light.color = new Color(0.6f, 0.75f, 1f);
            bolt._light.range = isStrike ? 12f : 6f;
            bolt._light.intensity = bolt._intensity;
            bolt._line.widthMultiplier = bolt._width;
            DungeonAudioComponent.Play(DungeonSound.Cast, to, isStrike ? 1f : 0.5f, isStrike ? 0.5f : 1.6f);
        }
    }
}
