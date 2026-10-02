using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Shows class colors, equipped armor pieces on the bones, a hand torch light and invisibility.
    public sealed class AdventurerVisualComponent : NetworkBehaviour
    {
        [System.Serializable]
        private sealed class ArmorPiece
        {
            public ArmorVisual Visual;
            public GameObject Prefab;
            public HumanBodyBones Bone;
            public bool IsMirrored;
        }

        [SerializeField]
        private AdventurerComponent _adventurer;

        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private SkinnedMeshRenderer _body;

        [SerializeField]
        private ArmorPiece[] _pieces;

        [SerializeField]
        private Light _torchLight;

        [SerializeField]
        private float _torchFlicker = 0.35f;

        private readonly Dictionary<ArmorVisual, List<GameObject>> _instances = new();
        private readonly List<Renderer> _renderers = new();
        private int _shownVersion = -1;
        private float _flicker;
        private bool _wasInvisible;

        public override void Spawned()
        {
            _body.material.SetColor("_BaseColor", _adventurer.Class.BodyColor);
            _renderers.AddRange(GetComponentsInChildren<Renderer>(true));

            foreach (ArmorPiece piece in _pieces)
            {
                Transform bone = _animator.GetBoneTransform(piece.Bone);

                if (bone == null || piece.Prefab == null)
                    continue;

                GameObject instance = Instantiate(piece.Prefab, bone, false);

                if (piece.IsMirrored)
                    instance.transform.localScale = new Vector3(-1f, 1f, 1f);

                instance.SetActive(false);

                if (!_instances.TryGetValue(piece.Visual, out List<GameObject> list))
                    _instances[piece.Visual] = list = new List<GameObject>();

                list.Add(instance);
            }
        }

        public override void Render()
        {
            if (_shownVersion != _adventurer.Inventory.Version)
                RefreshArmor();

            UpdateTorch();
            UpdateInvisibility();
        }

        private void RefreshArmor()
        {
            InventoryComponent inventory = _adventurer.Inventory;
            _shownVersion = inventory.Version;

            foreach (List<GameObject> list in _instances.Values)
            {
                foreach (GameObject instance in list)
                    instance.SetActive(false);
            }

            for (int i = 0; i < InventoryComponent.EquipmentCapacity; i++)
            {
                ArmorItemConfig armor = inventory.GetConfig(inventory.Equipment[i]) as ArmorItemConfig;

                if (armor == null || !_instances.TryGetValue(armor.Visual, out List<GameObject> list))
                    continue;

                foreach (GameObject instance in list)
                {
                    instance.SetActive(true);

                    foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>())
                        renderer.material.SetColor("_BaseColor", armor.VisualColor);
                }
            }
        }

        private void UpdateTorch()
        {
            if (_torchLight == null)
                return;

            InventoryComponent inventory = _adventurer.Inventory;
            int slot = _adventurer.Fighter.Combat.WeaponSlot;
            WeaponItemConfig main = inventory.GetEquippedConfig<WeaponItemConfig>(slot == 0 ? EquipSlot.Weapon1Main : EquipSlot.Weapon2Main);
            WeaponItemConfig off = inventory.GetEquippedConfig<WeaponItemConfig>(slot == 0 ? EquipSlot.Weapon1Off : EquipSlot.Weapon2Off);
            float range = Mathf.Max(main != null ? main.LightRange : 0f, off != null ? off.LightRange : 0f);
            bool isLit = range > 0f && _adventurer.Fighter.Health.IsAlive;

            if (_torchLight.enabled != isLit)
                _torchLight.enabled = isLit;

            if (!isLit)
                return;

            _flicker += Time.deltaTime * 9f;
            _torchLight.range = range;
            _torchLight.intensity = 2.4f + Mathf.PerlinNoise(_flicker, 0.7f) * _torchFlicker * 4f;
        }

        private void UpdateInvisibility()
        {
            bool isInvisible = _adventurer.IsInvisible && !HasInputAuthority;

            if (isInvisible == _wasInvisible)
                return;

            _wasInvisible = isInvisible;

            foreach (Renderer renderer in _renderers)
            {
                if (renderer != null)
                    renderer.enabled = !isInvisible;
            }
        }
    }
}
