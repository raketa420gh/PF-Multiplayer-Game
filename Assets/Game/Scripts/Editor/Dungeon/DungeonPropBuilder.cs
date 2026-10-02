using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Crypt props assembled from procedural meshes and textured materials. Returns unsaved objects for the content builder.
    internal static class DungeonPropBuilder
    {
        public const string PrefabsFolder = "Assets/Game/Prefabs/Dungeon";
        public const string MaterialsFolder = "Assets/Game/Materials/Dungeon";
        public const float WallHeight = 4.5f;
        public const float WallThickness = 0.6f;

        public static Material StoneWall => Textured("StoneWall", "StoneWall", 0.25f, 0.15f);
        public static Material StoneFloor => Textured("StoneFloor", "StoneFloor", 0.25f, 0.2f);
        public static Material Cobble => Textured("Cobble", "Cobble", 0.33f, 0.1f);
        public static Material Dirt => Textured("Dirt", "Dirt", 0.3f, 0.05f);
        public static Material WoodPlanks => Textured("WoodPlanks", "WoodPlanks", 0.5f, 0.25f);
        public static Material DarkWood => Textured("DarkWood", "DarkWood", 0.5f, 0.3f);
        public static Material RustyMetal => Textured("RustyMetal", "RustyMetal", 0.6f, 0.4f, 0.6f);
        public static Material Bone => Textured("Bone", "Bone", 0.8f, 0.35f);
        public static Material ZombieSkin => Textured("ZombieSkin", "ZombieSkin", 0.9f, 0.3f);
        public static Material ClothRed => Textured("ClothRed", "ClothRed", 1f, 0.1f);
        public static Material Gold => Textured("Gold", "Gold", 0.8f, 0.75f, 0.8f);
        public static Material Fire => FireMaterial();
        public static Material PortalBlue => Emissive("PortalBlue", new Color(0.2f, 0.5f, 1f), 4f);
        public static Material PortalRed => Emissive("PortalRed", new Color(1f, 0.25f, 0.15f), 4f);
        public static Material ShrineGlow => Emissive("ShrineGlow", new Color(0.9f, 0.8f, 0.4f), 2.5f);

        public static Material Textured(string name, string texture, float tiling, float smoothness, float metallic = 0f)
        {
            BattleEditorUtility.EnsureFolder(MaterialsFolder);
            string path = $"{MaterialsFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetTexture("_BaseMap", DungeonTextureBuilder.Load(texture, false));
            material.SetTexture("_BumpMap", DungeonTextureBuilder.Load(texture, true));
            material.SetFloat("_BumpScale", 1f);
            material.EnableKeyword("_NORMALMAP");
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            material.SetTextureScale("_BaseMap", Vector2.one * tiling);
            EditorUtility.SetDirty(material);

            return material;
        }

        private static Material FireMaterial()
        {
            BattleEditorUtility.EnsureFolder(MaterialsFolder);
            string path = $"{MaterialsFolder}/Fire.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{DungeonTextureBuilder.Folder}/Flame.png"));
            material.SetColor("_BaseColor", new Color(1f, 0.7f, 0.3f, 1f));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 2f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(material);

            return material;
        }

        public static Material Emissive(string name, Color color, float intensity)
        {
            BattleEditorUtility.EnsureFolder(MaterialsFolder);
            string path = $"{MaterialsFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", color);
            material.SetColor("_EmissionColor", color * intensity);
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(material);

            return material;
        }

        public static GameObject MeshObject(string name, Transform parent, Mesh mesh, Material material, Vector3 position = default,
            Vector3 euler = default, bool collider = true, bool isStatic = true)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            go.isStatic = isStatic;

            if (collider)
                go.AddComponent<MeshCollider>().sharedMesh = mesh;

            return go;
        }

        public static Light PointLight(Transform parent, Vector3 position, Color color, float range, float intensity, bool flicker, bool shadows = false)
        {
            GameObject go = BattleEditorUtility.CreateChild("Light", parent, position);
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            light.shadowStrength = 0.8f;

            if (flicker)
            {
                FlickerLightComponent component = go.AddComponent<FlickerLightComponent>();
                SerializedObject so = new SerializedObject(component);
                BattleEditorUtility.Set(so, "_light", light);
                BattleEditorUtility.Set(so, "_baseIntensity", intensity);
                BattleEditorUtility.Set(so, "_amplitude", intensity * 0.25f);
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            return light;
        }

        public static ParticleSystem Flame(Transform parent, Vector3 position, float scale)
        {
            ParticleSystem flame = BattleEditorUtility.CreateChild("Flame", parent, position).AddComponent<ParticleSystem>();
            flame.transform.rotation = Quaternion.LookRotation(Vector3.up);
            ParticleSystem.MainModule main = flame.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f * scale, 0.6f * scale);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f * scale, 0.26f * scale);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.8f, 0.3f), new Color(1f, 0.35f, 0.05f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 60;
            ParticleSystem.EmissionModule emission = flame.emission;
            emission.rateOverTime = 40f * scale;
            ParticleSystem.ShapeModule shape = flame.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 8f;
            shape.radius = 0.05f * scale;
            ParticleSystem.ColorOverLifetimeModule color = flame.colorOverLifetime;
            color.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.4f, 0.1f), 0.6f), new GradientColorKey(new Color(0.3f, 0.1f, 0.05f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;
            ParticleSystem.SizeOverLifetimeModule size = flame.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
            ParticleSystemRenderer renderer = flame.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = Fire;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            return flame;
        }

        public static GameObject Wall(float length, string name)
        {
            Mesh mesh = new DungeonMeshBuilder(0.5f).Box(new Vector3(0f, WallHeight * 0.5f, 0f), new Vector3(length, WallHeight + 0.3f, WallThickness)).Save(name);

            return MeshObject(name, null, mesh, StoneWall);
        }

        public static GameObject Pillar()
        {
            Mesh mesh = new DungeonMeshBuilder(0.5f)
                .Cylinder(new Vector3(0f, WallHeight * 0.5f, 0f), 0.35f, WallHeight, 8)
                .Box(new Vector3(0f, 0.2f, 0f), new Vector3(1f, 0.4f, 1f))
                .Box(new Vector3(0f, WallHeight - 0.2f, 0f), new Vector3(1f, 0.4f, 1f))
                .Save("Pillar");

            return MeshObject("Pillar", null, mesh, StoneWall);
        }

        public static GameObject Floor(float size)
        {
            Mesh mesh = new DungeonMeshBuilder(0.5f).Quad(Vector3.zero, Vector3.up, Vector3.right * size * 0.5f, Vector3.forward * size * 0.5f).Save("Floor" + size);

            return MeshObject("Floor", null, mesh, StoneFloor);
        }

        public static GameObject Ceiling(float size)
        {
            Mesh mesh = new DungeonMeshBuilder(0.5f).Quad(Vector3.zero, Vector3.down, Vector3.right * size * 0.5f, Vector3.back * size * 0.5f).Save("Ceiling" + size);

            return MeshObject("Ceiling", null, mesh, StoneWall);
        }

        /// Doorway through a wall: two posts and a lintel, opening 2.2 wide x 3 high.
        public static GameObject DoorFrame(float wallLength)
        {
            float side = (wallLength - 2.2f) * 0.5f;
            Mesh mesh = new DungeonMeshBuilder(0.5f)
                .Box(new Vector3(-1.1f - side * 0.5f, WallHeight * 0.5f, 0f), new Vector3(side, WallHeight + 0.3f, WallThickness))
                .Box(new Vector3(1.1f + side * 0.5f, WallHeight * 0.5f, 0f), new Vector3(side, WallHeight + 0.3f, WallThickness))
                .Box(new Vector3(0f, 3f + (WallHeight - 3f) * 0.5f + 0.075f, 0f), new Vector3(2.2f, WallHeight - 3f + 0.15f, WallThickness))
                .Box(new Vector3(-1.2f, 1.5f, 0f), new Vector3(0.2f, 3f, WallThickness + 0.2f))
                .Box(new Vector3(1.2f, 1.5f, 0f), new Vector3(0.2f, 3f, WallThickness + 0.2f))
                .Box(new Vector3(0f, 3.1f, 0f), new Vector3(2.6f, 0.2f, WallThickness + 0.2f))
                .Save("DoorFrame" + wallLength);

            return MeshObject("DoorFrame", null, mesh, StoneWall);
        }

        public static GameObject DoorLeaf()
        {
            Mesh mesh = new DungeonMeshBuilder(0.5f)
                .Box(new Vector3(1.05f, 1.45f, 0f), new Vector3(2.1f, 2.9f, 0.12f))
                .Box(new Vector3(1.05f, 0.7f, 0.08f), new Vector3(2.0f, 0.1f, 0.04f))
                .Box(new Vector3(1.05f, 2.2f, 0.08f), new Vector3(2.0f, 0.1f, 0.04f))
                .Save("DoorLeaf");

            GameObject leaf = MeshObject("Leaf", null, mesh, WoodPlanks, default, default, true, false);
            leaf.GetComponent<MeshCollider>().convex = true;

            return leaf;
        }

        public static GameObject Chest(string name, float width, float depth, float height, Material material, out Transform lid)
        {
            Mesh body = new DungeonMeshBuilder(0.6f).Box(new Vector3(0f, height * 0.35f, 0f), new Vector3(width, height * 0.7f, depth)).Save(name + "Body");
            Mesh top = new DungeonMeshBuilder(0.6f)
                .Box(new Vector3(0f, height * 0.15f, depth * 0.5f), new Vector3(width, height * 0.3f, depth))
                .Box(new Vector3(0f, height * 0.3f, depth * 0.5f), new Vector3(width * 0.9f, 0.04f, depth * 0.8f))
                .Save(name + "Lid");
            Mesh band = new DungeonMeshBuilder(0.6f)
                .Box(new Vector3(width * 0.3f, height * 0.35f, 0f), new Vector3(0.06f, height * 0.72f, depth + 0.02f))
                .Box(new Vector3(-width * 0.3f, height * 0.35f, 0f), new Vector3(0.06f, height * 0.72f, depth + 0.02f))
                .Box(new Vector3(0f, height * 0.5f, depth * 0.5f + 0.02f), new Vector3(0.14f, 0.16f, 0.04f))
                .Save(name + "Bands");

            GameObject root = new GameObject(name);
            MeshObject("Body", root.transform, body, material, default, default, true, false);
            MeshObject("Bands", root.transform, band, RustyMetal, default, default, false, false);
            GameObject lidObject = new GameObject("LidPivot");
            lidObject.transform.SetParent(root.transform, false);
            lidObject.transform.localPosition = new Vector3(0f, height * 0.7f, -depth * 0.5f);
            MeshObject("Lid", lidObject.transform, top, material, default, default, false, false);
            lid = lidObject.transform;

            return root;
        }

        public static GameObject Coffin(out Transform lid)
        {
            Mesh body = new DungeonMeshBuilder(0.6f).Box(new Vector3(0f, 0.35f, 0f), new Vector3(0.9f, 0.7f, 2.2f)).Save("CoffinBody");
            Mesh top = new DungeonMeshBuilder(0.6f).Box(new Vector3(0f, 0.08f, 1.1f), new Vector3(0.95f, 0.16f, 2.25f)).Save("CoffinLid");
            GameObject root = new GameObject("Coffin");
            MeshObject("Body", root.transform, body, StoneWall, default, default, true, false);
            GameObject lidObject = new GameObject("LidPivot");
            lidObject.transform.SetParent(root.transform, false);
            lidObject.transform.localPosition = new Vector3(0f, 0.7f, -1.1f);
            MeshObject("Lid", lidObject.transform, top, StoneWall, default, default, false, false);
            lid = lidObject.transform;

            return root;
        }

        public static GameObject Barrel()
        {
            Mesh mesh = new DungeonMeshBuilder(0.6f).Cylinder(new Vector3(0f, 0.45f, 0f), 0.36f, 0.9f, 12, 0.32f).Save("Barrel");
            Mesh bands = new DungeonMeshBuilder(0.6f)
                .Cylinder(new Vector3(0f, 0.2f, 0f), 0.375f, 0.06f, 12)
                .Cylinder(new Vector3(0f, 0.7f, 0f), 0.355f, 0.06f, 12)
                .Save("BarrelBands");
            GameObject root = new GameObject("Barrel");
            MeshObject("Body", root.transform, mesh, WoodPlanks, default, default, true, false);
            MeshObject("Bands", root.transform, bands, RustyMetal, default, default, false, false);

            return root;
        }

        public static GameObject Crate()
        {
            Mesh mesh = new DungeonMeshBuilder(0.8f).Box(new Vector3(0f, 0.4f, 0f), new Vector3(0.8f, 0.8f, 0.8f)).Save("Crate");
            Mesh frame = new DungeonMeshBuilder(0.8f)
                .Box(new Vector3(0f, 0.4f, 0.41f), new Vector3(0.84f, 0.06f, 0.04f))
                .Box(new Vector3(0f, 0.4f, -0.41f), new Vector3(0.84f, 0.06f, 0.04f))
                .Box(new Vector3(0.41f, 0.4f, 0f), new Vector3(0.04f, 0.06f, 0.84f))
                .Box(new Vector3(-0.41f, 0.4f, 0f), new Vector3(0.04f, 0.06f, 0.84f))
                .Save("CrateFrame");
            GameObject root = new GameObject("Crate");
            MeshObject("Body", root.transform, mesh, WoodPlanks, default, default, true, false);
            MeshObject("Frame", root.transform, frame, DarkWood, default, default, false, false);

            return root;
        }

        public static GameObject Bookshelf()
        {
            DungeonMeshBuilder builder = new DungeonMeshBuilder(0.6f)
                .Box(new Vector3(0f, 1.1f, -0.2f), new Vector3(1.6f, 2.2f, 0.05f))
                .Box(new Vector3(-0.78f, 1.1f, 0f), new Vector3(0.05f, 2.2f, 0.45f))
                .Box(new Vector3(0.78f, 1.1f, 0f), new Vector3(0.05f, 2.2f, 0.45f));

            for (int i = 0; i < 5; i++)
                builder.Box(new Vector3(0f, 0.05f + i * 0.52f, 0f), new Vector3(1.55f, 0.05f, 0.45f));

            Mesh mesh = builder.Save("Bookshelf");
            DungeonMeshBuilder books = new DungeonMeshBuilder(0.6f);
            System.Random random = new System.Random(7);

            for (int shelf = 0; shelf < 4; shelf++)
            {
                float x = -0.7f;

                while (x < 0.6f)
                {
                    float width = 0.05f + (float)random.NextDouble() * 0.08f;
                    float height = 0.25f + (float)random.NextDouble() * 0.18f;

                    if (random.NextDouble() > 0.15)
                        books.Box(new Vector3(x + width * 0.5f, 0.08f + shelf * 0.52f + height * 0.5f, 0.02f), new Vector3(width, height, 0.3f));

                    x += width + 0.01f;
                }
            }

            GameObject root = new GameObject("Bookshelf");
            MeshObject("Frame", root.transform, mesh, DarkWood, default, default, true, false);
            MeshObject("Books", root.transform, books.Save("Books"), ClothRed, default, default, false, false);

            return root;
        }

        public static GameObject Table()
        {
            DungeonMeshBuilder builder = new DungeonMeshBuilder(0.6f).Box(new Vector3(0f, 0.78f, 0f), new Vector3(2.2f, 0.08f, 0.9f));

            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                    builder.Box(new Vector3(x * 0.95f, 0.37f, z * 0.35f), new Vector3(0.1f, 0.74f, 0.1f));
            }

            GameObject root = new GameObject("Table");
            MeshObject("Table", root.transform, builder.Save("Table"), WoodPlanks, default, default, true, false);
            Mesh candle = new DungeonMeshBuilder(1f).Cylinder(new Vector3(0f, 0.1f, 0f), 0.03f, 0.2f, 6).Save("Candle");
            Transform candles = BattleEditorUtility.CreateChild("Candles", root.transform, new Vector3(0.6f, 0.82f, 0f)).transform;
            MeshObject("Candle", candles, candle, Bone, default, default, false, false);
            MeshObject("Candle", candles, candle, Bone, new Vector3(0.08f, -0.04f, 0.06f), default, false, false);
            Flame(candles, new Vector3(0f, 0.21f, 0f), 0.35f);
            PointLight(candles, new Vector3(0f, 0.35f, 0f), new Color(1f, 0.75f, 0.4f), 4f, 1.2f, true);

            return root;
        }

        public static GameObject Brazier()
        {
            Mesh mesh = new DungeonMeshBuilder(0.6f)
                .Cylinder(new Vector3(0f, 0.9f, 0f), 0.45f, 0.35f, 10, 0.5f)
                .Cylinder(new Vector3(0f, 0.4f, 0f), 0.06f, 0.8f, 6)
                .Cylinder(new Vector3(0f, 0.04f, 0f), 0.35f, 0.08f, 10)
                .Save("Brazier");
            GameObject root = new GameObject("Brazier");
            MeshObject("Bowl", root.transform, mesh, RustyMetal, default, default, true, false);
            Flame(root.transform, new Vector3(0f, 1.05f, 0f), 1.6f);
            PointLight(root.transform, new Vector3(0f, 1.6f, 0f), new Color(1f, 0.6f, 0.25f), 11f, 3.5f, true);

            return root;
        }

        public static GameObject WallTorch()
        {
            Mesh mesh = new DungeonMeshBuilder(0.8f)
                .Box(new Vector3(0f, 0f, 0.1f), new Vector3(0.12f, 0.25f, 0.2f))
                .Cylinder(new Vector3(0f, 0.2f, 0.22f), 0.03f, 0.5f, 6)
                .Save("WallTorch");
            GameObject root = new GameObject("WallTorch");
            MeshObject("Bracket", root.transform, mesh, RustyMetal, default, default, false, false);
            Flame(root.transform, new Vector3(0f, 0.48f, 0.22f), 0.8f);
            PointLight(root.transform, new Vector3(0f, 0.65f, 0.4f), new Color(1f, 0.62f, 0.28f), 9f, 2.8f, true);

            return root;
        }

        public static GameObject Banner()
        {
            Mesh mesh = new DungeonMeshBuilder(1f).DoubleQuad(new Vector3(0f, -0.9f, 0f), Vector3.forward, Vector3.right * 0.45f, Vector3.up * 0.9f).Save("Banner");
            Mesh rod = new DungeonMeshBuilder(1f).Cylinder(Vector3.zero, 0.03f, 1.1f, 6).Save("BannerRod");
            GameObject root = new GameObject("Banner");
            MeshObject("Cloth", root.transform, mesh, ClothRed, default, default, false, false);
            MeshObject("Rod", root.transform, rod, DarkWood, default, new Vector3(0f, 0f, 90f), false, false);

            return root;
        }

        public static GameObject SkullPile()
        {
            DungeonMeshBuilder builder = new DungeonMeshBuilder(2f);
            System.Random random = new System.Random(3);

            for (int i = 0; i < 14; i++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float radius = (float)random.NextDouble() * 0.5f;
                float y = 0.1f + (0.5f - radius) * 0.4f;
                builder.Cylinder(new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius), 0.1f, 0.16f, 7, 0.08f);
            }

            GameObject root = new GameObject("SkullPile");
            MeshObject("Skulls", root.transform, builder.Save("SkullPile"), Bone, default, default, false, false);

            return root;
        }

        public static GameObject Rubble()
        {
            DungeonMeshBuilder builder = new DungeonMeshBuilder(0.6f);
            System.Random random = new System.Random(11);

            for (int i = 0; i < 9; i++)
            {
                Vector3 size = new Vector3(0.2f + (float)random.NextDouble() * 0.5f, 0.15f + (float)random.NextDouble() * 0.3f, 0.2f + (float)random.NextDouble() * 0.5f);
                builder.Box(new Vector3(((float)random.NextDouble() - 0.5f) * 1.6f, size.y * 0.4f, ((float)random.NextDouble() - 0.5f) * 1.6f), size);
            }

            GameObject root = new GameObject("Rubble");
            MeshObject("Stones", root.transform, builder.Save("Rubble"), StoneWall, default, default, true, false);

            return root;
        }

        public static GameObject Sarcophagus()
        {
            Mesh mesh = new DungeonMeshBuilder(0.5f)
                .Box(new Vector3(0f, 0.45f, 0f), new Vector3(1.1f, 0.9f, 2.4f))
                .Box(new Vector3(0f, 0.95f, 0f), new Vector3(1.2f, 0.12f, 2.5f))
                .Box(new Vector3(0f, 1.1f, 0f), new Vector3(0.5f, 0.2f, 1.7f))
                .Save("Sarcophagus");
            GameObject root = new GameObject("Sarcophagus");
            MeshObject("Stone", root.transform, mesh, StoneWall, default, default, true, false);

            return root;
        }

        public static GameObject Altar(Material glow)
        {
            Mesh mesh = new DungeonMeshBuilder(0.5f)
                .Box(new Vector3(0f, 0.5f, 0f), new Vector3(1.4f, 1f, 0.8f))
                .Box(new Vector3(0f, 1.05f, 0f), new Vector3(1.6f, 0.12f, 1f))
                .Save("Altar");
            Mesh idol = new DungeonMeshBuilder(0.5f).Cylinder(new Vector3(0f, 1.5f, 0f), 0.22f, 0.8f, 8, 0.1f).Save("AltarIdol");
            GameObject root = new GameObject("Altar");
            MeshObject("Stone", root.transform, mesh, StoneWall, default, default, true, false);
            GameObject idolObject = MeshObject("Idol", root.transform, idol, glow, default, default, false, false);
            idolObject.name = "Glow";
            PointLight(idolObject.transform, new Vector3(0f, 1.8f, 0f), glow.GetColor("_BaseColor"), 6f, 2f, true);

            return root;
        }

        public static GameObject Portal(Material material)
        {
            Mesh pedestal = new DungeonMeshBuilder(0.5f)
                .Cylinder(new Vector3(0f, 0.15f, 0f), 0.9f, 0.3f, 12)
                .Box(new Vector3(0f, 0.9f, -0.5f), new Vector3(0.8f, 1.5f, 0.3f))
                .Save("PortalPedestal");
            Mesh ring = new DungeonMeshBuilder(1f).Cylinder(Vector3.zero, 0.95f, 0.08f, 24, 0.95f).Save("PortalRing");
            Mesh disc = new DungeonMeshBuilder(1f).Cylinder(Vector3.zero, 0.8f, 0.02f, 24).Save("PortalDisc");
            GameObject root = new GameObject("Portal");
            MeshObject("Pedestal", root.transform, pedestal, StoneWall, default, default, true, false);
            GameObject visual = BattleEditorUtility.CreateChild("Visual", root.transform, new Vector3(0f, 1.5f, 0f));
            visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            MeshObject("Ring", visual.transform, ring, material, default, default, false, false);
            MeshObject("Disc", visual.transform, disc, material, default, default, false, false);
            PointLight(visual.transform, new Vector3(0f, 0f, -0.3f), material.GetColor("_BaseColor"), 8f, 3f, true);

            return root;
        }

        public static GameObject Lever(out Transform handle)
        {
            Mesh baseMesh = new DungeonMeshBuilder(0.6f).Box(new Vector3(0f, 0.5f, 0f), new Vector3(0.4f, 1f, 0.4f)).Save("LeverBase");
            Mesh handleMesh = new DungeonMeshBuilder(1f).Cylinder(new Vector3(0f, 0.3f, 0f), 0.03f, 0.6f, 6).Save("LeverHandle");
            GameObject root = new GameObject("Lever");
            MeshObject("Base", root.transform, baseMesh, StoneWall, default, default, true, false);
            GameObject handleObject = BattleEditorUtility.CreateChild("Handle", root.transform, new Vector3(0f, 1f, 0f));
            MeshObject("Rod", handleObject.transform, handleMesh, RustyMetal, default, default, false, false);
            handle = handleObject.transform;

            return root;
        }

        public static GameObject SpikeTrap(out Transform spikes)
        {
            Mesh plate = new DungeonMeshBuilder(0.5f).Box(new Vector3(0f, 0.03f, 0f), new Vector3(2f, 0.06f, 2f)).Save("SpikePlate");
            DungeonMeshBuilder builder = new DungeonMeshBuilder(1f);

            for (int x = 0; x < 4; x++)
            {
                for (int z = 0; z < 4; z++)
                    builder.Cylinder(new Vector3(-0.75f + x * 0.5f, 0.35f, -0.75f + z * 0.5f), 0.05f, 0.7f, 6, 0.005f);
            }

            GameObject root = new GameObject("SpikeTrap");
            MeshObject("Plate", root.transform, plate, RustyMetal, default, default, false, false);
            GameObject spikesObject = BattleEditorUtility.CreateChild("Spikes", root.transform, new Vector3(0f, -0.7f, 0f));
            MeshObject("Spikes", spikesObject.transform, builder.Save("Spikes"), RustyMetal, default, default, false, false);
            spikes = spikesObject.transform;

            return root;
        }

        public static GameObject BladeTrap(out Transform pivot)
        {
            Mesh arm = new DungeonMeshBuilder(1f).Cylinder(new Vector3(0f, -1.4f, 0f), 0.05f, 2.8f, 6).Save("BladeArm");
            Mesh blade = new DungeonMeshBuilder(1f).Box(new Vector3(0f, -3f, 0f), new Vector3(1.4f, 0.5f, 0.04f)).Save("Blade");
            GameObject root = new GameObject("BladeTrap");
            GameObject pivotObject = BattleEditorUtility.CreateChild("Pivot", root.transform, new Vector3(0f, WallHeight - 0.2f, 0f));
            MeshObject("Arm", pivotObject.transform, arm, RustyMetal, default, default, false, false);
            MeshObject("Blade", pivotObject.transform, blade, RustyMetal, default, default, false, false);
            pivot = pivotObject.transform;

            return root;
        }

        public static GameObject Campfire()
        {
            DungeonMeshBuilder builder = new DungeonMeshBuilder(1f);

            for (int i = 0; i < 4; i++)
            {
                float angle = i * 45f;
                builder.Box(new Vector3(0f, 0.08f, 0f), new Vector3(0.9f, 0.12f, 0.12f));
            }

            GameObject root = new GameObject("Campfire");
            MeshObject("Logs", root.transform, builder.Save("CampfireLogs"), DarkWood, default, new Vector3(0f, 30f, 0f), false, false);
            MeshObject("Logs2", root.transform, AssetDatabase.LoadAssetAtPath<Mesh>($"{DungeonMeshBuilder.Folder}/CampfireLogs.asset"), DarkWood, default, new Vector3(0f, 110f, 0f), false, false);
            Flame(root.transform, new Vector3(0f, 0.15f, 0f), 1.4f);
            PointLight(root.transform, new Vector3(0f, 0.8f, 0f), new Color(1f, 0.65f, 0.3f), 9f, 2.8f, true);

            return root;
        }

        public static GameObject LootSack()
        {
            Mesh mesh = new DungeonMeshBuilder(1f).Cylinder(new Vector3(0f, 0.15f, 0f), 0.2f, 0.3f, 8, 0.12f).Save("LootSack");
            GameObject root = new GameObject("WorldItem");
            MeshObject("Sack", root.transform, mesh, WoodPlanks, default, default, false, false);
            PointLight(root.transform, new Vector3(0f, 0.4f, 0f), new Color(1f, 0.9f, 0.6f), 1.5f, 0.6f, false);

            return root;
        }

        public static GameObject Stairs(float width, float height, float depth, int steps)
        {
            DungeonMeshBuilder builder = new DungeonMeshBuilder(0.5f);

            for (int i = 0; i < steps; i++)
            {
                float stepHeight = height / steps;
                float stepDepth = depth / steps;
                builder.Box(new Vector3(0f, stepHeight * (i + 0.5f), stepDepth * (i + 0.5f)), new Vector3(width, stepHeight, stepDepth));
            }

            GameObject root = new GameObject("Stairs");
            MeshObject("Steps", root.transform, builder.Save($"Stairs{steps}"), StoneWall, default, default, true, true);

            return root;
        }

        public static GameObject SavePrefab(GameObject root, string name)
        {
            BattleEditorUtility.EnsureFolder(PrefabsFolder);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabsFolder}/{name}.prefab");
            Object.DestroyImmediate(root);

            return prefab;
        }
    }
}
