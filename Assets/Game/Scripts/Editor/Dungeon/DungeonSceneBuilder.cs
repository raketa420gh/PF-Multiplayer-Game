using System.Linq;
using Fusion;
using Game.Scripts.Battle;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Game.Scripts.Editor.Dungeon
{
    /// Assembles DungeonScene: dark lighting, Fusion bootstrap, context, director, the dungeon and the gameplay UI.
    /// Players come here from LobbyScene and are dropped into the dungeon as soon as the session has their kit.
    internal static class DungeonSceneBuilder
    {
        public const string ScenePath = "Assets/Game/Scenes/DungeonScene.unity";
        public const string Title = SceneTravel.DungeonTitle;
        private const string ReflectionPath = DungeonTextureBuilder.Folder + "/Reflection.cubemap";
        private const string NightReflectionPath = DungeonTextureBuilder.Folder + "/NightReflection.cubemap";

        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);

            // Darker and hazier than the tavern: the hall is lit by its torches as in Dark and Darker.
            Light fill = SetupLighting(0.55f, 0.018f);
            BuildVolume($"{DungeonContentBuilder.ConfigsFolder}/DungeonVolume.asset", 0.6f);
            Camera camera = BuildCamera();
            GameObject system = new GameObject("[System]");
            NetworkEvents events = system.AddComponent<NetworkEvents>();
            BattleBootstrapper bootstrapper = system.AddComponent<BattleBootstrapper>();
            SerializedObject so = new SerializedObject(bootstrapper);
            BattleEditorUtility.Set(so, "_sessionName", "Dungeon");
            BattleEditorUtility.Set(so, "_playerCount", GameServer.MaxCapacity);
            so.ApplyModifiedPropertiesWithoutUndo();

            BattleInputPolling input = system.AddComponent<BattleInputPolling>();
            BattleEditorUtility.Set(input, "_networkEvents", events);
            BattleContext battle = system.AddComponent<BattleContext>();
            system.AddComponent<CombatDebugView>();
            BattleFeedback feedback = BuildFeedback(system.transform);
            so = new SerializedObject(battle);
            BattleEditorUtility.Set(so, "_camera", camera);
            BattleEditorUtility.Set(so, "_input", input);
            BattleEditorUtility.Set(so, "_feedback", feedback);
            so.ApplyModifiedPropertiesWithoutUndo();

            ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(DungeonContentBuilder.DatabasePath);
            DungeonConfig config = AssetDatabase.LoadAssetAtPath<DungeonConfig>(DungeonContentBuilder.DungeonConfigPath);
            ClassConfig[] classes = LoadClasses();

            DungeonAdmission admission = system.AddComponent<DungeonAdmission>();
            BattleEditorUtility.Set(admission, "_networkEvents", events);
            DungeonDirector director = system.AddComponent<DungeonDirector>();
            so = new SerializedObject(director);
            BattleEditorUtility.Set(so, "_networkEvents", events);
            BattleEditorUtility.Set(so, "_admission", admission);
            BattleEditorUtility.Set(so, "_sessionPrefab", LoadNetworkObject("PlayerSession"));
            BattleEditorUtility.Set(so, "_adventurerPrefab", LoadNetworkObject("Adventurer"));
            BattleEditorUtility.Set(so, "_matchPrefab", LoadNetworkObject("Match"));
            BattleEditorUtility.Set(so, "_config", config);
            SerializedProperty monsters = so.FindProperty("_monsters");
            (string name, float weight)[] kinds = { ("SkeletonSwordsman", 1f), ("SkeletonArcher", 0.6f), ("SkeletonWarrior", 0.5f), ("FlyingHead", 0.8f) };
            monsters.arraySize = kinds.Length;

            for (int i = 0; i < kinds.Length; i++)
            {
                monsters.GetArrayElementAtIndex(i).FindPropertyRelative("Prefab").objectReferenceValue = LoadNetworkObject(kinds[i].name);
                monsters.GetArrayElementAtIndex(i).FindPropertyRelative("Weight").floatValue = kinds[i].weight;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            DungeonContext context = system.AddComponent<DungeonContext>();
            so = new SerializedObject(context);
            BattleEditorUtility.Set(so, "_battle", battle);
            BattleEditorUtility.Set(so, "_items", database);
            BattleEditorUtility.Set(so, "_classes", classes);
            BattleEditorUtility.Set(so, "_config", config);
            BattleEditorUtility.Set(so, "_director", director);
            so.ApplyModifiedPropertiesWithoutUndo();

            Transform dungeon = DungeonMapBuilder.Build(director, out Texture2D[] floorMaps);
            Transform village = dungeon.Find("Floor1");
            Transform hall = dungeon.Find("Floor2");
            fill.transform.SetParent(hall, true);
            so = new SerializedObject(dungeon.gameObject.AddComponent<FloorVisibilityView>());
            BattleEditorUtility.Set(so, "_context", context);
            BattleEditorUtility.Set(so, "_camera", camera);
            BattleEditorUtility.Set(so, "_floors", new[] { village, hall });
            SerializedProperty atmospheres = so.FindProperty("_atmospheres");
            atmospheres.arraySize = 2;
            // Night over the village: cold moonlit mist that swallows everything past ~100 m (no long sight lines, nothing far
            // to draw); the hall keeps the torch-lit gloom of the dungeon.
            SetAtmosphere(atmospheres.GetArrayElementAtIndex(0), new Color(0.3f, 0.36f, 0.42f), new Color(0.19f, 0.22f, 0.25f), new Color(0.08f, 0.08f, 0.075f),
                new Color(0.19f, 0.235f, 0.26f), 0.021f, 140f, BuildReflection(NightReflectionPath, new Color(0.07f, 0.085f, 0.11f), new Color(0.04f, 0.048f, 0.055f), new Color(0.01f, 0.011f, 0.01f)));
            BuildVillageVolume(village);
            SetAtmosphere(atmospheres.GetArrayElementAtIndex(1), new Color(0.4f, 0.4f, 0.46f) * 0.55f, new Color(0.32f, 0.31f, 0.33f) * 0.55f, new Color(0.22f, 0.2f, 0.18f) * 0.55f,
                new Color(0.035f, 0.035f, 0.045f), 0.018f, 120f, RenderSettings.customReflectionTexture as Cubemap);
            so.ApplyModifiedPropertiesWithoutUndo();
            ApplyAtmosphere(atmospheres.GetArrayElementAtIndex(0), camera);
            DungeonUiBuilder.Build(new DungeonUiBuilder.Inputs
            {
                Context = context,
                Database = database,
                Camera = camera,
                PreviewRig = AssetDatabase.LoadAssetAtPath<GameObject>(DungeonContentBuilder.Prefab("PreviewRig")),
                PieceSet = AssetDatabase.LoadAssetAtPath<ArmorPieceSetConfig>($"{DungeonContentBuilder.ConfigsFolder}/ArmorPieces.asset"),
                FloorMaps = floorMaps,
                ModuleNames = DungeonMapBuilder.ModuleNames,
                Title = Title
            });
            BuildSwarmWall(context);
            BuildAudio(system, context);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();
            Debug.Log($"[{nameof(DungeonSceneBuilder)}] Scene built: {ScenePath}");
        }

        private static void SetAtmosphere(SerializedProperty property, Color sky, Color equator, Color ground, Color fog, float density, float distance, Cubemap reflection)
        {
            property.FindPropertyRelative("Sky").colorValue = sky;
            property.FindPropertyRelative("Equator").colorValue = equator;
            property.FindPropertyRelative("Ground").colorValue = ground;
            property.FindPropertyRelative("Fog").colorValue = fog;
            property.FindPropertyRelative("FogDensity").floatValue = density;
            property.FindPropertyRelative("ViewDistance").floatValue = distance;
            property.FindPropertyRelative("Reflection").objectReferenceValue = reflection;
        }

        /// The scene opens on floor 1: its air is the saved default until the view takes over.
        private static void ApplyAtmosphere(SerializedProperty property, Camera camera)
        {
            RenderSettings.ambientSkyColor = property.FindPropertyRelative("Sky").colorValue;
            RenderSettings.ambientEquatorColor = property.FindPropertyRelative("Equator").colorValue;
            RenderSettings.ambientGroundColor = property.FindPropertyRelative("Ground").colorValue;
            RenderSettings.fogColor = property.FindPropertyRelative("Fog").colorValue;
            RenderSettings.fogDensity = property.FindPropertyRelative("FogDensity").floatValue;
            RenderSettings.customReflectionTexture = property.FindPropertyRelative("Reflection").objectReferenceValue as Cubemap;
            camera.backgroundColor = RenderSettings.fogColor;
            camera.farClipPlane = property.FindPropertyRelative("ViewDistance").floatValue;
        }

        internal static ClassConfig[] LoadClasses()
        {
            // Only the classes of the library: the folder may still hold assets of removed ones.
            return DungeonClassLibrary.CreateClasses()
                .Select(def => AssetDatabase.LoadAssetAtPath<ClassConfig>($"{DungeonContentBuilder.ClassesFolder}/{def.Name}.asset"))
                .ToArray();
        }

        internal static void BuildAudio(GameObject system, DungeonContext context)
        {
            GameObject go = BattleEditorUtility.CreateChild("Audio", system.transform);
            AudioSource music = go.AddComponent<AudioSource>();
            music.playOnAwake = false;
            AudioSource ambient = go.AddComponent<AudioSource>();
            ambient.playOnAwake = false;
            DungeonAudioComponent audio = go.AddComponent<DungeonAudioComponent>();
            string[] names = System.Enum.GetNames(typeof(DungeonSound));

            SerializedObject so = new SerializedObject(audio);
            BattleEditorUtility.Set(so, "_context", context);
            so.FindProperty("_sounds").arraySize = names.Length;

            for (int i = 0; i < names.Length; i++)
                BattleEditorUtility.Set(so, $"_sounds.Array.data[{i}]._clips", DungeonAudioBuilder.Load(names[i]));

            // Stone walls all around: every positioned sound gets the tail of a vaulted room.
            AudioReverbZone reverb = go.AddComponent<AudioReverbZone>();
            reverb.reverbPreset = AudioReverbPreset.StoneCorridor;
            reverb.minDistance = 4000f;
            reverb.maxDistance = 5000f;
            BattleEditorUtility.Set(so, "_menuMusic", AssetDatabase.LoadAssetAtPath<AudioClip>(DungeonAudioBuilder.Path("Menu")));
            BattleEditorUtility.Set(so, "_ambient", AssetDatabase.LoadAssetAtPath<AudioClip>(DungeonAudioBuilder.Path("Ambient")));
            BattleEditorUtility.Set(so, "_music", music);
            BattleEditorUtility.Set(so, "_ambientSource", ambient);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildSwarmWall(DungeonContext context)
        {
            GameObject go = new GameObject("[SwarmWall]");
            MeshFilter filter = go.AddComponent<MeshFilter>();
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = DungeonPropBuilder.SwarmWall;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.lightProbeUsage = LightProbeUsage.Off;

            SerializedObject so = new SerializedObject(go.AddComponent<SwarmWallView>());
            BattleEditorUtility.Set(so, "_context", context);
            BattleEditorUtility.Set(so, "_filter", filter);
            BattleEditorUtility.Set(so, "_renderer", renderer);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// Explicit post-processing profile; the project default volume carries lens flares and test components.
        internal static void BuildVolume(string path, float exposure)
        {
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);

            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            foreach (VolumeComponent component in profile.components)
                Object.DestroyImmediate(component, true);

            profile.components.Clear();
            Add<Tonemapping>(profile).mode.value = TonemappingMode.Neutral;
            Bloom bloom = Add<Bloom>(profile);
            bloom.threshold.value = 1.1f;
            bloom.intensity.value = 0.3f;
            bloom.scatter.value = 0.6f;
            Vignette vignette = Add<Vignette>(profile);
            vignette.intensity.value = 0.18f;
            vignette.smoothness.value = 0.45f;
            ColorAdjustments color = Add<ColorAdjustments>(profile);
            color.postExposure.value = exposure;
            color.contrast.value = 4f;
            color.saturation.value = 0f;
            FilmGrain grain = Add<FilmGrain>(profile);
            grain.type.value = FilmGrainLookup.Thin1;
            grain.intensity.value = 0.15f;
            Add<ScreenSpaceLensFlare>(profile).intensity.value = 0f;
            Add<MotionBlur>(profile).intensity.value = 0f;
            Add<ChromaticAberration>(profile).intensity.value = 0f;
            Add<DepthOfField>(profile).mode.value = DepthOfFieldMode.Off;
            Add<PaniniProjection>(profile).distance.value = 0f;
            Add<LensDistortion>(profile).intensity.value = 0f;
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            Volume volume = new GameObject("[Volume]").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }

        /// The village's own grade on top of the shared one: cold shadows, warm lantern light blooming in the mist.
        /// A child of floor 1, so the floor view switches it off below.
        private static void BuildVillageVolume(Transform village)
        {
            string path = $"{DungeonContentBuilder.ConfigsFolder}/VillageVolume.asset";
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);

            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            foreach (VolumeComponent component in profile.components)
                Object.DestroyImmediate(component, true);

            profile.components.Clear();
            Bloom bloom = Add<Bloom>(profile);
            bloom.threshold.value = 0.85f;
            bloom.intensity.value = 0.65f;
            bloom.scatter.value = 0.72f;
            bloom.tint.value = new Color(1f, 0.85f, 0.7f);
            ShadowsMidtonesHighlights grade = Add<ShadowsMidtonesHighlights>(profile);
            grade.shadows.value = new Vector4(0.86f, 0.97f, 1.1f, 0f);
            grade.highlights.value = new Vector4(1.08f, 1f, 0.9f, 0f);
            Vignette vignette = Add<Vignette>(profile);
            vignette.intensity.value = 0.28f;
            vignette.smoothness.value = 0.5f;
            ColorAdjustments color = Add<ColorAdjustments>(profile);
            color.postExposure.value = 0.75f;
            color.contrast.value = 12f;
            color.saturation.value = -12f;
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            Volume volume = BattleEditorUtility.CreateChild("[Village Volume]", village).AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.sharedProfile = profile;
        }

        private static T Add<T>(VolumeProfile profile) where T : VolumeComponent
        {
            T component = profile.Add<T>(true);
            AssetDatabase.AddObjectToAsset(component, profile);

            return component;
        }

        internal static Light SetupLighting(float brightness = 1f, float fogDensity = 0.008f)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.4f, 0.4f, 0.46f) * brightness;
            RenderSettings.ambientEquatorColor = new Color(0.32f, 0.31f, 0.33f) * brightness;
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.2f, 0.18f) * brightness;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.035f, 0.035f, 0.045f);
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.skybox = null;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = BuildReflection(ReflectionPath, new Color(0.52f, 0.47f, 0.4f), new Color(0.36f, 0.34f, 0.33f), new Color(0.17f, 0.16f, 0.15f));
            RenderSettings.reflectionIntensity = 1f;
            Lightmapping.bakedGI = false;
            Lightmapping.realtimeGI = false;

            // Ambient light alone is flat and hides the relief of the normal maps: a cool shadowless fill from above
            // shapes every surface the torches do not reach.
            Light fill = new GameObject("[Fill Light]").AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.72f, 0.8f, 1f);
            fill.intensity = 0.45f * brightness;
            fill.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(52f, 35f, 0f);

            return fill;
        }

        /// There is no sky underground and metal would mirror blackness: a plain torch-lit gradient gives blades and plate
        /// something to reflect.
        private static Cubemap BuildReflection(string path, Color top, Color horizon, Color bottom)
        {
            const int size = 32;
            Cubemap cubemap = AssetDatabase.LoadAssetAtPath<Cubemap>(path);

            if (cubemap == null)
            {
                cubemap = new Cubemap(size, TextureFormat.RGBA32, true);
                AssetDatabase.CreateAsset(cubemap, path);
            }

            Color[] pixels = new Color[size * size];

            for (int face = 0; face < 6; face++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float a = (x + 0.5f) / size * 2f - 1f;
                        float b = (y + 0.5f) / size * 2f - 1f;
                        float length = Mathf.Sqrt(1f + a * a + b * b);
                        float up = (CubemapFace)face switch
                        {
                            CubemapFace.PositiveY => 1f / length,
                            CubemapFace.NegativeY => -1f / length,
                            _ => -b / length
                        };
                        pixels[y * size + x] = up > 0f ? Color.Lerp(horizon, top, up) : Color.Lerp(horizon, bottom, -up);
                    }
                }

                cubemap.SetPixels(pixels, (CubemapFace)face);
            }

            cubemap.Apply(true);
            EditorUtility.SetDirty(cubemap);

            return cubemap;
        }

        internal static Camera BuildCamera()
        {
            GameObject go = new GameObject("[Camera]") { tag = "MainCamera" };
            go.transform.SetPositionAndRotation(new Vector3(0f, 2f, -4f), Quaternion.identity);
            Camera camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.farClipPlane = 120f;
            camera.nearClipPlane = 0.04f;
            go.AddComponent<AudioListener>();
            UniversalAdditionalCameraData data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;

            return camera;
        }

        internal static NetworkObject LoadNetworkObject(string name)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(DungeonContentBuilder.Prefab(name)).GetComponent<NetworkObject>();
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
            TMPro.TextMeshPro popup = AssetDatabase.LoadAssetAtPath<GameObject>(BattleEditorUtility.PrefabsFolder + "/DamagePopup.prefab").GetComponent<TMPro.TextMeshPro>();

            SerializedObject so = new SerializedObject(feedback);
            BattleEditorUtility.Set(so, "_hitVfx", BuildVfx(go.transform, "HitVfx", particle, new Color(0.9f, 0.15f, 0.1f), new Color(1f, 0.5f, 0.3f), 16, 3f, 0.06f));
            BattleEditorUtility.Set(so, "_blockVfx", BuildVfx(go.transform, "BlockVfx", particle, new Color(1f, 0.9f, 0.5f), Color.white, 24, 5f, 0.035f));
            BattleEditorUtility.Set(so, "_popupPrefab", popup);
            BattleEditorUtility.Set(so, "_audioSource", audio);
            BattleEditorUtility.Set(so, "_hitClips", BattleAudioBuilder.Load(BattleAudioBuilder.Hit));
            BattleEditorUtility.Set(so, "_blockClips", BattleAudioBuilder.Load(BattleAudioBuilder.Block));
            BattleEditorUtility.Set(so, "_worldClips", BattleAudioBuilder.Load(BattleAudioBuilder.Clank));
            BattleEditorUtility.Set(so, "_swingClips", BattleAudioBuilder.Load(BattleAudioBuilder.Swing));
            BattleEditorUtility.Set(so, "_shotClips", BattleAudioBuilder.Load(BattleAudioBuilder.Shot));
            BattleEditorUtility.Set(so, "_weaponSounds", CombatSoundBuilder.LoadSets());
            so.ApplyModifiedPropertiesWithoutUndo();

            return feedback;
        }

        private static ParticleSystem BuildVfx(Transform parent, string name, Material material, Color colorA, Color colorB, int count, float speed, float size)
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
            renderer.shadowCastingMode = ShadowCastingMode.Off;

            return ps;
        }

        private static void AddToBuildSettings()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

            if (scenes.Any(scene => scene.path == ScenePath))
                return;

            EditorBuildSettings.scenes = scenes.Append(new EditorBuildSettingsScene(ScenePath, true)).ToArray();
        }
    }
}
