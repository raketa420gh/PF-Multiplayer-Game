using Fusion;
using UnityEngine;

namespace Game.Scripts.Battle
{
    public sealed class ZoneHitbox : Hitbox
    {
        public HitZone Zone => _zone;

        [SerializeField]
        private HitZone _zone;
    }
}
