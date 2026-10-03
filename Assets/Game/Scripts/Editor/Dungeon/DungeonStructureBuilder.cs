using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Architecture inside a module: blocks, stairs, railings, iron bars, inner walls with doorways, pits and the labyrinth.
    /// Everything is placed in the local space of the parent (a module).
    internal static class DungeonStructureBuilder
    {
        public const float StepHeight = 0.2f;
        public const float StepDepth = 0.35f;
        public const float DoorWidth = 2.2f;
        public const float DoorHeight = 3f;
        public const float InnerWallThickness = 0.5f;
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

        /// Iron grille standing on the given point, running along local X.
        public static GameObject Bars(Transform parent, Vector3 center, float yaw, float length, float height)
        {
            string key = $"Bars_{Format(length)}x{Format(height)}";
            Mesh mesh = Cached(key, () => BarsMesh(new DungeonMeshBuilder(1f), -length * 0.5f, length, height).Save(key));
            GameObject bars = DungeonPropBuilder.MeshObject("Bars", parent, mesh, DungeonPropBuilder.RustyMetal, center, new Vector3(0f, yaw, 0f), false);
            BoxCollider collider = bars.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, height * 0.5f, 0f);
            collider.size = new Vector3(length, height, 0.12f);

            return bars;
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

        /// Inner stone wall standing on the given point, running along local X.
        public static GameObject Wall(Transform parent, Vector3 center, float yaw, float length, float height)
        {
            return Block(parent, "InnerWall", center + Vector3.up * height * 0.5f, new Vector3(length, height, InnerWallThickness), DungeonPropBuilder.StoneWall, yaw);
        }

        /// Inner wall with a doorway shifted by the offset along the wall; the door itself is placed by the caller.
        public static void Doorway(Transform parent, Vector3 center, float yaw, float length, float offset, float height)
        {
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            float left = length * 0.5f + offset - DoorWidth * 0.5f;
            float right = length * 0.5f - offset - DoorWidth * 0.5f;

            if (left > 0.05f)
                Wall(parent, center + rotation * new Vector3(-length * 0.5f + left * 0.5f, 0f, 0f), yaw, left, height);

            if (right > 0.05f)
                Wall(parent, center + rotation * new Vector3(length * 0.5f - right * 0.5f, 0f, 0f), yaw, right, height);

            if (height > DoorHeight + 0.05f)
            {
                Block(parent, "Lintel", center + rotation * new Vector3(offset, DoorHeight + (height - DoorHeight) * 0.5f, 0f),
                    new Vector3(DoorWidth, height - DoorHeight, InnerWallThickness), DungeonPropBuilder.StoneWall, yaw);
            }
        }

        /// Module floor with a rectangular pit: flagstones around the hole, pit walls and an earthen bottom.
        public static GameObject PitFloor(Transform parent, string name, float size, Rect hole, float depth)
        {
            float half = size * 0.5f;
            DungeonMeshBuilder floor = new DungeonMeshBuilder(0.5f);
            Strip(floor, -half, hole.xMin, -half, half);
            Strip(floor, hole.xMax, half, -half, half);
            Strip(floor, hole.xMin, hole.xMax, -half, hole.yMin);
            Strip(floor, hole.xMin, hole.xMax, hole.yMax, half);

            Vector3 middle = new Vector3(hole.center.x, -depth * 0.5f, hole.center.y);
            Vector3 up = Vector3.up * depth * 0.5f;
            DungeonMeshBuilder walls = new DungeonMeshBuilder(0.5f)
                .Quad(middle + Vector3.left * hole.width * 0.5f, Vector3.right, Vector3.forward * hole.height * 0.5f, up)
                .Quad(middle + Vector3.right * hole.width * 0.5f, Vector3.left, Vector3.forward * hole.height * 0.5f, up)
                .Quad(middle + Vector3.back * hole.height * 0.5f, Vector3.forward, Vector3.right * hole.width * 0.5f, up)
                .Quad(middle + Vector3.forward * hole.height * 0.5f, Vector3.back, Vector3.right * hole.width * 0.5f, up);
            DungeonMeshBuilder bottom = new DungeonMeshBuilder(0.5f)
                .Quad(new Vector3(hole.center.x, -depth, hole.center.y), Vector3.up, Vector3.right * hole.width * 0.5f, Vector3.forward * hole.height * 0.5f);

            GameObject root = new GameObject("Floor");
            root.transform.SetParent(parent, false);
            DungeonPropBuilder.MeshObject("Flagstones", root.transform, floor.Save(name + "Floor"), DungeonPropBuilder.StoneFloor);
            DungeonPropBuilder.MeshObject("PitWalls", root.transform, walls.Save(name + "Walls"), DungeonPropBuilder.Cobble);
            DungeonPropBuilder.MeshObject("PitBottom", root.transform, bottom.Save(name + "Bottom"), DungeonPropBuilder.Dirt);

            return root;
        }

        /// Labyrinth walls as one mesh: wallsX[x, z] stands between cells (x, z) and (x + 1, z), wallsZ[x, z] between (x, z) and (x, z + 1).
        public static GameObject Maze(Transform parent, string name, bool[,] wallsX, bool[,] wallsZ, float cell, float height)
        {
            int count = wallsZ.GetLength(0);
            float half = count * cell * 0.5f;
            DungeonMeshBuilder builder = new DungeonMeshBuilder(0.5f);

            for (int x = 0; x < count; x++)
            {
                for (int z = 0; z < count; z++)
                {
                    if (x < count - 1 && wallsX[x, z])
                        builder.Box(new Vector3((x + 1) * cell - half, height * 0.5f, (z + 0.5f) * cell - half), new Vector3(InnerWallThickness, height, cell + InnerWallThickness));

                    if (z < count - 1 && wallsZ[x, z])
                        builder.Box(new Vector3((x + 0.5f) * cell - half, height * 0.5f, (z + 1) * cell - half), new Vector3(cell + InnerWallThickness, height, InnerWallThickness));
                }
            }

            return DungeonPropBuilder.MeshObject("Maze", parent, builder.Save(name), DungeonPropBuilder.StoneWall);
        }

        /// Stone throne with gilded finials, facing local +Z.
        public static GameObject Throne(Transform parent, Vector3 position, float yaw)
        {
            Mesh stone = Cached("Throne", () => new DungeonMeshBuilder(0.5f)
                .Box(new Vector3(0f, 0.15f, 0f), new Vector3(2.4f, 0.3f, 2.2f))
                .Box(new Vector3(0f, 0.6f, 0.1f), new Vector3(1.3f, 0.6f, 1.2f))
                .Box(new Vector3(0f, 2.1f, -0.6f), new Vector3(1.5f, 3.6f, 0.4f))
                .Box(new Vector3(-0.8f, 0.9f, 0.05f), new Vector3(0.3f, 1.2f, 1.3f))
                .Box(new Vector3(0.8f, 0.9f, 0.05f), new Vector3(0.3f, 1.2f, 1.3f))
                .Save("Throne"));
            Mesh gold = Cached("ThroneGold", () => new DungeonMeshBuilder(1f)
                .Cylinder(new Vector3(-0.6f, 4.1f, -0.6f), 0.12f, 0.5f, 6, 0.01f)
                .Cylinder(new Vector3(0.6f, 4.1f, -0.6f), 0.12f, 0.5f, 6, 0.01f)
                .Cylinder(new Vector3(0f, 4.3f, -0.6f), 0.18f, 0.8f, 6, 0.01f)
                .Box(new Vector3(-0.8f, 1.54f, 0.6f), new Vector3(0.34f, 0.1f, 0.3f))
                .Box(new Vector3(0.8f, 1.54f, 0.6f), new Vector3(0.34f, 0.1f, 0.3f))
                .Box(new Vector3(0f, 2.6f, -0.38f), new Vector3(0.9f, 1.6f, 0.04f))
                .Save("ThroneGold"));
            GameObject throne = DungeonPropBuilder.MeshObject("Throne", parent, stone, DungeonPropBuilder.StoneWall, position, new Vector3(0f, yaw, 0f));
            DungeonPropBuilder.MeshObject("Gilding", throne.transform, gold, DungeonPropBuilder.Gold, default, default, false);

            return throne;
        }

        /// Flat cloth runner lying on the floor between two points.
        public static GameObject Carpet(Transform parent, Vector3 from, Vector3 to, float width)
        {
            Vector3 delta = to - from;
            Vector3 right = Vector3.Cross(Vector3.up, delta.normalized) * width * 0.5f;
            string key = $"Carpet_{Format(delta.magnitude)}x{Format(width)}_{Format(Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg)}";
            Mesh mesh = Cached(key, () => new DungeonMeshBuilder(0.5f).Quad(Vector3.zero, Vector3.up, right, delta * 0.5f).Save(key));

            return DungeonPropBuilder.MeshObject("Carpet", parent, mesh, DungeonPropBuilder.ClothRed, (from + to) * 0.5f + Vector3.up * 0.02f, default, false);
        }

        private static void Strip(DungeonMeshBuilder builder, float xMin, float xMax, float zMin, float zMax)
        {
            if (xMax - xMin < 0.01f || zMax - zMin < 0.01f)
                return;

            builder.Quad(new Vector3((xMin + xMax) * 0.5f, 0f, (zMin + zMax) * 0.5f), Vector3.up, Vector3.right * (xMax - xMin) * 0.5f, Vector3.forward * (zMax - zMin) * 0.5f);
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
