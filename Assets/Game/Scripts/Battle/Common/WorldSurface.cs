using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Battle
{
    /// Tells wood from stone by the material of the struck level geometry; everything unknown is stone.
    public static class WorldSurface
    {
        private static readonly string[] s_woodNames = { "wood", "plank", "crate", "barrel", "table", "door", "beam", "shelf", "chair", "bench", "log", "dummy" };
        private static readonly Dictionary<GameObject, ImpactSurface> s_cache = new();

        public static ImpactSurface Of(GameObject target)
        {
            if (target == null)
                return ImpactSurface.Stone;

            if (s_cache.TryGetValue(target, out ImpactSurface surface))
                return surface;

            Renderer renderer = target.GetComponentInParent<Renderer>();

            if (renderer == null)
                renderer = target.GetComponentInChildren<Renderer>();

            surface = renderer != null && IsWood(renderer.sharedMaterial) ? ImpactSurface.Wood : ImpactSurface.Stone;
            s_cache[target] = surface;

            return surface;
        }

        private static bool IsWood(Material material)
        {
            if (material == null)
                return false;

            string name = material.name.ToLowerInvariant();

            foreach (string wood in s_woodNames)
            {
                if (name.Contains(wood))
                    return true;
            }

            return false;
        }
    }
}
