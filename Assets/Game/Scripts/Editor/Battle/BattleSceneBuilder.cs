using System.Linq;
using Fusion;
using Game.Scripts.Battle;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Scripts.Editor.Battle
{
    internal static class BattleSceneBuilder
    {
        private const string PopupPath = BattleEditorUtility.PrefabsFolder + "/DamagePopup.prefab";

        private const string HelpText =
            "<b>Controls</b>\n" +
            "WASD — move, Shift — sprint, Space — jump\n" +
            "Ctrl / C — crouch (crouch + look down to duck under swings)\n" +
            "LMB — attack / draw bow, RMB — block / cancel draw\n" +
            "LMB inside the green window — continue combo\n" +
            "1 — sword & shield, 2 — greatsword, 3 — bow\n" +
            "4 — sword in left hand (RMB attack, LMB block)\n" +
            "B — bot mode: Passive / Block / Attack / Spar\n" +
            "H — toggle help, Esc — release cursor";

        [MenuItem("Tools/Game/Battle/Build Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildWorld();
            Camera camera = BuildCamera();
            Transform spawns = new GameObject("[Spawns]").transform;
            BattleContext context = BuildSystem(camera, spawns);
            BuildHud(context);

            EditorSceneManager.SaveScene(scene, BattleEditorUtility.ScenePath);
            AddToBuildSettings();
            Debug.Log($"[{nameof(BattleSceneBuilder)}] Scene built: {BattleEditorUtility.ScenePath}");
        }

        private static void BuildWorld()
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
            go.AddComponent<AudioListener>();
            go.AddComponent<UniversalAdditionalCameraData>();

            return camera;
        }

        private static BattleContext BuildSystem(Camera camera, Transform spawns)
        {
            GameObject system = new GameObject("[System]");
            NetworkEvents events = system.AddComponent<NetworkEvents>();
            system.AddComponent<BattleBootstrapper>();
            BattleInputPolling input = system.AddComponent<BattleInputPolling>();
            BattleSpawner spawner = system.AddComponent<BattleSpawner>();
            BattleContext context = system.AddComponent<BattleContext>();
            system.AddComponent<CombatDebugView>();
            BattleFeedback feedback = BuildFeedback(system.transform);

            BattleEditorUtility.Set(input, "_networkEvents", events);

            SerializedObject so = new SerializedObject(context);
            BattleEditorUtility.Set(so, "_camera", camera);
            BattleEditorUtility.Set(so, "_input", input);
            BattleEditorUtility.Set(so, "_feedback", feedback);
            so.ApplyModifiedPropertiesWithoutUndo();

            Transform[] playerPoints = Enumerable.Range(0, 4)
                .Select(i => Point(spawns, "Player" + i, new Vector3((i - 1.5f) * 3f, 0f, -12f), 0f))
                .ToArray();

            so = new SerializedObject(spawner);
            BattleEditorUtility.Set(so, "_networkEvents", events);
            BattleEditorUtility.Set(so, "_fighterPrefab", LoadNetworkObject(BattleContentBuilder.FighterPath));
            BattleEditorUtility.Set(so, "_botPrefab", LoadNetworkObject(BattleContentBuilder.BotPath));
            BattleEditorUtility.Set(so, "_playerPoints", playerPoints);

            (Vector3 position, int slot)[] bots =
            {
                (new Vector3(-6f, 0f, 8f), 0),
                (new Vector3(0f, 0f, 11f), 1),
                (new Vector3(8f, 0f, 18f), 2)
            };
            so.FindProperty("_bots").arraySize = bots.Length;

            for (int i = 0; i < bots.Length; i++)
            {
                BattleEditorUtility.Set(so, $"_bots.Array.data[{i}].Point", Point(spawns, "Bot" + i, bots[i].position, 180f));
                BattleEditorUtility.Set(so, $"_bots.Array.data[{i}].WeaponSlot", bots[i].slot);
                BattleEditorUtility.Set(so, $"_bots.Array.data[{i}].Mode", BotMode.Passive);
            }

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

            so.ApplyModifiedPropertiesWithoutUndo();

            return context;
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

        private static void BuildHud(BattleContext context)
        {
            GameObject canvasObject = new GameObject("[HUD]");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            Transform root = canvasObject.transform;
            Vector2 center = new Vector2(0.5f, 0.5f);
            Vector2 bottomLeft = Vector2.zero;
            Vector2 bottomRight = new Vector2(1f, 0f);
            Vector2 topLeft = new Vector2(0f, 1f);
            Vector2 topRight = Vector2.one;

            CreateImage("Crosshair", root, center, Vector2.zero, new Vector2(6f, 6f), new Color(1f, 1f, 1f, 0.85f));
            BattleEditorUtility.CreateSwingHint(root, context);
            Image combo = CreateImage("ComboIndicator", root, center, new Vector2(0f, -34f), new Vector2(46f, 8f), Color.white);

            CreateImage("HealthBack", root, bottomLeft, new Vector2(40f, 40f), new Vector2(360f, 30f), new Color(0f, 0f, 0f, 0.6f));
            Image healthFill = CreateImage("HealthFill", root, bottomLeft, new Vector2(44f, 44f), new Vector2(352f, 22f), new Color(0.75f, 0.15f, 0.12f));
            MakeFilled(healthFill);
            TMP_Text healthText = CreateText("HealthText", root, bottomLeft, new Vector2(48f, 42f), new Vector2(352f, 26f), 20f, TextAlignmentOptions.MidlineLeft);

            TMP_Text weaponText = CreateText("WeaponText", root, bottomRight, new Vector2(-40f, 76f), new Vector2(600f, 40f), 30f, TextAlignmentOptions.MidlineRight);
            TMP_Text stateText = CreateText("StateText", root, bottomRight, new Vector2(-40f, 40f), new Vector2(600f, 32f), 24f, TextAlignmentOptions.MidlineRight);
            TMP_Text botModeText = CreateText("BotModeText", root, topRight, new Vector2(-40f, -40f), new Vector2(600f, 32f), 24f, TextAlignmentOptions.MidlineRight);

            HudPanelView drawPanel = CreatePanel("DrawPanel", root, center, new Vector2(0f, -70f), new Vector2(220f, 12f));
            CreateImage("Back", drawPanel.transform, center, Vector2.zero, new Vector2(220f, 12f), new Color(0f, 0f, 0f, 0.6f));
            Image drawFill = CreateImage("Fill", drawPanel.transform, center, Vector2.zero, new Vector2(216f, 8f), new Color(1f, 0.85f, 0.3f));
            MakeFilled(drawFill);

            HudPanelView deathPanel = CreatePanel("DeathPanel", root, center, new Vector2(0f, 120f), new Vector2(900f, 200f));
            TMP_Text deathText = CreateText("Text", deathPanel.transform, center, Vector2.zero, new Vector2(900f, 200f), 64f, TextAlignmentOptions.Center);
            deathText.color = new Color(0.9f, 0.2f, 0.15f);

            HudPanelView helpPanel = CreatePanel("HelpPanel", root, topLeft, new Vector2(30f, -30f), new Vector2(760f, 300f));
            CreateImage("Back", helpPanel.transform, topLeft, Vector2.zero, new Vector2(760f, 300f), new Color(0f, 0f, 0f, 0.45f));
            TMP_Text help = CreateText("Text", helpPanel.transform, topLeft, new Vector2(16f, -12f), new Vector2(730f, 280f), 22f, TextAlignmentOptions.TopLeft);
            help.text = HelpText;

            drawPanel.gameObject.SetActive(false);
            deathPanel.gameObject.SetActive(false);

            SerializedObject so = new SerializedObject(canvasObject.AddComponent<BattleHudView>());
            BattleEditorUtility.Set(so, "_context", context);
            BattleEditorUtility.Set(so, "_healthFill", healthFill);
            BattleEditorUtility.Set(so, "_healthText", healthText);
            BattleEditorUtility.Set(so, "_weaponText", weaponText);
            BattleEditorUtility.Set(so, "_stateText", stateText);
            BattleEditorUtility.Set(so, "_botModeText", botModeText);
            BattleEditorUtility.Set(so, "_comboIndicator", combo);
            BattleEditorUtility.Set(so, "_drawPanel", drawPanel);
            BattleEditorUtility.Set(so, "_drawFill", drawFill);
            BattleEditorUtility.Set(so, "_deathPanel", deathPanel);
            BattleEditorUtility.Set(so, "_deathText", deathText);
            BattleEditorUtility.Set(so, "_helpPanel", helpPanel);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            return rect;
        }

        private static Image CreateImage(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            Image image = CreateRect(name, parent, anchor, position, size).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            return image;
        }

        private static void MakeFilled(Image image)
        {
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
        }

        private static TMP_Text CreateText(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, float fontSize,
            TextAlignmentOptions alignment)
        {
            TextMeshProUGUI text = CreateRect(name, parent, anchor, position, size).gameObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.text = string.Empty;

            return text;
        }

        private static HudPanelView CreatePanel(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            return CreateRect(name, parent, anchor, position, size).gameObject.AddComponent<HudPanelView>();
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
