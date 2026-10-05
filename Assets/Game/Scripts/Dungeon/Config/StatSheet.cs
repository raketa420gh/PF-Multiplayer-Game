using System.Globalization;
using System.Text;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Everything the character sheet shows. The first six are the hexagram attributes in ring order, the next six
    /// the edge stats: edge i joins attribute i and its next neighbour on the ring.
    public enum StatId : byte
    {
        Strength,
        Vitality,
        Spirit,
        Knowledge,
        Agility,
        Dexterity,
        PhysicalHealing,
        MagicalHealing,
        MagicalInteraction,
        Perception,
        InteractionSpeed,
        Handling,
        PhysicalPower,
        PhysicalDamage,
        Health,
        MagicalPower,
        MagicalDamage,
        CastSpeed,
        BonusCharges,
        MoveSpeed,
        ActionSpeed,
        ArmorRating,
        PhysicalReduction,
        MagicResistance,
        MagicalReduction,
        CooldownSpeed
    }

    /// Names, values and hover texts of the character sheet.
    public static class StatSheet
    {
        public const int AttributeCount = 6;
        private const string Good = "c8e632";
        private const string Bad = "e04a3a";
        private const string Plain = "f2ede0";
        private const string Dim = "9a927f";
        /// How far apart on the colour wheel the first and the last derived stat of one attribute stand.
        private const float HueSpread = 0.09f;

        private static readonly string[] s_names =
        {
            "Strength", "Vitality", "Spirit", "Knowledge", "Agility", "Dexterity",
            "Physical Healing", "Magical Healing", "Magical Interaction", "Perception", "Interaction Speed", "Handling",
            "Physical Power", "Physical Damage", "Health", "Magical Power", "Magical Damage",
            "Cast Speed", "Spell Memory", "Move Speed", "Action Speed",
            "Armor Rating", "Physical Reduction", "Magic Resistance", "Magical Reduction", "Cooldown Recovery"
        };

        private static readonly string[] s_descriptions =
        {
            "Power of the arms: how hard weapons hit.",
            "Health: how much you take before going down.",
            "Power of the mind: how hard spells hit and how much they heal.",
            "Learning: how fast spells are cast and how many charges they hold.",
            "Speed of the legs. The only attribute that makes you run faster.",
            "Speed of the hands: attacks, blocks, bow draws, drinking. Does not make you run faster.",
            "Multiplies healing from bandages, surgical kits, campfires and resting.",
            "Multiplies healing from potions and spells.",
            "Speed of praying at altars.",
            "How fast you discover items in containers and corpses.",
            "Speed of opening doors, pulling levers and activating portals.",
            "Speed of swapping weapons, reloading, bandaging and using utility items.",
            "The only source of the physical damage bonus. Strength plus flat bonuses from gear and perks.",
            "Physical Power run through the shared curve, plus Physical Damage bonuses of gear.",
            "Damage you can take before dying.",
            "The only source of the magical damage bonus; half as much it raises the healing you cast. Spirit plus flat bonuses.",
            "Magical Power run through the shared curve, plus Magical Damage bonuses of gear.",
            "Speed of casting spells and spell-like skills.",
            "Extra charges of every spell that uses charges: +1 at 20, 30 and 40 Knowledge.",
            "Base 300, capped at 330. Armor and weapon penalties are subtracted as they are.",
            "Pace of attacks, blocks, bow draws and drinking potions, animations included.",
            "Sum of the armor worn. Converts into Physical Reduction with diminishing returns.",
            "Share of incoming physical damage removed. Capped at 65%.",
            "Base 30 plus gear. Converts into Magical Reduction with diminishing returns.",
            "Share of incoming magical damage removed. Capped at 65%.",
            "Skill cooldowns and spell recovery by the fire run this much faster. Comes from gear and perks only."
        };

        private static readonly string[] s_thresholds =
        {
            "+5 Physical Power",
            "resting heals twice as fast",
            "+5 Magical Power",
            "the first spell of a run and after a campfire rest costs no charge",
            "slows do not affect you",
            "locked doors open without a lockpick"
        };

        private static readonly Color[] s_colors =
        {
            new(0.93f, 0.6f, 0.26f), new(0.88f, 0.33f, 0.28f), new(0.72f, 0.46f, 0.92f),
            new(0.38f, 0.62f, 0.96f), new(0.32f, 0.8f, 0.62f), new(0.82f, 0.84f, 0.32f)
        };

        /// Armor Rating, Physical Reduction, Magic Resistance, Magical Reduction, Cooldown Recovery.
        private static readonly Color[] s_gearColors =
        {
            new(0.8f, 0.82f, 0.88f), new(0.62f, 0.66f, 0.78f), new(0.5f, 0.8f, 0.96f), new(0.36f, 0.6f, 0.9f), new(0.62f, 0.9f, 0.86f)
        };

        private static readonly StatId[][] s_ownStats =
        {
            new[] { StatId.PhysicalPower, StatId.PhysicalDamage },
            new[] { StatId.Health },
            new[] { StatId.MagicalPower, StatId.MagicalDamage },
            new[] { StatId.CastSpeed, StatId.BonusCharges },
            new[] { StatId.MoveSpeed },
            new[] { StatId.ActionSpeed }
        };

        /// A flat stat and the share it turns into through a curve.
        private static readonly (StatId source, StatId result)[] s_links =
        {
            (StatId.PhysicalPower, StatId.PhysicalDamage), (StatId.MagicalPower, StatId.MagicalDamage),
            (StatId.ArmorRating, StatId.PhysicalReduction), (StatId.MagicResistance, StatId.MagicalReduction)
        };

        private static readonly StringBuilder s_builder = new();

        public static string Name(StatId stat) => s_names[(int)stat];
        public static bool IsAttribute(StatId stat) => stat < StatId.PhysicalHealing;
        public static bool IsEdge(StatId stat) => stat >= StatId.PhysicalHealing && stat < StatId.PhysicalPower;
        public static StatId[] OwnStats(StatId attribute) => s_ownStats[(int)attribute];
        /// Edge stat between the attribute with this index and the next one on the ring.
        public static StatId Edge(int attribute) => (StatId)((int)StatId.PhysicalHealing + attribute);

        /// Share stat that this flat stat turns into: Physical Power gives Physical Damage, Armor Rating Physical Reduction and so on.
        public static bool TryGetResult(StatId source, out StatId result)
        {
            foreach ((StatId linkSource, StatId linkResult) in s_links)
            {
                if (linkSource == source)
                {
                    result = linkResult;

                    return true;
                }
            }

            result = source;

            return false;
        }

        /// Attributes have their own colour, an edge blends its two ends, a derived stat gets a shade of its source's colour
        /// that no sibling shares, the stats of the gear have colours of their own.
        public static Color ColorOf(StatId stat)
        {
            if (IsAttribute(stat))
                return s_colors[(int)stat];

            if (IsEdge(stat))
                return Color.Lerp(s_colors[stat - StatId.PhysicalHealing], s_colors[(stat - StatId.PhysicalHealing + 1) % AttributeCount], 0.5f);

            int source = Source(stat);

            if (source < 0)
                return s_gearColors[stat - StatId.ArmorRating];

            StatId[] siblings = s_ownStats[source];
            float spread = siblings.Length > 1 ? System.Array.IndexOf(siblings, stat) / (siblings.Length - 1f) - 0.5f : 0f;
            Color.RGBToHSV(s_colors[source], out float hue, out float saturation, out float value);

            return Color.HSVToRGB(Mathf.Repeat(hue + spread * HueSpread, 1f), Mathf.Clamp01(saturation - spread * 0.3f), Mathf.Clamp01(value + spread * 0.12f));
        }

        /// Rich text value: bonuses are green, penalties red, neutral numbers plain.
        public static string Format(AdventurerStats stats, StatId stat)
        {
            string text = Value(stats, stat, out float sign);

            return $"<color=#{(sign > 0f ? Good : sign < 0f ? Bad : Plain)}>{text}</color>";
        }

        public static string Describe(AdventurerStats stats, StatId stat)
        {
            s_builder.Clear();
            s_builder.Append("<b>").Append(Tint(Name(stat), ColorOf(stat))).Append("</b>   ").AppendLine(Format(stats, stat));
            s_builder.Append("<size=88%>").AppendLine(s_descriptions[(int)stat]);

            if (IsAttribute(stat))
            {
                int index = (int)stat;
                int previous = (index + AttributeCount - 1) % AttributeCount;
                s_builder.AppendLine();

                foreach (StatId own in s_ownStats[index])
                    Line(stats, own, null);

                Line(stats, Edge(index), $"with {s_names[(index + 1) % AttributeCount]}");
                Line(stats, Edge(previous), $"with {s_names[previous]}");
                bool isReached = stats.Attributes.Get((StatType)index) >= DungeonFormulas.Threshold;
                s_builder.AppendLine().Append("<color=#").Append(isReached ? Good : Dim).Append('>').Append("At ").Append(DungeonFormulas.Threshold)
                    .Append(": ").Append(s_thresholds[index]).Append(".</color>");
            }
            else if (IsEdge(stat))
            {
                StatType first = (StatType)(stat - StatId.PhysicalHealing);
                StatType second = (StatType)(((int)first + 1) % AttributeCount);
                int a = stats.Attributes.Get(first);
                int b = stats.Attributes.Get(second);
                s_builder.AppendLine().Append("Edge stat: ").Append(Tint(s_names[(int)first], s_colors[(int)first])).Append(" x ")
                    .Append(Tint(s_names[(int)second], s_colors[(int)second])).AppendLine().Append("<color=#").Append(Dim).Append(">sqrt(").Append(a)
                    .Append(" x ").Append(b).Append(") = ").Append(DungeonFormulas.Edge(a, b).ToString("0.#", CultureInfo.InvariantCulture))
                    .Append(". Neglecting either attribute drags it down.</color>");
            }
            else if (TryGetSource(stat, out StatId flat))
            {
                s_builder.AppendLine().Append("<color=#").Append(Dim).Append(">From ").Append(Name(flat)).Append(" (").Append(Value(stats, flat, out _))
                    .Append(").</color>");
            }
            else
            {
                if (TryGetResult(stat, out StatId result))
                    Line(stats, result, "results from it");

                int source = Source(stat);
                s_builder.AppendLine().Append("<color=#").Append(Dim).Append('>')
                    .Append(source >= 0 ? $"From {s_names[source]} ({stats.Attributes.Get((StatType)source)})." : "From gear, perks and effects.").Append("</color>");
            }

            return s_builder.Append("</size>").ToString();
        }

        private static void Line(AdventurerStats stats, StatId stat, string note)
        {
            s_builder.Append("  ").Append(Name(stat)).Append("  ").Append(Format(stats, stat));

            if (note != null)
                s_builder.Append("  <color=#").Append(Dim).Append('>').Append(note).Append("</color>");

            s_builder.AppendLine();
        }

        private static string Tint(string text, Color color)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
        }

        private static bool TryGetSource(StatId result, out StatId source)
        {
            foreach ((StatId linkSource, StatId linkResult) in s_links)
            {
                if (linkResult == result)
                {
                    source = linkSource;

                    return true;
                }
            }

            source = result;

            return false;
        }

        /// Attribute a derived stat grows from, -1 for the ones that come from gear alone.
        private static int Source(StatId stat)
        {
            for (int i = 0; i < s_ownStats.Length; i++)
            {
                if (System.Array.IndexOf(s_ownStats[i], stat) >= 0)
                    return i;
            }

            return -1;
        }

        private static string Value(AdventurerStats stats, StatId stat, out float sign)
        {
            sign = 0f;

            switch (stat)
            {
                case StatId.PhysicalHealing: return Bonus(stats.PhysicalHealing, out sign);
                case StatId.MagicalHealing: return Bonus(stats.MagicalHealing, out sign);
                case StatId.MagicalInteraction: return Bonus(stats.MagicalInteractionSpeed, out sign);
                case StatId.Perception: return Bonus(stats.Perception, out sign);
                case StatId.InteractionSpeed: return Bonus(stats.InteractionSpeed, out sign);
                case StatId.Handling: return Bonus(stats.HandlingSpeed, out sign);
                case StatId.PhysicalPower: return stats.PhysicalPower.ToString("0");
                case StatId.PhysicalDamage: return Bonus(stats.GetDamageMultiplier(DamageType.Physical), out sign);
                case StatId.Health: return stats.MaxHealth.ToString();
                case StatId.MagicalPower: return stats.MagicalPower.ToString("0");
                case StatId.MagicalDamage: return Bonus(stats.GetDamageMultiplier(DamageType.Magical), out sign);
                case StatId.CastSpeed: return Bonus(stats.CastSpeed, out sign);
                case StatId.BonusCharges:
                    sign = stats.BonusCharges;

                    return $"+{stats.BonusCharges}";
                case StatId.CooldownSpeed: return Bonus(stats.CooldownSpeed, out sign);
                case StatId.MoveSpeed:
                    sign = Mathf.Round(stats.MoveSpeedRating - DungeonFormulas.BaseMoveSpeed);

                    return stats.MoveSpeedRating.ToString("0");
                case StatId.ActionSpeed: return Bonus(stats.ActionSpeed, out sign);
                case StatId.ArmorRating: return stats.ArmorRating.ToString("0");
                case StatId.PhysicalReduction: return Share(stats.PhysicalReduction, out sign);
                case StatId.MagicResistance: return stats.MagicResistance.ToString("0");
                case StatId.MagicalReduction: return Share(stats.MagicalReduction, out sign);
                default: return stats.Attributes.Get((StatType)stat).ToString();
            }
        }

        private static string Bonus(float multiplier, out float sign)
        {
            sign = Mathf.Round((multiplier - 1f) * 100f);

            return $"{sign:+0;-0;0}%";
        }

        private static string Share(float fraction, out float sign)
        {
            sign = Mathf.Round(fraction * 100f);

            return $"{sign:0}%";
        }
    }
}
