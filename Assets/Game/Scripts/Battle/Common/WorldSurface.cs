using System.Collections.Generic;
using Game.Scripts.Dungeon;
using UnityEngine;

namespace Game.Scripts.Battle
{
    /// Picks the surface of struck level geometry: a footstep tag wins, else the material name; everything unknown is stone.
    public static class WorldSurface
    {
        private static readonly string[] s_woodNames = { "wood", "plank", "crate", "barrel", "table", "door", "beam", "shelf", "chair", "bench", "log", "dummy" };
        private static readonly string[] s_hardWoodNames = { "door", "beam", "log", "table" };
        private static readonly string[] s_grassNames = { "grass", "moss", "foliage", "leaf", "leaves" };
        private static readonly string[] s_snowNames = { "snow", "frost" };
        private static readonly Dictionary<GameObject, ImpactSurface> s_cache = new();

        public static ImpactSurface Of(GameObject target, Vector3 point)
        {
            if (target == null)
                return ImpactSurface.Stone;

            FootstepSurfaceTag surfaceTag = target.GetComponentInParent<FootstepSurfaceTag>();

            if (surfaceTag != null)
            {
                switch (surfaceTag.GetSurface(point))
                {
                    case FootstepSurface.Grass:
                        return ImpactSurface.Grass;
                    case FootstepSurface.Wood:
                    case FootstepSurface.WoodStair:
                        return ImpactSurface.WoodHard;
                }
            }

            if (s_cache.TryGetValue(target, out ImpactSurface surface))
                return surface;

            Renderer renderer = target.GetComponentInParent<Renderer>();

            if (renderer == null)
                renderer = target.GetComponentInChildren<Renderer>();

            surface = renderer != null ? OfMaterial(renderer.sharedMaterial) : ImpactSurface.Stone;
            s_cache[target] = surface;

            return surface;
        }

        public static bool IsWood(Material material)
        {
            return material != null && Contains(material.name, s_woodNames);
        }

        private static ImpactSurface OfMaterial(Material material)
        {
            if (material == null)
                return ImpactSurface.Stone;

            if (Contains(material.name, s_snowNames))
                return ImpactSurface.Snow;

            if (Contains(material.name, s_grassNames))
                return ImpactSurface.Grass;

            if (Contains(material.name, s_hardWoodNames))
                return ImpactSurface.WoodHard;

            return IsWood(material) ? ImpactSurface.Wood : ImpactSurface.Stone;
        }

        private static bool Contains(string name, string[] parts)
        {
            name = name.ToLowerInvariant();

            foreach (string part in parts)
            {
                if (name.Contains(part))
                    return true;
            }

            return false;
        }
    }
}
