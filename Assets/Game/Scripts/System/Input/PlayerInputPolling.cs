using Fusion;
using UnityEngine;

namespace Game.Scripts
{
    public sealed class PlayerInputPolling : MonoBehaviour
    {
        [SerializeField] 
        private NetworkEvents _networkEvents;

        private readonly string _verticalAxis = "Vertical";
        private readonly string _horizontalAxis = "Horizontal";
        
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
            _playerInput.MoveDirection = new Vector2(Input.GetAxis(_verticalAxis), Input.GetAxis(_horizontalAxis));
            _playerInput.Buttons.Set(PlayerInputButtons.Sprint, Input.GetKey(KeyCode.LeftShift));
        }

        private void OnInput(NetworkRunner runner, NetworkInput input)
        {
            input.Set(_playerInput);
        }
    }
}