using Fusion;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// A lasting area an ability leaves on the ground: pools, clouds, walls of fire, a banner, a pyre that strikes after a delay, a storm
    /// of random bolts. The host pulses it; damage, heals and effects go through the owner so the owner's perks count, and still work
    /// when the owner is gone. Every client draws it from its kind.
    public sealed class AbilityZoneComponent : NetworkBehaviour
    {
        public const float Pulse = 0.5f;
        /// Wall of fire: depth of the strip across its length.
        private const float WallDepth = 1f;
        private const float WallHeight = 2.2f;
        private const float StrikeRadius = 1.4f;
        private const float StrikeHeight = 14f;
        private const float FlareLife = 15f;

        [Networked]
        public ZoneKind Kind { get; private set; }

        [Networked]
        public float Radius { get; private set; }

        public Color Color => new(_tint.x, _tint.y, _tint.z);

        [Networked]
        public int StrikeTick { get; private set; }

        [SerializeField, Tooltip("Looping sound per ZoneKind")]
        private AudioClip[] _loops;

        [SerializeField]
        private AudioClip _strikeSound;

        [SerializeField]
        private AudioClip _eruptSound;

        [Networked]
        private Vector3 _tint { get; set; }

        [Networked]
        private NetworkBehaviourId _owner { get; set; }

        [Networked]
        private int _team { get; set; }

        [Networked]
        private TickTimer _life { get; set; }

        [Networked]
        private TickTimer _pulse { get; set; }

        [Networked]
        private float _tick { get; set; }

        [Networked]
        private float _magnitude { get; set; }

        [Networked]
        private StatusEffectKind _effect { get; set; }

        [Networked]
        private float _effectMagnitude { get; set; }

        [Networked]
        private float _effectDuration { get; set; }

        [Networked]
        private float _stagger { get; set; }

        [Networked]
        private AbilityFlags _flags { get; set; }

        [Networked]
        private DamageType _damageType { get; set; }

        [Networked]
        private int _strikesLeft { get; set; }

        [Networked]
        private float _strikeInterval { get; set; }

        private GameObject _visual;
        private AudioSource _loop;
        private bool _hasErupted;

        public override void Spawned()
        {
            Vector3 size = new Vector3(Radius, WallHeight, WallDepth);
            _visual = AbilityVfx.Zone(Kind, transform, Radius, size, Color);
            AudioClip loop = _loops != null && (int)Kind < _loops.Length ? _loops[(int)Kind] : null;

            if (loop == null)
                return;

            _loop = gameObject.AddComponent<AudioSource>();
            _loop.clip = loop;
            _loop.loop = true;
            _loop.spatialBlend = 1f;
            _loop.rolloffMode = AudioRolloffMode.Linear;
            _loop.minDistance = 2f;
            _loop.maxDistance = 22f;
            _loop.volume = 0.6f;
            _loop.Play();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (_visual != null)
                Destroy(_visual);
        }

        public override void Render()
        {
            if (Kind != ZoneKind.Pyre || _hasErupted || StrikeTick <= 0 || Runner.Tick < StrikeTick)
                return;

            _hasErupted = true;
            AbilityVfx.Erupt(_visual);
            DungeonAudioComponent.Play(_eruptSound, transform.position, 1f);
        }

        /// Called before spawning. The ability gives the shape, the numbers and the effect; perks of the owner have already scaled them.
        public void Setup(AdventurerComponent owner, AbilityConfig ability, float radius, float duration, float tick, float magnitude)
        {
            Kind = ability.Zone;
            Radius = radius;
            _tint = new Vector3(ability.Color.r, ability.Color.g, ability.Color.b);
            _owner = owner.Id;
            _team = owner.Team;
            _tick = tick;
            _magnitude = magnitude;
            _effect = ability.Effect;
            _effectMagnitude = ability.Zone == ZoneKind.Banner ? magnitude : ability.EffectMagnitude;
            _effectDuration = ability.EffectDuration;
            _stagger = ability.StaggerDuration;
            _flags = ability.Flags;
            _damageType = ability.DamageType;
            _strikesLeft = ability.ProjectileCount;
            _strikeInterval = ability.ProjectileCount > 0 ? Mathf.Max(0.2f, (duration - 0.2f) / ability.ProjectileCount) : 0f;
            _life = TickTimer.CreateFromSeconds(owner.Runner, duration);
            _pulse = TickTimer.CreateFromSeconds(owner.Runner, ability.Zone == ZoneKind.Storm ? 0.2f : ability.ZoneDelay > 0f ? ability.ZoneDelay : 0.05f);
            StrikeTick = ability.ZoneDelay > 0f ? owner.Runner.Tick + Mathf.CeilToInt(ability.ZoneDelay / owner.Runner.DeltaTime) : 0;
        }

        /// A flare of the tracker's arrow: only light, for a while.
        public void SetupFlare(AdventurerComponent owner, Color color)
        {
            Kind = ZoneKind.Flare;
            Radius = 1f;
            _tint = new Vector3(color.r, color.g, color.b);
            _owner = owner.Id;
            _team = owner.Team;
            _life = TickTimer.CreateFromSeconds(owner.Runner, FlareLife);
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            if (_life.Expired(Runner))
            {
                Runner.Despawn(Object);

                return;
            }

            if (Kind == ZoneKind.Flare || !_pulse.Expired(Runner))
                return;

            AdventurerComponent owner = Owner;

            switch (Kind)
            {
                case ZoneKind.Pyre:
                    _pulse = TickTimer.CreateFromSeconds(Runner, 999f);
                    Burst(owner, transform.position, Radius, _magnitude);
                    break;
                case ZoneKind.Storm:
                    Strike(owner);
                    break;
                default:
                    _pulse = TickTimer.CreateFromSeconds(Runner, Pulse);
                    PulseBodies(owner);
                    break;
            }
        }

        private AdventurerComponent Owner => Runner.TryFindBehaviour(_owner, out NetworkBehaviour behaviour) ? behaviour as AdventurerComponent : null;

        private bool IsAlly(DamageReceiverComponent receiver)
        {
            return receiver.TryGetComponent(out AdventurerComponent adventurer) && adventurer.State == AdventurerState.Alive && adventurer.Team == _team;
        }

        private bool IsInside(DamageReceiverComponent receiver)
        {
            Vector3 local = transform.InverseTransformPoint(receiver.transform.position);

            if (Kind == ZoneKind.FireWall)
                return Mathf.Abs(local.x) <= Radius * 0.5f && Mathf.Abs(local.z) <= WallDepth && local.y > -1f && local.y < WallHeight;

            return new Vector2(local.x, local.z).sqrMagnitude <= Radius * Radius && Mathf.Abs(local.y) < 2.5f;
        }

        /// Pools, clouds, walls, circles and the banner: everyone inside gets their share every pulse.
        private void PulseBodies(AdventurerComponent owner)
        {
            foreach (DamageReceiverComponent receiver in DamageReceiverComponent.All)
            {
                if (!receiver.IsAlive || !IsInside(receiver))
                    continue;

                bool isAlly = IsAlly(receiver);

                switch (Kind)
                {
                    case ZoneKind.HealPool or ZoneKind.Sanctuary:
                        if (isAlly && receiver.TryGetComponent(out AdventurerComponent ally))
                            Heal(owner, ally, _tick * Pulse);
                        break;
                    case ZoneKind.Banner:
                        if (isAlly && receiver.TryGetComponent(out StatusEffectComponent banner))
                            banner.Add(_effect, _effectMagnitude, Pulse * 2f);
                        break;
                    case ZoneKind.PoisonCloud or ZoneKind.Frost:
                        AddEffect(owner, receiver);
                        break;
                    default:
                        Hurt(owner, receiver, _tick * Pulse, 0f);
                        AddEffect(owner, receiver);
                        break;
                }
            }
        }

        private void Strike(AdventurerComponent owner)
        {
            if (_strikesLeft <= 0)
            {
                _pulse = TickTimer.CreateFromSeconds(Runner, 999f);

                return;
            }

            _strikesLeft--;
            _pulse = TickTimer.CreateFromSeconds(Runner, _strikeInterval);
            Vector2 offset = Random.insideUnitCircle * Mathf.Max(0f, Radius - StrikeRadius * 0.5f);
            Vector3 ground = transform.position + new Vector3(offset.x, 0f, offset.y);

            if (Physics.Raycast(ground + Vector3.up * 3f, Vector3.down, out RaycastHit hit, 6f, 1, QueryTriggerInteraction.Ignore))
                ground = hit.point;

            RpcStrike(ground);
            Burst(owner, ground, StrikeRadius, _magnitude);
        }

        /// One blow to everyone within the radius of the point.
        private void Burst(AdventurerComponent owner, Vector3 center, float radius, float damage)
        {
            foreach (DamageReceiverComponent receiver in DamageReceiverComponent.All)
            {
                Vector3 offset = receiver.transform.position - center;

                if (!receiver.IsAlive || new Vector2(offset.x, offset.z).sqrMagnitude > radius * radius || Mathf.Abs(offset.y) > 3f)
                    continue;

                Hurt(owner, receiver, damage, _stagger);
                AddEffect(owner, receiver);
            }
        }

        private void Hurt(AdventurerComponent owner, DamageReceiverComponent receiver, float damage, float stagger)
        {
            if (damage <= 0f)
                return;

            if (owner != null)
            {
                owner.ZoneHit(receiver, damage, _damageType, _flags, stagger, transform.position);

                return;
            }

            Vector3 point = receiver.transform.position + Vector3.up;
            receiver.ApplyHit(new HitRequest
            {
                BaseDamage = Mathf.RoundToInt(damage), BodyRays = 1, Zone = HitZone.Torso, Point = point, Normal = Vector3.up, AttackerPosition = transform.position,
                StaggerDuration = stagger, DamageType = _damageType, Tag = DamageTag.Spell
            });
        }

        private void AddEffect(AdventurerComponent owner, DamageReceiverComponent receiver)
        {
            if (_effect == StatusEffectKind.None || !receiver.IsAlive || !receiver.TryGetComponent(out StatusEffectComponent effects))
                return;

            if (owner != null)
                owner.ApplyEffectTo(effects, _effect, _effectMagnitude, _effectDuration, _flags);
            else
                effects.Add(_effect, _effectMagnitude, _effectDuration);
        }

        private void Heal(AdventurerComponent owner, AdventurerComponent ally, float percent)
        {
            if (owner != null)
                owner.HealTarget(ally, percent, true, false);
            else
                ally.Fighter.Health.Restore(Mathf.Max(1, Mathf.RoundToInt(ally.Fighter.Health.MaxHealth * percent / 100f)));
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RpcStrike(Vector3 ground)
        {
            LightningBoltEffect.Play(ground + Vector3.up * StrikeHeight, ground, true);
            AbilityVfx.Burst(VfxStyle.Lightning, ground, StrikeRadius, Color);
            DungeonAudioComponent.Play(_strikeSound, ground, 1f, Random.Range(0.9f, 1.1f));
        }
    }
}
