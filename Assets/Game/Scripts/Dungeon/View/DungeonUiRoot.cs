using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Switches between tavern, HUD, inventory and result screens and owns the cursor state.
    public sealed class DungeonUiRoot : MonoBehaviour
    {
        [SerializeField]
        private DungeonContext _context;

        [SerializeField]
        private LobbyView _lobby;

        [SerializeField]
        private DungeonHudView _hud;

        [SerializeField]
        private InventoryView _inventory;

        [SerializeField]
        private ResultView _result;

        [SerializeField]
        private HelpView _help;

        private PlayerSessionComponent _session;
        private AdventurerComponent _adventurer;
        private SessionState _shownState = (SessionState)255;
        private bool _isInventoryOpen;
        private Fusion.NetworkBehaviourId _openedContainer;

        private void OnEnable()
        {
            _context.OnLocalSessionChanged += OnSessionChanged;
            _context.OnLocalAdventurerChanged += OnAdventurerChanged;
        }

        private void OnDisable()
        {
            _context.OnLocalSessionChanged -= OnSessionChanged;
            _context.OnLocalAdventurerChanged -= OnAdventurerChanged;
        }

        private void Start()
        {
            ShowLobby(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.H))
                _help.SetShown(!_help.IsShown);

            if (_session == null || _session.Object == null || !_session.Object.IsValid)
                return;

            if (_shownState != _session.State)
                ApplyState();

            if (_session.State != SessionState.InDungeon || _adventurer == null)
                return;

            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I))
                SetInventoryOpen(!_isInventoryOpen);

            if (Input.GetKeyDown(KeyCode.Escape) && _isInventoryOpen)
                SetInventoryOpen(false);

            UpdateContainer();
        }

        private void OnSessionChanged(PlayerSessionComponent session)
        {
            _session = session;
            _shownState = (SessionState)255;
            _lobby.Bind(session);
            _result.Bind(session);
        }

        private void OnAdventurerChanged(AdventurerComponent adventurer)
        {
            _adventurer = adventurer;

            if (adventurer != null)
                _inventory.Bind(adventurer.Inventory, adventurer.Actions, adventurer.Stats, adventurer.Class, "Inventory", true);
        }

        private void ApplyState()
        {
            _shownState = _session.State;

            switch (_session.State)
            {
                case SessionState.Lobby:
                    ShowLobby(true);
                    break;
                case SessionState.InDungeon:
                    _lobby.Hide();
                    _result.Hide();
                    _hud.Show();
                    SetInventoryOpen(false);
                    break;
                default:
                    _result.Bind(_session);
                    _hud.Hide();
                    _inventory.Hide();
                    _lobby.Hide();
                    _result.Show();
                    _context.Battle.Input.SetUiOpen(true);
                    break;
            }
        }

        private void ShowLobby(bool bind)
        {
            _hud.Hide();
            _result.Hide();
            _inventory.Hide();
            _lobby.Show();
            _context.Battle.Input.SetUiOpen(true);

            if (bind)
                _lobby.Bind(_session);
        }

        private void SetInventoryOpen(bool isOpen)
        {
            _isInventoryOpen = isOpen;
            _inventory.SetShown(isOpen);
            _context.Battle.Input.SetUiOpen(isOpen);

            if (!isOpen && _adventurer != null && _adventurer.OpenedContainer != null)
                _adventurer.RpcCloseContainer();
        }

        private void UpdateContainer()
        {
            ContainerComponent container = _adventurer.OpenedContainer;
            Fusion.NetworkBehaviourId id = container != null ? container.Id : default;

            if (id == _openedContainer)
                return;

            _openedContainer = id;

            if (container != null)
            {
                _inventory.SetOther(container.Inventory, container.DisplayName);
                SetInventoryOpen(true);
            }
            else
            {
                _inventory.SetOther(null, string.Empty);
            }
        }
    }
}
