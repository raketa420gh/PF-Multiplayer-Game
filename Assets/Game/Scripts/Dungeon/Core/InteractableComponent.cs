using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Base for anything the adventurer can hold F on. Progress is tracked by the interacting player.
    public abstract class InteractableComponent : NetworkBehaviour
    {
        public virtual string Prompt => _prompt;
        public virtual float HoldTime => _holdTime;
        public virtual bool IsAvailable => true;
        /// Which busy animation the adventurer plays while holding F.
        public virtual byte BusyKind => AdventurerComponent.BusyInteract;

        [SerializeField]
        private string _prompt = "Interact";

        [SerializeField]
        private float _holdTime = 1f;

        /// Called on the state authority when the hold completes.
        public abstract void Complete(AdventurerComponent adventurer);

        public virtual void Cancel(AdventurerComponent adventurer)
        {
        }
    }
}
