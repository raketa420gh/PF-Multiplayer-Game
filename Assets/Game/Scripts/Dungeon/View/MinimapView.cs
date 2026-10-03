using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Dungeon
{
    /// Parchment minimap of the current floor around the player, with the module name underneath.
    public sealed class MinimapView : MonoBehaviour
    {
        [SerializeField]
        private DungeonContext _context;

        [SerializeField]
        private RawImage _map;

        [SerializeField]
        private RectTransform _arrow;

        [SerializeField]
        private TMP_Text _moduleText;

        [SerializeField]
        private Texture2D[] _floorMaps;

        [SerializeField]
        private string[] _floorModuleNames;

        [SerializeField]
        private float _worldSize = 42f;

        [SerializeField]
        private float _windowSize = 22f;

        [SerializeField]
        private float _moduleSize = 14f;

        private void Update()
        {
            AdventurerComponent adventurer = _context.LocalAdventurer;

            if (adventurer == null || adventurer.Object == null || !adventurer.Object.IsValid)
                return;

            int floor = Mathf.Clamp(adventurer.Floor - 1, 0, _floorMaps.Length - 1);

            if (_map.texture != _floorMaps[floor])
                _map.texture = _floorMaps[floor];

            Vector3 position = adventurer.transform.position;
            float u = (position.x + _worldSize * 0.5f) / _worldSize;
            float v = (position.z + _worldSize * 0.5f) / _worldSize;
            float window = _windowSize / _worldSize;
            _map.uvRect = new Rect(u - window * 0.5f, v - window * 0.5f, window, window);
            _arrow.localRotation = Quaternion.Euler(0f, 0f, -adventurer.transform.eulerAngles.y);

            int x = Mathf.Clamp(Mathf.FloorToInt((position.x + _worldSize * 0.5f) / _moduleSize), 0, 2);
            int z = Mathf.Clamp(Mathf.FloorToInt((position.z + _worldSize * 0.5f) / _moduleSize), 0, 2);
            int index = floor * 9 + z * 3 + x;
            _moduleText.text = index < _floorModuleNames.Length ? _floorModuleNames[index] : string.Empty;
        }
    }
}
