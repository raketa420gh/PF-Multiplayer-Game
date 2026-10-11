using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Draws only the generated catacomb parts near the camera: renderers, lights and flickers of parts more than a few cells
    /// away stay off and their flames pause, the fog swallows them long before. A side or post on a cell border belongs to every
    /// cell it touches and is drawn while any of them is near. Colliders and network objects are untouched.
    public sealed class CatacombCullingView : MonoBehaviour
    {
        [SerializeField]
        private CatacombGenerator _catacombs;

        [SerializeField]
        private Camera _camera;

        [SerializeField, Tooltip("Parts up to this many cells from the camera's one are drawn")]
        private int _radius = 1;

        private const float Probe = 0.5f;

        private Renderer[][] _renderers = new Renderer[0][];
        private Light[][] _lights = new Light[0][];
        private ParticleSystem[][] _flames = new ParticleSystem[0][];
        private FlickerLightComponent[][] _flickers = new FlickerLightComponent[0][];
        private int[][] _cells = new int[0][];
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

            for (int i = 0; i < _cells.Length; i++)
            {
                bool isShown = IsNear(_cells[i], cell);

                if (isShown != _shown[i])
                    Show(i, isShown);
            }
        }

        private void Cache()
        {
            _version = _catacombs.Version;
            _cell = -1;
            Transform content = _catacombs.Content;
            int count = content != null ? content.childCount : 0;
            _renderers = new Renderer[count][];
            _lights = new Light[count][];
            _flames = new ParticleSystem[count][];
            _flickers = new FlickerLightComponent[count][];
            _cells = new int[count][];
            _shown = new bool[count];

            for (int i = 0; i < count; i++)
            {
                Transform part = content.GetChild(i);
                _renderers[i] = part.GetComponentsInChildren<Renderer>(true);
                _lights[i] = part.GetComponentsInChildren<Light>(true);
                _flames[i] = part.GetComponentsInChildren<ParticleSystem>(true);
                _flickers[i] = part.GetComponentsInChildren<FlickerLightComponent>(true);
                _cells[i] = Cells(part.position);
                _shown[i] = true;
            }
        }

        /// The cells around a point: one for a room centre, two for a side, up to four for a post.
        private int[] Cells(Vector3 position)
        {
            HashSet<int> cells = new();

            for (int corner = 0; corner < 4; corner++)
                cells.Add(_catacombs.GetCell(position + new Vector3(corner % 2 == 0 ? -Probe : Probe, 0f, corner < 2 ? -Probe : Probe)));

            int[] result = new int[cells.Count];
            cells.CopyTo(result);

            return result;
        }

        private bool IsNear(int[] cells, int cell)
        {
            int grid = _catacombs.Grid;

            foreach (int other in cells)
            {
                if (Mathf.Max(Mathf.Abs(other % grid - cell % grid), Mathf.Abs(other / grid - cell / grid)) <= _radius)
                    return true;
            }

            return false;
        }

        private void Show(int part, bool isShown)
        {
            _shown[part] = isShown;

            foreach (Renderer renderer in _renderers[part])
                renderer.enabled = isShown;

            foreach (Light light in _lights[part])
                light.enabled = isShown;

            foreach (FlickerLightComponent flicker in _flickers[part])
                flicker.enabled = isShown;

            foreach (ParticleSystem flame in _flames[part])
            {
                if (!isShown)
                    flame.Pause(false);
                else if (flame.isPaused)
                    flame.Play(false);
            }
        }
    }
}
