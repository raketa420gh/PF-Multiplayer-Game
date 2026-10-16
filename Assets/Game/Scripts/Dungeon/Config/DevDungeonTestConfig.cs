using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Scripts.Dungeon
{
    /// Dev Dungeon Test (queue mode DevTest): the catacombs hold a 2x2 grid of the key room (south-west) and these three rooms;
    /// corner rooms turn their blind sides out. An empty slot takes a random room.
    [CreateAssetMenu(menuName = "Game/Dungeon/Dev Dungeon Test Config")]
    public sealed class DevDungeonTestConfig : ScriptableObject
    {
        [SerializeField]
        private CatacombRoomComponent _east;

        [SerializeField]
        private CatacombRoomComponent _north;

        [SerializeField, FormerlySerializedAs("_south")]
        private CatacombRoomComponent _northEast;

        /// East, north, north-east of the key room.
        public CatacombRoomComponent[] Rooms()
        {
            return new[] { _east, _north, _northEast };
        }
    }
}
