using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// An item lying on the floor. Picked up instantly; belt items go onto the belt first.
    public sealed class WorldItemComponent : InteractableComponent
    {
        public override string Prompt => "Take " + (Config != null ? Config.DisplayName : "item");
        public override float HoldTime => 0f;
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
        private bool _isPermanent;

        public override void Spawned()
        {
            if (HasStateAuthority && !_isPermanent)
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
            // Weapons lie on the flat of the blade, shields face up, the open book shows its pages as in the hand.
            WeaponClass? weaponClass = (config as WeaponItemConfig)?.WeaponClass;
            _model.transform.localPosition = weaponClass switch
            {
                null or WeaponClass.Shield => Vector3.zero,
                WeaponClass.Spellbook => new Vector3(0f, 0.02f, 0f),
                _ => new Vector3(0f, 0.06f, 0f)
            };
            _model.transform.localRotation = weaponClass switch
            {
                null => Quaternion.identity,
                // The book's pages face away from the back of the hand that holds it: -x.
                WeaponClass.Spellbook => Quaternion.Euler(0f, 0f, -90f),
                WeaponClass.Shield => Quaternion.Euler(-90f, 0f, 0f),
                _ => Quaternion.Euler(0f, 0f, 90f)
            };

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

        /// Permanent items (the test ground table) never despawn on their own. Called before Spawned.
        public void Setup(ItemStack stack, bool isPermanent = false)
        {
            Stack = stack.At(0, 0);
            _isPermanent = isPermanent;
        }

        public override void Complete(AdventurerComponent adventurer)
        {
            ItemStack stack = Stack;

            if (adventurer.Inventory.TryLoot(ref stack))
                Runner.Despawn(Object);
            else
                Stack = stack;
        }
    }
}
