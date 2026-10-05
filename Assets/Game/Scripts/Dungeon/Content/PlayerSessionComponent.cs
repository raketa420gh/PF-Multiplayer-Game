using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    public enum SessionState : byte
    {
        Lobby,
        InDungeon,
        Dead,
        Extracted
    }

    /// Per-player object of a scene session: class, level, kit and stash. The tavern edits them, the dungeon takes the kit
    /// in and hands the run result back; the owner's PlayerPrefs carry everything between the scenes.
    public sealed class PlayerSessionComponent : NetworkBehaviour, InventoryActionsComponent.IOwner
    {
        public const byte LoadKit = 0;
        /// Stash pages load as kinds LoadStash … LoadStash + StashPages - 1.
        public const byte LoadStash = 1;
        public const int StashPages = 4;

        public event Action OnStateChanged;

        public InventoryComponent Kit => _kit;
        public InventoryComponent Stash => _stashes[0];
        public IReadOnlyList<InventoryComponent> Stashes => _stashes;
        public InventoryActionsComponent Actions => _actions;
        public string DisplayName => Name.ToString();
        public ClassConfig Class => FindClass(ClassId);
        public MerchantConfig[] Merchants => _merchants;
        public ItemConfig Currency => _currency;
        /// Coins the player can pay with: every stash page and the kit bag together.
        public int Coins
        {
            get
            {
                int coins = _kit.CountOf(_currency.Id);

                foreach (InventoryComponent stash in _stashes)
                    coins += stash.CountOf(_currency.Id);

                return coins;
            }
        }

        public AdventurerComponent Adventurer => Runner != null && Runner.TryFindBehaviour(AdventurerId, out NetworkBehaviour b) ? b as AdventurerComponent : null;

        [Networked]
        public SessionState State { get; private set; }

        [Networked]
        public byte ClassId { get; private set; }

        [Networked]
        public int Level { get; private set; }

        [Networked]
        public int Experience { get; private set; }

        [Networked]
        public NetworkString<_32> Name { get; private set; }

        [Networked]
        public NetworkBehaviourId AdventurerId { get; private set; }

        [Networked]
        public int LastRunValue { get; private set; }

        [Networked]
        public int LastRunKills { get; private set; }

        [Networked]
        public int LastRunExperience { get; private set; }

        [Networked]
        public NetworkBool HasLoadedKit { get; private set; }

        [Networked]
        public byte SkillA { get; private set; }

        [Networked]
        public byte SkillB { get; private set; } = 1;

        [Networked]
        public int PerkMask { get; private set; } = 1;

        [Networked]
        public int SpellMask { get; private set; } = ClassConfig.DefaultSpellMask;

        [SerializeField]
        private InventoryComponent _kit;

        [SerializeField]
        private InventoryComponent[] _stashes = Array.Empty<InventoryComponent>();

        [SerializeField]
        private InventoryActionsComponent _actions;

        [SerializeField]
        private ClassConfig[] _classes;

        [SerializeField]
        private DungeonConfig _config;

        [SerializeField]
        private MerchantConfig[] _merchants = Array.Empty<MerchantConfig>();

        [SerializeField]
        private ItemConfig _currency;

        [SerializeField, Tooltip("Coins a player without a single one finds in the stash on entering a scene")]
        private int _pityCoins = 10;

        private readonly byte[][][] _loadChunks = new byte[LoadStash + StashPages][][];
        private readonly byte[] _loadBuffer = new byte[InventoryComponent.Capacity * ItemStack.ByteSize * 2];
        private int _loadedChunks;
        private int _loadTarget;
        private SessionState _renderedState;
        private int _savedKitVersion = -1;
        private readonly int[] _savedStashVersions = { -1, -1, -1, -1 };

        public override void Spawned()
        {
            _actions.SetOwner(this);
            _renderedState = State;

            if (HasStateAuthority)
            {
                Level = Mathf.Max(1, Level);
                Name = "Player " + Object.InputAuthority.PlayerId;
            }

            if (!HasInputAuthority)
                return;

            if (DungeonContext.Instance != null)
                DungeonContext.Instance.SetLocalSession(this);

            RpcSetProfile(StashService.LoadLevel(), StashService.LoadExperience(), StashService.LoadClass(), StashService.LoadName());
            RpcSetBuild(StashService.LoadSkillA(), StashService.LoadSkillB(), StashService.LoadPerkMask(), StashService.LoadSpellMask());
            SendInventory(LoadKit, StashService.LoadKit());

            for (int page = 0; page < StashPages; page++)
                SendInventory((byte)(LoadStash + page), StashService.LoadStash(page));
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (DungeonContext.Instance != null && DungeonContext.Instance.LocalSession == this)
                DungeonContext.Instance.SetLocalSession(null);
        }

        public override void Render()
        {
            if (_renderedState != State)
            {
                _renderedState = State;
                OnStateChanged?.Invoke();
            }

            // Inside the dungeon the kit is at stake: it is written back only when the run ends, emptied or extracted.
            if (State != SessionState.InDungeon)
                SaveLocal();
        }

        /// Stores the kit, stash, profile and build of the local player; they are what the next scene loads.
        public void SaveLocal()
        {
            if (!HasInputAuthority || !HasLoadedKit || DungeonContext.Instance == null || DungeonContext.Instance.IsSandbox)
                return;

            if (_savedKitVersion != _kit.Version)
            {
                _savedKitVersion = _kit.Version;
                StashService.SaveKit(_kit);
            }

            for (int page = 0; page < _stashes.Length; page++)
            {
                if (_savedStashVersions[page] == _stashes[page].Version)
                    continue;

                _savedStashVersions[page] = _stashes[page].Version;
                StashService.SaveStash(page, _stashes[page]);
            }

            StashService.SaveProfile(Level, Experience, ClassId, DisplayName);
            StashService.SaveBuild(SkillA, SkillB, PerkMask, SpellMask);
        }

        /// Nobody descends naked: an empty kit is replaced by the starting one.
        public void EnsureKit()
        {
            if (_kit.CountItems() == 0 && _kit.GetEquipped(EquipSlot.Weapon1Main).IsEmpty)
                GiveDefaultKit();
        }

        public void OnDied(AdventurerComponent adventurer)
        {
            LastRunValue = 0;
            LastRunKills = adventurer.Kills;
            LastRunExperience = adventurer.RunExperience;
            _kit.Clear();
            State = SessionState.Dead;
            AdventurerId = default;
        }

        public void OnExtracted(AdventurerComponent adventurer)
        {
            LastRunValue = adventurer.Inventory.TotalValue();
            LastRunKills = adventurer.Kills;
            LastRunExperience = adventurer.RunExperience + 5;
            AddExperience(5);
            _kit.CopyFrom(adventurer.Inventory);
            State = SessionState.Extracted;
            AdventurerId = default;
        }

        public void OnAdventurerSpawned(AdventurerComponent adventurer)
        {
            AdventurerId = adventurer.Id;
            State = SessionState.InDungeon;
        }

        public void AddExperience(int amount)
        {
            Experience += amount;

            while (Level < _config.MaxLevel && Experience >= _config.ExperienceForLevel(Level))
            {
                Experience -= _config.ExperienceForLevel(Level);
                Level++;
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RpcSelectClass(byte classId)
        {
            if (State != SessionState.Lobby || ClassId == classId)
                return;

            ClassId = classId;
            SkillA = 0;
            SkillB = 1;
            PerkMask = 1;
            SpellMask = ClassConfig.DefaultSpellMask;
            StoreKit();
            GiveDefaultKit();
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RpcSelectSkill(byte slot, byte index)
        {
            if (State != SessionState.Lobby || index >= Class.Skills.Length)
                return;

            if (slot == 0)
            {
                if (SkillB == index)
                    SkillB = SkillA;

                SkillA = index;
            }
            else
            {
                if (SkillA == index)
                    SkillA = SkillB;

                SkillB = index;
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RpcTogglePerk(byte index)
        {
            if (State != SessionState.Lobby || index >= Class.Perks.Length)
                return;

            int bit = 1 << index;

            if ((PerkMask & bit) != 0)
            {
                PerkMask &= ~bit;

                return;
            }

            if (CountBits(PerkMask) >= ClassConfig.PerkCountForLevel(Level))
                return;

            PerkMask |= bit;
        }

        /// Toggles a spell in one of the two spell wheels; a wheel holds at most SpellWheelSize spells and a spell sits in one wheel only.
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RpcToggleSpell(byte wheel, byte index)
        {
            if (State != SessionState.Lobby || index >= Class.Spells.Length || wheel >= ClassConfig.WheelCount)
                return;

            int bit = ClassConfig.WheelBit(wheel, index);
            int other = ClassConfig.WheelBit(1 - wheel, index);
            int wheelMask = ((1 << ClassConfig.WheelBits) - 1) << (wheel * ClassConfig.WheelBits);

            if ((SpellMask & bit) == 0 && CountBits(SpellMask & wheelMask) >= ClassConfig.SpellWheelSize)
                return;

            SpellMask = (SpellMask ^ bit) & ~other;
        }

        /// Buys one ware (a full stack of stackables) into the stash; coins leave the stash first, then the kit bag.
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RpcBuy(byte merchantIndex, short itemId)
        {
            if (State != SessionState.Lobby || merchantIndex >= _merchants.Length)
                return;

            MerchantConfig merchant = _merchants[merchantIndex];
            ItemConfig item = _kit.Database.Get(itemId);

            if (item == null || !merchant.Sells(item) || Coins < merchant.Price || !AddToStash(ItemStack.Create(item, item.MaxStack, merchant.Rarity)))
                return;

            int left = merchant.Price;

            foreach (InventoryComponent stash in _stashes)
                left -= stash.Remove(_currency.Id, left);

            _kit.Remove(_currency.Id, left);
        }

        /// Sells a bag item of the kit or of a stash page to the merchants; the coins go into the stash.
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RpcSell(NetworkBehaviourId source, int index)
        {
            if (State != SessionState.Lobby || !Runner.TryFindBehaviour(source, out NetworkBehaviour behaviour) || behaviour is not InventoryComponent from
                || (from != _kit && Array.IndexOf(_stashes, from) < 0))
                return;

            ItemStack stack = from.Bag[index];

            if (CanSell(stack) && (from != _kit || HasRoomInStash(_currency)))
                GiveCoins(DungeonFormulas.SellPrice(from.GetConfig(from.RemoveAt(index)), stack));
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RpcSellEquipped(EquipSlot slot)
        {
            ItemStack stack = _kit.GetEquipped(slot);

            if (State != SessionState.Lobby || !CanSell(stack) || !HasRoomInStash(_currency))
                return;

            _kit.SetEquipment(slot, default);
            GiveCoins(DungeonFormulas.SellPrice(_kit.GetConfig(stack), stack));
        }

        /// Coins themselves are not for sale.
        public bool CanSell(in ItemStack stack)
        {
            return !stack.IsEmpty && !stack.IsHidden && stack.ItemId != _currency.Id && _kit.GetConfig(stack) != null;
        }

        public bool HasRoomInStash(ItemConfig item)
        {
            return Array.Exists(_stashes, stash => stash.HasRoom(item));
        }

        /// Puts a stack on the first page with a free spot for it, so a stack is never split over pages.
        public bool AddToStash(ItemStack stack)
        {
            ItemConfig config = _kit.GetConfig(stack);

            foreach (InventoryComponent stash in _stashes)
            {
                if (config != null && stash.HasRoom(config) && stash.TryAdd(stack))
                    return true;
            }

            return false;
        }

        private void GiveCoins(int amount)
        {
            while (amount > 0)
            {
                int count = Mathf.Min(amount, _currency.MaxStack);

                if (!AddToStash(ItemStack.Create(_currency, count, _currency.BaseRarity)))
                    return;

                amount -= count;
            }
        }

        /// Everything the kit carries goes into the stash pages, grid positions of the kit ignored.
        private void StoreKit()
        {
            for (int i = 0; i < InventoryComponent.Capacity; i++)
            {
                if (!_kit.Bag[i].IsEmpty)
                    AddToStash(_kit.Bag[i]);
            }

            for (int i = 0; i < InventoryComponent.EquipmentCapacity; i++)
            {
                if (!_kit.Equipment[i].IsEmpty)
                    AddToStash(_kit.Equipment[i]);
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RpcSetBuild(byte skillA, byte skillB, int perkMask, int spellMask)
        {
            SkillA = skillA;
            SkillB = skillB;
            // A build saved for another class may name perks this one does not have; they would take up perk slots unseen.
            PerkMask = perkMask & ((1 << Class.Perks.Length) - 1);
            SpellMask = spellMask;
        }

        private static int CountBits(int value)
        {
            int count = 0;

            for (uint bits = (uint)value; bits != 0; bits >>= 1)
                count += (int)(bits & 1);

            return count;
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RpcResetKit()
        {
            if (State != SessionState.Lobby)
                return;

            GiveDefaultKit();
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RpcSetProfile(int level, int experience, byte classId, string name)
        {
            Level = Mathf.Clamp(level, 1, _config.MaxLevel);
            Experience = Mathf.Max(0, experience);
            ClassId = classId;

            if (!string.IsNullOrWhiteSpace(name))
                Name = name;
        }

        private void GiveDefaultKit()
        {
            ClassConfig config = Class;
            _kit.Clear();

            foreach (StartingItem entry in config.StartingKit)
            {
                if (entry.Item == null)
                    continue;

                ItemStack stack = ItemStack.Create(entry.Item, entry.Count, entry.Item.BaseRarity);

                if (entry.IsEquipped)
                    _kit.SetEquipment(entry.Slot, stack);
                else
                    _kit.TryAdd(stack);
            }

            HasLoadedKit = true;
        }

        private void SendInventory(byte kind, byte[] data)
        {
            _actions.SendLoad(kind, data ?? Array.Empty<byte>());
        }

        private ClassConfig FindClass(int id)
        {
            foreach (ClassConfig config in _classes)
            {
                if (config.Id == id)
                    return config;
            }

            return _classes[0];
        }

        bool InventoryActionsComponent.IOwner.CanEquip(ItemConfig item, EquipSlot slot)
        {
            ClassConfig config = Class;

            return item switch
            {
                WeaponItemConfig weapon => weapon.Fits(config),
                ArmorItemConfig armor => armor.Fits(config),
                _ => true
            };
        }

        bool InventoryActionsComponent.IOwner.CanAccess(InventoryComponent other)
        {
            return Array.IndexOf(_stashes, other) >= 0 && State == SessionState.Lobby;
        }

        void InventoryActionsComponent.IOwner.OnUseItem(InventoryComponent source, int bagIndex, EquipSlot slot)
        {
        }

        void InventoryActionsComponent.IOwner.OnDropItem(ItemStack stack)
        {
            AddToStash(stack);
        }

        void InventoryActionsComponent.IOwner.OnLoadChunk(byte kind, byte chunk, byte chunkCount, byte[] data)
        {
            if (kind >= LoadStash + _stashes.Length)
                return;

            if (_loadTarget != kind || _loadChunks[kind] == null || _loadChunks[kind].Length != chunkCount)
            {
                _loadChunks[kind] = new byte[chunkCount][];
                _loadTarget = kind;
                _loadedChunks = 0;
            }

            if (_loadChunks[kind][chunk] == null)
                _loadedChunks++;

            _loadChunks[kind][chunk] = data;

            if (_loadedChunks < chunkCount)
                return;

            int offset = 0;

            foreach (byte[] part in _loadChunks[kind])
            {
                Array.Copy(part, 0, _loadBuffer, offset, part.Length);
                offset += part.Length;
            }

            InventoryComponent target = kind == LoadKit ? _kit : _stashes[kind - LoadStash];
            StashService.Deserialize(target, _loadBuffer, offset);
            _loadChunks[kind] = null;

            if (kind == LoadKit)
            {
                if (offset == 0)
                    GiveDefaultKit();
                else
                    HasLoadedKit = true;
            }
            else if (kind == LoadStash + _stashes.Length - 1 && Coins == 0)
            {
                // The stash pages arrive after the kit, the last one last: a broke player still gets a few coins for the merchants.
                AddToStash(ItemStack.Create(_currency, _pityCoins, _currency.BaseRarity));
            }
        }
    }
}
