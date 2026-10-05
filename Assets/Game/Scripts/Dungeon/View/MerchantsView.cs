using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Dungeon
{
    /// "Merchants" page of the tavern: the merchants on the left, the counter of the chosen one in the middle,
    /// the purse and the details of the hovered ware on the right. A click buys the ware into the stash.
    public sealed class MerchantsView : DisplayableView
    {
        [SerializeField]
        private Button _merchantButtonPrefab;

        [SerializeField]
        private RectTransform _merchantsRoot;

        [SerializeField]
        private MerchantWareView _warePrefab;

        [SerializeField]
        private RectTransform _waresRoot;

        [SerializeField, Tooltip("All, then one per ItemKind")]
        private Button[] _filterButtons;

        [SerializeField]
        private TMP_Text _titleText;

        [SerializeField]
        private TMP_Text _descriptionText;

        [SerializeField]
        private TMP_Text _coinsText;

        [SerializeField]
        private Image _coinsIcon;

        [SerializeField]
        private Image _detailsIcon;

        [SerializeField]
        private TMP_Text _detailsText;

        [SerializeField]
        private TMP_Text _statusText;

        [SerializeField]
        private Color _selectedColor = new(1f, 0.86f, 0.55f);

        [SerializeField]
        private Color _idleColor = new(0.62f, 0.58f, 0.5f);

        [SerializeField]
        private Color _selectedPlate = new(0.34f, 0.26f, 0.13f, 0.97f);

        [SerializeField]
        private Color _idlePlate = new(0.12f, 0.1f, 0.08f, 0.95f);

        private const string DetailsHint = "<color=#9a927f>Hover a ware to inspect it.\nClick to buy it into the stash.</color>";
        private readonly List<Button> _merchantButtons = new();
        private readonly List<MerchantWareView> _wares = new();
        private PlayerSessionComponent _session;
        private bool _isBuilt;
        private int _merchant;
        private int _filter;

        private void Awake()
        {
            for (int i = 0; i < _filterButtons.Length; i++)
            {
                int index = i;
                _filterButtons[i].onClick.AddListener(() => { Click(); SelectFilter(index); });
            }

            ShowDetails(null);
        }

        private void Update()
        {
            if (_session == null || _session.Object == null || !_session.Object.IsValid)
                return;

            if (!_isBuilt)
            {
                _isBuilt = true;
                BuildMerchants();
            }

            _coinsText.text = $"<color=#ffd98c>{_session.Coins}</color> <size=70%>gold</size>";
        }

        public void Bind(PlayerSessionComponent session)
        {
            _session = session;
            _isBuilt = false;
        }

        public override void Hide()
        {
            base.Hide();
            ShowDetails(null);
            _statusText.text = string.Empty;
        }

        private void BuildMerchants()
        {
            foreach (Button button in _merchantButtons)
                Destroy(button.gameObject);

            _merchantButtons.Clear();
            MerchantConfig[] merchants = _session.Merchants;
            _coinsIcon.sprite = _session.Currency.Icon;

            for (int i = 0; i < merchants.Length; i++)
            {
                int index = i;
                Button button = Instantiate(_merchantButtonPrefab, _merchantsRoot);
                button.gameObject.SetActive(true);
                button.GetComponentInChildren<TMP_Text>().text = $"<b>{merchants[i].DisplayName}</b>\n<size=70%><color=#9a927f>{merchants[i].Wares.Length} wares · {merchants[i].Price} gold each</color></size>";
                button.onClick.AddListener(() => { Click(); SelectMerchant(index); });
                _merchantButtons.Add(button);
            }

            if (merchants.Length > 0)
                SelectMerchant(0);
        }

        private void SelectMerchant(int index)
        {
            MerchantConfig merchant = _session.Merchants[index];
            _merchant = index;
            _titleText.text = merchant.DisplayName;
            _titleText.color = merchant.Color;
            _descriptionText.text = merchant.Description;

            for (int i = 0; i < _merchantButtons.Count; i++)
                _merchantButtons[i].image.color = i == index ? _selectedPlate : _idlePlate;

            SelectFilter(0);
        }

        /// 0 shows everything, the others one item kind each.
        private void SelectFilter(int filter)
        {
            _filter = filter;

            for (int i = 0; i < _filterButtons.Length; i++)
                _filterButtons[i].GetComponentInChildren<TMP_Text>().color = i == filter ? _selectedColor : _idleColor;

            foreach (MerchantWareView ware in _wares)
                Destroy(ware.gameObject);

            _wares.Clear();
            MerchantConfig merchant = _session.Merchants[_merchant];
            Color frame = _session.Stash.Database.GetRarityColor(merchant.Rarity);

            foreach (ItemConfig item in merchant.Wares)
            {
                if (filter > 0 && (int)item.Kind != filter - 1)
                    continue;

                MerchantWareView ware = Instantiate(_warePrefab, _waresRoot);
                ware.gameObject.SetActive(true);
                ware.Set(item, frame, merchant.Price);
                ware.OnClicked += OnWareClicked;
                ware.OnHovered += OnWareHovered;
                _wares.Add(ware);
            }

            _waresRoot.anchoredPosition = Vector2.zero;
        }

        private void ShowDetails(ItemConfig item)
        {
            _detailsIcon.enabled = item != null && item.Icon != null;

            if (item == null)
            {
                _detailsText.text = DetailsHint;

                return;
            }

            MerchantConfig merchant = _session.Merchants[_merchant];
            _detailsIcon.sprite = item.Icon;
            _detailsText.text = ItemTooltip.Build(_session.Stash.Database, item, ItemStack.Create(item, item.MaxStack, merchant.Rarity)) +
                $"\n\n<color=#ffd98c>Price: {merchant.Price} gold</color>{(item.MaxStack > 1 ? $"  <size=80%>for a stack of {item.MaxStack}</size>" : string.Empty)}";
        }

        private void SetStatus(string text, bool isGood)
        {
            _statusText.text = text;
            _statusText.color = isGood ? new Color(0.78f, 0.9f, 0.2f) : new Color(0.88f, 0.29f, 0.23f);
        }

        private static void Click()
        {
            DungeonAudioComponent.PlayUi(DungeonSound.Click, 0.5f);
        }

        private void OnWareClicked(MerchantWareView ware)
        {
            MerchantConfig merchant = _session.Merchants[_merchant];
            Click();

            if (_session.Coins < merchant.Price)
            {
                SetStatus($"Not enough gold for {ware.Item.DisplayName}", false);

                return;
            }

            if (!_session.HasRoomInStash(ware.Item))
            {
                SetStatus("The stash is full", false);

                return;
            }

            _session.RpcBuy((byte)_merchant, ware.Item.Id);
            SetStatus($"Bought {ware.Item.DisplayName}: look in the stash", true);
        }

        private void OnWareHovered(MerchantWareView ware, bool isHovered)
        {
            ShowDetails(isHovered ? ware.Item : null);
        }
    }
}
