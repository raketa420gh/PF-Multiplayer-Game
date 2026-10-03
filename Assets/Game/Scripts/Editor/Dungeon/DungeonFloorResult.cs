using System.Collections.Generic;
using Game.Scripts.Dungeon;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Everything the rooms of one floor hand over to the director: spawns, containers, portals and trap wiring.
    internal sealed class DungeonFloorResult
    {
        public readonly List<Transform> PlayerSpawns = new();
        public readonly List<Transform> MonsterSpawns = new();
        public readonly List<ContainerComponent> Containers = new();
        public readonly List<PortalComponent> EscapePortals = new();
        public readonly List<LeverComponent> Levers = new();
        public readonly List<TrapComponent> Traps = new();
        public PortalComponent DescendPortal;
        public Transform DescendDestination;
        public DoorComponent LockedDoor;
        public Transform BossSpawn;
        public Vector3 Center;
    }
}
