using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Dungeon
{
    /// Page tabs and the sell counter under the tavern stash: a click on a tab opens that page, an item dropped on a tab moves to
    /// that page, an item dropped on the counter is sold for coins that land in the stash.
    public sealed class StashPagesView : MonoBehaviour
    {
        [SerializeField]
        private InventoryView _inventory;

        [SerializeField]
        private Button[] _tabs;

        [SerializeField]
        private ItemDropZone _sellZone;

        [SerializeField]
        private TMP_Text _sellText;

        [SerializeField]
        private TMP_Text _coinsText;

        [SerializeField]
        private Color _activeTab = new(0.6f, 0.45f, 0.15f);

        [SerializeField]
        private Color _idleTab = new(0.16f, 0.14f, 0.12f);

        private const string SellHint = "Drop an item here to sell it";

        private PlayerSessionComponent _session;

        private void Awake()
        {
            for (int i = 0; i < _tabs.Length; i++)
            {
                int page = i;
                _tabs[i].onClick.AddListener(() => OnTabClicked(page));
                _tabs[i].GetComponent<ItemDropZone>().OnDropped += item => MoveToPage(item, page);
            }

            _sellZone.OnPreview += OnSellPreview;
            _sellZone.OnDropped += Sell;
            _sellText.text = SellHint;
        }

        private void Update()
        {
            if (_session != null && _session.Object != null && _session.Object.IsValid)
                _coinsText.text = $"{_session.Coins} gold";
        }

        public void Bind(PlayerSessionComponent session)
        {
            _session = session;

            if (session != null)
                Select(0);
        }

        private void Select(int page)
        {
            _inventory.SetOther(_session.Stashes[page], $"Stash · page {page + 1}");

            for (int i = 0; i < _tabs.Length; i++)
                _tabs[i].image.color = i == page ? _activeTab : _idleTab;
        }

        private void MoveToPage(ItemView item, int page)
        {
            InventoryComponent target = _session.Stashes[page];

            if (item.Inventory == target)
                return;

            for (int y = 0; y < target.Height; y++)
            {
                for (int x = 0; x < target.Width; x++)
                {
                    if (!target.CanPlace(item.Config, x, y))
                        continue;

                    if (item.IsEquipped)
                        _session.Actions.RpcUnequip(item.Slot, target.Id, x, y);
                    else
                        _session.Actions.RpcMove(item.Inventory.Id, item.BagIndex, target.Id, x, y);

                    return;
                }
            }
        }

        private void Sell(ItemView item)
        {
            _sellText.text = SellHint;

            if (!_session.CanSell(item.Stack))
                return;

            DungeonAudioComponent.PlayUi(DungeonSound.Click, 0.5f);

            if (item.IsEquipped)
                _session.RpcSellEquipped(item.Slot);
            else
                _session.RpcSell(item.Inventory.Id, item.BagIndex);
        }

        private void OnTabClicked(int page)
        {
            DungeonAudioComponent.PlayUi(DungeonSound.Click, 0.4f);
            Select(page);
        }

        private void OnSellPreview(ItemView item)
        {
            _sellText.text = item == null ? SellHint
                : _session.CanSell(item.Stack) ? $"Sell for <color=#fd6>{DungeonFormulas.SellPrice(item.Config, item.Stack)}g</color>"
                : "<color=#f66>Cannot be sold</color>";
        }
    }
}
