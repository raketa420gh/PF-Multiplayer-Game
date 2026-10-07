using System.Collections.Generic;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Scripts.Editor.Dungeon
{
    /// The village floor's land: a Unity terrain painted with grass, road dirt, swamp mud, forest litter, cliff rock and cobbles,
    /// grass, reeds and dead crops as details, forests of spruces and broadleaves, dead trees in the swamp and the graveyard,
    /// boulders, murky water in every hollow below the water level, invisible walls along the banks and round the map.
    internal static class VillageTerrainBuilder
    {
        public const string DataPath = "Assets/Game/Scenes/DungeonScene/VillageTerrain.asset";
        private const string LayersFolder = DungeonContentBuilder.ConfigsFolder + "/Terrain";
        private const int AlphaResolution = 1024;
        private const int DetailResolution = 1024;
        private const float TreeStep = 5.5f;

        private enum Layer
        {
            Grass,
            Dirt,
            Mud,
            Forest,
            Rock,
            Cobble
        }

        /// Areas the terrain leaves open: building plots and their yards, holes for the cellar pits.
        public sealed class Clearings
        {
            public readonly List<(Vector2 center, float radius)> Open = new();
            public readonly List<(Vector2 center, Vector2 halfSize, float yaw)> Holes = new();
        }

        public static Terrain Build(Transform parent, VillageGround ground, Clearings clearings, List<NavMeshBuildSource> sources)
        {
            BattleEditorUtility.EnsureFolder(LayersFolder);
            TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(DataPath);

            if (data == null)
            {
                data = new TerrainData();
                AssetDatabase.CreateAsset(data, DataPath);
            }

            data.heightmapResolution = VillageGround.Resolution;
            data.size = new Vector3(VillageLayout.Size, VillageGround.Depth, VillageLayout.Size);
            data.SetHeights(0, 0, ground.Normalized());
            data.alphamapResolution = AlphaResolution;
            data.terrainLayers = new[]
            {
                TerrainLayer("Grass", 0.05f), TerrainLayer("Dirt", 0.1f), TerrainLayer("Mud", 0.2f), TerrainLayer("ForestFloor", 0.08f),
                TerrainLayer("Rock", 0.12f), TerrainLayer("Cobble", 0.15f)
            };
            Paint(data, ground);
            Holes(data, ground, clearings);
            Details(data, ground, clearings);
            Trees(data, ground, clearings, sources);
            EditorUtility.SetDirty(data);

            GameObject go = Terrain.CreateTerrainGameObject(data);
            go.name = "Terrain";
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(-VillageLayout.Half, VillageGround.Bottom, -VillageLayout.Half);
            go.isStatic = true;
            Terrain terrain = go.GetComponent<Terrain>();
            terrain.materialTemplate = TerrainMaterial();
            terrain.heightmapPixelError = 4f;
            terrain.basemapDistance = 160f;
            terrain.treeDistance = 240f;
            terrain.treeBillboardDistance = 240f;
            terrain.treeMaximumFullLODCount = 2000;
            terrain.detailObjectDistance = 70f;
            terrain.detailObjectDensity = 1f;
            terrain.drawInstanced = true;
            terrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

            Water(parent, ground, clearings);
            Walls(parent, ground);

            return terrain;
        }

        private static TerrainLayer TerrainLayer(string texture, float smoothness)
        {
            string path = $"{LayersFolder}/{texture}.terrainlayer";
            TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);

            if (layer == null)
            {
                layer = new TerrainLayer();
                AssetDatabase.CreateAsset(layer, path);
            }

            layer.diffuseTexture = DungeonTextureBuilder.Load(texture, false);
            layer.normalMapTexture = DungeonTextureBuilder.Load(texture, true);
            layer.normalScale = 1f;
            layer.tileSize = new Vector2(4f, 4f);
            layer.smoothness = smoothness;
            layer.metallic = 0f;
            EditorUtility.SetDirty(layer);

            return layer;
        }

        private static Material TerrainMaterial()
        {
            string path = $"{DungeonPropBuilder.MaterialsFolder}/VillageTerrain.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(material);

            return material;
        }

        /// Layer weights from what the ground is: rock on steep slopes and hills, mud by the water and in the swamp, dirt on roads
        /// and round buildings, cobbles on the square, forest litter in the woods, grass elsewhere.
        private static void Paint(TerrainData data, VillageGround ground)
        {
            int layers = data.terrainLayers.Length;
            float[,,] maps = new float[AlphaResolution, AlphaResolution, layers];
            float step = VillageLayout.Size / AlphaResolution;
            float[] weights = new float[layers];

            for (int z = 0; z < AlphaResolution; z++)
            {
                for (int x = 0; x < AlphaResolution; x++)
                {
                    float wx = (x + 0.5f) * step - VillageLayout.Half;
                    float wz = (z + 0.5f) * step - VillageLayout.Half;
                    float height = ground.Height(wx, wz);
                    float noise = DungeonTextureBuilder.Noise(wx / 540f + 2f, wz / 540f + 4f, 40f, 3);
                    float swamp = VillageLayout.SwampShare(wx, wz);
                    float slope = ground.Slope(wx, wz);
                    float road = DungeonTextureBuilder.Step(0.8f + noise * 1.6f, -0.6f, ground.RoadDistance(wx, wz));
                    float pad = ground.PadMask(wx, wz);
                    float wet = DungeonTextureBuilder.Step(VillageLayout.WaterLevel + 0.6f, VillageLayout.WaterLevel + 0.05f, height);
                    float rock = Mathf.Max(DungeonTextureBuilder.Step(28f, 38f, slope), DungeonTextureBuilder.Step(14f, 4f, VillageLayout.EdgeDistance(wx, wz)));
                    float forest = Forest(wx, wz) * (0.6f + noise * 0.6f);
                    float plaza = DungeonTextureBuilder.Step(VillageLayout.PlazaRadius + 1f, VillageLayout.PlazaRadius - 1f, (new Vector2(wx, wz) - VillageLayout.Plaza).magnitude);
                    float field = InField(wx, wz, 0f) ? 1f : 0f;

                    System.Array.Clear(weights, 0, layers);
                    weights[(int)Layer.Grass] = 1f;
                    weights[(int)Layer.Forest] = forest * 1.4f;
                    weights[(int)Layer.Mud] = Mathf.Max(wet, swamp * (0.5f + noise)) * 2f;
                    weights[(int)Layer.Dirt] = Mathf.Max(road * 3f, pad * (0.6f + noise * 0.8f), field * 1.2f);
                    weights[(int)Layer.Cobble] = plaza * 4f;
                    weights[(int)Layer.Rock] = rock * 5f;
                    float total = 0f;

                    foreach (float weight in weights)
                        total += weight;

                    for (int i = 0; i < layers; i++)
                        maps[z, x, i] = weights[i] / total;
                }
            }

            data.SetAlphamaps(0, 0, maps);
        }

        private static void Holes(TerrainData data, VillageGround ground, Clearings clearings)
        {
            int resolution = data.holesResolution;
            bool[,] solid = new bool[resolution, resolution];
            float step = VillageLayout.Size / resolution;

            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                    solid[z, x] = true;
            }

            foreach ((Vector2 center, Vector2 halfSize, float yaw) in clearings.Holes)
            {
                Quaternion inverse = Quaternion.Euler(0f, -yaw, 0f);
                float reach = halfSize.magnitude + 1f;

                for (int z = 0; z < resolution; z++)
                {
                    float wz = (z + 0.5f) * step - VillageLayout.Half;

                    if (Mathf.Abs(wz - center.y) > reach)
                        continue;

                    for (int x = 0; x < resolution; x++)
                    {
                        float wx = (x + 0.5f) * step - VillageLayout.Half;

                        if (Mathf.Abs(wx - center.x) > reach)
                            continue;

                        Vector3 local = inverse * new Vector3(wx - center.x, 0f, wz - center.y);

                        if (Mathf.Abs(local.x) < halfSize.x && Mathf.Abs(local.z) < halfSize.y)
                            solid[z, x] = false;
                    }
                }
            }

            data.SetHoles(0, 0, solid);
        }

        /// Grass on meadows and forest edges, reeds where the ground meets the water, dead crops in rows on the fields.
        private static void Details(TerrainData data, VillageGround ground, Clearings clearings)
        {
            data.SetDetailResolution(DetailResolution, 16);
            data.detailPrototypes = new[]
            {
                Detail("GrassBlades", 0.7f, 1.1f, 0.35f, 0.6f, new Color(0.75f, 0.8f, 0.6f), new Color(0.85f, 0.75f, 0.55f)),
                Detail("Reeds", 0.9f, 1.4f, 1.1f, 1.8f, new Color(0.8f, 0.8f, 0.65f), new Color(0.75f, 0.65f, 0.5f)),
                Detail("DeadCrops", 0.6f, 0.9f, 0.8f, 1.2f, new Color(0.9f, 0.85f, 0.7f), new Color(0.8f, 0.7f, 0.55f))
            };
            int[][,] layers = { new int[DetailResolution, DetailResolution], new int[DetailResolution, DetailResolution], new int[DetailResolution, DetailResolution] };
            float step = VillageLayout.Size / DetailResolution;
            System.Random random = new System.Random(7);

            for (int z = 0; z < DetailResolution; z++)
            {
                for (int x = 0; x < DetailResolution; x++)
                {
                    float wx = (x + 0.5f) * step - VillageLayout.Half;
                    float wz = (z + 0.5f) * step - VillageLayout.Half;
                    float height = ground.Height(wx, wz);

                    if (height < VillageLayout.WaterLevel - 0.25f || ground.PadMask(wx, wz) > 0.2f || ground.RoadDistance(wx, wz) < 0.5f || IsCleared(clearings, wx, wz))
                        continue;

                    float noise = DungeonTextureBuilder.Noise(wx / 540f + 6f, wz / 540f + 1f, 50f, 3);
                    float shore = DungeonTextureBuilder.Step(VillageLayout.WaterLevel + 0.7f, VillageLayout.WaterLevel - 0.1f, height);

                    if (InField(wx, wz, -1f))
                    {
                        // Rows run along the field's length; furrows between them stay bare.
                        layers[2][z, x] = Mathf.Repeat(wx, 1.2f) < 0.55f ? 3 + random.Next(3) : 0;
                        continue;
                    }

                    if (shore > 0.3f && noise > 0.35f)
                    {
                        layers[1][z, x] = 1 + random.Next(4);
                        continue;
                    }

                    float density = (1f - Forest(wx, wz) * 0.6f) * DungeonTextureBuilder.Step(0.35f, 0.7f, noise) * (1f - VillageLayout.SwampShare(wx, wz) * 0.5f);

                    if (VillageLayout.Graveyard.Contains(new Vector2(wx, wz)))
                        density *= 0.6f;

                    layers[0][z, x] = Mathf.RoundToInt(density * 5f * (float)random.NextDouble());
                }
            }

            for (int i = 0; i < layers.Length; i++)
                data.SetDetailLayer(0, 0, i, layers[i]);
        }

        private static DetailPrototype Detail(string texture, float minWidth, float maxWidth, float minHeight, float maxHeight, Color healthy, Color dry)
        {
            return new DetailPrototype
            {
                prototypeTexture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{DungeonTextureBuilder.Folder}/{texture}.png"),
                renderMode = DetailRenderMode.Grass,
                minWidth = minWidth,
                maxWidth = maxWidth,
                minHeight = minHeight,
                maxHeight = maxHeight,
                healthyColor = healthy,
                dryColor = dry,
                noiseSpread = 0.3f,
                usePrototypeMesh = false,
                useInstancing = false
            };
        }

        /// Forests fill everything outside the settled places; the swamp and the graveyard get dead trees, the farm and the
        /// village a few old broadleaves. Every trunk is also a NavMesh obstacle.
        private static void Trees(TerrainData data, VillageGround ground, Clearings clearings, List<NavMeshBuildSource> sources)
        {
            List<TreePrototype> prototypes = new();
            int[] spruce = new int[DungeonVegetationBuilder.Variants];
            int[] broadleaf = new int[DungeonVegetationBuilder.Variants];
            int[] dead = new int[DungeonVegetationBuilder.Variants];
            int[] bush = new int[DungeonVegetationBuilder.Variants];

            for (int i = 0; i < DungeonVegetationBuilder.Variants; i++)
            {
                spruce[i] = Prototype(prototypes, $"Spruce{i}");
                broadleaf[i] = Prototype(prototypes, $"Broadleaf{i}");
                dead[i] = Prototype(prototypes, $"DeadTree{i}");
                bush[i] = Prototype(prototypes, $"Bush{i}");
            }

            data.treePrototypes = prototypes.ToArray();
            List<TreeInstance> trees = new();
            System.Random random = new System.Random(11);

            for (float wz = -VillageLayout.Half + 4f; wz < VillageLayout.Half - 4f; wz += TreeStep)
            {
                for (float wx = -VillageLayout.Half + 4f; wx < VillageLayout.Half - 4f; wx += TreeStep)
                {
                    float x = wx + ((float)random.NextDouble() - 0.5f) * TreeStep * 0.9f;
                    float z = wz + ((float)random.NextDouble() - 0.5f) * TreeStep * 0.9f;
                    float roll = (float)random.NextDouble();
                    float height = ground.Height(x, z);
                    float forest = Forest(x, z);
                    float swamp = VillageLayout.SwampShare(x, z);
                    bool isWet = height < VillageLayout.WaterLevel + 0.15f;

                    if (ground.RoadDistance(x, z) < 2.5f || ground.PadMask(x, z) > 0.05f || IsCleared(clearings, x, z) || InField(x, z, 4f)
                        || ground.WaterDistance(x, z) < 6f || VillageLayout.EdgeDistance(x, z) < 6f)
                        continue;

                    int prototype;

                    if (swamp > 0.5f)
                    {
                        if (roll > 0.07f + forest * 0.25f)
                            continue;

                        prototype = isWet || random.NextDouble() < 0.55 ? dead[random.Next(dead.Length)] : random.NextDouble() < 0.5 ? broadleaf[random.Next(broadleaf.Length)] : bush[random.Next(bush.Length)];
                    }
                    else if (isWet)
                    {
                        continue;
                    }
                    else if (VillageLayout.Graveyard.Contains(new Vector2(x, z)))
                    {
                        if (roll > 0.03f)
                            continue;

                        prototype = dead[random.Next(dead.Length)];
                    }
                    else if (forest > 0.5f)
                    {
                        if (roll > 0.42f + forest * 0.35f)
                            continue;

                        double kind = random.NextDouble();
                        prototype = kind < 0.62 ? spruce[random.Next(spruce.Length)] : kind < 0.84 ? broadleaf[random.Next(broadleaf.Length)] : kind < 0.93 ? bush[random.Next(bush.Length)] : dead[random.Next(dead.Length)];
                    }
                    else
                    {
                        if (roll > 0.035f)
                            continue;

                        prototype = random.NextDouble() < 0.5 ? broadleaf[random.Next(broadleaf.Length)] : bush[random.Next(bush.Length)];
                    }

                    float scale = 0.8f + (float)random.NextDouble() * 0.45f;
                    trees.Add(new TreeInstance
                    {
                        prototypeIndex = prototype,
                        position = new Vector3((x + VillageLayout.Half) / VillageLayout.Size, (height - VillageGround.Bottom) / VillageGround.Depth, (z + VillageLayout.Half) / VillageLayout.Size),
                        widthScale = scale,
                        heightScale = scale * (0.9f + (float)random.NextDouble() * 0.2f),
                        rotation = (float)random.NextDouble() * Mathf.PI * 2f,
                        color = Color.Lerp(Color.white, new Color(0.8f, 0.78f, 0.7f), (float)random.NextDouble()),
                        lightmapColor = Color.white
                    });

                    CapsuleCollider capsule = prototypes[prototype].prefab.GetComponent<CapsuleCollider>();

                    if (capsule != null)
                    {
                        sources.Add(new NavMeshBuildSource
                        {
                            shape = NavMeshBuildSourceShape.Capsule,
                            transform = Matrix4x4.TRS(new Vector3(x, height + 2.5f, z), Quaternion.identity, Vector3.one),
                            size = new Vector3(capsule.radius * 2f * scale, 6f, capsule.radius * 2f * scale),
                            area = 1
                        });
                    }
                }
            }

            data.SetTreeInstances(trees.ToArray(), false);
        }

        private static int Prototype(List<TreePrototype> prototypes, string name)
        {
            prototypes.Add(new TreePrototype { prefab = DungeonVegetationBuilder.Load(name), bendFactor = 0f });

            return prototypes.Count - 1;
        }

        /// 1 deep in the woods, 0 in the settled places (farm, village, graveyard, chapel), the swamp counts its own way.
        public static float Forest(float x, float z)
        {
            Vector2 p = new Vector2(x, z);
            float farm = DungeonTextureBuilder.Step(VillageLayout.FarmRadius - 12f, VillageLayout.FarmRadius + 4f, (p - VillageLayout.FarmCenter).magnitude);
            float village = DungeonTextureBuilder.Step(VillageLayout.VillageRadius - 14f, VillageLayout.VillageRadius + 2f, (p - VillageLayout.VillageCenter).magnitude);
            Rect yard = VillageLayout.Graveyard;
            float outside = new Vector2(Mathf.Max(yard.xMin - x, x - yard.xMax, 0f), Mathf.Max(yard.yMin - z, z - yard.yMax, 0f)).magnitude;
            float graveyard = DungeonTextureBuilder.Step(2f, 14f, outside);
            float clumps = DungeonTextureBuilder.Step(0.24f, 0.4f, DungeonTextureBuilder.Noise(x / 540f + 8f, z / 540f + 8f, 10f, 3));

            return farm * village * graveyard * Mathf.Max(clumps, DungeonTextureBuilder.Step(60f, 30f, VillageLayout.EdgeDistance(x, z)));
        }

        public static bool InField(float x, float z, float margin)
        {
            foreach ((Vector2 center, Vector2 size, float yaw) in VillageLayout.Fields)
            {
                Vector3 local = Quaternion.Euler(0f, -yaw, 0f) * new Vector3(x - center.x, 0f, z - center.y);

                if (Mathf.Abs(local.x) < size.x * 0.5f + margin && Mathf.Abs(local.z) < size.y * 0.5f + margin)
                    return true;
            }

            return false;
        }

        private static bool IsCleared(Clearings clearings, float x, float z)
        {
            foreach ((Vector2 center, float radius) in clearings.Open)
            {
                if ((new Vector2(x, z) - center).sqrMagnitude < radius * radius)
                    return true;
            }

            return false;
        }

        /// Flat dark water over every 2 m cell that dips below the water level, one mesh per map module for culling.
        private static void Water(Transform parent, VillageGround ground, Clearings clearings)
        {
            const float cell = 2f;
            int perModule = Mathf.RoundToInt(VillageLayout.Size / VillageLayout.Grid / cell);
            Material material = WaterMaterial();
            Transform root = BattleEditorUtility.CreateChild("Water", parent).transform;

            for (int mz = 0; mz < VillageLayout.Grid; mz++)
            {
                for (int mx = 0; mx < VillageLayout.Grid; mx++)
                {
                    DungeonMeshBuilder builder = new DungeonMeshBuilder(0.25f);

                    for (int cz = 0; cz < perModule; cz++)
                    {
                        for (int cx = 0; cx < perModule; cx++)
                        {
                            float x = -VillageLayout.Half + (mx * perModule + cx) * cell;
                            float z = -VillageLayout.Half + (mz * perModule + cz) * cell;
                            float lowest = Mathf.Min(Mathf.Min(ground.Height(x, z), ground.Height(x + cell, z)), Mathf.Min(ground.Height(x, z + cell), ground.Height(x + cell, z + cell)));

                            if (lowest > VillageLayout.WaterLevel - 0.02f || IsHole(clearings, x + cell * 0.5f, z + cell * 0.5f))
                                continue;

                            builder.Quad(new Vector3(x + cell * 0.5f, VillageLayout.WaterLevel, z + cell * 0.5f), Vector3.up, Vector3.right * cell * 0.5f, Vector3.forward * cell * 0.5f,
                                new Vector2(x, z));
                        }
                    }

                    if (builder.IsEmpty)
                        continue;

                    GameObject water = DungeonPropBuilder.MeshObject($"Water{mx}{mz}", root, builder.Save($"VillageWater{mx}{mz}"), material, default, default, false);
                    water.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
        }

        private static bool IsHole(Clearings clearings, float x, float z)
        {
            foreach ((Vector2 center, Vector2 halfSize, float yaw) in clearings.Holes)
            {
                Vector3 local = Quaternion.Euler(0f, -yaw, 0f) * new Vector3(x - center.x, 0f, z - center.y);

                if (Mathf.Abs(local.x) < halfSize.x + 2f && Mathf.Abs(local.z) < halfSize.y + 2f)
                    return true;
            }

            return false;
        }

        private static Material WaterMaterial()
        {
            string path = $"{DungeonPropBuilder.MaterialsFolder}/VillageWater.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", new Color(0.02f, 0.026f, 0.022f));
            material.SetFloat("_Smoothness", 0.9f);
            material.SetFloat("_Metallic", 0f);
            material.SetTexture("_BumpMap", DungeonTextureBuilder.Load("Mud", true));
            material.SetFloat("_BumpScale", 0.15f);
            material.EnableKeyword("_NORMALMAP");
            material.SetTextureScale("_BaseMap", Vector2.one * 0.5f);
            EditorUtility.SetDirty(material);

            return material;
        }

        /// The river and the stream cannot be waded: invisible walls follow both banks at the waterline, open under the bridges
        /// and where the water spreads into the swamp. The map ends in walls at the foot of the hills.
        private static void Walls(Transform parent, VillageGround ground)
        {
            Transform root = BattleEditorUtility.CreateChild("Invisible Walls", parent).transform;
            List<Vector2> bridges = new();

            foreach (Vector2 point in VillageLayout.RiverBridges)
                bridges.Add(VillageLayout.Nearest(ground.River, point, out _));

            foreach (Vector2 point in VillageLayout.StreamBridges)
                bridges.Add(VillageLayout.Nearest(ground.Stream, point, out _));

            BankWalls(root, ground, ground.River, VillageLayout.RiverWidth, bridges);
            BankWalls(root, ground, ground.Stream, VillageLayout.StreamWidth, bridges);

            for (int i = 0; i < 4; i++)
            {
                Quaternion side = Quaternion.Euler(0f, i * 90f, 0f);
                GameObject wall = BattleEditorUtility.CreateChild("Boundary", root, side * new Vector3(0f, 20f, VillageLayout.Playable));
                wall.transform.localRotation = side;
                wall.AddComponent<BoxCollider>().size = new Vector3(VillageLayout.Playable * 2f + 2f, 60f, 1f);
                wall.isStatic = true;
            }
        }

        private static void BankWalls(Transform root, VillageGround ground, IReadOnlyList<Vector2> line, float width, List<Vector2> bridges)
        {
            float offset = width * 0.5f + 3.6f;

            for (int i = 0; i < line.Count - 1; i++)
            {
                Vector2 a = line[i];
                Vector2 b = line[i + 1];
                Vector2 middle = (a + b) * 0.5f;
                Vector2 direction = (b - a).normalized;
                Vector2 normal = new Vector2(-direction.y, direction.x);

                if (VillageLayout.SwampShare(middle.x, middle.y) > 0.25f || VillageLayout.EdgeDistance(middle.x, middle.y) < 10f || bridges.Exists(bridge => (bridge - middle).magnitude < VillageArchitectureBuilder.BridgeWidth * 0.5f + 1.5f))
                    continue;

                foreach (float side in new[] { -1f, 1f })
                {
                    Vector2 point = middle + normal * offset * side;
                    float height = ground.Height(point);
                    GameObject wall = BattleEditorUtility.CreateChild("Bank", root, new Vector3(point.x, height + 1f, point.y));
                    wall.transform.localRotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.y));
                    wall.AddComponent<BoxCollider>().size = new Vector3(0.4f, 5f, (b - a).magnitude + 0.3f);
                    wall.isStatic = true;
                }
            }
        }
    }
}
