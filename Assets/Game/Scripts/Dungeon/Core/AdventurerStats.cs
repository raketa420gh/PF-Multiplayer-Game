using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Derived character numbers: class attributes + gear + perks + buffs run through the hexagram curves.
    public sealed class AdventurerStats : ICombatStats, StatusEffectComponent.IResistance
    {
        public ClassStats Attributes => _attributes;
        public int MaxHealth => _maxHealth;
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
        public float MagicalInteractionSpeed => _magicalInteractionSpeed;
        public float Perception => _perception;
        public float CooldownSpeed => _cooldownSpeed;
        public float CastSpeed => _castSpeed;
        public float PhysicalHealing => _physicalHealing;
        public float MagicalHealing => _magicalHealing;
        public int BonusCharges => _bonusCharges;

        private ClassStats _attributes;
        private int _maxHealth = 100;
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
        private float _magicalInteractionSpeed = 1f;
        private float _perception = 1f;
        private float _cooldownSpeed = 1f;
        private float _castSpeed = 1f;
        private float _physicalHealing = 1f;
        private float _magicalHealing = 1f;
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

        /// activeWeaponSet is -1 when the weapons are put away, heldBeltSlot is -1 when no belt item is in hand.
        public void Recalculate(ClassConfig config, InventoryComponent inventory, StatusEffectComponent effects, int activeWeaponSet, int perkCount,
            ShapeshiftForm form = ShapeshiftForm.None, int perkMask = 0, int heldBeltSlot = -1)
        {
            System.Array.Clear(_flat, 0, _flat.Length);
            float armor = 0f;
            float magicResistance = 0f;
            float moveAdd = 0f;

            for (int i = 0; i < InventoryComponent.EquipmentCapacity; i++)
            {
                ItemStack stack = inventory.Equipment[i];
                ItemConfig item = inventory.GetConfig(stack);

                if (item == null || !IsInUse((EquipSlot)i, activeWeaponSet, heldBeltSlot))
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
                else if (item is WeaponItemConfig weaponItem)
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
            float strength = Attribute(StatType.Strength) + Effect(effects, StatusEffectKind.Strength) + rage;
            float vitality = Attribute(StatType.Vitality);
            float spirit = Attribute(StatType.Spirit);
            float knowledge = Attribute(StatType.Knowledge);
            float agility = Attribute(StatType.Agility);
            float dexterity = Attribute(StatType.Dexterity);

            _attributes = new ClassStats(Mathf.RoundToInt(strength), Mathf.RoundToInt(vitality), Mathf.RoundToInt(spirit),
                Mathf.RoundToInt(knowledge), Mathf.RoundToInt(agility), Mathf.RoundToInt(dexterity));

            // Strength and Spirit only raise the powers; the damage bonus grows from the power alone.
            _physicalPower = strength + _flat[(int)StatType.PhysicalPower] + Effect(effects, StatusEffectKind.Power) + ThresholdPower(StatType.Strength);

            // Vitality
            float healthBonus = _flat[(int)StatType.MaxHealth] + Effect(effects, StatusEffectKind.Fortify) + FormHealthBonus(form);
            _maxHealth = Mathf.CeilToInt(DungeonFormulas.BaseHealth * DungeonFormulas.Scale(vitality, 1f) * (1f + healthBonus / 100f));

            _magicalPower = spirit + _flat[(int)StatType.MagicalPower] + ThresholdPower(StatType.Spirit);

            // Knowledge
            _castSpeed = Mathf.Max(0.4f, DungeonFormulas.Scale(knowledge, 0.8f));
            _bonusCharges = DungeonFormulas.BonusCharges(knowledge);

            // Agility: the only attribute that moves the feet.
            float haste = Effect(effects, StatusEffectKind.Haste) - Effect(effects, StatusEffectKind.Slow);
            float rating = DungeonFormulas.BaseMoveSpeed * DungeonFormulas.Scale(agility, 0.2f) + moveAdd + _flat[(int)StatType.MoveSpeed] + FormMoveAdd(form);
            rating *= 1f + (haste + rage * 0.7f) / 100f;
            _moveSpeedRating = Mathf.Min(rating, DungeonFormulas.MaxMoveSpeed);
            _moveSpeedMultiplier = Mathf.Max(0.3f, _moveSpeedRating / DungeonFormulas.BaseMoveSpeed);

            // Dexterity: the only attribute that speeds up the hands.
            _actionSpeed = Mathf.Max(0.4f, DungeonFormulas.Scale(dexterity, 0.5f) + _flat[(int)StatType.ActionSpeed] / 100f + Effect(effects, StatusEffectKind.ActionSpeed) / 100f);

            // Edges: geometric mean of two neighbours on the ring.
            _physicalHealing = Mathf.Max(0.3f, DungeonFormulas.Scale(DungeonFormulas.Edge(strength, vitality), 0.6f));
            _magicalHealing = Mathf.Max(0.3f, DungeonFormulas.Scale(DungeonFormulas.Edge(vitality, spirit), 0.6f));
            _magicalInteractionSpeed = Mathf.Max(0.4f, DungeonFormulas.Scale(DungeonFormulas.Edge(spirit, knowledge), 1.5f));
            _perception = Mathf.Max(0.3f, DungeonFormulas.Scale(DungeonFormulas.Edge(knowledge, agility), 1f));
            _interactionSpeed = Mathf.Max(0.4f, DungeonFormulas.Scale(DungeonFormulas.Edge(agility, dexterity), 1.5f));
            _handlingSpeed = Mathf.Max(0.4f, DungeonFormulas.Scale(DungeonFormulas.Edge(dexterity, strength), 1f));

            // Gear only.
            _cooldownSpeed = Mathf.Max(0.4f, 1f + _flat[(int)StatType.CooldownRecovery] / 100f);
            _armorRating = armor + _flat[(int)StatType.ArmorRating];
            _magicResistance = DungeonFormulas.BaseMagicResistance + magicResistance + _flat[(int)StatType.MagicResistance];
            _physicalReduction = DungeonFormulas.ArmorReduction(_armorRating) + rage * -0.01f;
            _magicalReduction = DungeonFormulas.MagicReduction(_magicResistance);

            float Attribute(StatType stat) => config.BaseStats.Get(stat) + _flat[(int)stat];
            float ThresholdPower(StatType attribute) => HasThreshold(attribute) ? DungeonFormulas.ThresholdPower : 0f;
        }

        /// Debuffs last as long as they were cast, only Agility 30 ignores slows.
        public float GetDurationScale(StatusEffectKind kind)
        {
            return kind == StatusEffectKind.Slow && HasThreshold(StatType.Agility) ? 0f : 1f;
        }

        /// Altars answer to magic; doors, levers, portals and the rest to the hands.
        public float GetInteractionSpeed(bool isMagical)
        {
            return isMagical ? _magicalInteractionSpeed : _interactionSpeed;
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

        /// Weapons, shields and belt items only count while they are in the hands; everything else is worn.
        private static bool IsInUse(EquipSlot slot, int activeWeaponSet, int heldBeltSlot)
        {
            return slot switch
            {
                EquipSlot.Weapon1Main or EquipSlot.Weapon1Off => activeWeaponSet == 0,
                EquipSlot.Weapon2Main or EquipSlot.Weapon2Off => activeWeaponSet == 1,
                >= EquipSlot.Utility1 and <= EquipSlot.Utility6 => (int)slot == heldBeltSlot,
                _ => true
            };
        }

        private void Apply(in StatModifier modifier)
        {
            _flat[(int)modifier.Stat] += modifier.Value;
        }
    }
}
