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
    /// arrives, and its circle closes in stages from the whole floor towards a random point in the second half of the clock.
    public sealed class MatchComponent : NetworkBehaviour
    {
        public const int FloorCount = 2;
        /// Floor a run starts on. The cursed village (floor 1) is cut for now: registration leads straight to the catacombs.
        public const int EntryFloor = 2;

        public DungeonConfig Config => _config;
        public float Elapsed => State == MatchState.Running ? Runner.SecondsSince(StartTick) : 0f;
        public bool IsRunning => State == MatchState.Running;

        [Networked]
        public MatchState State { get; private set; }

        [Networked]
        public int StartTick { get; private set; }

        [Networked]
        public int Round { get; private set; }

        /// Seed of the generated floor layout; every peer lays the same rooms out of it.
        [Networked]
        public int Seed { get; private set; }

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

        public void Begin(Vector3[] floorCenters, float[] floorRadii, Vector3[] finalCenters, int firstFloor)
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
            BeginFloor(firstFloor);
        }

        public void SetSeed(int seed)
        {
            Seed = seed;
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

        /// The swarm stays hidden and harmless until its first stage begins, and for good while the config turns it off.
        public bool IsSwarmActive(int floor)
        {
            return _config.IsSwarmEnabled && IsRunning && GetElapsed(floor) >= _config.SwarmStages[0].StartTime;
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
                    return Mathf.Lerp(previous, radius * stage.Share, (elapsed - stage.StartTime) / stage.Duration);

                previous = radius * stage.Share;
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
            if (!IsSwarmActive(floor))
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

        /// Seconds until the next escape portal of the floor shows up; negative once all of them have.
        public float GetTimeToNextPortal(int floor)
        {
            float elapsed = GetElapsed(floor);

            foreach (float time in _config.EscapePortalTimes)
            {
                if (elapsed < time)
                    return time - elapsed;
            }

            return -1f;
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
