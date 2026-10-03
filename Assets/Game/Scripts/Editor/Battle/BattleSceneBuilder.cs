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

        private static readonly string[] s_monsters = { "SkeletonSwordsman", "SkeletonArcher", "Zombie", "SkeletonChampion" };
        private static readonly string[] s_monsterLabels = { "Skeleton", "Archer", "Zombie", "Champion" };
        private static readonly string[] s_dungeonOnlyHud = { "Minimap", "TimerBack", "Timer", "Swarm", "ModuleBack", "Module", "Floor" };
        /// Decor from the character packs: an armoury row of outfit stands by the spawn, statues and fallen bodies further out.
        private static readonly (string prefab, Vector3 position, float yaw)[] s_figures =
        {
            ("StandPeasantMale", new Vector3(-10.5f, 0f, -12.5f), 60f), ("StandPeasantFemale", new Vector3(-10.5f, 0f, -10f), 90f),
            ("StandRangerMale", new Vector3(10.5f, 0f, -12.5f), -60f), ("StandRangerFemale", new Vector3(10.5f, 0f, -10f), -90f),
            ("StatueGuardian", new Vector3(-7f, 0f, 14f), 180f), ("StatueGuardian", new Vector3(7f, 0f, 14f), 180f),
            ("StatueMage", new Vector3(-20f, 0f, 30f), 135f), ("StatuePilgrim", new Vector3(20f, 0f, 30f), -135f),
            ("FallenPeasant", new Vector3(-12f, 0f, 8f), 70f), ("FallenRanger", new Vector3(13f, 0f, 12f), -40f)
        };

        private static readonly string[] s_tableItems =
        {
            "Arming Sword", "Round Shield", "Falchion", "Zweihander", "Battle Axe", "Spear", "Flanged Mace", "Rondel Dagger", "Recurve Bow",
            "Crossbow", "Torch", "Bandage", "Potion of Healing", "Potion of Protection", "Ale", "Throwing Knife", "Francisca Axe"
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
                ModuleNames = new string[0]
            });

            foreach (string name in s_dungeonOnlyHud)
                canvas.transform.Find("HUD/" + name).gameObject.SetActive(false);

            DungeonUiBuilder.BuildDevPanel(canvas, director, s_monsterLabels);

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

            for (int i = 0; i < 3; i++)
                Box("ArcheryTarget" + i, world, new Vector3(14f + i * 3f, 1.2f, 20f + i * 5f), new Vector3(1.2f, 1.2f, 0.3f), target);

            foreach ((string prefab, Vector3 position, float yaw) in s_figures)
            {
                GameObject figure = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(DungeonContentBuilder.Prefab(prefab)), world);
                figure.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            }

            return world;
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
            const float step = 0.52f;
            GameObject table = AssetDatabase.LoadAssetAtPath<GameObject>(DungeonContentBuilder.Prefab("Table"));

            for (int i = 0; i < 4; i++)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(table, world);
                instance.transform.position = new Vector3((i - 1.5f) * 2.2f, 0f, -9f);
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
            audio.spatialBlend = 0f;
            audio.volume = 0.6f;

            Material particle = BattleEditorUtility.GetUnlitMaterial("HitParticle", Color.white);

            SerializedObject so = new SerializedObject(feedback);
            BattleEditorUtility.Set(so, "_hitVfx", BuildVfx(go.transform, "HitVfx", particle, new Color(0.9f, 0.15f, 0.1f), new Color(1f, 0.5f, 0.3f), 16, 3f, 0.06f));
            BattleEditorUtility.Set(so, "_blockVfx", BuildVfx(go.transform, "BlockVfx", particle, new Color(1f, 0.9f, 0.5f), Color.white, 24, 5f, 0.035f));
            BattleEditorUtility.Set(so, "_popupPrefab", BuildPopup());
            BattleEditorUtility.Set(so, "_audioSource", audio);
            BattleEditorUtility.Set(so, "_hitClip", AssetDatabase.LoadAssetAtPath<AudioClip>(BattleAudioBuilder.HitPath));
            BattleEditorUtility.Set(so, "_blockClip", AssetDatabase.LoadAssetAtPath<AudioClip>(BattleAudioBuilder.BlockPath));
            BattleEditorUtility.Set(so, "_swingClip", AssetDatabase.LoadAssetAtPath<AudioClip>(BattleAudioBuilder.SwingPath));
            BattleEditorUtility.Set(so, "_shotClip", AssetDatabase.LoadAssetAtPath<AudioClip>(BattleAudioBuilder.ShotPath));
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
