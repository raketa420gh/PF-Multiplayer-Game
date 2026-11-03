using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// A lamp anyone can put out or light with F, the same for every peer. A part of them starts out cold; a lamp far from the
    /// camera keeps its light off, the fog swallows it long before.
    public sealed class CatacombLampComponent : InteractableComponent
    {
        public override string Prompt => _isOut ? "Light" : "Put out";

        [SerializeField]
        private Light _light;

        [SerializeField]
        private FlickerLightComponent _flicker;

        [SerializeField]
        private ParticleSystem[] _flames;

        [SerializeField, Range(0f, 1f)]
        private float _coldChance = 0.3f;

        private const float DrawDistance = 40f;

        private static Vector3 s_viewer;

        [Networked]
        private NetworkBool _isOut { get; set; }

        private bool _isDark;

        /// Where the local camera is, told by the culling view.
        public static void SetViewer(Vector3 position)
        {
            s_viewer = position;
        }

        public override void Spawned()
        {
            if (HasStateAuthority)
                _isOut = Random.value < _coldChance;

            _isDark = !_isOut;
            ShowFlames();
        }

        public override void Render()
        {
            if (_isDark != _isOut)
            {
                _isDark = _isOut;
                ShowFlames();
                DungeonAudioComponent.Play(DungeonSound.Rattle, transform.position);
            }

            _light.enabled = !_isDark && (s_viewer - transform.position).sqrMagnitude <= DrawDistance * DrawDistance;

            if (_flicker != null)
                _flicker.enabled = _light.enabled;
        }

        public override void Complete(AdventurerComponent adventurer)
        {
            _isOut = !_isOut;
        }

        private void ShowFlames()
        {
            foreach (ParticleSystem flame in _flames)
            {
                if (_isOut)
                    flame.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                else
                    flame.Play(true);
            }
        }
    }
}
