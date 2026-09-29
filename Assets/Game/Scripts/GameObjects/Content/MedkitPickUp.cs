using System;
using Fusion;
using UnityEngine;

namespace Game.Scripts.GameObjects.Content
{
    public sealed class MedkitPickUp : NetworkBehaviour, IInteractableComponent
    {
        public event Action OnInteracted;
        
        [SerializeField]
        private float _radius = 0.25f;
        
        [SerializeField]
        private LayerMask _layerMask;

        [SerializeField]
        private int _heal = 1;

        [SerializeField]
        private float _cooldown;

        [Networked, OnChangedRender(nameof(InvokeInteracted))]
        private TickTimer _timestamp { get; set; }

        public bool IsActive => _timestamp.ExpiredOrNotRunning(Runner);

        public override void Spawned()
        {
            Runner.SetIsSimulated(Object, true);
        }

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
        
        private void InvokeInteracted() => OnInteracted?.Invoke();
    }
}