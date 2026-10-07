using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Battle
{
    /// Offline animation browser: plays any state of the fighter controller on the bare character with any weapon in the
    /// hands, seen from an orbit camera or through the fighter's own eyes. No network session is involved.
    public sealed class AnimationTestView : MonoBehaviour
    {
        public const int BaseLayer = 0;
        public const int UpperLayer = 1;
        public const float PanelWidth = 250f;
        public const float Margin = 8f;
        public const float GuiHeight = 1080f;

        public int Layer => _layer;
        public bool IsPaused => _isPaused;
        public bool IsCurrent => IsPlaying(_layer, out _);
        public WeaponConfig Weapon => _weapons[_weaponIndex];
        /// A belt item is in the hand instead of the weapon.
        public bool HasItem => _itemIndex >= 0;

        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private BodyConfig _body;

        [Tooltip("Indexed by WeaponSocket")]
        [SerializeField]
        private Transform[] _sockets;

        [SerializeField]
        private WeaponConfig[] _weapons;

        [SerializeField]
        private HandItem[] _items;

        [SerializeField]
        private float _hitWeight = 0.7f;

        [SerializeField]
        private float _repeatPause = 0.4f;

        private const int HitLayer = 2;
        private const float Frame = 1f / 60f;
        private const float OrbitFieldOfView = 45f;
        private const float FirstPersonFieldOfView = 75f;
        private const float OrbitHeight = 1.1f;

        private static readonly string[] s_layerNames = { "Legs", "Upper body", "Hit reaction" };

        private static readonly string[][] s_states =
        {
            new[]
            {
                FighterAnimComponent.LocomotionState, FighterAnimComponent.AirState, FighterAnimComponent.JumpState,
                FighterAnimComponent.LandState, FighterAnimComponent.RestState, FighterAnimComponent.DeathState
            },
            new[]
            {
                FighterAnimComponent.CastState, FighterAnimComponent.CastFirstPersonState, FighterAnimComponent.CastReleaseState,
                FighterAnimComponent.UseState, FighterAnimComponent.UseFirstPersonState, FighterAnimComponent.HoldState, FighterAnimComponent.InteractState,
                FighterAnimComponent.ThrowState, FighterAnimComponent.OpenState, FighterAnimComponent.PickUpState,
                FighterAnimComponent.BandageState, FighterAnimComponent.BandageFirstPersonState
            },
            new[] { FighterAnimComponent.HitChestState, FighterAnimComponent.HitHeadState, FighterAnimComponent.HitStaggerState }
        };

        private readonly List<GameObject> _attachments = new();
        private readonly List<string> _weaponStates = new();
        private readonly string[] _current = { FighterAnimComponent.LocomotionState, string.Empty, string.Empty };
        private Transform[] _spineBones;
        private Transform _headBone;
        private MeleeAttackConfig _attack;
        private Vector2 _weaponScroll;
        private Vector2 _stateScroll;
        private int _weaponIndex;
        private int _itemIndex = -1;
        private int _layer = UpperLayer;
        private float _speed = 1f;
        private float _moveX;
        private float _moveY;
        private float _crouch;
        private float _pitch;
        private float _yaw = 150f;
        private float _tilt = 12f;
        private float _distance = 3.6f;
        private bool _isPaused;
        private bool _isRepeating = true;
        private bool _hasFootwork = true;
        private bool _isFirstPerson;
        private bool _isEditing;
        private bool _isAuthoredSide;
        private bool _isViewportBlocked;

        private void Awake()
        {
            _spineBones = new[]
            {
                _animator.GetBoneTransform(HumanBodyBones.Spine),
                _animator.GetBoneTransform(HumanBodyBones.Chest),
                _animator.GetBoneTransform(HumanBodyBones.UpperChest)
            };
            _headBone = _animator.GetBoneTransform(HumanBodyBones.Head);
            SelectWeapon(0);
        }

        private void Update()
        {
            _animator.speed = _isPaused ? 0f : _speed;
            _animator.SetFloat(FighterAnimComponent.MoveXParam, _moveX);
            _animator.SetFloat(FighterAnimComponent.MoveYParam, _moveY);
            _animator.SetFloat(FighterAnimComponent.CrouchParam, _crouch);
            _animator.SetFloat(FighterAnimComponent.ActionSpeedParam, 1f);
            _animator.SetBool(FighterAnimComponent.MirrorParam, _weapons[_weaponIndex].IsMirrored && !_isEditing && !HasItem);
            _animator.SetBool(FighterAnimComponent.FlipParam, !_isEditing);
            _animator.SetLayerWeight(UpperLayer, _current[BaseLayer] == FighterAnimComponent.DeathState ? 0f : 1f);
            _animator.SetLayerWeight(HitLayer, IsPlaying(HitLayer, out AnimatorStateInfo hit) && hit.normalizedTime < 1f ? _hitWeight : 0f);

            if (_isRepeating && IsPlaying(_layer, out AnimatorStateInfo info) && !info.loop && (info.normalizedTime - 1f) * GetLength(_layer) > _repeatPause)
                Play(_layer, _current[_layer]);

            UpdateOrbit();
        }

        private void LateUpdate()
        {
            bool isAuthoredSide = _weapons[_weaponIndex].IsMirrored && _isEditing && !HasItem;

            if (isAuthoredSide != _isAuthoredSide)
                PlaceAttachments(isAuthoredSide);

            if (_weapons[_weaponIndex].IsMirrored && !_isEditing && !HasItem)
                SocketMirror.Apply(_sockets);

            Transform root = _animator.transform;
            Quaternion step = Quaternion.AngleAxis(_pitch / _spineBones.Length, root.right);

            foreach (Transform bone in _spineBones)
                bone.rotation = step * bone.rotation;

            _headBone.localScale = _isFirstPerson ? Vector3.zero : Vector3.one;

            WeaponConfig weapon = _weapons[_weaponIndex];
            string upper = _current[UpperLayer];
            bool isDrawn = upper.EndsWith(FighterAnimComponent.DrawSuffix);
            bool hasArrow = isDrawn || upper.EndsWith(FighterAnimComponent.IdleSuffix);
            bool isActive = GetPhase(GetTime(UpperLayer)) == AttackPhase.Active;
            Vector3 drawHand = _sockets[(int)(weapon.IsMirrored ? WeaponSocket.LeftHand : WeaponSocket.RightHand)].position;

            foreach (GameObject attachment in _attachments)
            {
                if (!attachment.TryGetComponent(out WeaponVisual visual))
                    continue;

                visual.SetTrailActive(isActive);
                visual.SetDraw(isDrawn, drawHand, hasArrow);
                visual.SetClosed(upper.Contains(FighterAnimComponent.AttackSuffix) && visual.IsShutInAttack(GetTime(UpperLayer)) ||
                                 upper.Contains(FighterAnimComponent.BlockSuffix) ||
                                 upper.EndsWith(FighterAnimComponent.DeflectSuffix));
            }

            if (_isFirstPerson)
            {
                _camera.fieldOfView = FirstPersonFieldOfView;
                _camera.transform.SetPositionAndRotation(GetEyeRay().origin, root.rotation * Quaternion.Euler(_pitch, 0f, 0f));

                return;
            }

            Vector3 target = root.position + Vector3.up * OrbitHeight;
            Quaternion orbit = Quaternion.Euler(_tilt, _yaw, 0f);
            _camera.fieldOfView = OrbitFieldOfView;
            _camera.transform.SetPositionAndRotation(target - orbit * Vector3.forward * _distance, orbit);
        }

        private void OnGUI()
        {
            float scale = Screen.height / GuiHeight;
            float width = Screen.width / scale;
            float height = GuiHeight - Margin * 2f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            GUILayout.BeginArea(new Rect(Margin, Margin, PanelWidth, height), GUI.skin.box);
            _weaponScroll = GUILayout.BeginScrollView(_weaponScroll);

            for (int i = 0; i < _weapons.Length; i++)
            {
                if (Button(_weapons[i].name, i == _weaponIndex && !HasItem))
                    SelectWeapon(i);
            }

            GUILayout.Space(Margin);
            GUILayout.Label("Item in hand");

            for (int i = 0; i < _items.Length; i++)
            {
                if (Button(_items[i].Prefab.name, i == _itemIndex))
                    SelectItem(i);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(PanelWidth + Margin * 2f, Margin, PanelWidth, height), GUI.skin.box);
            _stateScroll = GUILayout.BeginScrollView(_stateScroll);
            DrawStates(_weapons[_weaponIndex].name, UpperLayer, _weaponStates);

            for (int layer = 0; layer < s_states.Length; layer++)
                DrawStates(s_layerNames[layer], layer, s_states[layer]);

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(width - PanelWidth - Margin, Margin, PanelWidth, height), GUI.skin.box);
            DrawPlayback();
            GUILayout.EndArea();
        }

        /// Edits are made on the clip as authored, so the animation editor turns mirroring and off-hand flips off while it is open.
        public void SetEditing(bool isEditing) => _isEditing = isEditing;

        public void SetPaused(bool isPaused) => _isPaused = isPaused;

        /// The mouse is over panels drawn on top of the viewport (the animation editor), so the wheel scrolls them, not the camera.
        public void SetViewportBlocked(bool isBlocked) => _isViewportBlocked = isBlocked;

        public void Seek(float normalizedTime) => Seek(_layer, normalizedTime);

        /// Seeks a layer without moving the focus; attack footwork is seeked through its attack, so both stay in step.
        public void Seek(int layer, float normalizedTime)
        {
            _isPaused = true;

            if (layer == BaseLayer && _current[BaseLayer].Contains(FighterAnimComponent.AttackLegsSuffix))
                layer = UpperLayer;

            Play(layer, _current[layer], normalizedTime);
        }

        /// The clip with the largest weight, so a blend tree reports the take that shapes the pose.
        public AnimationClip GetClip(int layer)
        {
            AnimationClip clip = null;
            float weight = 0f;

            foreach (AnimatorClipInfo info in _animator.GetCurrentAnimatorClipInfo(layer))
            {
                if (info.weight > weight)
                    (clip, weight) = (info.clip, info.weight);
            }

            return clip;
        }

        /// The state info reports an infinite length while the animator is paused, the clip does not.
        public float GetLength(int layer)
        {
            AnimationClip clip = GetClip(layer);

            return clip != null ? clip.length : 1f;
        }

        public float GetTime(int layer)
        {
            AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(layer);

            return (info.loop ? Mathf.Repeat(info.normalizedTime, 1f) : Mathf.Clamp01(info.normalizedTime)) * GetLength(layer);
        }

        /// The line of sight in first person: from the eye through the crosshair.
        public Ray GetEyeRay()
        {
            Transform root = _animator.transform;

            return new Ray(root.TransformPoint(_body.TransformPoint(_body.EyePoint, _pitch, _crouch)),
                root.rotation * Quaternion.Euler(_pitch, 0f, 0f) * Vector3.forward);
        }

        /// Point in GUI units (1080 high, y down) between the side panels.
        public bool IsOverViewport(Vector2 point)
        {
            return point.x > (PanelWidth + Margin) * 2f && point.x < Screen.width * GuiHeight / Screen.height - PanelWidth - Margin;
        }

        private void SelectWeapon(int index)
        {
            ClearHands();
            _weaponStates.Clear();
            _weaponIndex = index;
            WeaponConfig weapon = _weapons[index];
            string prefix = weapon.AnimationPrefix;

            foreach (WeaponAttachment attachment in weapon.Attachments)
                _attachments.Add(Instantiate(attachment.Prefab, _sockets[(int)attachment.Socket], false));

            _isAuthoredSide = false;

            _layer = UpperLayer;
            AddWeaponState(prefix + FighterAnimComponent.IdleSuffix);

            for (int i = 0; i < weapon.Attacks.Length; i++)
                AddWeaponState(prefix + FighterAnimComponent.AttackSuffix + i);

            if (weapon.HasRiposte)
                AddWeaponState(prefix + FighterAnimComponent.RiposteSuffix);

            foreach (string suffix in new[]
                     {
                         FighterAnimComponent.BlockSuffix, FighterAnimComponent.BlockImpactSuffix, FighterAnimComponent.BlockLowerSuffix, FighterAnimComponent.DeflectSuffix,
                         FighterAnimComponent.DrawSuffix, FighterAnimComponent.ReleaseSuffix
                     })
                AddWeaponState(prefix + suffix);

            Play(UpperLayer, _weaponStates[0]);
        }

        /// A belt item takes the place of the weapon, as in the game, and the hand goes to the hold pose.
        private void SelectItem(int index)
        {
            ClearHands();
            _itemIndex = index;
            _attachments.Add(_items[index].Create(_animator));
            _layer = UpperLayer;
            Play(UpperLayer, FighterAnimComponent.HoldState);
        }

        /// While edited, a weapon the game plays mirrored shows its clip as authored, right-handed: its attachments go to
        /// the sockets of the other side, mirrored with them, as the game's mirrored clip and sockets would put them.
        private void PlaceAttachments(bool isAuthoredSide)
        {
            WeaponAttachment[] attachments = _weapons[_weaponIndex].Attachments;

            for (int i = 0; i < attachments.Length && i < _attachments.Count; i++)
            {
                WeaponSocket socket = isAuthoredSide ? SocketMirror.Mirror(attachments[i].Socket) : attachments[i].Socket;
                _attachments[i].transform.SetParent(_sockets[(int)socket], false);
                _attachments[i].transform.localScale = new Vector3(isAuthoredSide ? -1f : 1f, 1f, 1f);
            }

            _isAuthoredSide = isAuthoredSide;
        }

        private void ClearHands()
        {
            foreach (GameObject attachment in _attachments)
                Destroy(attachment);

            _attachments.Clear();
            _itemIndex = -1;
        }

        private void AddWeaponState(string state)
        {
            if (_animator.HasState(UpperLayer, Animator.StringToHash(state)))
                _weaponStates.Add(state);
        }

        /// An attack takes its footwork along, any other upper body state puts the legs back under the fighter.
        private void Play(int layer, string state, float normalizedTime = 0f)
        {
            _current[layer] = state;
            _animator.Play(state, layer, normalizedTime);

            if (layer != UpperLayer)
                return;

            WeaponConfig weapon = _weapons[_weaponIndex];
            string attackPrefix = weapon.AnimationPrefix + FighterAnimComponent.AttackSuffix;
            bool isAttack = state.StartsWith(attackPrefix);
            string legs = state.Replace(FighterAnimComponent.AttackSuffix, FighterAnimComponent.AttackLegsSuffix);
            _attack = !isAttack ? null : int.TryParse(state[attackPrefix.Length..], out int index) ? weapon.Attacks[index] : weapon.Riposte;

            if (isAttack && _hasFootwork && _animator.HasState(BaseLayer, Animator.StringToHash(legs)))
                _current[BaseLayer] = legs;
            else if (_current[BaseLayer].Contains(FighterAnimComponent.AttackLegsSuffix))
                _current[BaseLayer] = FighterAnimComponent.LocomotionState;
            else
                return;

            _animator.Play(_current[BaseLayer], BaseLayer, normalizedTime);
        }

        private bool IsPlaying(int layer, out AnimatorStateInfo info)
        {
            info = _animator.GetCurrentAnimatorStateInfo(layer);

            return _current[layer].Length > 0 && info.IsName(_current[layer]);
        }

        private AttackPhase GetPhase(float time)
        {
            if (_attack == null)
                return AttackPhase.None;

            return time < _attack.ActiveStart ? AttackPhase.Windup : time <= _attack.ActiveEnd ? AttackPhase.Active : AttackPhase.Recovery;
        }

        private void UpdateOrbit()
        {
            if (!_isViewportBlocked && IsOverViewport(Input.mousePosition * GuiHeight / Screen.height))
                _distance = Mathf.Clamp(_distance - Input.mouseScrollDelta.y * 0.3f, 1f, 8f);

            if (!Input.GetMouseButton(1))
                return;

            _yaw += Input.GetAxisRaw("Mouse X") * 4f;
            _tilt = Mathf.Clamp(_tilt - Input.GetAxisRaw("Mouse Y") * 4f, -20f, 85f);
        }

        private void DrawStates(string title, int layer, IReadOnlyList<string> states)
        {
            GUILayout.Label(title);

            foreach (string state in states)
            {
                if (!Button(state, _current[layer] == state))
                    continue;

                _layer = layer;
                Play(layer, state);
            }
        }

        private void DrawPlayback()
        {
            float length = GetLength(_layer);
            float time = GetTime(_layer);
            AttackPhase phase = _layer == UpperLayer ? GetPhase(time) : AttackPhase.None;

            GUILayout.Label($"{s_layerNames[_layer]}: {_current[_layer]}");
            GUILayout.Label($"{time:F2} / {length:F2} s   frame {Mathf.RoundToInt(time / Frame)}{(phase == AttackPhase.None ? string.Empty : "   " + phase)}");

            GUI.changed = false;
            float scrub = GUILayout.HorizontalSlider(time, 0f, length);

            if (GUI.changed)
                Seek(scrub / length);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("<"))
                Seek(Mathf.Max(0f, time - Frame) / length);

            if (GUILayout.Button(_isPaused ? "Play" : "Pause"))
                _isPaused = !_isPaused;

            if (GUILayout.Button(">"))
                Seek(Mathf.Min(length, time + Frame) / length);

            GUILayout.EndHorizontal();

            _speed = Slider("Speed", _speed, 0.05f, 2f);
            _isRepeating = GUILayout.Toggle(_isRepeating, "Repeat one-shot states");
            _hasFootwork = GUILayout.Toggle(_hasFootwork, "Footwork under attacks");

            GUILayout.Space(Margin);
            _isFirstPerson = GUILayout.Toggle(_isFirstPerson, "First person");
            _pitch = Slider("Pitch", _pitch, -90f, 90f);

            GUILayout.Space(Margin);
            GUILayout.Label("Locomotion");
            _moveX = Slider("Move X", _moveX, -1f, 1f);
            _moveY = Slider("Move Y", _moveY, -1f, 3.6f);
            _crouch = Slider("Crouch", _crouch, 0f, 1f);

            GUILayout.FlexibleSpace();
            GUILayout.Label("RMB drag: orbit, wheel: zoom");
        }

        public static bool Button(string text, bool isSelected)
        {
            GUI.color = isSelected ? Color.yellow : Color.white;
            bool isPressed = GUILayout.Button(text);
            GUI.color = Color.white;

            return isPressed;
        }

        public static float Slider(string label, float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label} {value:F2}", GUILayout.Width(100f));
            value = GUILayout.HorizontalSlider(value, min, max);
            GUILayout.EndHorizontal();

            return value;
        }
    }
}
