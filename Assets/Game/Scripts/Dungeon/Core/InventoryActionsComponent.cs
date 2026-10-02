using System;
using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Client requests for moving, equipping, using and dropping items. Validated and applied on the state authority.
    public sealed class InventoryActionsComponent : NetworkBehaviour
    {
        public interface IOwner
        {
            bool CanEquip(ItemConfig item, EquipSlot slot);
            bool CanAccess(InventoryComponent other);
            void OnUseItem(InventoryComponent source, int bagIndex, EquipSlot slot);
            void OnDropItem(ItemStack stack);
            void OnLoadChunk(byte kind, byte chunk, byte chunkCount, byte[] data);
        }

        public const int ChunkItems = 16;

        public InventoryComponent Inventory => _inventory;

        [SerializeField]
        private InventoryComponent _inventory;

        private IOwner _owner;

        public void SetOwner(IOwner owner)
        {
            _owner = owner;
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RpcMove(NetworkBehaviourId source, int fromIndex, NetworkBehaviourId target, int x, int y)
        {
            if (!TryResolve(source, out InventoryComponent from) || !TryResolve(target, out InventoryComponent to))
                return;

            ItemStack stack = from.Bag[fromIndex];
            ItemConfig config = from.GetConfig(stack);

            if (config == null)
                return;

            int occupied = to.FindBagIndexAt(x, y);

            if (from == to && occupied == fromIndex)
                occupied = -1;

            if (occupied >= 0)
            {
                ItemStack other = to.Bag[occupied];

                if (other.ItemId == stack.ItemId && config.MaxStack > 1 && other.Count < config.MaxStack)
                {
                    int moved = Mathf.Min(config.MaxStack - other.Count, stack.Count);
                    to.RemoveAt(occupied);
                    to.TryPlaceAt(other.WithCount(other.Count + moved), other.X, other.Y);
                    from.RemoveAt(fromIndex, moved);

                    return;
                }

                Swap(from, fromIndex, to, occupied, x, y);

                return;
            }

            if (!to.CanPlace(config, x, y, from == to ? fromIndex : -1))
                return;

            from.RemoveAt(fromIndex);
            to.TryPlaceAt(stack, x, y);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RpcEquip(NetworkBehaviourId source, int fromIndex, EquipSlot slot)
        {
            if (!TryResolve(source, out InventoryComponent from) || !_inventory.HasEquipment)
                return;

            ItemStack stack = from.Bag[fromIndex];
            ItemConfig config = from.GetConfig(stack);

            if (config == null || !config.CanEquip(slot) || _inventory.IsSlotBlocked(slot))
                return;

            if (_owner != null && !_owner.CanEquip(config, slot))
                return;

            ItemStack previous = _inventory.GetEquipped(slot);
            from.RemoveAt(fromIndex);
            _inventory.SetEquipment(slot, stack.At(0, 0));
            _inventory.OnMainHandEquipped(slot, out ItemStack displaced);

            ReturnItem(from, previous, stack.X, stack.Y);
            ReturnItem(from, displaced, -1, -1);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RpcUnequip(EquipSlot slot, NetworkBehaviourId target, int x, int y)
        {
            if (!TryResolve(target, out InventoryComponent to))
                return;

            ItemStack stack = _inventory.GetEquipped(slot);

            if (stack.IsEmpty)
                return;

            bool placed = x >= 0 ? to.TryPlaceAt(stack, x, y) : to.TryAdd(stack);

            if (placed)
                _inventory.SetEquipment(slot, default);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RpcSwapEquipment(EquipSlot first, EquipSlot second)
        {
            ItemStack a = _inventory.GetEquipped(first);
            ItemStack b = _inventory.GetEquipped(second);

            if (!a.IsEmpty && !_inventory.CanEquip(a, second) || !b.IsEmpty && !_inventory.CanEquip(b, first))
                return;

            _inventory.SetEquipment(first, b);
            _inventory.SetEquipment(second, a);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RpcDrop(NetworkBehaviourId source, int index)
        {
            if (!TryResolve(source, out InventoryComponent from))
                return;

            ItemStack stack = from.RemoveAt(index);

            if (!stack.IsEmpty)
                _owner?.OnDropItem(stack);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RpcDropEquipment(EquipSlot slot)
        {
            ItemStack stack = _inventory.GetEquipped(slot);

            if (stack.IsEmpty)
                return;

            _inventory.SetEquipment(slot, default);
            _owner?.OnDropItem(stack);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RpcUse(NetworkBehaviourId source, int index)
        {
            if (TryResolve(source, out InventoryComponent from))
                _owner?.OnUseItem(from, index, EquipSlot.Count);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RpcUseEquipped(EquipSlot slot)
        {
            _owner?.OnUseItem(_inventory, -1, slot);
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RpcTakeAll(NetworkBehaviourId source)
        {
            if (!TryResolve(source, out InventoryComponent from) || from == _inventory)
                return;

            for (int i = 0; i < InventoryComponent.Capacity; i++)
            {
                ItemStack stack = from.Bag[i];

                if (!stack.IsEmpty && _inventory.TryAdd(stack))
                    from.RemoveAt(i);
            }

            for (int i = 0; i < InventoryComponent.EquipmentCapacity; i++)
            {
                ItemStack stack = from.Equipment[i];

                if (!stack.IsEmpty && _inventory.TryAdd(stack))
                    from.SetEquipment((EquipSlot)i, default);
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RpcLoad(byte kind, byte chunk, byte chunkCount, byte[] data)
        {
            _owner?.OnLoadChunk(kind, chunk, chunkCount, data);
        }

        /// Splits the serialized inventory into RPC-sized chunks.
        public void SendLoad(byte kind, byte[] data)
        {
            int chunkBytes = ChunkItems * ItemStack.ByteSize;
            int chunkCount = Mathf.Max(1, Mathf.CeilToInt(data.Length / (float)chunkBytes));

            for (int i = 0; i < chunkCount; i++)
            {
                int length = Mathf.Min(chunkBytes, data.Length - i * chunkBytes);
                byte[] chunk = new byte[Mathf.Max(0, length)];
                Array.Copy(data, i * chunkBytes, chunk, 0, chunk.Length);
                RpcLoad(kind, (byte)i, (byte)chunkCount, chunk);
            }
        }

        private bool TryResolve(NetworkBehaviourId id, out InventoryComponent inventory)
        {
            inventory = null;

            if (!Runner.TryFindBehaviour(id, out NetworkBehaviour behaviour) || behaviour is not InventoryComponent found)
                return false;

            if (found != _inventory && (_owner == null || !_owner.CanAccess(found)))
                return false;

            inventory = found;

            return true;
        }

        private static void Swap(InventoryComponent from, int fromIndex, InventoryComponent to, int toIndex, int x, int y)
        {
            ItemStack a = from.Bag[fromIndex];
            ItemStack b = to.Bag[toIndex];
            ItemConfig configA = from.GetConfig(a);
            ItemConfig configB = to.GetConfig(b);

            from.RemoveAt(fromIndex);
            to.RemoveAt(toIndex);

            if (to.CanPlace(configA, x, y) && from.CanPlace(configB, a.X, a.Y))
            {
                to.TryPlaceAt(a, x, y);
                from.TryPlaceAt(b, a.X, a.Y);

                return;
            }

            from.TryPlaceAt(a, a.X, a.Y);
            to.TryPlaceAt(b, b.X, b.Y);
        }

        private static void ReturnItem(InventoryComponent target, ItemStack stack, int x, int y)
        {
            if (stack.IsEmpty)
                return;

            if (x < 0 || !target.TryPlaceAt(stack, x, y))
                target.TryAdd(stack);
        }
    }
}
