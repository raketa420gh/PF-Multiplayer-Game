using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    public enum ShrineKind : byte
    {
        Health,
        Protection,
        Power,
        Speed
    }

    /// One-use altar: heals or grants a timed buff.
    public sealed class ShrineComponent : InteractableComponent
    {
        public override string Prompt => IsUsed ? "Depleted shrine" : $"Pray at the Shrine of {_kind}";
        public override bool IsAvailable => !IsUsed;

        [Networked]
        public NetworkBool IsUsed { get; private set; }

        [SerializeField]
        private ShrineKind _kind;

        [SerializeField]
        private float _magnitude = 50f;

        [SerializeField]
        private float _duration = 60f;

        [SerializeField]
        private GameObject _glow;

        public override void Render()
        {
            if (_glow != null && _glow.activeSelf == IsUsed)
                _glow.SetActive(!IsUsed);
        }

        public override void Complete(AdventurerComponent adventurer)
        {
            if (IsUsed)
                return;

            IsUsed = true;

            switch (_kind)
            {
                case ShrineKind.Health:
                    adventurer.Effects.Add(StatusEffectKind.HealOverTime, _magnitude, _duration);
                    break;
                case ShrineKind.Protection:
                    adventurer.Effects.Add(StatusEffectKind.Protection, _magnitude, _duration);
                    break;
                case ShrineKind.Power:
                    adventurer.Effects.Add(StatusEffectKind.Power, _magnitude, _duration);
                    break;
                case ShrineKind.Speed:
                    adventurer.Effects.Add(StatusEffectKind.Haste, _magnitude, _duration);
                    break;
            }
        }
    }
}
