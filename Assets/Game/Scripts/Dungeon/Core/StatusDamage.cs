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

        /// Share of a hit the striker deals: battle order adds, weakness takes (the physical one from physical hits only);
        /// a cursed monster hits softer.
        public static float Dealt(StatusEffectComponent effects, bool isMonster, bool isPhysical)
        {
            if (effects == null)
                return 1f;

            float curse = isMonster ? effects.GetMagnitude(StatusEffectKind.Cursed) * CursePerPoint : 0f;
            float weakness = effects.GetMagnitude(StatusEffectKind.Weakened) + (isPhysical ? effects.GetMagnitude(StatusEffectKind.PhysicalWeakness) : 0f);

            return UnityEngine.Mathf.Max(0f, 1f + (effects.GetMagnitude(StatusEffectKind.DamageBonus) - weakness) / 100f - curse);
        }
    }
}
