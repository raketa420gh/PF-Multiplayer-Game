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
        public bool IsSwarmEnabled => _isSwarmEnabled;
        public float SwarmDamagePerSecond => _swarmDamagePerSecond;
        public SwarmStage[] SwarmStages => _swarmStages;
        /// When each escape portal of a floor shows up, one portal per entry; the last entry serves any further portals.
        public float[] EscapePortalTimes => _escapePortalTimes;
        public float DescendPortalTime => _descendPortalTime;
        public float FallDamageThreshold => _fallDamageThreshold;
        public float FallDamagePerMeter => _fallDamagePerMeter;
        /// A drop at least this deep (a chasm) kills outright.
        public float LethalFall => _lethalFall;
        /// Longest time monsters ignore a freshly spawned adventurer who has not moved or pressed anything yet.
        public float LoadInProtection => _loadInProtection;
        public int[] ExperiencePerLevel => _experiencePerLevel;
        public int MaxLevel => _maxLevel;

        [SerializeField]
        private float _matchDuration = 900f;

        [SerializeField]
        private bool _isSwarmEnabled;

        [SerializeField]
        private float _swarmDamagePerSecond = 3f;

        [SerializeField]
        private SwarmStage[] _swarmStages =
        {
            new() { StartTime = 360f, Duration = 60f, Share = 0.82f },
            new() { StartTime = 450f, Duration = 60f, Share = 0.48f },
            new() { StartTime = 540f, Duration = 60f, Share = 0.22f },
            new() { StartTime = 630f, Duration = 60f, Share = 0f }
        };

        [SerializeField]
        private float[] _escapePortalTimes = { 420f, 600f, 720f, 870f };

        [SerializeField]
        private float _descendPortalTime = 60f;

        [SerializeField]
        private float _fallDamageThreshold = 4f;

        [SerializeField]
        private float _fallDamagePerMeter = 8f;

        [SerializeField]
        private float _lethalFall = 10f;

        [SerializeField]
        private float _loadInProtection = 20f;

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
