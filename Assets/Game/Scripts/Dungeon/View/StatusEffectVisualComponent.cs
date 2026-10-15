using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Looping looks of the active status effects on a fighter (flames of a burn, a sigil over the marked, the lightbearer's orb...).
    /// Reads the networked effects, so it works the same on every client.
    public sealed class StatusEffectVisualComponent : MonoBehaviour
    {
        private const float DefaultHeight = 1.8f;
        private const float LightFade = 3f;
        /// A look that stopped emitting is switched off once its last particles are surely gone.
        private const float SleepDelay = 2.5f;

        [SerializeField]
        private StatusEffectComponent _effects;

        [SerializeField]
        private AbilityVfxConfig _config;

        [SerializeField]
        private Transform _head;

        private sealed class Look
        {
            public GameObject Root;
            public ParticleSystem[] Systems;
            public Light Light;
            public float Intensity;
            public bool IsOn;
            public float OffTime;
        }

        private readonly Dictionary<StatusEffectKind, Look> _looks = new();
        private readonly HashSet<StatusEffectKind> _active = new();
        private int _signature;
        private bool _isLocal;
        private bool _isReady;

        private void Awake()
        {
            if (_config != null)
                _config.Register();
        }

        private void LateUpdate()
        {
            if (_effects == null || _effects.Object == null || !_effects.Object.IsValid)
                return;

            if (!_isReady)
            {
                _isReady = true;
                _isLocal = _effects.Object.HasInputAuthority;
                _signature = 0;
                Refresh();
            }

            int signature = _effects.GetSignature();

            if (signature != _signature)
            {
                _signature = signature;
                Refresh();
            }

            Animate(Time.deltaTime);
        }

        private void OnDisable()
        {
            _isReady = false;
        }

        private void Refresh()
        {
            _active.Clear();
            NetworkArray<StatusEffect> effects = _effects.Effects;

            for (int i = 0; i < effects.Length; i++)
            {
                StatusEffect effect = effects[i];

                if (effect.IsActive && IsShown(effect.Kind))
                    _active.Add(effect.Kind);
            }

            foreach (StatusEffectKind kind in _active)
            {
                if (!_looks.TryGetValue(kind, out Look look))
                    _looks[kind] = look = Create(kind);

                if (look != null && !look.IsOn)
                    TurnOn(look);
            }

            foreach (KeyValuePair<StatusEffectKind, Look> pair in _looks)
            {
                if (pair.Value != null && pair.Value.IsOn && !_active.Contains(pair.Key))
                    TurnOff(pair.Value);
            }
        }

        /// The local first-person body only shows what cannot get in front of the camera.
        private bool IsShown(StatusEffectKind kind)
        {
            return !_isLocal || kind is StatusEffectKind.Lightbearer or StatusEffectKind.Root;
        }

        private Look Create(StatusEffectKind kind)
        {
            float height = _head != null ? Mathf.Max(0.8f, _head.position.y - transform.position.y + 0.1f) : DefaultHeight;
            GameObject root = AbilityVfx.Status(kind, transform, height);

            if (root == null)
                return null;

            Light light = root.GetComponentInChildren<Light>();
            Look look = new Look { Root = root, Systems = root.GetComponentsInChildren<ParticleSystem>(), Light = light };

            if (light != null)
            {
                look.Intensity = light.intensity;
                light.intensity = 0f;
            }

            return look;
        }

        private static void TurnOn(Look look)
        {
            look.IsOn = true;
            look.Root.SetActive(true);

            foreach (ParticleSystem system in look.Systems)
                system.Play(false);
        }

        private static void TurnOff(Look look)
        {
            look.IsOn = false;
            look.OffTime = Time.time;

            foreach (ParticleSystem system in look.Systems)
                system.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }

        private void Animate(float deltaTime)
        {
            foreach (Look look in _looks.Values)
            {
                if (look == null || !look.Root.activeSelf)
                    continue;

                if (look.Light != null)
                {
                    float target = look.IsOn ? look.Intensity : 0f;
                    look.Light.intensity = Mathf.MoveTowards(look.Light.intensity, target, look.Intensity * LightFade * deltaTime);
                }

                if (!look.IsOn && Time.time - look.OffTime > SleepDelay)
                    look.Root.SetActive(false);
            }
        }
    }
}
