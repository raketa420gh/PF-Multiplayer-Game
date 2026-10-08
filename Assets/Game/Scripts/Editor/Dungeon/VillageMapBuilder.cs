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
    /// and the swamp with its huts and boardwalks. Adventurers start in a random farm house, where the monsters are few and the
    /// loot poor; the village is crowded with them and rich. The two red portals (graveyard and swamp) lead down to floor 2
    /// once half the floor's time has run out.
    internal static class VillageMapBuilder
    {
        private const int MaxVillageHouses = 66;
        private const int HouseSpots = 3;

        private static readonly List<(Vector2 from, Vector2 to)> s_hedges = new();
        private static readonly List<Vector2> s_graveyard = new();

        public sealed class Result
        {
            public readonly List<Transform> PlayerSpawns = new();
            public readonly List<Transform> SpawnHouses = new();
            public readonly List<Transform> MonsterSpawns = new();
            public readonly List<DungeonDirector.MonsterPlacement> Monsters = new();
            public readonly List<ContainerComponent> Containers = new();
            public readonly List<PortalComponent> DescendPortals = new();
            public readonly List<PortalComponent> EscapePortals = new();
            public readonly List<NavMeshBuildSource> Sources = new();
            /// Places in the points of interest where monsters may stand.
            public readonly List<Vector3> MonsterSpots = new();
            public Texture2D Map;
        }

        /// A building planned on the map: where it stands, what it is and the pad it needs.
        internal sealed class Plan
        {
            public string Kind;
            public Vector2 Position;
            public float Yaw;
            public HouseSpec Spec;
            public Vector2 HalfSize;
            public VillageGround.Pad Pad;
            /// A farm house: one team starts in it.
            public bool IsSpawn;
            public float Radius => HalfSize.magnitude;
        }

        public static Result Build(Transform floor, Transform spawns)
        {
            Result result = new Result();
            VillageGround ground = new VillageGround();
            s_hedges.Clear();
            VillageTerrainBuilder.Clearings clearings = new VillageTerrainBuilder.Clearings();
            List<Plan> plans = new();

            PlanFarm(plans);
            PlanGraveyard(plans);
            VillagePointsBuilder.Plan(plans);
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
            VillageGround.Pad island = new VillageGround.Pad { Center = VillageLayout.RedPortals[1].position, HalfSize = Vector2.one * 11f, IsRound = true, Margin = 5f, Height = VillageLayout.DeckHeight + 0.1f };
            ground.AddPad(island);
            VillageGround.Pad[] portalPads = new VillageGround.Pad[VillageLayout.RedPortals.Length];

            for (int i = 0; i < VillageLayout.RedPortals.Length; i++)
            {
                Vector2 position = VillageLayout.RedPortals[i].position;
                portalPads[i] = new VillageGround.Pad { Center = position, HalfSize = Vector2.one * (VillageArchitectureBuilder.ShrineRadius + 1f), IsRound = true, Margin = 4f, Height = i == 1 ? island.Height : float.NaN };
                ground.AddPad(portalPads[i]);
                clearings.Open.Add((position, VillageArchitectureBuilder.ShrineRadius + 5f));
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
            Clutter(BattleEditorUtility.CreateChild("Yards", dressing).transform, ground, plans, VillageLayout.Village, 100f, 90, new System.Random(45));
            Clutter(BattleEditorUtility.CreateChild("Farmyards", dressing).transform, ground, plans, VillageLayout.Farm, 90f, 36, new System.Random(46));
            DressGraveyard(dressing, ground, result, plans);
            DressSwamp(dressing, ground, result);
            VillagePointsBuilder.Dress(dressing, ground, result);
            DressWilds(dressing, ground);
            Moon(floor);

            for (int i = 0; i < VillageLayout.RedPortals.Length; i++)
            {
                (Vector2 position, float yaw) = VillageLayout.RedPortals[i];
                result.DescendPortals.Add(VillageArchitectureBuilder.PortalShrine(buildings, new Vector3(position.x, portalPads[i].Height, position.y), yaw));
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

        /// Farm houses stand round the yard loop, each at its own distance and turned its own way, never on a circle. Every one is
        /// a team's start, with the poorest loot of the floor.
        private static void PlanFarm(List<Plan> plans)
        {
            (float angle, float setback, float turn, string kind, HouseSpec spec)[] farm =
            {
                (64f, 4f, -9f, "Barn", new HouseSpec { Width = 8, Length = 12 }),
                (16f, 1.5f, 12f, "House", new HouseSpec { Width = 6, Length = 8, Storeys = 2, Style = Style.Timber, IsLit = true, Seed = 1 }),
                (-58f, 6f, -14f, "House", new HouseSpec { Width = 6, Length = 10, Style = Style.Plaster, Seed = 2 }),
                (146f, 2f, 18f, "House", new HouseSpec { Width = 6, Length = 8, Style = Style.Brick, Seed = 3 }),
                (226f, 5f, -6f, "House", new HouseSpec { Width = 8, Length = 10, Storeys = 2, Style = Style.Timber, IsLit = true, Seed = 4 }),
                (264f, 0.5f, 22f, "House", new HouseSpec { Width = 4, Length = 6, Style = Style.Plaster, HasChimney = false, Seed = 5 }),
                (106f, 3f, -25f, "House", new HouseSpec { Width = 4, Length = 4, Style = Style.Brick, HasChimney = false, Seed = 6 }),
                (40f, 2f, 15f, "House", new HouseSpec { Width = 6, Length = 6, Style = Style.Plaster, Seed = 7 })
            };

            foreach ((float angle, float setback, float turn, string kind, HouseSpec spec) in farm)
            {
                Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                Vector2 position = VillageLayout.FarmCenter + direction * (38f + setback + spec.Length * 0.5f);
                spec.Loot = VillageArchitectureBuilder.LootTier.Poor;
                plans.Add(new Plan { Kind = kind, Position = position, Yaw = Facing(-direction) + turn, Spec = spec, HalfSize = new Vector2(spec.Width, spec.Length) * 0.5f, IsSpawn = kind == "House" });
            }
        }

        private static void PlanGraveyard(List<Plan> plans)
        {
            plans.Add(new Plan { Kind = "Chapel", Position = VillageLayout.Chapel, Yaw = 0f, HalfSize = new Vector2(5.5f, 11f) });

            foreach ((Vector2 position, float yaw) in new[] { (new Vector2(108f, 216f), 90f), (new Vector2(102f, 150f), 90f), (new Vector2(216f, 172f), -90f), (new Vector2(170f, 116f), 0f) })
                plans.Add(new Plan { Kind = "Crypt", Position = position, Yaw = yaw, Spec = new HouseSpec { Width = 4, Length = 6, Style = Style.Brick, IsWindowless = true, HasChimney = false, IsFurnished = false, Seed = (int)position.x }, HalfSize = new Vector2(2f, 3f) });

            plans.Add(new Plan { Kind = "House", Position = new Vector2(106f, 118f), Yaw = 45f, Spec = new HouseSpec { Width = 4, Length = 6, Style = Style.Plaster, IsLit = true, Seed = 31 }, HalfSize = new Vector2(2f, 3f) });

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
                List<Vector2> line = VillageLayout.RoadLine(road, 1f);

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
                        Vector2 center = line[i] + normal * (road.Width * 0.5f + 2.2f + (float)random.NextDouble() * 2.4f + spec.Length * 0.5f);
                        Plan plan = new Plan { Kind = "House", Position = center, Yaw = Facing(-normal) + Rand(random, 7f), Spec = spec, HalfSize = new Vector2(spec.Width, spec.Length) * 0.5f };

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

            return new HouseSpec { Width = width, Length = length, Storeys = storeys, Style = (Style)random.Next(3), IsLit = random.NextDouble() < 0.4, Loot = VillageArchitectureBuilder.LootTier.Rich, Seed = random.Next() };
        }

        private static bool Fits(Plan plan, List<Plan> plans, VillageGround ground)
        {
            Vector2 p = plan.Position;
            float fromPlaza = (p - VillageLayout.Plaza).magnitude;

            if (VillageLayout.Village.Distance(p.x, p.y) > -plan.Radius - 3f || fromPlaza < VillageLayout.PlazaRadius + plan.Radius + 4f || ground.WaterDistance(p.x, p.y) < plan.Radius + 9f)
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
                    VillageArchitectureBuilder.Windmill(parent, position, plan.Yaw, site);
                    break;
                case "Chapel":
                    VillageArchitectureBuilder.Chapel(parent, position, plan.Yaw, site);
                    break;
                case "Tower":
                    VillagePointsBuilder.Tower(parent, position, plan.Yaw, site);
                    break;
                case "Pad":
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

            foreach ((float angle, float radius) in new[] { (34f, 31f), (122f, 36f), (243f, 29f), (318f, 38f) })
                LampPost(root, ground, c + new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * radius, angle + 180f + random.Next(-20, 20));

            Hedge(root, ground, new Vector2(-172f, 148f), new Vector2(-171f, 168f), random);
            Hedge(root, ground, new Vector2(-94f, 148f), new Vector2(-80f, 166f), random);
            Hedge(root, ground, new Vector2(-196f, 176f), new Vector2(-190f, 198f), random);

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
            // The prize of the square, guarded by the juggernaut.
            result.Containers.Add(PutContainer(root, ground, "GoldenChest", p + new Vector2(-3f, 9f), 180f));
            result.Containers.Add(PutContainer(root, ground, "Crate", p + new Vector2(9f, -6f), 20f));
            result.Containers.Add(PutContainer(root, ground, "Barrel", p + new Vector2(-8f, 7f), 0f));

            foreach (int street in new[] { 0, 1, 2, 3, 4 })
            {
                List<Vector2> line = VillageLayout.RoadLine(VillageLayout.Roads[street], 1f);
                float half = VillageLayout.Roads[street].Width * 0.5f + 1f;
                bool wasInside = VillageLayout.Village.Distance(line[0].x, line[0].y) < -10f;

                for (int i = 1; i < line.Count; i++)
                {
                    bool isInside = VillageLayout.Village.Distance(line[i].x, line[i].y) < -10f;

                    if (isInside != wasInside)
                        VillagePointsBuilder.GateArch(root, ground, line[i], Facing(line[i] - line[i - 1]), VillageLayout.Roads[street].Width + 1.6f);

                    wasInside = isInside;
                }

                for (int i = 18 + random.Next(10); i < line.Count; i += 26 + random.Next(16))
                {
                    if (VillageLayout.Village.Distance(line[i].x, line[i].y) > -4f)
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
            List<Vector2> wall = GraveyardWall(random);
            Vector2 gateStart = Vector2.zero;
            Vector2 runStart = Vector2.zero;
            bool isOpen = true;

            // Pieces of 3 m round the ragged outline; where a road crosses, the wall breaks off into a gate with a lamp.
            for (int i = 0; i < wall.Count; i++)
            {
                Vector2 a = wall[i];
                Vector2 b = wall[(i + 1) % wall.Count];
                int pieces = Mathf.Max(1, Mathf.CeilToInt((b - a).magnitude / 3f));

                for (int k = 0; k <= pieces; k++)
                {
                    Vector2 point = Vector2.Lerp(a, b, k / (float)pieces);
                    bool isGate = ground.RoadDistance(point.x, point.y) < 3.2f || Near(plans, point, 1.5f);

                    if (isGate && !isOpen)
                    {
                        VillageArchitectureBuilder.StoneWall(root, ground, runStart, point);
                        gateStart = point;
                        Vector2 inward = (r.center - point).normalized;
                        LampPost(root, ground, point - inward * 1.2f, Facing(-inward));
                    }
                    else if (!isGate && isOpen)
                    {
                        runStart = point;

                        if (gateStart != Vector2.zero)
                            Put(root, ground, DungeonVillageKitBuilder.Load("Prop_MetalFence_Ornament"), point + (r.center - point).normalized * 1f, Facing(point - gateStart) + 70f);
                    }

                    isOpen = isGate;
                }
            }

            if (!isOpen)
                VillageArchitectureBuilder.StoneWall(root, ground, runStart, wall[0]);

            List<Vector2> graves = new();

            for (float z = r.yMin + 5f; z < r.yMax - 4f; z += 3.6f)
            {
                for (float x = r.xMin + 4f; x < r.xMax - 4f; x += 2.8f)
                {
                    Vector2 point = new Vector2(x + Rand(random, 0.3f), z + Rand(random, 0.3f));
                    float plot = DungeonTextureBuilder.Noise(x / 540f + 4f, z / 540f + 2f, 24f, 2);

                    if (plot < 0.45f || random.NextDouble() < 0.25 || !Inside(s_graveyard, point, 2.5f) || ground.RoadDistance(point.x, point.y) < 2f || Near(plans, point, 3f) || Near(VillageLayout.RedPortals[0].position, point, 12f) || IsHedged(point))
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

                string kind = roll < 0.22 ? "Headstone" : roll < 0.38 ? "SculptedHeadstone" : roll < 0.46 ? "SkullTombstone" : roll < 0.64 ? "GraveCross" : roll < 0.76 ? "GraveSlab" : roll < 0.81 ? "ChestTomb" : "GraveMound";
                GameObject marker = Put(root, ground, VillageArchitectureBuilder.Piece(kind), grave, random.Next(-5, 5));

                if (kind is "Headstone" or "SculptedHeadstone" or "SkullTombstone" or "GraveCross")
                {
                    marker.transform.localPosition += Vector3.down * 0.05f;
                    marker.transform.localRotation *= Quaternion.Euler(Rand(random, 9f), 0f, Rand(random, 7f));
                }
            }

            foreach (Vector2 candle in new[] { new Vector2(196f, 206f), new Vector2(196f, 222f), new Vector2(150f, 160f), new Vector2(120f, 190f) })
                Put(root, ground, DungeonKitBuilder.Load("CandleStick_Stand"), candle, 0f);

            foreach ((Vector2 from, Vector2 to) in new[] { (new Vector2(86f, 168f), new Vector2(146f, 173f)), (new Vector2(161f, 99f), new Vector2(156f, 142f)),
                         (new Vector2(188f, 148f), new Vector2(234f, 153f)), (new Vector2(132f, 236f), new Vector2(127f, 202f)), (new Vector2(194f, 116f), new Vector2(207f, 136f)) })
                Hedge(root, ground, from, to, random);
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

            (Vector2 portal, float _) = VillageLayout.RedPortals[1];
            result.Containers.Add(PutContainer(root, ground, "Barrel", portal + new Vector2(7f, 4f), 20f));
            result.Containers.Add(PutContainer(root, ground, "Crate", portal + new Vector2(-7.5f, 3f), -15f));

            for (int i = 0; i < 40; i++)
            {
                Vector2 point = new Vector2(Rand(random, VillageLayout.Playable - 10f), -100f - (float)random.NextDouble() * 140f);

                if (ground.RoadDistance(point.x, point.y) < 3f)
                    continue;

                Put(root, ground, DungeonVegetationBuilder.Load(random.NextDouble() < 0.5 ? "Log" : "Stump"), point, random.Next(360));
            }
        }

        /// Rock faces on the steep flanks of the ridges and the hills at the map's edge (they also hide the stretched terrain
        /// texture there), boulders in the woods, and inside the open places boulders, logs and stumps a fighter can hide behind.
        private static void DressWilds(Transform parent, VillageGround ground)
        {
            Transform root = BattleEditorUtility.CreateChild("Wilds", parent).transform;
            System.Random random = new System.Random(59);
            int cliffs = 0;
            int cover = 0;

            for (int i = 0; i < 9000 && (cliffs < 420 || cover < 110); i++)
            {
                Vector2 point = new Vector2(Rand(random, VillageLayout.Half - 8f), Rand(random, VillageLayout.Half - 8f));
                float open = ground.Open(point);
                float slope = ground.Slope(point.x, point.y);

                if (ground.RoadDistance(point.x, point.y) < 3f || ground.PadMask(point.x, point.y) > 0f || ground.WaterDistance(point.x, point.y) < 4f
                    || ground.Height(point) < VillageLayout.WaterLevel + 0.1f || VillageLayout.Graveyard.Contains(point))
                    continue;

                bool isCliff = slope > 26f && open > -1f && open < 16f || VillageLayout.EdgeDistance(point.x, point.y) < 30f && random.NextDouble() < 0.1;
                bool isCover = open < -5f && VillageLayout.Village.Distance(point.x, point.y) > 0f && !VillageTerrainBuilder.InField(point.x, point.y, 3f) && random.NextDouble() < 0.06;

                if (isCliff && cliffs < 420)
                {
                    GameObject rock = Put(root, ground, DungeonVegetationBuilder.Load(random.NextDouble() < 0.7 ? $"Cliff{random.Next(2)}" : $"Boulder{2 + random.Next(2)}"), point, random.Next(360));
                    float scale = 0.45f + (float)random.NextDouble() * 0.6f;
                    // Sunk by the slope under it, so no side of the stone hangs over the ground.
                    rock.transform.localPosition += Vector3.down * (1.2f * scale + Mathf.Tan(Mathf.Min(slope, 60f) * Mathf.Deg2Rad) * 4f * scale);
                    rock.transform.localRotation = Quaternion.Euler(Rand(random, 12f), random.Next(360), Rand(random, 12f));
                    rock.transform.localScale = new Vector3(scale, scale * (0.8f + (float)random.NextDouble() * 0.5f), scale);
                    cliffs++;
                }
                else if (isCover && cover < 110)
                {
                    double kind = random.NextDouble();
                    string name = kind < 0.5 ? $"Boulder{random.Next(4)}" : kind < 0.8 ? "Log" : "Stump";
                    GameObject rock = Put(root, ground, DungeonVegetationBuilder.Load(name), point, random.Next(360));
                    rock.transform.localPosition += Vector3.down * 0.25f;
                    cover++;
                }
            }
        }

        /// A row of clipped yew between two points with a gap wherever a path runs through, one collider per stretch.
        private static void Hedge(Transform parent, VillageGround ground, Vector2 from, Vector2 to, System.Random random)
        {
            Transform hedge = BattleEditorUtility.CreateChild("Hedge", parent).transform;
            Vector2 delta = to - from;
            int count = Mathf.CeilToInt(delta.magnitude / 1.5f);
            Vector2 start = from;
            float yaw = Facing(delta);
            s_hedges.Add((from, to));

            for (int i = 0; i <= count + 1; i++)
            {
                Vector2 point = from + delta * Mathf.Min(i / (float)count, 1f);
                bool isGap = i > count || ground.RoadDistance(point.x, point.y) < 2.5f;

                if (!isGap)
                {
                    GameObject bush = Put(hedge, ground, DungeonVegetationBuilder.Load($"Bush{random.Next(DungeonVegetationBuilder.Variants)}"), point + new Vector2(Rand(random, 0.25f), Rand(random, 0.25f)), random.Next(360));
                    bush.transform.localScale = new Vector3(1.3f, 1.5f + (float)random.NextDouble() * 0.5f, 1.3f);
                    continue;
                }

                Vector2 end = from + delta * Mathf.Min((i - 1) / (float)count, 1f);

                if ((end - start).magnitude > 1f)
                {
                    Vector2 middle = (start + end) * 0.5f;
                    GameObject blocker = BattleEditorUtility.CreateChild("Blocker", hedge, Ground(ground, middle) + Vector3.up * 1.2f);
                    blocker.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                    blocker.AddComponent<BoxCollider>().size = new Vector3(1.4f, 2.6f, (end - start).magnitude + 1.2f);
                    blocker.isStatic = true;
                }

                start = from + delta * Mathf.Min((i + 1) / (float)count, 1f);
            }
        }

        /// The graveyard wall follows the yard's own ragged outline six metres inside it: rays from the centre, each searched
        /// for the point where the rim is that far away.
        private static List<Vector2> GraveyardWall(System.Random random)
        {
            const int corners = 34;
            VillageLayout.Zone yard = VillageLayout.Yard;
            s_graveyard.Clear();

            for (int i = 0; i < corners; i++)
            {
                float angle = (i + Rand(random, 0.25f)) / corners * Mathf.PI * 2f;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float inside = 0f;
                float outside = 160f;

                for (int k = 0; k < 24; k++)
                {
                    float middle = (inside + outside) * 0.5f;
                    Vector2 point = yard.Center + direction * middle;

                    if (yard.Distance(point.x, point.y) < -6f)
                        inside = middle;
                    else
                        outside = middle;
                }

                s_graveyard.Add(yard.Center + direction * inside);
            }

            return s_graveyard;
        }

        private static bool Inside(List<Vector2> polygon, Vector2 point, float margin)
        {
            bool isInside = false;

            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[j];

                if (a.y > point.y != b.y > point.y && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                    isInside = !isInside;

                Vector2 segment = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(point - a, segment) / segment.sqrMagnitude);

                if ((a + segment * t - point).magnitude < margin)
                    return false;
            }

            return isInside;
        }

        /// Yards between the houses: fences, woodpiles, haystacks, carts, crates and old trees, so no street or meadow of a
        /// settlement stays a long open sight line.
        private static void Clutter(Transform root, VillageGround ground, List<Plan> plans, VillageLayout.Zone zone, float reach, int count, System.Random random)
        {
            for (int i = 0, placed = 0; i < count * 30 && placed < count; i++)
            {
                Vector2 point = zone.Center + new Vector2(Rand(random, reach), Rand(random, reach));

                if (zone.Distance(point.x, point.y) > -6f || ground.RoadDistance(point.x, point.y) < 3f || ground.PadMask(point.x, point.y) > 0.02f || Near(plans, point, 3f)
                    || (point - VillageLayout.Plaza).magnitude < VillageLayout.PlazaRadius + 5f || VillageTerrainBuilder.InField(point.x, point.y, 2f) || (point - VillageLayout.Pond).magnitude < VillageLayout.PondRadius + 3f)
                    continue;

                float yaw = random.Next(360);
                double kind = random.NextDouble();
                placed++;

                if (kind < 0.26)
                {
                    GameObject tree = Put(root, ground, DungeonVegetationBuilder.Load(random.NextDouble() < 0.6 ? $"Broadleaf{random.Next(DungeonVegetationBuilder.Variants)}" : $"DeadTree{random.Next(DungeonVegetationBuilder.Variants)}"), point, yaw);
                    tree.transform.localScale = Vector3.one * (0.75f + (float)random.NextDouble() * 0.4f);
                }
                else if (kind < 0.48)
                {
                    int pieces = 3 + random.Next(4);

                    for (int k = 0; k < pieces; k++)
                    {
                        if (random.NextDouble() < 0.15)
                            continue;

                        Vector2 piece = point + Rotate(new Vector2(0f, k * VillageArchitectureBuilder.FenceModule), yaw);
                        GameObject fence = Put(root, ground, DungeonVillageKitBuilder.Load(k % 3 == 2 ? "Prop_WoodenFence_Extension1" : "Prop_WoodenFence_Single"), piece, yaw + 90f + Rand(random, 4f));
                        fence.transform.localPosition += Vector3.down * 0.03f;
                    }
                }
                else if (kind < 0.62)
                {
                    VillagePointsBuilder.LogPile(root, ground, point, yaw);
                }
                else if (kind < 0.74)
                {
                    Put(root, ground, VillageArchitectureBuilder.Piece(random.NextDouble() < 0.5 ? "Haystack" : "HayBale"), point, yaw);
                }
                else if (kind < 0.84)
                {
                    Put(root, ground, random.NextDouble() < 0.6 ? DungeonVillageKitBuilder.Load("Prop_Wagon") : DungeonKitBuilder.Load("Stall_Cart_Empty"), point, yaw);
                }
                else
                {
                    Put(root, ground, DungeonVillageKitBuilder.Load("Prop_Crate"), point, yaw);
                    Put(root, ground, DungeonVillageKitBuilder.Load("Prop_Crate"), point + Rotate(new Vector2(1.1f, 0.2f), yaw), yaw + Rand(random, 20f));
                    Put(root, ground, DungeonKitBuilder.Load("Barrel"), point + Rotate(new Vector2(0.3f, 1.2f), yaw), 0f);
                }
            }
        }

        private static bool IsHedged(Vector2 point)
        {
            foreach ((Vector2 from, Vector2 to) in s_hedges)
            {
                Vector2 segment = to - from;
                float t = Mathf.Clamp01(Vector2.Dot(point - from, segment) / segment.sqrMagnitude);

                if ((from + segment * t - point).magnitude < 2f)
                    return true;
            }

            return false;
        }

        /// Pale moonlight through the haze: the only sun of the floor, with shadows, so roofs keep the rooms dark.
        private static void Moon(Transform floor)
        {
            Light moon = BattleEditorUtility.CreateChild("[Moon]", floor).AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.color = new Color(0.58f, 0.7f, 0.88f);
            moon.intensity = 0.5f;
            moon.shadows = LightShadows.Soft;
            moon.shadowStrength = 0.85f;
            moon.transform.rotation = Quaternion.Euler(38f, -35f, 0f);
        }

        /// Every farm house is a start for one team: a few spots beside the table, facing the door (the director snaps them to the
        /// NavMesh around the furniture). The farm has no random monsters, only two weak ones in the barn and the far field.
        /// Monsters take the spots of every other building (in the village every other house), the graveyard, the chapel,
        /// the windmill and the swamp; a guard of the square and of each red portal always stands.
        private static void Spawns(Transform spawns, VillageGround ground, Result result, List<Plan> plans, Dictionary<Plan, VillageArchitectureBuilder.Site> sites)
        {
            System.Random random = new System.Random(61);
            List<Vector3> spots = new();

            int village = 0;

            foreach (Plan plan in plans)
            {
                if (!plan.IsSpawn)
                {
                    if (VillageLayout.Farm.Distance(plan.Position.x, plan.Position.y) < 0f)
                        continue;

                    if (plan.Kind != "House" || VillageLayout.Village.Distance(plan.Position.x, plan.Position.y) > 0f)
                        spots.AddRange(sites[plan].Spots);
                    else if (village++ % 2 == 0)
                        spots.Add(sites[plan].Spots[^1]);

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

            for (int i = 0, placed = 0; i < 200 && placed < 6; i++)
            {
                Vector2 point = new Vector2(Mathf.Lerp(VillageLayout.Graveyard.xMin + 10f, VillageLayout.Graveyard.xMax - 10f, (float)random.NextDouble()),
                    Mathf.Lerp(VillageLayout.Graveyard.yMin + 10f, VillageLayout.Graveyard.yMax - 20f, (float)random.NextDouble()));

                if (!Inside(s_graveyard, point, 4f))
                    continue;

                spots.Add(new Vector3(point.x, ground.Height(point), point.y));
                placed++;
            }

            spots.AddRange(result.MonsterSpots);
            spots.Add(Ground(ground, VillageLayout.Plaza + new Vector2(6f, 6f)));
            spots.Add(Ground(ground, VillageLayout.Plaza + new Vector2(-7f, -5f)));

            foreach (Vector3 spot in spots)
                result.MonsterSpawns.Add(BattleEditorUtility.CreateChild("Monster", spawns, spot + Vector3.up * 0.1f).transform);

            (Vector2 crypt, float _) = VillageLayout.RedPortals[0];
            (Vector2 swamp, float _) = VillageLayout.RedPortals[1];
            Vector2 p = VillageLayout.Plaza;
            result.Monsters.Add(Monster(spawns, "SkeletonArcher", Ground(ground, VillageLayout.Chapel + new Vector2(0f, 6f)) + Vector3.up * 0.5f, 180f));
            result.Monsters.Add(Monster(spawns, "SkeletonWarrior", Ground(ground, crypt + new Vector2(-6f, 0f)), -90f));
            result.Monsters.Add(Monster(spawns, "FlyingHead", Ground(ground, swamp + new Vector2(0f, 7f)) + Vector3.up * 0.3f, 0f));
            result.Monsters.Add(Monster(spawns, "Juggernaut", Ground(ground, p + new Vector2(-3f, 5f)), 180f));
            result.Monsters.Add(Monster(spawns, "SkeletonArcher", Ground(ground, p + new Vector2(8f, 9f)), 200f));
            result.Monsters.Add(Monster(spawns, "SkeletonArcher", Ground(ground, p + new Vector2(-9f, -7f)), 40f));
            result.Monsters.Add(Monster(spawns, "SkeletonSwordsman", Ground(ground, p + new Vector2(7f, -8f)), -30f));
            result.Monsters.Add(Monster(spawns, "SkeletonSwordsman", Ground(ground, p + new Vector2(-10f, 4f)), 110f));

            // The farm: a skeleton in the barn, a flying head over the far field.
            Plan barn = plans.Find(plan => plan.Kind == "Barn");
            result.Monsters.Add(Monster(spawns, "SkeletonSwordsman", sites[barn].Spots[^1] + Vector3.up * 0.1f, barn.Yaw));
            result.Monsters.Add(Monster(spawns, "FlyingHead", Ground(ground, VillageLayout.Fields[0].center) + Vector3.up * 0.3f, 90f));
        }

        private static DungeonDirector.MonsterPlacement Monster(Transform parent, string name, Vector3 position, float yaw)
        {
            Transform point = BattleEditorUtility.CreateChild(name, parent, position).transform;
            point.rotation = Quaternion.Euler(0f, yaw, 0f);

            return new DungeonDirector.MonsterPlacement { Point = point, Prefab = DungeonSceneBuilder.LoadNetworkObject(name) };
        }

        /// A wooden post with a lantern hanging from its arm.
        internal static void LampPost(Transform parent, VillageGround ground, Vector2 point, float yaw, float height = float.NaN)
        {
            float y = float.IsNaN(height) ? ground.Height(point) : height;
            Transform post = BattleEditorUtility.CreateChild("Lamp Post", parent, new Vector3(point.x, y, point.y)).transform;
            post.localRotation = Quaternion.Euler(0f, yaw, 0f);
            DungeonStructureBuilder.Block(post, "Post", new Vector3(0f, 1.5f, 0f), new Vector3(0.18f, 3.4f, 0.18f), DungeonPropBuilder.DarkWood);
            DungeonMapBuilder.Place(DungeonKitBuilder.Load("Lantern_Wall"), post, new Vector3(0f, 2.3f, 0.06f), 0f);
        }

        internal static GameObject Put(Transform parent, VillageGround ground, GameObject prefab, Vector2 point, float yaw)
        {
            return DungeonMapBuilder.Place(prefab, parent, Ground(ground, point), yaw, prefab.GetComponent<Fusion.NetworkObject>() == null);
        }

        internal static ContainerComponent PutContainer(Transform parent, VillageGround ground, string name, Vector2 point, float yaw)
        {
            return DungeonMapBuilder.Place(DungeonMapBuilder.Load(name), parent, Ground(ground, point), yaw, false).GetComponent<ContainerComponent>();
        }

        internal static Vector3 Ground(VillageGround ground, Vector2 point)
        {
            return new Vector3(point.x, ground.Height(point), point.y);
        }

        private static List<(Vector2 center, Vector2 halfSize, float yaw)> Footprints(List<Plan> plans)
        {
            List<(Vector2, Vector2, float)> footprints = new();

            foreach (Plan plan in plans)
            {
                if (plan.Kind != "Pad")
                    footprints.Add((plan.Position, plan.HalfSize, plan.Yaw));
            }

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
        internal static float Facing(Vector2 direction) => Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;

        /// Rotates a local (x, z) offset by a yaw, as Quaternion.Euler(0, yaw, 0) would.
        internal static Vector2 Rotate(Vector2 local, float yaw)
        {
            Vector3 turned = Quaternion.Euler(0f, yaw, 0f) * new Vector3(local.x, 0f, local.y);

            return new Vector2(turned.x, turned.z);
        }

        private static float Rand(System.Random random, float range) => ((float)random.NextDouble() * 2f - 1f) * range;
    }
}
