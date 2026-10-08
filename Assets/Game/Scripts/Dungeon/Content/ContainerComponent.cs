using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Chest, coffin, barrel, corpse: a networked inventory that opens at once; its loot is then searched item by item.
    public sealed class ContainerComponent : InteractableComponent
    {
        public override string Prompt => (IsOpen ? "Search" : _openVerb) + " " + _displayName;
        public override float HoldTime => 0f;
        /// A body can be looted only once it is dead.
        public override bool IsAvailable => (_body == null || _body.IsDead) && (_hanged == null || _hanged.IsLootable);
        public InventoryComponent Inventory => _inventory;
        public LootTableConfig LootTable => _lootTable;
        public string DisplayName => _displayName;

        [Networked]
        public NetworkBool IsOpen { get; private set; }

        [SerializeField]
        private InventoryComponent _inventory;

        [SerializeField]
        private string _displayName = "Chest";

        [SerializeField]
        private string _openVerb = "Open";

        [SerializeField]
        private LootTableConfig _lootTable;

        [SerializeField]
        private Transform _lid;

        [SerializeField]
        private Vector3 _lidOpenEuler = new(-110f, 0f, 0f);

        [SerializeField]
        private bool _isRemovedWhenEmpty;

        [SerializeField, Tooltip("Monster corpses: health of the body this container lies on")]
        private HealthComponent _body;

        [SerializeField, Tooltip("Interaction trigger switched on together with the availability")]
        private Collider _trigger;

        [SerializeField, Tooltip("Hanged bodies: a living one wakes up instead of being looted")]
        private HangedCorpseComponent _hanged;

        private float _lidBlend;

        public override void Render()
        {
            if (_trigger != null && _trigger.enabled != IsAvailable)
                _trigger.enabled = IsAvailable;

            if (_lid == null)
                return;

            _lidBlend = Mathf.MoveTowards(_lidBlend, IsOpen ? 1f : 0f, Time.deltaTime * 2.5f);
            _lid.localRotation = Quaternion.Euler(_lidOpenEuler * Mathf.SmoothStep(0f, 1f, _lidBlend));
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || !_isRemovedWhenEmpty || !IsOpen || _inventory.CountItems() > 0)
                return;

            Runner.Despawn(Object);
        }

        public void Fill(LootTableConfig table, int seed)
        {
            if (table != null)
                table.Roll(_inventory, seed);

            if (_hanged != null)
                _hanged.Arm(seed);
        }

        public override void Complete(AdventurerComponent adventurer)
        {
            if (_hanged != null && _hanged.TryWake())
                return;

            IsOpen = true;
            adventurer.OpenContainer(this);
        }

        /// Empties and closes the container between matches.
        public void ResetContainer()
        {
            _inventory.Clear();
            IsOpen = false;
        }
    }
}
