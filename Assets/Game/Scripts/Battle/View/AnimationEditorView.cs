using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Scripts.Battle
{
    /// Frame-by-frame bone posing on top of the animation test scene: pick a bone, turn it with the rotation rings and the change
    /// is keyed on the current frame of the clip under edit. Edited clips play from their untouched source with the keys turned on
    /// top; Save bakes the keys into the clip and keeps them in AnimationEditConfig, so the next animation rebuild puts them back.
    /// Grip points mark where hands take hold of the current weapon; a hand snapped to a point stays on it while the point moves.
    /// The weapon or item in a hand is posed like a bone: its keys move the hand socket that carries it.
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
        private const int BonesTab = 0;
        private const int PointsTab = 1;
        private const int ViewAxis = 3;
        private const int RingSegments = 64;
        private const float FrameRate = 60f;
        private const float GuiHeight = AnimationTestView.GuiHeight;
        private const float Margin = AnimationTestView.Margin;
        private const float HeaderHeight = 38f;
        private const float TimelineHeight = 140f;
        private const float InspectorWidth = 300f;
        private const float HelpWidth = 380f;
        private const float TrackLabelWidth = 96f;
        private const float RulerHeight = 18f;
        private const float RowHeight = 22f;
        private const float KeySize = 9f;
        private const float PickDistance = 10f;
        private const float ViewRingScale = 1.2f;
        private const float ReplayWindow = 2f;
        private const float JointScale = 0.05f;
        private const float PointScale = 0.3f;
        private const float PointReach = 0.8f;
        private const float PointSide = 0.2f;
        private const float PropMargin = 0.03f;
        private const float PasteTolerance = 0.05f;
        private const float RayLength = 3f;
        private const float RayStep = 0.25f;
        private const float RayTick = 0.015f;
        private const HumanBodyBones NoBone = HumanBodyBones.LastBone;

        private static readonly Vector3[] s_axes = { Vector3.right, Vector3.up, Vector3.forward };
        private static readonly Color[] s_axisColors = { new(1f, 0.3f, 0.3f), new(0.4f, 0.95f, 0.3f), new(0.3f, 0.6f, 1f), new(0.85f, 0.85f, 0.85f) };
        private static readonly Color s_activeColor = new(1f, 0.85f, 0.2f);
        private static readonly Color s_keyColor = new(1f, 0.55f, 0.15f);
        private static readonly Color s_trackColor = new(0.55f, 0.85f, 1f);
        private static readonly Color s_cursorColor = new(1f, 0.3f, 0.3f);
        private static readonly Color s_gripColor = new(1f, 0.35f, 1f);
        private static readonly Color s_rayColor = new(0.2f, 1f, 0.8f);
        private static readonly Color s_rangeColor = new(0.3f, 0.8f, 1f, 0.2f);
        private static readonly Color s_rowColor = new(1f, 1f, 1f, 0.03f);
        private static readonly string[] s_layerNames = { "Legs", "Upper body" };

        private static readonly string[] s_help =
        {
            "LMB joint / weapon / grip point — select,  empty space or Esc — deselect",
            "Shift+LMB — add a joint to the selection,  B — add the bones below", "Drag a ring — rotate (E),  drag an arrow — move (W)",
            "RMB — orbit,  wheel — zoom", "Space — play / pause,  , .  — frame,  [ ]  — previous / next key",
            "K — key selection,  Shift+K — key pose", "X / Del — delete key,  with Shift — the whole pose",
            "R — reset selection (or point rotation)", "C / V — copy / paste the selection (the whole pose if none)",
            "G — snap the selected hand to the nearest grip point",
            "Shift+drag the timeline — range,  I — in-betweens", "Z — undo,  Shift+Z — redo,  Tab — hide the editor"
        };

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
        /// Bones as local poses, sockets as their keys.
        private readonly List<(HumanBodyBones Bone, bool IsSocket, Pose Pose)> _clipboard = new();
        /// Every selected bone; _bone is the one that carries the gizmo.
        private readonly HashSet<HumanBodyBones> _selection = new();
        private readonly List<HumanBodyBones> _bones = new();
        private readonly Dictionary<HumanBodyBones, HumanBodyBones> _parents = new();
        private readonly Dictionary<HumanBodyBones, int> _depths = new();
        private readonly Dictionary<HumanBodyBones, string> _names = new();
        private readonly Vector3[] _ring = new Vector3[RingSegments + 1];
        /// Weapon sockets as the character is built (the palm of each hand); holding grips move them, so they are put back every frame.
        private readonly Dictionary<HumanBodyBones, Pose> _socketRests = new();
        private AnimatorOverrideController _overrides;
        private Material _material;
        private AnimationEditorSkin _skin;
        private JointLimits _limits;
        private ClipEdit _edit;
        private WeaponGrips _grips;
        private GripPoint _point;
        private HumanBodyBones _bone = NoBone;
        /// Hand whose socket is selected: the weapon or item in it is posed instead of a bone.
        private HumanBodyBones _prop = NoBone;
        private HumanBodyBones _carrier = NoBone;
        private Rect _headerRect;
        private Rect _timelineRect;
        private Rect _inspectorRect;
        private Rect _helpRect;
        private Vector2 _boneScroll;
        private Vector2 _pointScroll;
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
        private int _tab = BonesTab;
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
        private bool? _isWeaponMirrored;
        private bool _isMoving;
        private bool _isSkeletonVisible = true;
        private bool _arePointsVisible = true;
        private bool _isRayVisible = true;
        private bool _arePointsDirty;
        private bool _isHelpVisible;
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
            _limits = new JointLimits(_animator);

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
            StartCoroutine(ResetSocketsEachFrame());
        }

        private void OnDisable()
        {
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        }

        private void OnDestroy()
        {
            Destroy(_material);
            _skin?.Dispose();
            _limits?.Dispose();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
                _isOpen = !_isOpen;

            Vector2 mouse = Input.mousePosition;
            Vector2 point = new Vector2(mouse.x, Screen.height - mouse.y) * GuiHeight / Screen.height;
            bool isOverGui = _headerRect.Contains(point) || _timelineRect.Contains(point) || _inspectorRect.Contains(point) || _helpRect.Contains(point);
            _test.SetViewportBlocked(isOverGui);
            UpdateMirroring();

            if (Time.unscaledTime < _replayEnd && !_test.IsCurrent)
                _test.Seek(_editLayer, _replayTime);

            if (_test.Layer != _testLayer)
                _editLayer = (_testLayer = _test.Layer) == BaseLayer ? BaseLayer : UpperLayer;

            ClipEdit edit = GetEdit(Resolve(_test.GetClip(_editLayer)));

            // A range belongs to the clip it was dragged over.
            if (edit?.Clip != _edit?.Clip)
                _rangeStart = _rangeEnd = -1;

            _edit = edit;
            _frameCount = Mathf.Max(1, Mathf.RoundToInt(_test.GetLength(_editLayer) * FrameRate));
            _framePosition = _test.GetTime(_editLayer) * FrameRate;
            _frame = Mathf.RoundToInt(_framePosition);
            _grips = _config.GetGrips(_test.Weapon);
            _carrier = _grips.GetCarrier();

            if (_bone != NoBone && !IsLayerBone(_bone))
                Select(NoBone);

            if (_prop != NoBone && !IsHolding(_prop))
                _prop = NoBone;

            if (_point != null && (!HasPoints() || _grips.Find(_point.Name) != _point))
                _point = null;

            if (Input.GetMouseButtonUp(0))
                _isChanging = false;

            if (!_isOpen || _edit == null)
                return;

            UpdateNavigation();

            if (_isShowingBaked)
                return;

            UpdateGizmo(mouse, _test.IsOverViewport(point) && !isOverGui);

            // A drag in progress keeps its mode and its undo step.
            if (!Input.GetMouseButton(0))
                UpdateShortcuts();
        }

        /// Runs before AnimationTestView bends the spine for the look pitch and places the camera.
        private void LateUpdate()
        {
            if (!_isShowingBaked)
                ApplyEdits();
        }

        private void OnGUI()
        {
            float scale = Screen.height / GuiHeight;
            float width = Screen.width / scale;
            float left = (AnimationTestView.PanelWidth + Margin) * 2f + Margin;
            float right = width - AnimationTestView.PanelWidth - Margin * 2f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            _skin ??= new AnimationEditorSkin();

            _headerRect = new Rect(left, Margin, _isOpen ? right - left : 190f, HeaderHeight);
            _timelineRect = _inspectorRect = _helpRect = Rect.zero;
            GUILayout.BeginArea(_headerRect, _skin.Panel);
            GUILayout.BeginHorizontal();
            DrawHeader();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            if (!_isOpen)
                return;

            _timelineRect = new Rect(left, GuiHeight - Margin - TimelineHeight, right - left, TimelineHeight);
            GUILayout.BeginArea(_timelineRect, _skin.Panel);
            DrawTimelinePanel();
            GUILayout.EndArea();

            if (_isHelpVisible)
            {
                _helpRect = new Rect(left, _headerRect.yMax + Margin, HelpWidth, s_help.Length * 22f + 44f);
                GUILayout.BeginArea(_helpRect, _skin.Panel);
                GUILayout.Label("Shortcuts", _skin.Header);

                foreach (string line in s_help)
                    GUILayout.Label(line, _skin.Hint);

                GUILayout.EndArea();
            }

            if (_edit == null)
                return;

            DrawPointLabels(scale);
            _inspectorRect = new Rect(right - InspectorWidth, _headerRect.yMax + Margin, InspectorWidth, _timelineRect.y - _headerRect.yMax - Margin * 2f);
            GUILayout.BeginArea(_inspectorRect, _skin.Panel);
            DrawInspector();
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
                _names[bone] = Regex.Replace(bone.ToString(), "(?<=[a-z])(?=[A-Z])", " ");
            }
        }

        private void Override(ClipEdit edit)
        {
            if (edit.Clip == null || edit.Source == null)
                return;

            // A clip the game plays mirrored is shown baked: its edits cannot be laid on top of the mirrored pose.
            AnimationClip shown = _isShowingBaked || _test.IsMirrored(edit.Clip) ? null : edit.Source;

            // Every change rebinds the animator, which drops the state it plays for some frames. A clip without an override
            // reads back as itself.
            if (_overrides[edit.Clip] != (shown ?? edit.Clip))
                _overrides[edit.Clip] = shown;

            _clips[edit.Source] = edit.Clip;
        }

        private AnimationClip Resolve(AnimationClip clip)
        {
            return clip != null && _clips.TryGetValue(clip, out AnimationClip edited) ? edited : clip;
        }

        private ClipEdit GetEdit(AnimationClip clip)
        {
            if (clip == null || !IsEditable(clip) || _test.IsMirrored(clip))
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

        private void ResetSockets()
        {
            foreach (KeyValuePair<HumanBodyBones, Pose> rest in _socketRests)
                HandGrip.GetSocket(_animator, rest.Key).SetLocalPositionAndRotation(rest.Value.position, rest.Value.rotation);
        }

        /// The edits of both layers on top of the pose the animator has just written, held inside the joint limits as baking does.
        /// The sockets are not reset here: a generated clip keys them, and resetting them after the animator dropped the turn
        /// of the weapon in the hand that the clip carries there.
        private void ApplyEdits(bool hasHolds = true)
        {
            _limits.Capture();

            for (int layer = BaseLayer; layer <= UpperLayer; layer++)
                GetEdit(Resolve(_test.GetClip(layer)))?.Apply(_animator, _test.GetTime(layer) * FrameRate, hasHolds);

            _limits.Restrict();
        }

        /// The clip alone on the current frame: the animator writes it now, the edits come on top in LateUpdate.
        private void SampleClip()
        {
            ResetSockets();
            SetFrame(_frame);
            _animator.Update(0f);
        }

        /// Puts the sockets back at rest once the frame is drawn, before the animator writes the next one: a clip that keys
        /// them overwrites the rest, one that does not keeps it, and the edits never pile up on last frame's.
        private IEnumerator ResetSocketsEachFrame()
        {
            WaitForEndOfFrame end = new WaitForEndOfFrame();

            while (true)
            {
                yield return end;
                ResetSockets();
            }
        }

        private BoneKey Evaluate(HumanBodyBones bone, bool isSocket = false)
        {
            return _edit.Find(bone, isSocket)?.Evaluate(_frame) ?? new BoneKey(_frame, Quaternion.identity, Vector3.zero);
        }

        private bool HasTarget()
        {
            return _bone != NoBone || _prop != NoBone || _point != null;
        }

        /// What the key commands work on: the selected weapon socket or the selected bones.
        private bool IsSelected(BoneTrack track)
        {
            return track.IsSocket ? track.Bone == _prop : _selection.Contains(track.Bone);
        }

        private List<BoneTrack> GetSelectedTracks()
        {
            List<BoneTrack> tracks = new();

            if (_prop != NoBone)
                tracks.Add(_edit.GetOrAdd(_prop, true));

            foreach (HumanBodyBones bone in _selection)
                tracks.Add(_edit.GetOrAdd(bone));

            return tracks;
        }

        private string GetSelectionName()
        {
            return _prop != NoBone ? GetPropName(_prop) : _bone == NoBone ? "—" : _selection.Count > 1 ? $"{_names[_bone]}  +{_selection.Count - 1}" : _names[_bone];
        }

        private string GetPropName(HumanBodyBones hand)
        {
            return HandGrip.GetSocket(_animator, hand).GetChild(0).name.Replace("(Clone)", string.Empty);
        }

        /// Adding a bone that is already selected takes it out; the last one added carries the gizmo.
        private void Select(HumanBodyBones bone, bool isAdding = false)
        {
            if (!isAdding || bone == NoBone)
                _selection.Clear();

            if (bone != NoBone && !_selection.Add(bone))
            {
                _selection.Remove(bone);
                bone = NoBone;

                foreach (HumanBodyBones other in _selection)
                    bone = other;
            }

            _bone = bone;
            _prop = NoBone;
            _point = null;
            _tab = bone != NoBone ? BonesTab : _tab;
        }

        private void Select(GripPoint point)
        {
            Select(NoBone);
            _point = point;
            _tab = PointsTab;
        }

        private void SelectProp(HumanBodyBones hand)
        {
            Select(NoBone);
            _prop = hand;
            _tab = BonesTab;
        }

        /// Every bone below the selected ones joins them: a shoulder takes the whole arm.
        private void SelectBranch()
        {
            // The list is depth-first, so a parent is always decided before its children.
            foreach (HumanBodyBones bone in _bones)
            {
                if (IsLayerBone(bone) && _selection.Contains(_parents[bone]))
                    _selection.Add(bone);
            }
        }

        private static bool IsAdding()
        {
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        }

        private void SetFrame(int frame)
        {
            _framePosition = _frame = Mathf.Clamp(frame, 0, _frameCount);
            _test.Seek(_editLayer, Mathf.Clamp01(_frame / FrameRate / _test.GetLength(_editLayer)));
        }

        private void JumpToKey(int direction)
        {
            int target = -1;

            bool hasSelection = _bone != NoBone || _prop != NoBone;

            foreach (BoneTrack track in _edit.Tracks)
            {
                if (hasSelection && !IsSelected(track))
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

        private void UpdateNavigation()
        {
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

            if (Input.GetKeyDown(KeyCode.Escape))
                Select(NoBone);
        }

        private void UpdateShortcuts()
        {
            bool isShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (Input.GetKeyDown(KeyCode.K))
                SetKeys(isShift);

            if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.X))
                DeleteKeys(isShift);

            if (Input.GetKeyDown(KeyCode.W))
                _isMoving = true;

            if (Input.GetKeyDown(KeyCode.E))
                _isMoving = false;

            if (Input.GetKeyDown(KeyCode.R))
                ResetSelection();

            if (Input.GetKeyDown(KeyCode.C))
                CopyPose();

            if (Input.GetKeyDown(KeyCode.V))
                PastePose();

            if (Input.GetKeyDown(KeyCode.G))
                SnapToNearest();

            if (Input.GetKeyDown(KeyCode.B))
                SelectBranch();

            if (Input.GetKeyDown(KeyCode.I))
                GenerateInBetweens();

            if (Input.GetKeyDown(KeyCode.Z))
                StepHistory(isShift ? _redo : _undo, isShift ? _undo : _redo);
        }

        private void UpdateGizmo(Vector2 mouse, bool isOverView)
        {
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

            _hoverAxis = !isOverView || !HasTarget() ? -1 : !_isMoving ? PickAxis(mouse) : CanMove() ? PickMoveAxis(mouse) : -1;

            if (!isOverView || !Input.GetMouseButtonDown(0))
                return;

            if (_hoverAxis < 0)
            {
                Pick(mouse);

                return;
            }

            _dragAxis = _hoverAxis;
            _lastMouse = mouse;
            _moveTarget = GetTarget().position;
        }

        /// World pose the gizmo works on: the selected grip point, weapon socket or bone.
        private Pose GetTarget()
        {
            if (_point != null)
                return GetPointPose(_point);

            Transform target = _prop != NoBone ? HandGrip.GetSocket(_animator, _prop) : GetBone(_bone);

            return new Pose(target.position, target.rotation);
        }

        private void Rotate(Vector2 delta)
        {
            Pose target = GetTarget();
            Vector3 axis = _dragAxis < ViewAxis ? s_axes[_dragAxis] : Quaternion.Inverse(target.rotation) * _camera.transform.forward;
            Quaternion turn = Quaternion.AngleAxis(Vector2.Dot(delta, _dragTangent) / _gizmoRadius * Mathf.Rad2Deg, axis);

            if (_point != null)
            {
                SetPoint(_point.Pose.position, _point.Pose.rotation * turn);

                return;
            }

            bool isProp = _prop != NoBone;
            HumanBodyBones bone = isProp ? _prop : _bone;
            HandGrip grip = isProp ? null : GetActiveGrip(_bone);

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

            BoneKey key = Evaluate(bone, isProp);
            ChangeKey(bone, new BoneKey(_frame, key.Rotation * turn, key.Position), isProp);
        }

        /// A grip point slides over its weapon, a weapon shifts in its hand. The hips shift by themselves; any other joint is
        /// pulled by turning the bones above it, and the change is keyed as rotations of those bones. A hand or a foot bends its
        /// limb and keeps its own orientation.
        private void Move(Vector2 delta)
        {
            Transform camera = _camera.transform;
            Vector3 position = GetTarget().position;
            _moveTarget += _dragAxis < ViewAxis
                ? GetMoveAxis(_dragAxis) * (Vector2.Dot(delta, _dragTangent) * _dragScale)
                : (camera.right * delta.x + camera.up * delta.y) * (GetRingRadius(position, 0) / _gizmoRadius);

            if (_point != null)
            {
                SetPoint(GetCarrierSocket().InverseTransformPoint(_moveTarget), _point.Pose.rotation);

                return;
            }

            if (_prop != NoBone)
            {
                // The key shifts the socket in the frame of the hand as the keys pose it; a hold moves the hand itself away from
                // there, so that frame is taken back from the weapon.
                Transform socket = HandGrip.GetSocket(_animator, _prop);
                BoneKey prop = Evaluate(_prop, true);
                Quaternion hand = socket.rotation * Quaternion.Inverse(_socketRests[_prop].rotation * prop.Rotation);
                ChangeKey(_prop, new BoneKey(_frame, prop.Rotation, prop.Position + Quaternion.Inverse(hand) * (_moveTarget - socket.position)), true);

                return;
            }

            Transform bone = GetBone(_bone);
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

        /// Points and weapons always move; a bone only when every bone the move would key belongs to the edited layer.
        private bool CanMove()
        {
            if (_point != null || _prop != NoBone || _bone == HumanBodyBones.Hips)
                return true;

            HumanBodyBones[] chain = GetMoveChain(_bone);

            return chain.Length > 0 && Array.TrueForAll(chain, IsLayerBone);
        }

        /// Character axes for bones, weapon axes for weapons and grip points (Z runs along the blade).
        private Vector3 GetMoveAxis(int axis)
        {
            return (_bone == NoBone ? GetTarget().rotation : _animator.transform.rotation) * s_axes[axis];
        }

        /// Axis arrows; the square in the middle moves in the view plane.
        private int PickMoveAxis(Vector2 mouse)
        {
            Vector3 position = GetTarget().position;
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

        /// Index of the ring under the mouse (local X, Y, Z or the view ring) and the screen direction that turns it forward.
        private int PickAxis(Vector2 mouse)
        {
            Pose target = GetTarget();
            float best = PickDistance;
            int axis = -1;

            for (int i = 0; i <= ViewAxis; i++)
            {
                Vector3 normal = GetRingNormal(target.rotation, i);
                BuildRing(target.position, normal, GetRingRadius(target.position, i));

                for (int j = 0; j < RingSegments; j++)
                {
                    Vector3 a = _camera.WorldToScreenPoint(_ring[j]);
                    Vector3 b = _camera.WorldToScreenPoint(_ring[j + 1]);
                    float distance = DistanceToSegment(mouse, a, b);

                    if (a.z <= 0f || distance >= best)
                        continue;

                    best = distance;
                    axis = i;
                    Vector3 tangent = Vector3.Cross(normal, _ring[j] - target.position) * 0.05f;
                    _dragTangent = ((Vector2)_camera.WorldToScreenPoint(_ring[j] + tangent) - (Vector2)a).normalized;
                }
            }

            return axis;
        }

        /// Joints of the edited layer and grip points of the weapon, then the weapon itself; clicking past all of them clears
        /// the selection.
        private void Pick(Vector2 mouse)
        {
            HumanBodyBones bone = NoBone;
            GripPoint point = null;
            float best = PickDistance * 1.5f;

            foreach (HumanBodyBones candidate in _bones)
            {
                if (IsLayerBone(candidate) && TryGetScreenDistance(mouse, GetBone(candidate).position, best, out float distance))
                    (bone, best) = (candidate, distance);
            }

            if (ArePointsShown())
            {
                foreach (GripPoint candidate in _grips.Points)
                {
                    if (TryGetScreenDistance(mouse, GetPointPose(candidate).position, best, out float distance))
                        (point, best) = (candidate, distance);
                }
            }

            HumanBodyBones prop = point == null && bone == NoBone ? PickProp(mouse) : NoBone;

            if (point != null)
                Select(point);
            else if (prop != NoBone)
                SelectProp(prop);
            else if (bone != NoBone || !IsAdding())
                Select(bone, IsAdding());
        }

        /// The hand whose weapon or item is under the mouse, by the boxes of its meshes.
        private HumanBodyBones PickProp(Vector2 mouse)
        {
            Ray ray = _camera.ScreenPointToRay(mouse);

            foreach (HumanBodyBones hand in _socketRests.Keys)
            {
                if (!IsHolding(hand))
                    continue;

                foreach (MeshRenderer renderer in HandGrip.GetSocket(_animator, hand).GetComponentsInChildren<MeshRenderer>())
                {
                    Transform mesh = renderer.transform;
                    Bounds bounds = renderer.localBounds;
                    bounds.Expand(PropMargin);

                    if (bounds.IntersectRay(new Ray(mesh.InverseTransformPoint(ray.origin), mesh.InverseTransformVector(ray.direction))))
                        return hand;
                }
            }

            return NoBone;
        }

        private bool TryGetScreenDistance(Vector2 mouse, Vector3 position, float limit, out float distance)
        {
            Vector3 point = _camera.WorldToScreenPoint(position);
            distance = Vector2.Distance(mouse, point);

            return point.z > 0f && distance < limit;
        }

        private Vector3 GetRingNormal(Quaternion rotation, int axis)
        {
            return axis < ViewAxis ? rotation * s_axes[axis] : _camera.transform.forward;
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

        /// Grip points need a weapon in a hand socket, and hands are posed on the upper body layer only.
        private bool HasPoints()
        {
            return _carrier != NoBone && _editLayer == UpperLayer && !_test.HasItem;
        }

        private bool ArePointsShown()
        {
            return _arePointsVisible && HasPoints();
        }

        private Transform GetCarrierSocket()
        {
            return HandGrip.GetSocket(_animator, _carrier);
        }

        private Pose GetPointPose(GripPoint point)
        {
            Transform socket = GetCarrierSocket();
            Pose pose = point.Pose;

            return new Pose(socket.TransformPoint(pose.position), socket.rotation * pose.rotation);
        }

        private void SetPoint(Vector3 position, Quaternion rotation)
        {
            _point.SetPose(position, rotation);
            _arePointsDirty = true;
            Relink(_point);
        }

        /// Hands snapped to a point follow it in every edited clip of the weapon.
        private void Relink(GripPoint point)
        {
            foreach (ClipEdit stored in _config.Clips)
                GetEdit(stored.Clip);

            foreach (ClipEdit edit in _edits.Values)
            {
                foreach (HandGrip grip in edit.Grips)
                {
                    if (!grip.IsSnapped(_test.Weapon, point))
                        continue;

                    grip.Snap(_test.Weapon, point, _socketRests[grip.Hand]);
                    _dirty.Add(edit.Clip);
                }
            }
        }

        /// The weapon-carrying hand slides over the weapon (hold), the other one is pinned to it; an existing grip keeps its range.
        private void Snap(HumanBodyBones hand, GripPoint point)
        {
            HandGrip grip = _edit.FindGrip(hand);
            bool isHolding = hand == _carrier;
            int start = grip?.Start ?? 0;
            int end = grip?.End ?? _frameCount;
            PushUndo();

            if (grip == null || grip.IsHolding != isHolding)
                _edit.SetGrip(grip = isHolding ? HandGrip.Hold(hand, _socketRests[hand], start, end) : HandGrip.Pin(_animator, hand, start, end));

            grip.Snap(_test.Weapon, point, _socketRests[hand]);
            _dirty.Add(_edit.Clip);
        }

        private void SnapToNearest()
        {
            if (!HasPoints() || !_socketRests.TryGetValue(_bone, out Pose palm))
                return;

            Vector3 position = GetBone(_bone).TransformPoint(palm.position);
            GripPoint nearest = null;
            float best = float.MaxValue;

            foreach (GripPoint point in _grips.Points)
            {
                float distance = (GetPointPose(point).position - position).sqrMagnitude;

                if (distance < best)
                    (nearest, best) = (point, distance);
            }

            if (nearest != null)
                Snap(_bone, nearest);
        }

        /// The point takes the palm of a hand as it is posed now.
        private void TakeFromHand(HumanBodyBones hand)
        {
            Transform bone = GetBone(hand);
            Transform socket = GetCarrierSocket();
            Pose palm = _socketRests[hand];
            SetPoint(socket.InverseTransformPoint(bone.TransformPoint(palm.position)), Quaternion.Inverse(socket.rotation) * bone.rotation * palm.rotation);
        }

        private void AddPoint()
        {
            Select(_grips.Add(_point != null ? _point.Pose.position + Vector3.forward * 0.1f : Vector3.zero));
            _arePointsDirty = true;
        }

        private void RemovePoint()
        {
            _grips.Remove(_point);
            _point = null;
            _arePointsDirty = true;
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

        /// The hand carries something in its weapon socket, and the edited layer poses hands.
        private bool IsHolding(HumanBodyBones hand)
        {
            return _socketRests.ContainsKey(hand) && IsLayerBone(hand) && HandGrip.GetSocket(_animator, hand).childCount > 0;
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

        private void ChangeKey(HumanBodyBones bone, BoneKey key, bool isSocket = false)
        {
            BeginChange();
            _edit.GetOrAdd(bone, isSocket).SetKey(key);
            _dirty.Add(_edit.Clip);
        }

        /// Pins the current offset of the selection, or of every animated track, as keys on this frame.
        private void SetKeys(bool isPose)
        {
            if (!isPose && _bone == NoBone && _prop == NoBone)
                return;

            PushUndo();

            foreach (BoneTrack track in isPose ? _edit.Tracks : GetSelectedTracks())
                track.SetKey(track.Evaluate(_frame));

            _dirty.Add(_edit.Clip);
        }

        private void DeleteKeys(bool isPose)
        {
            List<BoneTrack> keyed = new();

            foreach (BoneTrack track in _edit.Tracks)
            {
                if ((isPose || IsSelected(track)) && track.IndexOf(_frame) >= 0)
                    keyed.Add(track);
            }

            if (keyed.Count == 0)
                return;

            PushUndo();

            foreach (BoneTrack track in keyed)
                track.RemoveKey(_frame);

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
            return _isDraggingPose || IsSelected(track);
        }

        /// R: the selection back to the clip on this frame, or the selected grip point back to the hand's own orientation.
        private void ResetSelection()
        {
            if (_point != null)
            {
                SetPoint(_point.Pose.position, Quaternion.identity);

                return;
            }

            if (_bone == NoBone && _prop == NoBone)
                return;

            PushUndo();

            foreach (BoneTrack track in GetSelectedTracks())
                track.SetKey(new BoneKey(_frame, Quaternion.identity, Vector3.zero));

            _dirty.Add(_edit.Clip);
        }

        private void ClearTrack()
        {
            PushUndo();
            _edit.RemoveTracks(IsSelected);
            _dirty.Add(_edit.Clip);
        }

        /// Copies the selected bones, or the whole layer when none is selected, as they stand on this frame: keys and pins, but
        /// not holds, which move the hand over the weapon on their own frames. A hand takes the keys of its weapon along; a
        /// selected weapon is copied alone.
        private void CopyPose()
        {
            _clipboard.Clear();

            if (_prop != NoBone)
            {
                CopySocket(_prop);

                return;
            }

            SampleClip();
            ApplyEdits(false);

            foreach (HumanBodyBones bone in _bones)
            {
                if (!IsLayerBone(bone) || (_selection.Count > 0 && !_selection.Contains(bone)))
                    continue;

                Transform transform = GetBone(bone);
                _clipboard.Add((bone, false, new Pose(transform.localPosition, transform.localRotation)));

                if (_socketRests.ContainsKey(bone))
                    CopySocket(bone);
            }
        }

        private void CopySocket(HumanBodyBones hand)
        {
            BoneKey key = Evaluate(hand, true);
            _clipboard.Add((hand, true, new Pose(key.Position, key.Rotation)));
        }

        /// Puts the copied bones into the same pose on this frame, whatever the clip does here: each key is the turn from the
        /// clip to the copy. Works across clips; bones that already stand as copied get no key.
        private void PastePose()
        {
            if (_clipboard.Count == 0)
                return;

            SampleClip();
            PushUndo();

            foreach ((HumanBodyBones bone, bool isSocket, Pose pose) in _clipboard)
            {
                if (!IsLayerBone(bone))
                    continue;

                Transform clip = GetBone(bone);
                BoneKey current = Evaluate(bone, isSocket);
                BoneKey key = isSocket
                    ? new BoneKey(_frame, pose.rotation, pose.position)
                    : new BoneKey(_frame, Quaternion.Inverse(clip.localRotation) * pose.rotation,
                        bone == HumanBodyBones.Hips ? pose.position - clip.localPosition : current.Position);

                if (Quaternion.Angle(key.Rotation, current.Rotation) > PasteTolerance || (key.Position - current.Position).sqrMagnitude > 1e-8f)
                    _edit.GetOrAdd(bone, isSocket).SetKey(key);
            }

            _dirty.Add(_edit.Clip);
        }

        /// Replaces the motion inside the selected range with a smooth blend between the full poses (clip x key) at its ends: every
        /// moving bone of the layer and every keyed weapon socket gets a key on every frame of the range. Ease stops at the ends,
        /// otherwise the speed outside carries in.
        private void GenerateInBetweens()
        {
            int start = Mathf.Min(_rangeStart, _rangeEnd);
            int end = Mathf.Min(Mathf.Max(_rangeStart, _rangeEnd), _frameCount);

            if (start < 0 || end - start < 2)
                return;

            int first = Mathf.Max(0, start - 1);
            int last = Mathf.Min(_frameCount, end + 1);
            int frame = _frame;
            List<(HumanBodyBones Bone, bool IsSocket)> targets = _bones.FindAll(IsLayerBone).ConvertAll(bone => (bone, false));

            // A socket rests in its hand as far as the clip goes, so the blend of its keys is all the motion it gets.
            foreach (HumanBodyBones hand in _socketRests.Keys)
            {
                if (IsLayerBone(hand))
                    targets.Add((hand, true));
            }

            Pose[,] clip = new Pose[last - first + 1, targets.Count];

            // The clip alone: edits are applied in LateUpdate, after the animator has been sampled here.
            for (int f = first; f <= last; f++)
            {
                SetFrame(f);
                _animator.Update(0f);

                for (int i = 0; i < targets.Count; i++)
                {
                    Transform bone = GetBone(targets[i].Bone);
                    clip[f - first, i] = targets[i].IsSocket ? _socketRests[targets[i].Bone] : new Pose(bone.localPosition, bone.localRotation);
                }
            }

            SetFrame(frame);
            _animator.Update(0f);
            PushUndo();

            int[] ends = { first, start, end, last };

            for (int i = 0; i < targets.Count; i++)
            {
                BoneTrack track = _edit.Find(targets[i].Bone, targets[i].IsSocket);
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

                track = _edit.GetOrAdd(targets[i].Bone, targets[i].IsSocket);

                foreach (BoneKey key in keys)
                    track.SetKey(key);
            }

            _dirty.Add(_edit.Clip);
        }

        /// Back to the saved state of the current clip; grip points are not part of a clip and stay.
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
            ResetSockets();

            foreach (AnimationClip clip in _dirty)
            {
                AnimationEditBaker.Save(_config, _edits[clip], _animator);
                Override(_edits[clip]);
            }

            if (_arePointsDirty)
                AnimationEditBaker.SaveConfig(_config);

            _dirty.Clear();
            _arePointsDirty = false;
#endif
        }

        /// The editor shows what the game shows: switching to or from a left-handed weapon swaps which clips play baked. The
        /// first frame sets the overrides once more, now that the test view knows the states flipped for good.
        private void UpdateMirroring()
        {
            bool isMirrored = _test.Weapon.IsMirrored && !_test.HasItem;

            if (isMirrored == _isWeaponMirrored)
                return;

            _isWeaponMirrored = isMirrored;
            Replay();

            foreach (ClipEdit edit in _config.Clips)
                Override(edit);
        }

        private void ToggleBaked()
        {
            Replay();
            _isShowingBaked = !_isShowingBaked;
            _dragAxis = _hoverAxis = -1;

            foreach (ClipEdit edit in _config.Clips)
                Override(edit);
        }

        /// Changed overrides and reimported clips rebind the animator back to its default states some frames later; until then
        /// the current state is put back whenever it gets dropped. Called before the change, while the clip length is still right.
        private void Replay()
        {
            // A replay still under way keeps its frame: until the state is back the animator reports the start of the clip.
            if (Time.unscaledTime >= _replayEnd)
                _replayTime = Mathf.Clamp01(_frame / FrameRate / _test.GetLength(_editLayer));

            _replayEnd = Time.unscaledTime + ReplayWindow;
            _test.Seek(_editLayer, _replayTime);
        }

        private void DrawHeader()
        {
            if (_skin.Button(_isOpen ? "Close editor  Tab" : "Animation editor  Tab", _isOpen, 170f))
                _isOpen = !_isOpen;

            if (!_isOpen)
                return;

            AnimationClip clip = _test.GetClip(_editLayer);
            GUILayout.Space(8f);
            GUILayout.Label(_edit == null ? $"{(clip != null ? clip.name : "-")}  ({(_test.IsMirrored(Resolve(clip)) ? "mirrored in the game, " : string.Empty)}read-only)" : _edit.Clip.name + (_dirty.Contains(_edit.Clip) ? "  *" : string.Empty),
                _skin.Header, GUILayout.MinWidth(120f));
            GUILayout.Label($"frame {_frame} / {_frameCount}", _skin.Label, GUILayout.Width(110f));
            GUILayout.FlexibleSpace();

            for (int layer = BaseLayer; layer <= UpperLayer; layer++)
            {
                if (_skin.Button(s_layerNames[layer], _editLayer == layer, 90f))
                    _editLayer = layer;
            }

            GUILayout.Space(12f);
            GUI.enabled = _edit != null && !_isShowingBaked;

            if (_skin.Button("Rotate  E", !_isMoving, 84f))
                _isMoving = false;

            if (_skin.Button("Move  W", _isMoving, 84f))
                _isMoving = true;

            GUILayout.Space(12f);
            GUI.enabled = _edit != null && _dirty.Contains(_edit.Clip);

            if (_skin.Button("Revert", false, 70f))
                Revert();

            GUI.enabled = _dirty.Count > 0 || _arePointsDirty;

            if (_skin.Button(_dirty.Count > 1 ? $"Save {_dirty.Count} clips" : "Save", GUI.enabled, 100f))
                Save();

            GUI.enabled = true;
            _isHelpVisible = _skin.Toggle(_isHelpVisible, "?", 28f);
        }

        private void DrawTimelinePanel()
        {
            if (_edit == null)
            {
                GUILayout.Label($"{s_layerNames[_editLayer]}: only generated clips (.anim) can be edited, library takes are read-only. " +
                    "Pick another state or layer.", _skin.Hint);

                return;
            }

            GUILayout.BeginHorizontal();

            if (_skin.Button("|<", false, 34f))
                SetFrame(0);

            if (_skin.Button("<", false, 34f))
                SetFrame(_frame - 1);

            if (_skin.Button(_test.IsPaused ? "Play" : "Pause", !_test.IsPaused, 64f))
                _test.SetPaused(!_test.IsPaused);

            if (_skin.Button(">", false, 34f))
                SetFrame(_frame + 1);

            if (_skin.Button(">|", false, 34f))
                SetFrame(_frameCount);

            GUILayout.Space(8f);

            if (_skin.Button("[  Prev key", false, 86f))
                JumpToKey(-1);

            if (_skin.Button("Next key  ]", false, 86f))
                JumpToKey(1);

            GUILayout.Space(8f);
            GUI.enabled = _undo.Count > 0;

            if (_skin.Button("Undo", false, 56f))
                StepHistory(_undo, _redo);

            GUI.enabled = _redo.Count > 0;

            if (_skin.Button("Redo", false, 56f))
                StepHistory(_redo, _undo);

            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            _isRayVisible = _skin.Toggle(_isRayVisible, "Aim ray", 72f);
            _isSkeletonVisible = _skin.Toggle(_isSkeletonVisible, "Skeleton", 80f);
            GUI.enabled = HasPoints();
            _arePointsVisible = _skin.Toggle(_arePointsVisible, "Grip points", 92f);
            GUI.enabled = true;

            if (_skin.Button("Baked clip", _isShowingBaked, 90f))
                ToggleBaked();

            GUILayout.EndHorizontal();

            GUI.enabled = !_isShowingBaked;
            GUILayout.BeginHorizontal();

            if (_skin.Button("Key  K"))
                SetKeys(false);

            if (_skin.Button("Key pose  Shift+K"))
                SetKeys(true);

            if (_skin.Button("Delete key  X"))
                DeleteKeys(false);

            if (_skin.Button("Delete pose  Shift+X"))
                DeleteKeys(true);

            if (_skin.Button(_bone == NoBone && _prop == NoBone ? "Copy pose  C" : "Copy selection  C"))
                CopyPose();

            GUI.enabled = !_isShowingBaked && _clipboard.Count > 0;

            if (_skin.Button("Paste  V"))
                PastePose();

            GUI.enabled = !_isShowingBaked;

            GUILayout.FlexibleSpace();
            int start = Mathf.Min(_rangeStart, _rangeEnd);
            int end = Mathf.Max(_rangeStart, _rangeEnd);
            GUI.enabled = !_isShowingBaked && start >= 0 && end - start >= 2;

            if (_skin.Button(GUI.enabled ? $"In-betweens {start}-{end}  I" : "In-betweens: Shift+drag a range", false, 220f))
                GenerateInBetweens();

            GUI.enabled = !_isShowingBaked;
            _isEasing = _skin.Toggle(_isEasing, "Ease", 56f);
            GUILayout.EndHorizontal();

            GUI.enabled = true;
            GUILayout.Space(4f);
            DrawTimeline(GUILayoutUtility.GetRect(0f, RulerHeight + RowHeight * 2f, GUILayout.ExpandWidth(true)));
        }

        /// Ruler, a row with the keys of every track and a row with the keys of the selection. Dragging a key retimes it, dragging
        /// anywhere else scrubs.
        private void DrawTimeline(Rect area)
        {
            Rect track = new Rect(area.x + TrackLabelWidth, area.y, area.width - TrackLabelWidth - KeySize, area.height);
            Rect poseRow = new Rect(area.x, area.y + RulerHeight, area.width, RowHeight);
            Rect boneRow = new Rect(area.x, poseRow.yMax, area.width, RowHeight);
            int step = track.width / _frameCount >= 8f ? 5 : track.width / _frameCount >= 4f ? 10 : 30;

            AnimationEditorSkin.Fill(area, new Color(0f, 0f, 0f, 0.35f));
            AnimationEditorSkin.Fill(poseRow, s_rowColor);
            AnimationEditorSkin.Fill(new Rect(area.x + TrackLabelWidth - 4f, area.y, 1f, area.height), AnimationEditorSkin.Line);

            for (int frame = 0; frame <= _frameCount; frame += step)
            {
                float x = GetX(track, frame);
                bool isMajor = frame % (step * 2) == 0;
                AnimationEditorSkin.Fill(new Rect(x, area.y, 1f, isMajor ? area.height : 5f), new Color(1f, 1f, 1f, isMajor ? 0.08f : 0.2f));

                if (isMajor)
                    GUI.Label(new Rect(x + 2f, area.y, 40f, RulerHeight), frame.ToString(), _skin.Hint);
            }

            GUI.Label(new Rect(poseRow.x + 4f, poseRow.y, TrackLabelWidth, RowHeight), "All tracks", _skin.Label);
            GUI.Label(new Rect(boneRow.x + 4f, boneRow.y, TrackLabelWidth - 8f, RowHeight), GetSelectionName(), _skin.Label);

            HandGrip grip = _bone == NoBone ? null : _edit.FindGrip(_bone);

            if (grip != null)
                AnimationEditorSkin.Fill(new Rect(GetX(track, grip.Start), boneRow.y + 4f, GetX(track, grip.End) - GetX(track, grip.Start), boneRow.height - 8f),
                    new Color(s_gripColor.r, s_gripColor.g, s_gripColor.b, 0.35f));

            if (_rangeStart >= 0 && _rangeEnd != _rangeStart)
                AnimationEditorSkin.Fill(new Rect(GetX(track, Mathf.Min(_rangeStart, _rangeEnd)), area.y, Mathf.Abs(GetX(track, _rangeEnd) - GetX(track, _rangeStart)),
                    area.height), s_rangeColor);

            foreach (BoneTrack boneTrack in _edit.Tracks)
            {
                foreach (BoneKey key in boneTrack.Keys)
                {
                    DrawKey(track, poseRow, key.Frame, key.Frame == _frame ? s_keyColor : s_trackColor);

                    if (IsSelected(boneTrack))
                        DrawKey(track, boneRow, key.Frame, key.Frame == _frame ? s_keyColor : s_activeColor);
                }
            }

            float cursor = GetX(track, _framePosition);
            AnimationEditorSkin.Fill(new Rect(cursor - 1f, area.y, 2f, area.height), s_cursorColor);
            AnimationEditorSkin.Fill(new Rect(cursor - 14f, area.y, 28f, RulerHeight - 2f), s_cursorColor);
            GUI.Label(new Rect(cursor - 14f, area.y, 28f, RulerHeight - 2f), _frame.ToString(), _skin.Badge);
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
            Rect key = new Rect(GetX(track, frame) - KeySize * 0.5f, row.center.y - KeySize * 0.5f, KeySize, KeySize);
            AnimationEditorSkin.Fill(new Rect(key.x - 1f, key.y - 1f, key.width + 2f, key.height + 2f), Color.black);
            AnimationEditorSkin.Fill(key, color);
        }

        private void DrawInspector()
        {
            GUILayout.BeginHorizontal();

            if (_skin.Button("Bones", _tab == BonesTab))
                _tab = BonesTab;

            GUI.enabled = HasPoints();

            if (_skin.Button("Grip points", _tab == PointsTab && HasPoints()))
                _tab = PointsTab;

            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.Space(4f);

            if (_tab == PointsTab && HasPoints())
                DrawPointsTab();
            else
                DrawBonesTab();
        }

        private void DrawBonesTab()
        {
            GUILayout.Label($"{s_layerNames[_editLayer]} bones", _skin.Header);
            _boneScroll = GUILayout.BeginScrollView(_boneScroll);

            foreach (HumanBodyBones bone in _bones)
            {
                if (!IsLayerBone(bone))
                    continue;

                if (DrawRow(_names[bone], _selection.Contains(bone), _depths[bone], _edit.Find(bone), _edit.FindGrip(bone) != null))
                    Select(bone, IsAdding());

                // The weapon or item in a hand is listed under it.
                if (IsHolding(bone) && DrawRow(GetPropName(bone), bone == _prop, _depths[bone] + 1, _edit.Find(bone, true), false))
                    SelectProp(bone);
            }

            GUILayout.EndScrollView();

            bool isProp = _prop != NoBone;
            HumanBodyBones target = isProp ? _prop : _bone;

            if (target == NoBone || _isShowingBaked)
                return;

            _skin.Separator();
            GUILayout.Label($"{GetSelectionName()}  ·  frame {_frame}", _skin.Header);

            if (isProp)
                GUILayout.Label("Moves and turns in the hand. A regripped hand follows it, a pinned hand stays on it.", _skin.Hint);
            else if (_socketRests.ContainsKey(_bone))
                DrawGrip();

            if (!isProp && GetActiveGrip(_bone) != null)
                return;

            BoneKey key = Evaluate(target, isProp);
            Vector3 euler = ToSigned(key.Rotation.eulerAngles);
            Vector3 position = key.Position;
            GUI.changed = false;
            euler.x = _skin.Slider("Rotate X", euler.x, -180f, 180f);
            euler.y = _skin.Slider("Rotate Y", euler.y, -180f, 180f);
            euler.z = _skin.Slider("Rotate Z", euler.z, -180f, 180f);

            if (isProp || _bone == HumanBodyBones.Hips)
            {
                position.x = _skin.Slider("Move X", position.x, -0.5f, 0.5f, "F3");
                position.y = _skin.Slider("Move Y", position.y, -0.5f, 0.5f, "F3");
                position.z = _skin.Slider("Move Z", position.z, -0.5f, 0.5f, "F3");
            }

            if (GUI.changed)
                ChangeKey(target, new BoneKey(_frame, Quaternion.Euler(euler), position), isProp);

            GUILayout.BeginHorizontal();

            if (_skin.Button("Key"))
                SetKeys(false);

            if (_skin.Button("Reset"))
                ResetSelection();

            if (_skin.Button("Delete key"))
                DeleteKeys(false);

            if (_skin.Button("Clear track"))
                ClearTrack();

            GUILayout.EndHorizontal();

            if (!isProp && _skin.Button("Add the bones below to the selection  B"))
                SelectBranch();
        }

        /// List entry of a track: KEY — a key on this frame, anim — keys elsewhere, grip — the hand is on a weapon.
        private bool DrawRow(string text, bool isOn, int depth, BoneTrack track, bool hasGrip)
        {
            bool isKeyed = track != null && track.Keys.Count > 0;
            bool isKey = isKeyed && track.IndexOf(_frame) >= 0;
            string tag = (isKey ? "KEY" : isKeyed ? "anim" : string.Empty) + (hasGrip ? "  grip" : string.Empty);

            return _skin.Row(text, isOn, depth * 10f, tag, isKey ? s_keyColor : isKeyed ? s_trackColor : s_gripColor);
        }

        private void DrawGrip()
        {
            HandGrip grip = _edit.FindGrip(_bone);

            if (HasPoints())
            {
                GUILayout.Label("Snap to a grip point  (G — nearest)", _skin.Hint);
                GUILayout.BeginHorizontal();

                foreach (GripPoint point in _grips.Points)
                {
                    if (_skin.Button(point.Name, grip != null && grip.IsSnapped(_test.Weapon, point)))
                        Snap(_bone, point);
                }

                GUILayout.EndHorizontal();
            }

            if (grip == null)
            {
                bool isHolding = IsHolding(_bone);

                if (_skin.Button(isHolding ? "Free regrip: hand slides, weapon stays" : "Pin to the weapon as posed"))
                    Pin(isHolding);

                return;
            }

            GUILayout.Label(grip.Point.Length > 0 ? $"On point “{grip.Point}”: moves with it; dragging the hand frees it" :
                grip.IsHolding ? "Regrip: drag the hand, the weapon keeps its path" : $"Pinned to the weapon in {grip.Anchor}: drag the hand to move the grip",
                _skin.Hint);
            GUI.changed = false;
            int start = Mathf.RoundToInt(_skin.Slider("From", grip.Start, 0f, _frameCount, "F0"));
            int end = Mathf.RoundToInt(_skin.Slider("To", grip.End, 0f, _frameCount, "F0"));

            if (GUI.changed)
            {
                BeginChange();
                grip.SetRange(start, end);
                _dirty.Add(_edit.Clip);
            }

            GUILayout.BeginHorizontal();

            if (_skin.Button("From here"))
                SetGripRange(grip, _frame, grip.End);

            if (_skin.Button("To here"))
                SetGripRange(grip, grip.Start, _frame);

            if (_skin.Button(grip.IsHolding ? "Reset" : "Re-pin"))
            {
                PushUndo();
                grip.Capture(_animator);
                _dirty.Add(_edit.Clip);
            }

            if (_skin.Button("Release"))
            {
                PushUndo();
                _edit.RemoveGrip(_bone);
                _dirty.Add(_edit.Clip);
            }

            GUILayout.EndHorizontal();
            _skin.Separator();
        }

        private void DrawPointsTab()
        {
            GUILayout.Label($"{_test.Weapon.name}  ·  in the {(_carrier == HumanBodyBones.RightHand ? "right" : "left")} hand", _skin.Header);
            GUILayout.Label("Points sit in the weapon socket frame, Z runs along the blade. A snapped hand follows its point in every clip.",
                _skin.Hint);
            _pointScroll = GUILayout.BeginScrollView(_pointScroll, GUILayout.Height(Mathf.Min(_grips.Points.Count, 6) * 21f + 6f));

            foreach (GripPoint point in _grips.Points)
            {
                if (_skin.Row(point.Name, point == _point, 0f, GetSnappedHands(point), s_gripColor))
                    Select(point);
            }

            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();

            if (_skin.Button("Add point"))
                AddPoint();

            GUI.enabled = _point != null;

            if (_skin.Button("Remove"))
                RemovePoint();

            GUI.enabled = true;
            GUILayout.EndHorizontal();

            if (_point == null || _isShowingBaked)
                return;

            _skin.Separator();
            GUILayout.Label(_point.Name, _skin.Header);
            Pose pose = _point.Pose;
            Vector3 position = pose.position;
            Vector3 euler = ToSigned(pose.rotation.eulerAngles);
            GUI.changed = false;
            position.z = _skin.Slider("Along Z", position.z, -PointReach, PointReach, "F3");
            position.x = _skin.Slider("Side X", position.x, -PointSide, PointSide, "F3");
            position.y = _skin.Slider("Side Y", position.y, -PointSide, PointSide, "F3");
            euler.z = _skin.Slider("Roll Z", euler.z, -180f, 180f);
            euler.x = _skin.Slider("Tilt X", euler.x, -180f, 180f);
            euler.y = _skin.Slider("Tilt Y", euler.y, -180f, 180f);

            if (GUI.changed)
                SetPoint(position, Quaternion.Euler(euler));

            GUILayout.Label("Snap a hand  (magnet)", _skin.Hint);
            GUILayout.BeginHorizontal();

            if (_skin.Button("Left hand", IsSnapped(HumanBodyBones.LeftHand, _point)))
                Snap(HumanBodyBones.LeftHand, _point);

            if (_skin.Button("Right hand", IsSnapped(HumanBodyBones.RightHand, _point)))
                Snap(HumanBodyBones.RightHand, _point);

            GUILayout.EndHorizontal();
            GUILayout.Label("Take the point from a hand as posed", _skin.Hint);
            GUILayout.BeginHorizontal();

            if (_skin.Button("Left palm"))
                TakeFromHand(HumanBodyBones.LeftHand);

            if (_skin.Button("Right palm"))
                TakeFromHand(HumanBodyBones.RightHand);

            if (_skin.Button("Reset turn"))
                ResetSelection();

            GUILayout.EndHorizontal();
        }

        /// Point names next to their markers in the viewport, with the hands snapped to them in this clip.
        private void DrawPointLabels(float scale)
        {
            if (!ArePointsShown() || _isShowingBaked)
                return;

            foreach (GripPoint point in _grips.Points)
            {
                Vector3 screen = _camera.WorldToScreenPoint(GetPointPose(point).position);

                if (screen.z <= 0f)
                    continue;

                string hands = GetSnappedHands(point);
                GUI.color = point == _point ? s_activeColor : s_gripColor;
                GUI.Label(new Rect(screen.x / scale + 8f, (Screen.height - screen.y) / scale - 18f, 160f, 20f),
                    hands.Length > 0 ? $"{point.Name}  ·  {hands}" : point.Name, _skin.Header);
                GUI.color = Color.white;
            }
        }

        private bool IsSnapped(HumanBodyBones hand, GripPoint point)
        {
            HandGrip grip = _edit.FindGrip(hand);

            return grip != null && grip.IsSnapped(_test.Weapon, point);
        }

        private string GetSnappedHands(GripPoint point)
        {
            bool isLeft = IsSnapped(HumanBodyBones.LeftHand, point);
            bool isRight = IsSnapped(HumanBodyBones.RightHand, point);

            return isLeft && isRight ? "L R" : isLeft ? "L" : isRight ? "R" : string.Empty;
        }

        private void SetGripRange(HandGrip grip, int start, int end)
        {
            PushUndo();
            grip.SetRange(start, end);
            _dirty.Add(_edit.Clip);
        }

        private static Vector3 ToSigned(Vector3 euler)
        {
            return new Vector3(Mathf.DeltaAngle(0f, euler.x), Mathf.DeltaAngle(0f, euler.y), Mathf.DeltaAngle(0f, euler.z));
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

        private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (!_isOpen || camera != _camera)
                return;

            _material.SetPass(0);
            GL.PushMatrix();
            GL.LoadProjectionMatrix(camera.projectionMatrix);
            GL.modelview = camera.worldToCameraMatrix;
            GL.Begin(GL.LINES);

            if (_isRayVisible)
                DrawEyeRay();

            if (_edit != null && !_isShowingBaked)
                DrawEdit(camera.transform);

            GL.End();
            GL.PopMatrix();
        }

        /// The line the crosshair lies on, with a cross every quarter of a metre: seen through the eye they stack into the
        /// crosshair itself.
        private void DrawEyeRay()
        {
            Ray ray = _test.GetEyeRay();
            Vector3 right = _animator.transform.right * RayTick;
            Vector3 up = Vector3.Cross(ray.direction, right);
            DrawLine(ray.origin, ray.GetPoint(RayLength), s_rayColor);

            for (int i = 1; i * RayStep <= RayLength; i++)
            {
                Vector3 point = ray.GetPoint(i * RayStep);
                DrawLine(point - right, point + right, s_rayColor);
                DrawLine(point - up, point + up, s_rayColor);
            }
        }

        private void DrawEdit(Transform camera)
        {
            foreach (HumanBodyBones bone in _bones)
            {
                if (!_isSkeletonVisible || !IsLayerBone(bone))
                    continue;

                Vector3 position = GetBone(bone).position;
                BoneTrack track = _edit.Find(bone);
                Color color = _selection.Contains(bone) ? s_activeColor : track == null || track.Keys.Count == 0 ? Color.white : track.IndexOf(_frame) >= 0
                    ? s_keyColor : s_trackColor;

                if (_parents[bone] != NoBone)
                    DrawLine(GetBone(_parents[bone]).position, position, new Color(1f, 1f, 1f, 0.5f));

                DrawDiamond(position, camera, GetRingRadius(position, 0) * JointScale * 2f, color);
            }

            foreach (HandGrip grip in _edit.Grips)
            {
                if (grip.GetWeight(_framePosition) > 0f)
                    DrawLine(HandGrip.GetSocket(_animator, grip.Anchor).position, GetBone(grip.Hand).position, s_gripColor);
            }

            if (ArePointsShown())
            {
                foreach (GripPoint point in _grips.Points)
                    DrawPoint(GetPointPose(point), camera, point == _point ? s_activeColor : s_gripColor);
            }

            int active = _dragAxis >= 0 ? _dragAxis : _hoverAxis;

            if (HasTarget() && _isMoving && CanMove())
                DrawMoveGizmo(GetTarget().position, camera, active);
            else if (HasTarget() && !_isMoving)
            {
                Pose target = GetTarget();

                for (int axis = 0; axis <= ViewAxis; axis++)
                {
                    BuildRing(target.position, GetRingNormal(target.rotation, axis), GetRingRadius(target.position, axis));

                    for (int i = 0; i < RingSegments; i++)
                        DrawLine(_ring[i], _ring[i + 1], axis == active ? s_activeColor : s_axisColors[axis]);
                }
            }
        }

        /// Diamond on the point with its handle axis (Z, blue) and palm up axis (Y, green).
        private void DrawPoint(Pose pose, Transform camera, Color color)
        {
            float size = GetRingRadius(pose.position, 0) * PointScale;
            DrawDiamond(pose.position, camera, size * 0.3f, color);
            DrawLine(pose.position - pose.forward * size, pose.position + pose.forward * size, s_axisColors[2]);
            DrawLine(pose.position, pose.position + pose.up * size * 0.6f, s_axisColors[1]);
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

        private static void DrawDiamond(Vector3 position, Transform camera, float size, Color color)
        {
            Vector3 right = camera.right * size;
            Vector3 up = camera.up * size;
            DrawLine(position + right, position + up, color);
            DrawLine(position + up, position - right, color);
            DrawLine(position - right, position - up, color);
            DrawLine(position - up, position + right, color);
        }

        private static void DrawLine(Vector3 from, Vector3 to, Color color)
        {
            GL.Color(color);
            GL.Vertex(from);
            GL.Vertex(to);
        }
    }
}
