using TMPro;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Pages of the hub, each opened by its station: the party board and the book of descents, the perks and skills page (with
    /// the spell wheels), the kit with or without a stash, a merchant's counter.
    public sealed class HubPagesView : DisplayableView
    {
        [SerializeField]
        private HubBoardView _board;

        [SerializeField]
        private SkillsView _skills;

        [SerializeField]
        private InventoryView _inventory;

        [SerializeField]
        private CharacterPreviewView _inventoryPreview;

        [SerializeField]
        private MerchantsView _merchants;

        [SerializeField]
        private StashPagesView _stashPages;

        [SerializeField]
        private TMP_Text _titleText;

        [SerializeField]
        private TMP_Text _rankText;

        private readonly AdventurerStats _previewStats = new();
        private PlayerSessionComponent _session;
        private int _shownClass = -1;

        private void Update()
        {
            if (_session == null || _session.Object == null || !_session.Object.IsValid)
                return;

            ClassConfig config = _session.Class;

            if (_shownClass != config.Id)
            {
                _shownClass = config.Id;
                _inventory.Bind(_session.Kit, _session.Actions, _previewStats, $"{config.DisplayName} · Kit", false);
                _inventoryPreview.Bind(_session.Kit, config);
            }

            _previewStats.Recalculate(config, _session.Kit, null, 0, ClassConfig.PerkCountForLevel(_session.Level), ShapeshiftForm.None, _session.PerkMask);
            _rankText.text = $"Lv\n<size=170%>{_session.Level}</size>";
        }

        public void Bind(PlayerSessionComponent session)
        {
            _session = session;
            _shownClass = -1;
            _board.Bind(session);
            _skills.Bind(session, _previewStats);
            _merchants.Bind(session);
            _stashPages.Bind(session);
        }

        public void Open(HubStation station, int index, string title)
        {
            bool isBoard = station is HubStation.Descent or HubStation.Party;
            bool isMerchant = station == HubStation.Merchant;
            bool isSkills = station == HubStation.Skills;
            _titleText.text = title;
            _board.SetShown(isBoard);
            _skills.SetShown(isSkills);
            _merchants.SetShown(isMerchant);
            _inventory.SetShown(!isBoard && !isMerchant && !isSkills);

            if (isBoard)
                _board.SetMode(station == HubStation.Party);
            else if (isMerchant)
                _merchants.Open(index);
            else if (!isSkills)
                _stashPages.Open(station);
        }
    }
}
