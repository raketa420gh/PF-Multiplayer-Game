using Fusion;
using UnityEngine;

namespace Game.Scripts.GameObjects.Content
{
    public sealed class MedkitPickUpView : NetworkBehaviour
    {
        [SerializeField]
        private MedkitPickUp _medkitPickUp;
        
        [SerializeField]
        private GameObject _visual;
        
        [SerializeField]
        private ParticleSystem _vfx;

        public override void Spawned()
        {
            _medkitPickUp.OnInteracted += OnInteracted;
            _visual.SetActive(_medkitPickUp.IsActive);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _medkitPickUp.OnInteracted -= OnInteracted;
        }

        public override void Render()
        {
            _visual.SetActive(_medkitPickUp.IsActive);
        }

        private void OnInteracted()
        {
            _vfx.Play(withChildren: true);
        }
    }
}