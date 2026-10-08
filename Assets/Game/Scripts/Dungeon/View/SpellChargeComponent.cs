using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// The ball of the spell being gathered in the clawed fingers of the casting hand, in the spell's colour, growing with
    /// the charge; it stays in the hand for a moment after the release, as the hand throws it.
    public sealed class SpellChargeComponent : NetworkBehaviour
    {
        [SerializeField]
        private AdventurerComponent _adventurer;

        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private Transform _ball;

        [SerializeField]
        private ParticleSystem[] _particles;

        [SerializeField]
        private Light _light;

        [SerializeField]
        private float _linger = 0.3f;

        [SerializeField]
        private float _lift = 0.04f;

        [SerializeField]
        private float _lightIntensity = 0.8f;

        private static readonly HumanBodyBones[] s_claw =
        {
            HumanBodyBones.RightThumbDistal, HumanBodyBones.RightIndexDistal, HumanBodyBones.RightMiddleDistal,
            HumanBodyBones.RightRingDistal, HumanBodyBones.RightLittleDistal, HumanBodyBones.RightMiddleProximal
        };

        private Transform[] _claw;
        private AbilityConfig _spell;
        private float _charge;
        private float _left;
        private float _flicker;

        private void Awake()
        {
            _claw = new Transform[s_claw.Length];

            for (int i = 0; i < s_claw.Length; i++)
                _claw[i] = _animator.GetBoneTransform(s_claw[i]) ?? _animator.GetBoneTransform(HumanBodyBones.RightHand);

            _ball.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (Object == null || !Object.IsValid)
                return;

            UpdateSpell();
            bool isShown = _spell != null && _left > 0f;

            if (_ball.gameObject.activeSelf != isShown)
                _ball.gameObject.SetActive(isShown);

            if (!isShown)
                return;

            Vector3 center = Vector3.zero;

            foreach (Transform bone in _claw)
                center += bone.position;

            float scale = Mathf.Lerp(0.35f, 1f, _charge) * Mathf.Clamp01(_left / (_linger * 0.5f));
            _ball.SetPositionAndRotation(center / _claw.Length + Vector3.up * _lift, Quaternion.identity);
            _ball.localScale = Vector3.one * scale;

            // Lightning crackles, everything else breathes.
            bool isLightning = _spell.Kind is AbilityKind.ChainLightning or AbilityKind.LightningStrike;
            _flicker += Time.deltaTime * (isLightning ? 30f : 8f);
            float flicker = 1f + (Mathf.PerlinNoise(_flicker, 0.3f) - 0.5f) * (isLightning ? 1.2f : 0.3f);
            _light.color = _spell.Color;
            _light.intensity = _lightIntensity * scale * flicker;
        }

        private void UpdateSpell()
        {
            if (_adventurer.IsHoldingCast && _adventurer.ReadiedSpellConfig != null)
            {
                if (_spell != _adventurer.ReadiedSpellConfig)
                {
                    _spell = _adventurer.ReadiedSpellConfig;

                    foreach (ParticleSystem particles in _particles)
                    {
                        ParticleSystem.MainModule main = particles.main;
                        main.startColor = _spell.Color;
                    }
                }

                _charge = _adventurer.CastCharge;
                _left = _linger;

                return;
            }

            _left -= Time.deltaTime;
        }
    }
}
