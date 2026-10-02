using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Dead adventurer body: a lootable container with the fallen character's full inventory and gear.
    public sealed class CorpseComponent : NetworkBehaviour
    {
        public ContainerComponent Container => _container;

        [Networked]
        public byte ClassId { get; private set; }

        [Networked]
        public NetworkString<_32> OwnerName { get; private set; }

        [SerializeField]
        private ContainerComponent _container;

        [SerializeField]
        private InventoryComponent _inventory;

        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private Renderer _bodyRenderer;

        [SerializeField]
        private ClassConfig[] _classes;

        [SerializeField]
        private string _deathState = "Death";

        public override void Spawned()
        {
            if (_animator != null)
            {
                _animator.Play(_deathState, 0, 0.999f);
                _animator.Update(0f);
                _animator.speed = 0f;
            }

            ApplyClassColor();
        }

        public void Setup(InventoryComponent source, ClassConfig config, string ownerName)
        {
            _inventory.CopyFrom(source);
            ClassId = config.Id;
            OwnerName = ownerName;
        }

        private void ApplyClassColor()
        {
            if (_bodyRenderer == null)
                return;

            foreach (ClassConfig config in _classes)
            {
                if (config.Id == ClassId)
                    _bodyRenderer.material.SetColor("_BaseColor", config.BodyColor);
            }
        }
    }
}
