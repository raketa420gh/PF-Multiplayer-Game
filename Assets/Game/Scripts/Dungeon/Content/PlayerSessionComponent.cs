using System;
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
        public const byte LoadStash = 1;

        public event Action OnStateChanged;

        public InventoryComponent Kit => _kit;
        public InventoryComponent Stash => _stash;
        public InventoryActionsComponent Actions => _actions;
        public string DisplayName => Name.ToString();
        public ClassConfig Class => FindClass(ClassId);
        public MerchantConfig[] Merchants => _merchants;
        public ItemConfig Currency => _currency;
        /// Coins the player can pay with: the stash and the kit bag together.
        public int Coins => _stash.CountOf(_currency.Id) + _kit.CountOf(_currency.Id);
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
        private InventoryComponent _stash;

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

        private readonly byte[][][] _loadChunks = new byte[2][][];
        private readonly byte[] _loadBuffer = new byte[InventoryComponent.Capacity * ItemStack.ByteSize * 2];
        private int _loadedChunks;
        private int _loadTarget;
        private SessionState _renderedState;
        private int _savedKitVersion = -1;
        private int _savedStashVersion = -1;

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
            SendInventory(LoadStash, StashService.LoadStash());
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

            if (_savedStashVersion != _stash.Version)
            {
                _savedStashVersion = _stash.Version;
                StashService.SaveStash(_stash);
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
            _stash.TakeAllFrom(_kit);
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

        /// Toggles a spell in the spell wheel; the wheel holds at most SpellWheelSize spells.
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RpcToggleSpell(byte index)
        {
            if (State != SessionState.Lobby || index >= Class.Spells.Length)
                return;

            int bit = 1 << index;

            if ((SpellMask & bit) == 0 && CountBits(SpellMask) >= ClassConfig.SpellWheelSize)
                return;

            SpellMask ^= bit;
        }

        /// Buys one ware (a full stack of stackables) into the stash; coins leave the stash first, then the kit bag.
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        public void RpcBuy(byte merchantIndex, short itemId)
        {
            if (State != SessionState.Lobby || merchantIndex >= _merchants.Length)
                return;

            MerchantConfig merchant = _merchants[merchantIndex];
            ItemConfig item = _stash.Database.Get(itemId);

            if (item == null || !merchant.Sells(item) || Coins < merchant.Price || !_stash.TryAdd(ItemStack.Create(item, item.MaxStack, merchant.Rarity)))
                return;

            _kit.Remove(_currency.Id, merchant.Price - _stash.Remove(_currency.Id, merchant.Price));
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RpcSetBuild(byte skillA, byte skillB, int perkMask, int spellMask)
        {
            SkillA = skillA;
            SkillB = skillB;
            PerkMask = perkMask;
            SpellMask = spellMask;
        }

        private static int CountBits(int value)
        {
            int count = 0;

            while (value != 0)
            {
                count += value & 1;
                value >>= 1;
            }

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
                WeaponItemConfig weapon => config.CanUseWeapon(weapon.WeaponClass),
                ArmorItemConfig armor => config.CanWearArmor(armor.ArmorType),
                _ => true
            };
        }

        bool InventoryActionsComponent.IOwner.CanAccess(InventoryComponent other)
        {
            return other == _stash && State == SessionState.Lobby;
        }

        void InventoryActionsComponent.IOwner.OnUseItem(InventoryComponent source, int bagIndex, EquipSlot slot)
        {
        }

        void InventoryActionsComponent.IOwner.OnDropItem(ItemStack stack)
        {
            _stash.TryAdd(stack);
        }

        void InventoryActionsComponent.IOwner.OnLoadChunk(byte kind, byte chunk, byte chunkCount, byte[] data)
        {
            if (kind > LoadStash)
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

            InventoryComponent target = kind == LoadKit ? _kit : _stash;
            StashService.Deserialize(target, _loadBuffer, offset);
            _loadChunks[kind] = null;

            if (kind == LoadKit)
            {
                if (offset == 0)
                    GiveDefaultKit();
                else
                    HasLoadedKit = true;
            }
            else if (Coins == 0)
            {
                // The stash arrives after the kit: a broke player still gets a few coins for the merchants.
                _stash.TryAdd(ItemStack.Create(_currency, _pityCoins, _currency.BaseRarity));
            }
        }
    }
}
