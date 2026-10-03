using UnityEngine;

namespace Game.Scripts.Battle
{
    /// Crosshair hint: while attacking, an arrow shows which way the current swing travels.
    public sealed class SwingHintView : MonoBehaviour
    {
        [SerializeField]
        private BattleContext _context;

        [SerializeField]
        private HudPanelView _arrow;

        [SerializeField]
        private HudPanelView _thrust;

        [SerializeField, Range(0f, 1f), Tooltip("Swings whose screen-plane share of motion is below this are shown as thrusts")]
        private float _thrustThreshold = 0.4f;

        private void Update()
        {
            FighterComponent fighter = _context.LocalFighter;
            Vector2 direction = Vector2.zero;
            bool isAttacking = fighter != null && fighter.Object != null && fighter.Object.IsValid &&
                               fighter.Combat.State == CombatState.Attack;

            if (isAttacking)
                direction = fighter.Combat.Attack.GetSwingDirection(fighter.Combat.Weapon.IsMirrored);

            bool isThrust = isAttacking && direction.magnitude < _thrustThreshold;
            _arrow.SetShown(isAttacking && !isThrust);
            _thrust.SetShown(isThrust);

            if (_arrow.IsShown)
                _arrow.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }
    }
}
