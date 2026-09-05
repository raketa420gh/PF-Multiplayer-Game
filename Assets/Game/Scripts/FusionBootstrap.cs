using Fusion;
using UnityEngine;

namespace Game.Scripts
{
    public sealed class FusionBootstrap : MonoBehaviour
    {
        private void Start()
        {
            gameObject.AddComponent<NetworkRunner>();
        }
    }
}