using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Scripts.Dungeon
{
    /// The Tangled Catacombs: a square grid of rooms laid out anew from the match seed, the same on every peer. The key room
    /// (the Great Hall, built into the scene) keeps the middle cell; the four corners get corner rooms turned so their blind sides
    /// face out, every other cell a random room turned a random quarter. Every side has two doorways; a random maze with a few
    /// loops decides which of them open into the neighbours. The host bakes the NavMesh of the result.
    public sealed class CatacombGenerator : MonoBehaviour
    {
        [Serializable]
        private sealed class RoomKind
        {
            public CatacombRoomComponent Prefab;
            public float Weight = 1f;
            [Tooltip("At most this many per layout; 0 = any number")]
            public int Limit;
        }

        /// Room kind (-1 = key room), quarter turns and open doorways of every cell, row by row from the south-west corner.
        public sealed class Plan
        {
            public int[] Kinds;
            public int[] Turns;
            /// Doorways from a cell to its east / north neighbour: bit 0 the one nearer the south-west corner, bit 1 the other.
            public int[] East;
            public int[] North;
        }

        public int Floor => _floor;
        public int Grid => _grid;
        public float Size => _grid * _pitch;
        public Texture2D Map => _map;
        /// Grows with every new layout, so views know to redraw.
        public int Version => _version;
        public IReadOnlyList<CatacombRoomComponent> Rooms => _rooms;

        [SerializeField]
        private int _floor = MatchComponent.EntryFloor;

        [SerializeField]
        private int _grid = 7;

        [SerializeField, Tooltip("Distance between neighbouring cell centres: the room plus one shared wall")]
        private float _pitch = 34.2f;

        [SerializeField]
        private RoomKind[] _kinds;

        [SerializeField]
        private string _keyTitle = "Great Hall";

        [SerializeField]
        private Texture2D _keyMap;

        [SerializeField, Tooltip("Side between two cells by its open doorways (bit 0 at local -X, bit 1 at +X); length runs along local X")]
        private GameObject[] _sides = new GameObject[4];

        [SerializeField, Tooltip("Stands on every grid corner")]
        private GameObject _post;

        [SerializeField, Range(0f, 1f), Tooltip("Chance of the second doorway of a passage, and of a wall of the maze to open anyway, giving loops")]
        private float _loops = 0.25f;

        [SerializeField]
        private float _doorWidth = 3f;

        [SerializeField, Tooltip("The two doorways of a side sit this far either side of its middle")]
        private float _doorOffset = 8.4f;

        [SerializeField]
        private int _cellPixels = 144;

        [SerializeField]
        private Color _parchment = new(0.82f, 0.74f, 0.55f);

        [SerializeField]
        private Color _ink = new(0.2f, 0.13f, 0.07f);

        [SerializeField]
        private float _voxelSize = 0.2f;

        [SerializeField]
        private float _agentRadius = 0.35f;

        private const int NotWalkable = 1;
        private const int BothDoorways = 3;

        private static readonly Vector2Int[] s_steps = { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };

        private readonly List<CatacombRoomComponent> _rooms = new();
        private string[] _titles = Array.Empty<string>();
        private Transform _content;
        private Texture2D _map;
        private NavMeshDataInstance _navMesh;
        private int _seed;
        private int _version;

        private void Update()
        {
            MatchComponent match = DungeonContext.Instance != null ? DungeonContext.Instance.Match : null;

            if (match != null && match.Object != null && match.Object.IsValid && match.Seed != 0 && match.Seed != _seed)
                Generate(match.Seed);
        }

        private void OnDestroy()
        {
            if (_navMesh.valid)
                NavMesh.RemoveNavMeshData(_navMesh);
        }

        /// Lays the rooms of the seed out; the old layout is gone at once.
        public void Generate(int seed)
        {
            _seed = seed;
            _version++;
            _rooms.Clear();

            if (_content != null)
            {
                _content.gameObject.SetActive(false);
                Destroy(_content.gameObject);
            }

            _content = new GameObject("[Generated]").transform;
            _content.SetParent(transform, false);
            Plan plan = Lay(seed);
            _titles = new string[plan.Kinds.Length];

            for (int cell = 0; cell < plan.Kinds.Length; cell++)
            {
                if (plan.Kinds[cell] < 0)
                {
                    _titles[cell] = _keyTitle;
                    continue;
                }

                CatacombRoomComponent room = Instantiate(_kinds[plan.Kinds[cell]].Prefab, CellCenter(cell), Quaternion.Euler(0f, plan.Turns[cell] * 90f, 0f), _content);
                _titles[cell] = room.Title;
                _rooms.Add(room);
                PlaceSides(plan, cell);
            }

            for (int z = 0; z <= _grid; z++)
            {
                for (int x = 0; x <= _grid; x++)
                    Instantiate(_post, Corner(x, z), Quaternion.identity, _content);
            }

            if (_map != null)
                Destroy(_map);

            _map = Paint(plan);
        }

        /// Host only: the walkable surface of the whole floor, the key room included.
        public void BakeNavMesh()
        {
            if (_navMesh.valid)
                NavMesh.RemoveNavMeshData(_navMesh);

            NavMeshBuildSettings settings = NavMesh.GetSettingsByID(0);
            settings.agentRadius = _agentRadius;
            settings.overrideVoxelSize = true;
            settings.voxelSize = _voxelSize;
            List<NavMeshBuildMarkup> markups = new();
            List<NavMeshBuildSource> sources = new();

            foreach (NavMeshModifier modifier in GetComponentsInChildren<NavMeshModifier>())
                markups.Add(new NavMeshBuildMarkup { root = modifier.transform, overrideArea = modifier.overrideArea, area = modifier.area, ignoreFromBuild = modifier.ignoreFromBuild });

            // Tops of walls, solid rock and ceilings are no floor: nothing may stand or open a portal up there.
            foreach (Transform child in GetComponentsInChildren<Transform>())
            {
                if (child.name.StartsWith("Wall") || child.name.StartsWith("Ceiling") || child.name.StartsWith("Catacomb"))
                    markups.Add(new NavMeshBuildMarkup { root = child, overrideArea = true, area = NotWalkable });
            }

            NavMeshBuilder.CollectSources(transform, 1, NavMeshCollectGeometry.PhysicsColliders, 0, markups, sources);
            // Down to the floors of the pits, but not of the chasms: nothing walks down there.
            Bounds bounds = new Bounds(transform.position + Vector3.up * 1f, new Vector3(Size + 2f, 16f, Size + 2f));
            _navMesh = NavMesh.AddNavMeshData(NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity));
        }

        public string GetTitle(Vector3 position)
        {
            return GetTitle(GetCell(position));
        }

        /// The cell under a point, row by row from the south-west corner; points outside fall to the nearest border cell.
        public int GetCell(Vector3 position)
        {
            Vector3 local = position - transform.position;
            int x = Mathf.Clamp(Mathf.FloorToInt(local.x / _pitch + _grid * 0.5f), 0, _grid - 1);
            int z = Mathf.Clamp(Mathf.FloorToInt(local.z / _pitch + _grid * 0.5f), 0, _grid - 1);

            return z * _grid + x;
        }

        public string GetTitle(int cell)
        {
            return cell < _titles.Length ? _titles[cell] : string.Empty;
        }

        /// The maze: a random spanning tree grown from the key room opens one doorway of a passage, sometimes both; a share of
        /// the remaining walls get a doorway anyway for loops. The key room opens both doorways on all four sides. Corner cells
        /// draw from the corner rooms, the rest from the others, by weight within their limits.
        public Plan Lay(int seed)
        {
            System.Random random = new System.Random(seed);
            int count = _grid * _grid;
            int key = count / 2;
            Plan plan = new Plan { Kinds = new int[count], Turns = new int[count], East = new int[count], North = new int[count] };
            bool[] visited = new bool[count];
            Stack<int> stack = new();
            List<int> next = new();
            visited[key] = true;
            stack.Push(key);

            while (stack.Count > 0)
            {
                int cell = stack.Peek();
                next.Clear();

                foreach (Vector2Int step in s_steps)
                {
                    int neighbour = Neighbour(cell, step);

                    if (neighbour >= 0 && !visited[neighbour])
                        next.Add(neighbour);
                }

                if (next.Count == 0)
                {
                    stack.Pop();
                    continue;
                }

                int chosen = next[random.Next(next.Count)];
                Open(plan, cell, chosen, 1 << random.Next(2) | (random.NextDouble() < _loops ? BothDoorways : 0));
                visited[chosen] = true;
                stack.Push(chosen);
            }

            for (int cell = 0; cell < count; cell++)
            {
                if (cell % _grid < _grid - 1 && random.NextDouble() < _loops)
                    plan.East[cell] |= 1 << random.Next(2);

                if (cell / _grid < _grid - 1 && random.NextDouble() < _loops)
                    plan.North[cell] |= 1 << random.Next(2);
            }

            foreach (Vector2Int step in s_steps)
                Open(plan, key, Neighbour(key, step), BothDoorways);

            int[] used = new int[_kinds.Length];

            for (int cell = 0; cell < count; cell++)
            {
                int corner = CornerTurns(cell);
                plan.Kinds[cell] = cell == key ? -1 : PickKind(random, used, corner >= 0);
                plan.Turns[cell] = corner >= 0 ? corner : random.Next(4);
            }

            return plan;
        }

        /// Parchment map of a layout: the cell maps turned with their rooms, then the walls of the grid inked over them.
        public Texture2D Paint(Plan plan)
        {
            int n = _cellPixels;
            int size = n * _grid;
            Color32[] pixels = new Color32[size * size];

            for (int cell = 0; cell < plan.Kinds.Length; cell++)
            {
                Texture2D source = plan.Kinds[cell] < 0 ? _keyMap : _kinds[plan.Kinds[cell]].Prefab.Map;
                Blit(pixels, size, cell % _grid * n, cell / _grid * n, source != null ? source.GetPixels32() : null, n, plan.Turns[cell]);
            }

            for (int cell = 0; cell < plan.Kinds.Length; cell++)
            {
                int x = cell % _grid * n;
                int z = cell / _grid * n;
                Line(pixels, size, x, z, true, cell % _grid < _grid - 1 ? plan.East[cell] : 0);
                Line(pixels, size, x, z, false, cell / _grid < _grid - 1 ? plan.North[cell] : 0);

                if (cell % _grid == 0)
                    Line(pixels, size, x - n, z, true, 0);

                if (cell / _grid == 0)
                    Line(pixels, size, x, z - n, false, 0);
            }

            Texture2D map = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Catacombs" };
            map.SetPixels32(pixels);
            map.Apply();

            return map;
        }

        /// Quarter turns that put the blind south and west sides of a corner room outwards in a corner cell; -1 elsewhere.
        private int CornerTurns(int cell)
        {
            int last = _grid - 1;
            int x = cell % _grid;
            int z = cell / _grid;

            if (x != 0 && x != last || z != 0 && z != last)
                return -1;

            return x == 0 ? (z == 0 ? 0 : 1) : (z == 0 ? 3 : 2);
        }

        private int PickKind(System.Random random, int[] used, bool isCorner)
        {
            float total = 0f;

            for (int i = 0; i < _kinds.Length; i++)
                total += IsFree(i, used, isCorner) ? _kinds[i].Weight : 0f;

            float roll = (float)random.NextDouble() * total;
            int kind = 0;

            for (int i = 0; i < _kinds.Length; i++)
            {
                if (!IsFree(i, used, isCorner))
                    continue;

                kind = i;
                roll -= _kinds[i].Weight;

                if (roll <= 0f)
                    break;
            }

            used[kind]++;

            return kind;
        }

        private bool IsFree(int kind, int[] used, bool isCorner)
        {
            return _kinds[kind].Prefab.IsCorner == isCorner && (_kinds[kind].Limit <= 0 || used[kind] < _kinds[kind].Limit);
        }

        private int Neighbour(int cell, Vector2Int step)
        {
            int x = cell % _grid + step.x;
            int z = cell / _grid + step.y;

            return x >= 0 && x < _grid && z >= 0 && z < _grid ? z * _grid + x : -1;
        }

        private void Open(Plan plan, int a, int b, int doorways)
        {
            int low = Mathf.Min(a, b);

            if (Mathf.Abs(a - b) == 1)
                plan.East[low] |= doorways;
            else
                plan.North[low] |= doorways;
        }

        /// The east and north sides of a cell, the west and south ones on the border; sides of the key room are its own walls.
        /// East and west sides turn -90°, so local +X runs north and bit 1 is always the doorway farther from the south-west.
        private void PlaceSides(Plan plan, int cell)
        {
            int x = cell % _grid;
            int z = cell / _grid;
            int key = _grid * _grid / 2;
            Vector3 center = CellCenter(cell);
            float half = _pitch * 0.5f;

            if (cell + 1 != key)
                Side(center + Vector3.right * half, -90f, x < _grid - 1 ? plan.East[cell] : 0);

            if (cell + _grid != key)
                Side(center + Vector3.forward * half, 0f, z < _grid - 1 ? plan.North[cell] : 0);

            if (x == 0)
                Side(center + Vector3.left * half, -90f, 0);

            if (z == 0)
                Side(center + Vector3.back * half, 0f, 0);
        }

        private void Side(Vector3 position, float yaw, int doorways)
        {
            Instantiate(_sides[doorways], position, Quaternion.Euler(0f, yaw, 0f), _content);
        }

        private Vector3 CellCenter(int cell)
        {
            float offset = (_grid - 1) * 0.5f;

            return transform.position + new Vector3((cell % _grid - offset) * _pitch, 0f, (cell / _grid - offset) * _pitch);
        }

        private Vector3 Corner(int x, int z)
        {
            return transform.position + new Vector3((x - _grid * 0.5f) * _pitch, 0f, (z - _grid * 0.5f) * _pitch);
        }

        /// Copies a cell map turned by quarter turns (clockwise seen from above, as a room turned by +90° yaw).
        private void Blit(Color32[] target, int size, int left, int bottom, Color32[] source, int n, int turns)
        {
            for (int j = 0; j < n; j++)
            {
                for (int i = 0; i < n; i++)
                {
                    (int si, int sj) = turns switch
                    {
                        1 => (n - 1 - j, i),
                        2 => (n - 1 - i, n - 1 - j),
                        3 => (j, n - 1 - i),
                        _ => (i, j)
                    };

                    target[(bottom + j) * size + left + i] = source != null && source.Length == n * n ? source[sj * n + si] : (Color32)_parchment;
                }
            }
        }

        /// The east (or north) side of the cell at left/bottom as an ink stroke; open doorways leave their stretch out.
        private void Line(Color32[] target, int size, int left, int bottom, bool isEast, int doorways)
        {
            int n = _cellPixels;
            int gap = Mathf.RoundToInt(_doorWidth / _pitch * n * 0.5f);
            int offset = Mathf.RoundToInt(_doorOffset / _pitch * n);
            int across = (isEast ? left : bottom) + n;

            for (int t = 0; t < n; t++)
            {
                int along = (isEast ? bottom : left) + t;
                bool isDoorway = (doorways & 1) != 0 && Mathf.Abs(t - n / 2 + offset) < gap || (doorways & 2) != 0 && Mathf.Abs(t - n / 2 - offset) < gap;

                if (isDoorway || along < 0 || along >= size)
                    continue;

                for (int c = Mathf.Max(0, across - 2); c <= Mathf.Min(size - 1, across + 1); c++)
                    target[isEast ? along * size + c : c * size + along] = _ink;
            }
        }
    }
}
