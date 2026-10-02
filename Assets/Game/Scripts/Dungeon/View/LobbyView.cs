using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Dungeon
{
    /// Tavern screen: pick a class, arrange the kit from the stash, enter the dungeon.
    public sealed class LobbyView : DisplayableView
    {
        [SerializeField]
        private DungeonContext _context;

        [SerializeField]
        private InventoryView _inventory;

        [SerializeField]
        private RectTransform _classButtonsRoot;

        [SerializeField]
        private Button _classButtonPrefab;

        [SerializeField]
        private TMP_Text _classInfoText;

        [SerializeField]
        private TMP_Text _profileText;

        [SerializeField]
        private TMP_Text _resultText;

        [SerializeField]
        private Button _enterButton;

        [SerializeField]
        private Button _resetKitButton;

        [SerializeField]
        private Button _wipeButton;

        private readonly StringBuilder _builder = new();
        private readonly AdventurerStats _previewStats = new();
        private PlayerSessionComponent _session;
        private int _shownClass = -1;

        private void Awake()
        {
            _enterButton.onClick.AddListener(() => _session?.RpcEnterDungeon());
            _resetKitButton.onClick.AddListener(() => _session?.RpcResetKit());
            _wipeButton.onClick.AddListener(Wipe);

            foreach (ClassConfig config in _context.Classes)
            {
                Button button = Instantiate(_classButtonPrefab, _classButtonsRoot);
                button.gameObject.SetActive(true);
                button.GetComponentInChildren<TMP_Text>().text = config.DisplayName;
                button.image.color = config.Color * 0.6f;
                byte id = config.Id;
                button.onClick.AddListener(() => _session?.RpcSelectClass(id));
            }
        }

        private void Update()
        {
            if (_session == null || _session.Object == null || !_session.Object.IsValid)
                return;

            ClassConfig config = _session.Class;

            if (_shownClass != config.Id)
            {
                _shownClass = config.Id;
                _classInfoText.text = BuildClassInfo(config);
                _inventory.Bind(_session.Kit, _session.Actions, _previewStats, config, "Kit", false);
            }

            _previewStats.Recalculate(config, _session.Kit, null, 0, ClassConfig.PerkCountForLevel(_session.Level));

            int needed = _context.Config.ExperienceForLevel(_session.Level);
            _profileText.text = $"{_session.DisplayName}   Level {_session.Level}   XP {_session.Experience}/{needed}   Perks {ClassConfig.PerkCountForLevel(_session.Level)}";
            _enterButton.interactable = _session.HasLoadedKit;
        }

        public void Bind(PlayerSessionComponent session)
        {
            _session = session;
            _shownClass = -1;

            if (session == null)
                return;

            _inventory.Bind(session.Kit, session.Actions, _previewStats, session.Class, "Kit", false);
            _inventory.SetOther(session.Stash, "Stash");
            _resultText.text = BuildResult(session);
        }

        private string BuildClassInfo(ClassConfig config)
        {
            ClassStats stats = config.BaseStats;
            _builder.Clear();
            _builder.AppendLine($"<size=130%><b>{config.DisplayName}</b></size>");
            _builder.AppendLine(config.Description);
            _builder.AppendLine();
            _builder.AppendLine($"STR {stats.Strength}  VIG {stats.Vigor}  AGI {stats.Agility}  DEX {stats.Dexterity}");
            _builder.AppendLine($"WILL {stats.Will}  KNOW {stats.Knowledge}  RES {stats.Resourcefulness}");
            _builder.AppendLine();
            _builder.AppendLine("<b>Skills</b> (Q / E)");

            foreach (AbilityConfig skill in config.Skills)
                _builder.AppendLine($"  {skill.Glyph} {skill.DisplayName} — {skill.Description}");

            if (config.Spells.Length > 0)
            {
                _builder.AppendLine("<b>Spells</b> (Z / X / V / R / T)");

                foreach (AbilityConfig spell in config.Spells)
                    _builder.AppendLine($"  {spell.Glyph} {spell.DisplayName} ×{spell.Charges} — {spell.Description}");
            }

            _builder.AppendLine("<b>Perks</b>");
            int unlocked = _session != null ? ClassConfig.PerkCountForLevel(_session.Level) : 1;

            for (int i = 0; i < config.Perks.Length; i++)
                _builder.AppendLine(i < unlocked ? $"  ✓ {config.Perks[i].Name} — {config.Perks[i].Description}" : $"  <color=#777>{config.Perks[i].Name} (level {1 + i * 5})</color>");

            return _builder.ToString();
        }

        private static string BuildResult(PlayerSessionComponent session)
        {
            return session.State switch
            {
                SessionState.Extracted => $"<color=#6cf>Extracted!</color>  Loot value {session.LastRunValue}g  ·  Kills {session.LastRunKills}  ·  XP +{session.LastRunExperience}",
                SessionState.Dead => $"<color=#f66>You died.</color>  All gear lost.  Kills {session.LastRunKills}  ·  XP +{session.LastRunExperience}",
                _ => string.Empty
            };
        }

        private void Wipe()
        {
            StashService.Clear();
            _session?.RpcResetKit();
        }
    }
}
