using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// The crosshair of a throwing weapon as in the Dark and Darker footage: two bars beside the dot. Drawing the throw
    /// closes them in slowly, at full power they snap shut on the dot; let go, they kick out wide and settle back.
    /// Gaps are the inner edges of the bars from the centre, in 1080p pixels.
    public sealed class ThrowCrosshairView : DisplayableView
    {
        [SerializeField]
        private RectTransform _left;

        [SerializeField]
        private RectTransform _right;

        [SerializeField]
        private float _openGap = 52f;

        [SerializeField, Tooltip("Where the slow close of the draw has come to at the snap")]
        private float _snapGap = 27f;

        [SerializeField]
        private float _closedGap = 6f;

        [SerializeField, Range(0f, 1f), Tooltip("Share of the full draw after which the bars snap shut")]
        private float _snapDraw = 0.91f;

        [SerializeField]
        private float _kickGap = 62f;

        [SerializeField]
        private float _kickRise = 0.06f;

        [SerializeField]
        private float _kickHold = 0.12f;

        [SerializeField]
        private float _kickTime = 0.23f;

        private bool _wasAiming;
        private float _sinceRelease = float.MaxValue;

        /// Called every frame; draw is 0..1 while the throw is held, a release starts the kick.
        public void Refresh(bool isShown, bool isAiming, float draw, bool isReleasing)
        {
            SetShown(isShown);

            if (_wasAiming && !isAiming && isReleasing)
                _sinceRelease = 0f;

            _wasAiming = isAiming;
            _sinceRelease += Time.deltaTime;

            if (!isShown)
                return;

            float gap = isAiming ? DrawGap(draw) : KickGap(_sinceRelease);
            _left.anchoredPosition = new Vector2(-gap, 0f);
            _right.anchoredPosition = new Vector2(gap, 0f);
        }

        private float DrawGap(float draw)
        {
            return draw < _snapDraw
                ? Mathf.Lerp(_openGap, _snapGap, draw / _snapDraw)
                : Mathf.Lerp(_snapGap, _closedGap, Mathf.InverseLerp(_snapDraw, 1f, draw));
        }

        private float KickGap(float time)
        {
            if (time < _kickRise)
                return Mathf.Lerp(_closedGap, _kickGap, time / _kickRise);

            return time < _kickHold ? _kickGap : Mathf.Lerp(_kickGap, _openGap, Mathf.InverseLerp(_kickHold, _kickTime, time));
        }
    }
}
