using System.Collections.Generic;
using Fusion;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// A clay pot on its side that spiders crawl out of, DaD's Spider Pot: while an adventurer is near it keeps up to MaxAlive spiders out, one at a time,
    /// and a dead spider comes back after the respawn delay. Its health is DaD's Impact Endurance: a blow takes the weapon's prop power minus the pot's
    /// resistance, a haft takes nothing, a spell or a trap shatters it at once. A shattered pot sends out no more spiders; the living ones stay.
    public sealed class SpiderNestComponent : NetworkBehaviour, DamageReceiverComponent.IHitModifier
    {
        private const int MonsterTeam = 2;

        [SerializeField]
        private HealthComponent _health;

        [SerializeField]
        private DamageReceiverComponent _receiver;

        [SerializeField]
        private NetworkObject _spider;

        [SerializeField, Tooltip("Where a spider comes out, facing out of the mouth")]
        private Transform _mouth;

        [SerializeField]
        private GameObject _whole;

        [SerializeField, Tooltip("Shown instead of the pot once it is shattered")]
        private GameObject _shards;

        [SerializeField, Tooltip("Impact Endurance (DaD): the pot's health in prop power")]
        private int _endurance = 4;

        [SerializeField, Tooltip("Impact Resistance (DaD): subtracted from each blow's prop power")]
        private int _resistance;

        [SerializeField]
        private int _maxAlive = 3;

        [SerializeField, Tooltip("Spiders come out only while an adventurer is this close")]
        private float _wakeRange = 12f;

        [SerializeField, Tooltip("Seconds between two spiders coming out")]
        private float _spawnInterval = 1.5f;

        [SerializeField, Tooltip("Seconds before a dead spider is replaced")]
        private float _respawnDelay = 6f;

        [SerializeField]
        private AudioSource _audio;

        [SerializeField]
        private AudioClip[] _hitSounds = System.Array.Empty<AudioClip>();

        [SerializeField]
        private AudioClip[] _breakSounds = System.Array.Empty<AudioClip>();

        private readonly List<MonsterComponent> _spawned = new();
        private int _alive;
        private int _released;
        private bool _isShown = true;

        [Networked]
        private byte _level { get; set; }

        [Networked]
        private TickTimer _nextSpawn { get; set; }

        public override void Spawned()
        {
            _receiver.SetModifier(this);
            _receiver.SetTeam(MonsterTeam);
            _receiver.OnHitEvent += OnHitEvent;

            if (HasStateAuthority)
                _health.SetMaxHealth(_endurance, true);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _receiver.OnHitEvent -= OnHitEvent;

            if (!HasStateAuthority)
                return;

            foreach (MonsterComponent spider in _spawned)
            {
                if (spider != null && spider.Object != null && spider.Object.IsValid)
                    runner.Despawn(spider.Object);
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || _health.IsDead)
                return;

            _spawned.RemoveAll(spider => spider == null || spider.Object == null || !spider.Object.IsValid);
            int alive = 0;

            foreach (MonsterComponent spider in _spawned)
                alive += spider.Fighter.Health.IsAlive ? 1 : 0;

            // The dead come back one by one, a respawn delay apart: three killed at once return after 6, 12 and 18 s.
            if (alive < _alive && _nextSpawn.ExpiredOrNotRunning(Runner))
                _nextSpawn = TickTimer.CreateFromSeconds(Runner, _respawnDelay);

            _alive = alive;

            if (alive >= _maxAlive || !_nextSpawn.ExpiredOrNotRunning(Runner) || !IsAdventurerNear())
                return;

            NetworkObject spawned = Runner.Spawn(_spider, _mouth.position, _mouth.rotation, PlayerRef.None, (_, obj) => obj.GetComponent<MonsterComponent>().Setup(_level));
            _spawned.Add(spawned.GetComponent<MonsterComponent>());
            _alive++;
            _nextSpawn = TickTimer.CreateFromSeconds(Runner, ++_released < _maxAlive ? _spawnInterval : _respawnDelay);
        }

        public override void Render()
        {
            if (_isShown == _health.IsDead)
                Show(!_health.IsDead, _health.IsDead);
        }

        /// Spawn callback: the floor the pot's spiders belong to.
        public void Setup(byte level)
        {
            _level = level;
        }

        void DamageReceiverComponent.IHitModifier.ModifyOutgoingHit(DamageReceiverComponent victim, ref HitRequest request)
        {
        }

        int DamageReceiverComponent.IHitModifier.ModifyIncomingDamage(int damage, in HitRequest request)
        {
            if (request.IsHandle)
                return 0;

            if ((request.Tag & (DamageTag.Spell | DamageTag.Trap)) != 0)
                return _health.MaxHealth;

            WeaponConfig weapon = request.Attacker != null && request.Attacker.TryGetComponent(out FighterComponent fighter) ? fighter.Combat.Weapon : null;
            int power = (request.Tag & DamageTag.Thrown) != 0 || weapon == null ? 2 : weapon.PropPower;

            return Mathf.Max(0, power - _resistance);
        }

        private bool IsAdventurerNear()
        {
            foreach (FighterComponent fighter in FighterComponent.All)
            {
                if (fighter.Receiver.Team != MonsterTeam && fighter.Health.IsAlive && (fighter.transform.position - transform.position).sqrMagnitude <= _wakeRange * _wakeRange)
                    return true;
            }

            return false;
        }

        private void Show(bool isWhole, bool isBreaking)
        {
            _isShown = isWhole;
            _whole.SetActive(isWhole);
            _shards.SetActive(!isWhole);

            if (isBreaking)
                Play(_breakSounds);
        }

        private void Play(AudioClip[] clips)
        {
            if (clips.Length > 0)
                _audio.PlayOneShot(clips[Random.Range(0, clips.Length)]);
        }

        private void OnHitEvent(HitEventData hit)
        {
            if (_health.IsAlive && hit.Result == HitResult.Hit)
                Play(_hitSounds);
        }
    }
}
