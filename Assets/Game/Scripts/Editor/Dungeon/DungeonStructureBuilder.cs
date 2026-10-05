using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Simple architecture: blocks, stairs, railings and iron bars, placed in the local space of the parent.
    internal static class DungeonStructureBuilder
    {
        public const float StepHeight = 0.2f;
        public const float StepDepth = 0.35f;
        public const float RailHeight = 1f;

        private static readonly Dictionary<string, Mesh> s_meshes = new();

        public static GameObject Block(Transform parent, string name, Vector3 center, Vector3 size, Material material, float yaw = 0f, bool hasCollider = true)
        {
            string key = $"Block_{Format(size.x)}x{Format(size.y)}x{Format(size.z)}";
            Mesh mesh = Cached(key, () => new DungeonMeshBuilder(0.5f).Box(Vector3.zero, size).Save(key));
            GameObject block = DungeonPropBuilder.MeshObject(name, parent, mesh, material, center, new Vector3(0f, yaw, 0f), false);

            if (hasCollider)
                block.AddComponent<BoxCollider>();

            return block;
        }

        /// Horizontal run of a flight that climbs the given height.
        public static float StairRun(float height)
        {
            return StepCount(height) * StepDepth;
        }

        /// Flight of steps climbing along local +Z from the base point. Feet and NavMesh agents walk a hidden ramp,
        /// so nothing has to step up.
        public static GameObject Stairs(Transform parent, Vector3 basePoint, float yaw, float width, float height, Material material)
        {
            int steps = StepCount(height);
            float rise = height / steps;
            float run = steps * StepDepth;
            string key = $"Stairs_{Format(width)}x{Format(height)}";
            Mesh visual = Cached(key, () =>
            {
                DungeonMeshBuilder builder = new DungeonMeshBuilder(0.5f);

                for (int i = 0; i < steps; i++)
                    builder.Box(new Vector3(0f, rise * (i + 1) * 0.5f, StepDepth * (i + 0.5f)), new Vector3(width, rise * (i + 1), StepDepth));

                return builder.Save(key);
            });
            Mesh ramp = Cached(key + "_Ramp", () => new DungeonMeshBuilder(0.5f).Prism(width, new Vector2(0f, 0f), new Vector2(0f, rise * 0.5f),
                new Vector2(run - StepDepth * 0.5f, height), new Vector2(run, height), new Vector2(run, 0f)).Save(key + "_Ramp"));
            GameObject stairs = DungeonPropBuilder.MeshObject("Stairs", parent, visual, material, basePoint, new Vector3(0f, yaw, 0f), false);
            MeshCollider collider = stairs.AddComponent<MeshCollider>();
            collider.sharedMesh = ramp;
            collider.convex = true;

            return stairs;
        }

        /// Wooden railing between two points of the same height.
        public static GameObject Rail(Transform parent, Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            float length = delta.magnitude;
            string key = $"Rail_{Format(length)}";
            Mesh mesh = Cached(key, () =>
            {
                DungeonMeshBuilder builder = new DungeonMeshBuilder(0.8f)
                    .Box(new Vector3(length * 0.5f, RailHeight - 0.04f, 0f), new Vector3(length, 0.08f, 0.14f))
                    .Box(new Vector3(length * 0.5f, RailHeight * 0.5f, 0f), new Vector3(length, 0.06f, 0.06f));
                int posts = Mathf.Max(1, Mathf.RoundToInt(length / 1.6f));

                for (int i = 0; i <= posts; i++)
                    builder.Box(new Vector3(length * i / posts, RailHeight * 0.5f, 0f), new Vector3(0.1f, RailHeight, 0.1f));

                return builder.Save(key);
            });
            float yaw = Mathf.Atan2(-delta.z, delta.x) * Mathf.Rad2Deg;
            GameObject rail = DungeonPropBuilder.MeshObject("Rail", parent, mesh, DungeonPropBuilder.DarkWood, from, new Vector3(0f, yaw, 0f), false);
            BoxCollider collider = rail.AddComponent<BoxCollider>();
            collider.center = new Vector3(length * 0.5f, RailHeight * 0.5f, 0f);
            collider.size = new Vector3(length, RailHeight, 0.14f);

            return rail;
        }

        public static DungeonMeshBuilder BarsMesh(DungeonMeshBuilder builder, float start, float length, float height)
        {
            int count = Mathf.Max(2, Mathf.RoundToInt(length / 0.2f));

            for (int i = 0; i <= count; i++)
                builder.Box(new Vector3(start + length * i / count, height * 0.5f, 0f), new Vector3(0.05f, height, 0.05f));

            foreach (float y in new[] { 0.12f, height * 0.5f, height - 0.12f })
                builder.Box(new Vector3(start + length * 0.5f, y, 0f), new Vector3(length, 0.07f, 0.09f));

            return builder;
        }

        private static int StepCount(float height)
        {
            return Mathf.Max(1, Mathf.RoundToInt(height / StepHeight));
        }

        private static Mesh Cached(string key, System.Func<Mesh> create)
        {
            if (s_meshes.TryGetValue(key, out Mesh mesh) && mesh != null)
                return mesh;

            mesh = create();
            s_meshes[key] = mesh;

            return mesh;
        }

        private static string Format(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
