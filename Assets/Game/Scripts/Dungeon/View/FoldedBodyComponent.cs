using Fusion;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// A skeleton that lies on the floor as a folded heap of bones, as in Dark and Darker: it unfolds from the legs up when it wakes and folds back
    /// down, head first, when it dies. Works on the fighter rig after it has been animated and before RetargetedModelComponent copies it: every
    /// muscle is blended between the folded pose and the animated one within a stretch of the way up that depends on its body part.
    [DefaultExecutionOrder(9000)]
    public sealed class FoldedBodyComponent : NetworkBehaviour
    {
        private const int FirstNeckMuscle = 9;
        private const int FirstLegMuscle = 21;
        private const int FirstArmMuscle = 37;
        /// How far the bones rattle while they come together, in muscle range, and how fast.
        private const float Rattle = 0.05f;
        private const float RattleRate = 34f;

        [SerializeField]
        private MonsterComponent _monster;

        [SerializeField]
        private FighterAnimComponent _anim;

        [SerializeField]
        private Animator _rig;

        [SerializeField, Tooltip("Muscles of the folded pose, in HumanTrait order")]
        private float[] _muscles;

        [SerializeField, Tooltip("HumanPose body position and rotation of the folded pose")]
        private Vector3 _bodyPosition;

        [SerializeField]
        private Quaternion _bodyRotation = Quaternion.identity;

        [SerializeField, Tooltip("Seconds a dying body takes to fold down")]
        private float _collapseTime = 0.9f;

        private HumanPoseHandler _handler;
        private HumanPose _pose;
        private Transform _hips;
        private Transform _leftFoot;
        private Transform _rightFoot;
        private float _footHeight;
        private float _stand = 1f;
        private float _deathStand;
        private float _deathTime = float.NegativeInfinity;
        private bool _wasAlive;

        private void LateUpdate()
        {
            if (_stand < 1f && Object != null && Object.IsValid)
                Pose(_stand, _wasAlive && _stand > 0f);
        }

        private void OnDestroy()
        {
            _handler?.Dispose();
        }

        public override void Spawned()
        {
            _anim.SetDeathSkipped(true);
            _wasAlive = _monster.Fighter.Health.IsAlive;
        }

        public override void Render()
        {
            bool isAlive = _monster.Fighter.Health.IsAlive;

            if (_wasAlive && !isAlive)
            {
                _deathTime = Time.time;
                _deathStand = _stand;
            }

            _wasAlive = isAlive;
            _stand = isAlive ? _monster.Rise : _deathStand * (1f - Mathf.Clamp01((Time.time - _deathTime) / _collapseTime));
        }

        /// Puts the rig between the folded pose (0) and the pose it is in (1).
        public void Pose(float stand, bool isRattling)
        {
            if (_handler == null)
                Bind();

            _handler.GetHumanPose(ref _pose);
            float legs = Stretch(stand, 0f, 0.6f);

            for (int i = 0; i < _pose.muscles.Length; i++)
            {
                float weight = i >= FirstArmMuscle ? Stretch(stand, 0.35f, 0.95f)
                    : i >= FirstLegMuscle ? legs
                    : i >= FirstNeckMuscle ? Stretch(stand, 0.55f, 1f)
                    : Stretch(stand, 0.2f + i / 3 * 0.07f, 0.75f + i / 3 * 0.07f);
                float rattle = isRattling ? Mathf.Sin(Time.time * RattleRate + i * 1.7f) * Rattle * 4f * weight * (1f - weight) : 0f;
                _pose.muscles[i] = Mathf.Lerp(_muscles[i], _pose.muscles[i], weight) + rattle;
            }

            _pose.bodyPosition = Vector3.Lerp(_bodyPosition, _pose.bodyPosition, legs);
            _pose.bodyRotation = Quaternion.Slerp(_bodyRotation, _pose.bodyRotation, legs);
            _handler.SetHumanPose(ref _pose);

            // Muscles know nothing of the floor: on the way up the hips are set so that the lower foot stands on it.
            float lowest = Mathf.Min(_leftFoot.position.y, _rightFoot.position.y) - _rig.transform.position.y;
            _hips.position += Vector3.up * ((_footHeight - lowest) * Mathf.Clamp01(legs * 3f));
        }

        private void Bind()
        {
            _handler = new HumanPoseHandler(_rig.avatar, _rig.transform);
            _hips = _rig.GetBoneTransform(HumanBodyBones.Hips);
            _leftFoot = _rig.GetBoneTransform(HumanBodyBones.LeftFoot);
            _rightFoot = _rig.GetBoneTransform(HumanBodyBones.RightFoot);
            _footHeight = Mathf.Min(_leftFoot.position.y, _rightFoot.position.y) - _rig.transform.position.y;
        }

        /// 0 before the stretch from..to of the way up, 1 after it, eased in between.
        private static float Stretch(float stand, float from, float to)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, stand));
        }
    }
}
