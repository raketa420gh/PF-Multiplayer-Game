using System.Collections.Generic;
using System.Linq;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Tripo props laid over a finished room prefab under its "Dressing" child, rebuilt whole on every run. A spot stands on the floor
    /// or platform under it (wall pieces: that high on the wall, back to it; hanging ones: that far below the ceiling). Models not
    /// generated yet stand in as page props (stone, wood or a pack model). Every floor spot is checked against walls, stairs, markers,
    /// doorway approaches and platform edges; the room's NavMesh is runtime, so solid props must leave the passages free.
    internal static partial class CatacombRoomBuilder
    {
        private const string DressingRoot = "Dressing";
        private const float DoorwayLane = 3.2f;
        private const float DoorwayDepth = 3f;
        /// Curb height over the dais top minus the curb's own height.
        private const float CurbSink = 0.02f - 0.43f;

        private sealed class Spot
        {
            public string Model;
            public float X, Z, Yaw, Height;
            /// Stands on a wall block on purpose (a pillar dressing a square pier).
            public bool IsOnBlock;
        }

        private sealed class Dressing
        {
            public Spot[] Spots;
            public Item[] Stand;
            /// Steady lights of the room's own, out of the players' reach (nothing to put out).
            public (Vector3 point, Color color, float range, float intensity)[] Glows = System.Array.Empty<(Vector3, Color, float, float)>();
            /// Pebbles strewn round each of these points.
            public (string model, int count, float radius, Vector2[] around)[] Scatter;
        }

        private static readonly Dictionary<string, Dressing> s_dressings = new()
        {
            ["WellRest"] = new Dressing
            {
                Spots = new[]
                {
                    // Piers of the hall, unlit torches and lamps, the rest of the light is the room's own.
                    Pier(1.45f, 7.45f), Pier(3.45f, 11.45f), Pier(5.45f, 1.45f),
                    OnWall("WallTorch", 0f, 16.8f, 180f, 1.7f), OnWall("WallTorch", -2f, -16.8f, 0f, 1.7f),
                    OnWall("WallTorch", 16.8f, -0.5f, 270f, 1.7f), OnWall("WallTorch", -16.8f, 4.5f, 90f, 1.7f),
                    At("IronCandlestick", 2.05f, 2.05f), At("IronCandlestick", -2.05f, -2.05f),
                    // The well on its dais, the dais edged with a worn curb.
                    At("StoneWell", 0f, 0f),
                    Curb(-2f, 2.66f, 180f), Curb(0f, 2.66f, 180f), Curb(2f, 2.66f, 180f), Curb(-2f, -2.66f, 0f), Curb(0f, -2.66f, 0f), Curb(2f, -2.66f, 0f),
                    Curb(2.66f, -2f, 270f), Curb(2.66f, 0f, 270f), Curb(2.66f, 2f, 270f), Curb(-2.66f, -2f, 90f), Curb(-2.66f, 0f, 90f), Curb(-2.66f, 2f, 90f),
                    // Burnt-down candles where people sit, sleep and pray over the dead.
                    At("CandleClusterA", -3.6f, 2.3f, 40f), At("CandleClusterA", -2.2f, -15.8f, 110f), At("CandleClusterA", 13.9f, 16.1f, 200f), At("CandleClusterA", -16f, -13.8f, 300f),
                    At("CandleClusterB", 12.5f, -16.1f, 160f), At("CandleClusterB", -14.6f, 13f, 70f),
                    // The vein's crystals on the dais: the well's cold blue light seems to come from them.
                    At("OssariteCluster", 1.6f, -1.3f, 30f), At("OssariteCluster", -1.4f, 1.6f, 200f), At("OssariteBone", 1.2f, 1.9f, 110f),
                    At("HangingChain", -12f, -6f), At("HangingLantern", -12f, -6f, 0f, 2f),
                    At("HangingChain", 10f, 4f), At("HangingLantern", 10f, 4f, 0f, 2f), At("HangingChain", -6f, 10f, 40f),
                    // The camp round the fire and the sleepers' stores along the south wall.
                    At("PotteryShards", 6.4f, -6.4f, 30f), At("GrainSack", 7.6f, -3f, 40f),
                    At("SackPile", 4.8f, -15.9f), At("GrainSack", 3.4f, -16.2f, 20f), At("GrainSack", 2.4f, -15.9f, -30f),
                    At("SupplyCrate", -5.6f, -16.1f, 5f), At("SupplyCrateSmall", -4.3f, -16.2f, 20f), At("BrokenBarrel", 11.4f, -15.6f, 80f),
                    // Ossuary nook behind the south-east wall.
                    At("SkullPile", 14.6f, -15.8f), At("Ribcage", 13f, -15f, 30f), At("BoneScatter", 15.6f, -14.2f, 60f), At("OssariteCluster", 16.2f, -13.9f),
                    // The chained one by the west wall and the dead in the alcove.
                    OnWall("WallShackles", -16.8f, -5.2f, 90f, 0.9f), OnWall("WallShackles", -16.8f, -6.4f, 90f, 0.9f),
                    At("Ribcage", -15.9f, -5.8f, 20f), At("BoneScatter", -15.2f, -4.4f, 120f),
                    At("SkeletonLying", -5.9f, -5.05f), At("OssariteBone", -6.3f, -6.25f, 70f),
                    // North-west stores and bones.
                    At("WoodBarrel", -15.9f, 13.7f), At("GrainSack", -13.8f, 16.2f), At("PlankDebris", -11f, 13.4f, 30f), At("PotteryShards", -14f, 14f, 80f),
                    At("SkullPile", -15.9f, 11.8f, 90f), At("Ribcage", -15f, 10.6f, 150f),
                    // North wall: the vein crops out of the stone.
                    At("OssariteCluster", -2.6f, 16.2f, 20f), At("OssariteBone", -3.6f, 15.8f, 40f), At("RubblePileSmall", 1.8f, 16f), At("FallenBlock", 3.1f, 15.9f, 20f),
                    // Fallen masonry round the piers and walls.
                    At("FallenBlock", 2.6f, 8.8f, 25f), At("ColumnDrum", -0.4f, 8.6f, 70f), At("RubblePileSmall", 2.2f, 9.9f),
                    At("FallenBlock", -9.5f, 7.3f, 60f), At("RubblePileSmall", -11.7f, 3.6f), At("ColumnDrum", -12f, -1.8f, 10f),
                    // East: chains, a fallen drum, rubble.
                    OnWall("WallShackles", 16.8f, 4.5f, 270f, 0.9f), At("BoneScatter", 15.8f, 4.7f, 10f),
                    At("ColumnDrum", 13.2f, -4.6f, 15f), At("RubblePileSmall", 15.6f, -4.2f), At("FallenBlock", 15.8f, -11.6f, 70f),
                    // Upper terraces.
                    At("GrainSack", 16.1f, 13f), At("SupplyCrateSmall", 13.6f, 16.1f, 10f), At("PotteryShards", 11.6f, 14.2f),
                    At("SackPile", -12.6f, -16f), At("RubblePileSmall", -15.8f, -12.2f), At("BoneScatter", -13f, -12.6f, 45f)
                },
                Stand = new[]
                {
                    // Not generated yet: the warden's lectern, log seats round the fire and bedrolls.
                    Prop(-3.8, 1.2, 0.6, 0.7, 1.3, "Конторка", 90), Prop(-4.6, 0.3, 0.4, 0.4, 0.5, "Табурет"),
                    Prop(4.5, -2.6, 0.5, 0.5, 0.45, "Бревно", 0, true), Prop(6.4, -4.5, 0.5, 0.5, 0.45, "Бревно", 0, true), Prop(2.6, -5, 0.5, 0.5, 0.45, "Бревно", 0, true),
                    Prop(-3.2, -13.8, 0.8, 2, 0.15, "Спальник"), Prop(-1.4, -13.8, 0.8, 2, 0.15, "Спальник"), Prop(0.4, -14.2, 0.8, 2, 0.15, "Спальник", 10)
                },
                Glows = new[] { (new Vector3(0f, 3.4f, 0f), new Color(0.62f, 0.8f, 1f), 22f, 28f) },
                Scatter = new[]
                {
                    ("RockA", 2, 1.3f, new[] { new Vector2(1.8f, 16f), new Vector2(2.2f, 9.9f), new Vector2(-11.7f, 3.6f), new Vector2(15.6f, -4.2f), new Vector2(-15.8f, -12.2f), new Vector2(14.6f, -15.8f) }),
                    ("RockB", 1, 1.3f, new[] { new Vector2(2.6f, 8.8f), new Vector2(-9.5f, 7.3f), new Vector2(3.1f, 15.9f), new Vector2(15.8f, -11.6f), new Vector2(-12f, -1.8f), new Vector2(13.2f, -4.6f) })
                }
            }
        };

        [MenuItem("Tools/Game/Dungeon/Catacombs/Dress Rooms")]
        public static void DressRooms()
        {
            foreach (KeyValuePair<string, Dressing> pair in s_dressings)
            {
                string path = $"{Folder}/{pair.Key}.prefab";

                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                    continue;

                GameObject room = PrefabUtility.LoadPrefabContents(path);

                try
                {
                    List<string> problems = Dress(room.transform, pair.Value);
                    PrefabUtility.SaveAsPrefabAsset(room, path);
                    Debug.Log($"Dressed {pair.Key}: {room.transform.Find(DressingRoot).childCount} pieces" + (problems.Count > 0 ? "\n" + string.Join("\n", problems) : string.Empty));
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(room);
                }
            }
        }

        private static List<string> Dress(Transform room, Dressing dressing)
        {
            Transform old = room.Find(DressingRoot);

            if (old != null)
                Object.DestroyImmediate(old.gameObject);

            Transform root = BattleEditorUtility.CreateChild(DressingRoot, room).transform;
            List<Bounds> blocks = Obstacles(room);
            List<Bounds> grounds = room.Cast<Transform>().Where(child => child.name == "Platform").SelectMany(child => child.GetComponentsInChildren<Renderer>()).Select(r => r.bounds).ToList();
            List<Bounds> placed = new();
            List<string> problems = new();
            System.Random random = new(7);

            foreach (Spot spot in dressing.Spots)
            {
                GameObject piece = Put(root, spot, Ground(grounds, spot.X, spot.Z));
                string problem = spot.IsOnBlock || TripoPropBuilder.MountOf(spot.Model) != TripoPropBuilder.Mount.Floor ? null : Problem(piece, blocks, grounds, placed);

                if (problem != null)
                    problems.Add($"{spot.Model} at ({spot.X}, {spot.Z}): {problem}");
            }

            foreach (Item item in dressing.Stand)
            {
                GameObject piece = BuildProp(root, item, Ground(grounds, item.X, item.Z));
                string problem = Problem(piece, blocks, grounds, placed);

                if (problem != null)
                    problems.Add($"{item.Name} at ({item.X}, {item.Z}): {problem}");
            }

            foreach ((Vector3 point, Color color, float range, float intensity) in dressing.Glows)
                DungeonPropBuilder.PointLight(root, point, color, range, intensity, false, true).name = "Glow Light";

            foreach ((string model, int count, float radius, Vector2[] around) in dressing.Scatter)
            {
                foreach (Vector2 center in around)
                {
                    for (int i = 0, tries = 0; i < count && tries < 40; tries++)
                    {
                        float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                        float distance = radius * (0.5f + 0.5f * (float)random.NextDouble());
                        Spot spot = At(model, center.x + Mathf.Cos(angle) * distance, center.y + Mathf.Sin(angle) * distance, (float)random.NextDouble() * 360f);
                        GameObject piece = Put(root, spot, Ground(grounds, spot.X, spot.Z));

                        if (Problem(piece, blocks, grounds, placed) == null)
                            i++;
                        else
                            Object.DestroyImmediate(piece);
                    }
                }
            }

            return problems;
        }

        private static GameObject Put(Transform root, Spot spot, float ground)
        {
            GameObject piece = (GameObject)PrefabUtility.InstantiatePrefab(TripoPropBuilder.Load(spot.Model), root);
            float y = TripoPropBuilder.MountOf(spot.Model) switch
            {
                TripoPropBuilder.Mount.Wall => ground + spot.Height,
                TripoPropBuilder.Mount.Ceiling => Height - spot.Height,
                _ => ground + spot.Height
            };
            piece.transform.SetLocalPositionAndRotation(new Vector3(spot.X, y, spot.Z), Quaternion.Euler(0f, spot.Yaw, 0f));

            // Named as a wall, so the NavMesh puts nothing on top of a solid piece.
            if (piece.GetComponent<Collider>() != null)
                piece.name = $"Wall Dressing {spot.Model}";

            // Static for batching and culling, but out of the room's bake: the props have no lightmap UVs.
            foreach (Transform part in piece.GetComponentsInChildren<Transform>())
                GameObjectUtility.SetStaticEditorFlags(part.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);

            return piece;
        }

        /// What a floor piece runs into, or null: walls, stairs, markers, doorway approaches, another solid piece or a platform edge.
        private static string Problem(GameObject piece, List<Bounds> blocks, List<Bounds> grounds, List<Bounds> placed)
        {
            Bounds bounds = DungeonKitBuilder.Bounds(piece);
            Bounds foot = new Bounds(new Vector3(bounds.center.x, 0f, bounds.center.z), new Vector3(bounds.size.x - 0.04f, 1f, bounds.size.z - 0.04f));
            Vector3 center = foot.center;

            if (blocks.Any(block => Flat(block).Intersects(foot)))
                return "hits a wall, stairs, a marker or a doorway";

            if (piece.GetComponent<Collider>() != null && placed.Any(other => other.Intersects(foot)))
                return "hits another piece";

            float ground = Ground(grounds, center.x, center.z);

            foreach (Vector3 corner in new[] { foot.min, foot.max, new Vector3(foot.min.x, 0f, foot.max.z), new Vector3(foot.max.x, 0f, foot.min.z) })
            {
                if (!Mathf.Approximately(Ground(grounds, corner.x, corner.z), ground))
                    return "hangs over a platform edge";
            }

            if (piece.GetComponent<Collider>() != null)
                placed.Add(foot);

            return null;
        }

        private static Bounds Flat(Bounds bounds)
        {
            return new Bounds(new Vector3(bounds.center.x, 0f, bounds.center.z), new Vector3(bounds.size.x, 1f, bounds.size.z));
        }

        private static List<Bounds> Obstacles(Transform room)
        {
            List<Bounds> blocks = new();

            foreach (Transform child in room)
            {
                if (child.name is "Wall" or "Stairs" or "Wall Column" || child.name.StartsWith("Wall Prop"))
                    blocks.AddRange(child.GetComponentsInChildren<Renderer>().Select(r => r.bounds));
                else if (child.name.StartsWith("Container") || child.name.StartsWith("Light") || child.name.StartsWith("Trap") || child.name == "Start")
                    blocks.Add(new Bounds(child.position, Vector3.one));
            }

            for (int side = 0; side < 4; side++)
            {
                foreach (float offset in new[] { -DoorOffset, DoorOffset })
                {
                    Vector3 along = Quaternion.Euler(0f, side * 90f, 0f) * Vector3.right;
                    Vector3 inward = Quaternion.Euler(0f, side * 90f, 0f) * Vector3.back;
                    Vector3 middle = along * offset - inward * (Half - DoorwayDepth * 0.5f);
                    Vector3 size = new Vector3(Mathf.Abs(along.x) * DoorwayLane + Mathf.Abs(inward.x) * DoorwayDepth, 1f, Mathf.Abs(along.z) * DoorwayLane + Mathf.Abs(inward.z) * DoorwayDepth);
                    blocks.Add(new Bounds(middle, size));
                }
            }

            return blocks;
        }

        private static float Ground(List<Bounds> grounds, float x, float z)
        {
            return grounds.Where(ground => x >= ground.min.x && x <= ground.max.x && z >= ground.min.z && z <= ground.max.z).Select(ground => ground.max.y).DefaultIfEmpty(0f).Max();
        }

        private static Spot At(string model, float x, float z, float yaw = 0f, float height = 0f)
        {
            return new Spot { Model = model, X = x, Z = z, Yaw = yaw, Height = height };
        }

        private static Spot OnWall(string model, float x, float z, float yaw, float height)
        {
            return At(model, x, z, yaw, height);
        }

        /// A dais curb piece lying inside the dais edge, sunk so its top stands a little over the dais top; east and west pieces
        /// a centimetre lower, so the corners they share with the others do not flicker.
        private static Spot Curb(float x, float z, float yaw)
        {
            return new Spot { Model = "WellDaisStep", X = x, Z = z, Yaw = yaw, Height = CurbSink - (yaw % 180f == 0f ? 0f : 0.01f), IsOnBlock = true };
        }

        private static Spot Pier(float x, float z)
        {
            return new Spot { Model = "StonePillar", X = x, Z = z, IsOnBlock = true };
        }
    }
}
