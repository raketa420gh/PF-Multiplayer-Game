using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Shows a humanoid model of its own skeleton in place of the fighter rig: every frame, after the rig has been animated and its arms
    /// solved, the rig's human pose is copied onto the model. Hitboxes and weapons stay on the rig.
    [DefaultExecutionOrder(10000)]
    public sealed class RetargetedModelComponent : MonoBehaviour
    {
        [SerializeField]
        private Animator _source;

        [SerializeField]
        private Avatar _avatar;

        [SerializeField]
        private Transform _model;

        private HumanPoseHandler _sourceHandler;
        private HumanPoseHandler _targetHandler;
        private HumanPose _pose;

        private void Awake()
        {
            _sourceHandler = new HumanPoseHandler(_source.avatar, _source.transform);
            _targetHandler = new HumanPoseHandler(_avatar, _model);
        }

        private void LateUpdate()
        {
            _sourceHandler.GetHumanPose(ref _pose);
            _targetHandler.SetHumanPose(ref _pose);
        }

        private void OnDestroy()
        {
            _sourceHandler?.Dispose();
            _targetHandler?.Dispose();
        }
    }
}
