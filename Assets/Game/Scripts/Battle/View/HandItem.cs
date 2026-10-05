using System;
using UnityEngine;

namespace Game.Scripts.Battle
{
    /// A belt item as it is carried: the model sits in the weapon socket of the right hand, so a clip that moves the socket
    /// (see AnimationEditConfig) carries the item along. A drink stands upright in the palm, mouth to the thumb; anything
    /// else lies on the hand.
    [Serializable]
    public sealed class HandItem
    {
        public GameObject Prefab => _prefab;

        [SerializeField]
        private GameObject _prefab;

        [SerializeField]
        private bool _isDrink;

        private static readonly Pose s_drinkSeat = new(new Vector3(0.065f, -0.01f, -0.07f), Quaternion.Euler(0f, 90f, 90f));
        private static readonly Pose s_flatSeat = new(new Vector3(0.02f, -0.14f, 0.08f), Quaternion.Euler(0f, 0f, 90f));

        public GameObject Create(Animator animator)
        {
            return Create(_prefab, animator, _isDrink);
        }

        public static GameObject Create(GameObject prefab, Animator animator, bool isDrink)
        {
            Pose seat = isDrink ? s_drinkSeat : s_flatSeat;
            GameObject item = UnityEngine.Object.Instantiate(prefab, HandGrip.GetSocket(animator, HumanBodyBones.RightHand), false);
            item.transform.SetLocalPositionAndRotation(seat.position, seat.rotation);

            return item;
        }
    }
}
