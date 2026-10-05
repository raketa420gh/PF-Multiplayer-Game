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

    /// Match state and the Dark Swarm. Every floor is a dungeon of its own: its clock starts when the first adventurer
    /// arrives, and its circle closes in stages from the whole floor towards a random point.
    public sealed class MatchComponent : NetworkBehaviour
    {
        public const int FloorCount = 1;

        public DungeonConfig Config => _config;
        public float Elapsed => State == MatchState.Running ? Runner.SecondsSince(StartTick) : 0f;
        public bool IsRunning => State == MatchState.Running;

        [Networked]
        public MatchState State { get; private set; }

        [Networked]
        public int StartTick { get; private set; }

        [Networked]
        public int Round { get; private set; }

        [Networked, Capacity(FloorCount)]
        private NetworkArray<Vector3> _floorCenters => default;

        [Networked, Capacity(FloorCount)]
        private NetworkArray<Vector3> _swarmCenters => default;

        [Networked, Capacity(FloorCount)]
        private NetworkArray<float> _floorRadii => default;

        [Networked, Capacity(FloorCount)]
        private NetworkArray<int> _floorStartTicks => default;

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
                _floorCenters.Set(i, floorCenters[i]);
                _swarmCenters.Set(i, finalCenters[i]);
                _floorRadii.Set(i, floorRadii[i]);
                _floorStartTicks.Set(i, 0);
            }

            StartTick = Runner.Tick;
            State = MatchState.Running;
            Round++;
            BeginFloor(1);
        }

        /// Starts the clock of a floor; until then its swarm stays wide open.
        public void BeginFloor(int floor)
        {
            _floorStartTicks.Set(Index(floor), Runner.Tick);
        }

        public void Finish()
        {
            State = MatchState.Finished;
        }

        public void ResetMatch()
        {
            State = MatchState.Waiting;
        }

        public float GetElapsed(int floor)
        {
            int tick = _floorStartTicks[Index(floor)];

            return State == MatchState.Running && tick > 0 ? Runner.SecondsSince(tick) : 0f;
        }

        public float GetTimeLeft(int floor)
        {
            return Mathf.Max(0f, _config.MatchDuration - GetElapsed(floor));
        }

        public bool IsTimeUp(int floor)
        {
            return GetElapsed(floor) >= _config.MatchDuration;
        }

        /// Current safe radius for a floor; stages interpolate from the previous radius to the stage radius.
        public float GetSafeRadius(int floor)
        {
            float radius = _floorRadii[Index(floor)];
            float elapsed = GetElapsed(floor);
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

        /// The circle starts on the floor center, where it covers every room, and drifts to its final point as it shrinks.
        public Vector3 GetSwarmCenter(int floor)
        {
            int index = Index(floor);
            float radius = _floorRadii[index];
            float closed = radius > 0f ? 1f - GetSafeRadius(floor) / radius : 1f;

            return Vector3.Lerp(_floorCenters[index], _swarmCenters[index], closed);
        }

        public float GetSwarmDamage(int floor, Vector3 position)
        {
            if (State != MatchState.Running)
                return 0f;

            if (GetTimeLeft(floor) < 60f)
                return _config.SwarmDamagePerSecond * 2f;

            Vector3 delta = position - GetSwarmCenter(floor);
            delta.y = 0f;

            if (delta.magnitude <= GetSafeRadius(floor))
                return 0f;

            float ramp = 1f + GetElapsed(floor) / _config.MatchDuration;

            return _config.SwarmDamagePerSecond * ramp;
        }

        public float GetSwarmTimeToNextStage(int floor)
        {
            float elapsed = GetElapsed(floor);

            foreach (SwarmStage stage in _config.SwarmStages)
            {
                if (elapsed < stage.StartTime)
                    return stage.StartTime - elapsed;
            }

            return 0f;
        }

        private static int Index(int floor)
        {
            return Mathf.Clamp(floor - 1, 0, FloorCount - 1);
        }
    }
}
