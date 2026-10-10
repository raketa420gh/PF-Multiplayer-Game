using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// A room of the Tangled Catacombs: one grid cell with two doorways on every side, so it may be turned any quarter; a corner
    /// room has them on its north and east sides only and goes into a corner of the grid. Its markers tell the host where
    /// monsters, containers and traps go; the start is where a team may begin a run.
    public sealed class CatacombRoomComponent : MonoBehaviour
    {
        public string Title => _title;
        public bool IsCorner => _isCorner;
        /// Parchment map of the cell, north up before the room is turned; readable, so the floor map is stitched from it.
        public Texture2D Map => _map;
        /// A point without a prefab takes any monster of the floor.
        public DungeonDirector.MonsterPlacement[] Monsters => _monsters;
        public DungeonDirector.MonsterPlacement[] Containers => _containers;
        public DungeonDirector.MonsterPlacement[] Traps => _traps;
        /// Spots of a starting team are its children; null when nobody starts here.
        public Transform StartSpots => _startSpots;

        [SerializeField]
        private string _title;

        [SerializeField]
        private bool _isCorner;

        [SerializeField]
        private Texture2D _map;

        [SerializeField]
        private DungeonDirector.MonsterPlacement[] _monsters;

        [SerializeField]
        private DungeonDirector.MonsterPlacement[] _containers;

        [SerializeField]
        private DungeonDirector.MonsterPlacement[] _traps;

        [SerializeField]
        private Transform _startSpots;
    }
}
