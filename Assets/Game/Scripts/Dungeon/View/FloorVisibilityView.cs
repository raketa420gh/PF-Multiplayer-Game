using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Renders only the floor the local adventurer stands on: renderers and lights of other floors stay off until a descent.
    /// Colliders are untouched, the host still simulates every floor.
    public sealed class FloorVisibilityView : MonoBehaviour
    {
        [SerializeField]
        private DungeonContext _context;

        [SerializeField]
        private Transform[] _floors;

        private Renderer[][] _renderers;
        private Light[][] _lights;
        private int _shownFloor = -1;

        private void Awake()
        {
            _renderers = new Renderer[_floors.Length][];
            _lights = new Light[_floors.Length][];

            for (int i = 0; i < _floors.Length; i++)
            {
                _renderers[i] = _floors[i].GetComponentsInChildren<Renderer>(true);
                _lights[i] = _floors[i].GetComponentsInChildren<Light>(true);
            }
        }

        private void LateUpdate()
        {
            AdventurerComponent adventurer = _context.LocalAdventurer;
            bool isInside = adventurer != null && adventurer.Object != null && adventurer.Object.IsValid;
            int floor = isInside ? Mathf.Clamp(adventurer.Floor - 1, 0, _floors.Length - 1) : 0;

            if (floor == _shownFloor)
                return;

            _shownFloor = floor;

            for (int i = 0; i < _floors.Length; i++)
            {
                bool isShown = i == floor;

                foreach (Renderer renderer in _renderers[i])
                    renderer.enabled = isShown;

                foreach (Light light in _lights[i])
                    light.enabled = isShown;
            }
        }
    }
}
