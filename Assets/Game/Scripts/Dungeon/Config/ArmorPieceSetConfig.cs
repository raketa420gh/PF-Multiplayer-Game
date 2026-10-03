using System;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    [Serializable]
    public struct ArmorPieceEntry
    {
        public ArmorVisual Visual;
        public GameObject Prefab;
        public HumanBodyBones Bone;
        public bool IsMirrored;
    }

    /// Which primitive piece hangs on which bone for every armor look; shared by players, monsters and the preview.
    [CreateAssetMenu(menuName = "Game/Dungeon/Armor Piece Set")]
    public sealed class ArmorPieceSetConfig : ScriptableObject
    {
        public ArmorPieceEntry[] Entries => _entries;

        [SerializeField]
        private ArmorPieceEntry[] _entries = Array.Empty<ArmorPieceEntry>();
    }
}
