using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Dungeon
{
    /// Parchment minimap of the current floor around the player with the Dark Swarm, the module name underneath.
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
        private SwarmZoneGraphic _swarm;

        [SerializeField]
        private Texture2D[] _floorMaps;

        [SerializeField]
        private string[] _floorModuleNames;

        [Tooltip("Side of every floor, the part of it the window shows and the number of modules along it")]
        [SerializeField]
        private float[] _floorSizes;

        [SerializeField]
        private float[] _windowSizes;

        [SerializeField]
        private int[] _floorGrids;

        private void Update()
        {
            AdventurerComponent adventurer = _context.LocalAdventurer;

            if (adventurer == null || adventurer.Object == null || !adventurer.Object.IsValid)
                return;

            int floor = Mathf.Clamp(adventurer.Floor - 1, 0, _floorMaps.Length - 1);

            CatacombGenerator catacombs = _context.Catacombs;
            bool isGenerated = catacombs != null && catacombs.Floor == adventurer.Floor && catacombs.Map != null;
            Texture map = isGenerated ? catacombs.Map : _floorMaps[floor];

            if (_map.texture != map)
                _map.texture = map;

            Vector3 position = adventurer.transform.position;
            float size = _floorSizes[floor];
            float u = (position.x + size * 0.5f) / size;
            float v = (position.z + size * 0.5f) / size;
            float window = _windowSizes[floor] / size;
            _map.uvRect = new Rect(u - window * 0.5f, v - window * 0.5f, window, window);
            _arrow.localRotation = Quaternion.Euler(0f, 0f, -adventurer.transform.eulerAngles.y);
            _swarm.Show(_context.Match, adventurer.Floor, position, _map.rectTransform.rect.width / _windowSizes[floor]);

            if (isGenerated)
            {
                _moduleText.text = catacombs.GetTitle(position);

                return;
            }

            int grid = _floorGrids[floor];
            float module = _floorSizes[floor] / grid;
            int x = Mathf.Clamp(Mathf.FloorToInt(position.x / module + grid * 0.5f), 0, grid - 1);
            int z = Mathf.Clamp(Mathf.FloorToInt(position.z / module + grid * 0.5f), 0, grid - 1);
            int index = z * grid + x;

            for (int i = 0; i < floor; i++)
                index += _floorGrids[i] * _floorGrids[i];

            _moduleText.text = index < _floorModuleNames.Length ? _floorModuleNames[index] : string.Empty;
        }
    }
}
