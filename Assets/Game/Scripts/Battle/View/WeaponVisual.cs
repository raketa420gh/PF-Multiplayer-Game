using UnityEngine;

namespace Game.Scripts.Battle
{
    public sealed class WeaponVisual : MonoBehaviour
    {
        [SerializeField]
        private TrailRenderer _trail;

        [Header("Bow")]
        [SerializeField]
        private LineRenderer _string;

        [SerializeField]
        private Transform _stringTop;

        [SerializeField]
        private Transform _stringBottom;

        [SerializeField]
        private Transform _nockRest;

        [SerializeField]
        private Transform _arrow;

        /// How far up the stave from the grip the arrow lies: on the fist, not through it.
        [SerializeField]
        private float _arrowRest = 0.06f;

        [Header("Book")]
        [SerializeField]
        private GameObject _bookOpen;

        [SerializeField]
        private GameObject _bookClosed;

        /// Clip seconds of the strike between which the book is shut: the other hand folds it first and it is opened again
        /// in the palm before the strike is over.
        [SerializeField]
        private Vector2 _attackShut = new Vector2(0f, float.MaxValue);

        /// Clip seconds at which the book taken out shut opens on the palm, and at which the book being put away is shut.
        [SerializeField]
        private float _equipOpen;

        [SerializeField]
        private float _unequipShut = float.MaxValue;

        public bool IsShutInAttack(float time) => time >= _attackShut.x && time < _attackShut.y;
        public bool IsShutInEquip(bool isUnequipping, float time) => isUnequipping ? time >= _unequipShut : time < _equipOpen;

        public void SetTrailActive(bool isActive)
        {
            if (_trail != null && _trail.emitting != isActive)
                _trail.emitting = isActive;
        }

        /// A book is shut to block and to strike with, open in the palm otherwise.
        public void SetClosed(bool isClosed)
        {
            if (_bookOpen == null)
                return;

            _bookOpen.SetActive(!isClosed);
            _bookClosed.SetActive(isClosed);
        }

        public void SetDraw(bool isDrawn, Vector3 handPosition, bool hasArrow)
        {
            if (_string == null)
                return;

            Vector3 nock = isDrawn && IsOnString(handPosition) ? handPosition : _nockRest.position;

            _string.SetPosition(0, _stringTop.position);
            _string.SetPosition(1, nock);
            _string.SetPosition(2, _stringBottom.position);

            _arrow.gameObject.SetActive(hasArrow);

            if (hasArrow)
                _arrow.SetPositionAndRotation(nock, Quaternion.LookRotation(transform.position + transform.forward * _arrowRest - nock, transform.forward));
        }

        /// The draw hand pulls the string only from behind the bow, between its limbs: nocking, it is elsewhere. At full draw
        /// the anchor is off the bow's plane (it is seen edge on, the arrow coming from below the eye, as in DaD).
        private bool IsOnString(Vector3 handPosition)
        {
            Vector3 local = transform.InverseTransformPoint(handPosition);

            return local.y < _nockRest.localPosition.y + 0.03f && Mathf.Abs(local.z) < 0.35f && Mathf.Abs(local.x) < 0.4f;
        }
    }
}
