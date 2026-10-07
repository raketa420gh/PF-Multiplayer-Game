using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Scripts.Dungeon
{
    /// "Spells" page of the tavern: the spells of the class to pick from and the spell wheels as rings of slots. A spell goes into a wheel
    /// by dragging it onto one of its slots (or by LMB / RMB for wheel I / II); a slot clicked or dragged off the wheels frees its spell.
    public sealed class SpellsView : DisplayableView
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

        [SerializeField, Tooltip("Shown instead of the wheels to a class without spells")]
        private GameObject _emptyRoot;

        [SerializeField]
        private RectTransform _tooltip;

        [SerializeField]
        private TMP_Text _tooltipText;

        private static readonly string[] s_wheelBadges = { "I", "II" };
        private readonly List<AbilityIconView> _spellIcons = new();
        private PlayerSessionComponent _session;
        private int _shownClass = -1;
        private int _shownMask;
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

        private void Update()
        {
            if (_session == null || _session.Object == null || !_session.Object.IsValid)
                return;

            ClassConfig config = _session.Class;

            if (_shownClass != config.Id)
            {
                _shownClass = config.Id;
                BuildIcons(config);
                ShowWheels(config);
                _shownMask = ~_session.SpellMask;
            }

            if (_shownMask != _session.SpellMask)
            {
                _shownMask = _session.SpellMask;
                Refresh(config);
            }
        }

        public void Bind(PlayerSessionComponent session)
        {
            _session = session;
            _shownClass = -1;
        }

        public override void Hide()
        {
            base.Hide();
            _tooltip.gameObject.SetActive(false);
            StopDrag();
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

        /// A wheel is shown only when one of the class skills opens it; its title is that skill.
        private void ShowWheels(ClassConfig config)
        {
            for (int wheel = 0; wheel < _wheels.Length; wheel++)
            {
                _wheels[wheel].SetActive(config.HasWheel(wheel));
                AbilityConfig memory = Array.Find(config.Skills, skill => skill.Kind == AbilityKind.SpellMemory && skill.Wheel == wheel);
                _wheelTitles[wheel].text = memory != null ? memory.DisplayName : string.Empty;
            }

            _emptyRoot.SetActive(!config.HasWheel(0));
        }

        /// Each wheel lists its spells in pool order, the way the wheel opens in the dungeon.
        private void Refresh(ClassConfig config)
        {
            for (int wheel = 0; wheel < ClassConfig.WheelCount; wheel++)
            {
                int slot = wheel * ClassConfig.SpellWheelSize;
                int end = slot + ClassConfig.SpellWheelSize;

                for (int spell = 0; spell < config.Spells.Length && slot < end; spell++)
                {
                    if (ClassConfig.IsInWheel(_session.SpellMask, wheel, spell))
                        _wheelSlots[slot++].SetAbility(spell, config.Spells[spell]);
                }

                for (int i = wheel * ClassConfig.SpellWheelSize; i < end; i++)
                {
                    if (i >= slot)
                        _wheelSlots[i].Clear();

                    _wheelSlots[i].SetSelected(i < slot);
                }
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
            for (int wheel = 0; wheel < ClassConfig.WheelCount; wheel++)
            {
                if (ClassConfig.IsInWheel(_session.SpellMask, wheel, spell))
                    return wheel;
            }

            return -1;
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
            _shownMask = ~_session.SpellMask;
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

        /// A spell dropped on a slot joins that wheel and takes the place of the spell the slot held; a spell already in the wheel stays.
        private void OnDropped(AbilityIconView target)
        {
            if (_dragged == null)
                return;

            _isDropHandled = true;
            int spell = _dragged.Index;
            int wheel = Array.IndexOf(_wheelSlots, target) / ClassConfig.SpellWheelSize;

            if (ClassConfig.IsInWheel(_session.SpellMask, wheel, spell))
                return;

            DungeonAudioComponent.PlayUi(DungeonSound.Click, 0.5f);

            if (target.Index >= 0)
                _session.RpcToggleSpell((byte)wheel, (byte)target.Index);

            _session.RpcToggleSpell((byte)wheel, (byte)spell);
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
