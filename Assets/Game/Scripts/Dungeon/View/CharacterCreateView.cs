using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Dungeon
{
    /// Character creation: the class list on the left, the class body in the middle, its description and attributes on the right.
    public sealed class CharacterCreateView : DisplayableView
    {
        public event Action<byte> OnCreated;
        public event Action OnCancelled;

        [SerializeField, Tooltip("Indexed like the class buttons")]
        private ClassConfig[] _classes;

        [SerializeField]
        private CharacterPreviewView _preview;

        [SerializeField]
        private Button[] _classButtons;

        [SerializeField]
        private TMP_Text _nameText;

        [SerializeField]
        private TMP_Text _descriptionText;

        [SerializeField]
        private Button _createButton;

        [SerializeField]
        private Button _backButton;

        private static readonly Color s_idleClass = new(0.05f, 0.04f, 0.035f, 0.96f);
        private static readonly Color s_pickedClass = new(0.22f, 0.17f, 0.1f, 0.98f);
        private int _selected;

        private void Awake()
        {
            for (int i = 0; i < _classButtons.Length; i++)
            {
                int index = i;
                _classButtons[i].onClick.AddListener(() => { Click(); Select(index); });
            }

            _createButton.onClick.AddListener(() => { Click(); OnCreated?.Invoke(_classes[_selected].Id); });
            _backButton.onClick.AddListener(() => { Click(); OnCancelled?.Invoke(); });
        }

        private void OnEnable()
        {
            Select(0);
        }

        private void Select(int index)
        {
            _selected = index;
            ClassConfig config = _classes[index];

            for (int i = 0; i < _classButtons.Length; i++)
                _classButtons[i].image.color = i == index ? s_pickedClass : s_idleClass;

            _preview.SetCharacterShown(true);
            _preview.Bind(null, config);
            _nameText.text = config.DisplayName;
            ClassStats stats = config.BaseStats;
            _descriptionText.text = $"{config.Description}\n\n<color=#d9b873>Attributes</color>\n" +
                $"Flesh {stats.Flesh}  ·  Grip {stats.Grip}  ·  Reflex {stats.Reflex}\nCraft {stats.Craft}  ·  Insight {stats.Insight}  ·  Resonance {stats.Resonance}";
        }

        private static void Click()
        {
            DungeonAudioComponent.PlayUi(DungeonSound.Click, 0.5f);
        }
    }
}
