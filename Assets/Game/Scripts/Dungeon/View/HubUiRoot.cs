using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Scripts.Dungeon
{
    /// Hub scene: the adventurer walks the settlement under the gameplay HUD; a station under the crosshair opens its page on F,
    /// Tab/I toggles the kit (closes any page), Escape closes the page or toggles the menu. Owns the cursor state through the input polling.
    public sealed class HubUiRoot : MonoBehaviour
    {
        [SerializeField]
        private DungeonContext _context;

        [SerializeField]
        private LoadingView _loading;

        [SerializeField]
        private DungeonHudView _hud;

        [SerializeField]
        private HubPagesView _pages;

        [SerializeField]
        private HelpView _help;

        [SerializeField]
        private SpellWheelView _wheel;

        [SerializeField]
        private PauseMenuView _pause;

        [SerializeField]
        private TMP_Text _promptText;

        [SerializeField]
        private TMP_Text _noticeText;

        [SerializeField]
        private float _noticeTime = 10f;

        [SerializeField, Tooltip("Stations and the world that hides them")]
        private LayerMask _stationMask;

        private static readonly RaycastHit[] s_lookHits = new RaycastHit[16];

        private PlayerSessionComponent _session;
        private AdventurerComponent _adventurer;
        private SpellWheelDriver _wheels;
        private bool _isReady;

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
            InteractOutline.SetStation(null);
        }

        private void Start()
        {
            _hud.Hide();
            _pages.Hide();
            _pause.Hide();
            _loading.Show();
            _loading.SetStatus("Entering the hub...", 1f);
            _context.Battle.Input.SetUiOpen(true);
            _promptText.text = string.Empty;
            _noticeText.text = NetworkLaunch.Notice ?? string.Empty;
            NetworkLaunch.Notice = null;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.H))
                _help.SetShown(!_help.IsShown);

            if (Time.timeSinceLevelLoad > _noticeTime)
                _noticeText.text = string.Empty;

            bool hasBody = _adventurer != null && _adventurer.Object != null && _adventurer.Object.IsValid;

            // The body is rebuilt when the build changes: the screen it left stays as it is.
            if (!_isReady && hasBody && _session != null && _session.HasLoadedKit && !SceneTravel.IsTraveling)
            {
                _isReady = true;
                _loading.Hide();
                _hud.Show();
                _context.Battle.Input.SetUiOpen(false);
            }

            if (!_isReady)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_pages.IsShown)
                    SetPageOpen(false);
                else
                    SetPauseOpen(!_pause.IsShown);
            }
            else if (_pages.IsShown && !IsTyping() && (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I)))
            {
                SetPageOpen(false);

                return;
            }

            if (_pause.IsShown || _pages.IsShown || !hasBody)
            {
                if (hasBody)
                    _wheels.Close(_adventurer, false);

                _promptText.text = string.Empty;
                InteractOutline.SetStation(null);

                return;
            }

            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I))
            {
                Open(HubStation.Character, 0, "Kit");

                return;
            }

            HubStationComponent station = FindStation();
            InteractOutline.SetStation(station);
            _promptText.text = station != null ? $"[F] {station.Prompt}" : string.Empty;

            if (station != null && Input.GetKeyDown(KeyCode.F))
                Use(station);
            else
                _wheels.Update(_adventurer, false);
        }

        private HubStationComponent FindStation()
        {
            Transform eye = _context.Battle.Camera.transform;

            // As with a chest: any part of the model counts, solid or trigger; anything else solid in front blocks the look.
            int count = Physics.RaycastNonAlloc(eye.position, eye.forward, s_lookHits, AdventurerComponent.InteractRange, _stationMask, QueryTriggerInteraction.Collide);
            HubStationComponent station = null;
            float nearest = float.MaxValue;
            float blocker = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = s_lookHits[i];
                HubStationComponent found = hit.collider.GetComponentInParent<HubStationComponent>();

                if (found == null)
                {
                    if (!hit.collider.isTrigger)
                        blocker = Mathf.Min(blocker, hit.distance);
                }
                else if (hit.distance < nearest)
                {
                    nearest = hit.distance;
                    station = found;
                }
            }

            return nearest <= blocker ? station : null;
        }

        private void Use(HubStationComponent station)
        {
            DungeonAudioComponent.PlayUi(DungeonSound.Click, 0.5f);

            if (station.Station != HubStation.Road)
            {
                Open(station.Station, station.Index, station.Title);

                return;
            }

            _session.SaveLocal();
            SceneTravel.Load(_session.Runner, SceneTravel.CharacterSelectScene, SceneTravel.CharacterSelectTitle);
        }

        private void Open(HubStation station, int index, string title)
        {
            _wheels.Close(_adventurer, false);
            _pages.Open(station, index, title);
            SetPageOpen(true);
        }

        private void SetPageOpen(bool isOpen)
        {
            InteractOutline.SetStation(null);
            _pages.SetShown(isOpen);
            _hud.SetShown(!isOpen);
            _promptText.text = string.Empty;
            _context.Battle.Input.SetUiOpen(isOpen);
        }

        private static bool IsTyping()
        {
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

            return selected != null && selected.TryGetComponent(out TMP_InputField field) && field.isFocused;
        }

        private void SetPauseOpen(bool isOpen)
        {
            _pause.SetShown(isOpen);
            _context.Battle.Input.SetUiOpen(isOpen);
        }

        private void OnSessionChanged(PlayerSessionComponent session)
        {
            _session = session;
            _pages.Bind(session);

            if (session != null)
                return;

            _isReady = false;
            _pages.Hide();
            _hud.Hide();
        }

        private void OnAdventurerChanged(AdventurerComponent adventurer)
        {
            _adventurer = adventurer;
        }

        private void OnResumeClicked()
        {
            SetPauseOpen(false);
        }
    }
}
