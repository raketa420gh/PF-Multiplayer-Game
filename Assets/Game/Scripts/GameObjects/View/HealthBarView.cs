using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts
{
    public sealed class HealthBarView : MonoBehaviour
    {
        [SerializeField]
        private Image _fill;

        [SerializeField]
        private TMP_Text _text;

        [SerializeField]
        private Gradient _colorByProgress;

        [SerializeField]
        private Vector3 _worldRotation = new(45f, 0f, 0f);

        private void LateUpdate()
        {
            transform.rotation = Quaternion.Euler(_worldRotation);
        }

        public void SetText(string text)
        {
            _text.text = text;
        }

        public void SetProgress(float progress)
        {
            _fill.fillAmount = progress;
            _fill.color = _colorByProgress.Evaluate(progress);
        }
    }
}
