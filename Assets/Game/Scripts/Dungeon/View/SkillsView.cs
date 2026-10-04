using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Scripts.Dungeon
{
    /// "Perks and Skills" page of the tavern: attribute sheet, the doll with the equipped perk and skill slots, the class pools to pick from.
    /// Perks and skills are equipped by a click or by dragging them onto a slot of their kind.
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

        [SerializeField, Tooltip("Equipped perks around the doll")]
        private AbilityIconView[] _perkSlots;

        [SerializeField, Tooltip("Q and E")]
        private AbilityIconView[] _skillSlots;

        [SerializeField]
        private StatsView _stats;

        [SerializeField]
        private RectTransform _tooltip;

        [SerializeField]
        private TMP_Text _tooltipText;

        private const int NoPerk = -1;

        private static readonly string[] s_skillKeys = { "Q", "E" };
        private readonly List<AbilityIconView> _perkIcons = new();
        private readonly List<AbilityIconView> _skillIcons = new();
        private readonly List<AbilityIconView> _spellIcons = new();
        private PlayerSessionComponent _session;
        private int _shownClass = -1;
        private (byte, byte, int, int, int) _shownBuild;
        /// Which perk each slot shows. The session only knows the set of perks; their places around the doll are the page's own.
        private int[] _slotPerks;
        private int _dropSlot = NoPerk;
        private AbilityIconView _perkGhost;
        private AbilityIconView _skillGhost;
        private AbilityIconView _ghost;
        private AbilityIconView _dragged;
        private bool _isDropHandled;

        private void Awake()
        {
            _slotPerks = new int[_perkSlots.Length];
            Array.Fill(_slotPerks, NoPerk);

            foreach (AbilityIconView slot in _perkSlots)
            {
                slot.OnClicked += OnPerkClicked;
                Subscribe(slot);
                slot.OnDropped += OnDropped;
            }

            foreach (AbilityIconView slot in _skillSlots)
            {
                Subscribe(slot);
                slot.OnDropped += OnDropped;
            }

            _perkGhost = CreateGhost(_perkIconPrefab);
            _skillGhost = CreateGhost(_skillIconPrefab);
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
                Array.Fill(_slotPerks, NoPerk);
                _preview.Bind(_session.Kit, config);
                BuildIcons(config);
            }

            (byte, byte, int, int, int) build = (_session.SkillA, _session.SkillB, _session.PerkMask, _session.Level, _session.SpellMask);

            if (_shownBuild != build)
            {
                _shownBuild = build;
                RefreshBuild(config);
            }
        }

        public void Bind(PlayerSessionComponent session, AdventurerStats stats)
        {
            _session = session;
            _stats.Bind(stats);
            _shownClass = -1;
        }

        public override void Hide()
        {
            base.Hide();
            _tooltip.gameObject.SetActive(false);
            StopDrag();
        }

        private AbilityIconView CreateGhost(AbilityIconView prefab)
        {
            AbilityIconView ghost = Instantiate(prefab, transform);
            CanvasGroup group = ghost.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.alpha = 0.9f;
            ghost.SetSelected(true);
            ghost.SetBadge(string.Empty);

            return ghost;
        }

        private void Subscribe(AbilityIconView icon)
        {
            icon.OnHovered += OnHovered;
            icon.OnDragStarted += OnDragStarted;
            icon.OnDragged += OnDragged;
            icon.OnDragEnded += OnDragEnded;
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
                Subscribe(icon);
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

            int allowed = Mathf.Min(ClassConfig.PerkCountForLevel(_session.Level), _perkSlots.Length);
            PlacePerks(config, allowed);

            for (int slot = 0; slot < _perkSlots.Length; slot++)
            {
                bool isFilled = _slotPerks[slot] != NoPerk;

                if (isFilled)
                    Show(_perkSlots[slot], config, _slotPerks[slot]);
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

        /// Perks that left the build free their slots; new ones take the slot they were dropped on, or the first free one.
        private void PlacePerks(ClassConfig config, int allowed)
        {
            for (int slot = 0; slot < _slotPerks.Length; slot++)
            {
                int perk = _slotPerks[slot];

                if (perk != NoPerk && (slot >= allowed || perk >= config.Perks.Length || (_session.PerkMask & (1 << perk)) == 0))
                    _slotPerks[slot] = NoPerk;
            }

            for (int perk = 0; perk < config.Perks.Length; perk++)
            {
                if ((_session.PerkMask & (1 << perk)) == 0 || Array.IndexOf(_slotPerks, perk) >= 0)
                    continue;

                bool isDropFree = _dropSlot != NoPerk && _dropSlot < allowed && _slotPerks[_dropSlot] == NoPerk;
                int slot = isDropFree ? _dropSlot : Array.IndexOf(_slotPerks, NoPerk);

                if (slot < 0 || slot >= allowed)
                    break;

                _slotPerks[slot] = perk;
                _dropSlot = NoPerk;
            }
        }

        private static void Show(AbilityIconView icon, ClassConfig config, int perk)
        {
            PerkDefinition definition = config.Perks[perk];
            string[] words = definition.Name.Split(' ');
            string glyph = words.Length > 1 ? $"{words[0][0]}{words[1][0]}" : definition.Name.Substring(0, Mathf.Min(2, definition.Name.Length));
            icon.Set(perk, definition.Icon, glyph, config.Color, $"<b>{definition.Name}</b>\n{definition.Description}");
        }

        private static void Show(AbilityIconView icon, AbilityConfig[] pool, int index)
        {
            AbilityConfig ability = pool[index];
            string detail = ability.IsSpell && !ability.IsCooldownBased ? $"{ability.Charges} charges · cast {ability.CastTime:0.##}s"
                : ability.Cooldown > 0f ? $"Cooldown {ability.Cooldown:0}s" : string.Empty;
            icon.Set(index, ability.Icon, ability.Glyph, ability.Color, $"<b>{ability.DisplayName}</b>\n{ability.Description}\n<size=80%><color=#9a927f>{detail}</color></size>");
        }

        private bool IsPerk(AbilityIconView icon)
        {
            return _perkIcons.Contains(icon) || Array.IndexOf(_perkSlots, icon) >= 0;
        }

        private bool IsSkill(AbilityIconView icon)
        {
            return _skillIcons.Contains(icon) || Array.IndexOf(_skillSlots, icon) >= 0;
        }

        /// A perk dropped on a slot: an equipped one trades places with the slot, a new one replaces whatever the slot held.
        private void DropPerk(int slot)
        {
            int perk = _dragged.Index;
            int from = Array.IndexOf(_slotPerks, perk);
            int occupant = _slotPerks[slot];

            if (slot >= ClassConfig.PerkCountForLevel(_session.Level) || from == slot)
                return;

            DungeonAudioComponent.PlayUi(DungeonSound.Click, 0.5f);

            if (from >= 0)
            {
                _slotPerks[from] = occupant;
                _slotPerks[slot] = perk;

                return;
            }

            if (occupant != NoPerk)
                _session.RpcTogglePerk((byte)occupant);

            _dropSlot = slot;
            _session.RpcTogglePerk((byte)perk);
        }

        private void MoveGhost(PointerEventData eventData)
        {
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)transform, eventData.position, eventData.pressEventCamera, out Vector3 point))
                _ghost.transform.position = point;
        }

        private void StopDrag()
        {
            if (_dragged == null)
                return;

            _dragged = null;
            _ghost.gameObject.SetActive(false);
            // The slots get their own frame colours back with the next refresh.
            _shownBuild = default;
        }

        private void OnPerkClicked(AbilityIconView icon, PointerEventData.InputButton button)
        {
            if (icon.Index < 0)
                return;

            DungeonAudioComponent.PlayUi(DungeonSound.Click, 0.5f);
            _dropSlot = NoPerk;
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
            bool isShown = isHovered && _dragged == null && !string.IsNullOrEmpty(icon.Tooltip);
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

        private void OnDragStarted(AbilityIconView icon, PointerEventData eventData)
        {
            bool isPerk = IsPerk(icon);

            if (_session == null || icon.Index < 0 || eventData.button != PointerEventData.InputButton.Left || (!isPerk && !IsSkill(icon)))
                return;

            _dragged = icon;
            _isDropHandled = false;
            _ghost = isPerk ? _perkGhost : _skillGhost;
            _ghost.CopyFrom(icon);
            _ghost.gameObject.SetActive(true);
            _ghost.transform.SetAsLastSibling();
            _tooltip.gameObject.SetActive(false);
            MoveGhost(eventData);

            int allowed = ClassConfig.PerkCountForLevel(_session.Level);

            for (int slot = 0; slot < _perkSlots.Length; slot++)
                _perkSlots[slot].SetTargeted(isPerk && slot < allowed);

            foreach (AbilityIconView slot in _skillSlots)
                slot.SetTargeted(!isPerk);
        }

        private void OnDragged(AbilityIconView icon, PointerEventData eventData)
        {
            if (_dragged == icon)
                MoveGhost(eventData);
        }

        private void OnDropped(AbilityIconView target)
        {
            if (_dragged == null)
                return;

            int perkSlot = Array.IndexOf(_perkSlots, target);
            int skillSlot = Array.IndexOf(_skillSlots, target);
            _isDropHandled = true;

            if (perkSlot >= 0 && IsPerk(_dragged))
            {
                DropPerk(perkSlot);
            }
            else if (skillSlot >= 0 && IsSkill(_dragged))
            {
                DungeonAudioComponent.PlayUi(DungeonSound.Click, 0.5f);
                _session.RpcSelectSkill((byte)skillSlot, (byte)_dragged.Index);
            }
        }

        private void OnDragEnded(AbilityIconView icon)
        {
            if (_dragged != icon)
                return;

            // An equipped perk dragged off its slot and dropped on no slot is taken off.
            if (!_isDropHandled && Array.IndexOf(_perkSlots, icon) >= 0)
            {
                DungeonAudioComponent.PlayUi(DungeonSound.Click, 0.5f);
                _session.RpcTogglePerk((byte)icon.Index);
            }

            StopDrag();
        }
    }
}
