using System.Linq;
using Game.Scripts.Battle;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Scripts.Dungeon
{
    /// Fits rigid armor pieces (helmets) on the bare character: offset, turn and scale of the prefab root on its bone, as ArmorDresser
    /// puts it there in the game. Save writes them into the prefab; DungeonContentBuilder keeps a prefab's root on a rebuild.
    public sealed class EquipmentFitView : DisplayableView
    {
        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private ArmorPieceSetConfig _pieces;

        [SerializeField, Tooltip("Pieces the fit editor lists: the ones whose prefab the content builder keeps")]
        private ArmorVisual[] _visuals;

        private const float GuiHeight = AnimationTestView.GuiHeight;
        private const float Margin = AnimationTestView.Margin;
        private const float PanelWidth = 360f;
        private const float StepWidth = 24f;
        private const float FieldOfView = 35f;

        private AnimationEditorSkin _skin;
        private ArmorPieceEntry[] _entries;
        private int _index;
        private Transform _piece;
        private Vector3 _position;
        private Vector3 _rotation;
        private Vector3 _scale = Vector3.one;
        private float _yaw = 180f;
        private float _tilt = 8f;
        private float _distance = 1f;
        private bool _isPaused = true;
        private Vector2 _scroll;

        private Transform Bone => _animator.GetBoneTransform(_entries[_index].Bone);
        private GameObject Prefab => _entries[_index].Prefab;

        private void Awake()
        {
            _entries = _pieces.Entries.Where(entry => entry.Prefab != null && _visuals.Contains(entry.Visual)).ToArray();
        }

        private void OnEnable()
        {
            if (_entries.Length > 0)
                Select(_index);
        }

        private void OnDisable()
        {
            if (_piece != null)
                Destroy(_piece.gameObject);

            _animator.speed = 1f;
        }

        private void Update()
        {
            _animator.speed = _isPaused ? 0f : 1f;
            Vector2 mouse = Input.mousePosition * GuiHeight / Screen.height;

            if (mouse.x > PanelWidth + Margin * 2f)
                _distance = Mathf.Clamp(_distance - Input.mouseScrollDelta.y * 0.1f, 0.25f, 4f);

            if (Input.GetMouseButton(1))
            {
                _yaw += Input.GetAxisRaw("Mouse X") * 4f;
                _tilt = Mathf.Clamp(_tilt - Input.GetAxisRaw("Mouse Y") * 4f, -80f, 85f);
            }

            if (_piece == null)
                return;

            _piece.SetLocalPositionAndRotation(_position, Quaternion.Euler(_rotation));
            _piece.localScale = _entries[_index].IsMirrored ? new Vector3(-_scale.x, _scale.y, _scale.z) : _scale;
        }

        private void LateUpdate()
        {
            Vector3 target = _entries.Length > 0 ? Bone.position : _animator.transform.position + Vector3.up * 1.6f;
            Quaternion orbit = Quaternion.Euler(_tilt, _yaw, 0f);
            _camera.fieldOfView = FieldOfView;
            _camera.transform.SetPositionAndRotation(target - orbit * Vector3.forward * _distance, orbit);
        }

        private void OnGUI()
        {
            _skin ??= new AnimationEditorSkin();
            float scale = Screen.height / GuiHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            GUILayout.BeginArea(new Rect(Margin, Margin, PanelWidth, GuiHeight - Margin * 3f - EditorMenuView.ButtonHeight), _skin.Panel);
            GUILayout.Label("Equipment fit", _skin.Header);
            GUILayout.Label("RMB drag — orbit around the bone, wheel — zoom", _skin.Hint);
            _scroll = GUILayout.BeginScrollView(_scroll);

            for (int i = 0; i < _entries.Length; i++)
            {
                if (_skin.Row(_entries[i].Visual.ToString(), i == _index, 0f, _entries[i].Bone.ToString(), AnimationEditorSkin.Dim))
                    Select(i);
            }

            if (_piece != null)
                DrawFit();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            _skin?.Dispose();
        }

        private void DrawFit()
        {
            _skin.Separator();
            GUILayout.BeginHorizontal();

            foreach ((string name, float yaw) in new[] { ("Front", 180f), ("Side", 270f), ("Back", 0f) })
            {
                if (_skin.Button(name))
                    (_yaw, _tilt) = (yaw, 0f);
            }

            _isPaused = !_skin.Toggle(!_isPaused, "Animate");
            GUILayout.EndHorizontal();

            GUILayout.Label("Position, mm", _skin.Label);
            _position = Axes(_position * 1000f, 400f, 1f, "F0") / 1000f;
            GUILayout.Label("Rotation, °", _skin.Label);
            _rotation = Axes(_rotation, 180f, 0.5f, "F1");
            GUILayout.Label("Scale", _skin.Label);
            float all = Axis("All", _scale.y, 0.01f, 2f, 0.005f, "F3");

            if (!Mathf.Approximately(all, _scale.y) && _scale.y > 0f)
                _scale *= all / _scale.y;

            _scale = Axes(_scale, 0f, 0.005f, "F3", 0.01f, 2f);
            _skin.Separator();
            bool isChanged = _position != Prefab.transform.localPosition || Quaternion.Euler(_rotation) != Prefab.transform.localRotation ||
                             _scale != Prefab.transform.localScale;
            GUILayout.BeginHorizontal();

            if (_skin.Button("Revert"))
                Select(_index);

            if (_skin.Button(isChanged ? "Save *" : "Save", isChanged))
                Save();

            GUILayout.EndHorizontal();
        }

        private void Select(int index)
        {
            if (_piece != null)
                Destroy(_piece.gameObject);

            _index = index;
            _piece = Instantiate(Prefab, Bone, false).transform;
            _position = Prefab.transform.localPosition;
            _rotation = Wrap(Prefab.transform.localEulerAngles);
            _scale = Prefab.transform.localScale;
        }

        private void Save()
        {
#if UNITY_EDITOR
            Prefab.transform.SetLocalPositionAndRotation(_position, Quaternion.Euler(_rotation));
            Prefab.transform.localScale = _scale;
            PrefabUtility.SavePrefabAsset(Prefab);
            Debug.Log($"[{nameof(EquipmentFitView)}] {Prefab.name}: position {_position:F4}, rotation {_rotation:F1}, scale {_scale:F4}");
#endif
        }

        /// Three axis rows; range 0 means the min..max given after.
        private Vector3 Axes(Vector3 value, float range, float step, string format, float min = 0f, float max = 0f)
        {
            if (range > 0f)
                (min, max) = (-range, range);

            return new Vector3(Axis("X", value.x, min, max, step, format), Axis("Y", value.y, min, max, step, format), Axis("Z", value.z, min, max, step, format));
        }

        /// A slider for the rough value and − / + for one step each.
        private float Axis(string text, float value, float min, float max, float step, string format)
        {
            GUILayout.BeginHorizontal();

            if (_skin.Button("−", false, StepWidth))
                value -= step;

            value = _skin.Slider(text, value, min, max, format);

            if (_skin.Button("+", false, StepWidth))
                value += step;

            GUILayout.EndHorizontal();

            return Mathf.Clamp(value, min, max);
        }

        private static Vector3 Wrap(Vector3 euler)
        {
            return new Vector3(Mathf.DeltaAngle(0f, euler.x), Mathf.DeltaAngle(0f, euler.y), Mathf.DeltaAngle(0f, euler.z));
        }
    }
}
