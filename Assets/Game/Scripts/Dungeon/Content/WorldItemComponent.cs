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
        private Transform _modelRoot;

        [SerializeField]
        private float _lifetime = 300f;

        [Networked]
        private TickTimer _despawnTimer { get; set; }

        private short _shownItem = -1;
        private GameObject _model;

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

            if (_model != null)
                Destroy(_model);

            GameObject prefab = config != null ? config.WorldModel : null;
            _renderer.enabled = prefab == null;

            if (prefab == null)
            {
                if (config != null)
                    _renderer.material.SetColor("_BaseColor", config.IconColor);

                return;
            }

            _model = Instantiate(prefab, _modelRoot != null ? _modelRoot : transform, false);
            bool isWeapon = config is WeaponItemConfig;
            _model.transform.localPosition = isWeapon ? new Vector3(0f, 0.06f, 0f) : Vector3.zero;
            _model.transform.localRotation = isWeapon ? Quaternion.Euler(0f, 0f, 90f) : Quaternion.identity;

            foreach (Collider collider in _model.GetComponentsInChildren<Collider>())
                Destroy(collider);

            foreach (Light light in _model.GetComponentsInChildren<Light>())
                Destroy(light);
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
