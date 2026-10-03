using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Instantiates armor pieces on a humanoid rig and toggles them by look. Plain helper, no networking.
    public sealed class ArmorDresser
    {
        private static readonly int s_baseColor = Shader.PropertyToID("_BaseColor");

        private readonly Dictionary<ArmorVisual, List<GameObject>> _instances = new();

        public ArmorDresser(Animator animator, ArmorPieceSetConfig config, int layer = -1)
        {
            if (animator == null || config == null)
                return;

            foreach (ArmorPieceEntry entry in config.Entries)
            {
                Transform bone = animator.GetBoneTransform(entry.Bone);

                if (bone == null || entry.Prefab == null)
                    continue;

                GameObject instance = Object.Instantiate(entry.Prefab, bone, false);

                if (entry.IsMirrored)
                    instance.transform.localScale = Vector3.Scale(instance.transform.localScale, new Vector3(-1f, 1f, 1f));

                if (layer >= 0)
                    instance.layer = layer;

                instance.SetActive(false);

                if (!_instances.TryGetValue(entry.Visual, out List<GameObject> list))
                    _instances[entry.Visual] = list = new List<GameObject>();

                list.Add(instance);
            }
        }

        public void Clear()
        {
            foreach (List<GameObject> list in _instances.Values)
            {
                foreach (GameObject instance in list)
                    instance.SetActive(false);
            }
        }

        public void Show(ArmorVisual visual, Color color)
        {
            if (!_instances.TryGetValue(visual, out List<GameObject> list))
                return;

            foreach (GameObject instance in list)
            {
                instance.SetActive(true);

                foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>())
                    renderer.material.SetColor(s_baseColor, color);
            }
        }

        /// Dresses from an inventory's equipment slots.
        public void Apply(InventoryComponent inventory)
        {
            Clear();

            for (int i = 0; i < InventoryComponent.EquipmentCapacity; i++)
            {
                if (inventory.GetConfig(inventory.Equipment[i]) is ArmorItemConfig armor)
                    Show(armor.Visual, armor.VisualColor);
            }
        }
    }
}
