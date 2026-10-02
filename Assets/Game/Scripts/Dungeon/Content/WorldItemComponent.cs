using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// An item lying on the floor. Picked up with a short hold.
    public sealed class WorldItemComponent : InteractableComponent
    {
        public override string Prompt => "Take " + (Config != null ? Config.DisplayName : "item");
        public override float HoldTime => 0.35f;
        public ItemConfig Config => _database.Get(Stack.ItemId);

        [Networked]
        public ItemStack Stack { get; private set; }

        [SerializeField]
        private ItemDatabase _database;

        [SerializeField]
        private Renderer _renderer;

        [SerializeField]
        private float _lifetime = 300f;

        [Networked]
        private TickTimer _despawnTimer { get; set; }

        private short _shownItem = -1;

        public override void Spawned()
        {
            if (HasStateAuthority)
                _despawnTimer = TickTimer.CreateFromSeconds(Runner, _lifetime);
        }

        public override void Render()
        {
            if (_shownItem == Stack.ItemId || _renderer == null)
                return;

            _shownItem = Stack.ItemId;
            ItemConfig config = Config;

            if (config != null)
                _renderer.material.SetColor("_BaseColor", config.IconColor);
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority && _despawnTimer.Expired(Runner))
                Runner.Despawn(Object);
        }

        public void Setup(ItemStack stack)
        {
            Stack = stack.At(0, 0);
        }

        public override void Complete(AdventurerComponent adventurer)
        {
            if (!adventurer.Inventory.TryAdd(Stack))
                return;

            Runner.Despawn(Object);
        }
    }
}
