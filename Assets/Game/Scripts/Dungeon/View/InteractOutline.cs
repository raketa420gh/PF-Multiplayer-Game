using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// What the local adventurer is looking at within reach: InteractOutlineFeature draws a white rim around these renderers.
    public static class InteractOutline
    {
        public static IReadOnlyList<Renderer> Renderers => s_renderers;
        public static IReadOnlyList<int> SubMeshes => s_subMeshes;

        private static readonly List<Renderer> s_renderers = new List<Renderer>();
        private static readonly List<int> s_subMeshes = new List<int>();
        private static Component s_target;
        private static Component s_station;

        public static void Set(InteractableComponent target)
        {
            Set(ref s_target, target);
        }

        /// Hub: the place under the crosshair, it is no interactable of the adventurer.
        public static void SetStation(HubStationComponent station)
        {
            Set(ref s_station, station);
        }

        private static void Set(ref Component slot, Component target)
        {
            if (target == slot)
                return;

            slot = target;
            s_renderers.Clear();
            s_subMeshes.Clear();
            Add(s_target);
            Add(s_station);
        }

        private static void Add(Component target)
        {
            if (target == null)
                return;

            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>())
            {
                if (renderer is not (MeshRenderer or SkinnedMeshRenderer) || renderer.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly)
                    continue;

                s_renderers.Add(renderer);
                s_subMeshes.Add(renderer.sharedMaterials.Length);
            }
        }
    }
}
