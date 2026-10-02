using Fusion;
using UnityEngine;

namespace Game.Scripts.Battle
{
    [DefaultExecutionOrder(-10)]
    public sealed class FighterCameraComponent : NetworkBehaviour
    {
        [SerializeField]
        private FighterComponent _fighter;

        [SerializeField]
        private float _fieldOfView = 75f;

        [SerializeField]
        private float _nearClip = 0.04f;

        [SerializeField]
        private float _hitPunch = 4f;

        [SerializeField]
        private float _punchDecay = 10f;

        [SerializeField]
        private Vector3 _deathOffset = new(0f, 0.35f, 0f);

        [SerializeField]
        private float _deathRoll = 60f;

        private BattleContext _context;
        private float _punch;
        private float _deathBlend;

        public override void Spawned()
        {
            _context = BattleContext.Instance;
            enabled = HasInputAuthority && _context != null;

            if (!enabled)
                return;

            _context.Camera.fieldOfView = _fieldOfView;
            _context.Camera.nearClipPlane = _nearClip;
            _fighter.Receiver.OnHitEvent += OnHitEvent;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _fighter.Receiver.OnHitEvent -= OnHitEvent;
        }

        private void LateUpdate()
        {
            float deltaTime = Time.deltaTime;
            Vector2 look = _context.Input.LookRotation;
            FighterMoveComponent move = _fighter.Move;
            float crouch = new NetworkBehaviourBufferInterpolator(move).Float(nameof(FighterMoveComponent.CrouchAmount));

            transform.rotation = Quaternion.Euler(0f, look.y, 0f);
            _punch = Mathf.Lerp(_punch, 0f, _punchDecay * deltaTime);
            _deathBlend = Mathf.MoveTowards(_deathBlend, _fighter.Health.IsAlive ? 0f : 1f, deltaTime * 2f);

            Vector3 eye = _fighter.Body.GetEyePosition(transform.position, look.y, look.x, crouch);
            eye = Vector3.Lerp(eye, transform.position + _deathOffset, _deathBlend);

            _context.Camera.transform.SetPositionAndRotation(eye,
                Quaternion.Euler(look.x - _punch, look.y, _deathRoll * _deathBlend));
        }

        private void OnHitEvent(HitEventData hit)
        {
            _punch = hit.Result == HitResult.Blocked ? _hitPunch * 0.5f : _hitPunch;
        }
    }
}
