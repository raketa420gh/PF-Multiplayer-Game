using System;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    [Serializable]
    public struct SwarmStage
    {
        public float StartTime;
        public float Duration;
        /// Safe radius at the end of the stage as a share of the floor radius.
        public float Share;
    }

    [CreateAssetMenu(menuName = "Game/Dungeon/Dungeon Config")]
    public sealed class DungeonConfig : ScriptableObject
    {
        public float MatchDuration => _matchDuration;
        public float SwarmDamagePerSecond => _swarmDamagePerSecond;
        public SwarmStage[] SwarmStages => _swarmStages;
        public float EscapePortalTime => _escapePortalTime;
        public float DescendPortalTime => _descendPortalTime;
        public int EscapePortalsPerFloor => _escapePortalsPerFloor;
        public float FallDamageThreshold => _fallDamageThreshold;
        public float FallDamagePerMeter => _fallDamagePerMeter;
        public int[] ExperiencePerLevel => _experiencePerLevel;
        public int MaxLevel => _maxLevel;

        [SerializeField]
        private float _matchDuration = 720f;

        [SerializeField]
        private float _swarmDamagePerSecond = 6f;

        [SerializeField]
        private SwarmStage[] _swarmStages =
        {
            new() { StartTime = 150f, Duration = 60f, Share = 0.82f },
            new() { StartTime = 330f, Duration = 60f, Share = 0.48f },
            new() { StartTime = 510f, Duration = 60f, Share = 0.22f },
            new() { StartTime = 660f, Duration = 60f, Share = 0f }
        };

        [SerializeField]
        private float _escapePortalTime = 60f;

        [SerializeField]
        private float _descendPortalTime = 60f;

        [SerializeField]
        private int _escapePortalsPerFloor = 3;

        [SerializeField]
        private float _fallDamageThreshold = 4f;

        [SerializeField]
        private float _fallDamagePerMeter = 8f;

        [SerializeField]
        private int[] _experiencePerLevel = { 50, 50, 50, 50, 50, 75, 75, 75, 75, 75, 75, 75, 75, 75, 75 };

        [SerializeField]
        private int _maxLevel = 15;

        public int ExperienceForLevel(int level)
        {
            int index = Mathf.Clamp(level - 1, 0, _experiencePerLevel.Length - 1);

            return _experiencePerLevel[index];
        }
    }
}
