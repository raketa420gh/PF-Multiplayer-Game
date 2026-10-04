using System.Linq;
using Fusion;
using Game.Scripts.Battle;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Dungeon;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Game.Scripts.Editor.Battle
{
    /// Gameplay test ground: naked adventurer, weapon table, chests, dummies, inventory and looting as in the dungeon, developer spawns.
    internal static class BattleSceneBuilder
    {
        private const string PopupPath = BattleEditorUtility.PrefabsFolder + "/DamagePopup.prefab";
        private const string NavMeshPath = "Assets/Game/Scenes/BattleScene/NavMesh.asset";
        private const float RampartHeight = 3f;

        private static readonly string[] s_monsters = { "SkeletonSwordsman", "SkeletonArcher", "FlyingHead", "SkeletonChampion" };
        private static readonly string[] s_monsterLabels = { "Skeleton", "Archer", "Flying Head", "Champion" };
        private static readonly string[] s_dungeonOnlyHud = { "Minimap", "TimerBack", "Timer", "Swarm", "ModuleBack", "Module", "Floor" };
        /// Decor from the character packs: an armoury row of outfit stands by the spawn, statues on the towers and fallen bodies further out.
        private static readonly (string prefab, Vector3 position, float yaw)[] s_figures =
        {
            ("StandPeasantMale", new Vector3(-10.5f, 0f, -12.5f), 60f), ("StandPeasantFemale", new Vector3(-10.5f, 0f, -10f), 90f),
            ("StandRangerMale", new Vector3(10.5f, 0f, -12.5f), -60f), ("StandRangerFemale", new Vector3(10.5f, 0f, -10f), -90f),
            ("StatueGuardian", new Vector3(-7f, 0f, 14f), 180f), ("StatueGuardian", new Vector3(7f, 0f, 14f), 180f),
            ("StatueMage", new Vector3(-20.5f, RampartHeight, 24.5f), 135f), ("StatuePilgrim", new Vector3(20.5f, RampartHeight, 30f), -135f),
            ("FallenPeasant", new Vector3(-12f, 0f, 8f), 70f), ("FallenRanger", new Vector3(13f, 0f, 12f), -40f)
        };

        private static readonly string[] s_tableItems =
        {
            "Arming Sword", "Round Shield", "Falchion", "Zweihander", "Battle Axe", "Spear", "Flanged Mace", "Rondel Dagger", "Recurve Bow",
            "Crossbow", "Torch", "Bandage", "Potion of Healing", "Potion of Protection", "Ale", "Throwing Knife", "Francisca Axe",
            "Short Sword", "Rapier", "Viking Sword", "Hatchet", "Morning Star", "Stiletto Dagger", "Felling Axe", "War Maul", "Halberd", "Potion of Invisibility"
        };

        [MenuItem("Tools/Game/Battle/Build Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, BattleEditorUtility.ScenePath);

            Transform world = BuildWorld();
            DungeonSceneBuilder.BuildVolume($"{DungeonContentBuilder.ConfigsFolder}/SandboxVolume.asset", 0f);
            Camera camera = BuildCamera();
            Transform spawns = new GameObject("[Spawns]").transform;
            ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(DungeonContentBuilder.DatabasePath);
            DungeonContext context = BuildSystem(camera, world, spawns, database, out SandboxDirector director);

            GameObject canvas = DungeonUiBuilder.Build(new DungeonUiBuilder.Inputs
            {
                Context = context,
                Database = database,
                Camera = camera,
                PreviewRig = AssetDatabase.LoadAssetAtPath<GameObject>(DungeonContentBuilder.Prefab("PreviewRig")),
                PieceSet = AssetDatabase.LoadAssetAtPath<ArmorPieceSetConfig>($"{DungeonContentBuilder.ConfigsFolder}/ArmorPieces.asset"),
                FloorMaps = new Texture2D[0],
                ModuleNames = new string[0],
                Title = "Training Grounds"
            });

            foreach (string name in s_dungeonOnlyHud)
                canvas.transform.Find("HUD/" + name).gameObject.SetActive(false);

            DungeonUiBuilder.BuildDevPanel(canvas, director, s_monsterLabels);
            DungeonMapBuilder.BakeNavMesh(world.gameObject, NavMeshPath);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, BattleEditorUtility.ScenePath);
            AddToBuildSettings();
            Debug.Log($"[{nameof(BattleSceneBuilder)}] Scene built: {BattleEditorUtility.ScenePath}");
        }

        private static Transform BuildWorld()
        {
            Transform world = new GameObject("[World]").transform;
            Material ground = BattleEditorUtility.GetMaterial("Ground", new Color(0.32f, 0.36f, 0.3f), 0f, 0.1f);
            Material stone = BattleEditorUtility.GetMaterial("Stone", new Color(0.5f, 0.5f, 0.52f), 0f, 0.15f);
            Material target = BattleEditorUtility.GetMaterial("Target", new Color(0.8f, 0.3f, 0.2f));

            Light light = BattleEditorUtility.CreateChild("Directional Light", world).AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            Box("Ground", world, new Vector3(0f, -0.5f, 5f), new Vector3(50f, 1f, 60f), ground);
            Box("WallNorth", world, new Vector3(0f, 1.5f, 35f), new Vector3(50f, 3f, 1f), stone);
            Box("WallSouth", world, new Vector3(0f, 1.5f, -25f), new Vector3(50f, 3f, 1f), stone);
            Box("WallEast", world, new Vector3(25f, 1.5f, 5f), new Vector3(1f, 3f, 60f), stone);
            Box("WallWest", world, new Vector3(-25f, 1.5f, 5f), new Vector3(1f, 3f, 60f), stone);
            Box("CoverBlock", world, new Vector3(-10f, 1f, 3f), new Vector3(2f, 2f, 2f), stone);
            Box("CoverLow", world, new Vector3(9f, 0.55f, 2f), new Vector3(4f, 1.1f, 0.6f), stone);
            Box("Pillar", world, new Vector3(4f, 1.5f, 12f), new Vector3(1f, 3f, 1f), stone);
            Box("Step", world, new Vector3(-14f, 0.15f, -6f), new Vector3(3f, 0.3f, 3f), stone);

            foreach (Vector3 position in new[] { new Vector3(-7f, 1.2f, 32f), new Vector3(-1f, 1.2f, 33f), new Vector3(4f, 1.2f, 32f), new Vector3(17.5f, RampartHeight + 1.2f, 31.5f) })
                Box("ArcheryTarget", world, position, new Vector3(1.2f, 1.2f, 0.3f), target);

            BuildRampart(world);
            BuildDressing(world);

            foreach ((string prefab, Vector3 position, float yaw) in s_figures)
                DungeonMapBuilder.Place(AssetDatabase.LoadAssetAtPath<GameObject>(DungeonContentBuilder.Prefab(prefab)), world, position, yaw);

            return world;
        }

        /// Vertical test course: two stone towers with stairs from different sides and a plank bridge with one open edge.
        private static void BuildRampart(Transform world)
        {
            Material stone = DungeonPropBuilder.Cobble;
            Material wood = DungeonPropBuilder.WoodPlanks;
            const float height = RampartHeight;
            float run = DungeonStructureBuilder.StairRun(height);
            DungeonStructureBuilder.Block(world, "TowerWest", new Vector3(-17.5f, height * 0.5f, 27.5f), new Vector3(11f, height, 11f), stone);
            DungeonStructureBuilder.Block(world, "TowerEast", new Vector3(17.5f, height * 0.5f, 27.5f), new Vector3(11f, height, 11f), stone);
            DungeonStructureBuilder.Block(world, "Bridge", new Vector3(0f, height - 0.15f, 27.5f), new Vector3(24f, 0.3f, 2.4f), wood);
            DungeonStructureBuilder.Stairs(world, new Vector3(-17.5f, 0f, 22f - run), 0f, 3f, height, stone);
            DungeonStructureBuilder.Stairs(world, new Vector3(12f - run, 0f, 31f), 90f, 3f, height, stone);

            foreach (float x in new[] { -4f, 4f })
                DungeonStructureBuilder.Block(world, "BridgePost", new Vector3(x, (height - 0.3f) * 0.5f, 27.5f), new Vector3(0.4f, height - 0.3f, 0.4f), DungeonPropBuilder.DarkWood);

            DungeonStructureBuilder.Rail(world, new Vector3(-23f, height, 22f), new Vector3(-19f, height, 22f));
            DungeonStructureBuilder.Rail(world, new Vector3(-16f, height, 22f), new Vector3(-12f, height, 22f));
            DungeonStructureBuilder.Rail(world, new Vector3(-12f, height, 22f), new Vector3(-12f, height, 26.3f));
            DungeonStructureBuilder.Rail(world, new Vector3(-12f, height, 28.7f), new Vector3(-12f, height, 33f));
            DungeonStructureBuilder.Rail(world, new Vector3(-12f, height, 28.7f), new Vector3(12f, height, 28.7f));
            DungeonStructureBuilder.Rail(world, new Vector3(12f, height, 22f), new Vector3(23f, height, 22f));
            DungeonStructureBuilder.Rail(world, new Vector3(12f, height, 22f), new Vector3(12f, height, 26.3f));
        }

        /// Props of the fantasy kit: a smithy and a camp by the spawn, market cover in the field, stores on the towers.
        private static void BuildDressing(Transform world)
        {
            (string kit, Vector3 position, float yaw)[] props =
            {
                ("WeaponStand", new Vector3(-9.4f, 0f, -8.6f), 20f), ("WeaponStand", new Vector3(9.4f, 0f, -8.6f), -20f),
                ("Anvil_Log", new Vector3(-20f, 0f, -19f), 30f), ("Whetstone", new Vector3(-17f, 0f, -22f), 10f), ("Workbench", new Vector3(-22.5f, 0f, -22.5f), 45f),
                ("Bucket_Metal", new Vector3(-18.6f, 0f, -19.4f), 0f), ("Cauldron", new Vector3(-22.6f, 0f, -17f), 0f),
                ("Bed_Twin1", new Vector3(21f, 0f, -23.1f), 0f), ("Bed_Twin2", new Vector3(18f, 0f, -23.1f), 0f), ("Nightstand_Shelf", new Vector3(19.5f, 0f, -24.1f), 0f),
                ("Bench", new Vector3(17f, 0f, -17f), 30f), ("Barrel_Holder", new Vector3(23.4f, 0f, -17f), -90f), ("Bag", new Vector3(23.2f, 0f, -19.4f), 40f),
                ("Stall_Empty", new Vector3(21f, 0f, 9f), -90f), ("FarmCrate_Apple", new Vector3(21.2f, 0.82f, 8.6f), -90f), ("FarmCrate_Carrot", new Vector3(21.2f, 0.82f, 9.5f), -80f),
                ("Stall_Cart_Empty", new Vector3(20.5f, 0f, 14f), -100f), ("Barrel_Apples", new Vector3(22.8f, 0f, 11.4f), 0f),
                ("Crate_Wooden", new Vector3(-4f, 0f, 6f), 10f), ("Crate_Wooden", new Vector3(-3f, 0f, 6.1f), -5f), ("Crate_Metal", new Vector3(-3.5f, 0.92f, 6f), 20f),
                ("Barrel", new Vector3(2f, 0f, 17f), 0f), ("Barrel", new Vector3(2.9f, 0f, 17.4f), 0f), ("Barrel", new Vector3(13f, 0f, 16f), 0f),
                ("Crate_Wooden", new Vector3(-13f, 0f, 14f), 30f), ("Vase_2", new Vector3(-12f, 0f, 15f), 0f), ("Bag", new Vector3(-14.2f, 0f, 14.6f), 70f),
                ("Crate_Wooden", new Vector3(-21f, RampartHeight, 31f), 15f), ("Barrel", new Vector3(-22f, RampartHeight, 29.4f), 0f),
                ("Crate_Wooden", new Vector3(21f, RampartHeight, 24f), -10f), ("Crate_Metal", new Vector3(22f, RampartHeight, 25.2f), 25f),
                ("Dummy", new Vector3(-22.5f, 0f, 4f), 90f), ("Dummy", new Vector3(-22.5f, 0f, 8f), 90f), ("Peg_Rack", new Vector3(-24.48f, 1.8f, 6f), 90f),
                ("Shield_Wooden", new Vector3(-24.48f, 1.9f, 2f), 90f), ("Shield_Wooden", new Vector3(-24.48f, 1.9f, 10f), 90f),
                ("Banner_1_Cloth", new Vector3(0f, 2.9f, 34.44f), 180f), ("Banner_2_Cloth", new Vector3(-6f, 2.8f, 34.44f), 180f), ("Banner_2_Cloth", new Vector3(6f, 2.8f, 34.44f), 180f)
            };

            foreach ((string kit, Vector3 position, float yaw) in props)
                DungeonMapBuilder.Place(DungeonKitBuilder.Load(kit), world, position, yaw);

            DungeonMapBuilder.Place(AssetDatabase.LoadAssetAtPath<GameObject>(DungeonContentBuilder.Prefab("Brazier")), world, new Vector3(-21f, 0f, -21f), 0f);
        }

        private static void Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject box = BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, name, parent, position, Vector3.zero, scale, material, true);
            box.isStatic = true;
        }

        private static Camera BuildCamera()
        {
            GameObject go = new GameObject("[Camera]") { tag = "MainCamera" };
            go.transform.SetPositionAndRotation(new Vector3(0f, 4f, -16f), Quaternion.Euler(15f, 0f, 0f));
            Camera camera = go.AddComponent<Camera>();
            camera.nearClipPlane = 0.04f;
            go.AddComponent<AudioListener>();
            go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;

            return camera;
        }

        private static DungeonContext BuildSystem(Camera camera, Transform world, Transform spawns, ItemDatabase database, out SandboxDirector director)
        {
            GameObject system = new GameObject("[System]");
            NetworkEvents events = system.AddComponent<NetworkEvents>();
            system.AddComponent<BattleBootstrapper>();
            BattleInputPolling input = system.AddComponent<BattleInputPolling>();
            BattleContext battle = system.AddComponent<BattleContext>();
            system.AddComponent<CombatDebugView>();
            BattleFeedback feedback = BuildFeedback(system.transform);

            BattleEditorUtility.Set(input, "_networkEvents", events);

            SerializedObject so = new SerializedObject(battle);
            BattleEditorUtility.Set(so, "_camera", camera);
            BattleEditorUtility.Set(so, "_input", input);
            BattleEditorUtility.Set(so, "_feedback", feedback);
            so.ApplyModifiedPropertiesWithoutUndo();

            DungeonContext context = system.AddComponent<DungeonContext>();
            so = new SerializedObject(context);
            BattleEditorUtility.Set(so, "_battle", battle);
            BattleEditorUtility.Set(so, "_items", database);
            BattleEditorUtility.Set(so, "_classes", DungeonSceneBuilder.LoadClasses());
            BattleEditorUtility.Set(so, "_config", AssetDatabase.LoadAssetAtPath<DungeonConfig>(DungeonContentBuilder.DungeonConfigPath));
            BattleEditorUtility.Set(so, "_isSandbox", true);
            so.ApplyModifiedPropertiesWithoutUndo();

            Transform[] playerPoints = Enumerable.Range(0, 4)
                .Select(i => Point(spawns, "Player" + i, new Vector3((i - 1.5f) * 3f, 0f, -12f), 0f))
                .ToArray();

            director = system.AddComponent<SandboxDirector>();
            so = new SerializedObject(director);
            BattleEditorUtility.Set(so, "_networkEvents", events);
            BattleEditorUtility.Set(so, "_context", context);
            BattleEditorUtility.Set(so, "_sessionPrefab", LoadNetworkObject(DungeonContentBuilder.Prefab("PlayerSession")));
            BattleEditorUtility.Set(so, "_adventurerPrefab", LoadNetworkObject(DungeonContentBuilder.Prefab("Adventurer")));
            BattleEditorUtility.Set(so, "_worldItemPrefab", LoadNetworkObject(DungeonContentBuilder.Prefab("WorldItem")));
            BattleEditorUtility.Set(so, "_botPrefab", LoadNetworkObject(BattleContentBuilder.BotPath));
            BattleEditorUtility.Set(so, "_monsters", s_monsters.Select(name => LoadNetworkObject(DungeonContentBuilder.Prefab(name))).ToArray());
            BattleEditorUtility.Set(so, "_playerSpawns", playerPoints);
            BattleEditorUtility.Set(so, "_containers", new[]
            {
                PlaceContainer("SmallOakChest", world, new Vector3(-7f, 0f, -10f), 90f),
                PlaceContainer("LargeOakChest", world, new Vector3(7f, 0f, -10f), -90f)
            });

            (Vector3 position, string prefab)[] dummies =
            {
                (new Vector3(-5f, 0f, -5f), BattleContentBuilder.DummyPath),
                (new Vector3(-2f, 0f, -5f), BattleContentBuilder.ShieldDummyPath),
                (new Vector3(3f, 0f, -5f), BattleContentBuilder.DummyPath),
                (new Vector3(12f, 0f, 6f), BattleContentBuilder.DummyPath)
            };
            so.FindProperty("_dummies").arraySize = dummies.Length;

            for (int i = 0; i < dummies.Length; i++)
            {
                BattleEditorUtility.Set(so, $"_dummies.Array.data[{i}].Point", Point(spawns, "Dummy" + i, dummies[i].position, 180f));
                BattleEditorUtility.Set(so, $"_dummies.Array.data[{i}].Prefab", LoadNetworkObject(dummies[i].prefab));
            }

            BuildTable(so, world, spawns, database);
            so.ApplyModifiedPropertiesWithoutUndo();

            return context;
        }

        /// A row of tables in front of the spawn with one of every weapon plus belt items, each on its own point.
        private static void BuildTable(SerializedObject director, Transform world, Transform spawns, ItemDatabase database)
        {
            const float step = 0.42f;
            GameObject table = AssetDatabase.LoadAssetAtPath<GameObject>(DungeonContentBuilder.Prefab("Table"));

            for (int i = 0; i < 4; i++)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(table, world);
                instance.transform.position = new Vector3((i - 1.5f) * 2.9f, 0f, -9f);
            }

            director.FindProperty("_table").arraySize = s_tableItems.Length;

            for (int i = 0; i < s_tableItems.Length; i++)
            {
                ItemConfig item = database.Find(s_tableItems[i]);
                Vector3 position = new Vector3((i - (s_tableItems.Length - 1) * 0.5f) * step, 0.82f, -9.3f);
                BattleEditorUtility.Set(director, $"_table.Array.data[{i}].Item", item);
                BattleEditorUtility.Set(director, $"_table.Array.data[{i}].Count", item.MaxStack);
                BattleEditorUtility.Set(director, $"_table.Array.data[{i}].Point", Point(spawns, "Table" + i, position, 0f));
            }
        }

        private static ContainerComponent PlaceContainer(string prefab, Transform world, Vector3 position, float yaw)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(DungeonContentBuilder.Prefab(prefab)), world);
            instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));

            return instance.GetComponent<ContainerComponent>();
        }

        private static NetworkObject LoadNetworkObject(string path)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<NetworkObject>();
        }

        private static Transform Point(Transform parent, string name, Vector3 position, float yaw)
        {
            Transform point = BattleEditorUtility.CreateChild(name, parent, position).transform;
            point.rotation = Quaternion.Euler(0f, yaw, 0f);

            return point;
        }

        private static BattleFeedback BuildFeedback(Transform parent)
        {
            GameObject go = BattleEditorUtility.CreateChild("Feedback", parent);
            BattleFeedback feedback = go.AddComponent<BattleFeedback>();
            AudioSource audio = go.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 1f;
            audio.rolloffMode = AudioRolloffMode.Linear;
            audio.minDistance = 2f;
            audio.maxDistance = 28f;
            audio.volume = 0.7f;

            Material particle = BattleEditorUtility.GetUnlitMaterial("HitParticle", Color.white);

            SerializedObject so = new SerializedObject(feedback);
            BattleEditorUtility.Set(so, "_hitVfx", BuildVfx(go.transform, "HitVfx", particle, new Color(0.9f, 0.15f, 0.1f), new Color(1f, 0.5f, 0.3f), 16, 3f, 0.06f));
            BattleEditorUtility.Set(so, "_blockVfx", BuildVfx(go.transform, "BlockVfx", particle, new Color(1f, 0.9f, 0.5f), Color.white, 24, 5f, 0.035f));
            BattleEditorUtility.Set(so, "_popupPrefab", BuildPopup());
            BattleEditorUtility.Set(so, "_audioSource", audio);
            BattleEditorUtility.Set(so, "_hitClips", BattleAudioBuilder.Load(BattleAudioBuilder.Hit));
            BattleEditorUtility.Set(so, "_blockClips", BattleAudioBuilder.Load(BattleAudioBuilder.Block));
            BattleEditorUtility.Set(so, "_worldClips", BattleAudioBuilder.Load(BattleAudioBuilder.Clank));
            BattleEditorUtility.Set(so, "_swingClips", BattleAudioBuilder.Load(BattleAudioBuilder.Swing));
            BattleEditorUtility.Set(so, "_shotClips", BattleAudioBuilder.Load(BattleAudioBuilder.Shot));
            so.ApplyModifiedPropertiesWithoutUndo();

            return feedback;
        }

        private static ParticleSystem BuildVfx(Transform parent, string name, Material material, Color colorA, Color colorB,
            int count, float speed, float size)
        {
            ParticleSystem ps = BattleEditorUtility.CreateChild(name, parent).AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.duration = 0.3f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
            main.gravityModifier = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 128;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 50f;
            shape.radius = 0.03f;

            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            return ps;
        }

        private static TextMeshPro BuildPopup()
        {
            GameObject go = new GameObject("DamagePopup");
            TextMeshPro text = go.AddComponent<TextMeshPro>();
            text.fontSize = 2.2f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.rectTransform.sizeDelta = new Vector2(3f, 0.6f);
            text.text = "0";

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, PopupPath);
            Object.DestroyImmediate(go);

            return prefab.GetComponent<TextMeshPro>();
        }

        private static void AddToBuildSettings()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

            if (scenes.Any(scene => scene.path == BattleEditorUtility.ScenePath))
                return;

            EditorBuildSettings.scenes = scenes.Append(new EditorBuildSettingsScene(BattleEditorUtility.ScenePath, true)).ToArray();
        }
    }
}
