using UnityEngine;

namespace Game.Scripts.Battle
{
    public enum HitZone : byte
    {
        Torso,
        Head,
        Legs,
        Block
    }

    [CreateAssetMenu(menuName = "Game/Battle/Hit Zone Config")]
    public sealed class HitZoneConfig : ScriptableObject
    {
        [SerializeField]
        private float _headMultiplier = 1.5f;

        [SerializeField]
        private float _torsoMultiplier = 1f;

        [SerializeField]
        private float _legsMultiplier = 0.6f;

        public float GetMultiplier(HitZone zone)
        {
            return zone switch
            {
                HitZone.Head => _headMultiplier,
                HitZone.Legs => _legsMultiplier,
                _ => _torsoMultiplier
            };
        }
    }
}
