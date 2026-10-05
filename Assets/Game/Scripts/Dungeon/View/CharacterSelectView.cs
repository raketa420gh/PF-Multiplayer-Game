using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Dungeon
{
    /// Character select laid out like Dark and Darker: the account's slots on the left, the picked character in the middle,
    /// Enter and Delete below. An empty slot opens the character creation.
    public sealed class CharacterSelectView : DisplayableView
    {
        public event Action<int> OnCreateRequested;
        public event Action<int> OnEnterRequested;

        [SerializeField]
        private ClassConfig[] _classes;

        [SerializeField]
        private CharacterPreviewView _preview;

        [SerializeField, Tooltip("One per character slot")]
        private Button[] _slots;

        [SerializeField]
        private TMP_Text[] _slotTexts;

        [SerializeField]
        private TMP_Text _nameText;

        [SerializeField]
        private Button _enterButton;

        [SerializeField]
        private Button _deleteButton;

        private static readonly Color s_idleSlot = new(0.05f, 0.04f, 0.035f, 0.96f);
        private static readonly Color s_pickedSlot = new(0.22f, 0.17f, 0.1f, 0.98f);
        private int _selected = -1;
        private bool _isDeleteArmed;

        private void Awake()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                int slot = i;
                _slots[i].onClick.AddListener(() => OnSlotClicked(slot));
            }

            _enterButton.onClick.AddListener(Enter);
            _deleteButton.onClick.AddListener(Delete);
        }

        private void OnEnable()
        {
            Select(StashService.HasCharacter(StashService.Slot) ? StashService.Slot : FirstCharacter());
        }

        private void Select(int slot)
        {
            _selected = slot;
            _isDeleteArmed = false;
            bool hasCharacter = slot >= 0;

            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].image.color = i == slot ? s_pickedSlot : s_idleSlot;
                _slotTexts[i].text = StashService.HasCharacter(i)
                    ? $"<color=#c8e632>Lv. {StashService.LoadLevel(i)}</color>  {FindClass(StashService.LoadClass(i)).DisplayName}"
                    : "<color=#6f6a5f>+  Create Character</color>";
            }

            _enterButton.interactable = hasCharacter;
            _deleteButton.interactable = hasCharacter;
            _deleteButton.GetComponentInChildren<TMP_Text>().text = "Delete";
            _preview.SetCharacterShown(hasCharacter);

            if (!hasCharacter)
            {
                _nameText.text = "Create a character to enter the tavern";

                return;
            }

            ClassConfig config = FindClass(StashService.LoadClass(slot));
            _preview.Bind(null, config);
            _nameText.text = $"<color=#c8e632>Lv. {StashService.LoadLevel(slot)}</color> {config.DisplayName}";
        }

        private int FirstCharacter()
        {
            for (int slot = 0; slot < _slots.Length; slot++)
            {
                if (StashService.HasCharacter(slot))
                    return slot;
            }

            return -1;
        }

        private ClassConfig FindClass(int id)
        {
            return Array.Find(_classes, config => config.Id == id) ?? _classes[0];
        }

        private void Enter()
        {
            Click();

            if (_selected >= 0)
                OnEnterRequested?.Invoke(_selected);
        }

        /// Deleting takes a second click on the same button, like the confirmation of the original.
        private void Delete()
        {
            Click();

            if (_selected < 0)
                return;

            if (!_isDeleteArmed)
            {
                _isDeleteArmed = true;
                _deleteButton.GetComponentInChildren<TMP_Text>().text = "<color=#e05040>Confirm</color>";

                return;
            }

            StashService.DeleteCharacter(_selected);
            Select(FirstCharacter());
        }

        private static void Click()
        {
            DungeonAudioComponent.PlayUi(DungeonSound.Click, 0.5f);
        }

        private void OnSlotClicked(int slot)
        {
            Click();

            if (StashService.HasCharacter(slot))
                Select(slot);
            else
                OnCreateRequested?.Invoke(slot);
        }
    }
}
