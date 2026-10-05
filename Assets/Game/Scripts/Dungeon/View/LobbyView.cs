using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Dungeon
{
    /// Tavern screen: the top tabs switch between the lobby, the perks and skills page, the stash with the kit and the merchants.
    public sealed class LobbyView : DisplayableView
    {
        [SerializeField]
        private LobbyHomeView _home;

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
        private Button[] _tabs;

        [SerializeField, Tooltip("Indexed like the tabs")]
        private DisplayableView[] _pages;

        [SerializeField]
        private TMP_Text _rankText;

        private static readonly Color s_idleTab = new(0.62f, 0.58f, 0.5f);
        private readonly AdventurerStats _previewStats = new();
        private PlayerSessionComponent _session;
        private int _shownClass = -1;

        private void Awake()
        {
            for (int i = 0; i < _tabs.Length; i++)
            {
                int index = i;
                _tabs[i].onClick.AddListener(() => { DungeonAudioComponent.PlayUi(DungeonSound.Click, 0.5f); SelectTab(index); });
            }
        }

        private void OnEnable()
        {
            SelectTab(0);
        }

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
            _home.Bind(session);
            _skills.Bind(session, _previewStats);
            _merchants.Bind(session);
            _stashPages.Bind(session);
        }

        private void SelectTab(int index)
        {
            for (int i = 0; i < _tabs.Length; i++)
            {
                _pages[i].SetShown(i == index);
                _tabs[i].image.color = i == index ? Color.white : Color.clear;
                _tabs[i].GetComponentInChildren<TMP_Text>().color = i == index ? Color.white : s_idleTab;
            }
        }
    }
}
