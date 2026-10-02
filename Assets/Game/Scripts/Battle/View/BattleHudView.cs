using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Battle
{
    public sealed class BattleHudView : MonoBehaviour
    {
        [SerializeField]
        private BattleContext _context;

        [SerializeField]
        private Image _healthFill;

        [SerializeField]
        private TMP_Text _healthText;

        [SerializeField]
        private TMP_Text _weaponText;

        [SerializeField]
        private TMP_Text _stateText;

        [SerializeField]
        private TMP_Text _botModeText;

        [SerializeField]
        private Image _comboIndicator;

        [SerializeField]
        private HudPanelView _drawPanel;

        [SerializeField]
        private Image _drawFill;

        [SerializeField]
        private HudPanelView _deathPanel;

        [SerializeField]
        private TMP_Text _deathText;

        [SerializeField]
        private HudPanelView _helpPanel;

        [SerializeField]
        private Color _comboOpenColor = Color.green;

        [SerializeField]
        private Color _comboQueuedColor = Color.yellow;

        [SerializeField]
        private Color _comboClosedColor = new(1f, 1f, 1f, 0.15f);

        private int _shownHealth = -1;
        private int _shownBotMode = -1;
        private int _shownRespawn = -1;
        private string _shownState;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.H))
                _helpPanel.SetShown(!_helpPanel.IsShown);

            FighterComponent fighter = _context.LocalFighter;

            if (fighter == null || fighter.Object == null || !fighter.Object.IsValid)
                return;

            UpdateHealth(fighter);
            UpdateCombat(fighter.Combat);
            UpdateBotMode();
        }

        private void UpdateHealth(FighterComponent fighter)
        {
            HealthComponent health = fighter.Health;
            _deathPanel.SetShown(health.IsDead);

            if (health.IsDead)
            {
                int respawn = Mathf.CeilToInt(fighter.RespawnTimeLeft);

                if (respawn != _shownRespawn)
                {
                    _shownRespawn = respawn;
                    _deathText.text = $"YOU DIED\nrespawn in {respawn}";
                }
            }

            if (health.CurrentHealth == _shownHealth)
                return;

            _shownHealth = health.CurrentHealth;
            _healthFill.fillAmount = health.Progress;
            _healthText.text = $"{health.CurrentHealth}/{health.MaxHealth}";
        }

        private void UpdateCombat(CombatComponent combat)
        {
            WeaponConfig weapon = combat.Weapon;
            CombatState state = combat.State;
            AttackPhase phase = combat.Phase;

            string stateName = state == CombatState.Attack
                ? $"Attack {combat.AttackIndex + 1}/{weapon.Attacks.Length} · {phase}"
                : state.ToString();

            if (stateName != _shownState)
            {
                _shownState = stateName;
                _stateText.text = stateName;
                _weaponText.text = $"[{combat.WeaponSlot + 1}] {weapon.DisplayName}";
            }

            _comboIndicator.color = combat.IsComboQueued ? _comboQueuedColor
                : combat.IsComboWindowOpen ? _comboOpenColor
                : _comboClosedColor;

            _drawPanel.SetShown(state == CombatState.Draw);
            _drawFill.fillAmount = combat.DrawPower;
        }

        private void UpdateBotMode()
        {
            foreach (FighterComponent fighter in FighterComponent.All)
            {
                if (!fighter.TryGetComponent(out BotBrainComponent brain))
                    continue;

                if ((int)brain.Mode != _shownBotMode)
                {
                    _shownBotMode = (int)brain.Mode;
                    _botModeText.text = $"Bots: {brain.Mode}  [B]";
                }

                return;
            }
        }
    }
}
