using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Draws only the catacomb rooms near the camera: renderers and lights of rooms more than a few cells away stay off and their
    /// flames pause, the fog swallows them long before. Colliders and network objects are untouched.
    public sealed class CatacombCullingView : MonoBehaviour
    {
        [SerializeField]
        private CatacombGenerator _catacombs;

        [SerializeField]
        private Camera _camera;

        [SerializeField, Tooltip("Rooms up to this many cells from the camera's one are drawn")]
        private int _radius = 2;

        private Renderer[][] _renderers = new Renderer[0][];
        private Light[][] _lights = new Light[0][];
        private ParticleSystem[][] _flames = new ParticleSystem[0][];
        private int[] _cells = new int[0];
        private bool[] _shown = new bool[0];
        private int _version = -1;
        private int _cell = -1;

        private void LateUpdate()
        {
            if (_catacombs.Version != _version)
                Cache();

            int cell = _catacombs.GetCell(_camera.transform.position);

            if (cell == _cell)
                return;

            _cell = cell;
            int grid = _catacombs.Grid;

            for (int i = 0; i < _cells.Length; i++)
            {
                bool isShown = Mathf.Max(Mathf.Abs(_cells[i] % grid - cell % grid), Mathf.Abs(_cells[i] / grid - cell / grid)) <= _radius;

                if (isShown != _shown[i])
                    Show(i, isShown);
            }
        }

        private void Cache()
        {
            _version = _catacombs.Version;
            _cell = -1;
            int count = _catacombs.Rooms.Count;
            _renderers = new Renderer[count][];
            _lights = new Light[count][];
            _flames = new ParticleSystem[count][];
            _cells = new int[count];
            _shown = new bool[count];

            for (int i = 0; i < count; i++)
            {
                CatacombRoomComponent room = _catacombs.Rooms[i];
                _renderers[i] = room.GetComponentsInChildren<Renderer>(true);
                _lights[i] = room.GetComponentsInChildren<Light>(true);
                _flames[i] = room.GetComponentsInChildren<ParticleSystem>(true);
                _cells[i] = _catacombs.GetCell(room.transform.position);
                _shown[i] = true;
            }
        }

        private void Show(int room, bool isShown)
        {
            _shown[room] = isShown;

            foreach (Renderer renderer in _renderers[room])
                renderer.enabled = isShown;

            foreach (Light light in _lights[room])
                light.enabled = isShown;

            foreach (ParticleSystem flame in _flames[room])
            {
                if (!isShown)
                    flame.Pause(false);
                else if (flame.isPaused)
                    flame.Play(false);
            }
        }
    }
}
