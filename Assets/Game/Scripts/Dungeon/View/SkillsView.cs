using System;
using System.Collections.Generic;
using System.Text;
using Game.Scripts.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Scripts.Dungeon
{
    /// "Perks and Skills" page of the tavern: attribute sheet, the doll with the equipped perk and skill slots, the class pools to pick from.
    public sealed class SkillsView : DisplayableView
    {
        [SerializeField]
        private CharacterPreviewView _preview;

        [SerializeField]
        private AbilityIconView _perkIconPrefab;

        [SerializeField]
        private AbilityIconView _skillIconPrefab;

        [SerializeField]
        private RectTransform _perksRoot;

        [SerializeField]
        private RectTransform _skillsRoot;

        [SerializeField]
        private RectTransform _spellsRoot;

        [SerializeField, Tooltip("Equipped perks around the doll, in unlock order")]
        private AbilityIconView[] _perkSlots;

        [SerializeField, Tooltip("Q and E")]
        private AbilityIconView[] _skillSlots;

        [SerializeField]
        private TMP_Text _statNamesText;

        [SerializeField]
        private TMP_Text _statValuesText;

        [SerializeField]
        private RectTransform _tooltip;

        [SerializeField]
        private TMP_Text _tooltipText;

        private static readonly string[] s_skillKeys = { "Q", "E" };
        private readonly List<AbilityIconView> _perkIcons = new();
        private readonly List<AbilityIconView> _skillIcons = new();
        private readonly List<AbilityIconView> _spellIcons = new();
        private readonly StringBuilder _names = new();
        private readonly StringBuilder _values = new();
        private PlayerSessionComponent _session;
        private AdventurerStats _stats;
        private int _shownClass = -1;
        private (byte, byte, int, int, int) _shownBuild;

        private void Awake()
        {
            foreach (AbilityIconView slot in _perkSlots)
            {
                slot.OnClicked += OnPerkClicked;
                slot.OnHovered += OnHovered;
            }

            foreach (AbilityIconView slot in _skillSlots)
                slot.OnHovered += OnHovered;

            _tooltip.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_session == null || _session.Object == null || !_session.Object.IsValid)
                return;

            ClassConfig config = _session.Class;

            if (_shownClass != config.Id)
            {
                _shownClass = config.Id;
                _shownBuild = default;
                _preview.Bind(_session.Kit, config);
                BuildIcons(config);
            }

            (byte, byte, int, int, int) build = (_session.SkillA, _session.SkillB, _session.PerkMask, _session.Level, _session.SpellMask);

            if (_shownBuild != build)
            {
                _shownBuild = build;
                RefreshBuild(config);
            }

            RefreshStats();
        }

        public void Bind(PlayerSessionComponent session, AdventurerStats stats)
        {
            _session = session;
            _stats = stats;
            _shownClass = -1;
        }

        public override void Hide()
        {
            base.Hide();
            _tooltip.gameObject.SetActive(false);
        }

        private void BuildIcons(ClassConfig config)
        {
            Fill(_perkIcons, _perkIconPrefab, _perksRoot, config.Perks.Length, OnPerkClicked);
            Fill(_skillIcons, _skillIconPrefab, _skillsRoot, config.Skills.Length, OnSkillClicked);
            Fill(_spellIcons, _skillIconPrefab, _spellsRoot, config.Spells.Length, OnSpellClicked);

            for (int i = 0; i < _perkIcons.Count; i++)
                Show(_perkIcons[i], config, i);

            for (int i = 0; i < _skillIcons.Count; i++)
                Show(_skillIcons[i], config.Skills, i);

            for (int i = 0; i < _spellIcons.Count; i++)
                Show(_spellIcons[i], config.Spells, i);
        }

        private void Fill(List<AbilityIconView> icons, AbilityIconView prefab, RectTransform root, int count, Action<AbilityIconView, PointerEventData.InputButton> onClicked)
        {
            foreach (AbilityIconView icon in icons)
                Destroy(icon.gameObject);

            icons.Clear();

            for (int i = 0; i < count; i++)
            {
                AbilityIconView icon = Instantiate(prefab, root);
                icon.gameObject.SetActive(true);
                icon.SetSelected(false);
                icon.OnClicked += onClicked;
                icon.OnHovered += OnHovered;
                icons.Add(icon);
            }
        }

        private void RefreshBuild(ClassConfig config)
        {
            for (int i = 0; i < _skillIcons.Count; i++)
            {
                int slot = i == _session.SkillA ? 0 : i == _session.SkillB ? 1 : -1;
                _skillIcons[i].SetSelected(slot >= 0);
                _skillIcons[i].SetBadge(slot >= 0 ? s_skillKeys[slot] : string.Empty);
            }

            for (int slot = 0; slot < _skillSlots.Length; slot++)
            {
                int index = slot == 0 ? _session.SkillA : _session.SkillB;

                if (index < config.Skills.Length)
                    Show(_skillSlots[slot], config.Skills, index);
                else
                    _skillSlots[slot].Clear();

                _skillSlots[slot].SetSelected(index < config.Skills.Length);
                _skillSlots[slot].SetBadge(s_skillKeys[slot]);
            }

            int allowed = ClassConfig.PerkCountForLevel(_session.Level);
            int perk = -1;

            for (int slot = 0; slot < _perkSlots.Length; slot++)
            {
                do
                    perk++;
                while (perk < config.Perks.Length && (_session.PerkMask & (1 << perk)) == 0);

                bool isFilled = slot < allowed && perk < config.Perks.Length;

                if (isFilled)
                    Show(_perkSlots[slot], config, perk);
                else
                    _perkSlots[slot].Clear();

                _perkSlots[slot].SetSelected(isFilled);
                _perkSlots[slot].SetBadge(slot < allowed ? string.Empty : $"Lv {ClassConfig.PerkSlotLevel(slot)}");
            }

            for (int i = 0; i < _perkIcons.Count; i++)
                _perkIcons[i].SetSelected((_session.PerkMask & (1 << i)) != 0);

            for (int i = 0; i < _spellIcons.Count; i++)
                _spellIcons[i].SetSelected((_session.SpellMask & (1 << i)) != 0);
        }

        private void RefreshStats()
        {
            ClassStats attributes = _stats.Attributes;
            _names.Clear();
            _values.Clear();

            Number("Flesh", attributes.Flesh);
            Number("Grip", attributes.Grip);
            Number("Reflex", attributes.Reflex);
            Number("Craft", attributes.Craft);
            Number("Insight", attributes.Insight);
            Number("Resonance", attributes.Resonance);
            Number("Health", _stats.MaxHealth);
            Gap();
            Number("Physical Power", _stats.PhysicalPower);
            Bonus("Physical Damage Bonus", _stats.GetDamageMultiplier(DamageType.Physical));
            Number("Magical Power", _stats.MagicalPower);
            Bonus("Magical Damage Bonus", _stats.GetDamageMultiplier(DamageType.Magical));
            Row("Bonus Spell Charges", $"+{_stats.BonusCharges}", _stats.BonusCharges);
            Gap();
            Number("Armor Rating", _stats.ArmorRating);
            Share("Physical Damage Reduction", _stats.PhysicalReduction);
            Number("Magic Resistance", _stats.MagicResistance);
            Share("Magical Damage Reduction", _stats.MagicalReduction);
            Gap();
            Row("Move Speed", $"{_stats.MoveSpeedRating:0} ({_stats.MoveSpeedMultiplier * 100f:0}%)", 0f);
            Bonus("Action Speed", _stats.ActionSpeed);
            Bonus("Handling Speed", _stats.HandlingSpeed);
            Bonus("Spell Casting Speed", _stats.CastSpeed);
            Bonus("Interaction Speed", _stats.InteractionSpeed);
            Bonus("Cooldown Recovery", _stats.CooldownSpeed);
            Gap();
            Number("Poise", _stats.Poise);
            Bonus("Stagger Recovery", _stats.StaggerRecovery);
            Bonus("Guard", _stats.Guard);
            Bonus("Impact", _stats.Impact);
            Row("Load", $"{_stats.Load * 100f:0}%", 0f);
            Gap();
            Bonus("Weakpoint Damage", _stats.Weakpoint);
            Bonus("Perception", _stats.Perception);
            Bonus("Mending", _stats.Mending);
            Share("Control Resistance", _stats.ControlResistance);
            Number("Concentration", _stats.Concentration);

            _statNamesText.text = _names.ToString();
            _statValuesText.text = _values.ToString();
        }

        private void Number(string name, float value)
        {
            Row(name, value.ToString("0"), value);
        }

        private void Bonus(string name, float multiplier)
        {
            float percent = Mathf.Round((multiplier - 1f) * 100f);
            Row(name, $"{percent:+0;-0;0}%", percent);
        }

        private void Share(string name, float fraction)
        {
            float percent = Mathf.Round(fraction * 100f);
            Row(name, $"{percent:0}%", percent);
        }

        /// Positive values are highlighted, negative ones are red, zero stays plain.
        private void Row(string name, string value, float sign)
        {
            _names.AppendLine(name);
            _values.Append("<color=#").Append(sign > 0f ? "c8e632" : sign < 0f ? "e04a3a" : "f2ede0").Append('>').Append(value).AppendLine("</color>");
        }

        private void Gap()
        {
            _names.AppendLine();
            _values.AppendLine();
        }

        private static void Show(AbilityIconView icon, ClassConfig config, int perk)
        {
            PerkDefinition definition = config.Perks[perk];
            string[] words = definition.Name.Split(' ');
            string glyph = words.Length > 1 ? $"{words[0][0]}{words[1][0]}" : definition.Name.Substring(0, Mathf.Min(2, definition.Name.Length));
            icon.Set(perk, glyph, config.Color, $"<b>{definition.Name}</b>\n{definition.Description}");
        }

        private static void Show(AbilityIconView icon, AbilityConfig[] pool, int index)
        {
            AbilityConfig ability = pool[index];
            string detail = ability.IsSpell && !ability.IsCooldownBased ? $"{ability.Charges} charges · cast {ability.CastTime:0.##}s"
                : ability.Cooldown > 0f ? $"Cooldown {ability.Cooldown:0}s" : string.Empty;
            icon.Set(index, ability.Glyph, ability.Color, $"<b>{ability.DisplayName}</b>\n{ability.Description}\n<size=80%><color=#9a927f>{detail}</color></size>");
        }

        private void OnPerkClicked(AbilityIconView icon, PointerEventData.InputButton button)
        {
            if (icon.Index < 0)
                return;

            DungeonAudioComponent.PlayUi(DungeonSound.Click, 0.5f);
            _session?.RpcTogglePerk((byte)icon.Index);
        }

        private void OnSpellClicked(AbilityIconView icon, PointerEventData.InputButton button)
        {
            DungeonAudioComponent.PlayUi(DungeonSound.Click, 0.5f);
            _session?.RpcToggleSpell((byte)icon.Index);
        }

        private void OnSkillClicked(AbilityIconView icon, PointerEventData.InputButton button)
        {
            DungeonAudioComponent.PlayUi(DungeonSound.Click, 0.5f);
            _session?.RpcSelectSkill((byte)(button == PointerEventData.InputButton.Right ? 1 : 0), (byte)icon.Index);
        }

        private void OnHovered(AbilityIconView icon, bool isHovered)
        {
            bool isShown = isHovered && !string.IsNullOrEmpty(icon.Tooltip);
            _tooltip.gameObject.SetActive(isShown);

            if (!isShown)
                return;

            // The tooltip opens towards the middle of the screen so it never leaves it.
            bool isLeft = icon.transform.position.x < transform.position.x;
            _tooltipText.text = icon.Tooltip;
            _tooltip.pivot = new Vector2(isLeft ? 0f : 1f, 0.5f);
            _tooltip.position = icon.transform.position;
            _tooltip.anchoredPosition += new Vector2(isLeft ? 64f : -64f, 0f);
        }
    }
}
