using UnityEngine;

namespace Game.Scripts
{
    public interface IInteractableComponent
    {
        void Interact(GameObject interactor);
    }
}