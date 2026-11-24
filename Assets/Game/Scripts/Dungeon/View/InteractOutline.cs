using System.Collections.Generic;
using Game.Scripts.Battle;
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
        private static readonly List<Renderer> s_buffer = new List<Renderer>();
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

        /// The visible renderers of every fighter but the outlined one: bodies that hide the rim when they stand in front of it.
        public static void CollectOccluders(List<Renderer> occluders)
        {
            occluders.Clear();

            foreach (FighterComponent fighter in FighterComponent.All)
            {
                if (s_target != null && fighter.transform == s_target.transform)
                    continue;

                fighter.GetComponentsInChildren(s_buffer);

                foreach (Renderer renderer in s_buffer)
                {
                    if (IsDrawn(renderer) && renderer.enabled && !renderer.forceRenderingOff)
                        occluders.Add(renderer);
                }
            }
        }

        private static bool IsDrawn(Renderer renderer)
        {
            return renderer is MeshRenderer or SkinnedMeshRenderer && renderer.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
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
                if (!IsDrawn(renderer))
                    continue;

                s_renderers.Add(renderer);
                s_subMeshes.Add(renderer.sharedMaterials.Length);
            }
        }
    }
}
