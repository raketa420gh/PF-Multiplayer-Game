using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// An item lying on the floor. Picked up instantly; belt items go onto the belt first.
    public sealed class WorldItemComponent : InteractableComponent
    {
        private const int WorldMask = 1;
        private const float FlightRadius = 0.05f;
        /// End over end, in degrees a second; a wall sends it back at this share of its speed.
        private const float Tumble = 600f;
        private const float Bounce = 0.3f;
        /// A surface this level or flatter is a floor to rest on.
        private const float FloorNormal = 0.6f;

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

        [Networked]
        private TickTimer _burnTimer { get; set; }

        [Networked]
        private Vector3 _velocity { get; set; }

        [Networked]
        private float _tumble { get; set; }

        [Networked]
        private float _yaw { get; set; }

        private short _shownItem = -1;
        private GameObject _model;
        private bool _isPermanent;
        private ParticleSystem[] _flames = new ParticleSystem[0];
        private Light[] _lights = new Light[0];

        public override void Spawned()
        {
            if (HasStateAuthority && !_isPermanent)
                _despawnTimer = TickTimer.CreateFromSeconds(Runner, _lifetime);
        }

        public override void Render()
        {
            if (_shownItem != Stack.ItemId && _renderer != null)
                ShowModel();

            AlignFlames();
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            if (_velocity != Vector3.zero)
                Fly();

            if (_despawnTimer.Expired(Runner))
                Runner.Despawn(Object);
            else if (Stack.IsLit && _burnTimer.ExpiredOrNotRunning(Runner))
                Burn();
        }

        /// Permanent items (the test ground table) never despawn on their own. Called before Spawned.
        public void Setup(ItemStack stack, bool isPermanent = false)
        {
            Stack = stack.At(0, 0);
            _isPermanent = isPermanent;
        }

        /// A thrown item flies from where it was spawned. Called before Spawned.
        public void Launch(Vector3 velocity)
        {
            _velocity = velocity;
            _yaw = Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg;
        }

        public override void Complete(AdventurerComponent adventurer)
        {
            // A burning torch is put out as it is picked up.
            ItemStack stack = Stack.WithLit(false);

            if (adventurer.Inventory.TryLoot(ref stack))
                Runner.Despawn(Object);
            else
                Stack = stack.WithLit(Stack.IsLit);
        }

        private void ShowModel()
        {

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
            // Weapons and torches lie on their side, shields face up, the open book shows its pages as in the hand.
            WeaponClass? weaponClass = (config as WeaponItemConfig)?.WeaponClass;
            bool isTorch = config is ConsumableItemConfig { IsLight: true };
            _model.transform.localPosition = weaponClass switch
            {
                null when isTorch => new Vector3(0f, 0.03f, 0f),
                null or WeaponClass.Shield => Vector3.zero,
                WeaponClass.Spellbook => new Vector3(0f, 0.02f, 0f),
                _ => new Vector3(0f, 0.06f, 0f)
            };
            _model.transform.localRotation = weaponClass switch
            {
                null when !isTorch => Quaternion.identity,
                // The book's pages face away from the back of the hand that holds it: -x.
                WeaponClass.Spellbook => Quaternion.Euler(0f, 0f, -90f),
                WeaponClass.Shield => Quaternion.Euler(-90f, 0f, 0f),
                _ => Quaternion.Euler(0f, 0f, 90f)
            };

            foreach (Collider collider in _model.GetComponentsInChildren<Collider>())
                Destroy(collider);

            _lights = _model.GetComponentsInChildren<Light>();
            _flames = isTorch ? _model.GetComponentsInChildren<ParticleSystem>() : new ParticleSystem[0];

            foreach (Light light in _lights)
                light.enabled = Stack.IsLit;

            // An unlit torch has no flame.
            if (Stack.IsLit)
                return;

            foreach (ParticleSystem flame in _flames)
                Destroy(flame.gameObject);

            _flames = new ParticleSystem[0];
        }

        /// A burning torch still flames and shines upward, lying or tumbling through the air.
        private void AlignFlames()
        {
            foreach (ParticleSystem flame in _flames)
            {
                flame.transform.rotation = Quaternion.LookRotation(Vector3.up);

                foreach (Light light in _lights)
                    light.transform.position = flame.transform.position + Vector3.up * 0.3f;
            }
        }

        /// Under gravity, end over end, until it hits a floor and lies there along its flight, head first or last as it came
        /// down; a wall knocks it back.
        private void Fly()
        {
            float deltaTime = Runner.DeltaTime;
            Vector3 velocity = _velocity + Physics.gravity * deltaTime;
            Vector3 step = velocity * deltaTime;
            Vector3 position = transform.position + step;
            Quaternion heading = Quaternion.Euler(0f, _yaw, 0f);
            _tumble = Mathf.Repeat(_tumble + Tumble * deltaTime, 360f);

            if (Physics.SphereCast(transform.position, FlightRadius, step, out RaycastHit hit, step.magnitude, WorldMask, QueryTriggerInteraction.Ignore))
            {
                position = transform.position + step.normalized * hit.distance;

                if (hit.normal.y > FloorNormal)
                {
                    _velocity = Vector3.zero;
                    transform.SetPositionAndRotation(hit.point, heading * Quaternion.Euler(0f, _tumble < 180f ? 90f : -90f, 0f));

                    return;
                }

                velocity = Vector3.Reflect(velocity, hit.normal) * Bounce;
            }

            _velocity = velocity;
            // The head is up at the throw (the model lies along -x) and goes over forward.
            transform.SetPositionAndRotation(position, heading * Quaternion.Euler(_tumble, 0f, 0f) * Quaternion.Euler(0f, 0f, -90f));
        }

        /// A burning torch burns a second of its fuel each second and is gone when the fuel runs out.
        private void Burn()
        {
            _burnTimer = TickTimer.CreateFromSeconds(Runner, 1f);

            if (Config is ConsumableItemConfig config && Stack.Burnt + 1 < config.Duration)
                Stack = Stack.WithBurnt(Stack.Burnt + 1);
            else
                Runner.Despawn(Object);
        }
    }
}
