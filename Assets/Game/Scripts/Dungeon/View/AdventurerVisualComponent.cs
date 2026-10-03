using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Shows class colors, equipped armor pieces on the bones, a hand torch light and invisibility.
    public sealed class AdventurerVisualComponent : NetworkBehaviour
    {
        [SerializeField]
        private AdventurerComponent _adventurer;

        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private SkinnedMeshRenderer _body;

        [SerializeField]
        private ArmorPieceSetConfig _pieceSet;

        [SerializeField]
        private Light _torchLight;

        [SerializeField]
        private float _torchFlicker = 0.35f;

        [SerializeField]
        private Light _handGlow;

        [SerializeField]
        private float[] _formScales = { 1f, 1.45f, 1f, 0.4f };

        [SerializeField]
        private Color[] _formTints = { Color.white, new(0.45f, 0.3f, 0.18f), new(0.12f, 0.1f, 0.12f), new(0.45f, 0.42f, 0.4f) };

        private readonly List<Renderer> _renderers = new();
        private ArmorDresser _dresser;
        private int _shownVersion = -1;
        private float _flicker;
        private bool _wasInvisible;
        private ShapeshiftForm _shownForm;
        private Color _classColor;

        public override void Spawned()
        {
            _classColor = _adventurer.Class.BodyColor;
            _body.material.SetColor("_BaseColor", _classColor);
            _dresser = new ArmorDresser(_animator, _pieceSet, gameObject.layer);
            _renderers.AddRange(GetComponentsInChildren<Renderer>(true));
        }

        public override void Render()
        {
            if (_shownVersion != _adventurer.Inventory.Version)
            {
                _shownVersion = _adventurer.Inventory.Version;
                _dresser.Apply(_adventurer.Inventory);
            }

            UpdateTorch();
            UpdateInvisibility();
            UpdateHandGlow();
            UpdateForm();
        }

        private void UpdateHandGlow()
        {
            if (_handGlow == null)
                return;

            AbilityConfig spell = _adventurer.ReadiedSpellConfig;
            bool isLit = spell != null && _adventurer.Fighter.Health.IsAlive && _adventurer.Form == ShapeshiftForm.None;

            if (_handGlow.enabled != isLit)
                _handGlow.enabled = isLit;

            if (isLit)
            {
                _handGlow.color = spell.Color;
                _handGlow.intensity = 1.2f + Mathf.Sin(Time.time * 6f) * 0.3f;
            }
        }

        private void UpdateForm()
        {
            ShapeshiftForm form = _adventurer.Form;

            if (form == _shownForm)
                return;

            _shownForm = form;
            int index = Mathf.Clamp((int)form, 0, _formScales.Length - 1);
            _animator.transform.localScale = Vector3.one * _formScales[index];
            _body.material.SetColor("_BaseColor", form == ShapeshiftForm.None ? _classColor : _formTints[index]);
            _dresser.Clear();

            if (form == ShapeshiftForm.None)
                _dresser.Apply(_adventurer.Inventory);
        }

        private void UpdateTorch()
        {
            if (_torchLight == null)
                return;

            InventoryComponent inventory = _adventurer.Inventory;
            int slot = _adventurer.Fighter.Combat.WeaponSlot;
            WeaponItemConfig main = inventory.GetEquippedConfig<WeaponItemConfig>(slot == 0 ? EquipSlot.Weapon1Main : EquipSlot.Weapon2Main);
            WeaponItemConfig off = inventory.GetEquippedConfig<WeaponItemConfig>(slot == 0 ? EquipSlot.Weapon1Off : EquipSlot.Weapon2Off);
            float range = Mathf.Max(main != null ? main.LightRange : 0f, off != null ? off.LightRange : 0f);
            bool isLit = range > 0f && _adventurer.Fighter.Health.IsAlive;

            if (_torchLight.enabled != isLit)
                _torchLight.enabled = isLit;

            if (!isLit)
                return;

            _flicker += Time.deltaTime * 9f;
            _torchLight.range = range;
            _torchLight.intensity = 2.4f + Mathf.PerlinNoise(_flicker, 0.7f) * _torchFlicker * 4f;
        }

        private void UpdateInvisibility()
        {
            bool isInvisible = _adventurer.IsInvisible && !HasInputAuthority;

            if (isInvisible == _wasInvisible)
                return;

            _wasInvisible = isInvisible;

            foreach (Renderer renderer in _renderers)
            {
                if (renderer != null)
                    renderer.enabled = !isInvisible;
            }
        }
    }
}
