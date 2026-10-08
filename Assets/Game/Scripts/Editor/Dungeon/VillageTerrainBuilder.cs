using System.Collections.Generic;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Scripts.Editor.Dungeon
{
    /// The village floor's land: a Unity terrain painted with grass, road dirt, swamp mud, forest litter, cliff rock, cobbles and
    /// puddles, grass, reeds and dead crops as details, closed land under dense forest with a rim of thicket round every open
    /// place, copses inside the wild ones, dead trees in the swamp and the graveyard, murky water in every hollow below the water
    /// level, invisible walls along the banks, round the open ground and round the map; the closed land is cut out of the NavMesh.
    internal static class VillageTerrainBuilder
    {
        public const string DataPath = "Assets/Game/Scenes/DungeonScene/VillageTerrain.asset";
        private const string LayersFolder = DungeonContentBuilder.ConfigsFolder + "/Terrain";
        private const int AlphaResolution = 1024;
        private const int DetailResolution = 1024;
        private const float TreeStep = 4.6f;
        private const float ThicketStep = 2.6f;
        private const float RimCell = 2f;
        private const float NavCell = 8f;

        private enum Layer
        {
            Grass,
            Dirt,
            Mud,
            Forest,
            Rock,
            Cobble,
            Puddle
        }

        /// Areas the terrain leaves open: building plots and their yards, holes cut through the terrain.
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
                TerrainLayer("Grass", "Grass", 0.08f), TerrainLayer("Dirt", "Dirt", 0.3f), TerrainLayer("Mud", "Mud", 0.55f), TerrainLayer("ForestFloor", "ForestFloor", 0.1f),
                TerrainLayer("Rock", "Rock", 0.18f), TerrainLayer("Cobble", "Cobble", 0.4f), TerrainLayer("Puddle", "Mud", 0.93f, 0.35f, 0.25f)
            };
            Paint(data, ground);
            Holes(data, clearings);
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
            // The mist swallows everything past ~110 m: nothing beyond it is drawn.
            terrain.heightmapPixelError = 5f;
            terrain.basemapDistance = 120f;
            terrain.treeDistance = 130f;
            terrain.treeBillboardDistance = 130f;
            terrain.treeMaximumFullLODCount = 4000;
            terrain.detailObjectDistance = 65f;
            terrain.detailObjectDensity = 1f;
            terrain.drawInstanced = true;
            terrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

            Water(parent, ground, clearings);
            Walls(parent, ground);
            CloseNavMesh(ground, sources);

            return terrain;
        }

        /// 1 in the closed land and its thicket rim, copses in the open wild places (not the village streets or the graveyard).
        public static float Forest(VillageGround ground, float x, float z)
        {
            float closed = DungeonTextureBuilder.Step(-4f, 2f, ground.Open(x, z));
            float copse = DungeonTextureBuilder.Step(0.6f, 0.68f, DungeonTextureBuilder.Noise(x / 540f + 8f, z / 540f + 8f, 16f, 3));
            bool isSettled = VillageLayout.Village.Distance(x, z) < 6f || VillageLayout.Graveyard.Contains(new Vector2(x, z)) || InField(x, z, 6f);

            return Mathf.Max(closed, isSettled ? 0f : copse * 0.85f);
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

        private static TerrainLayer TerrainLayer(string name, string texture, float smoothness, float tint = 1f, float normalScale = 1f)
        {
            string path = $"{LayersFolder}/{name}.terrainlayer";
            TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);

            if (layer == null)
            {
                layer = new TerrainLayer();
                AssetDatabase.CreateAsset(layer, path);
            }

            layer.diffuseTexture = DungeonTextureBuilder.Load(texture, false);
            layer.normalMapTexture = DungeonTextureBuilder.Load(texture, true);
            layer.normalScale = normalScale;
            layer.diffuseRemapMin = Vector4.zero;
            layer.diffuseRemapMax = new Vector4(tint, tint, tint * 1.08f, 1f);
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
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);

            return material;
        }

        /// Layer weights from what the ground is: rock on steep slopes and hills, mud by the water and in the swamp, dirt on roads
        /// and round buildings with puddles in the ruts, cobbles on the square, forest litter in the woods, grass elsewhere.
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
                    float fine = DungeonTextureBuilder.Noise(wx / 540f + 9f, wz / 540f + 6f, 140f, 2);
                    float swamp = VillageLayout.SwampShare(wx, wz);
                    float slope = ground.Slope(wx, wz);
                    float road = DungeonTextureBuilder.Step(0.8f + noise * 1.6f, -0.6f, ground.RoadDistance(wx, wz));
                    float pad = ground.PadMask(wx, wz);
                    float wet = DungeonTextureBuilder.Step(VillageLayout.WaterLevel + 0.6f, VillageLayout.WaterLevel + 0.05f, height);
                    float rock = Mathf.Max(DungeonTextureBuilder.Step(24f, 34f, slope + (noise - 0.5f) * 8f), DungeonTextureBuilder.Step(14f, 4f, VillageLayout.EdgeDistance(wx, wz)));
                    float forest = Forest(ground, wx, wz) * (0.6f + noise * 0.6f);
                    float plaza = DungeonTextureBuilder.Step(VillageLayout.PlazaRadius + 1f + fine * 3f, VillageLayout.PlazaRadius - 1f, (new Vector2(wx, wz) - VillageLayout.Plaza).magnitude);
                    float field = InField(wx, wz, 0f) ? 1f : 0f;
                    float puddle = DungeonTextureBuilder.Step(0.6f, 0.68f, fine) * Mathf.Max(road, pad * 0.6f, wet * 0.8f) * (1f - plaza);

                    System.Array.Clear(weights, 0, layers);
                    weights[(int)Layer.Grass] = 1f;
                    weights[(int)Layer.Forest] = forest * 1.4f;
                    weights[(int)Layer.Mud] = Mathf.Max(wet, swamp * (0.5f + noise)) * 2f;
                    weights[(int)Layer.Dirt] = Mathf.Max(road * 3f, pad * (0.6f + noise * 0.8f), field * 1.2f);
                    weights[(int)Layer.Cobble] = plaza * 4f;
                    weights[(int)Layer.Rock] = rock * 5f;
                    weights[(int)Layer.Puddle] = puddle * 6f;
                    float total = 0f;

                    foreach (float weight in weights)
                        total += weight;

                    for (int i = 0; i < layers; i++)
                        maps[z, x, i] = weights[i] / total;
                }
            }

            data.SetAlphamaps(0, 0, maps);
        }

        private static void Holes(TerrainData data, Clearings clearings)
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

        /// Grass on meadows and forest edges, thick and tall along the thicket rims, reeds where the ground meets the water,
        /// dead crops in rows on the fields.
        private static void Details(TerrainData data, VillageGround ground, Clearings clearings)
        {
            data.SetDetailResolution(DetailResolution, 16);
            data.detailPrototypes = new[]
            {
                Detail("GrassBlades", 0.7f, 1.2f, 0.4f, 0.8f, new Color(0.62f, 0.72f, 0.5f), new Color(0.8f, 0.72f, 0.5f)),
                Detail("Reeds", 0.9f, 1.4f, 1.1f, 1.9f, new Color(0.7f, 0.74f, 0.6f), new Color(0.7f, 0.62f, 0.46f)),
                Detail("DeadCrops", 0.6f, 0.9f, 0.8f, 1.3f, new Color(0.85f, 0.8f, 0.62f), new Color(0.75f, 0.66f, 0.5f))
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

                    if (height < VillageLayout.WaterLevel - 0.25f || ground.PadMask(wx, wz) > 0.2f || ground.RoadDistance(wx, wz) < 0.3f || IsCleared(clearings, wx, wz)
                        || ground.Slope(wx, wz) > 34f)
                        continue;

                    float noise = DungeonTextureBuilder.Noise(wx / 540f + 6f, wz / 540f + 1f, 50f, 3);
                    float shore = DungeonTextureBuilder.Step(VillageLayout.WaterLevel + 0.7f, VillageLayout.WaterLevel - 0.1f, height);
                    float open = ground.Open(wx, wz);

                    if (InField(wx, wz, -1f))
                    {
                        // Rows run along the field's length; furrows between them stay bare.
                        layers[2][z, x] = Mathf.Repeat(wx, 1.2f) < 0.55f ? 3 + random.Next(3) : 0;
                        continue;
                    }

                    if (shore > 0.3f && noise > 0.3f)
                    {
                        layers[1][z, x] = 1 + random.Next(5);
                        continue;
                    }

                    // The rim of the thicket and the road verges grow tall; the trodden middle of a place stays short.
                    float rim = DungeonTextureBuilder.Step(-8f, -1f, open) * DungeonTextureBuilder.Step(6f, 1f, open);
                    float verge = DungeonTextureBuilder.Step(0.3f, 1.5f, ground.RoadDistance(wx, wz)) * DungeonTextureBuilder.Step(5f, 1.5f, ground.RoadDistance(wx, wz));
                    float density = Mathf.Max(DungeonTextureBuilder.Step(0.3f, 0.62f, noise), rim, verge * 0.8f) * (1f - VillageLayout.SwampShare(wx, wz) * 0.5f) * (1f - Forest(ground, wx, wz) * 0.45f);

                    if (VillageLayout.Graveyard.Contains(new Vector2(wx, wz)))
                        density *= 0.7f;

                    layers[0][z, x] = Mathf.RoundToInt(density * 6f * (0.4f + (float)random.NextDouble() * 0.6f));
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

        /// Dense forest over the closed land (dead trees and bushes over the bog), a thicket rim of bushes and young trees round
        /// every open place, copses inside the wild ones, a few old trees in the settled places. Trunks near the open ground are
        /// NavMesh obstacles; deeper ones lie in the cut-out closed land anyway.
        private static void Trees(TerrainData data, VillageGround ground, Clearings clearings, List<NavMeshBuildSource> sources)
        {
            List<TreePrototype> prototypes = new();
            int[] spruce = new int[DungeonVegetationBuilder.Variants];
            int[] broadleaf = new int[DungeonVegetationBuilder.Variants];
            int[] dead = new int[DungeonVegetationBuilder.Variants];
            int[] bush = new int[DungeonVegetationBuilder.Variants];
            int[] swampTree = new int[DungeonVegetationBuilder.Variants];

            for (int i = 0; i < DungeonVegetationBuilder.Variants; i++)
            {
                spruce[i] = Prototype(prototypes, $"Spruce{i}");
                broadleaf[i] = Prototype(prototypes, $"Broadleaf{i}");
                dead[i] = Prototype(prototypes, $"DeadTree{i}");
                bush[i] = Prototype(prototypes, $"Bush{i}");
                swampTree[i] = Prototype(prototypes, $"SwampTree{i}");
            }

            data.treePrototypes = prototypes.ToArray();
            List<TreeInstance> trees = new();
            System.Random random = new System.Random(11);
            int Pick(int[] kinds) => kinds[random.Next(kinds.Length)];

            for (float wz = -VillageLayout.Half + 4f; wz < VillageLayout.Half - 4f; wz += TreeStep)
            {
                for (float wx = -VillageLayout.Half + 4f; wx < VillageLayout.Half - 4f; wx += TreeStep)
                {
                    float x = wx + ((float)random.NextDouble() - 0.5f) * TreeStep * 0.9f;
                    float z = wz + ((float)random.NextDouble() - 0.5f) * TreeStep * 0.9f;
                    float roll = (float)random.NextDouble();
                    float height = ground.Height(x, z);
                    float open = ground.Open(x, z);
                    float swamp = VillageLayout.SwampShare(x, z);
                    bool isWet = height < VillageLayout.WaterLevel + 0.15f;

                    if (ground.RoadDistance(x, z) < 2.5f || ground.PadMask(x, z) > 0.05f || IsCleared(clearings, x, z) || InField(x, z, 4f)
                        || ground.WaterDistance(x, z) < (swamp > 0.5f ? 2f : 5f) || VillageLayout.EdgeDistance(x, z) < 6f)
                        continue;

                    int prototype;

                    if (swamp > 0.5f)
                    {
                        if (open > VillageLayout.WallLine)
                        {
                            if (roll > 0.5f)
                                continue;

                            double kind = random.NextDouble();
                            prototype = kind < 0.45 ? Pick(swampTree) : kind < 0.75 ? Pick(dead) : kind < 0.88 ? Pick(bush) : Pick(broadleaf);
                        }
                        else
                        {
                            if (roll > (open > -5f ? 0.35f : 0.06f))
                                continue;

                            prototype = isWet || random.NextDouble() < 0.6 ? Pick(random.NextDouble() < 0.5 ? swampTree : dead) : Pick(bush);
                        }
                    }
                    else if (isWet)
                    {
                        continue;
                    }
                    else if (open > VillageLayout.WallLine)
                    {
                        if (roll > 0.86f)
                            continue;

                        double kind = random.NextDouble();
                        prototype = kind < 0.5 ? Pick(spruce) : kind < 0.8 ? Pick(broadleaf) : kind < 0.9 ? Pick(bush) : Pick(dead);
                    }
                    else if (VillageLayout.Graveyard.Contains(new Vector2(x, z)))
                    {
                        if (roll > 0.03f)
                            continue;

                        prototype = Pick(dead);
                    }
                    else if (Forest(ground, x, z) > 0.5f)
                    {
                        if (roll > 0.55f)
                            continue;

                        double kind = random.NextDouble();
                        prototype = kind < 0.4 ? Pick(spruce) : kind < 0.75 ? Pick(broadleaf) : kind < 0.92 ? Pick(bush) : Pick(dead);
                    }
                    else
                    {
                        if (roll > 0.03f)
                            continue;

                        prototype = random.NextDouble() < 0.6 ? Pick(broadleaf) : Pick(dead);
                    }

                    AddTree(trees, prototypes, sources, random, prototype, x, z, height, open < 4f);
                }
            }

            // The thicket rim: bushes shoulder to shoulder along the edge of the open ground, so the closed land reads as a wall.
            for (float wz = -VillageLayout.Half + 4f; wz < VillageLayout.Half - 4f; wz += ThicketStep)
            {
                for (float wx = -VillageLayout.Half + 4f; wx < VillageLayout.Half - 4f; wx += ThicketStep)
                {
                    float x = wx + ((float)random.NextDouble() - 0.5f) * ThicketStep;
                    float z = wz + ((float)random.NextDouble() - 0.5f) * ThicketStep;
                    float open = ground.Open(x, z);

                    if (open < -1.5f || open > 5f || random.NextDouble() > 0.6 || ground.RoadDistance(x, z) < 2.5f || ground.PadMask(x, z) > 0.05f || IsCleared(clearings, x, z)
                        || ground.WaterDistance(x, z) < 3f || VillageLayout.EdgeDistance(x, z) < 6f || ground.Height(x, z) < VillageLayout.WaterLevel - 0.3f)
                        continue;

                    AddTree(trees, prototypes, sources, random, Pick(bush), x, z, ground.Height(x, z), false);
                }
            }

            data.SetTreeInstances(trees.ToArray(), false);
        }

        private static void AddTree(List<TreeInstance> trees, List<TreePrototype> prototypes, List<NavMeshBuildSource> sources, System.Random random, int prototype, float x, float z, float height,
            bool isObstacle)
        {
            float scale = 0.8f + (float)random.NextDouble() * 0.45f;
            trees.Add(new TreeInstance
            {
                prototypeIndex = prototype,
                position = new Vector3((x + VillageLayout.Half) / VillageLayout.Size, (height - VillageGround.Bottom) / VillageGround.Depth, (z + VillageLayout.Half) / VillageLayout.Size),
                widthScale = scale,
                heightScale = scale * (0.9f + (float)random.NextDouble() * 0.2f),
                rotation = (float)random.NextDouble() * Mathf.PI * 2f,
                color = Color.Lerp(Color.white, new Color(0.78f, 0.76f, 0.68f), (float)random.NextDouble()),
                lightmapColor = Color.white
            });

            CapsuleCollider capsule = prototypes[prototype].prefab.GetComponent<CapsuleCollider>();

            if (!isObstacle || capsule == null)
                return;

            sources.Add(new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Capsule,
                transform = Matrix4x4.TRS(new Vector3(x, height + 2.5f, z), Quaternion.identity, Vector3.one),
                size = new Vector3(capsule.radius * 2f * scale, 6f, capsule.radius * 2f * scale),
                area = 1
            });
        }

        private static int Prototype(List<TreePrototype> prototypes, string name)
        {
            prototypes.Add(new TreePrototype { prefab = DungeonVegetationBuilder.Load(name), bendFactor = 0f });

            return prototypes.Count - 1;
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

        /// Black still water that mirrors the moonlit sky; the mud normals give it a slow ripple.
        private static Material WaterMaterial()
        {
            string path = $"{DungeonPropBuilder.MaterialsFolder}/VillageWater.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", new Color(0.012f, 0.02f, 0.022f));
            material.SetFloat("_Smoothness", 0.92f);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_SpecularHighlights", 1f);
            material.SetFloat("_EnvironmentReflections", 1f);
            material.SetTexture("_BumpMap", DungeonTextureBuilder.Load("Mud", true));
            material.SetFloat("_BumpScale", 0.22f);
            material.EnableKeyword("_NORMALMAP");
            material.SetTextureScale("_BaseMap", Vector2.one * 0.35f);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);

            return material;
        }

        /// The river and the stream cannot be waded: invisible walls follow both banks at the waterline, open under the bridges
        /// and where the water spreads into the swamp. The open ground is fenced off from the closed land along its rim, and
        /// the map ends in walls at the foot of the hills.
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
            RimWalls(root, ground);

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

                    // Banks inside the closed land are behind the rim walls already.
                    if (ground.Open(point) > VillageLayout.WallLine + 2f)
                        continue;

                    float height = ground.Height(point);
                    GameObject wall = BattleEditorUtility.CreateChild("Bank", root, new Vector3(point.x, height + 1f, point.y));
                    wall.transform.localRotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.y));
                    wall.AddComponent<BoxCollider>().size = new Vector3(0.4f, 5f, (b - a).magnitude + 0.3f);
                    wall.isStatic = true;
                }
            }
        }

        /// Marching squares over the open field at the wall line: one vertical collider strip per map module, ten metres tall
        /// from under the ground, so nothing walks or jumps into the closed land.
        private static void RimWalls(Transform root, VillageGround ground)
        {
            int cells = Mathf.RoundToInt(VillageLayout.Size / RimCell);
            int perModule = cells / VillageLayout.Grid;
            float[,] values = new float[cells + 1, cells + 1];

            for (int z = 0; z <= cells; z++)
            {
                for (int x = 0; x <= cells; x++)
                    values[z, x] = ground.Open(x * RimCell - VillageLayout.Half, z * RimCell - VillageLayout.Half) - VillageLayout.WallLine;
            }

            for (int mz = 0; mz < VillageLayout.Grid; mz++)
            {
                for (int mx = 0; mx < VillageLayout.Grid; mx++)
                {
                    List<Vector3> vertices = new();
                    List<int> triangles = new();

                    for (int z = mz * perModule; z < (mz + 1) * perModule; z++)
                    {
                        for (int x = mx * perModule; x < (mx + 1) * perModule; x++)
                        {
                            Vector2 origin = new Vector2(x * RimCell - VillageLayout.Half, z * RimCell - VillageLayout.Half);

                            if (VillageLayout.EdgeDistance(origin.x + RimCell * 0.5f, origin.y + RimCell * 0.5f) < VillageLayout.Half - VillageLayout.Playable)
                                continue;

                            float[] corner = { values[z, x], values[z, x + 1], values[z + 1, x + 1], values[z + 1, x] };
                            Vector2[] offsets = { Vector2.zero, new Vector2(RimCell, 0f), new Vector2(RimCell, RimCell), new Vector2(0f, RimCell) };
                            List<Vector2> crossings = new();

                            for (int e = 0; e < 4; e++)
                            {
                                float a = corner[e];
                                float b = corner[(e + 1) % 4];

                                if (a < 0f == b < 0f)
                                    continue;

                                crossings.Add(origin + Vector2.Lerp(offsets[e], offsets[(e + 1) % 4], a / (a - b)));
                            }

                            for (int i = 0; i + 1 < crossings.Count; i += 2)
                                Strip(vertices, triangles, ground, crossings[i], crossings[i + 1]);
                        }
                    }

                    if (vertices.Count == 0)
                        continue;

                    GameObject wall = BattleEditorUtility.CreateChild($"Rim{mx}{mz}", root);
                    wall.isStatic = true;
                    wall.AddComponent<MeshCollider>().sharedMesh = SaveMesh($"VillageRim{mx}{mz}", vertices, triangles);
                }
            }
        }

        private static void Strip(List<Vector3> vertices, List<int> triangles, VillageGround ground, Vector2 a, Vector2 b)
        {
            int start = vertices.Count;
            float ha = Mathf.Max(ground.Height(a), VillageLayout.WaterLevel);
            float hb = Mathf.Max(ground.Height(b), VillageLayout.WaterLevel);
            vertices.Add(new Vector3(a.x, ha - 2f, a.y));
            vertices.Add(new Vector3(a.x, ha + 8f, a.y));
            vertices.Add(new Vector3(b.x, hb + 8f, b.y));
            vertices.Add(new Vector3(b.x, hb - 2f, b.y));
            // Both faces, so the collider stops movement from either side.
            triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3, start, start + 2, start + 1, start, start + 3, start + 2 });
        }

        private static Mesh SaveMesh(string name, List<Vector3> vertices, List<int> triangles)
        {
            string path = $"{DungeonMeshBuilder.Folder}/{name}.asset";
            BattleEditorUtility.EnsureFolder(DungeonMeshBuilder.Folder);
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);

            if (mesh == null)
            {
                mesh = new Mesh();
                AssetDatabase.CreateAsset(mesh, path);
            }

            mesh.Clear();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.name = name;
            EditorUtility.SetDirty(mesh);

            return mesh;
        }

        /// The closed land must hold no NavMesh, or escape portals and monsters could be put on a ridge top behind the rim walls.
        private static void CloseNavMesh(VillageGround ground, List<NavMeshBuildSource> sources)
        {
            for (float z = -VillageLayout.Half + NavCell * 0.5f; z < VillageLayout.Half; z += NavCell)
            {
                for (float x = -VillageLayout.Half + NavCell * 0.5f; x < VillageLayout.Half; x += NavCell)
                {
                    if (ground.Open(x, z) < VillageLayout.WallLine + NavCell * 0.75f)
                        continue;

                    sources.Add(new NavMeshBuildSource
                    {
                        shape = NavMeshBuildSourceShape.ModifierBox,
                        transform = Matrix4x4.Translate(new Vector3(x, 10f, z)),
                        size = new Vector3(NavCell, 80f, NavCell),
                        area = 1
                    });
                }
            }
        }
    }
}
