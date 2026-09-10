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
        private bool _resetInputs;

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
            if (_resetInputs == true)
            {
                _playerInput.Buttons.Set(PlayerInputButtons.Attack, false);
                _resetInputs = false;
            }
            
            _playerInput.MoveDirection = new Vector2(Input.GetAxis(_verticalAxis), Input.GetAxis(_horizontalAxis));
            _playerInput.Buttons.Set(PlayerInputButtons.Sprint, Input.GetKey(KeyCode.LeftShift));
            
            if (Input.GetKeyDown(KeyCode.Space))
                _playerInput.Buttons.Set(PlayerInputButtons.Attack, true);
        }

        private void OnInput(NetworkRunner runner, NetworkInput input)
        {
            input.Set(_playerInput);
            _resetInputs = true;
        }
    }
}