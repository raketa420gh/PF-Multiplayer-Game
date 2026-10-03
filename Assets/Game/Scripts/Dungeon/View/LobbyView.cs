using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Dungeon
{
    /// Tavern screen: pick a class, two skills and perks, arrange the kit from the stash, enter the dungeon.
    public sealed class LobbyView : DisplayableView
    {
        [SerializeField]
        private DungeonContext _context;

        [SerializeField]
        private InventoryView _inventory;

        [SerializeField]
        private CharacterPreviewView _preview;

        [SerializeField]
        private RectTransform _classButtonsRoot;

        [SerializeField]
        private Button _classButtonPrefab;

        [SerializeField]
        private TMP_Text _classInfoText;

        [SerializeField]
        private RectTransform _skillRowsRoot;

        [SerializeField]
        private RectTransform _skillRowPrefab;

        [SerializeField]
        private RectTransform _perkRowsRoot;

        [SerializeField]
        private Button _perkRowPrefab;

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
        private readonly List<RectTransform> _skillRows = new();
        private readonly List<Button> _perkRows = new();
        private readonly List<Button> _classButtons = new();
        private PlayerSessionComponent _session;
        private int _shownClass = -1;
        private byte _shownSkillA = 255;
        private byte _shownSkillB = 255;
        private int _shownPerks = -1;
        private int _shownLevel = -1;

        private void Awake()
        {
            _enterButton.onClick.AddListener(() => { Click(); _session?.RpcEnterDungeon(); });
            _resetKitButton.onClick.AddListener(() => { Click(); _session?.RpcResetKit(); });
            _wipeButton.onClick.AddListener(Wipe);

            foreach (ClassConfig config in _context.Classes)
            {
                Button button = Instantiate(_classButtonPrefab, _classButtonsRoot);
                button.gameObject.SetActive(true);
                button.GetComponentInChildren<TMP_Text>().text = config.DisplayName;
                button.image.color = config.Color * 0.6f;
                byte id = config.Id;
                button.onClick.AddListener(() => { Click(); _session?.RpcSelectClass(id); });
                _classButtons.Add(button);
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
                _preview.Bind(_session.Kit, config);
                BuildRows(config);

                for (int i = 0; i < _classButtons.Count; i++)
                    _classButtons[i].image.color = _context.Classes[i].Color * (_context.Classes[i].Id == config.Id ? 1f : 0.5f);
            }

            if (_shownSkillA != _session.SkillA || _shownSkillB != _session.SkillB || _shownPerks != _session.PerkMask || _shownLevel != _session.Level)
            {
                _shownSkillA = _session.SkillA;
                _shownSkillB = _session.SkillB;
                _shownPerks = _session.PerkMask;
                _shownLevel = _session.Level;
                RefreshRows(config);
            }

            _previewStats.Recalculate(config, _session.Kit, null, 0, ClassConfig.PerkCountForLevel(_session.Level), ShapeshiftForm.None, _session.PerkMask);

            int needed = _context.Config.ExperienceForLevel(_session.Level);
            _profileText.text = $"{_session.DisplayName}   ·   Level {_session.Level}   ·   XP {_session.Experience}/{needed}";
            _enterButton.interactable = _session.HasLoadedKit;
        }

        public void Bind(PlayerSessionComponent session)
        {
            _session = session;
            _shownClass = -1;
            _shownPerks = -1;

            if (session == null)
                return;

            _inventory.Bind(session.Kit, session.Actions, _previewStats, session.Class, "Kit", false);
            _inventory.SetOther(session.Stash, "Stash");
            _preview.Bind(session.Kit, session.Class);
            _resultText.text = BuildResult(session);
        }

        private void BuildRows(ClassConfig config)
        {
            foreach (RectTransform row in _skillRows)
                Destroy(row.gameObject);

            foreach (Button row in _perkRows)
                Destroy(row.gameObject);

            _skillRows.Clear();
            _perkRows.Clear();

            for (int i = 0; i < config.Skills.Length; i++)
            {
                RectTransform row = Instantiate(_skillRowPrefab, _skillRowsRoot);
                row.gameObject.SetActive(true);
                AbilityConfig skill = config.Skills[i];
                row.Find("Text").GetComponent<TMP_Text>().text = $"<color=#{ColorUtility.ToHtmlStringRGB(skill.Color)}>{skill.Glyph}</color> <b>{skill.DisplayName}</b>  <size=80%>{skill.Description}</size>";
                byte index = (byte)i;
                row.Find("Q").GetComponent<Button>().onClick.AddListener(() => { Click(); _session?.RpcSelectSkill(0, index); });
                row.Find("E").GetComponent<Button>().onClick.AddListener(() => { Click(); _session?.RpcSelectSkill(1, index); });
                _skillRows.Add(row);
            }

            for (int i = 0; i < config.Perks.Length; i++)
            {
                Button row = Instantiate(_perkRowPrefab, _perkRowsRoot);
                row.gameObject.SetActive(true);
                PerkDefinition perk = config.Perks[i];
                row.GetComponentInChildren<TMP_Text>().text = $"<b>{perk.Name}</b>  <size=80%>{perk.Description}</size>";
                byte index = (byte)i;
                row.onClick.AddListener(() => { Click(); _session?.RpcTogglePerk(index); });
                _perkRows.Add(row);
            }

            _shownPerks = -1;
        }

        private void RefreshRows(ClassConfig config)
        {
            for (int i = 0; i < _skillRows.Count; i++)
            {
                _skillRows[i].Find("Q").GetComponent<Image>().color = _session.SkillA == i ? new Color(0.75f, 0.55f, 0.2f) : new Color(0.2f, 0.18f, 0.15f);
                _skillRows[i].Find("E").GetComponent<Image>().color = _session.SkillB == i ? new Color(0.75f, 0.55f, 0.2f) : new Color(0.2f, 0.18f, 0.15f);
            }

            int allowed = ClassConfig.PerkCountForLevel(_session.Level);

            for (int i = 0; i < _perkRows.Count; i++)
            {
                bool isOn = (_session.PerkMask & (1 << i)) != 0;
                _perkRows[i].image.color = isOn ? new Color(0.45f, 0.35f, 0.15f) : new Color(0.12f, 0.1f, 0.08f);
            }

            _classInfoText.text = BuildClassInfo(config) + $"\n<size=80%>Perk slots: {allowed} (next at level {NextPerkLevel(_session.Level)})</size>";
        }

        private static int NextPerkLevel(int level)
        {
            return level < 5 ? 5 : level < 10 ? 10 : level < 15 ? 15 : 15;
        }

        private string BuildClassInfo(ClassConfig config)
        {
            ClassStats stats = config.BaseStats;
            _builder.Clear();
            _builder.AppendLine($"<size=130%><b>{config.DisplayName}</b></size>");
            _builder.AppendLine(config.Description);
            _builder.AppendLine($"FLESH {stats.Flesh}  GRIP {stats.Grip}  REFLEX {stats.Reflex}  CRAFT {stats.Craft}  INSIGHT {stats.Insight}  RESONANCE {stats.Resonance}");

            if (config.Spells.Length > 0)
            {
                _builder.Append("<b>Spells</b> (wheel): ");

                foreach (AbilityConfig spell in config.Spells)
                    _builder.Append(spell.DisplayName).Append(", ");

                _builder.Length -= 2;
                _builder.AppendLine();
            }

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

        private static void Click()
        {
            DungeonAudioComponent.PlayUi(DungeonSound.Click, 0.5f);
        }

        private void Wipe()
        {
            Click();
            StashService.Clear();
            _session?.RpcResetKit();
        }
    }
}
