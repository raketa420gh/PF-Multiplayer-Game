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
        private GameObject _magicPrefab;

        [SerializeField]
        private float _stuckLifetime = 8f;

        [SerializeField]
        private float _spawnOffset = 0.6f;

        [SerializeField]
        private float _stuckOffset = 0.6f;

        [SerializeField]
        private Color[] _kindColors =
        {
            Color.white, new(0.5f, 0.6f, 1f), new(1f, 0.45f, 0.1f), new(0.55f, 0.85f, 1f), new(0.45f, 0.1f, 0.6f), new(1f, 0.95f, 0.6f), Color.white
        };

        private readonly Transform[] _arrows = new Transform[ProjectileComponent.Capacity];
        private readonly Transform[] _orbs = new Transform[ProjectileComponent.Capacity];
        private readonly int[] _shotTicks = new int[ProjectileComponent.Capacity];

        public override void Spawned()
        {
            for (int i = 0; i < _arrows.Length; i++)
            {
                _arrows[i] = Instantiate(_arrowPrefab).transform;
                _arrows[i].gameObject.SetActive(false);
                _shotTicks[i] = _projectiles.Projectiles[i].FireTick;

                if (_magicPrefab == null)
                    continue;

                _orbs[i] = Instantiate(_magicPrefab).transform;
                _orbs[i].gameObject.SetActive(false);
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            for (int i = 0; i < _arrows.Length; i++)
            {
                if (_arrows[i] != null)
                    Destroy(_arrows[i].gameObject);

                if (_orbs[i] != null)
                    Destroy(_orbs[i].gameObject);
            }
        }

        public override void Render()
        {
            float renderTick = Runner.Tick + Runner.LocalAlpha;

            for (int i = 0; i < _arrows.Length; i++)
            {
                ProjectileData data = _projectiles.Projectiles[i];
                bool isMagic = data.KindValue is not (ProjectileKind.Arrow or ProjectileKind.Thrown);
                Transform visual = isMagic && _orbs[i] != null ? _orbs[i] : _arrows[i];
                Transform other = visual == _arrows[i] ? _orbs[i] : _arrows[i];

                if (data.FireTick != _shotTicks[i] && data.FireTick > 0)
                {
                    _shotTicks[i] = data.FireTick;

                    if (BattleContext.Instance != null)
                        BattleContext.Instance.Feedback.PlayShot(data.Origin);

                    if (isMagic)
                        Tint(visual, _kindColors[Mathf.Clamp(data.Kind, 0, _kindColors.Length - 1)]);
                }

                bool isVisible = UpdateProjectile(data, visual, renderTick, isMagic);

                if (visual.gameObject.activeSelf != isVisible)
                    visual.gameObject.SetActive(isVisible);

                if (other != null && other.gameObject.activeSelf)
                    other.gameObject.SetActive(false);
            }
        }

        private static void Tint(Transform visual, Color color)
        {
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>())
                renderer.material.SetColor("_BaseColor", color);

            Light light = visual.GetComponentInChildren<Light>();

            if (light != null)
                light.color = color;
        }

        private bool UpdateProjectile(in ProjectileData data, Transform visual, float renderTick, bool isMagic)
        {
            if (data.FireTick == 0 || data.IsHidden)
                return false;

            float deltaTime = Runner.DeltaTime;

            if (data.FinishTick > 0)
            {
                if (isMagic)
                    return false;

                float flightTime = (data.FinishTick - data.FireTick) * deltaTime;
                Vector3 direction = data.GetVelocity(flightTime).normalized;
                visual.SetPositionAndRotation(data.HitPoint - direction * _stuckOffset, Quaternion.LookRotation(direction));

                return (renderTick - data.FinishTick) * deltaTime < _stuckLifetime;
            }

            float time = Mathf.Max(0f, (renderTick - data.FireTick) * deltaTime);
            Vector3 velocity = data.GetVelocity(time);
            visual.SetPositionAndRotation(data.GetPosition(time), Quaternion.LookRotation(velocity));

            return time * velocity.magnitude > _spawnOffset;
        }
    }
}
