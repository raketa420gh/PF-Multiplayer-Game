using Fusion;
using UnityEngine;

namespace Game.Scripts.Battle
{
    public sealed class BattleInputPolling : MonoBehaviour
    {
        public Vector2 LookRotation => _look;

        [SerializeField]
        private NetworkEvents _networkEvents;

        [SerializeField]
        private float _sensitivity = 2f;

        private const float MaxPitch = 90f;

        private Vector2 _look;
        private NetworkButtons _buttons;
        private bool _resetButtons;

        private void OnEnable()
        {
            _networkEvents.OnInput.AddListener(OnInput);
        }

        private void OnDisable()
        {
            _networkEvents.OnInput.RemoveListener(OnInput);
        }

        private void Update()
        {
            UpdateCursor();

            if (_resetButtons)
            {
                _buttons = default;
                _resetButtons = false;
            }

            if (Cursor.lockState != CursorLockMode.Locked)
                return;

            _look.y = Mathf.Repeat(_look.y + Input.GetAxisRaw("Mouse X") * _sensitivity, 360f);
            _look.x = Mathf.Clamp(_look.x - Input.GetAxisRaw("Mouse Y") * _sensitivity, -MaxPitch, MaxPitch);

            Accumulate(PlayerInputButtons.Primary, Input.GetMouseButton(0));
            Accumulate(PlayerInputButtons.Secondary, Input.GetMouseButton(1));
            Accumulate(PlayerInputButtons.Sprint, Input.GetKey(KeyCode.LeftShift));
            Accumulate(PlayerInputButtons.Crouch, Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C));
            Accumulate(PlayerInputButtons.Jump, Input.GetKey(KeyCode.Space));
            Accumulate(PlayerInputButtons.Weapon1, Input.GetKey(KeyCode.Alpha1));
            Accumulate(PlayerInputButtons.Weapon2, Input.GetKey(KeyCode.Alpha2));
            Accumulate(PlayerInputButtons.Weapon3, Input.GetKey(KeyCode.Alpha3));
            Accumulate(PlayerInputButtons.Weapon4, Input.GetKey(KeyCode.Alpha4));
            Accumulate(PlayerInputButtons.BotMode, Input.GetKey(KeyCode.B));
        }

        public void SetLook(Vector2 look)
        {
            _look = look;
        }

        private void UpdateCursor()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                Cursor.lockState = CursorLockMode.None;
            else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
                Cursor.lockState = CursorLockMode.Locked;

            Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
        }

        private void Accumulate(PlayerInputButtons button, bool isDown)
        {
            _buttons.Set(button, _buttons.IsSet(button) || isDown);
        }

        private void OnInput(NetworkRunner runner, NetworkInput input)
        {
            Vector2 move = Cursor.lockState == CursorLockMode.Locked
                ? new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"))
                : Vector2.zero;

            input.Set(new PlayerInputData
            {
                MoveDirection = Vector2.ClampMagnitude(move, 1f),
                LookRotation = _look,
                Buttons = _buttons
            });

            _resetButtons = true;
        }
    }
}
