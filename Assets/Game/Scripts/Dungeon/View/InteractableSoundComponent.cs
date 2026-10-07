using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Watches networked door / container / portal / lever state and plays the matching sound on every peer.
    public sealed class InteractableSoundComponent : NetworkBehaviour
    {
        [SerializeField]
        private DoorComponent _door;

        [SerializeField]
        private ContainerComponent _container;

        [SerializeField]
        private PortalComponent _portal;

        [SerializeField]
        private LeverComponent _lever;

        [SerializeField]
        private AudioSource _loop;

        private bool _doorOpen;
        private bool _containerOpen;
        private bool _portalActive;
        private bool _leverPulled;
        private bool _isFirst = true;

        public override void Render()
        {
            if (_door != null && _door.IsOpen != _doorOpen)
            {
                _doorOpen = _door.IsOpen;

                if (!_isFirst)
                    DungeonAudioComponent.Play(DungeonSound.DoorCreak, transform.position, 0.8f, _doorOpen ? 1f : 0.85f);
            }

            if (_container != null && _container.IsOpen != _containerOpen)
            {
                _containerOpen = _container.IsOpen;

                if (!_isFirst && _containerOpen)
                    DungeonAudioComponent.Play(DungeonSound.ChestOpen, transform.position);
            }

            if (_portal != null && _portal.IsOpen != _portalActive)
            {
                _portalActive = _portal.IsOpen;

                if (!_isFirst && _portalActive)
                    DungeonAudioComponent.Play(DungeonSound.PortalOpen, transform.position, 1f);

                if (_loop != null)
                {
                    if (_portalActive)
                        _loop.Play();
                    else
                        _loop.Stop();
                }
            }

            if (_lever != null && _lever.IsPulled != _leverPulled)
            {
                _leverPulled = _lever.IsPulled;

                if (!_isFirst && _leverPulled)
                    DungeonAudioComponent.Play(DungeonSound.Lever, transform.position);
            }

            _isFirst = false;
        }
    }
}
