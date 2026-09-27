using Fusion;
using UnityEngine;

namespace Game.Scripts.GameObjects.Content
{
    public sealed class Medkit : NetworkBehaviour, IInteractableComponent
    {
        [SerializeField]
        private float _radius = 0.25f;
        
        [SerializeField]
        private LayerMask _layerMask;

        [SerializeField]
        private int _heal = 1;

        [SerializeField]
        private float _cooldown;

        [Networked]
        private TickTimer _timestamp { get; set; }

        public void Interact(GameObject interactor)
        {
            if (_timestamp.IsRunning(Runner))
                return;

            if (interactor.TryGetComponent(out HealthComponent health) && health.IsNotFull)
            {
                health.Restore(_heal);
                _timestamp = TickTimer.CreateFromSeconds(Runner, _cooldown);
            }
        }
    }
}