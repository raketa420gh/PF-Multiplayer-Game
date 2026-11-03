using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Belt pouch: worn in a pouch slot, it opens extra bag cells under the bag.
    [CreateAssetMenu(menuName = "Game/Dungeon/Pouch Item")]
    public sealed class PouchItemConfig : ItemConfig
    {
        public override ItemKind Kind => ItemKind.Pouch;
        public int Cells => _cells;

        [SerializeField]
        private int _cells = 4;

        public override bool CanEquip(EquipSlot slot)
        {
            return slot is EquipSlot.Pouch1 or EquipSlot.Pouch2;
        }
    }
}
