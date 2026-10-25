using Fusion;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Plays footsteps at a cadence set by the ground speed; walking is quieter, as in Dark and Darker.
    public sealed class FootstepComponent : NetworkBehaviour
    {
        /// Level geometry and the water volumes on Ignore Raycast.
        private const int SurfaceMask = 1 | 1 << 2;
        private const float SurfaceProbe = 0.8f;
        private const float MinSpeed = 0.3f;

        private static readonly RaycastHit[] s_hits = new RaycastHit[8];

        [SerializeField]
        private FighterComponent _fighter;

        [SerializeField, Tooltip("Metres between two steps; the interval is stride / speed")]
        private float _stride = 1.3f;

        [SerializeField, Tooltip("Recorded steps; without them the synthesized pair plays")]
        private FootstepSoundConfig _sounds;

        [SerializeField, Tooltip("Footwear of a body without an inventory")]
        private Footwear _footwear = Footwear.Light;

        [SerializeField, Tooltip("Optional: the boots in the Feet slot pick the footwear")]
        private InventoryComponent _inventory;

        private float _lastStepTime = float.NegativeInfinity;
        private bool _toggle;

        /// A foot is planted as soon as the body moves and then every stride / speed seconds: tapping the keys never steps
        /// faster than a steady walk, and every class sounds the same at the same speed.
        public override void Render()
        {
            Vector3 velocity = _fighter.Move.Velocity;
            velocity.y = 0f;
            float speed = velocity.magnitude;

            if (!_fighter.Move.IsGrounded || !_fighter.Health.IsAlive || speed < MinSpeed || Time.time - _lastStepTime < _stride / speed)
                return;

            _lastStepTime = Time.time;
            _toggle = !_toggle;
            Vector3 position = transform.position;
            float volume = Mathf.Clamp01(speed / 4.2f) * 0.3f + 0.08f;
            float pitch = Random.Range(0.92f, 1.08f);
            Footwear footwear = GetFootwear();
            AudioClip clip = _sounds != null ? _sounds.Pick(footwear, GetSurface(position)) : null;

            if (clip != null)
                DungeonAudioComponent.Play(clip, position, volume, pitch * _sounds.GetPitch(footwear));
            else
                DungeonAudioComponent.Play(_toggle ? DungeonSound.Footstep : DungeonSound.FootstepB, position, volume, pitch);
        }

        private Footwear GetFootwear()
        {
            if (_inventory == null)
                return _footwear;

            return FootstepSoundConfig.FromArmor(_inventory.GetEquippedConfig<ArmorItemConfig>(EquipSlot.Feet));
        }

        /// The nearest tagged collider or solid ground under the feet; a ray from above the knee also meets water volumes.
        private FootstepSurface GetSurface(Vector3 feet)
        {
            int count = Physics.RaycastNonAlloc(feet + Vector3.up * SurfaceProbe, Vector3.down, s_hits, SurfaceProbe + 0.3f, SurfaceMask, QueryTriggerInteraction.Collide);
            FootstepSurface surface = FootstepSurface.Stone;
            float nearest = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = s_hits[i];
                FootstepSurfaceTag surfaceTag = hit.collider.GetComponentInParent<FootstepSurfaceTag>();

                if (hit.distance >= nearest || (surfaceTag == null && hit.collider.isTrigger))
                    continue;

                nearest = hit.distance;
                surface = surfaceTag != null ? surfaceTag.GetSurface(hit.point) : FootstepSurface.Stone;
            }

            return surface;
        }
    }
}
