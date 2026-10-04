using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Dungeon
{
    /// Full map of the current floor (M), Dark and Darker style: the whole floor, module names and the player's arrow.
    public sealed class MapView : DisplayableView
    {
        public bool HasFloors => _floorMaps.Length > 0;

        [SerializeField]
        private DungeonContext _context;

        [SerializeField]
        private RawImage _map;

        [SerializeField]
        private RectTransform _arrow;

        [SerializeField]
        private TMP_Text _title;

        [Tooltip("Labels for the modules of a floor, row by row from the south-west corner; as many as the largest floor has")]
        [SerializeField]
        private TMP_Text[] _moduleLabels;

        [SerializeField]
        private Texture2D[] _floorMaps;

        [SerializeField]
        private string[] _floorModuleNames;

        [SerializeField]
        private float _worldSize = 42f;

        [Tooltip("Side of every floor and the number of modules along it")]
        [SerializeField]
        private float[] _floorSizes;

        [SerializeField]
        private int[] _floorGrids;

        private int _shownFloor = -1;

        private void Update()
        {
            AdventurerComponent adventurer = _context.LocalAdventurer;

            if (adventurer == null || adventurer.Object == null || !adventurer.Object.IsValid)
                return;

            int floor = Mathf.Clamp(adventurer.Floor - 1, 0, _floorMaps.Length - 1);

            if (floor != _shownFloor)
                ShowFloor(floor);

            Vector3 position = adventurer.transform.position;
            Vector2 size = _map.rectTransform.rect.size;
            _arrow.anchoredPosition = new Vector2(position.x / _worldSize * size.x, position.z / _worldSize * size.y);
            _arrow.localRotation = Quaternion.Euler(0f, 0f, -adventurer.transform.eulerAngles.y);
        }

        private void ShowFloor(int floor)
        {
            _shownFloor = floor;
            _map.texture = _floorMaps[floor];
            _title.text = $"Floor {floor + 1}";

            int grid = _floorGrids[floor];
            Vector2 module = _map.rectTransform.rect.size * (_floorSizes[floor] / grid / _worldSize);
            int first = 0;

            for (int i = 0; i < floor; i++)
                first += _floorGrids[i] * _floorGrids[i];

            for (int i = 0; i < _moduleLabels.Length; i++)
            {
                TMP_Text label = _moduleLabels[i];
                bool isUsed = i < grid * grid && first + i < _floorModuleNames.Length;
                label.gameObject.SetActive(isUsed);

                if (!isUsed)
                    continue;

                label.text = _floorModuleNames[first + i];
                label.rectTransform.sizeDelta = new Vector2(module.x - 8f, label.rectTransform.sizeDelta.y);
                label.rectTransform.anchoredPosition = new Vector2((i % grid + 0.5f - grid * 0.5f) * module.x, (i / grid + 0.5f - grid * 0.5f) * module.y);
            }
        }
    }
}
