using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Switches between the loading, HUD, inventory and result screens of a gameplay scene and owns the cursor state.
    public sealed class DungeonUiRoot : MonoBehaviour
    {
        [SerializeField]
        private DungeonContext _context;

        [SerializeField]
        private LoadingView _loading;

        [SerializeField]
        private DungeonHudView _hud;

        [SerializeField]
        private InventoryView _inventory;

        [SerializeField]
        private ResultView _result;

        [SerializeField]
        private HelpView _help;

        [SerializeField]
        private SpellWheelView _wheel;

        [SerializeField]
        private MapView _map;

        [SerializeField]
        private CharacterPreviewView _inventoryPreview;

        [SerializeField]
        private PauseMenuView _pause;

        [SerializeField]
        private DungeonTestView _test;

        private PlayerSessionComponent _session;
        private AdventurerComponent _adventurer;
        private SessionState _shownState = (SessionState)255;
        private bool _isInventoryOpen;
        private SpellWheelDriver _wheels;
        private Fusion.NetworkBehaviourId _openedContainer;

        private void Awake()
        {
            _wheels = new SpellWheelDriver(_wheel, _context);
        }

        private void OnEnable()
        {
            _context.OnLocalSessionChanged += OnSessionChanged;
            _context.OnLocalAdventurerChanged += OnAdventurerChanged;
            _pause.OnResumeClicked += OnResumeClicked;
        }

        private void OnDisable()
        {
            _context.OnLocalSessionChanged -= OnSessionChanged;
            _context.OnLocalAdventurerChanged -= OnAdventurerChanged;
            _pause.OnResumeClicked -= OnResumeClicked;
        }

        private void Start()
        {
            ShowLoading();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.H))
                _help.SetShown(!_help.IsShown);

            if (_session == null || _session.Object == null || !_session.Object.IsValid)
                return;

            // The death screen waits while the owner watches their body fall.
            if (_shownState != _session.State && !(_session.State == SessionState.Dead && CorpseComponent.IsDeathCamActive))
                ApplyState();

            if (_session.State != SessionState.InDungeon || _adventurer == null)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
                Escape();

            if (_pause.IsShown)
            {
                _wheels.Close(_adventurer, false);

                return;
            }

            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I))
                SetInventoryOpen(!_isInventoryOpen);

            if (Input.GetKeyDown(KeyCode.M) && _map.HasFloors)
                _map.SetShown(!_map.IsShown);

            if (Input.GetKeyDown(KeyCode.F2) && _context.Match != null && _context.Match.IsTest)
                SetTestOpen(!_test.IsShown);

            UpdateContainer();
            _wheels.Update(_adventurer, _isInventoryOpen);
        }

        /// Escape closes the inventory or the map first; otherwise it toggles the menu.
        private void Escape()
        {
            if (_isInventoryOpen)
                SetInventoryOpen(false);
            else if (_test.IsShown)
                SetTestOpen(false);
            else if (_map.IsShown)
                _map.Hide();
            else
                SetPauseOpen(!_pause.IsShown);
        }

        private void SetTestOpen(bool isOpen)
        {
            _test.SetShown(isOpen);
            _context.Battle.Input.SetUiOpen(isOpen || _isInventoryOpen);
        }

        private void SetPauseOpen(bool isOpen)
        {
            _pause.SetShown(isOpen);
            _context.Battle.Input.SetUiOpen(isOpen);
        }

        private void OnSessionChanged(PlayerSessionComponent session)
        {
            _session = session;
            _shownState = (SessionState)255;
            _result.Bind(session);
        }

        private void OnAdventurerChanged(AdventurerComponent adventurer)
        {
            _adventurer = adventurer;

            if (adventurer != null)
            {
                _inventory.Bind(adventurer.Inventory, adventurer.Actions, adventurer.Stats, adventurer.Class.DisplayName, true);
                _inventory.SetSearcher(adventurer);
                _inventoryPreview.Bind(adventurer.Inventory, adventurer.Class);
            }
        }

        private void OnResumeClicked()
        {
            SetPauseOpen(false);
        }

        private void ApplyState()
        {
            _shownState = _session.State;

            switch (_session.State)
            {
                case SessionState.Lobby:
                case SessionState.Descending:
                    ShowLoading();
                    break;
                case SessionState.InDungeon:
                    _loading.Hide();
                    _result.Hide();
                    _hud.Show();
                    _pause.Hide();
                    SetInventoryOpen(false);
                    break;
                default:
                    _result.Bind(_session);
                    _test.Hide();
                    _hud.Hide();
                    _inventory.Hide();
                    _map.Hide();
                    _pause.Hide();
                    _loading.Hide();
                    _result.Show();
                    _context.Battle.Input.SetUiOpen(true);
                    break;
            }
        }

        /// The session waits for its kit and the adventurer spawn: the loading screen stays up until the body exists.
        private void ShowLoading()
        {
            _test.Hide();
            _hud.Hide();
            _result.Hide();
            _inventory.Hide();
            _map.Hide();
            _pause.Hide();
            _loading.Show();
            _loading.SetStatus("Entering...", 1f);
            _context.Battle.Input.SetUiOpen(true);
        }

        private void SetInventoryOpen(bool isOpen)
        {
            _isInventoryOpen = isOpen;
            _inventory.SetShown(isOpen);
            _context.Battle.Input.SetUiOpen(isOpen || _test.IsShown);

            if (isOpen)
                _map.Hide();

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
