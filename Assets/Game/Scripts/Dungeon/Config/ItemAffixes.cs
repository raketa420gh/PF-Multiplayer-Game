using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Random modifiers of looted weapons and armor, as in Dark and Darker: one per rarity step above Common.
    /// They are not stored: the stack seed rolls the same set on every peer and after every load.
    public static class ItemAffixes
    {
        public const int MaxCount = 5;

        private readonly struct Affix
        {
            public readonly StatType Stat;
            public readonly float Min;
            public readonly float Step;
            public readonly int Steps;

            public Affix(StatType stat, float min, float step, int steps)
            {
                Stat = stat;
                Min = min;
                Step = step;
                Steps = steps;
            }
        }

        private static readonly Affix[] s_pool =
        {
            new(StatType.Strength, 1f, 1f, 2), new(StatType.Vitality, 1f, 1f, 2), new(StatType.Spirit, 1f, 1f, 2),
            new(StatType.Knowledge, 1f, 1f, 2), new(StatType.Agility, 1f, 1f, 2), new(StatType.Dexterity, 1f, 1f, 2),
            new(StatType.MaxHealth, 2f, 1f, 4), new(StatType.ArmorRating, 4f, 2f, 6), new(StatType.MagicResistance, 4f, 2f, 6),
            new(StatType.MoveSpeed, 1f, 1f, 4), new(StatType.PhysicalPower, 1f, 1f, 2), new(StatType.MagicalPower, 1f, 1f, 2),
            new(StatType.ActionSpeed, 1f, 1f, 3), new(StatType.PhysicalDamageBonus, 0.01f, 0.01f, 3), new(StatType.MagicalDamageBonus, 0.01f, 0.01f, 3)
        };

        public static int Count(ItemConfig config, in ItemStack stack)
        {
            if (stack.Seed == 0 || config == null || config.Kind is not (ItemKind.Weapon or ItemKind.Armor))
                return 0;

            return Mathf.Clamp(stack.Rarity - (int)ItemRarity.Common, 0, MaxCount);
        }

        /// Fills the buffer (MaxCount long) with the modifiers of the stack and returns how many there are.
        public static int Roll(ItemConfig config, in ItemStack stack, StatModifier[] buffer)
        {
            int count = Count(config, stack);
            uint state = stack.Seed * 2654435761u + (uint)stack.ItemId * 40503u;
            int used = 0;

            // Xorshift never leaves zero.
            if (state == 0u)
                state = 1u;

            for (int i = 0; i < count; i++)
            {
                int index;

                do
                {
                    index = (int)(Next(ref state) % (uint)s_pool.Length);
                }
                while ((used & (1 << index)) != 0);

                used |= 1 << index;
                Affix affix = s_pool[index];
                buffer[i] = new StatModifier(affix.Stat, affix.Min + Next(ref state) % (uint)(affix.Steps + 1) * affix.Step);
            }

            return count;
        }

        public static string Describe(in StatModifier modifier)
        {
            return modifier.Stat switch
            {
                StatType.MaxHealth => $"Max Health {modifier.Value:+0.#;-0.#}%",
                StatType.ArmorRating => $"Armor Rating {modifier.Value:+0.#;-0.#}",
                StatType.MagicResistance => $"Magic Resistance {modifier.Value:+0.#;-0.#}",
                StatType.MoveSpeed => $"Move Speed {modifier.Value:+0.#;-0.#}",
                StatType.PhysicalPower => $"Physical Power {modifier.Value:+0.#;-0.#}",
                StatType.MagicalPower => $"Magical Power {modifier.Value:+0.#;-0.#}",
                StatType.ActionSpeed => $"Action Speed {modifier.Value:+0.#;-0.#}%",
                StatType.PhysicalDamageBonus => $"Physical Damage {modifier.Value * 100f:+0.#;-0.#}%",
                StatType.MagicalDamageBonus => $"Magical Damage {modifier.Value * 100f:+0.#;-0.#}%",
                StatType.CooldownRecovery => $"Cooldown Recovery {modifier.Value:+0.#;-0.#}%",
                StatType.RangedDamageBonus => $"Ranged Damage {modifier.Value * 100f:+0.#;-0.#}%",
                StatType.ReloadSpeed => $"Reload Speed {modifier.Value:+0.#;-0.#}%",
                StatType.HeadshotDamage => $"Headshot Damage {modifier.Value * 100f:+0.#;-0.#}%",
                _ => $"{modifier.Stat} {modifier.Value:+0.#;-0.#}"
            };
        }

        private static uint Next(ref uint state)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;

            return state >> 8;
        }
    }
}
