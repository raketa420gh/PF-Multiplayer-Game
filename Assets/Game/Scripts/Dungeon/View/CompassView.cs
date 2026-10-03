using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Heading strip at the top of the screen, like Dark and Darker's compass.
    public sealed class CompassView : MonoBehaviour
    {
        [SerializeField]
        private DungeonContext _context;

        [SerializeField]
        private RectTransform _strip;

        [SerializeField]
        private TMP_Text _labelPrefab;

        [SerializeField]
        private float _pixelsPerDegree = 4.5f;

        [SerializeField]
        private float _visibleDegrees = 120f;

        private readonly List<(TMP_Text label, float angle)> _labels = new();

        private void Awake()
        {
            string[] names = { "N", "15", "30", "45", "60", "75", "E", "105", "120", "135", "150", "165", "S", "195", "210", "225", "240", "255", "W", "285", "300", "315", "330", "345" };

            for (int i = 0; i < names.Length; i++)
            {
                TMP_Text label = Instantiate(_labelPrefab, _strip);
                label.gameObject.SetActive(true);
                label.text = names[i];
                bool isCardinal = names[i].Length == 1;
                label.fontSize = isCardinal ? 20f : 11f;
                label.color = isCardinal ? new Color(0.95f, 0.9f, 0.75f) : new Color(0.7f, 0.65f, 0.55f);
                _labels.Add((label, i * 15f));
            }
        }

        private void Update()
        {
            AdventurerComponent adventurer = _context.LocalAdventurer;
            float yaw = adventurer != null && adventurer.Object != null && adventurer.Object.IsValid
                ? _context.Battle.Input.LookRotation.y
                : 0f;

            foreach ((TMP_Text label, float angle) in _labels)
            {
                float delta = Mathf.DeltaAngle(yaw, angle);
                bool isVisible = Mathf.Abs(delta) < _visibleDegrees * 0.5f;
                label.gameObject.SetActive(isVisible);

                if (isVisible)
                {
                    label.rectTransform.anchoredPosition = new Vector2(delta * _pixelsPerDegree, 0f);
                    label.alpha = 1f - Mathf.Abs(delta) / (_visibleDegrees * 0.5f);
                }
            }
        }
    }
}
