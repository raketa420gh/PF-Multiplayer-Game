using System.Collections.Generic;
using System.IO;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEngine;
using Plan = Game.Scripts.Editor.Dungeon.VillageMapBuilder.Plan;
using HouseSpec = Game.Scripts.Editor.Dungeon.VillageArchitectureBuilder.HouseSpec;
using Style = Game.Scripts.Editor.Dungeon.VillageArchitectureBuilder.Style;

namespace Game.Scripts.Editor.Dungeon
{
    /// The points of interest between the settlements, each a small arena closed by the thicket with two or more ways out:
    /// the woodcutter's camp, the standing stones, the hunter's lodge, the gallows hill, the old mill, the ruined watchtower,
    /// the willow ford, and the islands of the swamp (palisades, sunken boats, skull piles, the fisher's jetty). Every one
    /// holds loot, its own light and spots for monsters. Mist banks drift over the bog, the ford and the river ravines.
    internal static class VillagePointsBuilder
    {
        private const string MistTexturePath = DungeonTextureBuilder.Folder + "/MistPuff.png";

        private static Material Stone => VillageArchitectureBuilder.Stone;
        private static Material DarkWood => DungeonPropBuilder.DarkWood;
        private static Material Planks => DungeonPropBuilder.WoodPlanks;

        /// Buildings and levelled ground the points need before the ground is levelled.
        public static void Plan(List<Plan> plans)
        {
            House(plans, "Woodcutter's Camp", new Vector2(-7f, 6f), new HouseSpec { Width = 4, Length = 6, Style = Style.Timber, IsLit = true, Seed = 51 });
            House(plans, "Hunter's Lodge", new Vector2(6f, 5f), new HouseSpec { Width = 6, Length = 8, Style = Style.Timber, IsLit = true, Seed = 52 });
            // The windmill fronts the north road; its yard is levelled wide enough for the ramp up to the gallery.
            Vector2 mill = Center("Abandoned Windmill") + new Vector2(0f, 6f);
            plans.Add(new Plan { Kind = "Windmill", Position = mill, Yaw = 180f, HalfSize = Vector2.one * VillageArchitectureBuilder.MillReach });
            plans.Add(new Plan { Kind = "Pad", Position = mill, Yaw = 180f, HalfSize = new Vector2(14f, 8f) });
            plans.Add(new Plan { Kind = "Tower", Position = Center("Watchtower") + new Vector2(2f, 3f), Yaw = 200f, HalfSize = Vector2.one * 4.8f });
            plans.Add(new Plan { Kind = "Pad", Position = Center("Standing Stones"), Yaw = 12f, HalfSize = Vector2.one * 10f });
            plans.Add(new Plan { Kind = "Pad", Position = Center("Gallows Hill") + new Vector2(3f, 4f), Yaw = -20f, HalfSize = new Vector2(5f, 3f) });
        }

        public static void Dress(Transform parent, VillageGround ground, VillageMapBuilder.Result result)
        {
            Transform root = BattleEditorUtility.CreateChild("Points", parent).transform;
            System.Random random = new System.Random(71);
            WoodcutterCamp(root, ground, result, random);
            StandingStones(root, ground, result, random);
            HunterLodge(root, ground, result, random);
            GallowsHill(root, ground, result, random);
            AbandonedWindmill(root, ground, result, random);
            Watchtower(root, ground, result, random);
            WillowFord(root, ground, result, random);
            SwampIslands(root, ground, result, random);
            Mists(root, ground);
        }

        /// Ruined round tower of rough stone: broken walls of uneven height, a doorway and a breach, a flagstone floor.
        public static void Tower(Transform parent, Vector3 position, float yaw, VillageArchitectureBuilder.Site site)
        {
            const float radius = 4.2f;
            const float thickness = 0.8f;
            const int segments = 16;
            System.Random random = new System.Random(81);
            Transform root = BattleEditorUtility.CreateChild("Watchtower", parent, position).transform;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            float length = 2f * Mathf.PI * radius / segments + 0.2f;
            DungeonPropBuilder.MeshObject("Floor", root, new DungeonMeshBuilder(0.5f).Cylinder(new Vector3(0f, -0.15f, 0f), radius + 0.4f, 0.3f, segments).Save("TowerFloor"), DungeonPropBuilder.Flagstone);

            for (int i = 0; i < segments; i++)
            {
                float angle = i * 360f / segments;
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                bool isBreach = i == 9;
                float height = random.NextDouble() < 0.4 ? 2.2f + (float)random.NextDouble() * 3f : 6.5f + (float)random.NextDouble() * 2f;

                if (isBreach)
                    continue;

                if (i == 0)
                {
                    DungeonStructureBuilder.Block(root, "Lintel", direction * radius + Vector3.up * (2.7f + (height - 2.7f) * 0.5f), new Vector3(thickness, Mathf.Max(height, 4f) - 2.7f, length), Stone, angle + 90f);

                    continue;
                }

                DungeonStructureBuilder.Block(root, "Wall", direction * radius + Vector3.up * height * 0.5f, new Vector3(thickness, height, length), Stone, angle + 90f);
            }

            VillageArchitectureBuilder.Tilted(root, "Fallen Beam", new Vector3(-2.4f, 0.2f, -1f), new Vector3(1.8f, 2.4f, 1.6f), 0.35f, 0.35f, DarkWood);
            site.Containers.Add(VillageArchitectureBuilder.Container("LargeOakChest", root, new Vector3(0.4f, 0f, -2.6f), 10f));
            VillageArchitectureBuilder.Place(DungeonMapBuilder.Load("Brazier"), root, new Vector3(-1.4f, 0f, 1.2f), 0f);
            VillageArchitectureBuilder.Place(DungeonMedievalBuilder.Load("Weapon Rack"), root, new Vector3(2.9f, 0f, -0.6f), -90f);

            for (int i = 0; i < 6; i++)
                VillageArchitectureBuilder.Place(DungeonVegetationBuilder.Load($"Stone{random.Next(4)}"), root, new Vector3(Rand(random, 3f), -0.1f, Rand(random, 3f)), random.Next(360));

            site.Spots.Add(root.TransformPoint(new Vector3(0f, 0.1f, 0.5f)));
        }

        /// Two posts and a beam over the road with a lantern hanging from one post, as at the gates of a lane.
        public static void GateArch(Transform parent, VillageGround ground, Vector2 point, float yaw, float width)
        {
            Transform arch = BattleEditorUtility.CreateChild("Gate Arch", parent, Ground(ground, point)).transform;
            arch.localRotation = Quaternion.Euler(0f, yaw, 0f);
            float half = width * 0.5f;

            foreach (float x in new[] { -half, half })
            {
                float y = ground.Height(point + Rotate(new Vector2(x, 0f), yaw)) - arch.localPosition.y;
                DungeonStructureBuilder.Block(arch, "Post", new Vector3(x, y + 1.8f, 0f), new Vector3(0.32f, 4.4f, 0.32f), DarkWood);
                VillageArchitectureBuilder.Tilted(arch, "Brace", new Vector3(x, y + 3.1f, 0f), new Vector3(x * 0.7f, y + 3.9f, 0f), 0.16f, 0.16f, DarkWood, false);
            }

            DungeonStructureBuilder.Block(arch, "Beam", new Vector3(0f, 4.05f, 0f), new Vector3(width + 1.2f, 0.36f, 0.36f), DarkWood, 0f, false);
            DungeonMapBuilder.Place(DungeonKitBuilder.Load("Lantern_Wall"), arch, new Vector3(-half + 0.2f, 2.6f, 0.18f), 0f);
        }

        /// A run of sharpened stakes leaning outward, lashed to a rail, with one collider along it.
        public static void Stakes(Transform parent, VillageGround ground, Vector2 from, Vector2 to, System.Random random)
        {
            Transform run = BattleEditorUtility.CreateChild("Palisade", parent).transform;
            Vector2 delta = to - from;
            float yaw = Facing(delta);
            int count = Mathf.CeilToInt(delta.magnitude / 0.42f);
            Mesh stake = new DungeonMeshBuilder(1f).Cylinder(new Vector3(0f, 0.95f, 0f), 0.12f, 1.9f, 6, 0.11f).Cylinder(new Vector3(0f, 2.15f, 0f), 0.11f, 0.5f, 6, 0.01f).Save("VillageStake");

            for (int i = 0; i <= count; i++)
            {
                if (random.NextDouble() < 0.08)
                    continue;

                Vector2 point = from + delta * (i / (float)count);
                GameObject go = DungeonPropBuilder.MeshObject("Stake", run, stake, DarkWood, Ground(ground, point) + Vector3.down * 0.3f, new Vector3(Rand(random, 6f), yaw, -18f + Rand(random, 8f)), false);
                go.transform.localScale = Vector3.one * (0.85f + (float)random.NextDouble() * 0.35f);
            }

            Vector2 middle = (from + to) * 0.5f;
            DungeonStructureBuilder.Block(run, "Rail", Ground(ground, middle) + Vector3.up * 0.9f, new Vector3(0.12f, 0.12f, delta.magnitude), DarkWood, yaw, false);
            GameObject blocker = BattleEditorUtility.CreateChild("Blocker", run, Ground(ground, middle) + Vector3.up * 1f);
            blocker.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            blocker.AddComponent<BoxCollider>().size = new Vector3(0.5f, 2.4f, delta.magnitude);
            blocker.isStatic = true;
        }

        /// Logs stacked three, two and one along the yaw.
        public static void LogPile(Transform parent, VillageGround ground, Vector2 point, float yaw)
        {
            Transform pile = BattleEditorUtility.CreateChild("Log Pile", parent, Ground(ground, point)).transform;
            pile.localRotation = Quaternion.Euler(0f, yaw, 0f);
            (float x, float y)[] logs = { (-0.62f, 0f), (0f, 0f), (0.62f, 0f), (-0.31f, 0.52f), (0.31f, 0.52f), (0f, 1.04f) };

            foreach ((float x, float y) in logs)
                DungeonMapBuilder.Place(DungeonVegetationBuilder.Load("Log"), pile, new Vector3(x, y - 0.05f, 0f), 0f);
        }

        private static void WoodcutterCamp(Transform root, VillageGround ground, VillageMapBuilder.Result result, System.Random random)
        {
            Vector2 c = Center("Woodcutter's Camp");
            Transform camp = BattleEditorUtility.CreateChild("Woodcutter's Camp", root).transform;
            Put(camp, ground, DungeonMapBuilder.Load("Campfire"), c + new Vector2(3f, -2f), 0f);
            LogPile(camp, ground, c + new Vector2(8f, 3f), 75f);
            LogPile(camp, ground, c + new Vector2(-4f, -9f), 10f);
            Put(camp, ground, DungeonKitBuilder.Load("Workbench"), c + new Vector2(9f, -5f), -100f);
            Put(camp, ground, DungeonKitBuilder.Load("Anvil_Log"), c + new Vector2(6f, -8f), 30f);

            for (int i = 0; i < 9; i++)
            {
                Vector2 point = c + Ring(random, 11f, 16f);

                if (ground.RoadDistance(point.x, point.y) > 2f)
                    Put(camp, ground, DungeonVegetationBuilder.Load("Stump"), point, random.Next(360));
            }

            GameObject axe = Put(camp, ground, DungeonKitBuilder.Load("Axe_Bronze"), c + new Vector2(-1f, 9f), 40f);
            axe.transform.localPosition += Vector3.up * 0.55f;
            axe.transform.localRotation *= Quaternion.Euler(0f, 0f, 70f);
            Put(camp, ground, DungeonVegetationBuilder.Load("Stump"), c + new Vector2(-1f, 9f), 0f);
            result.Containers.Add(VillageMapBuilder.PutContainer(camp, ground, "Crate", c + new Vector2(10f, 0f), 20f));
            result.Containers.Add(VillageMapBuilder.PutContainer(camp, ground, "Barrel", c + new Vector2(-9f, -4f), 0f));
            VillageMapBuilder.LampPost(camp, ground, c + new Vector2(-3f, -12f), 160f);
            Spots(result, ground, c, new Vector2(4f, 4f), new Vector2(-6f, -6f));
        }

        /// A broken ring of standing stones, two of them fallen, round a shrine and an offering chest.
        private static void StandingStones(Transform root, VillageGround ground, VillageMapBuilder.Result result, System.Random random)
        {
            Vector2 c = Center("Standing Stones");
            Transform ring = BattleEditorUtility.CreateChild("Standing Stones", root).transform;

            for (int i = 0; i < 9; i++)
            {
                float angle = (i * 40f + Rand(random, 12f)) * Mathf.Deg2Rad;
                Vector2 point = c + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (8.5f + Rand(random, 1.4f));
                float yaw = Facing(c - point) + 90f + Rand(random, 15f);

                if (ground.RoadDistance(point.x, point.y) < 1.5f)
                    continue;

                GameObject stone = Put(ring, ground, DungeonVegetationBuilder.Load($"Menhir{random.Next(4)}"), point, yaw);

                if (i == 2 || i == 6)
                {
                    stone.transform.localRotation = Quaternion.Euler(82f, yaw, 0f);
                    stone.transform.localPosition += Vector3.up * 0.3f;
                }
                else
                {
                    stone.transform.localRotation = Quaternion.Euler(Rand(random, 7f), yaw, Rand(random, 7f));
                    stone.transform.localPosition += Vector3.down * 0.3f;
                }
            }

            Put(ring, ground, DungeonMapBuilder.Load("ShrineProtection"), c, 0f);
            result.Containers.Add(VillageMapBuilder.PutContainer(ring, ground, "LargeOakChest", c + new Vector2(0f, -3.2f), 0f));
            Put(ring, ground, DungeonMapBuilder.Load("CandleCluster"), c + new Vector2(1.8f, -2.6f), 0f);
            Put(ring, ground, DungeonMapBuilder.Load("CandleCluster"), c + new Vector2(-1.9f, -2.4f), 60f);
            Put(ring, ground, DungeonMapBuilder.Load("SkullPile"), c + new Vector2(3.5f, 2.5f), random.Next(360));
            Spots(result, ground, c, new Vector2(5f, 5f), new Vector2(-5f, 4f), new Vector2(0f, -6f));
        }

        private static void HunterLodge(Transform root, VillageGround ground, VillageMapBuilder.Result result, System.Random random)
        {
            Vector2 c = Center("Hunter's Lodge");
            Transform lodge = BattleEditorUtility.CreateChild("Hunter's Lodge", root).transform;
            Put(lodge, ground, DungeonMapBuilder.Load("Campfire"), c + new Vector2(-3f, -3f), 0f);
            DryingRack(lodge, ground, c + new Vector2(-8f, 4f), 30f, random);
            DryingRack(lodge, ground, c + new Vector2(-9f, -5f), 100f, random);
            Put(lodge, ground, DungeonKitBuilder.Load("WeaponStand"), c + new Vector2(1f, -9f), 180f);
            Put(lodge, ground, DungeonKitBuilder.Load("Cage_Small"), c + new Vector2(8f, -6f), 20f);
            Put(lodge, ground, DungeonKitBuilder.Load("Peg_Rack"), c + new Vector2(-1f, -10f), 200f);
            result.Containers.Add(VillageMapBuilder.PutContainer(lodge, ground, "Crate", c + new Vector2(3f, -8f), 10f));
            result.Containers.Add(VillageMapBuilder.PutContainer(lodge, ground, "Barrel", c + new Vector2(10f, 0f), 0f));
            Put(lodge, ground, DungeonMapBuilder.Load("FallenRanger"), c + new Vector2(-12f, 1f), 70f);
            VillageMapBuilder.LampPost(lodge, ground, c + new Vector2(-2f, 10f), 10f);
            Spots(result, ground, c, new Vector2(-5f, 6f), new Vector2(5f, -5f));
        }

        /// The gallows on a trodden mound, stocks and a hanging cage beside it, braziers burning at the foot.
        private static void GallowsHill(Transform root, VillageGround ground, VillageMapBuilder.Result result, System.Random random)
        {
            Vector2 c = Center("Gallows Hill");
            Vector2 site = c + new Vector2(3f, 4f);
            Transform hill = BattleEditorUtility.CreateChild("Gallows Hill", root).transform;
            Transform gallows = BattleEditorUtility.CreateChild("Gallows", hill, Ground(ground, site)).transform;
            gallows.localRotation = Quaternion.Euler(0f, -20f, 0f);

            foreach (float x in new[] { -1.8f, 1.8f })
            {
                DungeonStructureBuilder.Block(gallows, "Post", new Vector3(x, 2.1f, 0f), new Vector3(0.3f, 4.4f, 0.3f), DarkWood);
                VillageArchitectureBuilder.Tilted(gallows, "Brace", new Vector3(x, 3.2f, 0f), new Vector3(x * 0.55f, 4.05f, 0f), 0.16f, 0.16f, DarkWood, false);
            }

            DungeonStructureBuilder.Block(gallows, "Beam", new Vector3(0f, 4.15f, 0f), new Vector3(4.4f, 0.32f, 0.32f), DarkWood);

            foreach (float x in new[] { -0.9f, 0.7f })
            {
                DungeonStructureBuilder.Block(gallows, "Rope", new Vector3(x, 3.45f, 0f), new Vector3(0.04f, 1.2f, 0.04f), DarkWood, 0f, false);
                DungeonStructureBuilder.Block(gallows, "Noose", new Vector3(x, 2.75f, 0f), new Vector3(0.22f, 0.26f, 0.05f), DarkWood, 0f, false);
            }

            Put(hill, ground, DungeonMedievalBuilder.Load("Stocks"), site + new Vector2(-6f, -2f), 70f);
            Put(hill, ground, DungeonMedievalBuilder.Load("X Stocks"), site + new Vector2(6f, -3f), -60f);
            Put(hill, ground, DungeonMedievalBuilder.Load("Cage"), site + new Vector2(-4f, 5f), 30f);
            Put(hill, ground, DungeonMapBuilder.Load("Brazier"), site + new Vector2(-3.5f, -3f), 0f);
            Put(hill, ground, DungeonMapBuilder.Load("Brazier"), site + new Vector2(3.5f, -3.6f), 0f);
            Put(hill, ground, DungeonMapBuilder.Load("SkullPile"), site + new Vector2(1.5f, 2.2f), random.Next(360));
            result.Containers.Add(VillageMapBuilder.PutContainer(hill, ground, "Coffin", site + new Vector2(-1f, -4.5f), 70f));
            result.Containers.Add(VillageMapBuilder.PutContainer(hill, ground, "SmallOakChest", site + new Vector2(5.5f, 3f), -110f));
            Spots(result, ground, c, new Vector2(-6f, -6f), new Vector2(8f, 0f));
        }

        /// The windmill's yard: a cart, hay and sacks, a fallen sail, a millstone on its edge, stores by the door and a lamp on the road.
        private static void AbandonedWindmill(Transform root, VillageGround ground, VillageMapBuilder.Result result, System.Random random)
        {
            Vector2 c = Center("Abandoned Windmill");
            Transform yard = BattleEditorUtility.CreateChild("Abandoned Windmill", root).transform;
            Put(yard, ground, DungeonVillageKitBuilder.Load("Prop_Wagon"), c + new Vector2(-12f, -2f), 120f);
            Put(yard, ground, VillageArchitectureBuilder.Piece("HayBale"), c + new Vector2(10f, -4f), 20f);
            Put(yard, ground, VillageArchitectureBuilder.Piece("HayBale"), c + new Vector2(11f, -5.5f), 70f);
            Put(yard, ground, VillageArchitectureBuilder.Piece("Haystack"), c + new Vector2(14f, 10f), 0f);
            Put(yard, ground, DungeonKitBuilder.Load("Bag"), c + new Vector2(8f, -1f), 0f);
            Put(yard, ground, DungeonKitBuilder.Load("FarmCrate_Empty"), c + new Vector2(9f, -2f), 30f);
            VillageArchitectureBuilder.Tilted(yard, "Fallen Sail", Ground(ground, c + new Vector2(14f, 4f)) + Vector3.up * 0.15f, Ground(ground, c + new Vector2(18f, 8f)) + Vector3.up * 0.9f, 1.5f, 0.12f, DarkWood);
            DungeonPropBuilder.MeshObject("Millstone", yard, new DungeonMeshBuilder(0.5f).Cylinder(Vector3.zero, 1.3f, 0.5f, 18).Save("MillstoneEdge"), Stone,
                Ground(ground, c + new Vector2(-4f, 14f)) + Vector3.up * 1.1f, new Vector3(0f, 30f, 80f));
            result.Containers.Add(VillageMapBuilder.PutContainer(yard, ground, "Crate", c + new Vector2(-9f, 3f), 15f));
            result.Containers.Add(VillageMapBuilder.PutContainer(yard, ground, "Barrel", c + new Vector2(4f, -3.5f), 0f));
            VillageMapBuilder.LampPost(yard, ground, c + new Vector2(-5f, -10f), 180f);
            Spots(result, ground, c, new Vector2(-8f, -4f), new Vector2(9f, 2f));
        }

        private static void Watchtower(Transform root, VillageGround ground, VillageMapBuilder.Result result, System.Random random)
        {
            Vector2 c = Center("Watchtower");
            Transform post = BattleEditorUtility.CreateChild("Watch Post", root).transform;
            Stakes(post, ground, c + new Vector2(-14f, 6f), c + new Vector2(-12f, -6f), random);
            Stakes(post, ground, c + new Vector2(-6f, -14f), c + new Vector2(6f, -13f), random);
            Stakes(post, ground, c + new Vector2(13f, -4f), c + new Vector2(12f, 9f), random);
            Put(post, ground, DungeonMapBuilder.Load("Campfire"), c + new Vector2(-6f, -3f), 0f);
            Put(post, ground, DungeonMedievalBuilder.Load("Large Banner"), c + new Vector2(-3f, 8f), 160f);
            result.Containers.Add(VillageMapBuilder.PutContainer(post, ground, "Crate", c + new Vector2(-9f, -7f), 30f));
            result.Containers.Add(VillageMapBuilder.PutContainer(post, ground, "Barrel", c + new Vector2(-10f, -5.5f), 0f));
            Put(post, ground, DungeonMapBuilder.Load("FallenPeasant"), c + new Vector2(7f, -8f), 200f);
            VillageMapBuilder.LampPost(post, ground, c + new Vector2(-8f, 4f), 100f);
            Spots(result, ground, c, new Vector2(-6f, 6f), new Vector2(8f, -6f));
        }

        /// The stream runs out into the bog: a cart stuck in the mud, stepping stones, a dead ranger, willows all round.
        private static void WillowFord(Transform root, VillageGround ground, VillageMapBuilder.Result result, System.Random random)
        {
            Vector2 c = Center("Willow Ford");
            Transform ford = BattleEditorUtility.CreateChild("Willow Ford", root).transform;
            GameObject wagon = Put(ford, ground, DungeonVillageKitBuilder.Load("Prop_Wagon"), c + new Vector2(4f, -3f), 35f);
            wagon.transform.localRotation = Quaternion.Euler(7f, 35f, -13f);
            wagon.transform.localPosition += Vector3.down * 0.35f;
            Put(ford, ground, DungeonMapBuilder.Load("FallenRanger"), c + new Vector2(6.5f, -1f), 120f);
            result.Containers.Add(VillageMapBuilder.PutContainer(ford, ground, "Crate", c + new Vector2(2f, -6f), 40f));

            for (int i = 0; i < 10; i++)
            {
                Vector2 point = c + new Vector2(-10f + i * 2.1f + Rand(random, 0.4f), 6f + Mathf.Sin(i * 0.7f) * 3f);
                GameObject stone = Put(ford, ground, DungeonVegetationBuilder.Load($"Stone{random.Next(4)}"), point, random.Next(360));
                stone.transform.localScale = Vector3.one * (1.2f + (float)random.NextDouble() * 0.5f);
            }

            VillageMapBuilder.LampPost(ford, ground, c + new Vector2(-6f, -8f), 40f);
            Spots(result, ground, c, new Vector2(-6f, 2f), new Vector2(8f, 6f));
        }

        /// Each island of the bog gets its own trouble: palisades of sharpened stakes, sunken boats, skull piles, cages,
        /// the hermit's cauldron, the fisher's jetty, and a gate over the south causeway.
        private static void SwampIslands(Transform root, VillageGround ground, VillageMapBuilder.Result result, System.Random random)
        {
            Transform swamp = BattleEditorUtility.CreateChild("Swamp Points", root).transform;

            Vector2 edge = Center("Swamp Edge");
            Stakes(swamp, ground, edge + new Vector2(-12f, 8f), edge + new Vector2(-4f, 14f), random);
            Stakes(swamp, ground, edge + new Vector2(8f, 10f), edge + new Vector2(14f, 2f), random);
            result.Containers.Add(VillageMapBuilder.PutContainer(swamp, ground, "Barrel", edge + new Vector2(-6f, -7f), 0f));
            Spots(result, ground, edge, new Vector2(-8f, 0f), new Vector2(6f, -8f));

            Vector2 huts = Center("Stilt Huts");
            Boat(swamp, ground, huts + new Vector2(-10f, -8f), 40f, 18f, random);
            Put(swamp, ground, DungeonMapBuilder.Load("FallenPeasant"), huts + new Vector2(8f, 6f), 30f);
            Spots(result, ground, huts, new Vector2(-6f, 8f), new Vector2(9f, -4f));

            Vector2 mire = Center("Western Mire");
            Boat(swamp, ground, mire + new Vector2(9f, 8f), -70f, -22f, random);
            SkullPost(swamp, ground, mire + new Vector2(-8f, 6f));
            SkullPost(swamp, ground, mire + new Vector2(10f, -8f));
            result.Containers.Add(VillageMapBuilder.PutContainer(swamp, ground, "Crate", mire + new Vector2(-6f, -9f), 25f));
            Spots(result, ground, mire, new Vector2(-4f, 10f), new Vector2(8f, -2f));

            Vector2 cellar = Center("Crimson Isle");

            for (int i = 0; i < 7; i++)
            {
                float angle = (i * 52f + 20f) * Mathf.Deg2Rad;
                Vector2 point = cellar + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (13f + Rand(random, 1.5f));

                if (ground.Open(point) < -1f && ground.RoadDistance(point.x, point.y) > 3f && VillageLayout.Smooth(VillageLayout.Boardwalks[4], 2f).TrueForAll(p => (p - point).magnitude > 4f))
                    DungeonStructureBuilder.Block(swamp, "Ruin", Ground(ground, point) + Vector3.up * 0.6f, new Vector3(0.8f, 1.2f + (float)random.NextDouble() * 2.4f, 2.4f), Stone, Facing(point - cellar) + 90f);
            }

            Spots(result, ground, cellar, new Vector2(8f, 8f), new Vector2(-9f, 4f));

            Vector2 causeway = Center("South Causeway");
            GateArch(swamp, ground, new Vector2(22f, -104f), 0f, 5.6f);
            Stakes(swamp, ground, causeway + new Vector2(-10f, 10f), causeway + new Vector2(-4f, 13f), random);
            Stakes(swamp, ground, causeway + new Vector2(5f, 13f), causeway + new Vector2(11f, 9f), random);

            Vector2 reeds = Center("Reed Banks");
            Boat(swamp, ground, reeds + new Vector2(8f, 6f), 120f, 25f, random);
            Boat(swamp, ground, reeds + new Vector2(-9f, -6f), 10f, -12f, random);
            NetRack(swamp, ground, reeds + new Vector2(-4f, 8f), 20f);
            Put(swamp, ground, DungeonMapBuilder.Load("Campfire"), reeds + new Vector2(2f, -2f), 0f);
            result.Containers.Add(VillageMapBuilder.PutContainer(swamp, ground, "Barrel", reeds + new Vector2(5f, -7f), 0f));
            VillageMapBuilder.LampPost(swamp, ground, reeds + new Vector2(-2f, -9f), 200f);
            Spots(result, ground, reeds, new Vector2(-8f, 2f), new Vector2(7f, -3f));

            Vector2 bog = Center("Black Bog");
            SkullPost(swamp, ground, bog + new Vector2(-7f, 7f));
            SkullPost(swamp, ground, bog + new Vector2(8f, -9f));
            Put(swamp, ground, DungeonKitBuilder.Load("Cage_Small"), bog + new Vector2(-9f, -4f), 30f);
            Put(swamp, ground, DungeonMapBuilder.Load("CandleCluster"), bog + new Vector2(-6f, -6f), 0f);
            Stakes(swamp, ground, bog + new Vector2(10f, 4f), bog + new Vector2(6f, 12f), random);
            Spots(result, ground, bog, new Vector2(-6f, 0f), new Vector2(4f, 8f));

            Vector2 hermit = Center("Hermit's Hut");
            Put(swamp, ground, DungeonKitBuilder.Load("Cauldron"), hermit + new Vector2(-5f, 4f), 0f);
            Put(swamp, ground, DungeonMapBuilder.Load("Campfire"), hermit + new Vector2(-5f, 4f), 0f);
            Put(swamp, ground, DungeonMapBuilder.Load("CandleCluster"), hermit + new Vector2(4f, 8f), 0f);
            SkullPost(swamp, ground, hermit + new Vector2(-10f, -6f));
            result.Containers.Add(VillageMapBuilder.PutContainer(swamp, ground, "SmallOakChest", hermit + new Vector2(-8f, 6f), 60f));
            Spots(result, ground, hermit, new Vector2(-6f, -4f), new Vector2(6f, 6f));

            Vector2 jetty = Center("Fisher's Jetty");
            VillageArchitectureBuilder.Boardwalk(swamp, ground, new[] { jetty + new Vector2(-6f, 6f), jetty + new Vector2(-12f, 14f), jetty + new Vector2(-14f, 22f) });
            Boat(swamp, ground, jetty + new Vector2(-17f, 17f), -30f, 4f, random);
            Put(swamp, ground, DungeonKitBuilder.Load("Barrel"), jetty + new Vector2(5f, 6f), 0f);
            NetRack(swamp, ground, jetty + new Vector2(8f, -4f), -70f);
            result.Containers.Add(VillageMapBuilder.PutContainer(swamp, ground, "Crate", jetty + new Vector2(6f, 4f), 10f));
            VillageMapBuilder.LampPost(swamp, ground, jetty + new Vector2(-8f, 15f), 120f, VillageLayout.DeckHeight);
            Spots(result, ground, jetty, new Vector2(-6f, -6f), new Vector2(8f, 2f));
        }

        /// A rowing boat sunk to its gunwales: hull planks, ribs and a thwart, rolled over to one side.
        private static void Boat(Transform parent, VillageGround ground, Vector2 point, float yaw, float roll, System.Random random)
        {
            float y = Mathf.Max(ground.Height(point), VillageLayout.WaterLevel - 0.35f);
            Transform boat = BattleEditorUtility.CreateChild("Sunken Boat", parent, new Vector3(point.x, y, point.y)).transform;
            boat.localRotation = Quaternion.Euler(Rand(random, 6f), yaw, roll);
            DungeonStructureBuilder.Block(boat, "Keel", new Vector3(0f, 0.04f, 0f), new Vector3(0.9f, 0.08f, 4f), Planks, 0f, false);

            foreach (float s in new[] { -1f, 1f })
            {
                Transform side = BattleEditorUtility.CreateChild("Side", boat, new Vector3(s * 0.62f, 0.32f, 0f)).transform;
                side.localRotation = Quaternion.Euler(0f, 0f, -s * 32f);
                DungeonStructureBuilder.Block(side, "Strake", Vector3.zero, new Vector3(0.06f, 0.72f, 3.9f), Planks, 0f, false);
                Transform bow = BattleEditorUtility.CreateChild("Bow", boat, new Vector3(s * 0.32f, 0.32f, 2.25f)).transform;
                bow.localRotation = Quaternion.Euler(0f, -s * 35f, 0f);
                DungeonStructureBuilder.Block(bow, "Strake", Vector3.zero, new Vector3(0.06f, 0.66f, 0.9f), Planks, 0f, false);
            }

            DungeonStructureBuilder.Block(boat, "Stern", new Vector3(0f, 0.32f, -1.98f), new Vector3(1.5f, 0.66f, 0.08f), Planks, 0f, false);

            foreach (float z in new[] { -1.2f, 0f, 1.2f })
                DungeonStructureBuilder.Block(boat, "Rib", new Vector3(0f, 0.12f, z), new Vector3(1.3f, 0.09f, 0.1f), DarkWood, 0f, false);

            DungeonStructureBuilder.Block(boat, "Thwart", new Vector3(0f, 0.45f, 0.4f), new Vector3(1.4f, 0.05f, 0.3f), Planks, 0f, false);
            BoxCollider box = boat.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.35f, 0f);
            box.size = new Vector3(1.6f, 0.8f, 4.4f);
            boat.gameObject.isStatic = true;
        }

        /// A stake with skulls heaped at its foot and one on top: the bog people mark their ground.
        private static void SkullPost(Transform parent, VillageGround ground, Vector2 point)
        {
            Transform post = BattleEditorUtility.CreateChild("Skull Post", parent, Ground(ground, point)).transform;
            DungeonStructureBuilder.Block(post, "Post", new Vector3(0f, 1.1f, 0f), new Vector3(0.18f, 2.6f, 0.18f), DarkWood);
            DungeonMapBuilder.Place(DungeonMapBuilder.Load("SkullPile"), post, Vector3.zero, 30f);
            DungeonMapBuilder.Place(DungeonMapBuilder.Load("CandleCluster"), post, new Vector3(0.6f, 0f, 0.4f), 0f);
        }

        /// Fishing nets hung to dry between two posts.
        private static void NetRack(Transform parent, VillageGround ground, Vector2 point, float yaw)
        {
            Transform rack = BattleEditorUtility.CreateChild("Net Rack", parent, Ground(ground, point)).transform;
            rack.localRotation = Quaternion.Euler(0f, yaw, 0f);

            foreach (float x in new[] { -1.6f, 1.6f })
                DungeonStructureBuilder.Block(rack, "Post", new Vector3(x, 1f, 0f), new Vector3(0.16f, 2.4f, 0.16f), DarkWood);

            DungeonStructureBuilder.Block(rack, "Pole", new Vector3(0f, 2.1f, 0f), new Vector3(3.6f, 0.1f, 0.1f), DarkWood, 0f, false);
            DungeonPropBuilder.MeshObject("Net", rack, new DungeonMeshBuilder(1f).DoubleQuad(new Vector3(0f, 1.45f, 0f), Vector3.forward, Vector3.right * 1.5f, Vector3.up * 0.65f).Save("VillageNet"),
                DungeonPropBuilder.DarkWood, default, default, false);
        }

        private static void DryingRack(Transform parent, VillageGround ground, Vector2 point, float yaw, System.Random random)
        {
            Transform rack = BattleEditorUtility.CreateChild("Drying Rack", parent, Ground(ground, point)).transform;
            rack.localRotation = Quaternion.Euler(0f, yaw, 0f);

            foreach (float x in new[] { -1.4f, 1.4f })
                DungeonStructureBuilder.Block(rack, "Post", new Vector3(x, 1f, 0f), new Vector3(0.16f, 2.2f, 0.16f), DarkWood);

            DungeonStructureBuilder.Block(rack, "Pole", new Vector3(0f, 1.95f, 0f), new Vector3(3.2f, 0.1f, 0.1f), DarkWood, 0f, false);

            for (int i = 0; i < 3; i++)
                DungeonStructureBuilder.Block(rack, "Hide", new Vector3(-0.9f + i * 0.9f, 1.35f, 0f), new Vector3(0.7f, 1.1f + (float)random.NextDouble() * 0.2f, 0.03f), DungeonPropBuilder.Textured("Hide", "DarkWood", 0.3f, 0.2f), 0f, false);
        }

        /// Banks of low mist over the bog islands, the ford and where the river runs in its ravine.
        private static void Mists(Transform parent, VillageGround ground)
        {
            Transform root = BattleEditorUtility.CreateChild("Mist", parent).transform;
            Material material = MistMaterial();

            foreach (VillageLayout.Zone zone in VillageLayout.Zones)
            {
                if (zone.Kind == VillageLayout.ZoneKind.Swamp)
                    Mist(root, material, new Vector3(zone.Center.x, VillageLayout.WaterLevel + 0.6f, zone.Center.y), zone.Radius * 1.6f, 16);
            }

            for (int i = 10; i < ground.River.Count; i += 30)
                Mist(root, material, new Vector3(ground.River[i].x, VillageLayout.WaterLevel + 0.8f, ground.River[i].y), 24f, 10);

            Mist(root, material, new Vector3(VillageLayout.Graveyard.center.x, ground.Height(VillageLayout.Graveyard.center) + 0.6f, VillageLayout.Graveyard.center.y), 110f, 22);
        }

        private static void Mist(Transform parent, Material material, Vector3 position, float size, int count)
        {
            GameObject go = BattleEditorUtility.CreateChild("Mist Bank", parent, position);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.duration = 30f;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(24f, 40f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(9f, 16f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new Color(0.62f, 0.7f, 0.74f, 0.11f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = count;
            main.scalingMode = ParticleSystemScalingMode.Shape;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = count / 32f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(size, 1.2f, size);

            ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(0.08f, 0.25f);
            velocity.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);

            ParticleSystem.RotationOverLifetimeModule rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f);

            ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
            color.enabled = true;
            Gradient fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            color.color = fade;

            ParticleSystem.SizeOverLifetimeModule growth = ps.sizeOverLifetime;
            growth.enabled = true;
            growth.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.75f, 1f, 1.25f));

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.maxParticleSize = 4f;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingFudge = 10f;
        }

        /// Soft transparent particles that fade where they touch the ground and when the camera walks into them.
        private static Material MistMaterial()
        {
            string path = $"{DungeonPropBuilder.MaterialsFolder}/VillageMist.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetTexture("_BaseMap", MistTexture());
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_SoftParticlesEnabled", 1f);
            material.SetFloat("_SoftParticlesNearFadeDistance", 0f);
            material.SetFloat("_SoftParticlesFarFadeDistance", 2.5f);
            material.SetFloat("_CameraFadingEnabled", 1f);
            material.SetFloat("_CameraNearFadeDistance", 1.5f);
            material.SetFloat("_CameraFarFadeDistance", 6f);
            material.SetVector("_SoftParticleFadeParams", new Vector4(0f, 1f / 2.5f, 0f, 0f));
            material.SetVector("_CameraFadeParams", new Vector4(1.5f, 1f / 4.5f, 0f, 0f));
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_SOFTPARTICLES_ON");
            material.EnableKeyword("_FADING_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetOverrideTag("RenderType", "Transparent");
            EditorUtility.SetDirty(material);

            return material;
        }

        /// A cloudy puff: fractal noise inside a soft round falloff, white with the shape in alpha.
        private static Texture2D MistTexture()
        {
            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size - 0.5f;
                    float dy = (y + 0.5f) / size - 0.5f;
                    float falloff = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) * 2f);
                    float cloud = DungeonTextureBuilder.Noise(x / (float)size + 3f, y / (float)size + 5f, 3f, 4);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(falloff * falloff * (0.4f + cloud * 1.1f)));
                }
            }

            texture.SetPixels(pixels);
            File.WriteAllBytes(MistTexturePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(MistTexturePath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(MistTexturePath);
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(MistTexturePath);
        }

        private static void House(List<Plan> plans, string zone, Vector2 offset, HouseSpec spec)
        {
            Vector2 position = Center(zone) + offset;
            plans.Add(new Plan { Kind = "House", Position = position, Yaw = Facing(-offset) + 8f, Spec = spec, HalfSize = new Vector2(spec.Width, spec.Length) * 0.5f });
        }

        private static void Spots(VillageMapBuilder.Result result, VillageGround ground, Vector2 center, params Vector2[] offsets)
        {
            foreach (Vector2 offset in offsets)
                result.MonsterSpots.Add(Ground(ground, center + offset));
        }

        private static Vector2 Center(string zone) => VillageLayout.FindZone(zone).Center;

        private static Vector2 Ring(System.Random random, float from, float to)
        {
            float angle = (float)random.NextDouble() * Mathf.PI * 2f;

            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Mathf.Lerp(from, to, (float)random.NextDouble());
        }

        private static GameObject Put(Transform parent, VillageGround ground, GameObject prefab, Vector2 point, float yaw) => VillageMapBuilder.Put(parent, ground, prefab, point, yaw);
        private static Vector3 Ground(VillageGround ground, Vector2 point) => VillageMapBuilder.Ground(ground, point);
        private static float Facing(Vector2 direction) => VillageMapBuilder.Facing(direction);
        private static Vector2 Rotate(Vector2 local, float yaw) => VillageMapBuilder.Rotate(local, yaw);
        private static float Rand(System.Random random, float range) => ((float)random.NextDouble() * 2f - 1f) * range;
    }
}
