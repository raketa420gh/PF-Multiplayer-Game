using Fusion;
using Game.Scripts.Battle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Scripts.Dungeon
{
    /// Renders a dressed copy of the character into a texture for the inventory and tavern screens. Dragging the picture turns the character.
    public sealed class CharacterPreviewView : MonoBehaviour, IDragHandler
    {
        [SerializeField]
        private GameObject _rigPrefab;

        [SerializeField]
        private ArmorPieceSetConfig _pieceSet;

        [SerializeField]
        private RawImage _image;

        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private Transform _stage;

        [SerializeField]
        private float _turnSpeed = 0f;

        [SerializeField, Tooltip("Degrees per dragged pixel")]
        private float _dragSpeed = 0.45f;

        [SerializeField, Tooltip("Tavern backdrop: hands stay empty and the body stands in the relaxed idle")]
        private bool _isUnarmed;

        private GameObject _rig;
        private Animator _animator;
        private CharacterModelComponent _model;
        private Transform[] _sockets;
        private ArmorDresser _dresser;
        private InventoryComponent _inventory;
        private ClassConfig _class;
        private int _shownVersion = -1;
        private int _shownWeapon = -1;
        private GameObject[] _weaponVisuals = System.Array.Empty<GameObject>();

        private void OnEnable()
        {
            EnsureRig();
            _camera.enabled = true;
            _shownVersion = -1;
        }

        private void OnDisable()
        {
            if (_camera != null)
                _camera.enabled = false;
        }

        private void Update()
        {
            if (_rig == null)
                return;

            // Bound to a class only (character select): the bare body in the relaxed idle.
            if (_inventory == null)
            {
                if (_animator != null)
                    _animator.SetLayerWeight(1, 0f);

                return;
            }

            if (_inventory.Object == null || !_inventory.Object.IsValid)
                return;

            _rig.transform.Rotate(0f, _turnSpeed * Time.deltaTime, 0f, Space.Self);

            if (_shownVersion == _inventory.Version)
                return;

            _shownVersion = _inventory.Version;
            _dresser.Apply(_inventory);
            ShowWeapon();
        }

        public void Bind(InventoryComponent inventory, ClassConfig config)
        {
            EnsureRig();
            _inventory = inventory;
            _class = config;
            _shownVersion = -1;

            if (_model != null && config != null)
                _model.SetBodyColor(config.BodyColor);
        }

        public void SetCharacterShown(bool isShown)
        {
            _stage.gameObject.SetActive(isShown);
        }

        private void EnsureRig()
        {
            if (_rig != null || _rigPrefab == null)
                return;

            _rig = Instantiate(_rigPrefab, _stage);
            _rig.transform.localPosition = Vector3.zero;
            _rig.transform.localRotation = Quaternion.Euler(0f, -15f, 0f);
            _animator = _rig.GetComponentInChildren<Animator>();
            _model = _rig.GetComponentInChildren<CharacterModelComponent>();
            _dresser = new ArmorDresser(_animator, _pieceSet, _rig.layer);
            _sockets = new Transform[4];
            _sockets[(int)WeaponSocket.RightHand] = FindSocket("RightHandSocket");
            _sockets[(int)WeaponSocket.LeftHand] = FindSocket("LeftHandSocket");
            _sockets[(int)WeaponSocket.RightShield] = FindSocket("RightHandShieldSocket");
            _sockets[(int)WeaponSocket.LeftShield] = FindSocket("LeftHandShieldSocket");

            if (_animator != null)
                _animator.SetFloat("ActionSpeed", 1f);
        }

        private Transform FindSocket(string name)
        {
            foreach (Transform child in _rig.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                    return child;
            }

            return _rig.transform;
        }

        private void ShowWeapon()
        {
            foreach (GameObject visual in _weaponVisuals)
            {
                if (visual != null)
                    Destroy(visual);
            }

            WeaponItemConfig main = _inventory.GetEquippedConfig<WeaponItemConfig>(EquipSlot.Weapon1Main);
            WeaponItemConfig off = _inventory.GetEquippedConfig<WeaponItemConfig>(EquipSlot.Weapon1Off);
            WeaponConfig config = main != null && !_isUnarmed ? main.GetWeapon(off) : null;

            // Unarmed, the whole body stands in the relaxed library idle instead of the fist guard.
            if (_animator != null)
                _animator.SetLayerWeight(1, config != null ? 1f : 0f);

            if (config == null)
            {
                _weaponVisuals = System.Array.Empty<GameObject>();

                return;
            }

            _weaponVisuals = new GameObject[config.Attachments.Length];

            for (int i = 0; i < config.Attachments.Length; i++)
            {
                WeaponAttachment attachment = config.Attachments[i];
                _weaponVisuals[i] = Instantiate(attachment.Prefab, _sockets[(int)attachment.Socket], false);
                SetLayer(_weaponVisuals[i], _rig.layer);
            }

            PlayIdle(config.AnimationPrefix);
        }

        private void PlayIdle(string prefix)
        {
            if (_animator != null)
                _animator.Play(prefix + FighterAnimComponent.IdleSuffix, 1, 0f);
        }

        private static void SetLayer(GameObject root, int layer)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = layer;
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            // The camera faces the character: the front follows the cursor when the turn goes against the drag.
            if (_rig != null)
                _rig.transform.Rotate(0f, -eventData.delta.x * _dragSpeed, 0f, Space.Self);
        }
    }
}
