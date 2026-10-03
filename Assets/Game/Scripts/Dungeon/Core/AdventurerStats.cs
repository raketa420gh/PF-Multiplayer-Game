using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Derived character numbers: class attributes + gear + perks + buffs run through the hexagram curves.
    public sealed class AdventurerStats : ICombatStats
    {
        public ClassStats Attributes => _attributes;
        public int MaxHealth => _maxHealth;
        public float Poise => _poise;
        public float StaggerRecovery => _staggerRecovery;
        public float Impact => _impact;
        public float Guard => _guard;
        public float Load => _load;
        public float ArmorRating => _armorRating;
        public float MagicResistance => _magicResistance;
        public float PhysicalReduction => _physicalReduction;
        public float MagicalReduction => _magicalReduction;
        public float MoveSpeedRating => _moveSpeedRating;
        public float MoveSpeedMultiplier => _moveSpeedMultiplier;
        public float PhysicalPower => _physicalPower;
        public float MagicalPower => _magicalPower;
        public float ActionSpeed => _actionSpeed;
        public float HandlingSpeed => _handlingSpeed;
        public float InteractionSpeed => _interactionSpeed;
        public float Weakpoint => _weakpoint;
        public float Perception => _perception;
        public float CooldownSpeed => _cooldownSpeed;
        public float ControlResistance => _controlResistance;
        public float Concentration => _concentration;
        public float CastSpeed => _castSpeed;
        public float Mending => _mending;
        public int BonusCharges => _bonusCharges;

        private ClassStats _attributes;
        private int _maxHealth = 100;
        private float _poise = DungeonFormulas.BasePoise;
        private float _staggerRecovery = 1f;
        private float _impact = 1f;
        private float _guard = 1f;
        private float _load = 1f;
        private float _armorRating;
        private float _magicResistance;
        private float _physicalReduction;
        private float _magicalReduction;
        private float _moveSpeedRating = DungeonFormulas.BaseMoveSpeed;
        private float _moveSpeedMultiplier = 1f;
        private float _physicalPower;
        private float _magicalPower;
        private float _actionSpeed = 1f;
        private float _handlingSpeed = 1f;
        private float _interactionSpeed = 1f;
        private float _weakpoint = 1f;
        private float _perception = 1f;
        private float _cooldownSpeed = 1f;
        private float _controlResistance;
        private float _concentration = DungeonFormulas.BaseConcentration;
        private float _castSpeed = 1f;
        private float _mending = 1f;
        private int _bonusCharges;
        private readonly float[] _flat = new float[(int)StatType.Count];
        private readonly StatModifier[] _affixes = new StatModifier[ItemAffixes.MaxCount];

        public float GetDamageMultiplier(DamageType type)
        {
            return type switch
            {
                DamageType.Physical => 1f + DungeonFormulas.PowerBonus(_physicalPower) + _flat[(int)StatType.PhysicalDamageBonus],
                DamageType.Magical => 1f + DungeonFormulas.PowerBonus(_magicalPower) + _flat[(int)StatType.MagicalDamageBonus],
                _ => 1f
            };
        }

        public void Recalculate(ClassConfig config, InventoryComponent inventory, StatusEffectComponent effects, int activeWeaponSet, int perkCount,
            ShapeshiftForm form = ShapeshiftForm.None, int perkMask = 0)
        {
            System.Array.Clear(_flat, 0, _flat.Length);
            float armor = 0f;
            float magicResistance = 0f;
            float moveAdd = 0f;

            for (int i = 0; i < InventoryComponent.EquipmentCapacity; i++)
            {
                ItemStack stack = inventory.Equipment[i];
                ItemConfig item = inventory.GetConfig(stack);

                if (item == null)
                    continue;

                int tier = Mathf.Max(0, stack.Rarity - (int)ItemRarity.Common);

                foreach (StatModifier modifier in item.Modifiers)
                    Apply(modifier);

                int affixCount = ItemAffixes.Roll(item, stack, _affixes);

                for (int a = 0; a < affixCount; a++)
                    Apply(_affixes[a]);

                if (item is ArmorItemConfig armorItem)
                {
                    armor += armorItem.ArmorRating + tier * 2f;
                    magicResistance += armorItem.MagicResistance;
                    moveAdd += armorItem.MoveSpeedPenalty;
                }
                else if (item is WeaponItemConfig weaponItem && IsActiveWeapon((EquipSlot)i, activeWeaponSet))
                {
                    moveAdd -= weaponItem.MoveSpeedPenalty;
                }
            }

            int applied = 0;

            for (int i = 0; i < config.Perks.Length && applied < perkCount; i++)
            {
                if ((perkMask & (1 << i)) == 0)
                    continue;

                applied++;

                foreach (StatModifier modifier in config.Perks[i].Modifiers)
                    Apply(modifier);
            }

            float rage = Effect(effects, StatusEffectKind.Rage);
            float flesh = Attribute(StatType.Flesh);
            float grip = Attribute(StatType.Grip) + Effect(effects, StatusEffectKind.Grip) + rage;
            float reflex = Attribute(StatType.Reflex);
            float craft = Attribute(StatType.Craft);
            float insight = Attribute(StatType.Insight);
            float resonance = Attribute(StatType.Resonance);

            _attributes = new ClassStats(Mathf.RoundToInt(flesh), Mathf.RoundToInt(grip), Mathf.RoundToInt(reflex),
                Mathf.RoundToInt(craft), Mathf.RoundToInt(insight), Mathf.RoundToInt(resonance));

            // Flesh
            float healthBonus = _flat[(int)StatType.MaxHealth] + Effect(effects, StatusEffectKind.Fortify) + FormHealthBonus(form);
            _maxHealth = Mathf.CeilToInt(DungeonFormulas.BaseHealth * DungeonFormulas.Scale(flesh, 1f) * (1f + healthBonus / 100f));
            _poise = Mathf.Max(0f, DungeonFormulas.BasePoise * DungeonFormulas.Scale(flesh, 1.5f));

            // Grip
            _physicalPower = grip + _flat[(int)StatType.PhysicalPower] + Effect(effects, StatusEffectKind.Power);
            _guard = Mathf.Max(0.3f, DungeonFormulas.Scale(grip, 1f));
            _load = Mathf.Max(0f, 2f - DungeonFormulas.Scale(grip, 1f));

            // Reflex
            _actionSpeed = Mathf.Max(0.4f, DungeonFormulas.Scale(reflex, 0.5f) + _flat[(int)StatType.ActionSpeed] / 100f + Effect(effects, StatusEffectKind.ActionSpeed) / 100f);
            _staggerRecovery = Mathf.Max(0.4f, DungeonFormulas.Scale(reflex, 0.8f));

            // Craft
            _interactionSpeed = Mathf.Max(0.4f, DungeonFormulas.Scale(craft, 1.5f));
            _weakpoint = Mathf.Max(0.5f, DungeonFormulas.Scale(craft, 0.5f));

            // Insight
            _cooldownSpeed = Mathf.Max(0.4f, DungeonFormulas.Scale(insight, 0.6f));
            _controlResistance = Mathf.Clamp(DungeonFormulas.Curve(insight), -0.5f, 0.8f);
            _concentration = Mathf.Max(0f, DungeonFormulas.BaseConcentration * DungeonFormulas.Scale(insight, 2f));

            // Resonance
            _magicalPower = resonance + _flat[(int)StatType.MagicalPower];
            _bonusCharges = DungeonFormulas.BonusCharges(resonance);

            // Edges: geometric mean of two neighbours on the ring.
            _impact = Mathf.Max(0.3f, DungeonFormulas.Scale(DungeonFormulas.Edge(flesh, grip), 1f));
            _handlingSpeed = Mathf.Max(0.4f, DungeonFormulas.Scale(DungeonFormulas.Edge(reflex, craft), 1f));
            _perception = Mathf.Max(0.3f, DungeonFormulas.Scale(DungeonFormulas.Edge(craft, insight), 1f));
            _castSpeed = Mathf.Max(0.4f, DungeonFormulas.Scale(DungeonFormulas.Edge(insight, resonance), 0.8f));
            _mending = Mathf.Max(0.3f, DungeonFormulas.Scale(DungeonFormulas.Edge(resonance, flesh), 0.6f));

            float haste = Effect(effects, StatusEffectKind.Haste) - Effect(effects, StatusEffectKind.Slow) * (1f - _controlResistance);
            float rating = DungeonFormulas.BaseMoveSpeed * DungeonFormulas.Scale(DungeonFormulas.Edge(grip, reflex), 0.2f)
                + (moveAdd < 0f ? moveAdd * _load : moveAdd) + _flat[(int)StatType.MoveSpeed] + FormMoveAdd(form);
            rating *= 1f + (haste + rage * 0.7f) / 100f;
            _moveSpeedRating = Mathf.Min(rating, DungeonFormulas.MaxMoveSpeed);
            _moveSpeedMultiplier = Mathf.Max(0.3f, _moveSpeedRating / DungeonFormulas.BaseMoveSpeed);

            _armorRating = armor + _flat[(int)StatType.ArmorRating];
            _magicResistance = DungeonFormulas.BaseMagicResistance + magicResistance + _flat[(int)StatType.MagicResistance];
            _physicalReduction = DungeonFormulas.ArmorReduction(_armorRating) + rage * -0.01f;
            _magicalReduction = DungeonFormulas.MagicReduction(_magicResistance);

            float Attribute(StatType stat) => config.BaseStats.Get(stat) + _flat[(int)stat];
        }

        public bool HasThreshold(StatType attribute)
        {
            return _attributes.Get(attribute) >= DungeonFormulas.Threshold;
        }

        public int ModifyIncomingDamage(int damage, DamageType type, HitZone zone)
        {
            float reduction = type switch
            {
                DamageType.Physical => _physicalReduction,
                DamageType.Magical => _magicalReduction,
                _ => 0f
            };

            return Mathf.RoundToInt(damage * (1f - reduction));
        }

        private static float FormHealthBonus(ShapeshiftForm form)
        {
            return form switch
            {
                ShapeshiftForm.Bear => 50f,
                ShapeshiftForm.Rat => -95f,
                _ => 0f
            };
        }

        private static float FormMoveAdd(ShapeshiftForm form)
        {
            return form switch
            {
                ShapeshiftForm.Bear => -60f,
                ShapeshiftForm.Panther => 15f,
                ShapeshiftForm.Rat => 30f,
                _ => 0f
            };
        }

        private static float Effect(StatusEffectComponent effects, StatusEffectKind kind)
        {
            return effects != null ? effects.GetMagnitude(kind) : 0f;
        }

        private static bool IsActiveWeapon(EquipSlot slot, int activeWeaponSet)
        {
            return activeWeaponSet == 0
                ? slot is EquipSlot.Weapon1Main or EquipSlot.Weapon1Off
                : slot is EquipSlot.Weapon2Main or EquipSlot.Weapon2Off;
        }

        private void Apply(in StatModifier modifier)
        {
            _flat[(int)modifier.Stat] += modifier.Value;
        }
    }
}
