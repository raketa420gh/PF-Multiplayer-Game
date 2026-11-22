using Fusion;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Training target: blows land on it as on an adventurer and it is whole again the next tick. It may shrug off a share of
    /// physical and magical damage, and it shows every hit as a number coloured by the kind of damage, risen from the point
    /// the blow touched it; a dot stays on that point for a while.
    public sealed class TrainingDummyComponent : NetworkBehaviour, DamageReceiverComponent.IHitModifier
    {
        [SerializeField]
        private HealthComponent _health;

        [SerializeField]
        private DamageReceiverComponent _receiver;

        [SerializeField]
        private Color _physicalColor = Color.white;

        [SerializeField]
        private Color _magicalColor = new(0.45f, 0.75f, 1f);

        [SerializeField]
        private Color _trueColor = new(1f, 0.9f, 0.3f);

        [SerializeField, Tooltip("Numbers of hits without a contact point rise from this height above the feet")]
        private float _numberHeight = 2f;

        [SerializeField, Tooltip("Numbers start this far off the surface, so the body does not hide them")]
        private float _numberOffset = 0.12f;

        [SerializeField, Tooltip("Dot left where a blow touched the dummy: a switched off child, copied per hit")]
        private GameObject _markTemplate;

        [SerializeField]
        private float _markLifetime = 15f;

        [Networked]
        private float _reduction { get; set; }

        public override void Spawned()
        {
            _receiver.SetModifier(this);
            _receiver.OnHitEvent += OnHitEvent;
            // It never dies, not even for the tick before it is whole again.
            _health.SetFloor(1);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _receiver.OnHitEvent -= OnHitEvent;
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority && _health.CurrentHealth < _health.MaxHealth)
                _health.CurrentHealth = _health.MaxHealth;
        }

        /// Spawn callback: the share of physical and magical damage the dummy ignores (0..1).
        public void Setup(float reduction)
        {
            _reduction = reduction;
        }

        void DamageReceiverComponent.IHitModifier.ModifyOutgoingHit(DamageReceiverComponent victim, ref HitRequest request)
        {
        }

        int DamageReceiverComponent.IHitModifier.ModifyIncomingDamage(int damage, in HitRequest request)
        {
            return request.IgnoreArmor || request.DamageType == DamageType.True ? damage : Mathf.RoundToInt(damage * (1f - _reduction));
        }

        private void OnHitEvent(HitEventData hit)
        {
            if (hit.Result != HitResult.Hit || BattleContext.Instance == null)
                return;

            Color color = hit.Type == DamageType.Magical ? _magicalColor : hit.Type == DamageType.True ? _trueColor : _physicalColor;
            bool hasContact = hit.Point != Vector3.zero;
            BattleContext.Instance.Feedback.ShowDamage(hasContact ? hit.Point + hit.Normal * _numberOffset : transform.position + Vector3.up * _numberHeight, hit.Damage, color);

            if (!hasContact)
                return;

            GameObject mark = Instantiate(_markTemplate, hit.Point, Quaternion.identity, transform);
            mark.SetActive(true);
            Destroy(mark, _markLifetime);
        }
    }
}
