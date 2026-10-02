using Fusion;

namespace Game.Scripts.Dungeon
{
    /// One item instance inside an inventory grid or an equipment slot.
    public struct ItemStack : INetworkStruct
    {
        public const int ByteSize = 6;

        public short ItemId;
        public byte Count;
        public byte X;
        public byte Y;
        public byte Rarity;

        public bool IsEmpty => ItemId <= 0;
        public ItemRarity RarityValue => (ItemRarity)Rarity;

        public static ItemStack Create(ItemConfig item, int count, ItemRarity rarity)
        {
            return new ItemStack { ItemId = item.Id, Count = (byte)count, Rarity = (byte)rarity };
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

        public void Write(byte[] buffer, int offset)
        {
            buffer[offset] = (byte)(ItemId & 0xFF);
            buffer[offset + 1] = (byte)(ItemId >> 8);
            buffer[offset + 2] = Count;
            buffer[offset + 3] = X;
            buffer[offset + 4] = Y;
            buffer[offset + 5] = Rarity;
        }

        public static ItemStack Read(byte[] buffer, int offset)
        {
            return new ItemStack
            {
                ItemId = (short)(buffer[offset] | (buffer[offset + 1] << 8)),
                Count = buffer[offset + 2],
                X = buffer[offset + 3],
                Y = buffer[offset + 4],
                Rarity = buffer[offset + 5]
            };
        }
    }
}
