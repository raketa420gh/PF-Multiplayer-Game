using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Game.Scripts.Editor.Dungeon
{
    /// The look of a mockup room in the game (Docs/LevelMockups/Atlas/data/detail): tens of thousands of detail pieces (stones of
    /// the masonry, moss, planks, chains, rubble…) become a few hundred combined meshes per room, by material, size class and
    /// part of the room, on Poly Haven scans (CC0). Man-made pieces get chamfered edges, natural ones rough lumps; small pieces
    /// cast no shadows and fade out with distance. Kit blocks take the PBR surface the mockup gives them. Pieces drawing what
    /// the game already has a model of (doors, lamps, props with a model) are left out; props without a model lose their block
    /// body to their pieces (the collider stays).
    internal static class CatacombDetailBuilder
    {
        public const string Root = "Detail";
        private const string DetailFolder = "Docs/LevelMockups/Atlas/data/detail";
        private const string TexturesFolder = "Assets/Game/Textures/PolyHaven";
        private const string MaterialsFolder = DungeonPropBuilder.MaterialsFolder + "/Catacomb";
        public const string MeshesFolder = DungeonMeshBuilder.Folder + "/Detail";
        private const int MaskSize = 1024;
        private const float BigChunk = 11.4f;
        private const float SmallChunk = 11.4f;
        private const float ChunkHeight = 8f;
        /// Pieces under this size fade out at the distance where the chunk is this part of the screen height (~25 m).
        private const float SmallPiece = 0.3f;
        private const float SmallCull = 0.3f;
        /// Only pieces this large cast shadows.
        private const float ShadowPiece = 0.6f;
        private const float FlatPiece = 0.025f;
        /// Cracks, soot, dust, drawings: thin as paint on the surface, not slats standing off it.
        private const float Decal = 0.006f;

        private sealed class Surface
        {
            public string Key;
            public string Texture;
            public float Tile;
            public Color Tint = Color.white;
            /// Pieces mapped in world space (Game/WorldStone): the surface runs on across neighbouring pieces. Kit blocks always are.
            public bool IsWorld;
            public float Smoothness = 1f;
            /// Metal of an untextured surface (a textured one takes it from its mask).
            public float Metallic;
            public Color Emission = Color.black;
            /// Natural lumps instead of chamfered boxes.
            public bool IsRough;
            /// Kit blocks of this key: their own surface (a pattern of courses), else the piece surface laid in world space.
            public string Block;
        }

        private sealed class Piece
        {
            public string Name;
            public string Key;
            public Vector3 Position;
            public Vector3 Size;
            public Quaternion Rotation;
            public bool IsRound;
            public string Owner;
            public int Seed;
        }

        private sealed class Bucket
        {
            public readonly List<Vector3> Vertices = new();
            public readonly List<Vector3> Normals = new();
            public readonly List<Vector2> Uvs = new();
            public readonly List<int> Triangles = new();
        }

        private enum Size
        {
            Small,
            Mid,
            Large
        }

        private static readonly Surface[] s_surfaces =
        {
            new() { Key = "rock", Texture = "rock_wall_02", Tile = 2.5f, Tint = new Color(0.82f, 0.8f, 0.78f), IsRough = true },
            new() { Key = "stone", Texture = "rock_surface", Tile = 1.2f, Tint = new Color(0.74f, 0.73f, 0.71f), Block = "CatacombWall" },
            new() { Key = "marble", Texture = "marble_01", Tile = 0.8f, Tint = new Color(0.72f, 0.7f, 0.66f), Smoothness = 0.8f },
            new() { Key = "plaster", Texture = "plastered_stone_wall", Tile = 2.5f, Tint = new Color(1.3f, 1.24f, 1.12f), IsWorld = true },
            new() { Key = "brick", Texture = "clay_floor_001", Tile = 1f, Tint = new Color(0.56f, 0.42f, 0.35f), Block = "CatacombBrick" },
            new() { Key = "slab", Texture = "concrete_floor_worn_001", Tile = 3f, Tint = new Color(0.62f, 0.61f, 0.58f), Block = "CatacombSlate" },
            new() { Key = "cobble", Texture = "rock_boulder_dry", Tile = 1.8f, Tint = new Color(0.68f, 0.66f, 0.62f), IsRough = true, Block = "CatacombFloor" },
            new() { Key = "pebble", Texture = "rock_boulder_dry", Tile = 1f, Tint = new Color(0.6f, 0.58f, 0.55f), IsRough = true },
            new() { Key = "planks", Texture = "wood_planks", Tile = 1.5f, Tint = new Color(0.82f, 0.74f, 0.64f), IsWorld = true },
            new() { Key = "studs", Texture = "rough_wood", Tile = 0.8f, Tint = new Color(0.42f, 0.36f, 0.3f) },
            new() { Key = "wood", Texture = "rough_wood", Tile = 0.8f, Tint = new Color(0.78f, 0.68f, 0.56f) },
            new() { Key = "timber", Texture = "rough_wood", Tile = 0.9f, Tint = new Color(0.5f, 0.42f, 0.34f) },
            new() { Key = "moss", Texture = "mossy_rock", Tile = 1.5f, Tint = new Color(0.62f, 0.82f, 0.48f), IsRough = true, Block = "CatacombMossy" },
            new() { Key = "dirt", Texture = "dirt_floor", Tile = 2f, Tint = new Color(0.8f, 0.74f, 0.66f), IsRough = true },
            new() { Key = "sand", Texture = "sand_01", Tile = 1.5f, Tint = new Color(0.66f, 0.62f, 0.55f), IsRough = true },
            new() { Key = "ash", Texture = "burned_ground_01", Tile = 1f, Tint = new Color(0.62f, 0.6f, 0.58f), IsRough = true },
            new() { Key = "soot", Texture = "burned_ground_01", Tile = 1f, Tint = new Color(0.22f, 0.2f, 0.19f), Smoothness = 0.6f },
            new() { Key = "iron", Texture = "rusty_metal_05", Tile = 1.25f, Tint = new Color(0.8f, 0.75f, 0.72f) },
            new() { Key = "bronze", Texture = "rust_coarse_01", Tile = 1.1f, Tint = new Color(0.85f, 0.68f, 0.48f) },
            new() { Key = "lead", Texture = "rusty_metal_04", Tile = 1f, Tint = new Color(0.6f, 0.62f, 0.66f) },
            new() { Key = "gold", Tint = new Color(1f, 0.74f, 0.32f), Smoothness = 0.72f, Metallic = 1f },
            new() { Key = "glass", Tint = new Color(0.36f, 0.48f, 0.42f), Smoothness = 0.92f },
            new() { Key = "cloth", Texture = "dirty_carpet", Tile = 0.6f, Tint = new Color(1.6f, 1.45f, 1.2f) },
            new() { Key = "clothRed", Texture = "dirty_carpet", Tile = 0.6f, Tint = new Color(1.1f, 0.5f, 0.42f) },
            new() { Key = "clothBlue", Texture = "dirty_carpet", Tile = 0.6f, Tint = new Color(0.8f, 0.95f, 1.15f) },
            new() { Key = "leather", Texture = "brown_leather", Tile = 0.4f, Tint = new Color(0.8f, 0.7f, 0.62f) },
            new() { Key = "rope", Texture = "bark_brown_02", Tile = 0.3f, Tint = new Color(1.2f, 1.05f, 0.75f) },
            new() { Key = "clay", Texture = "clay_floor_001", Tile = 0.7f, Tint = new Color(0.82f, 0.55f, 0.42f) },
            new() { Key = "straw", Texture = "wood_chips", Tile = 0.5f, Tint = new Color(1.25f, 1.08f, 0.66f) },
            new() { Key = "bone", Texture = "white_plaster_rough_01", Tile = 0.5f, Tint = new Color(0.88f, 0.83f, 0.7f) },
            new() { Key = "paper", Texture = "white_plaster_rough_01", Tile = 0.5f, Tint = new Color(0.86f, 0.79f, 0.62f) },
            new() { Key = "root", Texture = "bark_brown_02", Tile = 0.6f, Tint = new Color(0.62f, 0.52f, 0.4f), IsRough = true },
            new() { Key = "blood", Texture = "brown_mud", Tile = 0.6f, Tint = new Color(0.42f, 0.1f, 0.08f), Smoothness = 1.4f },
            new() { Key = "wet", Tint = new Color(0.12f, 0.15f, 0.16f), Smoothness = 0.93f },
            new() { Key = "fungus", Texture = "white_plaster_rough_01", Tile = 0.4f, Tint = new Color(0.84f, 0.8f, 0.64f), Emission = new Color(0.16f, 0.15f, 0.07f) },
            new() { Key = "ossarite", Texture = "white_plaster_rough_01", Tile = 0.4f, Tint = new Color(0.66f, 0.85f, 1f), Emission = new Color(0.25f, 0.62f, 1f) * 1.6f },
            new() { Key = "ember", Tint = new Color(1f, 0.54f, 0.23f), Emission = new Color(1f, 0.35f, 0.08f) * 3f },
            new() { Key = "rune", Tint = new Color(0.7f, 0.55f, 1f), Emission = new Color(0.48f, 0.24f, 1f) * 2f },
            new() { Key = "acid", Tint = new Color(0.55f, 1f, 0.42f), Emission = new Color(0.24f, 1f, 0.16f) * 2f }
        };

        /// Kit pieces the mockup's look may repaint (of props only their kit blocks, never a model).
        private static readonly HashSet<string> s_blockCategories = new() { "wall", "upper", "floor", "platform", "pit", "column", "stairs", "ceiling", "prop" };
        /// The leaf of a mockup door: the game puts its own door in the doorway.
        private static readonly HashSet<string> s_doorLeaf = new() { "Доска двери", "Кованая полоса", "Кольцо двери" };
        /// What the lamps leave on the walls and floors stays; holders, candles and flames are the lamp models' own.
        private static readonly HashSet<string> s_lampMarks = new() { "Копоть", "Сажа", "Зола" };

        private static readonly Dictionary<string, Material> s_materials = new();
        private static readonly Dictionary<string, Material> s_blocks = new();

        [MenuItem("Tools/Game/Dungeon/Catacombs/Build Detail Materials")]
        public static void BuildMaterials()
        {
            s_materials.Clear();
            s_blocks.Clear();
            BattleEditorUtility.EnsureFolder($"{MaterialsFolder}/Detail");

            foreach (Surface surface in s_surfaces)
            {
                s_materials[surface.Key] = MakeMaterial($"{MaterialsFolder}/Detail/{surface.Key}.mat", surface, surface.IsWorld);

                if (surface.Texture != null)
                    s_blocks[surface.Key] = surface.Block != null ? AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/{surface.Block}.mat")
                        : MakeMaterial($"{MaterialsFolder}/Catacomb{char.ToUpperInvariant(surface.Key[0])}{surface.Key.Substring(1)}.mat", surface, true);
            }

            AssetDatabase.SaveAssets();
        }

        /// Surface of a kit block of this look key, null if the key has none.
        public static Material Block(string key)
        {
            if (s_blocks.Count == 0)
                BuildMaterials();

            return s_blocks.TryGetValue(key, out Material material) ? material : null;
        }

        /// Repaints the room's kit blocks, hides block bodies of props drawn by their pieces and builds the pieces under Detail.
        /// placed: what each mockup item ("cat:id") put into the room.
        public static void Dress(string room, Transform root, List<HallItem> items, Dictionary<string, List<GameObject>> placed)
        {
            if (s_materials.Count == 0)
                BuildMaterials();

            Dictionary<string, string> keys = items.Where(item => !string.IsNullOrEmpty(item.m)).ToDictionary(item => $"{item.cat}:{item.id}", item => item.m);
            string lookPath = Path.Combine(Application.dataPath, "..", DetailFolder, $"{room}.look.json");
            HashSet<string> hidden = new(items.Where(item => item.hide).Select(item => $"{item.cat}:{item.id}"));

            if (File.Exists(lookPath))
            {
                string look = File.ReadAllText(lookPath);

                foreach (Match match in Regex.Matches(look, "\"(\\w+)\":\\[([^\\]]*)\\]"))
                {
                    string[] ids = Regex.Matches(match.Groups[2].Value, "\"([^\"]+)\"").Select(id => id.Groups[1].Value).ToArray();

                    if (match.Groups[1].Value == "hide")
                        hidden.UnionWith(ids);
                    else
                        foreach (string id in ids)
                            keys[id] = match.Groups[1].Value;
                }
            }

            foreach (KeyValuePair<string, string> pair in keys.Where(pair => s_blockCategories.Contains(pair.Key.Split(':')[0]) && placed.ContainsKey(pair.Key)))
                Repaint(placed[pair.Key], Block(pair.Value));

            List<Piece> pieces = Load(room);

            if (pieces.Count == 0)
                return;

            HashSet<string> modelled = new(placed.Where(pair => pair.Key.StartsWith("prop:") && pair.Value.Any(IsModel)).Select(pair => pair.Key.Substring(5)));
            HashSet<string> owners = new(pieces.Where(piece => piece.Owner != null).Select(piece => piece.Owner));

            // A prop without a model whose look the mockup builds of pieces: the pieces are its body.
            foreach (KeyValuePair<string, List<GameObject>> pair in placed.Where(pair => pair.Key.StartsWith("prop:")))
            {
                string id = pair.Key.Substring(5);

                if (modelled.Contains(id) || !owners.Contains(id) && !hidden.Contains(pair.Key))
                    continue;

                foreach (Renderer renderer in pair.Value.SelectMany(go => go.GetComponentsInChildren<Renderer>(true)))
                    renderer.enabled = false;
            }

            List<Piece> kept = pieces.Where(piece => !s_doorLeaf.Contains(piece.Name)
                && !(piece.Owner == "light" && !s_lampMarks.Contains(piece.Name))
                && !(piece.Owner != null && modelled.Contains(piece.Owner))).ToList();
            Build(room, root, kept);
            Debug.Log($"{room}: {kept.Count} of {pieces.Count} detail pieces");
        }

        private static void Repaint(List<GameObject> objects, Material material)
        {
            if (material == null)
                return;

            foreach (MeshRenderer renderer in objects.SelectMany(go => go.GetComponentsInChildren<MeshRenderer>(true))
                         .Where(renderer => renderer.TryGetComponent(out KitBlockComponent _) || renderer.TryGetComponent(out KitStairsComponent _)))
                renderer.sharedMaterials = renderer.sharedMaterials.Select(old => old != null && old.renderQueue < (int)RenderQueue.Transparent ? material : old).ToArray();
        }

        private static bool IsModel(GameObject go)
        {
            return go.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length > 0 || go.GetComponentsInChildren<MeshFilter>(true).Any(filter =>
            {
                string path = filter.sharedMesh != null ? AssetDatabase.GetAssetPath(filter.sharedMesh).ToLowerInvariant() : string.Empty;

                return path.EndsWith(".fbx") || path.EndsWith(".obj") || path.EndsWith(".glb") || path.EndsWith(".gltf") || path.EndsWith(".blend");
            });
        }

        // ---------- the mockup's pieces ----------

        /// Rows of the packed detail file: [name, material, x, y, z, w, h, d, ry | [quaternion], flags, owner].
        private static List<Piece> Load(string room)
        {
            string path = Path.Combine(Application.dataPath, "..", DetailFolder, $"{room}.js");
            List<Piece> pieces = new();

            if (!File.Exists(path))
                return pieces;

            string text = File.ReadAllText(path);
            string[] names = Strings(text, "\"names\":[");
            string[] mats = Strings(text, "\"mats\":[");
            int start = text.IndexOf("\"items\":[", StringComparison.Ordinal);

            foreach (string line in text.Substring(start).Split('\n').Skip(1))
            {
                string row = line.Trim().TrimEnd(',');

                if (!row.StartsWith("["))
                    continue;

                bool hasQuaternion = row.IndexOf('[', 1) >= 0;
                string owner = row.Contains('"') ? row.Substring(row.IndexOf('"') + 1, row.LastIndexOf('"') - row.IndexOf('"') - 1) : null;
                string[] tokens = (owner != null ? row.Substring(0, row.IndexOf('"')) : row).Split(new[] { '[', ']', ',' }, StringSplitOptions.RemoveEmptyEntries);
                float F(int i) => float.Parse(tokens[i], CultureInfo.InvariantCulture);
                int flagsAt = hasQuaternion ? 12 : 9;

                pieces.Add(new Piece
                {
                    Name = names[(int)F(0)],
                    Key = mats[(int)F(1)],
                    Position = new Vector3(F(2), F(3), F(4)),
                    Size = new Vector3(F(5), F(6), F(7)),
                    Rotation = hasQuaternion ? new Quaternion(F(8), F(9), F(10), F(11)).normalized : Quaternion.Euler(0f, F(8), 0f),
                    IsRound = tokens.Length > flagsAt && ((int)F(flagsAt) & 1) != 0,
                    Owner = owner,
                    Seed = pieces.Count * 7919 + 17
                });
            }

            return pieces;
        }

        private static string[] Strings(string text, string head)
        {
            int start = text.IndexOf(head, StringComparison.Ordinal) + head.Length;
            string list = text.Substring(start, text.IndexOf(']', start) - start);

            return Regex.Matches(list, "\"((?:[^\"\\\\]|\\\\.)*)\"").Select(match => Regex.Unescape(match.Groups[1].Value)).ToArray();
        }

        // ---------- meshes ----------

        /// The pieces go to <room>.catdetail (CatacombDetailImporter makes the meshes of it on import, so they stay out of the
        /// repository), the room gets a renderer per mesh.
        private static void Build(string room, Transform root, List<Piece> pieces)
        {
            Transform old = root.Find(Root);

            if (old != null)
                Object.DestroyImmediate(old.gameObject);

            Transform detail = new GameObject(Root).transform;
            detail.SetParent(root, false);
            string path = $"{MeshesFolder}/{room}.{CatacombDetailImporter.Extension}";
            BattleEditorUtility.EnsureFolder(MeshesFolder);
            File.WriteAllBytes(path, Write(pieces));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            foreach (Mesh mesh in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().Where(mesh => mesh.name.Contains('|')).OrderBy(mesh => mesh.name, StringComparer.Ordinal))
            {
                string[] parts = mesh.name.Split('|');
                Size size = (Size)Enum.Parse(typeof(Size), parts[1]);
                GameObject chunk = new GameObject($"{Root} {size} {parts[0]} {parts[2]}");
                chunk.transform.SetParent(detail, false);
                chunk.AddComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer renderer = chunk.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = s_materials[parts[0]];
                renderer.shadowCastingMode = size == Size.Large ? ShadowCastingMode.On : ShadowCastingMode.Off;
                renderer.receiveGI = ReceiveGI.LightProbes;
                renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
                renderer.allowOcclusionWhenDynamic = true;

                if (size != Size.Small)
                    continue;

                LODGroup lod = chunk.AddComponent<LODGroup>();
                lod.SetLODs(new[] { new LOD(SmallCull, new Renderer[] { renderer }) });
                lod.RecalculateBounds();
            }
        }

        private static byte[] Write(List<Piece> pieces)
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(pieces.Count);

            foreach (Piece piece in pieces)
            {
                writer.Write(piece.Key);
                foreach (float v in new[] { piece.Position.x, piece.Position.y, piece.Position.z, piece.Size.x, piece.Size.y, piece.Size.z, piece.Rotation.x, piece.Rotation.y, piece.Rotation.z, piece.Rotation.w })
                    writer.Write(v);
                writer.Write(piece.IsRound);
                writer.Write(piece.Seed);
            }

            return stream.ToArray();
        }

        /// Meshes of a .catdetail file, named "surface|size class|chunk": by material, size class and part of the room.
        public static List<Mesh> Meshes(byte[] data)
        {
            using BinaryReader reader = new BinaryReader(new MemoryStream(data));
            int count = reader.ReadInt32();
            Dictionary<string, Bucket> buckets = new();

            for (int i = 0; i < count; i++)
            {
                Piece piece = new Piece { Key = reader.ReadString() };
                float[] v = Enumerable.Range(0, 10).Select(_ => reader.ReadSingle()).ToArray();
                piece.Position = new Vector3(v[0], v[1], v[2]);
                piece.Size = new Vector3(v[3], v[4], v[5]);
                piece.Rotation = new Quaternion(v[6], v[7], v[8], v[9]);
                piece.IsRound = reader.ReadBoolean();
                piece.Seed = reader.ReadInt32();
                Surface surface = s_surfaces.FirstOrDefault(entry => entry.Key == piece.Key) ?? s_surfaces[1];
                float largest = Mathf.Max(piece.Size.x, piece.Size.y, piece.Size.z);
                float smallest = Mathf.Min(piece.Size.x, piece.Size.y, piece.Size.z);
                Size size = largest < SmallPiece || smallest < FlatPiece ? Size.Small : largest < ShadowPiece ? Size.Mid : Size.Large;
                float chunk = size == Size.Small ? SmallChunk : BigChunk;
                Vector3 center = piece.Position + piece.Rotation * (Vector3.up * piece.Size.y * 0.5f);
                string key = $"{surface.Key}|{size}|{Mathf.FloorToInt(center.x / chunk)},{Mathf.FloorToInt(center.y / ChunkHeight)},{Mathf.FloorToInt(center.z / chunk)}";

                if (!buckets.TryGetValue(key, out Bucket bucket))
                    buckets[key] = bucket = new Bucket();

                AddPiece(bucket, piece, surface, smallest);
            }

            List<Mesh> meshes = new();

            foreach (KeyValuePair<string, Bucket> pair in buckets.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                Mesh mesh = new Mesh { name = pair.Key, indexFormat = pair.Value.Vertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.SetVertices(pair.Value.Vertices);
                mesh.SetNormals(pair.Value.Normals);
                mesh.SetUVs(0, pair.Value.Uvs);
                mesh.SetTriangles(pair.Value.Triangles, 0);
                mesh.RecalculateTangents();
                mesh.RecalculateBounds();
                meshes.Add(mesh);
            }

            return meshes;
        }

        private static void AddPiece(Bucket bucket, Piece piece, Surface surface, float smallest)
        {
            int first = bucket.Vertices.Count;
            System.Random random = new System.Random(piece.Seed);
            float largest = Mathf.Max(piece.Size.x, piece.Size.y, piece.Size.z);
            Vector2 shift = new Vector2((float)random.NextDouble(), (float)random.NextDouble()) * 7f;

            Vector3 shape = piece.Size;

            if (smallest < FlatPiece)
                shape[shape.x <= shape.y && shape.x <= shape.z ? 0 : shape.y <= shape.z ? 1 : 2] = Mathf.Min(smallest, Decal);

            // Grain (V of the texture) along the piece's long side; every piece its own part of the texture.
            int grain = piece.Size.x >= piece.Size.y && piece.Size.x >= piece.Size.z ? 0 : piece.Size.y >= piece.Size.z ? 1 : 2;

            if (piece.IsRound)
                Cylinder(bucket, shape, largest >= 0.5f ? 14 : largest >= 0.15f ? 10 : 6);
            else if (smallest < FlatPiece || largest < SmallPiece)
                Chamfered(bucket, shape, 0f, null, grain);
            else if (surface.IsRough)
                Chamfered(bucket, piece.Size, Mathf.Min(0.3f * smallest, 0.14f), random, grain);
            else
                Chamfered(bucket, piece.Size, Mathf.Min(0.16f * smallest, 0.025f), null, grain);

            float scale = surface.Tile > 0f ? 1f / surface.Tile : 1f;

            for (int i = first; i < bucket.Vertices.Count; i++)
            {
                bucket.Uvs[i] = bucket.Uvs[i] * scale + shift;
                bucket.Vertices[i] = piece.Position + piece.Rotation * (bucket.Vertices[i] + Vector3.up * piece.Size.y * 0.5f);
                bucket.Normals[i] = piece.Rotation * bucket.Normals[i];
            }
        }

        /// A box centred on the origin with its edges cut at bevel: edge and corner facets take the normals of the faces they join,
        /// so they shade round between them, and the UVs of one of those faces, so the texture does not smear across them.
        /// 44 triangles (12 without a bevel). random: rough lump. UVs in metres.
        private static void Chamfered(Bucket bucket, Vector3 size, float bevel, System.Random random, int grain)
        {
            Vector3 half = size * 0.5f;
            int[] index = new int[81];
            int[] signs = { -1, 1 };
            Vector3[] jitter = new Vector3[8];

            for (int i = 0; i < 8; i++)
                jitter[i] = random == null ? Vector3.zero : Vector3.Scale(size, new Vector3((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f) * 0.16f);

            int Key(int axis, int sx, int sy, int sz) => axis * 27 + (sx + 1) * 9 + (sy + 1) * 3 + sz + 1;

            for (int axis = 0; axis < 3; axis++)
            foreach (int sx in signs)
            foreach (int sy in signs)
            foreach (int sz in signs)
            {
                Vector3 s = new Vector3(sx, sy, sz);
                Vector3 p = new Vector3(sx * (half.x - bevel), sy * (half.y - bevel), sz * (half.z - bevel));
                p[axis] = s[axis] * half[axis];
                Vector3 normal = Vector3.zero;
                normal[axis] = s[axis];
                index[Key(axis, sx, sy, sz)] = bucket.Vertices.Count;
                bucket.Vertices.Add(p + jitter[(sx + 1) / 2 + (sy + 1) + (sz + 1) * 2]);
                bucket.Normals.Add(normal);
                bucket.Uvs.Add(Project(p, axis, grain));
            }

            // A copy of a face vertex mapped as the given face.
            int Copy(int vertex, int axis)
            {
                bucket.Vertices.Add(bucket.Vertices[vertex]);
                bucket.Normals.Add(bucket.Normals[vertex]);
                bucket.Uvs.Add(Project(bucket.Vertices[vertex], axis, grain));

                return bucket.Vertices.Count - 1;
            }

            for (int axis = 0; axis < 3; axis++)
            foreach (int s0 in signs)
            {
                int j = (axis + 1) % 3;
                int k = (axis + 2) % 3;
                int[] Corner(int sj, int sk)
                {
                    int[] c = new int[3];
                    c[axis] = s0;
                    c[j] = sj;
                    c[k] = sk;

                    return c;
                }

                int V(int[] c) => index[Key(axis, c[0], c[1], c[2])];
                Quad(bucket, V(Corner(-1, -1)), V(Corner(1, -1)), V(Corner(1, 1)), V(Corner(-1, 1)));

                if (bevel <= 0f)
                    continue;

                // Edge facet to the next face along j, both signs of it, running along k.
                foreach (int sj in signs)
                {
                    int[] a = Corner(sj, -1);
                    int[] b = Corner(sj, 1);
                    Quad(bucket, Copy(V(a), axis), Copy(V(b), axis), Copy(index[Key(j, b[0], b[1], b[2])], axis), Copy(index[Key(j, a[0], a[1], a[2])], axis));
                }
            }

            if (bevel > 0f)
            {
                foreach (int sx in signs)
                foreach (int sy in signs)
                foreach (int sz in signs)
                    Triangle(bucket, Copy(index[Key(0, sx, sy, sz)], 1), Copy(index[Key(1, sx, sy, sz)], 1), Copy(index[Key(2, sx, sy, sz)], 1));
            }
        }

        /// Planar map of a point on the face across axis: V along the grain where the face holds it.
        private static Vector2 Project(Vector3 p, int axis, int grain)
        {
            int u = axis == 0 ? 2 : 0;
            int v = axis == 1 ? 2 : 1;

            if (u == grain)
                (u, v) = (v, u);

            return new Vector2(p[u], p[v]);
        }

        /// Elliptic cylinder of w × d around the origin, h high, smooth sides.
        private static void Cylinder(Bucket bucket, Vector3 size, int sides)
        {
            float hy = size.y * 0.5f;

            for (int cap = -1; cap <= 1; cap += 2)
            {
                int middle = bucket.Vertices.Count;
                bucket.Vertices.Add(new Vector3(0f, cap * hy, 0f));
                bucket.Normals.Add(Vector3.up * cap);
                bucket.Uvs.Add(Vector2.zero);

                for (int i = 0; i < sides; i++)
                {
                    Vector3 rim = Ring(size, i, sides, cap * hy);
                    bucket.Vertices.Add(rim);
                    bucket.Normals.Add(Vector3.up * cap);
                    bucket.Uvs.Add(new Vector2(rim.x, rim.z));
                }

                for (int i = 0; i < sides; i++)
                    Triangle(bucket, middle, middle + 1 + i, middle + 1 + (i + 1) % sides);
            }

            int wall = bucket.Vertices.Count;
            float girth = Mathf.PI * (size.x + size.z) * 0.5f;

            // The seam column twice, so U runs round once without wrapping back.
            for (int i = 0; i <= sides; i++)
            {
                Vector3 bottom = Ring(size, i, sides, -hy);
                Vector3 normal = new Vector3(bottom.x / Mathf.Max(size.x * size.x, 1e-4f), 0f, bottom.z / Mathf.Max(size.z * size.z, 1e-4f)).normalized;
                float u = girth * i / sides;
                bucket.Vertices.Add(bottom);
                bucket.Vertices.Add(Ring(size, i, sides, hy));
                bucket.Normals.Add(normal);
                bucket.Normals.Add(normal);
                bucket.Uvs.Add(new Vector2(u, -hy));
                bucket.Uvs.Add(new Vector2(u, hy));
            }

            for (int i = 0; i < sides; i++)
            {
                int a = wall + i * 2;
                Quad(bucket, a, a + 2, a + 3, a + 1);
            }
        }

        private static Vector3 Ring(Vector3 size, int i, int sides, float y)
        {
            float angle = i * Mathf.PI * 2f / sides;

            return new Vector3(Mathf.Cos(angle) * size.x * 0.5f, y, Mathf.Sin(angle) * size.z * 0.5f);
        }

        private static void Quad(Bucket bucket, int a, int b, int c, int d)
        {
            Triangle(bucket, a, b, c);
            Triangle(bucket, a, c, d);
        }

        /// Wound to face where its vertex normals point.
        private static void Triangle(Bucket bucket, int a, int b, int c)
        {
            Vector3 pa = bucket.Vertices[a];
            Vector3 face = Vector3.Cross(bucket.Vertices[b] - pa, bucket.Vertices[c] - pa);

            if (face.sqrMagnitude < 1e-12f)
                return;

            bool isFlipped = Vector3.Dot(face, bucket.Normals[a] + bucket.Normals[b] + bucket.Normals[c]) < 0f;
            bucket.Triangles.Add(a);
            bucket.Triangles.Add(isFlipped ? c : b);
            bucket.Triangles.Add(isFlipped ? b : c);
        }

        // ---------- materials ----------

        private static Material MakeMaterial(string path, Surface surface, bool isWorld)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find(isWorld && surface.Texture != null ? "Game/WorldStone" : "Universal Render Pipeline/Lit");

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.SetColor("_BaseColor", surface.Tint);
            material.SetFloat("_Smoothness", Mathf.Min(1f, surface.Texture != null ? 0.85f * surface.Smoothness : surface.Smoothness));
            material.SetFloat("_Metallic", surface.Texture != null ? 1f : surface.Metallic);

            if (surface.Texture != null)
            {
                string folder = $"{TexturesFolder}/{surface.Texture}";
                Texture2D color = Prepare($"{folder}/{surface.Texture}_Color.jpg", false, false);
                Texture2D normal = Prepare(File.Exists($"{folder}/{surface.Texture}_Normal.jpg") ? $"{folder}/{surface.Texture}_Normal.jpg" : $"{folder}/{surface.Texture}_Normal.png", true, true);
                Texture2D mask = Mask(folder, surface.Texture);
                material.SetTexture("_BaseMap", color);
                material.SetTextureScale("_BaseMap", isWorld ? Vector2.one / surface.Tile : Vector2.one);
                material.SetTexture("_BumpMap", normal);
                material.SetTexture("_MetallicGlossMap", mask);
                material.SetTexture("_OcclusionMap", mask);
                material.SetFloat("_BumpScale", 1f);
                material.SetFloat("_OcclusionStrength", 1f);
                material.EnableKeyword("_NORMALMAP");
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
                material.EnableKeyword("_OCCLUSIONMAP");
            }

            if (surface.Emission.maxColorComponent > 0f)
            {
                material.SetColor("_EmissionColor", surface.Emission);
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }

            EditorUtility.SetDirty(material);

            return material;
        }

        private static Texture2D Prepare(string path, bool isNormal, bool isLinear)
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            int size = texture != null && Mathf.Max(texture.width, texture.height) <= 1024 ? 1024 : 2048;
            TextureImporterType type = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;

            if (importer.textureType != type || importer.sRGBTexture == isLinear || importer.maxTextureSize != size)
            {
                importer.textureType = type;
                importer.sRGBTexture = !isLinear;
                importer.maxTextureSize = size;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// R metal, G AO, A smoothness, out of Poly Haven's ARM (R AO, G roughness, B metal).
        private static Texture2D Mask(string folder, string source)
        {
            string path = $"{folder}/{source}_Mask.png";

            if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) != null)
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            string armPath = $"{folder}/{source}_ARM.jpg";
            TextureImporter arm = (TextureImporter)AssetImporter.GetAtPath(armPath);
            arm.isReadable = true;
            arm.sRGBTexture = false;
            arm.maxTextureSize = MaskSize;
            arm.textureCompression = TextureImporterCompression.Uncompressed;
            arm.SaveAndReimport();
            Color[] pixels = AssetDatabase.LoadAssetAtPath<Texture2D>(armPath).GetPixels();
            int side = Mathf.RoundToInt(Mathf.Sqrt(pixels.Length));
            Texture2D mask = new Texture2D(side, side, TextureFormat.RGBA32, false, true);
            mask.SetPixels(pixels.Select(p => new Color(p.b, p.r, 0f, 1f - p.g)).ToArray());
            File.WriteAllBytes(path, mask.EncodeToPNG());
            Object.DestroyImmediate(mask);
            arm.isReadable = false;
            arm.SaveAndReimport();

            AssetDatabase.ImportAsset(path);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.sRGBTexture = false;
            importer.maxTextureSize = MaskSize;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
