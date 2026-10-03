using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Derived character numbers: class attributes + gear + perks + buffs run through the wiki curves.
    public sealed class AdventurerStats : ICombatStats
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
        public float InteractionSpeed => _interactionSpeed;
        public float CastSpeed => _castSpeed;
        public float BuffDurationMultiplier => _buffDuration;

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
        private float _interactionSpeed = 1f;
        private float _castSpeed = 1f;
        private float _buffDuration = 1f;
        private readonly float[] _flat = new float[(int)StatType.Count];

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
            _attributes = config.BaseStats;
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

            float strength = _attributes.Strength + _flat[(int)StatType.Strength] + Effect(effects, StatusEffectKind.Strength) + Effect(effects, StatusEffectKind.Rage);
            float vigor = _attributes.Vigor + _flat[(int)StatType.Vigor];
            float agility = _attributes.Agility + _flat[(int)StatType.Agility];
            float dexterity = _attributes.Dexterity + _flat[(int)StatType.Dexterity];
            float will = _attributes.Will + _flat[(int)StatType.Will];
            float knowledge = _attributes.Knowledge + _flat[(int)StatType.Knowledge];
            float resourcefulness = _attributes.Resourcefulness + _flat[(int)StatType.Resourcefulness];

            _attributes = new ClassStats(Mathf.RoundToInt(strength), Mathf.RoundToInt(vigor), Mathf.RoundToInt(agility),
                Mathf.RoundToInt(dexterity), Mathf.RoundToInt(will), Mathf.RoundToInt(knowledge), Mathf.RoundToInt(resourcefulness));

            float healthBonus = _flat[(int)StatType.MaxHealth] + Effect(effects, StatusEffectKind.Fortify) + FormHealthBonus(form);
            _maxHealth = Mathf.CeilToInt((DungeonFormulas.BaseHealth(strength, vigor) + DungeonFormulas.FlatHealthBonus) * (1f + healthBonus / 100f));
            _armorRating = armor + _flat[(int)StatType.ArmorRating];
            _magicResistance = DungeonFormulas.MagicResistance(will) + magicResistance + _flat[(int)StatType.MagicResistance];
            _physicalReduction = DungeonFormulas.ArmorReduction(_armorRating) + Effect(effects, StatusEffectKind.Rage) * -0.01f;
            _magicalReduction = DungeonFormulas.MagicReduction(_magicResistance);
            _physicalPower = strength + _flat[(int)StatType.PhysicalPower] + Effect(effects, StatusEffectKind.Power);
            _magicalPower = will + _flat[(int)StatType.MagicalPower];

            float haste = Effect(effects, StatusEffectKind.Haste) - Effect(effects, StatusEffectKind.Slow);
            float rating = DungeonFormulas.BaseMoveSpeed + DungeonFormulas.MoveSpeedAdd(agility) + moveAdd + _flat[(int)StatType.MoveSpeed] + FormMoveAdd(form);
            rating *= 1f + (haste + Effect(effects, StatusEffectKind.Rage) * 0.7f) / 100f;
            _moveSpeedRating = Mathf.Min(rating, DungeonFormulas.MaxMoveSpeed);
            _moveSpeedMultiplier = Mathf.Max(0.3f, _moveSpeedRating / DungeonFormulas.BaseMoveSpeed);

            _actionSpeed = Mathf.Max(0.4f, 1f + DungeonFormulas.ActionSpeed(agility, dexterity) + _flat[(int)StatType.ActionSpeed] / 100f + Effect(effects, StatusEffectKind.ActionSpeed) / 100f);
            _interactionSpeed = Mathf.Max(0.4f, 1f + DungeonFormulas.InteractionSpeed(dexterity, resourcefulness));
            _castSpeed = Mathf.Max(0.4f, 1f + DungeonFormulas.CastSpeed(knowledge));
            _buffDuration = Mathf.Max(0.2f, 1f + DungeonFormulas.BuffDuration(will));
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
