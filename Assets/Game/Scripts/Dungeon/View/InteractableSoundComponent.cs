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

        [SerializeField, Tooltip("The door's creak while someone holds it open; stops when the hold ends")]
        private AudioSource _creak;

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
                    DungeonAudioComponent.Play(_doorOpen ? DungeonSound.DoorOpen : DungeonSound.DoorClose, transform.position, 0.8f, Random.Range(0.96f, 1.04f));
            }

            if (_creak != null && _door != null && _door.IsOpening != _creak.isPlaying)
                SetCreak(_door.IsOpening);

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

        private void SetCreak(bool isOpening)
        {
            if (!isOpening)
            {
                _creak.Stop();

                return;
            }

            _creak.clip = DungeonAudioComponent.Clip(DungeonSound.DoorOpening);

            if (_creak.clip != null)
                _creak.Play();
        }
    }
}
