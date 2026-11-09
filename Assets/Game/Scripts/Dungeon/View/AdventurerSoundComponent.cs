using Fusion;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Item use, death, extraction, level-up and swarm damage sounds driven by networked state changes.
    public sealed class AdventurerSoundComponent : NetworkBehaviour
    {
        /// A use sound shorter than this share of the use time is looped until the use ends (bandage wrapping, surgery).
        private const float LoopShare = 0.8f;

        [SerializeField]
        private AdventurerComponent _adventurer;

        private AudioSource _use;
        private PendingAction _pending;
        private bool _isThrowing;
        private byte _belt = AdventurerComponent.NoBelt;
        private AdventurerState _state;
        private int _level;
        private float _swarmTimer;
        private bool _isFirst = true;

        public override void Spawned()
        {
            _use = new GameObject("UseVoice").AddComponent<AudioSource>();
            _use.transform.SetParent(transform, false);
            _use.transform.localPosition = Vector3.up * 1.4f;
            _use.playOnAwake = false;
            _use.spatialBlend = 1f;
            _use.rolloffMode = AudioRolloffMode.Linear;
            _use.minDistance = 1.5f;
            _use.maxDistance = 24f;
            _use.dopplerLevel = 0f;
            _use.volume = 0.6f;
        }

        public override void Render()
        {
            Vector3 position = transform.position + Vector3.up * 1.4f;
            PendingAction pending = _adventurer.Pending;

            if (pending != _pending)
            {
                _pending = pending;
                _use.Stop();

                if (!_isFirst && pending is PendingAction.Consumable or PendingAction.Utility)
                    PlayUse();
            }

            byte busy = _adventurer.Fighter.Combat.BusyKind;
            bool isThrowing = pending == PendingAction.Throw && busy is AdventurerComponent.BusyThrowRelease or AdventurerComponent.BusyTorchThrow;

            if (isThrowing != _isThrowing)
            {
                _isThrowing = isThrowing;

                if (!_isFirst && isThrowing && PendingItem() is { } thrown)
                    DungeonAudioComponent.Play(thrown.PickSound(ItemSound.Throw), position, 1f, Random.Range(0.96f, 1.04f), transform);
            }

            if (_adventurer.BeltSlot != _belt)
            {
                _belt = _adventurer.BeltSlot;
                ItemConfig drawn = _adventurer.HasBeltItemInHand ? _adventurer.Inventory.GetConfig(_adventurer.Inventory.GetEquipped((EquipSlot)_belt)) : null;

                if (!_isFirst && drawn != null)
                    DungeonAudioComponent.Play(drawn.PickSound(ItemSound.Equip), position, 1f, Random.Range(0.96f, 1.04f), transform);
            }

            if (_adventurer.State != _state)
            {
                _state = _adventurer.State;

                if (!_isFirst && _state == AdventurerState.Dead)
                    DungeonAudioComponent.Play(DungeonSound.Death, position, 1f, 1f, transform);
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

        /// The item's own use sound, looped while a long use lasts; items without one fall back to the generic drink / bandage.
        private void PlayUse()
        {
            ItemConfig item = PendingItem();
            AudioClip clip = item != null ? item.PickSound(ItemSound.Use) : null;
            float useTime = item switch
            {
                ConsumableItemConfig consumable => consumable.UseTime,
                UtilityItemConfig utility => utility.UseTime,
                _ => 0f
            };

            if (clip == null && _pending == PendingAction.Consumable)
                clip = DungeonAudioComponent.Clip(item is ConsumableItemConfig { Effect: ConsumableEffect.HealInstant } ? DungeonSound.Bandage : DungeonSound.Drink);

            if (clip == null)
                return;

            _use.clip = clip;
            _use.loop = clip.length < useTime * LoopShare;
            _use.pitch = Random.Range(0.97f, 1.03f);
            _use.Play();
        }

        private ItemConfig PendingItem()
        {
            InventoryComponent inventory = _adventurer.Inventory;

            return inventory.GetConfig(inventory.GetEquipped((EquipSlot)_adventurer.PendingSlot));
        }
    }
}
