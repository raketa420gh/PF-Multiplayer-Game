using System;
using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Networked grid inventory (bag) plus equipment slots. All mutations run on the state authority.
    public sealed class InventoryComponent : NetworkBehaviour
    {
        public const int Capacity = 60;
        public const int EquipmentCapacity = (int)EquipSlot.Count;

        public event Action OnChanged;

        public int Width => _width;
        public int Height => _height;
        public bool HasEquipment => _hasEquipment;
        public NetworkArray<ItemStack> Bag => _bag;
        public NetworkArray<ItemStack> Equipment => _equipment;
        public ItemDatabase Database => _database;

        [Networked]
        public int Version { get; private set; }

        [SerializeField]
        private ItemDatabase _database;

        [SerializeField]
        private int _width = 10;

        [SerializeField]
        private int _height = 4;

        [SerializeField]
        private bool _hasEquipment;

        [Networked, Capacity(Capacity)]
        private NetworkArray<ItemStack> _bag => default;

        [Networked, Capacity(EquipmentCapacity)]
        private NetworkArray<ItemStack> _equipment => default;

        private int _renderedVersion = -1;

        public override void Render()
        {
            if (_renderedVersion == Version)
                return;

            _renderedVersion = Version;
            OnChanged?.Invoke();
        }

        public ItemConfig GetConfig(in ItemStack stack)
        {
            return _database.Get(stack.ItemId);
        }

        public ItemStack GetEquipped(EquipSlot slot)
        {
            return _equipment[(int)slot];
        }

        public T GetEquippedConfig<T>(EquipSlot slot) where T : ItemConfig
        {
            return _database.Get<T>(_equipment[(int)slot].ItemId);
        }

        public int CountItems()
        {
            int count = 0;

            for (int i = 0; i < Capacity; i++)
                count += _bag[i].IsEmpty ? 0 : 1;

            return count;
        }

        public int TotalValue()
        {
            int value = 0;

            for (int i = 0; i < Capacity; i++)
            {
                ItemStack stack = _bag[i];

                if (!stack.IsEmpty)
                    value += GetConfig(stack).Value * stack.Count;
            }

            for (int i = 0; i < EquipmentCapacity; i++)
            {
                ItemStack stack = _equipment[i];

                if (!stack.IsEmpty)
                    value += GetConfig(stack).Value * stack.Count;
            }

            return value;
        }

        public int FindBagIndexAt(int x, int y)
        {
            for (int i = 0; i < Capacity; i++)
            {
                ItemStack stack = _bag[i];

                if (stack.IsEmpty)
                    continue;

                ItemConfig config = GetConfig(stack);

                if (x >= stack.X && x < stack.X + config.Width && y >= stack.Y && y < stack.Y + config.Height)
                    return i;
            }

            return -1;
        }

        public bool CanPlace(ItemConfig config, int x, int y, int ignoreIndex = -1)
        {
            if (x < 0 || y < 0 || x + config.Width > _width || y + config.Height > _height)
                return false;

            for (int i = 0; i < Capacity; i++)
            {
                if (i == ignoreIndex)
                    continue;

                ItemStack other = _bag[i];

                if (other.IsEmpty)
                    continue;

                ItemConfig otherConfig = GetConfig(other);

                if (x < other.X + otherConfig.Width && x + config.Width > other.X &&
                    y < other.Y + otherConfig.Height && y + config.Height > other.Y)
                    return false;
            }

            return true;
        }

        public bool TryAdd(ItemStack stack)
        {
            ItemConfig config = GetConfig(stack);

            if (config == null)
                return false;

            if (config.MaxStack > 1)
            {
                for (int i = 0; i < Capacity && stack.Count > 0; i++)
                {
                    ItemStack other = _bag[i];

                    if (!other.CanStackWith(stack) || other.Count >= config.MaxStack)
                        continue;

                    int moved = Mathf.Min(config.MaxStack - other.Count, stack.Count);
                    _bag.Set(i, other.WithCount(other.Count + moved));
                    stack.Count = (byte)(stack.Count - moved);
                }

                if (stack.Count == 0)
                {
                    Version++;

                    return true;
                }
            }

            int free = FindFreeIndex();

            if (free < 0 || !TryFindCell(config, out int cellX, out int cellY))
                return false;

            _bag.Set(free, stack.At(cellX, cellY));
            Version++;

            return true;
        }

        /// True when a new stack of the item fits somewhere in the bag.
        public bool HasRoom(ItemConfig config)
        {
            return FindFreeIndex() >= 0 && TryFindCell(config, out _, out _);
        }

        /// Items of one kind lying in the bag, all stacks together.
        public int CountOf(int itemId)
        {
            int count = 0;

            for (int i = 0; i < Capacity; i++)
                count += _bag[i].ItemId == itemId ? _bag[i].Count : 0;

            return count;
        }

        /// Takes up to the given number of items out of the bag stacks; returns how many were taken.
        public int Remove(int itemId, int count)
        {
            int left = count;

            for (int i = 0; i < Capacity && left > 0; i++)
            {
                ItemStack stack = _bag[i];

                if (stack.ItemId != itemId)
                    continue;

                int taken = Mathf.Min(left, stack.Count);
                left -= taken;
                _bag.Set(i, stack.Count > taken ? stack.WithCount(stack.Count - taken) : default);
            }

            if (left < count)
                Version++;

            return count - left;
        }

        public bool TryPlaceAt(ItemStack stack, int x, int y)
        {
            ItemConfig config = GetConfig(stack);
            int free = FindFreeIndex();

            if (config == null || free < 0 || !CanPlace(config, x, y))
                return false;

            _bag.Set(free, stack.At(x, y));
            Version++;

            return true;
        }

        public ItemStack RemoveAt(int index, int count = 0)
        {
            ItemStack stack = _bag[index];

            if (stack.IsEmpty)
                return default;

            if (count > 0 && count < stack.Count)
            {
                _bag.Set(index, stack.WithCount(stack.Count - count));
                Version++;

                return stack.WithCount(count);
            }

            _bag.Set(index, default);
            Version++;

            return stack;
        }

        /// Bag index of the unsearched item closest to the top-left corner, -1 when everything is discovered.
        public int FindHidden()
        {
            int found = -1;
            int best = int.MaxValue;

            for (int i = 0; i < Capacity; i++)
            {
                ItemStack stack = _bag[i];
                int order = stack.Y * _width + stack.X;

                if (stack.IsEmpty || !stack.IsHidden || order >= best)
                    continue;

                best = order;
                found = i;
            }

            return found;
        }

        public void Reveal(int index)
        {
            _bag.Set(index, _bag[index].WithHidden(false));
            Version++;
        }

        public void SetEquipment(EquipSlot slot, ItemStack stack)
        {
            _equipment.Set((int)slot, stack);
            Version++;
        }

        public void Clear()
        {
            for (int i = 0; i < Capacity; i++)
                _bag.Set(i, default);

            for (int i = 0; i < EquipmentCapacity; i++)
                _equipment.Set(i, default);

            Version++;
        }

        public void CopyFrom(InventoryComponent other)
        {
            for (int i = 0; i < Capacity; i++)
                _bag.Set(i, other._bag[i]);

            for (int i = 0; i < EquipmentCapacity; i++)
                _equipment.Set(i, other._equipment[i]);

            Version++;
        }

        /// Drops everything the other inventory holds into this bag, ignoring grid positions of the source.
        public void TakeAllFrom(InventoryComponent other)
        {
            for (int i = 0; i < Capacity; i++)
            {
                ItemStack stack = other._bag[i];

                if (!stack.IsEmpty)
                    TryAdd(stack);
            }

            for (int i = 0; i < EquipmentCapacity; i++)
            {
                ItemStack stack = other._equipment[i];

                if (!stack.IsEmpty)
                    TryAdd(stack);
            }
        }

        /// Repacks the bag: biggest items first, stacks merged, top-left fill.
        public void Sort()
        {
            System.Collections.Generic.List<ItemStack> stacks = new();

            for (int i = 0; i < Capacity; i++)
            {
                if (!_bag[i].IsEmpty)
                    stacks.Add(_bag[i]);

                _bag.Set(i, default);
            }

            stacks.Sort((a, b) =>
            {
                ItemConfig configA = GetConfig(a);
                ItemConfig configB = GetConfig(b);
                int area = configB.Width * configB.Height - configA.Width * configA.Height;

                return area != 0 ? area : a.ItemId.CompareTo(b.ItemId);
            });

            foreach (ItemStack stack in stacks)
                TryAdd(stack);

            Version++;
        }

        /// Removes one lockpick (or other utility) from the belt or bag. Returns false when none is carried.
        public bool TryConsumeUtility(UtilityKind kind)
        {
            for (int i = 0; i < EquipmentCapacity; i++)
            {
                ItemStack stack = _equipment[i];

                if (GetConfig(stack) is UtilityItemConfig utility && utility.UtilityKind == kind)
                {
                    _equipment.Set(i, stack.Count > 1 ? stack.WithCount(stack.Count - 1) : default);
                    Version++;

                    return true;
                }
            }

            for (int i = 0; i < Capacity; i++)
            {
                if (GetConfig(_bag[i]) is UtilityItemConfig utility && utility.UtilityKind == kind)
                {
                    RemoveAt(i, 1);

                    return true;
                }
            }

            return false;
        }

        public bool CanEquip(ItemStack stack, EquipSlot slot)
        {
            ItemConfig config = GetConfig(stack);

            return config != null && config.CanEquip(slot);
        }

        /// Two-handed weapons occupy both hands of a set; off-hand items are blocked by them.
        public bool IsSlotBlocked(EquipSlot slot)
        {
            EquipSlot main = slot switch
            {
                EquipSlot.Weapon1Off => EquipSlot.Weapon1Main,
                EquipSlot.Weapon2Off => EquipSlot.Weapon2Main,
                _ => EquipSlot.Count
            };

            if (main == EquipSlot.Count)
                return false;

            WeaponItemConfig weapon = GetEquippedConfig<WeaponItemConfig>(main);

            return weapon != null && weapon.IsTwoHanded;
        }

        public void OnMainHandEquipped(EquipSlot mainSlot, out ItemStack displaced)
        {
            displaced = default;
            WeaponItemConfig weapon = GetEquippedConfig<WeaponItemConfig>(mainSlot);
            EquipSlot off = mainSlot == EquipSlot.Weapon1Main ? EquipSlot.Weapon1Off : EquipSlot.Weapon2Off;

            if (weapon == null || !weapon.IsTwoHanded || _equipment[(int)off].IsEmpty)
                return;

            displaced = _equipment[(int)off];
            _equipment.Set((int)off, default);
            Version++;
        }

        public void Serialize(byte[] buffer, out int bagCount, out int equipmentCount)
        {
            int offset = 0;
            bagCount = 0;
            equipmentCount = 0;

            for (int i = 0; i < Capacity; i++)
            {
                ItemStack stack = _bag[i];

                if (stack.IsEmpty)
                    continue;

                stack.Write(buffer, offset);
                offset += ItemStack.ByteSize;
                bagCount++;
            }

            for (int i = 0; i < EquipmentCapacity; i++)
            {
                ItemStack stack = _equipment[i];

                if (stack.IsEmpty)
                    continue;

                stack.At(i, 0).Write(buffer, offset);
                offset += ItemStack.ByteSize;
                equipmentCount++;
            }
        }

        public void Deserialize(byte[] buffer, int bagCount, int equipmentCount)
        {
            Clear();
            int offset = 0;
            int count = bagCount + equipmentCount;
            int size = count > 0 && buffer.Length < count * ItemStack.ByteSize ? ItemStack.LegacyByteSize : ItemStack.ByteSize;

            for (int i = 0; i < bagCount; i++, offset += size)
            {
                ItemStack stack = ItemStack.Read(buffer, offset, size);
                ItemConfig config = GetConfig(stack);

                if (config != null && !TryPlaceAt(stack, stack.X, stack.Y))
                    TryAdd(stack);
            }

            for (int i = 0; i < equipmentCount; i++, offset += size)
            {
                ItemStack stack = ItemStack.Read(buffer, offset, size);
                EquipSlot slot = (EquipSlot)stack.X;

                if (slot < EquipSlot.Count && CanEquip(stack, slot))
                    _equipment.Set((int)slot, stack.At(0, 0));
            }

            Version++;
        }

        private bool TryFindCell(ItemConfig config, out int x, out int y)
        {
            for (y = 0; y <= _height - config.Height; y++)
            {
                for (x = 0; x <= _width - config.Width; x++)
                {
                    if (CanPlace(config, x, y))
                        return true;
                }
            }

            x = -1;
            y = -1;

            return false;
        }

        private int FindFreeIndex()
        {
            for (int i = 0; i < Capacity; i++)
            {
                if (_bag[i].IsEmpty)
                    return i;
            }

            return -1;
        }
    }
}
