using System;
using UnityEngine;

namespace Game.Scripts.Battle
{
    /// Scene-level service locator for networked prefabs that need scene references.
    public sealed class BattleContext : MonoBehaviour
    {
        public static BattleContext Instance => s_instance;

        public event Action<FighterComponent> OnLocalFighterChanged;

        public Camera Camera => _camera;
        public BattleInputPolling Input => _input;
        public BattleFeedback Feedback => _feedback;
        public FighterComponent LocalFighter => _localFighter;

        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private BattleInputPolling _input;

        [SerializeField]
        private BattleFeedback _feedback;

        private static BattleContext s_instance;
        private FighterComponent _localFighter;

        private void Awake()
        {
            s_instance = this;
        }

        private void OnDestroy()
        {
            if (s_instance == this)
                s_instance = null;
        }

        public void SetLocalFighter(FighterComponent fighter)
        {
            _localFighter = fighter;

            if (fighter != null)
                _input.SetLook(new Vector2(0f, fighter.transform.eulerAngles.y));

            OnLocalFighterChanged?.Invoke(fighter);
        }
    }
}
