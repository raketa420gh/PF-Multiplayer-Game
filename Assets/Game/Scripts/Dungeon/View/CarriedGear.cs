using System.Collections.Generic;
using Game.Scripts.Battle;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Equipped things that are not in the hands, as they are carried: the weapons of both sets on the back (short ones across
    /// the small of the back), the first item of each belt group on its hip - keys 3 left, 4 right. Plain helper, no networking.
    public sealed class CarriedGear
    {
        private const int Sets = 2;
        /// Shorter weapons (daggers, a book) do not reach from the shoulder to the hip: they lie across the belt.
        private const float ShortLength = 0.6f;
        /// A long weapon leans this far off the spine, its grip over the shoulder of its set.
        private const float BackLean = 24f;
        private const float Gap = 0.012f;
        private const float TuckLength = 0.25f;

        /// Seats are given in the character's axes (x right, y up, z forward) from the bone they hang on.
        private static readonly Vector3 s_backSeat = new(0f, 0.02f, -0.115f);
        /// Where the top edge of a shield comes: the nape, whatever its height.
        private static readonly Vector3 s_shieldSeat = new(0f, 0.3f, -0.115f);
        private static readonly Vector3 s_lumbarSeat = new(0f, 0.09f, -0.115f);
        private static readonly Vector3 s_hipSeat = new(0.13f, 0.04f, -0.02f);

        private readonly List<GameObject> _instances = new();
        private readonly Transform _back;
        private readonly Transform _hips;
        private readonly int _layer;
        private int _shownVersion = -1;
        private int _shownHeld;
        private bool _isVisible = true;

        public CarriedGear(Animator animator, int layer = -1)
        {
            _layer = layer;

            if (animator == null || animator.avatar == null || !animator.isHuman)
                return;

            _back = CreateAnchor(animator, animator.GetBoneTransform(HumanBodyBones.UpperChest) != null ? HumanBodyBones.UpperChest : HumanBodyBones.Chest);
            _hips = CreateAnchor(animator, HumanBodyBones.Hips);
        }

        /// heldSet: the weapon set in the hands, heldBelt: the equipment slot of the belt item in the hand; -1 for none.
        /// Returns true when the carried things were rebuilt.
        public bool Apply(InventoryComponent inventory, int heldSet, int heldBelt)
        {
            int held = heldSet + 1 | heldBelt + 1 << 8;

            if (_back == null || _hips == null || (_shownVersion == inventory.Version && _shownHeld == held))
                return false;

            _shownVersion = inventory.Version;
            _shownHeld = held;
            WeaponItemConfig[] weapons = new WeaponItemConfig[Sets * 2];

            for (int set = 0; set < Sets; set++)
            {
                if (set == heldSet)
                    continue;

                weapons[set * 2] = inventory.GetEquippedConfig<WeaponItemConfig>(set == 0 ? EquipSlot.Weapon1Main : EquipSlot.Weapon2Main);
                weapons[set * 2 + 1] = inventory.GetEquippedConfig<WeaponItemConfig>(set == 0 ? EquipSlot.Weapon1Off : EquipSlot.Weapon2Off);
            }

            Show(weapons, FindBeltItem(inventory, EquipSlot.Utility1, heldBelt), FindBeltItem(inventory, EquipSlot.Utility4, heldBelt));

            return true;
        }

        /// weapons: main and off hand of the first set, then of the second; null entries carry nothing.
        public void Show(WeaponItemConfig[] weapons, ItemConfig leftHip, ItemConfig rightHip)
        {
            DestroyInstances();
            float backDepth = 0f;
            float lumbarDepth = 0f;

            // Shields go on last: they cover what lies under them.
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < weapons.Length; i++)
                {
                    WeaponItemConfig config = weapons[i];

                    if (config == null || config.WorldModel == null || config.WeaponClass == WeaponClass.Shield != (pass == 1))
                        continue;

                    SeatWeapon(Create(config, _back), config, i / 2 == 0 ? 1f : -1f, ref backDepth, ref lumbarDepth);
                }
            }

            SeatOnHip(leftHip, -1f);
            SeatOnHip(rightHip, 1f);
            SetVisible(_isVisible);
        }

        public void Clear()
        {
            DestroyInstances();
            _shownVersion = -1;
        }

        public void SetVisible(bool isVisible)
        {
            _isVisible = isVisible;

            foreach (GameObject instance in _instances)
            {
                foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>())
                    renderer.enabled = isVisible;
            }
        }

        /// The first item of a belt group that is not the one in the hand.
        private static ItemConfig FindBeltItem(InventoryComponent inventory, EquipSlot first, int heldBelt)
        {
            for (int i = 0; i < AdventurerComponent.BeltGroupSize; i++)
            {
                EquipSlot slot = (EquipSlot)((int)first + i);
                ItemConfig config = (int)slot != heldBelt ? inventory.GetConfig(inventory.GetEquipped(slot)) : null;

                if (config != null && config.WorldModel != null)
                    return config;
            }

            return null;
        }

        /// A child of the bone turned to the character's axes as the bone stands in the avatar's T-pose.
        private static Transform CreateAnchor(Animator animator, HumanBodyBones bone)
        {
            Transform parent = animator.GetBoneTransform(bone);

            if (parent == null)
                return null;

            SkeletonBone[] skeleton = animator.avatar.humanDescription.skeleton;
            Quaternion bind = Quaternion.identity;

            for (Transform node = parent; node != null && node != animator.transform; node = node.parent)
            {
                foreach (SkeletonBone entry in skeleton)
                {
                    if (entry.name != node.name)
                        continue;

                    bind = entry.rotation * bind;
                    break;
                }
            }

            Transform anchor = new GameObject("Carried").transform;
            anchor.SetParent(parent, false);
            anchor.localRotation = Quaternion.Inverse(bind);

            return anchor;
        }

        private GameObject Create(ItemConfig config, Transform anchor)
        {
            GameObject instance = Object.Instantiate(config.WorldModel, anchor, false);

            foreach (Collider collider in instance.GetComponentsInChildren<Collider>())
                Object.Destroy(collider);

            // A torch on the belt is out.
            if (config is ConsumableItemConfig { IsLight: true })
            {
                foreach (Light light in instance.GetComponentsInChildren<Light>())
                    light.enabled = false;

                foreach (ParticleSystem flame in instance.GetComponentsInChildren<ParticleSystem>())
                    flame.gameObject.SetActive(false);
            }

            if (instance.TryGetComponent(out WeaponVisual visual))
                visual.SetCarried();

            if (_layer >= 0)
            {
                foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
                    child.gameObject.layer = _layer;
            }

            _instances.Add(instance);

            return instance;
        }

        /// The carry turn lays a weapon along z from the grip, flat across x. side: 1 = the first set, grip to the right; -1 = mirrored.
        private void SeatWeapon(GameObject instance, WeaponItemConfig config, float side, ref float backDepth, ref float lumbarDepth)
        {
            Transform transform = instance.transform;
            Quaternion turn = Quaternion.Euler(config.CarryEuler);
            Bounds bounds = config.CarryBounds;

            if (config.WeaponClass == WeaponClass.Shield)
            {
                transform.SetLocalPositionAndRotation(s_shieldSeat + Vector3.down * bounds.extents.y + Vector3.back * (backDepth + bounds.extents.z) - bounds.center, turn);
                backDepth += bounds.size.z + Gap;

                return;
            }

            bool isShort = bounds.size.z < ShortLength;
            // The end nearer the grip goes up over the shoulder; across the belt it points to the hand of its side.
            float grip = -bounds.min.z <= bounds.max.z ? 1f : -1f;
            Vector3 along = (isShort ? Vector3.left * side : Quaternion.Euler(0f, 0f, -side * BackLean) * Vector3.down) * grip;
            Quaternion rotation = Quaternion.LookRotation(along, Vector3.Cross(along, Vector3.back));
            Vector3 seat;

            if (isShort)
            {
                transform.SetParent(_hips, false);
                seat = s_lumbarSeat + Vector3.back * (lumbarDepth + bounds.extents.x);
                lumbarDepth += bounds.size.x + Gap;
            }
            else
            {
                seat = s_backSeat + Vector3.back * (backDepth + bounds.extents.x);
                backDepth += bounds.size.x + Gap;
            }

            transform.SetLocalPositionAndRotation(seat - rotation * bounds.center, rotation * turn);
        }

        /// The longest side hangs down, the thinnest lies against the hip.
        private void SeatOnHip(ItemConfig config, float side)
        {
            if (config == null)
                return;

            GameObject instance = Create(config, _hips);
            Bounds bounds = Measure(instance);
            Vector3 size = bounds.size;
            int longest = size.x > size.y && size.x >= size.z ? 0 : size.z > size.y ? 2 : 1;
            int thinnest = (longest + 1) % 3;
            int other = (longest + 2) % 3;

            if (size[other] < size[thinnest])
                (thinnest, other) = (other, thinnest);

            Vector3 up = Vector3.zero;
            Vector3 forward = Vector3.zero;
            up[longest] = 1f;
            forward[other] = 1f;
            Quaternion rotation = Quaternion.Inverse(Quaternion.LookRotation(forward, up));
            // The top hangs at the belt; a long thing (a torch) is tucked through it by half of what is over a hand's length.
            float top = s_hipSeat.y + Mathf.Max(0f, size[longest] - TuckLength) * 0.5f;
            Vector3 seat = new(side * (s_hipSeat.x + size[thinnest] * 0.5f), top - size[longest] * 0.5f, s_hipSeat.z);
            instance.transform.SetLocalPositionAndRotation(seat - rotation * bounds.center, rotation);
        }

        private void DestroyInstances()
        {
            foreach (GameObject instance in _instances)
            {
                if (instance != null)
                    Object.Destroy(instance);
            }

            _instances.Clear();
        }

        private static Vector3 Corner(Bounds bounds, int index)
        {
            return bounds.center + Vector3.Scale(bounds.extents, new Vector3((index & 1) * 2 - 1, (index >> 1 & 1) * 2 - 1, (index >> 2 & 1) * 2 - 1));
        }

        /// Bounds of the model's shown meshes in the model's own axes.
        private static Bounds Measure(GameObject root)
        {
            Bounds bounds = default;
            bool isStarted = false;
            Matrix4x4 toRoot = root.transform.worldToLocalMatrix;

            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null)
                    continue;

                Matrix4x4 matrix = toRoot * filter.transform.localToWorldMatrix;

                for (int i = 0; i < 8; i++)
                {
                    Vector3 point = matrix.MultiplyPoint3x4(Corner(filter.sharedMesh.bounds, i));

                    if (isStarted)
                        bounds.Encapsulate(point);
                    else
                        bounds = new Bounds(point, Vector3.zero);

                    isStarted = true;
                }
            }

            return bounds;
        }
    }
}
