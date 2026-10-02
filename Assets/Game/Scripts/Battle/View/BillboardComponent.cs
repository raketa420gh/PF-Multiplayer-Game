using UnityEngine;

namespace Game.Scripts.Battle
{
    [DefaultExecutionOrder(100)]
    public sealed class BillboardComponent : MonoBehaviour
    {
        private void LateUpdate()
        {
            if (BattleContext.Instance != null)
                transform.rotation = BattleContext.Instance.Camera.transform.rotation;
        }
    }
}
