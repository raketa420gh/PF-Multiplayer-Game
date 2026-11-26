using Game.Scripts.Battle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Scripts.Dungeon
{
    /// Renders a dressed copy of the character into a texture for the inventory and tavern screens: empty hands, the equipped
    /// weapons on the back, the belt items on the hips. Dragging the picture turns the character.
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

        [SerializeField]
        private bool _canTurn = true;

        [SerializeField, Tooltip("Replaces the fighter controller with a still pose (the seated tavern character)")]
        private RuntimeAnimatorController _poseController;

        private const int UpperLayer = 1;

        private static readonly int s_guard = Animator.StringToHash("Fists" + FighterAnimComponent.IdleSuffix);

        private GameObject _rig;
        private Animator _animator;
        private CharacterModelComponent _model;
        private ArmorDresser _dresser;
        private CarriedGear _carried;
        private InventoryComponent _inventory;
        private int _shownVersion = -1;
        private int _savedSlot = -1;

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

            // The hands are empty: the character stands in the fist guard, as in the game.
            if (_animator != null && _poseController == null && _animator.isActiveAndEnabled
                && _animator.GetCurrentAnimatorStateInfo(UpperLayer).shortNameHash != s_guard)
                _animator.Play(s_guard, UpperLayer);

            // Bound to a saved slot (character select): dressed from the saved kit once the context is up.
            if (_inventory == null || _inventory.Object == null || !_inventory.Object.IsValid)
            {
                DressSaved();

                return;
            }

            _rig.transform.Rotate(0f, _turnSpeed * Time.deltaTime, 0f, Space.Self);
            _carried.Apply(_inventory, -1, -1);

            if (_shownVersion == _inventory.Version)
                return;

            _shownVersion = _inventory.Version;
            _dresser.Apply(_inventory);
        }

        public void Bind(InventoryComponent inventory, ClassConfig config)
        {
            EnsureRig();
            _inventory = inventory;
            _shownVersion = -1;
            _savedSlot = -1;

            if (_carried != null)
                _carried.Clear();

            if (_model != null && config != null)
                _model.SetBodyColor(config.BodyColor);
        }

        /// A character that is not in the game yet: the kit saved in its slot.
        public void Bind(int savedSlot, ClassConfig config)
        {
            Bind(null, config);
            _savedSlot = savedSlot;
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
            _carried = new CarriedGear(_animator, _rig.layer);

            if (_animator == null)
                return;

            if (_poseController != null)
                _animator.runtimeAnimatorController = _poseController;
            else
                _animator.SetFloat("ActionSpeed", 1f);
        }

        private void DressSaved()
        {
            if (_savedSlot < 0 || _dresser == null || DungeonContext.Instance == null)
                return;

            ItemConfig[] equipment = StashService.LoadEquipment(_savedSlot, DungeonContext.Instance.Items);
            _savedSlot = -1;
            _dresser.Apply(equipment);
            _carried.Show(equipment);
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            // The camera faces the character: the front follows the cursor when the turn goes against the drag.
            if (_rig != null && _canTurn)
                _rig.transform.Rotate(0f, -eventData.delta.x * _dragSpeed, 0f, Space.Self);
        }
    }
}
