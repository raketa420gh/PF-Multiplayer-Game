using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Scripts.Dungeon
{
    /// Spells of the tavern skills page: the class spells in their pool section and the spell wheels tab as rings of slots. A spell goes into a wheel
    /// by dragging it onto one of its slots (or by LMB / RMB for wheel I / II); a slot clicked or dragged off the wheels frees its spell.
    public sealed class SpellsView : MonoBehaviour
    {
        [SerializeField]
        private AbilityIconView _spellIconPrefab;

        [SerializeField]
        private RectTransform _spellsRoot;

        [SerializeField, Tooltip("Ring of each wheel, wheel I first")]
        private GameObject[] _wheels;

        [SerializeField, Tooltip("SpellWheelSize slots per wheel, wheel I first")]
        private AbilityIconView[] _wheelSlots;

        [SerializeField, Tooltip("Name of the skill that opens each wheel")]
        private TMP_Text[] _wheelTitles;

        [SerializeField, Tooltip("Shown over a wheel whose skill slot holds no spell memory")]
        private GameObject[] _inactiveLabels;

        [SerializeField, Tooltip("Spells section of the skills pool, shown to a spell class only")]
        private GameObject _sectionRoot;

        [SerializeField]
        private RectTransform _tooltip;

        [SerializeField]
        private TMP_Text _tooltipText;

        private static readonly string[] s_wheelBadges = { "I", "II" };
        private readonly List<AbilityIconView> _spellIcons = new();
        private PlayerSessionComponent _session;
        private static readonly string[] s_wheelKeys = { "Q", "E" };
        private int _shownClass = -1;
        private long _shownLayout;
        private int _shownSkills = -1;
        private AbilityIconView _ghost;
        private AbilityIconView _dragged;
        private bool _isDropHandled;

        private void Awake()
        {
            foreach (AbilityIconView slot in _wheelSlots)
            {
                slot.OnClicked += OnSlotClicked;
                slot.OnDropped += OnDropped;
                Subscribe(slot);
            }

            _ghost = Instantiate(_spellIconPrefab, transform);
            CanvasGroup group = _ghost.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.alpha = 0.9f;
            _ghost.SetSelected(true);
            _ghost.SetBadge(string.Empty);
            _tooltip.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            _tooltip.gameObject.SetActive(false);
            StopDrag();
        }

        private void Update()
        {
            if (_session == null || _session.Object == null || !_session.Object.IsValid)
                return;

            ClassConfig config = _session.Class;

            int shownClass = config.Id | (_session.Subclass << 16);

            if (_shownClass != shownClass)
            {
                _shownClass = shownClass;
                BuildIcons(config);
                _shownSkills = -1;
                _shownLayout = ~_session.SpellLayout;
            }

            int skills = _session.SkillA | (_session.SkillB << 8);

            if (_shownSkills != skills)
            {
                _shownSkills = skills;
                ShowWheels(config);
            }

            if (_shownLayout != _session.SpellLayout)
            {
                _shownLayout = _session.SpellLayout;
                Refresh(config);
            }
        }

        public void Bind(PlayerSessionComponent session)
        {
            _session = session;
            _shownClass = -1;
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
            foreach (AbilityIconView icon in _spellIcons)
                Destroy(icon.gameObject);

            _spellIcons.Clear();

            for (int i = 0; i < config.Spells.Length; i++)
            {
                if (!ClassConfig.IsAvailable(config.Spells[i].Subclass, _session.Subclass))
                    continue;

                AbilityIconView icon = Instantiate(_spellIconPrefab, _spellsRoot);
                icon.SetAbility(i, config.Spells[i]);
                icon.gameObject.SetActive(true);
                icon.OnClicked += OnSpellClicked;
                Subscribe(icon);
                _spellIcons.Add(icon);
            }
        }

        /// Both wheels of a spell class are shown; one whose skill slot (Q / E) holds no spell memory is greyed out as inactive.
        private void ShowWheels(ClassConfig config)
        {
            for (int wheel = 0; wheel < _wheels.Length; wheel++)
            {
                int skill = wheel == 0 ? _session.SkillA : _session.SkillB;
                bool isActive = skill < config.Skills.Length && config.Skills[skill].Kind == AbilityKind.SpellMemory;
                _wheels[wheel].SetActive(config.HasSpellWheels);
                _wheels[wheel].GetComponent<CanvasGroup>().alpha = isActive ? 1f : 0.35f;
                _inactiveLabels[wheel].SetActive(config.HasSpellWheels && !isActive);
                _wheelTitles[wheel].text = isActive ? $"{config.Skills[skill].DisplayName} ({s_wheelKeys[wheel]})" : string.Empty;
            }

            _sectionRoot.SetActive(config.HasSpellWheels);
        }

        /// Every slot shows the spell put into it: the wheel opens in the dungeon in the same order.
        private void Refresh(ClassConfig config)
        {
            for (int slot = 0; slot < _wheelSlots.Length; slot++)
            {
                int spell = ClassConfig.GetSlotSpell(_session.SpellLayout, slot);
                bool isUsed = spell >= 0 && spell < config.Spells.Length;

                if (isUsed)
                    _wheelSlots[slot].SetAbility(spell, config.Spells[spell]);
                else
                    _wheelSlots[slot].Clear();

                _wheelSlots[slot].SetSelected(isUsed);
            }

            foreach (AbilityIconView icon in _spellIcons)
            {
                int wheel = WheelOf(icon.Index);
                icon.SetSelected(wheel >= 0);
                icon.SetBadge(wheel >= 0 ? s_wheelBadges[wheel] : string.Empty);
            }
        }

        private int WheelOf(int spell)
        {
            int slot = ClassConfig.FindSlot(_session.SpellLayout, spell);

            return slot >= 0 ? slot / ClassConfig.SpellWheelSize : -1;
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
            _shownLayout = ~_session.SpellLayout;
        }

        private void OnSpellClicked(AbilityIconView icon, PointerEventData.InputButton button)
        {
            DungeonAudioComponent.PlayUi(DungeonSound.Click, 0.5f);
            _session?.RpcToggleSpell((byte)(button == PointerEventData.InputButton.Right ? 1 : 0), (byte)icon.Index);
        }

        private void OnSlotClicked(AbilityIconView slot, PointerEventData.InputButton button)
        {
            if (slot.Index < 0)
                return;

            DungeonAudioComponent.PlayUi(DungeonSound.Click, 0.5f);
            _session?.RpcToggleSpell((byte)(Array.IndexOf(_wheelSlots, slot) / ClassConfig.SpellWheelSize), (byte)slot.Index);
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
            if (_session == null || icon.Index < 0 || eventData.button != PointerEventData.InputButton.Left)
                return;

            _dragged = icon;
            _isDropHandled = false;
            _ghost.CopyFrom(icon);
            _ghost.gameObject.SetActive(true);
            _ghost.transform.SetAsLastSibling();
            _tooltip.gameObject.SetActive(false);
            MoveGhost(eventData);

            foreach (AbilityIconView slot in _wheelSlots)
                slot.SetTargeted(true);
        }

        private void OnDragged(AbilityIconView icon, PointerEventData eventData)
        {
            if (_dragged == icon)
                MoveGhost(eventData);
        }

        /// A spell dropped on a slot takes exactly that slot; the spell the slot held swaps to the dragged one's old place.
        private void OnDropped(AbilityIconView target)
        {
            if (_dragged == null)
                return;

            _isDropHandled = true;
            DungeonAudioComponent.PlayUi(DungeonSound.Click, 0.5f);
            _session.RpcPlaceSpell((byte)Array.IndexOf(_wheelSlots, target), (byte)_dragged.Index);
        }

        private void OnDragEnded(AbilityIconView icon)
        {
            if (_dragged != icon)
                return;

            int slot = Array.IndexOf(_wheelSlots, icon);

            // A spell dragged out of its wheel and dropped on no slot leaves it.
            if (!_isDropHandled && slot >= 0)
            {
                DungeonAudioComponent.PlayUi(DungeonSound.Click, 0.5f);
                _session.RpcToggleSpell((byte)(slot / ClassConfig.SpellWheelSize), (byte)icon.Index);
            }

            StopDrag();
        }
    }
}
