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

        [Header("Book")]
        [SerializeField]
        private GameObject _bookOpen;

        [SerializeField]
        private GameObject _bookClosed;

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

            Vector3 nock = isDrawn ? handPosition : _nockRest.position;

            _string.SetPosition(0, _stringTop.position);
            _string.SetPosition(1, nock);
            _string.SetPosition(2, _stringBottom.position);

            _arrow.gameObject.SetActive(hasArrow);

            if (hasArrow)
                _arrow.SetPositionAndRotation(nock, Quaternion.LookRotation(transform.position - nock, transform.forward));
        }
    }
}
