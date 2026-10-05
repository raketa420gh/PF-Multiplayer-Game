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

        [Tooltip("Optional developer panel shown together with the inventory (test ground)")]
        [SerializeField]
        private DisplayableView _devPanel;

        private PlayerSessionComponent _session;
        private AdventurerComponent _adventurer;
        private SessionState _shownState = (SessionState)255;
        private bool _isInventoryOpen;
        private int _wheelSkill = -1;
        private int _wheelReadied = -1;
        private readonly System.Collections.Generic.List<int> _wheelSpells = new();
        private readonly System.Collections.Generic.List<(string, Sprite, string, Color, string)> _wheelEntries = new();
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
            ShowLoading();
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

            if (Input.GetKeyDown(KeyCode.M) && _map.HasFloors)
                _map.SetShown(!_map.IsShown);

            if (Input.GetKeyDown(KeyCode.Escape) && _isInventoryOpen)
                SetInventoryOpen(false);
            else if (Input.GetKeyDown(KeyCode.Escape))
                _map.Hide();

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
            ReadyHoveredSpell();

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
                _wheelEntries.Add(("Bear", null, "Br", new Color(0.7f, 0.5f, 0.3f), "+50% HP, slow, heavy claws"));
                _wheelEntries.Add(("Panther", null, "Pn", new Color(0.4f, 0.35f, 0.5f), "fast, quick claws"));
                _wheelEntries.Add(("Rat", null, "Rt", new Color(0.6f, 0.6f, 0.6f), "tiny, fragile, sneaky"));
                _wheel.Open("Shapeshift", _wheelEntries);
            }
            else
            {
                AbilityConfig[] spells = _adventurer.Class.Spells;
                _wheelSpells.Clear();

                for (int i = 0; i < spells.Length; i++)
                {
                    if (!_adventurer.IsSpellInWheel(i, skill.Wheel))
                        continue;

                    int index = _adventurer.SkillCount + i;
                    string detail = spells[i].IsCooldownBased ? $"{spells[i].Cooldown:0}s cd" : $"{_adventurer.GetCharges(index)}/{_adventurer.GetMaxCharges(index)}";
                    _wheelSpells.Add(i);
                    _wheelEntries.Add((spells[i].DisplayName, spells[i].Icon, spells[i].Glyph, spells[i].Color, detail));
                }

                _wheel.Open(skill.DisplayName, _wheelEntries, "Weapon");
            }

            _wheelReadied = -1;
            _context.Battle.Input.SetLookFrozen(true);
            ReadyHoveredSpell();
        }

        /// A spell is readied the moment the cursor enters its sector (the wheel opens on the centre); forms wait for the wheel to close.
        private void ReadyHoveredSpell()
        {
            int selected = _wheel.Selected;

            if (selected == _wheelReadied || _adventurer.GetSkill(_wheelSkill).Kind == AbilityKind.Shapeshift)
                return;

            _wheelReadied = selected;
            _adventurer.RpcReadySpell(selected == SpellWheelView.Center ? AdventurerComponent.NoSpell : (byte)_wheelSpells[selected]);
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

            if (apply && selected >= 0 && skill.Kind == AbilityKind.Shapeshift)
                _adventurer.RpcShapeshift((ShapeshiftForm)(selected + 1));
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

        private void ApplyState()
        {
            _shownState = _session.State;

            switch (_session.State)
            {
                case SessionState.Lobby:
                    ShowLoading();
                    break;
                case SessionState.InDungeon:
                    _loading.Hide();
                    _result.Hide();
                    _hud.Show();
                    SetInventoryOpen(false);
                    break;
                default:
                    _result.Bind(_session);
                    _hud.Hide();
                    _inventory.Hide();
                    _map.Hide();
                    _loading.Hide();
                    _result.Show();
                    _context.Battle.Input.SetUiOpen(true);
                    break;
            }
        }

        /// The session waits for its kit and the adventurer spawn: the loading screen stays up until the body exists.
        private void ShowLoading()
        {
            _hud.Hide();
            _result.Hide();
            _inventory.Hide();
            _map.Hide();
            _loading.Show();
            _loading.SetStatus("Entering...", 1f);
            _context.Battle.Input.SetUiOpen(true);
        }

        private void SetInventoryOpen(bool isOpen)
        {
            _isInventoryOpen = isOpen;
            _inventory.SetShown(isOpen);
            _context.Battle.Input.SetUiOpen(isOpen);

            if (isOpen)
                _map.Hide();

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
