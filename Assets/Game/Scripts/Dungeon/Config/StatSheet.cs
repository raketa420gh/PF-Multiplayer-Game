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
        Flesh,
        Grip,
        Reflex,
        Craft,
        Insight,
        Resonance,
        Toughness,
        MoveSpeed,
        Handling,
        Perception,
        CastSpeed,
        Mending,
        Health,
        PhysicalPower,
        PhysicalDamage,
        ActionSpeed,
        InteractionSpeed,
        Weakpoint,
        CooldownSpeed,
        ControlResistance,
        MagicalPower,
        MagicalDamage,
        BonusCharges,
        ArmorRating,
        PhysicalReduction,
        MagicResistance,
        MagicalReduction
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
            "Flesh", "Grip", "Reflex", "Craft", "Insight", "Resonance",
            "Toughness", "Move Speed", "Handling", "Perception", "Cast Speed", "Mending",
            "Health", "Physical Power", "Physical Damage", "Action Speed",
            "Interaction Speed", "Weakpoint", "Cooldown Recovery", "Control Resist", "Magical Power", "Magical Damage",
            "Spell Charges", "Armor Rating", "Physical Reduction", "Magic Resistance", "Magical Reduction"
        };

        private static readonly string[] s_descriptions =
        {
            "Body mass: how much punishment you take before going down and how fast bleeding wears off.",
            "Strength of the arms: how hard you hit.",
            "Speed of the nerves: how fast you swing, block, draw and drink.",
            "Skill of the hands: opening, picking, searching and hitting where it hurts.",
            "Clarity of the mind: skills come back sooner, slows wear off faster.",
            "Bond with magic: spell power, healing power and extra spell charges.",
            "Bleeding and burning on you wear off this much sooner and deal as much less damage.",
            "Base 300, capped at 330. Armor and weapon penalties are subtracted as they are.",
            "Speed of swapping weapons, reloading, bandaging and using utility items.",
            "How loud the footsteps of others are to you and how fast you discover items in containers and corpses.",
            "Speed of casting spells and spell-like skills.",
            "Multiplies incoming healing: spells, potions, bandages and the regeneration while resting.",
            "Damage you can take before dying.",
            "Raises physical damage along the shared attribute curve. Grip plus flat bonuses from gear and perks.",
            "Total multiplier of the physical damage you deal.",
            "Pace of attacks, blocks, bow draws and drinking potions, animations included.",
            "Speed of opening doors and chests, pulling levers and activating portals.",
            "Multiplies the damage of your hits to the head.",
            "Skill cooldowns run this much faster.",
            "Slows applied to you wear off this much sooner.",
            "Raises magical damage along the shared attribute curve and half as much the healing you cast. Resonance plus flat bonuses.",
            "Total multiplier of the magical damage you deal.",
            "Extra charges of every spell that uses charges: +1 at 20, 30 and 40 Resonance.",
            "Sum of the armor worn. Converts into Physical Reduction with diminishing returns.",
            "Share of incoming physical damage removed. Capped at 65%.",
            "Base 30 plus gear. Converts into Magical Reduction with diminishing returns.",
            "Share of incoming magical damage removed. Capped at 65%."
        };

        private static readonly string[] s_thresholds =
        {
            "resting heals twice as fast",
            "+10% physical damage",
            "+20% Action Speed for 2s after being staggered",
            "locked doors open without a lockpick",
            "slows do not affect you",
            "the first spell of a run and after a campfire rest costs no charge"
        };

        private static readonly Color[] s_colors =
        {
            new(0.88f, 0.33f, 0.28f), new(0.93f, 0.6f, 0.26f), new(0.82f, 0.84f, 0.32f),
            new(0.32f, 0.8f, 0.62f), new(0.38f, 0.62f, 0.96f), new(0.72f, 0.46f, 0.92f)
        };

        /// Armor Rating, Physical Reduction, Magic Resistance, Magical Reduction.
        private static readonly Color[] s_gearColors =
        {
            new(0.78f, 0.8f, 0.86f), new(0.86f, 0.7f, 0.5f), new(0.5f, 0.78f, 0.95f), new(0.72f, 0.62f, 0.96f)
        };

        private static readonly StatId[][] s_ownStats =
        {
            new[] { StatId.Health },
            new[] { StatId.PhysicalPower, StatId.PhysicalDamage },
            new[] { StatId.ActionSpeed },
            new[] { StatId.InteractionSpeed, StatId.Weakpoint },
            new[] { StatId.CooldownSpeed, StatId.ControlResistance },
            new[] { StatId.MagicalPower, StatId.MagicalDamage, StatId.BonusCharges }
        };

        private static readonly StringBuilder s_builder = new();

        public static string Name(StatId stat) => s_names[(int)stat];
        public static bool IsAttribute(StatId stat) => stat < StatId.Toughness;
        public static bool IsEdge(StatId stat) => stat >= StatId.Toughness && stat < StatId.Health;
        public static StatId[] OwnStats(StatId attribute) => s_ownStats[(int)attribute];
        /// Edge stat between the attribute with this index and the next one on the ring.
        public static StatId Edge(int attribute) => (StatId)((int)StatId.Toughness + attribute);

        /// Attributes have their own colour, an edge blends its two ends, a derived stat gets a shade of its source's colour
        /// that no sibling shares, the stats of the gear have colours of their own.
        public static Color ColorOf(StatId stat)
        {
            if (IsAttribute(stat))
                return s_colors[(int)stat];

            if (IsEdge(stat))
                return Color.Lerp(s_colors[stat - StatId.Toughness], s_colors[(stat - StatId.Toughness + 1) % AttributeCount], 0.5f);

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
                StatType first = (StatType)(stat - StatId.Toughness);
                StatType second = (StatType)(((int)first + 1) % AttributeCount);
                int a = stats.Attributes.Get(first);
                int b = stats.Attributes.Get(second);
                s_builder.AppendLine().Append("Edge stat: ").Append(Tint(s_names[(int)first], s_colors[(int)first])).Append(" x ")
                    .Append(Tint(s_names[(int)second], s_colors[(int)second])).AppendLine().Append("<color=#").Append(Dim).Append(">sqrt(").Append(a)
                    .Append(" x ").Append(b).Append(") = ").Append(DungeonFormulas.Edge(a, b).ToString("0.#", CultureInfo.InvariantCulture))
                    .Append(". Neglecting either attribute drags it down.</color>");
            }
            else
            {
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
                case StatId.Toughness: return Bonus(stats.Toughness, out sign);
                case StatId.MoveSpeed:
                    sign = Mathf.Round(stats.MoveSpeedRating - DungeonFormulas.BaseMoveSpeed);

                    return stats.MoveSpeedRating.ToString("0");
                case StatId.Handling: return Bonus(stats.HandlingSpeed, out sign);
                case StatId.Perception: return Bonus(stats.Perception, out sign);
                case StatId.CastSpeed: return Bonus(stats.CastSpeed, out sign);
                case StatId.Mending: return Bonus(stats.Mending, out sign);
                case StatId.Health: return stats.MaxHealth.ToString();
                case StatId.PhysicalPower: return stats.PhysicalPower.ToString("0");
                case StatId.PhysicalDamage: return Bonus(stats.GetDamageMultiplier(DamageType.Physical), out sign);
                case StatId.ActionSpeed: return Bonus(stats.ActionSpeed, out sign);
                case StatId.InteractionSpeed: return Bonus(stats.InteractionSpeed, out sign);
                case StatId.Weakpoint: return Bonus(stats.Weakpoint, out sign);
                case StatId.CooldownSpeed: return Bonus(stats.CooldownSpeed, out sign);
                case StatId.ControlResistance: return Share(stats.ControlResistance, out sign);
                case StatId.MagicalPower: return stats.MagicalPower.ToString("0");
                case StatId.MagicalDamage: return Bonus(stats.GetDamageMultiplier(DamageType.Magical), out sign);
                case StatId.BonusCharges:
                    sign = stats.BonusCharges;

                    return $"+{stats.BonusCharges}";
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
