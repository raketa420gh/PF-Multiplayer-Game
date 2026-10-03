using System;
using Game.Scripts.Battle;
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

    [Serializable]
    public struct ArmorOutfitEntry
    {
        public ArmorVisual Visual;
        public OutfitPart Part;
        public Material Material;
    }

    /// Every armor look: rigid pieces hung on bones and skinned outfit parts of the character model;
    /// shared by players, monsters and the preview.
    [CreateAssetMenu(menuName = "Game/Dungeon/Armor Piece Set")]
    public sealed class ArmorPieceSetConfig : ScriptableObject
    {
        public ArmorPieceEntry[] Entries => _entries;
        public ArmorOutfitEntry[] Outfits => _outfits;

        [SerializeField]
        private ArmorPieceEntry[] _entries = Array.Empty<ArmorPieceEntry>();

        [SerializeField]
        private ArmorOutfitEntry[] _outfits = Array.Empty<ArmorOutfitEntry>();
    }
}
