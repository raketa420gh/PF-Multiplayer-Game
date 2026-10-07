using Fusion;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Casting, item use, death, extraction, level-up and swarm damage sounds driven by networked state changes.
    public sealed class AdventurerSoundComponent : NetworkBehaviour
    {
        [SerializeField]
        private AdventurerComponent _adventurer;

        private PendingAction _pending;
        private AdventurerState _state;
        private int _level;
        private float _swarmTimer;
        private bool _isFirst = true;

        public override void Render()
        {
            Vector3 position = transform.position + Vector3.up * 1.4f;
            PendingAction pending = _adventurer.Pending;

            if (pending != _pending)
            {
                _pending = pending;

                if (!_isFirst)
                {
                    switch (pending)
                    {
                        case PendingAction.Ability:
                            DungeonAudioComponent.Play(DungeonSound.Cast, position, 0.7f);
                            break;
                        case PendingAction.Consumable:
                            DungeonAudioComponent.Play(IsBandage() ? DungeonSound.Bandage : DungeonSound.Drink, position, 0.8f);
                            break;
                    }
                }
            }

            if (_adventurer.State != _state)
            {
                _state = _adventurer.State;

                if (!_isFirst && _state == AdventurerState.Dead)
                    DungeonAudioComponent.Play(DungeonSound.Death, position, 1f);
                else if (!_isFirst && _state is AdventurerState.Extracted or AdventurerState.Descended && HasInputAuthority)
                    DungeonAudioComponent.PlayUi(DungeonSound.Extract, 0.8f);
            }

            PlayerSessionComponent session = _adventurer.Session;

            if (session != null && session.Level != _level)
            {
                if (!_isFirst && HasInputAuthority && session.Level > _level)
                    DungeonAudioComponent.PlayUi(DungeonSound.LevelUp, 0.8f);

                _level = session.Level;
            }

            if (HasInputAuthority && _adventurer.IsInSwarm)
            {
                _swarmTimer -= Time.deltaTime;

                if (_swarmTimer <= 0f)
                {
                    _swarmTimer = 1f;
                    DungeonAudioComponent.PlayUi(DungeonSound.SwarmTick, 0.5f);
                }
            }

            _isFirst = false;
        }

        private bool IsBandage()
        {
            InventoryComponent inventory = _adventurer.Inventory;

            for (EquipSlot slot = EquipSlot.Utility1; slot <= EquipSlot.Utility6; slot++)
            {
                if (inventory.GetConfig(inventory.GetEquipped(slot)) is ConsumableItemConfig consumable && consumable.Effect == ConsumableEffect.HealInstant)
                    return true;
            }

            return false;
        }
    }
}
