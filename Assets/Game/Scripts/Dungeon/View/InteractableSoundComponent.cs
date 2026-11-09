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

        [SerializeField, Tooltip("Escape portal showing up (its pedestal rises)")]
        private AudioClip[] _portalAppear;

        [SerializeField]
        private AudioClip[] _portalOpen;

        [SerializeField]
        private AudioClip[] _portalClose;

        [SerializeField, Tooltip("Someone steps through the portal")]
        private AudioClip[] _portalEnter;

        [SerializeField]
        private LeverComponent _lever;

        [SerializeField]
        private AudioSource _loop;

        [SerializeField, Tooltip("The door's creak while someone holds it open; stops when the hold ends")]
        private AudioSource _creak;

        private bool _doorOpen;
        private bool _containerOpen;
        private bool _portalActive;
        private bool _portalShown;
        private int _portalPasses;
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

            if (_portal != null && _portal.IsActive != _portalShown)
            {
                _portalShown = _portal.IsActive;

                // A way down opens the moment it shows up: its open sound covers that.
                if (!_isFirst && (!_portalShown || _portal.Kind == PortalKind.Escape))
                    PlayPortal(_portalShown ? _portalAppear : _portalClose);
            }

            if (_portal != null && _portal.Passes != _portalPasses)
            {
                _portalPasses = _portal.Passes;

                if (!_isFirst)
                    PlayPortal(_portalEnter);
            }

            if (_portal != null && _portal.IsOpen != _portalActive)
            {
                _portalActive = _portal.IsOpen;

                if (!_isFirst && _portalActive)
                    PlayPortal(_portalOpen);

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

        private void PlayPortal(AudioClip[] clips)
        {
            if (clips != null && clips.Length > 0)
                DungeonAudioComponent.Play(clips[Random.Range(0, clips.Length)], transform.position + Vector3.up * 1.2f);
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
