using Game.Scripts.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Battle
{
    /// Sets how a shield sits in the off hand. Pose puts the character in the shield pair's idle with the hand still and the shield on
    /// a Grip object: move and turn it with the scene tools until the hand holds it as it should, then Save. A rebuild solves every
    /// clip with that shield for the grip: the board keeps its path, the hand and the elbow follow it.
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
        private Transform _grip;

        [SerializeField]
        private Pose _sampled;

        private void OnDisable()
        {
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

            using (new EditorGUI.DisabledScope(_grip == null))
            {
                if (GUILayout.Button("Select Grip"))
                    Selection.activeTransform = _grip;

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

        private void CreatePreview()
        {
            ClosePreview();
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{BattleEditorUtility.AnimationsFolder}/{s_clips[_index]}.anim");
            GameObject model = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(BattleEditorUtility.ModelPath), Vector3.zero, Quaternion.identity);
            model.hideFlags = HideFlags.HideAndDontSave;
            Animator animator = model.GetComponent<Animator>();
            Transform socket = BattlePoseRig.CreateSockets(animator)[(int)WeaponSocket.LeftShield];
            _root = new GameObject("Shield Grip Preview") { hideFlags = HideFlags.DontSave };

            AnimationMode.StartAnimationMode();

            try
            {
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(model, clip, 0f);
                AnimationMode.EndSampling();

                // Skinning is baked into plain meshes: the body holds the pose once the sampling is over.
                foreach (SkinnedMeshRenderer skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    Mesh mesh = new Mesh();
                    skin.BakeMesh(mesh, true);
                    GameObject part = new GameObject(skin.name) { hideFlags = HideFlags.DontSave };
                    part.transform.SetParent(_root.transform, false);
                    part.transform.SetPositionAndRotation(skin.transform.position, skin.transform.rotation);
                    part.AddComponent<MeshFilter>().sharedMesh = mesh;
                    part.AddComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials;
                }

                Transform bone = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                Transform hand = new GameObject("Hand").transform;
                hand.SetParent(_root.transform, false);
                hand.SetPositionAndRotation(bone.position, bone.rotation);
                hand.localScale = bone.lossyScale;
                _grip = new GameObject("Grip").transform;
                _grip.SetParent(hand, false);
                _grip.SetLocalPositionAndRotation(socket.localPosition, socket.localRotation);
                _sampled = new Pose(socket.localPosition, socket.localRotation);
            }
            finally
            {
                AnimationMode.StopAnimationMode();
                DestroyImmediate(model);
            }

            Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>($"{BattleEditorUtility.PrefabsFolder}/{s_shields[_index]}.prefab"), _grip, false);

            foreach (Transform part in _root.GetComponentsInChildren<Transform>(true))
                part.gameObject.hideFlags = HideFlags.DontSave;

            Selection.activeTransform = _grip;
            SceneView view = SceneView.lastActiveSceneView;

            if (view != null)
                view.LookAt(_grip.position, Quaternion.LookRotation(new Vector3(0.6f, -0.3f, 1f)), 0.9f);
        }

        /// The grip turns by as much as the posed one stands off the sampled socket, on top of the stored grip.
        private void Save()
        {
            Pose posed = new Pose(_grip.localPosition, _grip.localRotation);
            Quaternion inverse = Quaternion.Inverse(_sampled.rotation);
            Pose change = new Pose(inverse * (posed.position - _sampled.position), inverse * posed.rotation);
            ShieldGripConfig.Set(s_shields[_index], change.GetTransformedBy(ShieldGripConfig.Get(s_shields[_index])));
            _sampled = posed;
        }

        private void ClosePreview()
        {
            if (_root != null)
            {
                foreach (MeshFilter filter in _root.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter.sharedMesh != null && !AssetDatabase.Contains(filter.sharedMesh))
                        DestroyImmediate(filter.sharedMesh);
                }

                DestroyImmediate(_root);
            }

            _root = null;
            _grip = null;
        }
    }
}
