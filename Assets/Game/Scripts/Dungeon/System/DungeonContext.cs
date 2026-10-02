using System;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Scene-level service locator for the dungeon: shared configs, local session and local adventurer.
    public sealed class DungeonContext : MonoBehaviour
    {
        public static DungeonContext Instance => s_instance;

        public event Action<PlayerSessionComponent> OnLocalSessionChanged;
        public event Action<AdventurerComponent> OnLocalAdventurerChanged;

        public BattleContext Battle => _battle;
        public ItemDatabase Items => _items;
        public ClassConfig[] Classes => _classes;
        public DungeonConfig Config => _config;
        public PlayerSessionComponent LocalSession => _localSession;
        public AdventurerComponent LocalAdventurer => _localAdventurer;
        public MatchComponent Match => _match;
        public DungeonDirector Director => _director;

        [SerializeField]
        private BattleContext _battle;

        [SerializeField]
        private ItemDatabase _items;

        [SerializeField]
        private ClassConfig[] _classes;

        [SerializeField]
        private DungeonConfig _config;

        [SerializeField]
        private DungeonDirector _director;

        private static DungeonContext s_instance;
        private PlayerSessionComponent _localSession;
        private AdventurerComponent _localAdventurer;
        private MatchComponent _match;

        private void Awake()
        {
            s_instance = this;
        }

        private void OnDestroy()
        {
            if (s_instance == this)
                s_instance = null;
        }

        public ClassConfig GetClass(int id)
        {
            foreach (ClassConfig config in _classes)
            {
                if (config.Id == id)
                    return config;
            }

            return _classes[0];
        }

        public void SetLocalSession(PlayerSessionComponent session)
        {
            _localSession = session;
            OnLocalSessionChanged?.Invoke(session);
        }

        public void SetLocalAdventurer(AdventurerComponent adventurer)
        {
            _localAdventurer = adventurer;
            OnLocalAdventurerChanged?.Invoke(adventurer);
        }

        public void SetMatch(MatchComponent match)
        {
            _match = match;
        }
    }
}
