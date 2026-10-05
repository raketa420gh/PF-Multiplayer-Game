using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Character select scene, the first one of the game: switches between the slot list and the creation page and travels
    /// to the tavern with the picked character. No network session runs here.
    public sealed class CharacterSelectUiRoot : MonoBehaviour
    {
        [SerializeField]
        private CharacterSelectView _select;

        [SerializeField]
        private CharacterCreateView _create;

        [SerializeField]
        private LoadingView _loading;

        private int _createSlot;

        private void OnEnable()
        {
            _select.OnCreateRequested += OnCreateRequested;
            _select.OnEnterRequested += OnEnterRequested;
            _create.OnCreated += OnCreated;
            _create.OnCancelled += OnCancelled;
        }

        private void OnDisable()
        {
            _select.OnCreateRequested -= OnCreateRequested;
            _select.OnEnterRequested -= OnEnterRequested;
            _create.OnCreated -= OnCreated;
            _create.OnCancelled -= OnCancelled;
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _loading.Hide();
            _create.Hide();
            _select.Show();
        }

        private void OnCreateRequested(int slot)
        {
            _createSlot = slot;
            _select.Hide();
            _create.Show();
        }

        private void OnCreated(byte classId)
        {
            StashService.CreateCharacter(_createSlot, classId);
            StashService.SelectSlot(_createSlot);
            OnCancelled();
        }

        private void OnCancelled()
        {
            _create.Hide();
            _select.Show();
        }

        private void OnEnterRequested(int slot)
        {
            StashService.SelectSlot(slot);
            SceneTravel.Load(null, SceneTravel.LobbyScene, SceneTravel.LobbyTitle);
        }
    }
}
