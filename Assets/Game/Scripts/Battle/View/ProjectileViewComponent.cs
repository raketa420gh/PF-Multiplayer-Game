using Fusion;
using Game.Scripts.Dungeon;
using UnityEngine;

namespace Game.Scripts.Battle
{
    public sealed class ProjectileViewComponent : NetworkBehaviour
    {
        private const byte NoLook = 255;

        [SerializeField]
        private ProjectileComponent _projectiles;

        [SerializeField]
        private GameObject _arrowPrefab;

        /// Fallback look of spells when there is no effect config.
        [SerializeField]
        private GameObject _magicPrefab;

        [SerializeField]
        private AbilityVfxConfig _vfx;

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
        /// Per-slot holder of the effect look, re-styled when the slot fires a different kind.
        private readonly Transform[] _spells = new Transform[ProjectileComponent.Capacity];
        private readonly GameObject[] _looks = new GameObject[ProjectileComponent.Capacity];
        private readonly Transform[] _spinners = new Transform[ProjectileComponent.Capacity];
        private readonly byte[] _lookKinds = new byte[ProjectileComponent.Capacity];
        private readonly int[] _shotTicks = new int[ProjectileComponent.Capacity];
        private readonly int[] _impactTicks = new int[ProjectileComponent.Capacity];
        private readonly bool[] _isFresh = new bool[ProjectileComponent.Capacity];

        public override void Spawned()
        {
            if (_vfx != null)
                _vfx.Register();

            bool hasVfx = AbilityVfxConfig.Instance != null;

            for (int i = 0; i < _arrows.Length; i++)
            {
                _arrows[i] = Instantiate(_arrowPrefab).transform;
                _arrows[i].gameObject.SetActive(false);
                _shotTicks[i] = _projectiles.Projectiles[i].FireTick;
                _impactTicks[i] = _shotTicks[i];
                _lookKinds[i] = NoLook;

                if (hasVfx)
                {
                    _spells[i] = new GameObject("SpellProjectile").transform;
                    _spells[i].gameObject.SetActive(false);
                }

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

                if (_spells[i] != null)
                    Destroy(_spells[i].gameObject);
            }
        }

        public override void Render()
        {
            float renderTick = Runner.Tick + Runner.LocalAlpha;

            for (int i = 0; i < _arrows.Length; i++)
            {
                ProjectileData data = _projectiles.Projectiles[i];
                bool isMagic = data.KindValue is not (ProjectileKind.Arrow or ProjectileKind.Thrown);
                Transform magic = _spells[i] != null ? _spells[i] : _orbs[i];
                Transform visual = isMagic && magic != null ? magic : _arrows[i];

                if (data.FireTick != _shotTicks[i] && data.FireTick > 0)
                {
                    _shotTicks[i] = data.FireTick;
                    _isFresh[i] = true;

                    if (BattleContext.Instance != null)
                        BattleContext.Instance.Feedback.PlayShot(data.Origin, BattleContext.Instance.Feedback.GetSounds(data.Sound), HasInputAuthority);

                    if (isMagic && visual == _spells[i])
                        Restyle(i, data.KindValue);
                    else if (isMagic)
                        Tint(visual, _kindColors[Mathf.Clamp(data.Kind, 0, _kindColors.Length - 1)]);
                }

                bool isVisible = UpdateProjectile(data, visual, renderTick, isMagic);

                if (isVisible && isMagic && _spinners[i] != null)
                    Spin(_spinners[i], data.KindValue, Mathf.Max(0f, (renderTick - data.FireTick) * Runner.DeltaTime));

                if (visual.gameObject.activeSelf != isVisible)
                    visual.gameObject.SetActive(isVisible);

                if (isVisible && _isFresh[i])
                {
                    _isFresh[i] = false;

                    if (visual == _spells[i] && _looks[i] != null)
                        AbilityVfx.Restart(_looks[i]);
                }

                Hide(_arrows[i], visual);
                Hide(_orbs[i], visual);
                Hide(_spells[i], visual);

                if (data.FinishTick > 0 && data.FireTick != _impactTicks[i])
                {
                    _impactTicks[i] = data.FireTick;

                    if (data.IsHidden)
                        continue;

                    if (isMagic)
                        AbilityVfx.Burst(GetStyle(data.KindValue), data.HitPoint, Mathf.Max(0.6f, data.Radius), default);
                    else
                        PlayStuck(data);
                }
            }
        }

        /// An arrow stuck in the level sounds the surface it went into, if its bow has recorded hits.
        private void PlayStuck(in ProjectileData data)
        {
            BattleFeedback feedback = BattleContext.Instance != null ? BattleContext.Instance.Feedback : null;
            WeaponSoundConfig sounds = feedback != null ? feedback.GetSounds(data.Sound) : null;

            if (sounds == null)
                return;

            Vector3 direction = data.GetVelocity((data.FinishTick - data.FireTick) * Runner.DeltaTime).normalized;
            ImpactSurface surface = Physics.Raycast(data.HitPoint - direction * 0.2f, direction, out RaycastHit hit, 0.4f, _projectiles.HitMask, QueryTriggerInteraction.Ignore)
                ? WorldSurface.Of(hit.collider.gameObject, data.HitPoint)
                : ImpactSurface.Stone;
            feedback.PlayWorldHit(data.HitPoint, -direction, sounds, surface);
        }

        private static VfxStyle GetStyle(ProjectileKind kind)
        {
            return kind switch
            {
                ProjectileKind.Fire => VfxStyle.Fire,
                ProjectileKind.Ice => VfxStyle.Frost,
                ProjectileKind.Holy => VfxStyle.Holy,
                ProjectileKind.Dark => VfxStyle.Dark,
                ProjectileKind.Flask => VfxStyle.Alchemy,
                ProjectileKind.Poison => VfxStyle.Poison,
                ProjectileKind.Net => VfxStyle.Physical,
                _ => VfxStyle.Arcane
            };
        }

        private static void Hide(Transform candidate, Transform visual)
        {
            if (candidate != null && candidate != visual && candidate.gameObject.activeSelf)
                candidate.gameObject.SetActive(false);
        }

        /// Flasks tumble end over end, nets spin flat.
        private static void Spin(Transform spinner, ProjectileKind kind, float time)
        {
            spinner.localRotation = kind == ProjectileKind.Net ? Quaternion.Euler(0f, time * 540f, 0f) : Quaternion.Euler(time * 720f, 0f, time * 90f);
        }

        private static void Tint(Transform visual, Color color)
        {
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>())
                renderer.material.SetColor("_BaseColor", color);

            Light light = visual.GetComponentInChildren<Light>();

            if (light != null)
                light.color = color;
        }

        /// Builds the slot's look for the kind unless it already wears it.
        private void Restyle(int index, ProjectileKind kind)
        {
            if (_lookKinds[index] == (byte)kind)
                return;

            if (_looks[index] != null)
                Destroy(_looks[index]);

            _lookKinds[index] = (byte)kind;
            _spinners[index] = null;
            Transform root = _spells[index];

            switch (kind)
            {
                case ProjectileKind.Flask:
                case ProjectileKind.Poison:
                    Color liquid = kind == ProjectileKind.Poison ? new Color(0.35f, 0.7f, 0.12f) : new Color(1f, 0.55f, 0.15f);
                    _looks[index] = AbilityVfx.Flask(root, liquid);
                    _spinners[index] = _looks[index].transform;
                    break;
                case ProjectileKind.Net:
                    _looks[index] = AbilityVfx.Net(root);
                    _spinners[index] = _looks[index].transform;
                    break;
                default:
                    _looks[index] = AbilityVfx.Projectile(GetStyle(kind), root, default);
                    break;
            }
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
