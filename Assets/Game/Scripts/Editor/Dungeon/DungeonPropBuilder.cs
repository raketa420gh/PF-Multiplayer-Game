using Game.Scripts.Battle;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Dungeon props assembled from procedural meshes and textured materials. Returns unsaved objects for the content builder.
    internal static class DungeonPropBuilder
    {
        public const string PrefabsFolder = "Assets/Game/Prefabs/Dungeon";
        public const string MaterialsFolder = "Assets/Game/Materials/Dungeon";
        public const float WallHeight = 4.5f;
        public const float WallThickness = 0.6f;

        public static Material StoneWall => Textured("StoneWall", "StoneWall", 0.5f, 0.15f);
        public static Material Cobble => Textured("Cobble", "Cobble", 0.5f, 0.15f);
        public static Material Ashlar => Textured("Ashlar", "Ashlar", 0.5f, 0.12f);
        public static Material Flagstone => Textured("Flagstone", "Flagstone", 0.5f, 0.18f);
        public static Material WoodPlanks => Textured("WoodPlanks", "WoodPlanks", 0.5f, 0.25f);
        public static Material DarkWood => Textured("DarkWood", "DarkWood", 0.5f, 0.3f);
        public static Material RustyMetal => Textured("RustyMetal", "RustyMetal", 0.6f, 0.4f, 0.6f);
        public static Material Bone => Textured("Bone", "Bone", 0.8f, 0.35f);
        public static Material Gold => Textured("Gold", "Gold", 0.8f, 0.75f, 0.8f);
        public static Material Fire => FireMaterial();
        public static Material PortalBlue => Emissive("PortalBlue", new Color(0.2f, 0.5f, 1f), 4f);
        public static Material PortalRed => Emissive("PortalRed", new Color(1f, 0.25f, 0.15f), 4f);
        public static Material ShrineGlow => Emissive("ShrineGlow", new Color(0.9f, 0.8f, 0.4f), 2.5f);
        public static Material SwarmWall => TransparentUnlit("SwarmWall", new Color(0.12f, 0.02f, 0.1f, 0.45f));

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
            Texture2D occlusion = DungeonTextureBuilder.LoadOcclusion(texture);
            material.SetTexture("_OcclusionMap", occlusion);
            material.SetFloat("_OcclusionStrength", 1f);

            if (occlusion != null)
                material.EnableKeyword("_OCCLUSIONMAP");
            else
                material.DisableKeyword("_OCCLUSIONMAP");

            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            material.SetTextureScale("_BaseMap", Vector2.one * tiling);
            EditorUtility.SetDirty(material);

            return material;
        }

        public static Material TransparentEmissive(string name, Color color, float intensity)
        {
            Material material = Emissive(name, color, intensity);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(material);

            return material;
        }

        /// Flat see-through tint: no lighting, emission or bloom.
        public static Material TransparentUnlit(string name, Color color)
        {
            Emissive(name, color, 0f).shader = Shader.Find("Universal Render Pipeline/Unlit");
            Material material = TransparentEmissive(name, color, 0f);
            material.DisableKeyword("_EMISSION");

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

        public static GameObject CandleCluster()
        {
            Mesh candle = new DungeonMeshBuilder(1f).Cylinder(new Vector3(0f, 0.1f, 0f), 0.03f, 0.2f, 6).Save("Candle");
            Mesh stub = new DungeonMeshBuilder(1f).Cylinder(new Vector3(0f, 0.06f, 0f), 0.035f, 0.12f, 6).Save("CandleStub");
            Mesh wax = new DungeonMeshBuilder(1f).Cylinder(new Vector3(0f, 0.01f, 0f), 0.16f, 0.02f, 10, 0.1f).Save("Wax");
            GameObject root = new GameObject("CandleCluster");
            MeshObject("Wax", root.transform, wax, Bone, default, default, false, false);
            MeshObject("Candle", root.transform, candle, Bone, new Vector3(0f, 0f, 0f), default, false, false);
            MeshObject("Candle", root.transform, stub, Bone, new Vector3(0.09f, 0f, 0.04f), default, false, false);
            MeshObject("Candle", root.transform, stub, Bone, new Vector3(-0.06f, 0f, 0.08f), default, false, false);
            Flame(root.transform, new Vector3(0f, 0.21f, 0f), 0.3f);
            PointLight(root.transform, new Vector3(0f, 0.3f, 0f), new Color(1f, 0.7f, 0.35f), 4f, 1.1f, true);

            return root;
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

        public static GameObject Table()
        {
            GameObject root = Solid("Table", "Table_Large");
            GameObject candles = (GameObject)PrefabUtility.InstantiatePrefab(DungeonKitBuilder.Load("CandleStick_Triple"), root.transform);
            candles.transform.localPosition = new Vector3(1f, 0.82f, 0.1f);

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
            return Wrap("WallTorch", "Torch_Metal", 1f);
        }

        /// Kit prefab (with its fire and collider) under a root of the project's own name.
        private static GameObject Wrap(string name, string kit, float scale)
        {
            GameObject root = new GameObject(name);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(DungeonKitBuilder.Load(kit), root.transform);
            instance.transform.localScale = Vector3.one * scale;

            return root;
        }

        /// Kit model with a box collider, ready to receive container logic.
        private static GameObject Solid(string name, string kit)
        {
            GameObject root = new GameObject(name);
            Bounds bounds = DungeonKitBuilder.Bounds(DungeonKitBuilder.Model(kit, root.transform));
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = bounds.size;

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

        /// Wooden armour stand: a mannequin of the animation packs wearing outfit parts, frozen in a pose of the animation library.
        public static GameObject ArmorStand(string name, bool isFemale, string clip, float time, params OutfitPart[] parts)
        {
            Mesh mesh = new DungeonMeshBuilder(0.6f).Cylinder(new Vector3(0f, 0.04f, 0f), 0.5f, 0.08f, 12, 0.44f).Save("StandBase");
            GameObject root = new GameObject(name);
            MeshObject("Base", root.transform, mesh, DarkWood, default, default, false, false);
            Figure(root, name, isFemale, clip, time, 0.08f, BattleCharacterBuilder.LoadMaterial(BattleCharacterBuilder.WoodMaterial), null, parts);

            return root;
        }

        /// Stone statue of an adventurer on a plinth.
        public static GameObject Statue(string name, bool isFemale, string clip, float time, params OutfitPart[] parts)
        {
            Mesh mesh = new DungeonMeshBuilder(0.5f)
                .Box(new Vector3(0f, 0.12f, 0f), new Vector3(1.1f, 0.24f, 1.1f))
                .Box(new Vector3(0f, 0.3f, 0f), new Vector3(0.9f, 0.12f, 0.9f))
                .Save("StatuePlinth");
            GameObject root = new GameObject(name);
            MeshObject("Plinth", root.transform, mesh, StoneWall, default, default, false, false);
            Figure(root, name, isFemale, clip, time, 0.36f, null, StoneWall, parts);

            return root;
        }

        /// Remains of an adventurer: a skeleton in what is left of its clothes, lying where the death animation ends.
        public static GameObject Fallen(string name, bool isFemale, params OutfitPart[] parts)
        {
            AnimationClip death = BattleEditorUtility.LoadLibraryClip("Death01");
            GameObject root = new GameObject(name);
            GameObject figure = BattleCharacterBuilder.CreateFigure(name, isFemale, death, death.length, Bone, null, parts);
            figure.transform.SetParent(root.transform, false);
            figure.transform.localPosition = new Vector3(0f, 0f, 0.6f);

            return root;
        }

        private static void Figure(GameObject root, string name, bool isFemale, string clip, float time, float height, Material body, Material surface,
            OutfitPart[] parts)
        {
            GameObject figure = BattleCharacterBuilder.CreateFigure(name, isFemale, BattleEditorUtility.LoadLibraryClip(clip), time, body, surface, parts);
            figure.transform.SetParent(root.transform, false);
            figure.transform.localPosition = new Vector3(0f, height, 0f);
            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 1f, 0f);
            collider.radius = 0.4f;
            collider.height = 2f;
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

        /// A pedestal portal stands in place; one without it is a bare floating ring that shows up out of nowhere.
        public static GameObject Portal(Material material, bool hasPedestal)
        {
            Mesh ring = new DungeonMeshBuilder(1f).Cylinder(Vector3.zero, 0.95f, 0.08f, 24, 0.95f).Save("PortalRing");
            Mesh disc = new DungeonMeshBuilder(1f).Cylinder(Vector3.zero, 0.8f, 0.02f, 24).Save("PortalDisc");
            GameObject root = new GameObject("Portal");

            if (hasPedestal)
            {
                Mesh pedestal = new DungeonMeshBuilder(0.5f)
                    .Cylinder(new Vector3(0f, 0.15f, 0f), 0.9f, 0.3f, 12)
                    .Box(new Vector3(0f, 0.9f, -0.5f), new Vector3(0.8f, 1.5f, 0.3f))
                    .Save("PortalPedestal");
                MeshObject("Pedestal", root.transform, pedestal, StoneWall, default, default, true, false);
            }

            GameObject visual = BattleEditorUtility.CreateChild("Visual", root.transform, new Vector3(0f, hasPedestal ? 1.5f : 1.15f, 0f));
            visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            MeshObject("Ring", visual.transform, ring, material, default, default, false, false);
            MeshObject("Disc", visual.transform, disc, material, default, default, false, false);
            PointLight(visual.transform, new Vector3(0f, 0f, -0.3f), material.GetColor("_BaseColor"), 8f, 3f, true);

            return root;
        }

        /// Stone lectern with a glowing orb of the portal's colour: the thing to hold F on before an escape portal opens.
        public static GameObject PortalPedestal(Material material, Vector3 offset)
        {
            Mesh stone = new DungeonMeshBuilder(0.5f)
                .Cylinder(offset + new Vector3(0f, 0.1f, 0f), 0.5f, 0.2f, 12)
                .Box(offset + new Vector3(0f, 0.65f, 0f), new Vector3(0.36f, 0.9f, 0.36f))
                .Cylinder(offset + new Vector3(0f, 1.15f, 0f), 0.34f, 0.1f, 12)
                .Save("PortalPedestalStone");
            Mesh orb = new DungeonMeshBuilder(1f).Cylinder(offset + new Vector3(0f, 1.32f, 0f), 0.14f, 0.24f, 12, 0.06f).Save("PortalPedestalOrb");
            GameObject root = new GameObject("Pedestal");
            MeshObject("Stone", root.transform, stone, StoneWall, default, default, true, false);
            MeshObject("Orb", root.transform, orb, material, default, default, false, false);
            PointLight(root.transform, offset + new Vector3(0f, 1.5f, 0f), material.GetColor("_BaseColor"), 4f, 1.5f, true);

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

        public static GameObject SavePrefab(GameObject root, string name)
        {
            BattleEditorUtility.EnsureFolder(PrefabsFolder);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabsFolder}/{name}.prefab");
            Object.DestroyImmediate(root);

            return prefab;
        }
    }
}
