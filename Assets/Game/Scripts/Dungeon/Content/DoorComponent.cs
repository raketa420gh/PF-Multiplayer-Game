using Fusion;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// A door that opens on a hold and, if it is a wooden one, can be broken down as in Dark and Darker: every blow that lands on the leaf takes the
    /// weapon's prop power minus the door's Impact Resistance off its Impact Endurance; a haft takes nothing. Open or locked, it breaks the same,
    /// and stays broken: the leaf flies apart and the doorway is open for good.
    public sealed class DoorComponent : InteractableComponent, IWorldHitReceiver
    {
        public override string Prompt => IsOpen ? "Close door" : IsLocked ? "Locked door" : "Open door";
        public override float HoldTime => IsOpen ? 0.15f : _openTime;
        public override bool IsAvailable => !IsBroken;
        public override bool IsRooting => !IsOpen;
        public override bool IsHandsOccupied => true;
        /// Someone is holding the door open; ends by itself at the tick the hold would have completed.
        public bool IsOpening => !IsOpen && Runner.Tick < _openingEndTick;
        public bool IsBroken => _endurance > 0 && _damage >= _endurance;

        [Networked]
        public NetworkBool IsOpen { get; private set; }

        [Networked]
        public NetworkBool IsLocked { get; private set; }

        [Networked]
        private int _openingEndTick { get; set; }

        /// The opener stood on the leaf's -Z side: it swings to +Z, away from them.
        [Networked]
        private NetworkBool _isOpenBackward { get; set; }

        /// Impact Endurance the blows have taken off so far.
        [Networked]
        private byte _damage { get; set; }

        [SerializeField]
        private Transform _leaf;

        [SerializeField]
        private float _openAngle = 100f;

        [SerializeField]
        private float _swingSpeed = 220f;

        [SerializeField]
        private float _openTime = 3f;

        [SerializeField]
        private Collider _blocker;

        [SerializeField]
        private bool _startsLocked;

        [SerializeField, Tooltip("Impact Endurance (DaD): what the blows have to take off to break the door down; 0 = it cannot be broken")]
        private int _endurance;

        [SerializeField, Tooltip("Impact Resistance (DaD): subtracted from the prop power of each blow")]
        private int _resistance = 3;

        [SerializeField, Tooltip("Interaction trigger: gone together with the leaf")]
        private Collider _trigger;

        [SerializeField, Tooltip("Physics layer of the planks a broken leaf flies apart into: they lie on the floor and are in nobody's way")]
        private int _debrisLayer;

        private const int Planks = 7;
        private const float DebrisTime = 8f;
        /// Degrees a blow that harms the leaf knocks it out of place, and how fast it comes back.
        private const float Jolt = 4f;
        private const float JoltDecay = 30f;

        private float _angle;
        private float _jolt;
        private int _renderedDamage;
        private bool _isShattered;

        public override void Spawned()
        {
            if (HasStateAuthority)
                IsLocked = _startsLocked;

            _angle = IsOpen ? OpenedAngle() : 0f;
            _renderedDamage = _damage;
            ApplyAngle();

            // A late joiner finds the doorway already empty.
            if (IsBroken)
                Remove();
        }

        public override void Render()
        {
            if (_isShattered)
                return;

            if (IsBroken)
            {
                Shatter();
                Remove();
                DungeonAudioComponent.Play(DungeonSound.DoorClose, transform.position + Vector3.up, 1f, 0.6f);

                return;
            }

            if (_renderedDamage != _damage)
            {
                _renderedDamage = _damage;
                _jolt = Jolt;
            }

            float target = IsOpen ? OpenedAngle() : 0f;

            if (Mathf.Approximately(_angle, target) && _jolt == 0f)
                return;

            _angle = Mathf.MoveTowards(_angle, target, _swingSpeed * Time.deltaTime);
            _jolt = Mathf.MoveTowards(_jolt, 0f, JoltDecay * Time.deltaTime);
            ApplyAngle();
        }

        public override void FixedUpdateNetwork()
        {
            if (_blocker != null)
                _blocker.enabled = !IsOpen && !IsBroken;
        }

        public override void Begin(AdventurerComponent adventurer, int completeTick)
        {
            if (!IsOpen)
                _openingEndTick = completeTick;
        }

        public override void Cancel(AdventurerComponent adventurer)
        {
            _openingEndTick = 0;
        }

        public override void Complete(AdventurerComponent adventurer)
        {
            _openingEndTick = 0;

            if (IsBroken)
                return;

            if (IsLocked)
            {
                if (!adventurer.Stats.HasThreshold(StatType.Dexterity) && !adventurer.Inventory.TryConsumeUtility(UtilityKind.Lockpick))
                    return;

                IsLocked = false;
            }

            if (!IsOpen)
                SwingAwayFrom(adventurer.transform.position);

            IsOpen = !IsOpen;
        }

        public void Unlock()
        {
            IsLocked = false;
        }

        /// Monsters shove doors open on their way to a target.
        public void ForceOpen(Vector3 from)
        {
            SwingAwayFrom(from);
            IsLocked = false;
            IsOpen = true;
        }

        void IWorldHitReceiver.OnWorldHit(WeaponConfig weapon, bool isHandle)
        {
            if (!isHandle && !IsBroken)
                _damage = (byte)Mathf.Min(_endurance, _damage + Mathf.Max(0, weapon.PropPower - _resistance));
        }

        private void SwingAwayFrom(Vector3 position) => _isOpenBackward = Vector3.Dot(position - transform.position, transform.forward) < 0f;

        private float OpenedAngle() => _isOpenBackward ? -_openAngle : _openAngle;

        private void ApplyAngle()
        {
            if (_leaf != null)
                _leaf.localRotation = Quaternion.Euler(0f, _angle + _jolt, 0f);
        }

        private void Remove()
        {
            _isShattered = true;
            _leaf.gameObject.SetActive(false);

            if (_trigger != null)
                _trigger.enabled = false;
        }

        /// The leaf flies apart into planks that lie about for a while. A sight only: every peer throws its own.
        private void Shatter()
        {
            if (Application.isBatchMode)
                return;

            // The leaf's box in the hinge's frame: x across the doorway, y up, z through the leaf.
            Renderer renderer = _leaf.GetComponentInChildren<Renderer>();
            Bounds local = renderer.localBounds;
            Bounds box = new Bounds(_leaf.InverseTransformPoint(renderer.bounds.center), Vector3.zero);

            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = local.center + Vector3.Scale(local.extents, new Vector3((i & 1) * 2 - 1, (i >> 1 & 1) * 2 - 1, (i >> 2 & 1) * 2 - 1));
                box.Encapsulate(_leaf.InverseTransformPoint(renderer.transform.TransformPoint(corner)));
            }

            Vector3 size = Vector3.Scale(box.size, _leaf.lossyScale);

            for (int i = 0; i < Planks; i++)
            {
                Vector3 at = box.center + new Vector3(box.size.x * ((i + 0.5f) / Planks - 0.5f), box.size.y * Random.Range(-0.2f, 0.2f), 0f);
                GameObject plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plank.layer = _debrisLayer;
                plank.transform.SetPositionAndRotation(_leaf.TransformPoint(at), _leaf.rotation * Quaternion.Euler(0f, 0f, Random.Range(-12f, 12f)));
                plank.transform.localScale = new Vector3(size.x / Planks * 0.9f, size.y * Random.Range(0.3f, 0.6f), Mathf.Min(size.z, 0.08f));
                plank.GetComponent<Renderer>().sharedMaterial = renderer.sharedMaterial;
                Rigidbody body = plank.AddComponent<Rigidbody>();
                body.mass = 3f;
                body.AddForce((_leaf.forward * Random.Range(-1f, 1f) + Vector3.up * Random.Range(0.2f, 0.8f)) * 9f + Random.insideUnitSphere * 3f, ForceMode.Impulse);
                body.AddTorque(Random.insideUnitSphere * 4f, ForceMode.Impulse);
                Destroy(plank, DebrisTime);
            }
        }
    }
}
