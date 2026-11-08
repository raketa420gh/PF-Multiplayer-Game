using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Dungeon
{
    /// Character creation, UI only: the class list on the left, the class hexagram in the middle, its description on the right,
    /// the nickname above the Create button.
    public sealed class CharacterCreateView : DisplayableView
    {
        public event Action<byte, string> OnCreated;
        public event Action OnCancelled;

        [SerializeField, Tooltip("Indexed like the class buttons")]
        private ClassConfig[] _classes;

        [SerializeField]
        private Button[] _classButtons;

        [SerializeField]
        private StatsView _stats;

        [SerializeField]
        private TMP_Text _nameText;

        [SerializeField]
        private TMP_Text _descriptionText;

        [SerializeField]
        private TMP_InputField _nicknameInput;

        [SerializeField]
        private Button _createButton;

        [SerializeField]
        private Button _backButton;

        private static readonly Color s_idleClass = new(0.05f, 0.04f, 0.035f, 0.96f);
        private static readonly Color s_pickedClass = new(0.22f, 0.17f, 0.1f, 0.98f);
        private readonly AdventurerStats _classStats = new();
        private int _selected;

        private string Nickname => _nicknameInput.text.Trim();

        private void Awake()
        {
            for (int i = 0; i < _classButtons.Length; i++)
            {
                int index = i;
                _classButtons[i].onClick.AddListener(() => { Click(); Select(index); });
                _classButtons[i].interactable = _classes[i].IsPlayable;
            }

            _nicknameInput.characterLimit = StashService.MaxNameLength;
            _nicknameInput.onValueChanged.AddListener(_ => RefreshCreate());
            _createButton.onClick.AddListener(() => { Click(); OnCreated?.Invoke(_classes[_selected].Id, Nickname); });
            _backButton.onClick.AddListener(() => { Click(); OnCancelled?.Invoke(); });
            _stats.Bind(_classStats);
        }

        private void OnEnable()
        {
            _nicknameInput.text = string.Empty;
            Select(Math.Max(0, Array.FindIndex(_classes, config => config.IsPlayable)));
            RefreshCreate();
        }

        private void Select(int index)
        {
            _selected = index;
            ClassConfig config = _classes[index];

            for (int i = 0; i < _classButtons.Length; i++)
                _classButtons[i].image.color = i == index ? s_pickedClass : s_idleClass;

            _classStats.Recalculate(config, null, null, -1, 0);
            _nameText.text = config.DisplayName;
            _descriptionText.text = config.Description;
        }

        private void RefreshCreate()
        {
            _createButton.interactable = Nickname.Length > 0;
        }

        private static void Click()
        {
            DungeonAudioComponent.PlayUi(DungeonSound.Click, 0.5f);
        }
    }
}
