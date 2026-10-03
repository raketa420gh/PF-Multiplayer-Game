using Fusion;
using UnityEngine;

namespace Game.Scripts.Battle
{
    public sealed class ZoneHitbox : Hitbox
    {
        public HitZone Zone => _zone;
        public bool IsRound => _isRound;

        [SerializeField]
        private HitZone _zone;

        [SerializeField, Tooltip("Disc inscribed in the box (round shields): normal along local Z, radius = min(X, Y) extent")]
        private bool _isRound;

        /// Round shields are discs: a ray through a corner of their box passes by and may hit what is behind.
        public static bool IsInsideShape(NetworkRunner runner, in LagCompensatedHit hit, PlayerRef player)
        {
            if (hit.Hitbox is not ZoneHitbox { IsRound: true } zoneHitbox)
                return true;

            runner.LagCompensation.PositionRotation(hit.Hitbox, player, out Vector3 center, out Quaternion rotation, true);

            return zoneHitbox.Contains(hit.Point, center, rotation);
        }

        /// The box is the broad phase; a round hitbox only accepts points inside its disc.
        public bool Contains(Vector3 point, Vector3 center, Quaternion rotation)
        {
            if (!_isRound)
                return true;

            Vector3 local = Quaternion.Inverse(rotation) * (point - center);
            float radius = Mathf.Min(BoxExtents.x, BoxExtents.y);

            return local.x * local.x + local.y * local.y <= radius * radius;
        }
    }
}
