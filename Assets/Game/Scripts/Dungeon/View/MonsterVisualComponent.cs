using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Dresses a monster with the attachments listed in its config (rusty helmets, rags, a crown for the boss) and gives it a voice.
    public sealed class MonsterVisualComponent : NetworkBehaviour
    {
        [SerializeField]
        private MonsterComponent _monster;

        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private ArmorPieceSetConfig _pieceSet;

        private Game.Scripts.Battle.CombatState _state;
        private bool _wasAlive = true;
        private float _nextIdleVoice;

        public override void Spawned()
        {
            if (_animator != null)
            {
                ArmorDresser dresser = new ArmorDresser(_animator, _pieceSet, gameObject.layer);

                foreach (MonsterAttachment attachment in _monster.Config.Attachments)
                    dresser.Show(attachment.Visual, attachment.Color);
            }

            _nextIdleVoice = Time.time + Random.Range(4f, 12f);
        }

        public override void Render()
        {
            Game.Scripts.Battle.CombatState state = _monster.Fighter.Combat.State;
            Vector3 position = transform.position + Vector3.up * 1.4f;

            if (state != _state)
            {
                if (state == Game.Scripts.Battle.CombatState.Attack)
                    DungeonAudioComponent.Play(_monster.Config.Voice, position, 0.9f, Random.Range(0.9f, 1.1f));

                _state = state;
            }

            bool isAlive = _monster.Fighter.Health.IsAlive;

            if (_wasAlive && !isAlive)
                DungeonAudioComponent.Play(_monster.Config.Voice, position, 1f, 0.7f);

            _wasAlive = isAlive;

            // The voice of a charger announces its ram, so it stays silent otherwise.
            if (isAlive && !_monster.Config.IsCharger && Time.time >= _nextIdleVoice)
            {
                _nextIdleVoice = Time.time + Random.Range(6f, 16f);
                DungeonAudioComponent.Play(_monster.Config.Voice, position, 0.35f, Random.Range(0.8f, 1f));
            }
        }
    }
}
