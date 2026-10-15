namespace Game.Scripts.Dungeon
{
    /// Status effects that scale every hit, shared by adventurers and monsters.
    public static class StatusDamage
    {
        /// Monsters have no attributes: a curse weakens their blows by this share per point.
        private const float CursePerPoint = 0.03f;

        /// Share of a hit the struck takes: vulnerable and marked bodies take more.
        public static float Taken(StatusEffectComponent effects)
        {
            return effects == null ? 1f : 1f + (effects.GetMagnitude(StatusEffectKind.Vulnerable) + effects.GetMagnitude(StatusEffectKind.Marked)) / 100f;
        }

        /// Share of a hit the striker deals: battle order adds, weakness takes; a cursed monster hits softer.
        public static float Dealt(StatusEffectComponent effects, bool isMonster)
        {
            if (effects == null)
                return 1f;

            float curse = isMonster ? effects.GetMagnitude(StatusEffectKind.Cursed) * CursePerPoint : 0f;

            return UnityEngine.Mathf.Max(0f, 1f + (effects.GetMagnitude(StatusEffectKind.DamageBonus) - effects.GetMagnitude(StatusEffectKind.Weakened)) / 100f - curse);
        }
    }
}
