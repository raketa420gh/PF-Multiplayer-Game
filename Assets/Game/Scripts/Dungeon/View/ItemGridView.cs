using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Dungeon
{
    /// Renders one inventory bag as a cell grid with item views placed over it.
    public sealed class ItemGridView : MonoBehaviour
    {
        public InventoryComponent Inventory => _inventory;
        public float CellSize => _cellSize;
        public RectTransform Rect => (RectTransform)transform;

        [SerializeField]
        private RectTransform _cellsRoot;

        [SerializeField]
        private RectTransform _itemsRoot;

        [SerializeField]
        private ItemView _itemPrefab;

        [SerializeField]
        private Image _cellPrefab;

        [SerializeField]
        private float _cellSize = 48f;

        private static readonly Color s_cellColor = new(0.16f, 0.14f, 0.11f, 0.85f);
        private readonly List<ItemView> _items = new();
        private readonly List<Image> _cells = new();
        private InventoryView _owner;
        private InventoryComponent _inventory;
        private int _shownVersion = -1;
        private int _builtWidth = -1;
        private int _builtHeight = -1;

        public void Bind(InventoryView owner, InventoryComponent inventory)
        {
            if (_inventory != null)
                _inventory.OnChanged -= Refresh;

            _owner = owner;
            _inventory = inventory;
            _shownVersion = -1;

            if (_inventory != null)
                _inventory.OnChanged += Refresh;

            gameObject.SetActive(inventory != null);
            Refresh();
        }

        private void OnDestroy()
        {
            if (_inventory != null)
                _inventory.OnChanged -= Refresh;
        }

        public void Refresh()
        {
            if (_inventory == null || _inventory.Object == null || !_inventory.Object.IsValid)
                return;

            BuildCells();
            _shownVersion = _inventory.Version;

            foreach (ItemView view in _items)
                view.gameObject.SetActive(false);

            int used = 0;

            for (int i = 0; i < InventoryComponent.Capacity; i++)
            {
                ItemStack stack = _inventory.Bag[i];
                ItemConfig config = _inventory.GetConfig(stack);

                if (config == null)
                    continue;

                ItemView view = GetView(used++);
                view.gameObject.SetActive(true);
                view.Bind(_owner, _inventory, stack, config, i, EquipSlot.Count, _cellSize);
            }
        }

        public bool TryGetCell(Vector2 screenPosition, Camera camera, out int x, out int y)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_cellsRoot, screenPosition, camera, out Vector2 local);
            x = Mathf.FloorToInt(local.x / _cellSize);
            y = Mathf.FloorToInt(-local.y / _cellSize);

            return x >= 0 && y >= 0 && x < _inventory.Width && y < _inventory.Height;
        }

        public void Highlight(int x, int y, int width, int height, bool isValid)
        {
            for (int i = 0; i < _cells.Count; i++)
            {
                int cx = i % _inventory.Width;
                int cy = i / _inventory.Width;
                bool inside = cx >= x && cx < x + width && cy >= y && cy < y + height;
                _cells[i].color = inside ? (isValid ? new Color(0.3f, 0.7f, 0.3f, 0.6f) : new Color(0.8f, 0.2f, 0.2f, 0.6f)) : s_cellColor;
            }
        }

        public void ClearHighlight()
        {
            foreach (Image cell in _cells)
                cell.color = s_cellColor;
        }

        private void BuildCells()
        {
            if (_builtWidth == _inventory.Width && _builtHeight == _inventory.Height)
                return;

            foreach (Image cell in _cells)
                Destroy(cell.gameObject);

            _cells.Clear();
            _builtWidth = _inventory.Width;
            _builtHeight = _inventory.Height;

            for (int y = 0; y < _builtHeight; y++)
            {
                for (int x = 0; x < _builtWidth; x++)
                {
                    Image cell = Instantiate(_cellPrefab, _cellsRoot);
                    RectTransform rect = cell.rectTransform;
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
                    rect.sizeDelta = new Vector2(_cellSize - 2f, _cellSize - 2f);
                    rect.anchoredPosition = new Vector2(x * _cellSize + 1f, -y * _cellSize - 1f);
                    cell.gameObject.SetActive(true);
                    _cells.Add(cell);
                }
            }

            Rect.sizeDelta = new Vector2(_builtWidth * _cellSize, _builtHeight * _cellSize);
            ClearHighlight();
        }

        private ItemView GetView(int index)
        {
            while (_items.Count <= index)
            {
                ItemView view = Instantiate(_itemPrefab, _itemsRoot);
                RectTransform rect = (RectTransform)view.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
                _items.Add(view);
            }

            return _items[index];
        }
    }
}
