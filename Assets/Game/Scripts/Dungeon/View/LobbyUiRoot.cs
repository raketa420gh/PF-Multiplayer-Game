using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Tavern scene: binds the lobby pages to the local session and lifts the loading screen once the kit has arrived.
    public sealed class LobbyUiRoot : MonoBehaviour
    {
        [SerializeField]
        private DungeonContext _context;

        [SerializeField]
        private LobbyView _lobby;

        [SerializeField]
        private HelpView _help;

        [SerializeField]
        private LoadingView _loading;

        private PlayerSessionComponent _session;

        private void OnEnable()
        {
            _context.OnLocalSessionChanged += OnSessionChanged;
        }

        private void OnDisable()
        {
            _context.OnLocalSessionChanged -= OnSessionChanged;
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _loading.SetStatus("Entering the tavern...", 1f);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.H))
                _help.SetShown(!_help.IsShown);

            bool isReady = _session != null && _session.Object != null && _session.Object.IsValid && _session.HasLoadedKit;

            if (isReady && _loading.IsShown && !SceneTravel.IsTraveling)
                _loading.Hide();
        }

        private void OnSessionChanged(PlayerSessionComponent session)
        {
            _session = session;
            _lobby.Bind(session);
        }
    }
}
