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
        private static InteractableComponent s_target;

        public static void Set(InteractableComponent target)
        {
            if (target == s_target)
                return;

            s_target = target;
            s_renderers.Clear();
            s_subMeshes.Clear();

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
