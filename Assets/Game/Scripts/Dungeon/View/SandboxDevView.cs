using Game.Scripts.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Dungeon
{
    /// Developer buttons of the test ground, shown while the inventory frees the cursor. Host only.
    public sealed class SandboxDevView : DisplayableView
    {
        [SerializeField]
        private SandboxDirector _director;

        [Tooltip("Indexed like the director's monster prefabs")]
        [SerializeField]
        private Button[] _monsterButtons;

        [SerializeField]
        private Button _botButton;

        [SerializeField]
        private Button _clearButton;

        [SerializeField]
        private Button _restockButton;

        [SerializeField]
        private Button _lobbyButton;

        [SerializeField]
        private Button _debugButton;

        private TMP_Text _debugLabel;

        private void Awake()
        {
            _debugLabel = _debugButton.GetComponentInChildren<TMP_Text>();
            _debugButton.onClick.AddListener(BattleDebugSettings.Toggle);
            for (int i = 0; i < _monsterButtons.Length; i++)
            {
                int index = i;
                _monsterButtons[i].onClick.AddListener(() => _director.SpawnMonster(index));
            }

            _botButton.onClick.AddListener(_director.SpawnBot);
            _clearButton.onClick.AddListener(_director.ClearMobs);
            _restockButton.onClick.AddListener(_director.Restock);
            _lobbyButton.onClick.AddListener(_director.ReturnToLobby);
        }

        private void OnEnable()
        {
            BattleDebugSettings.OnChanged += OnDebugChanged;
            OnDebugChanged(BattleDebugSettings.IsEnabled);
        }

        private void OnDisable()
        {
            BattleDebugSettings.OnChanged -= OnDebugChanged;
        }

        private void OnDebugChanged(bool isEnabled)
        {
            _debugLabel.text = isEnabled ? "Hitboxes: ON (F3)" : "Hitboxes: OFF (F3)";
        }
    }
}
