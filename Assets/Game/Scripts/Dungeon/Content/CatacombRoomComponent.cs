using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// A room of the Tangled Catacombs: one grid cell with two doorways on every side, so it may be turned any quarter; a corner
    /// room has them on its north and east sides only and goes into a corner of the grid. Its markers (CatacombMarkerComponent
    /// anywhere below it) tell the host where monsters, containers and traps go and where a team may begin a run.
    public sealed class CatacombRoomComponent : MonoBehaviour
    {
        public string Title => _title;
        public bool IsCorner => _isCorner;
        /// Parchment map of the cell, north up before the room is turned; readable, so the floor map is stitched from it.
        public Texture2D Map => _map;
        /// A point without a prefab takes any monster of the floor.
        public DungeonDirector.MonsterPlacement[] Monsters => Collect()._monsters;
        public DungeonDirector.MonsterPlacement[] Containers => Collect()._containers;
        public DungeonDirector.MonsterPlacement[] Traps => Collect()._traps;
        /// Spots of a starting team are its children; null when nobody starts here.
        public Transform StartSpots => Collect()._startSpots;

        [SerializeField]
        private string _title;

        [SerializeField]
        private bool _isCorner;

        [SerializeField]
        private Texture2D _map;

        private DungeonDirector.MonsterPlacement[] _monsters;
        private DungeonDirector.MonsterPlacement[] _containers;
        private DungeonDirector.MonsterPlacement[] _traps;
        private Transform _startSpots;

        private CatacombRoomComponent Collect()
        {
            if (_monsters != null)
                return this;

            List<DungeonDirector.MonsterPlacement>[] lists = { new(), new(), new() };

            foreach (CatacombMarkerComponent marker in GetComponentsInChildren<CatacombMarkerComponent>(true))
            {
                if (marker.Kind == CatacombMarkerKind.Start && _startSpots == null)
                    _startSpots = marker.transform;
                else if (marker.Kind != CatacombMarkerKind.Start)
                    lists[(int)marker.Kind].Add(new DungeonDirector.MonsterPlacement { Point = marker.transform, Prefab = marker.Prefab });
            }

            _monsters = lists[(int)CatacombMarkerKind.Monster].ToArray();
            _containers = lists[(int)CatacombMarkerKind.Container].ToArray();
            _traps = lists[(int)CatacombMarkerKind.Trap].ToArray();

            return this;
        }
    }
}
