using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Floating head: hovers and bobs, pulls back and shakes while it screeches, stretches through the ram and drops when killed.
    public sealed class FlyingHeadVisualComponent : MonoBehaviour
    {
        [SerializeField]
        private MonsterComponent _monster;

        [SerializeField]
        private Transform _head;

        [SerializeField]
        private Light _glow;

        [SerializeField]
        private float _hoverHeight = 1.55f;

        [SerializeField]
        private float _groundHeight = 0.2f;

        [SerializeField]
        private float _bobHeight = 0.07f;

        [SerializeField]
        private float _shake = 0.035f;

        [SerializeField]
        private float _pullBack = 0.3f;

        [SerializeField]
        private Vector3 _ramScale = new(0.85f, 0.85f, 1.3f);

        private const float FallGravity = 14f;

        private float _glowIntensity;
        private float _bobOffset;
        private float _fallSpeed;

        private void Awake()
        {
            _glowIntensity = _glow.intensity;
            _bobOffset = Random.value * 10f;
        }

        private void LateUpdate()
        {
            FighterComponent fighter = _monster.Fighter;

            if (fighter.Health.IsDead)
            {
                Fall();

                return;
            }

            AttackPhase phase = fighter.Combat.Phase;
            bool isScreeching = phase == AttackPhase.Windup;
            Vector3 target = new Vector3(0f, _hoverHeight + Mathf.Sin(Time.time * 2.2f + _bobOffset) * _bobHeight, isScreeching ? -_pullBack : 0f);
            Vector3 position = Vector3.Lerp(_head.localPosition, target, Time.deltaTime * 10f);

            _head.localPosition = isScreeching ? position + Random.insideUnitSphere * _shake : position;
            _head.localRotation = Quaternion.Euler(fighter.Move.Pitch, 0f, 0f);
            _head.localScale = Vector3.MoveTowards(_head.localScale, phase == AttackPhase.Active ? _ramScale : Vector3.one, Time.deltaTime * 4f);
            _glow.intensity = _glowIntensity * (phase == AttackPhase.None ? 1f : 3f);
        }

        private void Fall()
        {
            Vector3 position = _head.localPosition;

            if (position.y <= _groundHeight)
                return;

            _fallSpeed += FallGravity * Time.deltaTime;
            position.y = Mathf.Max(_groundHeight, position.y - _fallSpeed * Time.deltaTime);
            _head.localPosition = position;
            _head.localRotation = Quaternion.RotateTowards(_head.localRotation, Quaternion.Euler(-70f, 0f, 20f), Time.deltaTime * 300f);
            _head.localScale = Vector3.one;
            _glow.enabled = false;
        }
    }
}
