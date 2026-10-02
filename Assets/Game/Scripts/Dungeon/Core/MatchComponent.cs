using Fusion;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    public enum MatchState : byte
    {
        Waiting,
        Running,
        Finished
    }

    /// Match clock and the Dark Swarm per floor. The circle closes in stages towards a random point of each floor.
    public sealed class MatchComponent : NetworkBehaviour
    {
        public const int FloorCount = 2;

        public DungeonConfig Config => _config;
        public float Elapsed => State == MatchState.Running ? Runner.SecondsSince(StartTick) : 0f;
        public float TimeLeft => Mathf.Max(0f, _config.MatchDuration - Elapsed);
        public bool IsTimeUp => State == MatchState.Running && Elapsed >= _config.MatchDuration;
        public bool IsRunning => State == MatchState.Running;

        [Networked]
        public MatchState State { get; private set; }

        [Networked]
        public int StartTick { get; private set; }

        [Networked]
        public int Round { get; private set; }

        [Networked, Capacity(FloorCount)]
        private NetworkArray<Vector3> _swarmCenters => default;

        [Networked, Capacity(FloorCount)]
        private NetworkArray<float> _floorRadii => default;

        [SerializeField]
        private DungeonConfig _config;

        public override void Spawned()
        {
            if (DungeonContext.Instance != null)
                DungeonContext.Instance.SetMatch(this);
        }

        public void Begin(Vector3[] floorCenters, float[] floorRadii, Vector3[] finalCenters)
        {
            for (int i = 0; i < FloorCount; i++)
            {
                _swarmCenters.Set(i, finalCenters[i]);
                _floorRadii.Set(i, floorRadii[i]);
            }

            StartTick = Runner.Tick;
            State = MatchState.Running;
            Round++;
        }

        public void Finish()
        {
            State = MatchState.Finished;
        }

        public void Reset()
        {
            State = MatchState.Waiting;
        }

        /// Current safe radius for a floor; stages interpolate from the previous radius to the stage radius.
        public float GetSafeRadius(int floor)
        {
            int index = Mathf.Clamp(floor - 1, 0, FloorCount - 1);
            float radius = _floorRadii[index];

            if (State != MatchState.Running)
                return radius;

            float elapsed = Elapsed;
            float previous = radius;

            foreach (SwarmStage stage in _config.SwarmStages)
            {
                if (elapsed < stage.StartTime)
                    return previous;

                if (elapsed < stage.StartTime + stage.Duration)
                    return Mathf.Lerp(previous, stage.Radius, (elapsed - stage.StartTime) / stage.Duration);

                previous = stage.Radius;
            }

            return previous;
        }

        public Vector3 GetSwarmCenter(int floor)
        {
            return _swarmCenters[Mathf.Clamp(floor - 1, 0, FloorCount - 1)];
        }

        public float GetSwarmDamage(int floor, Vector3 position)
        {
            if (State != MatchState.Running)
                return 0f;

            if (TimeLeft < 60f)
                return _config.SwarmDamagePerSecond * 2f;

            Vector3 center = GetSwarmCenter(floor);
            float radius = GetSafeRadius(floor);
            Vector3 delta = position - center;
            delta.y = 0f;

            if (delta.magnitude <= radius)
                return 0f;

            float ramp = 1f + Elapsed / _config.MatchDuration;

            return _config.SwarmDamagePerSecond * ramp;
        }

        public float GetSwarmTimeToNextStage()
        {
            float elapsed = Elapsed;

            foreach (SwarmStage stage in _config.SwarmStages)
            {
                if (elapsed < stage.StartTime)
                    return stage.StartTime - elapsed;
            }

            return 0f;
        }
    }
}
