#if UNITY_EDITOR
using UnityEngine;

namespace Game.Scripts
{
    public class FpsCounter : MonoBehaviour
    {
        private const float UpdateInterval = 0.5f;

        private readonly GUIStyle _style = new();
        private float _elapsed;
        private int _frames;
        private string _text = string.Empty;

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            _frames++;

            if (_elapsed < UpdateInterval)
            {
                return;
            }

            float fps = _frames / _elapsed;
            _text = $"{fps:0} FPS  {1000f / fps:0.0} ms";
            _style.normal.textColor = fps >= 55f ? Color.green : fps >= 30f ? Color.yellow : Color.red;
            _elapsed = 0f;
            _frames = 0;
        }

        private void OnGUI()
        {
            _style.fontSize = Screen.height / 40;
            GUI.Label(new Rect(10f, 10f, 300f, 50f), _text, _style);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            GameObject go = new(nameof(FpsCounter)) { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go);
            go.AddComponent<FpsCounter>();
        }
    }
}
#endif
