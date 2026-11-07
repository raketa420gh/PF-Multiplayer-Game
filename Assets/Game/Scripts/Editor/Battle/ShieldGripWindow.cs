using Game.Scripts.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Battle
{
    /// Sets how a shield sits in the off hand. Pose puts the character in the shield pair's idle with the shield standing where the
    /// clip has it and a Hand object on the left hand: move and turn it with the scene tools, the arm follows it, until the hand holds
    /// the shield as it should, then Save. A rebuild solves every clip with that shield for the grip: the board keeps its path, the
    /// hand and the elbow take the grip.
    internal sealed class ShieldGripWindow : EditorWindow
    {
        private static readonly string[] s_labels = { "Round Shield", "Écu" };
        private static readonly string[] s_shields = { ShieldGripConfig.RoundShield, ShieldGripConfig.Ecu };
        private static readonly string[] s_clips = { "SwordShield_Idle", "SwordEcu_Idle" };

        [SerializeField]
        private int _index;

        [SerializeField]
        private GameObject _root;

        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private Transform _target;

        [SerializeField]
        private Transform _shield;

        [SerializeField]
        private Pose _sampled;

        [SerializeField]
        private Quaternion _upperRest;

        [SerializeField]
        private Quaternion _lowerRest;

        [SerializeField]
        private Vector3 _elbow;

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGui;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGui;
            ClosePreview();
        }

        private void OnGUI()
        {
            int index = EditorGUILayout.Popup("Shield", _index, s_labels);

            if (index != _index)
            {
                _index = index;
                ClosePreview();
            }

            Pose stored = ShieldGripConfig.Get(s_shields[_index]);
            EditorGUILayout.LabelField("Stored", $"{stored.position.ToString("F3")}  {stored.rotation.eulerAngles.ToString("F1")}");

            if (GUILayout.Button("Pose"))
                CreatePreview();

            using (new EditorGUI.DisabledScope(_target == null))
            {
                if (GUILayout.Button("Select Hand"))
                    Selection.activeTransform = _target;

                if (GUILayout.Button("Save"))
                    Save();
            }

            if (GUILayout.Button("Reset To Default"))
            {
                ShieldGripConfig.Set(s_shields[_index], Pose.identity);
                ClosePreview();
            }

            if (GUILayout.Button("Close Preview"))
                ClosePreview();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Saved grips reach the clips only through a rebuild (minutes).", MessageType.Info);

            if (GUILayout.Button("Rebuild Animations And Content"))
            {
                ClosePreview();
                BattleBuildMenu.BuildAnimationsAndContent();
            }
        }

        [MenuItem("Tools/Game/Battle/Shield Grip")]
        public static void Open()
        {
            GetWindow<ShieldGripWindow>("Shield Grip");
        }

        /// The character holds the clip's first frame as a humanoid pose; the shield stands where its socket was, apart from the hand.
        private void CreatePreview()
        {
            ClosePreview();
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{BattleEditorUtility.AnimationsFolder}/{s_clips[_index]}.anim");
            _root = new GameObject("Shield Grip Preview");
            GameObject model = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(BattleEditorUtility.ModelPath), _root.transform, false);
            _animator = model.GetComponent<Animator>();
            Transform socket = BattlePoseRig.CreateSockets(_animator)[(int)WeaponSocket.LeftShield];
            HumanPose pose = new HumanPose();
            HumanPoseHandler handler = new HumanPoseHandler(_animator.avatar, _animator.transform);
            Pose board;

            AnimationMode.StartAnimationMode();

            try
            {
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(model, clip, 0f);
                AnimationMode.EndSampling();
                handler.GetHumanPose(ref pose);
                board = new Pose(socket.position, socket.rotation);
                _sampled = new Pose(socket.localPosition, socket.localRotation);
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }

            handler.SetHumanPose(ref pose);
            handler.Dispose();

            Transform hand = _animator.GetBoneTransform(HumanBodyBones.LeftHand);
            _upperRest = _animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).localRotation;
            _lowerRest = _animator.GetBoneTransform(HumanBodyBones.LeftLowerArm).localRotation;
            _elbow = _animator.GetBoneTransform(HumanBodyBones.LeftLowerArm).position;
            _shield = new GameObject("Shield").transform;
            _shield.SetParent(_root.transform, false);
            _shield.SetPositionAndRotation(board.position, board.rotation);
            Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>($"{BattleEditorUtility.PrefabsFolder}/{s_shields[_index]}.prefab"), _shield, false);
            _target = new GameObject("Hand").transform;
            _target.SetParent(_root.transform, false);
            _target.SetPositionAndRotation(hand.position, hand.rotation);
            _target.hasChanged = false;

            foreach (Transform part in _root.GetComponentsInChildren<Transform>(true))
                part.gameObject.hideFlags = HideFlags.DontSave;

            Selection.activeTransform = _target;
            SceneView view = SceneView.lastActiveSceneView;

            if (view != null)
                view.LookAt(_shield.position, Quaternion.LookRotation(new Vector3(0.6f, -0.3f, 1f)), 0.9f);
        }

        /// Two-bone IK from the posed arm: the elbow stays on the side it was, the hand takes the target's turn.
        private void SolveArm()
        {
            Transform upper = _animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            Transform lower = _animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            Transform hand = _animator.GetBoneTransform(HumanBodyBones.LeftHand);
            upper.localRotation = _upperRest;
            lower.localRotation = _lowerRest;

            Vector3 shoulder = upper.position;
            float upperLength = Vector3.Distance(shoulder, lower.position);
            float lowerLength = Vector3.Distance(lower.position, hand.position);
            Vector3 reach = _target.position - shoulder;
            float distance = Mathf.Clamp(reach.magnitude, 0.01f, upperLength + lowerLength - 0.001f);
            Vector3 direction = reach.normalized;
            Vector3 bend = Vector3.ProjectOnPlane(_elbow - shoulder, direction).normalized;
            float angle = Mathf.Acos(Mathf.Clamp((upperLength * upperLength + distance * distance - lowerLength * lowerLength) / (2f * upperLength * distance), -1f, 1f));
            Vector3 elbow = shoulder + (direction * Mathf.Cos(angle) + bend * Mathf.Sin(angle)) * upperLength;

            upper.rotation = Quaternion.FromToRotation(lower.position - shoulder, elbow - shoulder) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, shoulder + direction * distance - lower.position) * lower.rotation;
            hand.rotation = _target.rotation;
        }

        /// The grip turns by as much as the shield now stands off the hand compared with the sampled socket, on top of the stored grip.
        private void Save()
        {
            Transform hand = _animator.GetBoneTransform(HumanBodyBones.LeftHand);
            Pose posed = new Pose(hand.InverseTransformPoint(_shield.position), Quaternion.Inverse(hand.rotation) * _shield.rotation);
            Quaternion inverse = Quaternion.Inverse(_sampled.rotation);
            Pose change = new Pose(inverse * (posed.position - _sampled.position), inverse * posed.rotation);
            ShieldGripConfig.Set(s_shields[_index], change.GetTransformedBy(ShieldGripConfig.Get(s_shields[_index])));
            _sampled = posed;
        }

        private void ClosePreview()
        {
            if (_root != null)
                DestroyImmediate(_root);

            _root = null;
            _target = null;
        }

        private void OnSceneGui(SceneView view)
        {
            if (_target == null || _animator == null || !_target.hasChanged)
                return;

            _target.hasChanged = false;
            SolveArm();
        }
    }
}
