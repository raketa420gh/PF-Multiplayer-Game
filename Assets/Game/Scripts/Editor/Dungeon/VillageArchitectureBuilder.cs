using System.Collections.Generic;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Buildings and structures of the cursed village. Houses are put together from the Medieval Village MegaKit on a 2 m module:
    /// walls with a door, windows and shutters, corner posts, plank floors, an inner staircase on two-storey houses, a tiled roof
    /// with gables and a chimney, furniture and loot inside. The rest is modelled here: the abandoned windmill, chapel ruin, stilt huts, bridges,
    /// boardwalks, graves, walls and the shrines of the red portals that lead down to the second floor.
    internal static class VillageArchitectureBuilder
    {
        public const float BridgeWidth = 3.6f;
        /// Half size of the abandoned windmill with its gallery.
        public const float MillReach = MillHalf + GalleryDepth;
        /// Flagstone ring round a red portal.
        public const float ShrineRadius = 4.5f;
        public const float FenceModule = DungeonVillageKitBuilder.Module;

        private const float Storey = DungeonVillageKitBuilder.Storey;
        private const float Module = DungeonVillageKitBuilder.Module;
        private const float Inset = DungeonVillageKitBuilder.WallDepth;
        private const float FloorTop = 0.12f;
        private const float StairWidth = 1.1f;
        /// The abandoned windmill: outer half size of the square stone tower, wall thickness, storey height, gallery depth.
        private const float MillHalf = 4.5f;
        private const float MillWall = 0.6f;
        private const float MillStorey = 3.2f;
        private const float GalleryDepth = 1.8f;

        public enum Style
        {
            Plaster,
            Brick,
            Timber
        }

        /// How rich the containers of a building are: farm poor, village rich.
        public enum LootTier
        {
            Poor,
            Normal,
            Rich
        }

        public sealed class HouseSpec
        {
            public int Width = 6;
            public int Length = 8;
            public int Storeys = 1;
            public Style Style;
            public bool IsLit;
            public bool IsWindowless;
            public bool HasChimney = true;
            public bool IsFurnished = true;
            public LootTier Loot = LootTier.Normal;
            public int Seed;
        }

        /// What a building leaves for the map builder: loot and spots inside where monsters can stand.
        public sealed class Site
        {
            public readonly List<ContainerComponent> Containers = new();
            public readonly List<Vector3> Spots = new();
        }

        private static readonly Dictionary<int, int[]> s_roofLengths = new() { [4] = new[] { 4, 6, 8 }, [6] = new[] { 6, 8, 10, 12, 14 }, [8] = new[] { 8, 10, 12, 14 } };

        public static Material Stone => DungeonVillageKitBuilder.Surface("MI_UnevenBrick");
        private static Material Planks => DungeonPropBuilder.WoodPlanks;
        private static Material DarkWood => DungeonPropBuilder.DarkWood;
        private static Material Tiles => DungeonVillageKitBuilder.Surface("MI_RoundTiles");

        /// Grave markers, haystacks and the plaza well, saved once as prefabs.
        public static void BuildPieces()
        {
            Material rock = DungeonVegetationBuilder.RockMaterial;
            SavePiece("Headstone", new DungeonMeshBuilder(1f).Box(new Vector3(0f, 0.4f, 0f), new Vector3(0.62f, 0.8f, 0.16f)).Box(new Vector3(0f, 0.84f, 0f), new Vector3(0.46f, 0.1f, 0.16f))
                .Box(new Vector3(0f, 0.92f, 0f), new Vector3(0.26f, 0.08f, 0.16f)), rock);
            SavePiece("GraveCross", new DungeonMeshBuilder(1f).Box(new Vector3(0f, 0.6f, 0f), new Vector3(0.16f, 1.2f, 0.14f)).Box(new Vector3(0f, 0.86f, 0f), new Vector3(0.62f, 0.15f, 0.14f)), rock);
            SavePiece("GraveSlab", new DungeonMeshBuilder(1f).Box(new Vector3(0f, 0.12f, 0f), new Vector3(0.9f, 0.24f, 1.9f)).Box(new Vector3(0f, 0.27f, 0f), new Vector3(0.8f, 0.06f, 1.8f)), rock);
            SavePiece("GraveMound", new DungeonMeshBuilder(0.5f).Prism(0.9f, new Vector2(-0.95f, 0f), new Vector2(-0.6f, 0.25f), new Vector2(0.6f, 0.25f), new Vector2(0.95f, 0f)), DungeonPropBuilder.Textured("GraveDirt", "Dirt", 0.5f, 0.05f));
            Material hay = DungeonPropBuilder.Textured("Hay", "Grass", 0.5f, 0.05f);
            hay.SetColor("_BaseColor", new Color(1.6f, 1.25f, 0.7f));
            SavePiece("Haystack", new DungeonMeshBuilder(1f).Cylinder(new Vector3(0f, 0.9f, 0f), 1.5f, 1.8f, 14, 1.2f).Cylinder(new Vector3(0f, 2.1f, 0f), 1.2f, 0.6f, 14, 0.2f), hay);
            SavePiece("HayBale", new DungeonMeshBuilder(1f).Box(new Vector3(0f, 0.35f, 0f), new Vector3(1.2f, 0.7f, 0.8f)), hay);
            SavePiece("Well", new DungeonMeshBuilder(0.5f).Cylinder(new Vector3(0f, 0.45f, 0f), 1.4f, 0.9f, 16).Box(new Vector3(-1.2f, 1.4f, 0f), new Vector3(0.18f, 1.9f, 0.18f)).Box(new Vector3(1.2f, 1.4f, 0f), new Vector3(0.18f, 1.9f, 0.18f)).Box(new Vector3(0f, 2.3f, 0f), new Vector3(2.6f, 0.16f, 0.16f)), Stone);
        }

        public static GameObject Piece(string name) => AssetDatabase.LoadAssetAtPath<GameObject>($"{DungeonVillageKitBuilder.PrefabsFolder}/{name}.prefab");

        /// A house on its pad: local +Z is the street side with the door.
        public static Transform House(Transform parent, string name, Vector3 position, float yaw, HouseSpec spec, Site site)
        {
            System.Random random = new System.Random(spec.Seed);
            Transform root = Root(parent, name, position, yaw);

            // The stairs need eight metres of wall to climb a storey.
            if (spec.Storeys > 1)
                spec.Length = Mathf.Max(spec.Length, 8);

            float halfW = spec.Width * 0.5f;
            float halfL = spec.Length * 0.5f;
            int columns = spec.Width / 2;
            int door = columns / 2;
            bool hasStairs = spec.Storeys > 1;
            float top = spec.Storeys * Storey;

            for (int storey = 0; storey < spec.Storeys; storey++)
            {
                float y = storey * Storey;
                string prefix = spec.Style == Style.Brick || spec.Style == Style.Timber && storey == 0 ? "Wall_UnevenBrick_" : "Wall_Plaster_";
                string corner = prefix == "Wall_UnevenBrick_" ? "Corner_Exterior_Brick" : "Corner_Exterior_Wood";

                for (int side = 0; side < 4; side++)
                {
                    bool isFront = side == 0;
                    bool isLong = side % 2 == 1;
                    int count = (isLong ? spec.Length : spec.Width) / 2;
                    float half = isLong ? halfL : halfW;
                    float face = isLong ? halfW : halfL;
                    float sideYaw = side switch { 0 => 0f, 1 => 90f, 2 => 180f, _ => -90f };
                    Quaternion turn = Quaternion.Euler(0f, sideYaw, 0f);

                    for (int i = 0; i < count; i++)
                    {
                        float along = -half + Module * 0.5f + i * Module;
                        Vector3 local = turn * new Vector3(-along, y, face);
                        bool isDoor = isFront && storey == 0 && count - 1 - i == door;
                        string wall = isDoor ? prefix + "Door_Flat" : WallPiece(prefix, random, spec.IsWindowless || storey == 0 && isLong && hasStairs && side == 3, storey > 0);
                        Kit(wall, root, local, sideYaw);

                        if (wall.Contains("_Window_"))
                            Window(root, wall, local, sideYaw, random, spec.IsLit);

                        if (isDoor)
                        {
                            Place(Load("HouseDoor"), root, turn * new Vector3(-along, y, face - 0.11f), sideYaw);

                            if (spec.IsLit)
                                Prop(DungeonKitBuilder.Load("Lantern_Wall"), root, turn * new Vector3(-along - 1.05f, y + 2.2f, face + 0.05f), sideYaw);
                        }
                    }

                    Kit(corner, root, new Vector3(side is 0 or 1 ? halfW : -halfW, y, side is 0 or 3 ? halfL : -halfL), sideYaw);
                }
            }

            Floors(root, spec, hasStairs);
            Roof(root, spec, top, random);

            if (spec.IsFurnished)
                Furnish(root, spec, random, site, hasStairs);

            if (spec.IsLit)
                DungeonPropBuilder.PointLight(root, new Vector3(0f, 2.4f, 0f), new Color(1f, 0.7f, 0.42f), 7f, 1.3f, true);

            site.Spots.Add(root.TransformPoint(new Vector3(0f, FloorTop, 0f)));

            return root;
        }

        /// A long brick barn with a hayloft smell: one tall room, hay, a cart and the farm's stores.
        public static Transform Barn(Transform parent, Vector3 position, float yaw, Site site)
        {
            Transform root = House(parent, "Barn", position, yaw, new HouseSpec { Width = 8, Length = 12, Style = Style.Timber, HasChimney = false, IsFurnished = false, Seed = 77 }, site);
            System.Random random = new System.Random(3);

            for (int i = 0; i < 5; i++)
                Place(Piece("HayBale"), root, new Vector3(-2.6f + (i % 2) * 0.1f, FloorTop + (i / 3) * 0.7f, -4.6f + (i % 3) * 0.9f), random.Next(-8, 8));

            site.Containers.Add(Container("Crate", root, new Vector3(2.8f, FloorTop, -4.8f), 10f));
            site.Containers.Add(Container("Barrel", root, new Vector3(3f, FloorTop, -3.4f), 0f));
            site.Containers.Add(Container("SmallOakChest", root, new Vector3(-3.1f, FloorTop, 1.5f), 90f));
            Prop(DungeonKitBuilder.Load("Workbench"), root, new Vector3(3f, FloorTop, 1f), -90f);
            Prop(DungeonKitBuilder.Load("FarmCrate_Carrot"), root, new Vector3(2.6f, FloorTop, 3.8f), 20f);
            Prop(DungeonKitBuilder.Load("FarmCrate_Apple"), root, new Vector3(1.6f, FloorTop, 4.6f), -10f);

            return root;
        }

        /// Abandoned windmill, three storeys to fight over: a square stone tower with a timber top, a conical cap and four lattice
        /// sails (one broken). Ground floor with doorways front and back, stairs up the west wall to the middle floor, which opens
        /// onto a wooden gallery round the tower (a jump down is safe, a ramp leads up to it from the east, a stretch of the west
        /// side has fallen), stairs up the east wall to the top floor with windows on every side. Local +Z is the front.
        public static Transform Windmill(Transform parent, Vector3 position, float yaw, Site site)
        {
            const float half = MillHalf;
            const float inner = MillHalf - MillWall;
            const float top = MillStorey * 3f;
            Transform root = Root(parent, "Windmill", position, yaw);
            float run = DungeonStructureBuilder.StairRun(MillStorey);
            float west = -inner + StairWidth + 0.1f;
            float east = inner - StairWidth - 0.1f;
            // A metre in front of each flight to step onto it, the landing at its top.
            float south = -inner + 1f + run;
            float north = inner - 1f - run;
            DungeonStructureBuilder.Block(root, "Floor", new Vector3(0f, FloorTop - 0.3f, 0f), new Vector3(inner * 2f, 0.6f, inner * 2f), Planks);

            // Walls, storey by storey: (front, east, back, west) openings as (centre, width, sill, head); width 0 is a blind wall.
            (float center, float width, float sill, float head)[][] openings =
            {
                new[] { (0f, 1.6f, 0f, 2.6f), (0f, 1.2f, 1.3f, 2.3f), (0f, 1.6f, 0f, 2.6f), (0f, 0f, 0f, 0f) },
                new[] { (0f, 1.4f, 0f, 2.4f), (0f, 0f, 0f, 0f), (1.2f, 1.4f, 0f, 2.4f), (0f, 1.2f, 1.2f, 2.3f) },
                new[] { (0f, 2.4f, 0.9f, 2.5f), (0f, 2.4f, 0.9f, 2.5f), (0f, 2.4f, 0.9f, 2.5f), (0f, 2.4f, 0.9f, 2.5f) }
            };

            for (int storey = 0; storey < 3; storey++)
            {
                for (int side = 0; side < 4; side++)
                {
                    (float center, float width, float sill, float head) = openings[storey][side];
                    MillSide(root, side, storey * MillStorey, center, width, sill, head, storey == 2 ? DarkWood : Stone);
                }
            }

            // Middle floor: open over the west flight. Top floor: open over the east flight.
            float y1 = MillStorey;
            float y2 = MillStorey * 2f;
            Slab(root, (west + inner) * 0.5f, 0f, inner - west, inner * 2f, y1);
            Slab(root, (west - inner) * 0.5f, (south + inner) * 0.5f, west + inner, inner - south, y1);
            Slab(root, (east - inner) * 0.5f, 0f, east + inner, inner * 2f, y2);
            Slab(root, (east + inner) * 0.5f, (north - inner) * 0.5f, inner - east, north + inner, y2);
            Slab(root, 0f, 0f, inner * 2f, inner * 2f, top, "Ceiling");
            DungeonStructureBuilder.Stairs(root, new Vector3(-inner + StairWidth * 0.5f + 0.05f, FloorTop, -inner + 1f), 0f, StairWidth, MillStorey - FloorTop + 0.06f, DarkWood);
            DungeonStructureBuilder.Stairs(root, new Vector3(inner - StairWidth * 0.5f - 0.05f, y1 + 0.06f, inner - 1f), 180f, StairWidth, MillStorey, DarkWood);
            DungeonStructureBuilder.Rail(root, new Vector3(west, y1 + 0.06f, -inner + 0.3f), new Vector3(west, y1 + 0.06f, south - 0.6f));
            DungeonStructureBuilder.Rail(root, new Vector3(east, y2 + 0.06f, inner - 0.3f), new Vector3(east, y2 + 0.06f, north + 0.6f));

            // The gallery round the middle floor; a stretch of the west side has fallen in.
            float deck = half + GalleryDepth * 0.5f;
            float outer = half + GalleryDepth;
            DungeonStructureBuilder.Block(root, "Gallery", new Vector3(0f, y1 - 0.06f, deck), new Vector3(outer * 2f, 0.24f, GalleryDepth), Planks);
            DungeonStructureBuilder.Block(root, "Gallery", new Vector3(0f, y1 - 0.06f, -deck), new Vector3(outer * 2f, 0.24f, GalleryDepth), Planks);
            DungeonStructureBuilder.Block(root, "Gallery", new Vector3(deck, y1 - 0.06f, 0f), new Vector3(GalleryDepth, 0.24f, half * 2f), Planks);
            DungeonStructureBuilder.Block(root, "Gallery", new Vector3(-deck, y1 - 0.06f, -2.75f), new Vector3(GalleryDepth, 0.24f, 3.5f), Planks);
            DungeonStructureBuilder.Block(root, "Gallery", new Vector3(-deck, y1 - 0.06f, 3f), new Vector3(GalleryDepth, 0.24f, 3f), Planks);
            Vector3 rail = Vector3.up * (y1 + 0.06f);
            DungeonStructureBuilder.Rail(root, rail + new Vector3(-outer, 0f, outer), rail + new Vector3(outer, 0f, outer));
            DungeonStructureBuilder.Rail(root, rail + new Vector3(-outer, 0f, -outer), rail + new Vector3(outer, 0f, -outer));
            DungeonStructureBuilder.Rail(root, rail + new Vector3(outer, 0f, -1.4f), rail + new Vector3(outer, 0f, outer));
            DungeonStructureBuilder.Rail(root, rail + new Vector3(-outer, 0f, -outer), rail + new Vector3(-outer, 0f, -4.5f));

            foreach (Vector3 corner in new[] { new Vector3(outer, 0f, outer), new Vector3(-outer, 0f, outer), new Vector3(outer, 0f, -outer), new Vector3(-outer, 0f, -outer) })
                Tilted(root, "Strut", corner * 0.85f + Vector3.up * (y1 - 0.2f), new Vector3(corner.x * 0.75f, 1.4f, corner.z * 0.75f), 0.2f, 0.2f, DarkWood, false);

            // Plank ramp from the yard up to the east gallery.
            Tilted(root, "Ramp", new Vector3(outer + 7f, -0.25f, -2.6f), new Vector3(outer - 0.1f, y1 + 0.06f, -2.6f), 1.4f, 0.14f, Planks);
            DungeonPropBuilder.MeshObject("Cap", root, new DungeonMeshBuilder(0.5f).Cylinder(new Vector3(0f, 1.8f, 0f), half * 1.45f, 3.6f, 20, 0.08f).Save("WindmillCap"), Tiles, new Vector3(0f, top, 0f), default, false);
            // The sails turn clear of the gallery on a shaft out of the timber top.
            DungeonStructureBuilder.Block(root, "Shaft", new Vector3(0f, top - 1.4f, (half + outer + 0.5f) * 0.5f), new Vector3(0.35f, 0.35f, outer + 0.5f - half), DarkWood, 0f, false);
            Sails(root, new Vector3(0f, top - 1.4f, outer + 0.5f));

            // Millstones and sacks below, bins and stores in the middle, the miller's chest at the top.
            float upper = y1 + 0.06f;
            float attic = y2 + 0.06f;
            Mesh millstone = new DungeonMeshBuilder(0.5f).Cylinder(new Vector3(0f, 0.25f, 0f), 1.3f, 0.5f, 18).Save("Millstone");
            DungeonPropBuilder.MeshObject("Millstone", root, millstone, Stone, new Vector3(1f, FloorTop, -1.2f));
            DungeonPropBuilder.MeshObject("Millstone", root, millstone, Stone, new Vector3(1f, FloorTop + 0.5f, -1.2f));
            Prop(DungeonKitBuilder.Load("Bag"), root, new Vector3(2.9f, FloorTop, 2.6f), 30f);
            Prop(DungeonKitBuilder.Load("Bag"), root, new Vector3(3.1f, FloorTop, 1.7f), -20f);
            Prop(DungeonKitBuilder.Load("FarmCrate_Empty"), root, new Vector3(-1.4f, upper, -1.6f), 10f);
            Prop(DungeonKitBuilder.Load("Workbench"), root, new Vector3(-0.6f, attic, 3.3f), 180f);
            Place(Piece("HayBale"), root, new Vector3(0.8f, upper, -0.6f), 8f);
            site.Containers.Add(Container("Barrel", root, new Vector3(3.2f, FloorTop, -3.2f), 0f));
            site.Containers.Add(Container("Crate", root, new Vector3(-2f, FloorTop, 3.2f), 15f));
            site.Containers.Add(Container("SmallOakChest", root, new Vector3(1.6f, upper, -3.3f), 180f));
            site.Containers.Add(Container("Barrel", root, new Vector3(0.4f, upper, -3.3f), 0f));
            site.Containers.Add(Container("LargeOakChest", root, new Vector3(-3.2f, attic, -1f), 90f));
            Prop(DungeonKitBuilder.Load("Lantern_Wall"), root, new Vector3(inner - 0.02f, 2.2f, -1.4f), -90f);
            DungeonPropBuilder.PointLight(root, new Vector3(0f, upper + 2.2f, 0f), new Color(1f, 0.7f, 0.42f), 6f, 1.1f, false);
            DungeonPropBuilder.PointLight(root, new Vector3(0f, attic + 2.4f, 0f), new Color(0.75f, 0.8f, 1f), 6f, 0.7f, false);

            site.Spots.Add(root.TransformPoint(new Vector3(-1f, FloorTop, 0.5f)));
            site.Spots.Add(root.TransformPoint(new Vector3(0f, upper, 0f)));
            site.Spots.Add(root.TransformPoint(new Vector3(0f, upper, half + 0.9f)));
            site.Spots.Add(root.TransformPoint(new Vector3(-0.5f, attic, -0.5f)));

            return root;
        }

        /// Roofless chapel in the graveyard: broken walls and windows, a pointed front gable, fallen beams, pews and the altar.
        public static Transform Chapel(Transform parent, Vector3 position, float yaw, Site site)
        {
            const float halfW = 5.5f;
            const float halfL = 11f;
            const float thickness = 0.8f;
            System.Random random = new System.Random(21);
            Transform root = Root(parent, "Chapel", position, yaw);
            DungeonStructureBuilder.Block(root, "Floor", new Vector3(0f, -0.2f, 0f), new Vector3(halfW * 2f, 0.44f, halfL * 2f), DungeonPropBuilder.Flagstone);

            for (int side = -1; side <= 1; side += 2)
            {
                for (float z = -halfL + 1f; z < halfL; z += 2f)
                {
                    bool isBroken = random.NextDouble() < 0.4;
                    float height = isBroken ? 2.2f + (float)random.NextDouble() * 3f : 7f;
                    float x = side * (halfW - thickness * 0.5f);

                    if (!isBroken && Mathf.Abs(z) < halfL - 2f && Mathf.RoundToInt(z) % 4 == 0)
                    {
                        Wall(root, new Vector3(x, 0f, z), 2f, 2.4f, thickness);
                        Wall(root, new Vector3(x, 5f, z), 2f, height - 5f, thickness);
                    }
                    else
                    {
                        Wall(root, new Vector3(x, 0f, z), 2f, height, thickness);
                    }
                }
            }

            // Front with the doorway and a gable, back wall broken down to the windows.
            Wall(root, new Vector3(-3.3f, 0f, -halfL + thickness * 0.5f), 4.4f, 7f, thickness, 90f);
            Wall(root, new Vector3(3.3f, 0f, -halfL + thickness * 0.5f), 4.4f, 7f, thickness, 90f);
            Wall(root, new Vector3(0f, 3.6f, -halfL + thickness * 0.5f), 2.2f, 3.4f, thickness, 90f);
            DungeonPropBuilder.MeshObject("Gable", root, new DungeonMeshBuilder(0.5f).Prism(thickness, new Vector2(-halfW, 0f), new Vector2(0f, 4f), new Vector2(halfW, 0f)).Save("ChapelGable"),
                Stone, new Vector3(0f, 7f, -halfL + thickness * 0.5f), new Vector3(0f, 90f, 0f));
            Wall(root, new Vector3(-3.5f, 0f, halfL - thickness * 0.5f), 4f, 4.2f, thickness, 90f);
            Wall(root, new Vector3(3.5f, 0f, halfL - thickness * 0.5f), 4f, 5.6f, thickness, 90f);
            Wall(root, new Vector3(0f, 0f, halfL - thickness * 0.5f), 3f, 1.6f, thickness, 90f);

            for (float z = -halfL + 3f; z < halfL - 3f; z += 4f)
            {
                if (random.NextDouble() < 0.55)
                    DungeonStructureBuilder.Block(root, "Beam", new Vector3(0f, 7.1f, z), new Vector3(halfW * 2f, 0.35f, 0.35f), DarkWood, 0f, false);
            }

            Tilted(root, "Fallen Beam", new Vector3(-3.2f, 0.2f, -2f), new Vector3(2.4f, 2.6f, 1.5f), 0.35f, 0.35f, DarkWood);
            Tilted(root, "Fallen Beam", new Vector3(4.6f, 0.2f, 6f), new Vector3(-0.5f, 3.4f, 4.2f), 0.35f, 0.35f, DarkWood);

            // Raised sanctuary: two shallow steps up to the altar.
            DungeonStructureBuilder.Block(root, "Step", new Vector3(0f, 0.1f, 7.6f), new Vector3(halfW * 2f - 1.6f, 0.2f, 5.2f), DungeonPropBuilder.Ashlar);
            DungeonStructureBuilder.Block(root, "Step", new Vector3(0f, 0.3f, 8.4f), new Vector3(halfW * 2f - 1.6f, 0.2f, 3.6f), DungeonPropBuilder.Ashlar);
            DungeonStructureBuilder.Block(root, "Altar", new Vector3(0f, 0.9f, 9f), new Vector3(2.6f, 1f, 1.1f), DungeonPropBuilder.Ashlar);
            Prop(DungeonKitBuilder.Load("CandleStick_Stand"), root, new Vector3(-1.9f, 0.4f, 9f), 0f);
            Prop(DungeonKitBuilder.Load("CandleStick_Stand"), root, new Vector3(1.9f, 0.4f, 9f), 0f);
            Prop(DungeonMedievalBuilder.Load("Goblet"), root, new Vector3(0.6f, 1.4f, 9f), 0f);
            site.Containers.Add(Container("LargeOakChest", root, new Vector3(0f, 0.4f, 7.9f), 180f));
            site.Containers.Add(Container("Coffin", root, new Vector3(-3.4f, 0f, 4.6f), 0f));

            for (float z = -7f; z < 4f; z += 2.2f)
            {
                foreach (float x in new[] { -2.4f, 2.4f })
                {
                    if (random.NextDouble() < 0.75)
                        Prop(DungeonKitBuilder.Load("Bench"), root, new Vector3(x + Rand(random, 0.2f), 0f, z), 90f + Rand(random, 14f));
                }
            }

            for (int i = 0; i < 9; i++)
                Place(DungeonVegetationBuilder.Load($"Stone{random.Next(4)}"), root, new Vector3(Rand(random, halfW - 1.2f), -0.1f, Rand(random, halfL - 1.5f)), random.Next(360));

            site.Spots.Add(root.TransformPoint(new Vector3(0f, 0f, 2f)));
            site.Spots.Add(root.TransformPoint(new Vector3(0f, 0.4f, 7f)));

            return root;
        }

        /// A small hut on piles over the swamp water, with a porch and a plank ramp down from the door.
        public static Transform StiltHut(Transform parent, Vector3 position, float yaw, int seed, float ground, Site site)
        {
            Transform root = House(parent, "Stilt Hut", position, yaw, new HouseSpec { Width = 4, Length = 6, Style = Style.Plaster, IsLit = seed % 2 == 0, HasChimney = false, Seed = seed }, site);
            float drop = position.y - ground + 0.6f;

            foreach (float x in new[] { -2f, 2f })
            {
                foreach (float z in new[] { -3f, 0f, 3f, 4.6f })
                    DungeonStructureBuilder.Block(root, "Pile", new Vector3(x * (z > 4f ? 0.6f : 1f), -drop * 0.5f, z), new Vector3(0.26f, drop, 0.26f), DarkWood, 0f, false);
            }

            DungeonStructureBuilder.Block(root, "Porch", new Vector3(0f, FloorTop - 0.08f, 3.9f), new Vector3(3.2f, 0.16f, 1.8f), Planks);
            Tilted(root, "Ramp", new Vector3(0f, FloorTop - 0.02f, 4.8f), new Vector3(0f, -drop + 0.55f, 4.8f + Mathf.Max(2.5f, drop * 3f)), 1.4f, 0.12f, Planks);

            return root;
        }

        /// Red portal down to floor 2 on a flagstone ring among broken standing stones and two braziers; local +Z is the way in.
        /// The portal shows only once the director opens it, the ring marks the place before that.
        public static PortalComponent PortalShrine(Transform parent, Vector3 position, float yaw)
        {
            System.Random random = new System.Random((int)position.x);
            Transform root = Root(parent, "Red Portal", position, yaw);
            DungeonPropBuilder.MeshObject("Floor", root, new DungeonMeshBuilder(0.5f).Cylinder(new Vector3(0f, -0.1f, 0f), ShrineRadius, 0.3f, 20).Save("PortalShrineFloor"), DungeonPropBuilder.Flagstone);

            for (int i = 0; i < 7; i++)
            {
                float angle = 50f + i * 43f + Rand(random, 6f);
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                float height = 1.4f + (float)random.NextDouble() * 1.6f;
                DungeonStructureBuilder.Block(root, "Standing Stone", direction * (ShrineRadius - 0.6f) + Vector3.up * (height * 0.5f - 0.1f), new Vector3(0.9f, height, 0.5f), DungeonVegetationBuilder.RockMaterial, angle + Rand(random, 8f));
            }

            Place(Load("Brazier"), root, new Vector3(-1.9f, 0.05f, 1.2f), 0f);
            Place(Load("Brazier"), root, new Vector3(1.9f, 0.05f, 1.2f), 0f);

            // Sunk a little so the pedestal's base is a ledge one walks onto.
            return Place(Load("DescendPortal"), root, new Vector3(0f, -0.1f, -0.6f), 0f).GetComponent<PortalComponent>();
        }

        /// Wooden bridge laid across from one end to the other: an arched deck of planks on posts with rails.
        public static void Bridge(Transform parent, Vector3 from, Vector3 to)
        {
            Transform root = BattleEditorUtility.CreateChild("Bridge", parent).transform;
            Vector3 flat = to - from;
            flat.y = 0f;
            Vector3 side = Vector3.Cross(Vector3.up, flat.normalized);
            int pieces = Mathf.CeilToInt(flat.magnitude / 1.5f);
            Vector3 previous = from;

            for (int i = 1; i <= pieces; i++)
            {
                float t = i / (float)pieces;
                Vector3 point = Vector3.Lerp(from, to, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.7f;
                Tilted(root, "Deck", previous, point, BridgeWidth, 0.2f, Planks);

                foreach (float s in new[] { -1f, 1f })
                {
                    Vector3 rail = side * (BridgeWidth * 0.5f - 0.1f) * s;
                    Tilted(root, "Rail", previous + rail + Vector3.up * 1f, point + rail + Vector3.up * 1f, 0.12f, 0.12f, DarkWood, false);
                    DungeonStructureBuilder.Block(root, "Post", point + rail + Vector3.up * 0.5f, new Vector3(0.14f, 1f, 0.14f), DarkWood, 0f, false);

                    if (i % 2 == 0 && i < pieces)
                        DungeonStructureBuilder.Block(root, "Pile", point + rail * 0.9f + Vector3.down * 1.6f, new Vector3(0.3f, 3.2f, 0.3f), DarkWood, 0f, false);
                }

                previous = point;
            }

            // Hand rails as colliders so nobody walks off the side into the river.
            foreach (float s in new[] { -1f, 1f })
                Tilted(root, "Rail Blocker", from + side * BridgeWidth * 0.5f * s + Vector3.up * 1.8f, to + side * BridgeWidth * 0.5f * s + Vector3.up * 1.8f, 0.15f, 3f, null);
        }

        /// Plank walk on short posts; it follows the ground where the ground is higher than the deck.
        public static void Boardwalk(Transform parent, VillageGround ground, IReadOnlyList<Vector2> path)
        {
            Transform root = BattleEditorUtility.CreateChild("Boardwalk", parent).transform;
            List<Vector2> line = VillageLayout.Smooth(path, 1.6f);
            Vector3 previous = Point(ground, line[0]);

            for (int i = 1; i < line.Count; i++)
            {
                Vector3 point = Point(ground, line[i]);
                Tilted(root, "Deck", previous, point, 2f, 0.14f, Planks);

                if (i % 2 == 0 && point.y - ground.Height(line[i]) > 0.25f)
                {
                    Vector3 side = Vector3.Cross(Vector3.up, (point - previous).normalized) * 0.85f;

                    foreach (Vector3 offset in new[] { side, -side })
                        DungeonStructureBuilder.Block(root, "Pile", point + offset + Vector3.down * 0.9f, new Vector3(0.16f, 1.8f, 0.16f), DarkWood, 0f, false);
                }

                previous = point;
            }
        }

        public static void Fence(Transform parent, Vector2 center, Vector2 size, float yaw, VillageGround ground, int gateSide)
        {
            Transform root = BattleEditorUtility.CreateChild("Fence", parent).transform;
            Quaternion turn = Quaternion.Euler(0f, yaw, 0f);

            for (int side = 0; side < 4; side++)
            {
                bool isLong = side % 2 == 0;
                float length = isLong ? size.x : size.y;
                float face = (isLong ? size.y : size.x) * 0.5f;
                int count = Mathf.RoundToInt(length / Module);
                Quaternion sideTurn = Quaternion.Euler(0f, yaw + side * 90f, 0f);

                for (int i = 0; i < count; i++)
                {
                    if (side == gateSide && Mathf.Abs(i - (count - 1) * 0.5f) < 1f)
                        continue;

                    Vector3 local = Quaternion.Euler(0f, side * 90f, 0f) * new Vector3(-length * 0.5f + Module * (i + 0.5f), 0f, face);
                    Vector3 world = turn * local + new Vector3(center.x, 0f, center.y);
                    world.y = ground.Height(world.x, world.z) - 0.03f;
                    Place(DungeonVillageKitBuilder.Load(i % 3 == 2 ? "Prop_WoodenFence_Extension1" : "Prop_WoodenFence_Single"), root, world, sideTurn.eulerAngles.y);
                }
            }
        }

        /// Low stone wall between two points on the ground, with a pillar at each end.
        public static void StoneWall(Transform parent, VillageGround ground, Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            int pieces = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / 4f));
            float yaw = Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg;

            for (int i = 0; i < pieces; i++)
            {
                Vector2 middle = from + delta * ((i + 0.5f) / pieces);
                float height = ground.Height(middle);
                DungeonStructureBuilder.Block(parent, "Wall", new Vector3(middle.x, height + 0.35f, middle.y), new Vector3(0.55f, 1.7f, delta.magnitude / pieces + 0.05f), Stone, yaw);
            }

            foreach (Vector2 end in new[] { from, to })
                DungeonStructureBuilder.Block(parent, "Wall Post", new Vector3(end.x, ground.Height(end) + 0.55f, end.y), new Vector3(0.8f, 2.3f, 0.8f), Stone, yaw);
        }

        public static GameObject Place(GameObject prefab, Transform parent, Vector3 localPosition, float yaw)
        {
            bool isNetworked = prefab.GetComponent<Fusion.NetworkObject>() != null;

            return DungeonMapBuilder.Place(prefab, parent, localPosition, yaw, !isNetworked);
        }

        public static GameObject Load(string name) => DungeonMapBuilder.Load(name);

        public static ContainerComponent Container(string name, Transform parent, Vector3 localPosition, float yaw)
        {
            return DungeonMapBuilder.Place(Load(name), parent, localPosition, yaw, false).GetComponent<ContainerComponent>();
        }

        /// Plank or beam stretched between two points, its top surface through them.
        public static GameObject Tilted(Transform parent, string name, Vector3 from, Vector3 to, float width, float thickness, Material material, bool hasCollider = true)
        {
            Vector3 delta = to - from;
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = (from + to) * 0.5f;
            go.transform.localRotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
            go.isStatic = true;
            Vector3 size = new Vector3(width, thickness, delta.magnitude + 0.04f);

            if (material != null)
            {
                Mesh mesh = new DungeonMeshBuilder(0.5f).Box(Vector3.down * thickness * 0.5f, size).Save($"Plank_{width:0.##}x{thickness:0.##}x{size.z:0.#}".Replace(',', '.'));
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            }

            if (hasCollider)
            {
                BoxCollider box = go.AddComponent<BoxCollider>();
                box.center = Vector3.down * thickness * 0.5f;
                box.size = size;
            }

            return go;
        }

        private static Vector3 Point(VillageGround ground, Vector2 point)
        {
            return new Vector3(point.x, Mathf.Max(ground.Height(point) + 0.06f, VillageLayout.DeckHeight), point.y);
        }

        private static string WallPiece(string prefix, System.Random random, bool isBlind, bool isUpper)
        {
            double roll = random.NextDouble();

            if (isBlind || roll < 0.42)
                return prefix == "Wall_Plaster_" && isUpper && roll < 0.2 ? "Wall_Plaster_WoodGrid" : prefix + "Straight";

            return prefix + (roll < 0.62 ? "Window_Wide_Flat" : roll < 0.82 ? "Window_Thin_Round" : "Window_Wide_Round");
        }

        /// The glazed frame of a window wall and maybe its shutters, open or shut; lit houses glow through the glass.
        private static void Window(Transform root, string wall, Vector3 local, float yaw, System.Random random, bool isLit)
        {
            string kind = wall.Substring(wall.IndexOf("Window_") + "Window_".Length);
            GameObject frame = Kit($"Window_{kind}1", root, local, yaw);

            if (isLit)
            {
                foreach (MeshRenderer renderer in frame.GetComponentsInChildren<MeshRenderer>())
                {
                    Material[] materials = renderer.sharedMaterials;

                    for (int i = 0; i < materials.Length; i++)
                    {
                        if (materials[i] != null && materials[i].name == "WindowDark")
                            materials[i] = DungeonVillageKitBuilder.LitWindow;
                    }

                    renderer.sharedMaterials = materials;
                }
            }

            double shutters = random.NextDouble();

            if (shutters < 0.6)
                Kit($"WindowShutters_{kind}_{(shutters < 0.25 || isLit ? "Open" : "Closed")}", root, local, yaw);
        }

        /// Plank floor on the ground and between storeys (with the stairwell left open), a ceiling under the roof.
        private static void Floors(Transform root, HouseSpec spec, bool hasStairs)
        {
            float ix = spec.Width * 0.5f - Inset + 0.05f;
            float iz = spec.Length * 0.5f - Inset + 0.05f;
            DungeonStructureBuilder.Block(root, "Floor", new Vector3(0f, FloorTop - 0.3f, 0f), new Vector3(ix * 2f, 0.6f, iz * 2f), Planks);

            for (int storey = 1; storey < spec.Storeys; storey++)
            {
                float y = storey * Storey;
                float run = DungeonStructureBuilder.StairRun(Storey);
                float x0 = -ix;
                float x1 = -ix + StairWidth + 0.1f;
                float z0 = -iz + 0.3f;
                float z1 = z0 + run;
                Slab(root, (x1 + ix) * 0.5f, 0f, ix - x1, iz * 2f, y);
                Slab(root, (x0 + x1) * 0.5f, (z1 + iz) * 0.5f, x1 - x0, iz - z1, y);
                Slab(root, (x0 + x1) * 0.5f, (-iz + z0) * 0.5f, x1 - x0, z0 + iz, y);
                DungeonStructureBuilder.Stairs(root, new Vector3(x0 + StairWidth * 0.5f + 0.05f, (storey - 1) * Storey + FloorTop, z0), 0f, StairWidth, Storey - FloorTop + 0.06f, DarkWood);
                DungeonStructureBuilder.Rail(root, new Vector3(x1, y + 0.06f, z0), new Vector3(x1, y + 0.06f, z1 - 0.6f));
                DungeonStructureBuilder.Rail(root, new Vector3(x0, y + 0.06f, z0), new Vector3(x1, y + 0.06f, z0));
            }

            Slab(root, 0f, 0f, ix * 2f, iz * 2f, spec.Storeys * Storey, "Ceiling");
        }

        private static void Slab(Transform root, float x, float z, float width, float length, float y, string name = "Floor")
        {
            if (width > 0.05f && length > 0.05f)
                DungeonStructureBuilder.Block(root, name, new Vector3(x, y - 0.06f, z), new Vector3(width, 0.24f, length), name == "Ceiling" ? DarkWood : Planks);
        }

        private static void Roof(Transform root, HouseSpec spec, float top, System.Random random)
        {
            int[] lengths = s_roofLengths[spec.Width];
            int length = lengths[^1];

            foreach (int option in lengths)
            {
                if (option >= spec.Length)
                {
                    length = option;
                    break;
                }
            }

            Kit($"Roof_RoundTiles_{spec.Width}x{length}", root, new Vector3(0f, top, 0f), 0f);
            Kit($"Roof_Front_Brick{spec.Width}", root, new Vector3(0f, top, spec.Length * 0.5f), 0f);
            Kit($"Roof_Front_Brick{spec.Width}", root, new Vector3(0f, top, -spec.Length * 0.5f), 180f);

            if (spec.HasChimney)
                Kit(random.NextDouble() < 0.5 ? "Prop_Chimney" : "Prop_Chimney2", root, new Vector3(spec.Width * 0.5f - 1.3f, top + 0.4f, -spec.Length * 0.25f), 0f);
        }

        /// Furniture along the walls and loot: table and chairs, a cupboard or shelves, a bed (upstairs if there is one),
        /// a chest and barrels. The stairwell (west wall) and the way from the door stay free.
        private static void Furnish(Transform root, HouseSpec spec, System.Random random, Site site, bool hasStairs)
        {
            float ix = spec.Width * 0.5f - Inset;
            float iz = spec.Length * 0.5f - Inset;
            float table = hasStairs ? 0.6f : 0f;
            Prop(DungeonKitBuilder.Load("Table_Large"), root, new Vector3(table + 0.3f, FloorTop, -0.4f), 90f);
            Prop(DungeonKitBuilder.Load("Chair_1"), root, new Vector3(table + 0.3f, FloorTop, -1.5f), 0f);
            Prop(DungeonKitBuilder.Load("Stool"), root, new Vector3(table + 0.3f, FloorTop, 0.8f), 0f);
            Prop(DungeonKitBuilder.Load(random.NextDouble() < 0.5 ? "Cabinet" : "Bookcase_2"), root, new Vector3(ix - 0.35f, FloorTop, -iz + 1.2f), -90f);

            if (spec.IsLit)
                Prop(DungeonKitBuilder.Load("CandleStick_Triple"), root, new Vector3(table + 0.3f, FloorTop + 0.8f, -0.3f), 0f);
            else
                Prop(DungeonKitBuilder.Load(random.NextDouble() < 0.5 ? "Mug" : "Table_Plate"), root, new Vector3(table + 0.2f, FloorTop + 0.8f, -0.2f), random.Next(360));

            site.Containers.Add(Container(Chest(spec.Loot, random), root, new Vector3(ix - 0.5f, FloorTop, iz - 1.4f), -90f));

            if (!hasStairs)
            {
                Prop(DungeonKitBuilder.Load("Bed_Twin1"), root, new Vector3(-ix + 0.6f, FloorTop, -iz + 1.1f), 90f);
                site.Containers.Add(Container("Barrel", root, new Vector3(-ix + 0.5f, FloorTop, iz - 0.6f), random.Next(360)));

                return;
            }

            float upper = Storey + FloorTop - 0.06f;
            Prop(DungeonKitBuilder.Load("Bed_Twin2"), root, new Vector3(ix - 1f, upper, -iz + 1.1f), 0f);
            Prop(DungeonKitBuilder.Load("Nightstand_Shelf"), root, new Vector3(ix - 0.35f, upper, 0.4f), -90f);
            site.Containers.Add(Container("Barrel", root, new Vector3(ix - 0.5f, FloorTop, 0.6f), random.Next(360)));
            site.Containers.Add(Container(spec.Loot == LootTier.Rich ? Chest(spec.Loot, random) : random.NextDouble() < 0.5 ? "Crate" : "SmallOakChest", root, new Vector3(0.6f, upper, iz - 0.6f), 180f));
            site.Spots.Add(root.TransformPoint(new Vector3(0.5f, upper, 0f)));
        }

        /// One side of a windmill storey with an opening (a doorway when its sill is 0); side 0 is the front, then east, back, west.
        /// The side walls fit between the front and back ones, so no faces meet at the corners.
        private static void MillSide(Transform root, int side, float y, float center, float width, float sill, float head, Material material)
        {
            float inset = side % 2 == 1 ? MillWall : 0f;
            float from = -MillHalf + inset;
            float to = MillHalf - inset;
            Quaternion turn = Quaternion.Euler(0f, side * 90f, 0f);

            void Segment(float a, float b, float bottom, float up)
            {
                if (b - a > 0.05f && up - bottom > 0.05f)
                    DungeonStructureBuilder.Block(root, "Wall", turn * new Vector3((a + b) * 0.5f, 0f, MillHalf - MillWall * 0.5f) + Vector3.up * (y + (bottom + up) * 0.5f), new Vector3(b - a, up - bottom, MillWall), material, side * 90f);
            }

            if (width <= 0f)
            {
                Segment(from, to, 0f, MillStorey);

                return;
            }

            Segment(from, center - width * 0.5f, 0f, MillStorey);
            Segment(center + width * 0.5f, to, 0f, MillStorey);
            Segment(center - width * 0.5f, center + width * 0.5f, 0f, sill);
            Segment(center - width * 0.5f, center + width * 0.5f, head, MillStorey);
        }

        /// Four lattice sails on a hub, one of them broken off short.
        private static void Sails(Transform root, Vector3 position)
        {
            Transform hub = BattleEditorUtility.CreateChild("Sails", root, position).transform;
            hub.localRotation = Quaternion.Euler(0f, 0f, 20f);
            DungeonStructureBuilder.Block(hub, "Hub", Vector3.zero, new Vector3(0.6f, 0.6f, 0.8f), DarkWood, 0f, false);

            for (int i = 0; i < 4; i++)
            {
                float length = i == 2 ? 3.2f : 7.5f;
                Transform arm = BattleEditorUtility.CreateChild("Sail", hub).transform;
                arm.localRotation = Quaternion.Euler(0f, 0f, i * 90f);
                DungeonStructureBuilder.Block(arm, "Spar", new Vector3(0f, length * 0.5f + 0.3f, 0.2f), new Vector3(0.22f, length, 0.22f), DarkWood, 0f, false);

                for (float y = 1.4f; y < length; y += 0.9f)
                    DungeonStructureBuilder.Block(arm, "Lath", new Vector3(0.75f, y, 0.25f), new Vector3(1.5f, 0.08f, 0.06f), DarkWood, 0f, false);

                DungeonStructureBuilder.Block(arm, "Lath", new Vector3(1.5f, (length + 1.4f) * 0.5f, 0.25f), new Vector3(0.08f, length - 1.4f, 0.06f), DarkWood, 0f, false);
            }
        }

        /// The house chest by loot tier: a crate or a small chest on the farm, oak chests in the woods, a golden one now and then in the village.
        private static string Chest(LootTier tier, System.Random random)
        {
            double roll = random.NextDouble();

            return tier switch
            {
                LootTier.Poor => roll < 0.5 ? "Crate" : "SmallOakChest",
                LootTier.Rich => roll < 0.15 ? "GoldenChest" : roll < 0.6 ? "LargeOakChest" : "SmallOakChest",
                _ => roll < 0.7 ? "SmallOakChest" : "LargeOakChest"
            };
        }

        private static void Wall(Transform root, Vector3 basePoint, float length, float height, float thickness, float yaw = 0f)
        {
            // Length runs along Z for the side walls; yaw 90 turns it along X.
            DungeonStructureBuilder.Block(root, "Wall", basePoint + Vector3.up * height * 0.5f, new Vector3(thickness, height, length), Stone, yaw);
        }

        private static Transform Root(Transform parent, string name, Vector3 position, float yaw)
        {
            Transform root = BattleEditorUtility.CreateChild(name, parent, position).transform;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            root.gameObject.isStatic = true;

            return root;
        }

        private static GameObject Kit(string name, Transform parent, Vector3 localPosition, float yaw)
        {
            return DungeonMapBuilder.Place(DungeonVillageKitBuilder.Load(name), parent, localPosition, yaw);
        }

        private static GameObject Prop(GameObject prefab, Transform parent, Vector3 localPosition, float yaw)
        {
            return DungeonMapBuilder.Place(prefab, parent, localPosition, yaw);
        }

        private static void SavePiece(string name, DungeonMeshBuilder builder, Material material)
        {
            GameObject root = new GameObject(name);
            Mesh mesh = builder.Save($"Village{name}");
            DungeonPropBuilder.MeshObject("Model", root.transform, mesh, material, default, default, false);
            BoxCollider box = root.AddComponent<BoxCollider>();
            box.center = mesh.bounds.center;
            box.size = mesh.bounds.size;
            PrefabUtility.SaveAsPrefabAsset(root, $"{DungeonVillageKitBuilder.PrefabsFolder}/{name}.prefab");
            Object.DestroyImmediate(root);
        }

        private static float Rand(System.Random random, float range)
        {
            return ((float)random.NextDouble() * 2f - 1f) * range;
        }
    }
}
