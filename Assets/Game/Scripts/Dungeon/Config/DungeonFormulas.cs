using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Piecewise-linear attribute curves taken from the Dark and Darker wiki.
    public static class DungeonFormulas
    {
        public const float BaseMoveSpeed = 300f;
        public const float MaxMoveSpeed = 330f;
        public const float FlatHealthBonus = 25f;
        public const float MaxDamageReduction = 0.65f;

        private static readonly Vector2[] s_powerBonus =
        {
            new(0f, -0.8f), new(5f, -0.3f), new(7f, -0.2f), new(11f, -0.08f), new(15f, 0f), new(50f, 0.35f), new(60f, 0.4f), new(100f, 0.5f)
        };

        private static readonly Vector2[] s_moveSpeedAdd =
        {
            new(0f, -10f), new(10f, -5f), new(15f, 0f), new(75f, 36f), new(100f, 43.5f)
        };

        private static readonly Vector2[] s_actionSpeed =
        {
            new(0f, -0.38f), new(10f, -0.08f), new(13f, -0.02f), new(15f, 0f), new(33f, 0.225f), new(45f, 0.345f), new(49f, 0.375f), new(100f, 0.63f)
        };

        private static readonly Vector2[] s_interactionSpeed =
        {
            new(0f, -0.26f), new(7f, -0.12f), new(15f, 0f), new(20f, 0.28f), new(25f, 0.52f), new(30f, 0.72f), new(35f, 0.88f), new(40f, 1f), new(45f, 1.08f), new(100f, 1.52f)
        };

        private static readonly Vector2[] s_castSpeed =
        {
            new(0f, -0.6f), new(5f, -0.35f), new(10f, -0.15f), new(15f, 0f), new(25f, 0.21f), new(40f, 0.51f), new(100f, 1.11f)
        };

        private static readonly Vector2[] s_baseHealth =
        {
            new(0f, 70f), new(15f, 100f), new(21f, 110.5f), new(44f, 145f), new(48f, 150f), new(64f, 166f), new(100f, 184f)
        };

        private static readonly Vector2[] s_armorReduction =
        {
            new(-12f, -0.388f), new(-4f, -0.268f), new(6f, -0.148f), new(12f, -0.07f), new(20f, 0.042f), new(75f, 0.1245f), new(100f, 0.161f),
            new(200f, 0.321f), new(300f, 0.431f), new(400f, 0.516f), new(500f, 0.618f), new(600f, 0.65f)
        };

        private static readonly Vector2[] s_magicResistance =
        {
            new(0f, -20f), new(5f, 0f), new(15f, 30f), new(33f, 102f), new(48f, 147f), new(58f, 167f), new(100f, 209f)
        };

        private static readonly Vector2[] s_magicReduction =
        {
            new(-15f, -0.25f), new(8f, -0.02f), new(18f, 0.03f), new(33f, 0.09f), new(53f, 0.15f), new(85f, 0.23f), new(280f, 0.62f), new(430f, 0.65f)
        };

        private static readonly Vector2[] s_buffDuration =
        {
            new(0f, -0.8f), new(15f, 0f), new(100f, 0.6f)
        };

        public static float PowerBonus(float power) => Sample(s_powerBonus, power);
        public static float MoveSpeedAdd(float agility) => Sample(s_moveSpeedAdd, agility);
        public static float ActionSpeed(float agility, float dexterity) => Sample(s_actionSpeed, agility * 0.25f + dexterity * 0.75f);
        public static float InteractionSpeed(float dexterity, float resourcefulness) => Sample(s_interactionSpeed, dexterity * 0.25f + resourcefulness * 0.75f);
        public static float CastSpeed(float knowledge) => Sample(s_castSpeed, knowledge);
        public static float BaseHealth(float strength, float vigor) => Sample(s_baseHealth, strength * 0.25f + vigor * 0.75f);
        public static float ArmorReduction(float armorRating) => Mathf.Min(Sample(s_armorReduction, armorRating), MaxDamageReduction);
        public static float MagicResistance(float will) => Sample(s_magicResistance, will);
        public static float MagicReduction(float magicResistance) => Mathf.Min(Sample(s_magicReduction, magicResistance), MaxDamageReduction);
        public static float BuffDuration(float will) => Sample(s_buffDuration, will);
        public static int MemoryCapacity(float knowledge) => Mathf.Max(0, Mathf.RoundToInt(knowledge) - 6);

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
