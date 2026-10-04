using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Scripts.Battle
{
    /// Frame-by-frame bone posing on top of the animation test scene: pick a bone, turn it with the rotation rings and the change
    /// is keyed on the current frame of the clip under edit. Edited clips play from their untouched source with the keys turned on
    /// top; Save bakes the keys into the clip and keeps them in AnimationEditConfig, so the next animation rebuild puts them back.
    [DefaultExecutionOrder(-100)]
    public sealed class AnimationEditorView : MonoBehaviour
    {
        [SerializeField]
        private AnimationTestView _test;

        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private AnimationEditConfig _config;

        [Tooltip("Rotation ring radius in pixels")]
        [SerializeField]
        private float _gizmoRadius = 90f;

        [SerializeField]
        private int _undoDepth = 100;

        private const int BaseLayer = AnimationTestView.BaseLayer;
        private const int UpperLayer = AnimationTestView.UpperLayer;
        private const int ViewAxis = 3;
        private const int RingSegments = 64;
        private const float FrameRate = 60f;
        private const float GuiHeight = AnimationTestView.GuiHeight;
        private const float Margin = AnimationTestView.Margin;
        private const float TimelineHeight = 196f;
        private const float BonePanelWidth = 280f;
        private const float TrackLabelWidth = 90f;
        private const float RulerHeight = 18f;
        private const float RowHeight = 22f;
        private const float KeySize = 9f;
        private const float PickDistance = 10f;
        private const float ViewRingScale = 1.2f;
        private const float ReplayWindow = 2f;
        private const float JointScale = 0.05f;
        private const HumanBodyBones NoBone = HumanBodyBones.LastBone;

        private static readonly Vector3[] s_axes = { Vector3.right, Vector3.up, Vector3.forward };
        private static readonly Color[] s_axisColors = { new(1f, 0.3f, 0.3f), new(0.4f, 0.95f, 0.3f), new(0.3f, 0.6f, 1f), new(0.85f, 0.85f, 0.85f) };
        private static readonly Color s_activeColor = new(1f, 0.85f, 0.2f);
        private static readonly Color s_keyColor = new(1f, 0.55f, 0.15f);
        private static readonly Color s_trackColor = new(0.55f, 0.85f, 1f);
        private static readonly Color s_cursorColor = new(1f, 0.25f, 0.25f);
        private static readonly Color s_gripColor = new(1f, 0.3f, 1f, 0.45f);
        private static readonly Color s_rangeColor = new(0.3f, 0.8f, 1f, 0.25f);
        private static readonly string[] s_layerNames = { "Legs", "Upper body" };

        private static readonly HumanBodyBones[] s_legBones =
        {
            HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg,
            HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot, HumanBodyBones.LeftToes, HumanBodyBones.RightToes
        };

        private readonly Dictionary<AnimationClip, ClipEdit> _edits = new();
        /// Source clip → edited clip, to recognise an overridden clip reported by the animator.
        private readonly Dictionary<AnimationClip, AnimationClip> _clips = new();
        private readonly HashSet<AnimationClip> _dirty = new();
        private readonly List<ClipEdit> _undo = new();
        private readonly List<ClipEdit> _redo = new();
        private readonly Dictionary<HumanBodyBones, BoneKey> _clipboard = new();
        private readonly List<HumanBodyBones> _bones = new();
        private readonly Dictionary<HumanBodyBones, HumanBodyBones> _parents = new();
        private readonly Dictionary<HumanBodyBones, int> _depths = new();
        private readonly Vector3[] _ring = new Vector3[RingSegments + 1];
        /// Weapon sockets as the character is built; holding grips move them, so they are put back every frame.
        private readonly Dictionary<HumanBodyBones, Pose> _socketRests = new();
        private AnimatorOverrideController _overrides;
        private Material _material;
        private GUIStyle _listStyle;
        private ClipEdit _edit;
        private HumanBodyBones _bone = NoBone;
        private Rect _toggleRect;
        private Rect _timelineRect;
        private Rect _boneRect;
        private Vector2 _boneScroll;
        private Vector2 _lastMouse;
        private Vector2 _dragTangent;
        private Vector3 _moveTarget;
        /// Metres per pixel along the dragged move axis.
        private float _dragScale;
        private int _hoverAxis = -1;
        private int _dragAxis = -1;
        private int _draggedKey = -1;
        private int _editLayer = UpperLayer;
        private int _testLayer = -1;
        private int _frame;
        private int _frameCount = 1;
        private int _rangeStart = -1;
        private int _rangeEnd = -1;
        private float _framePosition;
        private float _replayTime;
        private float _replayEnd;
        private bool _isOpen = true;
        private bool _isChanging;
        private bool _isScrubbing;
        private bool _isDraggingPose;
        private bool _isShowingBaked;
        private bool _isMoving;
        private bool _isSkeletonVisible = true;
        private bool _isSelectingRange;
        private bool _isEasing = true;

        private void Awake()
        {
            _material = new Material(Shader.Find("Hidden/Internal-Colored")) { hideFlags = HideFlags.HideAndDontSave };
            _material.SetInt("_ZTest", (int)CompareFunction.Always);
            _material.SetInt("_ZWrite", 0);
            _material.SetInt("_Cull", (int)CullMode.Off);

            _overrides = new AnimatorOverrideController(_animator.runtimeAnimatorController);
            _animator.runtimeAnimatorController = _overrides;

            foreach (ClipEdit edit in _config.Clips)
                Override(edit);

            CollectBones();

            foreach (HumanBodyBones hand in new[] { HumanBodyBones.LeftHand, HumanBodyBones.RightHand })
            {
                Transform socket = HandGrip.GetSocket(_animator, hand);

                if (socket != null)
                    _socketRests[hand] = new Pose(socket.localPosition, socket.localRotation);
            }
        }

        private void OnEnable()
        {
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        private void OnDisable()
        {
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        }

        private void OnDestroy()
        {
            Destroy(_material);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
                _isOpen = !_isOpen;

            _test.SetEditing(_isOpen);

            if (Time.unscaledTime < _replayEnd && !_test.IsCurrent)
                _test.Seek(_replayTime);

            if (_test.Layer != _testLayer)
                _editLayer = (_testLayer = _test.Layer) == BaseLayer ? BaseLayer : UpperLayer;

            _edit = GetEdit(Resolve(_test.GetClip(_editLayer)));
            _frameCount = Mathf.Max(1, Mathf.RoundToInt(_test.GetLength(_editLayer) * FrameRate));
            _framePosition = _test.GetTime(_editLayer) * FrameRate;
            _frame = Mathf.RoundToInt(_framePosition);

            if (_bone != NoBone && !IsLayerBone(_bone))
                _bone = NoBone;

            if (Input.GetMouseButtonUp(0))
                _isChanging = false;

            if (!_isOpen || _edit == null || _isShowingBaked)
                return;

            UpdateGizmo();
            UpdateShortcuts();
        }

        /// Runs before AnimationTestView bends the spine for the look pitch and places the camera.
        private void LateUpdate()
        {
            if (_isShowingBaked)
                return;

            foreach (KeyValuePair<HumanBodyBones, Pose> rest in _socketRests)
                HandGrip.GetSocket(_animator, rest.Key).SetLocalPositionAndRotation(rest.Value.position, rest.Value.rotation);

            for (int layer = BaseLayer; layer <= UpperLayer; layer++)
                GetEdit(Resolve(_test.GetClip(layer)))?.Apply(_animator, _test.GetTime(layer) * FrameRate);
        }

        private void OnGUI()
        {
            float scale = Screen.height / GuiHeight;
            float width = Screen.width / scale;
            float left = (AnimationTestView.PanelWidth + Margin) * 2f + Margin;
            float right = width - AnimationTestView.PanelWidth - Margin * 2f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            _listStyle ??= new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft };

            _toggleRect = new Rect(left, Margin, 200f, 26f);

            if (GUI.Button(_toggleRect, _isOpen ? "Close animation editor (Tab)" : "Animation editor (Tab)"))
                _isOpen = !_isOpen;

            if (!_isOpen)
            {
                _timelineRect = _boneRect = Rect.zero;

                return;
            }

            _timelineRect = new Rect(left, GuiHeight - Margin - TimelineHeight, right - left, TimelineHeight);
            _boneRect = _edit == null ? Rect.zero : new Rect(right - BonePanelWidth, Margin, BonePanelWidth, GuiHeight - TimelineHeight - Margin * 3f);

            GUILayout.BeginArea(_timelineRect, GUI.skin.box);
            DrawTimelinePanel();
            GUILayout.EndArea();

            if (_edit == null)
                return;

            GUILayout.BeginArea(_boneRect, GUI.skin.box);
            DrawBonePanel();
            GUILayout.EndArea();
        }

        private void CollectBones()
        {
            Dictionary<Transform, HumanBodyBones> bones = new();

            for (HumanBodyBones bone = 0; bone < HumanBodyBones.LastBone; bone++)
            {
                Transform transform = bone < HumanBodyBones.LeftEye || bone == HumanBodyBones.UpperChest ? _animator.GetBoneTransform(bone) : null;

                if (transform != null)
                    bones[transform] = bone;
            }

            // Depth-first order of the hierarchy, so the list reads as a tree.
            foreach (Transform transform in _animator.GetComponentsInChildren<Transform>())
            {
                if (!bones.TryGetValue(transform, out HumanBodyBones bone))
                    continue;

                Transform parent = transform.parent;

                while (parent != null && !bones.ContainsKey(parent))
                    parent = parent.parent;

                _bones.Add(bone);
                _parents[bone] = parent != null ? bones[parent] : NoBone;
                _depths[bone] = parent != null ? _depths[bones[parent]] + 1 : 0;
            }
        }

        private void Override(ClipEdit edit)
        {
            if (edit.Clip == null || edit.Source == null)
                return;

            _overrides[edit.Clip] = _isShowingBaked ? null : edit.Source;
            _clips[edit.Source] = edit.Clip;
        }

        private AnimationClip Resolve(AnimationClip clip)
        {
            return clip != null && _clips.TryGetValue(clip, out AnimationClip edited) ? edited : clip;
        }

        private ClipEdit GetEdit(AnimationClip clip)
        {
            if (clip == null || !IsEditable(clip))
                return null;

            if (_edits.TryGetValue(clip, out ClipEdit edit))
                return edit;

            return _edits[clip] = _config.Find(clip)?.Clone() ?? new ClipEdit(clip, null);
        }

        private bool IsLayerBone(HumanBodyBones bone)
        {
            return Array.IndexOf(s_legBones, bone) >= 0 == (_editLayer == BaseLayer);
        }

        private Transform GetBone(HumanBodyBones bone)
        {
            return _animator.GetBoneTransform(bone);
        }

        private BoneKey Evaluate(HumanBodyBones bone)
        {
            return _edit.Find(bone)?.Evaluate(_frame) ?? new BoneKey(_frame, Quaternion.identity, Vector3.zero);
        }

        private void SetFrame(int frame)
        {
            float length = _test.GetLength(_editLayer);
            _test.Seek(Mathf.Clamp01(Mathf.Clamp(frame, 0, _frameCount) / FrameRate / length));
            _framePosition = _frame = Mathf.Clamp(frame, 0, _frameCount);
        }

        private void JumpToKey(int direction)
        {
            int target = -1;

            foreach (BoneTrack track in _edit.Tracks)
            {
                if (_bone != NoBone && track.Bone != _bone)
                    continue;

                foreach (BoneKey key in track.Keys)
                {
                    if ((key.Frame - _frame) * direction > 0 && (target < 0 || (key.Frame - target) * direction < 0))
                        target = key.Frame;
                }
            }

            if (target >= 0)
                SetFrame(target);
        }

        private void UpdateGizmo()
        {
            Vector2 mouse = Input.mousePosition;

            if (_dragAxis >= 0)
            {
                if (!Input.GetMouseButton(0))
                {
                    _dragAxis = -1;

                    return;
                }

                Vector2 delta = mouse - _lastMouse;
                _lastMouse = mouse;

                if (delta == Vector2.zero)
                    return;

                if (_isMoving)
                    Move(delta);
                else
                    Rotate(delta);

                return;
            }

            Vector2 point = new Vector2(mouse.x, Screen.height - mouse.y) * GuiHeight / Screen.height;
            bool isOverView = _test.IsOverViewport(point) && !_toggleRect.Contains(point) && !_timelineRect.Contains(point) && !_boneRect.Contains(point);
            _hoverAxis = !isOverView || _bone == NoBone ? -1 : !_isMoving ? PickAxis(mouse) : CanMove(_bone) ? PickMoveAxis(mouse) : -1;

            if (!isOverView || !Input.GetMouseButtonDown(0))
                return;

            if (_hoverAxis < 0)
            {
                _bone = PickBone(mouse);

                return;
            }

            _dragAxis = _hoverAxis;
            _lastMouse = mouse;
            _moveTarget = GetBone(_bone).position;
        }

        private void Rotate(Vector2 delta)
        {
            Quaternion rotation = GetBone(_bone).rotation;
            Vector3 axis = _dragAxis < ViewAxis ? s_axes[_dragAxis] : Quaternion.Inverse(rotation) * _camera.transform.forward;
            Quaternion turn = Quaternion.AngleAxis(Vector2.Dot(delta, _dragTangent) / _gizmoRadius * Mathf.Rad2Deg, axis);
            HandGrip grip = GetActiveGrip(_bone);

            if (grip != null)
            {
                BeginChange();

                // A hold turns the hand about the wrist and the weapon stays: the socket turns back by as much.
                if (grip.IsHolding)
                    grip.SetPose(Quaternion.Inverse(turn) * grip.Position, Quaternion.Inverse(turn) * grip.Rotation);
                else
                    grip.SetPose(grip.Position, grip.Rotation * turn);

                _dirty.Add(_edit.Clip);

                return;
            }

            BoneKey key = Evaluate(_bone);
            ChangeKey(_bone, new BoneKey(_frame, key.Rotation * turn, key.Position));
        }

        /// The hips shift by themselves; any other joint is pulled by turning the bones above it, and the change is keyed as
        /// rotations of those bones. A hand or a foot bends its limb and keeps its own orientation.
        private void Move(Vector2 delta)
        {
            Transform bone = GetBone(_bone);
            Transform camera = _camera.transform;
            _moveTarget += _dragAxis < ViewAxis
                ? GetMoveAxis(_dragAxis) * (Vector2.Dot(delta, _dragTangent) * _dragScale)
                : (camera.right * delta.x + camera.up * delta.y) * (GetRingRadius(bone.position, 0) / _gizmoRadius);

            HandGrip grip = GetActiveGrip(_bone);

            if (grip != null)
            {
                BeginChange();
                grip.SetPose(grip.IsHolding
                    ? grip.Position - Quaternion.Inverse(bone.rotation) * (_moveTarget - bone.position)
                    : HandGrip.GetSocket(_animator, grip.Anchor).InverseTransformPoint(_moveTarget), grip.Rotation);
                _dirty.Add(_edit.Clip);

                return;
            }

            if (_bone == HumanBodyBones.Hips)
            {
                BoneKey hips = Evaluate(_bone);
                ChangeKey(_bone, new BoneKey(_frame, hips.Rotation, hips.Position + bone.parent.InverseTransformVector(_moveTarget - bone.position)));

                return;
            }

            HumanBodyBones[] chain = GetMoveChain(_bone);
            Quaternion[] locals = Array.ConvertAll(chain, link => GetBone(link).localRotation);
            Quaternion end = bone.rotation;

            if (chain.Length == 3)
            {
                TwoBoneIk.Solve(GetBone(chain[0]), GetBone(chain[1]), bone, _moveTarget);
                bone.rotation = end;
            }
            else
            {
                Transform parent = GetBone(chain[0]);
                parent.rotation = Quaternion.FromToRotation(bone.position - parent.position, _moveTarget - parent.position) * parent.rotation;
            }

            // The pose on screen is clip x key, so the key takes the change of the local rotation on top of its own value.
            for (int i = 0; i < chain.Length; i++)
            {
                BoneKey key = Evaluate(chain[i]);
                ChangeKey(chain[i], new BoneKey(_frame, key.Rotation * Quaternion.Inverse(locals[i]) * GetBone(chain[i]).localRotation, key.Position));
            }
        }

        /// Bones turned to move a joint: upper and lower limb plus the end for hands and feet, otherwise the parent.
        private HumanBodyBones[] GetMoveChain(HumanBodyBones bone)
        {
            return bone switch
            {
                HumanBodyBones.LeftHand => new[] { HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, bone },
                HumanBodyBones.RightHand => new[] { HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, bone },
                HumanBodyBones.LeftFoot => new[] { HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, bone },
                HumanBodyBones.RightFoot => new[] { HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, bone },
                _ => _parents[bone] == NoBone ? Array.Empty<HumanBodyBones>() : new[] { _parents[bone] }
            };
        }

        /// Every bone the move would key must belong to the edited layer.
        private bool CanMove(HumanBodyBones bone)
        {
            HumanBodyBones[] chain = GetMoveChain(bone);

            return bone == HumanBodyBones.Hips || chain.Length > 0 && Array.TrueForAll(chain, IsLayerBone);
        }

        private Vector3 GetMoveAxis(int axis)
        {
            return _animator.transform.rotation * s_axes[axis];
        }

        /// Axis arrows in character space; the square in the middle moves in the view plane.
        private int PickMoveAxis(Vector2 mouse)
        {
            Vector3 position = GetBone(_bone).position;
            float radius = GetRingRadius(position, 0);
            Vector2 center = _camera.WorldToScreenPoint(position);

            if (Vector2.Distance(mouse, center) < PickDistance * 1.5f)
                return ViewAxis;

            float best = PickDistance;
            int picked = -1;

            for (int i = 0; i < ViewAxis; i++)
            {
                Vector2 tip = _camera.WorldToScreenPoint(position + GetMoveAxis(i) * radius);
                float distance = DistanceToSegment(mouse, center, tip);

                if (distance >= best || (tip - center).sqrMagnitude < 1f)
                    continue;

                best = distance;
                picked = i;
                _dragTangent = (tip - center).normalized;
                _dragScale = radius / (tip - center).magnitude;
            }

            return picked;
        }

        /// A pinned hand inside its range moves its grip, not its keys: the weapon stays and the hand slides over it on every frame.
        private HandGrip GetActiveGrip(HumanBodyBones bone)
        {
            HandGrip grip = _edit.FindGrip(bone);

            return grip != null && grip.GetWeight(_frame) > 0f ? grip : null;
        }

        private void Pin(bool isHolding)
        {
            PushUndo();
            _edit.SetGrip(isHolding ? HandGrip.Hold(_bone, _socketRests[_bone], 0, _frameCount) : HandGrip.Pin(_animator, _bone, 0, _frameCount));
            _dirty.Add(_edit.Clip);
        }

        /// The hand carries something in its weapon socket.
        private bool IsHolding(HumanBodyBones hand)
        {
            Transform socket = HandGrip.GetSocket(_animator, hand);

            return socket != null && socket.childCount > 0;
        }

        private void UpdateShortcuts()
        {
            bool isShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (Input.GetKeyDown(KeyCode.Space))
                _test.SetPaused(!_test.IsPaused);

            if (Input.GetKeyDown(KeyCode.Comma))
                SetFrame(_frame - 1);

            if (Input.GetKeyDown(KeyCode.Period))
                SetFrame(_frame + 1);

            if (Input.GetKeyDown(KeyCode.LeftBracket))
                JumpToKey(-1);

            if (Input.GetKeyDown(KeyCode.RightBracket))
                JumpToKey(1);

            if (Input.GetKeyDown(KeyCode.K))
                SetKeys(isShift);

            if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.X))
                DeleteKeys(isShift);

            if (Input.GetKeyDown(KeyCode.W))
                _isMoving = true;

            if (Input.GetKeyDown(KeyCode.E))
                _isMoving = false;

            if (Input.GetKeyDown(KeyCode.R))
                ResetBone();

            if (Input.GetKeyDown(KeyCode.C))
                CopyPose();

            if (Input.GetKeyDown(KeyCode.V))
                PastePose();

            if (Input.GetKeyDown(KeyCode.I))
                GenerateInBetweens();

            if (Input.GetKeyDown(KeyCode.Z))
                StepHistory(isShift ? _redo : _undo, isShift ? _undo : _redo);
        }

        /// Index of the ring under the mouse (local X, Y, Z or the view ring) and the screen direction that turns it forward.
        private int PickAxis(Vector2 mouse)
        {
            Transform bone = GetBone(_bone);
            float best = PickDistance;
            int axis = -1;

            for (int i = 0; i <= ViewAxis; i++)
            {
                Vector3 normal = GetRingNormal(bone, i);
                BuildRing(bone.position, normal, GetRingRadius(bone.position, i));

                for (int j = 0; j < RingSegments; j++)
                {
                    Vector3 a = _camera.WorldToScreenPoint(_ring[j]);
                    Vector3 b = _camera.WorldToScreenPoint(_ring[j + 1]);
                    float distance = DistanceToSegment(mouse, a, b);

                    if (a.z <= 0f || distance >= best)
                        continue;

                    best = distance;
                    axis = i;
                    Vector3 tangent = Vector3.Cross(normal, _ring[j] - bone.position) * 0.05f;
                    _dragTangent = ((Vector2)_camera.WorldToScreenPoint(_ring[j] + tangent) - (Vector2)a).normalized;
                }
            }

            return axis;
        }

        /// Clicking past every joint clears the selection.
        private HumanBodyBones PickBone(Vector2 mouse)
        {
            HumanBodyBones picked = NoBone;
            float best = PickDistance * 1.5f;

            foreach (HumanBodyBones bone in _bones)
            {
                Vector3 point = _camera.WorldToScreenPoint(GetBone(bone).position);
                float distance = Vector2.Distance(mouse, point);

                if (IsLayerBone(bone) && point.z > 0f && distance < best)
                    (picked, best) = (bone, distance);
            }

            return picked;
        }

        private Vector3 GetRingNormal(Transform bone, int axis)
        {
            return axis < ViewAxis ? bone.rotation * s_axes[axis] : _camera.transform.forward;
        }

        private float GetRingRadius(Vector3 position, int axis)
        {
            float depth = Vector3.Dot(position - _camera.transform.position, _camera.transform.forward);
            float radius = _gizmoRadius * 2f * depth * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad) / Screen.height;

            return axis == ViewAxis ? radius * ViewRingScale : radius;
        }

        private void BuildRing(Vector3 center, Vector3 normal, float radius)
        {
            Vector3 u = Vector3.Cross(normal, Mathf.Abs(normal.y) < 0.9f ? Vector3.up : Vector3.right).normalized * radius;
            Vector3 v = Vector3.Cross(normal, u);

            for (int i = 0; i <= RingSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / RingSegments;
                _ring[i] = center + u * Mathf.Cos(angle) + v * Mathf.Sin(angle);
            }
        }

        /// Continuous edits (ring drags, sliders, key drags) take one undo step per mouse press.
        private void BeginChange()
        {
            if (_isChanging)
                return;

            _isChanging = true;
            PushUndo();
        }

        private void PushUndo()
        {
            _undo.Add(_edit.Clone());
            _redo.Clear();

            if (_undo.Count > _undoDepth)
                _undo.RemoveAt(0);
        }

        private void StepHistory(List<ClipEdit> from, List<ClipEdit> to)
        {
            if (from.Count == 0)
                return;

            ClipEdit snapshot = from[^1];
            from.RemoveAt(from.Count - 1);
            to.Add(_edits[snapshot.Clip].Clone());
            _edits[snapshot.Clip] = snapshot;
            _dirty.Add(snapshot.Clip);

            if (_edit.Clip == snapshot.Clip)
                _edit = snapshot;
        }

        private void ChangeKey(HumanBodyBones bone, BoneKey key)
        {
            BeginChange();
            _edit.GetOrAdd(bone).SetKey(key);
            _dirty.Add(_edit.Clip);
        }

        /// Pins the current pose of the selected bone, or of every animated bone, as keys on this frame.
        private void SetKeys(bool isPose)
        {
            if (!isPose && _bone == NoBone)
                return;

            PushUndo();

            if (!isPose)
                _edit.GetOrAdd(_bone).SetKey(Evaluate(_bone));
            else
                foreach (BoneTrack track in _edit.Tracks)
                    track.SetKey(track.Evaluate(_frame));

            _dirty.Add(_edit.Clip);
        }

        private void DeleteKeys(bool isPose)
        {
            BoneTrack selected = _edit.Find(_bone);

            if (isPose ? !_edit.HasKey(_frame) : selected == null || selected.IndexOf(_frame) < 0)
                return;

            PushUndo();

            foreach (BoneTrack track in _edit.Tracks)
            {
                if (isPose || track == selected)
                    track.RemoveKey(_frame);
            }

            _dirty.Add(_edit.Clip);
        }

        /// Keys never land on a frame that already holds a key of a moved track.
        private void MoveKeys(int from, int to)
        {
            foreach (BoneTrack track in _edit.Tracks)
            {
                if (IsMoved(track) && track.IndexOf(to) >= 0)
                    return;
            }

            BeginChange();

            foreach (BoneTrack track in _edit.Tracks)
            {
                if (IsMoved(track))
                    track.MoveKey(from, to);
            }

            _draggedKey = to;
            _dirty.Add(_edit.Clip);
        }

        private bool IsMoved(BoneTrack track)
        {
            return _isDraggingPose || track.Bone == _bone;
        }

        private void ResetBone()
        {
            if (_bone == NoBone)
                return;

            PushUndo();
            _edit.GetOrAdd(_bone).SetKey(new BoneKey(_frame, Quaternion.identity, Vector3.zero));
            _dirty.Add(_edit.Clip);
        }

        private void ClearTrack()
        {
            PushUndo();
            _edit.RemoveTrack(_bone);
            _dirty.Add(_edit.Clip);
        }

        private void CopyPose()
        {
            _clipboard.Clear();

            foreach (BoneTrack track in _edit.Tracks)
                _clipboard[track.Bone] = track.Evaluate(_frame);
        }

        private void PastePose()
        {
            if (_clipboard.Count == 0)
                return;

            PushUndo();

            foreach (KeyValuePair<HumanBodyBones, BoneKey> pair in _clipboard)
            {
                if (IsLayerBone(pair.Key))
                    _edit.GetOrAdd(pair.Key).SetKey(new BoneKey(_frame, pair.Value.Rotation, pair.Value.Position));
            }

            _dirty.Add(_edit.Clip);
        }

        /// Replaces the motion inside the selected range with a smooth blend between the full poses (clip x key) at its ends: every
        /// moving bone of the layer gets a key on every frame of the range. Ease stops at the ends, otherwise the speed outside carries in.
        private void GenerateInBetweens()
        {
            int start = Mathf.Min(_rangeStart, _rangeEnd);
            int end = Mathf.Min(Mathf.Max(_rangeStart, _rangeEnd), _frameCount);

            if (start < 0 || end - start < 2)
                return;

            int first = Mathf.Max(0, start - 1);
            int last = Mathf.Min(_frameCount, end + 1);
            int frame = _frame;
            List<HumanBodyBones> bones = _bones.FindAll(IsLayerBone);
            Pose[,] clip = new Pose[last - first + 1, bones.Count];

            // The clip alone: edits are applied in LateUpdate, after the animator has been sampled here.
            for (int f = first; f <= last; f++)
            {
                SetFrame(f);
                _animator.Update(0f);

                for (int i = 0; i < bones.Count; i++)
                {
                    Transform bone = GetBone(bones[i]);
                    clip[f - first, i] = new Pose(bone.localPosition, bone.localRotation);
                }
            }

            SetFrame(frame);
            _animator.Update(0f);
            PushUndo();

            int[] ends = { first, start, end, last };

            for (int i = 0; i < bones.Count; i++)
            {
                BoneTrack track = _edit.Find(bones[i]);
                float[][] values = new float[ends.Length][];
                Quaternion previous = Quaternion.identity;

                for (int j = 0; j < ends.Length; j++)
                {
                    BoneKey offset = track?.Evaluate(ends[j]) ?? new BoneKey(ends[j], Quaternion.identity, Vector3.zero);
                    Pose pose = clip[ends[j] - first, i];
                    Quaternion q = pose.rotation * offset.Rotation;
                    Vector3 p = pose.position + offset.Position;

                    if (j > 0 && Quaternion.Dot(previous, q) < 0f)
                        q = new Quaternion(-q.x, -q.y, -q.z, -q.w);

                    previous = q;
                    values[j] = new[] { q.x, q.y, q.z, q.w, p.x, p.y, p.z };
                }

                AnimationCurve[] curves = new AnimationCurve[values[0].Length];

                for (int c = 0; c < curves.Length; c++)
                {
                    float inSpeed = _isEasing || start == first ? 0f : values[1][c] - values[0][c];
                    float outSpeed = _isEasing || end == last ? 0f : values[3][c] - values[2][c];
                    curves[c] = new AnimationCurve(new Keyframe(start, values[1][c], inSpeed, inSpeed), new Keyframe(end, values[2][c], outSpeed, outSpeed));
                }

                BoneKey[] keys = new BoneKey[end - start + 1];
                bool isStill = track == null;

                for (int f = start; f <= end; f++)
                {
                    Pose pose = clip[f - first, i];
                    Quaternion target = new Quaternion(curves[0].Evaluate(f), curves[1].Evaluate(f), curves[2].Evaluate(f), curves[3].Evaluate(f)).normalized;
                    Vector3 position = new Vector3(curves[4].Evaluate(f), curves[5].Evaluate(f), curves[6].Evaluate(f));
                    BoneKey key = keys[f - start] = new BoneKey(f, Quaternion.Inverse(pose.rotation) * target, position - pose.position);
                    isStill &= Quaternion.Angle(key.Rotation, Quaternion.identity) < 0.05f && key.Position.sqrMagnitude < 1e-8f;
                }

                if (isStill)
                    continue;

                track = _edit.GetOrAdd(bones[i]);

                foreach (BoneKey key in keys)
                    track.SetKey(key);
            }

            _dirty.Add(_edit.Clip);
        }

        private void Revert()
        {
            PushUndo();
            AnimationClip clip = _edit.Clip;
            _edit = _edits[clip] = _config.Find(clip)?.Clone() ?? new ClipEdit(clip, null);
            _dirty.Remove(clip);
        }

        private void Save()
        {
#if UNITY_EDITOR
            Replay();

            foreach (AnimationClip clip in _dirty)
            {
                AnimationEditBaker.Save(_config, _edits[clip], _animator);
                Override(_edits[clip]);
            }

            _dirty.Clear();
#endif
        }

        private void ToggleBaked()
        {
            Replay();
            _isShowingBaked = !_isShowingBaked;

            foreach (ClipEdit edit in _config.Clips)
                Override(edit);
        }

        /// Changed overrides and reimported clips rebind the animator back to its default states some frames later; until then
        /// the current state is put back whenever it gets dropped. Called before the change, while the clip length is still right.
        private void Replay()
        {
            _replayTime = Mathf.Clamp01(_frame / FrameRate / _test.GetLength(_editLayer));
            _replayEnd = Time.unscaledTime + ReplayWindow;
            _test.Seek(_replayTime);
        }

        private void DrawTimelinePanel()
        {
            if (_edit == null)
            {
                GUILayout.Label($"{s_layerNames[_editLayer]}: {(_test.GetClip(_editLayer) != null ? _test.GetClip(_editLayer).name : "-")} is not editable. " +
                    "Only generated clips (.anim) can be edited; library takes are read-only.");
                DrawLayerButtons();

                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{_edit.Clip.name}{(_dirty.Contains(_edit.Clip) ? " *" : string.Empty)}    frame {_frame} / {_frameCount}", GUILayout.Width(330f));
            DrawLayerButtons();
            _isSkeletonVisible = GUILayout.Toggle(_isSkeletonVisible, "Skeleton", GUILayout.Width(80f));

            if (AnimationTestView.Button("Rotate (E)", !_isMoving))
                _isMoving = false;

            if (AnimationTestView.Button("Move (W)", _isMoving))
                _isMoving = true;

            if (AnimationTestView.Button(_isShowingBaked ? "Showing baked clip" : "Show baked clip", _isShowingBaked))
                ToggleBaked();

            GUILayout.FlexibleSpace();
            GUI.enabled = _dirty.Count > 0;

            if (GUILayout.Button("Revert", GUILayout.Width(70f)))
                Revert();

            if (GUILayout.Button(_dirty.Count > 1 ? $"Save ({_dirty.Count} clips)" : "Save", GUILayout.Width(110f)))
                Save();

            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("|<"))
                SetFrame(0);

            if (GUILayout.Button("<"))
                SetFrame(_frame - 1);

            if (GUILayout.Button(_test.IsPaused ? "Play" : "Pause", GUILayout.Width(70f)))
                _test.SetPaused(!_test.IsPaused);

            if (GUILayout.Button(">"))
                SetFrame(_frame + 1);

            if (GUILayout.Button(">|"))
                SetFrame(_frameCount);

            if (GUILayout.Button("Prev key"))
                JumpToKey(-1);

            if (GUILayout.Button("Next key"))
                JumpToKey(1);

            if (GUILayout.Button("Undo"))
                StepHistory(_undo, _redo);

            if (GUILayout.Button("Redo"))
                StepHistory(_redo, _undo);

            GUILayout.EndHorizontal();

            GUI.enabled = !_isShowingBaked;
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Key bone"))
                SetKeys(false);

            if (GUILayout.Button("Key pose"))
                SetKeys(true);

            if (GUILayout.Button("Delete key"))
                DeleteKeys(false);

            if (GUILayout.Button("Delete pose keys"))
                DeleteKeys(true);

            if (GUILayout.Button("Copy pose"))
                CopyPose();

            if (GUILayout.Button("Paste pose"))
                PastePose();

            int start = Mathf.Min(_rangeStart, _rangeEnd);
            int end = Mathf.Max(_rangeStart, _rangeEnd);
            GUI.enabled = !_isShowingBaked && start >= 0 && end - start >= 2;

            if (GUILayout.Button(GUI.enabled ? $"In-betweens {start}-{end} (I)" : "In-betweens (Shift+drag range)", GUILayout.Width(230f)))
                GenerateInBetweens();

            GUI.enabled = !_isShowingBaked;
            _isEasing = GUILayout.Toggle(_isEasing, "Ease", GUILayout.Width(60f));
            GUILayout.EndHorizontal();

            GUI.enabled = true;
            DrawTimeline(GUILayoutUtility.GetRect(0f, RulerHeight + RowHeight * 2f, GUILayout.ExpandWidth(true)));
            GUILayout.Label("LMB joint: select, ring / arrow: drag   E rotate   W move   RMB: orbit   Space: play   , . frame   [ ] key   K key (Shift: pose)   " +
                "X/Del delete (Shift: pose)   R reset   C/V pose   Shift+drag timeline: range   I in-betweens   Z undo (Shift: redo)   Tab: hide");
        }

        private void DrawLayerButtons()
        {
            for (int layer = BaseLayer; layer <= UpperLayer; layer++)
            {
                if (AnimationTestView.Button(s_layerNames[layer], _editLayer == layer))
                    _editLayer = layer;
            }
        }

        /// Ruler, a row with the keys of every bone and a row with the keys of the selected one. Dragging a key retimes it, dragging
        /// anywhere else scrubs.
        private void DrawTimeline(Rect area)
        {
            GUI.Box(area, GUIContent.none);
            Rect track = new Rect(area.x + TrackLabelWidth, area.y, area.width - TrackLabelWidth - KeySize, area.height);
            Rect poseRow = new Rect(area.x, area.y + RulerHeight, area.width, RowHeight);
            Rect boneRow = new Rect(area.x, poseRow.yMax, area.width, RowHeight);
            int step = track.width / _frameCount >= 8f ? 5 : track.width / _frameCount >= 4f ? 10 : 30;

            for (int frame = 0; frame <= _frameCount; frame += step)
            {
                float x = GetX(track, frame);
                Fill(new Rect(x, area.y, 1f, frame % (step * 2) == 0 ? 8f : 4f), Color.gray);

                if (frame % (step * 2) == 0)
                    GUI.Label(new Rect(x + 2f, area.y, 40f, RulerHeight), frame.ToString());
            }

            GUI.Label(poseRow, "  All bones");
            GUI.Label(boneRow, "  " + (_bone == NoBone ? "-" : _bone.ToString()));

            HandGrip grip = _bone == NoBone ? null : _edit.FindGrip(_bone);

            if (grip != null)
                Fill(new Rect(GetX(track, grip.Start), boneRow.y + 3f, GetX(track, grip.End) - GetX(track, grip.Start), boneRow.height - 6f), s_gripColor);

            if (_rangeStart >= 0 && _rangeEnd != _rangeStart)
                Fill(new Rect(GetX(track, Mathf.Min(_rangeStart, _rangeEnd)), area.y, Mathf.Abs(GetX(track, _rangeEnd) - GetX(track, _rangeStart)), area.height),
                    s_rangeColor);

            foreach (BoneTrack boneTrack in _edit.Tracks)
            {
                foreach (BoneKey key in boneTrack.Keys)
                {
                    DrawKey(track, poseRow, key.Frame, key.Frame == _frame ? s_keyColor : s_trackColor);

                    if (boneTrack.Bone == _bone)
                        DrawKey(track, boneRow, key.Frame, key.Frame == _frame ? s_keyColor : s_activeColor);
                }
            }

            Fill(new Rect(GetX(track, _framePosition) - 1f, area.y, 2f, area.height), s_cursorColor);
            HandleTimeline(track, poseRow, boneRow);
        }

        private void HandleTimeline(Rect track, Rect poseRow, Rect boneRow)
        {
            Event current = Event.current;
            int frame = Mathf.Clamp(Mathf.RoundToInt((current.mousePosition.x - track.x) / track.width * _frameCount), 0, _frameCount);
            bool isInside = track.Contains(current.mousePosition) || poseRow.Contains(current.mousePosition) || boneRow.Contains(current.mousePosition);

            switch (current.type)
            {
                case EventType.MouseDown when current.button == 0 && current.shift && isInside:
                    _isSelectingRange = true;
                    _rangeStart = _rangeEnd = frame;
                    SetFrame(frame);
                    current.Use();

                    break;
                case EventType.MouseDown when current.button == 0 && isInside:
                    _isDraggingPose = poseRow.Contains(current.mousePosition);
                    _draggedKey = _isShowingBaked ? -1 : FindKey(track, current.mousePosition.x, _isDraggingPose || boneRow.Contains(current.mousePosition));
                    _isScrubbing = _draggedKey < 0;
                    SetFrame(_draggedKey < 0 ? frame : _draggedKey);
                    current.Use();

                    break;
                case EventType.MouseDrag when _isSelectingRange:
                    _rangeEnd = frame;
                    SetFrame(frame);
                    current.Use();

                    break;
                case EventType.MouseDrag when _isScrubbing || _draggedKey >= 0:
                    if (_draggedKey >= 0 && frame != _draggedKey)
                        MoveKeys(_draggedKey, frame);

                    SetFrame(_draggedKey >= 0 ? _draggedKey : frame);
                    current.Use();

                    break;
                case EventType.MouseUp:
                    _isScrubbing = false;
                    _isSelectingRange = false;
                    _draggedKey = -1;

                    break;
            }
        }

        private int FindKey(Rect track, float x, bool isRow)
        {
            if (!isRow)
                return -1;

            foreach (BoneTrack boneTrack in _edit.Tracks)
            {
                if (!IsMoved(boneTrack))
                    continue;

                foreach (BoneKey key in boneTrack.Keys)
                {
                    if (Mathf.Abs(GetX(track, key.Frame) - x) <= KeySize * 0.5f)
                        return key.Frame;
                }
            }

            return -1;
        }

        private float GetX(Rect track, float frame)
        {
            return track.x + frame / _frameCount * track.width;
        }

        private void DrawKey(Rect track, Rect row, int frame, Color color)
        {
            Fill(new Rect(GetX(track, frame) - KeySize * 0.5f, row.center.y - KeySize * 0.5f, KeySize, KeySize), color);
        }

        private void DrawBonePanel()
        {
            GUILayout.Label($"{s_layerNames[_editLayer]} bones   (* animated, [key] keyed here)");
            _boneScroll = GUILayout.BeginScrollView(_boneScroll);

            foreach (HumanBodyBones bone in _bones)
            {
                if (!IsLayerBone(bone))
                    continue;

                BoneTrack track = _edit.Find(bone);
                string mark = (track == null || track.Keys.Count == 0 ? string.Empty : track.IndexOf(_frame) >= 0 ? "   [key]" : "   *") +
                    (_edit.FindGrip(bone) != null ? "   [pin]" : string.Empty);
                GUILayout.BeginHorizontal();
                GUILayout.Space(_depths[bone] * 10f);
                GUI.color = bone == _bone ? Color.yellow : Color.white;

                if (GUILayout.Button(bone + mark, _listStyle))
                    _bone = bone;

                GUI.color = Color.white;
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            if (_bone == NoBone || _isShowingBaked)
                return;

            BoneKey key = Evaluate(_bone);
            Vector3 euler = key.Rotation.eulerAngles;
            Vector3 position = key.Position;
            euler = new Vector3(Mathf.DeltaAngle(0f, euler.x), Mathf.DeltaAngle(0f, euler.y), Mathf.DeltaAngle(0f, euler.z));

            DrawGrip();

            if (GetActiveGrip(_bone) != null)
                return;

            GUILayout.Label($"{_bone} offset at frame {_frame}");
            GUI.changed = false;
            euler.x = AnimationTestView.Slider("Rotate X", euler.x, -180f, 180f);
            euler.y = AnimationTestView.Slider("Rotate Y", euler.y, -180f, 180f);
            euler.z = AnimationTestView.Slider("Rotate Z", euler.z, -180f, 180f);

            if (_bone == HumanBodyBones.Hips)
            {
                position.x = AnimationTestView.Slider("Move X", position.x, -0.5f, 0.5f);
                position.y = AnimationTestView.Slider("Move Y", position.y, -0.5f, 0.5f);
                position.z = AnimationTestView.Slider("Move Z", position.z, -0.5f, 0.5f);
            }

            if (GUI.changed)
                ChangeKey(_bone, new BoneKey(_frame, Quaternion.Euler(euler), position));

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Key"))
                SetKeys(false);

            if (GUILayout.Button("Reset"))
                ResetBone();

            if (GUILayout.Button("Delete key"))
                DeleteKeys(false);

            if (GUILayout.Button("Clear track"))
                ClearTrack();

            GUILayout.EndHorizontal();
        }

        private void DrawGrip()
        {
            if (_bone != HumanBodyBones.LeftHand && _bone != HumanBodyBones.RightHand)
                return;

            HandGrip grip = _edit.FindGrip(_bone);

            if (grip == null)
            {
                bool isHolding = IsHolding(_bone);

                if (GUILayout.Button(isHolding ? "Regrip: hand slides, weapon stays" : "Pin to weapon (other hand)"))
                    Pin(isHolding);

                return;
            }

            GUILayout.Label(grip.IsHolding ? "Regrip: drag the hand, the weapon keeps its path" : $"Pinned to the weapon in {grip.Anchor}: drag the hand to move the grip");
            GUI.changed = false;
            int start = FrameSlider("From frame", grip.Start);
            int end = FrameSlider("To frame", grip.End);

            if (GUI.changed)
            {
                BeginChange();
                grip.SetRange(start, end);
                _dirty.Add(_edit.Clip);
            }

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Range from here"))
                SetGripRange(grip, _frame, grip.End);

            if (GUILayout.Button("Range to here"))
                SetGripRange(grip, grip.Start, _frame);

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();

            if (GUILayout.Button(grip.IsHolding ? "Reset grip" : "Re-pin here"))
            {
                PushUndo();
                grip.Capture(_animator);
                _dirty.Add(_edit.Clip);
            }

            if (GUILayout.Button(grip.IsHolding ? "Remove regrip" : "Unpin"))
            {
                PushUndo();
                _edit.RemoveGrip(_bone);
                _dirty.Add(_edit.Clip);
            }

            GUILayout.EndHorizontal();
        }

        private int FrameSlider(string label, int frame)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label} {frame}", GUILayout.Width(100f));
            frame = Mathf.RoundToInt(GUILayout.HorizontalSlider(frame, 0f, _frameCount));
            GUILayout.EndHorizontal();

            return frame;
        }

        private void SetGripRange(HandGrip grip, int start, int end)
        {
            PushUndo();
            grip.SetRange(start, end);
            _dirty.Add(_edit.Clip);
        }

        private static bool IsEditable(AnimationClip clip)
        {
#if UNITY_EDITOR
            return AnimationEditBaker.IsEditable(clip);
#else
            return false;
#endif
        }

        private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(point - a, ab) / ab.sqrMagnitude) : 0f;

            return Vector2.Distance(point, a + ab * t);
        }

        private static void Fill(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (!_isOpen || _edit == null || _isShowingBaked || camera != _camera)
                return;

            _material.SetPass(0);
            GL.PushMatrix();
            GL.LoadProjectionMatrix(camera.projectionMatrix);
            GL.modelview = camera.worldToCameraMatrix;
            GL.Begin(GL.LINES);

            foreach (HumanBodyBones bone in _bones)
            {
                if (!_isSkeletonVisible || !IsLayerBone(bone))
                    continue;

                Vector3 position = GetBone(bone).position;
                BoneTrack track = _edit.Find(bone);
                Color color = bone == _bone ? s_activeColor : track == null || track.Keys.Count == 0 ? Color.white : track.IndexOf(_frame) >= 0
                    ? s_keyColor : s_trackColor;

                if (_parents[bone] != NoBone)
                    DrawLine(GetBone(_parents[bone]).position, position, new Color(1f, 1f, 1f, 0.5f));

                float size = GetRingRadius(position, 0) * JointScale * 2f;
                Vector3 right = camera.transform.right * size;
                Vector3 up = camera.transform.up * size;
                DrawLine(position + right, position + up, color);
                DrawLine(position + up, position - right, color);
                DrawLine(position - right, position - up, color);
                DrawLine(position - up, position + right, color);
            }

            foreach (HandGrip grip in _edit.Grips)
            {
                if (grip.GetWeight(_framePosition) > 0f)
                    DrawLine(HandGrip.GetSocket(_animator, grip.Anchor).position, GetBone(grip.Hand).position, s_gripColor);
            }

            if (_bone != NoBone && _isMoving && CanMove(_bone))
                DrawMoveGizmo(GetBone(_bone).position, camera.transform, _dragAxis >= 0 ? _dragAxis : _hoverAxis);
            else if (_bone != NoBone && !_isMoving)
            {
                Transform bone = GetBone(_bone);
                int active = _dragAxis >= 0 ? _dragAxis : _hoverAxis;

                for (int axis = 0; axis <= ViewAxis; axis++)
                {
                    BuildRing(bone.position, GetRingNormal(bone, axis), GetRingRadius(bone.position, axis));

                    for (int i = 0; i < RingSegments; i++)
                        DrawLine(_ring[i], _ring[i + 1], axis == active ? s_activeColor : s_axisColors[axis]);
                }
            }

            GL.End();
            GL.PopMatrix();
        }

        private void DrawMoveGizmo(Vector3 position, Transform camera, int active)
        {
            float radius = GetRingRadius(position, 0);
            float head = radius * 0.12f;

            for (int axis = 0; axis < ViewAxis; axis++)
            {
                Vector3 direction = GetMoveAxis(axis);
                Vector3 tip = position + direction * radius;
                Vector3 side = Vector3.Cross(direction, camera.forward).normalized * head;
                Color color = axis == active ? s_activeColor : s_axisColors[axis];
                DrawLine(position, tip, color);
                DrawLine(tip, tip - direction * head * 2f + side, color);
                DrawLine(tip, tip - direction * head * 2f - side, color);
            }

            Vector3 right = camera.right * head;
            Vector3 up = camera.up * head;
            Color center = active == ViewAxis ? s_activeColor : s_axisColors[ViewAxis];
            DrawLine(position - right - up, position + right - up, center);
            DrawLine(position + right - up, position + right + up, center);
            DrawLine(position + right + up, position - right + up, center);
            DrawLine(position - right + up, position - right - up, center);
        }

        private static void DrawLine(Vector3 from, Vector3 to, Color color)
        {
            GL.Color(color);
            GL.Vertex(from);
            GL.Vertex(to);
        }
    }
}
