using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Dungeon
{
    /// Counter of one merchant of the hub, the one whose station the player stands at: its wares, the purse and the details
    /// of the hovered ware on the right. A click buys the ware into the stash.
    public sealed class MerchantsView : DisplayableView
    {
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

        private const string DetailsHint = "<color=#9a927f>Hover a ware to inspect it.\nClick to buy it into the stash.</color>";
        private readonly List<MerchantWareView> _wares = new();
        private PlayerSessionComponent _session;
        private bool _isBuilt;
        private int _opened;
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
                _coinsIcon.sprite = _session.Currency.Icon;
                SelectMerchant(_opened);
            }

            _coinsText.text = $"<color=#ffd98c>{_session.Coins}</color> <size=70%>gold</size>";
        }

        public void Bind(PlayerSessionComponent session)
        {
            _session = session;
            _isBuilt = false;
        }

        /// The counter of one merchant, picked by the station the player stands at.
        public void Open(int merchant)
        {
            _opened = merchant;

            if (_isBuilt)
                SelectMerchant(merchant);
        }

        public override void Hide()
        {
            base.Hide();
            ShowDetails(null);
            _statusText.text = string.Empty;
        }

        private void SelectMerchant(int index)
        {
            _merchant = Mathf.Clamp(index, 0, _session.Merchants.Length - 1);
            MerchantConfig merchant = _session.Merchants[_merchant];
            _titleText.text = merchant.DisplayName;
            _titleText.color = merchant.Color;
            _descriptionText.text = $"{merchant.Description}   <color=#ffd98c>{merchant.Price} gold each</color>";
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
