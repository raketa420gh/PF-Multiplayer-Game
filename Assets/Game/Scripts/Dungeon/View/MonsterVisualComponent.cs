using Fusion;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Dresses a monster with the attachments listed in its config (rusty helmets, rags, a crown for the boss) and gives it a voice.
    /// Every sound is read off networked state on each client: combat state and phase, health, hit events and the alert flag.
    public sealed class MonsterVisualComponent : NetworkBehaviour
    {
        private const float VoiceHeight = 1.4f;
        private const float PainCooldown = 0.6f;
        private const float AlertCooldown = 12f;
        private const float BoneDropDelay = 0.35f;
        private const float FoleySpeed = 0.5f;

        [SerializeField]
        private MonsterComponent _monster;

        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private ArmorPieceSetConfig _pieceSet;

        private CombatState _state;
        private AttackPhase _phase;
        private bool _wasAlive = true;
        private bool _wasAlerted;
        private float _nextIdleVoice;
        private float _nextAlert;
        private float _nextPain;
        private float _nextFoley;
        private float _boneDropTime = float.PositiveInfinity;

        private Vector3 VoicePosition => transform.position + Vector3.up * VoiceHeight;

        public override void Spawned()
        {
            if (_animator != null)
            {
                ArmorDresser dresser = new ArmorDresser(_animator, _pieceSet, gameObject.layer);

                foreach (MonsterAttachment attachment in _monster.Config.Attachments)
                    dresser.Show(attachment.Visual, attachment.Color);
            }

            // A late joiner meets corpses and alerted monsters without hearing them die or notice anyone.
            _wasAlive = _monster.Fighter.Health.IsAlive;
            _wasAlerted = _monster.IsAlerted;
            _nextIdleVoice = Time.time + Random.Range(4f, 12f);
            _monster.Fighter.Receiver.OnHitEvent += OnHitEvent;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _monster.Fighter.Receiver.OnHitEvent -= OnHitEvent;
        }

        public override void Render()
        {
            MonsterSoundConfig sounds = _monster.Config.Sounds;

            if (sounds != null)
                RenderSounds(sounds);
            else
                RenderVoice();
        }

        private void RenderSounds(MonsterSoundConfig sounds)
        {
            CombatComponent combat = _monster.Fighter.Combat;
            bool isAlive = _monster.Fighter.Health.IsAlive;
            bool isAlerted = _monster.IsAlerted;

            if (combat.State != _state && combat.State == CombatState.Attack)
                Play(sounds.AttackVoice, 1f);

            if (combat.State != _state && combat.State == CombatState.Reload && combat.Weapon.IsRanged && combat.Weapon.Ranged.IsManualReload)
                Play(sounds.ReloadVoice, 0.6f);

            // A crossbow's release is its own shot sound, not a swing of the body.
            if (combat.State == CombatState.Attack && combat.Phase == AttackPhase.Active && _phase != AttackPhase.Active && !combat.Weapon.IsRanged)
                Play(sounds.AttackWhoosh, 0.8f);

            _state = combat.State;
            _phase = combat.Phase;

            if (_wasAlive && !isAlive)
            {
                Play(sounds.Death, 1f);
                _boneDropTime = Time.time + BoneDropDelay;
            }

            _wasAlive = isAlive;

            if (Time.time >= _boneDropTime)
            {
                _boneDropTime = float.PositiveInfinity;
                DungeonAudioComponent.Play(MonsterSoundConfig.Pick(sounds.BoneDrop), transform.position, 1f, Random.Range(0.95f, 1.05f));
            }

            if (!isAlive)
                return;

            if (isAlerted && !_wasAlerted && Time.time >= _nextAlert)
            {
                _nextAlert = Time.time + AlertCooldown;
                Play(sounds.Alert, 1f);
            }

            _wasAlerted = isAlerted;

            if (!isAlerted && Time.time >= _nextIdleVoice)
            {
                _nextIdleVoice = Time.time + Random.Range(9f, 20f);
                Play(sounds.Idle, 0.45f);
            }

            Vector3 velocity = _monster.Fighter.Move.Velocity;
            velocity.y = 0f;

            if (velocity.magnitude > FoleySpeed && Time.time >= _nextFoley)
            {
                _nextFoley = Time.time + Random.Range(0.7f, 1.5f);
                DungeonAudioComponent.Play(MonsterSoundConfig.Pick(sounds.Foley), transform.position + Vector3.up, 0.35f, Random.Range(0.9f, 1.1f), transform);
            }
        }

        /// The synthesized fallback of monsters without recorded sounds.
        private void RenderVoice()
        {
            CombatState state = _monster.Fighter.Combat.State;
            Vector3 position = VoicePosition;

            if (state != _state)
            {
                if (state == CombatState.Attack)
                    DungeonAudioComponent.Play(_monster.Config.Voice, position, 0.9f, Random.Range(0.9f, 1.1f), transform);

                _state = state;
            }

            bool isAlive = _monster.Fighter.Health.IsAlive;

            if (_wasAlive && !isAlive)
                DungeonAudioComponent.Play(_monster.Config.Voice, position, 1f, 0.7f, transform);

            _wasAlive = isAlive;

            // The voice of a charger announces its ram, so it stays silent otherwise.
            if (isAlive && !_monster.Config.IsCharger && Time.time >= _nextIdleVoice)
            {
                _nextIdleVoice = Time.time + Random.Range(6f, 16f);
                DungeonAudioComponent.Play(_monster.Config.Voice, position, 0.35f, Random.Range(0.8f, 1f), transform);
            }
        }

        private void Play(AudioClip[] clips, float volume)
        {
            AudioClip clip = MonsterSoundConfig.Pick(clips);

            if (clip != null)
                DungeonAudioComponent.Play(clip, VoicePosition, volume, Random.Range(0.95f, 1.05f), transform);
        }

        private void OnHitEvent(HitEventData hit)
        {
            MonsterSoundConfig sounds = _monster.Config.Sounds;

            if (sounds == null || hit.Result == HitResult.Blocked || !_monster.Fighter.Health.IsAlive || Time.time < _nextPain)
                return;

            _nextPain = Time.time + PainCooldown;
            Play(sounds.Pain, 0.9f);
        }
    }
}
