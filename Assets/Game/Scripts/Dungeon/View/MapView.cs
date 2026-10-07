using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Dungeon
{
    /// Full map of the current floor (M), Dark and Darker style: the whole floor, module names, the player's arrow and the portals
    /// that have shown up (blue escapes, red ways down).
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

        [Tooltip("Side of every floor and the number of modules along it")]
        [SerializeField]
        private float[] _floorSizes;

        [SerializeField]
        private int[] _floorGrids;

        [SerializeField, Tooltip("Template of a portal mark, cloned per portal")]
        private Image _portalMarker;

        [SerializeField]
        private Color _escapeColor = new(0.35f, 0.65f, 1f);

        [SerializeField]
        private Color _descendColor = new(1f, 0.3f, 0.2f);

        private readonly List<Image> _markers = new();
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
            float world = _floorSizes[floor];
            _arrow.anchoredPosition = new Vector2(position.x / world * size.x, position.z / world * size.y);
            _arrow.localRotation = Quaternion.Euler(0f, 0f, -adventurer.transform.eulerAngles.y);
            MarkPortals(floor, size, world);
        }

        private void MarkPortals(int floor, Vector2 size, float world)
        {
            int count = 0;
            DungeonDirector director = _context.Director;

            if (director != null && floor < director.Floors.Count)
            {
                DungeonDirector.FloorLayout layout = director.Floors[floor];

                foreach (PortalComponent portal in layout.EscapePortals)
                    count = Mark(portal, count, size, world);

                foreach (PortalComponent portal in layout.DescendPortals)
                    count = Mark(portal, count, size, world);
            }

            for (int i = count; i < _markers.Count; i++)
                _markers[i].gameObject.SetActive(false);
        }

        private int Mark(PortalComponent portal, int index, Vector2 size, float world)
        {
            if (portal == null || portal.Object == null || !portal.Object.IsValid || !portal.IsActive)
                return index;

            if (index == _markers.Count)
                _markers.Add(Instantiate(_portalMarker, _portalMarker.transform.parent));

            Image marker = _markers[index];
            Vector3 position = portal.transform.position;
            marker.gameObject.SetActive(true);
            marker.color = portal.Kind == PortalKind.Escape ? _escapeColor : _descendColor;
            marker.rectTransform.anchoredPosition = new Vector2(position.x / world * size.x, position.z / world * size.y);

            return index + 1;
        }

        private void ShowFloor(int floor)
        {
            _shownFloor = floor;
            _map.texture = _floorMaps[floor];
            _title.text = $"Floor {floor + 1}";

            int grid = _floorGrids[floor];
            Vector2 module = _map.rectTransform.rect.size / grid;
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
