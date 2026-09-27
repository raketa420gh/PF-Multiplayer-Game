using Fusion;
using UnityEngine;

namespace Game.Scripts
{
    public sealed class PlayerColorComponent : NetworkBehaviour
    {
        [SerializeField]
        private Renderer _renderer;
        
        [SerializeField]
        private Material _blue;
        
        [SerializeField]
        private Material _red;

        public override void Render()
        {
            _renderer.material = HasInputAuthority ? _blue : _red;
        }
    }
}