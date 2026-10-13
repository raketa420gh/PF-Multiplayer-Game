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
        /// A torch stands like a drink, its shaft through the closed fist.
        private static readonly Pose s_torchSeat = new(new Vector3(0.035f, -0.08f, 0.03f), Quaternion.Euler(0f, 90f, 90f));
        private static readonly Pose s_flatSeat = new(new Vector3(0.02f, -0.14f, 0.08f), Quaternion.Euler(0f, 0f, 90f));

        public GameObject Create(Animator animator)
        {
            return Create(_prefab, animator, _isDrink);
        }

        /// A torch is carried in the left hand: the left socket mirrors the right one across its local x, so the seat is mirrored the same way.
        public static GameObject Create(GameObject prefab, Animator animator, bool isDrink, bool isTorch = false)
        {
            Pose seat = isTorch ? s_torchSeat : isDrink ? s_drinkSeat : s_flatSeat;
            bool isLeft = isTorch;
            Quaternion rotation = seat.rotation;
            GameObject item = UnityEngine.Object.Instantiate(prefab, HandGrip.GetSocket(animator, isLeft ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand), false);
            item.transform.SetLocalPositionAndRotation(isLeft ? new Vector3(-seat.position.x, seat.position.y, seat.position.z) : seat.position,
                isLeft ? new Quaternion(rotation.x, -rotation.y, -rotation.z, rotation.w) : rotation);

            return item;
        }
    }
}
