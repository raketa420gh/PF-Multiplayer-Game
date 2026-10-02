using Fusion;
using UnityEngine;

namespace Game.Scripts.Battle
{
    public sealed class ProjectileViewComponent : NetworkBehaviour
    {
        [SerializeField]
        private ProjectileComponent _projectiles;

        [SerializeField]
        private GameObject _arrowPrefab;

        [SerializeField]
        private float _stuckLifetime = 8f;

        [SerializeField]
        private float _spawnOffset = 0.6f;

        [SerializeField]
        private float _stuckOffset = 0.6f;

        private readonly Transform[] _arrows = new Transform[ProjectileComponent.Capacity];
        private readonly int[] _shotTicks = new int[ProjectileComponent.Capacity];

        public override void Spawned()
        {
            for (int i = 0; i < _arrows.Length; i++)
            {
                _arrows[i] = Instantiate(_arrowPrefab).transform;
                _arrows[i].gameObject.SetActive(false);
                _shotTicks[i] = _projectiles.Projectiles[i].FireTick;
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            foreach (Transform arrow in _arrows)
            {
                if (arrow != null)
                    Destroy(arrow.gameObject);
            }
        }

        public override void Render()
        {
            float renderTick = Runner.Tick + Runner.LocalAlpha;

            for (int i = 0; i < _arrows.Length; i++)
            {
                ProjectileData data = _projectiles.Projectiles[i];
                Transform arrow = _arrows[i];

                if (data.FireTick != _shotTicks[i] && data.FireTick > 0)
                {
                    _shotTicks[i] = data.FireTick;

                    if (BattleContext.Instance != null)
                        BattleContext.Instance.Feedback.PlayShot(data.Origin);
                }

                bool isVisible = UpdateArrow(data, arrow, renderTick);

                if (arrow.gameObject.activeSelf != isVisible)
                    arrow.gameObject.SetActive(isVisible);
            }
        }

        private bool UpdateArrow(in ProjectileData data, Transform arrow, float renderTick)
        {
            if (data.FireTick == 0 || data.IsHidden)
                return false;

            float deltaTime = Runner.DeltaTime;

            if (data.FinishTick > 0)
            {
                float flightTime = (data.FinishTick - data.FireTick) * deltaTime;
                Vector3 direction = data.GetVelocity(flightTime).normalized;
                arrow.SetPositionAndRotation(data.HitPoint - direction * _stuckOffset, Quaternion.LookRotation(direction));

                return (renderTick - data.FinishTick) * deltaTime < _stuckLifetime;
            }

            float time = Mathf.Max(0f, (renderTick - data.FireTick) * deltaTime);
            Vector3 velocity = data.GetVelocity(time);
            arrow.SetPositionAndRotation(data.GetPosition(time), Quaternion.LookRotation(velocity));

            return time * velocity.magnitude > _spawnOffset;
        }
    }
}
