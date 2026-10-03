using Fusion;

namespace Game.Scripts.Dungeon
{
    /// One item instance inside an inventory grid or an equipment slot.
    public struct ItemStack : INetworkStruct
    {
        public const int ByteSize = 8;
        public const int LegacyByteSize = 6;
        private const byte HiddenFlag = 1;

        public short ItemId;
        public byte Count;
        public byte X;
        public byte Y;
        public byte Rarity;
        /// Rolls the random modifiers of the item, see ItemAffixes; 0 = a plain item without them.
        public ushort Seed;
        public byte Flags;

        public bool IsEmpty => ItemId <= 0;
        public ItemRarity RarityValue => (ItemRarity)Rarity;
        /// Loot nobody has searched yet: shown as an eye tile and locked until an adventurer discovers it.
        public bool IsHidden => (Flags & HiddenFlag) != 0;

        public static ItemStack Create(ItemConfig item, int count, ItemRarity rarity, int seed = 0)
        {
            return new ItemStack { ItemId = item.Id, Count = (byte)count, Rarity = (byte)rarity, Seed = (ushort)seed };
        }

        public bool CanStackWith(in ItemStack other)
        {
            return ItemId == other.ItemId && Rarity == other.Rarity && Seed == other.Seed && Flags == other.Flags;
        }

        public ItemStack At(int x, int y)
        {
            ItemStack copy = this;
            copy.X = (byte)x;
            copy.Y = (byte)y;

            return copy;
        }

        public ItemStack WithCount(int count)
        {
            ItemStack copy = this;
            copy.Count = (byte)count;

            return copy;
        }

        public ItemStack WithHidden(bool isHidden)
        {
            ItemStack copy = this;
            copy.Flags = (byte)(isHidden ? Flags | HiddenFlag : Flags & ~HiddenFlag);

            return copy;
        }

        public void Write(byte[] buffer, int offset)
        {
            buffer[offset] = (byte)(ItemId & 0xFF);
            buffer[offset + 1] = (byte)(ItemId >> 8);
            buffer[offset + 2] = Count;
            buffer[offset + 3] = X;
            buffer[offset + 4] = Y;
            buffer[offset + 5] = Rarity;
            buffer[offset + 6] = (byte)(Seed & 0xFF);
            buffer[offset + 7] = (byte)(Seed >> 8);
        }

        /// Saves made before the random modifiers store 6 bytes per item and no seed.
        public static ItemStack Read(byte[] buffer, int offset, int size = ByteSize)
        {
            return new ItemStack
            {
                ItemId = (short)(buffer[offset] | (buffer[offset + 1] << 8)),
                Count = buffer[offset + 2],
                X = buffer[offset + 3],
                Y = buffer[offset + 4],
                Rarity = buffer[offset + 5],
                Seed = size >= ByteSize ? (ushort)(buffer[offset + 6] | (buffer[offset + 7] << 8)) : (ushort)0
            };
        }
    }
}
