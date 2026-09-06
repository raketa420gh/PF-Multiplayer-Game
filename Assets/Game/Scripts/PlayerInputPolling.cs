using Fusion;
using UnityEngine;

namespace Game.Scripts
{
    public sealed class PlayerInputPolling : MonoBehaviour
    {
        [SerializeField] private NetworkEvents _networkEvents;

        private PlayerInputData _playerInput;

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
            float dx = Input.GetAxis("Horizontal");
            float dz = Input.GetAxis("Vertical");

            NetworkButtons buttons = new NetworkButtons();
            buttons.Set(PlayerInputButtons.Sprint, Input.GetKey(KeyCode.LeftShift));

            _playerInput = new PlayerInputData
            {
                MoveDirection = new Vector2(dx, dz),
                Buttons = buttons
            };
        }

        private void OnInput(NetworkRunner runner, NetworkInput input)
        {
            input.Set(_playerInput);
        }
    }
}