using Fusion;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Plays footsteps by travelled distance; walking is quieter, as in Dark and Darker.
    public sealed class FootstepComponent : NetworkBehaviour
    {
        [SerializeField]
        private FighterComponent _fighter;

        [SerializeField]
        private float _stride = 0.8f;

        private Vector3 _lastPosition;
        private float _travelled;
        private bool _toggle;

        public override void Spawned()
        {
            _lastPosition = transform.position;
        }

        public override void Render()
        {
            Vector3 position = transform.position;
            Vector3 delta = position - _lastPosition;
            delta.y = 0f;
            _lastPosition = position;

            if (!_fighter.Move.IsGrounded || !_fighter.Health.IsAlive)
                return;

            _travelled += delta.magnitude;

            if (_travelled < _stride)
                return;

            _travelled = 0f;
            _toggle = !_toggle;
            float speed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.001f);
            float volume = Mathf.Clamp01(speed / 4.2f) * 0.6f + 0.15f;
            DungeonAudioComponent.Play(_toggle ? DungeonSound.Footstep : DungeonSound.FootstepB, position, volume, Random.Range(0.92f, 1.08f));
        }
    }
}
