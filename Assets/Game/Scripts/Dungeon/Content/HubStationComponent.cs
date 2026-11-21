using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// What a place of the hub opens when the player presses F on it.
    public enum HubStation : byte
    {
        /// Book of descents: map, queue and the start of the run.
        Descent,
        /// Party board: create, join by code, leave.
        Party,
        Skills,
        /// The kit alone, as on Tab.
        Character,
        /// The kit beside the character's own stash pages.
        Stash,
        /// The kit beside the page all characters of the account share.
        SharedStash,
        Merchant,
        /// The kit and the stash with the counter that buys things off the player.
        Appraiser,
        /// Road gate: back to the character select.
        Road
    }

    /// A place of the hub tied to a page of the UI. Purely local: the trigger volume is found by the look ray of HubUiRoot.
    public sealed class HubStationComponent : MonoBehaviour
    {
        public HubStation Station => _station;
        public int Index => _index;
        public string Title => _title;
        public string Prompt => _prompt;

        [SerializeField]
        private HubStation _station;

        [SerializeField, Tooltip("Merchant: index in PlayerSessionComponent.Merchants")]
        private int _index;

        [SerializeField]
        private string _title;

        [SerializeField]
        private string _prompt;
    }
}
