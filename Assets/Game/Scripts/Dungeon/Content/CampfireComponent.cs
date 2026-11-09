using System.Collections.Generic;
using Fusion;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Campfire set on the floor: adventurers seated (G) within WarmRadius heal faster and get their spells back faster; burns out
    /// after a while.
    public sealed class CampfireComponent : NetworkBehaviour
    {
        public const float WarmRadius = 3.5f;

        public bool IsLit => _lifetime.ExpiredOrNotRunning(Runner) == false;

        [SerializeField]
        private float _burnTime = 180f;

        [SerializeField]
        private Light _light;

        /// The kit's sounds: the fire catching as it is set, then its crackle (Ignite, Burn).
        [SerializeField]
        private WeaponSoundConfig _sounds;

        [Networked]
        private TickTimer _lifetime { get; set; }

        private static readonly List<CampfireComponent> s_all = new();
        private float _flicker;
        private AudioSource _burn;

        public override void Spawned()
        {
            s_all.Add(this);

            if (HasStateAuthority)
                _lifetime = TickTimer.CreateFromSeconds(Runner, _burnTime);

            // Only a fire set just now is heard catching; one already burning when it comes into view only crackles.
            if (_lifetime.RemainingTime(Runner) > _burnTime - 1f)
                DungeonAudioComponent.Play(WeaponSoundConfig.Pick(_sounds, ItemSound.Ignite), transform.position + Vector3.up * 0.3f);

            AudioClip burn = WeaponSoundConfig.Pick(_sounds, ItemSound.Burn);

            if (burn == null)
                return;

            _burn = gameObject.AddComponent<AudioSource>();
            _burn.clip = burn;
            _burn.loop = true;
            _burn.spatialBlend = 1f;
            _burn.rolloffMode = AudioRolloffMode.Linear;
            _burn.minDistance = 1f;
            _burn.maxDistance = 14f;
            _burn.dopplerLevel = 0f;
            _burn.volume = 0.5f;
            _burn.time = Random.Range(0f, burn.length);
            _burn.Play();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            s_all.Remove(this);
        }

        /// A lit campfire burns within reach of the point.
        public static bool IsWarming(Vector3 position)
        {
            return s_all.Exists(fire => fire.IsLit && (fire.transform.position - position).sqrMagnitude < WarmRadius * WarmRadius);
        }

        public override void Render()
        {
            if (_light == null)
                return;

            if (_burn != null && !IsLit)
                _burn.Stop();

            _flicker += Time.deltaTime * 11f;
            _light.intensity = 2.2f + Mathf.PerlinNoise(_flicker, 0.3f) * 1.2f;
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority && _lifetime.Expired(Runner))
                Runner.Despawn(Object);
        }
    }
}
