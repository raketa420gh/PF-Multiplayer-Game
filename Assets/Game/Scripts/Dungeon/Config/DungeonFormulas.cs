using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Hexagram curves: every attribute runs through one shared curve, edge stats use the geometric mean of two neighbours.
    public static class DungeonFormulas
    {
        public const float BaseMoveSpeed = 300f;
        public const float MaxMoveSpeed = 330f;
        public const float BaseHealth = 125f;
        public const float BasePoise = 12f;
        public const float BaseConcentration = 10f;
        public const float BaseMagicResistance = 30f;
        public const float MaxDamageReduction = 0.65f;
        public const int Threshold = 30;

        /// 15 is neutral; 3% per point below 15 and up to 25, 2% up to 35, then 1%.
        private static readonly Vector2[] s_attribute =
        {
            new(0f, -0.45f), new(15f, 0f), new(25f, 0.3f), new(35f, 0.5f), new(100f, 1.15f)
        };

        private static readonly Vector2[] s_armorReduction =
        {
            new(-12f, -0.388f), new(-4f, -0.268f), new(6f, -0.148f), new(12f, -0.07f), new(20f, 0.042f), new(75f, 0.1245f), new(100f, 0.161f),
            new(200f, 0.321f), new(300f, 0.431f), new(400f, 0.516f), new(500f, 0.618f), new(600f, 0.65f)
        };

        private static readonly Vector2[] s_magicReduction =
        {
            new(-15f, -0.25f), new(8f, -0.02f), new(18f, 0.03f), new(33f, 0.09f), new(53f, 0.15f), new(85f, 0.23f), new(280f, 0.62f), new(430f, 0.65f)
        };

        public static float Curve(float value) => Sample(s_attribute, value);
        public static float Scale(float value, float weight) => 1f + weight * Curve(value);
        public static float Edge(float a, float b) => Mathf.Sqrt(Mathf.Max(0f, a) * Mathf.Max(0f, b));
        public static float PowerBonus(float power) => Curve(power);
        public static int BonusCharges(float resonance) => Mathf.Clamp(Mathf.FloorToInt((resonance - 10f) / 10f), 0, 3);
        /// Seconds to discover one unsearched item at neutral Perception: rarer loot takes longer to make out.
        public static float SearchTime(ItemRarity rarity) => 0.5f + 0.25f * (int)rarity;
        public static float ArmorReduction(float armorRating) => Mathf.Min(Sample(s_armorReduction, armorRating), MaxDamageReduction);
        public static float MagicReduction(float magicResistance) => Mathf.Min(Sample(s_magicReduction, magicResistance), MaxDamageReduction);

        public static float Sample(Vector2[] curve, float x)
        {
            if (x <= curve[0].x)
                return curve[0].y + (x - curve[0].x) * Slope(curve[0], curve[1]);

            for (int i = 1; i < curve.Length; i++)
            {
                if (x <= curve[i].x)
                    return Mathf.Lerp(curve[i - 1].y, curve[i].y, Mathf.InverseLerp(curve[i - 1].x, curve[i].x, x));
            }

            return curve[^1].y;
        }

        private static float Slope(Vector2 a, Vector2 b)
        {
            return (b.y - a.y) / (b.x - a.x);
        }
    }
}
