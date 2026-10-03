using System.Text;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Rich text card of an item stack: name in the rarity colour, kind specific numbers, modifiers and value.
    public static class ItemTooltip
    {
        private static readonly StringBuilder s_builder = new();
        private static readonly StatModifier[] s_affixes = new StatModifier[ItemAffixes.MaxCount];

        public static string Build(ItemDatabase database, ItemConfig config, ItemStack stack)
        {
            if (stack.IsHidden)
                return "<b>Unknown item</b>\n<i><size=80%>Not searched yet. Keep the container open to discover it.</size></i>";

            s_builder.Clear();
            Color color = database.GetRarityColor(stack.RarityValue);
            s_builder.AppendLine($"<color=#{ColorUtility.ToHtmlStringRGB(color)}><b>{config.DisplayName}</b></color>  <size=80%>{stack.RarityValue} {config.Kind}</size>");
            int tier = Mathf.Max(0, stack.Rarity - (int)ItemRarity.Common);

            switch (config)
            {
                case WeaponItemConfig weapon:
                    int damage = weapon.Weapon != null && weapon.Weapon.Attacks.Length > 0 ? weapon.Weapon.Attacks[0].Damage : weapon.Weapon != null ? weapon.Weapon.Ranged.MaxDamage : 0;
                    s_builder.AppendLine($"{(weapon.IsTwoHanded ? "Two-handed" : weapon.IsOffHand ? "Off-hand" : "One-handed")} {weapon.WeaponClass}");

                    if (damage > 0)
                        s_builder.AppendLine($"Damage {damage + tier}  ({weapon.DamageType})");

                    if (weapon.MoveSpeedPenalty > 0f)
                        s_builder.AppendLine($"Move Speed -{weapon.MoveSpeedPenalty:0}");

                    if (weapon.LightRange > 0f)
                        s_builder.AppendLine("Light source");
                    break;
                case ArmorItemConfig armor:
                    s_builder.AppendLine($"{armor.ArmorType} {armor.Slot}");
                    s_builder.AppendLine($"Armor Rating {armor.ArmorRating + tier * 2f:0}");

                    if (armor.MagicResistance != 0f)
                        s_builder.AppendLine($"Magic Resistance {armor.MagicResistance:+0;-0}");

                    if (armor.MoveSpeedPenalty != 0f)
                        s_builder.AppendLine($"Move Speed {armor.MoveSpeedPenalty:+0;-0}");
                    break;
                case ConsumableItemConfig consumable:
                    s_builder.AppendLine(consumable.Effect switch
                    {
                        ConsumableEffect.HealInstant => $"Heals {consumable.Magnitude + tier * 4f:0} after {consumable.UseTime:0.#}s",
                        ConsumableEffect.HealOverTime => $"Heals {consumable.Magnitude:0} over {Mathf.Max(1f, consumable.Duration - tier * 2.5f):0.#}s",
                        ConsumableEffect.Protection => $"Absorbs {consumable.Magnitude + tier * 5f:0} damage for {consumable.Duration:0}s",
                        ConsumableEffect.Invisibility => $"Invisible for {consumable.Duration + tier * 2f:0}s",
                        _ => $"+{consumable.Magnitude:0} move speed for {consumable.Duration:0}s"
                    });
                    break;
                case UtilityItemConfig utility:
                    s_builder.AppendLine(utility.UtilityKind.ToString());

                    if (utility.Damage > 0)
                        s_builder.AppendLine($"Thrown damage {utility.Damage}");
                    break;
            }

            foreach (StatModifier modifier in config.Modifiers)
                s_builder.AppendLine($"<color=#8fd>{ItemAffixes.Describe(modifier)}</color>");

            int affixCount = ItemAffixes.Roll(config, stack, s_affixes);

            for (int i = 0; i < affixCount; i++)
                s_builder.AppendLine($"<color=#6cf>{ItemAffixes.Describe(s_affixes[i])}</color>");

            if (!string.IsNullOrEmpty(config.Description))
                s_builder.AppendLine($"<i><size=80%>{config.Description}</size></i>");

            s_builder.Append($"<size=80%>Value {config.Value}g   {config.Width}x{config.Height}</size>");

            return s_builder.ToString();
        }
    }
}
