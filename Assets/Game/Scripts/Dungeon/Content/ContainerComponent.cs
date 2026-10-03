using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Chest, coffin, barrel, corpse: a networked inventory the adventurer opens by holding F.
    public sealed class ContainerComponent : InteractableComponent
    {
        public override string Prompt => IsOpen ? "Search " + _displayName : "Open " + _displayName;
        public override float HoldTime => IsOpen ? 0.2f : _openTime;
        public override byte BusyKind => IsOpen ? AdventurerComponent.BusyInteract : AdventurerComponent.BusyOpen;
        public InventoryComponent Inventory => _inventory;
        public LootTableConfig LootTable => _lootTable;
        public string DisplayName => _displayName;

        [Networked]
        public NetworkBool IsOpen { get; private set; }

        [Networked]
        public NetworkBool IsLooted { get; private set; }

        [SerializeField]
        private InventoryComponent _inventory;

        [SerializeField]
        private string _displayName = "Chest";

        [SerializeField]
        private float _openTime = 1.5f;

        [SerializeField]
        private LootTableConfig _lootTable;

        [SerializeField]
        private Transform _lid;

        [SerializeField]
        private Vector3 _lidOpenEuler = new(-110f, 0f, 0f);

        [SerializeField]
        private bool _isRemovedWhenEmpty;

        private float _lidBlend;

        public override void Render()
        {
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
            if (table == null)
                return;

            table.Roll(_inventory, seed);
            IsLooted = false;
        }

        public override void Complete(AdventurerComponent adventurer)
        {
            IsOpen = true;
            adventurer.OpenContainer(this);
        }

        public void MarkLooted()
        {
            IsLooted = true;
        }

        /// Empties and closes the container between matches.
        public void ResetContainer()
        {
            _inventory.Clear();
            IsOpen = false;
            IsLooted = false;
        }
    }
}
