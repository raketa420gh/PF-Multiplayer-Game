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

        [SerializeField]
        private SpellWheelView _wheel;

        [SerializeField]
        private CharacterPreviewView _inventoryPreview;

        [Tooltip("Optional developer panel shown together with the inventory (test ground)")]
        [SerializeField]
        private DisplayableView _devPanel;

        private PlayerSessionComponent _session;
        private AdventurerComponent _adventurer;
        private SessionState _shownState = (SessionState)255;
        private bool _isInventoryOpen;
        private int _wheelSkill = -1;
        private readonly System.Collections.Generic.List<(string, string, Color, string)> _wheelEntries = new();
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
            UpdateWheel();
        }

        private void UpdateWheel()
        {
            if (_isInventoryOpen || _adventurer.State != AdventurerState.Alive)
            {
                CloseWheel(false);

                return;
            }

            if (_wheelSkill < 0)
            {
                for (int i = 0; i < _adventurer.SkillCount; i++)
                {
                    AbilityConfig skill = _adventurer.GetSkill(i);

                    if (skill == null || skill.Kind is not (AbilityKind.SpellMemory or AbilityKind.Shapeshift) || !Input.GetKeyDown(i == 0 ? KeyCode.Q : KeyCode.E))
                        continue;

                    OpenWheel(i, skill);

                    return;
                }

                return;
            }

            _wheel.Move(new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 12f);

            if (!Input.GetKey(_wheelSkill == 0 ? KeyCode.Q : KeyCode.E))
                CloseWheel(true);
        }

        private void OpenWheel(int skillIndex, AbilityConfig skill)
        {
            _wheelSkill = skillIndex;
            _wheelEntries.Clear();
            bool isForms = skill.Kind == AbilityKind.Shapeshift;

            if (isForms)
            {
                _wheelEntries.Add(("Bear", "Br", new Color(0.7f, 0.5f, 0.3f), "+50% HP, slow, heavy claws"));
                _wheelEntries.Add(("Panther", "Pn", new Color(0.4f, 0.35f, 0.5f), "fast, quick claws"));
                _wheelEntries.Add(("Rat", "Rt", new Color(0.6f, 0.6f, 0.6f), "tiny, fragile, sneaky"));
                _wheel.Open("Shapeshift", _wheelEntries, (int)_adventurer.Form - 1);
            }
            else
            {
                AbilityConfig[] spells = _adventurer.Class.Spells;

                for (int i = 0; i < spells.Length; i++)
                {
                    int index = _adventurer.SkillCount + i;
                    string detail = spells[i].IsCooldownBased ? $"{spells[i].Cooldown:0}s cd" : $"{_adventurer.GetCharges(index)}/{_adventurer.GetMaxCharges(index)}";
                    _wheelEntries.Add((spells[i].DisplayName, spells[i].Glyph, spells[i].Color, detail));
                }

                _wheel.Open(skill.DisplayName, _wheelEntries, _adventurer.ReadiedSpell == AdventurerComponent.NoSpell ? -1 : _adventurer.ReadiedSpell);
            }

            _context.Battle.Input.SetLookFrozen(true);
        }

        private void CloseWheel(bool apply)
        {
            if (_wheelSkill < 0)
                return;

            int selected = _wheel.Selected;
            AbilityConfig skill = _adventurer.GetSkill(_wheelSkill);
            _wheel.Hide();
            _context.Battle.Input.SetLookFrozen(false);
            _wheelSkill = -1;

            if (!apply || selected < 0)
                return;

            if (skill.Kind == AbilityKind.Shapeshift)
                _adventurer.RpcShapeshift((ShapeshiftForm)(selected + 1));
            else
                _adventurer.RpcReadySpell((byte)selected);
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
            {
                _inventory.Bind(adventurer.Inventory, adventurer.Actions, adventurer.Stats, adventurer.Class, adventurer.Class.DisplayName, true);
                _inventoryPreview.Bind(adventurer.Inventory, adventurer.Class);
            }
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
            _lobby.SetShown(!_context.IsSandbox);
            _context.Battle.Input.SetUiOpen(true);

            if (bind)
                _lobby.Bind(_session);
        }

        private void SetInventoryOpen(bool isOpen)
        {
            _isInventoryOpen = isOpen;
            _inventory.SetShown(isOpen);
            _context.Battle.Input.SetUiOpen(isOpen);

            if (_devPanel != null)
                _devPanel.SetShown(isOpen);

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
