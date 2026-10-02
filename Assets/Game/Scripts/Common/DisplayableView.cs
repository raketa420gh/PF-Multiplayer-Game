using UnityEngine;

namespace Game.Scripts
{
    public abstract class DisplayableView : MonoBehaviour
    {
        public bool IsShown => gameObject.activeSelf;

        public virtual void Show()
        {
            gameObject.SetActive(true);
        }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetShown(bool isShown)
        {
            if (isShown == IsShown)
                return;

            if (isShown)
                Show();
            else
                Hide();
        }
    }
}
