using System.Collections.Generic;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using UnityEngine;
using UnityEngine.AI;
using Style = Game.Scripts.Editor.Dungeon.VillageArchitectureBuilder.Style;
using HouseSpec = Game.Scripts.Editor.Dungeon.VillageArchitectureBuilder.HouseSpec;

namespace Game.Scripts.Editor.Dungeon
{
    /// Floor 1, the cursed village, after the user's map. Plans every building on the natural ground first (they need level pads),
    /// then levels the pads, lays the terrain and puts up the farm, the village along its streets, the graveyard with the chapel
    /// and the swamp with its huts and boardwalks. Adventurers start at a random spot of the farm; the two stone cellars
    /// (graveyard and swamp) lead down to floor 2 once half the floor's time has run out.
    internal static class VillageMapBuilder
    {
        private const int MaxVillageHouses = 34;
        private const int HouseSpots = 3;

        public sealed class Result
        {
            public readonly List<Transform> PlayerSpawns = new();
            public readonly List<Transform> SpawnHouses = new();
            public readonly List<Transform> MonsterSpawns = new();
            public readonly List<DungeonDirector.MonsterPlacement> Monsters = new();
            public readonly List<ContainerComponent> Containers = new();
            public readonly List<PortalComponent> Cellars = new();
            public readonly List<PortalComponent> EscapePortals = new();
            public readonly List<NavMeshBuildSource> Sources = new();
            public Texture2D Map;
        }

        /// A building planned on the map: where it stands, what it is and the pad it needs.
        private sealed class Plan
        {
            public string Kind;
            public Vector2 Position;
            public float Yaw;
            public HouseSpec Spec;
            public Vector2 HalfSize;
            public VillageGround.Pad Pad;
            public float Radius => HalfSize.magnitude;
        }

        public static Result Build(Transform floor, Transform spawns)
        {
            Result result = new Result();
            VillageGround ground = new VillageGround();
            VillageTerrainBuilder.Clearings clearings = new VillageTerrainBuilder.Clearings();
            List<Plan> plans = new();

            PlanFarm(plans);
            PlanGraveyard(plans);
            PlanVillage(plans, ground);

            foreach (Plan plan in plans)
            {
                if (plan.Kind == "StiltHut")
                    continue;

                plan.Pad = new VillageGround.Pad { Center = plan.Position, HalfSize = plan.HalfSize + Vector2.one * 1.2f, Yaw = plan.Yaw, Margin = 4f };
                ground.AddPad(plan.Pad);
                clearings.Open.Add((plan.Position, plan.Radius + 2f));
            }

            ground.AddPad(new VillageGround.Pad { Center = VillageLayout.Plaza, HalfSize = Vector2.one * VillageLayout.PlazaRadius, IsRound = true, Margin = 6f });
            VillageGround.Pad island = new VillageGround.Pad { Center = VillageLayout.Cellars[1].position, HalfSize = Vector2.one * 11f, IsRound = true, Margin = 5f, Height = VillageLayout.DeckHeight + 0.1f };
            ground.AddPad(island);
            VillageGround.Pad[] cellarPads = new VillageGround.Pad[VillageLayout.Cellars.Length];

            for (int i = 0; i < VillageLayout.Cellars.Length; i++)
            {
                (Vector2 position, float yaw) = VillageLayout.Cellars[i];
                Vector2 middle = position + Rotate(new Vector2(0f, VillageArchitectureBuilder.CellarLength * 0.5f + 0.5f), yaw);
                cellarPads[i] = new VillageGround.Pad { Center = middle, HalfSize = new Vector2(3f, VillageArchitectureBuilder.CellarLength * 0.5f + 4f), Yaw = yaw, Margin = 4f, Height = i == 1 ? island.Height : float.NaN };
                ground.AddPad(cellarPads[i]);
                Vector2 hole = position + Rotate(new Vector2(0f, VillageArchitectureBuilder.CellarLength * 0.5f - 1f), yaw);
                clearings.Holes.Add((hole, new Vector2(VillageArchitectureBuilder.CellarHalfWidth + 0.15f, VillageArchitectureBuilder.CellarLength * 0.5f + 0.15f), yaw));
                clearings.Open.Add((middle, 9f));
            }

            foreach (Vector2 hut in StiltHutPositions())
                clearings.Open.Add((hut, 7f));

            clearings.Open.Add((VillageLayout.Plaza, VillageLayout.PlazaRadius + 4f));
            ground.ApplyPads();

            VillageTerrainBuilder.Build(floor, ground, clearings, result.Sources);
            Transform buildings = BattleEditorUtility.CreateChild("Buildings", floor).transform;
            Dictionary<Plan, VillageArchitectureBuilder.Site> sites = new();

            foreach (Plan plan in plans)
                sites[plan] = Raise(buildings, plan, ground, result);

            Transform dressing = BattleEditorUtility.CreateChild("Dressing", floor).transform;
            BuildBridges(dressing, ground);
            DressFarm(dressing, ground, result);
            DressVillage(dressing, ground, result);
            DressGraveyard(dressing, ground, result, plans);
            DressSwamp(dressing, ground, result);
            DressWilds(dressing, ground);
            Moon(floor);

            for (int i = 0; i < VillageLayout.Cellars.Length; i++)
            {
                (Vector2 position, float yaw) = VillageLayout.Cellars[i];
                result.Cellars.Add(VillageArchitectureBuilder.Cellar(buildings, new Vector3(position.x, cellarPads[i].Height, position.y), yaw));
            }

            Spawns(spawns, ground, result, plans, sites);

            for (int i = 0; i < 3; i++)
            {
                // Parked in the square and invisible; the director opens them at random spots of the map.
                Vector3 parking = new Vector3(VillageLayout.Plaza.x + (i - 1) * 3f, ground.Height(VillageLayout.Plaza), VillageLayout.Plaza.y - 6f);
                result.EscapePortals.Add(DungeonMapBuilder.Place(DungeonMapBuilder.Load("EscapePortal"), floor, parking, 0f, false).GetComponent<PortalComponent>());
            }

            result.Map = VillageMapPainter.Paint(ground, Footprints(plans), "Floor1");

            return result;
        }

        private static void PlanFarm(List<Plan> plans)
        {
            (float angle, string kind, HouseSpec spec)[] farm =
            {
                (60f, "Barn", new HouseSpec { Width = 8, Length = 12 }),
                (12f, "House", new HouseSpec { Width = 6, Length = 8, Storeys = 2, Style = Style.Timber, IsLit = true, Seed = 1 }),
                (-62f, "House", new HouseSpec { Width = 6, Length = 10, Style = Style.Plaster, Seed = 2 }),
                (150f, "House", new HouseSpec { Width = 6, Length = 8, Style = Style.Brick, Seed = 3 }),
                (200f, "House", new HouseSpec { Width = 8, Length = 10, Storeys = 2, Style = Style.Timber, IsLit = true, Seed = 4 }),
                (240f, "House", new HouseSpec { Width = 4, Length = 6, Style = Style.Plaster, HasChimney = false, Seed = 5 }),
                (110f, "House", new HouseSpec { Width = 4, Length = 4, Style = Style.Brick, HasChimney = false, Seed = 6 })
            };

            foreach ((float angle, string kind, HouseSpec spec) in farm)
            {
                Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                Vector2 position = VillageLayout.FarmCenter + direction * (36f + 2.5f + spec.Length * 0.5f);
                plans.Add(new Plan { Kind = kind, Position = position, Yaw = Facing(-direction), Spec = spec, HalfSize = new Vector2(spec.Width, spec.Length) * 0.5f });
            }

            plans.Add(new Plan { Kind = "Windmill", Position = new Vector2(-150f, 230f), Yaw = 160f, HalfSize = Vector2.one * 3.8f });
        }

        private static void PlanGraveyard(List<Plan> plans)
        {
            plans.Add(new Plan { Kind = "Chapel", Position = VillageLayout.Chapel, Yaw = 0f, HalfSize = new Vector2(5.5f, 11f) });

            foreach ((Vector2 position, float yaw) in new[] { (new Vector2(108f, 216f), 90f), (new Vector2(102f, 150f), 90f), (new Vector2(216f, 172f), -90f), (new Vector2(170f, 116f), 0f) })
                plans.Add(new Plan { Kind = "Crypt", Position = position, Yaw = yaw, Spec = new HouseSpec { Width = 4, Length = 6, Style = Style.Brick, IsWindowless = true, HasChimney = false, IsFurnished = false, Seed = (int)position.x }, HalfSize = new Vector2(2f, 3f) });

            plans.Add(new Plan { Kind = "House", Position = new Vector2(98f, 110f), Yaw = 45f, Spec = new HouseSpec { Width = 4, Length = 6, Style = Style.Plaster, IsLit = true, Seed = 31 }, HalfSize = new Vector2(2f, 3f) });

            for (int i = 0; i < VillageLayout.StiltHuts.Length; i++)
            {
                (Vector2 position, float yaw) = VillageLayout.StiltHuts[i];
                plans.Add(new Plan { Kind = "StiltHut", Position = position, Yaw = yaw, HalfSize = new Vector2(2f, 3f) });
            }
        }

        /// Houses line the streets on both sides, their doors to the street, as long as the plot is dry, level enough,
        /// off the roads and clear of the neighbours.
        private static void PlanVillage(List<Plan> plans, VillageGround ground)
        {
            System.Random random = new System.Random(5);
            int count = 0;
            int[] streets = { 0, 1, 2, 3, 4, 5, 6 };

            foreach (int street in streets)
            {
                VillageLayout.Road road = VillageLayout.Roads[street];
                List<Vector2> line = VillageLayout.Smooth(road.Points, 1f);

                foreach (float side in new[] { -1f, 1f })
                {
                    float cursor = 4f + (float)random.NextDouble() * 6f;
                    float travelled = 0f;

                    for (int i = 1; i < line.Count && count < MaxVillageHouses; i++)
                    {
                        travelled += Vector2.Distance(line[i - 1], line[i]);

                        if (travelled < cursor)
                            continue;

                        HouseSpec spec = RandomHouse(random);
                        Vector2 tangent = (line[i] - line[i - 1]).normalized;
                        Vector2 normal = new Vector2(-tangent.y, tangent.x) * side;
                        Vector2 center = line[i] + normal * (road.Width * 0.5f + 2.5f + spec.Length * 0.5f);
                        Plan plan = new Plan { Kind = "House", Position = center, Yaw = Facing(-normal), Spec = spec, HalfSize = new Vector2(spec.Width, spec.Length) * 0.5f };

                        if (!Fits(plan, plans, ground))
                        {
                            cursor = travelled + 3f;
                            continue;
                        }

                        plans.Add(plan);
                        count++;
                        cursor = travelled + spec.Width + 3f + (float)random.NextDouble() * 5f;
                    }
                }
            }
        }

        private static HouseSpec RandomHouse(System.Random random)
        {
            int width = new[] { 4, 6, 6, 8 }[random.Next(4)];
            int length = width == 4 ? new[] { 4, 6, 8 }[random.Next(3)] : Mathf.Max(width, new[] { 6, 8, 8, 10 }[random.Next(4)]);
            int storeys = length >= 8 && width >= 6 && random.NextDouble() < 0.45 ? 2 : 1;

            return new HouseSpec { Width = width, Length = length, Storeys = storeys, Style = (Style)random.Next(3), IsLit = random.NextDouble() < 0.4, Seed = random.Next() };
        }

        private static bool Fits(Plan plan, List<Plan> plans, VillageGround ground)
        {
            Vector2 p = plan.Position;
            float fromCenter = (p - VillageLayout.VillageCenter).magnitude;
            float fromPlaza = (p - VillageLayout.Plaza).magnitude;

            if (fromCenter > VillageLayout.VillageRadius - plan.Radius || fromPlaza < VillageLayout.PlazaRadius + plan.Radius + 4f || ground.WaterDistance(p.x, p.y) < plan.Radius + 9f)
                return false;

            foreach (Plan other in plans)
            {
                if ((other.Position - p).magnitude < plan.Radius + other.Radius + 2.5f)
                    return false;
            }

            float lowest = float.MaxValue;
            float highest = float.MinValue;

            foreach (Vector2 corner in new[] { new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(1f, 1f), new Vector2(-1f, 1f), Vector2.zero })
            {
                Vector2 point = p + Rotate(Vector2.Scale(corner, plan.HalfSize + Vector2.one), plan.Yaw);
                float height = ground.Height(point);
                lowest = Mathf.Min(lowest, height);
                highest = Mathf.Max(highest, height);

                if (ground.RoadDistance(point.x, point.y) < 0.8f || VillageLayout.SwampShare(point.x, point.y) > 0.05f)
                    return false;
            }

            return lowest > VillageLayout.WaterLevel + 0.4f && highest - lowest < 1.6f;
        }

        private static VillageArchitectureBuilder.Site Raise(Transform parent, Plan plan, VillageGround ground, Result result)
        {
            VillageArchitectureBuilder.Site site = new VillageArchitectureBuilder.Site();
            float height = plan.Pad != null ? plan.Pad.Height : ground.Height(plan.Position);
            Vector3 position = new Vector3(plan.Position.x, height, plan.Position.y);

            switch (plan.Kind)
            {
                case "Barn":
                    VillageArchitectureBuilder.Barn(parent, position, plan.Yaw, site);
                    break;
                case "Windmill":
                    VillageArchitectureBuilder.Windmill(parent, position, plan.Yaw);
                    break;
                case "Chapel":
                    VillageArchitectureBuilder.Chapel(parent, position, plan.Yaw, site);
                    break;
                case "Crypt":
                    Transform crypt = VillageArchitectureBuilder.House(parent, "Crypt", position, plan.Yaw, plan.Spec, site);
                    site.Containers.Add(VillageArchitectureBuilder.Container("Coffin", crypt, new Vector3(0f, 0.12f, -0.6f), 0f));
                    break;
                case "StiltHut":
                    float ground0 = ground.Height(plan.Position);
                    position.y = Mathf.Max(VillageLayout.DeckHeight + 0.05f, ground0 + 0.5f);
                    int index = System.Array.FindIndex(VillageLayout.StiltHuts, hut => hut.position == plan.Position);
                    VillageArchitectureBuilder.StiltHut(parent, position, plan.Yaw, index, ground0, site);
                    break;
                default:
                    VillageArchitectureBuilder.House(parent, "House", position, plan.Yaw, plan.Spec, site);
                    break;
            }

            result.Containers.AddRange(site.Containers);

            return site;
        }

        private static void BuildBridges(Transform parent, VillageGround ground)
        {
            Transform root = BattleEditorUtility.CreateChild("Bridges", parent).transform;

            foreach (Vector2 point in VillageLayout.RiverBridges)
                Bridge(root, ground, ground.River, point, VillageLayout.RiverWidth);

            foreach (Vector2 point in VillageLayout.StreamBridges)
                Bridge(root, ground, ground.Stream, point, VillageLayout.StreamWidth);
        }

        private static void Bridge(Transform parent, VillageGround ground, IReadOnlyList<Vector2> line, Vector2 near, float width)
        {
            Vector2 center = VillageLayout.Nearest(line, near, out Vector2 direction);
            Vector2 across = new Vector2(-direction.y, direction.x) * (width * 0.5f + 3.6f + 4.5f);
            Vector2 a = center - across;
            Vector2 b = center + across;
            VillageArchitectureBuilder.Bridge(parent, new Vector3(a.x, ground.Height(a) + 0.05f, a.y), new Vector3(b.x, ground.Height(b) + 0.05f, b.y));
        }

        /// Farmyard: a well in the middle, haystacks, a cart, crates and barrels; fenced fields with scarecrows; the pond.
        private static void DressFarm(Transform parent, VillageGround ground, Result result)
        {
            Transform root = BattleEditorUtility.CreateChild("Farm", parent).transform;
            System.Random random = new System.Random(41);
            Vector2 c = VillageLayout.FarmCenter;
            Put(root, ground, VillageArchitectureBuilder.Piece("Well"), c, 0f);

            foreach (Vector2 offset in new[] { new Vector2(14f, 12f), new Vector2(18f, 7f), new Vector2(-16f, -14f), new Vector2(-10f, 20f) })
                Put(root, ground, VillageArchitectureBuilder.Piece("Haystack"), c + offset, random.Next(360));

            Put(root, ground, DungeonVillageKitBuilder.Load("Prop_Wagon"), c + new Vector2(-14f, 8f), 70f);
            Put(root, ground, DungeonVillageKitBuilder.Load("Prop_Wagon"), c + new Vector2(20f, -16f), 200f);
            Put(root, ground, DungeonKitBuilder.Load("Stall_Cart_Empty"), c + new Vector2(6f, -18f), 30f);
            result.Containers.Add(PutContainer(root, ground, "Crate", c + new Vector2(-6f, 15f), 15f));
            result.Containers.Add(PutContainer(root, ground, "Barrel", c + new Vector2(-4f, 16f), 0f));
            result.Containers.Add(PutContainer(root, ground, "Barrel", c + new Vector2(12f, -10f), 40f));

            for (int i = 0; i < VillageLayout.Fields.Length; i++)
            {
                (Vector2 center, Vector2 size, float yaw) = VillageLayout.Fields[i];
                VillageArchitectureBuilder.Fence(root, center, size, yaw, ground, i % 4);
                Put(root, ground, DungeonKitBuilder.Load("Dummy"), center + Rotate(new Vector2(size.x * 0.2f, 0f), yaw), random.Next(360));
            }

            foreach (float angle in new[] { 30f, 130f, 250f, 330f })
                LampPost(root, ground, c + new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * 33f, angle + 180f);

            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                Put(root, ground, VillageArchitectureBuilder.Piece("HayBale"), VillageLayout.Pond + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (VillageLayout.PondRadius + 14f + random.Next(6)), random.Next(360));
            }
        }

        /// The square: the well, market stalls, braziers and lamps; lamps along the streets; carts and crates by the houses.
        private static void DressVillage(Transform parent, VillageGround ground, Result result)
        {
            Transform root = BattleEditorUtility.CreateChild("Village", parent).transform;
            System.Random random = new System.Random(43);
            Vector2 p = VillageLayout.Plaza;
            Put(root, ground, VillageArchitectureBuilder.Piece("Well"), p, 20f);

            for (int i = 0; i < 4; i++)
            {
                float angle = (i * 90f + 45f) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                LampPost(root, ground, p + direction * (VillageLayout.PlazaRadius - 1.5f), Facing(-direction));
                Put(root, ground, DungeonKitBuilder.Load(i % 2 == 0 ? "Stall_Empty" : "Stall_Cart_Empty"), p + direction * 8f + new Vector2(direction.y, -direction.x) * 3f, Facing(-direction));
            }

            Put(root, ground, DungeonMapBuilder.Load("Brazier"), p + new Vector2(4.5f, 0f), 0f);
            Put(root, ground, DungeonMapBuilder.Load("Brazier"), p + new Vector2(-4.5f, 0f), 0f);
            result.Containers.Add(PutContainer(root, ground, "Crate", p + new Vector2(9f, -6f), 20f));
            result.Containers.Add(PutContainer(root, ground, "Barrel", p + new Vector2(-8f, 7f), 0f));

            foreach (int street in new[] { 0, 1, 2, 3, 4 })
            {
                List<Vector2> line = VillageLayout.Smooth(VillageLayout.Roads[street].Points, 1f);
                float half = VillageLayout.Roads[street].Width * 0.5f + 1f;

                for (int i = 18; i < line.Count; i += 32)
                {
                    if ((line[i] - VillageLayout.VillageCenter).magnitude > VillageLayout.VillageRadius)
                        continue;

                    Vector2 tangent = (line[i] - line[i - 1]).normalized;
                    Vector2 side = new Vector2(-tangent.y, tangent.x) * (i % 64 < 32 ? 1f : -1f);
                    LampPost(root, ground, line[i] + side * half, Facing(-side));

                    if (random.NextDouble() < 0.45)
                        Put(root, ground, DungeonVillageKitBuilder.Load(random.NextDouble() < 0.5 ? "Prop_Wagon" : "Prop_Crate"), line[i] + side * (half + 1.5f) + tangent * 4f, Facing(tangent) + random.Next(-15, 15));
                }
            }
        }

        /// Walls with three gates, rows of graves in plots (a few dug open with a coffin), lanterns at the gates.
        private static void DressGraveyard(Transform parent, VillageGround ground, Result result, List<Plan> plans)
        {
            Transform root = BattleEditorUtility.CreateChild("Graveyard", parent).transform;
            Rect r = VillageLayout.Graveyard;
            System.Random random = new System.Random(47);
            (float from, float to)[] south = { (r.xMin, 117f), (126f, 219f), (228f, r.xMax) };
            (float from, float to)[] west = { (r.yMin, 195f), (204f, r.yMax) };

            foreach ((float from, float to) in south)
                VillageArchitectureBuilder.StoneWall(root, ground, new Vector2(from, r.yMin), new Vector2(to, r.yMin));

            foreach ((float from, float to) in west)
                VillageArchitectureBuilder.StoneWall(root, ground, new Vector2(r.xMin, from), new Vector2(r.xMin, to));

            VillageArchitectureBuilder.StoneWall(root, ground, new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax));
            VillageArchitectureBuilder.StoneWall(root, ground, new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, r.yMax));

            foreach ((Vector2 gate, float yaw) in new[] { (new Vector2(121.5f, r.yMin), 0f), (new Vector2(223.5f, r.yMin), 0f), (new Vector2(r.xMin, 199.5f), 90f) })
            {
                Vector2 across = Rotate(new Vector2(3.4f, 0f), yaw);
                LampPost(root, ground, gate + across + Rotate(new Vector2(0f, -1.2f), yaw), yaw + 180f);
                Put(root, ground, DungeonVillageKitBuilder.Load("Prop_MetalFence_Ornament"), gate - across * 0.62f + Rotate(new Vector2(0f, 0.9f), yaw), yaw + 70f);
            }

            List<Vector2> graves = new();

            for (float z = r.yMin + 5f; z < r.yMax - 4f; z += 3.6f)
            {
                for (float x = r.xMin + 4f; x < r.xMax - 4f; x += 2.8f)
                {
                    Vector2 point = new Vector2(x + Rand(random, 0.3f), z + Rand(random, 0.3f));
                    float plot = DungeonTextureBuilder.Noise(x / 540f + 4f, z / 540f + 2f, 24f, 2);

                    if (plot < 0.45f || random.NextDouble() < 0.25 || ground.RoadDistance(point.x, point.y) < 2f || Near(plans, point, 3f) || Near(VillageLayout.Cellars[0].position, point, 12f))
                        continue;

                    graves.Add(point);
                }
            }

            int coffins = 0;

            foreach (Vector2 grave in graves)
            {
                double roll = random.NextDouble();

                if (roll < 0.012 && coffins < 8)
                {
                    result.Containers.Add(PutContainer(root, ground, "Coffin", grave, random.Next(-6, 6)));
                    coffins++;
                    continue;
                }

                string kind = roll < 0.42 ? "Headstone" : roll < 0.7 ? "GraveCross" : roll < 0.84 ? "GraveSlab" : "GraveMound";
                GameObject marker = Put(root, ground, VillageArchitectureBuilder.Piece(kind), grave, random.Next(-5, 5));

                if (kind is "Headstone" or "GraveCross")
                {
                    marker.transform.localPosition += Vector3.down * 0.05f;
                    marker.transform.localRotation *= Quaternion.Euler(Rand(random, 9f), 0f, Rand(random, 7f));
                }
            }

            foreach (Vector2 candle in new[] { new Vector2(196f, 206f), new Vector2(196f, 222f), new Vector2(150f, 160f), new Vector2(120f, 190f) })
                Put(root, ground, DungeonKitBuilder.Load("CandleStick_Stand"), candle, 0f);
        }

        /// Boardwalks over the mire with lanterns on posts, rotting logs and stumps.
        private static void DressSwamp(Transform parent, VillageGround ground, Result result)
        {
            Transform root = BattleEditorUtility.CreateChild("Swamp", parent).transform;
            System.Random random = new System.Random(53);

            foreach (Vector2[] walk in VillageLayout.Boardwalks)
            {
                VillageArchitectureBuilder.Boardwalk(root, ground, walk);
                Vector2 end = walk[^1];
                Vector2 side = Rotate((walk[^1] - walk[^2]).normalized, 90f) * 1.4f;
                LampPost(root, ground, end + side, Facing(-side), Mathf.Max(VillageLayout.DeckHeight, ground.Height(end + side)));
            }

            (Vector2 cellar, float _) = VillageLayout.Cellars[1];
            result.Containers.Add(PutContainer(root, ground, "Barrel", cellar + new Vector2(6f, 4f), 20f));
            result.Containers.Add(PutContainer(root, ground, "Crate", cellar + new Vector2(-6.5f, 3f), -15f));

            for (int i = 0; i < 40; i++)
            {
                Vector2 point = new Vector2(Rand(random, VillageLayout.Playable - 10f), -100f - (float)random.NextDouble() * 140f);

                if (ground.RoadDistance(point.x, point.y) < 3f)
                    continue;

                Put(root, ground, DungeonVegetationBuilder.Load(random.NextDouble() < 0.5 ? "Log" : "Stump"), point, random.Next(360));
            }
        }

        /// Boulders in the woods and cliffs along the hills at the map's edge.
        private static void DressWilds(Transform parent, VillageGround ground)
        {
            Transform root = BattleEditorUtility.CreateChild("Wilds", parent).transform;
            System.Random random = new System.Random(59);

            for (int i = 0; i < 260; i++)
            {
                Vector2 point = new Vector2(Rand(random, VillageLayout.Half - 8f), Rand(random, VillageLayout.Half - 8f));
                float edge = VillageLayout.EdgeDistance(point.x, point.y);
                bool isCliff = edge < 30f;
                float forest = VillageTerrainBuilder.Forest(point.x, point.y);

                if (!isCliff && forest < 0.5f || ground.RoadDistance(point.x, point.y) < 3f || ground.PadMask(point.x, point.y) > 0f || ground.WaterDistance(point.x, point.y) < 5f)
                    continue;

                string name = isCliff ? $"Cliff{random.Next(2)}" : random.NextDouble() < 0.6 ? $"Boulder{random.Next(4)}" : $"Stone{random.Next(4)}";
                GameObject rock = Put(root, ground, DungeonVegetationBuilder.Load(name), point, random.Next(360));
                float scale = 0.8f + (float)random.NextDouble() * 0.6f;
                // Sunk by the slope under it, so no side of the stone hangs over the ground.
                float slope = Mathf.Tan(ground.Slope(point.x, point.y) * Mathf.Deg2Rad) * (isCliff ? 6f : 1.2f) * scale;
                rock.transform.localPosition += Vector3.down * ((isCliff ? 2.5f : 0.4f) + slope);
                rock.transform.localScale = Vector3.one * scale;
            }
        }

        /// Pale moonlight through the haze: the only sun of the floor, with shadows, so roofs keep the rooms dark.
        private static void Moon(Transform floor)
        {
            Light moon = BattleEditorUtility.CreateChild("[Moon]", floor).AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.color = new Color(0.62f, 0.68f, 0.85f);
            moon.intensity = 0.32f;
            moon.shadows = LightShadows.Soft;
            moon.shadowStrength = 0.85f;
            moon.transform.rotation = Quaternion.Euler(38f, -35f, 0f);
        }

        /// Every house of the farm and the village is a start for one team: a few spots beside the table, facing the door
        /// (the director snaps them to the NavMesh around the furniture).
        /// Monsters take the spots of the other buildings, the graveyard, the chapel and the swamp, some always.
        private static void Spawns(Transform spawns, VillageGround ground, Result result, List<Plan> plans, Dictionary<Plan, VillageArchitectureBuilder.Site> sites)
        {
            System.Random random = new System.Random(61);
            List<Vector3> spots = new();

            foreach (Plan plan in plans)
            {
                if (plan.Kind != "House")
                {
                    spots.AddRange(sites[plan].Spots);

                    continue;
                }

                Vector3 middle = sites[plan].Spots[^1];
                Transform house = BattleEditorUtility.CreateChild("House" + result.SpawnHouses.Count, spawns, middle + Vector3.up * 0.1f).transform;
                house.rotation = Quaternion.Euler(0f, plan.Yaw, 0f);
                result.SpawnHouses.Add(house);

                for (int i = 0; i < HouseSpots; i++)
                {
                    result.PlayerSpawns.Add(BattleEditorUtility.CreateChild("Player" + i, house, new Vector3(-1f, 0f, (i - 1) * 0.8f)).transform);
                }
            }

            for (int i = 0; i < 6; i++)
            {
                Vector2 point = new Vector2(Mathf.Lerp(VillageLayout.Graveyard.xMin + 10f, VillageLayout.Graveyard.xMax - 10f, (float)random.NextDouble()),
                    Mathf.Lerp(VillageLayout.Graveyard.yMin + 10f, VillageLayout.Graveyard.yMax - 20f, (float)random.NextDouble()));
                spots.Add(new Vector3(point.x, ground.Height(point), point.y));
            }

            spots.Add(Ground(ground, VillageLayout.Plaza + new Vector2(6f, 6f)));
            spots.Add(Ground(ground, VillageLayout.Plaza + new Vector2(-7f, -5f)));

            foreach (Vector3 spot in spots)
                result.MonsterSpawns.Add(BattleEditorUtility.CreateChild("Monster", spawns, spot + Vector3.up * 0.1f).transform);

            (Vector2 crypt, float _) = VillageLayout.Cellars[0];
            (Vector2 swamp, float _) = VillageLayout.Cellars[1];
            result.Monsters.Add(Monster(spawns, "SkeletonArcher", Ground(ground, VillageLayout.Chapel + new Vector2(0f, 6f)) + Vector3.up * 0.5f, 180f));
            result.Monsters.Add(Monster(spawns, "SkeletonSwordsman", Ground(ground, crypt + new Vector2(-4f, 0f)), -90f));
            result.Monsters.Add(Monster(spawns, "FlyingHead", Ground(ground, swamp + new Vector2(0f, 6f)) + Vector3.up * 0.3f, 0f));
        }

        private static DungeonDirector.MonsterPlacement Monster(Transform parent, string name, Vector3 position, float yaw)
        {
            Transform point = BattleEditorUtility.CreateChild(name, parent, position).transform;
            point.rotation = Quaternion.Euler(0f, yaw, 0f);

            return new DungeonDirector.MonsterPlacement { Point = point, Prefab = DungeonSceneBuilder.LoadNetworkObject(name) };
        }

        /// A wooden post with a lantern hanging from its arm.
        private static void LampPost(Transform parent, VillageGround ground, Vector2 point, float yaw, float height = float.NaN)
        {
            float y = float.IsNaN(height) ? ground.Height(point) : height;
            Transform post = BattleEditorUtility.CreateChild("Lamp Post", parent, new Vector3(point.x, y, point.y)).transform;
            post.localRotation = Quaternion.Euler(0f, yaw, 0f);
            DungeonStructureBuilder.Block(post, "Post", new Vector3(0f, 1.5f, 0f), new Vector3(0.18f, 3.4f, 0.18f), DungeonPropBuilder.DarkWood);
            DungeonMapBuilder.Place(DungeonKitBuilder.Load("Lantern_Wall"), post, new Vector3(0f, 2.3f, 0.06f), 0f);
        }

        private static GameObject Put(Transform parent, VillageGround ground, GameObject prefab, Vector2 point, float yaw)
        {
            return DungeonMapBuilder.Place(prefab, parent, Ground(ground, point), yaw, prefab.GetComponent<Fusion.NetworkObject>() == null);
        }

        private static ContainerComponent PutContainer(Transform parent, VillageGround ground, string name, Vector2 point, float yaw)
        {
            return DungeonMapBuilder.Place(DungeonMapBuilder.Load(name), parent, Ground(ground, point), yaw, false).GetComponent<ContainerComponent>();
        }

        private static Vector3 Ground(VillageGround ground, Vector2 point)
        {
            return new Vector3(point.x, ground.Height(point), point.y);
        }

        private static List<(Vector2 center, Vector2 halfSize, float yaw)> Footprints(List<Plan> plans)
        {
            List<(Vector2, Vector2, float)> footprints = new();

            foreach (Plan plan in plans)
                footprints.Add((plan.Position, plan.HalfSize, plan.Yaw));

            return footprints;
        }

        private static IEnumerable<Vector2> StiltHutPositions()
        {
            foreach ((Vector2 position, float _) in VillageLayout.StiltHuts)
                yield return position;
        }

        private static bool Near(List<Plan> plans, Vector2 point, float margin)
        {
            foreach (Plan plan in plans)
            {
                if ((plan.Position - point).magnitude < plan.Radius + margin)
                    return true;
            }

            return false;
        }

        private static bool Near(Vector2 a, Vector2 b, float distance) => (a - b).magnitude < distance;

        /// Yaw that turns local +Z towards the direction.
        private static float Facing(Vector2 direction) => Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;

        /// Rotates a local (x, z) offset by a yaw, as Quaternion.Euler(0, yaw, 0) would.
        private static Vector2 Rotate(Vector2 local, float yaw)
        {
            Vector3 turned = Quaternion.Euler(0f, yaw, 0f) * new Vector3(local.x, 0f, local.y);

            return new Vector2(turned.x, turned.z);
        }

        private static float Rand(System.Random random, float range) => ((float)random.NextDouble() * 2f - 1f) * range;
    }
}
