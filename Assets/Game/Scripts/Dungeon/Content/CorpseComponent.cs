using System.Collections.Generic;
using Fusion;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Dead adventurer body: a lootable container with the fallen character's full inventory and gear.
    /// Every client drops its own ragdoll from the pose the adventurer had there; only the moment of death goes over the network.
    public sealed class CorpseComponent : NetworkBehaviour
    {
        /// The owner watches their body fall before the death screen comes up.
        public static bool IsDeathCamActive => s_deathCam != null;
        public ContainerComponent Container => _container;
        /// The server sees no ragdoll: it lets a body be searched once any client's could have come to rest. Clients look at the bones,
        /// which take the look ray only after the body stops.
        public bool IsLootable => !HasStateAuthority || (Runner.Tick - _deathTick) * Runner.DeltaTime >= _lootDelay;

        [Networked]
        public byte ClassId { get; private set; }

        [Networked]
        public NetworkString<_32> OwnerName { get; private set; }

        [SerializeField]
        private ContainerComponent _container;

        [SerializeField]
        private InventoryComponent _inventory;

        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private CharacterModelComponent _model;

        [SerializeField]
        private RagdollComponent _ragdoll;

        [SerializeField]
        private ArmorPieceSetConfig _pieceSet;

        [SerializeField]
        private ClassConfig[] _classes;

        [SerializeField]
        private string _deathState = "Death";

        [SerializeField, Tooltip("Speed the killing blow gives the bone it struck")]
        private float _pushSpeed = 2.5f;

        [SerializeField]
        private float _lootDelay = 1f;

        [SerializeField]
        private float _camDistance = 2.4f;

        [SerializeField]
        private float _camHeight = 1.4f;

        [SerializeField]
        private float _camBlend = 0.8f;

        [SerializeField, Tooltip("How long the camera stays on the body after it stops")]
        private float _camHold = 1.2f;

        [Networked]
        private NetworkId _source { get; set; }

        [Networked]
        private Vector3 _velocity { get; set; }

        [Networked]
        private Vector3 _hitPoint { get; set; }

        [Networked]
        private Vector3 _push { get; set; }

        [Networked]
        private int _deathTick { get; set; }

        private const float LootRadius = 1.5f;
        private const int LevelMask = 1;

        private static readonly List<CorpseComponent> s_all = new();
        private static CorpseComponent s_deathCam;

        private ArmorDresser _dresser;
        private int _shownVersion = -1;
        private Vector3 _camFromPosition;
        private Quaternion _camFromRotation;
        private Vector3 _camBack;
        private float _camTime;
        private float _settledTime = -1f;

        private void LateUpdate()
        {
            if (s_deathCam == this)
                UpdateDeathCam();
        }

        public override void Spawned()
        {
            s_all.Add(this);
            _dresser = new ArmorDresser(_animator, _pieceSet, gameObject.layer);
            _animator.SetLayerWeight(1, 0f);
            _ragdoll.OnSettled += OnRagdollSettled;
            ApplyClassColor();

            if (!GameServer.IsDedicated && Runner.TryFindObject(_source, out NetworkObject source) && source.TryGetComponent(out FighterAnimComponent anim))
            {
                Fall(source, anim.Animator);

                return;
            }

            // Late join or the adventurer is out of reach: the body is already lying there.
            PoseDeath();
            _ragdoll.Freeze();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            s_all.Remove(this);
            _ragdoll.OnSettled -= OnRagdollSettled;

            if (s_deathCam == this)
                s_deathCam = null;
        }

        /// The body keeps wearing whatever is still equipped on it.
        public override void Render()
        {
            if (_shownVersion == _inventory.Version)
                return;

            _shownVersion = _inventory.Version;
            _dresser.Apply(_inventory);
        }

        /// State authority, before spawning: what the body carries and how it went down.
        public void Setup(InventoryComponent source, ClassConfig config, string ownerName, NetworkId body, Vector3 velocity, Vector3 hitPoint, Vector3 push)
        {
            _inventory.CopyFrom(source);
            ClassId = config.Id;
            OwnerName = ownerName;
            _source = body;
            _velocity = velocity;
            _hitPoint = hitPoint;
            _push = push;
            _deathTick = Runner.Tick;
        }

        /// Server side of the look at a body: whichever lies closest along the line of sight, near enough to where it died.
        public static CorpseComponent FindAlong(Vector3 origin, Vector3 direction, float range)
        {
            CorpseComponent nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (CorpseComponent corpse in s_all)
            {
                Vector3 offset = corpse.transform.position + Vector3.up * 0.3f - origin;
                float distance = Mathf.Clamp(Vector3.Dot(offset, direction), 0f, range);

                if (distance >= nearestDistance || (offset - direction * distance).sqrMagnitude > LootRadius * LootRadius)
                    continue;

                nearest = corpse;
                nearestDistance = distance;
            }

            return nearest;
        }

        private void Fall(NetworkObject source, Animator body)
        {
            _ragdoll.Begin(body, _velocity, _hitPoint, _push * _pushSpeed);

            foreach (Renderer renderer in body.GetComponentsInChildren<Renderer>())
                renderer.enabled = false;

            foreach (Light light in body.GetComponentsInChildren<Light>())
                light.enabled = false;

            if (!source.HasInputAuthority || BattleContext.Instance == null)
                return;

            Transform camera = BattleContext.Instance.Camera.transform;
            _camFromPosition = camera.position;
            _camFromRotation = camera.rotation;
            _camBack = -Vector3.ProjectOnPlane(camera.forward, Vector3.up).normalized;
            _camTime = 0f;
            s_deathCam = this;
        }

        private void PoseDeath()
        {
            _animator.enabled = true;
            _animator.Play(_deathState, 0, 0.999f);
            _animator.Update(0f);
            _animator.speed = 0f;
        }

        /// Third person on the falling body, eased out of the first-person view; it lets go a moment after the body stops.
        private void UpdateDeathCam()
        {
            if (_settledTime >= 0f && Time.time - _settledTime >= _camHold)
            {
                s_deathCam = null;

                return;
            }

            _camTime += Time.deltaTime;
            float blend = Mathf.SmoothStep(0f, 1f, _camTime / _camBlend);
            Vector3 pivot = _ragdoll.Hips.position + Vector3.up * 0.2f;
            Vector3 toCamera = _camBack * _camDistance + Vector3.up * _camHeight;
            float distance = toCamera.magnitude;

            if (Physics.SphereCast(pivot, 0.2f, toCamera / distance, out RaycastHit hit, distance, LevelMask, QueryTriggerInteraction.Ignore))
                distance = hit.distance;

            Vector3 position = Vector3.Lerp(_camFromPosition, pivot + toCamera.normalized * distance, blend);
            Quaternion rotation = Quaternion.Slerp(_camFromRotation, Quaternion.LookRotation(pivot - position), blend);
            BattleContext.Instance.Camera.transform.SetPositionAndRotation(position, rotation);
        }

        private void ApplyClassColor()
        {
            if (_model == null)
                return;

            foreach (ClassConfig config in _classes)
            {
                if (config.Id == ClassId)
                    _model.SetBodyColor(config.BodyColor);
            }
        }

        private void OnRagdollSettled(bool isLost)
        {
            _settledTime = Time.time;

            if (isLost)
                PoseDeath();
        }
    }
}
