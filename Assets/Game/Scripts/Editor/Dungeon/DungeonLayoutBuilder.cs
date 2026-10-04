using System.Globalization;
using System.IO;
using Game.Scripts.Dungeon;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Floor carved out of solid rock after a layout image: a pixel is a cell, red marks the floor and green its height.
    /// The image and the markers beside it are made by Tools/CryptLayout from a map of Dark and Darker.
    internal sealed class DungeonLayoutBuilder
    {
        public const string Folder = DungeonContentBuilder.ConfigsFolder + "/Layouts";

        public int Count => _count;

        /// Stairs are drawn in steps of this height; feet and NavMesh agents walk the smooth slope above them.
        private const float Step = 0.2f;
        private const float HeightScale = 50f;
        private const int HeightZero = 128;
        private const float Epsilon = 0.001f;

        private static readonly Vector2Int[] s_sides = { new(0, 1), new(1, 0), new(0, -1), new(-1, 0) };

        private readonly string _name;
        private readonly int _count;
        private readonly float _cell;
        private readonly float _half;
        private readonly float _ceiling;
        private readonly bool[,] _floor;
        private readonly float[,] _height;

        public DungeonLayoutBuilder(string name, float size, float ceiling)
        {
            Texture2D image = new Texture2D(2, 2);
            image.LoadImage(File.ReadAllBytes($"{Folder}/{name}.png"));
            Color32[] pixels = image.GetPixels32();
            _name = name;
            _count = image.width;
            _cell = size / _count;
            _half = size * 0.5f;
            _ceiling = ceiling;
            _floor = new bool[_count, _count];
            _height = new float[_count, _count];
            Object.DestroyImmediate(image);

            // Rows of the image run from the south.
            for (int z = 0; z < _count; z++)
            {
                for (int x = 0; x < _count; x++)
                {
                    Color32 pixel = pixels[z * _count + x];
                    _floor[x, z] = pixel.r > 127;
                    _height[x, z] = (pixel.g - HeightZero) / HeightScale;
                }
            }
        }

        public bool IsFloor(int x, int z)
        {
            return x >= 0 && z >= 0 && x < _count && z < _count && _floor[x, z];
        }

        public float Height(int x, int z)
        {
            return _height[x, z];
        }

        /// Geometry in chunks of the given number of cells, then everything the markers list.
        public void Build(Transform floor, Transform markers, DungeonFloorResult result, int chunk)
        {
            Transform geometry = new GameObject("Geometry").transform;
            geometry.SetParent(floor, false);

            for (int x = 0; x < _count; x += chunk)
            {
                for (int z = 0; z < _count; z += chunk)
                    BuildChunk(geometry, new RectInt(x, z, Mathf.Min(chunk, _count - x), Mathf.Min(chunk, _count - z)));
            }

            foreach (string line in File.ReadAllLines($"{Folder}/{_name}.txt"))
            {
                if (line.Length > 0)
                    Place(floor, markers, result, line.Split(';'));
            }
        }

        private void BuildChunk(Transform parent, RectInt area)
        {
            DungeonMeshBuilder flagstones = new DungeonMeshBuilder(0.5f);
            DungeonMeshBuilder cobble = new DungeonMeshBuilder(0.5f);
            DungeonMeshBuilder walls = new DungeonMeshBuilder(0.5f);
            DungeonMeshBuilder ceiling = new DungeonMeshBuilder(0.5f);
            DungeonMeshBuilder ground = new DungeonMeshBuilder(0.5f);
            bool[,] done = new bool[area.width, area.height];
            bool hasFloor = false;

            // Tops of the cells, merged into the largest rectangles of one height.
            for (int z = area.yMin; z < area.yMax; z++)
            {
                for (int x = area.xMin; x < area.xMax; x++)
                {
                    if (!_floor[x, z] || done[x - area.xMin, z - area.yMin])
                        continue;

                    hasFloor = true;
                    float top = Top(x, z);
                    RectInt rect = Grow(area, done, x, z, (cx, cz) => _floor[cx, cz] && Mathf.Abs(Top(cx, cz) - top) < Epsilon);
                    Flat(top < -Epsilon ? cobble : flagstones, rect, top, Vector3.up);
                    Flat(ceiling, rect, _ceiling, Vector3.down);
                }
            }

            if (!hasFloor)
                return;

            for (int side = 0; side < s_sides.Length; side++)
                Faces(area, side, walls, flagstones, cobble);

            Ground(area, ground);

            string name = $"{_name}_{area.xMin / area.width}_{area.yMin / area.height}";
            Transform chunk = new GameObject(name).transform;
            chunk.SetParent(parent, false);
            Add(chunk, "Flagstones", flagstones, DungeonPropBuilder.StoneFloor, false);
            Add(chunk, "Cobble", cobble, DungeonPropBuilder.Cobble, false);
            Add(chunk, "Walls", walls, DungeonPropBuilder.StoneWall, true);
            Add(chunk, "Ceiling", ceiling, DungeonPropBuilder.StoneWall, true);

            GameObject collider = new GameObject("Ground") { isStatic = true };
            collider.transform.SetParent(chunk, false);
            collider.AddComponent<MeshCollider>().sharedMesh = ground.Save(name + "_Ground");
        }

        private static void Add(Transform chunk, string name, DungeonMeshBuilder builder, Material material, bool hasCollider)
        {
            if (!builder.IsEmpty)
                DungeonPropBuilder.MeshObject(name, chunk, builder.Save($"{chunk.name}_{name}"), material, default, default, hasCollider);
        }

        /// Vertical faces on one side of the cells: walls up to the ceiling where the rock begins, risers where the
        /// neighbouring floor lies higher. Faces in a row that match are drawn as one.
        private void Faces(RectInt area, int side, DungeonMeshBuilder walls, DungeonMeshBuilder flagstones, DungeonMeshBuilder cobble)
        {
            Vector2Int direction = s_sides[side];
            bool isAlongX = direction.x == 0;
            int rows = isAlongX ? area.height : area.width;
            int length = isAlongX ? area.width : area.height;

            for (int row = 0; row < rows; row++)
            {
                int start = 0;
                float from = 0f;
                float to = 0f;
                bool isWall = false;

                for (int i = 0; i <= length; i++)
                {
                    float bottom = 0f;
                    float top = 0f;
                    bool isRock = false;

                    if (i < length)
                    {
                        int x = isAlongX ? area.xMin + i : area.xMin + row;
                        int z = isAlongX ? area.yMin + row : area.yMin + i;

                        if (_floor[x, z])
                        {
                            bottom = Top(x, z);
                            isRock = !IsFloor(x + direction.x, z + direction.y);
                            top = isRock ? _ceiling : Mathf.Max(bottom, Top(x + direction.x, z + direction.y));
                        }
                    }

                    if (i > start && Mathf.Abs(bottom - from) < Epsilon && Mathf.Abs(top - to) < Epsilon && isRock == isWall)
                        continue;

                    if (to - from > Epsilon)
                    {
                        int cx = isAlongX ? area.xMin + start : area.xMin + row;
                        int cz = isAlongX ? area.yMin + row : area.yMin + start;
                        float run = (i - start) * _cell;
                        Vector3 corner = new Vector3(cx * _cell - _half, from, cz * _cell - _half);
                        Vector3 along = isAlongX ? Vector3.right : Vector3.forward;
                        Vector3 edge = corner + new Vector3(Mathf.Max(direction.x, 0), 0f, Mathf.Max(direction.y, 0)) * _cell;
                        DungeonMeshBuilder builder = isWall ? walls : from < -Epsilon ? cobble : flagstones;
                        builder.Quad(edge + along * (run * 0.5f) + Vector3.up * ((to - from) * 0.5f), new Vector3(-direction.x, 0f, -direction.y),
                            along * (run * 0.5f), Vector3.up * ((to - from) * 0.5f), new Vector2(isAlongX ? edge.x : edge.z, from));
                    }

                    start = i;
                    from = bottom;
                    to = top;
                    isWall = isRock;
                }
            }
        }

        /// What feet stand on: level floors in large rectangles, slopes cell by cell.
        private void Ground(RectInt area, DungeonMeshBuilder builder)
        {
            bool[,] done = new bool[area.width, area.height];

            for (int z = area.yMin; z < area.yMax; z++)
            {
                for (int x = area.xMin; x < area.xMax; x++)
                {
                    if (!_floor[x, z] || done[x - area.xMin, z - area.yMin])
                        continue;

                    float height = Corner(x, z);

                    if (IsLevel(x, z, height))
                    {
                        Flat(builder, Grow(area, done, x, z, (cx, cz) => _floor[cx, cz] && IsLevel(cx, cz, height)), height, Vector3.up);

                        continue;
                    }

                    float x0 = x * _cell - _half;
                    float z0 = z * _cell - _half;
                    Vector3 a = new Vector3(x0, Corner(x, z), z0);
                    Vector3 b = new Vector3(x0, Corner(x, z + 1), z0 + _cell);
                    Vector3 c = new Vector3(x0 + _cell, Corner(x + 1, z + 1), z0 + _cell);
                    Vector3 d = new Vector3(x0 + _cell, Corner(x + 1, z), z0);
                    builder.Triangle(a, b, c).Triangle(a, c, d);
                }
            }
        }

        private bool IsLevel(int x, int z, float height)
        {
            return Mathf.Abs(Corner(x, z) - height) < Epsilon && Mathf.Abs(Corner(x + 1, z) - height) < Epsilon &&
                   Mathf.Abs(Corner(x, z + 1) - height) < Epsilon && Mathf.Abs(Corner(x + 1, z + 1) - height) < Epsilon;
        }

        /// Height of the slope at a corner of the grid: the highest of the floors that meet there, so it never sinks
        /// under the steps drawn below it.
        private float Corner(int x, int z)
        {
            float height = float.MinValue;

            for (int dx = -1; dx <= 0; dx++)
            {
                for (int dz = -1; dz <= 0; dz++)
                {
                    if (IsFloor(x + dx, z + dz))
                        height = Mathf.Max(height, _height[x + dx, z + dz]);
                }
            }

            return height;
        }

        private float Top(int x, int z)
        {
            return Mathf.Floor(_height[x, z] / Step + 0.01f) * Step;
        }

        /// The largest rectangle of matching cells that starts in the given one, east first, then north.
        private static RectInt Grow(RectInt area, bool[,] done, int x, int z, System.Func<int, int, bool> matches)
        {
            int width = 1;
            int depth = 1;

            while (x + width < area.xMax && !done[x + width - area.xMin, z - area.yMin] && matches(x + width, z))
                width++;

            for (; z + depth < area.yMax; depth++)
            {
                bool isRow = true;

                for (int i = 0; i < width && isRow; i++)
                    isRow = !done[x + i - area.xMin, z + depth - area.yMin] && matches(x + i, z + depth);

                if (!isRow)
                    break;
            }

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < depth; j++)
                    done[x + i - area.xMin, z + j - area.yMin] = true;
            }

            return new RectInt(x, z, width, depth);
        }

        private void Flat(DungeonMeshBuilder builder, RectInt rect, float y, Vector3 normal)
        {
            Vector2 min = new Vector2(rect.xMin * _cell - _half, rect.yMin * _cell - _half);
            Vector2 size = new Vector2(rect.width, rect.height) * _cell;
            builder.Quad(new Vector3(min.x + size.x * 0.5f, y, min.y + size.y * 0.5f), normal, Vector3.right * (size.x * 0.5f), Vector3.forward * (size.y * 0.5f), min);
        }

        /// A marker line: kind, prefab, position in the space of the floor, yaw.
        private void Place(Transform floor, Transform markers, DungeonFloorResult result, string[] fields)
        {
            string name = fields[1];
            Vector3 position = new Vector3(Number(fields[2]), Number(fields[3]), Number(fields[4]));
            float yaw = Number(fields[5]);

            switch (fields[0])
            {
                case "Put":
                    DungeonMapBuilder.Place(DungeonMapBuilder.Load(name), floor, position, yaw);
                    break;
                case "Dynamic":
                    DungeonMapBuilder.Place(DungeonMapBuilder.Load(name), floor, position, yaw, false);
                    break;
                case "Kit":
                    DungeonMapBuilder.Place(DungeonKitBuilder.Load(name), floor, position, yaw);
                    break;
                case "Chandelier":
                    DungeonMapBuilder.Place(DungeonMapBuilder.Load(name), floor, new Vector3(position.x, _ceiling, position.z), yaw);
                    break;
                case "Loot":
                    result.Containers.Add(DungeonMapBuilder.Place(DungeonMapBuilder.Load(name), floor, position, yaw, false).GetComponent<ContainerComponent>());
                    break;
                case "Escape":
                    result.EscapePortals.Add(DungeonMapBuilder.Place(DungeonMapBuilder.Load(name), floor, position, yaw, false).GetComponent<PortalComponent>());
                    break;
                case "Descend":
                    result.DescendPortal = DungeonMapBuilder.Place(DungeonMapBuilder.Load(name), floor, position, yaw, false).GetComponent<PortalComponent>();
                    break;
                case "Monster":
                    result.MonsterSpawns.Add(Marker(name, floor, markers, position, yaw));
                    break;
                case "Player":
                    result.PlayerSpawns.Add(Marker(name, floor, markers, position, yaw));
                    break;
                default:
                    throw new System.ArgumentException($"Unknown marker '{fields[0]}' in layout {_name}");
            }
        }

        private static Transform Marker(string name, Transform floor, Transform parent, Vector3 position, float yaw)
        {
            Transform marker = new GameObject(name).transform;
            marker.SetParent(parent, false);
            marker.SetPositionAndRotation(floor.position + position, Quaternion.Euler(0f, yaw, 0f));

            return marker;
        }

        private static float Number(string text)
        {
            return float.Parse(text, CultureInfo.InvariantCulture);
        }
    }
}
